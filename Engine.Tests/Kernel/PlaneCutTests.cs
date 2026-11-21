using Engine.Kernel;

namespace Engine.Tests.Kernel;

// ADR-0057 item 10 (TASK-0445, phase P0.178, finding E222): the cut of a solid of the owned kernel by a plane. The side that
// stays, the cap and its tag, the exact split of a polygon that crosses the plane, a face on the plane, and a cap with two
// pieces and with a hole. Engine.Tests/Drawing/SectionCutTests.cs compares the cut with the subtract of a box.
public sealed class PlaneCutTests
{
    private static readonly Guid A = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000000");
    private static readonly Guid B = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000000");
    private static readonly FaceTag Cap = new(Guid.Parse("cccccccc-0000-0000-0000-000000000000"), "cut");

    private static Vector3R V(Rational x, Rational y, Rational z) => new(x, y, z);

    // The area of the polygons along a unit normal of an axis: half of the sum of their Newell vectors along it.
    private static Rational Area(IEnumerable<KernelPolygon> polygons, Vector3R normal)
    {
        var sum = Rational.Zero;
        foreach (var polygon in polygons)
        {
            for (var i = 0; i < polygon.Vertices.Count; i++)
                sum += polygon.Vertices[i].Cross(polygon.Vertices[(i + 1) % polygon.Vertices.Count]).Dot(normal);
        }

        return sum / 2;
    }

    // A box of 20 at the origin, cut at z = 3 with the normal +z: the part with z <= 3 stays, 20 x 20 x 13, with the faces
    // -z and the four sides of the box, and one cap of 20 x 20 on z = 3 with the tag of the cap, which looks along +z. With the
    // normal -z the part with z >= 3 stays, 20 x 20 x 7, with the face +z.
    [Fact]
    public void A_Cut_Keeps_The_Part_Behind_The_Plane_And_Closes_It_With_One_Cap()
    {
        var box = KernelSolid.Box(A, 20, 20, 20);

        var below = PlanarBoolean.Cut(box, V(0, 0, 3), V(0, 0, 1), Cap)!;

        Assert.Equal((Rational)(20 * 20 * 13), below.Volume());
        Assert.All(below.Polygons, p => Assert.All(p.Vertices, v => Assert.True(v.Z <= 3)));
        Assert.True(Brep.Of(below).Valid);
        var cap = Assert.Single(below.Polygons, p => p.Tag == Cap);
        Assert.Equal(new PlaneR(V(0, 0, 1), 3), cap.Plane);
        Assert.Equal((Rational)400, Area([cap], V(0, 0, 1)));
        Assert.Equal(["+x", "+y", "-x", "-y", "-z"], below.Polygons.Where(p => p.Tag != Cap).Select(p => p.Tag.Element).Distinct().Order(StringComparer.Ordinal));

        var above = PlanarBoolean.Cut(box, V(0, 0, 3), V(0, 0, -1), Cap)!;

        Assert.Equal((Rational)(20 * 20 * 7), above.Volume());
        Assert.All(above.Polygons, p => Assert.All(p.Vertices, v => Assert.True(v.Z >= 3)));
        Assert.Equal(new PlaneR(V(0, 0, -1), -3), Assert.Single(above.Polygons, p => p.Tag == Cap).Plane);
        Assert.Contains(above.Polygons, p => p.Tag.Element == "+z");
        Assert.DoesNotContain(above.Polygons, p => p.Tag.Element == "-z");
    }

    // A slanted plane through a box: each part of a polygon that crosses the plane keeps the tag and the plane of its polygon,
    // and each vertex that is not a vertex of the box lies exactly on the plane and on an edge of its polygon. Each vertex of
    // the result is on the plane or behind it. The two sides of the plane give the volume of the box, exactly.
    [Fact]
    public void A_Polygon_That_Crosses_The_Plane_Splits_At_Exact_Points_On_The_Plane()
    {
        var box = KernelSolid.Box(A, 20, 20, 20);
        var (point, normal) = (V(new Rational(1, 3), new Rational(1, 7), 2), V(1, 2, 3));
        var plane = new PlaneR(normal, normal.Dot(point));

        var cut = PlanarBoolean.Cut(box, point, normal, Cap)!;

        var corners = box.Polygons.SelectMany(p => p.Vertices).ToHashSet();
        var crossed = 0;
        foreach (var part in cut.Polygons.Where(p => p.Tag != Cap))
        {
            var source = Assert.Single(box.Polygons, p => p.Tag == part.Tag && p.Plane == part.Plane);
            Assert.All(part.Vertices, v => Assert.True(plane.Side(v) <= 0));
            foreach (var vertex in part.Vertices.Where(v => !corners.Contains(v)))
            {
                crossed++;
                Assert.Equal(0, plane.Side(vertex));
                Assert.Contains(Enumerable.Range(0, source.Vertices.Count), i =>
                {
                    var (a, b) = (source.Vertices[i], source.Vertices[(i + 1) % source.Vertices.Count]);
                    var t = (vertex - a).Dot(b - a) / (b - a).Dot(b - a);
                    return (vertex - a).Cross(b - a).IsZero && t.Sign > 0 && t < 1;
                });
            }
        }

        Assert.True(crossed >= 6);
        Assert.All(cut.Polygons.Where(p => p.Tag == Cap), p => Assert.All(p.Vertices, v => Assert.Equal(0, plane.Side(v))));
        Assert.True(Brep.Of(cut).Valid);
        var other = PlanarBoolean.Cut(box, point, -normal, Cap)!;
        Assert.True(Brep.Of(other).Valid);
        Assert.Equal(box.Volume(), cut.Volume() + other.Volume());
    }

    // A face on the plane stays when it looks along the normal, because the solid is behind it: the cut at the face +z of a
    // box gives the polygons of the box, in their order, and no cap. A face that looks the other way goes: the cut at the face
    // -z with the normal +z, and at the face +z with the normal -z, leave no material.
    [Fact]
    public void A_Face_On_The_Plane_Stays_When_It_Looks_Along_The_Normal_And_Goes_When_It_Looks_Away()
    {
        var box = KernelSolid.Box(A, 20, 20, 20);

        var same = PlanarBoolean.Cut(box, V(0, 0, 10), V(0, 0, 1), Cap)!;

        Assert.Equal(box.Polygons, same.Polygons);
        Assert.Null(PlanarBoolean.Cut(box, V(0, 0, -10), V(0, 0, 1), Cap));
        Assert.Null(PlanarBoolean.Cut(box, V(0, 0, 10), V(0, 0, -1), Cap));
        Assert.Null(PlanarBoolean.Cut(box, V(0, 0, 11), V(0, 0, -1), Cap));
        Assert.Throws<ArgumentException>(() => PlanarBoolean.Cut(box, V(0, 0, 0), V(0, 0, 0), Cap));
    }

    // A plate of 60 x 40 x 10 with a square hole of 16 x 16: the cut through the hole at y = 20 gives two caps of 22 x 10, and
    // the cut across the hole at z = 5 gives one cap of 60 x 40 with a hole of 16 x 16, whose B-Rep face has one hole loop.
    [Fact]
    public void A_Cut_Through_A_Hole_Gives_Two_Caps_And_A_Cut_Across_It_Gives_A_Cap_With_A_Hole()
    {
        var plate = KernelSolid.Box(A, 60, 40, 10).Translate(V(30, 20, 5));
        var tool = KernelSolid.Box(B, 16, 16, 20).Translate(V(30, 20, 5));
        var holed = PlanarBoolean.Subtract(plate, tool)!;

        var through = PlanarBoolean.Cut(holed, V(30, 20, 0), V(0, 1, 0), Cap)!;

        Assert.Equal((Rational)(((60 * 20) - (16 * 8)) * 10), through.Volume());
        Assert.Equal((Rational)440, Area(through.Polygons.Where(p => p.Tag == Cap), V(0, 1, 0)));
        var faces = Brep.Of(through);
        Assert.True(faces.Valid);
        Assert.Equal(2, faces.Faces.Where(face => face.Tag == Cap).Sum(face => face.Loops.Count(loop => loop.Outer)));

        var across = PlanarBoolean.Cut(holed, V(0, 0, 5), V(0, 0, 1), Cap)!;

        Assert.Equal((Rational)(((60 * 40) - (16 * 16)) * 5), across.Volume());
        Assert.Equal((Rational)2144, Area(across.Polygons.Where(p => p.Tag == Cap), V(0, 0, 1)));
        var brep = Brep.Of(across);
        Assert.True(brep.Valid);
        var face = Assert.Single(brep.Faces, face => face.Tag == Cap);
        Assert.Equal((1, 1), (face.Loops.Count(loop => loop.Outer), face.Loops.Count(loop => !loop.Outer)));
    }
}
