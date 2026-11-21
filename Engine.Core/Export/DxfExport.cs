using System.Globalization;
using System.Text;
using Engine.Contracts;
using Engine.Core.Drawing;
using Engine.Core.Queries;

namespace Engine.Core.Export;

// ADR-0160 (phase P10b, TASK-0398): one sheet of a Document as one DXF file of release 12 (AC1009), from the read GetSheet. A
// DXF file is presentation, as a tessellation is: it never enters a command, an event, the Document or the log (CLAUDE.md,
// determinism rule 6). The unit is the millimetre of the paper. The code uses + − × ÷ and sqrt only: an arc is a polyline
// vertex with a bulge, which needs no angle, so the file is the same on each platform.
public static class DxfExport
{
    // ADR-0160 item 3: the four layers, each with its colour (the index of AutoCAD) and its line type.
    public const string FrameLayer = "FRAME";
    public const string VisibleLayer = "VISIBLE";
    public const string HiddenLayer = "HIDDEN";
    public const string TextLayer = "TEXT";

    // ADR-0165 item 3 (phase R30g, TASK-0405): the layer of the centre lines, with the colour 8 and the line type CENTER.
    public const string CentreLayer = "CENTER";

    // ADR-0169 item 6 (phase R30h, TASK-0411): the layer of the hatch, with the colour 8, and the layer of the cut lines, with
    // the colour 7 and the line type CENTER.
    public const string HatchLayer = "HATCH";
    public const string CutLayer = "CUT";

    // ADR-0160 item 4: an arc of an ellipse is a chain of arcs of circles, each within this distance of the ellipse, in
    // millimetres of the paper. The chain stops at MaxDepth halvings of a quarter of a turn.
    public const double Tolerance = 0.001;
    private const double Check = Tolerance / 2;
    private const int MaxDepth = 12;

    // The file of the live sheet with the identifier, with each body that gives no lines, or E-SHEET-NOT-FOUND.
    public static (byte[]? Content, IReadOnlyList<SheetSkippedBody> Skipped, ErrorDetail? Error) Write(Document document, Guid sheetId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.Sheets.FirstOrDefault(sheet => sheet.Id == sheetId) is not { } record)
            return (null, [], new ErrorDetail(DiagnosticCodes.SheetNotFound, $"No live sheet with id '{sheetId}' exists in the document."));

        var sheet = GetSheetQueryHandler.Read(document, record, ct);
        return (Encoding.ASCII.GetBytes(Text(sheet)), [.. sheet.Views.SelectMany(view => view.Skipped)], null);
    }

    // ADR-0160 items 1 to 7: the text of the file.
    public static string Text(SheetInfo sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var w = new Writer();

        // Item 2: the header gives the release, the extent of the paper and the millimetre.
        w.Pair(0, "SECTION").Pair(2, "HEADER");
        w.Pair(9, "$ACADVER").Pair(1, "AC1009");
        w.Pair(9, "$INSBASE").Point(0, 0);
        w.Pair(9, "$EXTMIN").Point(0, 0);
        w.Pair(9, "$EXTMAX").Point(sheet.Width, sheet.Height);
        w.Pair(9, "$LIMMIN").Pair(10, 0).Pair(20, 0);
        w.Pair(9, "$LIMMAX").Pair(10, sheet.Width).Pair(20, sheet.Height);
        w.Pair(9, "$INSUNITS").Pair(70, "4");
        w.Pair(9, "$MEASUREMENT").Pair(70, "1");
        w.Pair(0, "ENDSEC");

        // Item 3: the line types, the layers and the style of the text.
        w.Pair(0, "SECTION").Pair(2, "TABLES");
        w.Pair(0, "TABLE").Pair(2, "LTYPE").Pair(70, "3");
        w.Pair(0, "LTYPE").Pair(2, "CONTINUOUS").Pair(70, "0").Pair(3, "Solid line").Pair(72, "65").Pair(73, "0").Pair(40, 0);
        w.Pair(0, "LTYPE").Pair(2, "HIDDEN").Pair(70, "0").Pair(3, "Hidden 3 0.75").Pair(72, "65").Pair(73, "2").Pair(40, 3.75).Pair(49, 3).Pair(49, -0.75);
        // ADR-0165 item 3 (phase R30g, TASK-0405): the line type of a centre line, as in the SVG file.
        w.Pair(0, "LTYPE").Pair(2, "CENTER").Pair(70, "0").Pair(3, "Center 6 0.75 0.125 0.75").Pair(72, "65").Pair(73, "4").Pair(40, 7.625).Pair(49, 6).Pair(49, -0.75).Pair(49, 0.125).Pair(49, -0.75);
        w.Pair(0, "ENDTAB");
        w.Pair(0, "TABLE").Pair(2, "LAYER").Pair(70, "8");
        foreach (var (name, colour, type) in new[] { ("0", "7", "CONTINUOUS"), (FrameLayer, "7", "CONTINUOUS"), (VisibleLayer, "7", "CONTINUOUS"), (HiddenLayer, "8", "HIDDEN"), (CentreLayer, "8", "CENTER"), (HatchLayer, "8", "CONTINUOUS"), (CutLayer, "7", "CENTER"), (TextLayer, "7", "CONTINUOUS") })
            w.Pair(0, "LAYER").Pair(2, name).Pair(70, "0").Pair(62, colour).Pair(6, type);
        w.Pair(0, "ENDTAB");
        w.Pair(0, "TABLE").Pair(2, "STYLE").Pair(70, "1");
        w.Pair(0, "STYLE").Pair(2, "STANDARD").Pair(70, "0").Pair(40, 0).Pair(41, 1).Pair(50, 0).Pair(71, "0").Pair(42, 3.5).Pair(3, "txt").Pair(4, "");
        w.Pair(0, "ENDTAB");
        w.Pair(0, "ENDSEC");
        w.Pair(0, "SECTION").Pair(2, "BLOCKS").Pair(0, "ENDSEC");

        w.Pair(0, "SECTION").Pair(2, "ENTITIES");

        // Item 5: the frame and the title block, as in the SVG file. ADR-0179 items 4 to 6 (phase R30i, TASK-0428): each line of
        // TitleBlock.Layout on the layer FRAME; a centre line of the symbol has the line type CENTER of its own (group 6). The
        // texts are on the layer TEXT with the height of a capital letter.
        var block = TitleBlock.Layout(sheet);
        foreach (var line in block.Lines)
        {
            if (SvgExport.Stroke(line) == SvgExport.LineStroke.Chain)
                w.Pair(0, "LINE").Pair(8, FrameLayer).Pair(6, "CENTER").Point(line.FromX, line.FromY).Point(line.ToX, line.ToY, 11);
            else
                Entity(w, line, FrameLayer);
        }

        // Finding E228 of the codebase review of 2026-11-21 (TASK-0431): "%%" starts a control code of a TEXT, so "50%%d" of a field
        // showed "50°". Each "%" of a text of the block is "%%%", the code of one "%". The "%%c" of a diameter is not in the block.
        foreach (var text in block.Texts)
            Label(w, text.Text.Replace("%", "%%%", StringComparison.Ordinal), text.X, text.Y, text.Size, centred: false);

        // Item 4: the lines of each view, the hidden lines first, as in the SVG file.
        foreach (var view in sheet.Views)
        {
            w.Comment($"View {view.ViewId}.");
            foreach (var line in view.Lines.Where(line => SvgExport.Stroke(line) == SvgExport.LineStroke.Hidden))
                Entity(w, line, HiddenLayer);
            // ADR-0169 item 6 (phase R30h, TASK-0411): the cut lines after the centre lines, then the hatch, the visible lines and
            // the thick ends, the arrows and the texts, in the order of the SVG file.
            foreach (var line in view.Lines.Where(line => SvgExport.Stroke(line) == SvgExport.LineStroke.Chain))
                Entity(w, line, line.Kind == "cut" ? CutLayer : CentreLayer);
            foreach (var line in view.Lines.Where(line => SvgExport.Stroke(line) == SvgExport.LineStroke.Thin))
                Entity(w, line, HatchLayer);
            foreach (var line in view.Lines.Where(line => SvgExport.Stroke(line) == SvgExport.LineStroke.Thick))
                Entity(w, line, VisibleLayer);
            foreach (var line in view.Lines.Where(line => SvgExport.Stroke(line) == SvgExport.LineStroke.Arrow))
            {
                Entity(w, line, TextLayer);
                var head = SvgExport.Head(line);
                // A SOLID of three corners repeats its third corner as its fourth.
                w.Pair(0, "SOLID").Pair(8, TextLayer).Point(head[0].X, head[0].Y).Point(head[1].X, head[1].Y, 11).Point(head[2].X, head[2].Y, 12).Point(head[2].X, head[2].Y, 13);
            }

            foreach (var text in view.Texts)
                Label(w, text.Text, text.X, text.Y, text.Size, centred: true);
            foreach (var skipped in view.Skipped)
                w.Comment($"The body {skipped.Body} gives no lines: {skipped.Reason}");
        }

        // Item 6: the value of each dimension, centred at its place, or "lost", as in the SVG file.
        foreach (var dimension in sheet.Dimensions)
            Label(w, SvgExport.Value(dimension, "%%c"), dimension.X, dimension.Y, 3.5, centred: true);

        w.Pair(0, "ENDSEC");
        w.Pair(0, "EOF");
        return w.ToString();
    }

    // ADR-0160 item 4: a straight line is a LINE, a whole circle a CIRCLE, and an arc of a circle or an ellipse a POLYLINE whose
    // vertices carry the bulge of each arc: the tangent of a quarter of its angle, positive counter-clockwise.
    private static void Entity(Writer w, SheetLine line, string layer)
    {
        var whole = line.FromX == line.ToX && line.FromY == line.ToY;
        if (line.Circle is { } circle)
        {
            if (whole)
            {
                w.Pair(0, "CIRCLE").Pair(8, layer).Point(circle.X, circle.Y).Pair(40, circle.Radius);
                return;
            }

            Polyline(w, layer, CircleArc((line.FromX, line.FromY), (line.ToX, line.ToY), (circle.X, circle.Y), circle.Radius));
            return;
        }

        if (line.Ellipse is { } ellipse)
        {
            Polyline(w, layer, EllipseArc(ellipse, (line.FromX, line.FromY), (line.ToX, line.ToY), whole));
            return;
        }

        w.Pair(0, "LINE").Pair(8, layer).Point(line.FromX, line.FromY).Point(line.ToX, line.ToY, 11);
    }

    // A polyline from the first vertex to the last; each vertex but the last has the bulge of the arc that starts at it.
    private static void Polyline(Writer w, string layer, IReadOnlyList<(double X, double Y, double Bulge)> vertices)
    {
        w.Pair(0, "POLYLINE").Pair(8, layer).Pair(66, "1").Point(0, 0).Pair(70, "0");
        foreach (var (x, y, bulge) in vertices)
        {
            w.Pair(0, "VERTEX").Pair(8, layer).Point(x, y);
            if (bulge != 0)
                w.Pair(42, bulge);
        }

        w.Pair(0, "SEQEND").Pair(8, layer);
    }

    // ADR-0160 item 4: the arc of a circle from p to q, counter-clockwise about c. With the half chord h and d = sqrt(r² − h²),
    // the bulge is h / (r + d) for an arc of less than a half turn and (r + d) / h for a larger arc. An arc of a half turn or
    // more is two arcs, at the middle of the arc, so each bulge is at most 1.
    public static IReadOnlyList<(double X, double Y, double Bulge)> CircleArc((double X, double Y) p, (double X, double Y) q, (double X, double Y) c, double r)
    {
        var (u, v) = ((p.X - c.X, p.Y - c.Y), (q.X - c.X, q.Y - c.Y));
        var cross = Cross(u, v);
        if (cross > 0)
            return [(p.X, p.Y, MinorBulge(p, q, r)), (q.X, q.Y, 0)];

        // The middle of an arc of a half turn or more is on the side away from the chord: -(u + v), or u turned by a quarter
        // turn when u + v is zero.
        var sum = (u.Item1 + v.Item1, u.Item2 + v.Item2);
        var length = Math.Sqrt(Dot(sum, sum));
        var m = length > 0 ? (c.X - (r * sum.Item1 / length), c.Y - (r * sum.Item2 / length)) : (c.X - (r * u.Item2 / Math.Sqrt(Dot(u, u))), c.Y + (r * u.Item1 / Math.Sqrt(Dot(u, u))));
        return [(p.X, p.Y, MinorBulge(p, m, r)), (m.Item1, m.Item2, MinorBulge(m, q, r)), (q.X, q.Y, 0)];
    }

    private static double MinorBulge((double X, double Y) p, (double X, double Y) q, double r)
    {
        var (dx, dy) = (q.X - p.X, q.Y - p.Y);
        var h = Math.Sqrt((dx * dx) + (dy * dy)) / 2;
        var d = Math.Sqrt(Math.Max(0, (r * r) - (h * h)));
        return h / (r + d);
    }

    // ADR-0160 item 4: the arc of an ellipse from p to q, counter-clockwise on the paper, or the whole ellipse. In the frame of
    // the ellipse, a point is C + x M + y N with (x, y) on the unit circle, and N turns counter-clockwise from M. The arc is cut
    // into parts of less than a half turn of the unit circle, and each part is halved at the unit vector (a + b) / |a + b|
    // until the arc of a circle through its two ends and its middle is within the tolerance of the ellipse.
    public static IReadOnlyList<(double X, double Y, double Bulge)> EllipseArc(SheetEllipse ellipse, (double X, double Y) p, (double X, double Y) q, bool whole)
    {
        var m = (ellipse.MajorX, ellipse.MajorY);
        var n = (ellipse.MinorX, ellipse.MinorY);
        if (Cross(m, n) < 0)
            n = (-n.Item1, -n.Item2);
        var c = (ellipse.X, ellipse.Y);
        var (mm, nn) = (Dot(m, m), Dot(n, n));
        if (mm == 0 || nn == 0)
            return [(p.X, p.Y, 0), (q.X, q.Y, 0)];

        (double X, double Y) Frame((double X, double Y) point)
        {
            var d = (point.X - c.Item1, point.Y - c.Item2);
            return Unit((Dot(d, m) / mm, Dot(d, n) / nn));
        }

        (double X, double Y) Paper((double X, double Y) unit)
            => (c.Item1 + (unit.X * m.Item1) + (unit.Y * n.Item1), c.Item2 + (unit.X * m.Item2) + (unit.Y * n.Item2));

        var a = Frame(p);
        var b = whole ? a : Frame(q);

        // ADR-0164 item 3 (TASK-0400, finding E216 of the codebase review of 2026-11-17): a stop at each quarter turn from a,
        // up to the quarter that holds b, as in the PDF file. When no quarter holds b, the whole curve or an
        // end in the direction of a, the fourth quarter ends at a, and the last point becomes the end of the line. Until
        // TASK-0400 an end within the rounding of a gave a last part of a half turn, whose middle was the unit of (0, 0).
        var stops = new List<(double X, double Y)> { a };
        var current = a;
        for (var quarter = 0; quarter < 4; quarter++)
        {
            if (!whole && Cross(current, b) > 0 && Dot(current, b) >= 0)
            {
                stops.Add(b);
                break;
            }

            current = (-current.Y, current.X);
            stops.Add(current);
        }

        var vertices = new List<(double X, double Y, double Bulge)>();
        for (var i = 0; i + 1 < stops.Count; i++)
        {
            if (stops[i] == stops[i + 1])
                continue;
            Part(stops[i], stops[i + 1], 0);
        }

        // The first point and the last point are the ends of the line, so the chain meets the lines next to it.
        var last = whole ? p : q;
        vertices[0] = (p.X, p.Y, vertices[0].Bulge);
        vertices.Add((last.X, last.Y, 0));
        return vertices;

        void Part((double X, double Y) from, (double X, double Y) to, int depth)
        {
            var middle = Unit((from.X + to.X, from.Y + to.Y));
            var (p0, pm, p1) = (Paper(from), Paper(middle), Paper(to));
            var bulge = Bulge(p0, pm, p1);
            if (depth < MaxDepth && Deviation(p0, pm, p1, Paper(Unit((from.X + middle.X, from.Y + middle.Y))), Paper(Unit((middle.X + to.X, middle.Y + to.Y)))) > Check)
            {
                Part(from, middle, depth + 1);
                Part(middle, to, depth + 1);
                return;
            }

            vertices.Add((p0.X, p0.Y, bulge));
        }
    }

    // The bulge of the arc of a circle from p0 through pm to p1. With a = p0 − pm and b = p1 − pm, the angle α at pm gives the
    // angle 2 (π − α) of the arc, so the bulge is cot(α / 2) = sin α / (1 − cos α) = (b × a) / (|a| |b| − a · b), positive
    // counter-clockwise. Collinear points give 0, a straight line.
    public static double Bulge((double X, double Y) p0, (double X, double Y) pm, (double X, double Y) p1)
    {
        var a = (p0.X - pm.X, p0.Y - pm.Y);
        var b = (p1.X - pm.X, p1.Y - pm.Y);
        var denominator = (Math.Sqrt(Dot(a, a)) * Math.Sqrt(Dot(b, b))) - Dot(a, b);
        return denominator > 0 ? Cross(b, a) / denominator : 0;
    }

    // The larger distance of the two points of the ellipse at a quarter and three quarters of the part from the circle through
    // p0, pm and p1, or from the chord when the three points are on one line.
    private static double Deviation((double X, double Y) p0, (double X, double Y) pm, (double X, double Y) p1, (double X, double Y) q1, (double X, double Y) q3)
    {
        var (ax, ay) = (pm.X - p0.X, pm.Y - p0.Y);
        var (bx, by) = (p1.X - p0.X, p1.Y - p0.Y);
        var det = 2 * ((ax * by) - (ay * bx));
        if (det == 0)
        {
            var length = Math.Sqrt((bx * bx) + (by * by));
            double Off((double X, double Y) s) => length == 0 ? 0 : Math.Abs(((s.X - p0.X) * by) - ((s.Y - p0.Y) * bx)) / length;
            return Math.Max(Off(q1), Off(q3));
        }

        // The centre of the circle through p0, pm and p1, relative to p0.
        var (aa, bb) = ((ax * ax) + (ay * ay), (bx * bx) + (by * by));
        var (ox, oy) = (((by * aa) - (ay * bb)) / det, ((ax * bb) - (bx * aa)) / det);
        var radius = Math.Sqrt((ox * ox) + (oy * oy));
        double From((double X, double Y) s)
        {
            var (dx, dy) = (s.X - p0.X - ox, s.Y - p0.Y - oy);
            return Math.Abs(Math.Sqrt((dx * dx) + (dy * dy)) - radius);
        }

        return Math.Max(From(q1), From(q3));
    }

    // ADR-0160 item 6: a TEXT on the layer TEXT. A centred text has its middle at the place on its base line, as in the SVG file.
    private static void Label(Writer w, string text, double x, double y, double height, bool centred)
    {
        w.Pair(0, "TEXT").Pair(8, TextLayer).Point(x, y).Pair(40, height).Pair(1, Escape(text)).Pair(7, "STANDARD");
        if (centred)
            w.Pair(72, "1").Point(x, y, 11);
    }

    // ADR-0160 item 7: the file is ASCII. A character outside printable ASCII is \U+XXXX, as AutoCAD reads it; a character of
    // control is a space, and a pair of surrogates is "?", which the form \U+XXXX cannot hold.
    public static string Escape(string text)
    {
        var s = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (ch is >= ' ' and <= '~')
                s.Append(ch);
            else if (char.IsControl(ch))
                s.Append(' ');
            else if (char.IsSurrogate(ch))
                s.Append(char.IsHighSurrogate(ch) ? "?" : "");
            else
                s.Append(CultureInfo.InvariantCulture, $"\\U+{(int)ch:X4}");
        }

        return s.ToString();
    }

    // ADR-0160 item 7: a number as the shortest text that reads back as the same double, with the culture of no country; a
    // negative zero is 0. When that text has an exponent, its digits move to fixed notation, so a reader of DXF that does
    // not read an exponent reads the same number.
    public static string N(double value)
    {
        // ADR-0164 item 2 (TASK-0400, finding E215 of the codebase review of 2026-11-17): SvgExport.N gives 0 for a number that
        // is not finite, so no letter of "NaN" or "Infinity" reaches the search of an exponent. Until TASK-0400 the N of "NaN"
        // and the I of "Infinity" looked like an exponent, and the writer threw.
        var text = SvgExport.N(value);
        var e = text.AsSpan().IndexOfAnyInRange('A', 'Z');
        if (e < 0)
            return text;

        var sign = text[0] == '-' ? "-" : "";
        var mantissa = text[sign.Length..e];
        var exponent = int.Parse(text[(e + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        var point = mantissa.IndexOf('.', StringComparison.Ordinal);
        var digits = mantissa.Replace(".", "", StringComparison.Ordinal);
        var place = (point < 0 ? mantissa.Length : point) + exponent;
        var fixedText = place <= 0 ? "0." + new string('0', -place) + digits
            : place >= digits.Length ? digits + new string('0', place - digits.Length)
            : digits[..place] + "." + digits[place..];
        return sign + fixedText;
    }

    private static double Cross((double X, double Y) a, (double X, double Y) b) => (a.X * b.Y) - (a.Y * b.X);

    private static double Dot((double X, double Y) a, (double X, double Y) b) => (a.X * b.X) + (a.Y * b.Y);

    private static (double X, double Y) Unit((double X, double Y) a)
    {
        var length = Math.Sqrt(Dot(a, a));
        return (a.X / length, a.Y / length);
    }

    // The pairs of the file: a group code on one line, its value on the next line.
    private sealed class Writer
    {
        private readonly StringBuilder _text = new();

        public Writer Pair(int code, string value)
        {
            _text.Append(code.ToString(CultureInfo.InvariantCulture)).Append('\n').Append(value).Append('\n');
            return this;
        }

        public Writer Pair(int code, double value) => Pair(code, N(value));

        // A point of the plane of the paper: the codes 10, 20 and 30, or 11, 21 and 31.
        public Writer Point(double x, double y, int code = 10) => Pair(code, x).Pair(code + 10, y).Pair(code + 20, 0);

        // A comment of the group 999, on one line.
        public Writer Comment(string text) => Pair(999, Escape(text));

        public override string ToString() => _text.ToString();
    }
}
