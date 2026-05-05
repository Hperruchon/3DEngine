using System.Diagnostics;
using Engine.Contracts;
using Engine.Contracts.Handlers;

namespace Engine.Core;

// Per ADR-0006: serial execution per Document, atomic commit-at-end.
// Per ADR-0008 §2: every Apply returns a structured CommandResult.
// Per ADR-0005: events have monotonic Seq; Document.Version mirrors last emitted Seq.
public sealed class CommandBus
{
    private readonly Document _document;
    private readonly CommandRegistry _registry;
    private readonly IEventSink _events;
    private readonly SemaphoreSlim _serial = new(1, 1);
    private long _nextSeq = 1;

    public CommandBus(Document document, CommandRegistry registry, IEventSink events)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Document Document => _document;

    public async Task<CommandResult> Apply(Command command, CancellationToken ct = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        var stopwatch = Stopwatch.StartNew();
        await _serial.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // 1. Lookup
            if (!_registry.TryFind(command.Name, command.SchemaVersion, out var handler))
            {
                var error = new ErrorDetail(
                    DiagnosticCodes.CommandUnknown,
                    $"No handler registered for '{command.Name}'@{command.SchemaVersion}.");
                return await Reject(command, error, stopwatch, ct).ConfigureAwait(false);
            }

            // 2. Optimistic version check
            if (command.ExpectedDocumentVersion is { } expected && expected != _document.Version)
            {
                var error = new ErrorDetail(
                    DiagnosticCodes.CommandVersionStale,
                    $"Expected document version {expected} but document is at {_document.Version}.");
                return await Reject(command, error, stopwatch, ct).ConfigureAwait(false);
            }

            // 3. Run handler. No mutation of Document until commit-at-end.
            CommandHandlerResult handlerResult;
            try
            {
                handlerResult = await handler.Handle(command, _document, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return await Cancel(command, stopwatch, ct).ConfigureAwait(false);
            }

            if (!handlerResult.IsSuccess)
            {
                return await Reject(command, handlerResult.Error!, stopwatch, ct, handlerResult.Diagnostics)
                    .ConfigureAwait(false);
            }

            // 4. Commit: append log, emit applied event, advance version.
            var seq = _nextSeq++;
            _document.AppendCommand(command);
            await _events.Append(
                new EventRecord(
                    Seq: seq,
                    Timestamp: DateTime.UtcNow,
                    DocumentId: _document.DocumentId,
                    CauseCommandId: command.CommandId,
                    Kind: "command.applied",
                    Payload: new Dictionary<string, object?>
                    {
                        ["name"] = command.Name,
                        ["schemaVersion"] = command.SchemaVersion,
                    }),
                ct).ConfigureAwait(false);
            _document.AdvanceVersion(seq);

            return new CommandResult(
                CommandId: command.CommandId,
                CommandName: command.Name,
                Status: CommandStatus.Applied,
                AppliedAtSeq: seq,
                DocumentVersion: _document.Version,
                Outputs: handlerResult.Outputs,
                Diagnostics: handlerResult.Diagnostics,
                Error: null,
                DurationMs: stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            _serial.Release();
        }
    }

    private async Task<CommandResult> Reject(
        Command command,
        ErrorDetail error,
        Stopwatch stopwatch,
        CancellationToken ct,
        IReadOnlyList<Diagnostic>? diagnostics = null)
    {
        var seq = _nextSeq++;
        await _events.Append(
            new EventRecord(
                Seq: seq,
                Timestamp: DateTime.UtcNow,
                DocumentId: _document.DocumentId,
                CauseCommandId: command.CommandId,
                Kind: "command.rejected",
                Payload: new Dictionary<string, object?>
                {
                    ["name"] = command.Name,
                    ["schemaVersion"] = command.SchemaVersion,
                    ["errorCode"] = error.Code,
                }),
            ct).ConfigureAwait(false);
        _document.AdvanceVersion(seq);

        return new CommandResult(
            CommandId: command.CommandId,
            CommandName: command.Name,
            Status: CommandStatus.Rejected,
            AppliedAtSeq: null,
            DocumentVersion: _document.Version,
            Outputs: Outputs.Empty,
            Diagnostics: diagnostics ?? Array.Empty<Diagnostic>(),
            Error: error,
            DurationMs: stopwatch.ElapsedMilliseconds);
    }

    private async Task<CommandResult> Cancel(Command command, Stopwatch stopwatch, CancellationToken ct)
    {
        var seq = _nextSeq++;
        // Use a non-cancellable token for the cancellation event itself — we still need to record it.
        await _events.Append(
            new EventRecord(
                Seq: seq,
                Timestamp: DateTime.UtcNow,
                DocumentId: _document.DocumentId,
                CauseCommandId: command.CommandId,
                Kind: "command.cancelled",
                Payload: new Dictionary<string, object?>
                {
                    ["name"] = command.Name,
                    ["schemaVersion"] = command.SchemaVersion,
                }),
            CancellationToken.None).ConfigureAwait(false);
        _document.AdvanceVersion(seq);

        return new CommandResult(
            CommandId: command.CommandId,
            CommandName: command.Name,
            Status: CommandStatus.Cancelled,
            AppliedAtSeq: null,
            DocumentVersion: _document.Version,
            Outputs: Outputs.Empty,
            Diagnostics: Array.Empty<Diagnostic>(),
            Error: null,
            DurationMs: stopwatch.ElapsedMilliseconds);
    }
}
