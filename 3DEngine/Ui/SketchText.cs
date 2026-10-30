using System.Globalization;
using Engine.Core.Queries;

namespace ThreeDEngine.Desktop.Ui;

// The texts of a sketch in the desktop (ADR-0034 items 8 and 9): the label of each item, the row
// of each item in the window of the sketch, the length that a new distance starts with, and the
// text of a number for the pad. The labels are display numbers of the client in the order of the
// record, not identifiers: the origin is O, then P1, P2 for the points, L1 for the lines, and C1
// for the constraints.
internal static class SketchText
{
    public static IReadOnlyDictionary<Guid, string> Labels(SketchInfo sketch)
    {
        ArgumentNullException.ThrowIfNull(sketch);
        var labels = new Dictionary<Guid, string>();
        var number = 1;
        foreach (var point in sketch.Points)
            labels[point.Id] = point.Id == sketch.SketchId ? "O" : $"P{number++}";
        for (var index = 0; index < sketch.Lines.Count; index++)
            labels[sketch.Lines[index].Id] = $"L{index + 1}";
        // Phase K2e (TASK-0122): R for a circle (a ring) and A for an arc; C is a constraint.
        for (var index = 0; index < sketch.Circles.Count; index++)
            labels[sketch.Circles[index].Id] = $"R{index + 1}";
        for (var index = 0; index < sketch.Arcs.Count; index++)
            labels[sketch.Arcs[index].Id] = $"A{index + 1}";
        for (var index = 0; index < sketch.Constraints.Count; index++)
            labels[sketch.Constraints[index].Id] = $"C{index + 1}";
        return labels;
    }

    // The text of a number with no exponent and at most 15 decimals, so that the pad accepts it
    // (ADR-0032 item 3): 40, 12.346, -0.001.
    public static string Number(double value) => value.ToString("0.###############", CultureInfo.InvariantCulture);

    // A short text for a row: three decimals at most.
    public static string Short(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    public static IEnumerable<(Guid Id, string Text)> Rows(SketchInfo sketch)
    {
        ArgumentNullException.ThrowIfNull(sketch);
        var labels = Labels(sketch);
        foreach (var point in sketch.Points)
            yield return (point.Id, $"{labels[point.Id]}  ({Short(point.X)}, {Short(point.Y)})");
        foreach (var line in sketch.Lines)
            yield return (line.Id, $"{labels[line.Id]}  {labels[line.Start]}-{labels[line.End]}  {Short(Distance(sketch, [line.Id]) ?? 0)}");
        foreach (var circle in sketch.Circles)
            yield return (circle.Id, $"{labels[circle.Id]}  {labels[circle.Center]}  r {Short(circle.Radius)}");
        foreach (var arc in sketch.Arcs)
            yield return (arc.Id, $"{labels[arc.Id]}  {labels[arc.Center]} {labels[arc.Start]}-{labels[arc.End]}  r {Short(Radius(sketch, arc.Id) ?? 0)}");
        foreach (var constraint in sketch.Constraints)
        {
            var items = string.Join(" ", constraint.Items.Select(item => labels.GetValueOrDefault(item, "?")));
            var values = constraint.Values.Count == 0 ? string.Empty : " = " + string.Join(", ", constraint.Values.Select(Short));
            yield return (constraint.Id, $"{labels[constraint.Id]}  {constraint.Kind} {items}{values}  r {(constraint.Residual is { } residual ? Short(residual) : "not finite")}");
        }
    }

    // The radius of a circle, or the distance from the centre of an arc to its start, as the
    // engine measures it (SketchConstraints.CurveRadius), or null.
    public static double? Radius(SketchInfo sketch, Guid curve)
    {
        ArgumentNullException.ThrowIfNull(sketch);
        if (sketch.Circles.FirstOrDefault(circle => circle.Id == curve) is { } found)
            return found.Radius;
        if (sketch.Arcs.FirstOrDefault(arc => arc.Id == curve) is not { } arc)
            return null;
        var points = sketch.Points.ToDictionary(point => point.Id);
        var (c, s) = (points[arc.Center], points[arc.Start]);
        return Math.Sqrt(((s.X - c.X) * (s.X - c.X)) + ((s.Y - c.Y) * (s.Y - c.Y)));
    }

    // The distance of two points, of a point to a line, or the length of a line, as the engine
    // measures it for the residual of a constraint "distance" (ADR-0033 item 3), or null.
    public static double? Distance(SketchInfo sketch, IReadOnlyList<Guid> items)
    {
        var points = sketch.Points.ToDictionary(point => point.Id);
        var lines = sketch.Lines.ToDictionary(line => line.Id);
        switch (items)
        {
            case [var only] when lines.TryGetValue(only, out var line):
                return Length(points[line.End].X - points[line.Start].X, points[line.End].Y - points[line.Start].Y);
            case [var a, var b] when points.TryGetValue(a, out var p) && points.TryGetValue(b, out var q):
                return Length(p.X - q.X, p.Y - q.Y);
            case [var a, var b] when points.TryGetValue(a, out var p) && lines.TryGetValue(b, out var line):
            {
                var (start, end) = (points[line.Start], points[line.End]);
                var (dx, dy) = (end.X - start.X, end.Y - start.Y);
                var length = Length(dx, dy);
                return length == 0 ? Length(p.X - start.X, p.Y - start.Y) : Math.Abs((dx * (p.Y - start.Y)) - (dy * (p.X - start.X))) / length;
            }
            default:
                return null;
        }
    }

    private static double Length(double dx, double dy) => Math.Sqrt((dx * dx) + (dy * dy));
}
