using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0117 (phase R18d, TASK-0342): the rows of a body as in the render: the name larger with its state, the row "Made
// from", and each value with the label of its direction and the name of its constraint. ADR-0117 names this file in its
// enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class BodyRowsRenderTests
{
    private static readonly Vector2 Display = new(1280, 720);
    private static readonly Camera View = new() { YawDegrees = 30, PitchDegrees = 25, Distance = 40, Target = (5, 3, 1.5) };
    private static readonly Guid Body = Guid.Parse("63000000-0000-0000-0000-000000000000");
    private static readonly Guid SketchStep = Guid.Parse("63000000-0000-0000-0000-000000000001");
    private static readonly LiveBody Extrude = new(Body, "Extrude", "Plate", true);

    private static readonly IReadOnlyList<HistoryStep> History =
    [
        new(Body, "Extrude", "Plate", "correct", null, HistoryTap.None),
        new(SketchStep, "CreateSketch", "Plate on XY", "correct", null, HistoryTap.EditSketch),
    ];

    private static EdgeEntry Edge(double x, double y, double z)
        => new(new DatumReference(Body, "end", null), new DatumReference(Body, "side", null), 1, 1, [new FaceSegment(new DatumPoint(1, 2, 3), new DatumPoint(1 + x, 2 + y, 3 + z))]);

    // Item 3: a length along the x axis is a width, along the y axis a depth, along the z axis a height (the z axis is up),
    // and along another direction a length; an edge with no length is a length.
    [Theory]
    [InlineData(10, 0, 0, "Width")]
    [InlineData(-10, 0, 0, "Width")]
    [InlineData(0, 6, 0, "Depth")]
    [InlineData(0, 0, -3, "Height")]
    [InlineData(10, 0.001, 0, "Width")]
    [InlineData(10, 1, 0, "Length")]
    [InlineData(3, 4, 5, "Length")]
    [InlineData(0, 0, 0, "Length")]
    public void The_Label_Of_A_Length_Comes_From_The_Direction_Of_Its_Edge(double x, double y, double z, string expected)
        => Assert.Equal(expected, EdgeValues.Direction(Edge(x, y, z)));

    // Item 3: the plate of 10 by 6 on XY, extruded by 3: the distance, the length of 10 along x (a width) and the distance of
    // 6 along y (a depth), each with the name of its constraint in the sketch.
    [Fact]
    public async Task The_Values_Of_A_Plate_Are_A_Distance_A_Width_And_A_Depth_With_Their_Names()
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var sketch = Out(await host.Session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        (double X, double Y)[] corners = [(0, 0), (10, 0), (10, 6), (0, 6)];
        var points = new List<Guid>();
        foreach (var (x, y) in corners)
            points.Add(Out(await host.Session.Apply(new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }), "pointId"));
        var lines = new List<Guid>();
        for (var i = 0; i < points.Count; i++)
            lines.Add(Out(await host.Session.Apply(new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Count] }), "lineId"));
        Out(await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = lines[0], Value = 10 }), "constraintId");
        Out(await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = points[1], Second = points[2], Value = 6 }), "constraintId");
        var body = Out(await host.Session.Apply(new ExtrudeCommand { SketchId = sketch, Distance = 3 }), "bodyId");
        var faces = (await host.Session.Query<FacesInfo>(new GetFacesQuery { BodyId = body })).Result ?? throw new InvalidOperationException("No faces.");
        var info = (await host.Session.Query<SketchInfo>(new GetSketchQuery { SketchId = sketch })).Result!;

        var values = EdgeValues.Of(body, 3, faces.Edges, [info], View, Display.X, Display.Y, "mm");

        Assert.Equal([("Distance", (string?)null), ("Width", "C1"), ("Depth", "C2")], values.Select(value => (value.Label, value.Name)));
    }

    // Item 1: the name is in the font of a title, larger than the text, and the state is on the same line when it fits; a
    // long state of a failed and hidden body goes under the name, and the first row is higher by a line.
    [Fact]
    public void The_Name_Is_Larger_And_Its_State_Is_After_It_When_It_Fits()
    {
        Assert.True(DesktopUi.TitleSize > DesktopUi.FontSize, "The name is not larger than the text.");
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, Extrude, History);
        Step(ui, Extrude, History);
        Assert.True(ui.RowTitle(Extrude).OneLine, "The state of a correct extrude is not after its name.");
        var title = Rectangle(ui, DesktopUi.RowsTitleItem);
        Assert.Equal(DesktopUi.RowHeight, title.W - title.Y, 0.5f);

        var failed = Extrude with { Name = "The long name of the plate", Visible = false, Fault = new BodyFault("E-FEATURE-REBUILD", "The profile is open.") };
        Step(ui, failed, History);
        Step(ui, failed, History);
        Assert.False(ui.RowTitle(failed).OneLine, "The long state is on the line of a long name.");
        title = Rectangle(ui, DesktopUi.RowsTitleItem);
        Assert.True(title.W - title.Y > DesktopUi.RowHeight, $"The first row at {title} is not higher for the state under the name.");
        Assert.True(Rectangle(ui, DesktopUi.MadeFromItem).Y >= title.W - 0.5f, "The row \"Made from\" is not under the first row.");
    }

    // Item 2: the row "Made from" is under the first row, at least 44 pixels high; a tap on it opens the sketch of its step of
    // the history, by mouse and by touch, and does not reach the scene. With no step that opens a sketch, as for a box, the
    // row does not show.
    [Theory]
    [InlineData(DeviceKind.Mouse)]
    [InlineData(DeviceKind.Touch)]
    public void Made_From_Opens_The_Sketch_Of_The_Body(DeviceKind kind)
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, Extrude, History);
        Step(ui, Extrude, History);
        var title = Rectangle(ui, DesktopUi.RowsTitleItem);
        var row = Rectangle(ui, DesktopUi.MadeFromItem);
        Assert.True(row.Y >= title.W - 0.5f && row.W - row.Y >= DesktopUi.MinimumControlHeight, $"The row \"Made from\" at {row} is not a row under the first row at {title}.");

        ui.TakeActions();
        var at = new Vector2((row.X + row.Z) / 2f, (row.Y + row.W) / 2f);
        var (pointer, buttons) = kind == DeviceKind.Mouse ? (0UL, PointerButtons.Left) : (3UL, PointerButtons.None);
        var scene = new List<InputSample>();
        foreach (var phase in new[] { PointerPhase.Down, PointerPhase.Up })
        {
            scene.AddRange(ui.Route([new PointerSample(kind, 1, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, phase, 0)]));
            ui.Frame(1f / 60f, Display, Vector2.One, [Extrude], Body, null, history: History);
        }

        // A touch gives its release in the next frame.
        Step(ui, Extrude, History);
        Assert.Equal([new EditSketchAction(SketchStep)], ui.TakeActions());
        Assert.Empty(scene);

        Step(ui, Extrude, [History[0]]);
        Step(ui, Extrude, [History[0]]);
        Assert.Null(ui.ItemRectangle(DesktopUi.MadeFromItem));
    }

    private static void Step(DesktopUi ui, LiveBody body, IReadOnlyList<HistoryStep> history)
    {
        ui.Route([]);
        ui.Frame(1f / 60f, Display, Vector2.One, [body], body.Id, null, history: history);
    }

    private static Vector4 Rectangle(DesktopUi ui, string item) => ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item}.");

    private static Guid Out(CommandResult result, string name)
    {
        Assert.True(result.Outputs.TryGet<Guid>(name, out var value), $"{result.CommandName}: {result.Error?.Code} {result.Error?.Message}");
        return value;
    }
}
