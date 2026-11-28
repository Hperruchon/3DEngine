using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0193 items 1 to 6 (phase P11a, TASK-0479): adds a joint of the kind between a parent body and a child body, and places
// the child and each body under it. Each side names a face (FeatureId, Element and an optional ItemId) or a datum (DatumId).
// A pivot takes Cos and Sin, a slider takes Distance, and a fixed joint takes no value. The limits are optional. The joint
// takes the identifier of the command. The log holds the numbers of the command and no angle (CLAUDE.md, determinism rule 2).
public sealed record AddJointCommand : Command
{
    public override string Name => "AddJoint";
    public override int SchemaVersion => 1;

    public required string Kind { get; init; }
    public required Guid ParentBodyId { get; init; }
    public Guid? ParentFeatureId { get; init; }
    public string? ParentElement { get; init; }
    public Guid? ParentItemId { get; init; }
    public Guid? ParentDatumId { get; init; }
    public required Guid ChildBodyId { get; init; }
    public Guid? ChildFeatureId { get; init; }
    public string? ChildElement { get; init; }
    public Guid? ChildItemId { get; init; }
    public Guid? ChildDatumId { get; init; }
    public double? Cos { get; init; }
    public double? Sin { get; init; }
    public double? Distance { get; init; }
    public double? LowerCos { get; init; }
    public double? LowerSin { get; init; }
    public double? UpperCos { get; init; }
    public double? UpperSin { get; init; }
    public double? LowerDistance { get; init; }
    public double? UpperDistance { get; init; }
}
