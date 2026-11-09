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
public sealed class Proposals : IDisposable
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

    // The host gives its session and its subscription to the committed events (EngineHost.Subscribe).
    internal Proposals(DocumentSession session, Func<Action<EventRecord>, IDisposable> subscribe)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(subscribe);
        _session = session;
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
    // DocumentSession.Read and gives the version of the Document, so each later commit marks the proposal (item 4).
    internal Proposal? Add(string summary, IReadOnlyList<(ProposedCommand Text, Command Command)> commands, long version)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(commands);
        lock (_lock)
        {
            if (_open.Count >= MostOpen)
                return null;
            var entry = new Entry(Guid.NewGuid(), summary, [.. commands.Select(command => command.Text)], [.. commands.Select(command => command.Command)], version);
            _open.Add(entry);
            return View(entry);
        }
    }

    // Item 2: the agent withdraws an open proposal. A closed proposal, or a proposal that applies now, is refused.
    internal (Proposal? Proposal, bool Found) Withdraw(Guid id)
    {
        lock (_lock)
        {
            if (Of(id) is not { } entry)
                return (null, false);
            if (entry.State != ProposalStates.Open)
                return (null, true);
            return (CloseLocked(entry, ProposalStates.Withdrawn), true);
        }
    }

    // Item 6: the person accepts the proposal in the window. The envelopes apply in their order through the session, as the
    // commands of a person, and each one is one step of undo. The apply stops at the first command that the engine does not
    // apply; the commands before it stay applied, and the proposal is failed with the index and the code of that command.
    // While it applies, the state is applying, and a second accept or a reject of it is refused. The result is null when the
    // proposal is not open. The window calls this off the thread of the frame.
    public async Task<Proposal?> Accept(Guid id, CancellationToken ct = default)
    {
        Entry entry;
        lock (_lock)
        {
            if (Of(id) is not { State: ProposalStates.Open } open)
                return null;
            open.State = ProposalStates.Applying;
            entry = open;
        }

        try
        {
            for (var i = 0; i < entry.Commands.Count; i++)
            {
                var result = await _session.Apply(entry.Commands[i], ct).ConfigureAwait(false);
                lock (_lock)
                {
                    entry.Results.Add(result);
                    if (result.Status != CommandStatus.Applied)
                    {
                        (entry.FailedIndex, entry.FailedCode) = (i, result.Error?.Code);
                        return CloseLocked(entry, ProposalStates.Failed);
                    }
                }
            }

            lock (_lock)
                return CloseLocked(entry, ProposalStates.Accepted);
        }
        catch
        {
            // A session that closed, or a cancellation before a command started: the proposal is failed at the command that
            // did not run, with no code, and the exception goes to the caller.
            lock (_lock)
            {
                entry.FailedIndex = entry.Results.Count;
                CloseLocked(entry, ProposalStates.Failed);
            }

            throw;
        }
    }

    // Item 6: the person rejects the proposal; no command applies. The result is null when the proposal is not open.
    public Proposal? Reject(Guid id)
    {
        lock (_lock)
            return Of(id) is { State: ProposalStates.Open } entry ? CloseLocked(entry, ProposalStates.Rejected) : null;
    }

    public void Dispose() => _subscription.Dispose();

    // Item 4: an applied command after the post changes the version of the Document, so each open proposal is stale. The
    // action runs inside the commit; it takes only the lock of the queue.
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
        }
    }

    private Entry? Of(Guid id) => _open.FirstOrDefault(entry => entry.Id == id) ?? _closed.FirstOrDefault(entry => entry.Id == id);

    // Item 3: a closed proposal leaves the queue, and the queue keeps the last 32.
    private Proposal CloseLocked(Entry entry, string state)
    {
        entry.State = state;
        _open.Remove(entry);
        _closed.Add(entry);
        if (_closed.Count > MostKept)
            _closed.RemoveAt(0);
        return View(entry);
    }

    private static Proposal View(Entry entry)
        => new(entry.Id, entry.Summary, entry.State, entry.Version, entry.Stale, entry.Texts, [.. entry.Results], entry.FailedIndex, entry.FailedCode);

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
// proposal names the index of the command that stopped it and its code, or no code when the command did not run.
public sealed record Proposal(
    Guid Id,
    string Summary,
    string State,
    long DocumentVersion,
    bool Stale,
    IReadOnlyList<ProposedCommand> Commands,
    IReadOnlyList<CommandResult> Results,
    int? FailedIndex,
    string? FailedCode);

// ADR-0092 item 2: one envelope of a proposal, as the agent posted it, with the name and the version of its handler and the
// identifier of its command (the identifier of the envelope, or a new one).
public sealed record ProposedCommand(
    string Name,
    int SchemaVersion,
    IReadOnlyDictionary<string, JsonElement> Parameters,
    Guid CommandId,
    long? ExpectedDocumentVersion);
