using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Contracts.Sketch;
using Engine.Core.Drawing;
using Engine.Core.Features;
using Engine.Core.Sketch;
using Engine.Kernel;

namespace Engine.Core.Queries;

// ADR-0183 (phase K17, TASK-0443): the connectors of a live body. The query reads the solid of the owned
// kernel through MeasureTarget.SolidOf, as the measure queries do, so it needs no capability of the backend, it has their
// refusals (item 7), and it writes nothing to the log.
public sealed class GetConnectorsQueryHandler : IQueryHandler
{
    public string QueryName => "GetConnectors";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["datums"] = new("array"),
            ["faces"] = new("array"),
        };

    // Per ADR-0016.
    public Query Create(QueryInput input) => new GetConnectorsQuery
    {
        QueryId = input.QueryId,
        BodyId = (Guid)input.Parameters["bodyId"]!,
    };

    public Task<QueryHandlerResult> Handle(
        Query query,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var connectors = (GetConnectorsQuery)query;
        var (solid, error) = MeasureTarget.SolidOf(document, connectors.BodyId);
        if (solid is null)
            return Task.FromResult(QueryHandlerResult.Failure(error!));
        return Task.FromResult(QueryHandlerResult.Success(new ConnectorsInfo(Datums(document, connectors.BodyId), Faces(document, connectors.BodyId, solid))));
    }

    // Item 3: each datum with a name whose reference is on a feature with the body at the end of its chain of consumers
    // (KernelProjection.End, ADR-0059 item 3), in the order of the log, with the frame and the fault of its record.
    private static List<DatumConnector> Datums(Document document, Guid body)
    {
        var graph = FeatureGraph.Of(document);
        return [.. document.Datums
            .Where(datum => datum.Name is not null && KernelProjection.End(graph, datum.Reference.FeatureId).End == body)
            .OrderBy(datum => graph.Order(datum.Id))
            .Select(datum => new DatumConnector(datum.Id, datum.Name!, datum.Reference, datum.Frame, datum.Fault))];
    }

    // Items 4 and 5: each face name of the solid in the order of GetFaces, the order of the first face of each name in the
    // B-Rep. A name of one plane gives its centre and its outward normal; a whole side of a cylinder of the design gives its
    // axis and its radius; each other curved name gives no frame.
    private static List<FaceConnector> Faces(Document document, Guid body, KernelSolid solid)
    {
        var faces = new List<FaceConnector>();
        foreach (var tag in Brep.Of(solid).Faces.Select(face => face.Tag).Distinct())
        {
            var reference = new DatumReference(tag.Feature, tag.Element, tag.Item);
            var pieces = solid.Polygons.Where(polygon => polygon.Tag == tag).ToList();
            if (!MeasureTarget.IsCurved(solid, tag))
            {
                var (x, y, z) = Centre(pieces).ToDoubles();
                faces.Add(new FaceConnector(reference, ConnectorKind.Plane, new DatumPoint(x, y, z), Unit(pieces[0].Plane.Normal), null));
            }
            else if (MeasureCurve.Whole(document, body, solid, tag).Surface is { Kind: SideCurveKind.Cylinder } side)
            {
                var (x, y, z) = side.Base.ToDoubles();
                faces.Add(new FaceConnector(reference, ConnectorKind.Cylinder, new DatumPoint(x, y, z), Unit(side.Axis), side.Radius.ToDouble()));
            }
            else
            {
                faces.Add(new FaceConnector(reference, ConnectorKind.Curved, null, null, null));
            }
        }

        return faces;
    }

    // Item 4: the exact centre of the area of the convex pieces of one plane with the normal n. Each piece is a fan of
    // triangles (a, b, c) from its first vertex. The weight w = ((b - a) × (c - a)) · n of a triangle is its area times
    // 2 |n|, with the sign of its turn about n, so the factor 2 |n| is the same for each triangle and needs no root. The
    // centre is the sum of w (a + b + c) divided by 3 times the sum of w. Each step is + - * / on exact rationals, so a hole,
    // which the pieces leave out, has no area in the sum.
    internal static Vector3R Centre(IReadOnlyList<KernelPolygon> pieces)
    {
        var n = pieces[0].Plane.Normal;
        var (moment, weight) = (new Vector3R(Rational.Zero, Rational.Zero, Rational.Zero), Rational.Zero);
        foreach (var piece in pieces)
        {
            var a = piece.Vertices[0];
            for (var i = 1; i + 1 < piece.Vertices.Count; i++)
            {
                var (b, c) = (piece.Vertices[i], piece.Vertices[i + 1]);
                var w = (b - a).Cross(c - a).Dot(n);
                moment += (a + b + c) * w;
                weight += w;
            }
        }

        var scale = weight * 3;
        return new Vector3R(moment.X / scale, moment.Y / scale, moment.Z / scale);
    }

    // Item 6: the unit vector of an exact vector v that is not zero. Each component is the square root of the nearest double
    // to the exact square v_i² / (v · v), with the sign of v_i: one Math.Sqrt for each component, and nothing else that
    // rounds. The value does not depend on the scale of v, so the normal of a plane and of its canonical form give the
    // same bits, and a component of an exact unit, as 1 or 0.5, is exact.
    internal static DatumAxis Unit(Vector3R v)
    {
        var length = v.Dot(v);
        double Component(Rational c) => c.Sign * Math.Sqrt((c * c / length).ToDouble());
        return new DatumAxis(Component(v.X), Component(v.Y), Component(v.Z));
    }
}
