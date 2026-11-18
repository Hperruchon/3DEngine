using System.Numerics;
using Engine.Core.Queries;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Desktop;

// ADR-0168 (phase R30c, TASK-0408): the geometry of the sheet page: the paper in pixels of the window, the box of each view, the
// view under a point, and the place of a dragged view with the alignment of the projection. The page reads GetSheet and sends
// commands; the numbers of a command come from here, so a replay reads them from the log.
internal static class SheetPage
{
    // ADR-0168 item 1: the paper in the free part, the gap in pixels around it, and the margin of the box of a view in
    // millimetres of the paper for a tap.
    public const float Gap = 12f;
    public const double TapMargin = 3;

    // The paper on the screen: the pixel of its lower left corner and the pixels of one millimetre.
    public readonly record struct Paper(float Left, float Bottom, float Scale, double Width, double Height)
    {
        public Vector2 Pixel(double x, double y) => new(Left + (float)(x * Scale), Bottom - (float)(y * Scale));

        public (double X, double Y) Millimetres(Vector2 pixel) => ((pixel.X - Left) / Scale, (Bottom - pixel.Y) / Scale);

        public Vector4 Rectangle => new(Left, Bottom - (float)(Height * Scale), Left + (float)(Width * Scale), Bottom);
    }

    // ADR-0171 item 3 (TASK-0413): the distance in millimetres of the paper within which a cut snaps to the centre of a circle.
    public const double SectionSnap = 2;

    // ADR-0171 item 3: the place of a cut across its line on the paper: x for the looks "left" and "right", whose line is
    // vertical, and y for "up" and "down"; the place snaps to the nearest centre of a whole circle of the view within
    // SectionSnap.
    public static double CutPlace(SheetViewInfo view, string look, (double X, double Y) at)
    {
        ArgumentNullException.ThrowIfNull(view);
        var vertical = look is "left" or "right";
        var place = vertical ? at.X : at.Y;
        double? best = null;
        foreach (var line in view.Lines)
        {
            if (line.Circle is not { } circle || line.FromX != line.ToX || line.FromY != line.ToY)
                continue;
            var centre = vertical ? circle.X : circle.Y;
            if (Math.Abs(centre - place) <= SectionSnap && (best is null || Math.Abs(centre - place) < Math.Abs(best.Value - place)))
                best = centre;
        }

        return best ?? place;
    }

    // ADR-0171 item 1: the point of the design on a place of the paper in a view: u = cu + (x - X) / f and v = cv + (y - Y) / f,
    // with f the scale divided by the length L of the right, and the point (u Right + v Up) / L².
    public static (double X, double Y, double Z) DesignPoint(SheetViewInfo view, (double X, double Y) at)
    {
        ArgumentNullException.ThrowIfNull(view);
        var d = view.Direction;
        var square = (d[0] * d[0]) + (d[1] * d[1]) + (d[2] * d[2]);
        var f = (double)view.ScaleNumerator / view.ScaleDenominator / Math.Sqrt(square);
        var (u, v) = (view.CentreU + ((at.X - view.X) / f), view.CentreV + ((at.Y - view.Y) / f));
        return (((u * d[0]) + (v * d[3])) / square, ((u * d[1]) + (v * d[4])) / square, ((u * d[2]) + (v * d[5])) / square);
    }

    // ADR-0168 item 1: the largest paper with its aspect inside the rectangle (left, top, right, bottom) less the gap, at its
    // centre.
    public static Paper Fit(double width, double height, Vector4 room)
    {
        var (w, h) = (room.Z - room.X - (2 * Gap), room.W - room.Y - (2 * Gap));
        var scale = (float)Math.Max(0.01, Math.Min(w / width, h / height));
        var (pw, ph) = ((float)(width * scale), (float)(height * scale));
        var left = room.X + ((room.Z - room.X - pw) / 2);
        var top = room.Y + ((room.W - room.Y - ph) / 2);
        return new Paper(left, top + ph, scale, width, height);
    }

    // The box of a view on the paper in millimetres (left, bottom, right, top): its lines with each whole curve, or a square of
    // 10 mm at its place for a view with no line.
    public static (double X0, double Y0, double X1, double Y1) Box(SheetViewInfo view)
    {
        ArgumentNullException.ThrowIfNull(view);
        var (x0, y0, x1, y1) = (double.PositiveInfinity, double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity);
        void Add(double x, double y) => (x0, y0, x1, y1) = (Math.Min(x0, x), Math.Min(y0, y), Math.Max(x1, x), Math.Max(y1, y));
        foreach (var line in view.Lines)
        {
            Add(line.FromX, line.FromY);
            Add(line.ToX, line.ToY);
            if (line.Circle is { } c)
            {
                Add(c.X - c.Radius, c.Y - c.Radius);
                Add(c.X + c.Radius, c.Y + c.Radius);
            }

            if (line.Ellipse is { } e)
            {
                var (across, up) = (Math.Sqrt((e.MajorX * e.MajorX) + (e.MinorX * e.MinorX)), Math.Sqrt((e.MajorY * e.MajorY) + (e.MinorY * e.MinorY)));
                Add(e.X - across, e.Y - up);
                Add(e.X + across, e.Y + up);
            }
        }

        return double.IsFinite(x0) ? (x0, y0, x1, y1) : (view.X - 5, view.Y - 5, view.X + 5, view.Y + 5);
    }

    // ADR-0168 item 2: the view under a point of the paper, the last drawn first, or null for the free paper.
    public static Guid? Hit(SheetInfo sheet, (double X, double Y) point)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        foreach (var view in sheet.Views.Reverse())
        {
            var (x0, y0, x1, y1) = Box(view);
            if (point.X >= x0 - TapMargin && point.X <= x1 + TapMargin && point.Y >= y0 - TapMargin && point.Y <= y1 + TapMargin)
                return view.ViewId;
        }

        return null;
    }

    // ADR-0168 item 3 (D13 A): the new place of a dragged view and the flag Aligned of MoveSheetViews. The first view of the sheet
    // is the base: with "Keep aligned" on and no Shift, it moves free and takes the views on its lines of projection. A view on a
    // line of projection through the base slides on that line only. Each other drag, and each drag with Shift or "Keep
    // aligned" off, moves the view free and alone. A view that is not on the sheet gives null (finding V202, TASK-0415).
    public static (double X, double Y, bool Aligned)? Drag(SheetInfo sheet, Guid viewId, double dx, double dy, bool keepAligned, bool shift)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        if (sheet.Views.FirstOrDefault(candidate => candidate.ViewId == viewId) is not { } view)
            return null;
        if (!keepAligned || shift)
            return (view.X + dx, view.Y + dy, false);
        var first = sheet.Views[0];
        if (first.ViewId == viewId)
            return (view.X + dx, view.Y + dy, true);
        static bool Same(IReadOnlyList<double> a, IReadOnlyList<double> b, int i) => a[i] == b[i] && a[i + 1] == b[i + 1] && a[i + 2] == b[i + 2];
        if (Same(first.Direction, view.Direction, 0) && first.X == view.X)
            return (view.X, view.Y + dy, false);
        if (Same(first.Direction, view.Direction, 3) && first.Y == view.Y)
            return (view.X + dx, view.Y, false);
        return (view.X + dx, view.Y + dy, false);
    }

    // ADR-0168 item 2: the flat view of the window with the frame of a view, for "Go to", or null for a frame of no flat view.
    public static FlatView? FlatOf(IReadOnlyList<double> direction)
    {
        ArgumentNullException.ThrowIfNull(direction);
        foreach (var view in Enum.GetValues<FlatView>())
        {
            if (SheetPlacement.FrameOf(view).SequenceEqual(direction))
                return view;
        }

        return null;
    }

    // The points of a line of the sheet on the paper, in millimetres, with its arc as straight parts (ViewLines.Path).
    public static IReadOnlyList<(double U, double V)> Path(SheetLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        return ViewLines.Path(new ViewLineInfo(line.Body, line.FromX, line.FromY, line.ToX, line.ToY, line.Visible, line.Kind)
        {
            Circle = line.Circle is { } c ? new ViewCircleInfo(c.X, c.Y, c.Radius) : null,
            Ellipse = line.Ellipse is { } e ? new ViewEllipseInfo(e.X, e.Y, e.MajorX, e.MajorY, e.MinorX, e.MinorY) : null,
        });
    }
}

// ADR-0168 item 1: the read of the sheet of the page, off the thread of the frame, one read at a time, for a new sheet or a new
// version of the Document; until it ends, the page keeps the last read of the same sheet.
internal sealed class SheetReadCache
{
    private string? _asked;
    private Task<SheetInfo?>? _reading;
    private SheetInfo? _last;

    public int Computed { get; private set; }

    public (SheetInfo? Sheet, bool Pending) Read(long version, Guid sheet, Func<Task<SheetInfo?>> read)
    {
        ArgumentNullException.ThrowIfNull(read);
        if (_reading is { IsCompleted: true } done)
        {
            if (done.IsCompletedSuccessfully && done.Result is { } result)
                _last = result;
            _ = done.Exception;
            _reading = null;
        }

        var key = $"{version}:{sheet}";
        if (_asked != key && _reading is null)
        {
            _asked = key;
            _reading = read();
            Computed++;
        }

        var shown = _last?.SheetId == sheet ? _last : null;
        return (shown, _asked != key || _reading is not null);
    }
}

// ADR-0168 item 4 (finding V196 of the codebase review of 2026-11-18): the commands of the sheet page run on the thread pool,
// one at a time in their order, so a frame never waits for the serial lock of the session. Each work gives the text that the
// window shows when it ends.
internal sealed class SheetCommandQueue
{
    private readonly Queue<Func<string?>> _waiting = new();
    private Task<string?>? _running;

    public bool Busy => _running is not null || _waiting.Count > 0;

    public void Send(Func<string?> work)
    {
        ArgumentNullException.ThrowIfNull(work);
        _waiting.Enqueue(work);
    }

    // On the frame thread: the texts of the ended works, and the start of the next work.
    public IReadOnlyList<string> Poll()
    {
        var texts = new List<string>();
        if (_running is { IsCompleted: true } done)
        {
            var text = done.IsCompletedSuccessfully ? done.Result : $"The command failed: {done.Exception?.GetBaseException().Message}";
            if (text is not null)
                texts.Add(text);
            _running = null;
        }

        if (_running is null && _waiting.TryDequeue(out var next))
            _running = Task.Run(next);
        return texts;
    }

    // For a test: waits until each work has ended.
    internal async Task<IReadOnlyList<string>> Drain()
    {
        var texts = new List<string>();
        while (Busy)
        {
            texts.AddRange(Poll());
            if (_running is { } running)
                await running.ConfigureAwait(false);
        }

        return texts;
    }
}
