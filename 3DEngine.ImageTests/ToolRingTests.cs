using System.Numerics;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0081 (the redesign, slice b2, TASK-0253): the tool ring of the selected body, with no GPU. ADR-0081 names this file
// in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class ToolRingTests
{
    private static readonly Vector2 Display = new(1280, 720);
    private static readonly Guid Plate = Guid.Parse("61000000-0000-0000-0000-000000000000");
    private static readonly LiveBody Box = new(Plate, "Box", "Plate", true);
    private static readonly LiveBody Extrude = new(Plate, "Extrude", "Plate", true);

    private static void Step(DesktopUi ui, LiveBody body, Vector2 display, Vector2? centre, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, display, Vector2.One, [body], body.Id, null, ringCentre: centre);
    }

    private static Vector2 Centre(DesktopUi ui, string label)
    {
        var item = ui.ItemRectangle(DesktopUi.RingItem(label)) ?? throw new InvalidOperationException($"The ring has no {label}.");
        return new Vector2((item.X + item.Z) / 2, (item.Y + item.W) / 2);
    }

    private static void Tap(DesktopUi ui, LiveBody body, Vector2 display, Vector2 centre, Vector2 at, DeviceKind kind)
    {
        var (pointer, buttons) = kind == DeviceKind.Mouse ? (0UL, PointerButtons.Left) : (3UL, PointerButtons.None);
        Step(ui, body, display, centre, new PointerSample(kind, 1, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, PointerPhase.Down, 0));
        Step(ui, body, display, centre, new PointerSample(kind, 1, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, PointerPhase.Up, 0));
        Step(ui, body, display, centre);
    }

    // Items 1 and 2: on a landscape display the ring is around the body, with a third of the height as its inner radius;
    // each segment is deeper than 44 pixels and wider than 54 at its middle; the segments stay inside the display.
    [Fact]
    public void The_Ring_Of_A_Landscape_Display_Is_Around_The_Body_And_Each_Segment_Is_Large_Enough()
    {
        var layout = DesktopUi.Layout(Display, new Vector2(800, 360), 4);

        Assert.Equal((new Vector2(800, 360), 240f), (layout.Centre, layout.Inner));
        Assert.True(DesktopUi.RingBand >= DesktopUi.MinimumControlHeight);
        var middle = (layout.Inner + layout.Outer) / 2;
        Assert.True(2 * middle * MathF.Sin(DesktopUi.RingSegment / 2) >= 54, "A segment is narrower than 54 pixels at its middle.");
        foreach (var segment in layout.Segments(["a", "b", "c", "d"]))
        {
            foreach (var angle in new[] { segment.From, segment.To })
            {
                var edge = layout.Centre + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * layout.Outer);
                Assert.True(edge.X >= 0 && edge.X <= Display.X && edge.Y >= 0 && edge.Y <= Display.Y, $"The segment {segment.Label} leaves the display at {edge}.");
            }
        }

        // A body near the edge moves the ring inward, so its segments stay inside.
        Assert.True(DesktopUi.Layout(Display, new Vector2(1270, 10), 4).Centre.X <= Display.X - layout.Outer);
    }

    // Item 1: on a portrait display, as the phone, the ring is under the thumb in the lower right corner.
    [Fact]
    public void The_Ring_Of_A_Portrait_Display_Is_In_The_Lower_Right_Corner()
    {
        var portrait = new Vector2(390, 844);
        var layout = DesktopUi.Layout(portrait, new Vector2(195, 300), 3);

        Assert.Equal(new Vector2(portrait.X - 16, portrait.Y - 16), layout.Centre);
        foreach (var segment in layout.Segments(["a", "b", "c"]))
        {
            var at = layout.Middle(segment);
            Assert.True(at.X < layout.Centre.X && at.Y < layout.Centre.Y + 1 && at.X > 0 && at.Y > 0, $"The segment {segment.Label} at {at} is not up and left of the corner.");
        }
    }

    // Items 2 and 3: a tap and a click on a segment give its tool; the segment is a part of the panels for a tap, and the
    // inside of the ring is the scene. An extrude has Distance, and a box has none.
    [Theory]
    [InlineData(DeviceKind.Touch)]
    [InlineData(DeviceKind.Mouse)]
    public void A_Tap_On_A_Segment_Gives_Its_Tool(DeviceKind kind)
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var centre = new Vector2(800, 360);
        Step(ui, Extrude, Display, centre);
        Step(ui, Extrude, Display, centre);

        var hide = Centre(ui, DesktopUi.HideLabel);
        Assert.True(ui.OverPanel(hide.X, hide.Y), "A tap on Hide reaches the scene.");
        Assert.False(ui.OverPanel(centre.X, centre.Y), "The inside of the ring takes a tap.");
        Assert.NotNull(ui.ItemRectangle(DesktopUi.RingItem(DesktopUi.SetDistanceLabel)));

        Tap(ui, Extrude, Display, centre, hide, kind);
        Tap(ui, Extrude, Display, centre, Centre(ui, DesktopUi.SetDistanceLabel), kind);
        Assert.Equal([new SetVisibleAction(Plate, false), new EditDistanceAction(Plate)], ui.TakeActions());

        Tap(ui, Extrude, Display, centre, Centre(ui, DesktopUi.MoveLabel), kind);
        Assert.Equal(DesktopUi.MoveLabel, ui.Pad?.Title);

        using var other = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        Step(other, Box, Display, centre);
        Assert.Null(other.ItemRectangle(DesktopUi.RingItem(DesktopUi.SetDistanceLabel)));
    }

    // Item 1: with no centre, or while a pad is open, there is no ring.
    [Fact]
    public void No_Ring_Shows_With_No_Centre_Or_With_A_Pad()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        Step(ui, Box, Display, null);
        Assert.Null(ui.ItemRectangle(DesktopUi.RingItem(DesktopUi.MoveLabel)));

        var centre = new Vector2(800, 360);
        Step(ui, Box, Display, centre);
        Tap(ui, Box, Display, centre, Centre(ui, DesktopUi.MoveLabel), DeviceKind.Mouse);
        Assert.NotNull(ui.Pad);
        Step(ui, Box, Display, centre);
        Assert.Null(ui.ItemRectangle(DesktopUi.RingItem(DesktopUi.MoveLabel)));
    }
}
