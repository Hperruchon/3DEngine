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
}
