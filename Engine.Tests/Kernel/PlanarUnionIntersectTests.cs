using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Geometry.Manifold;
using Engine.Kernel;
using Engine.Tests.Geometry;

namespace Engine.Tests.Kernel;

// Row K16 (TASK-0433): the exact union and intersection of the owned kernel (ADR-0181 item 5), in the form of the subtract of
// ADR-0057. Each expected value comes from a computation by hand: a box of 10 is [-5, 5] on each axis.
public sealed class PlanarUnionIntersectTests
{
    private static readonly Guid A = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000000");
    private static readonly Guid B = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000000");

    private static Vector3R V(double x, double y, double z) => Vector3R.FromDoubles(x, y, z);

    private static KernelSolid Cube(Guid feature, double dx = 0) => KernelSolid.Box(feature, 10, 10, 10).Translate(V(dx, 0, 0));

    private static string[] Names(Brep brep, Guid feature)
        => [.. brep.Faces.Where(face => face.Tag.Feature == feature).Select(face => face.Tag.Element).Distinct().Order(StringComparer.Ordinal)];

    // Two cubes that overlap by half: the union is 1500 and keeps the outer faces of each cube, and no face of one cube
    // that lies inside the other. The intersection is 500, the box [0, 5] x [-5, 5] x [-5, 5], with the face +x of A and the
    // face -x of B, and one face on each other side.
    [Fact]
    public void Two_Cubes_That_Overlap_Give_The_Union_And_The_Intersection_With_The_Faces_Of_Both()
    {
        var union = PlanarBoolean.Union(Cube(A), Cube(B, 5))!;
        var joined = Brep.Of(union);

        Assert.True(joined.Valid);
        Assert.Equal((Rational)1500, union.Volume());
        Assert.Single(joined.Shells);
        Assert.Equal(["+y", "+z", "-x", "-y", "-z"], Names(joined, A));
        Assert.Equal(["+x", "+y", "+z", "-y", "-z"], Names(joined, B));
        Assert.Equal(((-5.0, -5.0, -5.0), (10.0, 5.0, 5.0)), PlanarSubtractTests.Bounds(union));

        var common = PlanarBoolean.Intersect(Cube(A), Cube(B, 5))!;
        var shared = Brep.Of(common);

        Assert.True(shared.Valid);
        Assert.Equal((Rational)500, common.Volume());
        Assert.Equal(((0.0, -5.0, -5.0), (5.0, 5.0, 5.0)), PlanarSubtractTests.Bounds(common));
        Assert.Equal((8, 12, 6), (shared.Vertices.Count, shared.Edges.Count, shared.Faces.Count));
        Assert.Contains(shared.Faces, face => face.Tag == new FaceTag(A, "+x"));
        Assert.Contains(shared.Faces, face => face.Tag == new FaceTag(B, "-x"));
        Assert.DoesNotContain(shared.Faces, face => face.Tag == new FaceTag(A, "-x") || face.Tag == new FaceTag(B, "+x"));
        Assert.Equal(["+x", "+y", "+z", "-x", "-y", "-z"], shared.Faces.Select(face => face.Tag.Element).Order(StringComparer.Ordinal));
    }

    // Two cubes that touch on the plane x = 5: the union is one closed shell of 2000 with no face on that plane, and the four
    // sides of the joint are split between the two cubes, so V - E + F = 12 - 20 + 10 = 2. The intersection has no volume,
    // so it gives no solid, as an empty subtract does.
    [Fact]
    public void Two_Cubes_That_Touch_Join_In_One_Shell_And_Share_No_Volume()
    {
        var union = PlanarBoolean.Union(Cube(A), Cube(B, 10))!;
        var brep = Brep.Of(union);

        Assert.True(brep.Valid);
        Assert.Equal((Rational)2000, union.Volume());
        Assert.Single(brep.Shells);
        Assert.Equal((12, 20, 10, 0), (brep.Vertices.Count, brep.Edges.Count, brep.Faces.Count, brep.Holes));
        Assert.DoesNotContain(brep.Faces, face => face.Tag == new FaceTag(A, "+x") || face.Tag == new FaceTag(B, "-x"));

        Assert.Null(PlanarBoolean.Intersect(Cube(A), Cube(B, 10)));
    }

    // TASK-0438 (finding E230 of the second codebase review of 2026-11-21; Q6 A): two cubes that touch on an edge only, at the
    // origin and at (10, 10, 0). The steps of csg.js remove no polygon, so the union keeps the twelve faces and has the volume
    // 2000; the edge from (5, 5, -5) to (5, 5, 5) has two coedges in each direction, so it has four faces and the B-Rep is not
    // valid. ADR-0181 ("Consequences") keeps the body, as Manifold gives a valid mesh. The intersection has no volume.
    [Fact]
    public void Two_Cubes_That_Touch_On_An_Edge_Join_In_A_Solid_With_An_Edge_Of_Four_Faces()
    {
        var other = KernelSolid.Box(B, 10, 10, 10).Translate(V(10, 10, 0));
        var union = PlanarBoolean.Union(Cube(A), other)!;
        var brep = Brep.Of(union);

        Assert.Equal((Rational)2000, union.Volume());
        Assert.False(brep.Valid);
        Assert.Equal(12, brep.Faces.Count);
        var edge = Assert.Single(brep.Edges, edge => edge.Faces.Count > 2);
        Assert.Equal(4, edge.Faces.Count);
        Assert.Equal(
            [(5.0, 5.0, -5.0), (5.0, 5.0, 5.0)],
            new[] { brep.Vertices[edge.From].ToDoubles(), brep.Vertices[edge.To].ToDoubles() }.Order());

        Assert.Null(PlanarBoolean.Intersect(Cube(A), other));
    }

    // Two cubes apart: the union is one solid of two shells, each a cube with its six faces; the intersection is empty.
    [Fact]
    public void Two_Cubes_Apart_Give_Two_Shells_And_No_Intersection()
    {
        var union = PlanarBoolean.Union(Cube(A), Cube(B, 20))!;
        var brep = Brep.Of(union);

        Assert.True(brep.Valid);
        Assert.Equal((Rational)2000, union.Volume());
        Assert.Equal(2, brep.Shells.Count);
        Assert.Equal(["+x", "+y", "+z", "-x", "-y", "-z"], Names(brep, A));
        Assert.Equal(["+x", "+y", "+z", "-x", "-y", "-z"], Names(brep, B));

        Assert.Null(PlanarBoolean.Intersect(Cube(A), Cube(B, 20)));
    }

    // A small box inside a cube: the union is the cube with its own faces only, and the intersection is the small box with
    // its own faces only. The order of the operands does not change either volume.
    [Fact]
    public void A_Box_Inside_A_Cube_Is_The_Intersection_And_The_Cube_Is_The_Union()
    {
        var inner = KernelSolid.Box(B, 2, 4, 6).Translate(V(1, 1, 1));

        foreach (var (first, second) in new[] { (Cube(A), inner), (inner, Cube(A)) })
        {
            var union = PlanarBoolean.Union(first, second)!;
            Assert.Equal((Rational)1000, union.Volume());
            Assert.All(union.Polygons, polygon => Assert.Equal(A, polygon.Tag.Feature));

            var common = PlanarBoolean.Intersect(first, second)!;
            Assert.Equal((Rational)48, common.Volume());
            Assert.All(common.Polygons, polygon => Assert.Equal(B, polygon.Tag.Feature));
            Assert.True(Brep.Of(common).Valid);
        }
    }

    // Random models of boxes, L extrudes and moves, joined or intersected with one or two others, in the kernel and in
    // Manifold, as the subtract of ADR-0057 item 7. The exact volume and the bounds agree with those of Manifold, each result
    // is a closed solid with V - E + F - H even and at most 2 for each shell, and an intersection that Manifold refuses as
    // empty, or gives as a flat solid of no volume, is empty in the kernel. The seed gives cases of each kind.
    [NativeManifoldFact]
    public void The_Union_And_The_Intersection_Agree_With_Manifold_Over_Random_Models()
    {
        using var backend = new ManifoldGeometryBackend();
        var ops = backend.TryGet<IBooleanOps>()!;
        var random = new Random(433);
        var (unions, intersections, empty, flat) = (0, 0, 0, 0);
        for (var model = 0; model < 60; model++)
        {
            var union = model % 2 == 0;
            var (kernel, key) = PlanarSubtractTests.Body(backend, random);
            for (var step = 0; step < 1 + random.Next(2) && kernel is not null; step++)
            {
                // One intersection in five is with a box of 100, which holds each body, so the result is the body.
                var (tool, toolKey) = model % 10 == 5 ? PlanarSubtractTests.Large(backend) : PlanarSubtractTests.Body(backend, random);
                var result = union ? PlanarBoolean.Union(kernel, tool) : PlanarBoolean.Intersect(kernel, tool);
                var resultKey = Guid.NewGuid();
                bool refused;
                try
                {
                    if (union)
                        ops.Union(new BodyHandle(resultKey), new BodyHandle(key), new BodyHandle(toolKey));
                    else
                        ops.Intersect(new BodyHandle(resultKey), new BodyHandle(key), new BodyHandle(toolKey));
                    refused = false;
                }
                catch (ManifoldNativeException)
                {
                    refused = true;
                }

                if (refused)
                {
                    Assert.False(union, $"Model {model}: Manifold refused a union.");
                    Assert.Null(result);
                    empty++;
                    break;
                }

                var (volume, box) = PlanarSubtractTests.Measure(backend, resultKey);

                // ADR-0181 item 3: two bodies that touch on a face give a flat solid of no volume in Manifold, and no solid in
                // the kernel. The handler refuses it with the kernel, so it is empty here too.
                if (!union && result is null && Math.Abs(volume) < 1e-9)
                {
                    flat++;
                    break;
                }

                Assert.True(result is not null, $"Model {model}, step {step}, union {union}: Manifold gives {volume} in {box}, and the kernel gives no solid.");
                var brep = Brep.Of(result);
                Assert.True(brep.Valid, $"Model {model}: the result is not closed.");
                var euler = brep.Vertices.Count - brep.Edges.Count + brep.Faces.Count - brep.Holes;
                Assert.True(euler % 2 == 0 && euler <= 2 * brep.Shells.Count, $"Model {model}: V - E + F - H = {euler}.");
                Assert.Equal(volume, result.Volume().ToDouble(), Math.Max(1e-9, Math.Abs(volume) * 1e-9));
                Assert.Equal(box, PlanarSubtractTests.Bounds(result));
                (kernel, key) = (result, resultKey);
                if (union)
                    unions++;
                else
                    intersections++;
            }
        }

        Assert.True(unions > 20, $"Only {unions} unions were compared.");
        Assert.True(intersections > 10, $"Only {intersections} intersections were compared.");
        Assert.True(empty > 0, "No intersection was empty.");
        Assert.True(flat > 0, "No intersection was flat.");
    }
}
