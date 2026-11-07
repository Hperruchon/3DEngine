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
            var layout = DesktopUi.SketchLayout(Display, centre, labels.Length, sketch, 476f, Display.X);

            Assert.True(layout.Inner >= sketch + 24f, $"The inner radius {layout.Inner} is inside the sketch of {sketch}.");
            Assert.True(layout.Centre.X - layout.Outer >= 476f + 16f - 0.01f, $"The band at {layout.Centre.X - layout.Outer} covers the panel.");
            foreach (var segment in layout.Segments(labels))
            {
                var middle = layout.Middle(segment);
                Assert.True(middle.X < layout.Centre.X, $"The segment {segment.Label} at {middle} is not on the left.");
            }
        }

        // V134 (TASK-0280): the centre stays at or left of the window of the sketch, and a large sketch keeps the band inside
        // the height of the display.
        foreach (var (centre, sketch) in new[] { (new Vector2(1000, 360), 60f), (new Vector2(700, 360), 380f), (new Vector2(700, 360), 450f) })
        {
            var layout = DesktopUi.SketchLayout(Display, centre, labels.Length, sketch, 476f, 784f);
            // The review of Codex of v0.265 (TASK-0284): only when the band then stays to the right of the panel.
            if (476f + layout.Outer + 16f <= 784f)
                Assert.True(layout.Centre.X <= 784f, $"The centre {layout.Centre} is under the window of the sketch.");
            else
                Assert.True(layout.Centre.X - layout.Outer >= 476f + 16f - 0.01f, $"The band of {layout.Centre} covers the panel.");
            Assert.True(layout.Centre.Y - layout.Outer >= 0f && layout.Centre.Y + layout.Outer <= Display.Y, $"The band of {layout.Centre}, {layout.Outer} is outside the display.");
        }
    }

    // ADR-0087 (the redesign, slice g, TASK-0276): with a line selected, "Constrain" comes before Finish; it opens the second
    // ring of the constraints that fit a line, with Back; a tap on Horizontal adds it, and a tap on Distance opens its pad, as
    // the buttons of the window of the sketch do. With no selection there is no "Constrain".
    [Fact]
    public void Constrain_Opens_The_Constraints_That_Fit_The_Selection()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var (start, end, line) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var sketch = new Engine.Core.Queries.SketchInfo(Guid.NewGuid(), Engine.Contracts.Sketch.Datums.XY.Id, "Plate",
            [new(start, 0, 0), new(end, 40, 10)], [new(line, start, end)], [], [], [], true, 4);
        var selection = new List<Guid>();
        var centre = new Vector2(700, 360);
        void Frame(params InputSample[] samples)
        {
            ui.Route(samples);
            ui.Frame(1f / 60f, Display, Vector2.One, [], null, null,
                sketchPanel: new SketchPanel([sketch], sketch, ThreeDEngine.Core.Sketching.SketchToolKind.Select, false, [.. selection], null), ringCentre: centre, ringBody: 60f);
        }

        void TapOn(string label)
        {
            var at = Centre(ui, label);
            Frame(new PointerSample(DeviceKind.Touch, 1, 3, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Down, 0));
            Frame(new PointerSample(DeviceKind.Touch, 1, 3, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Up, 0));

            // The release counts in the frame after it, and the ring shows its new labels in the frame after that.
            Frame();
            Frame();
        }

        Frame();
        Frame();
        Assert.Null(ui.ItemRectangle(DesktopUi.RingItem(DesktopUi.ConstrainLabel)));

        selection.Add(line);
        Frame();
        Frame();
        TapOn(DesktopUi.ConstrainLabel);
        foreach (var label in new[] { "Horizontal", "Vertical", "Distance", DesktopUi.BackLabel })
            Assert.NotNull(ui.ItemRectangle(DesktopUi.RingItem(label)));
        Assert.Null(ui.ItemRectangle(DesktopUi.RingItem(DesktopUi.LineToolLabel)));

        // ADR-0087 item 5: a label wider than the band starts to the right of the panel and ends nearer the panel than a
        // label centred on its segment, so it covers less of the sketch.
        var panelRight = ui.PanelRectangles[0].Z;
        foreach (var label in new[] { "Horizontal", "Vertical", "Distance" })
        {
            var text = ui.ItemRectangle(DesktopUi.RingTextItem(label)) ?? throw new InvalidOperationException($"No text of {label}.");
            var middle = ui.ItemRectangle(DesktopUi.RingItem(label)) ?? throw new InvalidOperationException($"No segment {label}.");
            Assert.True(text.X >= panelRight + 3.5f, $"{label} starts at {text.X}, on the panel that ends at {panelRight}.");
            Assert.True(text.Z < ((middle.X + middle.Z) / 2f) + ((text.Z - text.X) / 2f) - 1f, $"{label} at {text} is centred on its segment.");
        }

        TapOn("Horizontal");
        var added = Assert.IsType<AddConstraintAction>(Assert.Single(ui.TakeActions()));
        Assert.Equal("horizontal", added.Kind);
        Assert.Equal([line], added.Items);
        Assert.NotNull(ui.ItemRectangle(DesktopUi.RingItem(DesktopUi.LineToolLabel)));

        TapOn(DesktopUi.ConstrainLabel);
        TapOn("Distance");
        Assert.Equal("Distance", ui.Pad?.Title);
    }

    // ADR-0088 (the redesign, slice h, TASK-0277): while the ring of the sketch shows, the window of the sketch holds no
    // tool, no Finish and no kind of a constraint, and keeps Extrude, Delete and the rows. With no ring, it holds each of
    // them as before.
    [Fact]
    public void The_Window_Of_The_Sketch_Does_Not_Repeat_The_Ring()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var (start, end, line) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var sketch = new Engine.Core.Queries.SketchInfo(Guid.NewGuid(), Engine.Contracts.Sketch.Datums.XY.Id, "Plate",
            [new(start, 0, 0), new(end, 40, 10)], [new(line, start, end)], [], [], [], true, 4);
        var panel = new SketchPanel([sketch], sketch, ThreeDEngine.Core.Sketching.SketchToolKind.Select, false, [line], null);
        string[] ringLabels = [DesktopUi.LineToolLabel, DesktopUi.SelectToolLabel, DesktopUi.CircleToolLabel, DesktopUi.ArcToolLabel, DesktopUi.FinishLabel, "Horizontal", "Vertical", "Distance"];
        string[] kept = [DesktopUi.ExtrudeLabel, DesktopUi.DeleteLabel, "L1"];

        foreach (var ring in new[] { true, false })
        {
            for (var frame = 0; frame < 3; frame++)
            {
                ui.Route([]);
                ui.Frame(1f / 60f, Display, Vector2.One, [], null, null, sketchPanel: panel,
                    ringCentre: ring ? new Vector2(700, 360) : null, ringBody: 60f);
            }

            Assert.Equal(ring, ui.ItemRectangle(DesktopUi.RingItem(DesktopUi.LineToolLabel)) is not null);
            foreach (var label in ringLabels)
                Assert.True(ui.ItemRectangle(DesktopUi.SketchItem(label)) is null == ring, $"{label} in the window with the ring {ring}.");
            foreach (var label in kept)
                Assert.True(ui.ItemRectangle(DesktopUi.SketchItem(label)) is not null, $"No {label} in the window with the ring {ring}.");
        }
    }

    // Finding V134 of the codebase review of 2026-11-07 (c) (TASK-0280): each tool of the ring of a sketch has a place that a
    // tap reaches, a segment on the display and under no window and no anchor, or its button in the window of the sketch,
    // and a tap on that place of Finish ends the sketch. Until TASK-0280 a sketch at the right put Line and Finish under the
    // window, a large sketch put Finish below the display, and the window then held neither.
    [Fact]
    public void Each_Tool_Of_The_Ring_Of_A_Sketch_Has_A_Place_That_A_Tap_Reaches()
    {
        var cases = new[]
        {
            (Display: new Vector2(1280, 720), Centre: new Vector2(1000, 360), Sketch: 60f),
            (Display: new Vector2(1280, 720), Centre: new Vector2(700, 360), Sketch: 380f),
            (Display: new Vector2(1280, 720), Centre: new Vector2(700, 360), Sketch: 450f),
            (Display: new Vector2(1280, 720), Centre: new Vector2(1180, 650), Sketch: 90f),
            (Display: new Vector2(390, 844), Centre: new Vector2(195, 400), Sketch: 60f),
        };
        foreach (var (display, centre, size) in cases)
        {
            using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
            var (start, end, line) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            var sketch = new Engine.Core.Queries.SketchInfo(Guid.NewGuid(), Engine.Contracts.Sketch.Datums.XY.Id, "Plate",
                [new(start, 0, 0), new(end, 40, 10)], [new(line, start, end)], [], [], [], true, 4);
            var panel = new SketchPanel([sketch], sketch, ThreeDEngine.Core.Sketching.SketchToolKind.Select, false, [], null);
            void Frame(params InputSample[] samples)
            {
                ui.Route(samples);
                ui.Frame(1f / 60f, display, Vector2.One, [], null, null, sketchPanel: panel, ringCentre: centre, ringBody: size);
            }

            for (var frame = 0; frame < 3; frame++)
                Frame();

            var covers = ui.PanelRectangles.Concat(ui.FixedRectangles).ToList();
            bool Free(Vector2 at) => at.X >= 0 && at.Y >= 0 && at.X < display.X && at.Y < display.Y
                && !covers.Any(cover => at.X >= cover.X && at.X < cover.Z && at.Y >= cover.Y && at.Y < cover.W);
            Vector2? Place(string label)
            {
                if (ui.ItemRectangle(DesktopUi.RingItem(label)) is { } segment && new Vector2((segment.X + segment.Z) / 2, (segment.Y + segment.W) / 2) is var middle && Free(middle))
                    return middle;
                return ui.ItemRectangle(DesktopUi.SketchItem(label)) is { } button ? new Vector2((button.X + button.Z) / 2, (button.Y + button.W) / 2) : null;
            }

            foreach (var (label, _) in DesktopUi.SketchRingTools)
                Assert.True(Place(label) is not null, $"{label} has no place for a tap at {display} with the sketch at {centre}, {size}.");

            var finish = Place(DesktopUi.FinishLabel) ?? throw new InvalidOperationException("No Finish.");
            Frame(new PointerSample(DeviceKind.Touch, 1, 3, finish.X, finish.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Down, 0));
            Frame(new PointerSample(DeviceKind.Touch, 1, 3, finish.X, finish.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Up, 0));
            Frame();
            Assert.True(ui.TakeActions().Contains(new FinishSketchAction()), $"A tap on Finish at {finish} does not end the sketch at {display} with the sketch at {centre}, {size}.");
        }
    }

    // Finding V135 of the codebase review of 2026-11-07 (c) (TASK-0281): a tap on a segment of the ring that lies over a window
    // is the tap of the window only. While an item of the window is active, the window counted as not hovered, so until
    // TASK-0281 the ring took the release too, and one tap gave two actions.
    [Fact]
    public void A_Tap_On_A_Segment_Over_A_Window_Is_The_Tap_Of_The_Window_Only()
    {
        var display = new Vector2(390, 844);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var points = Enumerable.Range(0, 12).Select(i => new Engine.Contracts.Sketch.SketchPoint(Guid.NewGuid(), i, i)).ToList();
        var sketch = new Engine.Core.Queries.SketchInfo(Guid.NewGuid(), Engine.Contracts.Sketch.Datums.XY.Id, "Plate", points, [], [], [], [], true, 24);
        var panel = new SketchPanel([sketch], sketch, ThreeDEngine.Core.Sketching.SketchToolKind.Select, false, [], null);
        void Frame(params InputSample[] samples)
        {
            ui.Route(samples);
            ui.Frame(1f / 60f, display, Vector2.One, [], null, null, sketchPanel: panel, ringCentre: new Vector2(195, 400), ringBody: 60f);
        }

        for (var frame = 0; frame < 3; frame++)
            Frame();

        var window = ui.PanelRectangles.Last();
        var covered = DesktopUi.SketchRingTools
            .Select(tool => (tool.Label, Rectangle: ui.ItemRectangle(DesktopUi.RingItem(tool.Label))))
            .Where(item => item.Rectangle is { } r && (r.X + r.Z) / 2 >= window.X && (r.X + r.Z) / 2 < window.Z && (r.Y + r.W) / 2 >= window.Y && (r.Y + r.W) / 2 < window.W)
            .ToList();
        Assert.NotEmpty(covered);
        foreach (var (label, rectangle) in covered)
        {
            var at = new Vector2((rectangle!.Value.X + rectangle.Value.Z) / 2, (rectangle.Value.Y + rectangle.Value.W) / 2);
            Frame(new PointerSample(DeviceKind.Touch, 1, 3, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Down, 0));
            Frame(new PointerSample(DeviceKind.Touch, 1, 3, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Up, 0));
            Frame();
            var actions = ui.TakeActions();
            Assert.False(actions.Any(action => action is SketchToolAction or FinishSketchAction), $"A tap on {label} at {at}, over the window, gave {string.Join(", ", actions)}.");
        }
    }

    // Finding V136 of the codebase review of 2026-11-07 (c) (TASK-0281): on a portrait display of 390 by 844, no segment of the
    // ring of a sketch or of a body is under an anchor. Until TASK-0281 Line, and Move of a body, were under "Frame", and a tap
    // on them gave Frame.
    [Fact]
    public void No_Portrait_Segment_Is_Under_An_Anchor()
    {
        var display = new Vector2(390, 844);
        var (start, end, line) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var sketch = new Engine.Core.Queries.SketchInfo(Guid.NewGuid(), Engine.Contracts.Sketch.Datums.XY.Id, "Plate",
            [new(start, 0, 0), new(end, 40, 10)], [new(line, start, end)], [], [], [], true, 4);
        var panel = new SketchPanel([sketch], sketch, ThreeDEngine.Core.Sketching.SketchToolKind.Select, false, [], null);
        foreach (var (selection, sketchPanel, labels) in new[]
        {
            ((Guid?)null, panel, DesktopUi.SketchRingTools.Select(tool => tool.Label).ToArray()),
            ((Guid?)Plate, (SketchPanel?)null, new[] { DesktopUi.MoveLabel, DesktopUi.SetDistanceLabel, DesktopUi.HideLabel }),
        })
        {
            using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
            for (var frame = 0; frame < 3; frame++)
            {
                ui.Route([]);
                ui.Frame(1f / 60f, display, Vector2.One, [Extrude], selection, null, sketchPanel: sketchPanel, ringCentre: new Vector2(195, 400), ringBody: 60f);
            }

            Assert.NotEmpty(ui.FixedRectangles);
            foreach (var label in labels)
            {
                var segment = ui.ItemRectangle(DesktopUi.RingItem(label)) ?? throw new InvalidOperationException($"No segment {label}.");
                var middle = new Vector2((segment.X + segment.Z) / 2, (segment.Y + segment.W) / 2);
                Assert.False(ui.FixedRectangles.Any(anchor => middle.X >= anchor.X && middle.X < anchor.Z && middle.Y >= anchor.Y && middle.Y < anchor.W),
                    $"The segment {label} at {middle} is under an anchor.");
            }
        }
    }

    // Finding V137 of the codebase review of 2026-11-07 (c) (TASK-0282): ADR-0087 item 3, a change of the selection closes the
    // second ring, so the ring holds the tools again. Until TASK-0282 no test held it: a condition that never matched passed
    // each test of the ring.
    [Fact]
    public void A_Change_Of_The_Selection_Closes_The_Second_Ring()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var (start, end, line) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var sketch = new Engine.Core.Queries.SketchInfo(Guid.NewGuid(), Engine.Contracts.Sketch.Datums.XY.Id, "Plate",
            [new(start, 0, 0), new(end, 40, 10)], [new(line, start, end)], [], [], [], true, 4);
        var selection = new List<Guid> { line };
        void Frame(params InputSample[] samples)
        {
            ui.Route(samples);
            ui.Frame(1f / 60f, Display, Vector2.One, [], null, null,
                sketchPanel: new SketchPanel([sketch], sketch, ThreeDEngine.Core.Sketching.SketchToolKind.Select, false, [.. selection], null), ringCentre: new Vector2(700, 360), ringBody: 60f);
        }

        for (var frame = 0; frame < 3; frame++)
            Frame();
        var at = Centre(ui, DesktopUi.ConstrainLabel);
        Frame(new PointerSample(DeviceKind.Touch, 1, 3, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Down, 0));
        Frame(new PointerSample(DeviceKind.Touch, 1, 3, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Up, 0));
        Frame();
        Frame();
        Assert.NotNull(ui.ItemRectangle(DesktopUi.RingItem("Horizontal")));

        // The person selects a point in place of the line: a point fits "Fixed", so "Constrain" stays, and the tools come back.
        selection.Clear();
        selection.Add(end);
        Frame();
        Frame();
        Assert.Null(ui.ItemRectangle(DesktopUi.RingItem("Horizontal")));
        Assert.NotNull(ui.ItemRectangle(DesktopUi.RingItem(DesktopUi.LineToolLabel)));
        Assert.NotNull(ui.ItemRectangle(DesktopUi.RingItem(DesktopUi.ConstrainLabel)));
    }

    // The review of Codex of v0.265 (TASK-0284): a window of the sketch that the person moved to the left, or collapsed there,
    // does not pull the band under the panel. Until TASK-0284 a window at x 500 put each segment under the panel.
    [Fact]
    public void A_Window_Of_The_Sketch_At_The_Left_Does_Not_Pull_The_Ring_Under_The_Panel()
    {
        string[] labels = [.. DesktopUi.SketchRingTools.Select(tool => tool.Label)];
        var layout = DesktopUi.SketchLayout(Display, new Vector2(700, 360), labels.Length, 60f, 476f, 500f);
        Assert.True(layout.Centre.X - layout.Outer >= 476f + 16f - 0.01f, $"The band of {layout.Centre}, {layout.Outer} covers the panel.");
    }

    // The review of Codex of v0.265 (TASK-0284): the Images window comes after the ring in the frame. While it is open over a
    // segment, the window of the sketch keeps the button of that segment. Until TASK-0284 the check did not see the Images
    // window, and Line had no place for a tap.
    [Fact]
    public void A_Segment_Under_The_Images_Window_Keeps_Its_Button_In_The_Window_Of_The_Sketch()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var (start, end, line) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var sketch = new Engine.Core.Queries.SketchInfo(Guid.NewGuid(), Engine.Contracts.Sketch.Datums.XY.Id, "Plate",
            [new(start, 0, 0), new(end, 40, 10)], [new(line, start, end)], [], [], [], true, 4);
        var panel = new SketchPanel([sketch], sketch, ThreeDEngine.Core.Sketching.SketchToolKind.Select, false, [], null);
        var images = new ImagePanel([], null, 0, null);
        void Frame(params InputSample[] samples)
        {
            ui.Route(samples);
            ui.Frame(1f / 60f, Display, Vector2.One, [], null, null, sketchPanel: panel, imagePanel: images, ringCentre: new Vector2(700, 360), ringBody: 60f);
        }

        for (var frame = 0; frame < 3; frame++)
            Frame();

        // The Images window starts folded at (492, 16); a tap on the arrow of its title bar opens it.
        var folded = ui.PanelRectangles.Single(window => Math.Abs(window.X - 492f) < 1f);
        var arrow = new Vector2(folded.X + 24f, (folded.Y + folded.W) / 2f);
        Frame(new PointerSample(DeviceKind.Mouse, 1, 0, arrow.X, arrow.Y, 1, PointerButtons.Left, KeyModifiers.None, PointerPhase.Down, 0));
        Frame(new PointerSample(DeviceKind.Mouse, 1, 0, arrow.X, arrow.Y, 1, PointerButtons.Left, KeyModifiers.None, PointerPhase.Up, 0));
        Frame(new PointerSample(DeviceKind.Mouse, 1, 0, 1200, 700, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Move, 0));
        Frame();
        Frame();

        var open = ui.PanelRectangles.Single(window => Math.Abs(window.X - 492f) < 1f);
        Assert.True(open.W - open.Y > 100f, $"The Images window at {open} did not open.");
        var segment = ui.ItemRectangle(DesktopUi.RingItem(DesktopUi.LineToolLabel)) ?? throw new InvalidOperationException("No segment Line.");
        var middle = new Vector2((segment.X + segment.Z) / 2, (segment.Y + segment.W) / 2);
        Assert.True(middle.X >= open.X && middle.X < open.Z && middle.Y >= open.Y && middle.Y < open.W, $"Line at {middle} is not under the Images window at {open}.");
        Assert.NotNull(ui.ItemRectangle(DesktopUi.SketchItem(DesktopUi.LineToolLabel)));
    }
}
