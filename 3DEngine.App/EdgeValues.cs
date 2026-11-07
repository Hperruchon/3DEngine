using System.Numerics;
using Engine.Contracts.Sketch;
using Engine.Core.Queries;
using ThreeDEngine.Core.Measuring;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Desktop;

// ADR-0082 (the redesign, slice c, TASK-0254): the values of the selected extrude that a command changes, each on the edge
// of the body that it measures, in pixels of the window. The values are presentation (ADR-0007): they read the entry of
// GetFaces and the sketch of the extrude, and send nothing.
internal static class EdgeValues
{
    // A value on an edge. Constraint is null for the distance of the extrude; Sketch is the sketch of the constraint.
    // ADR-0084 item 2 (TASK-0267): Label names the value in its row: "Distance", "Length" or "Radius".
    public sealed record Value(string Key, string Text, Vector2 At, double Number, Guid Body, Guid? Sketch, Guid? Constraint, string Label = "Value");

    // How far the chip is from the middle of its edge, away from the middle of the body on the screen, in pixels.
    public const float Offset = 24f;

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

        var values = new List<Value>();
        bool Side(DatumReference face) => face.FeatureId == body && face.Element == "side";
        if (distance is { } d)
        {
            var along = edges.Where(edge => Side(edge.First) && Side(edge.Second))
                .Select(edge => Middle(edge, camera, width, height))
                .OfType<Vector2>()
                .OrderByDescending(point => Vector2.DistanceSquared(point, centre))
                .Cast<Vector2?>()
                .FirstOrDefault();
            if (along is { } on)
                values.Add(new Value(DistanceKey(body), $"{Dimensions.Format(d)} {unit}", Away(on, centre), d, body, null, null, "Distance"));
        }

        var items = edges.SelectMany(edge => new[] { edge.First, edge.Second }).Where(Side).Select(face => face.ItemId).OfType<Guid>().ToHashSet();
        if (sketches.FirstOrDefault(sketch => sketch.Lines.Any(line => items.Contains(line.Id)) || sketch.Circles.Any(circle => items.Contains(circle.Id)) || sketch.Arcs.Any(arc => items.Contains(arc.Id))) is not { } of)
            return values;

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
            if (edge is not null && Middle(edge, camera, width, height) is { } at)
                values.Add(new Value(ConstraintKey(constraint.Id), $"{prefix}{Dimensions.Format(constraint.Values[0])} {unit}", Away(at, centre), constraint.Values[0], body, of.SketchId, constraint.Id, label));
        }

        return values;
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

    private static Vector2 Away(Vector2 at, Vector2 centre)
    {
        var outward = at - centre;
        var length = outward.Length();
        return length < 1e-3f ? at + new Vector2(0f, -Offset) : at + (outward / length * Offset);
    }
}
