using System.Numerics;
using Engine.Contracts.Sketch;
using Engine.Core.Queries;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Desktop;

// The lines and the points of the live sketches in pixels of the window, for the UI layer
// to draw over the scene (ADR-0033 item 13). A point of a sketch at (x, y) is at
// origin + x·u + y·v in the space of the Document (item 1). The projection is the inverse
// of Picker.RayThrough, so a sketch and a pick agree on each pixel. This is presentation:
// it uses the camera in double precision and runs outside the log (ADR-0007).
internal static class SketchOverlay
{
    // The part of a line in front of the camera, and each point in front of it, in pixels.
    public sealed record Strokes(IReadOnlyList<(Vector2 A, Vector2 B)> Lines, IReadOnlyList<Vector2> Points);

    // The distance in front of the eye below which the overlay cuts a line.
    private const double Near = 1e-3;

    public static Strokes Of(IEnumerable<SketchInfo> sketches, Camera camera, double width, double height)
    {
        ArgumentNullException.ThrowIfNull(sketches);
        ArgumentNullException.ThrowIfNull(camera);
        var lines = new List<(Vector2, Vector2)>();
        var points = new List<Vector2>();
        if (!(width > 0 && height > 0))
            return new Strokes(lines, points);

        var frame = Frame.Of(camera, width, height);
        foreach (var sketch in sketches)
        {
            if (Datums.Find(sketch.DatumId) is not { } datum)
                continue;

            var world = sketch.Points.ToDictionary(point => point.Id, point => frame.ToCamera(World(datum, point.X, point.Y)));
            foreach (var line in sketch.Lines)
            {
                if (world.TryGetValue(line.Start, out var a) && world.TryGetValue(line.End, out var b) && frame.Segment(a, b) is { } segment)
                    lines.Add(segment);
            }

            foreach (var point in world.Values)
            {
                if (point.Forward >= Near)
                    points.Add(frame.ToPixels(point));
            }
        }

        return new Strokes(lines, points);
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
            return (ToPixels(a), ToPixels(b));
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

    private static double Dot((double X, double Y, double Z) a, (double X, double Y, double Z) b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

    private static (double X, double Y, double Z) Cross((double X, double Y, double Z) a, (double X, double Y, double Z) b)
        => ((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z), (a.X * b.Y) - (a.Y * b.X));

    private static (double X, double Y, double Z) Normal((double X, double Y, double Z) v)
    {
        var length = Math.Sqrt(Dot(v, v));
        return (v.X / length, v.Y / length, v.Z / length);
    }
}
