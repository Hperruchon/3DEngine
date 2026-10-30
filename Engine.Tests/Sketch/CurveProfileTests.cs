using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Hosting;
using Engine.Core.Queries;
using Engine.Core.Sketch;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.Sketch;

// The extrude of a profile with arcs and circles (ADR-0040, TASK-0126). ADR-0040 names this file in
// enforced-by: the polygon of a curve with + - * / sqrt only, its bits on each platform, the loop of
// lines and arcs in each direction of the walk, the refusals, and an extrude and a rebuild of a
// circle on the native backend.
public sealed class CurveProfileTests
{
    private static readonly Guid SketchId = Guid.Parse("5c000000-0000-0000-0000-0000000000c0");

    private static Guid Id(int n) => new($"00000000-0000-0000-0000-{n:D12}");

    // Point n has the identifier Id(n + 1), line n Id(100 + n), circle n Id(300 + n) and arc n
    // Id(400 + n).
    private static SketchRecord Record((double X, double Y)[] points, (int Start, int End)[] lines, (int Centre, double Radius)[] circles, (int Centre, int Start, int End)[] arcs)
    {
        var record = SketchModel.New(SketchId, Datums.XY.Id, null);
        return record with
        {
            Points = [.. record.Points, .. points.Select((p, n) => new SketchPoint(Id(n + 1), p.X, p.Y))],
            Lines = [.. lines.Select((l, n) => new SketchLine(Id(100 + n), Id(l.Start + 1), Id(l.End + 1)))],
            Circles = [.. circles.Select((c, n) => new SketchCircle(Id(300 + n), Id(c.Centre + 1), c.Radius))],
            Arcs = [.. arcs.Select((a, n) => new SketchArc(Id(400 + n), Id(a.Centre + 1), Id(a.Start + 1), Id(a.End + 1)))],
        };
    }

    private static double TwiceArea(IReadOnlyList<(double X, double Y)> loop)
        => Enumerable.Range(0, loop.Count).Sum(i => (loop[i].X * loop[(i + 1) % loop.Count].Y) - (loop[(i + 1) % loop.Count].X * loop[i].Y));

    // ADR-0040 item 2: an arc has the power of two of segments that gives a step of at most about
    // 2π / 64, and a full turn has 64. Each vertex is on the curve.
    [Theory]
    [InlineData(0.0, 1.0, 16)]
    [InlineData(-1.0, 0.0, 32)]
    [InlineData(0.0, -1.0, 64)]
    [InlineData(0.98480775301220806, 0.17364817766693033, 2)]
    [InlineData(0.99984769515639127, 0.017452406437283512, 1)]
    public void An_Arc_Has_The_Segments_Of_Its_Sweep(double endX, double endY, int segments)
    {
        var vertices = SketchProfile.Turn((2, 3), (12, 3), (2 + (10 * endX), 3 + (10 * endY)));

        Assert.Equal(segments, vertices.Count);
        Assert.All(vertices, v => Assert.Equal(10, Math.Sqrt(((v.X - 2) * (v.X - 2)) + ((v.Y - 3) * (v.Y - 3))), 12));
        Assert.All(vertices.Skip(1), v => Assert.True(v.Y > 3 || endY < 0, $"({v.X}, {v.Y}) turns clockwise"));
    }

    [Fact]
    public void A_Full_Turn_Has_64_Segments_Counter_Clockwise()
    {
        var vertices = SketchProfile.Turn((0, 0), (5, 0), null);

        Assert.Equal(64, vertices.Count);
        Assert.True(TwiceArea(vertices) > 0);
        Assert.Equal(0, vertices[16].X, 12);
        Assert.Equal(5, vertices[16].Y, 12);
    }

    // ADR-0040 item 3: the polygon has the same bits on Windows, Linux and macOS, as the first run on
    // Windows x64 gave them on 2026-10-31. The hash is FNV-1a of the bits of each coordinate.
    [Fact]
    public void A_Polygon_Of_A_Curve_Has_The_Same_Bits_On_Each_Platform()
    {
        var arc = SketchProfile.Turn((1.25, -0.5), (7.5, 2.25), (-3.0, 4.75));
        var circle = SketchProfile.Turn((0.3, 0.7), (4.1, 0.7), null);

        var hash = 14695981039346656037UL;
        foreach (var (x, y) in arc.Concat(circle))
        {
            foreach (var bits in new[] { BitConverter.DoubleToInt64Bits(x), BitConverter.DoubleToInt64Bits(y) })
            {
                hash ^= (ulong)bits;
                hash *= 1099511628211UL;
            }
        }

        Assert.Equal((32, 64), (arc.Count, circle.Count));
        Assert.True(hash == 0x5CA7A38157BBDA5EUL, $"The hash of the bits: 0x{hash:X16}");
    }

    // ADR-0040 item 1: a line and an arc form a loop. The walk meets the arc at its start or at its
    // end, and the loop turns counter-clockwise in both cases.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_Line_And_An_Arc_Form_A_Loop_In_Each_Direction_Of_The_Walk(bool reversed)
    {
        // The line from (−5, 0) to (5, 0), and the half circle about (0, 0) above it from (5, 0) to
        // (−5, 0), or below it from (−5, 0) to (5, 0).
        var sketch = Record([(-5, 0), (5, 0), (0, 0)], [(0, 1)], [], [reversed ? (2, 0, 1) : (2, 1, 0)]);

        var (loop, reason) = SketchProfile.Find(sketch);

        Assert.Null(reason);
        Assert.Equal(33, loop!.Count);
        Assert.True(TwiceArea(loop) > 0);
        Assert.All(loop, v => Assert.Equal(5, Math.Sqrt((v.X * v.X) + (v.Y * v.Y)), 12));
        Assert.All(loop, v => Assert.True(reversed ? v.Y <= 1e-12 : v.Y >= -1e-12, $"({v.X}, {v.Y})"));
    }

    // ADR-0040 items 1 and 5: each refusal names its reason.
    [Fact]
    public void A_Profile_That_Breaks_A_Rule_Of_A_Curve_Names_Its_Reason()
    {
        var circleAndLine = Record([(0, 0), (9, 0), (9, 5)], [(1, 2)], [(0, 2)], []);
        var twoCircles = Record([(0, 0), (9, 0)], [], [(0, 2), (1, 2)], []);
        var atCentre = Record([(0, 0), (0, 0), (5, 0)], [(1, 2)], [], [(0, 1, 2)]);
        var crossing = Record([(-5, 0), (5, 0), (0, 0), (0, -1), (0, 9)], [(0, 1), (3, 4), (4, 3)], [], [(2, 1, 0)]);

        Assert.Contains("a profile alone", SketchProfile.Find(circleAndLine).Reason);
        Assert.Contains("a profile alone", SketchProfile.Find(twoCircles).Reason);
        Assert.Contains("the radius zero", SketchProfile.Find(atCentre).Reason);
        Assert.Contains("edges", SketchProfile.Find(crossing).Reason);
    }

    // ADR-0040 items 4 and 6: an extrude of a circle on the native backend, and an edit of its radius
    // rebuilds the solid.
    [NativeManifoldFact]
    public async Task A_Circle_Extrudes_And_A_Change_Of_Its_Radius_Rebuilds_The_Solid()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var sketch = (Guid)(await session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id })).Outputs.Values["sketchId"]!;
        var centre = (Guid)(await session.Apply(new AddSketchPointCommand { SketchId = sketch, X = 0, Y = 0 })).Outputs.Values["pointId"]!;
        var circle = (Guid)(await session.Apply(new AddSketchCircleCommand { SketchId = sketch, CenterPointId = centre, Radius = 4 })).Outputs.Values["circleId"]!;
        var radius = (Guid)(await session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "radius", First = circle, Value = 4 })).Outputs.Values["constraintId"]!;

        var extrude = await session.Apply(new ExtrudeCommand { SketchId = sketch, Distance = 3 });
        Assert.True(extrude.Status == CommandStatus.Applied, $"{extrude.Error?.Code} {extrude.Error?.Message}");
        var body = (Guid)extrude.Outputs.Values["bodyId"]!;
        var before = (await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = body })).Result;

        Assert.Equal(CommandStatus.Applied, (await session.Apply(new SetSketchConstraintValueCommand { SketchId = sketch, ConstraintId = radius, Value = 6 })).Status);
        var after = (await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = body })).Result;
        var record = await session.Read((document, _) => document.Bodies.Single(item => item.Handle.Id == body));

        // The vertex half a turn from the start comes after 32 turns of the step, so it holds the
        // rounding of each; the comparison is to 12 digits.
        foreach (var (expected, actual) in new[] { (-4.0, before.MinX), (4.0, before.MaxX), (0.0, before.MinZ), (3.0, before.MaxZ), (-6.0, after.MinX), (6.0, after.MaxX), (6.0, after.MaxY) })
            Assert.Equal(expected, actual, 12);
        Assert.Null(record.Fault);
    }
}
