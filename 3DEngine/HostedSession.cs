using Engine.Api.Http;
using Engine.Contracts;
using Engine.Core;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.Desktop;

// ADR-0072 item 2 (phase R14e1, TASK-0222): the EngineHost of the surface library as the session of the application.
// The desktop serves the surface on the same host, so the person and an agent work on one document (ADR-0019).
// ADR-0092 item 7 (TASK-0326): the window reads the queue of the proposals of the host through this class, because the
// application project does not reference the surface library, which the phone does not have.
// ADR-0110 item 1 (TASK-0334): each card holds the proposed value of each command too, from the parameters that the queue
// keeps.
internal sealed class HostedSession(EngineHost host) : ISessionHost, IProposalDesk
{
    public EngineHost Host { get; } = host;

    public DocumentSession Session => Host.Session;

    public string? FileName => Host.FileName;

    public IDisposable Subscribe(Action<EventRecord> action) => Host.Subscribe(action);

    public int Listeners => Host.Listeners;

    public IReadOnlyList<ProposalCard> Waiting()
        => [.. Host.Proposals.Waiting().Select(proposal => new ProposalCard(proposal.Id, proposal.Summary,
            [.. proposal.Commands.Select(command => ProposalCard.Line(command.Name, command.Parameters))], proposal.Stale, proposal.State == ProposalStates.Applying,
            [.. proposal.Commands.Select(command => ProposedValues.Of(command.Name, command.Parameters))], [.. proposal.Commands.Select(command => command.CommandId)]))];

    public async Task<ProposalOutcome> Accept(Guid id) => Outcome(await Host.Proposals.Accept(id).ConfigureAwait(false));

    public ProposalOutcome Reject(Guid id) => Outcome(Host.Proposals.Reject(id));

    private static ProposalOutcome Outcome(Proposal? proposal)
        => proposal is null ? ProposalOutcome.Gone
            : new(proposal.State, proposal.Results.Count(result => result.Status == CommandStatus.Applied), proposal.Commands.Count, proposal.FailedIndex, proposal.FailedCode);
}
