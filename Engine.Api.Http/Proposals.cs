using System.Text.Json;
using Engine.Contracts;
using Engine.Core;

namespace Engine.Api.Http;

// ADR-0092 items 1, 3, 4 and 6 (TASK-0326): the queue of the proposals of an agent, outside the log. An agent posts a
// proposal over HTTP (ProposalsEndpoint); the person accepts or rejects it in the window. No route calls Accept or Reject,
// so an agent cannot accept its own proposal (item 2).
//
// The queue has its own lock. A snapshot (Waiting, Find) takes only that lock and never the lock of the session, so the frame
// of the window does not wait for a command. The order of the locks is the order of DocumentSession: the session first, this
// lock second. A post adds its proposal inside DocumentSession.Read, and the subscription marks a proposal inside a commit.
// Accept takes this lock only between two commands, never while it waits for the session.
//
// ADR-0185 (phase P15a, TASK-0456): Proposals.Preview.cs gives the preview of each proposal on a fork of the Document.
public sealed partial class Proposals : IDisposable
{
    // Item 3: at most 8 open proposals, first in, first out; the queue keeps the last 32 closed proposals.
    public const int MostOpen = 8;
    public const int MostKept = 32;

    // Item 2: a proposal holds from 1 to 16 envelopes, and a summary of 1 to 120 characters.
    public const int MostCommands = 16;
    public const int MostSummary = 120;

    // The kind of the event of an applied command. Only an applied command adds one to the version (ADR-0020).
    private const string CommandApplied = "command.applied";

    private readonly object _lock = new();
    private readonly DocumentSession _session;
    private readonly List<Entry> _open = new();
    private readonly List<Entry> _closed = new();
    private readonly IDisposable _subscription;

    // The host gives its session and its subscription to the committed events (EngineHost.Subscribe). ADR-0185 item 2: forks
    // makes the backend of each fork of a preview, and assets is the store that the fork reads as the session does; with no
    // forks the queue makes no preview.
    internal Proposals(
        DocumentSession session,
        Func<Action<EventRecord>, IDisposable> subscribe,
        Func<Engine.Contracts.Geometry.IGeometryBackend>? forks = null,
        Engine.Core.Assets.IAssetSource? assets = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(subscribe);
        _session = session;
        _forks = forks;
        _assets = assets;
        _subscription = subscribe(OnEvent);
    }

    // The open proposals and the proposal that applies now, the oldest first. The call does not read the session.
    public IReadOnlyList<Proposal> Waiting()
    {
        lock (_lock)
            return [.. _open.Select(View)];
    }

    // One open or kept proposal, or null.
    public Proposal? Find(Guid id)
    {
        lock (_lock)
            return Of(id) is { } entry ? View(entry) : null;
    }

    // Item 3: adds a proposal at the end of the queue, or gives null when 8 proposals are open. The caller calls this inside
    // DocumentSession.Read and gives the Document, so each later commit marks the proposal (item 4). ADR-0185 item 3: the
    // proposal keeps a copy of the log for its first preview, made here under the serial lock.
    internal Proposal? Add(string summary, IReadOnlyList<(ProposedCommand Text, Command Command)> commands, Document document)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(document);
        lock (_lock)
        {
            if (_open.Count >= MostOpen)
                return null;
            // ADR-0188 item 1 (phase P16, TASK-0463): the identifier of the proposal is the group of each of its commands, so the
            // accept applies them as one step of undo, and the fork of the preview applies the same commands.
            var id = Guid.NewGuid();
            var entry = new Entry(id, summary, [.. commands.Select(command => command.Text)], [.. commands.Select(command => command.Command with { GroupId = id })], document.Version);
            _open.Add(entry);
            AddedLocked(entry, document);
            return View(entry);
        }
    }

    // Item 2: the agent withdraws an open proposal. A closed proposal, or a proposal that applies now, is refused. The state
    // is the state of the proposal before the call, or null when no proposal has the identifier.
    internal (Proposal? Proposal, string? State) Withdraw(Guid id)
    {
        lock (_lock)
        {
            if (Of(id) is not { } entry)
                return (null, null);
            if (entry.State != ProposalStates.Open)
                return (null, entry.State);
            return (CloseLocked(entry, ProposalStates.Withdrawn), ProposalStates.Open);
        }
    }

    // Item 6, as ADR-0188 items 5 to 7 amend it (phase P16, TASK-0463, row P16 of the roadmap): the person accepts the
    // proposal in the window. The result that the person saw is the last preview of the queue (this overload), or the preview
    // of the version that the window gives (the other overload). The result is null when the proposal is not open. While the
    // accept runs, the state is applying, and a second accept or a reject of it is refused. The window calls this off the thread
    // of the frame.
    public Task<Proposal?> Accept(Guid id, CancellationToken ct = default) => Accept(id, last: true, seenVersion: null, ct);

    // ADR-0188 item 6: seenVersion is the version of the preview that the window showed, or null when it showed none. A preview
    // that the queue no longer keeps counts as none, so the accept shows the new preview and applies nothing.
    public Task<Proposal?> Accept(Guid id, long? seenVersion, CancellationToken ct = default) => Accept(id, last: false, seenVersion, ct);

    // ADR-0188 items 5 to 7:
    // - The result at the version now (Q4 A). When the version of the Document is the version of the preview that the person
    //   saw, that preview is the result, by the replay rule (ADR-0012), and no fork runs. Else the worker of the queue runs the
    //   fork at the new version, and the accept waits for it.
    // - The comparison. A result that differs from the result that the person saw, by content (PreviewContent), applies nothing:
    //   the proposal is open again with the new preview and the note W-API-PROPOSAL-RESULT-CHANGED in Held. A result whose
    //   commands stop or whose fork failed applies nothing either, and the proposal is failed with the index and the code.
    // - The apply. DocumentSession.ApplyGroup runs the commands, each with the group of the proposal, in one serial section at the
    //   version of the result. A commit between the comparison and the apply makes the group stale, and the accept compares
    //   again, so nothing applies on an old result.
    // - A failure that the dry run did not give, for example a fault of the backend or a command whose identifier the session
    //   knows (finding E198): the session reverses the group with one Undo, which the log holds, and the proposal is failed
    //   (Q3 A). So each command applies, or none.
    // - A host with no factory of forks gives no preview: the accept applies the group with no comparison.
    private async Task<Proposal?> Accept(Guid id, bool last, long? seenVersion, CancellationToken ct)
    {
        Entry entry;
        ProposalPreview? seen;
        lock (_lock)
        {
            if (Of(id) is not { State: ProposalStates.Open } open)
                return null;
            open.State = ProposalStates.Applying;
            open.Held = null;
            entry = open;
            seen = open.Preview is { } kept && (last || kept.DocumentVersion == seenVersion) ? kept : null;
        }

        try
        {
            while (true)
            {
                long? at = null;
                if (_forks is not null)
                {
                    var result = await ResultNow(entry, seen, ct).ConfigureAwait(false);
                    lock (_lock)
                    {
                        if (seen is null || !PreviewContent.Same(seen, result))
                            return HeldLocked(entry, seen, result);
                        if (result.Error is not null || result.FailedIndex is not null)
                        {
                            (entry.FailedIndex, entry.FailedCode) = (result.FailedIndex, result.FailedCode ?? result.Error?.Code);
                            return CloseLocked(entry, ProposalStates.Failed);
                        }
                    }

                    at = result.DocumentVersion;
                }

                // The flag ends in the same lock as the close, so no fork starts for this proposal between the two.
                lock (_lock)
                    entry.Committing = true;
                var outcome = await _session.ApplyGroup(entry.Commands, at, ct).ConfigureAwait(false);
                lock (_lock)
                {
                    entry.Committing = false;
                    if (outcome.Stale)
                        continue;
                    entry.Results.AddRange(outcome.Results);
                    if (outcome.FailedIndex is not { } failed)
                        return CloseLocked(entry, ProposalStates.Accepted);
                    (entry.FailedIndex, entry.FailedCode) = (failed, outcome.Known ? DiagnosticCodes.ApiProposalCommandKnown : outcome.Results[^1].Error?.Code);
                    return CloseLocked(entry, ProposalStates.Failed);
                }
            }
        }
        catch
        {
            // A session that closed, a cancellation, or a handler that threw (the session reversed the group first): the proposal
            // is failed with no code, and the exception goes to the caller.
            lock (_lock)
            {
                entry.Committing = false;
                if (entry.State == ProposalStates.Applying)
                {
                    entry.FailedIndex = entry.Results.Count;
                    CloseLocked(entry, ProposalStates.Failed);
                }
            }

            throw;
        }
    }

    // ADR-0188 item 5 (Q4 A): the result of the commands at the version of the Document now. The preview that the person saw is
    // the result when its version is the version now; else the accept waits for the preview of the worker at that version or a
    // later one.
    private async Task<ProposalPreview> ResultNow(Entry entry, ProposalPreview? seen, CancellationToken ct)
    {
        var now = await _session.Read((document, _) => document.Version, ct).ConfigureAwait(false);
        if (seen is not null && seen.DocumentVersion == now)
            return seen;
        return await Preview(entry.Id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"The proposal {entry.Id} left the queue while its accept ran.");
    }

    // ADR-0188 item 6: the result differs from the result that the person saw. The proposal is open again, with the new preview,
    // which the worker keeps in the queue, and the note.
    private Proposal HeldLocked(Entry entry, ProposalPreview? seen, ProposalPreview result)
    {
        entry.State = ProposalStates.Open;
        entry.Held = new ErrorDetail(
            DiagnosticCodes.ApiProposalResultChanged,
            seen is null
                ? $"The person saw no preview of the proposal. The preview of the version {result.DocumentVersion} is ready, and nothing applied. Read it, then accept again."
                : $"The result of the commands at the version {result.DocumentVersion} is not the result of the version {seen.DocumentVersion} that the person saw, and nothing applied. Read the new preview, then accept again.");
        SettleLocked(entry);
        return View(entry);
    }

    // Item 6: the person rejects the proposal; no command applies. The result is null when the proposal is not open.
    public Proposal? Reject(Guid id)
    {
        lock (_lock)
            return Of(id) is { State: ProposalStates.Open } entry ? CloseLocked(entry, ProposalStates.Rejected) : null;
    }

    // ADR-0185 item 7: the subscription ends first, then the fork that runs now stops and the queue waits for its end.
    public void Dispose()
    {
        _subscription.Dispose();
        StopPreviews();
    }

    // Item 4: an applied command after the post changes the version of the Document, so each open proposal is stale. The
    // action runs inside the commit; it takes only the lock of the queue. ADR-0185 item 3: a new version asks for a new preview
    // of each open proposal. ADR-0188 item 5: the session finds a known command of an accept by its version (finding E198), so
    // the queue no longer marks the commit of each command of an accept.
    private void OnEvent(EventRecord record)
    {
        if (record.Kind != CommandApplied)
            return;
        lock (_lock)
        {
            foreach (var entry in _open)
            {
                if (entry.State == ProposalStates.Open)
                    entry.Stale = true;
            }

            NewVersionLocked();
        }
    }

    private Entry? Of(Guid id) => _open.FirstOrDefault(entry => entry.Id == id) ?? _closed.FirstOrDefault(entry => entry.Id == id);

    // Item 3: a closed proposal leaves the queue, and the queue keeps the last 32.
    // ADR-0185 item 5: the preview of a proposal leaves the queue with it.
    private Proposal CloseLocked(Entry entry, string state)
    {
        entry.State = state;
        // ADR-0187 item 2 (TASK-0462): a closed proposal keeps its preview (ADR-0185 item 5) but not its shapes, which only the
        // window of a waiting proposal draws.
        (entry.Shapes, entry.ShapesLeftOut) = (null, 0);
        _open.Remove(entry);
        _closed.Add(entry);
        if (_closed.Count > MostKept)
        {
            DroppedLocked(_closed[0]);
            _closed.RemoveAt(0);
        }

        SettleLocked(entry);
        return View(entry);
    }

    private static Proposal View(Entry entry)
        => new(entry.Id, entry.Summary, entry.State, entry.Version, entry.Stale, entry.Texts, [.. entry.Results], entry.FailedIndex, entry.FailedCode, entry.Held);

    private sealed class Entry(Guid id, string summary, IReadOnlyList<ProposedCommand> texts, IReadOnlyList<Command> commands, long version)
    {
        public Guid Id { get; } = id;

        public string Summary { get; } = summary;

        public IReadOnlyList<ProposedCommand> Texts { get; } = texts;

        public IReadOnlyList<Command> Commands { get; } = commands;

        public long Version { get; } = version;

        public string State { get; set; } = ProposalStates.Open;

        public bool Stale { get; set; }

        public List<CommandResult> Results { get; } = new();

        public int? FailedIndex { get; set; }

        public string? FailedCode { get; set; }

        // ADR-0188 item 6 (phase P16, TASK-0463): the note of the last accept that applied nothing because the result changed,
        // or null. The next accept clears it.
        public ErrorDetail? Held { get; set; }

        // ADR-0188 item 5: true while the group of the accept commits, so its own commits ask for no preview of the proposal.
        public bool Committing { get; set; }

        // ADR-0185 items 3 and 5: the last preview, the copy of the log that the next preview starts from, true while a fork
        // computes it, and the callers that wait for a preview.
        public ProposalPreview? Preview { get; set; }

        public Engine.Core.Preview.ForkSource? Pending { get; set; }

        public bool Computing { get; set; }

        public List<Waiter> Waiters { get; } = new();

        // ADR-0187 item 2 (phase R25, TASK-0462): the shapes of the last preview of an open proposal, and the count of the
        // bodies above the bound; a proposal drops its shapes when it closes.
        public IReadOnlyList<Engine.Core.Preview.PreviewShape>? Shapes { get; set; }

        public int ShapesLeftOut { get; set; }
    }
}

// ADR-0092 item 4: the states of a proposal. Open and applying are not closed; the four others are.
public static class ProposalStates
{
    public const string Open = "open";
    public const string Applying = "applying";
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";
    public const string Withdrawn = "withdrawn";
    public const string Failed = "failed";
}

// ADR-0092 item 4: a proposal as the routes give it and the window reads it. DocumentVersion is the version of the Document
// at the post; Stale is true when a command applied after it. Results holds the result of each command that ran, and a failed
// proposal names the index of the command that stopped it and its code, or no code when the command did not run. ADR-0188
// (phase P16, TASK-0463): a failed proposal applied nothing, because one Undo of its group reversed each command that ran.
// Held is the note of the last accept that applied nothing because the result changed (W-API-PROPOSAL-RESULT-CHANGED), or null.
public sealed record Proposal(
    Guid Id,
    string Summary,
    string State,
    long DocumentVersion,
    bool Stale,
    IReadOnlyList<ProposedCommand> Commands,
    IReadOnlyList<CommandResult> Results,
    int? FailedIndex,
    string? FailedCode,
    ErrorDetail? Held = null);

// ADR-0092 item 2: one envelope of a proposal, as the agent posted it, with the name and the version of its handler and the
// identifier of its command (the identifier of the envelope, or a new one).
public sealed record ProposedCommand(
    string Name,
    int SchemaVersion,
    IReadOnlyDictionary<string, JsonElement> Parameters,
    Guid CommandId,
    long? ExpectedDocumentVersion);
