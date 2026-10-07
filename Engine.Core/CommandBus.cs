using System.Diagnostics;
using System.Runtime.CompilerServices;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Core.Geometry;

namespace Engine.Core;

// Per ADR-0006: serial execution per Document, atomic commit-at-end.
// Per ADR-0006 §7: duplicate CommandId returns the cached CommandResult.
// Per ADR-0008 §2: every Apply returns a structured CommandResult.
// Per ADR-0005: events have monotonic Seq. Per ADR-0020: Document.Version counts applied
// commands. The bus keeps Seq; the Document keeps the version; neither is computed
// from the other.
// Per ADR-0012 §2: bus owns the active backend and passes it to every Handle call.
//
// DocumentSession puts commands, queries and reads of one Document in one serial
// section (TASK-0034). The semaphore here stays for the callers with no session,
// Replay and the tests; inside a session it is always free.
public sealed class CommandBus
{
    // One bus for each Document (finding E10). The sequence counter belongs to the
    // bus, therefore a second bus on the same Document starts the sequence at 1
    // again. The table holds each Document weakly, so a Document that nobody uses
    // can go.
    private static readonly ConditionalWeakTable<Document, CommandBus> BusOfDocument = new();

    private readonly Document _document;
    private readonly CommandRegistry _registry;
    private readonly IEventSink _events;
    private readonly IdempotencyCache? _idempotency;
    private readonly IGeometryBackend _backend;
    private readonly SemaphoreSlim _serial = new(1, 1);
    private long _nextSeq = 1;

    public CommandBus(
        Document document,
        CommandRegistry registry,
        IEventSink events,
        IGeometryBackend? backend = null,
        IdempotencyCache? idempotency = null)
        : this(document, registry, events, backend, idempotency ?? new IdempotencyCache(), deduplicate: true)
    {
    }

    // A replay applies each entry of a log one time. The cache protects against a
    // transport that sends a command again (ADR-0006 §7); a log is not a
    // transport, and a live cache of 1,024 results can let one CommandId enter
    // the log two times (finding E17 of the codebase review of 2026-10-04).
    internal static CommandBus ForReplay(
        Document document,
        CommandRegistry registry,
        IEventSink events,
        IGeometryBackend? backend)
        => new(document, registry, events, backend, idempotency: null, deduplicate: false);

    private CommandBus(
        Document document,
        CommandRegistry registry,
        IEventSink events,
        IGeometryBackend? backend,
        IdempotencyCache? idempotency,
        bool deduplicate)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _backend = backend ?? NullGeometryBackend.Instance;
        _idempotency = deduplicate ? idempotency : null;

        if (!BusOfDocument.TryAdd(_document, this))
            throw new InvalidOperationException(
                "The Document already has a command bus. Use the DocumentSession of the Document.");
    }

    public Document Document => _document;

    public async Task<CommandResult> Apply(Command command, CancellationToken ct = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        var stopwatch = Stopwatch.StartNew();
        await _serial.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_idempotency is not null && _idempotency.TryGet(command.CommandId, out var cached))
                return cached;

            // Change, then publish (TASK-0034, scope item 7; the owner accepted it on
            // 2026-09-30). Decide changes the Document, and the result goes into the
            // cache. Only then do the events go to the sink, with
            // CancellationToken.None: the commit is done, and a cancellation must not
            // stop it in the middle (finding E4). A sink that throws loses an event,
            // which a subscriber recovers with a reset, and never a part of the
            // Document. A retry with the same CommandId gets the cached result.
            var (result, events) = await Decide(command, stopwatch, ct).ConfigureAwait(false);
            _idempotency?.Store(command.CommandId, result);

            foreach (var record in events)
                await _events.Append(record, CancellationToken.None).ConfigureAwait(false);

            return result;
        }
        finally
        {
            _serial.Release();
        }
    }

    private async Task<(CommandResult Result, IReadOnlyList<EventRecord> Events)> Decide(
        Command command,
        Stopwatch stopwatch,
        CancellationToken ct)
    {
        // 1. Lookup
        if (!_registry.TryFind(command.Name, command.SchemaVersion, out var handler))
        {
            var error = new ErrorDetail(
                DiagnosticCodes.CommandUnknown,
                $"No handler registered for '{command.Name}'@{command.SchemaVersion}.");
            return Reject(command, error, stopwatch);
        }

        // 2. Optimistic version check
        if (command.ExpectedDocumentVersion is { } expected && expected != _document.Version)
        {
            var error = new ErrorDetail(
                DiagnosticCodes.CommandVersionStale,
                $"Expected document version {expected} but document is at {_document.Version}.");
            return Reject(command, error, stopwatch);
        }

        // 3. Run handler. No mutation of Document until commit-at-end. The handler
        // is the last step that observes the token.
        CommandHandlerResult handlerResult;
        try
        {
            handlerResult = await handler.Handle(command, _document, _backend, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return Cancel(command, stopwatch);
        }

        if (!handlerResult.IsSuccess)
            return Reject(command, handlerResult.Error!, stopwatch, handlerResult.Diagnostics);

        RefuseWrongConsumedList(command, handlerResult.ConsumedBodies);
        return Commit(command, handlerResult, stopwatch);
    }

    // ADR-0021 item 2: a handler consumes only a live body, and it gives each
    // handle one time. A list that breaks the rule is a defect in the handler,
    // not a rejection of the command, so the bus throws. It throws before the
    // commit, so the Document does not change.
    private void RefuseWrongConsumedList(Command command, IReadOnlyList<BodyHandle> consumed)
    {
        var seen = new HashSet<Guid>();
        foreach (var handle in consumed)
        {
            if (!_document.HasBody(handle) || !seen.Add(handle.Id))
                throw new InvalidOperationException(
                    $"The handler of '{command.Name}'@{command.SchemaVersion} consumes body {handle.Id}, which is "
                    + "not live or is in its list two times. ADR-0021 item 2 permits a live body, one time.");
        }
    }

    // 4. Commit: compute each Seq, append the log, register the created bodies,
    // remove the consumed bodies. The log append adds one to the version
    // (ADR-0020). All events of one commit share consecutive Seqs, in the order
    // of ADR-0021 item 5: command.applied, each body.created, each body.consumed.
    // A subscriber that draws between two events therefore never sees fewer
    // bodies than the result has. Nothing here awaits, therefore nothing here
    // stops in the middle.
    private (CommandResult, IReadOnlyList<EventRecord>) Commit(
        Command command,
        CommandHandlerResult handlerResult,
        Stopwatch stopwatch)
    {
        var count = 1 + handlerResult.CreatedBodies.Count + handlerResult.ConsumedBodies.Count;
        var appliedSeq = TakeSeqs(count);
        var events = new List<EventRecord>(count)
        {
            Event(appliedSeq, command, "command.applied", new Dictionary<string, object?>
            {
                ["name"] = command.Name,
                ["schemaVersion"] = command.SchemaVersion,
            }),
        };

        _document.AppendCommand(command);

        var lastSeq = appliedSeq;
        foreach (var body in handlerResult.CreatedBodies)
        {
            _document.AddBody(body);
            lastSeq++;
            events.Add(Event(lastSeq, command, "body.created", new Dictionary<string, object?>
            {
                ["bodyId"] = body.Handle.Id,
                ["kind"] = body.Kind,
            }));
        }

        foreach (var handle in handlerResult.ConsumedBodies)
        {
            _document.RemoveBody(handle);
            lastSeq++;
            events.Add(Event(lastSeq, command, "body.consumed", new Dictionary<string, object?>
            {
                ["bodyId"] = handle.Id,
            }));
        }

        var result = new CommandResult(
            CommandId: command.CommandId,
            CommandName: command.Name,
            Status: CommandStatus.Applied,
            AppliedAtSeq: appliedSeq,
            DocumentVersion: _document.Version,
            Outputs: handlerResult.Outputs,
            Diagnostics: handlerResult.Diagnostics,
            Error: null,
            DurationMs: stopwatch.ElapsedMilliseconds);
        return (result, events);
    }

    private (CommandResult, IReadOnlyList<EventRecord>) Reject(
        Command command,
        ErrorDetail error,
        Stopwatch stopwatch,
        IReadOnlyList<Diagnostic>? diagnostics = null)
    {
        // A rejection does not change the version (ADR-0020) and takes the next Seq.
        var seq = TakeSeqs(1);

        var record = Event(seq, command, "command.rejected", new Dictionary<string, object?>
        {
            ["name"] = command.Name,
            ["schemaVersion"] = command.SchemaVersion,
            ["errorCode"] = error.Code,
        });

        var result = new CommandResult(
            CommandId: command.CommandId,
            CommandName: command.Name,
            Status: CommandStatus.Rejected,
            AppliedAtSeq: null,
            DocumentVersion: _document.Version,
            Outputs: Outputs.Empty,
            Diagnostics: diagnostics ?? Array.Empty<Diagnostic>(),
            Error: error,
            DurationMs: stopwatch.ElapsedMilliseconds);
        return (result, [record]);
    }

    private (CommandResult, IReadOnlyList<EventRecord>) Cancel(Command command, Stopwatch stopwatch)
    {
        var seq = TakeSeqs(1);

        var record = Event(seq, command, "command.cancelled", new Dictionary<string, object?>
        {
            ["name"] = command.Name,
            ["schemaVersion"] = command.SchemaVersion,
        });

        var result = new CommandResult(
            CommandId: command.CommandId,
            CommandName: command.Name,
            Status: CommandStatus.Cancelled,
            AppliedAtSeq: null,
            DocumentVersion: _document.Version,
            Outputs: Outputs.Empty,
            Diagnostics: Array.Empty<Diagnostic>(),
            Error: null,
            DurationMs: stopwatch.ElapsedMilliseconds);
        return (result, [record]);
    }

    // Takes count consecutive Seqs and gives the first. Until TASK-0035 this
    // method also refused a Seq at or below the version, which coupled the two
    // counters; ADR-0020 §2 separates them, and the version can no longer go back.
    private long TakeSeqs(int count)
    {
        var first = _nextSeq;
        _nextSeq += count;
        return first;
    }

    private EventRecord Event(long seq, Command command, string kind, Dictionary<string, object?> payload) => new(
        Seq: seq,
        Timestamp: DateTime.UtcNow,
        DocumentId: _document.DocumentId,
        CauseCommandId: command.CommandId,
        Kind: kind,
        Payload: payload);
}
