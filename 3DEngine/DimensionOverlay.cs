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
                labels.Add(new Label(((line.A + line.B) / 2) + LabelOffset, Dimensions.Label(dimension, unit), mark));
            }
        }
    }

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
