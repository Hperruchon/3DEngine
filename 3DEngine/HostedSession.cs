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
// keeps. ADR-0177 item 1 (phase R30f, TASK-0422): and the sheet item of each command, for the sheet page.
// ADR-0187 items 1 and 3 (phase R25, TASK-0462): a host that draws asks the queue for the shapes of each preview, and the
// window reads the last preview of the shown proposal through Preview.
internal sealed class HostedSession : ISessionHost, IProposalDesk
{
    public HostedSession(EngineHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        Host = host;
        host.Proposals.KeepShapes = true;
    }

    public EngineHost Host { get; }

    public DocumentSession Session => Host.Session;

    public string? FileName => Host.FileName;

    public IDisposable Subscribe(Action<EventRecord> action) => Host.Subscribe(action);

    public int Listeners => Host.Listeners;

    public IReadOnlyList<ProposalCard> Waiting()
        => [.. Host.Proposals.Waiting().Select(proposal => new ProposalCard(proposal.Id, proposal.Summary,
            [.. proposal.Commands.Select(command => ProposalCard.Line(command.Name, command.Parameters))], proposal.Stale, proposal.State == ProposalStates.Applying,
            [.. proposal.Commands.Select(command => ProposedValues.Of(command.Name, command.Parameters))], [.. proposal.Commands.Select(command => command.CommandId)],
            [.. proposal.Commands.Select(command => ProposedSheet.ItemOf(command.Name, command.Parameters, command.CommandId))]))];

    // ADR-0188 item 6 (phase P16, TASK-0463): seen is the version of the preview that the panel showed, or null.
    public async Task<ProposalOutcome> Accept(Guid id, long? seen) => Outcome(await Host.Proposals.Accept(id, seen).ConfigureAwait(false));

    public ProposalOutcome Reject(Guid id) => Outcome(Host.Proposals.Reject(id));

    // ADR-0187 item 3: the last preview of the proposal with its shapes, from the lock of the queue only, or null. ADR-0188 item
    // 6: with the code of the note of an accept that applied nothing because the result changed.
    public PreviewCard? Preview(Guid id)
        => Host.Proposals.Kept(id) is not { Preview: var preview } kept
            ? null
            : new PreviewCard(preview.ProposalId, preview.DocumentVersion, kept.Current, preview.Error?.Code, preview.FailedIndex, preview.FailedCode,
                preview.Bodies, preview.Sketches ?? [], preview.Measures ?? [], preview.Checks ?? [], kept.Shapes, kept.ShapesLeftOut, kept.Held);

    private static ProposalOutcome Outcome(Proposal? proposal)
        => proposal is null ? ProposalOutcome.Gone
            : new(proposal.State, proposal.Results.Count(result => result.Status == CommandStatus.Applied), proposal.Commands.Count, proposal.FailedIndex, proposal.FailedCode, proposal.Held?.Code);
}
