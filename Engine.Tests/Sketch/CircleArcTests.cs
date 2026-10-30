using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Queries;
using Engine.Core.Sketch;

namespace Engine.Tests.Sketch;

// The circle and the arc of phase K2d (TASK-0117, ADR-0033 items 2 and 3, ADR-0035): the two
// commands and their refusals, the radius as a variable of the solver, the equation of an arc, the
// kinds pointOn, equal, tangent and radius on a curve, their derivatives, a fixed solve with
// constant bits, a delete, and an extrude that refuses a curve.
public sealed class CircleArcTests
{
    private static readonly Guid SketchId = Guid.Parse("5c000000-0000-0000-0000-000000000000");

    private static Guid Id(int n) => new($"00000000-0000-0000-0000-{n:D12}");

    // Point n has the identifier Id(n + 1), line n Id(100 + n), circle n Id(300 + n), arc n
    // Id(400 + n) and constraint n Id(200 + n). An item is a point for 0 to 99, a line for 100 to
    // 299, a circle for 300 to 399 and an arc from 400.
    private static SketchRecord Record(
        (double X, double Y)[] points,
        (int Start, int End)[] lines,
        (int Centre, double Radius)[] circles,
        (int Centre, int Start, int End)[] arcs,
        params (string Kind, int[] Items, double[] Values)[] constraints)
    {
        var record = SketchModel.New(SketchId, Datums.XY.Id, null);
        return record with
        {
            Points = [.. record.Points, .. points.Select((p, n) => new SketchPoint(Id(n + 1), p.X, p.Y))],
            Lines = lines.Select((l, n) => new SketchLine(Id(100 + n), Id(l.Start + 1), Id(l.End + 1))).ToList(),
            Circles = circles.Select((c, n) => new SketchCircle(Id(300 + n), Id(c.Centre + 1), c.Radius)).ToList(),
            Arcs = arcs.Select((a, n) => new SketchArc(Id(400 + n), Id(a.Centre + 1), Id(a.Start + 1), Id(a.End + 1))).ToList(),
            Constraints = constraints.Select((c, n) => new SketchConstraint(Id(200 + n), c.Kind, c.Items.Select(Item).ToList(), c.Values)).ToList(),
        };

        static Guid Item(int item) => item < 100 ? Id(item + 1) : Id(item);
    }

    // A line p0→p1, a circle about p2, a second circle about p3, and an arc about p4 from p5 to p6.
    private static readonly (double X, double Y)[] Rough = [(0.5, 0.3), (9.7, 1.1), (5.2, 6.1), (12.3, 5.4), (2.1, -6.2), (5.3, -5.8), (1.7, -2.9), (8.8, 7.9)];

    private static readonly (int, int)[] OneLine = [(0, 1)];

    private static readonly (int, double)[] TwoCircles = [(2, 3.1), (3, 1.4)];

    private static readonly (int, int, int)[] OneArc = [(4, 5, 6)];

    // One constraint of each kind on a curve, and the free degrees after the solve: 2 for each of
    // the eight points and 1 for each of the two circles, less the rank of the equations (the arc
    // has its own equation).
    public static TheoryData<string, int[], double[], int> Kinds => new()
    {
        { "pointOn", [7, 300], [], 16 },
        { "pointOn", [7, 400], [], 16 },
        { "equal", [300, 301], [], 16 },
        { "equal", [300, 400], [], 16 },
        { "radius", [300], [2.5], 16 },
        { "radius", [400], [2.5], 16 },
        { "tangent", [100, 300], [], 16 },
        { "tangent", [100, 400], [], 16 },
        { "tangent", [300, 301], [1], 16 },
        { "tangent", [300, 301], [-1], 16 },
    };

    public static TheoryData<string, int[], double[]> Shapes()
    {
        var shapes = new TheoryData<string, int[], double[]>();
        foreach (var row in Kinds)
            shapes.Add((string)row[0], (int[])row[1], (double[])row[2]);
        return shapes;
    }

    private static SketchRecord Rough_(string kind, int[] items, double[] values)
        => Record(Rough, OneLine, TwoCircles, OneArc, (kind, items, values));

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Each_Kind_On_A_Curve_Solves_A_Rough_Sketch(string kind, int[] items, double[] values, int freedom)
    {
        var rough = Rough_(kind, items, values);
        Assert.True(Math.Abs(SketchConstraints.Residual(rough, rough.Constraints[0])) > 1e-3, "The rough sketch already satisfies the constraint.");

        var solved = SketchSolver.Solve(rough);

        Assert.True(Math.Abs(SketchConstraints.Residual(solved, solved.Constraints[0])) < SketchSolver.Tolerance, $"{kind}: {SketchConstraints.Residual(solved, solved.Constraints[0])}");
        Assert.Equal((true, freedom), SketchSolver.Status(solved));
        Assert.All(solved.Circles, circle => Assert.True(circle.Radius > 0));
    }

    // ADR-0033 item 2: the two radii of an arc are equal by an implicit equation.
    [Fact]
    public void The_Two_Radii_Of_An_Arc_Become_Equal()
    {
        var rough = Record(Rough, [], [], OneArc);
        var solved = SketchSolver.Solve(rough);

        var arc = solved.Arcs.Single();
        double Spoke(Guid point)
        {
            var (c, p) = (solved.Points.Single(q => q.Id == arc.Center), solved.Points.Single(q => q.Id == point));
            return Math.Sqrt(((p.X - c.X) * (p.X - c.X)) + ((p.Y - c.Y) * (p.Y - c.Y)));
        }

        Assert.True(Math.Abs(Spoke(arc.Start) - Spoke(arc.End)) < SketchSolver.Tolerance);
        Assert.Equal((true, 15), SketchSolver.Status(solved));
    }

    // ADR-0035 item 1: each derivative agrees with a central difference, also by a radius.
    [Theory]
    [MemberData(nameof(Shapes))]
    public void Each_Derivative_Agrees_With_A_Central_Difference(string kind, int[] items, double[] values)
    {
        var sketch = Rough_(kind, items, values);
        var (_, jacobian) = SketchSolver.Linearise(sketch);
        const double h = 1e-6;
        for (var column = 0; column < jacobian.GetLength(1); column++)
        {
            var plus = SketchSolver.Linearise(Moved(sketch, column, h)).Values;
            var minus = SketchSolver.Linearise(Moved(sketch, column, -h)).Values;
            for (var row = 0; row < plus.Length; row++)
            {
                var difference = (plus[row] - minus[row]) / (2 * h);
                Assert.True(Math.Abs(difference - jacobian[row, column]) < 1e-6, $"{kind}: row {row}, column {column}: the formula gives {jacobian[row, column]}, and the difference {difference}.");
            }
        }
    }

    // The record with the variable of a column moved by h: the x or the y of a point other than
    // the origin, then the radius of each circle.
    private static SketchRecord Moved(SketchRecord sketch, int column, double h)
    {
        var coordinates = 2 * (sketch.Points.Count - 1);
        if (column >= coordinates)
        {
            var circle = sketch.Circles[column - coordinates];
            return sketch with { Circles = sketch.Circles.Select(c => c.Id == circle.Id ? c with { Radius = c.Radius + h } : c).ToList() };
        }

        var id = sketch.Points[1 + (column / 2)].Id;
        return sketch with
        {
            Points = sketch.Points.Select(p => p.Id != id ? p : column % 2 == 0 ? p with { X = p.X + h } : p with { Y = p.Y + h }).ToList(),
        };
    }

    // ADR-0035 item 9: the bits of a fixed solve with curves are constants. The pipeline runs this
    // test on Windows, Linux and macOS, on x64 and arm64.
    [Fact]
    public void A_Solve_With_Curves_Has_The_Same_Bits_On_Each_Platform()
    {
        // A fixed centre with a circle of radius 3, a horizontal line tangent to it below, a second
        // circle tangent outside and equal to the first, and an arc about a fixed point with a
        // radius of 2 and its start on the first circle.
        var sketch = Record(
            [(1.1, 0.7), (9.3, 0.2), (2.0, 2.0), (8.2, 3.1), (4.1, 1.2), (5.6, 2.3), (4.3, -0.8)],
            [(0, 1)],
            [(2, 2.8), (3, 3.3)],
            [(4, 5, 6)],
            ("fixed", [2], [2, 2]),
            ("radius", [300], [3]),
            ("horizontal", [100], []),
            ("tangent", [100, 300], []),
            ("tangent", [300, 301], [1]),
            ("equal", [300, 301], []),
            ("horizontal", [100], []),
            ("fixed", [4], [4, 1]),
            ("radius", [400], [2]),
            ("pointOn", [5, 300], []));

        var solved = SketchSolver.Solve(sketch);

        Assert.All(solved.Constraints, c => Assert.True(Math.Abs(SketchConstraints.Residual(solved, c)) < SketchSolver.Tolerance, $"{c.Kind}: {SketchConstraints.Residual(solved, c)}"));
        var bits = string.Join(" ", solved.Points.Skip(1).Select(p => $"{BitConverter.DoubleToInt64Bits(p.X):X16},{BitConverter.DoubleToInt64Bits(p.Y):X16}")
            .Concat(solved.Circles.Select(c => $"{BitConverter.DoubleToInt64Bits(c.Radius):X16}")));
        // The centre (2, 2), the line at y = −1 below it, the second centre at 6 from the first, the
        // centre of the arc at (4, 1), and the two radii 3, as the first run on Windows x64 gave
        // them on 2026-10-30.
        Assert.True(
            bits == "3FEE227AA7C34C31,BFEFFFFFFFFFFFFF 4022909F86F5BDE2,BFEFFFFFFFFFFFFF 4000000000000000,4000000000000000 "
                + "401FA1867D1D3983,4008629B139F03E5 4010000000000000,3FEFFFFFFFFFFFFF 401393E4B8B7FDBB,40064F92E2DFF6ED "
                + "401144DE579B8223,BFEF308E05858526 4008000000000000 4008000000000000",
            "The bits of the solve: " + bits);
    }

    private static DocumentSession NewSession() => new(EngineHosting.CreateDefault(new InProcessMeshBackend()));

    private static async Task<Guid> Applied(DocumentSession session, Command command, string output)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Outputs.TryGet<Guid>(output, out var id) ? id : throw new InvalidOperationException($"No output {output}.");
    }

    private static async Task<string?> Refused(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.Equal(CommandStatus.Rejected, result.Status);
        return result.Error?.Code;
    }

    // ADR-0033 item 6 (phase K2d): the two commands, each refusal, and GetSketch with the curves.
    [Fact]
    public async Task The_Commands_Add_A_Circle_And_An_Arc_And_Refuse_Each_Wrong_Form()
    {
        var session = NewSession();
        var s = await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }, "sketchId");
        var c = await Applied(session, new AddSketchPointCommand { SketchId = s, X = 3, Y = 3 }, "pointId");
        var a = await Applied(session, new AddSketchPointCommand { SketchId = s, X = 5, Y = 3 }, "pointId");
        var b = await Applied(session, new AddSketchPointCommand { SketchId = s, X = 3, Y = 5 }, "pointId");

        var circle = await Applied(session, new AddSketchCircleCommand { SketchId = s, CenterPointId = c, Radius = 2 }, "circleId");
        var arc = await Applied(session, new AddSketchArcCommand { SketchId = s, CenterPointId = c, StartPointId = a, EndPointId = b }, "arcId");

        foreach (var radius in new[] { 0, -1, double.NaN, double.PositiveInfinity })
            Assert.Equal(DiagnosticCodes.SketchCurveInvalid, await Refused(session, new AddSketchCircleCommand { SketchId = s, CenterPointId = c, Radius = radius }));
        Assert.Equal(DiagnosticCodes.SketchItemNotFound, await Refused(session, new AddSketchCircleCommand { SketchId = s, CenterPointId = circle, Radius = 1 }));
        Assert.Equal(DiagnosticCodes.SketchCurveInvalid, await Refused(session, new AddSketchArcCommand { SketchId = s, CenterPointId = c, StartPointId = c, EndPointId = b }));
        Assert.Equal(DiagnosticCodes.SketchCurveInvalid, await Refused(session, new AddSketchArcCommand { SketchId = s, CenterPointId = c, StartPointId = a, EndPointId = a }));
        Assert.Equal(DiagnosticCodes.SketchConstraintInvalid, await Refused(session, new AddSketchConstraintCommand { SketchId = s, Kind = "pointOn", First = c, Second = circle }));
        Assert.Equal(DiagnosticCodes.SketchConstraintInvalid, await Refused(session, new AddSketchConstraintCommand { SketchId = s, Kind = "pointOn", First = a, Second = arc }));
        Assert.Equal(DiagnosticCodes.SketchConstraintInvalid, await Refused(session, new AddSketchConstraintCommand { SketchId = s, Kind = "radius", First = circle, Value = 0 }));
        Assert.Equal(DiagnosticCodes.SketchConstraintInvalid, await Refused(session, new AddSketchConstraintCommand { SketchId = s, Kind = "tangent", First = circle, Second = arc, Value = 2 }));
        Assert.Equal(DiagnosticCodes.SketchConstraintInvalid, await Refused(session, new AddSketchConstraintCommand { SketchId = s, Kind = "equal", First = circle, Second = circle }));

        var info = (await session.Query<SketchInfo>(new GetSketchQuery { SketchId = s })).Result!;
        Assert.Equal((circle, 2.0), (info.Circles.Single().Id, info.Circles.Single().Radius));
        Assert.Equal((arc, c, a, b), (info.Arcs.Single().Id, info.Arcs.Single().Center, info.Arcs.Single().Start, info.Arcs.Single().End));
        Assert.True(info.Solved);
    }

    // ADR-0033 item 6: a delete of the centre removes the circle, the arc and each constraint on
    // them, and a delete of the circle removes it alone.
    [Fact]
    public async Task A_Delete_Of_A_Centre_Removes_Its_Curves()
    {
        var session = NewSession();
        var s = await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }, "sketchId");
        var c = await Applied(session, new AddSketchPointCommand { SketchId = s, X = 3, Y = 3 }, "pointId");
        var d = await Applied(session, new AddSketchPointCommand { SketchId = s, X = 9, Y = 3 }, "pointId");
        var a = await Applied(session, new AddSketchPointCommand { SketchId = s, X = 5, Y = 3 }, "pointId");
        var b = await Applied(session, new AddSketchPointCommand { SketchId = s, X = 3, Y = 5 }, "pointId");
        var circle = await Applied(session, new AddSketchCircleCommand { SketchId = s, CenterPointId = c, Radius = 2 }, "circleId");
        var other = await Applied(session, new AddSketchCircleCommand { SketchId = s, CenterPointId = d, Radius = 1 }, "circleId");
        await Applied(session, new AddSketchArcCommand { SketchId = s, CenterPointId = c, StartPointId = a, EndPointId = b }, "arcId");
        await Applied(session, new AddSketchConstraintCommand { SketchId = s, Kind = "radius", First = circle, Value = 2 }, "constraintId");

        await Applied(session, new DeleteSketchItemCommand { SketchId = s, ItemId = c }, "itemId");
        var sketch = await session.Read((document, _) => document.Sketches.Single());
        Assert.Equal((other, 0, 0), (sketch.Circles.Single().Id, sketch.Arcs.Count, sketch.Constraints.Count));

        await Applied(session, new DeleteSketchItemCommand { SketchId = s, ItemId = other }, "itemId");
        Assert.Empty((await session.Read((document, _) => document.Sketches.Single())).Circles);
    }

    // ADR-0036: an extrude takes a profile of lines only, so a sketch with a circle is refused.
    [Fact]
    public async Task An_Extrude_Refuses_A_Sketch_With_A_Circle()
    {
        var session = NewSession();
        var s = await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }, "sketchId");
        var c = await Applied(session, new AddSketchPointCommand { SketchId = s, X = 3, Y = 3 }, "pointId");
        await Applied(session, new AddSketchCircleCommand { SketchId = s, CenterPointId = c, Radius = 2 }, "circleId");

        Assert.Equal(DiagnosticCodes.SketchProfileInvalid, await Refused(session, new ExtrudeCommand { SketchId = s, Distance = 5 }));
    }
}
