using System.Globalization;
using System.Numerics;
using Engine.Contracts.Sketch;
using Engine.Core.Queries;
using ThreeDEngine.Core.Measuring;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.Desktop;

// ADR-0082 (the redesign, slice c, TASK-0254): the values of the selected extrude that a command changes, each on the edge
// of the body that it measures, in pixels of the window. The values are presentation (ADR-0007): they read the entry of
// GetFaces and the sketch of the extrude, and send nothing.
internal static class EdgeValues
{
    // A value on an edge. Constraint is null for the distance of the extrude; Sketch is the sketch of the constraint.
    // ADR-0084 item 2 (TASK-0267): Label names the value in its row: "Distance", "Length" or "Radius".
    // ADR-0115 item 2 (phase R18c, TASK-0340): Line is the measured edge on the screen and the side of its dimension line, or
    // null for a value with no edge.
    public sealed record Value(string Key, string Text, Vector2 At, double Number, Guid Body, Guid? Sketch, Guid? Constraint, string Label = "Value", Measure? Line = null, string? Name = null);

    // ADR-0115 item 2: the two ends of the measured edge on the screen (one point, the same two times, for a radius), the
    // unit normal that points away from the outline, and the distance along it from the edge to the outside of the outline
    // and a gap. The window puts the dimension line and the pill farther by the half of the pill.
    public sealed record Measure(Vector2 A, Vector2 B, Vector2 Normal, float Offset)
    {
        public bool Leader => A == B;
    }

    // ADR-0115 item 2: the gap between the outline of the body and the nearest side of a pill, in pixels.
    public const float Gap = 16f;

    // ADR-0115 item 1 (Q6 A): a value with three decimals, and more when it has them, as "60.000" or "60.125"; at most 15
    // significant digits, so 0.1 + 0.2 gives "0.300". Until TASK-0340 a value had at most two decimals, as "60".
    public static string Exact(double value)
        => (value == 0d ? 0d : value).ToString("0.000############", CultureInfo.InvariantCulture);

    // ADR-0115 item 2: the dimension line of an edge from a to b is parallel to it, on the side away from the centre, past
    // each point of the outline. A radius has one point: its leader goes straight out from the centre.
    internal static Measure Outside(Vector2 a, Vector2 b, Vector2 centre, IReadOnlyList<Vector2> outline)
    {
        ArgumentNullException.ThrowIfNull(outline);
        var along = b - a;
        var length = along.Length();
        var middle = (a + b) / 2f;
        Vector2 normal;
        if (length < 1f)
        {
            var outward = middle - centre;
            normal = outward.Length() < 1e-3f ? new Vector2(0f, -1f) : Vector2.Normalize(outward);
            b = a;
        }
        else
        {
            normal = new Vector2(-along.Y, along.X) / length;
            if (Vector2.Dot(normal, middle - centre) < 0f)
                normal = -normal;
        }

        var reach = outline.Count == 0 ? 0f : Math.Max(0f, outline.Max(point => Vector2.Dot(point - a, normal)));
        return new Measure(a, b, normal, reach + Gap);
    }

    public static string DistanceKey(Guid body) => $"Value:{body:N}:distance";

    public static string ConstraintKey(Guid constraint) => $"Value:{constraint:N}";

    // Item 1: the distance on an edge between two side faces, and each length and radius of the sketch on its edge
    // between the face "end" and the side face of its item. The sketch is the sketch whose items name the side faces.
    public static IReadOnlyList<Value> Of(Guid body, double? distance, IReadOnlyList<EdgeEntry> edges, IReadOnlyCollection<SketchInfo> sketches,
        Camera camera, double width, double height, string unit)
    {
        ArgumentNullException.ThrowIfNull(edges);
        ArgumentNullException.ThrowIfNull(sketches);
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(unit);

        // The middle of the body on the screen: the mean of the middles of its edges on the screen.
        var middles = edges.Select(edge => Middle(edge, camera, width, height)).OfType<Vector2>().ToList();
        if (middles.Count == 0)
            return [];
        var centre = middles.Aggregate(Vector2.Zero, (sum, point) => sum + point) / middles.Count;

        // ADR-0115 item 2: the outline of the body on the screen: the ends of each segment of each edge.
        var outline = edges.SelectMany(edge => edge.Segments).SelectMany(segment => new[] { segment.Start, segment.End })
            .Select(point => Pixel(point, camera, width, height)).OfType<Vector2>().ToList();
        Measure MeasureOf(EdgeEntry edge, Vector2 fallback)
            => Ends(edge, camera, width, height) is { } ends ? Outside(ends.A, ends.B, centre, outline) : Outside(fallback, fallback, centre, outline);

        var values = new List<Value>();
        bool Side(DatumReference face) => face.FeatureId == body && face.Element == "side";
        if (distance is { } d)
        {
            var along = edges.Where(edge => Side(edge.First) && Side(edge.Second) && TrueLength(edge, camera, radius: false))
                .Select(edge => (Edge: edge, At: Middle(edge, camera, width, height)))
                .Where(pair => pair.At is not null)
                .OrderByDescending(pair => Vector2.DistanceSquared(pair.At.GetValueOrDefault(), centre))
                .FirstOrDefault();
            if (along.At is { } on)
            {
                var line = MeasureOf(along.Edge, on);
                values.Add(new Value(DistanceKey(body), $"{Exact(d)} {unit}", Placed(line), d, body, null, null, "Distance", line));
            }
        }

        var items = edges.SelectMany(edge => new[] { edge.First, edge.Second }).Where(Side).Select(face => face.ItemId).OfType<Guid>().ToHashSet();
        if (sketches.FirstOrDefault(sketch => sketch.Lines.Any(line => items.Contains(line.Id)) || sketch.Circles.Any(circle => items.Contains(circle.Id)) || sketch.Arcs.Any(arc => items.Contains(arc.Id))) is not { } of)
            return values;

        // ADR-0117 item 3 (phase R18d, TASK-0342): the name of each constraint in its sketch, as "C3".
        var names = SketchText.Labels(of);
        foreach (var constraint in of.Constraints)
        {
            if (constraint.Values.Count == 0)
                continue;
            var (item, prefix, label) = constraint.Kind switch
            {
                "distance" => (LineOf(of, constraint.Items), string.Empty, "Length"),
                "radius" when constraint.Items.Count == 1 => (constraint.Items[0], "R ", "Radius"),
                _ => ((Guid?)null, string.Empty, string.Empty),
            };
            if (item is not { } id)
                continue;
            var edge = edges.FirstOrDefault(candidate => Pair(candidate, body, id));
            if (edge is not null && !TrueLength(edge, camera, constraint.Kind == "radius"))
                continue;
            // ADR-0115 item 2: a radius has a leader from the middle of its edge, straight out from the centre.
            if (edge is not null && Middle(edge, camera, width, height) is { } at)
            {
                var line = constraint.Kind == "radius" ? Outside(at, at, centre, outline) : MeasureOf(edge, at);
                // ADR-0117 item 3: a length along an axis is a width, a depth or a height.
                var named = constraint.Kind == "distance" ? Direction(edge) : label;
                values.Add(new Value(ConstraintKey(constraint.Id), $"{prefix}{Exact(constraint.Values[0])} {unit}", Placed(line), constraint.Values[0], body, of.SketchId, constraint.Id, named, line,
                    names.TryGetValue(constraint.Id, out var name) ? name : null));
            }
        }

        return values;
    }

    // ADR-0146 item 1 (phase R22a, TASK-0381): in a flat view a value shows only where its length is true, as on a drawing:
    // the edge of a length or of a distance is across the direction of the view, and the circle of a radius faces the view.
    // With perspective each value shows. An edge is across the view when the part of its direction
    // along the view is at most 0.001 of its length; a circle faces the view when the normal of its plane is along the view
    // within the same limit. Each test squares both sides, so it needs no square root.
    internal static bool TrueLength(EdgeEntry edge, Camera camera, bool radius)
    {
        ArgumentNullException.ThrowIfNull(edge);
        ArgumentNullException.ThrowIfNull(camera);
        if (camera.Flat is null || edge.Segments.Count == 0)
            return true;
        var forward = camera.Axes().Forward;
        static (double X, double Y, double Z) Minus(DatumPoint a, DatumPoint b) => (a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        static double Dot((double X, double Y, double Z) a, (double X, double Y, double Z) b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
        static (double X, double Y, double Z) Cross((double X, double Y, double Z) a, (double X, double Y, double Z) b)
            => ((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z), (a.X * b.Y) - (a.Y * b.X));
        const double limit = 1e-6;
        if (!radius)
        {
            var d = Minus(edge.Segments[^1].End, edge.Segments[0].Start);
            var part = Dot(d, forward);
            return part * part <= limit * Dot(d, d) * Dot(forward, forward);
        }

        // The normal of the plane of the circle, from two chords of its segments.
        var count = edge.Segments.Count;
        if (count < 3)
            return true;
        var origin = edge.Segments[0].Start;
        var normal = Cross(Minus(edge.Segments[count / 3].Start, origin), Minus(edge.Segments[2 * count / 3].Start, origin));
        var off = Cross(normal, forward);
        return Dot(off, off) <= limit * Dot(normal, normal) * Dot(forward, forward);
    }

    // A distance of one line, or of the start and the end of one line, measures that line.
    private static Guid? LineOf(SketchInfo sketch, IReadOnlyList<Guid> items) => items switch
    {
        [var line] when sketch.Lines.Any(candidate => candidate.Id == line) => line,
        [var a, var b] => sketch.Lines.FirstOrDefault(line => (line.Start == a && line.End == b) || (line.Start == b && line.End == a))?.Id,
        _ => null,
    };

    // True for the edge between the face "end" of the body and the side face of the item.
    private static bool Pair(EdgeEntry edge, Guid body, Guid item)
    {
        static bool End(DatumReference face, Guid body) => face.FeatureId == body && face.Element == "end";
        bool SideOf(DatumReference face) => face.FeatureId == body && face.Element == "side" && face.ItemId == item;
        return (End(edge.First, body) && SideOf(edge.Second)) || (End(edge.Second, body) && SideOf(edge.First));
    }

    // The middle of the middle segment of the edge on the screen, or null when it is behind the eye.
    private static Vector2? Middle(EdgeEntry edge, Camera camera, double width, double height)
    {
        if (edge.Segments.Count == 0)
            return null;
        var segment = edge.Segments[edge.Segments.Count / 2];
        var middle = ((segment.Start.X + segment.End.X) / 2, (segment.Start.Y + segment.End.Y) / 2, (segment.Start.Z + segment.End.Z) / 2);
        return SketchOverlay.Segment(camera, width, height, middle, middle)?.A;
    }

    // ADR-0117 item 3 (phase R18d, TASK-0342): the label of a length from the direction of its edge in the Document: along
    // the x axis "Width", along the y axis "Depth", along the z axis "Height" (the z axis is up), else "Length".
    // An edge is along an axis when that part of its direction is at least 0.999 of its length; the test squares both
    // sides, so it needs no square root.
    internal static string Direction(EdgeEntry edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        if (edge.Segments.Count == 0)
            return "Length";
        var (a, b) = (edge.Segments[0].Start, edge.Segments[^1].End);
        var (x, y, z) = (b.X - a.X, b.Y - a.Y, b.Z - a.Z);
        var length = (x * x) + (y * y) + (z * z);
        const double along = 0.999 * 0.999;
        return length <= 0d ? "Length"
            : x * x >= along * length ? "Width"
            : y * y >= along * length ? "Depth"
            : z * z >= along * length ? "Height"
            : "Length";
    }

    // ADR-0115 item 2: the place of a value before the window adds the half of its pill: the middle of the dimension line.
    internal static Vector2 Placed(Measure line) => ((line.A + line.B) / 2f) + (line.Normal * line.Offset);

    // ADR-0115 item 2: the two ends of an edge on the screen, from the start of its first segment to the end of its last, or
    // null when one of them is behind the eye.
    private static (Vector2 A, Vector2 B)? Ends(EdgeEntry edge, Camera camera, double width, double height)
        => edge.Segments.Count > 0 && Pixel(edge.Segments[0].Start, camera, width, height) is { } a
            && Pixel(edge.Segments[^1].End, camera, width, height) is { } b ? (a, b) : null;

    private static Vector2? Pixel(DatumPoint point, Camera camera, double width, double height)
        => SketchOverlay.Segment(camera, width, height, (point.X, point.Y, point.Z), (point.X, point.Y, point.Z))?.A;
}
