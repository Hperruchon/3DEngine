using System.Globalization;
using System.Text;
using Engine.Contracts;
using Engine.Core.Queries;

namespace Engine.Core.Export;

// ADR-0159 (phase P10a, TASK-0396): one sheet of a Document as one SVG file, from the read GetSheet. An SVG file is
// presentation, as a tessellation is: it never enters a command, an event, the Document or the log (CLAUDE.md, determinism
// rule 6). The unit is the millimetre of the paper, and the code uses + − × ÷ and sqrt only, so the file is the same on each
// platform.
public static class SvgExport
{
    // ADR-0159 item 3: the widths of the lines in millimetres (ISO 128-20: a thick line of 0.5 and a thin line of 0.25), and a
    // hidden line of dashes of 12 widths with gaps of 3 widths.
    public const double Thick = 0.5;
    public const double Thin = 0.25;
    public const string Dashes = "3 0.75";

    // ADR-0165 item 3 (phase R30g, TASK-0405): a centre line of ISO 128, type 04, thin: a long dash of 24 widths, a gap of 3, a
    // dot of half a width, and a gap of 3.
    public const string CentreDashes = "6 0.75 0.125 0.75";

    // ADR-0159 item 4: the frame is 10 mm inside the edge of the paper, and the title block is 120 by 20 mm in the lower right
    // corner of the frame.
    public const double Margin = 10;
    public const double TitleWidth = 120;
    public const double TitleHeight = 20;

    // The file of the live sheet with the identifier, with each body that gives no lines, or E-SHEET-NOT-FOUND.
    public static (byte[]? Content, IReadOnlyList<SheetSkippedBody> Skipped, ErrorDetail? Error) Write(Document document, Guid sheetId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.Sheets.FirstOrDefault(sheet => sheet.Id == sheetId) is not { } record)
            return (null, [], new ErrorDetail(DiagnosticCodes.SheetNotFound, $"No live sheet with id '{sheetId}' exists in the document."));

        var sheet = GetSheetQueryHandler.Read(document, record, ct);
        return (Encoding.UTF8.GetBytes(Text(sheet)), [.. sheet.Views.SelectMany(view => view.Skipped)], null);
    }

    // ADR-0159 items 2 to 6: the text of the file.
    public static string Text(SheetInfo sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var svg = new Svg(sheet.Height);
        var w = new StringBuilder();
        w.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        w.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{N(sheet.Width)}mm\" height=\"{N(sheet.Height)}mm\" viewBox=\"0 0 {N(sheet.Width)} {N(sheet.Height)}\">\n");
        w.Append($"<title>{Escape(sheet.Name)}</title>\n");
        w.Append($"<desc>3DEngine SVG export of a sheet, millimetres. Sheet {sheet.SheetId}.</desc>\n");

        // Item 4: the frame and the title block.
        var (fx, fy, fw, fh) = (Margin, Margin, sheet.Width - (2 * Margin), sheet.Height - (2 * Margin));
        w.Append($"<g id=\"frame\" fill=\"none\" stroke=\"black\" stroke-width=\"{N(Thick)}\">\n");
        w.Append($"<rect x=\"{N(fx)}\" y=\"{N(svg.Y(fy + fh))}\" width=\"{N(fw)}\" height=\"{N(fh)}\"/>\n");
        var (tx, ty) = (fx + fw - TitleWidth, fy);
        w.Append($"<rect x=\"{N(tx)}\" y=\"{N(svg.Y(ty + TitleHeight))}\" width=\"{N(TitleWidth)}\" height=\"{N(TitleHeight)}\"/>\n");
        w.Append("</g>\n");
        w.Append("<g id=\"title\" font-family=\"sans-serif\" fill=\"black\">\n");
        w.Append($"<text x=\"{N(tx + 3)}\" y=\"{N(svg.Y(ty + 12))}\" font-size=\"5\">{Escape(sheet.Name)}</text>\n");
        var projection = sheet.ThirdAngle ? "Third angle" : "First angle";
        w.Append($"<text x=\"{N(tx + 3)}\" y=\"{N(svg.Y(ty + 4))}\" font-size=\"3.5\">{Escape($"{sheet.Paper} {(sheet.Landscape ? "landscape" : "portrait")} · {projection} · Scale {Scale(sheet)} · mm")}</text>\n");
        w.Append("</g>\n");

        // Items 2 and 3: the lines of each view, the hidden lines first, so a visible line covers a hidden line.
        foreach (var view in sheet.Views)
        {
            w.Append($"<g id=\"view-{view.ViewId}\" fill=\"none\" stroke=\"black\">\n");
            foreach (var line in view.Lines.Where(line => !line.Visible))
                w.Append($"<path stroke-width=\"{N(Thin)}\" stroke-dasharray=\"{Dashes}\" {svg.Path(line)}/>\n");
            // ADR-0165 item 3 (phase R30g, TASK-0405): the centre lines, thin with a long dash and a dot, between the hidden and
            // the visible lines.
            foreach (var line in view.Lines.Where(line => line.Kind == "centre"))
                w.Append($"<path stroke-width=\"{N(Thin)}\" stroke-dasharray=\"{CentreDashes}\" {svg.Path(line)}/>\n");
            foreach (var line in view.Lines.Where(line => line.Visible && line.Kind != "centre"))
                w.Append($"<path stroke-width=\"{N(Thick)}\" {svg.Path(line)}/>\n");
            foreach (var skipped in view.Skipped)
                w.Append($"<desc>The body {skipped.Body} gives no lines: {Escape(skipped.Reason)}</desc>\n");
            w.Append("</g>\n");
        }

        // Item 5: the value of each dimension at its place, or "lost".
        w.Append("<g id=\"dimensions\" font-family=\"sans-serif\" font-size=\"3.5\" fill=\"black\" text-anchor=\"middle\">\n");
        foreach (var dimension in sheet.Dimensions)
            w.Append($"<text id=\"dimension-{dimension.DimensionId}\" x=\"{N(dimension.X)}\" y=\"{N(svg.Y(dimension.Y))}\">{Escape(Value(dimension))}</text>\n");
        w.Append("</g>\n");
        w.Append("</svg>\n");
        return w.ToString();
    }

    // ADR-0159 item 5: the text of a dimension. A radius has the prefix "R" (ISO 129-1); a lost dimension shows "lost". ADR-0165
    // item 4 (phase R30g, TASK-0405): a diameter has the sign of the diameter, \u2300 in SVG by default; the DXF and the PDF
    // file give their own sign.
    public static string Value(SheetDimensionInfo dimension, string diameter = "\u2300")
        => dimension.Value is not { } value ? "lost" : (dimension.Label switch { "radius" => "R", "diameter" => diameter, _ => "" }) + N(value);

    // ADR-0159 item 4: the scale of the title block: the scale of each view when they have one scale, or "as shown". The DXF
    // file gives the same text (ADR-0160 item 5).
    public static string Scale(SheetInfo sheet)
    {
        var scales = sheet.Views.Select(view => Reduced(view.ScaleNumerator, view.ScaleDenominator)).Distinct().ToList();
        return scales.Count == 1 ? $"{scales[0].Numerator}:{scales[0].Denominator}" : "as shown";
    }

    private static (long Numerator, long Denominator) Reduced(long numerator, long denominator)
    {
        var (a, b) = (numerator, denominator);
        while (b != 0)
            (a, b) = (b, a % b);
        return (numerator / a, denominator / a);
    }

    // ADR-0159 item 6: a number as the shortest text that reads back as the same double, with the culture of no country. A
    // negative zero is 0. ADR-0164 item 2 (TASK-0400, finding E215 of the codebase review of 2026-11-17): a number that is not
    // finite is 0, as in the DXF and the PDF file.
    public static string N(double value) => value == 0 || !double.IsFinite(value) ? "0" : value.ToString("R", CultureInfo.InvariantCulture);

    // ADR-0164 item 4 (TASK-0400, finding E217 of the codebase review of 2026-11-17): a character that XML 1.0 does not permit
    // is the replacement character U+FFFD: a character of control other than the tab, the line feed and the return, U+FFFE,
    // U+FFFF, and a surrogate with no pair. The rules of a name accept U+FFFE and U+FFFF, so until TASK-0400 such a name gave
    // a file that was not XML.
    private static string Escape(string text)
    {
        var s = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (char.IsHighSurrogate(ch) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                s.Append(ch).Append(text[++i]);
                continue;
            }

            var permitted = ch is '\t' or '\n' or '\r' || (ch >= ' ' && !char.IsSurrogate(ch) && ch != (char)0xFFFE && ch != (char)0xFFFF);
            s.Append(!permitted ? ((char)0xFFFD).ToString() : ch switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                _ => ch.ToString(),
            });
        }

        return s.ToString();
    }

    // The y axis of the paper goes up, and the y axis of SVG goes down.
    private readonly struct Svg(double height)
    {
        public double Y(double y) => height - y;

        // ADR-0159 item 2: a straight line, the arc of a circle or of an ellipse from the start to the end, counter-clockwise on the
        // paper, or the whole curve when the start is the end.
        public string Path(SheetLine line)
        {
            var (fx, fy, ex, ey) = (line.FromX, Y(line.FromY), line.ToX, Y(line.ToY));
            var whole = line.FromX == line.ToX && line.FromY == line.ToY;
            if (line.Circle is { } circle)
            {
                var r = N(circle.Radius);
                if (whole)
                    return $"d=\"M {N(circle.X + circle.Radius)} {N(Y(circle.Y))} A {r} {r} 0 1 0 {N(circle.X - circle.Radius)} {N(Y(circle.Y))} A {r} {r} 0 1 0 {N(circle.X + circle.Radius)} {N(Y(circle.Y))} Z\"";
                // On the paper the arc goes counter-clockwise; the flip of y makes it the direction of sweep 0. It is larger than a
                // half turn when the end is on the right of the start, seen from the centre.
                var large = Cross(line.FromX - circle.X, line.FromY - circle.Y, line.ToX - circle.X, line.ToY - circle.Y) < 0 ? 1 : 0;
                return $"d=\"M {N(fx)} {N(fy)} A {r} {r} 0 {large} 0 {N(ex)} {N(ey)}\"";
            }

            if (line.Ellipse is { } ellipse)
            {
                // The frame of the ellipse: the two unit half axes. The path is in that frame, and the transform is a rotation or a
                // reflection with the flip of y, so it changes no width and no dash. The angle of the axes needs no arc tangent.
                var a = Math.Sqrt((ellipse.MajorX * ellipse.MajorX) + (ellipse.MajorY * ellipse.MajorY));
                var b = Math.Sqrt((ellipse.MinorX * ellipse.MinorX) + (ellipse.MinorY * ellipse.MinorY));
                var (e1x, e1y, e2x, e2y) = (ellipse.MajorX / a, ellipse.MajorY / a, ellipse.MinorX / b, ellipse.MinorY / b);
                var transform = $"transform=\"matrix({N(e1x)} {N(-e1y)} {N(e2x)} {N(-e2y)} {N(ellipse.X)} {N(Y(ellipse.Y))})\"";
                var (ra, rb) = (N(a), N(b));
                if (whole)
                    return $"{transform} d=\"M {ra} 0 A {ra} {rb} 0 1 1 {N(-a)} 0 A {ra} {rb} 0 1 1 {ra} 0 Z\"";
                var (sx, sy) = (line.FromX - ellipse.X, line.FromY - ellipse.Y);
                var (tx, ty) = (line.ToX - ellipse.X, line.ToY - ellipse.Y);
                // Counter-clockwise on the paper is the positive direction of the frame when the frame has the orientation of the
                // paper. An affine map keeps the side of the end, so the large arc comes from the paper.
                var sweep = Cross(e1x, e1y, e2x, e2y) > 0 ? 1 : 0;
                var large = Cross(sx, sy, tx, ty) < 0 ? 1 : 0;
                return $"{transform} d=\"M {N((sx * e1x) + (sy * e1y))} {N((sx * e2x) + (sy * e2y))} A {ra} {rb} 0 {large} {sweep} {N((tx * e1x) + (ty * e1y))} {N((tx * e2x) + (ty * e2y))}\"";
            }

            return $"d=\"M {N(fx)} {N(fy)} L {N(ex)} {N(ey)}\"";
        }

        private static double Cross(double ax, double ay, double bx, double by) => (ax * by) - (ay * bx);
    }
}
