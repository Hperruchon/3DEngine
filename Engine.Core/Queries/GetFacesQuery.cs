using Engine.Contracts;
using Engine.Contracts.Sketch;

namespace Engine.Core.Queries;

// ADR-0058 item 4 (phase K4b, TASK-0184): the faces of a live body, from the B-Rep of the owned kernel.
public sealed record GetFacesQuery : Query
{
    public override string Name => "GetFaces";
    public override int SchemaVersion => 1;

    public required Guid BodyId { get; init; }
}

// One face name of the body: the reference as CreateDatum takes it (the feature, the element and the item that made
// the face), the count of its parts that do not touch, the count of its holes, its area, a point inside it, and its unit
// normal, or null when the name is the facets of a curve, which have no one plane. ADR-0075 item 2 (phase R12a,
// TASK-0236): the outline, each edge between a face of the name and a face of another name, which a client draws as the
// mark of a selected face.
public sealed record FaceEntry(DatumReference Reference, int Parts, int Holes, double Area, DatumPoint Point, DatumAxis? Normal, IReadOnlyList<FaceSegment> Outline);

// One edge of an outline, from one vertex of the B-Rep to the other.
public sealed record FaceSegment(DatumPoint Start, DatumPoint End);

// ADR-0077 items 1 and 2 (phase R12b, TASK-0245): one edge of the body, the pair of the names of its two faces, the lower
// one first; the count of its parts, the chains of its segments that share a vertex; its length; and its segments.
public sealed record EdgeEntry(DatumReference First, DatumReference Second, int Parts, double Length, IReadOnlyList<FaceSegment> Segments);

// The faces of the body, in the order of the first face of each name in the B-Rep, and its edges, in the order of the
// first B-Rep edge of each pair (ADR-0077).
public sealed record FacesInfo(IReadOnlyList<FaceEntry> Faces, IReadOnlyList<EdgeEntry> Edges);
