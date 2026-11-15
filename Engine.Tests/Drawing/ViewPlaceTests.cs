using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Drawing;
using Engine.Core.Hosting;
using Engine.Geometry.Manifold;
using Engine.Kernel;
using Engine.Tests.Geometry;

namespace Engine.Tests.Drawing;

// ADR-0145 (phase P0.151, TASK-0379): the exact place of a line along the axis of a curved side, for the findings E202 and
// E203 of the codebase review of 2026-11-14 (e). ADR-0145 names this file in enforced-by. The ends of an outline come from
// doubles, so the tests compare them within 1e-9.
public sealed class ViewPlaceTests
{
    private const double Close = 1e-9;

    private static (Rational, Rational)[] Polygon(double radius)
        => [.. CreateCylinderCommandHandler.Circle(radius).Select(p => (Rational.FromDouble(p.X), Rational.FromDouble(p.Y)))];

    private static void Near((double X, double Y) expected, (Rational X, Rational Y) actual)
    {
        var (x, y) = (actual.X.ToDouble(), actual.Y.ToDouble());
        Assert.True(Math.Abs(x - expected.X) < Close && Math.Abs(y - expected.Y) < Close, $"expected {expected}, actual ({x}, {y})");
    }

    private static (Rational X, Rational Y)[] Ends(ViewLine line) => [.. new[] { line.From, line.To }.OrderBy(p => p.Y)];

    // E202: the outline of a hole through a plate of 5 mm goes from face to face of the plate, not to the ends of the
    // cylinder of 10 mm that cut it.
    [Fact]
    public void The_Outline_Of_A_Hole_Goes_From_Face_To_Face()
    {
        var feature = Guid.NewGuid();
        var tool = KernelSolid.Cylinder(feature, Polygon(3), 10);
        var plate = PlanarBoolean.Subtract(KernelSolid.Box(Guid.NewGuid(), 20, 20, 5), tool)!;
        var curves = new Dictionary<FaceTag, SideCurve> { [new FaceTag(feature, "side")] = new(SideCurveKind.Cylinder, new Vector3R(0, 0, -5), new Vector3R(0, 0, 10), 3) };
        var lines = ViewEdges.Of([(Guid.NewGuid(), Brep.Of(plate))], ViewFrame.Of(StandardView.Front), curves);

        var outlines = lines.Where(line => line.Kind == ViewLineKind.Outline).OrderBy(line => line.From.X).ToArray();
        Assert.Equal(2, outlines.Length);
        foreach (var (line, x) in outlines.Zip(new[] { -3.0, 3.0 }))
        {
            Assert.False(line.Visible);
            Near((x, -2.5), Ends(line)[0]);
            Near((x, 2.5), Ends(line)[1]);
        }
    }

    // E202: the outline of a cone that a box cut at the middle of its height goes from its base to the cut, at the half of its
    // radius, not to the apex.
    [Fact]
    public void The_Outline_Of_A_Cut_Cone_Ends_At_The_Cut()
    {
        var feature = Guid.NewGuid();
        var cone = KernelSolid.Cone(feature, Polygon(10), 20);
        var frustum = PlanarBoolean.Subtract(cone, KernelSolid.Box(Guid.NewGuid(), 40, 40, 20).Translate(new Vector3R(0, 0, 10)))!;
        var curves = new Dictionary<FaceTag, SideCurve> { [new FaceTag(feature, "side")] = new(SideCurveKind.Cone, new Vector3R(0, 0, -10), new Vector3R(0, 0, 20), 10) };
        var lines = ViewEdges.Of([(Guid.NewGuid(), Brep.Of(frustum))], ViewFrame.Of(StandardView.Front), curves);

        var outlines = lines.Where(line => line.Kind == ViewLineKind.Outline).OrderBy(line => line.From.X + line.To.X).ToArray();
        Assert.Equal(2, outlines.Length);
        foreach (var (line, sign) in outlines.Zip(new[] { -1.0, 1.0 }))
        {
            Assert.True(line.Visible);
            Near((sign * 10, -10), Ends(line)[0]);
            Near((sign * 5, 0), Ends(line)[1]);
        }
    }

    // E204: from the top, a cone that a box cut at the middle of its height is the whole circle of its base, radius 10, and
    // the whole circle of its cut, radius 5, at the place of the cut along the axis.
    [Fact]
    public void A_Cut_Cone_From_The_Top_Shows_The_Circle_Of_Its_Cut()
    {
        var feature = Guid.NewGuid();
        var cone = KernelSolid.Cone(feature, Polygon(10), 20);
        var frustum = PlanarBoolean.Subtract(cone, KernelSolid.Box(Guid.NewGuid(), 40, 40, 20).Translate(new Vector3R(0, 0, 10)))!;
        var curves = new Dictionary<FaceTag, SideCurve> { [new FaceTag(feature, "side")] = new(SideCurveKind.Cone, new Vector3R(0, 0, -10), new Vector3R(0, 0, 20), 10) };
        var lines = ViewEdges.Of([(Guid.NewGuid(), Brep.Of(frustum))], ViewFrame.Of(StandardView.Top), curves);

        Assert.Equal(2, lines.Count);
        Assert.All(lines, line => Assert.True(line.Visible && line.From == line.To));
        Assert.Equal(new[] { new ViewCircle((0, 0), 5), new ViewCircle((0, 0), 10) }, lines.Select(line => line.Circle).OrderBy(circle => circle!.Radius));
    }

    // E203: a cylinder that a Rotate of 180 degrees about x turned is along the view from the top within the rounding of the
    // turn, so it is one whole circle, at the origin and moved, and the view does not throw.
    [NativeManifoldTheory]
    [InlineData(0, 0)]
    [InlineData(100, 50)]
    public async Task A_Cylinder_Turned_By_180_Degrees_Is_One_Circle_From_The_Top(double dx, double dy)
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var body = await Body(session, new CreateCylinderCommand { Radius = 10, Height = 20 });
        body = await Body(session, new RotateCommand { BodyId = body, Qw = Math.Cos(Math.PI / 2), Qx = Math.Sin(Math.PI / 2), Qy = 0, Qz = 0 });
        if (dx != 0 || dy != 0)
            await Body(session, new TranslateCommand { BodyId = body, Dx = dx, Dy = dy, Dz = 0 });

        var (lines, skipped) = await session.ReadGeometry((document, _) => ViewEdges.Of(document, ViewFrame.Of(StandardView.Top)));

        Assert.Empty(skipped);
        var line = Assert.Single(lines);
        Assert.True(line.Visible);
        Assert.NotNull(line.Circle);
        Assert.Equal(line.From, line.To);
        Assert.Equal((Rational)10, line.Circle!.Radius);
        Near((dx, dy), line.Circle.Centre);
    }

    // ADR-0147 item 1 (finding E205 of the codebase review of 2026-11-14 (e), TASK-0380): a cylinder that turns of 30 and 60
    // degrees about x laid on its side is across the top view within the rounding of the turns, so each end is one visible
    // line from x = -10 to 10, with no hidden half under it and no ellipse; the outline is two lines.
    [NativeManifoldFact]
    public async Task A_Cylinder_That_Two_Turns_Laid_On_Its_Side_Shows_Each_End_As_One_Line()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var body = await Body(session, new CreateCylinderCommand { Radius = 10, Height = 20 });
        body = await Body(session, new RotateCommand { BodyId = body, Qw = Math.Cos(Math.PI / 12), Qx = Math.Sin(Math.PI / 12), Qy = 0, Qz = 0 });
        await Body(session, new RotateCommand { BodyId = body, Qw = Math.Cos(Math.PI / 6), Qx = Math.Sin(Math.PI / 6), Qy = 0, Qz = 0 });

        var (lines, _) = await session.ReadGeometry((document, _) => ViewEdges.Of(document, ViewFrame.Of(StandardView.Top)));

        Assert.Equal(4, lines.Count);
        Assert.All(lines, line => Assert.True(line.Visible && line.Ellipse is null && line.Circle is null));
        var ends = lines.Where(line => line.Kind == ViewLineKind.Edge).ToArray();
        Assert.Equal(2, ends.Length);
        foreach (var end in ends)
        {
            var xs = new[] { end.From.X.ToDouble(), end.To.X.ToDouble() }.Order().ToArray();
            Assert.True(Math.Abs(xs[0] + 10) < Close && Math.Abs(xs[1] - 10) < Close, $"an end runs from {xs[0]} to {xs[1]}");
        }
    }

    private static async Task<Guid> Body(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Outputs.TryGet<Guid>("bodyId", out var id) ? id : throw new InvalidOperationException("No output bodyId.");
    }
}
