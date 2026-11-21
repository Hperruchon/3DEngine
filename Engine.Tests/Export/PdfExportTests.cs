using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Drawing;
using Engine.Core.Export;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Engine.Tests.Export;

// ADR-0161 (phase P10c, TASK-0401): one sheet as one PDF file of version 1.4, from the engine, the route and the command line.
// ADR-0161 names this file in enforced-by. The test reads the table of the objects and the content of the page with its own
// reader.
public sealed class PdfExportTests : IDisposable
{
    private static readonly double[] Front = [1, 0, 0, 0, 0, 1, 0, -1, 0];
    private static readonly double[] Top = [1, 0, 0, 0, 1, 0, 0, 0, 1];
    private readonly string _folder = Directory.CreateTempSubdirectory("3de-pdf-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    // A sheet of A4 landscape (297 by 210) with the lines and the dimensions of the test, and no body.
    private static SheetInfo Sheet(IReadOnlyList<SheetLine> lines, IReadOnlyList<SheetDimensionInfo>? dimensions = null, string name = "Sheet 1")
        => new(Guid.NewGuid(), name, "A4", true, false, 297, 210, [new SheetViewInfo(Guid.NewGuid(), Front, 1, 2, 100, 100, lines, [])], dimensions ?? []);

    // The content of the page: the text between "stream" and "endstream" of object 5, after the checks of the structure.
    private static string Content(string file)
    {
        Assert.True(file.All(ch => ch is '\n' or (>= ' ' and <= '~')), "The file is ASCII.");
        Assert.StartsWith("%PDF-1.4\n", file, StringComparison.Ordinal);
        Assert.EndsWith("%%EOF\n", file, StringComparison.Ordinal);
        var start = int.Parse(Regex.Match(file, @"startxref\n(\d+)\n%%EOF\n$").Groups[1].Value, CultureInfo.InvariantCulture);
        Assert.StartsWith("xref\n0 7\n0000000000 65535 f \n", file[start..], StringComparison.Ordinal);
        var entries = Regex.Matches(file[start..], @"(\d{10}) 00000 n \n").Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)).ToArray();
        Assert.Equal(6, entries.Length);
        for (var i = 0; i < entries.Length; i++)
            Assert.StartsWith($"{i + 1} 0 obj\n", file[entries[i]..], StringComparison.Ordinal);
        var stream = Regex.Match(file, @"5 0 obj\n<< /Length (\d+) >>\nstream\n");
        var length = int.Parse(stream.Groups[1].Value, CultureInfo.InvariantCulture);
        var body = file.Substring(stream.Index + stream.Length, length);
        Assert.StartsWith("endstream\nendobj\n", file[(stream.Index + stream.Length + length)..], StringComparison.Ordinal);
        return body;
    }

    private static string Content(SheetInfo sheet) => Content(PdfExport.Text(sheet));

    // The paths of the content after the line "0.5 w [] 0 d" or "0.25 w [3 0.75] 0 d": each path from "m" to "S" as its numbers
    // and its operators.
    private static List<(bool Hidden, List<(string Op, double[] Numbers)> Parts)> Paths(string content)
    {
        var paths = new List<(bool Hidden, List<(string Op, double[] Numbers)> Parts)>();
        var hidden = false;
        var inView = false;
        foreach (var line in content.Split('\n'))
        {
            if (line.StartsWith("% View", StringComparison.Ordinal))
                inView = true;
            if (line == "0.25 w [3 0.75] 0 d")
                hidden = true;
            if (line == "0.5 w [] 0 d")
                hidden = false;
            if (!inView || !line.EndsWith(" S", StringComparison.Ordinal) || line.EndsWith("re S", StringComparison.Ordinal))
                continue;
            var parts = new List<(string Op, double[] Numbers)>();
            var numbers = new List<double>();
            foreach (var token in line.Split(' '))
            {
                if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    numbers.Add(value);
                    continue;
                }

                if (token != "S")
                    parts.Add((token, [.. numbers]));
                numbers.Clear();
            }

            paths.Add((hidden, parts));
        }

        return paths;
    }

    // The point of a cubic Bézier curve at s.
    private static (double X, double Y) Bezier((double X, double Y) p0, double[] c, double s)
    {
        var r = 1 - s;
        double At(double a, double b, double d, double e) => (r * r * r * a) + (3 * r * r * s * b) + (3 * r * s * s * d) + (s * s * s * e);
        return (At(p0.X, c[0], c[2], c[4]), At(p0.Y, c[1], c[3], c[5]));
    }

    // Each point at 17 places of each curve of a path, with the start of the path and its end.
    private static ((double X, double Y) Start, (double X, double Y) End, List<(double X, double Y)> Points, int Curves) Sample(List<(string Op, double[] Numbers)> parts)
    {
        Assert.Equal("m", parts[0].Op);
        var current = (parts[0].Numbers[0], parts[0].Numbers[1]);
        var start = current;
        var points = new List<(double X, double Y)>();
        foreach (var (op, numbers) in parts.Skip(1))
        {
            Assert.Equal("c", op);
            for (var k = 0; k <= 16; k++)
                points.Add(Bezier(current, numbers, k / 16.0));
            current = (numbers[4], numbers[5]);
        }

        return (start, current, points, parts.Count - 1);
    }

    // The distance of a point from the ellipse C + cos t M + sin t N, by the nearest of 720 points and five steps of Newton.
    private static double FromEllipse(SheetEllipse e, (double X, double Y) p)
    {
        (double X, double Y) E(double t) => (e.X + (Math.Cos(t) * e.MajorX) + (Math.Sin(t) * e.MinorX), e.Y + (Math.Cos(t) * e.MajorY) + (Math.Sin(t) * e.MinorY));
        double Distance(double t) => Math.Sqrt(Math.Pow(E(t).X - p.X, 2) + Math.Pow(E(t).Y - p.Y, 2));
        var t = Enumerable.Range(0, 720).Select(i => i * Math.PI / 360).MinBy(Distance);
        for (var i = 0; i < 5; i++)
        {
            var (x, y) = E(t);
            var (dx, dy) = ((-Math.Sin(t) * e.MajorX) + (Math.Cos(t) * e.MinorX), (-Math.Sin(t) * e.MajorY) + (Math.Cos(t) * e.MinorY));
            var (ddx, ddy) = (e.X - x, e.Y - y);
            var f = ((x - p.X) * dx) + ((y - p.Y) * dy);
            var df = (dx * dx) + (dy * dy) + ((x - p.X) * ddx) + ((y - p.Y) * ddy);
            t -= f / df;
        }

        return Distance(t);
    }

    // Items 1 to 3: the file has the six objects in their order, its table of places, and the length of its stream; the page
    // is the paper in points, and the content starts with the matrix of the millimetre. A straight line is a path from its
    // start to its end with y up; a hidden line is thin with dashes, and comes before the visible lines.
    [Fact]
    public void A_Line_Is_A_Path_On_The_Paper_In_Millimetres()
    {
        var text = PdfExport.Text(Sheet([new SheetLine(Guid.NewGuid(), 10, 20, 30.5, 20, true, "edge"), new SheetLine(Guid.NewGuid(), 10, 40, 10, 60, false, "edge")]));

        // A4 landscape is 297 by 210 mm, 841.88976 by 595.27559 points.
        Assert.Contains($"/MediaBox [0 0 {PdfExport.N(297 * PdfExport.PointsPerMillimetre)} {PdfExport.N(210 * PdfExport.PointsPerMillimetre)}]", text, StringComparison.Ordinal);
        Assert.Equal(841.8897637795275, 297 * PdfExport.PointsPerMillimetre, 1e-9);
        Assert.Contains("/BaseFont /Helvetica /Encoding /WinAnsiEncoding", text, StringComparison.Ordinal);
        // ADR-0179 item 4 (TASK-0428): the label "Date of issue" of the title block is text of the page; the file holds no date of
        // its own, no /CreationDate and no /ModDate.
        Assert.DoesNotContain("/CreationDate", text, StringComparison.Ordinal);
        Assert.DoesNotContain("/ModDate", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Date", text[..text.IndexOf("stream\n", StringComparison.Ordinal)] + text[text.IndexOf("endstream", StringComparison.Ordinal)..], StringComparison.Ordinal);
        var content = Content(text);
        Assert.StartsWith("2.834645669291339 0 0 2.834645669291339 0 0 cm\n", content, StringComparison.Ordinal);
        var paths = Paths(content);
        Assert.Equal(2, paths.Count);
        Assert.True(paths[0].Hidden);
        Assert.Equal(["m 10 40", "l 10 60"], paths[0].Parts.Select(part => part.Op + " " + string.Join(" ", part.Numbers.Select(n => n.ToString(CultureInfo.InvariantCulture)))));
        Assert.False(paths[1].Hidden);
        Assert.Contains("10 20 m 30.5 20 l S\n", content, StringComparison.Ordinal);
        Assert.Contains("10 40 m 10 60 l S\n", content, StringComparison.Ordinal);
    }

    public static TheoryData<double, double, double, double, double, double, double> Curves => new()
    {
        { 10, 0, 0, 10, 0.1, 0.2, 0 },        // a circle of radius 10, an arc of less than a quarter turn
        { 10, 0, 0, 10, 0.3, 0.7, 0 },        // a circle, more than a half turn
        { 100, 0, 0, 100, 0.05, 0, 1 },       // the whole of a circle of radius 100
        { 6, 8, -4, 3, 0.37, 0.6, 0 },        // an ellipse of 10 by 5 at a slant
        { 0, 8, 4, 0, 0.1, 0.25, 0 },         // the minor half axis the other way
        { 40, 0, 0, 2, 0.2, 0.5, 0 },         // a flat ellipse of 40 by 2
        { 6, 8, -1.2, 0.9, 0.3, 0, 1 },       // the whole of a flat ellipse at a slant
    };

    // Item 4: an arc of a circle or an ellipse is a chain of Bézier curves from the start of the line to its end. Each point of
    // each curve is within 0.001 mm of the curve of the design.
    [Theory]
    [MemberData(nameof(Curves))]
    public void An_Arc_Is_A_Chain_Of_Curves_Within_The_Tolerance(double majorX, double majorY, double minorX, double minorY, double start, double turn, double whole)
    {
        var ellipse = new SheetEllipse(50, 60, majorX, majorY, minorX, minorY);
        var sign = (majorX * minorY) - (majorY * minorX) > 0 ? 1 : -1;
        (double, double) E(double turns)
        {
            var t = turns * 2 * Math.PI;
            return (50 + (Math.Cos(t) * majorX) + (sign * Math.Sin(t) * minorX), 60 + (Math.Cos(t) * majorY) + (sign * Math.Sin(t) * minorY));
        }

        var from = E(start);
        var to = whole == 1 ? from : E(start + turn);
        var circle = majorX == minorY && majorY == 0 && minorX == 0;
        var line = new SheetLine(Guid.NewGuid(), from.Item1, from.Item2, to.Item1, to.Item2, true, "edge")
        {
            Circle = circle ? new SheetCircle(50, 60, majorX) : null,
            Ellipse = circle ? null : ellipse,
        };

        var (first, last, points, curves) = Sample(Assert.Single(Paths(Content(Sheet([line])))).Parts);

        Assert.Equal(from, first);
        Assert.Equal(to, last);
        Assert.InRange(curves, 1, 400);
        var worst = points.Max(point => FromEllipse(ellipse, point));
        Assert.True(worst <= PdfExport.Tolerance, $"The worst distance from the curve is {worst}.");

        // Each point is on the counter-clockwise arc from the start to the end: its parameter in the frame of the half axes,
        // with N turned counter-clockwise from M, is between start and start + turn.
        if (whole == 0)
        {
            var (mm, nn) = ((majorX * majorX) + (majorY * majorY), (minorX * minorX) + (minorY * minorY));
            foreach (var (x, y) in points)
            {
                var (dx, dy) = (x - 50, y - 60);
                var turns = Math.Atan2(sign * ((dx * minorX) + (dy * minorY)) / nn, ((dx * majorX) + (dy * majorY)) / mm) / (2 * Math.PI);
                var along = turns - start - Math.Floor(turns - start + 1e-9);
                Assert.True(along <= turn + 1e-9 || along >= 1 - 1e-9, $"The point ({x}, {y}) is at {along} of a turn, outside the arc of {turn}.");
            }
        }
    }

    // Item 4: an arc whose end is in the direction of its start, but not equal to it, is a whole turn less the rounding, with a
    // curve on each side of the centre, as the line of the read says: counter-clockwise from the start to the end.
    [Fact]
    public void An_Almost_Closed_Arc_Goes_Around_The_Centre()
    {
        var ellipse = new SheetEllipse(50, 60, 8, 0, 0, 4);
        var line = new SheetLine(Guid.NewGuid(), 58, 60, 57.99999999999999, 60, true, "edge") { Ellipse = ellipse };

        var (_, last, points, curves) = Sample(Assert.Single(Paths(Content(Sheet([line])))).Parts);

        Assert.Equal((57.99999999999999, 60.0), last);
        Assert.InRange(curves, 4, 400);
        Assert.Contains(points, point => point.X < 42.01);
        Assert.Contains(points, point => point.Y < 56.01);
        Assert.True(points.Max(point => FromEllipse(ellipse, point)) <= PdfExport.Tolerance);
    }

    // Items 5 and 6: the frame and the title block are rectangles at the places of the SVG file, with the name and the paper,
    // the projection and the scale in Helvetica; a dimension is centred at its place with the widths of Helvetica, with "R" for
    // a radius and "lost" with no source. A middle dot is its byte of WinAnsiEncoding in octal.
    [Fact]
    public void The_Title_Block_And_The_Dimensions_Are_Texts()
    {
        var dimensions = new SheetDimensionInfo[]
        {
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "constraint", 120, 30, false, "length", 12.5),
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "constraint", 140, 30, false, "radius", 3),
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "constraint", 180, 30, true, null, null),
        };

        var sheet = Sheet([], dimensions, "Bracket (A) é") with { TitleBlock = SheetTitleBlock.Empty with { Creator = "H. P.", SheetNumber = "1/2" } };
        var content = Content(sheet);

        // ADR-0179 items 4 to 6 (TASK-0428): before the first view, the content holds each line of TitleBlock.Layout in its order,
        // with its width and its dashes, then each text of the layout at its place, with the size of the font of its capital
        // letters. Until TASK-0428 it held two rectangles and two texts.
        var layout = TitleBlock.Layout(sheet);
        var block = new StringBuilder();
        foreach (var line in layout.Lines)
        {
            var stroke = SvgExport.Stroke(line);
            block.Append(CultureInfo.InvariantCulture, $"{PdfExport.N(stroke == SvgExport.LineStroke.Thick ? SvgExport.Thick : SvgExport.Thin)} w [{(stroke == SvgExport.LineStroke.Chain ? SvgExport.CentreDashes : "")}] 0 d\n");
            block.Append(line.Circle is { } c
                ? FormattableString.Invariant($"{PdfExport.N(c.X + c.Radius)} {PdfExport.N(c.Y)} m")
                : FormattableString.Invariant($"{PdfExport.N(line.FromX)} {PdfExport.N(line.FromY)} m {PdfExport.N(line.ToX)} {PdfExport.N(line.ToY)} l S\n"));
            if (line.Circle is not null)
                block.Append(content[(content.IndexOf(block.ToString(), StringComparison.Ordinal) + block.Length)..].Split('\n')[0]).Append('\n');
        }

        foreach (var text in layout.Texts)
            block.Append(CultureInfo.InvariantCulture, $"BT /F1 {PdfExport.N(SvgExport.FontSize(text.Size))} Tf {PdfExport.N(text.X)} {PdfExport.N(text.Y)} Td {PdfExport.Literal(text.Text)} Tj ET\n");
        Assert.Contains("0 J 0 j\n" + block + "% View", content, StringComparison.Ordinal);
        Assert.Contains("Td (Bracket \\(A\\) \\351) Tj ET\n", content, StringComparison.Ordinal);
        Assert.Contains("Td (1/2) Tj ET\n", content, StringComparison.Ordinal);
        // "12.5": four characters of 556, 556, 278 and 556 thousandths, 1.946 em wide; at 3.5 mm it starts 3.4055 mm left of 120.
        Assert.Equal(1946, PdfExport.Width("12.5"));
        Assert.Contains($"BT /F1 3.5 Tf {PdfExport.N(120 - (1946 * 3.5 / 2000))} 30 Td (12.5) Tj ET\n", content, StringComparison.Ordinal);
        Assert.Contains("Td (R3) Tj ET\n", content, StringComparison.Ordinal);
        Assert.Contains($"BT /F1 3.5 Tf {PdfExport.N(180 - (1556 * 3.5 / 2000))} 30 Td (lost) Tj ET\n", content, StringComparison.Ordinal);
    }

    // Item 7: a string is ASCII in WinAnsiEncoding, and a number has no exponent; a number that is not finite is 0.
    [Fact]
    public void A_String_And_A_Number_Are_Ascii()
    {
        Assert.Equal("(a\\(b\\)\\\\c\\351?)", PdfExport.Literal("a(b)\\c\u00e9\ud83d\ude00"));
        Assert.Equal("0.0000001", PdfExport.N(1e-7));
        Assert.Equal("0", PdfExport.N(double.NaN));
        Assert.Equal("0", PdfExport.N(double.PositiveInfinity));
        Assert.Equal("0", PdfExport.N(-0.0));
    }

    // Item 7: the bytes of the file do not depend on the culture of the computer.
    [Fact]
    public void The_File_Is_The_Same_In_Each_Culture()
    {
        var sheet = Sheet([new SheetLine(Guid.NewGuid(), 10.25, 20.5, 30.125, 20, true, "edge")],
            [new SheetDimensionInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "constraint", 120.5, 30.25, false, "length", 12.5)]);
        var culture = CultureInfo.CurrentCulture;
        string invariant, french;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            invariant = PdfExport.Text(sheet);
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            french = PdfExport.Text(sheet);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }

        Assert.Equal(invariant, french);
        Assert.Contains("10.25 20.5 m 30.125 20 l S\n", invariant, StringComparison.Ordinal);
    }

    // Item 1: the export of a Document reads the sheet as GetSheet does; a mesh body gives a skipped entry and a comment in the
    // content, and a sheet that is not live gives E-SHEET-NOT-FOUND.
    [Fact]
    public void The_Export_Names_A_Mesh_Body_And_Refuses_A_Sheet_That_Is_Not_Live()
    {
        var mesh = new BodyHandle(Guid.NewGuid());
        var sheet = new SheetRecord(Guid.NewGuid(), "Sheet 1", "A3", false, false, [new SheetView(Guid.NewGuid(), Front, 1, 1, 100, 200)], []);
        var document = new Document();
        document.AddBody(new BodyRecord(mesh, ImportStlCommandHandler.MeshKind));
        document.AddSheet(sheet);

        var (content, skipped, error) = PdfExport.Write(document, sheet.Id);
        var (none, _, missing) = PdfExport.Write(document, Guid.NewGuid());

        Assert.Null(error);
        Assert.Equal(mesh.Id, Assert.Single(skipped).Body);
        var text = Encoding.ASCII.GetString(content!);
        Assert.Contains($"% The body {mesh.Id} gives no lines: ", Content(text), StringComparison.Ordinal);
        Assert.Contains($"/MediaBox [0 0 {PdfExport.N(297 * PdfExport.PointsPerMillimetre)} {PdfExport.N(420 * PdfExport.PointsPerMillimetre)}]", text, StringComparison.Ordinal);
        Assert.Null(none);
        Assert.Equal(DiagnosticCodes.SheetNotFound, missing?.Code);
    }

    private static async Task<Guid> Applied(DocumentSession session, Command command, string output)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Outputs.TryGet<Guid>(output, out var id) ? id : throw new InvalidOperationException($"No output {output}.");
    }

    private static AddSheetViewCommand View(Guid sheet, double[] d, long numerator, long denominator, double x, double y) => new()
    {
        SheetId = sheet, RightX = d[0], RightY = d[1], RightZ = d[2], UpX = d[3], UpY = d[4], UpZ = d[5], EyeX = d[6], EyeY = d[7], EyeZ = d[8],
        ScaleNumerator = numerator, ScaleDenominator = denominator, X = x, Y = y,
    };

    // Items 1 and 3: a box of 20 by 30 in a Front view at 1 : 2 gives four visible lines of 10 by 15 mm around the place of the
    // view, and a dimension of the extrude shows its distance in effect.
    [NativeManifoldFact]
    public async Task A_Box_Gives_Its_Lines_And_Its_Dimension()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var sketch = await Applied(session, new CreateSketchCommand { DatumId = Datums.XZ.Id }, "sketchId");
        var points = new List<Guid>();
        foreach (var (x, y) in new[] { (0.0, 0.0), (20.0, 0.0), (20.0, 30.0), (0.0, 30.0) })
            points.Add(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }, "pointId"));
        for (var i = 0; i < 4; i++)
            await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % 4] }, "lineId");
        var body = await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 10 }, "bodyId");
        var sheetId = await Applied(session, new CreateSheetCommand { SheetName = "Plate" }, "sheetId");
        var view = await Applied(session, View(sheetId, Front, 1, 2, 100, 80), "viewId");
        await Applied(session, new PlaceSheetDimensionCommand { SheetId = sheetId, ViewId = view, Source = body, SourceKind = "distance", X = 100, Y = 100 }, "dimensionId");

        var (content, skipped, error) = await session.Read((document, _) => PdfExport.Write(document, sheetId));

        Assert.Null(error);
        Assert.Empty(skipped);
        var page = Content(Encoding.ASCII.GetString(content!));
        var lines = Paths(page).Where(path => !path.Hidden).ToArray();
        Assert.Equal(4, lines.Length);
        var numbers = lines.SelectMany(path => path.Parts.SelectMany(part => part.Numbers)).ToArray();
        var xs = numbers.Where((_, i) => i % 2 == 0).ToArray();
        var ys = numbers.Where((_, i) => i % 2 == 1).ToArray();
        Assert.Equal((95.0, 105.0, 72.5, 87.5), (xs.Min(), xs.Max(), ys.Min(), ys.Max()));
        Assert.Contains("Td (10) Tj ET\n", page, StringComparison.Ordinal);
    }

    // Item 4: a cylinder that a Rotate turned about x, seen from the top, gives the ellipses of its two ends as chains of
    // curves within the tolerance of the ellipses of the read.
    [NativeManifoldFact]
    public async Task A_Turned_Cylinder_Gives_Its_Ellipses_As_Curves()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var cylinder = await Applied(session, new CreateCylinderCommand { Radius = 10, Height = 20 }, "bodyId");
        await Applied(session, new RotateCommand { BodyId = cylinder, Qw = Math.Sqrt(0.8), Qx = Math.Sqrt(0.2), Qy = 0, Qz = 0 }, "bodyId");
        var sheetId = await Applied(session, new CreateSheetCommand { SheetName = "Cylinder" }, "sheetId");
        await Applied(session, View(sheetId, Top, 1, 1, 150, 100), "viewId");

        var read = (await session.Query<SheetInfo>(new GetSheetQuery { SheetId = sheetId })).Result!;
        var (content, _, error) = await session.Read((document, _) => PdfExport.Write(document, sheetId));

        Assert.Null(error);
        var ellipses = read.Views[0].Lines.Where(line => line.Ellipse is not null).ToArray();
        Assert.NotEmpty(ellipses);
        var curved = Paths(Content(Encoding.ASCII.GetString(content!))).Where(path => path.Parts.Skip(1).Any(part => part.Op == "c")).Select(path => Sample(path.Parts)).ToList();
        foreach (var line in ellipses)
        {
            var (_, _, points, _) = Assert.Single(curved, path => path.Start == (line.FromX, line.FromY) && path.End == (line.ToX, line.ToY));
            Assert.True(points.Max(point => FromEllipse(line.Ellipse!, point)) <= PdfExport.Tolerance);
        }
    }

    // Item 8: GET /export/pdf gives the file of the writer, a sheet that is not live gives 404 with its code, and a request
    // with no sheet gives 400.
    [Fact]
    public async Task The_Route_Gives_The_File_404_And_400()
    {
        var host = new EngineHost(new InProcessMeshBackend());
        var sheetId = (await host.Session.Apply(new CreateSheetCommand { SheetName = "Sheet 1" })).Outputs.TryGet<Guid>("sheetId", out var id) ? id : Guid.Empty;
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddEngineSurface(_ => host);
        await using var app = builder.Build();
        app.MapEngineSurface();
        await app.StartAsync();
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        using var file = await http.GetAsync($"/export/pdf?sheetId={sheetId}");
        using var missing = await http.GetAsync($"/export/pdf?sheetId={Guid.NewGuid()}");
        using var bad = await http.GetAsync("/export/pdf");

        Assert.Equal(HttpStatusCode.OK, file.StatusCode);
        Assert.Equal("application/pdf", file.Content.Headers.ContentType?.MediaType);
        Assert.Equal((await host.Session.Read((document, _) => PdfExport.Write(document, sheetId))).Content, await file.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(DiagnosticCodes.SheetNotFound, JsonDocument.Parse(await missing.Content.ReadAsStringAsync()).RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal(DiagnosticCodes.ApiBadRequest, JsonDocument.Parse(await bad.Content.ReadAsStringAsync()).RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    // Item 8: the command line writes the sheet that --sheet names, before or after --folder; a sheet that is not live gives
    // its code and the exit code 1, and pdf with no --sheet is a usage error.
    [NativeManifoldFact]
    public async Task The_Command_Line_Writes_The_Sheet()
    {
        var document = Path.Combine(_folder, "part" + DocumentFile.Extension);
        Guid sheetId;
        using (var backend = new ManifoldGeometryBackend())
        {
            var session = new DocumentSession(EngineHosting.CreateDefault(backend));
            DocumentFile.Create(session, document);
            sheetId = await Applied(session, new CreateSheetCommand { SheetName = "Sheet 1" }, "sheetId");
            session.Close();
        }
        var pdf = Path.Combine(_folder, "sheet.pdf");

        var (exit, output) = Run("export", "pdf", document, pdf, "--sheet", sheetId.ToString());
        var (folderExit, _) = Run("export", "pdf", document, pdf, "--folder", _folder, "--sheet", sheetId.ToString());
        var (missingExit, missingOutput) = Run("export", "pdf", document, pdf, "--sheet", Guid.NewGuid().ToString());

        Assert.True(exit == 0, output);
        Assert.Equal(pdf, JsonDocument.Parse(output).RootElement.GetProperty("file").GetString());
        Assert.Contains("Td (Sheet 1) Tj ET\n", Content(await File.ReadAllTextAsync(pdf)), StringComparison.Ordinal);
        Assert.Equal(0, folderExit);
        Assert.Equal(1, missingExit);
        Assert.Equal(DiagnosticCodes.SheetNotFound, JsonDocument.Parse(missingOutput).RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(2, Run("export", "pdf", document, pdf).Exit);
    }

    private static (int Exit, string Output) Run(params string[] args)
    {
        var stdout = new StringWriter();
        var exit = global::Engine.Cli.Cli.Run(args, stdout, new StringWriter());
        return (exit, stdout.ToString());
    }

    // ADR-0165 items 3 and 4 (phase R30g, TASK-0405): a centre line is a thin path with the dashes of the SVG file, and a
    // diameter has the sign of WinAnsiEncoding at byte 330 in octal, centred with its width of 778 thousandths.
    [Fact]
    public void A_Centre_Line_Has_Its_Dashes_And_A_Diameter_Its_Sign()
    {
        var dimension = new SheetDimensionInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "constraint", 120, 30, false, "diameter", 16);

        var content = Content(Sheet([new SheetLine(Guid.NewGuid(), 15, 10, 15, 30, true, "centre")], [dimension]));

        Assert.Contains("0.25 w [6 0.75 0.125 0.75] 0 d\n15 10 m 15 30 l S\n0.5 w [] 0 d\n", content, StringComparison.Ordinal);
        Assert.Equal(778 + 556 + 556, PdfExport.Width("\u00d816"));
        Assert.Contains($"BT /F1 3.5 Tf {PdfExport.N(120 - (1890 * 3.5 / 2000))} 30 Td (\\33016) Tj ET\n", content, StringComparison.Ordinal);
    }
}
