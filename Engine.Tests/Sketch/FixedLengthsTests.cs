using Engine.Contracts.Sketch;
using Engine.Core.Sketch;

namespace Engine.Tests.Sketch;

// ADR-0149 item 1 (phase R22c, TASK-0384): the lines of a sketch whose length the constraints fix, by the rank of the
// Jacobian of the constraints with the gradient of the length as one more row.
public sealed class FixedLengthsTests
{
    private static readonly Guid SketchId = Guid.Parse("5d000000-0000-0000-0000-000000000000");

    private static Guid Id(int n) => new($"00000000-0000-0000-0000-{n:D12}");

    // Point n has the identifier Id(n + 1), line n Id(100 + n), constraint n Id(200 + n). An item is a point for 0 to 99 and
    // a line from 100.
    private static SketchRecord Record((double X, double Y)[] points, (int Start, int End)[] lines, params (string Kind, int[] Items, double[] Values)[] constraints)
    {
        var record = SketchModel.New(SketchId, Datums.XY.Id, null);
        return record with
        {
            Points = [.. record.Points, .. points.Select((p, n) => new SketchPoint(Id(n + 1), p.X, p.Y))],
            Lines = lines.Select((l, n) => new SketchLine(Id(100 + n), Id(l.Start + 1), Id(l.End + 1))).ToList(),
            Constraints = constraints.Select((c, n) => new SketchConstraint(Id(200 + n), c.Kind, c.Items.Select(i => i < 100 ? Id(i + 1) : Id(i)).ToList(), c.Values)).ToList(),
        };
    }

    private static readonly (double X, double Y)[] Corners = [(0, 0), (10, 0), (10, 6), (0, 6)];

    private static readonly (int, int)[] Rectangle = [(0, 1), (1, 2), (2, 3), (3, 0)];

    // The numbers of the lines whose length is fixed.
    private static int[] Fixed(SketchRecord record)
    {
        var lines = SketchSolver.FixedLengths(record);
        return [.. record.Lines.Select((line, n) => (line.Id, n)).Where(entry => lines.Contains(entry.Id)).Select(entry => entry.n)];
    }

    // Item 1: a rectangle with a length on the bottom and on the right and no other constraint can shear, so only those two
    // lengths are fixed; the top and the left are free.
    [Fact]
    public void A_Rectangle_With_Two_Lengths_Fixes_Only_Them()
    {
        var record = Record(Corners, Rectangle, ("distance", [100], [10]), ("distance", [101], [6]));

        Assert.Equal(new[] { 0, 1 }, Fixed(record));
    }

    // Item 1: with each side horizontal or vertical, the two lengths fix the top and the left too, by the closure of the
    // rectangle.
    [Fact]
    public void A_Rectangle_With_Its_Directions_Fixes_Each_Length()
    {
        var record = Record(Corners, Rectangle,
            ("distance", [100], [10]), ("distance", [101], [6]),
            ("horizontal", [100], []), ("vertical", [101], []), ("horizontal", [102], []), ("vertical", [103], []));

        Assert.Equal(new[] { 0, 1, 2, 3 }, Fixed(record));
    }

    // Item 1: a sketch with no constraint fixes no length; a line from the origin to a point with a fixed constraint keeps
    // its length, because both of its ends are fixed.
    [Fact]
    public void A_Free_Line_Is_Not_Fixed_And_A_Line_Between_Fixed_Points_Is()
    {
        Assert.Empty(SketchSolver.FixedLengths(Record([(0, 0), (4, 3)], [(0, 1)])));
        var record = Record([(0, 0), (4, 3)], [(0, 1)], ("fixed", [0], [0, 0]), ("fixed", [1], [4, 3]));
        Assert.Equal(new[] { 0 }, Fixed(record));
    }

    // TASK-0391 (finding E206 of the codebase review of 2026-11-15): a stair of 100 lines, each horizontal or vertical, with
    // a length on two lines of three. A length fixes its line only, so the lines with a length are the fixed lines.
    // GetSketch reads the status and the fixed lengths, and the window reads the active sketch in each frame. On the
    // computer of the owner, one full elimination for each line took 5.4 s; after TASK-0391 the status takes about 1 ms
    // and the analysis of GetSketch about 8 ms. The limit of 1 s leaves room for a slow runner of continuous integration.
    [Fact]
    public void The_Status_And_The_Fixed_Lengths_Of_A_Stair_Of_100_Lines_Take_Less_Than_One_Second()
    {
        const int count = 100;
        var points = Enumerable.Range(0, count + 1).Select(k => (X: 5.0 * ((k + 1) / 2), Y: 4.0 * (k / 2))).ToArray();
        var lines = Enumerable.Range(0, count).Select(n => (n, n + 1)).ToArray();
        var constraints = Enumerable.Range(0, count)
            .Select(n => (Kind: n % 2 == 0 ? "horizontal" : "vertical", Items: new[] { 100 + n }, Values: Array.Empty<double>()))
            .Concat(Enumerable.Range(0, count).Where(n => n % 3 != 2)
                .Select(n => (Kind: "distance", Items: new[] { 100 + n }, Values: new[] { n % 2 == 0 ? 5.0 : 4.0 })))
            .ToArray();
        var record = Record(points, lines, constraints);

        var clock = System.Diagnostics.Stopwatch.StartNew();
        var (solved, freedom) = SketchSolver.Status(record);
        var fixedLines = Fixed(record);
        clock.Stop();

        Assert.True(solved);
        Assert.Equal(Enumerable.Range(0, count).Where(n => n % 3 != 2).ToArray(), fixedLines);
        Assert.Equal((2 * (count + 1)) - count - constraints.Count(c => c.Kind == "distance"), freedom);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(1), $"The status and the fixed lengths took {clock.Elapsed}.");
    }

    // TASK-0400 (finding E213 of the codebase review of 2026-11-17): a stair of 600 lines with 1,000 constraints. Before
    // TASK-0400 the back-substitution of the analysis took each column for each line: on the computer of the owner the
    // analysis took about 930 ms and the status about 40 ms. Now the analysis takes about 100 ms. The test compares the two
    // on one computer, so a slow runner does not change the result: the analysis takes at most six times the status and
    // 100 ms. The points, the lines and the constraints have identifiers from 1, 10,001, 20,001 and 30,001, so 600 lines fit.
    [Fact]
    public void The_Fixed_Lengths_Of_A_Stair_Of_600_Lines_Take_A_Time_Near_The_Status()
    {
        const int count = 600;
        var record = SketchModel.New(SketchId, Datums.XY.Id, null);
        var lineIds = Enumerable.Range(0, count).Select(n => Id(10_001 + n)).ToArray();
        record = record with
        {
            Points = [.. record.Points, .. Enumerable.Range(0, count + 1).Select(k => new SketchPoint(Id(k + 1), 5.0 * ((k + 1) / 2), 4.0 * (k / 2)))],
            Lines = [.. Enumerable.Range(0, count).Select(n => new SketchLine(lineIds[n], Id(n + 1), Id(n + 2)))],
            Constraints =
            [
                .. Enumerable.Range(0, count).Select(n => new SketchConstraint(Id(20_001 + n), n % 2 == 0 ? "horizontal" : "vertical", [lineIds[n]], [])),
                .. Enumerable.Range(0, count).Where(n => n % 3 != 2)
                    .Select(n => new SketchConstraint(Id(30_001 + n), "distance", [lineIds[n]], [n % 2 == 0 ? 5.0 : 4.0])),
            ],
        };
        SketchSolver.FixedLengths(record);
        SketchSolver.Status(record);

        var status = System.Diagnostics.Stopwatch.StartNew();
        SketchSolver.Status(record);
        status.Stop();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var fixedLines = SketchSolver.FixedLengths(record);
        clock.Stop();

        Assert.Equal(1_000, record.Constraints.Count);
        Assert.Equal(Enumerable.Range(0, count).Where(n => n % 3 != 2).Select(n => lineIds[n]).ToHashSet(), fixedLines.ToHashSet());
        Assert.True(clock.Elapsed < (6 * status.Elapsed) + TimeSpan.FromMilliseconds(100), $"The fixed lengths took {clock.Elapsed}, and the status {status.Elapsed}.");
    }

    // TASK-0392 (finding E207 of the codebase review of 2026-11-15): the freedom and the fixed lengths do not depend on the
    // size of a part of the sketch. A rigid rectangle (a right angle, two pairs of parallel sides and two lengths) fixes each
    // side and has 3 free degrees; a loose one (a right angle, one pair of parallel sides and one length) fixes its bottom only
    // and has 5. A row of an angle has no unit, and a row of a length has millimetres, so before TASK-0392 a ratio of sizes of
    // about 1e6 hid rows of the small or of the large rectangle.
    [Theory]
    [InlineData(1e5, 0.001)]
    [InlineData(0.001, 1e5)]
    [InlineData(1, 1e-6)]
    [InlineData(1e-6, 1)]
    public void The_Freedom_And_The_Fixed_Lengths_Do_Not_Depend_On_The_Size(double rigid, double loose)
    {
        static ((double X, double Y)[] Points, (int, int)[] Lines, (string Kind, int[] Items, double[] Values)[] Constraints) Rectangle(
            double size, double left, int first, bool isRigid)
        {
            var points = new (double X, double Y)[] { (left, 0), (left + (10 * size), 0), (left + (10 * size), 6 * size), (left, 6 * size) };
            var lines = new (int, int)[] { (first, first + 1), (first + 1, first + 2), (first + 2, first + 3), (first + 3, first) };
            var line = 100 + first;
            var constraints = new List<(string Kind, int[] Items, double[] Values)>
            {
                ("perpendicular", [line, line + 1], []), ("parallel", [line, line + 2], []), ("distance", [line], [10 * size]),
            };
            if (isRigid)
                constraints.AddRange([("parallel", [line + 1, line + 3], []), ("distance", [line + 1], [6 * size])]);
            return (points, lines, [.. constraints]);
        }

        var a = Rectangle(rigid, 0, 0, isRigid: true);
        var b = Rectangle(loose, 20 * Math.Max(rigid, loose), 4, isRigid: false);
        var record = Record([.. a.Points, .. b.Points], [.. a.Lines, .. b.Lines], [.. a.Constraints, .. b.Constraints]);

        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, Fixed(record));
        Assert.Equal((true, 3 + 5), SketchSolver.Status(record));
    }
}
