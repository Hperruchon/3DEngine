using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Kernel;

namespace Engine.Core.Queries;

// ADR-0180 items 5 to 8 (phase K15a, TASK-0432). The query reads the planes of the two faces in the owned kernel. A name
// that the body does not have gives E-MEASURE-NAME-NOT-FOUND, and a curved face gives E-MEASURE-CURVED.
public sealed class GetAngleQueryHandler : IQueryHandler
{
    public string QueryName => "GetAngle";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyIdA"] = new("guid", Required: true),
            ["featureIdA"] = new("guid", Required: true),
            ["elementA"] = new("string", Required: true),
            ["itemIdA"] = new("guid"),
            ["bodyIdB"] = new("guid", Required: true),
            ["featureIdB"] = new("guid", Required: true),
            ["elementB"] = new("string", Required: true),
            ["itemIdB"] = new("guid"),
        };

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["cosine"] = new("number"),
            ["sine"] = new("number"),
        };

    // Per ADR-0016.
    public Query Create(QueryInput input) => new GetAngleQuery
    {
        QueryId = input.QueryId,
        BodyIdA = (Guid)input.Parameters["bodyIdA"]!,
        FeatureIdA = (Guid)input.Parameters["featureIdA"]!,
        ElementA = (string)input.Parameters["elementA"]!,
        ItemIdA = input.Parameters.GetValueOrDefault("itemIdA") as Guid?,
        BodyIdB = (Guid)input.Parameters["bodyIdB"]!,
        FeatureIdB = (Guid)input.Parameters["featureIdB"]!,
        ElementB = (string)input.Parameters["elementB"]!,
        ItemIdB = input.Parameters.GetValueOrDefault("itemIdB") as Guid?,
    };

    public Task<QueryHandlerResult> Handle(
        Query query,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var angle = (GetAngleQuery)query;
        var (a, errorA) = Plane(document, angle.BodyIdA, new FaceTag(angle.FeatureIdA, angle.ElementA, angle.ItemIdA));
        if (a is null)
            return Task.FromResult(QueryHandlerResult.Failure(errorA!));
        var (b, errorB) = Plane(document, angle.BodyIdB, new FaceTag(angle.FeatureIdB, angle.ElementB, angle.ItemIdB));
        if (b is null)
            return Task.FromResult(QueryHandlerResult.Failure(errorB!));
        return Task.FromResult(QueryHandlerResult.Success(Between(a.Value.Normal, b.Value.Normal)));
    }

    private static (PlaneR? Plane, ErrorDetail? Error) Plane(Document document, Guid body, FaceTag tag)
    {
        var (solid, error) = MeasureTarget.SolidOf(document, body);
        return solid is null ? (null, error) : MeasureTarget.PlaneOf(solid, body, tag);
    }

    // Item 5: with the outward normals m and n, the square of the cosine is (m · n)² / (|m|² |n|²), and the square of the
    // sine is |m × n|² / (|m|² |n|²). Each square is exact, and the two add to 1. The root of each square is the only step
    // after the exact ones (item 8): no inverse cosine and no other transcendental function.
    internal static AngleInfo Between(Vector3R m, Vector3R n)
    {
        var lengths = m.Dot(m) * n.Dot(n);
        var dot = m.Dot(n);
        var cross = m.Cross(n);
        var cosine = Math.Sqrt((dot * dot / lengths).ToDouble());
        var sine = Math.Sqrt((cross.Dot(cross) / lengths).ToDouble());
        return new AngleInfo(dot.Sign < 0 ? -cosine : cosine, sine);
    }
}
