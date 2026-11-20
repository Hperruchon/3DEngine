using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0176 (phase R30e, TASK-0421): "Put on sheet" in the pad of a value of a flat view. ADR-0176 names this file in its
// enforced-by field. With no GPU.
[Collection(DearImGui.Collection)]
public sealed class SheetValueTests
{
    private static readonly Vector2 Display = new(1280, 720);

    private static Guid Out(CommandResult result, string name)
    {
        Assert.True(result.Outputs.TryGet<Guid>(name, out var value), $"{result.CommandName}: {result.Error?.Code} {result.Error?.Message}");
        return value;
    }

    // A rectangle of 10 by 6 on the plane XY with a length of 10 on its first line, extruded by 3, and a sheet A4 with the front
    // view at (100, 150) at 1:1. The front view has the right x and the up z, so the box of its lines is x 0 to 10 and z 0 to 3,
    // with the centre (5, 1.5), and a point (x, y, z) goes to (95 + x, 148.5 + z) on the paper.
    private static async Task<(EngineHost Host, Guid Body, Guid Length, Guid Sheet, Guid View)> Plate()
    {
        var host = new EngineHost(new ManifoldGeometryBackend());
        var sketch = Out(await host.Session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        (double X, double Y)[] corners = [(0, 0), (10, 0), (10, 6), (0, 6)];
        var points = new List<Guid>();
        foreach (var (x, y) in corners)
            points.Add(Out(await host.Session.Apply(new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }), "pointId"));
        var lines = new List<Guid>();
        for (var i = 0; i < points.Count; i++)
            lines.Add(Out(await host.Session.Apply(new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Count] }), "lineId"));
        var length = Out(await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = lines[0], Value = 10 }), "constraintId");
        var body = Out(await host.Session.Apply(new ExtrudeCommand { SketchId = sketch, Distance = 3 }), "bodyId");
        var sheet = Out(await host.Session.Apply(new CreateSheetCommand { SheetName = "Sheet 1", Paper = "A4", Landscape = true }), "sheetId");
        var d = SheetPlacement.FrameOf(FlatView.Front);
        var view = Out(await host.Session.Apply(new AddSheetViewCommand
        {
            SheetId = sheet, RightX = d[0], RightY = d[1], RightZ = d[2], UpX = d[3], UpY = d[4], UpZ = d[5], EyeX = d[6], EyeY = d[7], EyeZ = d[8],
            ScaleNumerator = 1, ScaleDenominator = 1, X = 100, Y = 150,
        }), "viewId");
        return (host, body, length, sheet, view);
    }

    // The values of the body in the front view of the window.
    private static async Task<IReadOnlyList<EdgeValues.Value>> FrontValues(EngineHost host, Guid body)
    {
        var faces = (await host.Session.Query<FacesInfo>(new GetFacesQuery { BodyId = body })).Result ?? throw new InvalidOperationException("No faces.");
        var sketches = new List<SketchInfo>();
        foreach (var id in await host.Session.Read((document, _) => document.Sketches.Select(sketch => sketch.Id).ToArray()))
            sketches.Add((await host.Session.Query<SketchInfo>(new GetSketchQuery { SketchId = id })).Result!);
        var camera = new Camera { Distance = 40, Target = (5, 3, 1.5) };
        camera.ShowFlat(FlatView.Front);
        return EdgeValues.Of(body, 3, faces.Edges, sketches, camera, Display.X, Display.Y, "mm");
    }

    private static Task<IReadOnlyList<SheetDimension>> Dimensions(EngineHost host, Guid sheet)
        => host.Session.Read((document, _) => (IReadOnlyList<SheetDimension>)document.Sheets.First(live => live.Id == sheet).Dimensions);

    private static Task<int> LogCount(EngineHost host) => host.Session.Read((document, _) => document.Log.Count);

    // Items 1, 4 and 5: the length of the top edge goes 10 mm over its middle, and the distance 10 mm beside a vertical edge;
    // each is one PlaceSheetDimension with the source and the kind of its value, on the front view.
    [Fact]
    public async Task Put_On_Sheet_Places_Each_Value_Outside_Its_Edge_On_The_View_Of_The_Frame()
    {
        var (host, body, length, sheet, view) = await Plate();
        using var _ = host;
        var values = await FrontValues(host, body);
        var lengthValue = values.Single(value => value.Constraint == length);
        var distanceValue = values.Single(value => value.Key == EdgeValues.DistanceKey(body));
        Assert.NotNull(lengthValue.Model);
        Assert.NotNull(distanceValue.Model);
        var actions = new SheetActions(host.Session);
        var before = await LogCount(host);

        Assert.Equal("Put 10.000 mm on the front view of Sheet 1.", actions.PutOnSheet(FlatView.Front, lengthValue));
        Assert.Equal("Put 3.000 mm on the front view of Sheet 1.", actions.PutOnSheet(FlatView.Front, distanceValue));

        Assert.Equal(before + 2, await LogCount(host));
        var placed = await Dimensions(host, sheet);
        Assert.Equal(2, placed.Count);
        // The top edge from (0, 0, 3) to (10, 0, 3) is (95, 151.5) to (105, 151.5) on the paper; its middle is above the centre
        // (100, 150), so the value goes up.
        Assert.Equal((view, length, "constraint", 100.0, 161.5), (placed[0].ViewId, placed[0].Source, placed[0].SourceKind, placed[0].X, placed[0].Y));
        // A vertical edge is at x 95 or 105 on the paper, from y 148.5 to 151.5; the value goes away from the centre.
        Assert.Equal((view, body, "distance", 150.0), (placed[1].ViewId, placed[1].Source, placed[1].SourceKind, placed[1].Y));
        Assert.Contains(placed[1].X, new[] { 85.0, 115.0 });
    }

    // Item 5: a value that the sheet shows already gives a text and no command.
    [Fact]
    public async Task A_Value_That_The_Sheet_Shows_Gives_No_Command()
    {
        var (host, body, length, _, _) = await Plate();
        using var _host = host;
        var value = (await FrontValues(host, body)).Single(candidate => candidate.Constraint == length);
        var actions = new SheetActions(host.Session);
        actions.PutOnSheet(FlatView.Front, value);
        var before = await LogCount(host);

        Assert.Equal("Sheet 1 shows this value already.", actions.PutOnSheet(FlatView.Front, value));
        Assert.Equal(before, await LogCount(host));
    }

    // Item 3: with no sheet view of the frame of the flat view, a text and no command.
    [Fact]
    public async Task A_Flat_View_That_No_Sheet_Has_Gives_No_Command()
    {
        var (host, body, length, _, _) = await Plate();
        using var _host = host;
        var value = (await FrontValues(host, body)).Single(candidate => candidate.Constraint == length);
        var before = await LogCount(host);

        Assert.Equal("No sheet has the top view: add the view first.", new SheetActions(host.Session).PutOnSheet(FlatView.Top, value));
        Assert.Equal(before, await LogCount(host));
    }

    // Item 3: a section is not a view of the frame. A section of the top view with the look "up" has the frame of the front
    // view; with that section on the first sheet, the value goes on the front view of the second sheet.
    [Fact]
    public async Task A_Section_With_The_Frame_Is_Not_The_View_Of_The_Value()
    {
        var (host, body, length, first, oldFront) = await Plate();
        using var _host = host;
        Out(await host.Session.Apply(new RemoveSheetViewCommand { SheetId = first, ViewId = oldFront }), "viewId");
        var top = await View(host, first, FlatView.Top, 100, 100);
        Out(await host.Session.Apply(new AddSheetSectionCommand { SheetId = first, ViewId = top, Look = "up", PointX = 5, PointY = 3, PointZ = 1.5, X = 200, Y = 100 }), "viewId");
        var second = Out(await host.Session.Apply(new CreateSheetCommand { SheetName = "Sheet 2", Paper = "A4", Landscape = true }), "sheetId");
        var front = await View(host, second, FlatView.Front, 100, 150);
        var value = (await FrontValues(host, body)).Single(candidate => candidate.Constraint == length);

        Assert.Equal("Put 10.000 mm on the front view of Sheet 2.", new SheetActions(host.Session).PutOnSheet(FlatView.Front, value));
        Assert.Empty(await Dimensions(host, first));
        Assert.Equal(front, Assert.Single(await Dimensions(host, second)).ViewId);
    }

    private static async Task<Guid> View(EngineHost host, Guid sheet, FlatView view, double x, double y)
    {
        var d = SheetPlacement.FrameOf(view);
        return Out(await host.Session.Apply(new AddSheetViewCommand
        {
            SheetId = sheet, RightX = d[0], RightY = d[1], RightZ = d[2], UpX = d[3], UpY = d[4], UpZ = d[5], EyeX = d[6], EyeY = d[7], EyeZ = d[8],
            ScaleNumerator = 1, ScaleDenominator = 1, X = x, Y = y,
        }), "viewId");
    }

    // Item 4: a radius has one point; its value goes from the centre of the view through that point. An edge through the
    // centre keeps its normal, and a point at the centre goes up.
    [Fact]
    public void The_Place_Of_A_Radius_And_Of_An_Edge_Through_The_Centre()
    {
        var view = new SheetViewInfo(Guid.NewGuid(), SheetPlacement.FrameOf(FlatView.Top), 2, 1, 100, 100, [], []) { CentreU = 0, CentreV = 0 };
        var point = new DatumPoint(3, 4, 7);

        // At 2:1 the point (3, 4) is (106, 108) on the paper: 10 mm farther along (0.6, 0.8) is (112, 116).
        Assert.Equal((112.0, 116.0), SheetValuePlace.Place(view, point, point));
        // An edge from (-5, 0) to (5, 0) goes through the centre: its normal (0, 1) stays.
        Assert.Equal((100.0, 110.0), SheetValuePlace.Place(view, new DatumPoint(-5, 0, 0), new DatumPoint(5, 0, 0)));
        Assert.Equal((100.0, 110.0), SheetValuePlace.Place(view, new DatumPoint(0, 0, 9), new DatumPoint(0, 0, 9)));
        // An edge from (5, 2) to (-5, 2) is (110, 104) to (90, 104): its first normal (0, -20) points to the centre, so it turns.
        Assert.Equal((100.0, 114.0), SheetValuePlace.Place(view, new DatumPoint(5, 2, 0), new DatumPoint(-5, 2, 0)));
    }

    // The window part: one frame of the UI with the chips of the body.
    private static void Step(DesktopUi ui, IReadOnlyList<EdgeValues.Value> values, Guid body, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, Display, Vector2.One, [new LiveBody(body, "Extrude", null, true)], body, values: values);
    }

    private static void Tap(DesktopUi ui, IReadOnlyList<EdgeValues.Value> values, Guid body, string item)
    {
        var rectangle = ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item}.");
        var (x, y) = ((rectangle.X + rectangle.Z) / 2, (rectangle.Y + rectangle.W) / 2);
        foreach (var phase in new[] { PointerPhase.Down, PointerPhase.Up })
            Step(ui, values, body, new PointerSample(DeviceKind.Touch, 1, 3, x, y, 1, PointerButtons.None, KeyModifiers.None, phase, 0));
        Step(ui, values, body);
        Step(ui, values, body);
    }

    private static readonly (DatumPoint, DatumPoint) Edge = (new DatumPoint(0, 0, 3), new DatumPoint(10, 0, 3));

    private static SheetRecord SheetWith(params SheetView[] views) => new(Guid.NewGuid(), "Sheet 1", "A4", true, false, views, []);

    private static SheetView ViewOf(FlatView view, SheetSection? section = null)
        => new(Guid.NewGuid(), SheetPlacement.FrameOf(view), 1, 1, 100, 150, Section: section);

    // Item 2: in the front view, with a sheet that has the front view, the pad of a length has the line "Put on sheet", at
    // least 44 pixels high; a tap gives the action of that value and the pad stays open.
    [Fact]
    public void The_Pad_Of_A_Value_Puts_It_On_The_Sheet()
    {
        var (body, sketch, constraint) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        IReadOnlyList<EdgeValues.Value> values = [new EdgeValues.Value(EdgeValues.ConstraintKey(constraint), "10.000 mm", new Vector2(700, 300), 10, body, sketch, constraint, Model: Edge)];
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        ui.ShowView(FlatView.Front);
        ui.ShowSheets([SheetWith(ViewOf(FlatView.Top), ViewOf(FlatView.Front))]);
        Step(ui, values, body);
        Step(ui, values, body);

        Tap(ui, values, body, values[0].Key);
        var line = ui.ItemRectangle(DesktopUi.PadItem(DesktopUi.PutOnSheetLabel)) ?? throw new InvalidOperationException("No line Put on sheet.");
        Assert.True(line.W - line.Y >= DesktopUi.ValueSize, $"The line {line} is lower than 44 pixels.");
        ui.TakeActions();
        Tap(ui, values, body, DesktopUi.PadItem(DesktopUi.PutOnSheetLabel));

        Assert.Equal([new PutOnSheetAction(FlatView.Front, values[0])], ui.TakeActions());
        Assert.NotNull(ui.Pad);
    }

    // Item 2: no line outside a flat view, with no sheet view of the frame, with only a section of that frame, or for a value
    // with no edge.
    [Theory]
    [InlineData("no flat view")]
    [InlineData("no view of the frame")]
    [InlineData("a section only")]
    [InlineData("no edge")]
    public void The_Pad_Has_No_Line_When_A_Condition_Fails(string condition)
    {
        var (body, sketch, constraint) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        IReadOnlyList<EdgeValues.Value> values = [new EdgeValues.Value(EdgeValues.ConstraintKey(constraint), "10.000 mm", new Vector2(700, 300), 10, body, sketch, constraint,
            Model: condition == "no edge" ? null : Edge)];
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        ui.ShowView(condition == "no flat view" ? null : FlatView.Front);
        var front = ViewOf(FlatView.Front);
        ui.ShowSheets([condition switch
        {
            "no view of the frame" => SheetWith(ViewOf(FlatView.Top)),
            "a section only" => SheetWith(ViewOf(FlatView.Front, new SheetSection(front.Id, [0, 0, 0], "A"))),
            _ => SheetWith(front),
        }]);
        Step(ui, values, body);
        Step(ui, values, body);

        Tap(ui, values, body, values[0].Key);

        Assert.NotNull(ui.Pad);
        Assert.Null(ui.ItemRectangle(DesktopUi.PadItem(DesktopUi.PutOnSheetLabel)));
    }

    // Item 2 and ADR-0172 item 2: the line takes its height in the pad, so each key, the field and the line show inside the pad
    // with no scroll, on the phones, in the smallest window, in a short wide window and on a desktop.
    [Theory]
    [InlineData(640, 360)]
    [InlineData(844, 390)]
    [InlineData(915, 412)]
    [InlineData(360, 560)]
    [InlineData(360, 640)]
    [InlineData(900, 560)]
    [InlineData(1280, 720)]
    public void Each_Key_Of_The_Pad_With_The_Line_Shows_With_No_Scroll(int width, int height)
    {
        var body = Guid.NewGuid();
        var display = new Vector2(width, height);
        IReadOnlyList<EdgeValues.Value> values = [new EdgeValues.Value(EdgeValues.DistanceKey(body), "3.000 mm", display / 3f, 3, body, null, null, Model: Edge)];
        using var ui = new DesktopUi(string.Empty, Document.Millimetre);
        ui.ShowView(FlatView.Front);
        ui.ShowSheets([SheetWith(ViewOf(FlatView.Front))]);
        for (var i = 0; i < 3; i++)
        {
            if (i == 1)
                ui.OpenDistancePad(body, 3);
            ui.Frame(1f / 60f, display, Vector2.One, [new LiveBody(body, "Extrude", null, true)], body, values: values);
        }

        var pad = ui.PanelRectangles[^1];
        foreach (var item in DesktopUi.PadRows.SelectMany(row => row).Append("distance").Append(DesktopUi.PutOnSheetLabel))
        {
            var box = ui.ItemRectangle(DesktopUi.PadItem(item)) ?? throw new InvalidOperationException($"{width}x{height}: the pad has no {item}.");
            Assert.True(box.X >= pad.X - 0.5f && box.Y >= pad.Y - 0.5f && box.Z <= pad.Z + 0.5f && box.W <= pad.W + 0.5f,
                $"{width}x{height}: {item} at {box} is not inside the pad at {pad}.");
        }
    }
}
