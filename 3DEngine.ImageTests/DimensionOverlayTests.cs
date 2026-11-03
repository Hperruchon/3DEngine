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
}
