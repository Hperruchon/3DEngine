using Engine.Contracts;

namespace Engine.Core.Queries;

// ADR-0180 item 5 (phase K15a, TASK-0432): the angle between two plane faces. Each face is named by its body and the
// reference that GetFaces gives: the feature, the element, and the item when GetFaces gives one (answer Q3 A of the
// codebase review of 2026-11-21). The two faces can be on two bodies. Item 12 (phase K15b, TASK-0434): one face can be the
// whole side of a cylinder or a cone.
public sealed record GetAngleQuery : Query
{
    public override string Name => "GetAngle";
    public override int SchemaVersion => 1;

    public required Guid BodyIdA { get; init; }
    public required Guid FeatureIdA { get; init; }
    public required string ElementA { get; init; }
    public Guid? ItemIdA { get; init; }
    public required Guid BodyIdB { get; init; }
    public required Guid FeatureIdB { get; init; }
    public required string ElementB { get; init; }
    public Guid? ItemIdB { get; init; }
}

// The cosine and the sine of the angle between the outward normals of the two faces, from 0 to 180 degrees. ADR-0180 item 12
// (phase K15b, TASK-0434): for the side of a cylinder or a cone, the normal of the side is its outward normal nearest to the
// normal of the plane face. The cosine is 1 for two faces that look the same way and -1 for two faces that look at each
// other. The sine is never negative. The query gives no angle, so a client that shows degrees computes them (CLAUDE.md,
// determinism rule 2).
public sealed record AngleInfo(double Cosine, double Sine);
