using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Assets;
using Engine.Core.Preview;

namespace Engine.Api.Http;

// ADR-0185 (phase P15a, TASK-0456): the preview of each proposal of the queue, on a fork of the Document. The owner answered
// Q2 to Q5 of the third codebase review of 2026-11-21 with option A on 2026-11-22.
//
// - The fork (item 2, Q2 A): a copy of the log under the serial lock of the session, then DocumentFork.Run off that lock, in a
//   new session with a new backend from the forks of the host. The host frees the backend after the fork.
// - When (item 3, Q3 A): the post copies the log for the first preview, and each new version of the Document asks for a new
//   preview of each open proposal. One worker computes the previews one after the other, so at most one fork runs at a time.
//   A burst of commits gives one preview at the last version, not one for each version.
// - The bound (item 5): one preview for each proposal of the queue, so at most 8 open and 32 kept. A closed proposal keeps its
//   last preview and gets no new one; a proposal that leaves the queue takes its preview with it.
// - The cancellation (item 7): a fork that a new version makes old runs to its end, and its preview serves each caller that
//   asked before its copy of the log. So a stream of commits cannot hold a caller for ever. Only Dispose stops a fork: the
//   replay, each command and each query of the fork, and each walk of the check that splits polygons read the token of the stop.
//
// ADR-0187 (phase R25, TASK-0462): a host that draws sets KeepShapes, so each fork also reads the shape of each body with a
// new solid (PreviewShape), and the queue keeps the shapes of the last preview of each open proposal for the window (Kept).
// The answer of the route does not give them.
//
// The worker starts with no flow of the execution context, so it carries no token of the serial section of the session and
// waits for the section like each other caller (DocumentSession, findings E42 and E43). The order of the locks stays the order
// of DocumentSession: the session first, the lock of the queue second.
public sealed partial class Proposals
{
    private readonly Func<IGeometryBackend>? _forks;
    private readonly IAssetSource? _assets;
    private readonly CancellationTokenSource _stop = new();

    // Under _lock: the worker that runs now, or null; true when a version came after the last copy of the log; true after
    // Dispose.
    private Task? _worker;
    private bool _behind;
    private bool _stopped;

    // TASK-0482 (run 46103697245 of the close of TASK-0480): under _lock, the version of the copy of the log that the worker took
    // and did not yet give to the proposals, or -1. Until the hand-over the copy covers each proposal that waits, so a caller in
    // that window does not mark the queue behind.
    private long _copying = -1;

    // TASK-0482: a seam of the tests, null in production. The worker awaits it after each copy of the log and before the
    // hand-over, so a test calls Preview in that window with no timing.
    internal Func<CancellationToken, Task>? AfterCopy { get; set; }

    // True when the host gives a backend for each fork.
    public bool MakesPreviews => _forks is not null;

    // ADR-0187 item 1 (TASK-0462): true when each fork reads the shapes after the commands, for a host that draws. A host sets
    // it before the first proposal; a fork that runs reads the value at its start.
    public bool KeepShapes
    {
        get => Volatile.Read(ref _keepShapes);
        set => Volatile.Write(ref _keepShapes, value);
    }

    private bool _keepShapes;

    // ADR-0187 item 3 (TASK-0462): the last preview of a proposal with its shapes, for the frame of the window: no read of the
    // session and no wait, only the lock of the queue. Null when no proposal of the queue has the identifier, or when its
    // first preview is not complete. Current is false while a fork computes a preview of a later version, or while a later
    // version waits for its fork.
    public KeptPreview? Kept(Guid id)
    {
        lock (_lock)
        {
            if (Of(id) is not { Preview: { } preview } entry)
                return null;
            var current = !Waits(entry) || (entry.Pending is null && !entry.Computing && !_behind);
            return new KeptPreview(preview, entry.Shapes ?? [], entry.ShapesLeftOut, current, entry.Held?.Code);
        }
    }

    // Item 3: the preview of an open or kept proposal, or null when no proposal of the queue has the identifier. For an open
    // proposal the call waits for a preview of the version of the Document at the call, or of a later version. For a closed
    // proposal it gives the last preview, and waits only for a fork that computes it now. The window calls this off the thread
    // of the frame.
    // TASK-0461 (finding E240 of the codebase review of 2026-11-23): a proposal that leaves the queue while the call waits gives
    // its last preview, or null when it has none, as an identifier that no proposal has. The stop of the host cancels the wait
    // with OperationCanceledException and no token of the caller; the route gives E-API-HOST-STOPPED for it. Until TASK-0461
    // a proposal that left the queue cancelled the wait too, and the route gave no error envelope.
    public async Task<ProposalPreview?> Preview(Guid id, CancellationToken ct = default)
    {
        if (_forks is null)
            throw new InvalidOperationException("The host makes no fork of the Document, so it gives no preview (ADR-0185 item 3).");
        lock (_lock)
        {
            if (_stopped)
                throw new OperationCanceledException("The host stopped, so it gives no preview.");
        }

        var version = await _session.Read((document, _) => document.Version, ct).ConfigureAwait(false);
        Waiter waiter;
        lock (_lock)
        {
            if (Of(id) is not { } entry)
                return null;
            waiter = new Waiter(version, new TaskCompletionSource<ProposalPreview?>(TaskCreationOptions.RunContinuationsAsynchronously));
            entry.Waiters.Add(waiter);
            SettleLocked(entry);
            // ADR-0188 item 5: a commit while the group of an accept committed asked for no preview of that proposal, so a caller
            // that waits for a later version than the last preview asks for a copy of the log itself.
            // A copy that a fork already takes costs one more read of the log, and nothing else.
            // TASK-0461 (run 45362050126 of the branch p0187-review-findings): a fork that runs, or a copy that waits, at the
            // version of the call covers it, so the queue is not behind. Until TASK-0461 only the last preview counted: the first
            // preview of a new proposal marked the queue behind while its own fork ran, and Kept gave Current false after that
            // fork until the worker read the log again.
            // TASK-0482 (run 46103697245 of the close of TASK-0480): a copy of the log that the worker took and did not yet give
            // to the proposal covers it too. Until TASK-0482 a call in that window marked the queue behind, so the waiter got
            // the preview of its version and Kept gave Current false until the worker read the log again; on a Windows runner
            // ProposalPreviewWindowTests read Kept in that window.
            if (Waits(entry) && entry.Waiters.Contains(waiter) && Math.Max(Covered(entry), _copying) < version)
                _behind = true;
            if (_stopped)
                waiter.Done.TrySetCanceled(CancellationToken.None);
            KickLocked();
        }

        try
        {
            return await waiter.Done.Task.WaitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            lock (_lock)
                Of(id)?.Waiters.Remove(waiter);
            throw;
        }
    }

    // TASK-0461: the last version that a preview, a fork that runs or a copy that waits gives the proposal, or -1.
    private static long Covered(Entry entry)
        => Math.Max(entry.Preview?.DocumentVersion ?? -1, Math.Max(entry.Pending?.Version ?? -1, entry.Computing ? entry.ComputingVersion : -1));

    // TASK-0461: true while a version came after the last copy of the log, and the count of the callers that wait for the
    // preview of a proposal, for a test.
    internal bool Behind
    {
        get
        {
            lock (_lock)
                return _behind;
        }
    }

    internal int WaitersOf(Guid id)
    {
        lock (_lock)
            return Of(id)?.Waiters.Count ?? 0;
    }

    // Item 7: true after the stop of the host started, for a test that holds a fork until the stop waits for it (TASK-0461).
    internal bool Stopped
    {
        get
        {
            lock (_lock)
                return _stopped;
        }
    }

    // Item 5: the count of the previews that the queue keeps, for the tests.
    internal int KeptPreviews
    {
        get
        {
            lock (_lock)
                return _open.Concat(_closed).Count(entry => entry.Preview is not null);
        }
    }

    // Item 3: the post. The queue keeps the copy of the log for the first preview; the caller holds the serial lock.
    private void AddedLocked(Entry entry, Document document)
    {
        if (_forks is null)
            return;
        entry.Pending = ForkSource.Of(document);
        KickLocked();
    }

    // ADR-0188 item 5 (phase P16, TASK-0463): an open proposal and a proposal whose accept compares get a preview of each new
    // version, because the accept compares the result at the version of its apply. While the group of the accept commits, its own
    // commits ask for no preview of it, so the kept preview stays the result that applied.
    private static bool Waits(Entry entry) => entry.State == ProposalStates.Open || (entry.State == ProposalStates.Applying && !entry.Committing);

    // Item 3: a commit. The action runs inside the commit, so it only marks the queue and starts the worker; it does not wait.
    private void NewVersionLocked()
    {
        if (_forks is null || !_open.Any(Waits))
            return;
        _behind = true;
        KickLocked();
    }

    // Item 5: a proposal that leaves the queue. A caller that waits for its preview gets the last preview, or null (finding
    // E240, TASK-0461).
    private static void DroppedLocked(Entry entry)
    {
        foreach (var waiter in entry.Waiters)
            waiter.Done.TrySetResult(entry.Preview);

        entry.Waiters.Clear();
    }

    // Gives each waiter its preview when the preview is of its version or later, or when the proposal is closed and no fork
    // computes it now or later.
    private static void SettleLocked(Entry entry)
    {
        if (entry.Preview is not { } preview)
            return;
        var last = !Waits(entry) && entry.Pending is null && !entry.Computing;
        entry.Waiters.RemoveAll(waiter => (last || preview.DocumentVersion >= waiter.Version) && waiter.Done.TrySetResult(preview));
    }

    private void KickLocked()
    {
        if (_worker is not null || _stopped || _forks is null)
            return;
        using (ExecutionContext.SuppressFlow())
            _worker = Task.Run(Work);
    }

    // Item 7: Dispose stops the worker and waits for its end, so no fork runs after the host. A fork stops at the next read of
    // the token: before each entry of the replay and each command, in each query of the fork, and at each node of each walk of
    // the check that splits polygons. A command that runs now ends first (ADR-0006 section 4). A factory of backends that
    // waits does not read the token, so the stop waits for it (finding T192 of the codebase review of 2026-11-23, TASK-0461).
    private void StopPreviews()
    {
        Task? worker;
        lock (_lock)
        {
            if (_stopped)
                return;
            _stopped = true;
            worker = _worker;
        }

        _stop.Cancel();
        try
        {
            worker?.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }

        lock (_lock)
        {
            foreach (var entry in _open.Concat(_closed))
            {
                foreach (var waiter in entry.Waiters)
                    waiter.Done.TrySetCanceled(CancellationToken.None);
                entry.Waiters.Clear();
            }
        }

        _stop.Dispose();
    }

    // One round: a copy of the log when a version came, then a fork for each proposal that waits for a preview, the oldest
    // first. The worker ends when no proposal waits and no version came.
    private async Task Work()
    {
        var ct = _stop.Token;
        try
        {
            while (true)
            {
                bool behind;
                lock (_lock)
                    behind = _behind;

                ForkSource? fresh = null;
                if (behind)
                {
                    // Item 2 (Q2 A): the copy of the log under the serial lock. The mark clears in the same section, so a
                    // later commit marks the queue again.
                    fresh = await _session.Read(
                        (document, _) =>
                        {
                            lock (_lock)
                            {
                                _behind = false;
                                _copying = document.Version;
                            }

                            return ForkSource.Of(document);
                        },
                        ct).ConfigureAwait(false);
                    if (AfterCopy is { } afterCopy)
                        await afterCopy(ct).ConfigureAwait(false);
                }

                var jobs = new List<(Entry Entry, ForkSource Source)>();
                lock (_lock)
                {
                    foreach (var entry in _open.Concat(_closed))
                    {
                        if (fresh is not null && Waits(entry)
                            && (entry.Pending?.Version ?? entry.Preview?.DocumentVersion ?? -1) < fresh.Version)
                            entry.Pending = fresh;
                        if (entry.Pending is { } source)
                        {
                            jobs.Add((entry, source));
                            entry.Pending = null;
                            entry.Computing = true;
                            entry.ComputingVersion = source.Version;
                        }
                    }

                    _copying = -1;

                    if (jobs.Count == 0 && !_behind)
                    {
                        _worker = null;
                        return;
                    }
                }

                foreach (var (entry, source) in jobs)
                {
                    var (preview, shapes, leftOut) = await Compute(entry, source, ct).ConfigureAwait(false);
                    lock (_lock)
                    {
                        entry.Computing = false;
                        entry.Preview = preview;
                        // ADR-0187 item 2: only an open proposal keeps its shapes, so at most 8 previews hold shapes. ADR-0188 item 6:
                        // a proposal whose accept runs keeps them too, because an accept that applies nothing shows the new shapes.
                        (entry.Shapes, entry.ShapesLeftOut) = Waits(entry) ? (shapes, leftOut) : (null, 0);
                        SettleLocked(entry);
                    }
                }
            }
        }
        catch (Exception)
        {
            // Dispose stopped the worker, or the session is closed: the host stops. Compute gives each other fault of a fork
            // as a failed preview, so no caller waits for a worker that ended.
            lock (_lock)
            {
                _worker = null;
                _copying = -1;
                foreach (var entry in _open.Concat(_closed))
                {
                    entry.Computing = false;
                    foreach (var waiter in entry.Waiters)
                        waiter.Done.TrySetCanceled(CancellationToken.None);
                    entry.Waiters.Clear();
                }
            }
        }
    }

    // Item 2: one fork. A fork that cannot replay the copy, or that gets no backend, gives a failed preview with
    // E-API-PREVIEW-FAILED and its cause. Only Dispose stops a fork. P15b (TASK-0459): the fork takes the factory of the host and
    // frees each backend that it takes, the backend of the fork and, when items 8 to 10 need it, the backend of the base.
    // ADR-0187 item 1 (TASK-0462): with KeepShapes the fork also gives the shapes after the commands, under the bound of
    // PreviewShape.MostTriangles; a failed preview has no shape.
    private async Task<(ProposalPreview Preview, IReadOnlyList<PreviewShape>? Shapes, int LeftOut)> Compute(Entry entry, ForkSource source, CancellationToken ct)
    {
        try
        {
            var fork = await DocumentFork.Run(source, entry.Commands, _forks!, _assets, ct, KeepShapes ? PreviewShape.MostTriangles : 0).ConfigureAwait(false);
            return (new ProposalPreview(
                entry.Id,
                PreviewStates.Ready,
                fork.DocumentVersion,
                fork.Results,
                fork.FailedIndex,
                fork.FailedCode,
                fork.Bodies,
                fork.Sketches,
                fork.Measures,
                fork.Checks,
                null), fork.Shapes, fork.ShapesLeftOut);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            return (new ProposalPreview(
                entry.Id,
                PreviewStates.Failed,
                source.Version,
                [],
                null,
                null,
                null,
                null,
                null,
                null,
                new ErrorDetail(DiagnosticCodes.ApiPreviewFailed, $"The fork of the Document at the version {source.Version} failed: {ex.Message}")), null, 0);
        }
    }

    // A caller that waits for a preview of the version Version or later.
    private sealed record Waiter(long Version, TaskCompletionSource<ProposalPreview?> Done);
}

// ADR-0185 item 3: the states of a preview. Ready: the fork ran the commands, and Results, FailedIndex, FailedCode and Bodies
// give the outcome. Failed: the fork could not replay the log, and Error gives the code E-API-PREVIEW-FAILED and the cause.
public static class PreviewStates
{
    public const string Ready = "ready";
    public const string Failed = "failed";
}

// ADR-0185 item 3: the preview of a proposal as GET /proposals/{id}/preview gives it. DocumentVersion is the version of the
// Document that the fork copied. Results holds the result of each command that ran on the fork, and FailedIndex and FailedCode
// name the command that stopped it, as in a failed accept (Accept). Bodies gives the bodies that the commands add,
// remove and change (item 4). P15b (TASK-0459): Sketches gives the state of the solver of each sketch that the commands add
// or change (item 8), Measures the measures of each body that they add or change (item 9), and Checks each pair whose state
// changes (item 10). Each of the four is null for a failed preview.
public sealed record ProposalPreview(
    Guid ProposalId,
    string State,
    long DocumentVersion,
    IReadOnlyList<CommandResult> Results,
    int? FailedIndex,
    string? FailedCode,
    BodyDifferences? Bodies,
    IReadOnlyList<SketchChange>? Sketches,
    IReadOnlyList<MeasureChange>? Measures,
    IReadOnlyList<PairChange>? Checks,
    ErrorDetail? Error);

// ADR-0187 item 3 (phase R25, TASK-0462): the last preview of a proposal as the window reads it: the preview of the route, the
// shapes after the commands (empty with no KeepShapes or for a failed preview), the count of the bodies whose shape is above
// the bound, and true when no later version of the Document waits for a new preview. ADR-0188 item 6 (phase P16, TASK-0463):
// Held is the code of the note of the last accept that applied nothing because the result changed, or null.
public sealed record KeptPreview(ProposalPreview Preview, IReadOnlyList<PreviewShape> Shapes, int ShapesLeftOut, bool Current, string? Held = null);
