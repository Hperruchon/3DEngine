using Engine.Contracts;

namespace Engine.Core.Queries;

// First geometry query per TASK-0011. Returns the axis-aligned bounding box
// for a body known to the active backend.
public sealed record GetBoundingBoxQuery : Query
{
    public override string Name => "GetBoundingBox";
    public override int SchemaVersion => 1;

    public required Guid BodyId { get; init; }
}
