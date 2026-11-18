using System.Globalization;
using System.Text;
using Engine.Contracts;
using Engine.Core.Queries;

namespace Engine.Core.Export;

// ADR-0161 (phase P10c, TASK-0401): one sheet of a Document as one PDF file of version 1.4, from the read GetSheet. A PDF file
// is presentation, as a tessellation is: it never enters a command, an event, the Document or the log (CLAUDE.md, determinism
// rule 6). The page is the paper; one matrix turns the millimetre into the point, so each number of the content is in
// millimetres of the paper. An arc is a chain of cubic Bézier curves made with + − × ÷ and sqrt only, the file holds no date,
// and the objects come in one order, so the file is the same on each platform.
public static class PdfExport
{
    // ADR-0161 item 2: the points of one millimetre (72 points in 25.4 mm).
    public const double PointsPerMillimetre = 72 / 25.4;

    // ADR-0161 item 4: each Bézier curve of an arc is within this distance of the circle or the ellipse, in millimetres of the
    // paper. A part of a quarter turn is halved at most MaxDepth times.
    public const double Tolerance = 0.001;
    private const int MaxDepth = 12;

    // ADR-0161 item 6: the widths of the characters of Helvetica in thousandths of the size of the font, from its metrics,
    // for the characters of the value of a dimension; each other character takes the width of a digit. ADR-0165 item 4: the
    // sign Ø of a diameter, which WinAnsiEncoding holds in place of ⌀.
    private static readonly Dictionary<char, int> Widths = new()
    {
        ['0'] = 556, ['1'] = 556, ['2'] = 556, ['3'] = 556, ['4'] = 556, ['5'] = 556, ['6'] = 556, ['7'] = 556, ['8'] = 556, ['9'] = 556,
        ['.'] = 278, ['-'] = 333, ['+'] = 584, ['R'] = 722, ['\u00d8'] = 778, ['l'] = 222, ['o'] = 556, ['s'] = 500, ['t'] = 278, [' '] = 278,
    };

    // ADR-0169 item 6 (phase R30h, TASK-0411): the widths of the capital letters A to Z of Helvetica, in their order, for the
    // letters of a section. They are an array, because the gate of the codes refuses a literal of one capital letter that
    // begins a code (finding T91).
    private static readonly int[] Capitals = [667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778, 667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611];

    private const int DigitWidth = 556;

    // The file of the live sheet with the identifier, with each body that gives no lines, or E-SHEET-NOT-FOUND.
    public static (byte[]? Content, IReadOnlyList<SheetSkippedBody> Skipped, ErrorDetail? Error) Write(Document document, Guid sheetId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.Sheets.FirstOrDefault(sheet => sheet.Id == sheetId) is not { } record)
            return (null, [], new ErrorDetail(DiagnosticCodes.SheetNotFound, $"No live sheet with id '{sheetId}' exists in the document."));

        var sheet = GetSheetQueryHandler.Read(document, record, ct);
        return (Encoding.ASCII.GetBytes(Text(sheet)), [.. sheet.Views.SelectMany(view => view.Skipped)], null);
    }

    // ADR-0161 items 1 to 3: the text of the file. Each byte is ASCII, so the length of a stream is the length of its text.
    public static string Text(SheetInfo sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var content = Content(sheet);
        string[] objects =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {N(sheet.Width * PointsPerMillimetre)} {N(sheet.Height * PointsPerMillimetre)}] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            $"<< /Length {content.Length} >>\nstream\n{content}endstream",
            $"<< /Title {Literal(sheet.Name)} /Producer (3DEngine) >>",
        ];

        var w = new StringBuilder();
        w.Append("%PDF-1.4\n");
        var offsets = new List<int>();
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(w.Length);
            w.Append(CultureInfo.InvariantCulture, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        var table = w.Length;
        w.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
            w.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
        w.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R /Info 6 0 R >>\nstartxref\n{table}\n%%EOF\n");
        return w.ToString();
    }

    // ADR-0161 items 3 to 6: the content of the page, in millimetres of the paper with y up.
    private static string Content(SheetInfo sheet)
    {
        var w = new StringBuilder();
        w.Append(CultureInfo.InvariantCulture, $"{N(PointsPerMillimetre)} 0 0 {N(PointsPerMillimetre)} 0 0 cm\n");
        w.Append("0 G 0 g 0 J 0 j\n");

        // Item 5: the frame and the title block, as in the SVG file.
        var (fx, fy, fw, fh) = (SvgExport.Margin, SvgExport.Margin, sheet.Width - (2 * SvgExport.Margin), sheet.Height - (2 * SvgExport.Margin));
        var (tx, ty) = (fx + fw - SvgExport.TitleWidth, fy);
        w.Append(CultureInfo.InvariantCulture, $"{N(SvgExport.Thick)} w\n");
        w.Append(CultureInfo.InvariantCulture, $"{N(fx)} {N(fy)} {N(fw)} {N(fh)} re S\n");
        w.Append(CultureInfo.InvariantCulture, $"{N(tx)} {N(ty)} {N(SvgExport.TitleWidth)} {N(SvgExport.TitleHeight)} re S\n");
        Label(w, sheet.Name, tx + 3, ty + 12, 5, centred: false);
        var projection = sheet.ThirdAngle ? "Third angle" : "First angle";
        Label(w, $"{sheet.Paper} {(sheet.Landscape ? "landscape" : "portrait")} · {projection} · Scale {SvgExport.Scale(sheet)} · mm", tx + 3, ty + 4, 3.5, centred: false);

        // Item 3: the lines of each view, the hidden lines first, as in the SVG file.
        foreach (var view in sheet.Views)
        {
            w.Append(CultureInfo.InvariantCulture, $"% View {view.ViewId}.\n");
            w.Append(CultureInfo.InvariantCulture, $"{N(SvgExport.Thin)} w [{SvgExport.Dashes}] 0 d\n");
            foreach (var line in view.Lines.Where(line => SvgExport.Stroke(line) == SvgExport.LineStroke.Hidden))
                Path(w, line);
            // ADR-0165 item 3 (phase R30g, TASK-0405): the centre lines, thin with a long dash and a dot, as in the SVG file.
            // ADR-0169 item 6 (phase R30h, TASK-0411): the cut lines with them, then the hatch, the visible lines and the thick
            // ends, the arrows and the texts, in the order of the SVG file.
            w.Append(CultureInfo.InvariantCulture, $"{N(SvgExport.Thin)} w [{SvgExport.CentreDashes}] 0 d\n");
            foreach (var line in view.Lines.Where(line => SvgExport.Stroke(line) == SvgExport.LineStroke.Chain))
                Path(w, line);
            if (view.Lines.Any(line => SvgExport.Stroke(line) == SvgExport.LineStroke.Thin))
            {
                w.Append(CultureInfo.InvariantCulture, $"{N(SvgExport.Thin)} w [] 0 d\n");
                foreach (var line in view.Lines.Where(line => SvgExport.Stroke(line) == SvgExport.LineStroke.Thin))
                    Path(w, line);
            }

            w.Append(CultureInfo.InvariantCulture, $"{N(SvgExport.Thick)} w [] 0 d\n");
            foreach (var line in view.Lines.Where(line => SvgExport.Stroke(line) == SvgExport.LineStroke.Thick))
                Path(w, line);
            if (view.Lines.Any(line => SvgExport.Stroke(line) == SvgExport.LineStroke.Arrow))
            {
                w.Append(CultureInfo.InvariantCulture, $"{N(SvgExport.Thin)} w\n");
                foreach (var line in view.Lines.Where(line => SvgExport.Stroke(line) == SvgExport.LineStroke.Arrow))
                {
                    Path(w, line);
                    var head = SvgExport.Head(line);
                    w.Append(CultureInfo.InvariantCulture, $"{N(head[0].X)} {N(head[0].Y)} m {N(head[1].X)} {N(head[1].Y)} l {N(head[2].X)} {N(head[2].Y)} l h f\n");
                }
            }

            foreach (var text in view.Texts)
                Label(w, text.Text, text.X, text.Y, text.Size, centred: true);
            foreach (var skipped in view.Skipped)
                w.Append(CultureInfo.InvariantCulture, $"% The body {skipped.Body} gives no lines: {OneLine(skipped.Reason)}\n");
        }

        // Item 6: the value of each dimension, centred at its place, or "lost", as in the SVG file.
        foreach (var dimension in sheet.Dimensions)
            Label(w, SvgExport.Value(dimension, "\u00d8"), dimension.X, dimension.Y, 3.5, centred: true);
        return w.ToString();
    }

    // ADR-0161 item 4: a straight line, or the chain of Bézier curves of an arc of a circle or an ellipse from the start to the
    // end, counter-clockwise on the paper, or the whole curve when the start is the end.
    private static void Path(StringBuilder w, SheetLine line)
    {
        var (p, q) = ((line.FromX, line.FromY), (line.ToX, line.ToY));
        IReadOnlyList<(double X, double Y)>? points = line.Circle is { } circle
            ? Arc((circle.X, circle.Y), (circle.Radius, 0), (0, circle.Radius), p, q)
            : line.Ellipse is { } ellipse ? Arc((ellipse.X, ellipse.Y), (ellipse.MajorX, ellipse.MajorY), (ellipse.MinorX, ellipse.MinorY), p, q)
            : null;
        if (points is null)
        {
            w.Append(CultureInfo.InvariantCulture, $"{N(p.Item1)} {N(p.Item2)} m {N(q.Item1)} {N(q.Item2)} l S\n");
            return;
        }

        w.Append(CultureInfo.InvariantCulture, $"{N(points[0].X)} {N(points[0].Y)} m");
        for (var i = 1; i + 2 < points.Count; i += 3)
            w.Append(CultureInfo.InvariantCulture, $" {N(points[i].X)} {N(points[i].Y)} {N(points[i + 1].X)} {N(points[i + 1].Y)} {N(points[i + 2].X)} {N(points[i + 2].Y)} c");
        w.Append(" S\n");
    }

    // ADR-0161 item 4: the points of the chain of Bézier curves of the arc of C + x M + y N from p to q, counter-clockwise on the
    // paper, for (x, y) on the unit circle: the start, then three points for each curve. N is turned to the side of the
    // paper's counter-clockwise turn from M. The arc has a stop at each quarter turn from the start, so each part is a quarter
    // turn or less; each part is halved at the unit vector (u + v) / |u + v| until 0.1 · a · t⁶ ≤ Tolerance / 2, with a the
    // larger half axis and t = tan(θ/4) of the part. A curve of a part from u to v has the control points u + k u⊥ and
    // v − k v⊥, with k = 4/3 · t. Start equal to end is the whole curve; an end in the direction of the start, but not equal
    // to it, is a whole turn less the rounding. The first and the last point are the ends of the line.
    public static IReadOnlyList<(double X, double Y)> Arc((double X, double Y) c, (double X, double Y) m, (double X, double Y) n, (double X, double Y) p, (double X, double Y) q)
    {
        if (Cross(m, n) < 0)
            n = (-n.X, -n.Y);
        var (mm, nn) = (Dot(m, m), Dot(n, n));
        var whole = p == q;
        if (!(mm > 0 && nn > 0))
            return [p, p, q, q];

        (double X, double Y) Frame((double X, double Y) point)
        {
            var d = (point.X - c.X, point.Y - c.Y);
            return Unit((Dot(d, m) / mm, Dot(d, n) / nn));
        }

        (double X, double Y) Paper((double X, double Y) u)
            => (c.X + (u.X * m.X) + (u.Y * n.X), c.Y + (u.X * m.Y) + (u.Y * n.Y));

        (double X, double Y) Vector((double X, double Y) u) => ((u.X * m.X) + (u.Y * n.X), (u.X * m.Y) + (u.Y * n.Y));

        var a = Frame(p);
        var b = whole ? a : Frame(q);
        var largest = Math.Sqrt(Math.Max(mm, nn));

        // The stops: a, then each quarter turn, up to the quarter that holds b. When no quarter holds b, the whole curve or an
        // end in the direction of the start, the fourth quarter ends at a, and its last point becomes the end of the line.
        var stops = new List<(double X, double Y)> { a };
        var current = a;
        for (var quarter = 0; quarter < 4; quarter++)
        {
            var next = (-current.Y, current.X);
            if (!whole && Cross(current, b) > 0 && Dot(current, b) >= 0)
            {
                stops.Add(b);
                break;
            }

            stops.Add(next);
            current = next;
        }

        var points = new List<(double X, double Y)> { p };
        for (var i = 0; i + 1 < stops.Count; i++)
            Part(stops[i], stops[i + 1], 0);
        points[^1] = whole ? p : q;
        return points;

        void Part((double X, double Y) u, (double X, double Y) v, int depth)
        {
            var h = Math.Sqrt(Dot((v.X - u.X, v.Y - u.Y), (v.X - u.X, v.Y - u.Y))) / 2;
            var t = h / (1 + Math.Sqrt(Math.Max(0, 1 - (h * h))));
            if (depth < MaxDepth && 0.1 * largest * t * t * t * t * t * t > Tolerance / 2)
            {
                var middle = Unit((u.X + v.X, u.Y + v.Y));
                Part(u, middle, depth + 1);
                Part(middle, v, depth + 1);
                return;
            }

            var k = 4.0 / 3 * t;
            var (du, dv) = (Vector((-u.Y, u.X)), Vector((-v.Y, v.X)));
            var (pu, pv) = (Paper(u), Paper(v));
            points.Add((pu.X + (k * du.X), pu.Y + (k * du.Y)));
            points.Add((pv.X - (k * dv.X), pv.Y - (k * dv.Y)));
            points.Add(pv);
        }
    }

    // ADR-0161 item 6: a text of Helvetica at its place; a centred text has its middle at the place on its base line, with the
    // width from the metrics of the font.
    private static void Label(StringBuilder w, string text, double x, double y, double size, bool centred)
    {
        var start = centred ? x - (Width(text) * size / 2000) : x;
        w.Append(CultureInfo.InvariantCulture, $"BT /F1 {N(size)} Tf {N(start)} {N(y)} Td {Literal(text)} Tj ET\n");
    }

    // The width of a text in thousandths of the size of the font.
    public static int Width(string text)
        => text.Sum(ch => ch is >= 'A' and <= 'Z' ? Capitals[ch - 'A'] : Widths.TryGetValue(ch, out var width) ? width : DigitWidth);

    // ADR-0161 item 7: a string of PDF in WinAnsiEncoding, in ASCII: a parenthesis and a backslash have a backslash before them,
    // a character from U+00A0 to U+00FF is its byte in octal, and each other character outside printable ASCII is "?".
    public static string Literal(string text)
    {
        var s = new StringBuilder("(");
        foreach (var ch in text)
        {
            if (ch is '(' or ')' or '\\')
                s.Append('\\').Append(ch);
            else if (ch is >= ' ' and <= '~')
                s.Append(ch);
            else if (ch is >= '\u00a0' and <= '\u00ff')
                s.Append('\\').Append(Convert.ToString(ch, 8));
            else if (!char.IsLowSurrogate(ch))
                s.Append('?');
        }

        return s.Append(')').ToString();
    }

    // A text on one line of a comment.
    private static string OneLine(string text) => new([.. text.Select(ch => ch is >= ' ' and <= '~' ? ch : ' ')]);

    // ADR-0161 item 7: a number of PDF has no exponent. The shortest text that reads back as the same double, with its digits
    // in fixed notation when that text has an exponent; a number that is not finite is 0, which a reader of PDF can read.
    public static string N(double value) => double.IsFinite(value) ? DxfExport.N(value) : "0";

    private static double Cross((double X, double Y) a, (double X, double Y) b) => (a.X * b.Y) - (a.Y * b.X);

    private static double Dot((double X, double Y) a, (double X, double Y) b) => (a.X * b.X) + (a.Y * b.Y);

    private static (double X, double Y) Unit((double X, double Y) a)
    {
        var length = Math.Sqrt(Dot(a, a));
        return (a.X / length, a.Y / length);
    }
}
