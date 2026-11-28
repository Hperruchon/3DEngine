using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0193 items 4 and 6 (phase P11a, TASK-0479): sets the value of a joint and places its child and each body under it. A
// pivot takes Cos and Sin, and a slider takes Distance. The value is absolute, as the placement of PlaceBody is.
public sealed record SetJointValueCommand : Command
{
    public override string Name => "SetJointValue";
    public override int SchemaVersion => 1;

    public required Guid JointId { get; init; }
    public double? Cos { get; init; }
    public double? Sin { get; init; }
    public double? Distance { get; init; }
}
