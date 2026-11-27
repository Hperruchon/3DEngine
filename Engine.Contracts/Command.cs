namespace Engine.Contracts;

public abstract record Command
{
    public Guid CommandId { get; init; } = Guid.NewGuid();
    public abstract string Name { get; }
    public abstract int SchemaVersion { get; }
    public long? ExpectedDocumentVersion { get; init; }

    // ADR-0188 item 1 (phase P16, TASK-0463): the identifier of the group of the command, or null. The accept of a proposal sets
    // the identifier of the proposal on each of its commands. The history joins the consecutive steps of one group into one
    // step of undo. A document file holds the member only when it is not null, so an entry with no group has its bytes of
    // before P16. ADR-0190 (TASK-0473): an Undo with a group is the reversal of a failed group of that proposal, which only the
    // session applies, and it puts nothing on the redo list.
    public Guid? GroupId { get; init; }
}
