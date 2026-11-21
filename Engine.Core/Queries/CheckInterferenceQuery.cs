using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;

namespace Engine.Core.Queries;

// ADR-0182 items 1 and 2 (phase K11, TASK-0436): the pair check at one position. A parameter of a query is flat (ADR-0016),
// so each list is one text:
// - BodyIds: the identifiers of the bodies to check, separated by commas, two or more. With no value, each live body of the
//   Document is checked, in the order of the Document.
// - IntendedPairs: the pairs whose contact or overlap is intended, separated by commas; the two identifiers of a pair are
//   separated by a colon, as "a:b". With no value, no pair is intended.
public sealed record CheckInterferenceQuery : Query
{
    public override string Name => "CheckInterference";
    public override int SchemaVersion => 1;

    public string? BodyIds { get; init; }
    public string? IntendedPairs { get; init; }
}

// Item 3: the class of a pair. Apart: a gap above zero. Touching: no shared volume and a gap of zero, on a face, an edge or a
// point. Overlapping: a shared volume above zero.
public enum PairClass
{
    Apart,
    Touching,
    Overlapping,
}

// Item 6: the basis of the geometric answer, one of the classes of a check result. The pair check gives only "Guaranteed": the
// class, the volume, the box and the gap come from the geometry alone. A later check adds the other classes.
public enum CheckBasis
{
    Guaranteed,
}

// Items 3 to 6: the answer for one pair, A before B in the order of the bodies of the check. Volume is the volume of the
// overlap in mm³, and Box its box, or null when the class is not Overlapping. Gap is the minimum distance in mm, 0 when the
// bodies touch or overlap, with a point of A and a point of B at that distance; the points are null when the class is
// Overlapping. Intended is true for a pair of IntendedPairs. Fault is true for a pair that overlaps and is not intended.
// MeshOnly is true when a body is a mesh body of an STL file or has a curved face: each value is then exact for its
// triangles or its facets only.
public sealed record PairCheck(
    Guid BodyIdA,
    Guid BodyIdB,
    PairClass Class,
    double Volume,
    Aabb? Box,
    double Gap,
    DatumPoint? PointA,
    DatumPoint? PointB,
    bool Intended,
    bool Fault,
    bool MeshOnly,
    CheckBasis Basis);

// The answer of the check: each pair of the bodies, in the order of the bodies.
public sealed record InterferenceInfo(IReadOnlyList<PairCheck> Pairs);
