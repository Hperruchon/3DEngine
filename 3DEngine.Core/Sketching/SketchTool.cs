namespace ThreeDEngine.Core.Sketching;

// A point of the active sketch as the tool sees it: its position in the plane, and its pixel in
// the window, or null when it is behind the eye.
public sealed record ToolPoint(Guid Id, double X, double Y, double? PixelX, double? PixelY);

// A line of the active sketch: its two points.
public sealed record ToolLine(Guid Id, Guid Start, Guid End);

// A circle or an arc of the active sketch (phase K2e): the segments in pixels of the part of its
// polyline in front of the eye, for a pick, and its centre and radius in the plane, for a snap
// (phase R11a, ADR-0041).
public sealed record ToolCurve(Guid Id, IReadOnlyList<((double X, double Y) A, (double X, double Y) B)> Pixels, (double X, double Y) Centre = default, double Radius = 0);

// The active sketch at one frame: each point, each line, each curve and the identifier of each
// constraint, in the order of the record.
public sealed record SketchPicture(IReadOnlyList<ToolPoint> Points, IReadOnlyList<ToolLine> Lines, IReadOnlyCollection<Guid> Constraints, IReadOnlyList<ToolCurve> Curves)
{
    public SketchPicture(IReadOnlyList<ToolPoint> points, IReadOnlyList<ToolLine> lines, IReadOnlyCollection<Guid> constraints)
        : this(points, lines, constraints, [])
    {
    }

    public static SketchPicture Empty { get; } = new([], [], []);
}

// What the tool asks of the session. Each call is one command of the session (ADR-0034 item 4);
// it gives the identifier of the new item, or null when the session refused the command.
public interface ISketchEditor
{
    Guid? AddPoint(double x, double y);

    Guid? AddLine(Guid start, Guid end);

    Guid? AddCircle(Guid centre, double radius);

    Guid? AddArc(Guid centre, Guid start, Guid end);
}

public enum SketchToolKind
{
    Line,
    Select,
    Circle,
    Arc,
}

// The tools of the sketch mode (ADR-0034 item 4, Q2 A and Q6 A): a pure state machine. It reads no
// clock, no device and no session of its own: the host gives it the picture of the active sketch
// and an editor, so a test runs it with no window.
//
// The tool Line makes a chain. A tap within HitPixels of a point uses that point; each other tap
// adds a point. Each point after the first adds a line from the last point. A tap on the first
// point closes the chain, and a tap on the last point ends it. The tool Select toggles the nearest
// point, or else the nearest line, in a selection of three items at most. Phase K2g (TASK-0127): the
// limit was two, and symmetric takes two points and a line.
//
// Phase K2e (TASK-0122): the tool Circle takes the centre, then a point on the circle, and adds a
// circle whose radius is the distance in the plane. The tool Arc takes the centre, the start and
// the end, and adds an arc counter-clockwise from the start (ADR-0033 item 2). A new end goes on
// the circle of the start, so the implicit equation of the arc holds at the add. A tap on a point
// uses that point, as for a line. The tool Select also picks the nearest curve.
public sealed class SketchTool
{
    // Half the height of a control (ADR-0027 item 6), so that a finger hits a point.
    public const double HitPixels = 22;

    public const int MaximumSelection = 3;

    private readonly List<Guid> _selection = new();

    // The points of the curve that the tool Circle or Arc makes, with their positions.
    private readonly List<(Guid Id, double X, double Y)> _curve = new();
    private SketchToolKind _kind;
    private int _chainLines;

    // A change of the tool ends the chain.
    public SketchToolKind Kind
    {
        get => _kind;
        set
        {
            _kind = value;
            End();
        }
    }

    // The first and the last point of the open chain, or null when no chain is open.
    public Guid? First { get; private set; }

    public Guid? Last { get; private set; }

    public IReadOnlyList<Guid> Selection => _selection;

    // A sketch point intent at a pixel of the window, with the position of the tap on the plane
    // (rounded by the host, ADR-0034 item 3), or null when the ray meets no plane.
    public void Tap(double pixelX, double pixelY, (double X, double Y)? position, SketchPicture picture, ISketchEditor editor)
    {
        ArgumentNullException.ThrowIfNull(picture);
        ArgumentNullException.ThrowIfNull(editor);
        if (_kind == SketchToolKind.Select)
        {
            if ((NearestPoint(pixelX, pixelY, picture) ?? NearestEdge(pixelX, pixelY, picture)) is { } item)
                Toggle(item);
            return;
        }

        // ADR-0041 (phase R11a): a tap with no point within HitPixels takes a snap.
        var hit = NearestPoint(pixelX, pixelY, picture);
        var at = hit is null ? Snap(pixelX, pixelY, position, picture) : position;
        if (_kind is SketchToolKind.Circle or SketchToolKind.Arc)
            PlaceCurve(hit, at, (pixelX, pixelY), picture, editor);
        else
            Place(hit, at, editor);
    }

    // ADR-0041 item 6 (phase R11b): the position that a tap at the pixel would take: a point within
    // HitPixels, else the snap. Finding V66 of the codebase review of 2026-10-31 (c) (TASK-0151): the
    // end of an arc goes on the circle of its start, so the mark was not where the point went. For the
    // end of an arc the target is on the circle, as PlaceCurve puts it, and null where the curve ends.
    public (double X, double Y)? Target(double pixelX, double pixelY, (double X, double Y)? position, SketchPicture picture)
    {
        ArgumentNullException.ThrowIfNull(picture);
        var hit = NearestPoint(pixelX, pixelY, picture);
        var at = hit is { } id && picture.Points.First(point => point.Id == id) is { } point
            ? (point.X, point.Y)
            : Snap(pixelX, pixelY, position, picture);
        if (hit is not null || _kind != SketchToolKind.Arc || _curve.Count != 2 || at is not { } p)
            return at;

        Refresh(picture);
        var c = _curve[0];
        var distance = Math.Sqrt(((p.X - c.X) * (p.X - c.X)) + ((p.Y - c.Y) * (p.Y - c.Y)));
        if (!(distance > 0))
            return null;
        var (end, closes) = ArcEnd(p, (pixelX, pixelY), picture, distance);
        return closes ? null : end;
    }

    // ADR-0041: the snap of a tap at a pixel, in this order, each within HitPixels: the midpoint of
    // a line; else the nearest point on a line or a curve; else the position of the grid that the
    // host gives. A snap is a position only: it adds no constraint.
    public static (double X, double Y)? Snap(double pixelX, double pixelY, (double X, double Y)? position, SketchPicture picture)
    {
        ArgumentNullException.ThrowIfNull(picture);
        var points = picture.Points
            .Where(point => point.PixelX is not null && point.PixelY is not null)
            .ToDictionary(point => point.Id);

        (double X, double Y)? midpoint = null;
        var best = HitPixels;
        foreach (var line in picture.Lines)
        {
            if (!points.TryGetValue(line.Start, out var a) || !points.TryGetValue(line.End, out var b))
                continue;
            var (mx, my) = ((a.PixelX!.Value + b.PixelX!.Value) / 2, (a.PixelY!.Value + b.PixelY!.Value) / 2);
            var distance = Math.Sqrt(((mx - pixelX) * (mx - pixelX)) + ((my - pixelY) * (my - pixelY)));
            if (distance <= best)
                (midpoint, best) = (((a.X + b.X) / 2, (a.Y + b.Y) / 2), distance);
        }
        if (midpoint is not null)
            return midpoint;

        (double X, double Y)? on = null;
        best = HitPixels;
        foreach (var line in picture.Lines)
        {
            if (!points.TryGetValue(line.Start, out var a) || !points.TryGetValue(line.End, out var b))
                continue;
            var (distance, t) = Along(pixelX, pixelY, (a.PixelX!.Value, a.PixelY!.Value), (b.PixelX!.Value, b.PixelY!.Value));
            if (distance <= best)
                (on, best) = ((a.X + (t * (b.X - a.X)), a.Y + (t * (b.Y - a.Y))), distance);
        }
        if (position is { } p)
        {
            foreach (var curve in picture.Curves)
            {
                var (dx, dy) = (p.X - curve.Centre.X, p.Y - curve.Centre.Y);
                var length = Math.Sqrt((dx * dx) + (dy * dy));
                if (!(curve.Radius > 0) || !(length > 0))
                    continue;
                foreach (var (a, b) in curve.Pixels)
                {
                    var (distance, _) = Along(pixelX, pixelY, a, b);
                    if (distance <= best)
                        (on, best) = ((curve.Centre.X + (dx * curve.Radius / length), curve.Centre.Y + (dy * curve.Radius / length)), distance);
                }
            }
        }

        return on ?? position;
    }

    // ADR-0034 item 7: the next point from the pad. With an open chain the two numbers are an
    // offset from the last point, else a position. The tool uses a point only at the exact
    // position, with no distance in pixels.
    public void Next(double a, double b, SketchPicture picture, ISketchEditor editor)
    {
        ArgumentNullException.ThrowIfNull(picture);
        ArgumentNullException.ThrowIfNull(editor);
        // The tools of a curve take the two numbers as a position.
        if (_kind is SketchToolKind.Circle or SketchToolKind.Arc)
        {
            PlaceCurve(picture.Points.FirstOrDefault(point => point.X == a && point.Y == b)?.Id, (a, b), null, picture, editor);
            return;
        }

        var from = Last is { } last ? picture.Points.FirstOrDefault(point => point.Id == last) : null;
        var (x, y) = from is null ? (a, b) : (from.X + a, from.Y + b);
        Place(picture.Points.FirstOrDefault(point => point.X == x && point.Y == y)?.Id, (x, y), editor);
    }

    // The line from the last point to the first point, and the end of the chain. A chain of one
    // line has the line already, so it only ends.
    public void Close(ISketchEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        if (First is { } first && Last is { } last && first != last && _chainLines >= 2)
            editor.AddLine(last, first);
        End();
    }

    public void End()
    {
        (First, Last, _chainLines) = (null, null, 0);
        _curve.Clear();
    }

    // A row of the window or a tap of the tool Select: an item that is selected is cleared; a new
    // item is added, and a third item replaces the first.
    public void Toggle(Guid item)
    {
        if (_selection.Remove(item))
            return;
        _selection.Add(item);
        if (_selection.Count > MaximumSelection)
            _selection.RemoveAt(0);
    }

    public void ClearSelection() => _selection.Clear();

    // ADR-0034 item 5: after an undo, a redo or an edit of another client, the selection keeps only
    // the items of the record, and the chain ends when one of its two ends is gone.
    public void Forget(SketchPicture picture)
    {
        ArgumentNullException.ThrowIfNull(picture);
        var live = picture.Points.Select(point => point.Id).Concat(picture.Lines.Select(line => line.Id)).Concat(picture.Curves.Select(curve => curve.Id)).Concat(picture.Constraints).ToHashSet();
        _selection.RemoveAll(item => !live.Contains(item));
        if ((First is { } first && !live.Contains(first)) || (Last is { } last && !live.Contains(last)))
            End();
    }

    private void Place(Guid? hit, (double X, double Y)? position, ISketchEditor editor)
    {
        if (Last is not { } last)
        {
            var start = hit ?? (position is { } p ? editor.AddPoint(p.X, p.Y) : null);
            if (start is { } id)
                (First, Last) = (id, id);
            return;
        }

        if (hit == last)
        {
            End();
            return;
        }

        // A tap on the first point of a chain of one line would draw that line again.
        if (hit == First && _chainLines < 2)
        {
            End();
            return;
        }

        var target = hit ?? (position is { } q ? editor.AddPoint(q.X, q.Y) : null);
        if (target is not { } next || editor.AddLine(last, next) is null)
            return;

        _chainLines++;
        if (next == First)
            End();
        else
            Last = next;
    }

    // A tap of the tool Circle or Arc: the centre first, then a point on the circle, or the start
    // and then the end of an arc. A tap on the centre or on the start again ends the curve.
    private void PlaceCurve(Guid? hit, (double X, double Y)? position, (double X, double Y)? pixel, SketchPicture picture, ISketchEditor editor)
    {
        var at = hit is { } id && picture.Points.FirstOrDefault(point => point.Id == id) is { } point ? (point.X, point.Y) : position;
        if (at is not { } p || (hit is { } again && _curve.Any(item => item.Id == again)))
        {
            if (hit is not null)
                End();
            return;
        }

        if (_curve.Count == 0)
        {
            if ((hit ?? editor.AddPoint(p.X, p.Y)) is { } centre)
            {
                _curve.Add((centre, p.X, p.Y));
                (First, Last) = (centre, centre);
            }
            return;
        }

        // Finding V57 of the codebase review of 2026-10-31 (TASK-0133): the centre and the start take
        // their current positions from the picture, so a point that moved between two taps counts
        // where it is now.
        Refresh(picture);
        var c = _curve[0];
        var (dx, dy) = (p.X - c.X, p.Y - c.Y);
        var distance = Math.Sqrt((dx * dx) + (dy * dy));
        if (!(distance > 0))
            return;

        if (_kind == SketchToolKind.Circle)
        {
            editor.AddCircle(c.Id, distance);
            End();
            return;
        }

        if (_curve.Count == 1)
        {
            if ((hit ?? editor.AddPoint(p.X, p.Y)) is { } start)
            {
                _curve.Add((start, p.X, p.Y));
                Last = start;
            }
            return;
        }

        var (arcEnd, closes) = ArcEnd(p, pixel, picture, distance);
        if (hit is null && closes)
        {
            End();
            return;
        }

        var end = hit ?? editor.AddPoint(arcEnd.X, arcEnd.Y);
        if (end is { } e)
            editor.AddArc(c.Id, _curve[1].Id, e);
        End();
    }

    // The new end of an arc for a tap at the position p, at the distance from the centre: on the circle
    // of the start, along the direction of the tap. Closes is true when the curve ends there.
    // Finding V61 (TASK-0133): a new end in the direction of the start made a second point at the
    // position of the start and an arc of a full turn; it ends the curve.
    // Finding V63 of the codebase review of 2026-10-31 (b) (TASK-0144): the limit was 1e-9 of the
    // radius, so an end 0.003 mm from the start made a second point and an arc of nearly a full turn.
    // The curve ends when the new end is within HitPixels of the start on the screen. The scale is
    // the pixels for each unit of the plane between the tap and the start. The pad gives no pixel, and
    // keeps the limit of V61.
    private ((double X, double Y) End, bool Closes) ArcEnd((double X, double Y) p, (double X, double Y)? pixel, SketchPicture picture, double distance)
    {
        var (c, s) = (_curve[0], _curve[1]);
        var (dx, dy) = (p.X - c.X, p.Y - c.Y);
        var radius = Math.Sqrt(((s.X - c.X) * (s.X - c.X)) + ((s.Y - c.Y) * (s.Y - c.Y)));
        var (ex, ey) = (c.X + (dx * radius / distance), c.Y + (dy * radius / distance));
        var gap = Math.Sqrt(((ex - s.X) * (ex - s.X)) + ((ey - s.Y) * (ey - s.Y)));
        var apart = Math.Sqrt(((p.X - s.X) * (p.X - s.X)) + ((p.Y - s.Y) * (p.Y - s.Y)));
        var scale = picture.Points.FirstOrDefault(point => point.Id == s.Id) is { PixelX: { } sx, PixelY: { } sy } && pixel is { } tap && apart > 0
            ? Math.Sqrt(((tap.X - sx) * (tap.X - sx)) + ((tap.Y - sy) * (tap.Y - sy))) / apart
            : 0;
        return ((ex, ey), gap <= 1e-9 * radius || (scale > 0 && gap * scale <= HitPixels));
    }

    private void Refresh(SketchPicture picture)
    {
        for (var i = 0; i < _curve.Count; i++)
        {
            if (picture.Points.FirstOrDefault(point => point.Id == _curve[i].Id) is { } now)
                _curve[i] = (now.Id, now.X, now.Y);
        }
    }

    // Finding V62 (TASK-0133): the nearest of the lines and the curves, so a tap on a circle that a
    // line touches selects the circle. A line within HitPixels took the pick before a nearer curve.
    private static Guid? NearestEdge(double x, double y, SketchPicture picture)
    {
        var line = NearestLine(x, y, picture);
        var curve = NearestCurve(x, y, picture);
        if (line is null || curve is null)
            return line ?? curve;
        return Distance(x, y, picture, curve.Value) < Distance(x, y, picture, line.Value) ? curve : line;
    }

    // The distance in pixels from a point to a line or a curve of the picture.
    private static double Distance(double x, double y, SketchPicture picture, Guid item)
    {
        if (picture.Curves.FirstOrDefault(curve => curve.Id == item) is { } found)
            return found.Pixels.Select(segment => ToSegment(x, y, segment.A, segment.B)).DefaultIfEmpty(double.PositiveInfinity).Min();
        var line = picture.Lines.First(item2 => item2.Id == item);
        var (a, b) = (picture.Points.First(point => point.Id == line.Start), picture.Points.First(point => point.Id == line.End));
        return ToSegment(x, y, (a.PixelX!.Value, a.PixelY!.Value), (b.PixelX!.Value, b.PixelY!.Value));
    }

    private static Guid? NearestCurve(double x, double y, SketchPicture picture)
    {
        Guid? nearest = null;
        var best = HitPixels;
        foreach (var curve in picture.Curves)
        {
            foreach (var (a, b) in curve.Pixels)
            {
                var distance = ToSegment(x, y, a, b);
                if (distance <= best)
                    (nearest, best) = (curve.Id, distance);
            }
        }

        return nearest;
    }

    private static Guid? NearestPoint(double x, double y, SketchPicture picture)
    {
        Guid? nearest = null;
        var best = HitPixels;
        foreach (var point in picture.Points)
        {
            if (point.PixelX is not { } px || point.PixelY is not { } py)
                continue;
            var distance = Math.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
            if (distance <= best)
                (nearest, best) = (point.Id, distance);
        }

        return nearest;
    }

    private static Guid? NearestLine(double x, double y, SketchPicture picture)
    {
        var pixels = picture.Points
            .Where(point => point.PixelX is not null && point.PixelY is not null)
            .ToDictionary(point => point.Id, point => (X: point.PixelX!.Value, Y: point.PixelY!.Value));
        Guid? nearest = null;
        var best = HitPixels;
        foreach (var line in picture.Lines)
        {
            if (!pixels.TryGetValue(line.Start, out var a) || !pixels.TryGetValue(line.End, out var b))
                continue;
            var distance = ToSegment(x, y, a, b);
            if (distance <= best)
                (nearest, best) = (line.Id, distance);
        }

        return nearest;
    }

    // The distance in pixels from a point to a segment, and the parameter of the nearest point of the
    // segment from a (0) to b (1).
    private static (double Distance, double T) Along(double x, double y, (double X, double Y) a, (double X, double Y) b)
    {
        var (dx, dy) = (b.X - a.X, b.Y - a.Y);
        var squared = dx * dx + dy * dy;
        var t = squared == 0 ? 0 : Math.Clamp(((x - a.X) * dx + (y - a.Y) * dy) / squared, 0, 1);
        var (cx, cy) = (a.X + t * dx - x, a.Y + t * dy - y);
        return (Math.Sqrt(cx * cx + cy * cy), t);
    }

    // The distance in pixels from a point to a segment.
    private static double ToSegment(double x, double y, (double X, double Y) a, (double X, double Y) b)
    {
        var (dx, dy) = (b.X - a.X, b.Y - a.Y);
        var squared = dx * dx + dy * dy;
        var t = squared == 0 ? 0 : Math.Clamp(((x - a.X) * dx + (y - a.Y) * dy) / squared, 0, 1);
        var (cx, cy) = (a.X + t * dx - x, a.Y + t * dy - y);
        return Math.Sqrt(cx * cx + cy * cy);
    }
}
