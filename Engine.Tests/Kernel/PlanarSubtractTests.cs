using System.Numerics;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Geometry.Manifold;
using Engine.Kernel;
using Engine.Tests.Geometry;

namespace Engine.Tests.Kernel;

// The exact planar subtract of the owned kernel (ADR-0057, phase K4a, TASK-0183). ADR-0057 names this file in
// enforced-by. The exact numbers and the predicate, the box and the extrude, the subtract with the provenance of each
// face, the topology, and a comparison with Manifold over random models.
public sealed class PlanarSubtractTests
{
    private static readonly Guid A = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000000");
    private static readonly Guid B = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000000");
    private static readonly Guid C = Guid.Parse("cccccccc-0000-0000-0000-000000000000");

    private static Vector3R V(double x, double y, double z) => Vector3R.FromDoubles(x, y, z);

    // Item 2: a double enters with its bits, and leaves with one rounding to the nearest.
    [Fact]
    public void A_Double_Enters_Exactly_And_Leaves_Rounded_To_The_Nearest()
    {
        var tenth = Rational.FromDouble(0.1);
        Assert.Equal(new Rational(3602879701896397, BigInteger.Pow(2, 55)), tenth);
        Assert.Equal(1.0 / 3.0, (Rational.One / 3).ToDouble());
        Assert.Equal(0.1 + 0.2, (Rational.FromDouble(0.1) + Rational.FromDouble(0.2)).ToDouble());
        var random = new Random(183);
        for (var i = 0; i < 2000; i++)
        {
            var value = (random.NextDouble() - 0.5) * Math.Pow(10, random.Next(-30, 30));
            Assert.Equal(value, Rational.FromDouble(value).ToDouble());
        }
    }

    // Finding E136 of the codebase review of 2026-11-05 (b) (TASK-0192): below the smallest normal double the conversion
    // rounded to 53 bits and then again in Math.ScaleB. The quantum there is 2^-1074, so the nearest double comes from one
    // rounding: a value above half of the quantum gives the quantum, a tie goes to the even multiple, and each quotient of
    // two doubles gives the double that a division of doubles gives, which IEEE 754 rounds one time.
    [Fact]
    public void A_Value_Below_The_Smallest_Normal_Double_Rounds_One_Time()
    {
        var quantum = Rational.FromDouble(double.Epsilon);
        var tiny = new Rational(BigInteger.One, BigInteger.Pow(2, 60));
        Assert.Equal(double.Epsilon, ((new Rational(1, 2) + tiny) * quantum).ToDouble());
        Assert.Equal(double.Epsilon, ((new Rational(3, 2) - tiny) * quantum).ToDouble());
        Assert.Equal(0.0, (new Rational(1, 2) * quantum).ToDouble());
        Assert.Equal(2 * double.Epsilon, (new Rational(3, 2) * quantum).ToDouble());
        Assert.Equal(-double.Epsilon, ((new Rational(-1, 2) - tiny) * quantum).ToDouble());

        var random = new Random(136);
        for (var i = 0; i < 2000; i++)
        {
            var x = (random.NextDouble() + 0.5) * Math.Pow(2, -random.Next(1000, 1074));
            var y = (random.NextDouble() + 0.5) * Math.Pow(2, random.Next(0, 60));
            Assert.Equal(x / y, (Rational.FromDouble(x) / Rational.FromDouble(y)).ToDouble());
        }
    }

    // Item 3: the sign of the determinant is exact. Four points whose rounded determinant is not zero lie on one plane,
    // and a point one unit in the last place off the plane has its true side.
    [Fact]
    public void Orient3D_Gives_The_True_Side()
    {
        var (a, b, c) = (V(0.1, 0.2, 0.3), V(0.7, 0.1, 0.5), V(0.3, 0.9, 0.2));
        var third = Rational.One / 3;
        var d = a + ((b - a) * third) + ((c - a) * third);
        Assert.Equal(0, Predicates.Orient3D(a, b, c, d));
        var up = (b - a).Cross(c - a);
        Assert.Equal(1, Predicates.Orient3D(a, b, c, d + (up * new Rational(1, BigInteger.Pow(10, 40)))));
        Assert.Equal(-1, Predicates.Orient3D(a, c, b, d + (up * new Rational(1, BigInteger.Pow(10, 40)))));
        Assert.Equal(0, Predicates.Orient3D(a, b, a, d));
    }

    // Item 4: a box has 8 vertices, 12 edges and 6 faces, each with its outward normal and its tag, and its exact volume.
    [Fact]
    public void A_Box_Is_A_Closed_Solid_With_Six_Named_Faces()
    {
        var box = KernelSolid.Box(A, Rational.FromDouble(4), Rational.FromDouble(6), Rational.FromDouble(2.5));
        var brep = Brep.Of(box);

        Assert.True(brep.Valid);
        Assert.Equal((8, 12, 6, 0), (brep.Vertices.Count, brep.Edges.Count, brep.Faces.Count, brep.Holes));
        Assert.Equal(Rational.FromDouble(60), box.Volume());
        Assert.Equal(["+x", "-x", "+y", "-y", "+z", "-z"], brep.Faces.Select(face => face.Tag.Element));
        Assert.All(box.Polygons, polygon => Assert.True(polygon.Plane.Side(new Vector3R(Rational.Zero, Rational.Zero, Rational.Zero)) < 0));
    }

    // An L of 10 by 8 with a leg of 3, counter-clockwise, with one item for each side.
    private static readonly Guid[] Sides = [.. Enumerable.Range(1, 6).Select(i => Guid.Parse($"0000000{i}-0000-0000-0000-000000000000"))];

    private static ProfileVertex[] L(double w = 10, double h = 8, double t = 3) =>
    [
        new(Rational.FromDouble(0), Rational.FromDouble(0), Sides[0]),
        new(Rational.FromDouble(w), Rational.FromDouble(0), Sides[1]),
        new(Rational.FromDouble(w), Rational.FromDouble(t), Sides[2]),
        new(Rational.FromDouble(t), Rational.FromDouble(t), Sides[3]),
        new(Rational.FromDouble(t), Rational.FromDouble(h), Sides[4]),
        new(Rational.FromDouble(0), Rational.FromDouble(h), Sides[5]),
    ];

    private static readonly (Vector3R O, Vector3R U, Vector3R V, Vector3R N) XY = (V(0, 0, 0), V(1, 0, 0), V(0, 1, 0), V(0, 0, 1));

    // Item 4: an extrude of a concave profile is one solid of 8 faces: start, end and a side for each line, for each sign
    // of the distance, with the exact volume of the area times the distance.
    [Theory]
    [InlineData(5.0)]
    [InlineData(-5.0)]
    public void An_Extrude_Of_A_Concave_Profile_Is_A_Closed_Solid_With_Named_Faces(double distance)
    {
        var solid = KernelSolid.Extrude(A, L(), XY.O, XY.U, XY.V, XY.N, Rational.FromDouble(distance));
        var brep = Brep.Of(solid);

        Assert.True(brep.Valid);
        Assert.Equal((12, 18, 8, 0), (brep.Vertices.Count, brep.Edges.Count, brep.Faces.Count, brep.Holes));
        Assert.Equal(Rational.FromDouble(5 * ((10 * 3) + (3 * 5))), solid.Volume());
        Assert.Equal(["start", "end", "side", "side", "side", "side", "side", "side"], brep.Faces.Select(face => face.Tag.Element));
        Assert.Equal(Sides, brep.Faces.Skip(2).Select(face => face.Tag.Item!.Value));
    }

    // Items 5 and 6: a plate minus a box through it gives a ring: 16 vertices, 24 edges, 10 faces and 2 holes, so
    // V - E + F - H = 0, a genus of one. The walls of the hole carry the tags of the faces of the box, and the top and
    // the bottom of the plate keep their tags with a hole each.
    [Fact]
    public void A_Box_Through_A_Plate_Gives_A_Ring_With_The_Faces_Of_Both()
    {
        var plate = KernelSolid.Box(A, 10, 10, 2);
        var tool = KernelSolid.Box(B, 4, 4, 4);

        var ring = PlanarBoolean.Subtract(plate, tool)!;
        var brep = Brep.Of(ring);

        Assert.True(brep.Valid);
        Assert.Equal((16, 24, 10, 2), (brep.Vertices.Count, brep.Edges.Count, brep.Faces.Count, brep.Holes));
        Assert.Equal(0, brep.Vertices.Count - brep.Edges.Count + brep.Faces.Count - brep.Holes);
        Assert.Equal((Rational)(200 - 32), ring.Volume());
        Assert.Equal(
            ["+x", "+y", "+z", "-x", "-y", "-z"],
            brep.Faces.Where(face => face.Tag.Feature == A).Select(face => face.Tag.Element).Order(StringComparer.Ordinal));
        Assert.Equal(["+x", "+y", "-x", "-y"], brep.Faces.Where(face => face.Tag.Feature == B).Select(face => face.Tag.Element).Order(StringComparer.Ordinal));
        Assert.All(brep.Faces.Where(face => face.Tag.Element is "+z" or "-z"), face => Assert.Equal([true, false], face.Loops.Select(loop => loop.Outer)));
        Assert.Single(brep.Shells);
    }

    // Item 6: a slot through the middle of a plate of 20 by 10 cuts it into two solids, and a hole goes through one of
    // them. The top and the bottom of the plate then each have two outer loops, and the hole is a loop of the face of
    // its own half: each vertex of a hole lies between the least and the greatest x of the outer loop of its face. The
    // hole is on the right and on the left, so the order of the outer loops cannot hide a hole in the wrong face.
    [Theory]
    [InlineData(5)]
    [InlineData(-5)]
    public void A_Hole_In_One_Half_Of_A_Cut_Plate_Is_A_Loop_Of_The_Face_Of_That_Half(double x)
    {
        var plate = KernelSolid.Box(A, 20, 10, 2);
        var slot = KernelSolid.Box(B, 2, 12, 4);
        var hole = KernelSolid.Box(C, 2, 2, 4).Translate(V(x, 0, 0));

        var cut = PlanarBoolean.Subtract(PlanarBoolean.Subtract(plate, slot)!, hole)!;
        var brep = Brep.Of(cut);

        Assert.True(brep.Valid);
        Assert.Equal((24, 36, 16, 2), (brep.Vertices.Count, brep.Edges.Count, brep.Faces.Count, brep.Holes));
        Assert.Equal(2, brep.Shells.Count);
        Assert.Equal((Rational)(400 - 40 - 8), cut.Volume());
        var caps = brep.Faces.Where(face => face.Tag.Feature == A && face.Tag.Element is "+z" or "-z").ToList();
        Assert.Equal(4, caps.Count);
        Assert.All(caps.Where(face => face.Loops.Count == 2), face =>
        {
            var outer = face.Loops[0].Vertices.Select(v => brep.Vertices[v].X).ToList();
            Assert.All(face.Loops[1].Vertices, v =>
            {
                var hx = brep.Vertices[v].X;
                Assert.True(hx > outer.Min() && hx < outer.Max(), "A hole is a loop of the face of the other half.");
            });
        });
        Assert.Equal(2, caps.Count(face => face.Loops.Count == 2));
    }

    // Item 5: a box that holds the plate leaves nothing, so the subtract refuses, as an empty subtract of Manifold does.
    [Fact]
    public void A_Subtract_That_Leaves_Nothing_Gives_No_Solid()
        => Assert.Null(PlanarBoolean.Subtract(KernelSolid.Box(A, 2, 2, 2), KernelSolid.Box(B, 3, 3, 3)));

    // Item 5: a box of 4 at (5, 5, 5) cuts a notch of 2 by 2 by 2 at a corner of a cube of 10: three faces of the tool
    // and six faces of the cube, one solid.
    [Fact]
    public void A_Box_At_A_Corner_Cuts_A_Notch()
    {
        var cube = KernelSolid.Box(A, 10, 10, 10);
        var tool = KernelSolid.Box(B, 4, 4, 4).Translate(V(5, 5, 5));

        var notched = PlanarBoolean.Subtract(cube, tool)!;
        var brep = Brep.Of(notched);

        Assert.True(brep.Valid);
        Assert.Equal((Rational)(1000 - 8), notched.Volume());
        Assert.Equal(2, brep.Vertices.Count - brep.Edges.Count + brep.Faces.Count - brep.Holes);
        Assert.Equal(["-x", "-y", "-z"], brep.Faces.Where(face => face.Tag.Feature == B).Select(face => face.Tag.Element).Order(StringComparer.Ordinal));
        Assert.Equal(6, brep.Faces.Count(face => face.Tag.Feature == A));
    }

    // Item 7: random models of boxes, L extrudes and moves, minus one or two others, in the kernel and in Manifold. The
    // exact volume and the bounds agree with those of Manifold, each result is a closed solid, V - E + F - H is even and
    // at most 2 for each shell, and a result that Manifold refuses as empty is empty in the kernel too.
    [NativeManifoldFact]
    public void The_Subtract_Agrees_With_Manifold_Over_Random_Models()
    {
        using var backend = new ManifoldGeometryBackend();
        var random = new Random(4183);
        var (compared, empty) = (0, 0);
        for (var model = 0; model < 60; model++)
        {
            var (kernel, key) = Body(backend, random);
            for (var cut = 0; cut < 1 + random.Next(2) && kernel is not null; cut++)
            {
                // One model in ten is cut by a box of 100, which holds each body, so the result is empty.
                var (tool, toolKey) = model % 10 == 9 ? Large(backend) : Body(backend, random);
                var result = PlanarBoolean.Subtract(kernel, tool);
                var resultKey = Guid.NewGuid();
                bool refused;
                try
                {
                    backend.TryGet<IBooleanOps>()!.Subtract(new BodyHandle(resultKey), new BodyHandle(key), new BodyHandle(toolKey));
                    refused = false;
                }
                catch (ManifoldNativeException)
                {
                    refused = true;
                }

                if (refused)
                {
                    Assert.Null(result);
                    empty++;
                    kernel = null;
                    break;
                }

                Assert.NotNull(result);
                var brep = Brep.Of(result);
                Assert.True(brep.Valid, $"Model {model}: the result is not closed.");
                var euler = brep.Vertices.Count - brep.Edges.Count + brep.Faces.Count - brep.Holes;
                Assert.True(euler % 2 == 0 && euler <= 2 * brep.Shells.Count, $"Model {model}: V - E + F - H = {euler}.");
                var (volume, box) = Measure(backend, resultKey);
                Assert.Equal(volume, result.Volume().ToDouble(), Math.Max(1e-9, Math.Abs(volume) * 1e-9));
                var (min, max) = Bounds(result);
                Assert.Equal(box, (min, max));
                (kernel, key) = (result, resultKey);
                compared++;
            }
        }

        Assert.True(compared > 40, $"Only {compared} subtracts were compared.");
        Assert.True(empty > 0, "No subtract was empty.");
    }

    private static (KernelSolid Solid, Guid Key) Large(ManifoldGeometryBackend backend)
    {
        var key = Guid.NewGuid();
        backend.TryGet<IMeshOps>()!.CreateBox(new BodyHandle(key), new BoxParameters(100, 100, 100));
        return (KernelSolid.Box(Guid.NewGuid(), 100, 100, 100), key);
    }

    // A random body in the kernel and in Manifold: a box or an L extrude on XY, moved by whole steps.
    private static (KernelSolid Solid, Guid Key) Body(ManifoldGeometryBackend backend, Random random)
    {
        var key = Guid.NewGuid();
        KernelSolid solid;
        if (random.Next(2) == 0)
        {
            var (x, y, z) = (1 + random.Next(12), 1 + random.Next(12), 1 + random.Next(6));
            solid = KernelSolid.Box(Guid.NewGuid(), x, y, z);
            backend.TryGet<IMeshOps>()!.CreateBox(new BodyHandle(key), new BoxParameters(x, y, z));
        }
        else
        {
            var (w, h, t, d) = (4 + random.Next(8), 4 + random.Next(8), 1 + random.Next(3), 1 + random.Next(6));
            var profile = L(w, h, t);
            solid = KernelSolid.Extrude(Guid.NewGuid(), profile, XY.O, XY.U, XY.V, XY.N, d);
            backend.TryGet<IExtrudeOps>()!.Extrude(new BodyHandle(key), new ExtrudeParameters(
                [.. profile.Select(p => (p.X.ToDouble(), p.Y.ToDouble()))], d, (0, 0, 0), (1, 0, 0), (0, 1, 0), (0, 0, 1)));
        }

        var (dx, dy, dz) = (random.Next(-6, 7), random.Next(-6, 7), random.Next(-4, 5));
        var moved = Guid.NewGuid();
        backend.TryGet<ITransformOps>()!.Translate(new BodyHandle(moved), new BodyHandle(key), dx, dy, dz);
        return (solid.Translate(V(dx, dy, dz)), moved);
    }

    // The volume and the bounds of a solid of Manifold, from its triangles.
    private static (double Volume, ((double, double, double), (double, double, double)) Box) Measure(ManifoldGeometryBackend backend, Guid key)
    {
        var mesh = backend.TryGet<ITessellationOps>()!.Tessellate(new BodyHandle(key));
        var volume = 0.0;
        for (var i = 0; i < mesh.Indices.Length; i += 3)
        {
            double P(int corner, int axis) => mesh.Positions[(mesh.Indices[i + corner] * 3) + axis];
            volume += ((P(0, 0) * ((P(1, 1) * P(2, 2)) - (P(1, 2) * P(2, 1))))
                - (P(0, 1) * ((P(1, 0) * P(2, 2)) - (P(1, 2) * P(2, 0))))
                + (P(0, 2) * ((P(1, 0) * P(2, 1)) - (P(1, 1) * P(2, 0))))) / 6;
        }

        var box = backend.TryGet<IGeometryQuery>()!.GetBoundingBox(new BodyHandle(key));
        return (volume, ((box.MinX, box.MinY, box.MinZ), (box.MaxX, box.MaxY, box.MaxZ)));
    }

    private static ((double, double, double) Min, (double, double, double) Max) Bounds(KernelSolid solid)
    {
        var points = solid.Polygons.SelectMany(polygon => polygon.Vertices).Select(point => point.ToDoubles()).ToList();
        return ((points.Min(p => p.X), points.Min(p => p.Y), points.Min(p => p.Z)), (points.Max(p => p.X), points.Max(p => p.Y), points.Max(p => p.Z)));
    }

    // Finding E137 of the codebase review of 2026-11-05 (b) (TASK-0192): a diamond prism through a plate whose vertex (5, 0)
    // touches the side +x. Each of the two faces +z and -z of the plate has its outer loop and the hole of the diamond.
    // The walk of a loop took the first edge that left a vertex, so on one face the hole joined the outer loop.
    [Fact]
    public void A_Hole_That_Touches_The_Boundary_At_A_Vertex_Is_A_Hole_On_Each_Face()
    {
        ProfileVertex[] diamond =
        [
            new(Rational.FromDouble(5), Rational.FromDouble(0), Sides[0]),
            new(Rational.FromDouble(3), Rational.FromDouble(1), Sides[1]),
            new(Rational.FromDouble(1), Rational.FromDouble(0), Sides[2]),
            new(Rational.FromDouble(3), Rational.FromDouble(-1), Sides[3]),
        ];
        var tool = KernelSolid.Extrude(B, diamond, V(0, 0, -5), XY.U, XY.V, XY.N, Rational.FromDouble(10));
        var brep = Brep.Of(PlanarBoolean.Subtract(KernelSolid.Box(A, 10, 10, 2), tool)!);

        var caps = brep.Faces.Where(face => face.Tag.Feature == A && face.Tag.Element is "+z" or "-z").ToList();
        Assert.Equal(2, caps.Count);
        Assert.All(caps, face => Assert.Equal([true, false], face.Loops.Select(loop => loop.Outer)));
    }

    // Finding E138 of the codebase review of 2026-11-05 (b) (TASK-0192): four bars cut a moat around an island of 8 by 8 in
    // a plate of 20 by 20, and a pin of 1 by 1 goes through the island. On the face +z the plate keeps the moat as its
    // hole, and the island keeps the pin. The owner of a hole was the first outer loop that held it, the plate.
    [Fact]
    public void A_Hole_In_An_Island_Belongs_To_The_Island()
    {
        var cut = KernelSolid.Box(A, 20, 20, 2);
        foreach (var (sx, sy, x, y) in new[] { (10.0, 1.0, 0.0, 4.5), (10.0, 1.0, 0.0, -4.5), (1.0, 10.0, 4.5, 0.0), (1.0, 10.0, -4.5, 0.0) })
            cut = PlanarBoolean.Subtract(cut, KernelSolid.Box(B, Rational.FromDouble(sx), Rational.FromDouble(sy), 4).Translate(V(x, y, 0)))!;
        var brep = Brep.Of(PlanarBoolean.Subtract(cut, KernelSolid.Box(B, 1, 1, 4))!);

        var tops = brep.Faces.Where(face => face.Tag.Feature == A && face.Tag.Element == "+z")
            .Select(face => (Right: face.Loops[0].Vertices.Max(v => brep.Vertices[v].X.ToDouble()), Holes: face.Loops.Count(loop => !loop.Outer)))
            .OrderBy(face => face.Right)
            .ToList();
        Assert.Equal([(4.0, 1), (10.0, 1)], tops);
    }
}
