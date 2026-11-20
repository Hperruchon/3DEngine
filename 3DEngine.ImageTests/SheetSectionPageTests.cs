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

// ADR-0171 (phase R30h, TASK-0413): the action "Section" of the sheet page: the place of a cut and its snap, the point of the
// design on a place of the paper, the row and the two taps, Escape, and the command off the frame thread. ADR-0171 names this
// file in its enforced-by field. With no GPU.
[Collection(DearImGui.Collection)]
public sealed class SheetSectionPageTests
{
    private static readonly SheetPlacement.Box Plate = new((0, 0, 0), (60, 40, 10));
    private static readonly LiveBody Body = new(Guid.Parse("7d000000-0000-0000-0000-000000000002"), "Plate", "Extrude", true);

    // A host with a box of 60 by 40 by 10 moved to (30, 20, 5), so its corner is at the origin, and a sheet with Front, Top and
    // Right at 2:1 on A3.
    private static async Task<(EngineHost Host, SheetInfo Sheet, SheetRecord Record)> Setup()
    {
        var host = new EngineHost(new ManifoldGeometryBackend());
        var box = await host.Session.Apply(new CreateBoxCommand { SizeX = 60, SizeY = 40, SizeZ = 10 });
        Assert.True(box.Outputs.TryGet<Guid>("bodyId", out var id));
        await host.Session.Apply(new TranslateCommand { BodyId = id, Dx = 30, Dy = 20, Dz = 5 });
        new SheetActions(host.Session).Create("Sheet 1", "A3", true, (2, 1), SheetPlacement.Standard, Plate);
        var (sheet, record) = await Read(host);
        return (host, sheet, record);
    }

    private static async Task<(SheetInfo Sheet, SheetRecord Record)> Read(EngineHost host)
    {
        var record = await host.Session.Read((document, _) => document.Sheets.Single());
        return ((await host.Session.Query<SheetInfo>(new GetSheetQuery { SheetId = record.Id })).Result!, record);
    }

    // A view of the front at 1:1 at (100, 80) with two whole circles, at x 100 and 103 and y 80 and 83, and one arc at x 98.
    private static SheetViewInfo Circles()
    {
        SheetLine Whole(double x, double y) => new(Guid.Empty, x + 4, y, x + 4, y, true, "edge") { Circle = new SheetCircle(x, y, 4) };
        var arc = new SheetLine(Guid.Empty, 102, 70, 98, 74, true, "edge") { Circle = new SheetCircle(98, 70, 4) };
        return new SheetViewInfo(Guid.NewGuid(), [1, 0, 0, 0, 0, 1, 0, -1, 0], 1, 1, 100, 80, [Whole(100, 80), Whole(103, 83), arc], []);
    }

    // Item 3: a cut snaps to the nearest centre of a whole circle within 2 mm, across its line: y for "up" and "down", x for
    // "left" and "right"; an arc gives no snap, and a place farther than 2 mm stays.
    [Fact]
    public void A_Cut_Snaps_To_The_Centre_Of_A_Whole_Circle_Within_Two_Millimetres()
    {
        var view = Circles();

        Assert.Equal(80, SheetPage.CutPlace(view, "up", (0, 81.4)));
        Assert.Equal(83, SheetPage.CutPlace(view, "down", (0, 82)));
        Assert.Equal(86, SheetPage.CutPlace(view, "up", (0, 86)));
        Assert.Equal(100, SheetPage.CutPlace(view, "left", (101.4, 0)));
        Assert.Equal(103, SheetPage.CutPlace(view, "right", (101.6, 0)));
        Assert.Equal(97.5, SheetPage.CutPlace(view, "left", (97.5, 0)));
    }

    // Item 1: the centre of each view in GetSheet turns a place of the paper into a point of the design. The box of the plate
    // has its centre at (30, 20, 5); at 2:1, 10 mm of paper is 5 mm of the design.
    [Fact]
    public async Task A_Place_Of_The_Paper_Is_A_Point_Of_The_Design()
    {
        var (host, sheet, _) = await Setup();
        using var _ = host;
        var (front, top, right) = (sheet.Views[0], sheet.Views[1], sheet.Views[2]);

        Assert.Equal((30.0, 5.0), (front.CentreU, front.CentreV));
        Assert.Equal((30.0, 20.0, 0.0), Round(SheetPage.DesignPoint(top, (top.X, top.Y))));
        Assert.Equal((35.0, 22.0, 0.0), Round(SheetPage.DesignPoint(top, (top.X + 10, top.Y + 4))));
        Assert.Equal((35.0, 0.0, 7.0), Round(SheetPage.DesignPoint(front, (front.X + 10, front.Y + 4))));
        Assert.Equal((0.0, 25.0, 7.0), Round(SheetPage.DesignPoint(right, (right.X + 10, right.Y + 4))));
    }

    private static (double, double, double) Round((double X, double Y, double Z) p) => (Math.Round(p.X, 9) + 0, Math.Round(p.Y, 9) + 0, Math.Round(p.Z, 9) + 0);

    // Items 2 to 4: "Section" on a selected view opens its row with the look "up"; a look changes it; a tap on the view sets
    // the cut, a tap on the free paper gives one action with the point of the cut and the place, and ends the section. A tap
    // on another place before the cut does nothing, and no drag moves a view while the section is open.
    [Fact]
    public async Task Section_A_Look_And_Two_Taps_Give_One_Action()
    {
        var (host, sheet, record) = await Setup();
        using var _ = host;
        var display = new Vector2(1280, 720);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display, sheet, record);
        ui.OpenPage();
        Frame(ui, display, sheet, record);
        Frame(ui, display, sheet, record);
        var top = sheet.Views[1];
        ui.TakeActions();

        Tap(ui, display, sheet, record, ui.PagePaper!.Value.Pixel(top.X, top.Y));
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.PageItem(DesktopUi.SectionLabel))));
        Assert.Equal((top.ViewId, "up", (double?)null), ui.PageSection);
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.SectionLookItem("Left"))));
        Assert.Equal("left", ui.PageSection!.Value.Look);
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.SectionLookItem("Down"))));
        Assert.Equal("down", ui.PageSection!.Value.Look);

        var free = (X: 40.0, Y: 260.0);
        Assert.Null(SheetPage.Hit(sheet, free));
        Tap(ui, display, sheet, record, ui.PagePaper!.Value.Pixel(free.X, free.Y));
        Assert.Null(ui.PageSection!.Value.Cut);
        Assert.False(Drag(ui, display, sheet, record, ui.PagePaper!.Value.Pixel(top.X, top.Y), ui.PagePaper!.Value.Pixel(free.X, free.Y)));
        Assert.Empty(ui.TakeActions());
        Assert.Null(ui.PageSection!.Value.Cut);

        // The pointer goes through whole pixels, so a place on the paper is within one pixel, less than 1 mm here.
        Tap(ui, display, sheet, record, ui.PagePaper!.Value.Pixel(top.X + 3, top.Y + 6));
        var cut = ui.PageSection!.Value.Cut!.Value;
        Assert.InRange(cut, top.Y + 5, top.Y + 7);
        Tap(ui, display, sheet, record, ui.PagePaper!.Value.Pixel(free.X, free.Y));

        var action = Assert.IsType<SheetSectionAction>(Assert.Single(ui.TakeActions()));
        var point = SheetPage.DesignPoint(top, (top.X, cut));
        Assert.Equal((record.Id, top.ViewId, "down", point.X, point.Y, point.Z), (action.Sheet, action.View, action.Look, action.PointX, action.PointY, action.PointZ));
        Assert.Equal((30.0, 20 + ((cut - top.Y) / 2), 0.0), (action.PointX, action.PointY, action.PointZ));
        Assert.InRange(action.X, free.X - 1, free.X + 1);
        Assert.InRange(action.Y, free.Y - 1, free.Y + 1);
        Assert.Null(ui.PageSection);
        Assert.Null(ui.PageView);
    }

    // Item 2: Escape ends an open section first, then the selection; "Cancel section" ends it too.
    [Fact]
    public async Task Escape_And_Cancel_End_A_Section()
    {
        var (host, sheet, record) = await Setup();
        using var _ = host;
        var display = new Vector2(1280, 720);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display, sheet, record);
        ui.OpenPage();
        Frame(ui, display, sheet, record);
        Frame(ui, display, sheet, record);
        var front = sheet.Views[0];

        Tap(ui, display, sheet, record, ui.PagePaper!.Value.Pixel(front.X, front.Y));
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.PageItem(DesktopUi.SectionLabel))));
        Assert.NotNull(ui.PageSection);
        Frame(ui, display, sheet, record, new KeySample(InputKey.Escape, true, KeyModifiers.None, 1));
        Frame(ui, display, sheet, record, new KeySample(InputKey.Escape, false, KeyModifiers.None, 2));
        Assert.Null(ui.PageSection);
        Assert.Equal(front.ViewId, ui.PageView);

        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.PageItem(DesktopUi.SectionLabel))));
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.PageItem(DesktopUi.SectionCancelLabel))));
        Assert.Null(ui.PageSection);
        Assert.True(ui.PageOpen);
    }

    // Item 4: the action of the page gives one AddSheetSection with the first free letter, off the frame thread in the host,
    // and the text of the panel names the label; a look that is not valid gives the message of the command.
    [Fact]
    public async Task The_Action_Adds_A_Section_With_The_First_Free_Letter()
    {
        var (host, sheet, record) = await Setup();
        using var _ = host;
        var actions = new SheetActions(host.Session);
        var top = sheet.Views[1];

        Assert.Equal("Added the section A-A.", actions.Section(record.Id, top.ViewId, "up", (30, 20, 0), 60, 250));
        Assert.Equal("Added the section B-B.", actions.Section(record.Id, top.ViewId, "left", (30, 20, 0), 360, 250));
        Assert.StartsWith("AddSheetSection: The look of a section", actions.Section(record.Id, top.ViewId, "north", (30, 20, 0), 60, 250), StringComparison.Ordinal);
        var (after, _) = await Read(host);
        Assert.Equal(["A-A", "B-B"], after.Views.Where(view => view.Section is not null).Select(view => view.Texts.Single(text => text.Text.Contains('-', StringComparison.Ordinal)).Text));
        Assert.Contains(after.Views[1].Lines, line => line.Kind == "cut");
    }

    // Finding V204 of the codebase review of 2026-11-18 (b) (TASK-0416): a read without the view of an open section ends the
    // section and the selection, so the next drag moves a view; the choice of another sheet ends the section and the row of the
    // formats.
    [Fact]
    public async Task A_Section_Ends_When_Its_View_Or_Its_Sheet_Goes()
    {
        var (host, sheet, record) = await Setup();
        using var _ = host;
        var display = new Vector2(1280, 720);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display, sheet, record);
        ui.OpenPage();
        Frame(ui, display, sheet, record);
        Frame(ui, display, sheet, record);
        var (front, top) = (sheet.Views[0], sheet.Views[1]);
        ui.TakeActions();

        Tap(ui, display, sheet, record, ui.PagePaper!.Value.Pixel(top.X, top.Y));
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.PageItem(DesktopUi.SectionLabel))));
        Assert.Equal(top.ViewId, ui.PageSection?.View);
        Assert.Equal("Removed the view. Undo gives it back.", new SheetActions(host.Session).Remove(record.Id, top.ViewId));
        var (removed, removedRecord) = await Read(host);
        Frame(ui, display, removed, removedRecord);
        Assert.Null(ui.PageSection);
        Assert.Null(ui.PageView);
        Assert.True(Drag(ui, display, removed, removedRecord, ui.PagePaper!.Value.Pixel(front.X, front.Y), ui.PagePaper!.Value.Pixel(front.X + 10, front.Y)));
        Assert.Single(ui.TakeActions().OfType<SheetMoveAction>());

        new SheetActions(host.Session).Create("Sheet 2", "A3", true, (2, 1), SheetPlacement.Standard, Plate);
        var records = await host.Session.Read((document, _) => document.Sheets.ToList());
        void Both(params InputSample[] samples)
        {
            ui.ShowSheets(records);
            ui.ShowPage(removed, false);
            ui.Route(samples);
            ui.Frame(1f / 60f, display, Vector2.One, [Body], null, new DocumentLine("plate", null));
        }

        void TapBoth(Vector2 at)
        {
            Both(Pointer(at, PointerButtons.None, PointerPhase.Move, 0));
            Both(Pointer(at, PointerButtons.Left, PointerPhase.Down, 50_000_000));
            Both(Pointer(at, PointerButtons.Left, PointerPhase.Up, 100_000_000));
            Both();
            Both();
        }

        Both();
        Both();
        // The drag selected the front view.
        Assert.Equal(front.ViewId, ui.PageView);
        TapBoth(Middle(Item(ui, DesktopUi.PageItem(DesktopUi.SectionLabel))));
        Assert.NotNull(ui.PageSection);
        TapBoth(Middle(Item(ui, DesktopUi.PageItem($"Sheet {records[1].Id:N}"))));
        Assert.Equal(records[1].Id, ui.PageSheetId);
        Assert.Null(ui.PageSection);
        TapBoth(Middle(Item(ui, DesktopUi.PageItem($"Sheet {records[0].Id:N}"))));
        TapBoth(Middle(Item(ui, DesktopUi.PageItem(DesktopUi.ExportSheetLabel))));
        Assert.True(ui.ExportOpen);
        TapBoth(Middle(Item(ui, DesktopUi.PageItem($"Sheet {records[1].Id:N}"))));
        Assert.False(ui.ExportOpen);
    }

    // Finding V205 of the codebase review of 2026-11-18 (b) (TASK-0416): only one of the row of the formats and a section is
    // open: "Export" ends a section, "Section" closes the row, and Escape closes the row first.
    [Fact]
    public async Task The_Formats_And_A_Section_Are_Not_Open_Together()
    {
        var (host, sheet, record) = await Setup();
        using var _ = host;
        var display = new Vector2(1280, 720);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display, sheet, record);
        ui.OpenPage();
        Frame(ui, display, sheet, record);
        Frame(ui, display, sheet, record);
        var front = sheet.Views[0];

        Tap(ui, display, sheet, record, ui.PagePaper!.Value.Pixel(front.X, front.Y));
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.PageItem(DesktopUi.SectionLabel))));
        Assert.NotNull(ui.PageSection);
        Tap(ui, display, sheet, record, Middle(Item(ui, DesktopUi.PageItem(DesktopUi.ExportSheetLabel))));
        Assert.True(ui.ExportOpen);
        Assert.Null(ui.PageSection);

        Frame(ui, display, sheet, record, new KeySample(InputKey.Escape, true, KeyModifiers.None, 1));
        Frame(ui, display, sheet, record, new KeySample(InputKey.Escape, false, KeyModifiers.None, 2));
        Assert.False(ui.ExportOpen);
        Assert.True(ui.PageOpen);
    }

    // Finding V208 of the codebase review of 2026-11-18 (b) (TASK-0416): "Section" shows for a view whose frame is a flat view,
    // and not for a slanted view whose right has no rational length, which the engine refuses as a parent.
    [Fact]
    public async Task Section_Shows_Only_For_A_View_That_The_Engine_Can_Cut()
    {
        var (host, _, record) = await Setup();
        using var _ = host;
        var slant = await host.Session.Apply(new AddSheetViewCommand
        {
            SheetId = record.Id, RightX = 1, RightY = 1, RightZ = 0, UpX = -1, UpY = 1, UpZ = 0, EyeX = 0, EyeY = 0, EyeZ = 1,
            ScaleNumerator = 1, ScaleDenominator = 1, X = 60, Y = 260,
        });
        Assert.Equal(CommandStatus.Applied, slant.Status);
        var (sheet, withSlant) = await Read(host);
        var display = new Vector2(1280, 720);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Frame(ui, display, sheet, withSlant);
        ui.OpenPage();
        Frame(ui, display, sheet, withSlant);
        Frame(ui, display, sheet, withSlant);
        var (front, pictorial) = (sheet.Views[0], sheet.Views[^1]);

        Tap(ui, display, sheet, withSlant, ui.PagePaper!.Value.Pixel(pictorial.X, pictorial.Y));
        Assert.Equal(pictorial.ViewId, ui.PageView);
        Assert.Null(ui.ItemRectangle(DesktopUi.PageItem(DesktopUi.SectionLabel)));
        Assert.NotNull(ui.ItemRectangle(DesktopUi.PageItem(DesktopUi.ScaleViewLabel)));
        Tap(ui, display, sheet, withSlant, ui.PagePaper!.Value.Pixel(front.X, front.Y));
        Assert.Equal(front.ViewId, ui.PageView);
        Assert.NotNull(ui.ItemRectangle(DesktopUi.PageItem(DesktopUi.SectionLabel)));
    }

    private static void Frame(DesktopUi ui, Vector2 display, SheetInfo? sheet, SheetRecord? record, params InputSample[] samples)
    {
        ui.ShowSheets(record is null ? [] : [record]);
        ui.ShowPage(sheet, false);
        ui.Route(samples);
        ui.Frame(1f / 60f, display, Vector2.One, [Body], null, new DocumentLine("plate", null));
    }

    private static PointerSample Pointer(Vector2 at, PointerButtons buttons, PointerPhase phase, ulong time)
        => new(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, buttons, KeyModifiers.None, phase, time);

    private static void Tap(DesktopUi ui, Vector2 display, SheetInfo sheet, SheetRecord record, Vector2 at)
    {
        Frame(ui, display, sheet, record, Pointer(at, PointerButtons.None, PointerPhase.Move, 0));
        Frame(ui, display, sheet, record, Pointer(at, PointerButtons.Left, PointerPhase.Down, 50_000_000));
        Frame(ui, display, sheet, record, Pointer(at, PointerButtons.Left, PointerPhase.Up, 100_000_000));
        Frame(ui, display, sheet, record);
        Frame(ui, display, sheet, record);
    }

    // A drag from one place to another; true when a view moved during it.
    private static bool Drag(DesktopUi ui, Vector2 display, SheetInfo sheet, SheetRecord record, Vector2 from, Vector2 to)
    {
        Frame(ui, display, sheet, record, Pointer(from, PointerButtons.None, PointerPhase.Move, 0));
        Frame(ui, display, sheet, record, Pointer(from, PointerButtons.Left, PointerPhase.Down, 10_000_000));
        var moved = false;
        for (var i = 1; i <= 5; i++)
        {
            Frame(ui, display, sheet, record, Pointer(Vector2.Lerp(from, to, i / 5f), PointerButtons.Left, PointerPhase.Move, (ulong)(10_000_000 * (i + 1))));
            moved |= ui.PageDragging;
        }

        Frame(ui, display, sheet, record, Pointer(to, PointerButtons.Left, PointerPhase.Up, 100_000_000));
        Frame(ui, display, sheet, record);
        return moved;
    }

    private static Vector4 Item(DesktopUi ui, string item) => ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item}.");

    private static Vector2 Middle(Vector4 box) => new((box.X + box.Z) / 2f, (box.Y + box.W) / 2f);
}
