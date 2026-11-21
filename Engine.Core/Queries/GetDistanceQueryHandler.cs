using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Contracts.Sketch;
using Engine.Kernel;

namespace Engine.Core.Queries;

// ADR-0180 items 4 and 6 to 8 (phase K15a, TASK-0432). The query reads the pieces of the two sides in the owned kernel. A
// name that the body does not have, or a part of a name, gives E-MEASURE-NAME-NOT-FOUND; a side with a curved face that
// item 11 (phase K15b, TASK-0434) gives no closed form for gives E-MEASURE-CURVED.
public sealed class GetDistanceQueryHandler : IQueryHandler
{
    public string QueryName => "GetDistance";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyIdA"] = new("guid", Required: true),
            ["featureIdA"] = new("guid"),
            ["elementA"] = new("string"),
            ["itemIdA"] = new("guid"),
            ["secondFeatureIdA"] = new("guid"),
            ["secondElementA"] = new("string"),
            ["secondItemIdA"] = new("guid"),
            ["bodyIdB"] = new("guid", Required: true),
            ["featureIdB"] = new("guid"),
            ["elementB"] = new("string"),
            ["itemIdB"] = new("guid"),
            ["secondFeatureIdB"] = new("guid"),
            ["secondElementB"] = new("string"),
            ["secondItemIdB"] = new("guid"),
        };

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["distance"] = new("number"),
            ["pointA"] = new("object"),
            ["pointB"] = new("object"),
        };

    // Per ADR-0016.
    public Query Create(QueryInput input) => new GetDistanceQuery
    {
        QueryId = input.QueryId,
        BodyIdA = (Guid)input.Parameters["bodyIdA"]!,
        FeatureIdA = input.Parameters.GetValueOrDefault("featureIdA") as Guid?,
        ElementA = input.Parameters.GetValueOrDefault("elementA") as string,
        ItemIdA = input.Parameters.GetValueOrDefault("itemIdA") as Guid?,
        SecondFeatureIdA = input.Parameters.GetValueOrDefault("secondFeatureIdA") as Guid?,
        SecondElementA = input.Parameters.GetValueOrDefault("secondElementA") as string,
        SecondItemIdA = input.Parameters.GetValueOrDefault("secondItemIdA") as Guid?,
        BodyIdB = (Guid)input.Parameters["bodyIdB"]!,
        FeatureIdB = input.Parameters.GetValueOrDefault("featureIdB") as Guid?,
        ElementB = input.Parameters.GetValueOrDefault("elementB") as string,
        ItemIdB = input.Parameters.GetValueOrDefault("itemIdB") as Guid?,
        SecondFeatureIdB = input.Parameters.GetValueOrDefault("secondFeatureIdB") as Guid?,
        SecondElementB = input.Parameters.GetValueOrDefault("secondElementB") as string,
        SecondItemIdB = input.Parameters.GetValueOrDefault("secondItemIdB") as Guid?,
    };

    public Task<QueryHandlerResult> Handle(
        Query query,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var distance = (GetDistanceQuery)query;
        var (a, errorA) = MeasureTarget.Resolve(document, new MeasureTarget.Side(
            distance.BodyIdA, distance.FeatureIdA, distance.ElementA, distance.ItemIdA,
            distance.SecondFeatureIdA, distance.SecondElementA, distance.SecondItemIdA), "A");
        if (a is null)
            return Task.FromResult(QueryHandlerResult.Failure(errorA!));
        var (b, errorB) = MeasureTarget.Resolve(document, new MeasureTarget.Side(
            distance.BodyIdB, distance.FeatureIdB, distance.ElementB, distance.ItemIdB,
            distance.SecondFeatureIdB, distance.SecondElementB, distance.SecondItemIdB), "B");
        if (b is null)
            return Task.FromResult(QueryHandlerResult.Failure(errorB!));

        // ADR-0180 item 11 (phase K15b, TASK-0434): a whole curved face of the design is measured from its curve.
        var (info, error) = CurvedDistance.Between(a, b);
        return Task.FromResult(info is null ? QueryHandlerResult.Failure(error!) : QueryHandlerResult.Success(info));
    }

    // Item 4: the nearest pair of the pieces of the two sides. A body is a solid: when the pieces do not touch, a side
    // that lies inside the body of the other side is at the distance 0, and its first vertex is the common point.
    internal static DistanceInfo Between(MeasureTarget.Resolved a, MeasureTarget.Resolved b)
    {
        var pair = NearestPoints.Nearest(a.Pieces, b.Pieces)!.Value;
        if (pair.Squared.Sign > 0)
        {
            var insideA = b.Pieces[0].Vertices[0];
            var insideB = a.Pieces[0].Vertices[0];
            if (a.Solid is { } solidA && Inside(solidA, insideA, pair.Squared))
                pair = new NearestPoints.Pair(Rational.Zero, insideA, insideA);
            else if (b.Solid is { } solidB && Inside(solidB, insideB, pair.Squared))
                pair = new NearestPoints.Pair(Rational.Zero, insideB, insideB);
        }

        // Item 8: the root of the exact square is the only step after the exact ones.
        var (ax, ay, az) = pair.A.ToDoubles();
        var (bx, by, bz) = pair.B.ToDoubles();
        return new DistanceInfo(Math.Sqrt(pair.Squared.ToDouble()), new DatumPoint(ax, ay, az), new DatumPoint(bx, by, bz));
    }

    // True when a point at a squared distance of at least gap from the surface of the solid is inside the solid. A cube
    // about the point with a half side h, where 3 h² < gap, does not touch the surface, so it is all inside or all outside.
    // The exact subtract of the kernel gives no solid for the cube minus the solid exactly when the cube is inside.
    internal static bool Inside(KernelSolid solid, Vector3R point, Rational gap)
    {
        var half = Rational.One;
        while (half * half * 3 >= gap)
            half /= 2;
        var side = half * 2;
        var cube = KernelSolid.Box(Guid.Empty, side, side, side).Translate(point);
        return PlanarBoolean.Subtract(cube, solid) is null;
    }
}
