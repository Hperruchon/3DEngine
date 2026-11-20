using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Core.Queries;
using Engine.Kernel;

namespace Engine.Core.Drawing;

// ADR-0155 (phase P10.2, TASK-0393): the paper of a sheet, the frame of a view, and the lines of a view on the paper. The
// lines are presentation (ADR-0155 item 5): a read computes them, and no command, event or Document holds them.
internal static class SheetLayout
{
    // ADR-0165 item 3: the length of paper in millimetres by which a centre line goes past its ends.
    public const double CentreOverrun = 3;

    // ADR-0169 item 5 (phase R30h, TASK-0411): the cut line of a section goes CutOverrun past the box of its parent; the outer
    // CutEnd of each end is thick; an arrow is ArrowLength long; a letter is LetterGap past the head of its arrow; the label is
    // LabelGap under the box of the section; each text has the height TextSize. Each value is in millimetres of the paper.
    public const double CutOverrun = 5;
    public const double CutEnd = 5;
    public const double ArrowLength = 7;
    public const double LetterGap = 4;
    public const double LabelGap = 10;
    public const double TextSize = 5;

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
    //
    // ADR-0169 items 3 to 5 (phase R30h, TASK-0411): a section view cuts each solid and gives the hatch of its cut faces and its
    // label; a view that is the parent of a section gives the cut line, the arrows and the letters of that section.
    //
    // ADR-0171 item 1 (TASK-0413): the centre (cu, cv) of the box comes with the lines, so a client turns a place on the paper
    // back into a point of the view.
    public static (IReadOnlyList<SheetLine> Lines, IReadOnlyList<SheetSkippedBody> Skipped, IReadOnlyList<SheetText> Texts, (double U, double V) Centre) Place(Document document, SheetRecord sheet, SheetView view)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(view);
        var frame = Frame(view.Direction)
            ?? throw new InvalidOperationException($"The view '{view.Id}' has a direction that is not a valid frame (ADR-0155 item 1).");
        (Vector3R Point, Guid View)? cut = view.Section is { } section ? (CutPoint(view.Id, section), view.Id) : null;
        var (lines, skipped, curves, cutFaces) = ViewEdges.Section(document, frame, cut);
        // ADR-0164 item 1 (TASK-0400, finding E212 of the codebase review of 2026-11-17): the length of the frame comes from
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

        // ADR-0165 items 1 and 3 (phase R30g, TASK-0405): the box and the centre come from the edges and the outlines, so a flag
        // never moves a view. A view with HiddenLines false leaves out its hidden lines; a view with CentreLines true adds its
        // centre lines, from each line, the hidden lines too, so the axis of a hole stays when its walls do not show.
        var shown = lines.Where(line => view.HiddenLines || line.Visible).ToList();
        if (view.CentreLines)
            shown.AddRange(ViewEdges.Centres(lines, frame, curves));

        var placed = new List<SheetLine>(shown.Count);
        foreach (var line in shown)
        {
            var (from, to) = (Point(line.From), Point(line.To));
            if (line.Kind == ViewLineKind.Centre)
            {
                // ADR-0165 item 3: a centre line goes 3 mm of paper past its ends (ISO 128).
                var (dx, dy) = (to.X - from.X, to.Y - from.Y);
                var length = Math.Sqrt((dx * dx) + (dy * dy));
                if (length > 0)
                {
                    var (ux, uy) = (CentreOverrun * dx / length, CentreOverrun * dy / length);
                    (from, to) = ((from.X - ux, from.Y - uy), (to.X + ux, to.Y + uy));
                }
            }

            var circle = line.Circle is { } c
                ? new SheetCircle(Point(c.Centre).X, Point(c.Centre).Y, factor * c.Radius.ToDouble())
                : null;
            var ellipse = line.Ellipse is { } e
                ? new SheetEllipse(Point(e.Centre).X, Point(e.Centre).Y, factor * e.Major.X, factor * e.Major.Y, factor * e.Minor.X, factor * e.Minor.Y)
                : null;
            placed.Add(new SheetLine(line.Body, from.X, from.Y, to.X, to.Y, line.Visible, line.Kind switch { ViewLineKind.Outline => "outline", ViewLineKind.Centre => "centre", _ => "edge" })
            {
                Circle = circle,
                Ellipse = ellipse,
            });
        }

        // ADR-0169 item 4: the hatch of the cut faces of each body, up to the right for the first body and down to the right for
        // the next one.
        for (var b = 0; b < cutFaces.Count; b++)
        {
            var loops = cutFaces[b].Loops.Select(loop => (IReadOnlyList<(double X, double Y)>)[.. loop.Select(Point)]).ToList();
            placed.AddRange(SectionCut.Hatch(cutFaces[b].Body, loops, b % 2 == 0));
        }

        // ADR-0169 item 5: the box of the edges and the outlines on the paper, or the centre of a view with no line.
        var (x0, y0) = lines.Count == 0 ? (view.X, view.Y) : Paper(minU, minV);
        var (x1, y1) = lines.Count == 0 ? (view.X, view.Y) : Paper(maxU, maxV);
        var texts = new List<SheetText>();
        if (lines.Count > 0)
        {
            foreach (var child in sheet.Views.Where(other => other.Section is { } s && s.Parent == view.Id))
            {
                if (Frame(child.Direction) is not { } childFrame)
                    continue;
                var point = CutPoint(child.Id, child.Section!);
                var normal = childFrame.TowardEye;
                var (a, b, c) = (normal.Dot(frame.Right), normal.Dot(frame.Up), normal.Dot(frame.TowardEye));
                if (!c.IsZero || a.IsZero == b.IsZero)
                    continue;

                // The plane is u = P · Right (a vertical line on the paper) or v = P · Up (a horizontal line), and the direction of
                // sight is the opposite of the normal.
                var vertical = b.IsZero;
                var place = vertical ? Paper(point.Dot(frame.Right).ToDouble(), 0).X : Paper(0, point.Dot(frame.Up).ToDouble()).Y;
                // Finding E221 of the codebase review of 2026-11-18 (b) (TASK-0416): a point far from the part can put the cut line
                // at a place on the paper that is not a finite number. That section then has no cut line, no arrows and no letters,
                // so the read stays valid JSON and the files write no 0 in place of infinity.
                if (!double.IsFinite(place))
                    continue;
                var (low, high) = vertical ? (y0, y1) : (x0, x1);
                var sight = -(vertical ? a.Sign : b.Sign);
                (double X, double Y) At(double along, double across)
                    => vertical ? (place + across, along) : (along, place + across);
                SheetLine Line(double from, double to, string kind)
                {
                    var (p, q) = (At(from, 0), At(to, 0));
                    return new SheetLine(child.Id, p.X, p.Y, q.X, q.Y, true, kind);
                }

                placed.Add(Line(low - CutOverrun, high + CutOverrun, "cut"));
                placed.Add(Line(low - CutOverrun, low - CutOverrun + CutEnd, "cut-end"));
                placed.Add(Line(high + CutOverrun - CutEnd, high + CutOverrun, "cut-end"));
                foreach (var end in new[] { low - CutOverrun + (CutEnd / 2), high + CutOverrun - (CutEnd / 2) })
                {
                    var (tail, tip) = (At(end, 0), At(end, sight * ArrowLength));
                    placed.Add(new SheetLine(child.Id, tail.X, tail.Y, tip.X, tip.Y, true, "arrow"));
                    var letter = At(end, sight * (ArrowLength + LetterGap));
                    // Finding E224 (TASK-0416): TextSize is the height of a capital letter, so the base line is half of it under the
                    // place, and the letter is centred on the line of its arrow. Until TASK-0416 the shift was 0.35 of TextSize.
                    texts.Add(new SheetText(letter.X, letter.Y - (TextSize / 2), child.Section!.Letter, TextSize));
                }
            }
        }

        if (view.Section is { } own)
            texts.Add(new SheetText((x0 + x1) / 2, y0 - LabelGap, $"{own.Letter}-{own.Letter}", TextSize));

        return (placed, [.. skipped.Select(entry => new SheetSkippedBody(entry.Body, entry.Reason))], texts, (cu, cv));
    }

    // ADR-0169 item 1: the exact point of the cut plane of a section.
    private static Vector3R CutPoint(Guid view, SheetSection section)
        => section.Point is { Count: 3 } point && point.All(double.IsFinite)
            ? Vector3R.FromDoubles(point[0], point[1], point[2])
            : throw new InvalidOperationException($"The section '{view}' has a point that is not three finite numbers (ADR-0169 item 1).");
}
