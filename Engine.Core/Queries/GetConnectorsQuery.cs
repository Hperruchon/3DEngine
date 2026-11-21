using Engine.Contracts;
using Engine.Contracts.Sketch;

namespace Engine.Core.Queries;

// ADR-0183 (phase K17, TASK-0443): the connectors of a live body. A connector is a named datum of the
// body, or an implicit frame that the query gives from a face of the body.
public sealed record GetConnectorsQuery : Query
{
    public override string Name => "GetConnectors";
    public override int SchemaVersion => 1;

    public required Guid BodyId { get; init; }
}

// ADR-0183 item 4: the kind of a face. Plane: a face of one plane, with its centre and its outward unit normal. Cylinder: the
// whole side of a cylinder of the design, with a point of its axis, the unit direction of the axis and the radius. Curved:
// each other curved face, with no frame.
public enum ConnectorKind
{
    Plane,
    Cylinder,
    Curved,
}

// ADR-0183 item 3: a named datum of the body, with its reference, its frame as the datum gives it (ADR-0051 item 4), and its
// fault, or null when the last resolution succeeded.
public sealed record DatumConnector(Guid DatumId, string Name, DatumReference Reference, DatumFrame Frame, DatumFault? Fault);

// ADR-0183 item 4: one face name of the body, as GetFaces names it. For a plane face, Point is the centre of its area and
// Direction its outward unit normal. For a cylinder, Point is the centre of the base of the side, Direction the unit
// direction of the axis from the base to the other end, and Radius the radius. For a curved face, Point, Direction and
// Radius are null.
public sealed record FaceConnector(DatumReference Reference, ConnectorKind Kind, DatumPoint? Point, DatumAxis? Direction, double? Radius);

// The connectors of the body: its named datums in the order of the log, and its faces in the order of GetFaces.
public sealed record ConnectorsInfo(IReadOnlyList<DatumConnector> Datums, IReadOnlyList<FaceConnector> Faces);
