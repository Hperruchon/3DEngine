using System.Numerics;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// The dimension overlay of the desktop (ADR-0056, TASK-0178) with the library and no GPU: the toggles of the panel take a
// tap and a click and are off at each start, the line of the size shows with the dimensions only, and the strokes of the
// overall box and of the selected body.
[Collection(DearImGui.Collection)]
public class DimensionOverlayTests
{
    private static readonly Vector2 Display = new(1280, 720);

    private static readonly LiveBody[] Bodies =
    [
        new(Guid.Parse("11111111-0000-0000-0000-000000000000"), "Box", null, true),
        new(Guid.Parse("22222222-0000-0000-0000-000000000000"), "Box", null, true),
    ];

    private static readonly Guid Selected = Bodies[0].Id;

    private const string Size = "Size 1200 x 18 x 370 mm\nThickness 18 mm";

    private static void Step(DesktopUi ui, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, Display, Vector2.One, Bodies, Selected, size: Size);
    }

    private static DesktopUi Opened()
    {
        var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        Step(ui);
        Step(ui);
        return ui;
    }

    private static PointerSample Pointer(DeviceKind kind, (double X, double Y) at, PointerPhase phase)
        => new(kind, 1, kind == DeviceKind.Mouse ? 0UL : 3UL, at.X, at.Y, 1,
            kind == DeviceKind.Mouse ? PointerButtons.Left : PointerButtons.None, KeyModifiers.None, phase, 0);

    private static void Tap(DesktopUi ui, DeviceKind kind, string label)
    {
        var rectangle = ui.ItemRectangle(label) ?? throw new InvalidOperationException($"The panel has no control {label}.");
        var at = ((rectangle.X + rectangle.Z) / 2.0, (rectangle.Y + rectangle.W) / 2.0);
        Step(ui, Pointer(kind, at, PointerPhase.Down));
        Step(ui, Pointer(kind, at, PointerPhase.Up));
        Step(ui);
        Step(ui);
    }

    // Item 2: the toggles are off at each start, a tap and a click change each one, and Hidden too waits for Dimensions.
    [Theory]
    [InlineData(DeviceKind.Mouse)]
    [InlineData(DeviceKind.Touch)]
    public void The_Toggles_Are_Off_At_The_Start_And_Take_A_Tap_And_A_Click(DeviceKind kind)
    {
        using var ui = Opened();
        Assert.False(ui.ShowDimensions);
        Assert.False(ui.HiddenToo);

        Tap(ui, kind, DesktopUi.HiddenTooLabel);
        Assert.False(ui.HiddenToo);

        Tap(ui, kind, DesktopUi.DimensionsLabel);
        Assert.True(ui.ShowDimensions);
        Tap(ui, kind, DesktopUi.HiddenTooLabel);
        Assert.True(ui.HiddenToo);
        Tap(ui, kind, DesktopUi.DimensionsLabel);
        Assert.False(ui.ShowDimensions);
    }

    // Item 2 and ADR-0027 item 6: each toggle is at least 44 pixels high and inside the panel.
    [Fact]
    public void Each_Toggle_Is_At_Least_44_Pixels_High_And_Inside_The_Panel()
    {
        using var ui = Opened();
        var panel = ui.PanelRectangles[0];
        foreach (var label in new[] { DesktopUi.DimensionsLabel, DesktopUi.HiddenTooLabel })
        {
            var rectangle = ui.ItemRectangle(label) ?? throw new InvalidOperationException($"The panel has no control {label}.");
            Assert.True(rectangle.W - rectangle.Y >= DesktopUi.MinimumControlHeight, $"{label} is {rectangle.W - rectangle.Y} pixels high.");
            Assert.True(rectangle.X >= panel.X && rectangle.Z <= panel.Z && rectangle.Y >= panel.Y && rectangle.W <= panel.W, $"{label} at {rectangle} is not inside the panel {panel}.");
        }
    }

    // Item 6: the line of the size of the selected body shows while the dimensions show, and not before.
    [Fact]
    public void The_Size_Of_The_Selected_Body_Shows_With_The_Dimensions_Only()
    {
        using var ui = Opened();
        Assert.Null(ui.ItemRectangle(DesktopUi.SizeLabel));

        Tap(ui, DeviceKind.Mouse, DesktopUi.DimensionsLabel);

        Assert.NotNull(ui.ItemRectangle(DesktopUi.SizeLabel));
    }

    // Items 4 and 6: the overall box gives three dimension lines with arrows, six extension lines and three labels with
    // the unit, and the selected body gives the same set with its mark.
    [Fact]
    public void A_Box_Gives_Three_Dimension_Lines_Six_Extension_Lines_And_Three_Labels()
    {
        var camera = new Camera { FarPlane = 20000f };
        ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) overall = ((0, 0, 0), (1200, 1900, 370));
        camera.Frame(overall.Min, overall.Max, 1280f / 720f);
        ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) board = ((0, 480, 0), (1200, 498, 370));

        var strokes = DimensionOverlay.Of(overall, board, camera, 1280, 720, "mm");

        Assert.Equal(6, strokes.Lines.Count(line => line.Arrows));
        Assert.Equal(12, strokes.Lines.Count(line => !line.Arrows));
        Assert.Equal(
            ["X 1200 mm", "Y 1900 mm", "Z 370 mm", "X 1200 mm", "Y 18 mm", "Z 370 mm"],
            strokes.Labels.Select(label => label.Text));
        Assert.Equal(
            [.. Enumerable.Repeat(DimensionOverlay.Mark.Overall, 3), .. Enumerable.Repeat(DimensionOverlay.Mark.Selected, 3)],
            strokes.Labels.Select(label => label.Mark));
        Assert.All(strokes.Lines, line => Assert.True(float.IsFinite(line.A.X) && float.IsFinite(line.B.Y)));
        Assert.Empty(DimensionOverlay.Of(null, null, camera, 1280, 720, "mm").Lines);
    }

    // Item 6 (Q1 A): the size and the thickness of the selected body for the panel.
    [Fact]
    public void The_Size_Text_Gives_The_Extents_And_The_Thickness()
    {
        Assert.Equal(Size, DimensionOverlay.SizeText(((0, 0, 0), (1200, 18, 370)), null, "mm"));
        Assert.Equal("Size 1200 x 200 x 370 mm\nThickness 18 mm", DimensionOverlay.SizeText(((0, 0, 0), (1200, 200, 370)), -18, "mm"));
    }

    // The camera of the image test of the shelf (DimensionImageTests), with no GPU.
    private static Camera ShelfCamera(((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) shelf)
    {
        var camera = new Camera { YawDegrees = 30, PitchDegrees = 20, Distance = 40 };
        camera.FarPlane = camera.FarPlaneFor(shelf.Min, shelf.Max);
        camera.Frame(shelf.Min, shelf.Max, 1280f / 720f);
        camera.YawDegrees = 35;
        camera.Distance *= 1.3;
        return camera;
    }

    private static bool Overlap(DimensionOverlay.Label a, Vector2 aSize, DimensionOverlay.Label b, Vector2 bSize)
        => a.At.X < b.At.X + bSize.X && a.At.X + aSize.X > b.At.X && a.At.Y < b.At.Y + bSize.Y && a.At.Y + aSize.Y > b.At.Y;

    // Finding V79 of the codebase review of 2026-11-05 (TASK-0181): with the camera of the image test, the labels of the
    // thin board overlap where the overlay puts them. The panel places them apart, in the font of the panel, and inside
    // the window.
    [Fact]
    public void The_Labels_Of_A_Thin_Board_Do_Not_Overlap()
    {
        using var ui = Opened();
        ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) shelf = ((-18, 0, 0), (1200, 1900, 650));
        ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) board = ((0, 280, 18), (1200, 298, 388));
        var strokes = DimensionOverlay.Of(shelf, board, ShelfCamera(shelf), Display.X, Display.Y, "mm");
        var raw = strokes.Labels;
        Assert.Contains(raw, a => raw.Any(b => !ReferenceEquals(a, b) && Overlap(a, ui.TextSize(a.Text), b, ui.TextSize(b.Text))));

        ui.Frame(1f / 60f, Display, Vector2.One, Bodies, Selected, dimensions: strokes, size: Size);

        var placed = ui.PlacedLabels;
        Assert.Equal(raw.Select(label => label.Text), placed.Select(label => label.Text));
        for (var i = 0; i < placed.Count; i++)
        {
            var size = ui.TextSize(placed[i].Text);
            Assert.True(placed[i].At.X >= 0 && placed[i].At.Y >= 0 && placed[i].At.X + size.X <= Display.X && placed[i].At.Y + size.Y <= Display.Y, $"{placed[i].Text} at {placed[i].At} leaves the window.");
            for (var j = 0; j < i; j++)
                Assert.False(Overlap(placed[i], size, placed[j], ui.TextSize(placed[j].Text)), $"{placed[i].Text} meets {placed[j].Text}.");
        }
    }

    // V79: the placement moves a label that meets an earlier one, and keeps each label inside the window.
    [Fact]
    public void The_Placement_Moves_A_Label_Apart_And_Into_The_Window()
    {
        static Vector2 Size(string _) => new(100, 20);
        DimensionOverlay.Label[] labels =
        [
            new(new Vector2(500, 300), "A", DimensionOverlay.Mark.Overall),
            new(new Vector2(510, 305), "B", DimensionOverlay.Mark.Overall),
            new(new Vector2(1250, 715), "C", DimensionOverlay.Mark.Selected),
            new(new Vector2(-40, -10), "D", DimensionOverlay.Mark.Selected),
        ];

        var placed = DimensionOverlay.Place(labels, Size, Display);

        Assert.Equal(new Vector2(500, 300), placed[0].At);
        Assert.Equal(new Vector2(510, 322), placed[1].At);
        Assert.Equal(new Vector2(1180, 700), placed[2].At);
        Assert.Equal(new Vector2(0, 0), placed[3].At);
    }

    // Finding V80 of the codebase review of 2026-11-05 (TASK-0181): the camera of the review is close to the shelf, so the
    // dimension line of X runs out of the window. Its label is at the middle of the part inside the window.
    [Fact]
    public void A_Label_Is_At_The_Middle_Of_The_Part_Of_Its_Line_In_The_Window()
    {
        var camera = new Camera { Target = (600, 0, 650), Distance = 300, YawDegrees = 80, PitchDegrees = 10, FarPlane = 20000f };

        var strokes = DimensionOverlay.Of(((0, 0, 0), (1200, 1900, 650)), null, camera, Display.X, Display.Y, "mm");

        var x = Assert.Single(strokes.Labels, label => label.Text == "X 1200 mm");
        Assert.True(x.At.X is >= 0 and < 1280 && x.At.Y is >= -40 and < 720, $"The label of X is at {x.At}.");
        Assert.Equal((new Vector2(0, 10), new Vector2(100, 10)), DimensionOverlay.Inside(new Vector2(-100, 10), new Vector2(100, 10), 1280, 720));
        Assert.Null(DimensionOverlay.Inside(new Vector2(-100, -10), new Vector2(-50, -20), 1280, 720));
    }
}
