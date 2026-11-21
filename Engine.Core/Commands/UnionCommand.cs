using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0181 items 1 and 2 (phase K16, TASK-0433): the union of two bodies, in the form of Subtract. It makes a NEW body, the
// volume of the two bodies together, and it consumes both operands (ADR-0021 item 3). The handle of the new body comes
// from the CommandId (ADR-0012 §4), so a replay gives the same handle. The new body takes the name and the visibility of
// the first body.
public sealed record UnionCommand : Command
{
    public override string Name => "Union";
    public override int SchemaVersion => 1;

    public required Guid FirstBodyId { get; init; }
    public required Guid SecondBodyId { get; init; }
}
