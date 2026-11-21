using System.Numerics;
using Engine.Contracts;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0175 (phase R31b, TASK-0420): in the density "Compact" the rows of the structure view are 30 pixels high with the text
// of a list; in the density "For touch" they are 44 pixels high. The device gives the first density, and two cells of the net
// of the views change it. Each other control keeps 44 pixels. ADR-0175 names this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class RowDensityTests
{
    private static readonly Vector2 Landscape = new(1280, 720);
    private static readonly LiveBody Plate = new(Guid.Parse("75000000-0000-0000-0000-000000000001"), "Box", "Plate", true);
    private static readonly LiveBody Post = new(Guid.Parse("75000000-0000-0000-0000-000000000002"), "Cylinder", "Post", true);
    private static readonly DocumentLine File = new("bracket.3de", null);

    private static void Step(DesktopUi ui, Vector2 display, IReadOnlyList<LiveBody>? bodies = null, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, display, Vector2.One, bodies ?? [Plate, Post], null, File, ringCentre: display / 2f, ringBody: 80f);
    }

    private static PointerSample Pointer(Vector2 at, PointerPhase phase, ulong ms)
        => new(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.Left, KeyModifiers.None, phase, ms * 1_000_000);

    private static Vector2 Mid(Vector4 box) => new((box.X + box.Z) / 2f, (box.Y + box.W) / 2f);

    private static Vector4 Rectangle(DesktopUi ui, string item)
        => ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item}.");

    // A tap on the middle of an item, and a frame after it; the actions of the tap.
    private static IReadOnlyList<PanelAction> Tap(DesktopUi ui, Vector2 display, string item, IReadOnlyList<LiveBody>? bodies = null)
    {
        ui.TakeActions();
        var at = Mid(Rectangle(ui, item));
        Step(ui, display, bodies, Pointer(at, PointerPhase.Down, 0));
        Step(ui, display, bodies, Pointer(at, PointerPhase.Up, 50));
        Step(ui, display, bodies);
        return ui.TakeActions();
    }

    // The structure view open, after three frames.
    private static DesktopUi Opened(Vector2 display, DesktopUi.RowDensity density, IReadOnlyList<LiveBody>? bodies = null)
    {
        var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre) { Density = density };
        ui.OpenStructure();
        for (var i = 0; i < 3; i++)
            Step(ui, display, bodies);
        return ui;
    }

    // Item 1: a phone starts "For touch", and each other device starts "Compact".
    [Fact]
    public void A_Phone_Starts_For_Touch_And_Each_Other_Device_Starts_Compact()
    {
        Assert.Equal(OperatingSystem.IsAndroid() ? DesktopUi.RowDensity.ForTouch : DesktopUi.RowDensity.Compact, DesktopUi.DeviceDensity);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Assert.Equal(DesktopUi.DeviceDensity, ui.Density);
    }

    // Items 1 and 3: each row of the structure view is as high as a row of the density, two body rows have no gap between
    // them, and the text of a compact row is the text of a list. A compact first row is one line that ends before the mark at
    // its right; a first row for touch is two lines. A tap on a compact body row selects the body.
    [Theory]
    [InlineData(1280, 720, true)]
    [InlineData(360, 640, true)]
    [InlineData(1280, 720, false)]
    [InlineData(360, 640, false)]
    public void Each_Row_Of_The_Structure_View_Has_The_Height_And_The_Text_Of_Its_Density(int width, int height, bool compact)
    {
        var display = new Vector2(width, height);
        using var ui = Opened(display, compact ? DesktopUi.RowDensity.Compact : DesktopUi.RowDensity.ForTouch);
        var row = compact ? DesktopUi.CompactRowHeight : DesktopUi.RowHeight;

        var first = Rectangle(ui, DesktopUi.DocumentRowItem);
        var rows = new[] { Plate, Post }.Select(body => Rectangle(ui, DesktopUi.RowText(body))).ToArray();
        foreach (var box in rows)
            Assert.True(Math.Abs(box.W - box.Y - row) <= 0.5f, $"The row at {box} is not {row} pixels high.");
        // A first row of two lines has a margin of 4 pixels above and under them.
        var firstRow = compact ? DesktopUi.CompactRowHeight : (2f * DesktopUi.FontSize) + 8f;
        Assert.True(Math.Abs(first.W - first.Y - firstRow) <= 0.5f, $"The first row at {first} is not {firstRow} pixels high.");
        // The view ends one margin of the window (14 pixels) under its last row, open and folded.
        Assert.True(Math.Abs(ui.PanelRectangles[0].W - rows[1].W - 14f) <= 0.5f, $"The view at {ui.PanelRectangles[0]} does not end 14 pixels under its last row at {rows[1]}.");
        Assert.True(Math.Abs(rows[1].Y - rows[0].W) <= 0.5f, $"The rows at {rows[0]} and {rows[1]} have a gap.");
        Assert.Equal(compact ? DesktopUi.ListSize : DesktopUi.FontSize, ui.StructureRowFontSize);

        var state = string.Concat(ui.DocumentState([Plate, Post]).Select(part => part.Text));
        var text = compact ? ui.ListTextWidth("Part  " + state) : ui.TextWidths(state).Sans;
        // Dear ImGui rounds the width of each text up to a whole pixel, and the first row measures two texts.
        Assert.True(Math.Abs(ui.StructureTextEnd - first.X - text) <= 2f, $"The first row ends at {ui.StructureTextEnd}, not at {first.X + text}.");
        // The mark is 12 pixels left of the right end of the row, and 6 pixels wide on each side.
        Assert.True(ui.StructureTextEnd <= first.Z - 18f, $"The text of the first row ends at {ui.StructureTextEnd}, on the mark of the row at {first}.");

        Assert.Equal([new SelectBodyAction(Post.Id)], Tap(ui, display, DesktopUi.RowText(Post)));

        ui.OpenStructure(false);
        Step(ui, display);
        Step(ui, display);
        first = Rectangle(ui, DesktopUi.DocumentRowItem);
        Assert.True(Math.Abs(ui.PanelRectangles[0].W - first.W - 14f) <= 0.5f, $"The folded view at {ui.PanelRectangles[0]} does not end 14 pixels under its row at {first}.");
    }

    // Finding V220 of the codebase review of 2026-11-20 (TASK-0426): in the narrowest structure view (560 by 560, a row of 288
    // pixels), the state of the first row of an assembly of 1,000 failed bodies is cut to the width of the row less its mark, in
    // each density. Until TASK-0426 the text ran under the mark (to 322 and 336 pixels) and the edge of the window cut it.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void The_First_Row_Ends_Before_Its_Mark_In_A_Narrow_Structure_View(bool compact)
    {
        var display = new Vector2(560, 560);
        LiveBody[] bodies = [.. Enumerable.Range(1, 1000).Select(i => new LiveBody(Guid.Parse($"75000000-0000-0000-0002-{i:D12}"), "Box", $"Box {i}", true, new BodyFault("E-FEATURE-REBUILD", "The profile is open.")))];
        var assembly = new DocumentLine("bracket.3de", null, Kind: Document.Assembly);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre) { Density = compact ? DesktopUi.RowDensity.Compact : DesktopUi.RowDensity.ForTouch };
        for (var i = 0; i < 3; i++)
            ui.Frame(1f / 60f, display, Vector2.One, bodies, null, assembly, ringCentre: display / 2f, ringBody: 80f);

        var first = Rectangle(ui, DesktopUi.DocumentRowItem);
        Assert.True(ui.StructureTextEnd <= first.Z - 18f, $"The text of the first row ends at {ui.StructureTextEnd}, on the mark of the row at {first}.");
        Assert.True(ui.StructureTextEnd > first.X + 40f, $"The first row at {first} shows no state.");
    }

    // Item 1: in the density "Compact" each control that is not a row of the structure view keeps 44 pixels: the filter, Clear,
    // "Show all", the path line, Undo, Redo, the tools of the ring and the cells of the net of the views.
    [Fact]
    public void Each_Other_Control_Keeps_44_Pixels_In_The_Density_Compact()
    {
        LiveBody[] bodies = [Plate with { Visible = false }, .. Enumerable.Range(1, DesktopUi.FilterFrom - 1).Select(i => new LiveBody(Guid.Parse($"75000000-0000-0000-0001-{i:D12}"), "Box", $"Box {i}", true))];
        using var ui = Opened(Landscape, DesktopUi.RowDensity.Compact, bodies);
        Assert.True(Math.Abs(Rectangle(ui, DesktopUi.RowText(bodies[1])).W - Rectangle(ui, DesktopUi.RowText(bodies[1])).Y - DesktopUi.CompactRowHeight) <= 0.5f, "The body row is not compact.");
        var controls = new[] { DesktopUi.FilterLabel, DesktopUi.ClearLabel, DesktopUi.ShowAllLabel, DesktopUi.PathSegmentItem(PathLevel.Document), DesktopUi.UndoLabel, DesktopUi.RedoLabel }
            .Select(label => (label, Rectangle(ui, label)))
            .Append((DesktopUi.NewLabel, ui.RingTarget(DesktopUi.NewLabel) ?? throw new InvalidOperationException("No ring.")))
            .ToList();
        // "Show all" has the gap of two items above it.
        var last = Rectangle(ui, DesktopUi.RowText(bodies[^1]));
        Assert.True(Rectangle(ui, DesktopUi.ShowAllLabel).Y >= last.W + 5f, "Show all touches the last body row.");
        ui.OpenViews();
        Step(ui, Landscape, bodies);
        Step(ui, Landscape, bodies);
        controls.AddRange(new[] { DesktopUi.CompactLabel, DesktopUi.ForTouchLabel, DesktopUi.ViewItem(FlatView.Top) }.Select(label => (label, Rectangle(ui, label))));
        foreach (var (label, box) in controls)
            Assert.True(box.W - box.Y >= DesktopUi.MinimumControlHeight - 0.5f, $"{label} at {box} is lower than 44 pixels.");
    }

    // Item 2: the net of the views holds "Compact" at the left of "Top" and "For touch" at its right, on the row of "Top". A
    // tap sets the density and does not close the net. The text of each cell fits its cell at the least scale.
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(360, 640)]
    public void The_Net_Of_The_Views_Sets_The_Density(int width, int height)
    {
        var display = new Vector2(width, height);
        using var ui = Opened(display, DesktopUi.RowDensity.Compact);
        ui.OpenViews();
        Step(ui, display);
        Step(ui, display);

        var top = Rectangle(ui, DesktopUi.ViewItem(FlatView.Top));
        var compact = Rectangle(ui, DesktopUi.CompactLabel);
        var touch = Rectangle(ui, DesktopUi.ForTouchLabel);
        Assert.True(Math.Abs(compact.Y - top.Y) <= 0.5f && Math.Abs(touch.Y - top.Y) <= 0.5f, $"The cells at {compact} and {touch} are not on the row of Top at {top}.");
        Assert.True(compact.Z <= top.X && top.X - compact.Z <= 8f && touch.X >= top.Z && touch.X - top.Z <= 8f, $"The cells at {compact} and {touch} are not next to Top at {top}.");
        foreach (var (label, box) in new[] { (DesktopUi.CompactLabel, compact), (DesktopUi.ForTouchLabel, touch) })
            Assert.True(ui.ViewCellNeed(label) <= box.Z - box.X, $"{label} needs {ui.ViewCellNeed(label)} pixels in a cell of {box.Z - box.X}.");

        Assert.Empty(Tap(ui, display, DesktopUi.ForTouchLabel));
        Assert.Equal(DesktopUi.RowDensity.ForTouch, ui.Density);
        Assert.True(ui.ViewsOpen);
        Tap(ui, display, DesktopUi.CompactLabel);
        Assert.Equal(DesktopUi.RowDensity.Compact, ui.Density);
        Assert.True(ui.ViewsOpen);
    }
}
