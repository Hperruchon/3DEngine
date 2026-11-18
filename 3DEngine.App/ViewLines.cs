using System.Numerics;
using Engine.Core.Queries;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Desktop;

// ADR-0166 items 2 and 3 (phase R30a, TASK-0406): the lines of the sheet in a flat view of the window. The window asks
// GetViewLines for the exact frame of the flat view, and draws each line in pixels of the window over a light paper. The lines
// are presentation (ADR-0007): the window reads them, and sends nothing.
internal static class ViewLines
{
    // The count of the straight parts of a whole circle or ellipse. A circle of 200 pixels of radius is within 0.25 pixel.
    internal const int WholeCurveParts = 96;

    // A line on the screen: its points in pixels of the window, in order, true when it is visible, and true for a centre line
    // (finding E220 of the codebase review of 2026-11-18, TASK-0410).
    public sealed record Stroke(IReadOnlyList<Vector2> Points, bool Visible, bool Centre = false);

    // Finding V198 of the codebase review of 2026-11-18 (TASK-0410): the paper shows when the switch "Lines" is on in a flat
    // view. The gizmo does not show then, because the paper covers the body and its handles.
    public static bool PaperShows(bool linesOn, Camera camera)
    {
        ArgumentNullException.ThrowIfNull(camera);
        return linesOn && FrameOf(camera) is not null;
    }

    // Finding V196 of the codebase review of 2026-11-18 (TASK-0410): the lines come from the bodies, so an event of a sheet does
    // not start a new read. Each other event does. Until TASK-0410 each new view of a sheet started a new read.
    public static bool ChangesLines(string kind)
    {
        ArgumentNullException.ThrowIfNull(kind);
        return !kind.StartsWith("sheet.", StringComparison.Ordinal);
    }

    // Finding V201 of the codebase review of 2026-11-18 (TASK-0410): the text of each body that the view skips, with its name and
    // the reason of the engine, as the sheet gives it. Until TASK-0410 such a body went under the paper with no text.
    public static IReadOnlyList<string> Notes(ViewLinesInfo lines, IReadOnlyList<LiveBody> live)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(live);
        return [.. lines.Skipped.Select(skipped =>
            $"{live.FirstOrDefault(body => body.Id == skipped.Body)?.Name ?? "A body"} gives no lines: {skipped.Reason}")];
    }

    // ADR-0166 item 2: the exact frame of the flat view of the camera, nine whole numbers: the right, the up and the direction
    // toward the eye, from the axes of the camera. A flat view has the axes of the world, so each number is -1, 0 or 1. Null
    // with no flat view.
    public static double[]? FrameOf(Camera camera)
    {
        ArgumentNullException.ThrowIfNull(camera);
        if (camera.Flat is null)
            return null;
        var (right, up, forward) = camera.Axes();
        static double Whole(double value) => Math.Round(value) + 0.0;
        return
        [
            Whole(right.X), Whole(right.Y), Whole(right.Z),
            Whole(up.X), Whole(up.Y), Whole(up.Z),
            Whole(-forward.X), Whole(-forward.Y), Whole(-forward.Z),
        ];
    }

    // The query of the lines of the frame.
    public static GetViewLinesQuery Query(IReadOnlyList<double> d)
    {
        ArgumentNullException.ThrowIfNull(d);
        return new GetViewLinesQuery { RightX = d[0], RightY = d[1], RightZ = d[2], UpX = d[3], UpY = d[4], UpZ = d[5], EyeX = d[6], EyeY = d[7], EyeZ = d[8] };
    }

    // ADR-0166 item 2: each line of the read in pixels of the window. A point (u, v) of the view is the point u R + v U + d E of
    // the world, where R, U and E are the unit right, up and direction toward the eye of the frame, and d puts it at the depth
    // of the target, so the camera projects it as each point of the part. Lines of another frame give no stroke.
    public static IReadOnlyList<Stroke> Of(ViewLinesInfo lines, Camera camera, double width, double height)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(camera);
        if (FrameOf(camera) is not { } frame || !frame.SequenceEqual(lines.Direction))
            return [];
        var (r, u, e) = ((frame[0], frame[1], frame[2]), (frame[3], frame[4], frame[5]), (frame[6], frame[7], frame[8]));
        var target = camera.Target;
        var depth = (target.X * e.Item1) + (target.Y * e.Item2) + (target.Z * e.Item3);
        (double X, double Y, double Z) World((double U, double V) p)
            => ((p.U * r.Item1) + (p.V * u.Item1) + (depth * e.Item1), (p.U * r.Item2) + (p.V * u.Item2) + (depth * e.Item2), (p.U * r.Item3) + (p.V * u.Item3) + (depth * e.Item3));

        var strokes = new List<Stroke>(lines.Lines.Count);
        foreach (var line in lines.Lines)
        {
            var path = Path(line);
            var points = new List<Vector2>(path.Count);
            for (var i = 0; i + 1 < path.Count; i++)
            {
                if (SketchOverlay.Segment(camera, width, height, World(path[i]), World(path[i + 1])) is not { } segment)
                    continue;
                if (points.Count == 0)
                    points.Add(segment.A);
                points.Add(segment.B);
            }

            if (points.Count >= 2)
                strokes.Add(new Stroke(points, line.Visible, line.Kind == "centre"));
        }

        return strokes;
    }

    // The points of a line on the view: its two ends, or its arc of a circle or an ellipse from the start to the end,
    // counter-clockwise on the view, in straight parts; a start equal to the end is the whole curve.
    internal static IReadOnlyList<(double U, double V)> Path(ViewLineInfo line)
    {
        ArgumentNullException.ThrowIfNull(line);
        (double X, double Y, (double X, double Y) Major, (double X, double Y) Minor)? curve = line switch
        {
            { Circle: { } c } => (c.X, c.Y, (c.Radius, 0.0), (0.0, c.Radius)),
            { Ellipse: { } e } => (e.X, e.Y, (e.MajorX, e.MajorY), (e.MinorX, e.MinorY)),
            _ => null,
        };
        if (curve is not { } k)
            return [(line.FromX, line.FromY), (line.ToX, line.ToY)];

        // A point c + cos t M + sin t N; with M x N < 0 a growing t turns clockwise, so the arc goes the other way.
        var (m, n) = (k.Major, k.Minor);
        var turn = (m.X * n.Y) - (m.Y * n.X) >= 0 ? 1.0 : -1.0;
        double Angle(double x, double y)
        {
            var (dx, dy) = (x - k.X, y - k.Y);
            return Math.Atan2(((dx * n.X) + (dy * n.Y)) / ((n.X * n.X) + (n.Y * n.Y)), ((dx * m.X) + (dy * m.Y)) / ((m.X * m.X) + (m.Y * m.Y)));
        }

        var start = Angle(line.FromX, line.FromY);
        var sweep = 2 * Math.PI;
        if ((line.FromX, line.FromY) != (line.ToX, line.ToY))
        {
            sweep = turn * (Angle(line.ToX, line.ToY) - start);
            sweep -= 2 * Math.PI * Math.Floor(sweep / (2 * Math.PI));
            if (sweep == 0)
                sweep = 2 * Math.PI;
        }

        var parts = Math.Max(4, (int)Math.Ceiling(WholeCurveParts * sweep / (2 * Math.PI)));
        var points = new List<(double U, double V)>(parts + 1) { (line.FromX, line.FromY) };
        for (var i = 1; i < parts; i++)
        {
            var t = start + (turn * sweep * i / parts);
            points.Add((k.X + (Math.Cos(t) * m.X) + (Math.Sin(t) * n.X), k.Y + (Math.Cos(t) * m.Y) + (Math.Sin(t) * n.Y)));
        }

        points.Add((line.ToX, line.ToY));
        return points;
    }

    // ADR-0166 item 3: the dashes of a hidden line: on pixels drawn and off pixels free, along the whole path, so a dash goes
    // on across a corner of the path.
    public static IReadOnlyList<(Vector2 A, Vector2 B)> Dashes(IReadOnlyList<Vector2> points, float on, float off)
        => Dashes(points, [on, off]);

    // The dashes of a pattern of lengths in pixels, drawn and free in turn from the first, along the whole path. A centre line
    // has a long dash, a gap, a short dash and a gap (finding E220, TASK-0410).
    public static IReadOnlyList<(Vector2 A, Vector2 B)> Dashes(IReadOnlyList<Vector2> points, IReadOnlyList<float> pattern)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(pattern);
        if (pattern.Count == 0 || pattern.Count % 2 != 0 || pattern.Any(length => !(length > 0f)))
            throw new ArgumentException("A pattern of dashes has pairs of a drawn and a free length, each greater than zero.", nameof(pattern));
        var dashes = new List<(Vector2 A, Vector2 B)>();
        var (part, left) = (0, pattern[0]);
        var drawing = true;
        for (var i = 0; i + 1 < points.Count; i++)
        {
            var (a, b) = (points[i], points[i + 1]);
            var length = Vector2.Distance(a, b);
            var at = 0f;
            while (length - at > 0f)
            {
                var step = Math.Min(left, length - at);
                if (drawing && step > 0f)
                    dashes.Add((Vector2.Lerp(a, b, at / length), Vector2.Lerp(a, b, (at + step) / length)));
                at += step;
                left -= step;
                if (left <= 0f)
                {
                    part = (part + 1) % pattern.Count;
                    (drawing, left) = (part % 2 == 0, pattern[part]);
                }
            }
        }

        return dashes;
    }
}

// ADR-0166 item 2: the lines of the flat view, read off the thread of the frame. A read starts for a new frame or a new version
// of the bodies (ChangesLines; finding V196, TASK-0410), one read at a time; until it ends, the window keeps the last lines. GetViewLines waits for the serial lock
// of the session, and a body with many faces takes long (R-0069), so the frame does not wait for it.
internal sealed class ViewLinesCache
{
    private string? _asked;
    private Task<ViewLinesInfo?>? _reading;
    private ViewLinesInfo? _last;

    // The number of reads that started, for a test.
    public int Computed { get; private set; }

    // The last lines that a read gave, or null, and true while a read for this version and frame has not ended.
    public (ViewLinesInfo? Lines, bool Pending) Lines(long version, IReadOnlyList<double> direction, Func<Task<ViewLinesInfo?>> read)
    {
        ArgumentNullException.ThrowIfNull(direction);
        ArgumentNullException.ThrowIfNull(read);
        if (_reading is { IsCompleted: true } done)
        {
            // A failed read, for example of a session that closes, keeps the last lines.
            if (done.IsCompletedSuccessfully && done.Result is { } result)
                _last = result;
            _ = done.Exception;
            _reading = null;
        }

        var key = $"{version}:{string.Join(",", direction)}";
        if (_asked != key && _reading is null)
        {
            _asked = key;
            _reading = read();
            Computed++;
        }

        return (_last, _asked != key || _reading is not null);
    }
}
