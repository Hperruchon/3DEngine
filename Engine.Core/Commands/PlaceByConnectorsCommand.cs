using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0194 item 4 (phase P11b, TASK-0489, the answer Q6 A): places a child body by two connectors one time, as a fixed joint
// places it, and records no joint. Each side names a face (FeatureId, Element and an optional ItemId) or a datum (DatumId), as for
// AddJoint. The log holds the command; the placement is its result, as for a joint.
public sealed record PlaceByConnectorsCommand : Command
{
    public override string Name => "PlaceByConnectors";
    public override int SchemaVersion => 1;

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
}
