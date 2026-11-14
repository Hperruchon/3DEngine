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
// chain, and each other sketch is grey (ADR-0034 item 9). ADR-0053 item 7 (phase K0i, TASK-0171): a sketch draws on
// the frame of its datum, an origin plane or a datum of the Document, and item 8: the lines and the points of a
// reference of the sketch draw dim.
internal static class SketchOverlay
{
    // How an item draws: an item of the active sketch, or of all sketches when no sketch is
    // active; a selected item; or an item of a sketch that is not active.
    public enum Mark
    {
        Active,
        Selected,
        Other,
        Reference,
    }

    public sealed record Stroke(Vector2 A, Vector2 B, Mark Mark);

    public sealed record Spot(Vector2 At, Mark Mark);

    public sealed record Label(Vector2 At, string Text);

    // The part of a line in front of the camera, and each point in front of it, in pixels; the
    // labels of the active sketch, and the ring of the first point of an open chain.
    // Phase R11b (TASK-0134): Snap is the pixel of the point that a tap under the mouse would take.
    // ADR-0120 item 3 (phase R18f, TASK-0347): Plate is the four corners of the plate of the active sketch on the screen, or
    // null; Under is true while the sketch is under the body that it makes.
    public sealed record Strokes(IReadOnlyList<Stroke> Lines, IReadOnlyList<Spot> Points, IReadOnlyList<Label> Labels, IReadOnlyList<Vector2> Rings, Vector2? Snap = null,
        IReadOnlyList<Vector2>? Plate = null, bool Under = false);

    // ADR-0120 item 3: the margin of the plate around the points of the sketch, as a part of the larger side of their box, and
    // the smallest margin in the units of the sketch.
    internal const double PlateMargin = 0.12;
    internal const double PlateMinimum = 2;

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
        IReadOnlyList<Vector2>? plate = null;
        foreach (var sketch in sketches)
        {
            if (PlaneOf(sketch) is not { } datum)
                continue;

            // ADR-0120 item 3 (phase R18f, TASK-0347): the plate of the active sketch: the box of its points in its plane, with
            // no origin, and a margin; none when a corner is behind the eye or the sketch has no point.
            if (focus?.Sketch == sketch.SketchId && sketch.Points.Where(point => point.Id != sketch.SketchId).ToList() is { Count: > 0 } drawn)
            {
                var (minX, maxX, minY, maxY) = (drawn.Min(point => point.X), drawn.Max(point => point.X), drawn.Min(point => point.Y), drawn.Max(point => point.Y));
                var margin = Math.Max(PlateMinimum, PlateMargin * Math.Max(maxX - minX, maxY - minY));
                var corners = new[] { (minX - margin, minY - margin), (maxX + margin, minY - margin), (maxX + margin, maxY + margin), (minX - margin, maxY + margin) }
                    .Select(corner => frame.ToCamera(World(datum, corner.Item1, corner.Item2))).ToList();
                if (corners.All(corner => corner.Forward >= Near) && corners.Select(frame.ToPixels).ToList() is { } pixels && pixels.All(Finite))
                    plate = pixels;
            }

            var active = focus is null || focus.Sketch == sketch.SketchId;
            var driven = Driven(sketch);
            Mark MarkOf(Guid item) => !active ? Mark.Other : focus?.Selected.Contains(item) == true ? Mark.Selected : driven.Contains(item) ? Mark.Reference : Mark.Active;
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

            foreach (var curve in Curves(sketch))
            {
                var first = true;
                foreach (var (a, b) in Pairs(curve.Polyline))
                {
                    if (frame.Segment(frame.ToCamera(World(datum, a.X, a.Y)), frame.ToCamera(World(datum, b.X, b.Y))) is not { } segment)
                        continue;
                    lines.Add(new Stroke(segment.A, segment.B, MarkOf(curve.Id)));
                    if (named is not null && first)
                        labels.Add(new Label(segment.A + LabelOffset, named[curve.Id]));
                    first = false;
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

        return new Strokes(lines, points, labels, rings, Plate: plate);
    }

    // The place of a label from its point or the middle of its line, in pixels.
    private static readonly Vector2 LabelOffset = new(8f, -30f);

    // ADR-0034 item 4: the active sketch as the tool sees it, with the pixel of each point in front
    // of the camera. With no camera, no point has a pixel; the tool then reads the positions only.
    public static SketchPicture Picture(SketchInfo sketch, Camera? camera = null, double width = 0, double height = 0)
    {
        ArgumentNullException.ThrowIfNull(sketch);
        var frame = camera is not null && width > 0 && height > 0 && PlaneOf(sketch) is { } datum ? (Frame.Of(camera, width, height), datum) : default;
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
        var geometry = Geometry(sketch);
        var curves = Curves(sketch).Select(curve => new ToolCurve(curve.Id, frame.Item1 is null ? [] :
        [
            .. Pairs(curve.Polyline)
                .Select(pair => frame.Item1.Segment(frame.Item1.ToCamera(World(frame.datum, pair.A.X, pair.A.Y)), frame.Item1.ToCamera(World(frame.datum, pair.B.X, pair.B.Y))))
                .OfType<(Vector2 A, Vector2 B)>()
                .Select(segment => (((double)segment.A.X, (double)segment.A.Y), ((double)segment.B.X, (double)segment.B.Y))),
        ], geometry[curve.Id].Centre, geometry[curve.Id].Radius, geometry[curve.Id].Start, geometry[curve.Id].End)).ToArray();
        return new SketchPicture(points, [.. sketch.Lines.Select(line => new ToolLine(line.Id, line.Start, line.End))], [.. sketch.Constraints.Select(constraint => constraint.Id)], curves)
        {
            // Phase R11c (TASK-0165): the pixel of an intersection, for its snap.
            ToPixel = frame.Item1 is null ? null : position => Pixel(sketch, camera!, width, height, position) is { } pixel ? (pixel.X, pixel.Y) : null,
        };
    }

    // ADR-0041 (phase R11a): the centre and the radius of each curve in the plane, for a snap; the
    // radius of an arc is the distance of its start from its centre. Phase R11c (TASK-0165): an arc
    // also gives its start and its end, for the sweep of an intersection.
    private static Dictionary<Guid, ((double X, double Y) Centre, double Radius, (double X, double Y)? Start, (double X, double Y)? End)> Geometry(SketchInfo sketch)
    {
        var points = sketch.Points.ToDictionary(point => point.Id);
        var geometry = new Dictionary<Guid, ((double X, double Y) Centre, double Radius, (double X, double Y)? Start, (double X, double Y)? End)>();
        foreach (var circle in sketch.Circles.Where(circle => points.ContainsKey(circle.Center)))
            geometry[circle.Id] = ((points[circle.Center].X, points[circle.Center].Y), circle.Radius, null, null);
        foreach (var arc in sketch.Arcs.Where(arc => points.ContainsKey(arc.Center) && points.ContainsKey(arc.Start)))
        {
            var (c, s) = (points[arc.Center], points[arc.Start]);
            (double X, double Y)? e = points.TryGetValue(arc.End, out var end) ? (end.X, end.Y) : null;
            geometry[arc.Id] = ((c.X, c.Y), Math.Sqrt(((s.X - c.X) * (s.X - c.X)) + ((s.Y - c.Y) * (s.Y - c.Y))), (s.X, s.Y), e);
        }
        return geometry;
    }

    // Phase K2e (TASK-0122): the segments of a full turn of a circle. An arc has the part of them
    // that its sweep covers, at least one.
    public const int TurnSegments = 64;

    // The polyline of each circle and each arc of a sketch in the plane, counter-clockwise from the
    // start of an arc (ADR-0033 item 2). The polyline is presentation (ADR-0007): it stays outside
    // the log, so it uses a sine, a cosine and an arc tangent. An arc whose end is not on the circle
    // of its start, as in a sketch that is not solved, ends in the direction of its end. A curve of
    // radius 0 has no polyline.
    public static IEnumerable<(Guid Id, IReadOnlyList<(double X, double Y)> Polyline)> Curves(SketchInfo sketch)
    {
        ArgumentNullException.ThrowIfNull(sketch);
        var points = sketch.Points.ToDictionary(point => point.Id);
        foreach (var circle in sketch.Circles)
        {
            if (points.TryGetValue(circle.Center, out var c))
                yield return (circle.Id, Polyline(c.X, c.Y, circle.Radius, 0, 2 * Math.PI));
        }

        foreach (var arc in sketch.Arcs)
        {
            if (!points.TryGetValue(arc.Center, out var c) || !points.TryGetValue(arc.Start, out var s) || !points.TryGetValue(arc.End, out var e))
                continue;
            var radius = Math.Sqrt(((s.X - c.X) * (s.X - c.X)) + ((s.Y - c.Y) * (s.Y - c.Y)));
            var from = Math.Atan2(s.Y - c.Y, s.X - c.X);
            var sweep = Math.Atan2(e.Y - c.Y, e.X - c.X) - from;
            while (sweep <= 0)
                sweep += 2 * Math.PI;
            yield return (arc.Id, Polyline(c.X, c.Y, radius, from, sweep));
        }
    }

    private static IReadOnlyList<(double X, double Y)> Polyline(double cx, double cy, double radius, double from, double sweep)
    {
        if (!(radius > 0) || !double.IsFinite(radius) || !double.IsFinite(sweep))
            return [];
        var count = Math.Max(1, (int)Math.Ceiling(TurnSegments * sweep / (2 * Math.PI)));
        return [.. Enumerable.Range(0, count + 1).Select(i => from + (sweep * i / count)).Select(angle => (cx + (radius * Math.Cos(angle)), cy + (radius * Math.Sin(angle))))];
    }

    private static IEnumerable<((double X, double Y) A, (double X, double Y) B)> Pairs(IReadOnlyList<(double X, double Y)> polyline)
    {
        for (var i = 1; i < polyline.Count; i++)
            yield return (polyline[i - 1], polyline[i]);
    }

    // The pixel of a position of the plane of a sketch, or null when it is behind the eye or not
    // finite (ADR-0041 item 6).
    public static Vector2? Pixel(SketchInfo sketch, Camera camera, double width, double height, (double X, double Y) position)
    {
        ArgumentNullException.ThrowIfNull(sketch);
        return Pixel(PlaneOf(sketch), camera, width, height, position);
    }

    // The pixel of a position of an origin plane, or null; ADR-0047 item 3 draws a proposal with it. An image stays on
    // an origin plane (ADR-0053 item 10).
    public static Vector2? Pixel(Guid datumId, Camera camera, double width, double height, (double X, double Y) position)
        => Pixel(OriginPlane(datumId), camera, width, height, position);

    // ADR-0056 item 4 (phase R13): the pixels of a segment of the space of the Document, clipped as each line of a sketch
    // is, or null when no part of it is in front of the eye.
    public static (Vector2 A, Vector2 B)? Segment(Camera camera, double width, double height, (double X, double Y, double Z) a, (double X, double Y, double Z) b)
    {
        ArgumentNullException.ThrowIfNull(camera);
        if (!(width > 0 && height > 0))
            return null;
        var frame = Frame.Of(camera, width, height);
        return frame.Segment(frame.ToCamera(a), frame.ToCamera(b));
    }

    private static Vector2? Pixel(DatumFrame? plane, Camera camera, double width, double height, (double X, double Y) position)
    {
        ArgumentNullException.ThrowIfNull(camera);
        if (!(width > 0 && height > 0) || plane is not { } datum)
            return null;
        var frame = Frame.Of(camera, width, height);
        var seen = frame.ToCamera(World(datum, position.X, position.Y));
        if (seen.Forward < Near)
            return null;
        var pixel = frame.ToPixels(seen);
        return Finite(pixel) ? pixel : null;
    }

    // ADR-0053 item 7: the frame of the datum of a sketch, as GetSketch gives it, or the origin plane of its datum when
    // the record holds no frame.
    public static DatumFrame? PlaneOf(SketchInfo sketch)
    {
        ArgumentNullException.ThrowIfNull(sketch);
        return sketch.Frame ?? OriginPlane(sketch.DatumId);
    }

    private static DatumFrame? OriginPlane(Guid datumId)
        => Datums.Find(datumId) is { } datum ? new DatumFrame(new DatumPoint(0, 0, 0), datum.U, datum.V, datum.Normal) : null;

    // Item 8: the points and the lines that the references of the sketch drive.
    private static HashSet<Guid> Driven(SketchInfo sketch)
        => [.. sketch.References.SelectMany(reference => reference.Line is { } line ? reference.Points.Append(line) : reference.Points)];

    // A position (x, y) of the plane is the point O + x U + y V.
    private static (double X, double Y, double Z) World(DatumFrame datum, double x, double y)
        => (datum.Origin.X + (x * datum.U.X) + (y * datum.V.X), datum.Origin.Y + (x * datum.U.Y) + (y * datum.V.Y), datum.Origin.Z + (x * datum.U.Z) + (y * datum.V.Z));

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
        double Height,
        double? FlatDepth)
    {
        // The same axes as Picker.RayThrough. ADR-0137 item 3 (phase R21a, TASK-0369): the axes of the camera, and for a flat
        // view the distance of the target as the depth of each point, so a point projects with no perspective.
        public static Frame Of(Camera camera, double width, double height)
        {
            var (right, up, forward) = camera.Axes();
            return new Frame(camera.Eye(), right, up, forward, Math.Tan(camera.FieldOfViewDegrees * Math.PI / 360.0), width / height, width, height,
                camera.Flat is null ? null : camera.Distance);
        }

        public CameraPoint ToCamera((double X, double Y, double Z) p)
        {
            var d = (p.X - Eye.X, p.Y - Eye.Y, p.Z - Eye.Z);
            return new CameraPoint(Dot(d, Right), Dot(d, Up), Dot(d, Forward));
        }

        public Vector2 ToPixels(CameraPoint p)
        {
            var (x, y) = Pixels(p);
            return new Vector2((float)x, (float)y);
        }

        private (double X, double Y) Pixels(CameraPoint p)
        {
            var depth = FlatDepth ?? p.Forward;
            var nx = p.Right / depth / (TanHalf * Aspect);
            var ny = p.Up / depth / TanHalf;
            return ((nx + 1) / 2 * Width, (1 - ny) / 2 * Height);
        }

        // The part of the segment in front of the near distance and inside the window with a margin
        // of one window on each side, or null. Finding V52 of the codebase review of 2026-10-30 (b)
        // (TASK-0124): the pixel of the cut at the near distance was farther than PixelLimit, and the
        // limit of V49 dropped a line whose far part was in the window. The clip in double
        // precision keeps each visible part of the line.
        public (Vector2 A, Vector2 B)? Segment(CameraPoint a, CameraPoint b)
        {
            if (a.Forward < Near && b.Forward < Near)
                return null;
            if (a.Forward < Near)
                a = Cut(b, a);
            else if (b.Forward < Near)
                b = Cut(a, b);
            if (Clip(Pixels(a), Pixels(b), (-Width, -Height), (2 * Width, 2 * Height)) is not { } clipped)
                return null;
            var (start, end) = (new Vector2((float)clipped.A.X, (float)clipped.A.Y), new Vector2((float)clipped.B.X, (float)clipped.B.Y));
            return Finite(start) && Finite(end) ? (start, end) : null;
        }

        // The part of the segment from a to b inside the rectangle from low to high (Liang and
        // Barsky), or null when no part is inside or a pixel is not finite.
        private static ((double X, double Y) A, (double X, double Y) B)? Clip((double X, double Y) a, (double X, double Y) b, (double X, double Y) low, (double X, double Y) high)
        {
            if (!double.IsFinite(a.X) || !double.IsFinite(a.Y) || !double.IsFinite(b.X) || !double.IsFinite(b.Y))
                return null;
            var (dx, dy) = (b.X - a.X, b.Y - a.Y);
            var (enter, leave) = (0.0, 1.0);
            foreach (var (p, q) in new[] { (-dx, a.X - low.X), (dx, high.X - a.X), (-dy, a.Y - low.Y), (dy, high.Y - a.Y) })
            {
                if (p == 0)
                {
                    if (q < 0)
                        return null;
                    continue;
                }
                var r = q / p;
                if (p < 0)
                    enter = Math.Max(enter, r);
                else
                    leave = Math.Min(leave, r);
                if (enter > leave)
                    return null;
            }
            return ((a.X + (enter * dx), a.Y + (enter * dy)), (a.X + (leave * dx), a.Y + (leave * dy)));
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
