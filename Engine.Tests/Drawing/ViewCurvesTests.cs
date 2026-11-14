using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Drawing;
using Engine.Core.Hosting;
using Engine.Geometry.Manifold;
using Engine.Kernel;
using Engine.Tests.Geometry;

namespace Engine.Tests.Drawing;

// ADR-0143 (phase K9b, TASK-0376): the circles of a view from the curves of the design. ADR-0143 names this file in
// enforced-by. The facets of a curved side come from a turn in doubles, so an end of an arc is compared within 1e-9; the
// centre and the radius of a circle come from the design, so they are compared exactly.
public sealed class ViewCurvesTests
{
    private const double Close = 1e-9;

    private static (Rational, Rational)[] Polygon(double radius)
        => [.. CreateCylinderCommandHandler.Circle(radius).Select(p => (Rational.FromDouble(p.X), Rational.FromDouble(p.Y)))];

    // A cylinder of the radius and the height on the z axis, as CreateCylinder makes it, with its curve.
    private static (KernelSolid Solid, FaceTag Tag, SideCurve Curve) Cylinder(Rational radius, Rational height)
    {
        var feature = Guid.NewGuid();
        var curve = new SideCurve(SideCurveKind.Cylinder, new Vector3R(0, 0, -height / 2), new Vector3R(0, 0, height), radius);
        return (KernelSolid.Cylinder(feature, Polygon(radius.ToDouble()), height), new FaceTag(feature, "side"), curve);
    }

    private static IReadOnlyList<ViewLine> Lines(StandardView view, IReadOnlyDictionary<FaceTag, SideCurve> curves, params KernelSolid[] solids)
        => ViewEdges.Of([.. solids.Select(solid => (Guid.NewGuid(), Brep.Of(solid)))], ViewFrame.Of(view), curves);

    private static (double X, double Y) D((Rational X, Rational Y) p) => (p.X.ToDouble(), p.Y.ToDouble());

    private static void Near((double X, double Y) expected, (Rational X, Rational Y) actual)
    {
        var (x, y) = D(actual);
        Assert.True(Math.Abs(x - expected.X) < Close && Math.Abs(y - expected.Y) < Close, $"expected {expected}, actual ({x}, {y})");
    }

    // Item 2: from the top, the ends of a cylinder are one whole circle with the centre and the radius of the design.
    [Fact]
    public void A_Cylinder_From_The_Top_Is_One_Whole_Circle()
    {
        var (solid, tag, curve) = Cylinder(10, 20);
        var line = Assert.Single(Lines(StandardView.Top, new Dictionary<FaceTag, SideCurve> { [tag] = curve }, solid));

        Assert.True(line.Visible);
        Assert.Equal(new ViewCircle((0, 0), 10), line.Circle);
        Assert.Equal(line.From, line.To);
        Near((10, 0), line.From);
    }

    // Item 3: from the front, each end of a cylinder is one line, and the outline is two lines, so the cylinder is one
    // rectangle of four lines.
    [Fact]
    public void A_Cylinder_From_The_Front_Is_Four_Lines()
    {
        var (solid, tag, curve) = Cylinder(10, 20);
        var lines = Lines(StandardView.Front, new Dictionary<FaceTag, SideCurve> { [tag] = curve }, solid);

        Assert.Equal(4, lines.Count);
        Assert.All(lines, line => Assert.True(line.Visible && line.Circle is null));
        foreach (var z in new[] { -10.0, 10.0 })
        {
            var end = Assert.Single(lines, line => line.Kind == ViewLineKind.Edge && D(line.From).Y == z);
            var xs = new[] { D(end.From).X, D(end.To).X }.Order().ToArray();
            Assert.True(Math.Abs(xs[0] + 10) < Close && xs[1] == 10, $"{xs[0]} {xs[1]}");
        }
    }

    // Items 2 and 4: a box over the right half of a cylinder splits its circle into a visible arc and a hidden arc, each
    // counter-clockwise from the place where the edge of the box crosses the circle.
    [Fact]
    public void A_Box_Over_Half_Of_A_Circle_Gives_A_Visible_Arc_And_A_Hidden_Arc()
    {
        var (solid, tag, curve) = Cylinder(10, 20);
        var box = KernelSolid.Box(Guid.NewGuid(), 20, 30, 2).Translate(new Vector3R(10, 0, 20));
        var arcs = Lines(StandardView.Top, new Dictionary<FaceTag, SideCurve> { [tag] = curve }, solid, box).Where(line => line.Circle is not null).ToArray();

        Assert.Equal(2, arcs.Length);
        var visible = Assert.Single(arcs, arc => arc.Visible);
        var hidden = Assert.Single(arcs, arc => !arc.Visible);
        Assert.All(arcs, arc => Assert.Equal(new ViewCircle((0, 0), 10), arc.Circle));
        Near((0, 10), visible.From);
        Near((0, -10), visible.To);
        Near((0, -10), hidden.From);
        Near((0, 10), hidden.To);
        Assert.Equal((Rational.Zero, Rational.Zero), (visible.From.X, visible.To.X));
    }

    // Item 2: a frame of another length scales the view, so the radius of the design is not the radius on it: the facets
    // stay, and no line has a circle.
    [Fact]
    public void A_Frame_Of_Another_Length_Keeps_The_Facets()
    {
        var (solid, tag, curve) = Cylinder(10, 20);
        var frame = new ViewFrame(new(2, 0, 0), new(0, 2, 0), new(0, 0, 1));
        var lines = ViewEdges.Of([(Guid.NewGuid(), Brep.Of(solid))], frame, new Dictionary<FaceTag, SideCurve> { [tag] = curve });

        Assert.True(frame.IsValid);
        Assert.Equal(64, lines.Count);
        Assert.All(lines, line => Assert.Null(line.Circle));
    }

    // Item 2: the outline of a sphere is one whole circle with the centre and the radius of the design.
    [Fact]
    public void The_Outline_Of_A_Sphere_Is_One_Whole_Circle()
    {
        var feature = Guid.NewGuid();
        var (positions, triangles) = CreateSphereCommandHandler.Mesh(10);
        var vertices = Enumerable.Range(0, positions.Length / 3).Select(i => Vector3R.FromDoubles(positions[3 * i], positions[(3 * i) + 1], positions[(3 * i) + 2])).ToArray();
        var sphere = KernelSolid.Mesh(feature, "surface", vertices, triangles).Translate(new Vector3R(5, 0, 0));
        var curves = new Dictionary<FaceTag, SideCurve> { [new FaceTag(feature, "surface")] = new(SideCurveKind.Sphere, new Vector3R(5, 0, 0), new Vector3R(0, 0, 0), 10) };

        foreach (var view in new[] { StandardView.Front, StandardView.Top })
        {
            var line = Assert.Single(Lines(view, curves, sphere));
            Assert.Equal(new ViewCircle((5, 0), 10), line.Circle);
            Assert.True(line.Visible);
            Assert.Equal(line.From, line.To);
        }
    }

    // Item 1: the engine gives the curves of the design through an extrude of a circle, a cylinder, a translate and a
    // subtract: the plate shows a circle of the radius of its sketch and a hole of the radius of the cylinder.
    [NativeManifoldFact]
    public async Task The_Engine_Gives_The_Circles_Of_The_Design()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var sketch = await Output(session, new CreateSketchCommand { DatumId = Datums.XY.Id }, "sketchId");
        var centre = await Output(session, new AddSketchPointCommand { SketchId = sketch, X = 20, Y = 0 }, "pointId");
        await Output(session, new AddSketchCircleCommand { SketchId = sketch, CenterPointId = centre, Radius = 7 }, "circleId");
        var disc = await Output(session, new ExtrudeCommand { SketchId = sketch, Distance = 5 }, "bodyId");
        var tool = await Output(session, new CreateCylinderCommand { Radius = 3, Height = 20 }, "bodyId");
        var moved = await Output(session, new TranslateCommand { BodyId = tool, Dx = 20, Dy = 0, Dz = 0 }, "bodyId");
        await Output(session, new SubtractCommand { MinuendBodyId = disc, SubtrahendBodyId = moved }, "bodyId");

        var (top, skipped) = await session.ReadGeometry((document, _) => ViewEdges.Of(document, ViewFrame.Of(StandardView.Top)));
        var (front, _) = await session.ReadGeometry((document, _) => ViewEdges.Of(document, ViewFrame.Of(StandardView.Front)));

        Assert.Empty(skipped);
        Assert.Equal(new[] { new ViewCircle((20, 0), 3), new ViewCircle((20, 0), 7) }, top.Select(line => line.Circle).OrderBy(circle => circle!.Radius));
        Assert.All(top, line => Assert.True(line.Visible && line.From == line.To));

        // From the front the disc is a rectangle, and the walls of the hole are two hidden lines.
        Assert.Equal(6, front.Count);
        Assert.Equal(2, front.Count(line => !line.Visible));
    }

    private static async Task<Guid> Output(DocumentSession session, Command command, string name)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Outputs.TryGet<Guid>(name, out var id) ? id : throw new InvalidOperationException($"No output {name}.");
    }
}
