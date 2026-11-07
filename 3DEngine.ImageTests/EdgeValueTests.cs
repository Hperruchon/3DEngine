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
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0082 (the redesign, slice c, TASK-0254): the values of the selected extrude on its edges, with no GPU. ADR-0082 names
// this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class EdgeValueTests
{
    private static readonly Vector2 Display = new(1280, 720);
    private static readonly Camera View = new() { YawDegrees = 30, PitchDegrees = 25, Distance = 40, Target = (5, 3, 1.5) };

    private static Guid Out(CommandResult result, string name)
    {
        Assert.True(result.Outputs.TryGet<Guid>(name, out var value), $"{result.CommandName}: {result.Error?.Code} {result.Error?.Message}");
        return value;
    }

    // A rectangle of 10 by 6 with a length of 10 on its first line and a distance of 6 between the ends of its second line,
    // extruded by 3.
    private static async Task<(EngineHost Host, Guid Body, Guid Sketch, Guid Length, Guid Width)> Plate()
    {
        var host = new EngineHost(new ManifoldGeometryBackend());
        var sketch = Out(await host.Session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        (double X, double Y)[] corners = [(0, 0), (10, 0), (10, 6), (0, 6)];
        var points = new List<Guid>();
        foreach (var (x, y) in corners)
            points.Add(Out(await host.Session.Apply(new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }), "pointId"));
        var lines = new List<Guid>();
        for (var i = 0; i < points.Count; i++)
            lines.Add(Out(await host.Session.Apply(new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Count] }), "lineId"));
        var length = Out(await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = lines[0], Value = 10 }), "constraintId");
        var width = Out(await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = points[1], Second = points[2], Value = 6 }), "constraintId");
        var body = Out(await host.Session.Apply(new ExtrudeCommand { SketchId = sketch, Distance = 3 }), "bodyId");
        return (host, body, sketch, length, width);
    }

    private static async Task<IReadOnlyList<EdgeValues.Value>> ValuesOf(EngineHost host, Guid body, double? distance)
    {
        var faces = (await host.Session.Query<FacesInfo>(new GetFacesQuery { BodyId = body })).Result ?? throw new InvalidOperationException("No faces.");
        var sketches = new List<SketchInfo>();
        foreach (var id in await host.Session.Read((document, _) => document.Sketches.Select(sketch => sketch.Id).ToArray()))
            sketches.Add((await host.Session.Query<SketchInfo>(new GetSketchQuery { SketchId = id })).Result!);
        return EdgeValues.Of(body, distance, faces.Edges, sketches, View, Display.X, Display.Y, "mm");
    }

    // Item 1: an extrude gives its distance, and the length of a line and the distance of the two ends of a line of its
    // sketch, each once; each chip is away from the middle of the body.
    [Fact]
    public async Task An_Extrude_Gives_Its_Distance_And_The_Lengths_Of_Its_Sketch()
    {
        var (host, body, sketch, length, width) = await Plate();
        using var _ = host;

        var values = await ValuesOf(host, body, 3);

        Assert.Equal(3, values.Count);
        Assert.Equal((3.0, (Guid?)null, "3 mm"), (values[0].Number, values[0].Constraint, values[0].Text));
        Assert.Equal(EdgeValues.DistanceKey(body), values[0].Key);
        var byConstraint = values.Skip(1).ToDictionary(value => value.Constraint!.Value);
        Assert.Equal((10.0, (Guid?)sketch), (byConstraint[length].Number, byConstraint[length].Sketch));
        Assert.Equal((6.0, (Guid?)sketch), (byConstraint[width].Number, byConstraint[width].Sketch));
    }

    // Item 1: a box has no chip, because no command changes its sizes.
    [Fact]
    public async Task A_Box_Gives_No_Value()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var box = Out(await host.Session.Apply(new CreateBoxCommand { SizeX = 10, SizeY = 6, SizeZ = 3 }), "bodyId");

        Assert.Empty(await ValuesOf(host, box, null));
    }

    private static void Step(DesktopUi ui, IReadOnlyList<EdgeValues.Value> values, Guid body, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, Display, Vector2.One, [new LiveBody(body, "Extrude", null, true)], body, values: values);
    }

    private static (float X, float Y) Centre(DesktopUi ui, string item)
    {
        var rectangle = ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item}.");
        return ((rectangle.X + rectangle.Z) / 2, (rectangle.Y + rectangle.W) / 2);
    }

    private static List<InputSample> Tap(DesktopUi ui, IReadOnlyList<EdgeValues.Value> values, Guid body, DeviceKind kind, string item)
    {
        var at = Centre(ui, item);
        var (pointer, buttons) = kind == DeviceKind.Mouse ? (0UL, PointerButtons.Left) : (3UL, PointerButtons.None);
        var scene = new List<InputSample>();
        foreach (var phase in new[] { PointerPhase.Down, PointerPhase.Up })
        {
            scene.AddRange(ui.Route([new PointerSample(kind, 1, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, phase, 0)]));
            ui.Frame(1f / 60f, Display, Vector2.One, [new LiveBody(body, "Extrude", null, true)], body, values: values);
        }
        Step(ui, values, body);
        Step(ui, values, body);
        return scene;
    }

    // Items 2 to 4: each chip is at least 44 pixels; a tap on the distance asks the host for its pad, by touch and by mouse,
    // and does not reach the scene; while the pad is open, the chip is pending.
    [Theory]
    [InlineData(DeviceKind.Touch)]
    [InlineData(DeviceKind.Mouse)]
    public void A_Tap_On_The_Distance_Opens_Its_Pad_And_The_Chip_Is_Pending(DeviceKind kind)
    {
        var body = Guid.NewGuid();
        IReadOnlyList<EdgeValues.Value> values = [new EdgeValues.Value(EdgeValues.DistanceKey(body), "3 mm", new Vector2(700, 300), 3, body, null, null)];
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, values, body);
        Step(ui, values, body);
        var chip = ui.ItemRectangle(values[0].Key) ?? throw new InvalidOperationException("No chip.");
        Assert.True(chip.Z - chip.X >= DesktopUi.ValueSize && chip.W - chip.Y >= DesktopUi.ValueSize, $"The chip {chip} is smaller than 44 pixels.");

        var scene = Tap(ui, values, body, kind, values[0].Key);

        Assert.Empty(scene);
        Assert.Equal([new EditDistanceAction(body)], ui.TakeActions());
        ui.OpenDistancePad(body, 3);
        Step(ui, values, body);
        Assert.Equal(values[0].Key, ui.PendingValue);
    }

    // Item 3: a tap on the value of a constraint opens its pad, and the apply gives the new value of that constraint of the
    // sketch of the extrude.
    [Fact]
    public void A_Tap_On_A_Length_Opens_Its_Pad_And_The_Apply_Gives_The_New_Value()
    {
        var (body, sketch, constraint) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        IReadOnlyList<EdgeValues.Value> values = [new EdgeValues.Value(EdgeValues.ConstraintKey(constraint), "10 mm", new Vector2(700, 300), 10, body, sketch, constraint)];
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, values, body);
        Step(ui, values, body);

        Tap(ui, values, body, DeviceKind.Touch, values[0].Key);
        Assert.Equal([new PadField("value", "10")], ui.Pad!.Fields);
        Assert.Equal(values[0].Key, ui.PendingValue);
        foreach (var label in new[] { "Clear", "1", "2", "Apply" })
            Tap(ui, values, body, DeviceKind.Mouse, DesktopUi.PadItem(label));

        Assert.Equal([new SetBodyValueAction(sketch, constraint, 12)], ui.TakeActions());
    }

    // Item 1: with the pad of another tool open, no chip shows.
    [Fact]
    public void No_Chip_Shows_While_The_Pad_Of_Another_Tool_Is_Open()
    {
        var body = Guid.NewGuid();
        IReadOnlyList<EdgeValues.Value> values = [new EdgeValues.Value(EdgeValues.DistanceKey(body), "3 mm", new Vector2(700, 300), 3, body, null, null)];
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, values, body);
        Step(ui, values, body);
        Tap(ui, values, body, DeviceKind.Touch, DesktopUi.MoveLabel);

        Assert.NotNull(ui.Pad);
        Assert.Null(ui.ItemRectangle(values[0].Key));
    }

    // Finding V119 of the codebase review of 2026-11-07 (TASK-0264), items 1 and 4: the pad of another tool from the panel,
    // while the pad of a chip is open, ends the pending state, and no chip shows. Until TASK-0264 the chip stayed pending
    // for a pad that was not open.
    [Fact]
    public void The_Pad_Of_Another_Tool_Ends_The_Pending_Chip()
    {
        var (body, sketch, constraint) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        IReadOnlyList<EdgeValues.Value> values = [new EdgeValues.Value(EdgeValues.ConstraintKey(constraint), "10 mm", new Vector2(700, 300), 10, body, sketch, constraint)];
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, values, body);
        Step(ui, values, body);
        Tap(ui, values, body, DeviceKind.Touch, values[0].Key);
        Assert.Equal(values[0].Key, ui.PendingValue);

        Tap(ui, values, body, DeviceKind.Touch, DesktopUi.MoveLabel);

        Assert.Equal(DesktopUi.MoveLabel, ui.Pad!.Title);
        Assert.Null(ui.PendingValue);
        Assert.Null(ui.ItemRectangle(values[0].Key));
    }
}
