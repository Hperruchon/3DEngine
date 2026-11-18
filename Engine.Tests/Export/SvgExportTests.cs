using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
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

// ADR-0159 (phase P10a, TASK-0396): one sheet as one SVG file, from the engine, the route and the command line. ADR-0159 names
// this file in enforced-by.
public sealed class SvgExportTests : IDisposable
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
    private static readonly double[] Front = [1, 0, 0, 0, 0, 1, 0, -1, 0];
    private readonly string _folder = Directory.CreateTempSubdirectory("3de-svg-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

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

    // A sheet of A4 landscape (297 by 210) with the lines and the dimensions of the test, and no body.
    private static SheetInfo Sheet(IReadOnlyList<SheetLine> lines, IReadOnlyList<SheetDimensionInfo>? dimensions = null, string name = "Sheet 1", params (long, long)[] scales)
    {
        var views = (scales.Length == 0 ? [(1L, 1L)] : scales)
            .Select((scale, i) => new SheetViewInfo(Guid.NewGuid(), Front, scale.Item1, scale.Item2, 100, 100, i == 0 ? lines : [], []))
            .ToList();
        return new SheetInfo(Guid.NewGuid(), name, "A4", true, false, 297, 210, views, dimensions ?? []);
    }

    private static IReadOnlyList<XElement> Paths(string text, string? view = null)
        => [.. XDocument.Parse(text).Descendants(Svg + "g").Where(g => ((string?)g.Attribute("id"))?.StartsWith(view ?? "view-", StringComparison.Ordinal) == true).SelectMany(g => g.Elements(Svg + "path"))];

    // Items 2 and 6: the paper is the view box in millimetres, y goes down, and a visible straight line is a thick path from
    // its start to its end. A hidden line is thin, with dashes, and comes before the visible lines.
    [Fact]
    public void A_Line_Is_A_Path_On_The_Paper_With_Y_Down()
    {
        var text = SvgExport.Text(Sheet([new SheetLine(Guid.NewGuid(), 10, 20, 30.5, 20, true, "edge"), new SheetLine(Guid.NewGuid(), 10, 40, 10, 60, false, "edge")]));

        var root = XDocument.Parse(text).Root!;
        Assert.Equal(("297mm", "210mm", "0 0 297 210"), ((string?)root.Attribute("width"), (string?)root.Attribute("height"), (string?)root.Attribute("viewBox")));
        var paths = Paths(text);
        Assert.Equal(2, paths.Count);
        Assert.Equal(("M 10 170 L 10 150", "0.25", "3 0.75"), ((string?)paths[0].Attribute("d"), (string?)paths[0].Attribute("stroke-width"), (string?)paths[0].Attribute("stroke-dasharray")));
        Assert.Equal(("M 10 190 L 30.5 190", "0.5", null), ((string?)paths[1].Attribute("d"), (string?)paths[1].Attribute("stroke-width"), (string?)paths[1].Attribute("stroke-dasharray")));
    }

    // Item 2: an arc of a circle goes counter-clockwise on the paper, so its sweep flag is 0 after the flip of y; it is the large
    // arc when it turns more than a half turn. A start equal to the end is the whole circle.
    [Fact]
    public void An_Arc_Of_A_Circle_Has_The_Flags_Of_Its_Turn()
    {
        var circle = new SheetCircle(100, 100, 10);
        var quarter = new SheetLine(Guid.NewGuid(), 110, 100, 100, 110, true, "edge") { Circle = circle };
        var threeQuarters = new SheetLine(Guid.NewGuid(), 100, 110, 110, 100, true, "edge") { Circle = circle };
        var whole = new SheetLine(Guid.NewGuid(), 110, 100, 110, 100, true, "outline") { Circle = circle };

        var paths = Paths(SvgExport.Text(Sheet([quarter, threeQuarters, whole]))).Select(path => (string?)path.Attribute("d")).ToArray();

        Assert.Equal("M 110 110 A 10 10 0 0 0 100 100", paths[0]);
        Assert.Equal("M 100 100 A 10 10 0 1 0 110 110", paths[1]);
        Assert.Equal("M 110 110 A 10 10 0 1 0 90 110 A 10 10 0 1 0 110 110 Z", paths[2]);
    }

    // Item 2: an arc of an ellipse is in the frame of its two unit half axes, with a transform that rotates or reflects and
    // moves, so a width or a dash does not change. The sweep follows the orientation of the half axes on the paper.
    [Fact]
    public void An_Arc_Of_An_Ellipse_Is_In_The_Frame_Of_Its_Axes()
    {
        // Half axes (0, 8) and (-4, 0): a quarter turn of the ellipse of 8 by 4, with the orientation of the paper.
        var ellipse = new SheetEllipse(50, 60, 0, 8, -4, 0);
        var arc = new SheetLine(Guid.NewGuid(), 50, 68, 46, 60, true, "edge") { Ellipse = ellipse };
        // The same ellipse with the minor half axis the other way: the other orientation.
        var mirrored = new SheetLine(Guid.NewGuid(), 50, 68, 46, 60, true, "edge") { Ellipse = ellipse with { MinorX = 4 } };
        var whole = new SheetLine(Guid.NewGuid(), 50, 68, 50, 68, false, "outline") { Ellipse = ellipse };
        // The ellipse with its axes along x and y: the end of the arc is off each axis of the paper.
        var level = new SheetLine(Guid.NewGuid(), 58, 60, 50, 64, true, "edge") { Ellipse = new SheetEllipse(50, 60, 8, 0, 0, 4) };

        var paths = Paths(SvgExport.Text(Sheet([arc, mirrored, whole, level])));

        // The hidden whole curve comes first.
        Assert.Equal(("matrix(0 -1 -1 0 50 150)", "M 8 0 A 8 4 0 1 1 -8 0 A 8 4 0 1 1 8 0 Z"), ((string?)paths[0].Attribute("transform"), (string?)paths[0].Attribute("d")));
        Assert.Equal(("matrix(0 -1 -1 0 50 150)", "M 8 0 A 8 4 0 0 1 0 4"), ((string?)paths[1].Attribute("transform"), (string?)paths[1].Attribute("d")));
        Assert.Equal(("matrix(0 -1 1 0 50 150)", "M 8 0 A 8 4 0 0 0 0 -4"), ((string?)paths[2].Attribute("transform"), (string?)paths[2].Attribute("d")));
        Assert.Equal(("matrix(1 0 0 -1 50 150)", "M 8 0 A 8 4 0 0 1 0 4"), ((string?)paths[3].Attribute("transform"), (string?)paths[3].Attribute("d")));
    }

    // Items 4 and 5: the title block has the name, the paper, the projection and the one scale of the views in lowest terms; a
    // sheet with two scales says "as shown". A dimension shows its value at its place, with "R" for a radius, and "lost" with
    // no source. A name with the characters of XML is text.
    [Fact]
    public void The_Title_Block_And_The_Dimensions_Have_Their_Text()
    {
        var dimensions = new SheetDimensionInfo[]
        {
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "constraint", 120, 30, false, "length", 12.5),
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "constraint", 140, 30, false, "radius", 3),
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "distance", 160, 30, false, "distance", 0.1),
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "constraint", 180, 30, true, null, null),
        };

        var one = XDocument.Parse(SvgExport.Text(Sheet([], dimensions, "Bracket <A&B>", (2, 4), (3, 6))));
        var two = XDocument.Parse(SvgExport.Text(Sheet([], null, "Sheet 1", (1, 2), (2, 1))));

        // A4 landscape: the frame is 10 mm inside the edge, and the title block is 120 by 20 mm in its lower right corner.
        var rects = one.Descendants(Svg + "g").Single(g => (string?)g.Attribute("id") == "frame").Elements(Svg + "rect")
            .Select(rect => string.Join(" ", new[] { "x", "y", "width", "height" }.Select(name => (string?)rect.Attribute(name)))).ToArray();
        Assert.Equal(["10 10 277 190", "167 180 120 20"], rects);
        var title = one.Descendants(Svg + "g").Single(g => (string?)g.Attribute("id") == "title").Elements(Svg + "text").Select(text => text.Value).ToArray();
        Assert.Equal(["Bracket <A&B>", "A4 landscape · First angle · Scale 1:2 · mm"], title);
        Assert.Equal("Bracket <A&B>", one.Root!.Element(Svg + "title")!.Value);
        Assert.EndsWith("Scale as shown · mm", two.Descendants(Svg + "text").ElementAt(1).Value, StringComparison.Ordinal);
        var texts = one.Descendants(Svg + "g").Single(g => (string?)g.Attribute("id") == "dimensions").Elements(Svg + "text").ToArray();
        Assert.Equal(["12.5", "R3", "0.1", "lost"], texts.Select(text => text.Value));
        Assert.Equal(("120", "180"), ((string?)texts[0].Attribute("x"), (string?)texts[0].Attribute("y")));
    }

    // Item 6: the text of the file does not depend on the culture of the computer.
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
            invariant = SvgExport.Text(sheet);
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            french = SvgExport.Text(sheet);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }

        Assert.Equal(invariant, french);
        Assert.Contains("M 10.25 189.5 L 30.125 190", invariant, StringComparison.Ordinal);
        Assert.Contains(">12.5<", invariant, StringComparison.Ordinal);
    }

    // Items 1 and 3: the export of a Document reads the sheet as GetSheet does; a mesh body gives a skipped entry and a
    // description in the file, and a sheet that is not live gives E-SHEET-NOT-FOUND.
    [Fact]
    public void The_Export_Names_A_Mesh_Body_And_Refuses_A_Sheet_That_Is_Not_Live()
    {
        var mesh = new BodyHandle(Guid.NewGuid());
        var sheet = new SheetRecord(Guid.NewGuid(), "Sheet 1", "A3", false, false, [new SheetView(Guid.NewGuid(), Front, 1, 1, 100, 200)], []);
        var document = new Document();
        document.AddBody(new BodyRecord(mesh, ImportStlCommandHandler.MeshKind));
        document.AddSheet(sheet);

        var (content, skipped, error) = SvgExport.Write(document, sheet.Id);
        var (none, _, missing) = SvgExport.Write(document, Guid.NewGuid());

        Assert.Null(error);
        Assert.Equal(mesh.Id, Assert.Single(skipped).Body);
        var file = XDocument.Parse(Encoding.UTF8.GetString(content!));
        Assert.Contains(file.Descendants(Svg + "desc"), desc => desc.Value.Contains(mesh.Id.ToString(), StringComparison.Ordinal));
        Assert.Equal("420mm", (string?)file.Root!.Attribute("height"));
        Assert.Null(none);
        Assert.Equal(DiagnosticCodes.SheetNotFound, missing?.Code);
    }

    // Items 1 and 2: a box of 20 by 30 in a Front view at 1 : 2 gives four thick lines of 10 by 15 mm around the place of the
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

        var (content, skipped, error) = await session.Read((document, _) => SvgExport.Write(document, sheetId));

        Assert.Null(error);
        Assert.Empty(skipped);
        var text = Encoding.UTF8.GetString(content!);
        var lines = Paths(text).Select(path => (string)path.Attribute("d")!).ToArray();
        Assert.Equal(4, lines.Length);
        var numbers = lines.SelectMany(d => d.Split(' ').Where(part => part is not ("M" or "L")).Select(part => double.Parse(part, CultureInfo.InvariantCulture))).ToArray();
        var xs = numbers.Where((_, i) => i % 2 == 0).ToArray();
        var ys = numbers.Where((_, i) => i % 2 == 1).ToArray();
        Assert.Equal((95.0, 105.0, 122.5, 137.5), (xs.Min(), xs.Max(), ys.Min(), ys.Max()));
        Assert.Contains(">10<", text, StringComparison.Ordinal);
    }

    // Item 7: GET /export/svg gives the file of the writer, a sheet that is not live gives 404 with its code, and a request
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

        using var file = await http.GetAsync($"/export/svg?sheetId={sheetId}");
        using var missing = await http.GetAsync($"/export/svg?sheetId={Guid.NewGuid()}");
        using var bad = await http.GetAsync("/export/svg");

        Assert.Equal(HttpStatusCode.OK, file.StatusCode);
        Assert.Equal("image/svg+xml", file.Content.Headers.ContentType?.MediaType);
        Assert.Equal((await host.Session.Read((document, _) => SvgExport.Write(document, sheetId))).Content, await file.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(DiagnosticCodes.SheetNotFound, JsonDocument.Parse(await missing.Content.ReadAsStringAsync()).RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal(DiagnosticCodes.ApiBadRequest, JsonDocument.Parse(await bad.Content.ReadAsStringAsync()).RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    // Item 7: the command line writes the sheet that --sheet names, before or after --folder; a sheet that is not live gives
    // its code and the exit code 1, and svg with no --sheet, or --sheet with stl, is a usage error.
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
        var svg = Path.Combine(_folder, "sheet.svg");

        var (exit, output) = Run("export", "svg", document, svg, "--sheet", sheetId.ToString());
        var (folderExit, _) = Run("export", "svg", document, svg, "--folder", _folder, "--sheet", sheetId.ToString());
        var (missingExit, missingOutput) = Run("export", "svg", document, svg, "--sheet", Guid.NewGuid().ToString());

        Assert.True(exit == 0, output);
        Assert.Equal(svg, JsonDocument.Parse(output).RootElement.GetProperty("file").GetString());
        Assert.Equal("Sheet 1", XDocument.Load(svg).Root!.Element(Svg + "title")!.Value);
        Assert.Equal(0, folderExit);
        Assert.Equal(1, missingExit);
        Assert.Equal(DiagnosticCodes.SheetNotFound, JsonDocument.Parse(missingOutput).RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(2, Run("export", "svg", document, svg).Exit);
        Assert.Equal(2, Run("export", "stl", document, svg, "--sheet", sheetId.ToString()).Exit);
        Assert.Equal(2, Run("export", "svg", document, svg, "--sheet", "sheet").Exit);
    }

    private static (int Exit, string Output) Run(params string[] args)
    {
        var stdout = new StringWriter();
        var exit = global::Engine.Cli.Cli.Run(args, stdout, new StringWriter());
        return (exit, stdout.ToString());
    }

    // ADR-0164 items 2 and 4 (TASK-0400, findings E215 and E217 of the codebase review of 2026-11-17): a name with U+FFFE and
    // U+FFFF, which the rules of a name accept, gives a file that is XML, with U+FFFD in their place, and a pair of surrogates
    // stays; a number that is not
    // finite is 0. Until TASK-0400 the parse of the file threw, and the file held "NaN".
    [Fact]
    public void A_Name_With_A_Character_That_Xml_Does_Not_Permit_Gives_Xml()
    {
        var name = "Plate " + (char)0xFFFE + (char)0xFFFF + " A&B " + (char)0xD83D + (char)0xDE00;
        Assert.Null(BodyNames.Refusal(name));

        var text = SvgExport.Text(Sheet([new SheetLine(Guid.NewGuid(), double.NaN, 20, 30.5, 20, true, "edge")], null, name));

        var file = XDocument.Parse(text);
        Assert.Equal("Plate " + (char)0xFFFD + (char)0xFFFD + " A&B " + (char)0xD83D + (char)0xDE00, file.Root!.Element(Svg + "title")!.Value);
        Assert.Equal("M 0 190 L 30.5 190", (string?)Paths(text)[0].Attribute("d"));
        Assert.Equal(["0", "0"], new[] { double.NaN, double.NegativeInfinity }.Select(SvgExport.N));
    }

    // ADR-0165 items 3 and 4 (phase R30g, TASK-0405): a centre line is a thin path with the long dash and the dot of ISO 128,
    // after the hidden lines and before the visible lines, and a diameter has the sign \u2300.
    [Fact]
    public void A_Centre_Line_Has_Its_Dashes_And_A_Diameter_Its_Sign()
    {
        var lines = new[]
        {
            new SheetLine(Guid.NewGuid(), 10, 20, 30, 20, true, "edge"),
            new SheetLine(Guid.NewGuid(), 15, 10, 15, 30, true, "centre"),
            new SheetLine(Guid.NewGuid(), 12, 10, 12, 30, false, "edge"),
        };
        var dimension = new SheetDimensionInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "constraint", 120, 30, false, "diameter", 16);

        var text = SvgExport.Text(Sheet(lines, [dimension]));

        var paths = Paths(text);
        Assert.Equal(["3 0.75", "6 0.75 0.125 0.75", null], paths.Select(path => (string?)path.Attribute("stroke-dasharray")));
        Assert.Equal(("0.25", "M 15 200 L 15 180"), ((string?)paths[1].Attribute("stroke-width"), (string?)paths[1].Attribute("d")));
        Assert.Contains(">\u230016<", text, StringComparison.Ordinal);
    }
}
