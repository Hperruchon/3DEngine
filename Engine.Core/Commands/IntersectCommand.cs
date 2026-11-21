using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0181 items 1 and 2 (phase K16, TASK-0433): the intersection of two bodies, in the form of Subtract. It makes a NEW
// body, the volume that the two bodies share, and it consumes both operands (ADR-0021 item 3). The handle of the new body
// comes from the CommandId (ADR-0012 §4), so a replay gives the same handle. The new body takes the name and the
// visibility of the first body. Two bodies that share no volume give no body: the command is refused (ADR-0181 item 3).
public sealed record IntersectCommand : Command
{
    public override string Name => "Intersect";
    public override int SchemaVersion => 1;

    public required Guid FirstBodyId { get; init; }
    public required Guid SecondBodyId { get; init; }
}
