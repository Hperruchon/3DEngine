using System.Numerics;
using Engine.Contracts.Sketch;
using Engine.Core.Queries;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Sketching;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.Desktop;

// The lines and the points of the live sketches in pixels of the window, for the UI layer
// to draw over the scene (ADR-0033 item 13). A point of a sketch at (x, y) is at
// origin + x·u + y·v in the space of the Document (item 1). The projection is the inverse
// of Picker.RayThrough, so a sketch and a pick agree on each pixel. This is presentation:
// it uses the camera in double precision and runs outside the log (ADR-0007). With a focus, the
// active sketch has its labels, its selected items and the ring of the first point of an open
// chain, and each other sketch is grey (ADR-0034 item 9).
internal static class SketchOverlay
{
    // How an item draws: an item of the active sketch, or of all sketches when no sketch is
    // active; a selected item; or an item of a sketch that is not active.
    public enum Mark
    {
        Active,
        Selected,
        Other,
    }

    public sealed record Stroke(Vector2 A, Vector2 B, Mark Mark);

    public sealed record Spot(Vector2 At, Mark Mark);

    public sealed record Label(Vector2 At, string Text);

    // The part of a line in front of the camera, and each point in front of it, in pixels; the
    // labels of the active sketch, and the ring of the first point of an open chain.
    public sealed record Strokes(IReadOnlyList<Stroke> Lines, IReadOnlyList<Spot> Points, IReadOnlyList<Label> Labels, IReadOnlyList<Vector2> Rings);

    // The active sketch, its selected items, and the first point of its open chain.
    public sealed record Focus(Guid Sketch, IReadOnlyCollection<Guid> Selected, Guid? ChainStart);

    // The distance in front of the eye below which the overlay cuts a line.
    private const double Near = 1e-3;

    public static Strokes Of(IEnumerable<SketchInfo> sketches, Camera camera, double width, double height, Focus? focus = null)
    {
        ArgumentNullException.ThrowIfNull(sketches);
        ArgumentNullException.ThrowIfNull(camera);
        var lines = new List<Stroke>();
        var points = new List<Spot>();
        var labels = new List<Label>();
        var rings = new List<Vector2>();
        if (!(width > 0 && height > 0))
            return new Strokes(lines, points, labels, rings);

        var frame = Frame.Of(camera, width, height);
        foreach (var sketch in sketches)
        {
            if (Datums.Find(sketch.DatumId) is not { } datum)
                continue;

            var active = focus is null || focus.Sketch == sketch.SketchId;
            Mark MarkOf(Guid item) => !active ? Mark.Other : focus?.Selected.Contains(item) == true ? Mark.Selected : Mark.Active;
            var named = focus is not null && active ? SketchText.Labels(sketch) : null;

            var world = sketch.Points.ToDictionary(point => point.Id, point => frame.ToCamera(World(datum, point.X, point.Y)));
            foreach (var line in sketch.Lines)
            {
                if (world.TryGetValue(line.Start, out var a) && world.TryGetValue(line.End, out var b) && frame.Segment(a, b) is { } segment)
                {
                    lines.Add(new Stroke(segment.A, segment.B, MarkOf(line.Id)));
                    if (named is not null)
                        labels.Add(new Label((segment.A + segment.B) / 2 + LabelOffset, named[line.Id]));
                }
            }

            foreach (var (id, point) in world)
            {
                if (point.Forward < Near)
                    continue;
                var pixel = frame.ToPixels(point);
                if (!Finite(pixel))
                    continue;
                points.Add(new Spot(pixel, MarkOf(id)));
                if (named is not null)
                    labels.Add(new Label(pixel + LabelOffset, named[id]));
                if (active && focus?.ChainStart == id)
                    rings.Add(pixel);
            }
        }

        return new Strokes(lines, points, labels, rings);
    }

    // The place of a label from its point or the middle of its line, in pixels.
    private static readonly Vector2 LabelOffset = new(8f, -30f);

    // ADR-0034 item 4: the active sketch as the tool sees it, with the pixel of each point in front
    // of the camera. With no camera, no point has a pixel; the tool then reads the positions only.
    public static SketchPicture Picture(SketchInfo sketch, Camera? camera = null, double width = 0, double height = 0)
    {
        ArgumentNullException.ThrowIfNull(sketch);
        var frame = camera is not null && width > 0 && height > 0 && Datums.Find(sketch.DatumId) is { } datum ? (Frame.Of(camera, width, height), datum) : default;
        var points = sketch.Points.Select(point =>
        {
            if (frame.Item1 is null)
                return new ToolPoint(point.Id, point.X, point.Y, null, null);
            var seen = frame.Item1.ToCamera(World(frame.datum, point.X, point.Y));
            if (seen.Forward < Near)
                return new ToolPoint(point.Id, point.X, point.Y, null, null);
            var pixel = frame.Item1.ToPixels(seen);
            return Finite(pixel) ? new ToolPoint(point.Id, point.X, point.Y, pixel.X, pixel.Y) : new ToolPoint(point.Id, point.X, point.Y, null, null);
        }).ToArray();
        return new SketchPicture(points, [.. sketch.Lines.Select(line => new ToolLine(line.Id, line.Start, line.End))], [.. sketch.Constraints.Select(constraint => constraint.Id)]);
    }

    private static (double X, double Y, double Z) World(Datum datum, double x, double y)
        => ((x * datum.U.X) + (y * datum.V.X), (x * datum.U.Y) + (y * datum.V.Y), (x * datum.U.Z) + (y * datum.V.Z));

    // A point in the frame of the camera: to the right, up, and forward from the eye.
    private readonly record struct CameraPoint(double Right, double Up, double Forward);

    private sealed record Frame(
        (double X, double Y, double Z) Eye,
        (double X, double Y, double Z) Right,
        (double X, double Y, double Z) Up,
        (double X, double Y, double Z) Forward,
        double TanHalf,
        double Aspect,
        double Width,
        double Height)
    {
        // The same axes as Picker.RayThrough.
        public static Frame Of(Camera camera, double width, double height)
        {
            var eye = camera.Eye();
            var forward = Normal((camera.Target.X - eye.X, camera.Target.Y - eye.Y, camera.Target.Z - eye.Z));
            var right = Normal(Cross(forward, (0, 1, 0)));
            var up = Cross(right, forward);
            return new Frame(eye, right, up, forward, Math.Tan(camera.FieldOfViewDegrees * Math.PI / 360.0), width / height, width, height);
        }

        public CameraPoint ToCamera((double X, double Y, double Z) p)
        {
            var d = (p.X - Eye.X, p.Y - Eye.Y, p.Z - Eye.Z);
            return new CameraPoint(Dot(d, Right), Dot(d, Up), Dot(d, Forward));
        }

        public Vector2 ToPixels(CameraPoint p)
        {
            var nx = p.Right / p.Forward / (TanHalf * Aspect);
            var ny = p.Up / p.Forward / TanHalf;
            return new Vector2((float)((nx + 1) / 2 * Width), (float)((1 - ny) / 2 * Height));
        }

        // The part of the segment in front of the near distance, or null.
        public (Vector2 A, Vector2 B)? Segment(CameraPoint a, CameraPoint b)
        {
            if (a.Forward < Near && b.Forward < Near)
                return null;
            if (a.Forward < Near)
                a = Cut(b, a);
            else if (b.Forward < Near)
                b = Cut(a, b);
            var (start, end) = (ToPixels(a), ToPixels(b));
            return Finite(start) && Finite(end) ? (start, end) : null;
        }

        // The point between front and behind at the near distance.
        private static CameraPoint Cut(CameraPoint front, CameraPoint behind)
        {
            var t = (front.Forward - Near) / (front.Forward - behind.Forward);
            return new CameraPoint(
                front.Right + (t * (behind.Right - front.Right)),
                front.Up + (t * (behind.Up - front.Up)),
                Near);
        }
    }

    // Finding V45 of the codebase review of 2026-10-29: a finite position far from the camera, as
    // 1e308, gives a pixel that is not finite as a float, and its vertices went to the GPU. Such a
    // point or segment is not drawn and has no pixel for the tool.
    // Finding V49 of the codebase review of 2026-10-30 (TASK-0118): a finite pixel of about 1e38
    // made vertices that are not finite in the line code of the UI library, so a pixel farther
    // than this limit from the window is dropped too.
    public const float PixelLimit = 1e7f;

    private static bool Finite(Vector2 pixel)
        => float.IsFinite(pixel.X) && float.IsFinite(pixel.Y) && Math.Abs(pixel.X) <= PixelLimit && Math.Abs(pixel.Y) <= PixelLimit;

    private static double Dot((double X, double Y, double Z) a, (double X, double Y, double Z) b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

    private static (double X, double Y, double Z) Cross((double X, double Y, double Z) a, (double X, double Y, double Z) b)
        => ((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z), (a.X * b.Y) - (a.Y * b.X));

    private static (double X, double Y, double Z) Normal((double X, double Y, double Z) v)
    {
        var length = Math.Sqrt(Dot(v, v));
        return (v.X / length, v.Y / length, v.Z / length);
    }
}
