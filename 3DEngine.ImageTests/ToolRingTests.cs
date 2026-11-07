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

    // Finding V121 of the codebase review of 2026-11-07 (TASK-0264), item 1: on a portrait display the segments go from the
    // left up to the top, for each count. Until TASK-0264 the fourth segment went past the top and out of the display.
    [Theory]
    [InlineData(1080, 2316)]
    [InlineData(390, 844)]
    public void Each_Segment_Of_A_Portrait_Ring_Is_Between_The_Left_And_The_Top(int width, int height)
    {
        var portrait = new Vector2(width, height);
        // ADR-0086 item 3 (TASK-0275): five segments too, for the tools of a sketch.
        for (var count = 1; count <= 5; count++)
        {
            var layout = DesktopUi.Layout(portrait, new Vector2(width / 2f, 300), count);
            foreach (var segment in layout.Segments([.. Enumerable.Range(0, count).Select(i => $"s{i}")]))
            {
                Assert.True(segment.From >= MathF.PI - 1e-4f && segment.To <= (1.5f * MathF.PI) + 1e-4f,
                    $"{count} segments at {portrait}: {segment.Label} goes from {segment.From} to {segment.To}.");
                foreach (var angle in new[] { segment.From, segment.To })
                {
                    var outer = layout.Centre + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * layout.Outer);
                    Assert.True(outer.X <= portrait.X && outer.Y >= 0, $"{count} segments at {portrait}: {segment.Label} reaches {outer}.");
                }

                Assert.True((segment.To - segment.From) * layout.Inner >= DesktopUi.ValueSize, $"{segment.Label} is narrower than 44 pixels.");
            }
        }
    }

    // Finding V122 of the codebase review of 2026-11-07 (TASK-0264): the portrait ring is 16 pixels inside the safe area,
    // so the navigation bar of the phone does not cover Move. Until TASK-0264 it read no inset.
    [Fact]
    public void The_Portrait_Ring_Is_Inside_The_Safe_Area()
    {
        var portrait = new Vector2(390, 844);
        var inset = new Vector4(0f, 40f, 12f, 48f);

        var layout = DesktopUi.Layout(portrait, new Vector2(195, 300), 3, 0f, inset);

        Assert.Equal(new Vector2(portrait.X - 16 - inset.Z, portrait.Y - 16 - inset.W), layout.Centre);
    }

    // Finding V120 of the codebase review of 2026-11-07 (TASK-0264): a chip that lies on a segment gives the tool of the chip
    // only, because the chip is drawn over the ring. Until TASK-0264 one tap on a chip over Hide also hid the body.
    [Fact]
    public void A_Tap_On_A_Chip_Over_A_Segment_Gives_The_Chip_Only()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var centre = new Vector2(640, 360);
        var layout = DesktopUi.Layout(Display, centre, 3);
        var hide = layout.Segments([DesktopUi.MoveLabel, DesktopUi.SetDistanceLabel, DesktopUi.HideLabel])[2];
        var at = layout.Middle(hide);
        var (sketch, constraint) = (Guid.NewGuid(), Guid.NewGuid());
        IReadOnlyList<EdgeValues.Value> values = [new EdgeValues.Value(EdgeValues.ConstraintKey(constraint), "6 mm", at, 6, Plate, sketch, constraint)];
        void Frame(params InputSample[] samples)
        {
            ui.Route(samples);
            ui.Frame(1f / 60f, Display, Vector2.One, [Extrude], Plate, null, ringCentre: centre, values: values);
        }

        Frame();
        Frame();
        var chip = ui.ItemRectangle(values[0].Key) ?? throw new InvalidOperationException("No chip.");
        Assert.True(at.X >= chip.X && at.X < chip.Z && at.Y >= chip.Y && at.Y < chip.W, $"The chip {chip} is not on Hide at {at}.");
        Assert.Equal(DesktopUi.HideLabel, layout.At([DesktopUi.MoveLabel, DesktopUi.SetDistanceLabel, DesktopUi.HideLabel], at));
        Frame(new PointerSample(DeviceKind.Touch, 1, 3, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Down, 0));
        Frame(new PointerSample(DeviceKind.Touch, 1, 3, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Up, 0));
        Frame();

        Assert.DoesNotContain(ui.TakeActions(), action => action is SetVisibleAction);
        Assert.Equal(DesktopUi.EdgeValueLabel, ui.Pad?.Title);
    }

    // Finding V130 of the codebase review of 2026-11-07 (b) (TASK-0271): a mesh body takes no move (ADR-0083 item 4), so its
    // ring has no Move; until TASK-0271 Move gave a refusal only.
    [Fact]
    public void The_Ring_Of_A_Mesh_Body_Has_No_Move()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var mesh = new LiveBody(Plate, Engine.Core.Commands.ImportStlCommandHandler.MeshKind, "bracket.stl", true);
        var centre = new Vector2(800, 360);
        Step(ui, mesh, Display, centre);
        Step(ui, mesh, Display, centre);

        Assert.Null(ui.ItemRectangle(DesktopUi.RingItem(DesktopUi.MoveLabel)));
        Assert.NotNull(ui.ItemRectangle(DesktopUi.RingItem(DesktopUi.HideLabel)));
    }

    // The review of Codex of v0.251 (TASK-0271): on a portrait display of 390 by 844 the label of each of four segments is
    // inside the display; until TASK-0271 "Sketch on face" went past the right edge.
    [Fact]
    public void Each_Label_Of_A_Portrait_Ring_Is_Inside_The_Display()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var portrait = new Vector2(390, 844);
        var face = new FaceLine("Face +z: 1 square mm", true, []);
        for (var frame = 0; frame < 2; frame++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, portrait, Vector2.One, [Extrude], Extrude.Id, null, face: face, ringCentre: new Vector2(195, 300));
        }

        foreach (var label in new[] { DesktopUi.MoveLabel, DesktopUi.SetDistanceLabel, DesktopUi.HideLabel, DesktopUi.SketchOnFaceButtonLabel })
        {
            var text = ui.ItemRectangle(DesktopUi.RingTextItem(label)) ?? throw new InvalidOperationException($"No label {label}.");
            Assert.True(text.X >= 0 && text.Z <= portrait.X, $"The label {label} at {text} is outside the display.");
        }
    }

    // ADR-0086 items 1 and 2 (the redesign, slice f, TASK-0275): while a sketch is active, the ring holds its tools in the place
    // of the tools of the body; a tap on a tool gives it, and a tap on Finish ends the sketch. Until TASK-0275 the ring hid.
    [Theory]
    [InlineData(DeviceKind.Touch)]
    [InlineData(DeviceKind.Mouse)]
    public void While_A_Sketch_Is_Active_The_Ring_Holds_Its_Tools(DeviceKind kind)
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var sketch = new Engine.Core.Queries.SketchInfo(Guid.NewGuid(), Engine.Contracts.Sketch.Datums.XY.Id, "Plate", [], [], [], [], [], true, 0);
        var panel = new SketchPanel([sketch], sketch, ThreeDEngine.Core.Sketching.SketchToolKind.Line, false, [], null);
        var centre = new Vector2(800, 360);
        void Frame(params InputSample[] samples)
        {
            ui.Route(samples);
            ui.Frame(1f / 60f, Display, Vector2.One, [Extrude], Extrude.Id, null, sketchPanel: panel, ringCentre: centre);
        }

        Frame();
        Frame();
        foreach (var (label, _) in DesktopUi.SketchRingTools)
            Assert.NotNull(ui.ItemRectangle(DesktopUi.RingItem(label)));
        Assert.Null(ui.ItemRectangle(DesktopUi.RingItem(DesktopUi.MoveLabel)));

        var (pointer, buttons) = kind == DeviceKind.Mouse ? (0UL, PointerButtons.Left) : (3UL, PointerButtons.None);
        foreach (var label in new[] { DesktopUi.CircleToolLabel, DesktopUi.FinishLabel })
        {
            var at = Centre(ui, label);
            Frame(new PointerSample(kind, 1, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, PointerPhase.Down, 0));
            Frame(new PointerSample(kind, 1, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, PointerPhase.Up, 0));
            Frame();
        }

        Assert.Equal([new SketchToolAction(ThreeDEngine.Core.Sketching.SketchToolKind.Circle), new FinishSketchAction()], ui.TakeActions());
    }

    // ADR-0086 item 1: on a landscape display the ring of a sketch is on the left of the sketch, its inner radius is 24 pixels
    // more than the half of the diagonal of the sketch, and its band stays 16 pixels to the right of the panel. The first form
    // of TASK-0275 put the ring onto the points of the sketch.
    [Fact]
    public void The_Ring_Of_A_Sketch_Is_On_Its_Left_Between_The_Panel_And_The_Sketch()
    {
        string[] labels = [.. DesktopUi.SketchRingTools.Select(tool => tool.Label)];
        foreach (var (centre, sketch) in new[] { (new Vector2(700, 360), 90f), (new Vector2(500, 300), 60f), (new Vector2(900, 200), 150f) })
        {
            var layout = DesktopUi.SketchLayout(Display, centre, labels.Length, sketch, 476f);

            Assert.True(layout.Inner >= sketch + 24f, $"The inner radius {layout.Inner} is inside the sketch of {sketch}.");
            Assert.True(layout.Centre.X - layout.Outer >= 476f + 16f - 0.01f, $"The band at {layout.Centre.X - layout.Outer} covers the panel.");
            foreach (var segment in layout.Segments(labels))
            {
                var middle = layout.Middle(segment);
                Assert.True(middle.X < layout.Centre.X, $"The segment {segment.Label} at {middle} is not on the left.");
            }
        }
    }
}
