using System.Numerics;
using Engine.Contracts;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0122 (phase R20b, TASK-0348): "Dimensions" is a segment of the ring of a body. ADR-0122 names this file in its
// enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class RingDimensionsTests
{
    private static readonly Vector2 Landscape = new(1440, 900);
    private static readonly Guid Body = Guid.Parse("65000000-0000-0000-0000-000000000000");
    private static readonly Guid SketchStep = Guid.Parse("65000000-0000-0000-0000-000000000001");
    private static readonly LiveBody Extrude = new(Body, "Extrude", "Bracket", true);
    private static readonly LiveBody Box = new(Body, "Box", "Block", true);

    private static readonly IReadOnlyList<HistoryStep> Made =
    [
        new(Body, "Extrude", "Bracket", "correct", null, HistoryTap.None),
        new(SketchStep, "CreateSketch", "Plate on XY", "correct", null, HistoryTap.EditSketch),
    ];

    private static void Step(DesktopUi ui, Vector2 display, LiveBody body, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, display, Vector2.One, [body], body.Id, null, ringCentre: display / 2f, ringBody: 120f, history: body.Kind == "Extrude" ? Made : [Made[0]]);
    }

    private static IReadOnlyList<PanelAction> Tap(DesktopUi ui, Vector2 display, LiveBody body, string item)
    {
        ui.TakeActions();
        var at = ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item}.");
        var (x, y) = ((at.X + at.Z) / 2f, (at.Y + at.W) / 2f);
        PointerSample Mouse(PointerPhase phase, ulong ms)
            => new(DeviceKind.Mouse, 1, 0, x, y, 1, phase == PointerPhase.Move ? PointerButtons.None : PointerButtons.Left, KeyModifiers.None, phase, ms * 1_000_000);
        Step(ui, display, body, Mouse(PointerPhase.Move, 0));
        Step(ui, display, body, Mouse(PointerPhase.Down, 10));
        Step(ui, display, body, Mouse(PointerPhase.Up, 20));
        Step(ui, display, body);
        return ui.TakeActions();
    }

    // Items 1 and 2: a tap on "Dimensions" turns the dimensions on, with no action to the engine, and the segment has the
    // colour of an active control while they are on; a second tap turns them off. The segment has its icon.
    [Fact]
    public void A_Tap_On_Dimensions_Turns_The_Dimensions_On_And_Off()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, Landscape, Extrude);
        Step(ui, Landscape, Extrude);
        Assert.Contains(DesktopUi.DimensionsLabel, ui.RingLabels);
        Assert.NotNull(ui.ItemRectangle(DesktopUi.RingIconItem(DesktopUi.DimensionsLabel)));
        Assert.False(ui.ShowDimensions);
        Assert.False(ui.RingInUse(DesktopUi.DimensionsLabel));

        Assert.Empty(Tap(ui, Landscape, Extrude, DesktopUi.RingItem(DesktopUi.DimensionsLabel)));
        Assert.True(ui.ShowDimensions);
        Assert.True(ui.RingInUse(DesktopUi.DimensionsLabel));
        Assert.False(ui.RingInUse(DesktopUi.FocusLabel));

        Assert.Empty(Tap(ui, Landscape, Extrude, DesktopUi.RingItem(DesktopUi.DimensionsLabel)));
        Assert.False(ui.ShowDimensions);
        Assert.False(ui.RingInUse(DesktopUi.DimensionsLabel));
    }

    // Item 1: the ring and the toggle of the panel change one state, so a toggle that the panel turned on shows on the ring.
    [Fact]
    public void The_Ring_Shows_The_Dimensions_That_The_Panel_Turned_On()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        ui.ShowDimensionsOn();
        Step(ui, Landscape, Box);
        Step(ui, Landscape, Box);
        Assert.True(ui.RingInUse(DesktopUi.DimensionsLabel));
    }

    // Item 1: on a small landscape ring "Rename" leaves first, then "Edit sketch", then "Dimensions"; a box at the size of a
    // phone keeps "Dimensions", and a portrait ring keeps it.
    [Theory]
    [InlineData(640, 360, "Extrude", new[] { DesktopUi.MoveLabel, DesktopUi.SetDistanceLabel, DesktopUi.HideLabel, DesktopUi.FocusLabel })]
    [InlineData(915, 412, "Box", new[] { DesktopUi.MoveLabel, DesktopUi.HideLabel, DesktopUi.FocusLabel, DesktopUi.DimensionsLabel })]
    [InlineData(390, 844, "Box", new[] { DesktopUi.MoveLabel, DesktopUi.HideLabel, DesktopUi.FocusLabel, DesktopUi.DimensionsLabel })]
    [InlineData(390, 844, "Extrude", new[] { DesktopUi.EditSketchLabel, DesktopUi.MoveLabel, DesktopUi.SetDistanceLabel, DesktopUi.HideLabel, DesktopUi.FocusLabel, DesktopUi.DimensionsLabel })]
    public void Dimensions_Leaves_A_Small_Ring_After_Rename_And_Edit_Sketch(int width, int height, string kind, string[] labels)
    {
        var display = new Vector2(width, height);
        var body = kind == "Extrude" ? Extrude : Box;
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, display, body);
        Step(ui, display, body);
        Assert.Equal(labels, ui.RingLabels);
    }
}
