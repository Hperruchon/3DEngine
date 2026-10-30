using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Queries;
using Engine.Core.Sketch;

namespace Engine.Tests.Sketch;

// The seven constraints of points and lines of phase K2c (TASK-0110, ADR-0033 item 3, ADR-0035):
// pointOn, midpoint, parallel, perpendicular, equal, symmetric and angle. Each one solves, the
// derivatives of its equations agree with a central difference, a fixed solve gives constant
// bits, an angle keeps its branch, and each wrong form is refused.
public sealed class LineConstraintTests
{
    private static readonly Guid SketchId = Guid.Parse("5c000000-0000-0000-0000-000000000000");

    private static readonly double Cos30 = Math.Sqrt(3) / 2;

    private static Guid Id(int n) => new($"00000000-0000-0000-0000-{n:D12}");

    // Point n has the identifier Id(n + 1), line n Id(100 + n), constraint n Id(200 + n). An
    // item is a point for 0 to 99 and a line from 100.
    private static SketchRecord Record(
        (double X, double Y)[] points,
        (int Start, int End)[] lines,
        params (string Kind, int[] Items, double[] Values)[] constraints)
    {
        var record = SketchModel.New(SketchId, Datums.XY.Id, null);
        return record with
        {
            Points = [.. record.Points, .. points.Select((p, n) => new SketchPoint(Id(n + 1), p.X, p.Y))],
            Lines = lines.Select((l, n) => new SketchLine(Id(100 + n), Id(l.Start + 1), Id(l.End + 1))).ToList(),
            Constraints = constraints.Select((c, n) => new SketchConstraint(Id(200 + n), c.Kind, c.Items.Select(i => i < 100 ? Id(i + 1) : Id(i)).ToList(), c.Values)).ToList(),
        };
    }

    // Two rough lines, p0→p1 and p2→p3, and one free point p4.
    private static readonly (double X, double Y)[] Rough = [(1.1, 0.9), (9.3, 2.2), (2.4, 5.1), (8.2, 7.6), (4.3, 4.7)];

    private static readonly (int, int)[] TwoLines = [(0, 1), (2, 3)];

    // One constraint of each kind, and the free degrees after the solve: 2 for each of the five
    // points, less the rank of its equations.
    public static TheoryData<string, int[], double[], int> Kinds => new()
    {
        { "pointOn", [4, 100], [], 9 },
        { "midpoint", [4, 100], [], 8 },
        { "parallel", [100, 101], [], 9 },
        { "perpendicular", [100, 101], [], 9 },
        { "equal", [100, 101], [], 9 },
        { "symmetric", [2, 3, 100], [], 8 },
        { "angle", [100, 101], [Cos30, 0.5], 9 },
    };

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Each_Kind_Solves_A_Rough_Sketch(string kind, int[] items, double[] values, int freedom)
    {
        var rough = Record(Rough, TwoLines, (kind, items, values));
        Assert.True(Math.Abs(SketchConstraints.Residual(rough, rough.Constraints[0])) > 1e-3, "The rough sketch already satisfies the constraint.");

        var solved = SketchSolver.Solve(rough);

        Assert.True(Math.Abs(SketchConstraints.Residual(solved, solved.Constraints[0])) < SketchSolver.Tolerance);
        Assert.Equal((true, freedom), SketchSolver.Status(solved));
    }

    // ADR-0035 item 1: the solver computes each derivative by formula. Each column must agree
    // with a central difference of the values, at the rough positions and with a line of
    // length zero.
    public static TheoryData<string, int[], double[]> Shapes()
    {
        var shapes = new TheoryData<string, int[], double[]>();
        foreach (var row in Kinds)
            shapes.Add((string)row[0], (int[])row[1], (double[])row[2]);
        return shapes;
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void Each_Derivative_Agrees_With_A_Central_Difference(string kind, int[] items, double[] values)
    {
        foreach (var points in new[] { Rough, Rough.Select((p, n) => n == 1 ? Rough[0] : p).ToArray() })
        {
            var sketch = Record(points, TwoLines, (kind, items, values));
            var (_, jacobian) = SketchSolver.Linearise(sketch);
            const double h = 1e-6;
            for (var column = 0; column < jacobian.GetLength(1); column++)
            {
                // A line of length zero has a kink, so the test leaves out the columns of its ends there.
                if (points[1] == points[0] && column / 2 is 0 or 1)
                    continue;
                var plus = SketchSolver.Linearise(Moved(sketch, column, h)).Values;
                var minus = SketchSolver.Linearise(Moved(sketch, column, -h)).Values;
                for (var row = 0; row < plus.Length; row++)
                {
                    var difference = (plus[row] - minus[row]) / (2 * h);
                    Assert.True(Math.Abs(difference - jacobian[row, column]) < 1e-6, $"{kind}: row {row}, column {column}: the formula gives {jacobian[row, column]}, and the difference {difference}.");
                }
            }
        }
    }

    // The record with the coordinate of a column moved by h.
    private static SketchRecord Moved(SketchRecord sketch, int column, double h)
    {
        var id = sketch.Points[1 + (column / 2)].Id;
        return sketch with
        {
            Points = sketch.Points.Select(p => p.Id != id ? p : column % 2 == 0 ? p with { X = p.X + h } : p with { Y = p.Y + h }).ToList(),
        };
    }

    // ADR-0035 item 9: the bits of each solved position are constants. The pipeline runs this
    // test on Windows, Linux and macOS, on x64 and arm64.
    [Fact]
    public void A_Solve_Of_The_Seven_Kinds_Has_The_Same_Bits_On_Each_Platform()
    {
        // A fixed point, a horizontal line of length 10, a second line at 30 degrees to it and
        // equal to it, a midpoint, a point on the second line at 3 from its start, and two
        // points symmetric about the second line, on a line through that point and normal to it.
        var sketch = Record(
            [(1.1, 0.7), (11.3, 1.9), (2.2, 3.1), (9.7, 7.3), (6.1, 0.4), (4.4, 4.9), (3.3, 6.2), (5.6, 3.1)],
            [(0, 1), (2, 3), (6, 7), (5, 6)],
            ("fixed", [0], [1.5, 2.25]),
            ("horizontal", [100], []),
            ("distance", [100], [10]),
            ("angle", [100, 101], [Cos30, 0.5]),
            ("equal", [100, 101], []),
            ("coincident", [2, 0], []),
            ("midpoint", [4, 100], []),
            ("pointOn", [5, 101], []),
            ("distance", [5, 2], [3]),
            ("symmetric", [6, 7, 101], []),
            ("perpendicular", [103, 101], []),
            ("parallel", [102, 103], []));

        var solved = SketchSolver.Solve(sketch);

        Assert.All(solved.Constraints, c => Assert.True(Math.Abs(SketchConstraints.Residual(solved, c)) < SketchSolver.Tolerance, $"{c.Kind}: {SketchConstraints.Residual(solved, c)}"));
        var bits = string.Join(" ", solved.Points.Skip(1).Select(p => $"{BitConverter.DoubleToInt64Bits(p.X):X16},{BitConverter.DoubleToInt64Bits(p.Y):X16}"));
        // The positions: (1.5, 2.25), (11.5, 2.25), the start of the second line at (1.5, 2.25) with
        // one unit of the last place, its end (10.16…, 7.25), the midpoint (6.5, 2.25), the point
        // (4.098…, 3.75), and the two symmetric points, as the first run on Windows x64 gave them
        // on 2026-10-30, after the fix of the review of that date.
        Assert.True(
            bits == "3FF8000000000000,4002000000000000 4027000000000000,4002000000000000 3FF8000000000001,4002000000000001 "
                + "4024520CD1372FEB,401D000000000001 401A000000000000,4002000000000000 4010646E17211E72,400E000000000202 "
                + "4008EA1EDD314C83,4015D0D0CB7A1DBF 401453CCBFA9F16B,40005E5E690C3062",
            "The bits of the solve: " + bits);
    }

    // ADR-0033 item 3: an angle holds dot(d₁, d₂)·cos θ + cross(d₁, d₂)·sin θ > 0, so a second
    // line in the opposite direction does not satisfy it, and a solve from near that direction
    // turns the line to the direction of the angle.
    [Fact]
    public void An_Angle_Keeps_Its_Branch()
    {
        var opposite = Record([(0, 0), (10, 0), (5, 5), (5 - (8 * Cos30), 5 - 4)], TwoLines, ("angle", [100, 101], [Cos30, 0.5]));
        Assert.Equal(2, SketchConstraints.Residual(opposite, opposite.Constraints[0]), 9);

        var near = Record([(0, 0), (10, 0), (5, 5), (5 - (8 * Cos30) + 0.7, 5 - 4 - 0.9)], TwoLines, ("angle", [100, 101], [Cos30, 0.5]), ("fixed", [0], [0, 0]), ("fixed", [1], [10, 0]));
        var solved = SketchSolver.Solve(near);

        var (a, b, c, d) = (solved.Points[1], solved.Points[2], solved.Points[3], solved.Points[4]);
        var (d1x, d1y, d2x, d2y) = (b.X - a.X, b.Y - a.Y, d.X - c.X, d.Y - c.Y);
        Assert.True(SketchSolver.Status(solved).Solved, $"({c.X}, {c.Y}) to ({d.X}, {d.Y}), residual {SketchConstraints.Residual(solved, solved.Constraints[0])}");
        Assert.True((((d1x * d2x) + (d1y * d2y)) * Cos30) + (((d1x * d2y) - (d1y * d2x)) * 0.5) > 0);
    }

    // The finding of the codebase review of 2026-10-30: from an exact parallel or normal start,
    // or from the opposite direction of an angle, the equations of TASK-0110 that grew with |d₂|
    // took the second line to one point and reported it as solved. A solve now ends with each
    // line at its length, or the sketch is not solved; it never ends with a line of length zero.
    [Theory]
    [InlineData("perpendicular", 1.0, 5.0, 9.0, 5.0, 0.0, 0.0)]
    [InlineData("parallel", 5.0, 1.0, 5.0, 9.0, 0.0, 0.0)]
    [InlineData("angle", 9.0, 5.0, 1.0, 5.0, 1.0, 0.0)]
    [InlineData("angle", 9.0, 5.0, 1.0, 5.01, 1.0, 0.0)]
    [InlineData("angle", 9.0, 5.0, 1.0, 5.3, Cos30Value, 0.5)]
    [InlineData("angle", 4.0, 3.0, -4.0, 3.0, 1.0, 0.0)]
    public void No_Solve_Takes_A_Line_To_One_Point(string kind, double ax, double ay, double bx, double by, double cos, double sin)
    {
        double[] values = kind == "angle" ? [cos, sin] : [];
        var sketch = Record([(0, 0), (10, 0), (ax, ay), (bx, by)], TwoLines, ("fixed", [0], [0, 0]), ("fixed", [1], [10, 0]), (kind, [100, 101], values));

        var solved = SketchSolver.Solve(sketch);

        var (c, d) = (solved.Points[3], solved.Points[4]);
        var length = Math.Sqrt(((d.X - c.X) * (d.X - c.X)) + ((d.Y - c.Y) * (d.Y - c.Y)));
        Assert.True(length > 1, $"{kind}: the second line has the length {length}.");
        if (SketchSolver.Status(solved).Solved && kind == "angle")
            Assert.True((((d.X - c.X) * cos) + ((d.Y - c.Y) * sin)) > 0, "The second line has the direction opposite to the angle.");
    }

    private const double Cos30Value = 0.8660254037844386;

    private static DocumentSession NewSession() => new(EngineHosting.CreateDefault(new InProcessMeshBackend()));

    private static async Task<Guid> Applied(DocumentSession session, Command command, string output)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Outputs.TryGet<Guid>(output, out var id) ? id : throw new InvalidOperationException($"No output {output}.");
    }

    // A sketch with the line a→b, the line c→d and the free point e.
    private static async Task<(Guid Sketch, Guid A, Guid C, Guid E, Guid AB, Guid CD)> Scene(DocumentSession session)
    {
        var sketch = await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }, "sketchId");
        var ids = new List<Guid>();
        foreach (var (x, y) in Rough)
            ids.Add(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }, "pointId"));
        var ab = await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = ids[0], EndPointId = ids[1] }, "lineId");
        var cd = await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = ids[2], EndPointId = ids[3] }, "lineId");
        return (sketch, ids[0], ids[2], ids[4], ab, cd);
    }

    // ADR-0033 item 6: each wrong form of the seven kinds gives E-SKETCH-CONSTRAINT-INVALID, and
    // tangent and radius wait for phase K2d.
    [Fact]
    public async Task Each_Wrong_Form_Is_Refused()
    {
        var session = NewSession();
        var (s, a, c, e, ab, cd) = await Scene(session);
        AddSketchConstraintCommand[] wrong =
        [
            new() { SketchId = s, Kind = "pointOn", First = a, Second = ab },
            new() { SketchId = s, Kind = "midpoint", First = a, Second = ab },
            new() { SketchId = s, Kind = "midpoint", First = ab, Second = e },
            new() { SketchId = s, Kind = "parallel", First = ab, Second = ab },
            new() { SketchId = s, Kind = "perpendicular", First = ab, Second = e },
            new() { SketchId = s, Kind = "equal", First = ab, Second = cd, Value = 2 },
            new() { SketchId = s, Kind = "symmetric", First = c, Second = c, Third = ab },
            new() { SketchId = s, Kind = "symmetric", First = c, Second = e },
            new() { SketchId = s, Kind = "symmetric", First = a, Second = e, Third = ab },
            new() { SketchId = s, Kind = "angle", First = ab, Second = cd, Value = 1, SecondValue = 1 },
            new() { SketchId = s, Kind = "angle", First = ab, Second = cd, Value = 1 },
            new() { SketchId = s, Kind = "angle", First = ab, Second = cd, Value = double.NaN, SecondValue = 0 },
            new() { SketchId = s, Kind = "tangent", First = ab, Second = cd },
        ];

        foreach (var command in wrong)
        {
            var result = await session.Apply(command);
            Assert.True((CommandStatus.Rejected, DiagnosticCodes.SketchConstraintInvalid) == (result.Status, result.Error?.Code), $"{command.Kind} {command.First} {command.Second} {command.Third} {command.Value} {command.SecondValue}: {result.Status} {result.Error?.Code}");
        }
    }

    // ADR-0033 item 3: the handler records the pair of an angle divided by its length, at the
    // add and at a change of its values.
    [Fact]
    public async Task The_Pair_Of_An_Angle_Is_Recorded_As_A_Unit_Pair()
    {
        var session = NewSession();
        var (s, _, _, _, ab, cd) = await Scene(session);

        var angle = await Applied(session, new AddSketchConstraintCommand { SketchId = s, Kind = "angle", First = ab, Second = cd, Value = 0.6 + 4e-10, SecondValue = 0.8 }, "constraintId");
        AssertUnit();
        await Applied(session, new SetSketchConstraintValueCommand { SketchId = s, ConstraintId = angle, Value = 0.8, SecondValue = 0.6 - 4e-10 }, "constraintId");
        AssertUnit();

        void AssertUnit()
        {
            var values = session.Read((document, _) => document.Sketches.Single().Constraints.Single().Values).GetAwaiter().GetResult();
            Assert.NotEqual(0.6 + 4e-10, values[0]);
            Assert.True(Math.Abs((values[0] * values[0]) + (values[1] * values[1]) - 1) < 1e-15);
        }
    }

    // GetSketch gives each new kind with its residual, and the command solves the sketch.
    [Fact]
    public async Task GetSketch_Gives_The_Residual_Of_A_New_Kind()
    {
        var session = NewSession();
        var (s, _, _, _, ab, cd) = await Scene(session);

        await Applied(session, new AddSketchConstraintCommand { SketchId = s, Kind = "parallel", First = ab, Second = cd }, "constraintId");
        var info = (await session.Query<SketchInfo>(new GetSketchQuery { SketchId = s })).Result!;

        var constraint = Assert.Single(info.Constraints);
        Assert.Equal("parallel", constraint.Kind);
        Assert.True(Math.Abs(constraint.Residual!.Value) < SketchSolver.Tolerance);
        Assert.True(info.Solved);
    }
}
