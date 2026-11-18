using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
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

// ADR-0160 (phase P10b, TASK-0398): one sheet as one DXF file of release 12, from the engine, the route and the command line.
// ADR-0160 names this file in enforced-by. The test reads the file with its own reader of the pairs of group codes.
public sealed class DxfExportTests : IDisposable
{
    private static readonly double[] Front = [1, 0, 0, 0, 0, 1, 0, -1, 0];
    private static readonly double[] Top = [1, 0, 0, 0, 1, 0, 0, 0, 1];
    private readonly string _folder = Directory.CreateTempSubdirectory("3de-dxf-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    // One entity of the file: its type and its pairs, in their order.
    private sealed record Entity(string Type, List<(int Code, string Value)> Pairs)
    {
        public string? Text(int code) => Pairs.FirstOrDefault(pair => pair.Code == code).Value;

        public double Number(int code) => double.Parse(Text(code)!, CultureInfo.InvariantCulture);

        public double? Bulge => Text(42) is { } bulge ? double.Parse(bulge, CultureInfo.InvariantCulture) : null;
    }

    // The pairs of the file, and the entities of each section by its name.
    private static Dictionary<string, List<Entity>> Read(string text)
    {
        Assert.EndsWith("0\nEOF\n", text, StringComparison.Ordinal);
        var lines = text.Split('\n')[..^1];
        Assert.True(lines.Length % 2 == 0, "The file has pairs of lines.");
        var sections = new Dictionary<string, List<Entity>>();
        List<Entity>? current = null;
        for (var i = 0; i < lines.Length; i += 2)
        {
            var (code, value) = (int.Parse(lines[i], CultureInfo.InvariantCulture), lines[i + 1]);
            if (code == 0 && value == "SECTION")
            {
                Assert.Equal("2", lines[i + 2]);
                sections[lines[i + 3]] = current = [];
                i += 2;
                continue;
            }

            if (code == 0 && value is "ENDSEC" or "EOF")
            {
                current = null;
                continue;
            }

            if (code == 0)
                current!.Add(new Entity(value, []));
            else if (current is { Count: > 0 })
                current[^1].Pairs.Add((code, value));
            else
                Assert.True(current is not null, $"The pair {code} {value} is outside a section.");
        }

        return sections;
    }

    // The polylines of the entities: each POLYLINE with its VERTEX entities, up to its SEQEND.
    private static List<(string Layer, List<Entity> Vertices)> Polylines(List<Entity> entities)
    {
        var polylines = new List<(string Layer, List<Entity> Vertices)>();
        foreach (var entity in entities)
        {
            if (entity.Type == "POLYLINE")
                polylines.Add((entity.Text(8)!, []));
            else if (entity.Type == "VERTEX")
                polylines[^1].Vertices.Add(entity);
        }

        return polylines;
    }

    // A sheet of A4 landscape (297 by 210) with the lines and the dimensions of the test, and no body.
    private static SheetInfo Sheet(IReadOnlyList<SheetLine> lines, IReadOnlyList<SheetDimensionInfo>? dimensions = null, string name = "Sheet 1")
        => new(Guid.NewGuid(), name, "A4", true, false, 297, 210, [new SheetViewInfo(Guid.NewGuid(), Front, 1, 2, 100, 100, lines, [])], dimensions ?? []);

    private static List<Entity> Entities(SheetInfo sheet) => Read(DxfExport.Text(sheet))["ENTITIES"];

    // The centre of the arc of a polyline segment from p to q with a bulge b: on the left normal of the chord for b > 0, at
    // the distance h (1 − b²) / (2 b) from the middle of the chord, with h the half chord.
    private static (double X, double Y) Centre((double X, double Y) p, (double X, double Y) q, double b)
    {
        var (dx, dy) = (q.X - p.X, q.Y - p.Y);
        var length = Math.Sqrt((dx * dx) + (dy * dy));
        var offset = (length / 2) * (1 - (b * b)) / (2 * b);
        return (((p.X + q.X) / 2) - (dy / length * offset), ((p.Y + q.Y) / 2) + (dx / length * offset));
    }

    private static (double X, double Y) At(Entity vertex) => (vertex.Number(10), vertex.Number(20));

    // Items 2 and 3: the header gives release 12, the paper and the millimetre; the layer HIDDEN has the line type HIDDEN of
    // dashes of 3 and gaps of 0.75. A straight line is a LINE on the paper with y up; a hidden line is on the layer HIDDEN
    // and comes before the visible lines, which are on the layer VISIBLE.
    [Fact]
    public void A_Line_Is_A_Line_On_The_Paper_With_Y_Up()
    {
        var text = DxfExport.Text(Sheet([new SheetLine(Guid.NewGuid(), 10, 20, 30.5, 20, true, "edge"), new SheetLine(Guid.NewGuid(), 10, 40, 10, 60, false, "edge")]));

        Assert.True(text.All(ch => ch is '\n' or (>= ' ' and <= '~')), "The file is ASCII.");
        var sections = Read(text);
        Assert.Equal(["HEADER", "TABLES", "BLOCKS", "ENTITIES"], sections.Keys);
        Assert.Contains("9\n$ACADVER\n1\nAC1009\n", text, StringComparison.Ordinal);
        Assert.Contains("9\n$EXTMAX\n10\n297\n20\n210\n30\n0\n", text, StringComparison.Ordinal);
        Assert.Contains("9\n$INSUNITS\n70\n4\n", text, StringComparison.Ordinal);
        var tables = sections["TABLES"];
        var hidden = tables.Single(entity => entity.Type == "LTYPE" && entity.Text(2) == "HIDDEN");
        Assert.Equal(["3.75", "3", "-0.75"], hidden.Pairs.Where(pair => pair.Code is 40 or 49).Select(pair => pair.Value));
        Assert.Equal("HIDDEN", tables.Single(entity => entity.Type == "LAYER" && entity.Text(2) == DxfExport.HiddenLayer).Text(6));
        Assert.Equal("CONTINUOUS", tables.Single(entity => entity.Type == "LAYER" && entity.Text(2) == DxfExport.VisibleLayer).Text(6));

        var lines = sections["ENTITIES"].Where(entity => entity.Type == "LINE" && entity.Text(8) != DxfExport.FrameLayer).ToArray();
        Assert.Equal(2, lines.Length);
        Assert.Equal((DxfExport.HiddenLayer, 10.0, 40.0, 10.0, 60.0), (lines[0].Text(8), lines[0].Number(10), lines[0].Number(20), lines[0].Number(11), lines[0].Number(21)));
        Assert.Equal((DxfExport.VisibleLayer, 10.0, 20.0, 30.5, 20.0), (lines[1].Text(8), lines[1].Number(10), lines[1].Number(20), lines[1].Number(11), lines[1].Number(21)));
    }

    // Item 4: a whole circle is a CIRCLE. An arc of a circle is a polyline from its start to its end; its bulge is the tangent
    // of a quarter of its angle, positive counter-clockwise, and an arc of a half turn or more is two arcs, so each bulge is
    // at most 1. The centre of each arc is the centre of the circle.
    [Fact]
    public void An_Arc_Of_A_Circle_Is_A_Polyline_With_A_Bulge()
    {
        var circle = new SheetCircle(100, 100, 10);
        var quarter = new SheetLine(Guid.NewGuid(), 110, 100, 100, 110, true, "edge") { Circle = circle };
        var threeQuarters = new SheetLine(Guid.NewGuid(), 100, 110, 110, 100, true, "edge") { Circle = circle };
        var half = new SheetLine(Guid.NewGuid(), 110, 100, 90, 100, true, "edge") { Circle = circle };
        var whole = new SheetLine(Guid.NewGuid(), 110, 100, 110, 100, false, "outline") { Circle = circle };

        var entities = Entities(Sheet([quarter, threeQuarters, half, whole]));

        var round = Assert.Single(entities, entity => entity.Type == "CIRCLE");
        Assert.Equal((DxfExport.HiddenLayer, 100.0, 100.0, 10.0), (round.Text(8), round.Number(10), round.Number(20), round.Number(40)));
        var polylines = Polylines(entities);
        Assert.Equal([2, 3, 3], polylines.Select(polyline => polyline.Vertices.Count));
        Assert.Equal(Math.Sqrt(2) - 1, polylines[0].Vertices[0].Bulge!.Value, 15);
        Assert.Null(polylines[0].Vertices[1].Bulge);
        Assert.Equal((110.0, 100.0), At(polylines[0].Vertices[0]));
        Assert.Equal((100.0, 110.0), At(polylines[0].Vertices[1]));
        Assert.Equal((110.0, 100.0), At(polylines[1].Vertices[2]));
        foreach (var (_, vertices) in polylines)
        {
            for (var i = 0; i + 1 < vertices.Count; i++)
            {
                var bulge = vertices[i].Bulge!.Value;
                Assert.InRange(bulge, 0, 1);
                var centre = Centre(At(vertices[i]), At(vertices[i + 1]), bulge);
                Assert.True(Math.Abs(centre.X - 100) < 1e-12 && Math.Abs(centre.Y - 100) < 1e-12, $"centre {centre}");
            }
        }

        Assert.Equal((100.0, 110.0), At(polylines[2].Vertices[1]));
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

    public static TheoryData<double, double, double, double, double, double, double> Ellipses => new()
    {
        { 8, 0, 0, 4, 0, 0.15, 0 },          // axes along x and y, from the end of the major axis to a point off the axes
        { 0, 8, -4, 0, 0.1, 0.25, 0 },       // a quarter turn off the axes, with the orientation of the paper
        { 0, 8, 4, 0, 0.1, 0.25, 0 },        // the minor half axis the other way
        { 6, 8, -4, 3, 0.37, 0.6, 0 },       // half axes of 10 and 5 at a slant, more than a half turn
        { 40, 0, 0, 2, 0.2, 0.5, 0 },        // a flat ellipse of 40 by 2, a half turn
        { 8, 0, 0, 4, 0.05, 0, 1 },          // the whole ellipse
        { 6, 8, -1.2, 0.9, 0.3, 0, 1 },      // the whole of a flat ellipse at a slant
    };

    // Item 4: an arc of an ellipse is a polyline of arcs of circles. It starts at the start of the line and ends at its end,
    // each arc goes counter-clockwise, and each point of each arc is within 0.001 mm of the ellipse.
    [Theory]
    [MemberData(nameof(Ellipses))]
    public void An_Arc_Of_An_Ellipse_Is_Within_The_Tolerance(double majorX, double majorY, double minorX, double minorY, double start, double turn, double whole)
    {
        var ellipse = new SheetEllipse(50, 60, majorX, majorY, minorX, minorY);
        // The start is at the parameter start × 2π and the end at (start + turn) × 2π, in the orientation of the paper: N turns
        // counter-clockwise from M. The points come from doubles, so they are on the ellipse only within its rounding.
        var sign = (majorX * minorY) - (majorY * minorX) > 0 ? 1 : -1;
        (double, double) E(double turns)
        {
            var t = turns * 2 * Math.PI;
            return (50 + (Math.Cos(t) * majorX) + (sign * Math.Sin(t) * minorX), 60 + (Math.Cos(t) * majorY) + (sign * Math.Sin(t) * minorY));
        }

        var from = E(start);
        var to = whole == 1 ? from : E(start + turn);
        var line = new SheetLine(Guid.NewGuid(), from.Item1, from.Item2, to.Item1, to.Item2, true, "edge") { Ellipse = ellipse };

        var vertices = Assert.Single(Polylines(Entities(Sheet([line])))).Vertices;

        Assert.Equal(from, At(vertices[0]));
        Assert.Equal(to, At(vertices[^1]));
        Assert.Null(vertices[^1].Bulge);
        Assert.InRange(vertices.Count, 3, 200);
        var worst = 0.0;
        for (var i = 0; i + 1 < vertices.Count; i++)
        {
            var (p, q, bulge) = (At(vertices[i]), At(vertices[i + 1]), vertices[i].Bulge!.Value);
            Assert.InRange(bulge, 0, 1);
            var c = Centre(p, q, bulge);
            var r = Math.Sqrt(Math.Pow(p.X - c.X, 2) + Math.Pow(p.Y - c.Y, 2));
            var (a0, a1) = (Math.Atan2(p.Y - c.Y, p.X - c.X), Math.Atan2(q.Y - c.Y, q.X - c.X));
            var sweep = 4 * Math.Atan(bulge);
            for (var k = 0; k <= 16; k++)
            {
                var a = a0 + (sweep * k / 16);
                worst = Math.Max(worst, FromEllipse(ellipse, (c.X + (r * Math.Cos(a)), c.Y + (r * Math.Sin(a)))));
            }

            Assert.True(Math.Abs(Math.IEEERemainder(a0 + sweep - a1, 2 * Math.PI)) < 1e-9, "The arc ends at the next vertex.");
        }

        Assert.True(worst <= DxfExport.Tolerance, $"The worst distance from the ellipse is {worst}.");
    }

    // Items 5 and 6: the frame and the title block are lines of the layer FRAME at the places of the SVG file; the title block
    // has the name, the paper, the projection and the scale. A dimension is a centred text of 3.5 mm at its place, with "R" for
    // a radius, and "lost" with no source. A character outside ASCII is \U+XXXX.
    [Fact]
    public void The_Title_Block_And_The_Dimensions_Are_Texts()
    {
        var dimensions = new SheetDimensionInfo[]
        {
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "constraint", 120, 30, false, "length", 12.5),
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "constraint", 140, 30, false, "radius", 3),
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "constraint", 180, 30, true, null, null),
        };

        var entities = Entities(Sheet([], dimensions, "Bracket <A&B> é"));

        var frame = entities.Where(entity => entity.Type == "LINE" && entity.Text(8) == DxfExport.FrameLayer)
            .Select(line => string.Create(CultureInfo.InvariantCulture, $"{line.Number(10)},{line.Number(20)}-{line.Number(11)},{line.Number(21)}")).ToArray();
        Assert.Equal(["10,10-287,10", "287,10-287,200", "287,200-10,200", "10,200-10,10", "167,10-287,10", "287,10-287,30", "287,30-167,30", "167,30-167,10"], frame);
        var texts = entities.Where(entity => entity.Type == "TEXT").ToArray();
        Assert.All(texts, text => Assert.Equal(DxfExport.TextLayer, text.Text(8)));
        Assert.Equal(["Bracket <A&B> \\U+00E9", "A4 landscape \\U+00B7 First angle \\U+00B7 Scale 1:2 \\U+00B7 mm", "12.5", "R3", "lost"], texts.Select(text => text.Text(1)));
        Assert.Equal((170.0, 22.0, 5.0, null), (texts[0].Number(10), texts[0].Number(20), texts[0].Number(40), texts[0].Text(72)));
        Assert.Equal(("1", 120.0, 30.0, 3.5), (texts[2].Text(72), texts[2].Number(11), texts[2].Number(21), texts[2].Number(40)));
    }

    // Item 7: the text of a name is ASCII, and a number has no exponent.
    [Fact]
    public void A_Text_And_A_Number_Are_Ascii()
    {
        Assert.Equal("a b\\U+00E9?", DxfExport.Escape("a\tb\u00e9\ud83d\ude00"));
        Assert.Equal("0.0000001", DxfExport.N(1e-7));
        Assert.Equal("-0.00000123", DxfExport.N(-1.23e-6));
        Assert.Equal("12300000000000000000000", DxfExport.N(1.23e22));
        Assert.Equal(1.23e-6, double.Parse(DxfExport.N(1.23e-6), CultureInfo.InvariantCulture));
        Assert.Equal("-12.5", DxfExport.N(-12.5));
        Assert.Equal("0", DxfExport.N(-0.0));
        // ADR-0163 item 2 (TASK-0400, finding E215): a number that is not finite is 0; until TASK-0400 the writer threw.
        Assert.Equal(["0", "0", "0"], new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity }.Select(DxfExport.N));
        Assert.Contains("10\n0\n20\n20\n30\n0\n", DxfExport.Text(Sheet([new SheetLine(Guid.NewGuid(), double.NaN, 20, 30.5, 20, true, "edge")])), StringComparison.Ordinal);
    }

    // Item 7: the text of the file does not depend on the culture of the computer.
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
            invariant = DxfExport.Text(sheet);
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            french = DxfExport.Text(sheet);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }

        Assert.Equal(invariant, french);
        Assert.Contains("10\n10.25\n20\n20.5\n30\n0\n11\n30.125\n", invariant, StringComparison.Ordinal);
    }

    // Item 1: the export of a Document reads the sheet as GetSheet does; a mesh body gives a skipped entry and a comment in the
    // file, and a sheet that is not live gives E-SHEET-NOT-FOUND.
    [Fact]
    public void The_Export_Names_A_Mesh_Body_And_Refuses_A_Sheet_That_Is_Not_Live()
    {
        var mesh = new BodyHandle(Guid.NewGuid());
        var sheet = new SheetRecord(Guid.NewGuid(), "Sheet 1", "A3", false, false, [new SheetView(Guid.NewGuid(), Front, 1, 1, 100, 200)], []);
        var document = new Document();
        document.AddBody(new BodyRecord(mesh, ImportStlCommandHandler.MeshKind));
        document.AddSheet(sheet);

        var (content, skipped, error) = DxfExport.Write(document, sheet.Id);
        var (none, _, missing) = DxfExport.Write(document, Guid.NewGuid());

        Assert.Null(error);
        Assert.Equal(mesh.Id, Assert.Single(skipped).Body);
        var text = Encoding.ASCII.GetString(content!);
        Assert.Contains($"999\nThe body {mesh.Id} gives no lines: ", text, StringComparison.Ordinal);
        Assert.Contains("9\n$EXTMAX\n10\n297\n20\n420\n", text, StringComparison.Ordinal);
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

    // Items 1 and 4: a box of 20 by 30 in a Front view at 1 : 2 gives four visible lines of 10 by 15 mm around the place of the
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

        var (content, skipped, error) = await session.Read((document, _) => DxfExport.Write(document, sheetId));

        Assert.Null(error);
        Assert.Empty(skipped);
        var entities = Read(Encoding.ASCII.GetString(content!))["ENTITIES"];
        var lines = entities.Where(entity => entity.Type == "LINE" && entity.Text(8) == DxfExport.VisibleLayer).ToArray();
        Assert.Equal(4, lines.Length);
        var xs = lines.SelectMany(line => new[] { line.Number(10), line.Number(11) }).ToArray();
        var ys = lines.SelectMany(line => new[] { line.Number(20), line.Number(21) }).ToArray();
        Assert.Equal((95.0, 105.0, 72.5, 87.5), (xs.Min(), xs.Max(), ys.Min(), ys.Max()));
        Assert.Contains(entities, entity => entity.Type == "TEXT" && entity.Text(1) == "10");
    }

    // Item 4: a cylinder that a Rotate turned about x, seen from the top, gives the ellipses of its two ends as polylines of
    // arcs, each vertex within the tolerance of the ellipse of the read.
    [NativeManifoldFact]
    public async Task A_Turned_Cylinder_Gives_Its_Ellipses_As_Arcs()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var cylinder = await Applied(session, new CreateCylinderCommand { Radius = 10, Height = 20 }, "bodyId");
        await Applied(session, new RotateCommand { BodyId = cylinder, Qw = Math.Sqrt(0.8), Qx = Math.Sqrt(0.2), Qy = 0, Qz = 0 }, "bodyId");
        var sheetId = await Applied(session, new CreateSheetCommand { SheetName = "Cylinder" }, "sheetId");
        await Applied(session, View(sheetId, Top, 1, 1, 150, 100), "viewId");

        var read = (await session.Query<SheetInfo>(new GetSheetQuery { SheetId = sheetId })).Result!;
        var (content, _, error) = await session.Read((document, _) => DxfExport.Write(document, sheetId));

        Assert.Null(error);
        var ellipses = read.Views[0].Lines.Where(line => line.Ellipse is not null).ToArray();
        Assert.NotEmpty(ellipses);
        var polylines = Polylines(Read(Encoding.ASCII.GetString(content!))["ENTITIES"]);
        Assert.Equal(ellipses.Length + read.Views[0].Lines.Count(line => line.Circle is not null && (line.FromX, line.FromY) != (line.ToX, line.ToY)), polylines.Count);
        foreach (var line in ellipses)
        {
            var (_, vertices) = Assert.Single(polylines, polyline => At(polyline.Vertices[0]) == (line.FromX, line.FromY) && At(polyline.Vertices[^1]) == (line.ToX, line.ToY));
            Assert.All(vertices, vertex => Assert.True(FromEllipse(line.Ellipse!, At(vertex)) < 1e-9));
        }
    }

    // Item 8: GET /export/dxf gives the file of the writer, a sheet that is not live gives 404 with its code, and a request
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

        using var file = await http.GetAsync($"/export/dxf?sheetId={sheetId}");
        using var missing = await http.GetAsync($"/export/dxf?sheetId={Guid.NewGuid()}");
        using var bad = await http.GetAsync("/export/dxf");

        Assert.Equal(HttpStatusCode.OK, file.StatusCode);
        Assert.Equal("image/vnd.dxf", file.Content.Headers.ContentType?.MediaType);
        Assert.Equal((await host.Session.Read((document, _) => DxfExport.Write(document, sheetId))).Content, await file.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(DiagnosticCodes.SheetNotFound, JsonDocument.Parse(await missing.Content.ReadAsStringAsync()).RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal(DiagnosticCodes.ApiBadRequest, JsonDocument.Parse(await bad.Content.ReadAsStringAsync()).RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    // Item 8: the command line writes the sheet that --sheet names, before or after --folder; a sheet that is not live gives
    // its code and the exit code 1, and dxf with no --sheet is a usage error.
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
        var dxf = Path.Combine(_folder, "sheet.dxf");

        var (exit, output) = Run("export", "dxf", document, dxf, "--sheet", sheetId.ToString());
        var (folderExit, _) = Run("export", "dxf", document, dxf, "--folder", _folder, "--sheet", sheetId.ToString());
        var (missingExit, missingOutput) = Run("export", "dxf", document, dxf, "--sheet", Guid.NewGuid().ToString());

        Assert.True(exit == 0, output);
        Assert.Equal(dxf, JsonDocument.Parse(output).RootElement.GetProperty("file").GetString());
        Assert.Contains(Read(await File.ReadAllTextAsync(dxf))["ENTITIES"], entity => entity.Type == "TEXT" && entity.Text(1) == "Sheet 1");
        Assert.Equal(0, folderExit);
        Assert.Equal(1, missingExit);
        Assert.Equal(DiagnosticCodes.SheetNotFound, JsonDocument.Parse(missingOutput).RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(2, Run("export", "dxf", document, dxf).Exit);
        Assert.Equal(2, Run("export", "step", document, dxf, "--sheet", sheetId.ToString()).Exit);
    }

    private static (int Exit, string Output) Run(params string[] args)
    {
        var stdout = new StringWriter();
        var exit = global::Engine.Cli.Cli.Run(args, stdout, new StringWriter());
        return (exit, stdout.ToString());
    }

    // ADR-0163 item 3 (TASK-0400, finding E216 of the codebase review of 2026-11-17): an arc whose end is in the direction of
    // its start, but not equal to it, is a whole turn less the rounding, as in the PDF file. Until TASK-0400 its last part was
    // a half turn whose middle was the unit of (0, 0), and the chain was a line through the centre.
    [Fact]
    public void An_Almost_Closed_Arc_Of_An_Ellipse_Goes_Around_The_Centre()
    {
        var ellipse = new SheetEllipse(50, 60, 8, 0, 0, 4);
        var line = new SheetLine(Guid.NewGuid(), 58, 60, 57.99999999999999, 60, true, "edge") { Ellipse = ellipse };

        var vertices = Assert.Single(Polylines(Entities(Sheet([line])))).Vertices;

        Assert.Equal((57.99999999999999, 60.0), At(vertices[^1]));
        Assert.All(vertices.SkipLast(1), vertex => Assert.InRange(vertex.Bulge!.Value, 1e-9, 1));
        Assert.Contains(vertices, vertex => At(vertex).X < 42.01);
        Assert.Contains(vertices, vertex => At(vertex).Y < 56.01);
        Assert.All(vertices, vertex => Assert.True(FromEllipse(ellipse, At(vertex)) < 1e-9));
    }
}
