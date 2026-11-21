using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Contracts.Sketch;
using Engine.Kernel;

namespace Engine.Core.Queries;

// ADR-0180 items 3 and 7 (phase K15a, TASK-0432). The query reads the solid of the owned kernel, so it needs no capability
// of the backend. The refusals are those of GetFaces: E-GEOM-BODY-NOT-FOUND, E-MESH-UNSUPPORTED, E-GEOM-BODY-FAULTED and
// E-GEOM-FACES-FAILED.
public sealed class GetMassPropertiesQueryHandler : IQueryHandler
{
    public string QueryName => "GetMassProperties";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["volume"] = new("number"),
            ["centre"] = new("object"),
            ["facets"] = new("boolean"),
        };

    // Per ADR-0016.
    public Query Create(QueryInput input) => new GetMassPropertiesQuery
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
        var mass = (GetMassPropertiesQuery)query;
        var (solid, error) = MeasureTarget.SolidOf(document, mass.BodyId);
        if (solid is null)
            return Task.FromResult(QueryHandlerResult.Failure(error!));

        // ADR-0180 item 10 (phase K15b, TASK-0434): the volume of the curve of the design where a closed form exists, else
        // the exact values of the solid, with the flag of facets when a face is curved.
        return Task.FromResult(QueryHandlerResult.Success(MeasureCurve.DesignMass(document, mass.BodyId) ?? Of(solid)));
    }

    // Item 3: the exact volume of the kernel, and the centre as the sum of the centres of the tetrahedra of a fan of each
    // polygon with the origin, each weighted by its signed volume. With six times the volume of a tetrahedron
    // a · (b × c), the centre is the sum of (a + b + c) a · (b × c), divided by 24 times the volume. Each step is exact;
    // each value leaves as a double with one rounding.
    internal static MassProperties Of(KernelSolid solid)
    {
        var volume = solid.Volume();
        var moment = new Vector3R(Rational.Zero, Rational.Zero, Rational.Zero);
        foreach (var polygon in solid.Polygons)
        {
            var a = polygon.Vertices[0];
            for (var i = 1; i + 1 < polygon.Vertices.Count; i++)
            {
                var (b, c) = (polygon.Vertices[i], polygon.Vertices[i + 1]);
                moment += (a + b + c) * a.Dot(b.Cross(c));
            }
        }

        var scale = volume * 24;
        var centre = new Vector3R(moment.X / scale, moment.Y / scale, moment.Z / scale).ToDoubles();
        return new MassProperties(volume.ToDouble(), new DatumPoint(centre.X, centre.Y, centre.Z), MeasureTarget.HasCurve(solid));
    }
}
