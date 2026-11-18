using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Core.Commands;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0167 (phase R30b, TASK-0407): "Add to sheet", "Standard views", the place of a view by the projection, the scale that
// fits, the pad of a new sheet and the scale of each view. ADR-0167 names this file in its enforced-by field. With no GPU.
[Collection(DearImGui.Collection)]
public sealed class SheetAddTests
{
    // The plate of 2026-11-17 as a box: 60 x 40 x 10.
    private static readonly SheetPlacement.Box Plate = new((0, 0, 0), (60, 40, 10));

    private static readonly LiveBody Body = new(Guid.Parse("7c000000-0000-0000-0000-000000000001"), "Plate", "Extrude", true);

    // Item 3: the frame of each flat view is the frame that the window gives GetViewLines for the camera of the session.
    [Theory]
    [InlineData(FlatView.Front)]
    [InlineData(FlatView.Back)]
    [InlineData(FlatView.Right)]
    [InlineData(FlatView.Left)]
    [InlineData(FlatView.Top)]
    [InlineData(FlatView.Bottom)]
    public void The_Frame_Of_A_Flat_View_Is_The_Frame_Of_The_Camera(FlatView view)
    {
        var camera = SessionScene.Create().Cameras[0];
        camera.ShowFlat(view);
        Assert.Equal(ViewLines.FrameOf(camera), SheetPlacement.FrameOf(view));
    }

    // Item 3: the size of each paper with each orientation is the size that GetSheet gives.
    [Fact]
    public async Task The_Paper_Is_The_Paper_Of_GetSheet()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        foreach (var paper in SheetRecord.Papers)
        {
            foreach (var landscape in new[] { true, false })
            {
                var create = new CreateSheetCommand { SheetName = $"{paper} {landscape}", Paper = paper, Landscape = landscape };
                await host.Session.Apply(create);
                var read = (await host.Session.Query<SheetInfo>(new GetSheetQuery { SheetId = create.CommandId })).Result!;
                Assert.Equal((read.Width, read.Height), SheetPlacement.Paper(paper, landscape));
            }
        }
    }

    // Item 3: next to the front view, first angle puts the top view under it, the view from the right at its left, the view
    // from the left at its right and the view from below over it; third angle is the other way; the back view shares an axis
    // and no side, so it has none.
    [Fact]
    public void The_Projection_Gives_The_Side_Of_A_View()
    {
        var front = SheetPlacement.FrameOf(FlatView.Front);
        (int, int)? Side(FlatView view, bool third) => SheetPlacement.Side(front, SheetPlacement.FrameOf(view), third);

        Assert.Equal((0, -1), Side(FlatView.Top, false));
        Assert.Equal((-1, 0), Side(FlatView.Right, false));
        Assert.Equal((1, 0), Side(FlatView.Left, false));
        Assert.Equal((0, 1), Side(FlatView.Bottom, false));
        Assert.Equal((0, 1), Side(FlatView.Top, true));
        Assert.Equal((1, 0), Side(FlatView.Right, true));
        Assert.Null(Side(FlatView.Back, false));
        Assert.Equal((0, 1), SheetPlacement.Side(SheetPlacement.FrameOf(FlatView.Top), front, false));
        Assert.Null(SheetPlacement.Side(SheetPlacement.FrameOf(FlatView.Top), SheetPlacement.FrameOf(FlatView.Right), false));
    }

    // Item 4: the largest scale of ISO 5455 at which the front, the top and the side view fit the area of the paper: the plate
    // fits A3 and A4 at 2:1, as the sheet of 2026-11-17; a box of 1000 x 500 x 300 fits A3 at 1:5; a box of 2 x 1 x 1 fits A4
    // at 50:1.
    [Fact]
    public void The_Scale_Is_The_Largest_That_Fits_The_Standard_Views()
    {
        Assert.Equal((2L, 1L), SheetPlacement.FitScale("A3", true, false, Plate));
        Assert.Equal((2L, 1L), SheetPlacement.FitScale("A4", true, false, Plate));
        Assert.Equal((1L, 5L), SheetPlacement.FitScale("A3", true, false, new SheetPlacement.Box((0, 0, 0), (1000, 500, 300))));
        Assert.Equal((50L, 1L), SheetPlacement.FitScale("A4", true, false, new SheetPlacement.Box((0, 0, 0), (2, 1, 1))));
        Assert.Equal((1L, 1000L), SheetPlacement.FitScale("A4", true, false, new SheetPlacement.Box((0, 0, 0), (1e6, 1e6, 1e6))));
    }

    // Item 3: the group of the standard views of the plate at 2:1 on A3: the top view 60 mm under the front view, the view from
    // the right 110 mm left of it, and the box of the group at the centre of the area (10 to 410 across, 30 to 287 up).
    [Fact]
    public void The_Standard_Views_Are_A_Group_At_The_Centre_Of_The_Area()
    {
        var places = SheetPlacement.Group("A3", true, false, [.. SheetPlacement.Standard.Select(SheetPlacement.FrameOf)], 2, 1, Plate);

        Assert.Equal([(255.0, 203.5), (255.0, 143.5), (145.0, 203.5)], places);
    }

    private static SheetRecord SheetWith(params SheetView[] views) => new(Guid.NewGuid(), "Sheet 1", "A3", true, false, views, []);

    private static SheetView ViewOf(FlatView view, double x, double y, long n = 2, long d = 1) => new(Guid.NewGuid(), SheetPlacement.FrameOf(view), n, d, x, y);

    // Item 3: next to a front view the top view and the view from the right go where the projection puts them. When a view is
    // in the place of the projection, the new view goes to the nearest free place, which meets no view.
    [Fact]
    public void A_View_Goes_Where_The_Projection_Puts_It_Or_To_The_Nearest_Free_Place()
    {
        var front = ViewOf(FlatView.Front, 255, 203.5);

        Assert.Equal(((255.0, 143.5), true), SheetPlacement.Place(SheetWith(front), SheetPlacement.FrameOf(FlatView.Top), 2, 1, Plate));
        Assert.Equal(((145.0, 203.5), true), SheetPlacement.Place(SheetWith(front), SheetPlacement.FrameOf(FlatView.Right), 2, 1, Plate));

        var back = ViewOf(FlatView.Back, 255, 143.5);
        var ((x, y), fits) = SheetPlacement.Place(SheetWith(front, back), SheetPlacement.FrameOf(FlatView.Top), 2, 1, Plate);
        Assert.True(fits);
        foreach (var other in new[] { (X: 255.0, Y: 203.5, W: 60.0, H: 10.0), (X: 255.0, Y: 143.5, W: 60.0, H: 10.0) })
            Assert.True(Math.Abs(x - other.X) >= 60 + other.W || Math.Abs(y - other.Y) >= 40 + other.H, $"The top view at ({x}, {y}) meets a view at ({other.X}, {other.Y}).");
        Assert.True(x - 60 >= 10 && x + 60 <= 410 && y - 40 >= 30 && y + 40 <= 287, $"The top view at ({x}, {y}) leaves the area.");

        // A view larger than the area has no free place: it goes to the centre of the area, and the result says so.
        Assert.Equal(((210.0, 158.5), false), SheetPlacement.Place(SheetWith(), SheetPlacement.FrameOf(FlatView.Front), 10, 1, Plate));
    }

    // Item 5: a free ratio is two whole numbers greater than zero.
    [Fact]
    public void A_Free_Ratio_Is_Two_Whole_Numbers()
    {
        Assert.Equal((3L, 4L), SheetPlacement.Ratio("3:4"));
        Assert.Equal((1L, 25L), SheetPlacement.Ratio(" 1 : 25 "));
        foreach (var text in new[] { "", "3", "3:0", "0:2", "-1:2", "1.5:1", "a:b", "1:2:3" })
            Assert.Null(SheetPlacement.Ratio(text));
    }

    private static async Task<Guid> Box(EngineHost host)
    {
        var result = await host.Session.Apply(new CreateBoxCommand { SizeX = 60, SizeY = 40, SizeZ = 10 });
        return result.Outputs.TryGet<Guid>("bodyId", out var id) ? id : throw new InvalidOperationException("No box.");
    }

    private static Task<IReadOnlyList<SheetRecord>> Sheets(EngineHost host)
        => host.Session.Read((document, _) => (IReadOnlyList<SheetRecord>)[.. document.Sheets]);

    // Items 2 and 3: the pad makes a sheet with the front view at its place in the group; "Standard views" then adds the top
    // view and the view from the right at the scale of the first view and holds the front view; a second "Add" of the top view
    // adds nothing. Each view is one step of the log.
    [Fact]
    public async Task A_New_Sheet_Takes_The_Front_View_And_Standard_Views_Add_The_Others()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        await Box(host);
        var actions = new SheetActions(host.Session);

        Assert.Equal("Added the front view to Sheet 1 at 2:1.", actions.Create("Sheet 1", "A3", true, (2, 1), [FlatView.Front], Plate));
        var sheet = Assert.Single(await Sheets(host));
        Assert.Equal((255.0, 203.5, 2L, 1L), (sheet.Views[0].X, sheet.Views[0].Y, sheet.Views[0].ScaleNumerator, sheet.Views[0].ScaleDenominator));

        var before = await host.Session.Read((document, _) => document.Log.Count);
        Assert.Equal("Added the top and the right view to Sheet 1 at 2:1. Sheet 1 has the front view already.", actions.Add(sheet, SheetPlacement.Standard, Plate));
        sheet = Assert.Single(await Sheets(host));
        Assert.Equal(before + 2, await host.Session.Read((document, _) => document.Log.Count));
        Assert.Equal(
            [(FlatView.Front, 255.0, 203.5), (FlatView.Top, 255.0, 143.5), (FlatView.Right, 145.0, 203.5)],
            sheet.Views.Select(view => (SheetPlacement.Standard.Single(standard => SheetPlacement.FrameOf(standard).SequenceEqual(view.Direction)), view.X, view.Y)));
        Assert.All(sheet.Views, view => Assert.Equal((2L, 1L), (view.ScaleNumerator, view.ScaleDenominator)));

        Assert.Equal("Sheet 1 has the top view already.", actions.Add(sheet, [FlatView.Top], Plate));
        Assert.Equal(3, Assert.Single(await Sheets(host)).Views.Count);
    }

    // Item 5: the scale of each view is one command and one step of undo, and the places stay.
    [Fact]
    public async Task The_Scale_Of_Each_View_Is_One_Step()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        await Box(host);
        var actions = new SheetActions(host.Session);
        actions.Create("Sheet 1", "A3", true, (2, 1), SheetPlacement.Standard, Plate);
        var sheet = Assert.Single(await Sheets(host));

        Assert.Equal("Each view of Sheet 1 is at 1:1.", actions.Scale(sheet, (1, 1)));
        var scaled = Assert.Single(await Sheets(host));
        Assert.All(scaled.Views, view => Assert.Equal((1L, 1L), (view.ScaleNumerator, view.ScaleDenominator)));
        Assert.Equal(sheet.Views.Select(view => (view.X, view.Y)), scaled.Views.Select(view => (view.X, view.Y)));
        await host.Session.Apply(new UndoCommand());
        Assert.All(Assert.Single(await Sheets(host)).Views, view => Assert.Equal((2L, 1L), (view.ScaleNumerator, view.ScaleDenominator)));
    }

    // Item 2: the window follows the live sheets from the events: a new sheet, a new view, an undo and a remove.
    [Fact]
    public async Task The_Window_Follows_The_Live_Sheets()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        await Box(host);
        var before = new CreateSheetCommand { SheetName = "Old" };
        await host.Session.Apply(before);
        using var sheets = new SessionSheets(new HostedSession(host));
        Assert.Equal(["Old"], sheets.Live.Select(sheet => sheet.Name));

        new SheetActions(host.Session).Create("Sheet 2", "A4", false, (1, 1), [FlatView.Top], Plate);
        sheets.Apply();
        Assert.Equal([("Old", 0), ("Sheet 2", 1)], sheets.Live.Select(sheet => (sheet.Name, sheet.Views.Count)));
        Assert.False(sheets.Live[1].Landscape);

        await host.Session.Apply(new UndoCommand());
        sheets.Apply();
        Assert.Equal([("Old", 0), ("Sheet 2", 0)], sheets.Live.Select(sheet => (sheet.Name, sheet.Views.Count)));

        await host.Session.Apply(new RemoveSheetCommand { SheetId = before.CommandId });
        sheets.Apply();
        Assert.Equal(["Sheet 2"], sheets.Live.Select(sheet => sheet.Name));
    }

    private static void Frame(DesktopUi ui, Vector2 display, params InputSample[] samples)
    {
        ui.Route(samples);
        ui.Frame(1f / 60f, display, Vector2.One, [Body], null, new DocumentLine("plate", null));
    }

    private static void Tap(DesktopUi ui, Vector2 display, Vector4 target)
    {
        var at = new Vector2((target.X + target.Z) / 2f, (target.Y + target.W) / 2f);
        Frame(ui, display, new PointerSample(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Move, 0));
        Frame(ui, display, new PointerSample(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.Left, KeyModifiers.None, PointerPhase.Down, 50_000_000));
        Frame(ui, display, new PointerSample(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.Left, KeyModifiers.None, PointerPhase.Up, 100_000_000));
        Frame(ui, display);
        Frame(ui, display);
    }

    private static Vector4 Item(DesktopUi ui, string item) => ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item}.");

    // Item 2: the net has "Add to sheet" and "Standard views" under its cells, inside the display. "Add to sheet" gives nothing in
    // the 3D view and the front view in the front view; "Standard views" gives the front, the top and the side view.
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(640, 360)]
    [InlineData(390, 844)]
    public void The_Net_Has_Add_To_Sheet_And_Standard_Views(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display);
        Frame(ui, display);
        ui.TakeActions();

        foreach (var (view, label, expected) in new (FlatView?, string, AddToSheetAction?)[]
        {
            (null, DesktopUi.AddToSheetLabel, null),
            (FlatView.Front, DesktopUi.AddToSheetLabel, new AddToSheetAction([FlatView.Front])),
            (null, DesktopUi.StandardViewsLabel, new AddToSheetAction(SheetPlacement.Standard)),
        })
        {
            ui.ShowView(view);
            Tap(ui, display, Item(ui, DesktopUi.ViewsLabel));
            var bottom = Item(ui, DesktopUi.ViewItem(FlatView.Bottom));
            var button = Item(ui, DesktopUi.SheetItem(label));
            Assert.True(button.X >= 0 && button.Z <= width && button.Y >= 0 && button.W <= height, $"{width}x{height}: {label} {button} is out of the display.");
            Assert.True(button.Y > bottom.W, $"{label} {button} is not under the cells {bottom}.");
            Assert.True(button.W - button.Y >= DesktopUi.MinimumControlHeight - 0.5f, $"{label} {button} is lower than 44 pixels.");
            Tap(ui, display, button);
            Assert.Equal(expected is null ? [] : [expected], ui.TakeActions().OfType<AddToSheetAction>());
            if (ui.ViewsOpen)
                Tap(ui, display, Item(ui, DesktopUi.ViewsLabel));
        }
    }

    // Item 2: the message of an add wraps inside the panel; until TASK-0407 a long message went past its right edge.
    [Fact]
    public void A_Long_Message_Wraps_Inside_The_Panel()
    {
        var display = new Vector2(1280, 720);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        for (var frame = 0; frame < 3; frame++)
            ui.Frame(1f / 60f, display, Vector2.One, [Body], null, new DocumentLine("plate", "Added."));
        var one = Item(ui, DesktopUi.DocumentMessageLabel);
        var line = new DocumentLine("plate", "Added the top and the right view to Sheet 1 at 2:1. Sheet 1 has the front view already.");
        for (var frame = 0; frame < 3; frame++)
            ui.Frame(1f / 60f, display, Vector2.One, [Body], null, line);

        var message = Item(ui, DesktopUi.DocumentMessageLabel);
        var panel = ui.PanelRectangles.Single(rectangle => rectangle.X <= message.X && rectangle.Z >= message.X && rectangle.Y <= message.Y && rectangle.W >= message.W);
        Assert.True(message.Z <= panel.Z, $"The message {message} goes past the panel {panel}.");
        Assert.True(message.W - message.Y > 1.5f * (one.W - one.Y), $"The message {message} is one line, as {one}.");
    }

    // Item 2: the command line has "Add view to sheet" in a flat view, "Standard views" always, and "Scale the sheet views"
    // with a sheet that has a view.
    [Fact]
    public void The_Command_Line_Has_The_Commands_Of_A_Sheet()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        IReadOnlyList<string> Names() => [.. ui.Commands([Body], null).Select(command => command.Name)];

        Assert.DoesNotContain(DesktopUi.AddViewToSheetLabel, Names());
        Assert.Contains(DesktopUi.StandardViewsLabel, Names());
        Assert.DoesNotContain(DesktopUi.ScaleSheetLabel, Names());

        ui.ShowView(FlatView.Top);
        ui.ShowSheet(SheetWith(ViewOf(FlatView.Front, 100, 100)));
        var commands = ui.Commands([Body], null);
        ui.TakeActions();
        commands.Single(command => command.Name == DesktopUi.AddViewToSheetLabel).Run();
        Assert.Equal([new AddToSheetAction([FlatView.Top])], ui.TakeActions());
        commands.Single(command => command.Name == DesktopUi.ScaleSheetLabel).Run();
        Assert.Equal((2L, 1L), ui.SheetPadShown?.Scale);
        Assert.False(ui.SheetPadShown!.NewSheet);
    }

    // Item 5: the pad of a new sheet starts on A3 landscape at the scale that fits; a new paper takes its scale that fits until a
    // person chooses a scale. Add gives the sheet with its paper, its scale and its views, and closes the pad; the keys of the
    // views rest while the pad is open.
    [Fact]
    public void The_Pad_Of_A_New_Sheet_Gives_The_Paper_And_The_Scale()
    {
        var display = new Vector2(1280, 720);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display);
        ui.OpenSheetPad([FlatView.Front], new SheetPlacement.Box((0, 0, 0), (200, 100, 50)));
        Frame(ui, display);
        Frame(ui, display);
        var pad = ui.SheetPadShown ?? throw new InvalidOperationException("No pad.");
        Assert.Equal(("A3", true, (1L, 1L)), (pad.Paper, pad.Landscape, pad.Scale));

        Tap(ui, display, Item(ui, DesktopUi.SheetPadItem("A4")));
        Assert.Equal(("A4", (1L, 2L)), (pad.Paper, pad.Scale));
        Tap(ui, display, Item(ui, DesktopUi.SheetPadItem(DesktopUi.SheetPadPortrait)));
        Tap(ui, display, Item(ui, DesktopUi.SheetPadItem("1:5")));
        Tap(ui, display, Item(ui, DesktopUi.SheetPadItem("A0")));
        Assert.Equal(("A0", false, (1L, 5L)), (pad.Paper, pad.Landscape, pad.Scale));
        pad.TypeRatio("3:4");
        Assert.Equal((3L, 4L), pad.Scale);

        ui.TakeActions();
        Frame(ui, display, new KeySample(InputKey.Digit1, true, KeyModifiers.None, 0));
        Assert.DoesNotContain(ui.TakeActions(), action => action is FlatViewAction);
        Tap(ui, display, Item(ui, DesktopUi.SheetPadItem(DesktopUi.SheetPadAdd)));
        Assert.Equal([new CreateSheetAction("A0", false, 3, 4, [FlatView.Front])], ui.TakeActions().OfType<CreateSheetAction>());
        Assert.Null(ui.SheetPadShown);

        // Enter gives Add, and Escape closes the pad with no action.
        ui.OpenSheetPad(SheetPlacement.Standard, Plate);
        Frame(ui, display, new KeySample(InputKey.Enter, true, KeyModifiers.None, 0));
        Assert.Equal([new CreateSheetAction("A3", true, 2, 1, SheetPlacement.Standard)], ui.TakeActions().OfType<CreateSheetAction>());
        ui.OpenSheetPad(SheetPlacement.Standard, Plate);
        Frame(ui, display, new KeySample(InputKey.Escape, true, KeyModifiers.None, 0));
        Assert.Null(ui.SheetPadShown);
        Assert.Empty(ui.TakeActions().OfType<CreateSheetAction>());
    }
}
