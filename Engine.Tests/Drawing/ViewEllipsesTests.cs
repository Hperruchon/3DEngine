using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Drawing;
using Engine.Core.Hosting;
using Engine.Geometry.Manifold;
using Engine.Kernel;
using Engine.Tests.Geometry;

namespace Engine.Tests.Drawing;

// ADR-0144 (phase K9c, TASK-0377): the ellipse of an end at a slant to the view, and the outline of a cylinder and a cone
// from its axis. ADR-0144 names this file in enforced-by. The half axes and the outline come from doubles with + - * / and
// the square root, so the tests compare them within 1e-9.
public sealed class ViewEllipsesTests
{
    private const double Close = 1e-9;

    // The exact turn about x with the cosine 3/5 and the sine 4/5: the images of the axes x, y and z.
    private static readonly (Vector3R X, Vector3R Y, Vector3R Z) Turn =
        (new Vector3R(1, 0, 0), new Vector3R(0, new Rational(3, 5), new Rational(4, 5)), new Vector3R(0, new Rational(-4, 5), new Rational(3, 5)));

    private static (Rational, Rational)[] Polygon(double radius)
        => [.. CreateCylinderCommandHandler.Circle(radius).Select(p => (Rational.FromDouble(p.X), Rational.FromDouble(p.Y)))];

    private static Vector3R Map(Vector3R q) => (Turn.X * q.X) + (Turn.Y * q.Y) + (Turn.Z * q.Z);

    // A cylinder or a cone of radius 10 and height 20 on the z axis, turned by Turn, with its curve turned the same way.
    private static IReadOnlyList<ViewLine> Turned(SideCurveKind kind, StandardView view)
    {
        var feature = Guid.NewGuid();
        var solid = kind == SideCurveKind.Cylinder ? KernelSolid.Cylinder(feature, Polygon(10), 20) : KernelSolid.Cone(feature, Polygon(10), 20);
        var curve = new SideCurve(kind, Map(new Vector3R(0, 0, -10)), Map(new Vector3R(0, 0, 20)), 10);
        return ViewEdges.Of([(Guid.NewGuid(), Brep.Of(solid.Rotate(Turn.X, Turn.Y, Turn.Z)))], ViewFrame.Of(view),
            new Dictionary<FaceTag, SideCurve> { [new FaceTag(feature, "side")] = curve });
    }

    private static void Near((double X, double Y) expected, (Rational X, Rational Y) actual)
    {
        var (x, y) = (actual.X.ToDouble(), actual.Y.ToDouble());
        Assert.True(Math.Abs(x - expected.X) < Close && Math.Abs(y - expected.Y) < Close, $"expected {expected}, actual ({x}, {y})");
    }

    private static void Near((double X, double Y) expected, (double X, double Y) actual)
        => Assert.True(Math.Abs(actual.X - expected.X) < Close && Math.Abs(actual.Y - expected.Y) < Close, $"expected {expected}, actual {actual}");

    // Item 1: a cylinder at a slant shows each end as an ellipse with the radius of the design as its larger half axis:
    // the near end whole, and the far end as a visible arc outside the side and a hidden arc behind it.
    [Fact]
    public void Each_End_Of_A_Cylinder_At_A_Slant_Is_An_Ellipse()
    {
        var lines = Turned(SideCurveKind.Cylinder, StandardView.Front);
        var arcs = lines.Where(line => line.Ellipse is not null).ToArray();

        Assert.Equal(5, lines.Count);
        Assert.Equal(3, arcs.Length);
        Assert.All(arcs, arc =>
        {
            Near((10, 0), (Math.Abs(arc.Ellipse!.Major.X), Math.Abs(arc.Ellipse.Major.Y)));
            Near((0, 8), (Math.Abs(arc.Ellipse.Minor.X), Math.Abs(arc.Ellipse.Minor.Y)));
        });

        var near = Assert.Single(arcs, arc => arc.Ellipse!.Centre == (0, 6));
        Assert.True(near.Visible);
        Assert.Equal(near.From, near.To);

        var visible = Assert.Single(arcs, arc => arc.Ellipse!.Centre == (0, -6) && arc.Visible);
        var hidden = Assert.Single(arcs, arc => arc.Ellipse!.Centre == (0, -6) && !arc.Visible);
        Near((-10, -6), visible.From);
        Near((10, -6), visible.To);
        Near((10, -6), hidden.From);
        Near((-10, -6), hidden.To);
    }

    // Item 2: the outline of a cylinder at a slant is two lines at the radius of the design from its axis, from one end to
    // the other.
    [Fact]
    public void The_Outline_Of_A_Cylinder_At_A_Slant_Is_At_Its_Radius()
    {
        var outlines = Turned(SideCurveKind.Cylinder, StandardView.Front).Where(line => line.Kind == ViewLineKind.Outline)
            .OrderBy(line => line.From.X).ToArray();

        Assert.Equal(2, outlines.Length);
        Assert.All(outlines, line => Assert.True(line.Visible && line.Ellipse is null && line.Circle is null));
        var ends = outlines.Select(line => new[] { line.From, line.To }.OrderBy(p => p.Y).ToArray()).ToArray();
        Near((-10, -6), ends[0][0]);
        Near((-10, 6), ends[0][1]);
        Near((10, -6), ends[1][0]);
        Near((10, 6), ends[1][1]);
    }

    // Item 2: the outline of a cone at a slant is two lines from its apex to the points where a tangent from the apex
    // meets the ellipse of its base: (±10 √5 / 3, -2 / 3) for the base at (0, -6) with the half axes 10 and 8 and the apex
    // at (0, 6).
    [Fact]
    public void The_Outline_Of_A_Cone_At_A_Slant_Is_Tangent_To_Its_Base()
    {
        var lines = Turned(SideCurveKind.Cone, StandardView.Front);
        var outlines = lines.Where(line => line.Kind == ViewLineKind.Outline).ToArray();
        var x = 10 * Math.Sqrt(5) / 3;

        Assert.Equal(2, outlines.Length);
        foreach (var sign in new[] { -1.0, 1.0 })
        {
            var line = Assert.Single(outlines, line => Math.Sign(line.From.X.ToDouble() + line.To.X.ToDouble()) == sign);
            Assert.True(line.Visible);
            var ends = new[] { line.From, line.To }.OrderByDescending(p => p.Y).ToArray();
            Near((0, 6), ends[0]);
            Near((sign * x, -2.0 / 3), ends[1]);
        }

        Assert.All(lines.Where(line => line.Kind == ViewLineKind.Edge), line => Assert.Equal((Rational)0, line.Ellipse!.Centre.X));
    }

    // Item 2: with a turn about x and then about z, the cone has no symmetry on the view. In each view where the apex is
    // outside the ellipse of the base, each line of the outline ends on that ellipse, and the line from the apex touches it
    // there: in the coordinates of the ellipse the end T and the apex q give |T|² = 1 and T · q = 1.
    [Fact]
    public void The_Outline_Of_A_Cone_Touches_Its_Base_In_Each_View()
    {
        (Vector3R X, Vector3R Y, Vector3R Z) turn = (
            new Vector3R(new Rational(3, 5), new Rational(4, 5), 0),
            new Vector3R(new Rational(-12, 25), new Rational(9, 25), new Rational(4, 5)),
            new Vector3R(new Rational(16, 25), new Rational(-12, 25), new Rational(3, 5)));
        Vector3R Turned(Vector3R q) => (turn.X * q.X) + (turn.Y * q.Y) + (turn.Z * q.Z);
        var feature = Guid.NewGuid();
        var solid = KernelSolid.Cone(feature, Polygon(10), 20).Rotate(turn.X, turn.Y, turn.Z);
        var curve = new SideCurve(SideCurveKind.Cone, Turned(new Vector3R(0, 0, -10)), Turned(new Vector3R(0, 0, 20)), 10);
        var checkedViews = 0;
        foreach (var view in new[] { StandardView.Front, StandardView.Top, StandardView.Right })
        {
            var frame = ViewFrame.Of(view);
            var lines = ViewEdges.Of([(Guid.NewGuid(), Brep.Of(solid))], frame, new Dictionary<FaceTag, SideCurve> { [new FaceTag(feature, "side")] = curve });
            var ellipse = lines.First(line => line.Ellipse is not null).Ellipse!;
            var (cx, cy) = (ellipse.Centre.X.ToDouble(), ellipse.Centre.Y.ToDouble());
            var (mm, nn) = ((ellipse.Major.X * ellipse.Major.X) + (ellipse.Major.Y * ellipse.Major.Y), (ellipse.Minor.X * ellipse.Minor.X) + (ellipse.Minor.Y * ellipse.Minor.Y));
            (double A, double B) Coordinates(double x, double y)
                => ((((x - cx) * ellipse.Major.X) + ((y - cy) * ellipse.Major.Y)) / mm, (((x - cx) * ellipse.Minor.X) + ((y - cy) * ellipse.Minor.Y)) / nn);
            var apex = curve.Base + curve.Axis;
            var (ax, ay) = (apex.Dot(frame.Right).ToDouble(), apex.Dot(frame.Up).ToDouble());
            var q = Coordinates(ax, ay);
            if ((q.A * q.A) + (q.B * q.B) <= 1)
                continue;
            checkedViews++;

            var outlines = lines.Where(line => line.Kind == ViewLineKind.Outline).ToArray();
            Assert.Equal(2, outlines.Length);
            foreach (var line in outlines)
            {
                var ends = new[] { line.From, line.To }.Select(p => (X: p.X.ToDouble(), Y: p.Y.ToDouble()))
                    .OrderBy(p => ((p.X - ax) * (p.X - ax)) + ((p.Y - ay) * (p.Y - ay))).ToArray();
                Near((ax, ay), ends[0]);
                var end = Coordinates(ends[1].X, ends[1].Y);
                Near((1, 1), ((end.A * end.A) + (end.B * end.B), (end.A * q.A) + (end.B * q.B)));
            }
        }

        Assert.Equal(3, checkedViews);
    }

    // Item 1: the engine gives the ellipses of a cylinder turned by a quaternion in doubles: the turn is not exactly along
    // the view, so each end is an ellipse, and its centres and half axes are those of the exact turn within the rounding.
    [NativeManifoldFact]
    public async Task The_Engine_Gives_The_Ellipses_Of_A_Turned_Cylinder()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var cylinder = await Body(session, new CreateCylinderCommand { Radius = 10, Height = 20 });
        await Body(session, new RotateCommand { BodyId = cylinder, Qw = Math.Sqrt(0.8), Qx = Math.Sqrt(0.2), Qy = 0, Qz = 0 });

        var (lines, skipped) = await session.ReadGeometry((document, _) => ViewEdges.Of(document, ViewFrame.Of(StandardView.Front)));

        Assert.Empty(skipped);
        var arcs = lines.Where(line => line.Ellipse is not null).ToArray();
        Assert.NotEmpty(arcs);
        Assert.All(arcs, arc =>
        {
            var centre = (arc.Ellipse!.Centre.X.ToDouble(), Math.Abs(arc.Ellipse.Centre.Y.ToDouble()));
            Near((0, 6), centre);
            Near((10, 8), (Math.Sqrt((arc.Ellipse.Major.X * arc.Ellipse.Major.X) + (arc.Ellipse.Major.Y * arc.Ellipse.Major.Y)),
                Math.Sqrt((arc.Ellipse.Minor.X * arc.Ellipse.Minor.X) + (arc.Ellipse.Minor.Y * arc.Ellipse.Minor.Y))));
        });
        Assert.Equal(2, lines.Count(line => line.Kind == ViewLineKind.Outline));
    }

    private static async Task<Guid> Body(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Outputs.TryGet<Guid>("bodyId", out var id) ? id : throw new InvalidOperationException("No output bodyId.");
    }
}
