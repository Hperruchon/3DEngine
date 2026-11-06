using System.Numerics;
using Engine.Core.Queries;
using ThreeDEngine.Core.Measuring;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Desktop;

// ADR-0075 item 3 (phase R12a, TASK-0236): the mark of the selected face, its outline in pixels of the window, and its
// line in the panel. The overlay is presentation (ADR-0007): it reads the entry of GetFaces and sends nothing.
internal static class FaceOverlay
{
    // Each edge of the outline that is in front of the eye, clipped to the window, in pixels. Finding V107 of the codebase
    // review of 2026-11-06 (d) (TASK-0243): the offset of a drag of the gizmo moves the outline with the body, as the
    // renderer moves the mesh; it is zero with no drag.
    public static IReadOnlyList<(Vector2 A, Vector2 B)> Of(IReadOnlyList<FaceSegment> outline, Camera camera, double width, double height,
        (double X, double Y, double Z) offset = default)
    {
        ArgumentNullException.ThrowIfNull(outline);
        ArgumentNullException.ThrowIfNull(camera);
        var (dx, dy, dz) = offset;
        var lines = new List<(Vector2 A, Vector2 B)>();
        foreach (var segment in outline)
        {
            if (SketchOverlay.Segment(camera, width, height, (segment.Start.X + dx, segment.Start.Y + dy, segment.Start.Z + dz), (segment.End.X + dx, segment.End.Y + dy, segment.End.Z + dz)) is { } line)
                lines.Add(line);
        }

        return lines;
    }

    // ADR-0077 item 3 (phase R12b, TASK-0245): the edge whose segments on the screen pass nearest the point, at most
    // within pixels away, or null.
    public static EdgeEntry? Nearest(IReadOnlyList<EdgeEntry> edges, Camera camera, double width, double height, double x, double y,
        double within = 12, (double X, double Y, double Z) offset = default)
    {
        ArgumentNullException.ThrowIfNull(edges);
        EdgeEntry? nearest = null;
        var best = within * within;
        var point = new Vector2((float)x, (float)y);
        foreach (var edge in edges)
        {
            foreach (var (a, b) in Of(edge.Segments, camera, width, height, offset))
            {
                var d = DistanceSquared(point, a, b);
                if (d <= best)
                    (best, nearest) = (d, edge);
            }
        }

        return nearest;
    }

    private static double DistanceSquared(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var length = Vector2.Dot(ab, ab);
        var t = length == 0 ? 0 : Math.Clamp(Vector2.Dot(p - a, ab) / length, 0, 1);
        var nearest = a + (ab * t);
        return Vector2.DistanceSquared(p, nearest);
    }

    // ADR-0077 item 4: the line of the edge in the panel: the elements of its two names and its length.
    public static string EdgeText(EdgeEntry edge, string unit)
    {
        ArgumentNullException.ThrowIfNull(edge);
        ArgumentNullException.ThrowIfNull(unit);
        return $"Edge {edge.First.Element} | {edge.Second.Element}: {Dimensions.Format(edge.Length)} {unit}";
    }

    // The line of the face in the panel: the element of its name and its area. The font of the panel has no sign of a
    // square, so the unit is in words.
    public static string Text(FaceEntry face, string unit)
    {
        ArgumentNullException.ThrowIfNull(face);
        ArgumentNullException.ThrowIfNull(unit);
        return $"Face {face.Reference.Element}: {Dimensions.Format(face.Area)} square {unit}";
    }
}
