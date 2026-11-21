using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Kernel;

namespace Engine.Core.Queries;

// ADR-0180 items 5 to 8 (phase K15a, TASK-0432). The query reads the planes of the two faces in the owned kernel. A name
// that the body does not have gives E-MEASURE-NAME-NOT-FOUND, and a curved face that item 12 (phase K15b, TASK-0434) gives
// no closed form for gives E-MEASURE-CURVED.
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
        var (planeA, curveA, errorA) = Face(document, angle.BodyIdA, new FaceTag(angle.FeatureIdA, angle.ElementA, angle.ItemIdA));
        if (errorA is not null)
            return Task.FromResult(QueryHandlerResult.Failure(errorA));
        var (planeB, curveB, errorB) = Face(document, angle.BodyIdB, new FaceTag(angle.FeatureIdB, angle.ElementB, angle.ItemIdB));
        if (errorB is not null)
            return Task.FromResult(QueryHandlerResult.Failure(errorB));
        if (planeA is { } a && planeB is { } b)
            return Task.FromResult(QueryHandlerResult.Success(Between(a.Normal, b.Normal)));

        // Item 12 (phase K15b, TASK-0434): the side of a cylinder or a cone against a plane face.
        var (curve, plane) = curveA is not null ? (curveA, planeB) : (curveB!, planeA);
        if (plane is not { } other)
            return Task.FromResult(QueryHandlerResult.Failure(MeasureTarget.Curved("The two faces are curved, and the angle of two curved faces has no closed form in K15b.")));
        if (curve.Kind == Drawing.SideCurveKind.Sphere)
            return Task.FromResult(QueryHandlerResult.Failure(MeasureTarget.Curved("A sphere has an outward normal in each direction, so its angle to a face is not one value.")));
        return Task.FromResult(QueryHandlerResult.Success(AboutAxis(curve, other.Normal)));
    }

    private static (PlaneR? Plane, MeasureCurve.Surface? Curve, ErrorDetail? Error) Face(Document document, Guid body, FaceTag tag)
    {
        var (solid, error) = MeasureTarget.SolidOf(document, body);
        return solid is null ? (null, null, error) : MeasureTarget.FaceOf(document, solid, body, tag);
    }

    // Item 12: the outward normals of the side of a cylinder or a cone make one angle β with its axis a, from the base to
    // the other end or to the apex: 90 degrees for a cylinder, and cos β = r / L, sin β = h / L for a cone of radius r and
    // height h = |a|, with L² = r² + h². For a plane face with the outward normal m at the angle φ to a, the nearest of these
    // normals is in the plane of a and m, at the angle |φ - β| to m: the angle between the plane face and the side where
    // the side looks most nearly the same way. So cos = cos φ cos β + sin φ sin β and sin = |sin φ cos β - cos φ sin β|.
    // Each term is the root of an exact square, as in item 5; a sum of two roots is the only other step.
    internal static AngleInfo AboutAxis(MeasureCurve.Surface curve, Vector3R m)
    {
        ArgumentNullException.ThrowIfNull(curve);
        var a = curve.Axis;
        var (h2, dot, cross) = (a.Dot(a), a.Dot(m), a.Cross(m));
        var (x2, lengths) = (cross.Dot(cross), a.Dot(a) * m.Dot(m));
        double Root(Rational square) => Math.Sqrt(square.ToDouble());
        if (curve.Kind == Drawing.SideCurveKind.Cylinder)
            return new AngleInfo(Root(x2 / lengths), Root(dot * dot / lengths));

        var (r2, scale) = (curve.Radius * curve.Radius, lengths * ((curve.Radius * curve.Radius) + h2));
        var signed = dot.Sign < 0 ? -1 : 1;
        var cosine = (signed * Root(dot * dot * r2 / scale)) + Root(x2 * h2 / scale);
        var sine = Math.Abs(Root(x2 * r2 / scale) - (signed * Root(dot * dot * h2 / scale)));
        return new AngleInfo(cosine, sine);
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
