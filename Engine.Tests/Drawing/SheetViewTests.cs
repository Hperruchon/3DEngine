using System.Diagnostics;
using System.Globalization;
using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;
using Xunit.Abstractions;

namespace Engine.Tests.Drawing;

// ADR-0155 (phase P10.2, TASK-0393): the views of a sheet. ADR-0155 names this file in enforced-by. The three commands, each
// refusal, undo and redo, the document file, and the read of the lines of each view on the paper.
public sealed class SheetViewTests(ITestOutputHelper output) : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-sheet-view-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    // The right, the up and the direction toward the eye of the standard views (ADR-0142 item 2).
    private static readonly double[] Front = [1, 0, 0, 0, 0, 1, 0, -1, 0];
    private static readonly double[] Top = [1, 0, 0, 0, 1, 0, 0, 0, 1];
    private static readonly double[] Right = [0, 1, 0, 0, 0, 1, 1, 0, 0];

    public static TheoryData<double[]> StandardViews => new()
    {
        Front,
        { [-1, 0, 0, 0, 0, 1, 0, 1, 0] },
        Right,
        { [0, -1, 0, 0, 0, 1, -1, 0, 0] },
        Top,
        { [1, 0, 0, 0, -1, 0, 0, 0, -1] },
    };

    private static (DocumentSession Session, InMemoryEventSink Events) NewSession()
    {
        var kit = EngineHosting.CreateDefault(new InProcessMeshBackend());
        return (new DocumentSession(kit), kit.Events);
    }

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

    private static Task<SheetRecord> Sheet(DocumentSession session)
        => session.Read((document, _) => document.Sheets.Single());

    // A command with the first nine numbers of the direction; a missing number is NaN.
    private static AddSheetViewCommand View(Guid sheet, double[] direction, long numerator = 1, long denominator = 1, double x = 100, double y = 80)
    {
        double D(int i) => i < direction.Length ? direction[i] : double.NaN;
        return new()
        {
            SheetId = sheet, RightX = D(0), RightY = D(1), RightZ = D(2), UpX = D(3), UpY = D(4), UpZ = D(5), EyeX = D(6), EyeY = D(7), EyeZ = D(8),
            ScaleNumerator = numerator, ScaleDenominator = denominator, X = x, Y = y,
        };
    }

    // A view as text, to compare two records, with the culture of no country.
    private static string Text(SheetView view)
        => string.Create(CultureInfo.InvariantCulture, $"{view.Id} [{string.Join(",", view.Direction.Select(value => value.ToString(CultureInfo.InvariantCulture)))}] {view.ScaleNumerator}:{view.ScaleDenominator} ({view.X},{view.Y})");

    private static async Task<SheetInfo> Read(DocumentSession session, Guid sheet)
    {
        var result = await session.Query<SheetInfo>(new GetSheetQuery { SheetId = sheet });
        Assert.True(result.Error is null, result.Error?.Message);
        return result.Result!;
    }

    // Item 1: AddSheetView gives a view with the identifier of its command, its nine numbers, its scale and its place, and the
    // event sheet.changed carries the views.
    [Fact]
    public async Task An_Added_View_Has_Its_Record_And_The_Event_Sheet_Changed()
    {
        var (session, events) = NewSession();
        var sheet = await Applied(session, new CreateSheetCommand { SheetName = "Sheet 1" }, "sheetId");
        var command = View(sheet, Front, 1, 2, 148.5, 105);

        var id = await Applied(session, command, "viewId");

        Assert.Equal(command.CommandId, id);
        var view = Assert.Single((await Sheet(session)).Views);
        Assert.Equal($"{id} [1,0,0,0,0,1,0,-1,0] 1:2 (148.5,105)", Text(view));
        var changed = (IReadOnlyDictionary<string, object?>)events.Snapshot().Last(record => record.Kind == "sheet.changed").Payload!;
        Assert.Equal(Text(view), Text(Assert.Single((IReadOnlyList<SheetView>)changed["views"]!)));
    }

    // Item 1: each standard view of ADR-0142 item 2 is a valid direction.
    [Theory]
    [MemberData(nameof(StandardViews))]
    public async Task Each_Of_The_Six_Standard_Frames_Is_A_Valid_Direction(double[] direction)
    {
        var (session, _) = NewSession();
        var sheet = await Applied(session, new CreateSheetCommand { SheetName = "Sheet 1" }, "sheetId");

        await Applied(session, View(sheet, direction), "viewId");

        Assert.Equal(direction, Assert.Single((await Sheet(session)).Views).Direction);
    }

    // Items 1, 2, 4 and 6: a sheet that is not live, the identifier of a command in the log, a direction that is not an exact
    // valid frame, a scale part that is not greater than zero, a place off the paper, and a view that is not on the sheet are
    // refused, each with its code, and the sheet does not change.
    [Fact]
    public async Task Each_Wrong_View_Command_Gives_Its_Code()
    {
        var (session, _) = NewSession();
        var sheet = await Applied(session, new CreateSheetCommand { SheetName = "Sheet 1", Paper = "A3", Landscape = false }, "sheetId");
        var view = await Applied(session, View(sheet, Front), "viewId");
        var before = Text(Assert.Single((await Sheet(session)).Views));

        Assert.Equal(DiagnosticCodes.SheetNotFound, await Refused(session, View(Guid.NewGuid(), Front)));
        Assert.Equal(DiagnosticCodes.SheetNotFound, await Refused(session, new MoveSheetViewCommand { SheetId = Guid.NewGuid(), ViewId = view, X = 1, Y = 1 }));
        Assert.Equal(DiagnosticCodes.SheetNotFound, await Refused(session, new RemoveSheetViewCommand { SheetId = Guid.NewGuid(), ViewId = view }));
        foreach (var direction in new double[][]
        {
            [1, 0, 0, 0.5, 0, 1, 0, -1, 0], // no right angle
            [1, 0, 0, 0, 0, 2, 0, -1, 0], // two lengths
            [1, 0, 0, 0, 0, 1, 0, 1, 0], // the eye on the wrong side
            [1, 0, 0, 0, 0, 1, 0, -1, 1], // the eye not along right x up
            [1, 0, 0, 0, 0, 1, 0, -1, double.NaN],
            [1, 0, 0, 0, 0, 1, double.PositiveInfinity, -1, 0],
            [0, 0, 0, 0, 0, 0, 0, -1, 0], // no right
            [0.6, 0.8, 0, 0, 0, 1, 0.8, -0.6, 0], // one length only in decimal, not in binary
        })
        {
            Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, View(sheet, direction)));
        }
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, View(sheet, Front, 0, 1)));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, View(sheet, Front, 1, 0)));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, View(sheet, Front, -1, 2)));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, View(sheet, Front, 1, -2)));
        // A3 portrait is 297 mm across and 420 mm up.
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, View(sheet, Front, x: 298, y: 100)));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, View(sheet, Front, x: 100, y: 421)));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, View(sheet, Front, x: -0.5, y: 100)));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, View(sheet, Front, x: double.NaN, y: 100)));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, new MoveSheetViewCommand { SheetId = sheet, ViewId = view, X = 100, Y = 420.5 }));
        Assert.Equal(DiagnosticCodes.SheetViewNotFound, await Refused(session, new MoveSheetViewCommand { SheetId = sheet, ViewId = Guid.NewGuid(), X = 1, Y = 1 }));
        Assert.Equal(DiagnosticCodes.SheetViewNotFound, await Refused(session, new RemoveSheetViewCommand { SheetId = sheet, ViewId = Guid.NewGuid() }));
        for (var i = 0; i < 1024; i++)
            await session.Apply(new RedoCommand()); // refused Redo commands fill the cache of the results of the session
        Assert.Equal(DiagnosticCodes.SheetIdInUse, await Refused(session, View(sheet, Top) with { CommandId = view }));

        // The corners of the paper are on it.
        await Applied(session, View(sheet, Top, x: 0, y: 0), "viewId");
        await Applied(session, View(sheet, Top, x: 297, y: 420), "viewId");
        Assert.Equal(before, Text((await Sheet(session)).Views[0]));
        Assert.Equal(3, (await Sheet(session)).Views.Count);
    }

    // Items 2 and 3: a move gives the view its new place and keeps the order of the views; a remove takes one view away and
    // keeps the other; an undo of each gives back the record before, and a redo the record after.
    [Fact]
    public async Task Move_And_Remove_Of_A_View_Have_Undo_And_Redo()
    {
        var (session, events) = NewSession();
        var sheet = await Applied(session, new CreateSheetCommand { SheetName = "Sheet 1" }, "sheetId");
        var front = await Applied(session, View(sheet, Front, x: 80, y: 120), "viewId");
        var top = await Applied(session, View(sheet, Top, x: 80, y: 50), "viewId");
        async Task<string> Views()
            => string.Join(" ", (await Sheet(session)).Views.Select(view => string.Create(CultureInfo.InvariantCulture, $"{(view.Id == front ? "front" : view.Id == top ? "top" : "other")}({view.X},{view.Y})")));
        async Task<IReadOnlyList<string>> Kinds(Command command)
        {
            var count = events.Snapshot().Count;
            Assert.Equal(CommandStatus.Applied, (await session.Apply(command)).Status);
            return [.. events.Snapshot().Skip(count).Select(record => record.Kind)];
        }

        Assert.Equal(["command.applied", "sheet.changed"], await Kinds(new MoveSheetViewCommand { SheetId = sheet, ViewId = front, X = 200, Y = 150.5 }));
        Assert.Equal("front(200,150.5) top(80,50)", await Views());
        Assert.Equal(["command.applied", "sheet.changed"], await Kinds(new RemoveSheetViewCommand { SheetId = sheet, ViewId = front }));
        Assert.Equal("top(80,50)", await Views());

        Assert.Equal(["command.applied", "sheet.changed"], await Kinds(new UndoCommand()));
        Assert.Equal("front(200,150.5) top(80,50)", await Views());
        Assert.Equal(["command.applied", "sheet.changed"], await Kinds(new UndoCommand()));
        Assert.Equal("front(80,120) top(80,50)", await Views());
        Assert.Equal(["command.applied", "sheet.changed"], await Kinds(new RedoCommand()));
        Assert.Equal(["command.applied", "sheet.changed"], await Kinds(new RedoCommand()));
        Assert.Equal("top(80,50)", await Views());
    }

    // Item 3 (Q6 A): the removal of a view removes the dimensions of that view in the same new record, and keeps the others.
    // No command places a dimension before P10.3, so the test gives the handler a Document with a sheet that holds two.
    [Fact]
    public async Task The_Removal_Of_A_View_Removes_Its_Dimensions()
    {
        var (front, top) = (Guid.NewGuid(), Guid.NewGuid());
        var sheet = new SheetRecord(Guid.NewGuid(), "Sheet 1", "A4", true, false,
            [new SheetView(front, Front, 1, 1, 80, 120), new SheetView(top, Top, 1, 1, 80, 50)],
            [new SheetDimension(Guid.NewGuid(), front, Guid.NewGuid(), "constraint", 90, 130), new SheetDimension(Guid.NewGuid(), top, Guid.NewGuid(), "distance", 90, 60)]);
        var document = new Document();
        document.AddSheet(sheet);

        var result = await new RemoveSheetViewCommandHandler().Handle(
            new RemoveSheetViewCommand { SheetId = sheet.Id, ViewId = front }, document, new InProcessMeshBackend(), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        var changed = Assert.Single(result.ChangedSheets);
        Assert.Equal(top, Assert.Single(changed.Views).Id);
        Assert.Equal(top, Assert.Single(changed.Dimensions).ViewId);
    }

    // ADR-0153 item 1: an open of the document file replays the commands and gives the same views.
    [Fact]
    public async Task An_Open_Of_The_File_Gives_The_Same_Views()
    {
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, "views" + DocumentFile.Extension);
        var (live, _) = NewSession();
        DocumentFile.Create(live, path);
        var sheet = await Applied(live, new CreateSheetCommand { SheetName = "Sheet 1", Paper = "A2" }, "sheetId");
        var front = await Applied(live, View(sheet, [2, 0, 0, 0, 0, 2, 0, -3, 0], 3, 7, 120.125, 90.5), "viewId");
        await Applied(live, View(sheet, Top, 5, 1, 300, 200), "viewId");
        await Applied(live, new MoveSheetViewCommand { SheetId = sheet, ViewId = front, X = 10.75, Y = 400 }, "viewId");
        var expected = (await Sheet(live)).Views.Select(Text).ToArray();
        live.Close();

        var (opened, _) = NewSession();
        await DocumentFile.Open(opened, path);
        var actual = (await Sheet(opened)).Views.Select(Text).ToArray();
        opened.Close();

        Assert.Equal(2, actual.Length);
        Assert.Equal(expected, actual);
    }

    // Item 5: a read gives the paper and the lines of each view on it, in millimetres from the lower left corner. The box of
    // 20 by 10 by 30 at the origin, moved by (5, 0, 7), has its lines from x = -5 to 15 and z = -8 to 22 in the Front view, so
    // its centre (5, 7) goes to the place of the view. At 1 : 2 the box is 10 by 15 mm on the paper, and at 2 : 1 it is 40 by
    // 60 mm; a frame of length 2 gives the same lines. The Top view gives the box 20 by 10 at 1 : 1.
    [NativeManifoldFact]
    public async Task A_Read_Gives_The_Lines_Of_Each_View_On_The_Paper()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var box = await Applied(session, new CreateBoxCommand { SizeX = 20, SizeY = 10, SizeZ = 30 }, "bodyId");
        await Applied(session, new TranslateCommand { BodyId = box, Dx = 5, Dy = 0, Dz = 7 }, "bodyId");
        var sheet = await Applied(session, new CreateSheetCommand { SheetName = "Sheet 1" }, "sheetId");
        await Applied(session, View(sheet, Front, 1, 2, 100, 80), "viewId");
        await Applied(session, View(sheet, Front, 2, 1, 150, 100), "viewId");
        await Applied(session, View(sheet, [2, 0, 0, 0, 0, 2, 0, -5, 0], 1, 2, 100, 80), "viewId");
        await Applied(session, View(sheet, Top, 1, 1, 60, 40), "viewId");

        var read = await Read(session, sheet);

        Assert.Equal((sheet, "Sheet 1", "A4", true, false, 297.0, 210.0), (read.SheetId, read.Name, read.Paper, read.Landscape, read.ThirdAngle, read.Width, read.Height));
        Assert.Empty(read.Dimensions);
        Assert.Equal(4, read.Views.Count);
        Assert.All(read.Views, view => Assert.Empty(view.Skipped));
        Assert.Equal((95.0, 72.5, 105.0, 87.5), Box(read.Views[0]));
        Assert.Equal((130.0, 70.0, 170.0, 130.0), Box(read.Views[1]));
        Assert.Equal(Box(read.Views[0]), Box(read.Views[2]));
        Assert.Equal((50.0, 35.0, 70.0, 45.0), Box(read.Views[3]));
        Assert.All(read.Views[0].Lines, line => Assert.Equal(("edge", true), (line.Kind, line.Visible)));
        Assert.Equal(4, read.Views[0].Lines.Count);
        Assert.Equal(new[] { "(100,80)", "(150,100)", "(100,80)", "(60,40)" }, read.Views.Select(view => string.Create(CultureInfo.InvariantCulture, $"({view.X},{view.Y})")));
    }

    // The smallest and the largest place of the ends of the lines of a view on the paper.
    private static (double MinX, double MinY, double MaxX, double MaxY) Box(SheetViewInfo view)
    {
        var xs = view.Lines.SelectMany(line => new[] { line.FromX, line.ToX }).ToArray();
        var ys = view.Lines.SelectMany(line => new[] { line.FromY, line.ToY }).ToArray();
        return (xs.Min(), ys.Min(), xs.Max(), ys.Max());
    }

    // Item 5: a view with no line has its centre at the origin of the part; a mesh body gives no line and one skipped entry
    // with its reason (ADR-0142 item 6); a sheet that is not live gives E-SHEET-NOT-FOUND.
    [Fact]
    public async Task A_Read_Names_A_Mesh_Body_As_Skipped()
    {
        var mesh = new BodyHandle(Guid.NewGuid());
        var sheet = new SheetRecord(Guid.NewGuid(), "Sheet 1", "A3", false, false, [new SheetView(Guid.NewGuid(), Front, 1, 1, 100, 200)], []);
        var document = new Document();
        document.AddBody(new BodyRecord(mesh, ImportStlCommandHandler.MeshKind));
        document.AddSheet(sheet);
        var handler = new GetSheetQueryHandler();

        var result = await handler.Handle(new GetSheetQuery { SheetId = sheet.Id }, document, new InProcessMeshBackend(), CancellationToken.None);
        var missing = await handler.Handle(new GetSheetQuery { SheetId = Guid.NewGuid() }, document, new InProcessMeshBackend(), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        var read = (SheetInfo)result.Result!;
        Assert.Equal((297.0, 420.0), (read.Width, read.Height));
        var view = Assert.Single(read.Views);
        Assert.Empty(view.Lines);
        var skipped = Assert.Single(view.Skipped);
        Assert.Equal(mesh.Id, skipped.Body);
        Assert.Contains("mesh body", skipped.Reason, StringComparison.Ordinal);
        Assert.Equal(DiagnosticCodes.SheetNotFound, missing.Error!.Code);
    }

    // Items 5 and 7 (Q7 A): three views of a sphere give its outline as a circle of the radius times the scale, at the place
    // of each view. The test writes the time of the read; ADR-0155 item 7 opens a register entry when it is more than 1 s.
    [NativeManifoldFact]
    public async Task A_Read_Of_Three_Views_Of_A_Sphere_Gives_Its_Circles()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        await Applied(session, new CreateSphereCommand { Radius = 10 }, "bodyId");
        var sheet = await Applied(session, new CreateSheetCommand { SheetName = "Sheet 1" }, "sheetId");
        await Applied(session, View(sheet, Front, 1, 2, 60, 150), "viewId");
        await Applied(session, View(sheet, Top, 1, 2, 60, 60), "viewId");
        await Applied(session, View(sheet, Right, 1, 2, 160, 150), "viewId");

        var clock = Stopwatch.StartNew();
        var read = await Read(session, sheet);
        clock.Stop();
        output.WriteLine($"The read of three views of a sphere took {clock.ElapsedMilliseconds} ms.");

        foreach (var view in read.Views)
        {
            var circle = Assert.Single(view.Lines, line => line.Circle is not null).Circle!;
            Assert.Equal((view.X, view.Y, 5.0), (circle.X, circle.Y, circle.Radius));
        }
    }

    // ADR-0164 item 1 (TASK-0400, findings E211 and E212 of the codebase review of 2026-11-17): a view of an exact frame of
    // any length gives the curves of the design at the scale of the view. A cylinder of radius 10 along z at 1 : 1 is one
    // whole circle of radius 10 from the top with a frame of length 1, 2, 5, √2 or 4√2 (top views turned in their plane), 1e-200
    // or 1e200, and two ellipses with a major half axis of 10 and a minor one of 10 · 2/3 in the pictorial view of whole
    // numbers of length 3 (ADR-0162 item 16), also with a direction toward the eye of length 3e200. Until TASK-0400 a frame of
    // length 2 gave 64 straight lines and no circle, and the frames of 1e-200 and 1e200 gave numbers that are not finite and
    // one point.
    [NativeManifoldTheory]
    [InlineData(1, 0, 0, 0, 1, 0, 0, 0, 1)]
    [InlineData(2, 0, 0, 0, 2, 0, 0, 0, 1)]
    [InlineData(3, 4, 0, -4, 3, 0, 0, 0, 1)]
    [InlineData(1e-200, 0, 0, 0, 1e-200, 0, 0, 0, 1)]
    [InlineData(1e200, 0, 0, 0, 1e200, 0, 0, 0, 1)]
    [InlineData(1, 1, 0, -1, 1, 0, 0, 0, 1)]
    [InlineData(4, 4, 0, -4, 4, 0, 0, 0, 1)]
    [InlineData(2, 2, -1, -1, 2, 2, 2, -1, 2)]
    [InlineData(2, 2, -1, -1, 2, 2, 2e200, -1e200, 2e200)]
    public async Task A_View_Of_Any_Length_Gives_The_Curves_Of_The_Design(double rx, double ry, double rz, double ux, double uy, double uz, double ex, double ey, double ez)
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        await Applied(session, new CreateCylinderCommand { Radius = 10, Height = 20 }, "bodyId");
        var sheet = await Applied(session, new CreateSheetCommand { SheetName = "Sheet 1", Paper = "A3" }, "sheetId");
        await Applied(session, View(sheet, [rx, ry, rz, ux, uy, uz, ex, ey, ez], 1, 1, 200, 150), "viewId");

        var view = Assert.Single((await Read(session, sheet)).Views);

        Assert.All(view.Lines, line => Assert.True(double.IsFinite(line.FromX) && double.IsFinite(line.FromY) && double.IsFinite(line.ToX) && double.IsFinite(line.ToY)));
        if (ez == 1)
        {
            var circle = Assert.Single(view.Lines, line => line.Kind != "centre").Circle;
            Assert.NotNull(circle);
            Assert.Equal((200.0, 150.0), (circle!.X, circle.Y));
            Assert.Equal(10, circle.Radius, 12);
            return;
        }

        var ellipses = view.Lines.Where(line => line.Ellipse is not null).Select(line => line.Ellipse!).ToArray();
        Assert.NotEmpty(ellipses);
        Assert.All(ellipses, e =>
        {
            Assert.Equal(10, Math.Sqrt((e.MajorX * e.MajorX) + (e.MajorY * e.MajorY)), 9);
            Assert.Equal(20.0 / 3, Math.Sqrt((e.MinorX * e.MinorX) + (e.MinorY * e.MinorY)), 9);
        });
    }

    // ADR-0165 items 1 to 3 (phase R30g, TASK-0405): a plate of 60 by 40 by 10 with a through-hole of radius 8 in its middle. At
    // 1 : 1 the Top view gives the cross of the hole, 3 mm of paper past the circle, and the Front view the axis of the hole
    // from the bottom face to the top face, 3 mm past each. SetSheetViewLines takes the hidden lines out of the Front view and
    // keeps its axis, takes the centre lines out of the Top view, refuses a sheet or a view that is not there, and an undo
    // gives the flags back. A flag never moves a view.
    [NativeManifoldFact]
    public async Task A_Hole_Gives_Its_Centre_Lines_And_The_Flags_Hide_The_Lines()
    {
        using var backend = new ManifoldGeometryBackend();
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
        await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = tool }, "bodyId");
        var sheet = await Applied(session, new CreateSheetCommand { SheetName = "Plate", Paper = "A3" }, "sheetId");
        var front = await Applied(session, View(sheet, Front, x: 200, y: 200), "viewId");
        var top = await Applied(session, View(sheet, Top, x: 200, y: 100), "viewId");

        static string[] Centres(SheetViewInfo view) => [.. view.Lines.Where(line => line.Kind == "centre")
            .Select(line => string.Create(CultureInfo.InvariantCulture, $"{Math.Round(line.FromX, 9)},{Math.Round(line.FromY, 9)} {Math.Round(line.ToX, 9)},{Math.Round(line.ToY, 9)}"))];
        var before = await Read(session, sheet);
        Assert.Equal(["200,192 200,208"], Centres(before.Views[0]));
        Assert.Equal(["189,100 211,100", "200,89 200,111"], Centres(before.Views[1]));
        Assert.Equal(2, before.Views[0].Lines.Count(line => !line.Visible));
        Assert.All(before.Views, view => Assert.True(view.HiddenLines && view.CentreLines));

        await Applied(session, new SetSheetViewLinesCommand { SheetId = sheet, ViewId = front, HiddenLines = false, CentreLines = true }, "viewId");
        await Applied(session, new SetSheetViewLinesCommand { SheetId = sheet, ViewId = top, HiddenLines = true, CentreLines = false }, "viewId");
        var after = await Read(session, sheet);

        Assert.DoesNotContain(after.Views[0].Lines, line => !line.Visible);
        Assert.Equal(["200,192 200,208"], Centres(after.Views[0]));
        Assert.Empty(Centres(after.Views[1]));
        Assert.Equal((false, true, true, false), (after.Views[0].HiddenLines, after.Views[0].CentreLines, after.Views[1].HiddenLines, after.Views[1].CentreLines));
        Assert.Equal(before.Views[0].Lines.Where(line => line.Visible && line.Kind != "centre").Select(line => (line.FromX, line.FromY)), after.Views[0].Lines.Where(line => line.Kind != "centre").Select(line => (line.FromX, line.FromY)));
        Assert.Equal(DiagnosticCodes.SheetNotFound, await Refused(session, new SetSheetViewLinesCommand { SheetId = Guid.NewGuid(), ViewId = top, HiddenLines = true, CentreLines = true }));
        Assert.Equal(DiagnosticCodes.SheetViewNotFound, await Refused(session, new SetSheetViewLinesCommand { SheetId = sheet, ViewId = Guid.NewGuid(), HiddenLines = true, CentreLines = true }));

        await Applied(session, new UndoCommand(), "commandId");
        var undone = await Read(session, sheet);
        Assert.Equal(["189,100 211,100", "200,89 200,111"], Centres(undone.Views[1]));
        Assert.False(undone.Views[0].HiddenLines);
    }

    // ADR-0165 item 3 and finding E218 of the codebase review of 2026-11-18 (TASK-0410): a plate of 60 by 40 by 10 with two
    // through-holes of radius 5 at (15, 20) and (45, 20), and a block of 15 by 40 by 10 on the plate over the right half of the
    // second hole. In the Top view the circle of the second hole is a visible arc and a hidden arc, and each hole has its
    // cross. Until TASK-0410 the second hole had no cross.
    [NativeManifoldFact]
    public async Task A_Hole_That_A_Body_Covers_In_Part_Keeps_Its_Cross()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        async Task<Guid> Block(double x0, double y0, double x1, double y1)
        {
            var sketch = await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }, "sketchId");
            var points = new List<Guid>();
            foreach (var (x, y) in new[] { (x0, y0), (x1, y0), (x1, y1), (x0, y1) })
                points.Add(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }, "pointId"));
            for (var i = 0; i < 4; i++)
                await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % 4] }, "lineId");
            return await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 10 }, "bodyId");
        }

        var plate = await Block(0, 0, 60, 40);
        foreach (var x in new[] { 15.0, 45.0 })
        {
            var tool = await Applied(session, new CreateCylinderCommand { Radius = 5, Height = 20 }, "bodyId");
            tool = await Applied(session, new TranslateCommand { BodyId = tool, Dx = x, Dy = 20, Dz = 5 }, "bodyId");
            plate = await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = tool }, "bodyId");
        }

        var cover = await Block(45, 0, 60, 40);
        await Applied(session, new TranslateCommand { BodyId = cover, Dx = 0, Dy = 0, Dz = 10 }, "bodyId");
        var sheet = await Applied(session, new CreateSheetCommand { SheetName = "Plate", Paper = "A3" }, "sheetId");
        await Applied(session, View(sheet, Top, x: 200, y: 100), "viewId");

        var view = Assert.Single((await Read(session, sheet)).Views);
        var circles = view.Lines.Where(line => line.Kind != "centre" && line.Circle is { Radius: 5 }).ToList();
        output.WriteLine(string.Join(Environment.NewLine, circles.Select(line => $"{line.Visible} {line.Circle} {line.FromX},{line.FromY} {line.ToX},{line.ToY}")));
        Assert.Contains(circles, line => !line.Visible && line.Circle!.X == 215);
        var centres = view.Lines.Where(line => line.Kind == "centre")
            .Select(line => string.Create(CultureInfo.InvariantCulture, $"{Math.Round(line.FromX, 9)},{Math.Round(line.FromY, 9)} {Math.Round(line.ToX, 9)},{Math.Round(line.ToY, 9)}"))
            .Order(StringComparer.Ordinal);
        Assert.Equal(["177,100 193,100", "185,92 185,108", "207,100 223,100", "215,92 215,108"], centres);
    }
}
