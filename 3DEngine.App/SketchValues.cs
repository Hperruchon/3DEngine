using System.Numerics;
using Engine.Contracts.Sketch;
using Engine.Core.Queries;
using ThreeDEngine.Core.Measuring;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.Desktop;

// ADR-0105 (the redesign, phase R16s, TASK-0327): the values of the active sketch, each on the line or the curve that it
// measures, in pixels of the window. The values are presentation (ADR-0007): they read the record of GetSketch and send
// nothing. A value is an EdgeValues.Value with no body, so that the chips of the extrude draw it.
internal static class SketchValues
{
    // ADR-0105 item 3 (TASK-0327): the key of the chip of a constraint of the active sketch. It is not a key of EdgeValues,
    // which starts with "Value:", so a chip of a sketch and a chip of an extrude never have the same rectangle.
    public static string Key(Guid constraint) => $"SketchValue:{constraint:N}";

    // ADR-0105 item 1 (TASK-0327): a value for each distance of one line, or of two points, and for each radius of a circle
    // or an arc. A constraint of a reference item, or a value whose place is behind the eye, has no value.
    public static IReadOnlyList<EdgeValues.Value> Of(SketchInfo sketch, Camera camera, double width, double height, string unit)
    {
        ArgumentNullException.ThrowIfNull(sketch);
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(unit);

        Vector2? PixelOf(double x, double y) => SketchOverlay.Pixel(sketch, camera, width, height, (x, y));

        // ADR-0105 item 2 (TASK-0327): the centre of the sketch on the screen is the middle of the box of the pixels of its
        // points. The origin O of the frame is not a point of the drawing, so it is not in the box.
        var pixels = sketch.Points.Where(point => point.Id != sketch.SketchId).Select(point => PixelOf(point.X, point.Y)).OfType<Vector2>().ToList();
        if (pixels.Count == 0)
            return [];
        var centre = (pixels.Aggregate(Vector2.Min) + pixels.Aggregate(Vector2.Max)) / 2f;

        var points = sketch.Points.ToDictionary(point => point.Id);
        var lines = sketch.Lines.ToDictionary(line => line.Id);
        var curves = SketchOverlay.Curves(sketch).ToDictionary(curve => curve.Id, curve => curve.Polyline);
        var driven = sketch.References.SelectMany(reference => reference.Line is { } line ? reference.Points.Append(line) : reference.Points).ToHashSet();
        var labels = SketchText.Labels(sketch);

        var values = new List<EdgeValues.Value>();
        foreach (var constraint in sketch.Constraints)
        {
            if (constraint.Values.Count == 0 || constraint.Items.Any(driven.Contains))
                continue;

            // ADR-0105 item 2 (TASK-0327): a distance is at the middle of its line or of its two points, and a radius at the
            // point of its curve farthest from the centre.
            var (at, prefix, label) = constraint.Kind switch
            {
                "distance" => (Ends(constraint.Items) is { } ends ? PixelOf((ends.A.X + ends.B.X) / 2, (ends.A.Y + ends.B.Y) / 2) : null, string.Empty, "Length"),
                "radius" when constraint.Items.Count == 1 && curves.TryGetValue(constraint.Items[0], out var polyline)
                    => (Farthest(polyline), "R ", "Radius"),
                _ => ((Vector2?)null, string.Empty, string.Empty),
            };
            if (at is not { } place)
                continue;

            // ADR-0105 item 3 (TASK-0327): the label of the constraint, two spaces, the value and the unit.
            var number = constraint.Values[0];
            values.Add(new EdgeValues.Value(Key(constraint.Id), $"{labels[constraint.Id]}  {prefix}{Dimensions.Format(number)} {unit}", Away(place, centre),
                number, Guid.Empty, sketch.SketchId, constraint.Id, label));
        }

        return values;

        // A distance of one line, or of two points (the two ends of a line are two points), measures between two points.
        (SketchPoint A, SketchPoint B)? Ends(IReadOnlyList<Guid> items) => items switch
        {
            [var only] when lines.TryGetValue(only, out var line) && points.TryGetValue(line.Start, out var a) && points.TryGetValue(line.End, out var b) => (a, b),
            [var first, var second] when points.TryGetValue(first, out var a) && points.TryGetValue(second, out var b) => (a, b),
            _ => null,
        };

        // The pixel of the polyline of a curve farthest from the centre, of the pixels in front of the eye, or null.
        Vector2? Farthest(IReadOnlyList<(double X, double Y)> polyline)
            => polyline.Select(point => PixelOf(point.X, point.Y)).OfType<Vector2>()
                .OrderByDescending(pixel => Vector2.DistanceSquared(pixel, centre))
                .Cast<Vector2?>()
                .FirstOrDefault();
    }

    // ADR-0105 item 2 (TASK-0327): the chip goes 24 pixels farther from the centre, as EdgeValues does; a place at the
    // centre goes up.
    private static Vector2 Away(Vector2 at, Vector2 centre)
    {
        var outward = at - centre;
        var length = outward.Length();
        return length < 1e-3f ? at + new Vector2(0f, -EdgeValues.Offset) : at + (outward / length * EdgeValues.Offset);
    }
}
