using System.Globalization;
using Engine.Core.Commands;
using Engine.Core.Drawing;
using Engine.Kernel;

namespace Engine.Tests.Drawing;

// Finding E222 of the codebase review of 2026-11-18 (b) (TASK-0445, phase P0.178): the cut of a section by the cut of the
// owned kernel by a plane (ADR-0057 item 10, ADR-0169 item 3). Until TASK-0445 the read subtracted a box that held the half
// space on the side of the eye. Each test here compares the two paths on the solids of the engine: the box, the plate with
// a hole, the sphere, the cone through its axis (finding E223) and the panels of the bookcase (K10a), with cut planes in
// general places, through vertices and edges, and on a face.
public sealed class SectionCutTests
{
    private static readonly Guid Body = Guid.Parse("b0d1e5aa-0000-0000-0000-000000000000");
    private static readonly Guid View = Guid.Parse("5ec710aa-0000-0000-0000-000000000000");

    private static Vector3R V(double x, double y, double z) => Vector3R.FromDoubles(x, y, z);

    private static ViewFrame Front => ViewFrame.Of(StandardView.Front);

    private static ViewFrame Top => ViewFrame.Of(StandardView.Top);

    // A parent whose right is (3, 4, 0), so a section with the look "left" has a slanted cut plane with an exact frame.
    private static ViewFrame Slanted => new(new Vector3R(3, 4, 0), new Vector3R(0, 0, 5), new Vector3R(20, -15, 0));

    private static ViewFrame Section(ViewFrame parent, string look) => SectionCut.Frame(parent, look) ?? throw new InvalidOperationException(look);

    // The reference: the cut of ADR-0169 item 3 until TASK-0445, a subtract of the box of the points P + a R' + b U' + c E'
    // with |a| <= A, |b| <= A and 0 <= c <= A, where A is 1 plus the largest of |a|, |b| and |c| over the vertices of the
    // solid, each face with the tag of the view.
    internal static KernelSolid? BoxCut(KernelSolid solid, Vector3R point, ViewFrame frame, Guid view)
    {
        var (r, u, e) = (frame.Right, frame.Up, frame.TowardEye);
        var (rr, uu, ee) = (r.Dot(r), u.Dot(u), e.Dot(e));
        var extent = Rational.Zero;
        foreach (var vertex in solid.Polygons.SelectMany(polygon => polygon.Vertices))
        {
            var d = vertex - point;
            foreach (var value in new[] { d.Dot(r) / rr, d.Dot(u) / uu, d.Dot(e) / ee })
            {
                var size = value.Sign < 0 ? -value : value;
                if (size > extent)
                    extent = size;
            }
        }

        var a = extent + Rational.One;
        var corners = new Vector3R[8];
        for (var i = 0; i < 8; i++)
        {
            var (x, y, z) = ((i & 1) != 0 ? a : -a, (i & 2) != 0 ? a : -a, (i & 4) != 0 ? a : Rational.Zero);
            corners[i] = point + (r * x) + (u * y) + (e * z);
        }

        int[][] quads = [[1, 3, 7, 5], [0, 4, 6, 2], [2, 6, 7, 3], [0, 1, 5, 4], [4, 5, 7, 6], [0, 2, 3, 1]];
        var triangles = quads.SelectMany(q => new[] { q[0], q[1], q[2], q[0], q[2], q[3] }).ToArray();
        var box = KernelSolid.Mesh(SectionCut.Tag(view).Feature, SectionCut.Tag(view).Element, corners, triangles);
        return PlanarBoolean.Subtract(solid, box);
    }

    private static List<(double X, double Y)> Circle(double radius)
        => CreateCylinderCommandHandler.Circle(radius);

    private static IReadOnlyList<(Rational X, Rational Y)> Exact(List<(double X, double Y)> circle)
        => [.. circle.Select(p => (Rational.FromDouble(p.X), Rational.FromDouble(p.Y)))];

    // The plate of SheetSectionTests: 60 by 40 by 10 with a through-hole of radius 8 at (30, 20), in the owned kernel.
    internal static KernelSolid Plate()
    {
        var side = Guid.Parse("51de0000-0000-0000-0000-000000000000");
        ProfileVertex P(double x, double y) => new(Rational.FromDouble(x), Rational.FromDouble(y), side);
        var plate = KernelSolid.Extrude(Body, [P(0, 0), P(60, 0), P(60, 40), P(0, 40)], V(0, 0, 0), V(1, 0, 0), V(0, 1, 0), V(0, 0, 1), 10);
        var tool = KernelSolid.Cylinder(Guid.Parse("c1100000-0000-0000-0000-000000000000"), Exact(Circle(8)), 20).Translate(V(30, 20, 5));
        return PlanarBoolean.Subtract(plate, tool)!;
    }

    // The sphere of CreateSphere with a radius of 20, as KernelProjection reads it.
    internal static KernelSolid Sphere(double radius)
    {
        var (positions, triangles) = CreateSphereCommandHandler.Mesh(radius);
        var vertices = new Vector3R[positions.Length / 3];
        for (var i = 0; i < vertices.Length; i++)
            vertices[i] = V(positions[3 * i], positions[(3 * i) + 1], positions[(3 * i) + 2]);
        return KernelSolid.Mesh(Body, "surface", vertices, triangles);
    }

    // The solids and the cut planes of each case: the point and the frame of the section.
    private static IEnumerable<(KernelSolid Solid, Vector3R Point, ViewFrame Frame)> Cuts(string name)
    {
        switch (name)
        {
            case "box":
            {
                var box = KernelSolid.Box(Body, 20, 20, 20);
                yield return (box, V(3, 1, 2), Section(Front, "left"));
                yield return (box, V(0, 0, 0), Section(Top, "up"));
                yield return (box, V(1, 2, 3), Section(Slanted, "left"));
                yield return (box, V(1, 2, 3), Section(Slanted, "down"));
                // On the face +y, which looks toward the eye, and on the face -y, which looks away.
                yield return (box, V(0, 10, 0), Section(Top, "down"));
                yield return (box, V(0, -10, 0), Section(Top, "down"));
                break;
            }
            case "plate":
            {
                var plate = Plate();
                // Through the axis of the hole, across the axis, past the plate, and on its top face.
                yield return (plate, V(30, 20, 0), Section(Top, "up"));
                yield return (plate, V(0, 0, 5), Section(Front, "down"));
                yield return (plate, V(30, 20, 0), Section(Front, "left"));
                yield return (plate, V(0, 50, 0), Section(Top, "up"));
                yield return (plate, V(0, 0, 10), Section(Front, "down"));
                yield return (plate, V(31, 23, 7), Section(Slanted, "left"));
                break;
            }
            case "sphere":
            {
                var sphere = Sphere(20);
                yield return (sphere, V(0, 0, 0), Section(Front, "left"));
                yield return (sphere, V(0, 0, 7), Section(Front, "down"));
                break;
            }
            case "cone":
            {
                // Finding E223: a cut through the axis of a cone, in the two looks of SheetSectionTests; the plane of the
                // base, and a plane through the apex only.
                var cone = KernelSolid.Cone(Body, Exact(Circle(10)), 20);
                yield return (cone, V(0, 0, 0), Section(Front, "left"));
                yield return (cone, V(0, 0, 0), Section(Top, "up"));
                yield return (cone, V(0, 0, -10), Section(Front, "down"));
                yield return (cone, V(0, 0, 10), Section(Front, "down"));
                break;
            }
            case "bookcase":
            {
                // The five panels of the open case of K10a, each a body: through the left side, across the depth, and on the
                // top face of the shelf "Bas" (z = 618).
                foreach (var part in Assembly.BookcaseCase.Parts.Skip(14))
                {
                    var panel = KernelSolid.Box(Body, Rational.FromDouble(part.SizeX), Rational.FromDouble(part.SizeY), Rational.FromDouble(part.SizeZ))
                        .Translate(V(part.X, part.Y, part.Z));
                    yield return (panel, V(-291, 0, 1000), Section(Front, "left"));
                    yield return (panel, V(0, 0, 0), Section(Top, "down"));
                    yield return (panel, V(0, 0, 618), Section(Front, "down"));
                }

                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(name), name, "No such case.");
        }
    }

    private static string Text(Rational value) => value.ToString();

    private static string Text(Vector3R p) => $"({Text(p.X)} {Text(p.Y)} {Text(p.Z)})";

    // A polygon as text: its tag, its plane and its vertices from the least one, so two equal polygons that start at another
    // vertex give one text.
    private static string Text(KernelPolygon polygon)
        => $"{polygon.Tag} {Text(polygon.Plane.Normal)} {Text(polygon.Plane.Offset)} {Ring(polygon.Vertices)}";

    // A closed ring of vertices as text, from its least vertex.
    private static string Ring(IReadOnlyList<Vector3R> vertices)
    {
        static int Compare(Vector3R a, Vector3R b)
        {
            var x = a.X.CompareTo(b.X);
            if (x != 0)
                return x;
            var y = a.Y.CompareTo(b.Y);
            return y != 0 ? y : a.Z.CompareTo(b.Z);
        }

        var least = 0;
        for (var i = 1; i < vertices.Count; i++)
        {
            if (Compare(vertices[i], vertices[least]) < 0)
                least = i;
        }

        return string.Join(" ", Enumerable.Range(0, vertices.Count).Select(i => Text(vertices[(least + i) % vertices.Count])));
    }

    // The faces of a B-Rep as text: the tag, the plane and the loops of each face. A loop starts at its least vertex and
    // has no vertex where it goes straight on, because the pieces of a partition put more vertices on an edge.
    private static string[] Faces(Brep brep)
    {
        string Loop(BrepLoop loop)
        {
            var points = loop.Vertices.Select(v => brep.Vertices[v]).ToList();
            for (var i = 0; points.Count > 3 && i < points.Count;)
            {
                var (before, at, after) = (points[(i + points.Count - 1) % points.Count], points[i], points[(i + 1) % points.Count]);
                if ((at - before).Cross(after - at).IsZero && (at - before).Dot(after - at).Sign > 0)
                {
                    points.RemoveAt(i);
                    i = 0;
                }
                else
                {
                    i++;
                }
            }

            return (loop.Outer ? "outer " : "hole ") + Ring(points);
        }

        return [.. brep.Faces.Select(face => $"{face.Tag} {Text(face.Plane.Normal)} {Text(face.Plane.Offset)}: "
            + string.Join(" / ", face.Loops.Select(Loop).Order(StringComparer.Ordinal))).Order(StringComparer.Ordinal)];
    }

    // The sum of the Newell vectors of the polygons along the normal: the area of the polygons times the length of the normal.
    private static Rational Area(IEnumerable<KernelPolygon> polygons, Vector3R normal)
    {
        var sum = Rational.Zero;
        foreach (var polygon in polygons)
        {
            var newell = new Vector3R(Rational.Zero, Rational.Zero, Rational.Zero);
            for (var i = 0; i < polygon.Vertices.Count; i++)
                newell += polygon.Vertices[i].Cross(polygon.Vertices[(i + 1) % polygon.Vertices.Count]);
            sum += newell.Dot(normal);
        }

        return sum / 2;
    }

    // A line of a view as text, with its two ends and its two tags in one order.
    private static string Text(ViewLine line)
    {
        var (a, b) = ($"{Text(line.From.X)} {Text(line.From.Y)}", $"{Text(line.To.X)} {Text(line.To.Y)}");
        var (first, second) = (line.First.ToString(), line.Second.ToString());
        return string.Join(" | ", line.Kind, line.Visible, string.CompareOrdinal(a, b) < 0 ? a + " " + b : b + " " + a,
            string.CompareOrdinal(first, second) < 0 ? first + " " + second : second + " " + first);
    }

    // The hatch of the cut faces of a B-Rep on the plane of the view, each line with its ends in one order, to 1e-9.
    private static List<string> Hatch(Brep brep, ViewFrame frame)
    {
        var tag = SectionCut.Tag(View);
        var loops = brep.Faces.Where(face => face.Tag == tag).SelectMany(face => face.Loops)
            .Select(loop => (IReadOnlyList<(double X, double Y)>)[.. loop.Vertices.Select(v => (brep.Vertices[v].Dot(frame.Right).ToDouble(), brep.Vertices[v].Dot(frame.Up).ToDouble()))])
            .ToList();
        static string P(double x, double y) => string.Create(CultureInfo.InvariantCulture, $"{Math.Round(x, 9)},{Math.Round(y, 9)}");
        return [.. SectionCut.Hatch(Body, loops, true).Select(line => (P(line.FromX, line.FromY), P(line.ToX, line.ToY)))
            .Select(ends => string.CompareOrdinal(ends.Item1, ends.Item2) < 0 ? ends.Item1 + " " + ends.Item2 : ends.Item2 + " " + ends.Item1)
            .Order(StringComparer.Ordinal)];
    }

    // ADR-0169 item 3 and ADR-0057 item 10: the cut of the kernel gives the section that the subtract of the box gave. For each
    // cut plane of each case: both paths leave material or both leave none; the polygons of the solid are the same polygons,
    // with their tags and their exact vertices, up to their order and the first vertex; the cap faces lie on the cut plane
    // with the tag of the view and look toward the eye, with the same exact area; both results are closed with the same exact
    // volume; and the lines of the view and the hatch of the cut faces are the same, up to the order of the two ends of a
    // line. Both results are closed and share each polygon of the solid, so their caps have one boundary on one plane and
    // are the same region.
    [Theory]
    [InlineData("box")]
    [InlineData("plate")]
    [InlineData("sphere")]
    [InlineData("cone")]
    [InlineData("bookcase")]
    public void The_Cut_By_A_Plane_Gives_The_Section_Of_The_Subtract_Of_A_Box(string name)
    {
        // The subtract first splits the polygons of the solid by the partition of the solid itself. The facets of the sphere
        // are not exactly convex, because each vertex is the double of a sine, so that partition splits some of them; for
        // the sphere the two paths give the same faces of the B-Rep, but not the same polygons.
        var samePolygons = name != "sphere";
        var tag = SectionCut.Tag(View);
        var count = 0;
        foreach (var (solid, point, frame) in Cuts(name))
        {
            var place = $"{name} {count++} at {Text(point)} toward {Text(frame.TowardEye)}";
            var box = BoxCut(solid, point, frame, View);
            var cut = SectionCut.Apply(solid, point, frame, View);
            Assert.True((box is null) == (cut is null), place + ": one path leaves material and the other none.");
            if (box is null || cut is null)
                continue;

            string[] Solid(KernelSolid result) => [.. result.Polygons.Where(p => p.Tag != tag).Select(Text).Order(StringComparer.Ordinal)];
            if (samePolygons)
                Assert.Equal(Solid(box), Solid(cut));
            Assert.All(cut.Polygons.Where(p => p.Tag != tag), p => Assert.Contains(solid.Polygons, q => q.Tag == p.Tag && q.Plane == p.Plane));

            var plane = new PlaneR(frame.TowardEye, frame.TowardEye.Dot(point)).Canonical();
            var caps = cut.Polygons.Where(p => p.Tag == tag).ToList();
            Assert.All(caps, p => Assert.Equal(plane, p.Plane));
            Assert.All(caps, p => Assert.All(p.Vertices, v => Assert.Equal(0, plane.Side(v))));
            Assert.All(box.Polygons.Where(p => p.Tag == tag), p => Assert.Equal(plane, p.Plane));
            Assert.Equal(Area(box.Polygons.Where(p => p.Tag == tag), frame.TowardEye), Area(caps, frame.TowardEye));
            Assert.True(caps.Count <= box.Polygons.Count(p => p.Tag == tag), place);

            var (boxBrep, cutBrep) = (Brep.Of(box), Brep.Of(cut));
            Assert.True(boxBrep.Valid && cutBrep.Valid, place + ": a result is not closed.");
            Assert.Equal(box.Volume(), cut.Volume());
            Assert.Equal(Faces(boxBrep), Faces(cutBrep));
            Assert.Equal(
                ViewEdges.Of([(Body, boxBrep)], frame).Select(Text).Order(StringComparer.Ordinal),
                ViewEdges.Of([(Body, cutBrep)], frame).Select(Text).Order(StringComparer.Ordinal));
            Assert.Equal(Hatch(boxBrep, frame), Hatch(cutBrep, frame));
        }

        Assert.True(count >= 2, name);
    }

    // Finding E222: the measure of the work is the count of the splits of a polygon by a plane (Bsp.Split), on this thread, and
    // not a time (ADR-0173 item 4). The sphere of radius 20 (3,968 triangles), cut through its centre along Front with the look
    // "left": the subtract of the box splits 6,132,711 times, most of them in the partition of the sphere itself, and gives
    // 3,025 polygons with 37 cap pieces. The cut of the kernel splits 10,220 times: 3,968 for the triangles, and the rest in
    // the partition of the walls of the cap and the clip of its square. It gives 2,074 polygons with 28 cap pieces. Each count
    // is exact, so a change of the work of either path fails this test.
    [Fact]
    public void The_Cut_Of_A_Sphere_Splits_Far_Fewer_Polygons_Than_The_Subtract_Of_A_Box()
    {
        var sphere = Sphere(20);
        var (point, frame) = (V(0, 0, 0), Section(Front, "left"));
        var tag = SectionCut.Tag(View);

        var start = Bsp.SplitsOnThisThread;
        var box = BoxCut(sphere, point, frame, View)!;
        var boxSplits = Bsp.SplitsOnThisThread - start;
        start = Bsp.SplitsOnThisThread;
        var cut = SectionCut.Apply(sphere, point, frame, View)!;
        var cutSplits = Bsp.SplitsOnThisThread - start;

        Assert.Equal(
            (6_132_711L, 3_025, 37),
            (boxSplits, box.Polygons.Count, box.Polygons.Count(p => p.Tag == tag)));
        Assert.Equal(
            (10_220L, 2_074, 28),
            (cutSplits, cut.Polygons.Count, cut.Polygons.Count(p => p.Tag == tag)));
        Assert.True(cutSplits * 100 < boxSplits, $"{cutSplits} splits against {boxSplits}.");
    }
}
