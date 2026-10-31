using Engine.Contracts.Sketch;
using static Engine.Core.Sketch.SketchModel;

namespace Engine.Core.Sketch;

// ADR-0033 item 3: the kinds of a constraint, the check of its items and values, and its
// residual. The residual uses + - * / and sqrt only, so each platform gives the same bits
// (CLAUDE.md, rules 1 and 2). Phase K2a gives the kinds 1 to 5. Phase K2c (TASK-0110) gives the
// kinds 6 to 10, 12 and 14 on points and lines; phase K2d (TASK-0117) gives the circle, the arc,
// tangent, radius, and pointOn and equal on a circle or an arc. The radius of an arc is the
// distance from its centre to its start.
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
    public const string Tangent = "tangent";
    public const string Radius = "radius";

    // ADR-0033 item 3: the limit of |cos² + sin² − 1| of the pair of an angle.
    public const double UnitPairLimit = 1e-9;

    // The kinds that this phase gives.
    public static IReadOnlyList<string> Given { get; } =
        [Coincident, Horizontal, Vertical, Distance, Fixed, PointOn, Midpoint, Parallel, Perpendicular, Equal, Tangent, Symmetric, Radius, Angle];

    // The kinds whose equation has two rows: a position or a vector of two coordinates.
    public static bool HasTwoRows(string kind) => kind is Coincident or Fixed or Midpoint or Symmetric;

    // Null when the kind, the items and the values form a constraint of this phase in the
    // sketch, or the reason of E-SKETCH-CONSTRAINT-INVALID.
    public static string? Refusal(SketchRecord sketch, string kind, IReadOnlyList<Guid> items, IReadOnlyList<double> values)
    {
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
            // A point of symmetric on an end of the line puts the other point on it too.
            Symmetric => kinds is [ItemKind.Point, ItemKind.Point, ItemKind.Line] && items[0] != items[1]
                && !IsEnd(sketch, items[0], items[2]) && !IsEnd(sketch, items[1], items[2]) && values.Count == 0,
            Angle => kinds is [ItemKind.Line, ItemKind.Line] && items[0] != items[1] && values.Count == 2,
            _ => false,
        };

        // TASK-0117: the kinds on a circle or an arc. A point of pointOn is not a point of its
        // curve, two curves are two curves, and a tangent of two curves gives 1 (outside) or −1
        // (inside). A line of a tangent has no end at the centre of its curve: that end is on the
        // line, so the radius must be 0 (finding E97 of the codebase review of 2026-10-30 (b)). An entry
        // of an open or a replay passes, because v0.117 to v0.122 applied it (E103, TASK-0132, Q1 A).
        fits = fits || kind switch
        {
            PointOn => items.Count == 2 && kinds[0] == ItemKind.Point && IsCurve(kinds[1])
                && !CurvePoints(sketch, items[1]).Contains(items[0]) && values.Count == 0,
            Equal => items.Count == 2 && IsCurve(kinds[0]) && IsCurve(kinds[1]) && items[0] != items[1] && values.Count == 0,
            Tangent => items.Count == 2 && ((kinds[0] == ItemKind.Line && IsCurve(kinds[1]) && (SketchResults.ApplyingEntry.Value || !IsEnd(sketch, Centre(sketch, items[1]), items[0])) && values.Count == 0)
                || (IsCurve(kinds[0]) && IsCurve(kinds[1]) && items[0] != items[1] && values is [1 or -1])),
            Radius => kinds is [ItemKind.Circle or ItemKind.Arc] && values.Count == 1,
            _ => false,
        };
        if (!fits)
            return $"A constraint '{kind}' does not take the items ({shape}) and {values.Count} values (ADR-0033 item 3).";

        if (values.Any(value => !double.IsFinite(value)))
            return $"A value of the constraint '{kind}' is not a finite number.";
        if (kind == Radius && !(values[0] > 0))
            return $"The radius of a constraint 'radius' is {values[0]}, and a radius is positive.";
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

    private static bool IsCurve(ItemKind kind) => kind is ItemKind.Circle or ItemKind.Arc;

    // The points that define a curve: the centre of a circle, or the centre, the start and the end
    // of an arc.
    private static IReadOnlyList<Guid> CurvePoints(SketchRecord sketch, Guid curve)
        => KindOf(sketch, curve) == ItemKind.Circle ? [Circle(sketch, curve).Center] : [Arc(sketch, curve).Center, Arc(sketch, curve).Start, Arc(sketch, curve).End];

    // The centre of a circle or of an arc.
    public static Guid Centre(SketchRecord sketch, Guid curve)
        => KindOf(sketch, curve) == ItemKind.Circle ? Circle(sketch, curve).Center : Arc(sketch, curve).Center;

    // The radius of a circle, or the distance from the centre of an arc to its start.
    public static double CurveRadius(SketchRecord sketch, Guid curve)
    {
        if (KindOf(sketch, curve) == ItemKind.Circle)
            return Circle(sketch, curve).Radius;
        var arc = Arc(sketch, curve);
        var (c, s) = (Point(sketch, arc.Center), Point(sketch, arc.Start));
        return Length(s.X - c.X, s.Y - c.Y);
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
            // ADR-0035, History of TASK-0110: each residual below has the unit of the equation of
            // the solver: a length, or for parallel, perpendicular and angle a value of the two
            // unit directions, which no length changes.
            case PointOn when KindOf(sketch, constraint.Items[1]) != ItemKind.Line:
            {
                var (p, c) = (Point(sketch, constraint.Items[0]), Point(sketch, Centre(sketch, constraint.Items[1])));
                return Length(p.X - c.X, p.Y - c.Y) - CurveRadius(sketch, constraint.Items[1]);
            }
            case Equal when KindOf(sketch, constraint.Items[0]) != ItemKind.Line:
                return CurveRadius(sketch, constraint.Items[0]) - CurveRadius(sketch, constraint.Items[1]);
            case Radius:
                return CurveRadius(sketch, constraint.Items[0]) - constraint.Values[0];
            case Tangent when KindOf(sketch, constraint.Items[0]) == ItemKind.Line:
            {
                var c = Point(sketch, Centre(sketch, constraint.Items[1]));
                var (a, b) = Ends(sketch, constraint.Items[0]);
                return Math.Abs(Offset(b.X - a.X, b.Y - a.Y, c.X - a.X, c.Y - a.Y)) - CurveRadius(sketch, constraint.Items[1]);
            }
            case Tangent:
            {
                var (c1, c2) = (Point(sketch, Centre(sketch, constraint.Items[0])), Point(sketch, Centre(sketch, constraint.Items[1])));
                var (r1, r2) = (CurveRadius(sketch, constraint.Items[0]), CurveRadius(sketch, constraint.Items[1]));
                var distance = Length(c1.X - c2.X, c1.Y - c2.Y);
                return constraint.Values[0] > 0 ? distance - (r1 + r2) : distance - Math.Abs(r1 - r2);
            }
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
                var n = Normaliser(d1x, d1y) * Normaliser(d2x, d2y);
                return constraint.Kind switch
                {
                    Parallel => ((d1x * d2y) - (d1y * d2x)) / n,
                    Perpendicular => ((d1x * d2x) + (d1y * d2y)) / n,
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

    // The length of u₂ − R(θ)·u₁ for the unit directions of the two lines: zero when d₂ has the
    // direction of d₁ turned by θ, which is the condition of ADR-0033 item 3 with its inequality.
    // A direction of length zero has the unit direction (1, 0).
    private static double AngleError(double d1x, double d1y, double d2x, double d2y, double cos, double sin)
    {
        var l1 = Length(d1x, d1y);
        var (u1x, u1y) = l1 == 0 ? (1.0, 0.0) : (d1x / l1, d1y / l1);
        var l2 = Length(d2x, d2y);
        var (u2x, u2y) = l2 == 0 ? (1.0, 0.0) : (d2x / l2, d2y / l2);
        var (wx, wy) = ((cos * u1x) - (sin * u1y), (sin * u1x) + (cos * u1y));
        return Length(u2x - wx, u2y - wy);
    }
}
