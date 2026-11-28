using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Joints;
using Engine.Contracts.Schema;
using Engine.Core.Commands;
using Engine.Core.Features;
using Engine.Kernel;

namespace Engine.Core.Queries;

// ADR-0195 (phase K12a, TASK-0490): the travel of a pivot. The query turns the child of the joint and each body under it through
// tight joints in memory, from the lower limit to the upper limit, and gives the first contact with a body that does not turn
//. Each step uses + - * / and sqrt only, so each platform gives the same answer, and the query
// writes nothing to the log.
public sealed class CheckTravelQueryHandler : IQueryHandler
{
    public string QueryName => "CheckTravel";
    public int SchemaVersion => 1;

    // Item 3: the gap of a contact by default, in millimetres.
    public const double DefaultTolerance = 0.01;

    // Item 5: the most samples of one travel. A travel that needs more is refused, so it cannot hold the session.
    internal const int SampleLimit = 4096;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["jointId"] = new("guid", Required: true),
            ["tolerance"] = new("number", Unit: UnitKinds.Length),
        };

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["jointId"] = new("guid"),
            ["free"] = new("boolean"),
            ["contact"] = new("object"),
            ["movingBodyId"] = new("guid"),
            ["fixedBodyId"] = new("guid"),
            ["gap"] = new("number", Unit: UnitKinds.Length),
            ["samples"] = new("integer", Unit: UnitKinds.Count),
            ["radius"] = new("number", Unit: UnitKinds.Length),
            ["tolerance"] = new("number", Unit: UnitKinds.Length),
            ["lower"] = new("object"),
            ["upper"] = new("object"),
        };

    // Per ADR-0016.
    public Query Create(QueryInput input) => new CheckTravelQuery
    {
        QueryId = input.QueryId,
        JointId = (Guid)input.Parameters["jointId"]!,
        Tolerance = input.Parameters.GetValueOrDefault("tolerance") as double?,
    };

    // A body of the travel: its solid and its pieces in the Document now, or the solid of its shape for a body that turns.
    private sealed record Part(Guid Id, KernelSolid Solid, IReadOnlyList<NearestPoints.Piece> Pieces, Vector3R Low, Vector3R High);

    public Task<QueryHandlerResult> Handle(Query query, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);
        var travel = (CheckTravelQuery)query;
        var tolerance = travel.Tolerance ?? DefaultTolerance;
        if (!(tolerance > 0 && double.IsFinite(tolerance)))
            return Failure(DiagnosticCodes.CheckTravelInvalid, $"The tolerance of a travel is a finite length above zero, and the query gives {tolerance} (ADR-0195 item 3).");
        if (document.Joints.FirstOrDefault(joint => joint.Id == travel.JointId) is not { } joint)
            return Failure(DiagnosticCodes.JointNotFound, $"No joint with id '{travel.JointId}' exists in the document.");
        if (joint.Kind != JointRecord.Pivot || joint.Limits is not { } limits)
            return Failure(DiagnosticCodes.CheckTravelInvalid, $"The joint '{joint.Id}' is not a pivot with limits, and a travel turns a pivot from its lower to its upper limit (ADR-0195 item 1).");
        var parent = document.Bodies.FirstOrDefault(body => body.Handle.Id == joint.ParentBodyId);
        var child = document.Bodies.FirstOrDefault(body => body.Handle.Id == joint.ChildBodyId);
        if (parent is null || child is null)
            return Failure(DiagnosticCodes.GeomBodyNotFound, $"The parent or the child of the joint '{joint.Id}' is not a live body, so the joint places nothing.");

        // Item 2: the bodies that turn are the child and each body under it through tight joints; each other live body does not
        // turn. A pair of the two bodies of a joint is an intended contact, so it is exempt.
        var parentPlacement = Placements.Of(parent);
        var moving = new List<(BodyRecord Body, KernelSolid Shape)>();
        foreach (var body in new[] { child }.Concat(JointTree.Subtree(document, document.Joints, child.Handle.Id, BodyPlacement.Identity).Select(item => item.Body)))
        {
            var (solid, error) = MeasureTarget.SolidOf(document, body.Handle.Id);
            if (solid is null)
                return Failure(error!);
            if (KernelProjection.OfRecord(document, body, local: true).Solid is not { } shape)
                return Failure(DiagnosticCodes.GeomFacesFailed, $"The body '{body.Handle.Id}' has no solid of its shape.");
            moving.Add((body, shape));
        }

        var turning = moving.Select(item => item.Body.Handle.Id).ToHashSet();
        var still = new List<Part>();
        foreach (var body in LogOrder.Bodies(document).Where(body => !turning.Contains(body.Handle.Id)))
        {
            var (solid, error) = MeasureTarget.SolidOf(document, body.Handle.Id);
            if (solid is null)
                return Failure(error!);
            still.Add(Of(body.Handle.Id, solid));
        }

        var exempt = document.Joints.Select(other => Key(other.ParentBodyId, other.ChildBodyId)).ToHashSet();
        var pairs = moving.SelectMany(m => still.Where(s => !exempt.Contains(Key(m.Body.Handle.Id, s.Id))).Select(s => (Moving: m, Still: s))).ToList();
        var work = pairs.Sum(pair => (long)pair.Moving.Shape.Polygons.Count * pair.Still.Pieces.Count);
        if (work > CheckInterferenceQueryHandler.WorkLimit)
            return Failure(DiagnosticCodes.CheckWorkLimit, $"One sample of the travel needs {work} products of the pieces of its bodies, above the limit of {CheckInterferenceQueryHandler.WorkLimit}.");

        var (lower, upper) = (Unit(limits.Lower.Cos, limits.Lower.Sin), Unit(limits.Upper.Cos, limits.Upper.Sin));

        // Item 2: the placement of each body that turns at a value of the joint.
        Dictionary<Guid, BodyPlacement> At((double C, double S) value)
        {
            var at = JointTree.Child(parentPlacement, joint with { Value = new JointValue(value.C, value.S, 0) });
            var placed = new Dictionary<Guid, BodyPlacement> { [child.Handle.Id] = at };
            foreach (var (body, placement) in JointTree.Subtree(document, document.Joints, child.Handle.Id, at))
                placed[body.Handle.Id] = placement;
            return placed;
        }

        // Item 3: the radius is the largest distance of a vertex that turns from the axis of the pivot, with a margin for the
        // rounding of the doubles.
        var frame = joint.Parent.Frame;
        var (ox, oy, oz) = Placements.Apply(parentPlacement, frame.Origin.X, frame.Origin.Y, frame.Origin.Z);
        var (nx, ny, nz) = Placements.Direction(parentPlacement, frame.Normal.X, frame.Normal.Y, frame.Normal.Z);
        var length = Math.Sqrt((nx * nx) + (ny * ny) + (nz * nz));
        (nx, ny, nz) = (nx / length, ny / length, nz / length);
        var start = At(lower);
        var squared = 0.0;
        foreach (var (body, shape) in moving)
        {
            foreach (var vertex in shape.Polygons.SelectMany(polygon => polygon.Vertices))
            {
                var (vx, vy, vz) = vertex.ToDoubles();
                var (px, py, pz) = Placements.Apply(start[body.Handle.Id], vx, vy, vz);
                var (dx, dy, dz) = (px - ox, py - oy, pz - oz);
                var along = (dx * nx) + (dy * ny) + (dz * nz);
                squared = Math.Max(squared, (dx * dx) + (dy * dy) + (dz * dz) - (along * along));
            }
        }

        var radius = Math.Sqrt(squared);
        var bound = (radius * (1 + 1e-9)) + 1e-9;

        // Item 3: the smallest gap of a pair at a value of the joint. The first sample also finds an overlap, which has the gap 0;
        // after it a contact comes before an overlap, because no step passes a gap.
        (double Gap, Guid Moving, Guid Still) Sample((double C, double S) value, bool first)
        {
            var placed = At(value);
            var best = (Gap: double.PositiveInfinity, Moving: Guid.Empty, Still: Guid.Empty);
            foreach (var group in pairs.GroupBy(pair => pair.Moving.Body.Handle.Id))
            {
                var shape = group.First().Moving.Shape;
                var part = Of(group.Key, Placements.Apply(placed[group.Key], shape));
                foreach (var (_, other) in group)
                {
                    ct.ThrowIfCancellationRequested();
                    double gap;
                    if (first && ShareVolume(part, other) && PlanarBoolean.Intersect(part.Solid, other.Solid, ct) is not null)
                        gap = 0;
                    else
                        gap = Math.Sqrt(NearestPoints.Nearest(part.Pieces, other.Pieces, ct)!.Value.Squared.ToDouble());
                    if (gap < best.Gap)
                        best = (gap, group.Key, other.Id);
                }
            }

            return best;
        }

        // Items 3 and 4: the conservative advance. At each sample the gap g bounds the turn to the next sample: each point that
        // turns moves at most the radius times the chord of the step, so a step whose chord times the bound is below g passes no
        // contact. The step is the rest of the travel, halved until it is not above a half turn and its chord is small enough.
        var value = lower;
        for (var samples = 1; ; samples++)
        {
            if (samples > SampleLimit)
                return Failure(DiagnosticCodes.CheckWorkLimit, $"The travel needs more than {SampleLimit} samples at the tolerance {tolerance} mm (ADR-0195 item 5).");
            var (gap, movingId, stillId) = pairs.Count == 0 ? (double.PositiveInfinity, Guid.Empty, Guid.Empty) : Sample(value, samples == 1);
            if (gap <= tolerance)
                return Success(new TravelInfo(joint.Id, false, new JointValue(value.C, value.S, 0), movingId, stillId, gap, samples, radius, tolerance, Value(lower), Value(upper)));

            var rest = Divide(upper, value);
            if ((rest.S == 0 && rest.C > 0) || value == upper)
                return Success(new TravelInfo(joint.Id, true, null, null, null, null, samples, radius, tolerance, Value(lower), Value(upper)));

            var step = rest;
            var whole = true;
            var allowed = gap * (1 - 1e-9) / bound;
            while (Beyond(step) || Chord(step) > allowed)
            {
                step = Half(step);
                whole = false;
            }

            value = whole ? upper : Unit(Multiply(value, step));
        }
    }

    private static Part Of(Guid id, KernelSolid solid)
    {
        var vertices = solid.Polygons.SelectMany(polygon => polygon.Vertices).ToList();
        var low = new Vector3R(vertices.Min(v => v.X), vertices.Min(v => v.Y), vertices.Min(v => v.Z));
        var high = new Vector3R(vertices.Max(v => v.X), vertices.Max(v => v.Y), vertices.Max(v => v.Z));
        return new Part(id, solid, [.. solid.Polygons.Select(polygon => new NearestPoints.Piece(polygon.Vertices))], low, high);
    }

    // True when two boxes share a length above zero on each axis, as in CheckInterference.
    private static bool ShareVolume(Part a, Part b)
    {
        static bool Axis(Rational lowA, Rational highA, Rational lowB, Rational highB)
            => (highA < highB ? highA : highB) > (lowA > lowB ? lowA : lowB);

        return Axis(a.Low.X, a.High.X, b.Low.X, b.High.X) && Axis(a.Low.Y, a.High.Y, b.Low.Y, b.High.Y) && Axis(a.Low.Z, a.High.Z, b.Low.Z, b.High.Z);
    }

    private static (Guid, Guid) Key(Guid a, Guid b) => a.CompareTo(b) < 0 ? (a, b) : (b, a);

    private static (double C, double S) Unit(double c, double s)
    {
        var r = Math.Sqrt((c * c) + (s * s));
        return (c / r, s / r);
    }

    private static (double C, double S) Unit((double C, double S) turn) => Unit(turn.C, turn.S);

    private static JointValue Value((double C, double S) turn) => new(turn.C, turn.S, 0);

    private static (double C, double S) Multiply((double C, double S) a, (double C, double S) b)
        => ((a.C * b.C) - (a.S * b.S), (a.C * b.S) + (a.S * b.C));

    // The turn a after the turn b is taken back: a times the conjugate of b.
    private static (double C, double S) Divide((double C, double S) a, (double C, double S) b)
        => ((a.C * b.C) + (a.S * b.S), (a.S * b.C) - (a.C * b.S));

    // True for a turn of more than a half turn, or of a half turn.
    private static bool Beyond((double C, double S) turn) => turn.S < 0 || (turn.S == 0 && turn.C < 0);

    // The chord of a turn of a length of one, with no cancellation for a small turn.
    private static double Chord((double C, double S) turn) => Math.Sqrt(((1 - turn.C) * (1 - turn.C)) + (turn.S * turn.S));

    // The half of a turn from 0 to 360 degrees: a turn from 0 to 180 degrees. A turn below 90 degrees uses s / (2 c') for the
    // sine, so a small turn keeps its bits.
    private static (double C, double S) Half((double C, double S) turn)
    {
        if (turn.C >= 0 && turn.S >= 0)
        {
            var c = Math.Sqrt((1 + turn.C) / 2);
            return (c, turn.S / (2 * c));
        }

        var cosine = Math.Sqrt(Math.Max(0, (1 + turn.C) / 2));
        return (Beyond(turn) ? -cosine : cosine, Math.Sqrt(Math.Max(0, (1 - turn.C) / 2)));
    }

    private static Task<QueryHandlerResult> Success(TravelInfo info) => Task.FromResult(QueryHandlerResult.Success(info));

    private static Task<QueryHandlerResult> Failure(string code, string message) => Failure(new ErrorDetail(code, message));

    private static Task<QueryHandlerResult> Failure(ErrorDetail error) => Task.FromResult(QueryHandlerResult.Failure(error));
}
