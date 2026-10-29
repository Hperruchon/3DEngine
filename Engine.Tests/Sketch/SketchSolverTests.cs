using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Queries;
using Engine.Core.Sketch;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.Sketch;

// The solver of the five constraints (ADR-0035, TASK-0102). ADR-0035 names this file in
// enforced-by. The solve of a rough rectangle, the bits of fixed solves on each platform,
// a set with no solution, the held point of a move, the free degrees, the undo, and an
// open of a file that holds a constraint that its positions do not satisfy.
public sealed class SketchSolverTests : IDisposable
{
    private static readonly Guid SketchId = Guid.Parse("5c000000-0000-0000-0000-000000000000");

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-solver-" + Guid.NewGuid().ToString("N"));

    public SketchSolverTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private static Guid Id(int n) => new($"00000000-0000-0000-0000-{n:D12}");

    private static SketchRecord Record(
        (double X, double Y)[] points,
        (int Start, int End)[] lines,
        params (string Kind, int[] Items, double[] Values)[] constraints)
    {
        // Point n has the identifier Id(n + 1), line n Id(100 + n), constraint n Id(200 + n).
        var record = SketchModel.New(SketchId, Datums.XY.Id, null);
        return record with
        {
            Points = [.. record.Points, .. points.Select((p, n) => new SketchPoint(Id(n + 1), p.X, p.Y))],
            Lines = lines.Select((l, n) => new SketchLine(Id(100 + n), Id(l.Start + 1), Id(l.End + 1))).ToList(),
            Constraints = constraints.Select((c, n) => new SketchConstraint(Id(200 + n), c.Kind, c.Items.Select(Item).ToList(), c.Values)).ToList(),
        };

        // An item is a point for 0 to 99 and a line from 100; -1 is the origin.
        static Guid Item(int item) => item == -1 ? SketchId : item < 100 ? Id(item + 1) : Id(item);
    }

    // A rectangle of four rough points, its four lines, and the constraints of item 4.
    private static SketchRecord Rectangle(params (string Kind, int[] Items, double[] Values)[] constraints) => Record(
        [(0.3, -0.2), (39.6, 0.4), (40.2, 20.3), (-0.1, 19.8)],
        [(0, 1), (1, 2), (2, 3), (3, 0)],
        constraints);

    private static readonly (string, int[], double[])[] Closed =
    [
        ("horizontal", [100], []),
        ("horizontal", [102], []),
        ("vertical", [101], []),
        ("vertical", [103], []),
        ("distance", [100], [40]),
        ("distance", [101], [20]),
        ("fixed", [0], [0, 0]),
    ];

    private static (double X, double Y)[] Positions(SketchRecord sketch) => Positions(sketch.Points);

    // The positions of the points other than the origin, to nine decimals, with no negative zero.
    private static (double X, double Y)[] Positions(IReadOnlyList<SketchPoint> points)
        => points.Skip(1).Select(p => (Math.Round(p.X, 9) + 0, Math.Round(p.Y, 9) + 0)).ToArray();

    // Acceptance 2: a rough rectangle becomes exact, with no free degree.
    [Fact]
    public void A_Rough_Rectangle_Becomes_Exact_And_Has_No_Free_Degree()
    {
        var before = Rectangle(Closed);
        Assert.Equal((false, 0), SketchSolver.Status(before));

        var solved = SketchSolver.Solve(before);

        Assert.Equal([(0.0, 0.0), (40.0, 0.0), (40.0, 20.0), (0.0, 20.0)], Positions(solved));
        Assert.Equal((true, 0), SketchSolver.Status(solved));
        Assert.All(solved.Constraints, c => Assert.Equal(0, SketchConstraints.Residual(solved, c), 9));
    }

    // ADR-0035 item 9: the bits of each solved position are constants. The pipeline runs this
    // test on Windows, Linux and macOS, on x64 and arm64.
    [Fact]
    public void The_Solved_Positions_Have_The_Same_Bits_On_Each_Platform()
    {
        // A triangle with irrational positions and one more point: a fixed point, a horizontal
        // line of length 10, a line of length 8, a distance of two points, and a point at 6 from
        // the first point and at 1.75 from the second line.
        var triangle = Record(
            [(1.1, 0.7), (11.3, 1.9), (5.2, 8.3), (6.5, 5.0)],
            [(0, 1), (1, 2), (2, 0)],
            ("fixed", [0], [1.5, 2.25]),
            ("horizontal", [100], []),
            ("distance", [100], [10]),
            ("distance", [101], [8]),
            ("distance", [0, 2], [7]),
            ("distance", [3, 0], [6]),
            ("distance", [3, 101], [1.75]));

        var solved = SketchSolver.Solve(triangle);

        Assert.Equal((true, 0), SketchSolver.Status(solved));
        // The positions: (1.5, 2.25 + one unit of the last place), (11.5, 2.25), (5.75, 7.8121…) and
        // (7.2653…, 3.9115…), as the first run on Windows x64 gave them on 2026-10-29.
        Assert.Equal(
            "3FF8000000000000,4002000000000001 4027000000000000,4002000000000000 4017000000000000,401F3FA3F3C02963 401D0FBA84DAD660,400F4AC73316C8F0",
            string.Join(" ", solved.Points.Skip(1).Select(p => $"{BitConverter.DoubleToInt64Bits(p.X):X16},{BitConverter.DoubleToInt64Bits(p.Y):X16}")));
    }

    // Acceptance 3 and ADR-0035 item 6: a set with no solution keeps the positions that the
    // command gave.
    [Fact]
    public void A_Set_With_No_Solution_Keeps_The_Positions_Of_The_Command()
    {
        var conflict = Rectangle([.. Closed, ("distance", [102], [41])]);

        var result = SketchSolver.Solve(conflict);

        Assert.Same(conflict, result);
        Assert.False(SketchSolver.Status(result).Solved);
    }

    // A constraint that repeats a constraint adds no equation of its own, so the set solves.
    [Fact]
    public void A_Redundant_Constraint_That_Agrees_Still_Solves()
    {
        var redundant = Rectangle([.. Closed, ("horizontal", [100], []), ("distance", [102], [40])]);

        var solved = SketchSolver.Solve(redundant);

        Assert.Equal([(0.0, 0.0), (40.0, 0.0), (40.0, 20.0), (0.0, 20.0)], Positions(solved));
        Assert.Equal((true, 0), SketchSolver.Status(solved));
    }

    // ADR-0035 item 5: two constraints that both say "A, B and P are on one line" give rows that
    // are proportional by an irrational factor, so the elimination leaves a pivot near zero and not
    // zero. The solver drops it: the set solves, and one degree stays free, P along the line.
    [Fact]
    public void A_Redundant_Row_With_Irrational_Entries_Is_Dropped()
    {
        var collinear = Record(
            [(0, 0), (7.3, 2.9), (3.1, 1.4)],
            [(0, 1), (0, 2)],
            ("fixed", [0], [0, 0]),
            ("fixed", [1], [7.3, 2.9]),
            ("distance", [2, 100], [0]),
            ("distance", [1, 101], [0]));

        var solved = SketchSolver.Solve(collinear);

        Assert.NotSame(collinear, solved);
        Assert.Equal((true, 1), SketchSolver.Status(solved));
    }

    // ADR-0035 item 8: two degrees for each point other than the origin, less the rank.
    [Theory]
    [InlineData(0, 8)]
    [InlineData(1, 7)]
    [InlineData(3, 5)]
    [InlineData(5, 3)]
    [InlineData(7, 0)]
    public void The_Freedom_Is_Two_For_Each_Point_Less_The_Rank(int count, int freedom)
    {
        var sketch = SketchSolver.Solve(Rectangle(Closed[..count]));

        Assert.Equal((true, freedom), SketchSolver.Status(sketch));
    }

    // A point on the line keeps its side: the solver does not mirror it through the line.
    [Fact]
    public void A_Distance_To_A_Line_Keeps_The_Side_Of_The_Point()
    {
        var below = Record(
            [(0, 0), (10, 0), (5, -3)],
            [(0, 1)],
            ("fixed", [0], [0, 0]),
            ("fixed", [1], [10, 0]),
            ("distance", [2, 100], [2]));

        var solved = SketchSolver.Solve(below);

        Assert.Equal((5.0, -2.0), Positions(solved)[2]);
    }

    // Acceptance 4 and ADR-0035 item 7: the moved point stays where the command put it, and
    // a fixed point goes back.
    [Fact]
    public async Task A_Move_Holds_The_Point_When_A_Solution_Exists()
    {
        var session = NewSession();
        var sketch = await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }, "sketchId");
        var a = await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 0, Y = 0 }, "pointId");
        var b = await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 10, Y = 0 }, "pointId");
        var line = await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = a, EndPointId = b }, "lineId");
        await Applied(session, new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = line, Value = 10 }, "constraintId");

        // A line of length 10 with a free start: B stays at (3, 4), and A goes to (-3, -4).
        await Applied(session, new MoveSketchPointCommand { SketchId = sketch, PointId = b, X = 3, Y = 4 }, "pointId");
        var moved = await Sketch(session, sketch);
        Assert.Equal([(-3.0, -4.0), (3.0, 4.0)], Positions(moved.Points));

        await Applied(session, new AddSketchConstraintCommand { SketchId = sketch, Kind = "fixed", First = a, Value = 0, SecondValue = 0 }, "constraintId");
        await Applied(session, new MoveSketchPointCommand { SketchId = sketch, PointId = b, X = 6, Y = 8 }, "pointId");
        Assert.Equal([(0.0, 0.0), (6.0, 8.0)], Positions((await Sketch(session, sketch)).Points));

        await Applied(session, new MoveSketchPointCommand { SketchId = sketch, PointId = a, X = 3, Y = 3 }, "pointId");
        var back = await Sketch(session, sketch);
        Assert.Equal((0.0, 0.0), (Math.Round(back.Points[1].X, 9) + 0, Math.Round(back.Points[1].Y, 9) + 0));
        Assert.True(back.Solved);
    }

    // Acceptance 6: an undo gives back the record before the step, with the rough positions.
    [Fact]
    public async Task An_Undo_After_A_Solve_Gives_Back_The_Record_Before()
    {
        var session = NewSession();
        var sketch = await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }, "sketchId");
        var a = await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 0, Y = 0 }, "pointId");
        var b = await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 10, Y = 1 }, "pointId");
        var line = await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = a, EndPointId = b }, "lineId");
        var rough = await Sketch(session, sketch);

        await Applied(session, new AddSketchConstraintCommand { SketchId = sketch, Kind = "horizontal", First = line }, "constraintId");
        Assert.Equal(0, (await Sketch(session, sketch)).Constraints[0].Residual!.Value, 9);

        await Applied(session, new UndoCommand(), "commandId");
        var undone = await Sketch(session, sketch);
        Assert.Equal(rough.Points, undone.Points);
    }

    // Acceptance 7 and ADR-0035 item 10: a file whose log holds a constraint that its
    // positions do not satisfy opens, and its sketch is solved. The log holds the inputs only.
    [NativeManifoldFact]
    public async Task An_Open_Solves_A_Sketch_Of_The_File()
    {
        var path = Path.Combine(_folder, "rough" + DocumentFile.Extension);
        File.WriteAllText(path, string.Join("\n", RoughFile) + "\n");

        // The file records the backend Manifold, and a replay uses the recorded backend.
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        Assert.Empty(await DocumentFile.Open(session, path));
        var sketch = await Sketch(session, Guid.Parse("10000000-0000-0000-0000-000000000000"));
        session.Close();

        Assert.True(sketch.Solved);
        Assert.Equal(0, sketch.Points[2].Y, 9);
        Assert.DoesNotContain(File.ReadAllLines(path), line => line.Contains("residual", StringComparison.Ordinal));
    }

    // A file that the server of v0.101 (449e622) wrote on 2026-10-29: a line from (0, 0) to
    // (40, 3), a fixed start, and a constraint "horizontal" that the positions of the log do not
    // satisfy. At v0.101 GetSketch gave the residuals 0 and 3.
    private static readonly string[] RoughFile =
    [
        """{"format":"3dengine-document","formatVersion":1,"lengthUnit":"mm"}""",
        """{"version":1,"backend":{"name":"manifold","version":"3.5.2"},"command":{"name":"CreateSketch","schemaVersion":1,"commandId":"10000000-0000-0000-0000-000000000000","expectedDocumentVersion":null,"parameters":{"datumId":"00000000-0000-0000-0000-000000000001"}}}""",
        """{"version":2,"backend":{"name":"manifold","version":"3.5.2"},"command":{"name":"AddSketchPoint","schemaVersion":1,"commandId":"20000000-0000-0000-0000-000000000000","expectedDocumentVersion":null,"parameters":{"sketchId":"10000000-0000-0000-0000-000000000000","x":0,"y":0}}}""",
        """{"version":3,"backend":{"name":"manifold","version":"3.5.2"},"command":{"name":"AddSketchPoint","schemaVersion":1,"commandId":"30000000-0000-0000-0000-000000000000","expectedDocumentVersion":null,"parameters":{"sketchId":"10000000-0000-0000-0000-000000000000","x":40,"y":3}}}""",
        """{"version":4,"backend":{"name":"manifold","version":"3.5.2"},"command":{"name":"AddSketchLine","schemaVersion":1,"commandId":"40000000-0000-0000-0000-000000000000","expectedDocumentVersion":null,"parameters":{"endPointId":"30000000-0000-0000-0000-000000000000","sketchId":"10000000-0000-0000-0000-000000000000","startPointId":"20000000-0000-0000-0000-000000000000"}}}""",
        """{"version":5,"backend":{"name":"manifold","version":"3.5.2"},"command":{"name":"AddSketchConstraint","schemaVersion":1,"commandId":"50000000-0000-0000-0000-000000000000","expectedDocumentVersion":null,"parameters":{"first":"20000000-0000-0000-0000-000000000000","kind":"fixed","secondValue":0,"sketchId":"10000000-0000-0000-0000-000000000000","value":0}}}""",
        """{"version":6,"backend":{"name":"manifold","version":"3.5.2"},"command":{"name":"AddSketchConstraint","schemaVersion":1,"commandId":"60000000-0000-0000-0000-000000000000","expectedDocumentVersion":null,"parameters":{"first":"40000000-0000-0000-0000-000000000000","kind":"horizontal","sketchId":"10000000-0000-0000-0000-000000000000"}}}""",
    ];

    private static DocumentSession NewSession() => new(EngineHosting.CreateDefault(new InProcessMeshBackend()));

    private static async Task<Guid> Applied(DocumentSession session, Command command, string output)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Outputs.TryGet<Guid>(output, out var id) ? id : Guid.Empty;
    }

    private static async Task<SketchInfo> Sketch(DocumentSession session, Guid id)
    {
        var result = await session.Query<SketchInfo>(new GetSketchQuery { SketchId = id });
        Assert.True(result.Error is null, result.Error?.Message);
        return result.Result!;
    }
}
