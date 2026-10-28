using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Sketch;
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

    // Each handle that a commit of this bus created, live or consumed. One bus
    // serves one Document from its first command, so the set is complete
    // (TASK-0058).
    private readonly HashSet<Guid> _createdHandles = [];

    // ADR-0031 item 3: the steps of the Document, for Undo and Redo. One bus serves
    // one Document from its first command, so the history is complete, as the set
    // above is.
    private readonly StepHistory _history = new();

    // ADR-0033 item 5: each sketch identifier that a commit of this bus created, as the
    // set of handles above does for the bodies.
    private readonly HashSet<Guid> _createdSketches = [];

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

    // ADR-0015 item 4: the file that each applied command goes into before its
    // result, or null for a bus with no file (a replay, the command line, a test).
    // The session sets it, in its serial section or before its first command.
    internal Persistence.DocumentJournal? Journal { get; set; }

    public Task<CommandResult> Apply(Command command, CancellationToken ct = default)
        => Apply(command, lookUp: true, ct);

    // ADR-0015 item 8: an open applies each entry of the file one time. The file can
    // hold one CommandId two times (E17), and a look-up in the cache skipped the
    // second entry with no error (finding E75 of the second codebase review of
    // 2026-10-28). The result still goes into the cache, as in a live session.
    internal Task<CommandResult> ApplyEntry(Command command, CancellationToken ct = default)
        => Apply(command, lookUp: false, ct);

    private async Task<CommandResult> Apply(Command command, bool lookUp, CancellationToken ct)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        var stopwatch = Stopwatch.StartNew();
        await _serial.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (lookUp && _idempotency is not null && _idempotency.TryGet(command.CommandId, out var cached))
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

        // ADR-0015 item 4: after a failed write the file is not the log, so the
        // session refuses each later command before its handler runs.
        if (Journal is { Broken: true } broken)
            return Reject(command, new ErrorDetail(
                DiagnosticCodes.IoWriteFailed,
                $"An earlier write of the document file {broken.FilePath} failed, so the session refuses each command."), stopwatch);

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

        if (handlerResult.History != HistoryMove.None)
            return Move(command, handler, handlerResult, stopwatch);

        RefuseWrongBodyLists(command, handlerResult);
        RefuseWrongSketchLists(command, handlerResult);

        if (Journal is { } journal && Write(journal, command, handler, handlerResult, stopwatch) is { } refused)
            return refused;

        // ADR-0031 item 3 and ADR-0033 item 10: the records before the commit, so that an
        // Undo can give them back.
        var step = new Step(
            command.CommandId,
            handlerResult.CreatedBodies,
            handlerResult.ConsumedBodies.Select(LiveRecord).ToList(),
            handlerResult.ChangedBodies.Select(body => (LiveRecord(body.Handle), body)).ToList(),
            handlerResult.CreatedSketches,
            handlerResult.ChangedSketches.Select(sketch => (LiveSketch(sketch.Id), sketch)).ToList());
        var committed = Commit(
            command,
            new Effect(
                handlerResult.CreatedBodies, handlerResult.ConsumedBodies, [], handlerResult.ChangedBodies,
                handlerResult.CreatedSketches, [], handlerResult.ChangedSketches),
            handlerResult.Outputs, handlerResult.Diagnostics, stopwatch);
        _history.Record(step);
        return committed;
    }

    private BodyRecord LiveRecord(BodyHandle handle)
        => _document.Bodies.First(body => body.Handle.Id == handle.Id);

    private SketchRecord LiveSketch(Guid id)
        => _document.Sketches.First(sketch => sketch.Id == id);

    // ADR-0031 items 4, 7 and 8: an Undo or a Redo. The bus makes the lists from the
    // step, so RefuseWrongBodyLists does not judge them: a reversal makes a consumed body
    // live again, which ADR-0021 item 1, as amended, permits only here. No call of the
    // backend: each body that a reversal makes live again is still in the backend.
    private (CommandResult, IReadOnlyList<EventRecord>) Move(
        Command command,
        ICommandHandler handler,
        CommandHandlerResult handlerResult,
        Stopwatch stopwatch)
    {
        if (handlerResult.History is not (HistoryMove.Undo or HistoryMove.Redo)
            || handlerResult.CreatedBodies.Count + handlerResult.ConsumedBodies.Count + handlerResult.ChangedBodies.Count
                + handlerResult.CreatedSketches.Count + handlerResult.ChangedSketches.Count > 0)
            throw new InvalidOperationException(
                $"The handler of '{command.Name}'@{command.SchemaVersion} gives the move {handlerResult.History} and a list of "
                + "bodies or sketches, or a move that does not exist. ADR-0031 item 7: the bus computes the changes of a move.");

        var undo = handlerResult.History == HistoryMove.Undo;
        if ((undo ? _history.NextUndo : _history.NextRedo) is not { } step)
            return Reject(command, undo
                ? new ErrorDetail(DiagnosticCodes.CommandNothingToUndo, "The document has no step to undo.")
                : new ErrorDetail(DiagnosticCodes.CommandNothingToRedo, "The document has no undone step to redo."),
                stopwatch, handlerResult.Diagnostics);

        if (Journal is { } journal && Write(journal, command, handler, handlerResult, stopwatch) is { } refused)
            return refused;

        var outputs = new Outputs(new Dictionary<string, object?> { ["commandId"] = step.CommandId });
        if (undo)
        {
            var reversed = Commit(
                command,
                new Effect(
                    step.Consumed, [], step.Created.Select(body => body.Handle).ToList(),
                    step.Changed.Select(change => change.Before).ToList(),
                    [], step.CreatedSketches.Select(sketch => sketch.Id).ToList(),
                    step.ChangedSketches.Select(change => change.Before).ToList()),
                outputs, handlerResult.Diagnostics, stopwatch);
            _history.Undone();
            return reversed;
        }

        var applied = Commit(
            command,
            new Effect(
                step.Created, step.Consumed.Select(body => body.Handle).ToList(), [],
                step.Changed.Select(change => change.After).ToList(),
                step.CreatedSketches, [], step.ChangedSketches.Select(change => change.After).ToList()),
            outputs, handlerResult.Diagnostics, stopwatch);
        _history.Redone();
        return applied;
    }

    // ADR-0015 item 4: the entry goes into the file before the commit, so each
    // applied command is in the file before its result. A command that the file
    // cannot hold, or a failed write, is a rejection, and the Document does not
    // change. The body that the handler made stays in the backend: a leak, not a
    // wrong Document.
    private (CommandResult, IReadOnlyList<EventRecord>)? Write(
        Persistence.DocumentJournal journal,
        Command command,
        ICommandHandler handler,
        CommandHandlerResult handlerResult,
        Stopwatch stopwatch)
    {
        if (handler is not Persistence.ICommandEncoder encoder)
            throw new InvalidOperationException(
                $"The handler of '{command.Name}'@{command.SchemaVersion} has no encoder, so a document file cannot hold its "
                + "command. ADR-0015 item 11: each handler of HandlerCatalog implements ICommandEncoder.");

        var line = Persistence.EntryCodec.Entry(_document.Version + 1, _backend.Identity, command, encoder.Encode(command), out var refusal);
        if (line is null)
            return Reject(command, new ErrorDetail(DiagnosticCodes.IoNotEncodable, refusal!), stopwatch, handlerResult.Diagnostics);

        try
        {
            journal.Append(line);
            return null;
        }
        catch (IOException ex)
        {
            return Reject(command, new ErrorDetail(DiagnosticCodes.IoWriteFailed, ex.Message), stopwatch, handlerResult.Diagnostics);
        }
    }

    // ADR-0021 item 2: a handler consumes only a live body, and it gives each
    // handle one time. ADR-0021 item 1: a consumed body never becomes live again,
    // so a handler creates only a handle that this bus never created. A list that
    // breaks a rule is a defect in the handler or the backend, not a rejection of
    // the command, so the bus throws. It throws before the commit, so the
    // Document does not change. Until TASK-0058 the bus checked the consumed list
    // only, and a command that created and consumed its own handle was applied
    // with no live body (finding E44 of the codebase review of 2026-10-09).
    private void RefuseWrongBodyLists(Command command, CommandHandlerResult result)
    {
        var consumed = new HashSet<Guid>();
        foreach (var handle in result.ConsumedBodies)
        {
            if (!_document.HasBody(handle) || !consumed.Add(handle.Id))
                throw new InvalidOperationException(
                    $"The handler of '{command.Name}'@{command.SchemaVersion} consumes body {handle.Id}, which is "
                    + "not live or is in its list two times. ADR-0021 item 2 permits a live body, one time.");
        }

        var created = new HashSet<Guid>();
        foreach (var body in result.CreatedBodies)
        {
            // A handle that the same command consumes is live, so this bus created
            // it before; the first clause covers it. TASK-0062 removed a clause that
            // tested it again and never decided (finding E53 of the review of
            // 2026-10-11).
            if (_createdHandles.Contains(body.Handle.Id) || !created.Add(body.Handle.Id))
                throw new InvalidOperationException(
                    $"The handler of '{command.Name}'@{command.SchemaVersion} creates body {body.Handle.Id}, which "
                    + "this bus created before, which the same command consumes, or which is in its list two times. "
                    + "ADR-0021 item 1: a consumed body never becomes live again.");
        }

        // ADR-0028 item 6: a changed body is live, the same command neither creates
        // nor consumes it, it is in the list one time, and its kind does not change.
        var changed = new HashSet<Guid>();
        foreach (var body in result.ChangedBodies)
        {
            var live = _document.Bodies.FirstOrDefault(candidate => candidate.Handle.Id == body.Handle.Id);
            if (live is null || consumed.Contains(body.Handle.Id) || created.Contains(body.Handle.Id)
                || !changed.Add(body.Handle.Id) || live.Kind != body.Kind)
                throw new InvalidOperationException(
                    $"The handler of '{command.Name}'@{command.SchemaVersion} changes body {body.Handle.Id}, which is "
                    + "not live, which the same command creates or consumes, which is in its list two times, or whose "
                    + "kind it changes. ADR-0028 item 6 permits a change of a live body, one time, with the same kind.");
        }
    }

    // ADR-0033 items 5 and 8: a created sketch has an identifier that this bus never
    // gave a sketch, and a changed sketch is live, not created by the same command, in
    // its list one time, with the same identifier and the same datum. A list that
    // breaks a rule is a defect of the handler, so the bus throws before the commit.
    private void RefuseWrongSketchLists(Command command, CommandHandlerResult result)
    {
        var created = new HashSet<Guid>();
        foreach (var sketch in result.CreatedSketches)
        {
            if (_createdSketches.Contains(sketch.Id) || !created.Add(sketch.Id))
                throw new InvalidOperationException(
                    $"The handler of '{command.Name}'@{command.SchemaVersion} creates sketch {sketch.Id}, which this bus "
                    + "created before or which is in its list two times. ADR-0033 item 5: an identifier is given one time.");
        }

        var changed = new HashSet<Guid>();
        foreach (var sketch in result.ChangedSketches)
        {
            var live = _document.Sketches.FirstOrDefault(candidate => candidate.Id == sketch.Id);
            if (live is null || created.Contains(sketch.Id) || !changed.Add(sketch.Id) || live.DatumId != sketch.DatumId)
                throw new InvalidOperationException(
                    $"The handler of '{command.Name}'@{command.SchemaVersion} changes sketch {sketch.Id}, which is not "
                    + "live, which the same command creates, which is in its list two times, or whose datum it changes. "
                    + "ADR-0033 item 8 permits a change of a live sketch, one time, on the same datum.");
        }
    }

    // The changes of one commit to the live bodies and the live sketches. Only an Undo
    // removes a body or a sketch.
    private sealed record Effect(
        IReadOnlyList<BodyRecord> Created,
        IReadOnlyList<BodyHandle> Consumed,
        IReadOnlyList<BodyHandle> Removed,
        IReadOnlyList<BodyRecord> Changed,
        IReadOnlyList<SketchRecord> CreatedSketches,
        IReadOnlyList<Guid> RemovedSketches,
        IReadOnlyList<SketchRecord> ChangedSketches);

    // 4. Commit: compute each Seq, append the log, register the created bodies,
    // remove the consumed and the removed bodies, then the same for the sketches. The
    // log append adds one to the version (ADR-0020). All events of one commit share
    // consecutive Seqs, in the order of ADR-0031 item 5 and ADR-0033 item 9:
    // command.applied, each body.created, each body.consumed, each body.removed, each
    // body.changed, each sketch.created, each sketch.removed, each sketch.changed.
    // A subscriber that draws between two events therefore never sees fewer
    // bodies than the result has. Nothing here awaits, therefore nothing here
    // stops in the middle.
    private (CommandResult, IReadOnlyList<EventRecord>) Commit(
        Command command,
        Effect effect,
        Outputs outputs,
        IReadOnlyList<Diagnostic> diagnostics,
        Stopwatch stopwatch)
    {
        var (created, consumed, removed, changed) = (effect.Created, effect.Consumed, effect.Removed, effect.Changed);
        var count = 1 + created.Count + consumed.Count + removed.Count + changed.Count
            + effect.CreatedSketches.Count + effect.RemovedSketches.Count + effect.ChangedSketches.Count;
        var appliedSeq = TakeSeqs(count);
        var events = new List<EventRecord>(count)
        {
            Event(appliedSeq, command, "command.applied", new Dictionary<string, object?>
            {
                ["name"] = command.Name,
                ["schemaVersion"] = command.SchemaVersion,
            }),
        };

        _document.AppendCommand(new LogEntry(command, _backend.Identity));

        var lastSeq = appliedSeq;
        foreach (var body in created)
        {
            _document.AddBody(body);
            _createdHandles.Add(body.Handle.Id);
            lastSeq++;
            events.Add(Event(lastSeq, command, "body.created", new Dictionary<string, object?>
            {
                ["bodyId"] = body.Handle.Id,
                ["kind"] = body.Kind,
                ["name"] = body.Name,
                ["visible"] = body.Visible,
            }));
        }

        foreach (var handle in consumed)
        {
            _document.RemoveBody(handle);
            lastSeq++;
            events.Add(Event(lastSeq, command, "body.consumed", new Dictionary<string, object?>
            {
                ["bodyId"] = handle.Id,
            }));
        }

        // ADR-0031 item 6: an Undo removes each body that its step created.
        foreach (var handle in removed)
        {
            _document.RemoveBody(handle);
            lastSeq++;
            events.Add(Event(lastSeq, command, "body.removed", new Dictionary<string, object?>
            {
                ["bodyId"] = handle.Id,
            }));
        }

        foreach (var body in changed)
        {
            _document.ReplaceBody(body);
            lastSeq++;
            events.Add(Event(lastSeq, command, "body.changed", new Dictionary<string, object?>
            {
                ["bodyId"] = body.Handle.Id,
                ["name"] = body.Name,
                ["visible"] = body.Visible,
            }));
        }

        // ADR-0033 item 9: a client reads the content of a sketch with GetSketch.
        foreach (var sketch in effect.CreatedSketches)
        {
            _document.AddSketch(sketch);
            _createdSketches.Add(sketch.Id);
            lastSeq++;
            events.Add(Event(lastSeq, command, "sketch.created", new Dictionary<string, object?>
            {
                ["sketchId"] = sketch.Id,
                ["datumId"] = sketch.DatumId,
                ["name"] = sketch.Name,
            }));
        }

        foreach (var id in effect.RemovedSketches)
        {
            _document.RemoveSketch(id);
            lastSeq++;
            events.Add(Event(lastSeq, command, "sketch.removed", new Dictionary<string, object?>
            {
                ["sketchId"] = id,
            }));
        }

        foreach (var sketch in effect.ChangedSketches)
        {
            _document.ReplaceSketch(sketch);
            lastSeq++;
            events.Add(Event(lastSeq, command, "sketch.changed", new Dictionary<string, object?>
            {
                ["sketchId"] = sketch.Id,
            }));
        }

        var result = new CommandResult(
            CommandId: command.CommandId,
            CommandName: command.Name,
            Status: CommandStatus.Applied,
            AppliedAtSeq: appliedSeq,
            DocumentVersion: _document.Version,
            Outputs: outputs,
            Diagnostics: diagnostics,
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

    // The payload is read-only. The broadcaster gives one record to each subscriber, to
    // each action of the host and to the ring of /events; until TASK-0074 an action could
    // change the value that the ring kept (finding E63 of the codebase review of
    // 2026-10-13).
    private EventRecord Event(long seq, Command command, string kind, Dictionary<string, object?> payload) => new(
        Seq: seq,
        Timestamp: DateTime.UtcNow,
        DocumentId: _document.DocumentId,
        CauseCommandId: command.CommandId,
        Kind: kind,
        Payload: new ReadOnlyDictionary<string, object?>(payload));
}
