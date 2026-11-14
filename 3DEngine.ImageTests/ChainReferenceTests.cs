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

// ADR-0148 (phase R22b, TASK-0383): a chain of the design gives one reference in parentheses, and each value keeps its row in
// each view. ADR-0148 names this file in enforced-by. With no GPU.
[Collection(DearImGui.Collection)]
public sealed class ChainReferenceTests
{
    private static readonly Vector2 Display = new(1280, 720);

    private static Guid Out(CommandResult result, string name)
    {
        Assert.True(result.Outputs.TryGet<Guid>(name, out var value), $"{result.CommandName}: {result.Error?.Code} {result.Error?.Message}");
        return value;
    }

    private static Camera Camera(FlatView? view)
    {
        var camera = new Camera { ZUp = true, YawDegrees = 30, PitchDegrees = 25, Distance = 150, Target = (30, 15, 2.5), FarPlane = 5000f };
        if (view is { } flat)
            camera.ShowFlat(flat);
        return camera;
    }

    // An L of 60 by 30 with a step: the bottom of 60, the right side of 10, the step of 40 and the left side of 30 have a value;
    // the side of the step and the top have none. Extruded by 5.
    private static Task<IReadOnlyList<EdgeValues.Value>> ValuesOfL(FlatView? view)
        => ValuesOf(view, [(0, 0), (60, 0), (60, 10), (20, 10), (20, 30), (0, 30)], [(0, 60.0), (1, 10.0), (2, 40.0), (5, 30.0)]);

    // A profile of the corners with the values on its lines (the line i goes from the corner i to the next), extruded by 5.
    private static async Task<IReadOnlyList<EdgeValues.Value>> ValuesOf(FlatView? view, (double X, double Y)[] corners, (int Line, double Value)[] valued)
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var sketch = Out(await host.Session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var points = new List<Guid>();
        foreach (var (x, y) in corners)
            points.Add(Out(await host.Session.Apply(new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }), "pointId"));
        var lines = new List<Guid>();
        for (var i = 0; i < points.Count; i++)
            lines.Add(Out(await host.Session.Apply(new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Count] }), "lineId"));
        foreach (var (line, value) in valued)
            Out(await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = lines[line], Value = value }), "constraintId");

        // ADR-0149 (TASK-0384): each line is horizontal or vertical, so the design fixes the length of each free line.
        for (var i = 0; i < lines.Count; i++)
        {
            var (a, b) = (corners[i], corners[(i + 1) % corners.Length]);
            Out(await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = a.Y == b.Y ? "horizontal" : "vertical", First = lines[i] }), "constraintId");
        }
        var body = Out(await host.Session.Apply(new ExtrudeCommand { SketchId = sketch, Distance = 5 }), "bodyId");

        var faces = (await host.Session.Query<FacesInfo>(new GetFacesQuery { BodyId = body })).Result ?? throw new InvalidOperationException("No faces.");
        var info = (await host.Session.Query<SketchInfo>(new GetSketchQuery { SketchId = sketch })).Result!;
        return EdgeValues.Of(body, 5, faces.Edges, [info], Camera(view), Display.X, Display.Y, "mm");
    }

    // Item 2: from the top, the top of the L closes the chain 40 + (20) = 60 along x, and the side of the step closes the
    // chain 10 + (20) = 30 along y; from the front only the chain along x is across the view; with perspective no reference
    // shows. Item 1: each of the five values of the design is in the list in each view.
    [Theory]
    [InlineData(FlatView.Top, new[] { 20.0, 20.0 })]
    [InlineData(FlatView.Front, new[] { 20.0 })]
    [InlineData(null, new double[0])]
    public async Task A_Line_That_Closes_A_Chain_Shows_Its_Reference(FlatView? view, double[] expected)
    {
        var values = await ValuesOfL(view);

        Assert.Equal(new[] { 5.0, 10.0, 30.0, 40.0, 60.0 }, values.Where(value => !value.Reference).Select(value => value.Number).Order());
        var references = values.Where(value => value.Reference).ToArray();
        Assert.Equal(expected, references.Select(value => value.Number).Order());
        Assert.All(references, value => Assert.Equal("(20.000 mm)", value.Text));
        Assert.All(references, value => Assert.Null(value.Constraint));
    }

    // Item 2: a U of 60 by 30 with a notch of 20: the bottom, the right side, the top right of 20 and the left side have a
    // value. The top left and the bottom of the notch have none, so along x each of them leaves a gap in the chain of the
    // bottom, and no line closes a chain: no reference shows.
    [Fact]
    public async Task A_Line_That_Leaves_A_Gap_Shows_No_Reference()
    {
        var values = await ValuesOf(FlatView.Top, [(0, 0), (60, 0), (60, 30), (40, 30), (40, 10), (20, 10), (20, 30), (0, 30)], [(0, 60.0), (1, 30.0), (2, 20.0), (7, 30.0)]);

        Assert.DoesNotContain(values, value => value.Reference);
    }

    // Items 1 and 2: in the window a value whose length the view does not show true has its row and no chip, the reference has
    // its chip and no row, and a tap on the reference opens no pad.
    [Fact]
    public void A_Reference_Has_A_Chip_And_No_Row_And_No_Pad()
    {
        var plate = Guid.Parse("63000000-0000-0000-0000-000000000000");
        var sketch = Guid.Parse("63000000-0000-0000-0000-000000000001");
        var width = Guid.Parse("63000000-0000-0000-0000-000000000002");
        var line = Guid.Parse("63000000-0000-0000-0000-000000000003");
        var body = new LiveBody(plate, "Extrude", "Plate", true);
        IReadOnlyList<EdgeValues.Value> values =
        [
            new(EdgeValues.DistanceKey(plate), "5.000 mm", new Vector2(700, 520), 5, plate, null, null, "Distance", InView: false),
            new(EdgeValues.ConstraintKey(width), "60.000 mm", new Vector2(700, 600), 60, plate, sketch, width, "Width"),
            new(EdgeValues.ReferenceKey(line), "(20.000 mm)", new Vector2(900, 400), 20, plate, sketch, null, "Reference", Reference: true),
        ];
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        void Frame(params InputSample[] samples)
        {
            ui.Route(samples);
            ui.Frame(1f / 60f, Display, Vector2.One, [body], plate, null, values: values);
        }

        Frame();
        Frame();
        ui.TakeActions();

        Assert.NotNull(ui.ItemRectangle(DesktopUi.RowItem(values[0].Key)));
        Assert.NotNull(ui.ItemRectangle(DesktopUi.RowItem(values[1].Key)));
        Assert.Null(ui.ItemRectangle(DesktopUi.RowItem(values[2].Key)));
        Assert.Null(ui.ItemRectangle(values[0].Key));
        var chip = ui.ItemRectangle(values[2].Key);
        Assert.NotNull(chip);

        var at = new Vector2((chip!.Value.X + chip.Value.Z) / 2, (chip.Value.Y + chip.Value.W) / 2);
        Frame(new PointerSample(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Move, 0));
        foreach (var phase in new[] { PointerPhase.Down, PointerPhase.Up })
            Frame(new PointerSample(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.Left, KeyModifiers.None, phase, 0));
        Frame();
        Frame();

        Assert.Null(ui.Pad);
        Assert.Empty(ui.TakeActions());
    }
}
