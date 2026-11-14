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

    public sealed record Label(Vector2 At, string Text, Mark Mark);

    // ADR-0125 item 3 (phase R20c, TASK-0352): one dimension of a box: the measured edge on the screen, the side of its
    // dimension line away from the outline of the box, and the distance to the outside of that outline, as a value on an edge,
    // with the axis and the value ("X 1200 mm"). The window adds the word of the box, the plate and the lines.
    public sealed record Measured(EdgeValues.Measure Line, string Text, Mark Mark);

    public sealed record Strokes(IReadOnlyList<Measured> Measures);

    // The dimensions of each box that is not null, seen from the camera. Until TASK-0352 a dimension line was out from its
    // edge by one twentieth of the largest extent in the space of the Document, and the label was 20 pixels over its middle,
    // so a label lay on the body and on its edges.
    public static Strokes Of(
        ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)? overall,
        ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)? selected,
        Camera camera, double width, double height, string unit)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(unit);
        var measures = new List<Measured>();
        var eye = camera.Eye();
        Add(overall, Mark.Overall);
        Add(selected, Mark.Selected);
        return new Strokes(measures);

        void Add(((double X, double Y, double Z) Min, (double X, double Y, double Z) Max)? box, Mark mark)
        {
            if (box is not { } b)
                return;
            // ADR-0125 item 3: the outline of a box on the screen is its corners in front of the eye, and its centre is their
            // mean. A box is convex, so a dimension line past each corner is outside the outline.
            var outline = Corners(b.Min, b.Max)
                .Select(corner => SketchOverlay.Segment(camera, width, height, corner, corner)?.A)
                .OfType<Vector2>()
                .ToList();
            if (outline.Count == 0)
                return;
            var centre = outline.Aggregate(Vector2.Zero, (sum, point) => sum + point) / outline.Count;
            // V147 (TASK-0311): the dimensions take the up axis of the camera.
            foreach (var dimension in Dimensions.Of(b.Min, b.Max, eye, camera.ZUp))
            {
                // Finding V80 of the codebase review of 2026-11-05 (TASK-0181): the clip keeps a band of one window on
                // each side, so the middle of a line partly behind the eye was outside the window. The measure takes the
                // part of the edge inside the window, and an edge with no such part has no dimension.
                if (SketchOverlay.Segment(camera, width, height, dimension.EdgeFrom, dimension.EdgeTo) is not { } edge
                    || Inside(edge.A, edge.B, (float)width, (float)height) is not { } shown)
                    continue;
                measures.Add(new Measured(EdgeValues.Outside(shown.A, shown.B, centre, outline), Dimensions.Label(dimension, unit), mark));
            }
        }
    }

    // The eight corners of a box.
    private static IEnumerable<(double X, double Y, double Z)> Corners((double X, double Y, double Z) min, (double X, double Y, double Z) max)
    {
        foreach (var x in new[] { min.X, max.X })
        {
            foreach (var y in new[] { min.Y, max.Y })
            {
                foreach (var z in new[] { min.Z, max.Z })
                    yield return (x, y, z);
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
    // ADR-0125 item 4 (phase R20c, TASK-0352): a label also moves off each rectangle of the covers (min x, min y, max x,
    // max y), the controls that are drawn over it. When the moves down and up do not free it, it moves right of the cover
    // that it meets, then left of it, from its place.
    internal static IReadOnlyList<Label> Place(IReadOnlyList<Label> labels, Func<string, Vector2> size, Vector2 window, IReadOnlyList<Vector4>? covers = null)
    {
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(size);
        var blocked = (covers ?? []).Select(cover => (Min: new Vector2(cover.X, cover.Y), Max: new Vector2(cover.Z, cover.W))).ToList();
        var placed = new List<(Label Label, Vector2 Min, Vector2 Max)>();
        var steps = labels.Count + blocked.Count;
        foreach (var label in labels)
        {
            var extent = size(label.Text);
            var at = Clamp(label.At, extent, window);
            var start = at;
            for (var step = 0; step <= steps && Meets(at, extent) is { } other; step++)
                at = Clamp(new Vector2(at.X, other.Max.Y + LabelGap), extent, window);
            for (var step = 0; step <= steps && Meets(at, extent) is { } other; step++)
                at = Clamp(new Vector2(start.X, Math.Min(at.Y, other.Min.Y) - extent.Y - LabelGap), extent, window);
            if (Meets(at, extent) is not null)
            {
                at = start;
                for (var step = 0; step <= steps && Meets(at, extent) is { } other; step++)
                    at = Clamp(new Vector2(Math.Max(at.X, other.Max.X) + LabelGap, start.Y), extent, window);
                for (var step = 0; step <= steps && Meets(at, extent) is { } other; step++)
                    at = Clamp(new Vector2(Math.Min(at.X, other.Min.X) - extent.X - LabelGap, start.Y), extent, window);

                // Finding V176 of the codebase review of 2026-11-14 (TASK-0356): a label that no move frees stays at its place,
                // under the control (ADR-0125, consequences). Until TASK-0356 it stayed where the moves up ended, at the top
                // edge, on the other labels of the same case.
                if (Meets(at, extent) is not null)
                    at = start;
            }

            placed.Add((label with { At = at }, at, at + extent));
        }

        return [.. placed.Select(item => item.Label)];

        (Vector2 Min, Vector2 Max)? Meets(Vector2 at, Vector2 extent)
        {
            foreach (var (_, min, max) in placed)
            {
                if (at.X < max.X && at.X + extent.X > min.X && at.Y < max.Y && at.Y + extent.Y > min.Y)
                    return (min, max);
            }
            foreach (var (min, max) in blocked)
            {
                if (at.X < max.X && at.X + extent.X > min.X && at.Y < max.Y && at.Y + extent.Y > min.Y)
                    return (min, max);
            }
            return null;
        }
    }

    private static Vector2 Clamp(Vector2 at, Vector2 extent, Vector2 window)
        => new(Math.Clamp(at.X, 0f, Math.Max(0f, window.X - extent.X)), Math.Clamp(at.Y, 0f, Math.Max(0f, window.Y - extent.Y)));

    // ADR-0056 item 6 (Q1 A): the size of the selected body and its thickness, for the panel, on two lines, because one
    // line of the panel at the font size of ADR-0027 does not hold both.
    // ADR-0125 item 5 (phase R20c, TASK-0352): a body that is not a board has no thickness line. Until TASK-0352 a cube of
    // 400 showed "Thickness 400 mm".
    public static string SizeText(((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) box, double? distance, string unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        var (min, max) = box;
        var size = $"Size {Dimensions.Format(max.X - min.X)} x {Dimensions.Format(max.Y - min.Y)} x {Dimensions.Format(max.Z - min.Z)} {unit}";
        var thickness = Dimensions.Thickness(min, max, distance);
        return Board(min, max, thickness) ? $"{size}\nThickness {Dimensions.Format(thickness)} {unit}" : size;
    }

    // ADR-0125 item 5: a body is a board when its thickness is more than zero and at most a quarter of the middle extent of
    // its box. A panel of 18 by 400 by 800 is a board; a cube, and a bar of 40 by 40 by 800, are not.
    internal const double BoardRatio = 4;

    internal static bool Board((double X, double Y, double Z) min, (double X, double Y, double Z) max, double thickness)
    {
        double[] extents = [max.X - min.X, max.Y - min.Y, max.Z - min.Z];
        Array.Sort(extents);
        return thickness > 0 && thickness * BoardRatio <= extents[1];
    }
}
