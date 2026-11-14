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

// ADR-0149 (phase R22c, TASK-0384): in a flat view a line with no value whose length the design does not fix shows that
// length in amber, "not fixed". ADR-0149 names this file in enforced-by. With no GPU.
[Collection(DearImGui.Collection)]
public sealed class FreeSizeTests
{
    private static readonly Vector2 Display = new(1280, 720);

    private static Guid Out(CommandResult result, string name)
    {
        Assert.True(result.Outputs.TryGet<Guid>(name, out var value), $"{result.CommandName}: {result.Error?.Code} {result.Error?.Message}");
        return value;
    }

    private static Camera Camera(FlatView view)
    {
        var camera = new Camera { ZUp = true, YawDegrees = 30, PitchDegrees = 25, Distance = 150, Target = (30, 15, 2.5), FarPlane = 5000f };
        camera.ShowFlat(view);
        return camera;
    }

    // A profile of the corners with the lengths on its lines and, when directions is true, each line horizontal or vertical;
    // extruded by 5.
    private static async Task<IReadOnlyList<EdgeValues.Value>> ValuesOf(FlatView view, (double X, double Y)[] corners, (int Line, double Value)[] valued, bool directions)
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
        for (var i = 0; directions && i < lines.Count; i++)
        {
            var (a, b) = (corners[i], corners[(i + 1) % corners.Length]);
            Out(await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = a.Y == b.Y ? "horizontal" : "vertical", First = lines[i] }), "constraintId");
        }

        var body = Out(await host.Session.Apply(new ExtrudeCommand { SketchId = sketch, Distance = 5 }), "bodyId");
        var faces = (await host.Session.Query<FacesInfo>(new GetFacesQuery { BodyId = body })).Result ?? throw new InvalidOperationException("No faces.");
        var info = (await host.Session.Query<SketchInfo>(new GetSketchQuery { SketchId = sketch })).Result!;
        return EdgeValues.Of(body, 5, faces.Edges, [info], Camera(view), Display.X, Display.Y, "mm");
    }

    private static readonly (double X, double Y)[] L = [(0, 0), (60, 0), (60, 10), (20, 10), (20, 30), (0, 30)];

    private static readonly (int, double)[] LValues = [(0, 60.0), (1, 10.0), (2, 40.0), (5, 30.0)];

    // Item 2: an L with no direction constraint can lean, so the side of its step and its top are not fixed: from the top
    // both show "20.000 mm not fixed", and no reference; from the front only the top is across the view.
    [Theory]
    [InlineData(FlatView.Top, 2)]
    [InlineData(FlatView.Front, 1)]
    public async Task A_Line_That_The_Design_Does_Not_Fix_Shows_Not_Fixed(FlatView view, int count)
    {
        var values = await ValuesOf(view, L, LValues, directions: false);

        var free = values.Where(value => value.Free).ToArray();
        Assert.Equal(count, free.Length);
        Assert.All(free, value => Assert.Equal("20.000 mm not fixed", value.Text));
        Assert.All(free, value => Assert.True(value.InView && value.Constraint is null && !value.Reference));
        Assert.DoesNotContain(values, value => value.Reference);
    }

    // Item 2: with each line horizontal or vertical, the design fixes each length of the L, so no size is free and the
    // references of ADR-0148 show; a rectangle of two lengths and its directions has no free size and no reference.
    [Fact]
    public async Task A_Line_That_The_Design_Fixes_Is_Not_Free()
    {
        var l = await ValuesOf(FlatView.Top, L, LValues, directions: true);
        Assert.DoesNotContain(l, value => value.Free);
        Assert.Equal(2, l.Count(value => value.Reference));

        var rectangle = await ValuesOf(FlatView.Top, [(0, 0), (10, 0), (10, 6), (0, 6)], [(0, 10.0), (1, 6.0)], directions: true);
        Assert.DoesNotContain(rectangle, value => value.Free || value.Reference);
    }

    // Item 2: in the window a free size has a chip and no row, and a tap on it opens no pad and sends nothing.
    [Fact]
    public void A_Free_Size_Has_A_Chip_And_No_Row_And_No_Pad()
    {
        var plate = Guid.Parse("64000000-0000-0000-0000-000000000000");
        var sketch = Guid.Parse("64000000-0000-0000-0000-000000000001");
        var width = Guid.Parse("64000000-0000-0000-0000-000000000002");
        var line = Guid.Parse("64000000-0000-0000-0000-000000000003");
        var body = new LiveBody(plate, "Extrude", "Plate", true);
        IReadOnlyList<EdgeValues.Value> values =
        [
            new(EdgeValues.ConstraintKey(width), "60.000 mm", new Vector2(700, 600), 60, plate, sketch, width, "Width"),
            new(EdgeValues.FreeKey(line), "20.000 mm not fixed", new Vector2(900, 400), 20, plate, sketch, null, "Not fixed", Free: true),
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
        Assert.Null(ui.ItemRectangle(DesktopUi.RowItem(values[1].Key)));
        var chip = ui.ItemRectangle(values[1].Key);
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
