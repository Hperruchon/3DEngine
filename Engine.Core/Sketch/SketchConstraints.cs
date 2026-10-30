using Engine.Contracts.Sketch;
using static Engine.Core.Sketch.SketchModel;

namespace Engine.Core.Sketch;

// ADR-0033 item 3: the kinds of a constraint, the check of its items and values, and its
// residual. The residual uses + - * / and sqrt only, so each platform gives the same bits
// (CLAUDE.md, rules 1 and 2). Phase K2a gives the kinds 1 to 5. Phase K2c (TASK-0110) gives the
// kinds 6 to 10, 12 and 14 on points and lines; phase K2d gives the circle, the arc and the others.
public static class SketchConstraints
{
    public const string Coincident = "coincident";
    public const string Horizontal = "horizontal";
    public const string Vertical = "vertical";
    public const string Distance = "distance";
    public const string Fixed = "fixed";
    public const string PointOn = "pointOn";
    public const string Midpoint = "midpoint";
    public const string Parallel = "parallel";
    public const string Perpendicular = "perpendicular";
    public const string Equal = "equal";
    public const string Symmetric = "symmetric";
    public const string Angle = "angle";

    // ADR-0033 item 3: the limit of |cos² + sin² − 1| of the pair of an angle.
    public const double UnitPairLimit = 1e-9;

    // The kinds that this phase gives.
    public static IReadOnlyList<string> Given { get; } =
        [Coincident, Horizontal, Vertical, Distance, Fixed, PointOn, Midpoint, Parallel, Perpendicular, Equal, Symmetric, Angle];

    // The kinds 11 and 13, which ADR-0033 names and phase K2d gives with the circle and the arc.
    public static IReadOnlyList<string> Later { get; } = ["tangent", "radius"];

    // The kinds whose equation has two rows: a position or a vector of two coordinates.
    public static bool HasTwoRows(string kind) => kind is Coincident or Fixed or Midpoint or Symmetric or Angle;

    // Null when the kind, the items and the values form a constraint of this phase in the
    // sketch, or the reason of E-SKETCH-CONSTRAINT-INVALID.
    public static string? Refusal(SketchRecord sketch, string kind, IReadOnlyList<Guid> items, IReadOnlyList<double> values)
    {
        if (Later.Contains(kind))
            return $"The kind '{kind}' is a constraint of phase K2d, and this engine does not give it yet (ADR-0033 item 3).";
        if (!Given.Contains(kind))
            return $"The kind '{kind}' is not a kind of constraint. The kinds are {string.Join(", ", Given)}.";

        var kinds = items.Select(item => KindOf(sketch, item)).ToList();
        var shape = string.Join(", ", kinds);
        var fits = kind switch
        {
            Coincident => kinds is [ItemKind.Point, ItemKind.Point] && items[0] != items[1] && values.Count == 0,
            Horizontal or Vertical => kinds is [ItemKind.Line] && values.Count == 0,
            Distance => kinds is [ItemKind.Point, ItemKind.Point] or [ItemKind.Point, ItemKind.Line] or [ItemKind.Line]
                && (kinds is not [ItemKind.Point, ItemKind.Point] || items[0] != items[1])
                && values.Count == 1,
            Fixed => kinds is [ItemKind.Point] && values.Count == 2,
            // The point of pointOn and midpoint is not an end of its line: on an end, pointOn
            // says nothing and midpoint makes the line a point.
            PointOn or Midpoint => kinds is [ItemKind.Point, ItemKind.Line] && !IsEnd(sketch, items[0], items[1]) && values.Count == 0,
            Parallel or Perpendicular or Equal => kinds is [ItemKind.Line, ItemKind.Line] && items[0] != items[1] && values.Count == 0,
            Symmetric => kinds is [ItemKind.Point, ItemKind.Point, ItemKind.Line] && items[0] != items[1] && values.Count == 0,
            Angle => kinds is [ItemKind.Line, ItemKind.Line] && items[0] != items[1] && values.Count == 2,
            _ => false,
        };
        if (!fits)
            return $"A constraint '{kind}' does not take the items ({shape}) and {values.Count} values (ADR-0033 item 3).";

        if (values.Any(value => !double.IsFinite(value)))
            return $"A value of the constraint '{kind}' is not a finite number.";
        if (kind == Distance && values[0] < 0)
            return $"The length of a constraint 'distance' is {values[0]}, and a length is not negative.";
        if (kind == Angle && !(Math.Abs((values[0] * values[0]) + (values[1] * values[1]) - 1) <= UnitPairLimit))
            return $"The pair ({values[0]}, {values[1]}) of a constraint 'angle' is not a cosine and a sine: cos² + sin² differs from 1 by more than {UnitPairLimit}.";
        return null;
    }

    // ADR-0033 item 3: the values that a handler records. The pair of an angle is divided by its
    // length, so cos² + sin² is 1 to the last bits; each other kind keeps its values.
    public static IReadOnlyList<double> Recorded(string kind, IReadOnlyList<double> values)
    {
        if (kind != Angle)
            return values;
        var length = Math.Sqrt((values[0] * values[0]) + (values[1] * values[1]));
        return [values[0] / length, values[1] / length];
    }

    private static bool IsEnd(SketchRecord sketch, Guid point, Guid lineId)
    {
        var line = Line(sketch, lineId);
        return line.Start == point || line.End == point;
    }

    // ADR-0033 items 3 and 7: zero when the positions satisfy the constraint. For a kind
    // with two coordinates, the residual is the distance between the two positions.
    public static double Residual(SketchRecord sketch, SketchConstraint constraint)
    {
        switch (constraint.Kind)
        {
            case Coincident:
            {
                var (p, q) = (Point(sketch, constraint.Items[0]), Point(sketch, constraint.Items[1]));
                return Length(p.X - q.X, p.Y - q.Y);
            }
            case Horizontal:
            {
                var (a, b) = Ends(sketch, constraint.Items[0]);
                return b.Y - a.Y;
            }
            case Vertical:
            {
                var (a, b) = Ends(sketch, constraint.Items[0]);
                return b.X - a.X;
            }
            case Distance:
                return MeasuredDistance(sketch, constraint.Items) - constraint.Values[0];
            case Fixed:
            {
                var p = Point(sketch, constraint.Items[0]);
                return Length(p.X - constraint.Values[0], p.Y - constraint.Values[1]);
            }
            // ADR-0035, History of TASK-0110: each residual below is in length units, as the
            // equation of the solver is.
            case PointOn:
            {
                var p = Point(sketch, constraint.Items[0]);
                var (a, b) = Ends(sketch, constraint.Items[1]);
                return Offset(b.X - a.X, b.Y - a.Y, p.X - a.X, p.Y - a.Y);
            }
            case Midpoint:
            {
                var p = Point(sketch, constraint.Items[0]);
                var (a, b) = Ends(sketch, constraint.Items[1]);
                return Length(p.X - ((a.X + b.X) / 2), p.Y - ((a.Y + b.Y) / 2));
            }
            case Parallel or Perpendicular or Equal or Angle:
            {
                var (d1x, d1y) = Direction(sketch, constraint.Items[0]);
                var (d2x, d2y) = Direction(sketch, constraint.Items[1]);
                var n1 = Normaliser(d1x, d1y);
                return constraint.Kind switch
                {
                    Parallel => ((d1x * d2y) - (d1y * d2x)) / n1,
                    Perpendicular => ((d1x * d2x) + (d1y * d2y)) / n1,
                    Equal => Length(d1x, d1y) - Length(d2x, d2y),
                    _ => AngleError(d1x, d1y, d2x, d2y, constraint.Values[0], constraint.Values[1]),
                };
            }
            case Symmetric:
            {
                var (p, q) = (Point(sketch, constraint.Items[0]), Point(sketch, constraint.Items[1]));
                var (a, b) = Ends(sketch, constraint.Items[2]);
                var (dx, dy) = (b.X - a.X, b.Y - a.Y);
                var (mx, my) = ((p.X + q.X) / 2, (p.Y + q.Y) / 2);
                var n = Normaliser(dx, dy);
                var across = ((dx * (my - a.Y)) - (dy * (mx - a.X))) / n;
                var along = (((q.X - p.X) * dx) + ((q.Y - p.Y) * dy)) / n;
                return Length(across, along);
            }
            default:
                throw new InvalidOperationException($"The constraint {constraint.Id} has the kind '{constraint.Kind}', which this engine does not give.");
        }
    }

    // The distance of two points, of a point to a line, or the length of a line. A line of
    // length zero is a point, so the distance to it is the distance to its start.
    private static double MeasuredDistance(SketchRecord sketch, IReadOnlyList<Guid> items)
    {
        if (items.Count == 1)
        {
            var (a, b) = Ends(sketch, items[0]);
            return Length(b.X - a.X, b.Y - a.Y);
        }

        var p = Point(sketch, items[0]);
        if (KindOf(sketch, items[1]) == ItemKind.Point)
        {
            var q = Point(sketch, items[1]);
            return Length(p.X - q.X, p.Y - q.Y);
        }

        var (start, end) = Ends(sketch, items[1]);
        var (dx, dy) = (end.X - start.X, end.Y - start.Y);
        var length = Length(dx, dy);
        if (length == 0)
            return Length(p.X - start.X, p.Y - start.Y);
        var cross = (dx * (p.Y - start.Y)) - (dy * (p.X - start.X));
        return Math.Abs(cross) / length;
    }

    private static (SketchPoint Start, SketchPoint End) Ends(SketchRecord sketch, Guid lineId)
    {
        var line = Line(sketch, lineId);
        return (Point(sketch, line.Start), Point(sketch, line.End));
    }

    private static double Length(double dx, double dy) => Math.Sqrt((dx * dx) + (dy * dy));

    private static (double X, double Y) Direction(SketchRecord sketch, Guid lineId)
    {
        var (a, b) = Ends(sketch, lineId);
        return (b.X - a.X, b.Y - a.Y);
    }

    // The length of a direction, or 1 for a line of length zero, so that a quotient by it is
    // defined and the same on each platform.
    private static double Normaliser(double dx, double dy)
    {
        var length = Length(dx, dy);
        return length == 0 ? 1 : length;
    }

    // The signed distance of the offset w from the line of the direction d: cross(d, w) / |d|.
    // For a direction of length zero, the distance |w|.
    private static double Offset(double dx, double dy, double wx, double wy)
    {
        var length = Length(dx, dy);
        return length == 0 ? Length(wx, wy) : ((dx * wy) - (dy * wx)) / length;
    }

    // The length of d₂ − |d₂|·R(θ)·d₁ / |d₁|: zero when d₂ has the direction of d₁ turned by θ,
    // which is the condition of ADR-0033 item 3 with its inequality. A direction of length
    // zero has the unit direction (1, 0).
    private static double AngleError(double d1x, double d1y, double d2x, double d2y, double cos, double sin)
    {
        var l1 = Length(d1x, d1y);
        var (u1x, u1y) = l1 == 0 ? (1.0, 0.0) : (d1x / l1, d1y / l1);
        var l2 = Length(d2x, d2y);
        var (wx, wy) = ((cos * u1x) - (sin * u1y), (sin * u1x) + (cos * u1y));
        return Length(d2x - (l2 * wx), d2y - (l2 * wy));
    }
}
