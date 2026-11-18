using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Core.Queries;
using Engine.Kernel;

namespace Engine.Core.Drawing;

// ADR-0155 (phase P10.2, TASK-0393): the paper of a sheet, the frame of a view, and the lines of a view on the paper. The
// lines are presentation (ADR-0155 item 5): a read computes them, and no command, event or Document holds them.
internal static class SheetLayout
{
    // ADR-0155 item 4: the size of the paper in millimetres with its orientation, ISO 216.
    public static (double Width, double Height) PaperSize(SheetRecord sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var (shortSide, longSide) = sheet.Paper switch
        {
            "A4" => (210.0, 297.0),
            "A3" => (297.0, 420.0),
            "A2" => (420.0, 594.0),
            "A1" => (594.0, 841.0),
            "A0" => (841.0, 1189.0),
            _ => throw new ArgumentException($"The paper '{sheet.Paper}' is not one of {string.Join(", ", SheetRecord.Papers)}.", nameof(sheet)),
        };
        return sheet.Landscape ? (longSide, shortSide) : (shortSide, longSide);
    }

    // ADR-0155 item 1 (Q1 A): the exact frame of nine numbers, the right, the up and the direction toward the eye, or null
    // when a number is not finite or the frame is not valid (ADR-0155 item 1). No tolerance applies.
    public static ViewFrame? Frame(IReadOnlyList<double> direction)
    {
        if (direction is not { Count: 9 } || direction.Any(value => !double.IsFinite(value)))
            return null;
        Vector3R V(int first) => new(Rational.FromDouble(direction[first]), Rational.FromDouble(direction[first + 1]), Rational.FromDouble(direction[first + 2]));
        var frame = new ViewFrame(V(0), V(3), V(6));
        return frame.IsValid ? frame : null;
    }

    // ADR-0155 item 5 (Q2 A): the lines of the view on the paper, in millimetres from the lower left corner, and each body
    // that gives no lines. A point (u, v) of the view goes to (X + f (u - cu), Y + f (v - cv)), where f is the scale divided
    // by the length L of Right, and (cu, cv) is the centre of the box of the lines: their ends, and the whole circle or
    // ellipse of a line that has one. A view with no line has its centre at the origin of the part. The numbers become
    // doubles first, and the code uses + - * / and sqrt only.
    public static (IReadOnlyList<SheetLine> Lines, IReadOnlyList<SheetSkippedBody> Skipped) Place(Document document, SheetView view)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(view);
        var frame = Frame(view.Direction)
            ?? throw new InvalidOperationException($"The view '{view.Id}' has a direction that is not a valid frame (ADR-0155 item 1).");
        var (lines, skipped) = ViewEdges.Of(document, frame);
        // ADR-0163 item 1 (TASK-0400, finding E212 of the codebase review of 2026-11-17): the length of the frame comes from
        // ViewFrame.Length, which never squares a double. Until TASK-0400 a frame of a length near 1e-200 gave the factor +∞
        // and a read that JSON cannot write, and one near 1e200 gave 0 and a view of one point.
        var factor = (double)view.ScaleNumerator / view.ScaleDenominator / frame.Length.Value;

        var (minU, minV, maxU, maxV) = (double.PositiveInfinity, double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity);
        void Add(double u, double v)
        {
            (minU, maxU) = (Math.Min(minU, u), Math.Max(maxU, u));
            (minV, maxV) = (Math.Min(minV, v), Math.Max(maxV, v));
        }
        foreach (var line in lines)
        {
            Add(line.From.X.ToDouble(), line.From.Y.ToDouble());
            Add(line.To.X.ToDouble(), line.To.Y.ToDouble());
            if (line.Circle is { } circle)
            {
                var (u, v, r) = (circle.Centre.X.ToDouble(), circle.Centre.Y.ToDouble(), circle.Radius.ToDouble());
                Add(u - r, v - r);
                Add(u + r, v + r);
            }
            if (line.Ellipse is { } ellipse)
            {
                var (u, v) = (ellipse.Centre.X.ToDouble(), ellipse.Centre.Y.ToDouble());
                var across = Math.Sqrt((ellipse.Major.X * ellipse.Major.X) + (ellipse.Minor.X * ellipse.Minor.X));
                var up = Math.Sqrt((ellipse.Major.Y * ellipse.Major.Y) + (ellipse.Minor.Y * ellipse.Minor.Y));
                Add(u - across, v - up);
                Add(u + across, v + up);
            }
        }
        var (cu, cv) = lines.Count == 0 ? (0.0, 0.0) : ((minU + maxU) / 2, (minV + maxV) / 2);

        (double X, double Y) Paper(double u, double v) => (view.X + (factor * (u - cu)), view.Y + (factor * (v - cv)));
        (double X, double Y) Point((Rational X, Rational Y) p) => Paper(p.X.ToDouble(), p.Y.ToDouble());

        var placed = new List<SheetLine>(lines.Count);
        foreach (var line in lines)
        {
            var (from, to) = (Point(line.From), Point(line.To));
            var circle = line.Circle is { } c
                ? new SheetCircle(Point(c.Centre).X, Point(c.Centre).Y, factor * c.Radius.ToDouble())
                : null;
            var ellipse = line.Ellipse is { } e
                ? new SheetEllipse(Point(e.Centre).X, Point(e.Centre).Y, factor * e.Major.X, factor * e.Major.Y, factor * e.Minor.X, factor * e.Minor.Y)
                : null;
            placed.Add(new SheetLine(line.Body, from.X, from.Y, to.X, to.Y, line.Visible, line.Kind == ViewLineKind.Outline ? "outline" : "edge")
            {
                Circle = circle,
                Ellipse = ellipse,
            });
        }

        return (placed, [.. skipped.Select(entry => new SheetSkippedBody(entry.Body, entry.Reason))]);
    }
}
