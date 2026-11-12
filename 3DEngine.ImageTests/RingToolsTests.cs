using System.Numerics;
using Engine.Contracts;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0119 (phase R18e, TASK-0346): "Edit sketch" and "Rename" on the ring of a body, the span of a landscape ring, and the
// history arc drawn as in the render. ADR-0119 names this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class RingToolsTests
{
    private static readonly Vector2 Landscape = new(1440, 900);
    private static readonly Vector2 Centre = new(720, 450);
    private static readonly Guid Body = Guid.Parse("64000000-0000-0000-0000-000000000000");
    private static readonly Guid SketchStep = Guid.Parse("64000000-0000-0000-0000-000000000001");
    private static readonly LiveBody Extrude = new(Body, "Extrude", "Bracket", true);
    private static readonly LiveBody Box = new(Body, "Box", "Block", true);

    private static readonly IReadOnlyList<HistoryStep> Made =
    [
        new(Body, "Extrude", "Bracket", "correct", null, HistoryTap.None),
        new(SketchStep, "CreateSketch", "Plate on XY", "correct", null, HistoryTap.EditSketch),
    ];

    private static void Step(DesktopUi ui, Vector2 display, LiveBody body, IReadOnlyList<HistoryStep>? history, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, display, Vector2.One, [body], body.Id, null, ringCentre: display.X >= display.Y ? Centre : new Vector2(display.X / 2f, display.Y / 2f), ringBody: 120f, history: history);
    }

    private static IReadOnlyList<PanelAction> Tap(DesktopUi ui, Vector2 display, LiveBody body, IReadOnlyList<HistoryStep>? history, string item)
    {
        ui.TakeActions();
        var at = ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item}.");
        var (x, y) = ((at.X + at.Z) / 2f, (at.Y + at.W) / 2f);
        PointerSample Mouse(PointerPhase phase, ulong ms)
            => new(DeviceKind.Mouse, 1, 0, x, y, 1, phase == PointerPhase.Move ? PointerButtons.None : PointerButtons.Left, KeyModifiers.None, phase, ms * 1_000_000);
        Step(ui, display, body, history, Mouse(PointerPhase.Move, 0));
        Step(ui, display, body, history, Mouse(PointerPhase.Down, 10));
        Step(ui, display, body, history, Mouse(PointerPhase.Up, 20));
        Step(ui, display, body, history);
        return ui.TakeActions();
    }

    // Item 1: on a landscape display the ring of a body that a sketch made starts with "Edit sketch" and ends with "Rename",
    // as the render; a tap on "Edit sketch" opens the sketch of its step, and a tap on "Rename" opens the name field.
    [Fact]
    public void Edit_Sketch_Opens_The_Sketch_And_Rename_Opens_The_Name_Field()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, Landscape, Extrude, Made);
        Step(ui, Landscape, Extrude, Made);
        // ADR-0122 item 1 (phase R20b, TASK-0348): "Dimensions" is before "Rename".
        Assert.Equal([DesktopUi.EditSketchLabel, DesktopUi.MoveLabel, DesktopUi.SetDistanceLabel, DesktopUi.HideLabel, DesktopUi.FocusLabel, DesktopUi.DimensionsLabel, DesktopUi.RenameLabel], ui.RingLabels);

        Assert.Equal([new EditSketchAction(SketchStep)], Tap(ui, Landscape, Extrude, Made, DesktopUi.RingItem(DesktopUi.EditSketchLabel)));
        Assert.Null(ui.ItemRectangle(DesktopUi.RenameItem));
        Assert.Empty(Tap(ui, Landscape, Extrude, Made, DesktopUi.RingItem(DesktopUi.RenameLabel)));
        Assert.NotNull(ui.ItemRectangle(DesktopUi.RenameItem));
    }

    // Item 1: a body that no sketch made has no "Edit sketch"; a portrait display has no "Rename" on the thumb ring, because
    // its panel keeps the name field.
    [Fact]
    public void A_Box_Has_No_Edit_Sketch_And_A_Portrait_Ring_Has_No_Rename()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, Landscape, Box, [Made[0]]);
        Step(ui, Landscape, Box, [Made[0]]);
        Assert.Equal([DesktopUi.MoveLabel, DesktopUi.HideLabel, DesktopUi.FocusLabel, DesktopUi.DimensionsLabel, DesktopUi.RenameLabel], ui.RingLabels);

        var portrait = new Vector2(390, 844);
        Step(ui, portrait, Extrude, Made);
        Step(ui, portrait, Extrude, Made);
        Assert.Equal([DesktopUi.EditSketchLabel, DesktopUi.MoveLabel, DesktopUi.SetDistanceLabel, DesktopUi.HideLabel, DesktopUi.FocusLabel, DesktopUi.DimensionsLabel], ui.RingLabels);
    }

    // Item 2: the segments of a landscape ring span at most four segments, so the top one ends before Accept of a proposal at
    // 305 degrees; six segments are narrower, and each narrowed one is at least 46 pixels at the inner radius. On a small ring with no room,
    // "Rename" and then "Edit sketch" leave the ring.
    [Theory]
    [InlineData(1440, 900, 7)]
    [InlineData(1280, 720, 7)]
    [InlineData(915, 412, 4)]
    [InlineData(844, 390, 4)]
    [InlineData(640, 360, 4)]
    public void A_Landscape_Ring_Spans_At_Most_Four_Segments(int width, int height, int count)
    {
        var display = new Vector2(width, height);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, display, Extrude, Made);
        Step(ui, display, Extrude, Made);
        var ring = ui.RingLayoutOfFrame ?? throw new InvalidOperationException("No ring.");
        Assert.Equal(count, ui.RingLabels.Count);
        var segments = ring.Segments(ui.RingLabels);
        Assert.True(segments[^1].To - segments[0].From <= DesktopUi.RingSpan + 0.001f, $"{width}x{height}: the ring spans {segments[^1].To - segments[0].From} radians.");
        // A segment is at least 44 pixels at the middle of the band; a narrowed segment is at least 46 pixels at the inner radius.
        Assert.True(ring.Segment * (ring.Inner + (DesktopUi.RingBand / 2f)) >= DesktopUi.ValueSize, $"{width}x{height}: a segment is {ring.Segment * (ring.Inner + (DesktopUi.RingBand / 2f))} pixels at the middle of the band.");
        Assert.True(ring.Segment >= DesktopUi.RingSegment - 0.0001f || ring.Segment * ring.Inner >= DesktopUi.ValueSize + 1.9f, $"{width}x{height}: a narrowed segment is {ring.Segment * ring.Inner} pixels at the inner radius.");
        Assert.True(segments[0].From >= ((305f / 180f) * MathF.PI) - (2f * MathF.PI) - 0.001f, $"{width}x{height}: the top segment starts at {segments[0].From}, before 305 degrees.");
    }

    // Item 1: the command line holds "Edit sketch" for a body that a sketch made, and its run opens that sketch.
    [Fact]
    public void Edit_Sketch_Is_A_Command_For_A_Body_That_A_Sketch_Made()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, Landscape, Box, [Made[0]]);
        Assert.DoesNotContain(DesktopUi.EditSketchLabel, ui.Commands([Box], Body).Select(command => command.Name));
        Step(ui, Landscape, Extrude, Made);
        var edit = Assert.Single(ui.Commands([Extrude], Body), command => command.Name == DesktopUi.EditSketchLabel);
        ui.TakeActions();
        edit.Run();
        Assert.Equal([new EditSketchAction(SketchStep)], ui.TakeActions());
    }

    // Item 3: each node of the history arc keeps a target of 44 pixels on the arc, and the drawn dot is smaller: the dot of the
    // selected body has a radius of 10 pixels, and each other dot 6 pixels.
    [Fact]
    public void Each_Node_Keeps_Its_Target_And_Draws_A_Smaller_Dot()
    {
        Assert.True(DesktopUi.HistoryDotSelected * 2f < DesktopUi.HistoryNode && DesktopUi.HistoryDot < DesktopUi.HistoryDotSelected, "The dots are not smaller than the target.");
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, Landscape, Extrude, Made);
        Step(ui, Landscape, Extrude, Made);
        foreach (var step in Made)
        {
            var node = ui.ItemRectangle(DesktopUi.HistoryItem(step.Id)) ?? throw new InvalidOperationException($"No node of {step.Text}.");
            Assert.Equal(DesktopUi.HistoryNode, node.Z - node.X, 0.5f);
            Assert.Equal(DesktopUi.HistoryNode, node.W - node.Y, 0.5f);
        }

        Assert.Equal([new EditSketchAction(SketchStep)], Tap(ui, Landscape, Extrude, Made, DesktopUi.HistoryItem(SketchStep)));
    }
}
