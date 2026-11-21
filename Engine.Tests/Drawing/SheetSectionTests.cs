using System.Globalization;
using System.Text;
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
using Engine.Kernel;
using Engine.Tests.Geometry;

namespace Engine.Tests.Drawing;

// ADR-0169 (phase R30h, TASK-0411): the section view of a sheet. ADR-0169 names this file in enforced-by. The command and its
// refusals, undo, the document file, the cut, the hatch, the cut line with its arrows and letters, the label, and the three
// files.
public sealed class SheetSectionTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-sheet-section-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private static readonly double[] Front = [1, 0, 0, 0, 0, 1, 0, -1, 0];
    private static readonly double[] Top = [1, 0, 0, 0, 1, 0, 0, 0, 1];
    private static readonly double[] Right = [0, 1, 0, 0, 0, 1, 1, 0, 0];

    private static DocumentSession NewSession() => new(EngineHosting.CreateDefault(new InProcessMeshBackend()));

    private static async Task<Guid> Applied(DocumentSession session, Command command, string output)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Outputs.TryGet<Guid>(output, out var id) ? id : throw new InvalidOperationException($"No output {output}.");
    }

    private static async Task<string> Refused(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.Equal(CommandStatus.Rejected, result.Status);
        return result.Error!.Code;
    }

    private static Task<SheetRecord> Sheet(DocumentSession session) => session.Read((document, _) => document.Sheets.Single());

    private static AddSheetViewCommand View(Guid sheet, double[] d, long numerator = 1, long denominator = 1, double x = 100, double y = 80) => new()
    {
        SheetId = sheet, RightX = d[0], RightY = d[1], RightZ = d[2], UpX = d[3], UpY = d[4], UpZ = d[5], EyeX = d[6], EyeY = d[7], EyeZ = d[8],
        ScaleNumerator = numerator, ScaleDenominator = denominator, X = x, Y = y,
    };

    private static AddSheetSectionCommand Section(Guid sheet, Guid parent, string look, double px = 0, double py = 0, double pz = 0, double x = 200, double y = 100, string? letter = null) => new()
    {
        SheetId = sheet, ViewId = parent, Look = look, PointX = px, PointY = py, PointZ = pz, X = x, Y = y, Letter = letter,
    };

    private static string Text(SheetView view)
        => string.Create(CultureInfo.InvariantCulture, $"{view.Id} [{string.Join(",", view.Direction)}] {view.ScaleNumerator}:{view.ScaleDenominator} ({view.X},{view.Y}) {view.HiddenLines} {view.CentreLines} {view.Section?.Parent} [{(view.Section is null ? "" : string.Join(",", view.Section.Point))}] {view.Section?.Letter}");

    // Items 1 and 2: a section of Front with the look "left" has the frame of Right, and one with the look "down" the frame of
    // Top; with the look "right" and "up" the eye is on the other side. Each takes the scale of its parent, no hidden lines,
    // the centre lines, its point and the first free letter, or the letter that the command gives. An undo removes the section.
    [Fact]
    public async Task A_Section_Has_The_Frame_Of_Its_Look_The_Scale_Of_Its_Parent_And_A_Letter()
    {
        var session = NewSession();
        var sheet = await Applied(session, new CreateSheetCommand { SheetName = "Sheet 1", Paper = "A3" }, "sheetId");
        var front = await Applied(session, View(sheet, Front, 1, 2), "viewId");

        var left = await session.Apply(Section(sheet, front, "left", 1, 2, 3, 250, 80));
        var down = await Applied(session, Section(sheet, front, "down", 0, 0, 4.5, 100, 200), "viewId");
        var right = await Applied(session, Section(sheet, front, "right", letter: "Z"), "viewId");
        var up = await Applied(session, Section(sheet, front, "up"), "viewId");

        Assert.Equal(CommandStatus.Applied, left.Status);
        Assert.True(left.Outputs.TryGet<string>("letter", out var letter));
        Assert.Equal("A", letter);
        var views = (await Sheet(session)).Views;
        Assert.Equal(5, views.Count);
        var a = views[1];
        Assert.True(left.Outputs.TryGet<Guid>("viewId", out var leftId));
        Assert.Equal(leftId, a.Id);
        Assert.Equal(Right, a.Direction);
        Assert.Equal((1L, 2L, 250.0, 80.0, false, true), (a.ScaleNumerator, a.ScaleDenominator, a.X, a.Y, a.HiddenLines, a.CentreLines));
        Assert.Equal((front, "A"), (a.Section!.Parent, a.Section.Letter));
        Assert.Equal([1.0, 2, 3], a.Section.Point);
        Assert.Equal(down, views[2].Id);
        Assert.Equal(Top, views[2].Direction);
        Assert.Equal("B", views[2].Section!.Letter);
        Assert.Equal(right, views[3].Id);
        Assert.Equal(new double[] { 0, -1, 0, 0, 0, 1, -1, 0, 0 }, views[3].Direction);
        Assert.Equal("Z", views[3].Section!.Letter);
        Assert.Equal(up, views[4].Id);
        Assert.Equal(new double[] { 1, 0, 0, 0, -1, 0, 0, 0, -1 }, views[4].Direction);
        Assert.Equal("C", views[4].Section!.Letter);
        Assert.Null(views[0].Section);

        await Applied(session, new UndoCommand(), "commandId");
        Assert.Equal(4, (await Sheet(session)).Views.Count);
    }

    // Item 2: the default letters leave out I, O and Q, and a sheet whose default letters are each in use refuses a section
    // with no letter and takes one with a free letter.
    [Fact]
    public async Task The_Default_Letters_Leave_Out_I_O_And_Q()
    {
        var session = NewSession();
        var sheet = await Applied(session, new CreateSheetCommand { SheetName = "Sheet 1", Paper = "A0" }, "sheetId");
        var front = await Applied(session, View(sheet, Front), "viewId");
        for (var i = 0; i < SectionCut.DefaultLetters.Length; i++)
            await Applied(session, Section(sheet, front, "left"), "viewId");

        var letters = string.Concat((await Sheet(session)).Views.Skip(1).Select(view => view.Section!.Letter));
        Assert.Equal("ABCDEFGHJKLMNPRSTUVWXYZ", letters);
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, Section(sheet, front, "left")));
        await Applied(session, Section(sheet, front, "left", letter: "Q"), "viewId");
    }

    // Item 2: each refusal, in the order of the record.
    [Fact]
    public async Task A_Section_Refuses_Each_Value_That_Is_Not_Valid()
    {
        var session = NewSession();
        var sheet = await Applied(session, new CreateSheetCommand { SheetName = "Sheet 1" }, "sheetId");
        var front = await Applied(session, View(sheet, Front), "viewId");
        var slant = await Applied(session, View(sheet, [1, 1, 0, -1, 1, 0, 0, 0, 1]), "viewId");
        await Applied(session, Section(sheet, front, "left", letter: "K"), "viewId");

        Assert.Equal(DiagnosticCodes.SheetNotFound, await Refused(session, Section(Guid.NewGuid(), front, "left")));
        for (var i = 0; i < 1024; i++)
            await session.Apply(new RedoCommand()); // as for a view: refused Redo commands fill the cache of the results
        Assert.Equal(DiagnosticCodes.SheetIdInUse, await Refused(session, Section(sheet, front, "left") with { CommandId = front }));
        Assert.Equal(DiagnosticCodes.SheetViewNotFound, await Refused(session, Section(sheet, Guid.NewGuid(), "left")));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, Section(sheet, front, "north")));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, Section(sheet, front, "Left")));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, Section(sheet, front, "left", px: double.NaN)));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, Section(sheet, front, "left", pz: double.PositiveInfinity)));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, Section(sheet, slant, "left")));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, Section(sheet, front, "left", letter: "a")));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, Section(sheet, front, "left", letter: "AB")));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, Section(sheet, front, "left", letter: "")));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, Section(sheet, front, "left", letter: "K")));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, Section(sheet, front, "left", x: 300)));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, Section(sheet, front, "left", y: double.NaN)));
        Assert.Equal(3, (await Sheet(session)).Views.Count);
    }

    // Item 2: the frame of a section is null for a parent of a length that is not rational and for a look that is not one of
    // the four, and the nine numbers are null when a double does not hold one of them exactly.
    [Fact]
    public void The_Frame_Of_A_Section_Needs_An_Exact_Length_And_Exact_Doubles()
    {
        static Vector3R V(Rational x, Rational y, Rational z) => new(x, y, z);
        var front = ViewFrame.Of(StandardView.Front);
        Assert.Equal(ViewFrame.Of(StandardView.Right), SectionCut.Frame(front, "left"));
        Assert.Equal(ViewFrame.Of(StandardView.Top), SectionCut.Frame(front, "down"));
        Assert.Null(SectionCut.Frame(front, "across"));
        Assert.Null(SectionCut.Frame(new ViewFrame(V(1, 1, 0), V(-1, 1, 0), V(0, 0, 1)), "left"));
        var third = Rational.One / 3;
        Assert.Null(SectionCut.Direction(new ViewFrame(V(third, 0, 0), V(0, third, 0), V(0, 0, 1))));
        Assert.Equal([2.0, 0, 0, 0, 2, 0, 0, 0, 1], SectionCut.Direction(new ViewFrame(V(2, 0, 0), V(0, 2, 0), V(0, 0, 1)))!.ToArray());
    }

    // Item 1: an open of the document file replays the commands and gives the same sections.
    [Fact]
    public async Task An_Open_Of_The_File_Gives_The_Same_Sections()
    {
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, "sections" + DocumentFile.Extension);
        var live = NewSession();
        DocumentFile.Create(live, path);
        var sheet = await Applied(live, new CreateSheetCommand { SheetName = "Sheet 1", Paper = "A2" }, "sheetId");
        var front = await Applied(live, View(sheet, Front, 3, 7), "viewId");
        var a = await Applied(live, Section(sheet, front, "left", 1.25, -2, 3.5), "viewId");
        await Applied(live, Section(sheet, a, "up", letter: "M"), "viewId");
        var expected = (await Sheet(live)).Views.Select(Text).ToArray();
        live.Close();

        var opened = NewSession();
        await DocumentFile.Open(opened, path);
        var actual = (await Sheet(opened)).Views.Select(Text).ToArray();
        opened.Close();

        Assert.Equal(3, actual.Length);
        Assert.Equal(expected, actual);
    }

    // A plate of 60 by 40 by 10 from a sketch on XY with a through-hole of radius 8 at (30, 20), and a sheet A3 with Top at
    // (200, 200), 1 : 1.
    private static async Task<(DocumentSession Session, Guid Sheet, Guid Top, Guid Plate)> Plate(ManifoldGeometryBackend backend)
    {
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var sketch = await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }, "sketchId");
        var points = new List<Guid>();
        foreach (var (x, y) in new[] { (0.0, 0.0), (60.0, 0.0), (60.0, 40.0), (0.0, 40.0) })
            points.Add(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }, "pointId"));
        for (var i = 0; i < 4; i++)
            await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % 4] }, "lineId");
        var plate = await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 10 }, "bodyId");
        var tool = await Applied(session, new CreateCylinderCommand { Radius = 8, Height = 20 }, "bodyId");
        tool = await Applied(session, new TranslateCommand { BodyId = tool, Dx = 30, Dy = 20, Dz = 5 }, "bodyId");
        plate = await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = tool }, "bodyId");
        var sheet = await Applied(session, new CreateSheetCommand { SheetName = "Plate", Paper = "A3" }, "sheetId");
        var top = await Applied(session, View(sheet, Top, x: 200, y: 200), "viewId");
        return (session, sheet, top, plate);
    }

    private static async Task<SheetInfo> Read(DocumentSession session, Guid sheet)
    {
        var result = await session.Query<SheetInfo>(new GetSheetQuery { SheetId = sheet });
        Assert.True(result.Error is null, result.Error?.Message);
        return result.Result!;
    }

    private static string P(double x, double y) => string.Create(CultureInfo.InvariantCulture, $"{Math.Round(x, 9)},{Math.Round(y, 9)}");

    private static string[] Lines(SheetViewInfo view, string kind)
        => [.. view.Lines.Where(line => line.Kind == kind).Select(line => P(line.FromX, line.FromY) + " " + P(line.ToX, line.ToY))];

    // Items 3 to 5: a section of Top through the axis of the hole with the look "up" has the frame of Front and cuts the plate
    // at y = 20. Its section shows the two cut faces, x from 0 to 22 and from 38 to 60, z from 0 to 10, at (200, 100): on the
    // paper from 170 to 192 and from 208 to 230 across, and from 95 to 105 up. The hatch goes up to the right, 2.5 mm apart,
    // inside the cut faces only, the axis of the hole shows, and the label "A-A" is 10 mm under the box. Top shows the cut line at y = 200 from 165 to
    // 235, its thick ends, two arrows of 7 mm up, and the letter A 4 mm past each head.
    [NativeManifoldFact]
    public async Task A_Section_Gives_Its_Cut_Its_Hatch_Its_Cut_Line_And_Its_Label()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, sheet, top, plate) = await Plate(backend);
        var section = await Applied(session, Section(sheet, top, "up", 30, 20, 0, 200, 100), "viewId");

        var read = await Read(session, sheet);

        var parent = read.Views[0];
        Assert.Equal(["165,200 235,200"], Lines(parent, "cut"));
        Assert.Equal(["165,200 170,200", "230,200 235,200"], Lines(parent, "cut-end"));
        Assert.Equal(["167.5,200 167.5,207", "232.5,200 232.5,207"], Lines(parent, "arrow"));
        Assert.All(parent.Lines.Where(line => line.Kind is "cut" or "cut-end" or "arrow"), line => Assert.Equal(section, line.Body));
        // Finding E224 (TASK-0416): the size 5 is the height of a capital, so the base line is 2.5 mm under the place.
        Assert.Equal([new SheetText(167.5, 211 - 2.5, "A", 5), new SheetText(232.5, 211 - 2.5, "A", 5)], parent.Texts);
        Assert.Empty(Lines(parent, "hatch"));
        Assert.Null(parent.Section);

        var cut = read.Views[1];
        Assert.Equal((top, "A"), (cut.Section!.Parent, cut.Section.Letter));
        Assert.Empty(cut.Skipped);
        Assert.False(cut.HiddenLines);
        Assert.DoesNotContain(cut.Lines, line => !line.Visible);
        Assert.Equal([new SheetText(200, 85, "A-A", 5)], cut.Texts);
        // ADR-0169 item 3: the edges of the cut faces along the hole give its axis, from z = 0 to 10 and 3 mm past each end.
        Assert.Equal(["200,92 200,108"], Lines(cut, "centre"));
        var edges = cut.Lines.Where(line => line.Kind is "edge" or "outline").ToList();
        Assert.Equal((170.0, 95.0, 230.0, 105.0), (edges.Min(l => Math.Min(l.FromX, l.ToX)), edges.Min(l => Math.Min(l.FromY, l.ToY)), edges.Max(l => Math.Max(l.FromX, l.ToX)), edges.Max(l => Math.Max(l.FromY, l.ToY))));
        var hatch = cut.Lines.Where(line => line.Kind == "hatch").ToList();
        Assert.All(hatch, line => Assert.Equal(plate, line.Body));
        Assert.All(hatch, line => Assert.Equal(line.ToX - line.FromX, line.ToY - line.FromY, 9));
        Assert.All(hatch, line => Assert.True(line.ToX > line.FromX));
        bool Inside(double x, double y) => y >= 95 - 1e-9 && y <= 105 + 1e-9 && ((x >= 170 - 1e-9 && x <= 192 + 1e-9) || (x >= 208 - 1e-9 && x <= 230 + 1e-9));
        Assert.All(hatch, line => Assert.True(Inside(line.FromX, line.FromY) && Inside(line.ToX, line.ToY), P(line.FromX, line.FromY) + " " + P(line.ToX, line.ToY)));
        Assert.DoesNotContain(hatch, line => line.FromX < 200 && line.ToX > 200);
        // Each hatch line is x - y = n 2.5 sqrt 2 on the paper; the two faces of 22 by 10 hold 9 lines each.
        var step = 2.5 * Math.Sqrt(2);
        Assert.All(hatch, line => Assert.Equal(Math.Round((line.FromX - line.FromY) / step), (line.FromX - line.FromY) / step, 6));
        Assert.Equal(9, hatch.Count(line => line.FromX < 200));
        Assert.Equal(9, hatch.Count(line => line.FromX > 200));
    }

    // Items 3 and 4: a section of Front with the look "down" through z = 5 has the frame of Top and cuts the plate across the
    // axis of the hole. Its cut face is the rectangle of 60 by 40 with the hole, at (200, 100): the edge of the hole is one
    // whole visible circle of radius 8, and the hatch stays in the rectangle and out of the hole, so a hatch line that meets
    // the hole stops at it and goes on past it. The ends of a hatch line on the hole lie on the chords of the facets.
    [NativeManifoldFact]
    public async Task A_Cut_Across_A_Hole_Gives_Its_Circle_And_A_Hatch_Around_It()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, sheet, _, _) = await Plate(backend);
        var front = await Applied(session, View(sheet, Front, x: 200, y: 250), "viewId");
        await Applied(session, Section(sheet, front, "down", 0, 0, 5, 200, 100), "viewId");

        var cut = (await Read(session, sheet)).Views[2];

        Assert.Equal(Top, cut.Direction);
        var circle = Assert.Single(cut.Lines, line => line.Circle is not null && line.Kind == "edge");
        Assert.Equal((200.0, 100.0, 8.0, true), (circle.Circle!.X, circle.Circle.Y, circle.Circle.Radius, circle.Visible && circle.FromX == circle.ToX && circle.FromY == circle.ToY));
        var hatch = cut.Lines.Where(line => line.Kind == "hatch").ToList();
        static double Distance(double x, double y) => Math.Sqrt(((x - 200) * (x - 200)) + ((y - 100) * (y - 100)));
        Assert.All(hatch, line => Assert.True(line.FromX >= 170 - 1e-9 && line.ToX <= 230 + 1e-9 && Math.Min(line.FromY, line.ToY) >= 80 - 1e-9 && Math.Max(line.FromY, line.ToY) <= 120 + 1e-9));
        Assert.All(hatch, line => Assert.True(Distance((line.FromX + line.ToX) / 2, (line.FromY + line.ToY) / 2) > 7.5, P(line.FromX, line.FromY) + " " + P(line.ToX, line.ToY)));
        Assert.True(hatch.Count(line => Distance(line.FromX, line.FromY) < 8 + 1e-9 || Distance(line.ToX, line.ToY) < 8 + 1e-9) >= 8);
    }

    // Item 3: a cut plane that leaves no material of a body gives no line and the body as skipped, with the label at the place
    // of the view; a cut plane that does not cross a body gives its lines with no hatch; with the parent removed the section
    // keeps its cut and no view shows its cut line.
    [NativeManifoldFact]
    public async Task A_Cut_That_Removes_A_Body_Skips_It_And_One_Behind_It_Keeps_Its_Lines()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, sheet, top, plate) = await Plate(backend);
        await Applied(session, Section(sheet, top, "up", 0, 50, 0, 100, 100), "viewId");
        await Applied(session, Section(sheet, top, "down", 0, 50, 0, 300, 100), "viewId");

        var read = await Read(session, sheet);

        var removed = read.Views[1];
        Assert.Empty(removed.Lines);
        Assert.Contains("ADR-0169 item 3", Assert.Single(removed.Skipped).Reason);
        Assert.Equal(plate, removed.Skipped[0].Body);
        Assert.Equal([new SheetText(100, 90, "A-A", 5)], removed.Texts);
        var behind = read.Views[2];
        Assert.Empty(behind.Skipped);
        Assert.Contains(behind.Lines, line => line.Kind == "edge");
        Assert.Empty(Lines(behind, "hatch"));

        await Applied(session, new RemoveSheetViewCommand { SheetId = sheet, ViewId = top }, "viewId");
        var orphan = await Read(session, sheet);
        Assert.Equal(2, orphan.Views.Count);
        Assert.All(orphan.Views, view => Assert.DoesNotContain(view.Lines, line => line.Kind is "cut" or "cut-end" or "arrow"));
        Assert.Equal("B-B", Assert.Single(orphan.Views[1].Texts).Text);
    }

    // Item 4: two bodies that the plane cuts have hatches in two directions: up to the right for the first, down to the right
    // for the next.
    [NativeManifoldFact]
    public async Task Two_Cut_Bodies_Have_Hatches_In_Two_Directions()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var first = await Applied(session, new CreateBoxCommand { SizeX = 20, SizeY = 20, SizeZ = 20 }, "bodyId");
        var second = await Applied(session, new CreateBoxCommand { SizeX = 20, SizeY = 20, SizeZ = 20 }, "bodyId");
        second = await Applied(session, new TranslateCommand { BodyId = second, Dx = 30, Dy = 0, Dz = 0 }, "bodyId");
        var sheet = await Applied(session, new CreateSheetCommand { SheetName = "Two", Paper = "A3" }, "sheetId");
        var front = await Applied(session, View(sheet, Front, x: 200, y: 200), "viewId");
        await Applied(session, Section(sheet, front, "down", 0, 0, 0, 200, 100), "viewId");

        var cut = (await Read(session, sheet)).Views[1];

        var hatch = cut.Lines.Where(line => line.Kind == "hatch").ToList();
        var ordered = new[] { first, second }.OrderBy(id => id).ToArray();
        Assert.Contains(hatch, line => line.Body == ordered[0]);
        Assert.Contains(hatch, line => line.Body == ordered[1]);
        Assert.All(hatch.Where(line => line.Body == ordered[0]), line => Assert.Equal(line.ToX - line.FromX, line.ToY - line.FromY, 9));
        Assert.All(hatch.Where(line => line.Body == ordered[1]), line => Assert.Equal(line.ToX - line.FromX, line.FromY - line.ToY, 9));
    }

    // Item 6: the SVG, the DXF and the PDF file of the sheet of the plate hold the hatch, the cut line, the thick ends, the
    // arrows with their heads and the texts, in the order of the record.
    [NativeManifoldFact]
    public async Task The_Three_Files_Draw_The_Hatch_The_Cut_Line_The_Arrows_And_The_Texts()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, sheet, top, _) = await Plate(backend);
        await Applied(session, Section(sheet, top, "up", 30, 20, 0, 200, 100), "viewId");
        var read = await Read(session, sheet);

        var svg = SvgExport.Text(read);
        var hatchPath = "<path stroke-width=\"0.25\" d=\"M ";
        Assert.Contains("stroke-dasharray=\"6 0.75 0.125 0.75\" d=\"M 165 ", svg, StringComparison.Ordinal);
        Assert.Contains("<path stroke-width=\"0.5\" d=\"M 165 ", svg, StringComparison.Ordinal);
        Assert.Contains("<path fill=\"black\" stroke=\"none\" d=\"M 167.5 ", svg, StringComparison.Ordinal);
        Assert.Contains(">A-A</text>", svg, StringComparison.Ordinal);
        Assert.Equal(2, svg.Split(">A</text>").Length - 1);
        var section = svg[svg.IndexOf($"view-{read.Views[1].ViewId}", StringComparison.Ordinal)..];
        Assert.True(section.IndexOf(hatchPath, StringComparison.Ordinal) is var h && h > 0 && h < section.IndexOf("<path stroke-width=\"0.5\"", StringComparison.Ordinal));

        var dxf = DxfExport.Text(read);
        Assert.Contains("\n2\nHATCH\n70\n0\n62\n8\n6\nCONTINUOUS\n", dxf, StringComparison.Ordinal);
        Assert.Contains("\n2\nCUT\n70\n0\n62\n7\n6\nCENTER\n", dxf, StringComparison.Ordinal);
        Assert.Contains("0\nLINE\n8\nCUT\n10\n165\n", dxf, StringComparison.Ordinal);
        Assert.Contains("0\nLINE\n8\nHATCH\n", dxf, StringComparison.Ordinal);
        Assert.Contains("0\nSOLID\n8\nTEXT\n10\n167.5\n20\n207\n", dxf, StringComparison.Ordinal);
        Assert.Contains("1\nA-A\n7\nSTANDARD\n72\n1\n", dxf, StringComparison.Ordinal);

        var pdf = PdfExport.Text(read);
        Assert.Contains("167.5 207 m ", pdf, StringComparison.Ordinal);
        Assert.Contains(" l h f\n", pdf, StringComparison.Ordinal);
        Assert.Contains("(A-A) Tj", pdf, StringComparison.Ordinal);
        Assert.Contains("0.25 w [] 0 d\n", pdf, StringComparison.Ordinal);
        Assert.Equal(Encoding.ASCII.GetByteCount(pdf), pdf.Length);
    }

    // Finding E224 of the codebase review of 2026-11-18 (b) (TASK-0416): the Size of a text is the height of a capital letter in
    // the three files. The DXF file writes it as the height (group 40); the SVG and the PDF file write the size of the font
    // that gives a capital of Helvetica of that height, Size / 0.718.
    [NativeManifoldFact]
    public async Task The_Size_Of_A_Text_Is_The_Height_Of_A_Capital_In_The_Three_Files()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, sheet, top, _) = await Plate(backend);
        await Applied(session, Section(sheet, top, "up", 30, 20, 0, 200, 100), "viewId");
        var read = await Read(session, sheet);
        var font = (5 / 0.718).ToString("R", CultureInfo.InvariantCulture);

        var svg = SvgExport.Text(read);
        Assert.Contains($"font-size=\"{font}\">A-A</text>", svg, StringComparison.Ordinal);
        Assert.Equal(2, svg.Split($"font-size=\"{font}\">A</text>").Length - 1);
        var labels = PdfExport.Text(read).Split('\n').Where(line => line.EndsWith("(A-A) Tj ET", StringComparison.Ordinal) || line.EndsWith("(A) Tj ET", StringComparison.Ordinal)).ToList();
        Assert.Equal(3, labels.Count);
        Assert.All(labels, line => Assert.StartsWith($"BT /F1 {font} Tf ", line, StringComparison.Ordinal));
        Assert.Contains("\n40\n5\n1\nA-A\n", DxfExport.Text(read), StringComparison.Ordinal);
    }

    // Finding E221 of the codebase review of 2026-11-18 (b) (TASK-0416): a section whose point is far from the part, x = 1e308
    // on a Top view at 2 : 1, would put its cut line at x = +∞ on the paper. The parent then gives no cut line, no arrows and no
    // letters of it, the read is valid JSON, and the SVG file has no arrow head at 0.
    [Fact]
    public async Task A_Cut_Line_At_A_Place_That_Is_Not_Finite_Is_Not_Given()
    {
        var session = NewSession();
        await Applied(session, new CreateBoxCommand { SizeX = 60, SizeY = 40, SizeZ = 10 }, "bodyId");
        var sheet = await Applied(session, new CreateSheetCommand { SheetName = "Far", Paper = "A3" }, "sheetId");
        var top = await Applied(session, View(sheet, Top, 2, 1, 200, 200), "viewId");
        await Applied(session, Section(sheet, top, "left", 1e308, 0, 0, 100, 100), "viewId");

        var read = await Read(session, sheet);

        var parent = read.Views[0];
        Assert.Contains(parent.Lines, line => line.Kind == "edge" || line.Kind == "outline");
        Assert.DoesNotContain(parent.Lines, line => line.Kind is "cut" or "cut-end" or "arrow");
        Assert.Empty(parent.Texts);
        Assert.All(read.Views.SelectMany(view => view.Lines), line => Assert.True(double.IsFinite(line.FromX) && double.IsFinite(line.FromY) && double.IsFinite(line.ToX) && double.IsFinite(line.ToY)));
        Assert.All(read.Views.SelectMany(view => view.Texts), text => Assert.True(double.IsFinite(text.X) && double.IsFinite(text.Y)));
        Assert.Contains("\"sheetId\"", System.Text.Json.JsonSerializer.Serialize(read, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)), StringComparison.Ordinal);
        Assert.DoesNotContain("<path fill=\"black\"", SvgExport.Text(read), StringComparison.Ordinal);
    }

    // Finding E222 of the codebase review of 2026-11-18 (b) (TASK-0416): the read keeps the cut of a section. A second read, and
    // a read after a move of a view, make no new cut and give the same lines; a read after a change of the body cuts again;
    // and an export, which reads the sheet, takes the kept cut.
    [NativeManifoldFact]
    public async Task A_Read_Keeps_The_Cut_Until_A_Body_Changes()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, sheet, top, plate) = await Plate(backend);
        var section = await Applied(session, Section(sheet, top, "up", 30, 20, 0, 200, 100), "viewId");

        var first = await Read(session, sheet);
        Assert.Equal(1, SectionCut.CutsOf(section));
        var again = await Read(session, sheet);
        await Applied(session, new MoveSheetViewsCommand { SheetId = sheet, ViewId = top, X = 210, Y = 200 }, "viewId");
        var moved = await Read(session, sheet);
        await session.Read((document, _) => SvgExport.Write(document, sheet));
        Assert.Equal(1, SectionCut.CutsOf(section));
        string Cut(SheetInfo read) => string.Join(" ", read.Views[1].Lines.Select(line => P(line.FromX, line.FromY) + " " + P(line.ToX, line.ToY)));
        Assert.Equal(Cut(first), Cut(again));
        Assert.Equal(Cut(first), Cut(moved));

        await Applied(session, new TranslateCommand { BodyId = plate, Dx = 0, Dy = 1, Dz = 0 }, "bodyId");
        var changed = await Read(session, sheet);
        Assert.Equal(2, SectionCut.CutsOf(section));
        Assert.NotEqual(Cut(first), Cut(changed));
    }

    // Finding E226 of the codebase review of 2026-11-20 (TASK-0426): the read keeps the cut of each body of a section, also when
    // one read needs more than 16 cuts. A plate and 16 boxes that the plane of the section cuts: the second read makes no new
    // cut. Until TASK-0426 the read kept the last 16 cuts only, so each of the 17 lookups of the second read missed. A read
    // keeps no cut of a body that is gone: after a subtract of a box from the plate, the view keeps one cut for each live body.
    [NativeManifoldFact]
    public async Task A_Read_Keeps_The_Cut_Of_Each_Body_Also_Past_Sixteen_Bodies()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, sheet, top, plate) = await Plate(backend);
        var boxes = new List<Guid>();
        for (var i = 0; i < 16; i++)
        {
            var box = await Applied(session, new CreateBoxCommand { SizeX = 2, SizeY = 40, SizeZ = 2 }, "bodyId");
            boxes.Add(await Applied(session, new TranslateCommand { BodyId = box, Dx = 70 + (3 * i), Dy = 20, Dz = 1 }, "bodyId"));
        }

        var section = await Applied(session, Section(sheet, top, "up", 30, 20, 0, 200, 100), "viewId");

        await Read(session, sheet);
        var first = SectionCut.CutsOf(section);
        Assert.Equal(17, first);
        await Read(session, sheet);
        Assert.Equal(first, SectionCut.CutsOf(section));
        Assert.Equal(17, SectionCut.KeptOf(section));

        await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = boxes[0] }, "bodyId");
        await Read(session, sheet);
        var live = await session.Read((document, _) => document.Bodies.Count);
        Assert.Equal(16, live);
        Assert.Equal(live, SectionCut.KeptOf(section));
    }

    // Finding E223 of the codebase review of 2026-11-18 (b) (TASK-0416, Q1 A of the owner): a section through the axis of a cone
    // shows the axis in each look. A cone of radius 10 and height 20 at the origin: a section of Front with the look "left" and
    // a section of Top with the look "up", each through (0, 0, 0), give one centre line along the axis, 20 mm long plus 3 mm at
    // each end. Until TASK-0416 the section of Top gave none, because the edges of its cut are slanted lines of the side.
    [NativeManifoldFact]
    public async Task A_Section_Through_The_Axis_Of_A_Cone_Shows_The_Axis_In_Each_Look()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        await Applied(session, new CreateConeCommand { Radius = 10, Height = 20 }, "bodyId");
        var sheet = await Applied(session, new CreateSheetCommand { SheetName = "Cone", Paper = "A3" }, "sheetId");
        var front = await Applied(session, View(sheet, Front, x: 100, y: 200), "viewId");
        var top = await Applied(session, View(sheet, Top, x: 300, y: 200), "viewId");
        await Applied(session, Section(sheet, front, "left", 0, 0, 0, 100, 80), "viewId");
        await Applied(session, Section(sheet, top, "up", 0, 0, 0, 300, 80), "viewId");

        var read = await Read(session, sheet);

        foreach (var cut in read.Views.Skip(2))
        {
            var axis = Assert.Single(cut.Lines, line => line.Kind == "centre");
            Assert.Equal(axis.FromX, axis.ToX, 9);
            Assert.Equal(26, Math.Abs(axis.ToY - axis.FromY), 9);
        }
    }
}
