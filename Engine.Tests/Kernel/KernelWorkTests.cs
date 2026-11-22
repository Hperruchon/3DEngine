using Engine.Kernel;

namespace Engine.Tests.Kernel;

// TASK-0452 (register entry R-0073): the measures of the work of the kernel, which a test reads in place of a time (ADR-0173
// item 4), and the exact volume as a sum in pairs. The cut of a plate by a sphere holds its counts in
// CreateSphereAndTorusCommandTests.A_Sphere_Cuts_A_Hole_Of_One_Name.
public sealed class KernelWorkTests
{
    private static readonly PlaneR Diagonal = new PlaneR(new Vector3R(1, 1, 1), 3).Canonical();

    private static int Side(PlaneR plane, Vector3R p)
        => Bsp.Approximate.Of(plane).Side(plane, p, Bsp.Approximate.Near(p.X), Bsp.Approximate.Near(p.Y), Bsp.Approximate.Near(p.Z));

    // A point on the plane needs the exact sign, and a point far from it does not. The count of another thread does not
    // change the count of this thread.
    [Fact]
    public void The_Count_Of_The_Exact_Signs_Holds_Each_Side_That_The_Filter_Cannot_Decide_On_This_Thread()
    {
        var start = Bsp.ExactSignsOnThisThread;
        Assert.Equal(0, Side(Diagonal, new Vector3R(1, 1, 1)));
        Assert.Equal(1, Bsp.ExactSignsOnThisThread - start);

        Assert.Equal(1, Side(Diagonal, new Vector3R(5, 5, 5)));
        Assert.Equal(-1, Side(Diagonal, new Vector3R(-5, 0, 0)));
        Assert.Equal(1, Bsp.ExactSignsOnThisThread - start);

        var other = 0L;
        var thread = new Thread(() =>
        {
            for (var i = 0; i < 10; i++)
                Side(Diagonal, new Vector3R(3, 0, 0));
            other = Bsp.ExactSignsOnThisThread;
        });
        thread.Start();
        thread.Join();
        Assert.Equal((10L, 1L), (other, Bsp.ExactSignsOnThisThread - start));
    }

    // The sum in pairs is the sum in order, for each count of values from 0 to 33, so also for each odd count.
    [Fact]
    public void The_Sum_In_Pairs_Is_The_Sum_In_Order()
    {
        var random = new Random(452);
        for (var count = 0; count <= 33; count++)
        {
            var values = Enumerable.Range(0, count).Select(_ => new Rational(random.Next(-100_000, 100_000), random.Next(1, 100_000))).ToList();
            var inOrder = values.Aggregate(Rational.Zero, (sum, value) => sum + value);

            Assert.Equal(inOrder, KernelSolid.SumInPairs([.. values]));
        }
    }

    // A box less a box that turns by the angle of cos 3/5 and sin 4/5 about z: the split points have denominators that are not
    // powers of two. The volume is the sum in order of the terms of each fan, divided by 6.
    [Fact]
    public void The_Volume_Of_A_Cut_With_Exact_Split_Points_Is_The_Sum_In_Order()
    {
        var (c, s) = (new Rational(3, 5), new Rational(4, 5));
        var tool = KernelSolid.Box(Guid.NewGuid(), 4, 4, 20)
            .Rotate(new Vector3R(c, s, 0), new Vector3R(-s, c, 0), new Vector3R(0, 0, 1))
            .Translate(new Vector3R(new Rational(1, 3), new Rational(2, 7), 0));
        var cut = PlanarBoolean.Subtract(KernelSolid.Box(Guid.NewGuid(), 10, 10, 2), tool)!;

        var inOrder = Rational.Zero;
        foreach (var polygon in cut.Polygons)
        {
            for (var i = 1; i + 1 < polygon.Vertices.Count; i++)
                inOrder += polygon.Vertices[0].Dot(polygon.Vertices[i].Cross(polygon.Vertices[i + 1]));
        }

        Assert.Equal(inOrder / 6, cut.Volume());
        Assert.Equal((Rational)(200 - 32), cut.Volume());
    }
}
