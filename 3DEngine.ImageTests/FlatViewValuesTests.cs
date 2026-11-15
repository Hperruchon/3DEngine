using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;

namespace ThreeDEngine.ImageTests;

// ADR-0146 (phase R22a, TASK-0381): in a flat view a value shows only where its length is true. ADR-0146 names this file in
// enforced-by. With no GPU.
[Collection(DearImGui.Collection)]
public sealed class FlatViewValuesTests
{
    private static readonly Vector2 Display = new(1280, 720);

    private static Guid Out(CommandResult result, string name)
    {
        Assert.True(result.Outputs.TryGet<Guid>(name, out var value), $"{result.CommandName}: {result.Error?.Code} {result.Error?.Message}");
        return value;
    }

    private static Camera Camera(FlatView? view)
    {
        var camera = new Camera { ZUp = true, YawDegrees = 30, PitchDegrees = 25, Distance = 60, Target = (5, 3, 1.5), FarPlane = 5000f };
        if (view is { } flat)
            camera.ShowFlat(flat);
        return camera;
    }

    private static async Task<IReadOnlyList<EdgeValues.Value>> ValuesOf(EngineHost host, Guid body, double distance, Camera camera)
    {
        var faces = (await host.Session.Query<FacesInfo>(new GetFacesQuery { BodyId = body })).Result ?? throw new InvalidOperationException("No faces.");
        var sketches = new List<SketchInfo>();
        foreach (var id in await host.Session.Read((document, _) => document.Sketches.Select(sketch => sketch.Id).ToArray()))
            sketches.Add((await host.Session.Query<SketchInfo>(new GetSketchQuery { SketchId = id })).Result!);
        return EdgeValues.Of(body, distance, faces.Edges, sketches, camera, Display.X, Display.Y, "mm");
    }

    // Item 1: a plate of 10 along x and 6 along y, extruded by 3 along z, shows from the front its length and its distance,
    // from the top its length and its width, from the right its width and its distance, and with perspective all three.
    [Theory]
    [InlineData(null, new[] { 3.0, 6.0, 10.0 })]
    [InlineData(FlatView.Front, new[] { 3.0, 10.0 })]
    [InlineData(FlatView.Back, new[] { 3.0, 10.0 })]
    [InlineData(FlatView.Top, new[] { 6.0, 10.0 })]
    [InlineData(FlatView.Bottom, new[] { 6.0, 10.0 })]
    [InlineData(FlatView.Right, new[] { 3.0, 6.0 })]
    [InlineData(FlatView.Left, new[] { 3.0, 6.0 })]
    public async Task A_Value_Shows_Where_Its_Length_Is_True(FlatView? view, double[] expected)
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

        var values = await ValuesOf(host, body, 3, Camera(view));

        Assert.Equal(expected, values.Select(value => value.Number).Order());
    }

    // Item 1: a radius takes the rule of its circle, not of its chord. A half circle in the plane XY has a chord along x, which
    // is across the front view, but its circle does not face the front view; from the top it does.
    [Fact]
    public void A_Radius_Takes_The_Rule_Of_Its_Circle_Not_Of_Its_Chord()
    {
        var points = Enumerable.Range(0, 9).Select(i => (X: 4 * Math.Cos(Math.PI * i / 8), Y: 4 * Math.Sin(Math.PI * i / 8))).ToArray();
        var segments = points.Zip(points.Skip(1), (a, b) => new FaceSegment(new DatumPoint(a.X, a.Y, 2), new DatumPoint(b.X, b.Y, 2))).ToArray();
        var reference = new DatumReference(Guid.NewGuid(), "end", null);
        var edge = new EdgeEntry(reference, reference with { Element = "side" }, 1, 4 * Math.PI, segments);

        Assert.True(EdgeValues.TrueLength(edge, Camera(FlatView.Front), radius: false));
        Assert.False(EdgeValues.TrueLength(edge, Camera(FlatView.Front), radius: true));
        Assert.True(EdgeValues.TrueLength(edge, Camera(FlatView.Top), radius: true));
        Assert.True(EdgeValues.TrueLength(edge, Camera(null), radius: true));
    }

    // Item 1: the radius of a half arc of a profile in the shape of a D does not show from the front, where the chord of the
    // arc is across the view and its circle is a line, and shows from the top.
    [Theory]
    [InlineData(FlatView.Top, true)]
    [InlineData(FlatView.Front, false)]
    public async Task The_Radius_Of_A_Half_Arc_Shows_Where_Its_Circle_Faces_The_View(FlatView view, bool shown)
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var sketch = Out(await host.Session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var centre = Out(await host.Session.Apply(new AddSketchPointCommand { SketchId = sketch, X = 0, Y = 0 }), "pointId");
        var right = Out(await host.Session.Apply(new AddSketchPointCommand { SketchId = sketch, X = 4, Y = 0 }), "pointId");
        var left = Out(await host.Session.Apply(new AddSketchPointCommand { SketchId = sketch, X = -4, Y = 0 }), "pointId");
        Out(await host.Session.Apply(new AddSketchLineCommand { SketchId = sketch, StartPointId = left, EndPointId = right }), "lineId");
        var arc = Out(await host.Session.Apply(new AddSketchArcCommand { SketchId = sketch, CenterPointId = centre, StartPointId = right, EndPointId = left }), "arcId");
        Out(await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "radius", First = arc, Value = 4 }), "constraintId");
        var body = Out(await host.Session.Apply(new ExtrudeCommand { SketchId = sketch, Distance = 2 }), "bodyId");

        var values = await ValuesOf(host, body, 2, Camera(view));

        Assert.Equal(shown, values.Any(value => value.Label == "Radius"));
    }

    // Item 1: the radius of a disc shows with perspective and from the top, where its circle faces the view, and not from the
    // front, where its circle is a line. A disc has no edge between two side faces, so its distance has no edge in any view.
    [Theory]
    [InlineData(null, new[] { 4.0 })]
    [InlineData(FlatView.Top, new[] { 4.0 })]
    [InlineData(FlatView.Front, new double[0])]
    public async Task A_Radius_Shows_Where_Its_Circle_Faces_The_View(FlatView? view, double[] expected)
    {
        using var host = new EngineHost(new ManifoldGeometryBackend());
        var sketch = Out(await host.Session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var centre = Out(await host.Session.Apply(new AddSketchPointCommand { SketchId = sketch, X = 0, Y = 0 }), "pointId");
        var circle = Out(await host.Session.Apply(new AddSketchCircleCommand { SketchId = sketch, CenterPointId = centre, Radius = 4 }), "circleId");
        Out(await host.Session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "radius", First = circle, Value = 4 }), "constraintId");
        var body = Out(await host.Session.Apply(new ExtrudeCommand { SketchId = sketch, Distance = 2 }), "bodyId");

        var values = await ValuesOf(host, body, 2, Camera(view));

        Assert.Equal(expected, values.Select(value => value.Number).Order());
    }
}
