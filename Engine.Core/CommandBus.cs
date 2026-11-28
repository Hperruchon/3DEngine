using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Assembly;
using Engine.Contracts.Drawing;
using Engine.Contracts.Images;
using Engine.Contracts.Joints;
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

    // ADR-0190 item 2 (TASK-0473): the group whose reversal the session applies now (Reverse), or null.
    private Guid? _reversal;

    // ADR-0033 item 5: each sketch identifier that a commit of this bus created, as the
    // set of handles above does for the bodies.
    private readonly HashSet<Guid> _createdSketches = [];

    // ADR-0045 item 7: each image identifier that a commit of this bus created.
    private readonly HashSet<Guid> _createdImages = [];

    // ADR-0153 item 1: the identifier of each sheet that this bus created, as for an image.
    private readonly HashSet<Guid> _createdSheets = [];
    private readonly HashSet<Guid> _createdDatums = [];

    // ADR-0193 item 6 (phase P11a, TASK-0479): the identifier of each joint that this bus created, as for a sheet.
    private readonly HashSet<Guid> _createdJoints = [];

    // ADR-0193 item 9: for each applied step that changes the placement of a body, each such body and its new placement. A joint
    // places bodies that its command does not name, so the feature list reads the placements of a step here (PlacedBy), and
    // the walks of the steps give each body the placement that the step gave it. A replay and a fork apply each command through
    // a bus, so they make the same table. Finding E251 of the second codebase review of 2026-11-28 (TASK-0488): the key is the
    // place of the entry in the log, because a log can hold one command identifier two times (E17); until TASK-0488 the key was
    // the identifier, and the second entry of a PlaceBody that changed nothing read the placements of the first.
    private readonly Dictionary<int, IReadOnlyList<(Guid Body, BodyPlacement Placement)>> _placedBy = [];

    // ADR-0193 item 9: the placements that the step at the place of the log gave, or null when the Document has no bus or the
    // step changed no placement.
    internal static IReadOnlyList<(Guid Body, BodyPlacement Placement)>? PlacedBy(Document document, int entry)
        => BusOfDocument.TryGetValue(document, out var bus) && bus._placedBy.TryGetValue(entry, out var placed) ? placed : null;

    // ADR-0071 item 5: each reference identifier that a commit of this bus created.
    private readonly HashSet<Guid> _createdReferences = [];

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

    // ADR-0190 item 2 (TASK-0473, finding E242): the Undo of the steps of a failed group, with the group of the proposal, so the
    // log marks it. The session calls it in its serial section, with no cancellation (ADR-0188 item 5).
    internal async Task<CommandResult> Reverse(Guid group)
    {
        _reversal = group;
        try
        {
            return await Apply(new Commands.UndoCommand { GroupId = group }, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _reversal = null;
        }
    }

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
            // stop it in the middle (finding E4). A sink must not throw (IEventSink,
            // finding E18): until TASK-0140 this comment said that a subscriber
            // recovers a lost event with a reset, and no code sends one for it.
            // A retry with the same CommandId gets the cached result while the cache
            // holds it: the cache keeps the last results up to its capacity, 1,024 by default
            // (ADR-0006 §7, finding E31).
            // E100 (TASK-0123): an open (ApplyEntry) and a replay (a bus with no cache) apply an
            // entry of a log, which the guard of a repeated identifier does not refuse.
            var (result, events) = await Decide(command, entry: !lookUp || _idempotency is null, stopwatch, ct).ConfigureAwait(false);

            // Finding E22 of the codebase review of 2026-10-04 (R-0033, TASK-0321): a cancelled result does not go
            // into the cache, so a retry with the same CommandId runs the handler again. Until TASK-0321 the retry got
            // Cancelled with no call of the handler until 1,024 other results took the place of the cancellation.
            if (result.Status != CommandStatus.Cancelled)
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
        bool entry,
        Stopwatch stopwatch,
        CancellationToken ct)
    {
        // The value flows into the handler and goes back at the end of this method.
        Sketch.SketchResults.ApplyingEntry.Value = entry;

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

        // ADR-0071 item 2: a command applies only in a kind of document that its handler permits, so an assembly
        // holds no body and a part holds no reference. The refusal comes before the handler runs.
        if (!Commands.DocumentKinds.Permits(handler, _document.Kind))
            return Reject(command, new ErrorDetail(
                DiagnosticCodes.CommandKindRefused,
                $"The command '{command.Name}' does not apply in a document of the kind '{_document.Kind}' (ADR-0071 item 2)."), stopwatch);

        // Finding E163 of the codebase review of 2026-11-06 (d) (TASK-0242): a caller in the process can give a null value
        // to a required field, and the file then held an entry that an open refuses. The refusal comes before the handler,
        // so the backend and the Document do not change.
        if (handler is Persistence.ICommandEncoder fields
            && handler.Parameters.FirstOrDefault(field => field.Value.Required && fields.Encode(command).GetValueOrDefault(field.Key) is null) is { Key: { } missing })
            return Reject(command, new ErrorDetail(
                DiagnosticCodes.CommandFieldMissing,
                $"The command '{command.Name}' has no value for its required field '{missing}'."), stopwatch);

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
        // Finding E22 of the codebase review of 2026-10-04 (R-0033, TASK-0321): only a cancellation of the caller is
        // Cancelled. An OperationCanceledException with a live token of the caller leaves the bus, as each other
        // exception of a handler does. Until TASK-0321 the bus turned each such exception into Cancelled.
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Cancel(command, stopwatch);
        }

        if (!handlerResult.IsSuccess)
            return Reject(command, handlerResult.Error!, stopwatch, handlerResult.Diagnostics);

        if (handlerResult.History != HistoryMove.None)
            return Move(command, handler, handlerResult, stopwatch, entry);

        RefuseWrongBodyLists(command, handlerResult);
        RefuseWrongSketchLists(command, handlerResult);
        RefuseWrongImageLists(command, handlerResult);
        RefuseWrongDatumLists(command, handlerResult);
        RefuseWrongReferenceLists(command, handlerResult);
        RefuseWrongSheetLists(command, handlerResult);
        RefuseWrongJointLists(command, handlerResult);

        if (Journal is { } journal && Write(journal, command, handler, handlerResult, stopwatch) is { } refused)
            return refused;

        // ADR-0193 item 9: the placements of the step, before the log holds its command.
        var placed = handlerResult.ChangedBodies
            .Where(body => LiveRecord(body.Handle).Placement != body.Placement)
            .Select(body => (body.Handle.Id, Placements.Of(body)))
            .ToList();
        if (placed.Count > 0)
            _placedBy[_document.Log.Count] = placed;

        // ADR-0031 item 3 and ADR-0033 item 10: the records before the commit, so that an
        // Undo can give them back.
        var step = new Step(
            command.CommandId,
            command.GroupId,
            handlerResult.CreatedBodies,
            handlerResult.ConsumedBodies.Select(LiveRecord).ToList(),
            handlerResult.ChangedBodies.Select(body => (LiveRecord(body.Handle), body)).ToList(),
            handlerResult.CreatedSketches,
            handlerResult.ChangedSketches.Select(sketch => (LiveSketch(sketch.Id), sketch)).ToList(),
            handlerResult.CreatedImages,
            handlerResult.ChangedImages.Select(image => (LiveImage(image.Id), image)).ToList(),
            handlerResult.CreatedDatums,
            handlerResult.ChangedDatums.Select(datum => (LiveDatum(datum.Id), datum)).ToList(),
            handlerResult.CreatedReferences,
            handlerResult.CreatedSheets,
            handlerResult.ChangedSheets.Select(sheet => (LiveSheet(sheet.Id), sheet)).ToList(),
            handlerResult.RemovedSheets.Select(LiveSheet).ToList(),
            handlerResult.CreatedJoints,
            handlerResult.ChangedJoints.Select(joint => (LiveJoint(joint.Id), joint)).ToList(),
            handlerResult.RemovedJoints.Select(LiveJoint).ToList());
        var committed = Commit(
            command,
            [new Effect(
                handlerResult.CreatedBodies, handlerResult.ConsumedBodies, [], handlerResult.ChangedBodies,
                handlerResult.CreatedSketches, [], handlerResult.ChangedSketches,
                handlerResult.CreatedImages, [], handlerResult.ChangedImages,
                handlerResult.CreatedDatums, [], handlerResult.ChangedDatums,
                handlerResult.CreatedReferences, [],
                handlerResult.CreatedSheets, handlerResult.RemovedSheets, handlerResult.ChangedSheets,
                handlerResult.CreatedJoints, handlerResult.RemovedJoints, handlerResult.ChangedJoints)],
            handlerResult.Outputs, handlerResult.Diagnostics, stopwatch);
        _history.Record(step);
        return committed;
    }

    // ADR-0037 item 8: the fault of a body in an event, or null.
    private static IReadOnlyDictionary<string, object?>? Fault(BodyFault? fault)
        => fault is null ? null : new Dictionary<string, object?> { ["code"] = fault.Code, ["message"] = fault.Message };

    // ADR-0191 item 8 (phase P11.0, TASK-0474): the placement of the body as an object of the seven numbers, or null at the
    // identity, as the fault is null when the last rebuild succeeded.
    private static Dictionary<string, object?> Placed(Dictionary<string, object?> payload, BodyPlacement? placement)
    {
        payload["placement"] = placement is { } p
            ? new Dictionary<string, object?> { ["qw"] = p.Qw, ["qx"] = p.Qx, ["qy"] = p.Qy, ["qz"] = p.Qz, ["dx"] = p.Dx, ["dy"] = p.Dy, ["dz"] = p.Dz }
            : null;
        return payload;
    }

    private BodyRecord LiveRecord(BodyHandle handle)
        => _document.Bodies.First(body => body.Handle.Id == handle.Id);

    private SketchRecord LiveSketch(Guid id)
        => _document.Sketches.First(sketch => sketch.Id == id);

    private ImageRecord LiveImage(Guid id)
        => _document.Images.First(image => image.Id == id);

    private DatumRecord LiveDatum(Guid id)
        => _document.Datums.First(datum => datum.Id == id);

    private SheetRecord LiveSheet(Guid id)
        => _document.Sheets.First(sheet => sheet.Id == id);

    private JointRecord LiveJoint(Guid id)
        => _document.Joints.First(joint => joint.Id == id);

    // ADR-0031 items 4, 7 and 8: an Undo or a Redo. The bus makes the lists from the
    // step, so RefuseWrongBodyLists does not judge them: a reversal makes a consumed body
    // live again, which ADR-0021 item 1, as amended, permits only here. No call of the
    // backend: each body that a reversal makes live again is still in the backend.
    private (CommandResult, IReadOnlyList<EventRecord>) Move(
        Command command,
        ICommandHandler handler,
        CommandHandlerResult handlerResult,
        Stopwatch stopwatch,
        bool entry)
    {
        if (handlerResult.History is not (HistoryMove.Undo or HistoryMove.Redo)
            || handlerResult.CreatedBodies.Count + handlerResult.ConsumedBodies.Count + handlerResult.ChangedBodies.Count
                + handlerResult.CreatedSketches.Count + handlerResult.ChangedSketches.Count
                + handlerResult.CreatedImages.Count + handlerResult.ChangedImages.Count
                + handlerResult.CreatedDatums.Count + handlerResult.ChangedDatums.Count
                + handlerResult.CreatedReferences.Count
                + handlerResult.CreatedSheets.Count + handlerResult.ChangedSheets.Count + handlerResult.RemovedSheets.Count
                + handlerResult.CreatedJoints.Count + handlerResult.ChangedJoints.Count + handlerResult.RemovedJoints.Count > 0)
            throw new InvalidOperationException(
                $"The handler of '{command.Name}'@{command.SchemaVersion} gives the move {handlerResult.History} and a list of "
                + "bodies, sketches, images, datums or references, or a move that does not exist. ADR-0031 item 7: the bus computes the changes of a move.");

        // ADR-0188 item 4 (phase P16, TASK-0463): a group holds steps only. An Undo or a Redo in a group would move a unit that
        // the group itself builds, and a failed accept could then reverse a step of the person, so the bus refuses it before the
        // history and the file. ADR-0190 items 1 and 2 (TASK-0473, finding E242): the one exception is the reversal of a failed
        // group, an Undo with the group of the proposal. The session applies it (Reverse), and an open or a replay applies it as
        // an entry of the log: the bus refused each other Undo with a group before the file, so each such entry is a reversal.
        var undo = handlerResult.History == HistoryMove.Undo;
        var reversal = undo && command.GroupId is { } marked && (entry || _reversal == marked);
        if (command.GroupId is { } group && !reversal)
            return Reject(command, new ErrorDetail(
                DiagnosticCodes.CommandGroupMove,
                $"The command '{command.Name}' has the group {group}. A group holds steps only, so an Undo or a Redo has no group (ADR-0188 item 4)."),
                stopwatch, handlerResult.Diagnostics);

        if ((undo ? _history.NextUndo : _history.NextRedo) is not { } unit)
            return Reject(command, undo
                ? new ErrorDetail(DiagnosticCodes.CommandNothingToUndo, "The document has no step to undo.")
                : new ErrorDetail(DiagnosticCodes.CommandNothingToRedo, "The document has no undone step to redo."),
                stopwatch, handlerResult.Diagnostics);

        // ADR-0190 item 2: a reversal reverses the steps of its own group only.
        if (reversal && unit[^1].GroupId != command.GroupId)
            return Reject(command, new ErrorDetail(
                DiagnosticCodes.CommandGroupMove,
                $"The Undo of the group {command.GroupId} reverses the steps of that group only, and the last unit of the history has another group (ADR-0190 item 2)."),
                stopwatch, handlerResult.Diagnostics);

        if (Journal is { } journal && Write(journal, command, handler, handlerResult, stopwatch) is { } refused)
            return refused;

        // ADR-0188 item 3: a unit of a group has several steps. The output commandId gives the last step of the unit, which is the
        // step of ADR-0031 item 1 for a unit of one step. An Undo reverses the steps from the last to the first, and a Redo
        // applies them again from the first to the last, each with its changes of ADR-0031 item 4, in one commit.
        var outputs = new Outputs(new Dictionary<string, object?> { ["commandId"] = unit[^1].CommandId });
        if (undo)
        {
            var reversed = Commit(command, [.. unit.Reverse().Select(Reversal)], outputs, handlerResult.Diagnostics, stopwatch);
            // ADR-0190 item 3: the reversal of a failed group puts nothing on the redo list.
            if (reversal)
                _history.Reversed();
            else
                _history.Undone();
            return reversed;
        }

        var applied = Commit(command, [.. unit.Select(Repeat)], outputs, handlerResult.Diagnostics, stopwatch);
        _history.Redone();
        return applied;
    }

    // ADR-0031 item 4: the changes of an Undo of one step.
    private static Effect Reversal(Step step) => new(
        step.Consumed, [], step.Created.Select(body => body.Handle).ToList(),
        step.Changed.Select(change => change.Before).ToList(),
        [], step.CreatedSketches.Select(sketch => sketch.Id).ToList(),
        step.ChangedSketches.Select(change => change.Before).ToList(),
        [], step.CreatedImages.Select(image => image.Id).ToList(),
        step.ChangedImages.Select(change => change.Before).ToList(),
        [], step.CreatedDatums.Select(datum => datum.Id).ToList(),
        step.ChangedDatums.Select(change => change.Before).ToList(),
        [], step.CreatedReferences.Select(reference => reference.Id).ToList(),
        step.RemovedSheets, step.CreatedSheets.Select(sheet => sheet.Id).ToList(),
        step.ChangedSheets.Select(change => change.Before).ToList(),
        step.RemovedJoints, step.CreatedJoints.Select(joint => joint.Id).ToList(),
        step.ChangedJoints.Select(change => change.Before).ToList());

    // ADR-0031 item 4: a Redo of one step gives the changes of its first commit.
    private static Effect Repeat(Step step) => new(
        step.Created, step.Consumed.Select(body => body.Handle).ToList(), [],
        step.Changed.Select(change => change.After).ToList(),
        step.CreatedSketches, [], step.ChangedSketches.Select(change => change.After).ToList(),
        step.CreatedImages, [], step.ChangedImages.Select(change => change.After).ToList(),
        step.CreatedDatums, [], step.ChangedDatums.Select(change => change.After).ToList(),
        step.CreatedReferences, [],
        step.CreatedSheets, step.RemovedSheets.Select(sheet => sheet.Id).ToList(),
        step.ChangedSheets.Select(change => change.After).ToList(),
        step.CreatedJoints, step.RemovedJoints.Select(joint => joint.Id).ToList(),
        step.ChangedJoints.Select(change => change.After).ToList());

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

    // ADR-0045 item 7: the rule of the sketches, for the images. A changed image also keeps
    // its asset.
    private void RefuseWrongImageLists(Command command, CommandHandlerResult result)
    {
        var created = new HashSet<Guid>();
        foreach (var image in result.CreatedImages)
        {
            if (_createdImages.Contains(image.Id) || !created.Add(image.Id))
                throw new InvalidOperationException(
                    $"The handler of '{command.Name}'@{command.SchemaVersion} creates image {image.Id}, which this bus "
                    + "created before or which is in its list two times. ADR-0045 item 7: an identifier is given one time.");
        }

        var changed = new HashSet<Guid>();
        foreach (var image in result.ChangedImages)
        {
            var live = _document.Images.FirstOrDefault(candidate => candidate.Id == image.Id);
            if (live is null || created.Contains(image.Id) || !changed.Add(image.Id)
                || live.DatumId != image.DatumId || live.Asset != image.Asset)
                throw new InvalidOperationException(
                    $"The handler of '{command.Name}'@{command.SchemaVersion} changes image {image.Id}, which is not "
                    + "live, which the same command creates, which is in its list two times, or whose datum or asset it "
                    + "changes. ADR-0045 item 7 permits a change of a live image, one time, with the same datum and asset.");
        }
    }

    // ADR-0051 item 4: the rule of the images, for the datums. A changed datum also keeps its
    // reference: a rebuild changes only its frame and its fault.
    private void RefuseWrongDatumLists(Command command, CommandHandlerResult result)
    {
        var created = new HashSet<Guid>();
        foreach (var datum in result.CreatedDatums)
        {
            if (_createdDatums.Contains(datum.Id) || !created.Add(datum.Id))
                throw new InvalidOperationException(
                    $"The handler of '{command.Name}'@{command.SchemaVersion} creates datum {datum.Id}, which this bus "
                    + "created before or which is in its list two times. ADR-0051 item 4: an identifier is given one time.");
        }

        var changed = new HashSet<Guid>();
        foreach (var datum in result.ChangedDatums)
        {
            var live = _document.Datums.FirstOrDefault(candidate => candidate.Id == datum.Id);
            if (live is null || created.Contains(datum.Id) || !changed.Add(datum.Id) || live.Reference != datum.Reference)
                throw new InvalidOperationException(
                    $"The handler of '{command.Name}'@{command.SchemaVersion} changes datum {datum.Id}, which is not "
                    + "in effect, which the same command creates, which is in its list two times, or whose reference it "
                    + "changes. ADR-0051 item 4 permits a change of a datum in effect, one time, with the same reference.");
        }
    }

    // ADR-0071 item 5: the rule of the images, for the references. A reference never changes, so a list has
    // created references only.
    private void RefuseWrongReferenceLists(Command command, CommandHandlerResult result)
    {
        var created = new HashSet<Guid>();
        foreach (var reference in result.CreatedReferences)
        {
            if (_createdReferences.Contains(reference.Id) || !created.Add(reference.Id))
                throw new InvalidOperationException(
                    $"The handler of '{command.Name}'@{command.SchemaVersion} creates reference {reference.Id}, which this bus "
                    + "created before or which is in its list two times. ADR-0071 item 5: an identifier is given one time.");
        }
    }

    // ADR-0153 item 1 (phase P10.1, TASK-0389): the rule of the images, for the sheets. A sheet is in one of the three lists
    // one time; a created identifier is new to this bus; a changed or a removed sheet is live; a changed sheet keeps its
    // identifier, which the record gives.
    private void RefuseWrongSheetLists(Command command, CommandHandlerResult result)
    {
        var seen = new HashSet<Guid>();
        foreach (var sheet in result.CreatedSheets)
        {
            if (_createdSheets.Contains(sheet.Id) || !seen.Add(sheet.Id))
                throw new InvalidOperationException(
                    $"The handler of '{command.Name}'@{command.SchemaVersion} creates sheet {sheet.Id}, which this bus "
                    + "created before or which is in a list two times. ADR-0153 item 1: an identifier is given one time.");
        }

        foreach (var id in result.ChangedSheets.Select(sheet => sheet.Id).Concat(result.RemovedSheets))
        {
            if (!_document.Sheets.Any(sheet => sheet.Id == id) || !seen.Add(id))
                throw new InvalidOperationException(
                    $"The handler of '{command.Name}'@{command.SchemaVersion} changes or removes sheet {id}, which is not live "
                    + "or which is in a list two times. ADR-0153 item 1 permits one change or one removal of a live sheet.");
        }
    }

    // ADR-0193 item 6 (phase P11a, TASK-0479): the rule of the sheets, for the joints.
    private void RefuseWrongJointLists(Command command, CommandHandlerResult result)
    {
        var seen = new HashSet<Guid>();
        foreach (var joint in result.CreatedJoints)
        {
            if (_createdJoints.Contains(joint.Id) || !seen.Add(joint.Id))
                throw new InvalidOperationException(
                    $"The handler of '{command.Name}'@{command.SchemaVersion} creates joint {joint.Id}, which this bus "
                    + "created before or which is in a list two times. ADR-0193 item 6: an identifier is given one time.");
        }

        foreach (var id in result.ChangedJoints.Select(joint => joint.Id).Concat(result.RemovedJoints))
        {
            if (!_document.Joints.Any(joint => joint.Id == id) || !seen.Add(id))
                throw new InvalidOperationException(
                    $"The handler of '{command.Name}'@{command.SchemaVersion} changes or removes joint {id}, which is not in "
                    + "the Document or which is in a list two times. ADR-0193 item 6 permits one change or one removal of a joint.");
        }
    }

    // The changes of one commit to the live bodies, sketches, images, datums and references. Only an
    // Undo removes a body, a sketch, an image, a datum or a reference.
    private sealed record Effect(
        IReadOnlyList<BodyRecord> Created,
        IReadOnlyList<BodyHandle> Consumed,
        IReadOnlyList<BodyHandle> Removed,
        IReadOnlyList<BodyRecord> Changed,
        IReadOnlyList<SketchRecord> CreatedSketches,
        IReadOnlyList<Guid> RemovedSketches,
        IReadOnlyList<SketchRecord> ChangedSketches,
        IReadOnlyList<ImageRecord> CreatedImages,
        IReadOnlyList<Guid> RemovedImages,
        IReadOnlyList<ImageRecord> ChangedImages,
        IReadOnlyList<DatumRecord> CreatedDatums,
        IReadOnlyList<Guid> RemovedDatums,
        IReadOnlyList<DatumRecord> ChangedDatums,
        IReadOnlyList<PartReference> CreatedReferences,
        IReadOnlyList<Guid> RemovedReferences,
        IReadOnlyList<SheetRecord> CreatedSheets,
        IReadOnlyList<Guid> RemovedSheets,
        IReadOnlyList<SheetRecord> ChangedSheets,
        IReadOnlyList<JointRecord> CreatedJoints,
        IReadOnlyList<Guid> RemovedJoints,
        IReadOnlyList<JointRecord> ChangedJoints);

    // 4. Commit: compute each Seq, append the log, register the created bodies,
    // remove the consumed and the removed bodies, then the same for the sketches. The
    // log append adds one to the version (ADR-0020). All events of one commit share
    // consecutive Seqs, in the order of ADR-0031 item 5 and ADR-0033 item 9:
    // command.applied, each body.created, each body.consumed, each body.removed, each
    // body.changed, each sketch.created, each sketch.removed, each sketch.changed, and
    // since ADR-0045 item 8 each image.created, each image.removed, each image.changed, and since
    // ADR-0051 item 9 each datum.created, each datum.removed, each datum.changed, and since ADR-0071
    // item 5 each reference.created and each reference.removed.
    // A subscriber that draws between two events therefore never sees fewer
    // bodies than the result has. Nothing here awaits, therefore nothing here
    // stops in the middle.
    //
    // ADR-0188 item 3 (phase P16, TASK-0463): an Undo or a Redo of a unit of several steps gives one effect for each step. The
    // commit gives command.applied once, then the events of each effect in the order of ADR-0031 item 5, one effect after the
    // other. A unit of one step gives the events of ADR-0031 item 5, as before.
    private (CommandResult, IReadOnlyList<EventRecord>) Commit(
        Command command,
        IReadOnlyList<Effect> effects,
        Outputs outputs,
        IReadOnlyList<Diagnostic> diagnostics,
        Stopwatch stopwatch)
    {
        var count = 1 + effects.Sum(effect => effect.Created.Count + effect.Consumed.Count + effect.Removed.Count + effect.Changed.Count
            + effect.CreatedSketches.Count + effect.RemovedSketches.Count + effect.ChangedSketches.Count
            + effect.CreatedImages.Count + effect.RemovedImages.Count + effect.ChangedImages.Count
            + effect.CreatedSheets.Count + effect.RemovedSheets.Count + effect.ChangedSheets.Count
            + effect.CreatedDatums.Count + effect.RemovedDatums.Count + effect.ChangedDatums.Count
            + effect.CreatedReferences.Count + effect.RemovedReferences.Count
            + effect.CreatedJoints.Count + effect.RemovedJoints.Count + effect.ChangedJoints.Count);
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
        foreach (var effect in effects)
            lastSeq = Apply(command, effect, events, lastSeq);

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

    // The changes of one effect to the Document and their events, after the event with the sequence lastSeq. It gives the
    // sequence of its last event.
    private long Apply(Command command, Effect effect, List<EventRecord> events, long lastSeq)
    {
        var (created, consumed, removed, changed) = (effect.Created, effect.Consumed, effect.Removed, effect.Changed);
        foreach (var body in created)
        {
            _document.AddBody(body);
            _createdHandles.Add(body.Handle.Id);
            lastSeq++;
            events.Add(Event(lastSeq, command, "body.created", Placed(new Dictionary<string, object?>
            {
                ["bodyId"] = body.Handle.Id,
                ["kind"] = body.Kind,
                ["name"] = body.Name,
                ["visible"] = body.Visible,
                ["geometry"] = body.GeometryKey,
                ["fault"] = Fault(body.Fault),
            }, body.Placement)));
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
            events.Add(Event(lastSeq, command, "body.changed", Placed(new Dictionary<string, object?>
            {
                ["bodyId"] = body.Handle.Id,
                ["name"] = body.Name,
                ["visible"] = body.Visible,
                ["geometry"] = body.GeometryKey,
                ["fault"] = Fault(body.Fault),
            }, body.Placement)));
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

        // ADR-0045 item 8: the record of an image is small, so its event carries it.
        foreach (var image in effect.CreatedImages)
        {
            _document.AddImage(image);
            _createdImages.Add(image.Id);
            lastSeq++;
            events.Add(Event(lastSeq, command, "image.created", ImageFields(image)));
        }

        foreach (var id in effect.RemovedImages)
        {
            _document.RemoveImage(id);
            lastSeq++;
            events.Add(Event(lastSeq, command, "image.removed", new Dictionary<string, object?>
            {
                ["imageId"] = id,
            }));
        }

        foreach (var image in effect.ChangedImages)
        {
            _document.ReplaceImage(image);
            lastSeq++;
            events.Add(Event(lastSeq, command, "image.changed", ImageFields(image)));
        }

        // ADR-0051 item 9: a client reads the frame and the fault of a datum with GetDatum.
        foreach (var datum in effect.CreatedDatums)
        {
            _document.AddDatum(datum);
            _createdDatums.Add(datum.Id);
            lastSeq++;
            events.Add(Event(lastSeq, command, "datum.created", new Dictionary<string, object?>
            {
                ["datumId"] = datum.Id,
                ["name"] = datum.Name,
            }));
        }

        foreach (var id in effect.RemovedDatums)
        {
            _document.RemoveDatum(id);
            lastSeq++;
            events.Add(Event(lastSeq, command, "datum.removed", new Dictionary<string, object?>
            {
                ["datumId"] = id,
            }));
        }

        foreach (var datum in effect.ChangedDatums)
        {
            _document.ReplaceDatum(datum);
            lastSeq++;
            events.Add(Event(lastSeq, command, "datum.changed", new Dictionary<string, object?>
            {
                ["datumId"] = datum.Id,
            }));
        }

        // ADR-0071 item 5: the record of a reference is small, so its event carries it.
        foreach (var reference in effect.CreatedReferences)
        {
            _document.AddReference(reference);
            _createdReferences.Add(reference.Id);
            lastSeq++;
            events.Add(Event(lastSeq, command, "reference.created", new Dictionary<string, object?>
            {
                ["referenceId"] = reference.Id,
                ["part"] = reference.Part,
                ["version"] = reference.Version,
                ["x"] = reference.X,
                ["y"] = reference.Y,
                ["z"] = reference.Z,
            }));
        }

        foreach (var id in effect.RemovedReferences)
        {
            _document.RemoveReference(id);
            lastSeq++;
            events.Add(Event(lastSeq, command, "reference.removed", new Dictionary<string, object?>
            {
                ["referenceId"] = id,
            }));
        }

        // ADR-0153 item 1 (phase P10.1, TASK-0389): each sheet.created, each sheet.removed and each sheet.changed, after the
        // reference events.
        foreach (var sheet in effect.CreatedSheets)
        {
            _document.AddSheet(sheet);
            _createdSheets.Add(sheet.Id);
            lastSeq++;
            events.Add(Event(lastSeq, command, "sheet.created", SheetFields(sheet)));
        }

        foreach (var id in effect.RemovedSheets)
        {
            _document.RemoveSheet(id);
            lastSeq++;
            events.Add(Event(lastSeq, command, "sheet.removed", new Dictionary<string, object?>
            {
                ["sheetId"] = id,
            }));
        }

        foreach (var sheet in effect.ChangedSheets)
        {
            _document.ReplaceSheet(sheet);
            lastSeq++;
            events.Add(Event(lastSeq, command, "sheet.changed", SheetFields(sheet)));
        }

        // ADR-0193 item 6 (phase P11a, TASK-0479): each joint.created, each joint.removed and each joint.changed, after the
        // sheet events. The body events of the same step come first, so a subscriber has the placement of each child.
        foreach (var joint in effect.CreatedJoints)
        {
            _document.AddJoint(joint);
            _createdJoints.Add(joint.Id);
            lastSeq++;
            events.Add(Event(lastSeq, command, "joint.created", JointFields(joint)));
        }

        foreach (var id in effect.RemovedJoints)
        {
            _document.RemoveJoint(id);
            lastSeq++;
            events.Add(Event(lastSeq, command, "joint.removed", new Dictionary<string, object?>
            {
                ["jointId"] = id,
            }));
        }

        foreach (var joint in effect.ChangedJoints)
        {
            _document.ReplaceJoint(joint);
            lastSeq++;
            events.Add(Event(lastSeq, command, "joint.changed", JointFields(joint)));
        }

        return lastSeq;
    }

    // ADR-0153 item 1: the record of a sheet is small, so its event carries it.
    private static Dictionary<string, object?> SheetFields(SheetRecord sheet) => new()
    {
        ["sheetId"] = sheet.Id,
        ["name"] = sheet.Name,
        ["paper"] = sheet.Paper,
        ["landscape"] = sheet.Landscape,
        ["thirdAngle"] = sheet.ThirdAngle,
        ["views"] = sheet.Views,
        ["dimensions"] = sheet.Dimensions,
        // ADR-0179 item 3 (phase R30i, TASK-0428): the title block, null for a sheet with no SetSheetTitle.
        ["titleBlock"] = sheet.TitleBlock,
    };

    // ADR-0193 item 6: the record of a joint is small, so its event carries it.
    private static Dictionary<string, object?> JointFields(JointRecord joint) => new()
    {
        ["jointId"] = joint.Id,
        ["kind"] = joint.Kind,
        ["parentBodyId"] = joint.ParentBodyId,
        ["parent"] = joint.Parent,
        ["childBodyId"] = joint.ChildBodyId,
        ["child"] = joint.Child,
        ["value"] = joint.Value,
        ["limits"] = joint.Limits,
        // ADR-0194 item 1 (phase P11b, TASK-0489): "tight" or "loose".
        ["state"] = joint.State,
    };

    private static Dictionary<string, object?> ImageFields(ImageRecord image) => new()
    {
        ["imageId"] = image.Id,
        ["asset"] = image.Asset,
        ["datumId"] = image.DatumId,
        ["originU"] = image.OriginU,
        ["originV"] = image.OriginV,
        ["width"] = image.Width,
        ["height"] = image.Height,
        ["scale"] = image.Scale,
    };

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
