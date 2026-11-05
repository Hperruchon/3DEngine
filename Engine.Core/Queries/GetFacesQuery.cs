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
// normal, or null when the name is the facets of a curve, which have no one plane.
public sealed record FaceEntry(DatumReference Reference, int Parts, int Holes, double Area, DatumPoint Point, DatumAxis? Normal);

// The faces of the body, in the order of the first face of each name in the B-Rep.
public sealed record FacesInfo(IReadOnlyList<FaceEntry> Faces);
