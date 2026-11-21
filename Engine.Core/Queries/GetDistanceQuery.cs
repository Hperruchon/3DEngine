using Engine.Contracts;
using Engine.Contracts.Sketch;

namespace Engine.Core.Queries;

// ADR-0180 item 4 (phase K15a, TASK-0432): the minimum distance between two sides, A and B. Each side is one of these:
// - a body: its id only;
// - a face of the body: the feature and the element, and the item when GetFaces gives one;
// - the edge between two faces of the body: the names of the two faces, as an edge of GetFaces gives them, in either
//   order. The fields "Second" give the second face.
public sealed record GetDistanceQuery : Query
{
    public override string Name => "GetDistance";
    public override int SchemaVersion => 1;

    public required Guid BodyIdA { get; init; }
    public Guid? FeatureIdA { get; init; }
    public string? ElementA { get; init; }
    public Guid? ItemIdA { get; init; }
    public Guid? SecondFeatureIdA { get; init; }
    public string? SecondElementA { get; init; }
    public Guid? SecondItemIdA { get; init; }
    public required Guid BodyIdB { get; init; }
    public Guid? FeatureIdB { get; init; }
    public string? ElementB { get; init; }
    public Guid? ItemIdB { get; init; }
    public Guid? SecondFeatureIdB { get; init; }
    public string? SecondElementB { get; init; }
    public Guid? SecondItemIdB { get; init; }
}

// The minimum distance in millimetres, and a point of side A and a point of side B at that distance (answer Q4 A of the
// codebase review of 2026-11-21). Two sides that touch or overlap give 0 and one common point twice.
public sealed record DistanceInfo(double Distance, DatumPoint PointA, DatumPoint PointB);
