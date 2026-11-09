using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Numeric;
using ThreeDEngine.Core.Sketching;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;

namespace ThreeDEngine.ImageTests;

// ADR-0105 (the redesign, phase R16s, TASK-0327): the values of the active sketch on its lines and its curves, with no GPU.
// ADR-0105 names this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class SketchValueTests
{
    private static readonly Vector2 Display = new(1440, 900);

    // The camera looks along the negative z axis at the plane XY, so x goes to the right and y goes up on the screen.
    private static readonly Camera Facing = new() { YawDegrees = 0, PitchDegrees = 0, Distance = 200, Target = (0, 0, 0) };

    private static readonly Guid Sketch = Guid.Parse("75000000-0000-0000-0000-000000000000");
    private static readonly Guid P1 = Guid.Parse("75000000-0000-0000-0000-000000000001");
    private static readonly Guid P2 = Guid.Parse("75000000-0000-0000-0000-000000000002");
    private static readonly Guid P3 = Guid.Parse("75000000-0000-0000-0000-000000000003");
    private static readonly Guid P4 = Guid.Parse("75000000-0000-0000-0000-000000000004");
    private static readonly Guid P5 = Guid.Parse("75000000-0000-0000-0000-000000000005");
    private static readonly Guid L1 = Guid.Parse("75000000-0000-0000-0000-000000000011");
    private static readonly Guid L2 = Guid.Parse("75000000-0000-0000-0000-000000000012");
    private static readonly Guid L3 = Guid.Parse("75000000-0000-0000-0000-000000000013");
    private static readonly Guid L4 = Guid.Parse("75000000-0000-0000-0000-000000000014");
    private static readonly Guid R1 = Guid.Parse("75000000-0000-0000-0000-000000000021");
    private static readonly Guid C1 = Guid.Parse("75000000-0000-0000-0000-000000000031");
    private static readonly Guid C2 = Guid.Parse("75000000-0000-0000-0000-000000000032");
    private static readonly Guid C3 = Guid.Parse("75000000-0000-0000-0000-000000000033");
    private static readonly Guid C4 = Guid.Parse("75000000-0000-0000-0000-000000000034");

    // A rectangle of 60 by 40 about the origin on XY, with a circle of radius 8 at (10, 5): C1 is the length 60 of the line
    // L1 at the bottom, C2 the distance 40 of the two ends of L2 at the right, C3 a horizontal with no value, and C4 the radius.
    private static readonly SketchInfo Plate = new(
        Sketch, Datums.XY.Id, "Plate",
        [new(Sketch, 0, 0), new(P1, -30, -20), new(P2, 30, -20), new(P3, 30, 20), new(P4, -30, 20), new(P5, 10, 5)],
        [new(L1, P1, P2), new(L2, P2, P3), new(L3, P3, P4), new(L4, P4, P1)],
        [new(R1, P5, 8)],
        [],
        [
            new SketchConstraintInfo(C1, "distance", [L1], [60], 0),
            new SketchConstraintInfo(C2, "distance", [P2, P3], [40], 0),
            new SketchConstraintInfo(C3, "horizontal", [L3], [], 0),
            new SketchConstraintInfo(C4, "radius", [R1], [8], 0),
        ],
        true, 0);

    private static IReadOnlyList<EdgeValues.Value> ValuesOf(SketchInfo sketch, Camera? camera = null)
        => SketchValues.Of(sketch, camera ?? Facing, Display.X, Display.Y, "mm");

    private static Vector2 PixelOf(SketchInfo sketch, double x, double y)
        => SketchOverlay.Pixel(sketch, Facing, Display.X, Display.Y, (x, y)) ?? throw new InvalidOperationException($"No pixel of ({x}, {y}).");

    private static SketchPanel Panel(params Guid[] selection) => new([Plate], Plate, SketchToolKind.Select, false, selection, null);

    private static UiDrawData Step(DesktopUi ui, SketchPanel panel, IReadOnlyList<EdgeValues.Value> values, IReadOnlyList<EdgeValues.Value>? edgeValues = null, Guid? selection = null, params InputSample[] samples)
    {
        ui.Route(samples);
        return ui.Frame(1f / 60f, Display, Vector2.One, [], selection, null, sketchPanel: panel, values: edgeValues, sketchValues: values);
    }

    private static DesktopUi Opened(SketchPanel panel, IReadOnlyList<EdgeValues.Value> values)
    {
        var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, panel, values);
        Step(ui, panel, values);
        return ui;
    }

    private static Vector2 Middle(Vector4 rectangle) => new((rectangle.X + rectangle.Z) / 2, (rectangle.Y + rectangle.W) / 2);

    private static Vector4 Item(DesktopUi ui, string item) => ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item}.");

    // A press and a release at the middle of an item; it gives the samples that reach the scene.
    private static List<InputSample> Tap(DesktopUi ui, SketchPanel panel, IReadOnlyList<EdgeValues.Value> values, string item, DeviceKind kind)
    {
        var at = Middle(Item(ui, item));
        var (pointer, buttons) = kind == DeviceKind.Mouse ? (0UL, PointerButtons.Left) : (3UL, PointerButtons.None);
        var scene = new List<InputSample>();
        foreach (var phase in new[] { PointerPhase.Down, PointerPhase.Up })
        {
            scene.AddRange(ui.Route([new PointerSample(kind, 1, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, phase, 0)]));
            ui.Frame(1f / 60f, Display, Vector2.One, [], null, null, sketchPanel: panel, sketchValues: values);
        }
        Step(ui, panel, values);
        Step(ui, panel, values);
        return scene;
    }

    // Items 1 and 3: the length of a line, the distance of two points and the radius of a circle each give a value, with the
    // label of the constraint, two spaces, the value and the unit; a constraint with no length gives none.
    [Fact]
    public void A_Rectangle_And_A_Circle_Give_Their_Lengths_And_Their_Radius()
    {
        var values = ValuesOf(Plate);

        Assert.Equal(["C1  60 mm", "C2  40 mm", "C4  R 8 mm"], values.Select(value => value.Text));
        Assert.Equal([C1, C2, C4], values.Select(value => value.Constraint!.Value));
        Assert.Equal([60.0, 40.0, 8.0], values.Select(value => value.Number));
        Assert.All(values, value => Assert.Equal(Sketch, value.Sketch));
        Assert.Equal([SketchValues.Key(C1), SketchValues.Key(C2), SketchValues.Key(C4)], values.Select(value => value.Key));
        Assert.DoesNotContain(values, value => value.Key == EdgeValues.ConstraintKey(value.Constraint!.Value));
        Assert.Equal(["C1  60 in", "C2  40 in", "C4  R 8 in"], SketchValues.Of(Plate, Facing, Display.X, Display.Y, "in").Select(value => value.Text));
    }

    // Item 1: a constraint of a reference item has no value.
    [Fact]
    public void A_Constraint_Of_A_Reference_Item_Has_No_Value()
    {
        var referenced = Plate with { References = [new SketchReference(Guid.NewGuid(), new DatumReference(Guid.NewGuid(), "side", null), [P1, P2], L1, null)] };

        Assert.Equal([C4], ValuesOf(referenced).Select(value => value.Constraint!.Value));
    }

    // Item 1: a value whose place is behind the eye has no value. The eye is at (0, 0, 5) and looks along the x axis, so the
    // line at the right is in front, and the line at the left, the circle and the middle of the two points at x 0 are behind.
    [Fact]
    public void A_Value_Behind_The_Eye_Has_No_Value()
    {
        var (a, b, c, d, e, ring) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var (front, back) = (Guid.NewGuid(), Guid.NewGuid());
        var (inFront, behind, curve, across) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var sketch = new SketchInfo(Sketch, Datums.XY.Id, null,
            [new(Sketch, 0, 0), new(a, 10, -5), new(b, 20, -5), new(c, -20, -5), new(d, -10, -5), new(e, -40, 0)],
            [new(front, a, b), new(back, c, d)],
            [new(ring, e, 5)],
            [],
            [
                new SketchConstraintInfo(inFront, "distance", [front], [10], 0),
                new SketchConstraintInfo(behind, "distance", [back], [10], 0),
                new SketchConstraintInfo(curve, "radius", [ring], [5], 0),
                new SketchConstraintInfo(across, "distance", [a, d], [20], 0),
            ],
            true, 0);
        var along = new Camera { YawDegrees = -90, PitchDegrees = 0, Distance = 50, Target = (50, 0, 5) };

        Assert.Equal([inFront], ValuesOf(sketch, along).Select(value => value.Constraint!.Value));
    }

    // Item 2: the value of a distance is at the middle of its line, or of its two points, 24 pixels farther from the centre of
    // the sketch on the screen; the value of a radius is on its curve at the point farthest from that centre, 24 pixels out.
    [Fact]
    public void A_Value_Is_At_The_Middle_Of_Its_Line_And_24_Pixels_Out()
    {
        var values = ValuesOf(Plate).ToDictionary(value => value.Constraint!.Value);
        var centre = PixelOf(Plate, 0, 0);

        var bottom = PixelOf(Plate, 0, -20);
        Assert.Equal(bottom.X, values[C1].At.X, 0.01f);
        Assert.Equal(bottom.Y + EdgeValues.Offset, values[C1].At.Y, 0.01f);
        Assert.True(bottom.Y > centre.Y, "The line at the bottom is not under the centre on the screen.");

        var right = PixelOf(Plate, 30, 0);
        Assert.Equal(right.X + EdgeValues.Offset, values[C2].At.X, 0.01f);
        Assert.Equal(right.Y, values[C2].At.Y, 0.01f);

        // The point of the circle farthest from the centre is in the direction of its centre (10, 5) from the origin.
        var direction = Vector2.Normalize(new Vector2(10, 5));
        var farthest = PixelOf(Plate, 10 + (8 * direction.X), 5 + (8 * direction.Y));
        var outward = Vector2.Normalize(farthest - centre);
        var expected = farthest + (outward * EdgeValues.Offset);
        Assert.True(Vector2.Distance(expected, values[C4].At) < 3f, $"The radius is at {values[C4].At}, not at {expected}.");
    }

    // Item 3: each chip of the active sketch shows, with at least 44 pixels on each side, and its rectangle is a part of the
    // panels for a tap. ADR-0082 item 1: no chip of an extrude shows while a sketch is active.
    [Fact]
    public void The_Chips_Show_While_The_Sketch_Is_Active_And_The_Chips_Of_An_Extrude_Do_Not()
    {
        var values = ValuesOf(Plate);
        var body = Guid.NewGuid();
        IReadOnlyList<EdgeValues.Value> edgeValues =
        [
            new EdgeValues.Value(EdgeValues.DistanceKey(body), "10 mm", new Vector2(300, 700), 10, body, null, null, "Distance"),
            new EdgeValues.Value(EdgeValues.ConstraintKey(C1), "60 mm", new Vector2(900, 700), 60, body, Sketch, C1, "Length"),
        ];
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, Panel(), values, edgeValues, body);
        Step(ui, Panel(), values, edgeValues, body);

        foreach (var value in values)
        {
            var chip = Item(ui, value.Key);
            Assert.True(chip.Z - chip.X >= DesktopUi.ValueSize && chip.W - chip.Y >= DesktopUi.ValueSize, $"The chip {chip} is smaller than 44 pixels.");
            var middle = Middle(chip);
            Assert.True(Vector2.Distance(middle, value.At) < 1f, $"The chip of {value.Text} is at {middle}, not at {value.At}.");
            Assert.True(ui.OverPanel(middle.X, middle.Y), $"A tap on the chip of {value.Text} reaches the scene.");
        }

        Assert.Null(ui.ItemRectangle(EdgeValues.DistanceKey(body)));
        Assert.Null(ui.ItemRectangle(EdgeValues.ConstraintKey(C1)));

        // With no active sketch, the chips of the extrude show and the chips of the sketch do not.
        ui.Frame(1f / 60f, Display, Vector2.One, [new LiveBody(body, "Extrude", null, true)], body, values: edgeValues, sketchValues: values);
        ui.Frame(1f / 60f, Display, Vector2.One, [new LiveBody(body, "Extrude", null, true)], body, values: edgeValues, sketchValues: values);
        Assert.NotNull(ui.ItemRectangle(EdgeValues.DistanceKey(body)));
        Assert.Null(ui.ItemRectangle(SketchValues.Key(C1)));
    }

    // Item 4: a press and a release on a chip open the pad that the row of its constraint opens, by touch and by mouse, and
    // the tap does not reach the sketch tools; the apply gives the same action as the apply of the row.
    [Theory]
    [InlineData(DeviceKind.Touch)]
    [InlineData(DeviceKind.Mouse)]
    public void A_Tap_On_A_Chip_Opens_The_Pad_Of_Its_Row(DeviceKind kind)
    {
        var values = ValuesOf(Plate);

        // The row: select C1, then Value.
        var selected = Panel(C1);
        using var row = Opened(selected, values);
        Tap(row, selected, values, DesktopUi.SketchItem(DesktopUi.ValueLabel), kind);
        var rowPad = row.Pad ?? throw new InvalidOperationException("The row opened no pad.");
        var (rowTitle, rowUnit, rowFields) = (rowPad.Title, rowPad.Unit, rowPad.Fields);
        foreach (var key in new[] { "Clear", "7", "5", "Apply" })
            Tap(row, selected, values, DesktopUi.PadItem(key), kind);
        var rowSet = Assert.IsType<SetConstraintValueAction>(Assert.Single(row.TakeActions()));

        // The chip, with no selection.
        var none = Panel();
        using var ui = Opened(none, values);
        var scene = Tap(ui, none, values, SketchValues.Key(C1), kind);
        Assert.Empty(scene);
        var pad = ui.Pad ?? throw new InvalidOperationException("The chip opened no pad.");
        Assert.Equal((rowTitle, rowUnit), (pad.Title, pad.Unit));
        Assert.Equal(rowFields, pad.Fields);
        Assert.Equal([new PadField("length", "60")], pad.Fields);
        foreach (var key in new[] { "Clear", "7", "5", "Apply" })
            Tap(ui, none, values, DesktopUi.PadItem(key), kind);
        var set = Assert.IsType<SetConstraintValueAction>(Assert.Single(ui.TakeActions()));

        Assert.Equal((C1, 75.0), (set.Constraint, Assert.Single(set.Values)));
        Assert.Equal(rowSet.Constraint, set.Constraint);
        Assert.Equal(rowSet.Values, set.Values);
    }

    // Item 4: a tap on the chip of a radius opens the pad of the radius.
    [Fact]
    public void A_Tap_On_The_Chip_Of_A_Radius_Opens_Its_Pad()
    {
        var values = ValuesOf(Plate);
        var none = Panel();
        using var ui = Opened(none, values);

        Assert.Empty(Tap(ui, none, values, SketchValues.Key(C4), DeviceKind.Touch));
        Assert.Equal([new PadField("length", "8")], ui.Pad!.Fields);
        foreach (var key in new[] { "Clear", "9", "Apply" })
            Tap(ui, none, values, DesktopUi.PadItem(key), DeviceKind.Touch);

        var set = Assert.IsType<SetConstraintValueAction>(Assert.Single(ui.TakeActions()));
        Assert.Equal((C4, 9.0), (set.Constraint, Assert.Single(set.Values)));
    }

    // Item 4: while the pad of a chip is open, the chip has the colour of the dimensions and a dashed border; the other chips
    // keep their colours.
    [Fact]
    public void The_Chip_Is_Pending_While_Its_Pad_Is_Open()
    {
        var values = ValuesOf(Plate);
        var none = Panel();
        using var ui = Opened(none, values);
        int Dimension(UiDrawData data, string key)
        {
            var chip = Item(ui, key);
            return data.Vertices.Count(vertex => vertex.Colour == DesktopUi.DimensionColour
                && vertex.Position.X >= chip.X - 2 && vertex.Position.X <= chip.Z + 2 && vertex.Position.Y >= chip.Y - 2 && vertex.Position.Y <= chip.W + 2);
        }

        var before = Step(ui, none, values);
        Assert.Null(ui.PendingValue);
        Assert.Equal(0, Dimension(before, SketchValues.Key(C2)));

        Tap(ui, none, values, SketchValues.Key(C2), DeviceKind.Touch);
        var during = Step(ui, none, values);

        Assert.NotNull(ui.Pad);
        Assert.Equal(SketchValues.Key(C2), ui.PendingValue);
        // The dashes of 6 pixels with gaps of 4 pixels on a border of at least 4 times 44 pixels give at least 17 dashes,
        // each with at least 2 vertices of the full colour, and the text adds more.
        Assert.True(Dimension(during, SketchValues.Key(C2)) >= 17 * 2, $"The pending chip has {Dimension(during, SketchValues.Key(C2))} vertices of the colour of the dimensions.");
        Assert.Equal(0, Dimension(during, SketchValues.Key(C1)));
    }

    // ADR-0105 item 1: the host gives the window the values of its active sketch, from the live sketches of the frame, with
    // the move of the sketch under its body (ADR-0085 item 2); with no active sketch, it gives none. The test runs the method
    // that NativeThreeDEngine.Render calls, with a sketch that the engine made.
    [Fact]
    public async Task The_Host_Gives_The_Values_Of_Its_Active_Sketch()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var sketch = Out(await host.Session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id, SketchName = "Plate" }), "sketchId");
        (double X, double Y)[] corners = [(-30, -20), (30, -20), (30, 20), (-30, 20)];
        var points = new List<Guid>();
        foreach (var (x, y) in corners)
            points.Add(Out(await host.Session.Apply(new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }), "pointId"));
        var lines = new List<Guid>();
        for (var i = 0; i < points.Count; i++)
            lines.Add(Out(await host.Session.Apply(new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Count] }), "lineId"));
        await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = lines[0], Value = 60 });
        await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "horizontal", First = lines[2] });
        await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = points[1], Second = points[2], Value = 40 });
        var info = (await host.Session.Query<SketchInfo>(new GetSketchQuery { SketchId = sketch })).Result!;
        var actions = new SketchActions(host.Session);
        IReadOnlyList<EdgeValues.Value> Host() => NativeThreeDEngine.SketchValuesOf(actions, [info], Facing, Display.X, Display.Y, "mm");

        Assert.Empty(Host());
        Assert.Empty(NativeThreeDEngine.SketchValuesOf(null, [info], Facing, Display.X, Display.Y, "mm"));

        actions.Edit(sketch);
        var values = Host();
        Assert.Equal(["C1  60 mm", "C3  40 mm"], values.Select(value => value.Text));
        Assert.Equal(SketchValues.Of(info, Facing, Display.X, Display.Y, "mm").Select(value => (value.Key, value.At)), values.Select(value => (value.Key, value.At)));

        // ADR-0085 item 2: the sketch under its body is farther from the eye, so its values are nearer to its centre.
        actions.Lift = SketchLift.Under(sketch, SketchOverlay.PlaneOf(info)!, (-30, -20, 0), (30, 20, 10)) ?? throw new InvalidOperationException("No move.");
        var lifted = Host();
        Assert.Equal(SketchValues.Of(actions.Lift.Apply(info), Facing, Display.X, Display.Y, "mm").Select(value => value.At), lifted.Select(value => value.At));
        Assert.True(lifted[0].At.Y < values[0].At.Y - 1f, $"The value of the moved sketch is at {lifted[0].At}, as the value of the record at {values[0].At}.");

        // A sketch that is not live gives no value; the end of the edit gives none.
        Assert.Empty(NativeThreeDEngine.SketchValuesOf(actions, [], Facing, Display.X, Display.Y, "mm"));
        actions.Finish();
        Assert.Empty(Host());
    }

    private static Guid Out(CommandResult result, string name)
    {
        Assert.True(result.Outputs.TryGet<Guid>(name, out var value), $"{result.CommandName}: {result.Error?.Code} {result.Error?.Message}");
        return value;
    }
}
