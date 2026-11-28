using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0194 item 2 (phase P11b, TASK-0489, the answer Q5 A): sets the state of a joint, "tight" or "loose". A loose joint keeps its
// record and gives no placement, so a person can move its child away; the state "tight" places the child and each body under
// it at the value of the joint again.
public sealed record SetJointStateCommand : Command
{
    public override string Name => "SetJointState";
    public override int SchemaVersion => 1;

    public required Guid JointId { get; init; }
    public required string State { get; init; }
}
