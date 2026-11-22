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
//   asked before its copy of the log. So a stream of commits cannot hold a caller for ever. Only Dispose stops a fork.
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

    // True when the host gives a backend for each fork.
    public bool MakesPreviews => _forks is not null;

    // Item 3: the preview of an open or kept proposal, or null when no proposal of the queue has the identifier. For an open
    // proposal the call waits for a preview of the version of the Document at the call, or of a later version. For a closed
    // proposal it gives the last preview, and waits only for a fork that computes it now. The window calls this off the thread
    // of the frame.
    public async Task<ProposalPreview?> Preview(Guid id, CancellationToken ct = default)
    {
        if (_forks is null)
            throw new InvalidOperationException("The host makes no fork of the Document, so it gives no preview (ADR-0185 item 3).");

        var version = await _session.Read((document, _) => document.Version, ct).ConfigureAwait(false);
        Waiter waiter;
        lock (_lock)
        {
            if (Of(id) is not { } entry)
                return null;
            waiter = new Waiter(version, new TaskCompletionSource<ProposalPreview>(TaskCreationOptions.RunContinuationsAsynchronously));
            entry.Waiters.Add(waiter);
            SettleLocked(entry);
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

    // Item 3: a commit. The action runs inside the commit, so it only marks the queue and starts the worker; it does not wait.
    private void NewVersionLocked()
    {
        if (_forks is null || !_open.Any(entry => entry.State == ProposalStates.Open))
            return;
        _behind = true;
        KickLocked();
    }

    // Item 5: a proposal that leaves the queue. A caller that waits for its preview gets the last preview, or a cancellation.
    private static void DroppedLocked(Entry entry)
    {
        foreach (var waiter in entry.Waiters)
        {
            if (entry.Preview is { } preview)
                waiter.Done.TrySetResult(preview);
            else
                waiter.Done.TrySetCanceled(CancellationToken.None);
        }

        entry.Waiters.Clear();
    }

    // Gives each waiter its preview when the preview is of its version or later, or when the proposal is closed and no fork
    // computes it now or later.
    private static void SettleLocked(Entry entry)
    {
        if (entry.Preview is not { } preview)
            return;
        var last = entry.State != ProposalStates.Open && entry.Pending is null && !entry.Computing;
        entry.Waiters.RemoveAll(waiter => (last || preview.DocumentVersion >= waiter.Version) && waiter.Done.TrySetResult(preview));
    }

    private void KickLocked()
    {
        if (_worker is not null || _stopped || _forks is null)
            return;
        using (ExecutionContext.SuppressFlow())
            _worker = Task.Run(Work);
    }

    // Item 7: Dispose stops the worker and waits for its end, so no fork runs after the host. A fork stops at its next command;
    // a command that runs now ends first (ADR-0006 section 4).
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
                                _behind = false;
                            return ForkSource.Of(document);
                        },
                        ct).ConfigureAwait(false);
                }

                var jobs = new List<(Entry Entry, ForkSource Source)>();
                lock (_lock)
                {
                    foreach (var entry in _open.Concat(_closed))
                    {
                        if (fresh is not null && entry.State == ProposalStates.Open
                            && (entry.Pending?.Version ?? entry.Preview?.DocumentVersion ?? -1) < fresh.Version)
                            entry.Pending = fresh;
                        if (entry.Pending is { } source)
                        {
                            jobs.Add((entry, source));
                            entry.Pending = null;
                            entry.Computing = true;
                        }
                    }

                    if (jobs.Count == 0 && !_behind)
                    {
                        _worker = null;
                        return;
                    }
                }

                foreach (var (entry, source) in jobs)
                {
                    var preview = await Compute(entry, source, ct).ConfigureAwait(false);
                    lock (_lock)
                    {
                        entry.Computing = false;
                        entry.Preview = preview;
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
    // E-API-PREVIEW-FAILED and its cause. Only Dispose stops a fork.
    private async Task<ProposalPreview> Compute(Entry entry, ForkSource source, CancellationToken ct)
    {
        IGeometryBackend? backend = null;
        try
        {
            backend = _forks!();
            var fork = await DocumentFork.Run(source, entry.Commands, backend, _assets, ct).ConfigureAwait(false);
            return new ProposalPreview(entry.Id, PreviewStates.Ready, fork.DocumentVersion, fork.Results, fork.FailedIndex, fork.FailedCode, fork.Bodies, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            return new ProposalPreview(
                entry.Id,
                PreviewStates.Failed,
                source.Version,
                [],
                null,
                null,
                null,
                new ErrorDetail(DiagnosticCodes.ApiPreviewFailed, $"The fork of the Document at the version {source.Version} failed: {ex.Message}"));
        }
        finally
        {
            (backend as IDisposable)?.Dispose();
        }
    }

    // A caller that waits for a preview of the version Version or later.
    private sealed record Waiter(long Version, TaskCompletionSource<ProposalPreview> Done);
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
// remove and change (item 4). Row P15b adds the state of the solver, the measures and the checks.
public sealed record ProposalPreview(
    Guid ProposalId,
    string State,
    long DocumentVersion,
    IReadOnlyList<CommandResult> Results,
    int? FailedIndex,
    string? FailedCode,
    BodyDifferences? Bodies,
    ErrorDetail? Error);
