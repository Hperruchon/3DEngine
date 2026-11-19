using System.Numerics;
using Engine.Contracts;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0174 (phase R31a, TASK-0419): with no selected body the ring holds the tools of the part, with a second level for "New"
// and "Sketch"; a structure view, folded to one row, takes the place of the panel; the file name of the path line opens the
// file menu; and "Dimensions" and "Hidden too" are in the net of the views. ADR-0174 names this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class PartRingTests
{
    private static readonly Vector2 Landscape = new(1280, 720);
    private static readonly Vector2 Phone = new(412, 915);
    private static readonly LiveBody Plate = new(Guid.Parse("74000000-0000-0000-0000-000000000001"), "Box", "Plate", true);
    private static readonly LiveBody Post = new(Guid.Parse("74000000-0000-0000-0000-000000000002"), "Cylinder", "Post", true);
    private static readonly DocumentLine File = new("bracket.3de", null);
    private static readonly ImagePanel NoImages = new([], null, 0, null);

    // The host gives the centre of the box of the selected body, or of the visible bodies with no selection (ADR-0174 item 1);
    // part is that centre with no selection, or null for a host that gives none.
    private static void Step(DesktopUi ui, Vector2 display, IReadOnlyList<LiveBody>? bodies = null, Guid? selection = null, ImagePanel? images = null, params InputSample[] frame)
        => StepAt(ui, display, null, bodies, selection, images, frame);

    private static void StepAt(DesktopUi ui, Vector2 display, Vector2? part, IReadOnlyList<LiveBody>? bodies = null, Guid? selection = null, ImagePanel? images = null, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, display, Vector2.One, bodies ?? [Plate, Post], selection, File, imagePanel: images,
            ringCentre: selection is null ? part : display / 2f, ringBody: 80f);
    }

    private static PointerSample Pointer(Vector2 at, PointerPhase phase, ulong ms)
        => new(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.Left, KeyModifiers.None, phase, ms * 1_000_000);

    private static Vector2 Mid(Vector4 box) => new((box.X + box.Z) / 2f, (box.Y + box.W) / 2f);

    // A tap on the middle of an item, and a frame after it.
    private static void Tap(DesktopUi ui, Vector2 display, string item, IReadOnlyList<LiveBody>? bodies = null, Guid? selection = null, ImagePanel? images = null)
    {
        var at = Mid(ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item}."));
        Step(ui, display, bodies, selection, images, Pointer(at, PointerPhase.Down, 0));
        Step(ui, display, bodies, selection, images, Pointer(at, PointerPhase.Up, 50));
        Step(ui, display, bodies, selection, images);
    }

    private static DesktopUi Started(Vector2 display, IReadOnlyList<LiveBody>? bodies = null, ImagePanel? images = null)
    {
        // The first frame sizes the structure view at its whole room, so the ring of the part takes its place from the third frame.
        var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        for (var i = 0; i < 3; i++)
            Step(ui, display, bodies, images: images);
        return ui;
    }

    // Item 1: with no selected body the ring holds "New", "Sketch" and "Bodies", and "Images" when the host gives images; with
    // a selected body it holds the tools of the body. On a landscape display its caption is "TOOLS FOR THE PART", its centre
    // is the centre that the host gives, and with no centre from the host the structure view does not move it.
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(412, 915)]
    public void With_No_Selection_The_Ring_Holds_The_Tools_Of_The_Part(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = Started(display);
        Assert.Equal([DesktopUi.NewLabel, DesktopUi.PartSketchLabel, DesktopUi.BodiesLabel], ui.RingLabels);

        using var images = Started(display, images: NoImages);
        Assert.Equal([DesktopUi.NewLabel, DesktopUi.PartSketchLabel, DesktopUi.BodiesLabel, DesktopUi.PartImagesLabel], images.RingLabels);

        using var empty = Started(display, bodies: []);
        Assert.Equal([DesktopUi.NewLabel, DesktopUi.PartSketchLabel], empty.RingLabels);

        Step(ui, display, selection: Plate.Id);
        Assert.Contains(DesktopUi.MoveLabel, ui.RingLabels);
        Assert.DoesNotContain(DesktopUi.NewLabel, ui.RingLabels);

        if (width > height)
        {
            Step(ui, display);
            Step(ui, display);
            Assert.NotNull(ui.ItemRectangle(DesktopUi.RingCaptionItem));
            var folded = ui.RingLayoutOfFrame?.Centre ?? throw new InvalidOperationException("No ring.");
            ui.OpenStructure();
            Step(ui, display);
            Step(ui, display);
            Assert.Equal(folded, ui.RingLayoutOfFrame?.Centre);

            var part = new Vector2(700f, 380f);
            StepAt(ui, display, part);
            Assert.Equal(part, ui.RingLayoutOfFrame?.Centre);
        }
    }

    // Item 1: the ring of the part does not show on the paper of the lines of a flat view, where its labels do not read.
    [Fact]
    public void The_Ring_Of_The_Part_Does_Not_Show_On_The_Paper_Of_A_Flat_View()
    {
        using var ui = Started(Landscape);
        ui.ShowLines([new ThreeDEngine.Desktop.ViewLines.Stroke([new Vector2(700, 400), new Vector2(900, 400)], true)]);
        Step(ui, Landscape);
        Step(ui, Landscape);
        Assert.NotNull(ui.PaperShown);
        Assert.Empty(ui.RingLabels);
        ui.ShowLines(null);
        Step(ui, Landscape);
        Assert.NotEmpty(ui.RingLabels);
    }

    // Item 2: "New" opens Box, Cylinder, Cone, Sphere, Torus and Back; a kind opens its pad and the ring goes back to the first
    // level. "Back" goes back with no action.
    [Fact]
    public void New_Opens_Its_Second_Level_And_A_Kind_Opens_Its_Pad()
    {
        using var ui = Started(Landscape);
        Tap(ui, Landscape, DesktopUi.RingTextItem(DesktopUi.NewLabel));
        Assert.Equal([.. DesktopUi.PartNewTools, DesktopUi.BackLabel], ui.RingLabels);
        Assert.Equal("Box", DesktopUi.RingText(DesktopUi.NewBoxLabel));

        Tap(ui, Landscape, DesktopUi.RingTextItem(DesktopUi.BackLabel));
        Assert.Equal(DesktopUi.PartLevel.Tools, ui.PartLevelOfFrame);
        Assert.Empty(ui.TakeActions());
        Assert.Null(ui.Pad);

        Tap(ui, Landscape, DesktopUi.RingTextItem(DesktopUi.NewLabel));
        Tap(ui, Landscape, DesktopUi.RingTextItem(DesktopUi.NewCylinderLabel));
        Assert.Equal(DesktopUi.NewCylinderLabel, ui.Pad?.Title);
        Assert.Equal(DesktopUi.PartLevel.Tools, ui.PartLevelOfFrame);
    }

    // Item 2: "Sketch" opens XY, XZ, YZ, Face and Back; XY starts a sketch on the plane XY, Face arms a tap on a face, and the
    // ring goes back to the first level.
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(412, 915)]
    public void Sketch_Opens_The_Planes_And_A_Plane_Starts_A_Sketch(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = Started(display);
        Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.PartSketchLabel));
        Assert.Equal([.. DesktopUi.NewSketchButtons.Select(button => button.Label), DesktopUi.SketchOnFaceLabel, DesktopUi.BackLabel], ui.RingLabels);

        Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.NewSketchButtons[0].Label));
        Assert.Equal([new NewSketchAction(DesktopUi.NewSketchButtons[0].Datum)], ui.TakeActions());
        Assert.Equal(DesktopUi.PartLevel.Tools, ui.PartLevelOfFrame);

        Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.PartSketchLabel));
        Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.SketchOnFaceLabel));
        Assert.Contains(ui.TakeActions(), action => action is SketchOnFaceAction);
    }

    // Item 2: a selection takes the ring of the part back to its first level.
    [Fact]
    public void A_Selection_Takes_The_Ring_Back_To_Its_First_Level()
    {
        using var ui = Started(Landscape);
        Tap(ui, Landscape, DesktopUi.RingTextItem(DesktopUi.NewLabel));
        Assert.Equal(DesktopUi.PartLevel.New, ui.PartLevelOfFrame);
        Step(ui, Landscape, selection: Plate.Id);
        Step(ui, Landscape);
        Assert.Equal(DesktopUi.PartLevel.Tools, ui.PartLevelOfFrame);
    }

    // Item 3: the structure view starts folded, with its first row only; a tap on that row, or on "Bodies", opens it with a row
    // for each body, and a second tap folds it. It has the ground of a window and is at most 360 pixels wide. "Show all" shows
    // only while a body is hidden, and the filter only from eight bodies.
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(412, 915)]
    public void The_Structure_View_Folds_And_Opens(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = Started(display);
        var row = DesktopUi.RowText(Plate);
        Assert.NotNull(ui.ItemRectangle(DesktopUi.DocumentRowItem));
        Assert.Null(ui.ItemRectangle(row));
        Assert.True(ui.PanelRectangles[0].Z - ui.PanelRectangles[0].X <= DesktopUi.RowsWidth + 0.5f);

        Tap(ui, display, DesktopUi.DocumentRowItem);
        Assert.True(ui.StructureOpen);
        Assert.NotNull(ui.ItemRectangle(row));
        Assert.Null(ui.ItemRectangle(DesktopUi.ShowAllLabel));
        Assert.Null(ui.ItemRectangle(DesktopUi.FilterLabel));

        Tap(ui, display, row);
        Assert.Equal([new SelectBodyAction(Plate.Id)], ui.TakeActions());

        Tap(ui, display, DesktopUi.DocumentRowItem);
        Assert.False(ui.StructureOpen);
        Tap(ui, display, DesktopUi.RingTextItem(DesktopUi.BodiesLabel));
        Assert.True(ui.StructureOpen);

        IReadOnlyList<LiveBody> hidden = [Plate with { Visible = false }, Post];
        Step(ui, display, hidden);
        Step(ui, display, hidden);
        Assert.NotNull(ui.ItemRectangle(DesktopUi.ShowAllLabel));

        IReadOnlyList<LiveBody> many = [.. Enumerable.Range(1, DesktopUi.FilterFrom).Select(i => new LiveBody(Guid.Parse($"74000000-0000-0000-0001-{i:D12}"), "Box", $"Box {i}", true))];
        Step(ui, display, many);
        Step(ui, display, many);
        Assert.NotNull(ui.ItemRectangle(DesktopUi.FilterLabel));
    }

    // Item 4: with no selected body a tap on the file name opens the file menu under it; a tap on an entry closes it and gives
    // the action; a tap outside closes it with no action. With a selected body the file name selects the document.
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(412, 915)]
    public void The_File_Name_Opens_The_File_Menu(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = Started(display);
        var file = DesktopUi.PathSegmentItem(PathLevel.Document);
        Assert.Null(ui.ItemRectangle(DesktopUi.SaveAsLabel));

        Tap(ui, display, file);
        Assert.True(ui.FileMenuOpen);
        var menu = ui.ItemRectangle(DesktopUi.FileMenuItem) ?? throw new InvalidOperationException("No file menu.");
        Assert.True(menu.Y >= ui.ItemRectangle(file)!.Value.W, "The menu is not under the file name.");
        foreach (var label in new[] { DesktopUi.OpenProjectLabel, DesktopUi.SaveAsLabel, DesktopUi.ExportStlLabel, DesktopUi.ImportStlLabel })
            Assert.NotNull(ui.ItemRectangle(label));

        Tap(ui, display, DesktopUi.SaveAsLabel);
        Assert.False(ui.FileMenuOpen);
        Assert.Equal([new SaveAsAction()], ui.TakeActions());

        // A release outside the menu on a tool of the ring closes the menu only, and the ring takes no tap.
        Tap(ui, display, file);
        Assert.True(ui.FileMenuOpen);
        var outside = Mid(ui.ItemRectangle(DesktopUi.RingTextItem(DesktopUi.NewLabel)) ?? throw new InvalidOperationException("No New."));
        Step(ui, display, null, null, null, Pointer(outside, PointerPhase.Down, 0));
        Step(ui, display, null, null, null, Pointer(outside, PointerPhase.Up, 50));
        Step(ui, display);
        Assert.False(ui.FileMenuOpen);
        Assert.Empty(ui.TakeActions());
        Assert.Equal(DesktopUi.PartLevel.Tools, ui.PartLevelOfFrame);

        Step(ui, display, selection: Plate.Id);
        Tap(ui, display, file, selection: Plate.Id);
        Assert.False(ui.FileMenuOpen);
        Assert.Equal([new PathAction(PathLevel.Document)], ui.TakeActions());
    }

    // Item 5: the net of the views holds "Dimensions" and "Hidden too" on the row of "Lines". A tap changes the switch and does
    // not close the net; "Hidden too" takes no tap while "Dimensions" is off; each text fits its cell at the least scale.
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(360, 640)]
    public void The_Dimension_Switches_Are_In_The_Net_Of_The_Views(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = Started(display);
        // A new window of Dear ImGui takes no tap in its first frame, so the net has two frames before a tap.
        ui.OpenViews();
        Step(ui, display);
        Step(ui, display);
        var lines = ui.ItemRectangle(DesktopUi.LinesItem) ?? throw new InvalidOperationException("No Lines.");
        foreach (var label in new[] { DesktopUi.DimensionsLabel, DesktopUi.HiddenTooLabel })
        {
            var cell = ui.ItemRectangle(label) ?? throw new InvalidOperationException($"No {label}.");
            Assert.Equal(lines.Y, cell.Y, 0.5f);
            Assert.True(ui.ViewCellNeed(label) <= cell.Z - cell.X, $"{label} needs {ui.ViewCellNeed(label)} in a cell of {cell.Z - cell.X} at {display}.");
        }

        Tap(ui, display, DesktopUi.HiddenTooLabel);
        Assert.False(ui.HiddenToo);
        Tap(ui, display, DesktopUi.DimensionsLabel);
        Assert.True(ui.ShowDimensions);
        Assert.True(ui.ViewsOpen);
        Tap(ui, display, DesktopUi.HiddenTooLabel);
        Assert.True(ui.HiddenToo);
    }
}
