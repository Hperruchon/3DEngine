using Engine.Core.Commands;
using Engine.Kernel;

namespace Engine.Tests.Kernel;

// ADR-0089 item 6 (phase K5d, TASK-0289): the side of a vertex comes from a filter of doubles, and from the exact sign of
// Rational only when the doubles are too near zero. These tests hold that the filter gives the exact sign each time: on
// the planes and the vertices of a sphere, on points exactly on a plane, and on points a tiny distance from it.
public sealed class SideFilterTests
{
    private static int Exact(PlaneR plane, Vector3R p) => (plane.Normal.Dot(p) - plane.Offset).Sign;

    private static int Filtered(PlaneR plane, Vector3R p)
    {
        var (x, y, z) = p.ToDoubles();
        return Bsp.Approximate.Of(plane).Side(plane, p, x, y, z);
    }

    // The planes of 200 triangles of a sphere, against each vertex of the sphere: about 400,000 sides.
    [Fact]
    public void The_Filter_Gives_The_Exact_Side_Of_Each_Vertex_Of_A_Sphere()
    {
        var (positions, triangles) = CreateSphereCommandHandler.Mesh(6);
        var vertices = Enumerable.Range(0, positions.Length / 3)
            .Select(i => Vector3R.FromDoubles(positions[3 * i], positions[(3 * i) + 1], positions[(3 * i) + 2])).ToArray();
        var mismatches = 0;
        var zeros = 0;
        for (var t = 0; t < triangles.Length; t += 3 * (triangles.Length / 3 / 200))
        {
            var plane = PlaneR.Through(vertices[triangles[t]], vertices[triangles[t + 1]], vertices[triangles[t + 2]])!.Value;
            foreach (var vertex in vertices)
            {
                var exact = Exact(plane, vertex);
                zeros += exact == 0 ? 1 : 0;
                mismatches += exact == Filtered(plane, vertex) ? 0 : 1;
            }
        }

        Assert.Equal(0, mismatches);
        Assert.True(zeros > 0, "No vertex was on a plane, so the exact branch was not tried.");
    }

    // A point exactly on a plane, and a point 2^-200 from it on each side: the doubles cannot see the distance, so the
    // filter goes to the exact sign.
    [Fact]
    public void A_Point_On_A_Plane_Or_A_Tiny_Distance_From_It_Gets_The_Exact_Side()
    {
        var random = new Random(289);
        var tiny = new Rational(1, System.Numerics.BigInteger.Pow(2, 200));
        for (var i = 0; i < 500; i++)
        {
            Vector3R Point() => Vector3R.FromDoubles((random.NextDouble() * 200) - 100, (random.NextDouble() * 200) - 100, (random.NextDouble() * 200) - 100);
            var (a, b, c) = (Point(), Point(), Point());
            if (PlaneR.Through(a, b, c) is not { } plane)
                continue;
            var on = a + ((b - a) * Rational.FromDouble(random.NextDouble())) + ((c - a) * Rational.FromDouble(random.NextDouble()));
            var front = on + (plane.Normal * tiny);
            var back = on - (plane.Normal * tiny);

            Assert.Equal((0, 1, -1), (Filtered(plane, on), Filtered(plane, front), Filtered(plane, back)));
            Assert.Equal((0, 1, -1), (Exact(plane, on), Exact(plane, front), Exact(plane, back)));
        }
    }

    // The sign with no reduction to lowest terms is the sign of the reduced sum.
    [Fact]
    public void The_Sign_With_No_Reduction_Is_The_Exact_Sign()
    {
        var random = new Random(2891);
        Rational R() => new(random.Next(-1000, 1000), random.Next(1, 1000));
        for (var i = 0; i < 20000; i++)
        {
            var (x1, y1, x2, y2, x3, y3, z) = (R(), R(), R(), R(), R(), R(), R());
            Assert.Equal(((x1 * y1) + (x2 * y2) + (x3 * y3) - z).Sign, Rational.SignOfDotMinus(x1, y1, x2, y2, x3, y3, z));
        }
    }
}
