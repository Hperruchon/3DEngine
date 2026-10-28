using Engine.Contracts.Sketch;
using static Engine.Core.Sketch.SketchModel;

namespace Engine.Core.Sketch;

// ADR-0033 item 3: the kinds of a constraint, the check of its items and values, and its
// residual. The residual uses + - * / and sqrt only, so each platform gives the same bits
// (CLAUDE.md, rules 1 and 2). Phase K2a gives the kinds 1 to 5; phase K2 gives the others.
public static class SketchConstraints
{
    public const string Coincident = "coincident";
    public const string Horizontal = "horizontal";
    public const string Vertical = "vertical";
    public const string Distance = "distance";
    public const string Fixed = "fixed";

    // The kinds 1 to 5, which this phase gives.
    public static IReadOnlyList<string> Given { get; } = [Coincident, Horizontal, Vertical, Distance, Fixed];

    // The kinds 6 to 14, which ADR-0033 names and phase K2 gives.
    public static IReadOnlyList<string> Later { get; } =
        ["pointOn", "midpoint", "parallel", "perpendicular", "equal", "tangent", "symmetric", "radius", "angle"];

    // Null when the kind, the items and the values form a constraint of this phase in the
    // sketch, or the reason of E-SKETCH-CONSTRAINT-INVALID.
    public static string? Refusal(SketchRecord sketch, string kind, IReadOnlyList<Guid> items, IReadOnlyList<double> values)
    {
        if (Later.Contains(kind))
            return $"The kind '{kind}' is a constraint of phase K2, and this engine does not give it yet (ADR-0033 item 3).";
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
            _ => false,
        };
        if (!fits)
            return $"A constraint '{kind}' does not take the items ({shape}) and {values.Count} values (ADR-0033 item 3).";

        if (values.Any(value => !double.IsFinite(value)))
            return $"A value of the constraint '{kind}' is not a finite number.";
        if (kind == Distance && values[0] < 0)
            return $"The length of a constraint 'distance' is {values[0]}, and a length is not negative.";
        return null;
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
}
