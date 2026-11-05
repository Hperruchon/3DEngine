using System.Numerics;
using ThreeDEngine.Core.Measuring;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Desktop;

// ADR-0056 items 4 to 6 (phase R13, TASK-0178): the dimension lines of the overall box and of the selected body in
// pixels of the window, with their labels, and the text of the size of the selected body. The overlay is presentation
// (ADR-0007): it reads the boxes of the meshes that the desktop holds and sends nothing to the engine.
internal static class DimensionOverlay
{
    // Which box a stroke measures: the overall box, or the box of the selected body.
    public enum Mark
    {
        Overall,
        Selected,
    }

    // A dimension line has an arrow at each end; an extension line has none.
    public sealed record Line(Vector2 A, Vector2 B, bool Arrows, Mark Mark);

    public sealed record Label(Vector2 At, string Text, Mark Mark);

    public sealed record Strokes(IReadOnlyList<Line> Lines, IReadOnlyList<Label> Labels);

    // The place of a label from the middle of its dimension line, in pixels.
    private static readonly Vector2 LabelOffset = new(6f, -20f);

    // The lines and the labels of each box that is not null, seen from the camera.
    public static Strokes Of(
        ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)? overall,
        ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)? selected,
        Camera camera, double width, double height, string unit)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(unit);
        var lines = new List<Line>();
        var labels = new List<Label>();
        var eye = camera.Eye();
        Add(overall, Mark.Overall);
        Add(selected, Mark.Selected);
        return new Strokes(lines, labels);

        void Add(((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)? box, Mark mark)
        {
            if (box is not { } b)
                return;
            foreach (var dimension in Dimensions.Of(b.Min, b.Max, eye))
            {
                if (SketchOverlay.Segment(camera, width, height, dimension.From, dimension.To) is not { } line)
                    continue;
                lines.Add(new Line(line.A, line.B, true, mark));
                foreach (var (edge, end) in new[] { (dimension.EdgeFrom, dimension.From), (dimension.EdgeTo, dimension.To) })
                {
                    if (SketchOverlay.Segment(camera, width, height, edge, end) is { } extension)
                        lines.Add(new Line(extension.A, extension.B, false, mark));
                }
                // Finding V80 of the codebase review of 2026-11-05 (TASK-0181): the clip keeps a band of one window on
                // each side, so the middle of a line partly behind the eye was outside the window. The label is at the
                // middle of the part inside the window, and a line with no such part has no label.
                if (Inside(line.A, line.B, (float)width, (float)height) is { } shown)
                    labels.Add(new Label(((shown.A + shown.B) / 2) + LabelOffset, Dimensions.Label(dimension, unit), mark));
            }
        }
    }

    // The part of a segment inside the window from (0, 0) to (width, height), or null (the clip of Liang and Barsky).
    internal static (Vector2 A, Vector2 B)? Inside(Vector2 a, Vector2 b, float width, float height)
    {
        var (low, high) = (0f, 1f);
        var d = b - a;
        foreach (var (p, q) in new[] { (-d.X, a.X), (d.X, width - a.X), (-d.Y, a.Y), (d.Y, height - a.Y) })
        {
            if (p == 0)
            {
                if (q < 0)
                    return null;
                continue;
            }

            var t = q / p;
            if (p < 0)
                low = Math.Max(low, t);
            else
                high = Math.Min(high, t);
            if (low > high)
                return null;
        }

        return (a + (d * low), a + (d * high));
    }

    // The gap between two labels that the placement moves apart, in pixels.
    private const float LabelGap = 2f;

    // Finding V79 of the codebase review of 2026-11-05 (TASK-0181): the labels of a thin board overlapped. Each label, in
    // its order, stays inside the window, and moves down while it meets a label placed before it; at the bottom edge it
    // moves up from its place. The size of each text comes from the caller, so the rule holds for each font.
    internal static IReadOnlyList<Label> Place(IReadOnlyList<Label> labels, Func<string, Vector2> size, Vector2 window)
    {
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(size);
        var placed = new List<(Label Label, Vector2 Min, Vector2 Max)>();
        foreach (var label in labels)
        {
            var extent = size(label.Text);
            var at = Clamp(label.At, extent, window);
            var start = at;
            for (var step = 0; step <= labels.Count && Meets(at, extent) is { } other; step++)
                at = Clamp(new Vector2(at.X, other.Max.Y + LabelGap), extent, window);
            for (var step = 0; step <= labels.Count && Meets(at, extent) is { } other; step++)
                at = Clamp(new Vector2(start.X, Math.Min(at.Y, other.Min.Y) - extent.Y - LabelGap), extent, window);
            placed.Add((label with { At = at }, at, at + extent));
        }

        return [.. placed.Select(item => item.Label)];

        (Label Label, Vector2 Min, Vector2 Max)? Meets(Vector2 at, Vector2 extent)
        {
            foreach (var item in placed)
            {
                if (at.X < item.Max.X && at.X + extent.X > item.Min.X && at.Y < item.Max.Y && at.Y + extent.Y > item.Min.Y)
                    return item;
            }
            return null;
        }
    }

    private static Vector2 Clamp(Vector2 at, Vector2 extent, Vector2 window)
        => new(Math.Clamp(at.X, 0f, Math.Max(0f, window.X - extent.X)), Math.Clamp(at.Y, 0f, Math.Max(0f, window.Y - extent.Y)));

    // ADR-0056 item 6 (Q1 A): the size of the selected body and its thickness, for the panel, on two lines, because one
    // line of the panel at the font size of ADR-0027 does not hold both.
    public static string SizeText(((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) box, double? distance, string unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        var (min, max) = box;
        return $"Size {Dimensions.Format(max.X - min.X)} x {Dimensions.Format(max.Y - min.Y)} x {Dimensions.Format(max.Z - min.Z)} {unit}"
            + $"\nThickness {Dimensions.Format(Dimensions.Thickness(min, max, distance))} {unit}";
    }
}
