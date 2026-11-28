using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0193 item 6 (phase P11a, TASK-0479): removes a joint. The child keeps its placement and becomes the root of its own tree.
public sealed record RemoveJointCommand : Command
{
    public override string Name => "RemoveJoint";
    public override int SchemaVersion => 1;

    public required Guid JointId { get; init; }
}
