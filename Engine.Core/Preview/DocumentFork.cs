using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core.Assets;
using Engine.Core.Hosting;

namespace Engine.Core.Preview;

// ADR-0185 item 2 (phase P15a, TASK-0456): the fork of a Document for the preview of a proposal. The owner allowed a second
// Document in one runtime (R-0055, ADR-0071), and chose its form on 2026-11-22 (Q2 A of the third codebase review of
// 2026-11-21): a copy of the log under the serial lock of the first session, then a replay of the copy in a new session with
// its own backend, off that lock.
//
// The fork is a DocumentSession with the in-memory sink of its kit and no broadcaster, so no event of a fork reaches a client
// (item 6). The first Document, its log, its version and its events do not change: the fork reads only its copy of the log.
public static class DocumentFork
{
    // Replays the copy of the log on a new backend of the factory, then applies the commands of the proposal in their order, as
    // an accept does (Proposals.Accept): the apply stops at the first command that the engine does not apply. P15b (TASK-0459,
    // items 8 to 10): the fork then reads the state of the solver, the measures and the checks after the commands, and, when a
    // value before them is necessary, replays the same copy again in a second session (the base) and reads it there. Each
    // session closes and each backend is freed before the next one starts, so at most one backend of a fork lives at a time.
    //
    // A copy that does not replay throws: BackendMismatchException when an entry records another backend (ADR-0015 item 6),
    // and ReplayDivergenceException when an entry does not apply again or gives another version. A factory that throws gives
    // its exception. A cancellation throws OperationCanceledException.
    public static async Task<ForkResult> Run(
        ForkSource source,
        IReadOnlyList<Command> commands,
        Func<IGeometryBackend> backends,
        IAssetSource? assets = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(backends);

        var results = new List<CommandResult>(commands.Count);
        int? failedIndex = null;
        string? failedCode = null;
        BodyDifferences bodies;
        PreviewPlan plan;
        PreviewValues after;

        var backend = backends();
        try
        {
            var session = await Replay(source, backend, assets, ct).ConfigureAwait(false);
            try
            {
                var (bodiesBefore, sketchesBefore) = await session.Read(Records, ct).ConfigureAwait(false);

                for (var i = 0; i < commands.Count; i++)
                {
                    var result = await session.Apply(commands[i], ct).ConfigureAwait(false);
                    if (result.Status == CommandStatus.Cancelled && ct.IsCancellationRequested)
                        throw new OperationCanceledException(ct);

                    // Finding E198, as Proposals.Accept: a command whose identifier the session knows gets the cached result
                    // of the bus, and nothing applies. Its result then gives an earlier version, so the preview stops there
                    // with no result for that command.
                    if (result.Status == CommandStatus.Applied && result.DocumentVersion != source.Version + results.Count + 1)
                    {
                        (failedIndex, failedCode) = (i, DiagnosticCodes.ApiProposalCommandKnown);
                        break;
                    }

                    results.Add(result);
                    if (result.Status != CommandStatus.Applied)
                    {
                        (failedIndex, failedCode) = (i, result.Error?.Code);
                        break;
                    }
                }

                var (bodiesAfter, sketchesAfter) = await session.Read(Records, ct).ConfigureAwait(false);
                bodies = BodyDifferences.Of(bodiesBefore, bodiesAfter);
                plan = PreviewPlan.Of(bodiesBefore, bodiesAfter, sketchesBefore, sketchesAfter);
                after = await PreviewValues.Read(session, plan, after: true, ct).ConfigureAwait(false);
            }
            finally
            {
                session.Close();
            }
        }
        finally
        {
            (backend as IDisposable)?.Dispose();
        }

        // Item 10: the base, the fork before the first command of the proposal. It equals the first Document at its version by
        // the replay rule (ADR-0012), so the first Document is not read again and its serial lock is not taken.
        PreviewValues? before = null;
        if (plan.NeedsBase)
        {
            var baseBackend = backends();
            try
            {
                var session = await Replay(source, baseBackend, assets, ct).ConfigureAwait(false);
                try
                {
                    before = await PreviewValues.Read(session, plan, after: false, ct).ConfigureAwait(false);
                }
                finally
                {
                    session.Close();
                }
            }
            finally
            {
                (baseBackend as IDisposable)?.Dispose();
            }
        }

        var (sketches, measures, checks) = PreviewValues.Differences(plan, after, before);
        return new ForkResult(source.Version, results, failedIndex, failedCode, bodies, sketches, measures, checks);
    }

    // The bodies and the sketches of the Document, as arrays: the function of a read must not keep a reference to the Document.
    private static (BodyRecord[] Bodies, Engine.Contracts.Sketch.SketchRecord[] Sketches) Records(Document document, IEventSink events)
        => ([.. document.Bodies], [.. document.Sketches]);

    // The replay of the copy in a new session on the backend. It applies each entry one time, with no look-up in the
    // idempotency cache, as an open of a document file does (ADR-0015 item 8). Each result still goes into the cache, as in the
    // first session. A replay that throws closes its session.
    private static async Task<DocumentSession> Replay(ForkSource source, IGeometryBackend backend, IAssetSource? assets, CancellationToken ct)
    {
        var session = new DocumentSession(EngineHosting.CreateDefault(backend, source.LengthUnit, source.Kind, assets));
        try
        {
            for (var index = 0; index < source.Log.Count; index++)
            {
                var entry = source.Log[index];
                if (entry.Backend.Name != backend.Identity.Name)
                    throw new BackendMismatchException(index, entry, backend.Identity);

                CommandResult replayed;
                try
                {
                    replayed = await session.ApplyEntry(entry.Command, ct).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
                {
                    throw new ReplayDivergenceException(index, entry.Command, ex);
                }

                if (replayed.Status == CommandStatus.Cancelled && ct.IsCancellationRequested)
                    throw new OperationCanceledException(ct);
                if (replayed.Status != CommandStatus.Applied || replayed.DocumentVersion != index + 1)
                    throw new ReplayDivergenceException(index, entry.Command, replayed);
            }

            return session;
        }
        catch
        {
            session.Close();
            throw;
        }
    }
}

// Item 2 (Q2 A): the copy of the log of a Document at one version, with its unit and its kind. Of must run inside
// DocumentSession.Read, so the copy is one state of the log. A log entry does not change, so the copy holds the entries.
public sealed record ForkSource(long Version, IReadOnlyList<LogEntry> Log, string LengthUnit, string Kind)
{
    public static ForkSource Of(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new ForkSource(document.Version, [.. document.Log], document.LengthUnit, document.Kind);
    }
}

// Item 2: the outcome of the commands of a proposal on a fork at the version DocumentVersion. Results holds the result of each
// command that ran; FailedIndex and FailedCode name the command that stopped the apply, as in a failed accept.
// Items 8 to 10 (P15b, TASK-0459): Sketches, Measures and Checks give the state of the solver, the measures and the checks.
public sealed record ForkResult(
    long DocumentVersion,
    IReadOnlyList<CommandResult> Results,
    int? FailedIndex,
    string? FailedCode,
    BodyDifferences Bodies,
    IReadOnlyList<SketchChange> Sketches,
    IReadOnlyList<MeasureChange> Measures,
    IReadOnlyList<PairChange> Checks);

// Item 4 (Q5 A): the bodies that the commands add, remove and change, each list in the order of the body identifiers. A body
// changes when its record after the commands is not equal to its record before them. A body record has no list, so the
// equality of the record compares its content (ProposalPreviewTests holds that).
public sealed record BodyDifferences(
    IReadOnlyList<BodyRecord> Added,
    IReadOnlyList<BodyRecord> Removed,
    IReadOnlyList<BodyChange> Changed)
{
    public static BodyDifferences Of(IReadOnlyCollection<BodyRecord> before, IReadOnlyCollection<BodyRecord> after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        var was = before.ToDictionary(body => body.Handle.Id);
        var now = after.ToDictionary(body => body.Handle.Id);
        return new BodyDifferences(
            [.. after.Where(body => !was.ContainsKey(body.Handle.Id)).OrderBy(body => body.Handle.Id)],
            [.. before.Where(body => !now.ContainsKey(body.Handle.Id)).OrderBy(body => body.Handle.Id)],
            [.. after
                .Where(body => was.TryGetValue(body.Handle.Id, out var old) && !old.Equals(body))
                .OrderBy(body => body.Handle.Id)
                .Select(body => new BodyChange(was[body.Handle.Id], body))]);
    }
}

// Item 4: one changed body, its record before the commands and after them. The handle is the same.
public sealed record BodyChange(BodyRecord Before, BodyRecord After);
