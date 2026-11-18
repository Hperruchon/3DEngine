using Engine.Contracts.Drawing;
using Engine.Core.Export;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Desktop;

// ADR-0167 items 3 and 4 (phase R30b, TASK-0407): the place and the scale of a new view of a sheet, as the window computes
// them before it sends AddSheetView. The size of a view on the paper comes from the box of the live bodies along the right
// and the up of its frame, times its scale. The code uses + - * / only. The numbers go into the command, so a replay reads
// them from the log and does not compute them again.
internal static class SheetPlacement
{
    public sealed record Box((double X, double Y, double Z) Min, (double X, double Y, double Z) Max);

    // ADR-0167 item 3: the gap between two views on the paper, in millimetres (D5 A).
    public const double Gap = 10;

    // ADR-0167 item 4: the scales of ISO 5455, the largest first, for the scale that fits.
    public static readonly IReadOnlyList<(long Numerator, long Denominator)> Series =
        [(50, 1), (20, 1), (10, 1), (5, 1), (2, 1), (1, 1), (1, 2), (1, 5), (1, 10), (1, 20), (1, 50), (1, 100), (1, 200), (1, 500), (1, 1000)];

    // ADR-0167 item 5: the scales of the pad (D6 A).
    public static readonly IReadOnlyList<(long Numerator, long Denominator)> PadScales = [(10, 1), (5, 1), (2, 1), (1, 1), (1, 2), (1, 5), (1, 10)];

    // The exact frame of each flat view of the window: the right, the up and the direction toward the eye, as the standard views of the engine.
    // A test checks that each one is the frame that ViewLines gives for the camera of the session.
    public static double[] FrameOf(FlatView view) => view switch
    {
        FlatView.Front => [1, 0, 0, 0, 0, 1, 0, -1, 0],
        FlatView.Back => [-1, 0, 0, 0, 0, 1, 0, 1, 0],
        FlatView.Right => [0, 1, 0, 0, 0, 1, 1, 0, 0],
        FlatView.Left => [0, -1, 0, 0, 0, 1, -1, 0, 0],
        FlatView.Top => [1, 0, 0, 0, 1, 0, 0, 0, 1],
        FlatView.Bottom => [1, 0, 0, 0, -1, 0, 0, 0, -1],
        _ => throw new ArgumentOutOfRangeException(nameof(view), view, "A flat view is one of the six views."),
    };

    // ADR-0167 item 3: the standard views of D4 A: the front, the top and the side view from the right.
    public static readonly IReadOnlyList<FlatView> Standard = [FlatView.Front, FlatView.Top, FlatView.Right];

    // The size of the paper in millimetres with its orientation, as for a sheet of the engine. A test checks it against GetSheet.
    public static (double Width, double Height) Paper(string paper, bool landscape)
    {
        var (shortSide, longSide) = paper switch
        {
            "A4" => (210.0, 297.0),
            "A3" => (297.0, 420.0),
            "A2" => (420.0, 594.0),
            "A1" => (594.0, 841.0),
            "A0" => (841.0, 1189.0),
            _ => throw new ArgumentException($"The paper '{paper}' is not one of {string.Join(", ", SheetRecord.Papers)}.", nameof(paper)),
        };
        return landscape ? (longSide, shortSide) : (shortSide, longSide);
    }

    // ADR-0167 item 3: the part of the paper for the views: inside the frame of the files (SvgExport), above the band of
    // the title block. Left, bottom, right, top in millimetres.
    public static (double X0, double Y0, double X1, double Y1) Area(double width, double height)
        => (SvgExport.Margin, SvgExport.Margin + SvgExport.TitleHeight, width - SvgExport.Margin, height - SvgExport.Margin);

    // The size of the box along the right and along the up of a frame of length 1, in units of the design.
    public static (double Across, double Up) Extent(IReadOnlyList<double> frame, Box box)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(box);
        var (dx, dy, dz) = (box.Max.X - box.Min.X, box.Max.Y - box.Min.Y, box.Max.Z - box.Min.Z);
        double Along(int first) => (Math.Abs(frame[first]) * dx) + (Math.Abs(frame[first + 1]) * dy) + (Math.Abs(frame[first + 2]) * dz);
        return (Along(0), Along(3));
    }

    // ADR-0167 item 3: the side of the paper where a view goes next to a reference view, as the projection of the sheet puts it,
    // or null when the two views share no axis. A view that shares the right of the reference goes under it or over it, and a
    // view that shares the up goes left or right of it. In first angle a view from the right of the reference goes at its left
    // and a view from above goes under it; third angle is the other way.
    public static (int X, int Y)? Side(IReadOnlyList<double> reference, IReadOnlyList<double> view, bool thirdAngle)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(view);
        static bool Same(IReadOnlyList<double> a, int i, IReadOnlyList<double> b, int j) => a[i] == b[j] && a[i + 1] == b[j + 1] && a[i + 2] == b[j + 2];
        static double Dot(IReadOnlyList<double> a, int i, IReadOnlyList<double> b, int j) => (a[i] * b[j]) + (a[i + 1] * b[j + 1]) + (a[i + 2] * b[j + 2]);
        var sign = thirdAngle ? 1 : -1;
        if (Same(view, 0, reference, 0) && Math.Abs(Dot(view, 6, reference, 3)) == 1)
            return (0, sign * (int)Dot(view, 6, reference, 3));
        if (Same(view, 3, reference, 3) && Math.Abs(Dot(view, 6, reference, 0)) == 1)
            return (sign * (int)Dot(view, 6, reference, 0), 0);
        return null;
    }

    // A view on the paper: its centre and its half size, in millimetres.
    private readonly record struct Rect(double X, double Y, double HalfWidth, double HalfHeight)
    {
        public bool Inside((double X0, double Y0, double X1, double Y1) area)
            => X - HalfWidth >= area.X0 && X + HalfWidth <= area.X1 && Y - HalfHeight >= area.Y0 && Y + HalfHeight <= area.Y1;

        public bool Meets(Rect other)
            => Math.Abs(X - other.X) < HalfWidth + other.HalfWidth && Math.Abs(Y - other.Y) < HalfHeight + other.HalfHeight;
    }

    private static Rect RectOf(IReadOnlyList<double> frame, long numerator, long denominator, double x, double y, Box box)
    {
        var (across, up) = Extent(frame, box);
        var scale = (double)numerator / denominator;
        return new Rect(x, y, across * scale / 2, up * scale / 2);
    }

    // ADR-0167 item 3: the place of a new view on a sheet. First the place that the projection gives next to each view of the
    // sheet with a shared axis, in the order of the sheet, with a gap of 10 mm; the first that is inside the area and meets no
    // view wins. Else the free place on a grid of 5 mm that is nearest to the first place of the projection, or to the centre
    // of the area. Else the centre of the area: the view then meets another view or leaves the area, and the result says so.
    public static ((double X, double Y) Place, bool Fits) Place(SheetRecord sheet, IReadOnlyList<double> frame, long numerator, long denominator, Box box)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var (width, height) = Paper(sheet.Paper, sheet.Landscape);
        var area = Area(width, height);
        var placed = sheet.Views.Select(view => (Frame: view.Direction, Rect: RectOf(view.Direction, view.ScaleNumerator, view.ScaleDenominator, view.X, view.Y, box))).ToList();
        var size = RectOf(frame, numerator, denominator, 0, 0, box);
        bool Free(Rect rect) => rect.Inside(area) && placed.All(other => !rect.Meets(other.Rect));

        (double X, double Y)? wanted = null;
        foreach (var (reference, rect) in placed)
        {
            if (Side(reference, frame, sheet.ThirdAngle) is not { } side)
                continue;
            var candidate = size with
            {
                X = rect.X + (side.X * (rect.HalfWidth + Gap + size.HalfWidth)),
                Y = rect.Y + (side.Y * (rect.HalfHeight + Gap + size.HalfHeight)),
            };
            wanted ??= (candidate.X, candidate.Y);
            if (Free(candidate))
                return ((candidate.X, candidate.Y), true);
        }

        var target = wanted ?? ((area.X0 + area.X1) / 2, (area.Y0 + area.Y1) / 2);
        (double X, double Y)? best = null;
        var bestDistance = double.PositiveInfinity;
        for (var x = area.X0 + size.HalfWidth; x <= area.X1 - size.HalfWidth; x += 5)
        {
            for (var y = area.Y0 + size.HalfHeight; y <= area.Y1 - size.HalfHeight; y += 5)
            {
                var distance = ((x - target.X) * (x - target.X)) + ((y - target.Y) * (y - target.Y));
                if (distance < bestDistance && Free(size with { X = x, Y = y }))
                    (best, bestDistance) = ((x, y), distance);
            }
        }

        return best is { } free ? (free, true) : (((area.X0 + area.X1) / 2, (area.Y0 + area.Y1) / 2), false);
    }

    // ADR-0167 items 3 and 4: the places of a group of views on a new sheet: each view next to the first view of the group with
    // a shared axis, as the projection puts it, or right of the group; then the box of the group goes to the centre of the
    // area.
    public static IReadOnlyList<(double X, double Y)> Group(string paper, bool landscape, bool thirdAngle, IReadOnlyList<IReadOnlyList<double>> frames, long numerator, long denominator, Box box)
    {
        ArgumentNullException.ThrowIfNull(frames);
        var (width, height) = Paper(paper, landscape);
        var area = Area(width, height);
        var rects = new List<Rect>(frames.Count);
        foreach (var frame in frames)
        {
            var size = RectOf(frame, numerator, denominator, 0, 0, box);
            Rect? at = null;
            for (var j = 0; j < rects.Count && at is null; j++)
            {
                if (Side(frames[j], frame, thirdAngle) is { } side)
                {
                    at = size with
                    {
                        X = rects[j].X + (side.X * (rects[j].HalfWidth + Gap + size.HalfWidth)),
                        Y = rects[j].Y + (side.Y * (rects[j].HalfHeight + Gap + size.HalfHeight)),
                    };
                }
            }

            rects.Add(at ?? (rects.Count == 0 ? size : size with { X = rects.Max(rect => rect.X + rect.HalfWidth) + Gap + size.HalfWidth }));
        }

        var (left, right) = (rects.Min(rect => rect.X - rect.HalfWidth), rects.Max(rect => rect.X + rect.HalfWidth));
        var (bottom, top) = (rects.Min(rect => rect.Y - rect.HalfHeight), rects.Max(rect => rect.Y + rect.HalfHeight));
        var (dx, dy) = (((area.X0 + area.X1) / 2) - ((left + right) / 2), ((area.Y0 + area.Y1) / 2) - ((bottom + top) / 2));
        return [.. rects.Select(rect => (rect.X + dx, rect.Y + dy))];
    }

    // ADR-0167 item 4: the largest scale of ISO 5455 at which the standard views fit the area of the paper, or the smallest
    // scale of the series when none fits.
    public static (long Numerator, long Denominator) FitScale(string paper, bool landscape, bool thirdAngle, Box box)
    {
        var (width, height) = Paper(paper, landscape);
        var area = Area(width, height);
        IReadOnlyList<IReadOnlyList<double>> frames = [.. Standard.Select(FrameOf)];
        foreach (var (numerator, denominator) in Series)
        {
            var places = Group(paper, landscape, thirdAngle, frames, numerator, denominator, box);
            if (places.Zip(frames).All(pair => RectOf(pair.Second, numerator, denominator, pair.First.X, pair.First.Y, box).Inside(area)))
                return (numerator, denominator);
        }

        return Series[^1];
    }

    // The text of a scale, as "2:1".
    public static string Text((long Numerator, long Denominator) scale) => $"{scale.Numerator}:{scale.Denominator}";

    // ADR-0167 item 5: a free ratio "n:d" of two whole numbers greater than zero, or null.
    public static (long Numerator, long Denominator)? Ratio(string text)
    {
        var parts = (text ?? string.Empty).Split(':');
        return parts.Length == 2
            && long.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var n)
            && long.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var d)
            && n > 0 && d > 0
                ? (n, d)
                : null;
    }
}
