using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core.Exact;
using Engine.Core.Sketch;

namespace Engine.Tests.Exact;

// The exact predicates and the profile of a sketch that uses them (ADR-0050, phase K1, TASK-0164).
// ADR-0050 names this file in enforced-by. The cases where the rounded value is wrong came from a
// search with exact BigInt arithmetic in Node.js on 2026-11-02, and each test computes the rounded
// value again to show the defect.
public sealed class PredicateTests
{
    // The rounded orient2d, as SketchProfile computed it before TASK-0164.
    private static double Rounded((double X, double Y) a, (double X, double Y) b, (double X, double Y) c)
        => ((b.X - a.X) * (c.Y - a.Y)) - ((b.Y - a.Y) * (c.X - a.X));

    [Theory]
    [InlineData(0, 0, 10, 0, 5, 1, 1)]
    [InlineData(0, 0, 10, 0, 5, -1, -1)]
    [InlineData(0, 0, 10, 0, 20, 0, 0)]
    [InlineData(1, 1, 1, 1, 7, -3, 0)]
    public void Orient2D_Gives_The_Side_Of_A_Point(double ax, double ay, double bx, double by, double cx, double cy, int sign)
        => Assert.Equal(sign, Predicates.Orient2D((ax, ay), (bx, by), (cx, cy)));

    // Three points on the line y = 3x: each x has fewer than 51 bits, so each 3x is exact and the
    // points are on one line exactly. The differences round, and the rounded value is not zero.
    [Theory]
    [InlineData(80135507.250122070, 0.033893249929008107, 88104.687500119209)]
    [InlineData(12998518.000015259, 4393.5000038146973, 0.0067713372409912154)]
    [InlineData(9098.9687500298023, 0.59290599823088996, 0.090136408805960855)]
    public void Three_Points_On_One_Line_Give_Zero(double x1, double x2, double x3)
    {
        (double X, double Y) a = (x1, 3 * x1), b = (x2, 3 * x2), c = (x3, 3 * x3);

        Assert.NotEqual(0, Rounded(a, b, c));
        Assert.Equal(0, Predicates.Orient2D(a, b, c));
    }

    // A point right of the line y = x by less than one unit in the last place of 0.5: the rounded
    // value is zero.
    [Fact]
    public void A_Point_Near_A_Line_Gives_Its_True_Side()
    {
        (double X, double Y) a = (12, 12), b = (24, 24), c = (0.50000000000173828, 0.50000000000173661);

        Assert.Equal(0, Rounded(a, b, c));
        Assert.Equal(-1, Predicates.Orient2D(a, b, c));
    }

    // ADR-0050 item 2: an underflow and an overflow of the rounded value go to the exact path.
    [Fact]
    public void An_Underflow_And_An_Overflow_Give_The_True_Side()
    {
        Assert.Equal(0, Rounded((0, 0), (1e-200, 0), (0, 1e-200)));
        Assert.Equal(1, Predicates.Orient2D((0, 0), (1e-200, 0), (0, 1e-200)));
        Assert.Equal(1, Predicates.Orient2D((0, 0), (double.Epsilon, 0), (0, double.Epsilon)));
        Assert.True(double.IsPositiveInfinity(Rounded((-1e308, 0), (1e308, 0), (0, 1e308))));
        Assert.Equal(1, Predicates.Orient2D((-1e308, 0), (1e308, 0), (0, 1e308)));
        Assert.Equal(-1, Predicates.Orient2D((-1e308, 0), (1e308, 0), (0, -1e308)));
    }

    // ADR-0050 item 2: the two products are subnormal, so the bound 4ε times the permanent underflows
    // to zero, and each difference in X rounds. The rounded value has the wrong sign, and only the
    // smallest permanent sends it to the exact path.
    [Theory]
    [InlineData(-1.0291489853153120e-161, 1.0239626593450114e-161, 3.6198004425175312e-162, 1.2229318367730140e-161, 3.9705990551377381e-162, -1)]
    [InlineData(-1.4292922381796613e-161, 1.0410301850559791e-161, 3.7703401246674702e-162, 1.2570292619632033e-161, 4.1000096361831979e-162, 1)]
    [InlineData(-1.5565610465094406e-161, 1.0307169928105980e-161, 6.5399632184888557e-162, 1.3519457339435486e-161, 7.3519455875283627e-162, 1)]
    public void Subnormal_Products_Give_The_True_Side(double ax, double bx, double by, double cx, double cy, int sign)
    {
        (double X, double Y) a = (ax, 0), b = (bx, by), c = (cx, cy);

        Assert.Equal(-sign, Math.Sign(Rounded(a, b, c)));
        Assert.Equal(sign, Predicates.Orient2D(a, b, c));
        Assert.Equal(sign, Predicates.Orient2DExact(a, b, c));
    }

    [Fact]
    public void A_Coordinate_That_Is_Not_Finite_Is_Refused()
    {
        Assert.Throws<ArgumentException>(() => Predicates.Orient2D((0, 0), (double.PositiveInfinity, 0), (0, 1)));
        Assert.Throws<ArgumentException>(() => Predicates.Orient2D((0, 0), (1, 0), (double.NaN, 1)));
        Assert.Throws<ArgumentException>(() => Predicates.AreaSign([(0, 0), (1, 0), (0, double.NegativeInfinity)]));
    }

    // ADR-0050 item 2: the filter never gives a sign that the exact path does not give. The points are
    // near one line, one or two units in the last place from it, over a wide range of exponents, so
    // most cases are near the bound of the filter.
    [Fact]
    public void The_Filter_Agrees_With_The_Exact_Path()
    {
        var random = new Random(1164);
        var exactCases = 0;
        for (var i = 0; i < 200_000; i++)
        {
            var scale = Math.ScaleB(1, random.Next(-1000, 1000));
            var slope = random.Next(-5, 6);
            (double X, double Y) Near(double x)
            {
                var y = slope * x;
                for (var k = random.Next(0, 3); k > 0; k--)
                    y = random.Next(2) == 0 ? Math.BitIncrement(y) : Math.BitDecrement(y);
                return (x, y);
            }

            var a = Near(scale * random.NextDouble());
            var b = Near(scale * (1 + random.NextDouble()));
            var c = Near(scale * random.NextDouble() * 4);
            var exact = Predicates.Orient2DExact(a, b, c);
            Assert.Equal(exact, Predicates.Orient2D(a, b, c));
            if (exact == 0)
                exactCases++;
        }

        Assert.True(exactCases > 1000, $"Only {exactCases} cases are on one line exactly.");
    }

    [Fact]
    public void AreaSign_Gives_The_Turn_Of_A_Loop()
    {
        Assert.Equal(1, Predicates.AreaSign([(0, 0), (4, 0), (4, 3), (0, 3)]));
        Assert.Equal(-1, Predicates.AreaSign([(0, 0), (0, 3), (4, 3), (4, 0)]));
        Assert.Equal(0, Predicates.AreaSign([(0, 0), (1, 1), (2, 2)]));
    }

    // Point n has the identifier Id(n + 1) and line n Id(100 + n), as in CurveProfileTests.
    private static Guid Id(int n) => new($"00000000-0000-0000-0000-{n:D12}");

    private static SketchRecord Record((double X, double Y)[] points, (int Start, int End)[] lines, (int Centre, int Start, int End)[]? arcs = null)
    {
        var record = SketchModel.New(Guid.Parse("5c000000-0000-0000-0000-0000000000c1"), Datums.XY.Id, null);
        return record with
        {
            Points = [.. record.Points, .. points.Select((p, n) => new SketchPoint(Id(n + 1), p.X, p.Y))],
            Lines = [.. lines.Select((l, n) => new SketchLine(Id(100 + n), Id(l.Start + 1), Id(l.End + 1)))],
            Arcs = [.. (arcs ?? []).Select((a, n) => new SketchArc(Id(400 + n), Id(a.Centre + 1), Id(a.Start + 1), Id(a.End + 1)))],
        };
    }

    // ADR-0050 item 4: the vertex q of a loop is on the line from r to s exactly, and the loop touches
    // itself at q. The rounded value of q gave the side of w and x, so SketchProfile accepted the loop
    // before TASK-0164.
    [Fact]
    public void A_Loop_That_Touches_Itself_On_A_Line_Is_Refused()
    {
        (double X, double Y) r = (0.055095419393502598, 0.16528625818050779);
        (double X, double Y) q = (1.1527888780213473, 3.4583666340640420);
        (double X, double Y) s = (2.4888110181049399, 7.4664330543148196);
        (double X, double Y) w = (s.X - 1, s.Y), x = (r.X, r.Y + 1);
        var sketch = Record([r, s, w, q, x], [(0, 1), (1, 2), (2, 3), (3, 4), (4, 0)]);

        Assert.True(3 * r.X == r.Y && 3 * q.X == q.Y && 3 * s.X == s.Y);
        Assert.True(Rounded(r, s, q) > 0 && Rounded(r, s, w) > 0 && Rounded(r, s, x) > 0);
        var (loop, reason) = SketchProfile.Find(sketch);

        Assert.Null(loop);
        Assert.Equal($"The lines '{Id(100)}' and '{Id(102)}' cross or touch.", reason);
    }

    // ADR-0050 item 4: a sliver triangle has a very small area and turns counter-clockwise. Its third
    // corner is two units in the last place above the line y = x. The rounded sum is zero, so
    // SketchProfile refused it with "The area of the loop is zero." before TASK-0164.
    [Fact]
    public void A_Sliver_Triangle_Has_Its_True_Turn()
    {
        (double X, double Y)[] corners = [(28, 28), (37, 37), (1.4609375, 1.4609375000000004)];
        var sketch = Record(corners, [(0, 1), (1, 2), (2, 0)]);

        var rounded = Enumerable.Range(0, 3).Sum(i => (corners[i].X * corners[(i + 1) % 3].Y) - (corners[(i + 1) % 3].X * corners[i].Y));
        Assert.Equal(0, rounded);
        var (loop, reason) = SketchProfile.Find(sketch);

        Assert.Null(reason);
        Assert.Equal(corners, loop);
    }

    // ADR-0050 item 4: an arc with very large values gives a vertex that is not finite, and the
    // profile gives a reason. Before the check, the exact predicate refused the vertex with an
    // exception.
    [Fact]
    public void A_Vertex_That_Is_Not_Finite_Gives_A_Reason()
    {
        var sketch = Record([(1.7e308, 0), (1.7e308, -1e307), (1.7e308, 1)], [(2, 1)], [(0, 1, 2)]);

        var (loop, reason) = SketchProfile.Find(sketch);

        Assert.Null(loop);
        Assert.Equal("A vertex of the polygon of the loop is not finite, because the values of the sketch are too large.", reason);
    }
}
