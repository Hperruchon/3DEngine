namespace Engine.Kernel;

// One vertex of a closed profile on its frame, and the item that names the side from this vertex to the next one: the
// line, or the arc or circle whose facets that side is (ADR-0057 item 4, the answer of the curved sides).
public readonly record struct ProfileVertex(Rational X, Rational Y, Guid SideItem);

// ADR-0057 item 4: a closed solid of the kernel, as convex planar polygons with outward normals. A concave face is cut
// into triangles, because the partition of item 5 splits a convex polygon only; the topology of item 6 joins the pieces
// of one plane and one tag again.
public sealed class KernelSolid
{
    public IReadOnlyList<KernelPolygon> Polygons { get; }

    internal KernelSolid(IReadOnlyList<KernelPolygon> polygons) => Polygons = polygons;

    // A box with its centre at the origin, as CreateBox makes it, with the faces +x, -x, +y, -y, +z and -z.
    public static KernelSolid Box(Guid feature, Rational sizeX, Rational sizeY, Rational sizeZ)
    {
        var two = (Rational)2;
        var (hx, hy, hz) = (sizeX / two, sizeY / two, sizeZ / two);
        Vector3R P(int x, int y, int z) => new(x > 0 ? hx : -hx, y > 0 ? hy : -hy, z > 0 ? hz : -hz);
        return Closed(feature,
        [
            ("+x", null, [P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1)]),
            ("-x", null, [P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1)]),
            ("+y", null, [P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1)]),
            ("-y", null, [P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1)]),
            ("+z", null, [P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1)]),
            ("-z", null, [P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), P(1, -1, -1)]),
        ]);
    }

    // An extrude of a closed profile on the frame (origin, u, v, normal) by a signed distance: the faces start, end, and
    // one side for each vertex of the profile, named by its side item. A point (x, y) of the profile is origin + x u + y v.
    public static KernelSolid Extrude(Guid feature, IReadOnlyList<ProfileVertex> profile, Vector3R origin, Vector3R u, Vector3R v, Vector3R normal, Rational distance)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (profile.Count < 3)
            throw new ArgumentException("A profile has at least three vertices.", nameof(profile));
        if (distance.IsZero)
            throw new ArgumentException("An extrude has a distance that is not zero.", nameof(distance));
        Vector3R At(ProfileVertex p, Rational h) => origin + (u * p.X) + (v * p.Y) + (normal * h);
        var bottom = profile.Select(p => At(p, Rational.Zero)).ToArray();
        var top = profile.Select(p => At(p, distance)).ToArray();
        var faces = new List<(string Element, Guid? Item, IReadOnlyList<Vector3R> Vertices)>
        {
            ("start", null, [.. bottom.Reverse()]),
            ("end", null, top),
        };
        for (var i = 0; i < profile.Count; i++)
        {
            var j = (i + 1) % profile.Count;
            faces.Add(("side", profile[i].SideItem, [bottom[i], bottom[j], top[j], top[i]]));
        }

        return Closed(feature, faces);
    }

    // ADR-0076 item 3 (phase K5a, TASK-0238): a cylinder with its centre at the origin and its axis along +z, as
    // CreateCylinder makes it, from the polygon of its circle: the faces +z, -z, and side, the facets with one name and
    // no item. The caller gives the polygon counter-clockwise, as ADR-0040 item 2 computes it.
    public static KernelSolid Cylinder(Guid feature, IReadOnlyList<(Rational X, Rational Y)> circle, Rational height)
    {
        ArgumentNullException.ThrowIfNull(circle);
        if (circle.Count < 3)
            throw new ArgumentException("A circle has at least three vertices.", nameof(circle));
        if (height.Sign <= 0)
            throw new ArgumentException("A cylinder has a height greater than zero.", nameof(height));
        var half = height / (Rational)2;
        var bottom = circle.Select(p => new Vector3R(p.X, p.Y, -half)).ToArray();
        var top = circle.Select(p => new Vector3R(p.X, p.Y, half)).ToArray();
        var faces = new List<(string Element, Guid? Item, IReadOnlyList<Vector3R> Vertices)>
        {
            ("-z", null, [.. bottom.Reverse()]),
            ("+z", null, top),
        };
        for (var i = 0; i < circle.Count; i++)
        {
            var j = (i + 1) % circle.Count;
            faces.Add(("side", null, [bottom[i], bottom[j], top[j], top[i]]));
        }

        return Closed(feature, faces);
    }

    // The solid moved by an exact offset; each tag stays.
    public KernelSolid Translate(Vector3R offset) => new([.. Polygons.Select(p => p.Moved(offset))]);

    // ADR-0112 item 5 (TASK-0335): the solid turned about the origin; x, y and z are the images of the axes, and each tag
    // stays. A map with a determinant of zero or less is not a turn, because it flattens the solid or turns it inside out.
    public KernelSolid Rotate(Vector3R x, Vector3R y, Vector3R z)
    {
        if (x.Dot(y.Cross(z)).Sign <= 0)
            throw new ArgumentException("A turn has a determinant greater than zero.");
        return new([.. Polygons.Select(p => p.Mapped(x, y, z))]);
    }

    // The exact volume, by the divergence theorem over a fan of each polygon.
    public Rational Volume()
    {
        var sum = Rational.Zero;
        foreach (var polygon in Polygons)
        {
            var a = polygon.Vertices[0];
            for (var i = 1; i + 1 < polygon.Vertices.Count; i++)
                sum += a.Dot(polygon.Vertices[i].Cross(polygon.Vertices[i + 1]));
        }

        return sum / 6;
    }

    // ADR-0089 item 3 (phase K5c, TASK-0288): a cone with its base at -height / 2 and its apex at +height / 2 on the z
    // axis, as CreateCone makes it, from the polygon of its base circle: the faces -z and side, the triangles of the side
    // with one name and no item. The caller gives the polygon counter-clockwise, as ADR-0040 item 2 computes it.
    public static KernelSolid Cone(Guid feature, IReadOnlyList<(Rational X, Rational Y)> circle, Rational height)
    {
        ArgumentNullException.ThrowIfNull(circle);
        if (circle.Count < 3)
            throw new ArgumentException("A circle has at least three vertices.", nameof(circle));
        if (height.Sign <= 0)
            throw new ArgumentException("A cone has a height greater than zero.", nameof(height));
        var half = height / (Rational)2;
        var bottom = circle.Select(p => new Vector3R(p.X, p.Y, -half)).ToArray();
        var apex = new Vector3R(Rational.Zero, Rational.Zero, half);
        var faces = new List<(string Element, Guid? Item, IReadOnlyList<Vector3R> Vertices)>
        {
            ("-z", null, [.. bottom.Reverse()]),
        };
        for (var i = 0; i < circle.Count; i++)
            faces.Add(("side", null, [bottom[i], bottom[(i + 1) % circle.Count], apex]));

        return Closed(feature, faces);
    }

    // ADR-0089 item 5 (phase K5d, TASK-0289): a closed mesh of triangles with one name and no item, as CreateSphere and
    // CreateTorus make it: three indices for each triangle, counter-clockwise when seen from outside.
    public static KernelSolid Mesh(Guid feature, string element, IReadOnlyList<Vector3R> vertices, IReadOnlyList<int> triangles)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(triangles);
        if (triangles.Count == 0 || triangles.Count % 3 != 0)
            throw new ArgumentException("A mesh holds three indices for each triangle.", nameof(triangles));
        var faces = new List<(string Element, Guid? Item, IReadOnlyList<Vector3R> Vertices)>(triangles.Count / 3);
        for (var t = 0; t < triangles.Count; t += 3)
            faces.Add((element, null, [vertices[triangles[t]], vertices[triangles[t + 1]], vertices[triangles[t + 2]]]));
        return Closed(feature, faces);
    }

    // The faces of a new solid: each one with its plane and its tag, cut into convex pieces, and all of them turned when
    // the volume is negative, so that each normal points out for each sign of the distance and each turn of the profile.
    // Finding E187 of the codebase review of 2026-11-08 (TASK-0296): TASK-0288 put Cone between this comment and its method.
    private static KernelSolid Closed(Guid feature, IEnumerable<(string Element, Guid? Item, IReadOnlyList<Vector3R> Vertices)> faces)
    {
        var polygons = new List<KernelPolygon>();
        foreach (var (element, item, vertices) in faces)
        {
            var plane = PlaneOf(vertices) ?? throw new ArgumentException($"The face '{element}' has no plane.");
            polygons.AddRange(Convex(new KernelPolygon(vertices, plane, new FaceTag(feature, element, item))));
        }

        var solid = new KernelSolid(polygons);
        return solid.Volume().Sign < 0 ? new KernelSolid([.. polygons.Select(p => p.Flipped())]) : solid;
    }

    // The plane of a polygon, from its Newell normal, which a polygon with collinear first vertices also has.
    internal static PlaneR? PlaneOf(IReadOnlyList<Vector3R> vertices)
    {
        var n = new Vector3R(Rational.Zero, Rational.Zero, Rational.Zero);
        for (var i = 0; i < vertices.Count; i++)
            n += vertices[i].Cross(vertices[(i + 1) % vertices.Count]);
        return n.IsZero ? null : new PlaneR(n, n.Dot(vertices[0])).Canonical();
    }

    // The polygon itself when it is convex, else its triangles by ear clipping. Each sign is exact: a turn is the sign of
    // the cross product of two edges along the normal of the plane.
    internal static IEnumerable<KernelPolygon> Convex(KernelPolygon polygon)
    {
        var n = polygon.Plane.Normal;
        int Turn(Vector3R a, Vector3R b, Vector3R c) => (b - a).Cross(c - b).Dot(n).Sign;
        var points = polygon.Vertices.ToList();
        var count = points.Count;
        if (Enumerable.Range(0, count).All(i => Turn(points[i], points[(i + 1) % count], points[(i + 2) % count]) >= 0))
        {
            yield return polygon;
            yield break;
        }

        while (points.Count > 3)
        {
            var cut = false;
            for (var i = 0; i < points.Count; i++)
            {
                var (a, b, c) = (points[(i + points.Count - 1) % points.Count], points[i], points[(i + 1) % points.Count]);
                var turn = Turn(a, b, c);
                if (turn == 0)
                {
                    points.RemoveAt(i);
                    cut = true;
                    break;
                }
                if (turn < 0)
                    continue;
                var inside = points.Where(p => p != a && p != b && p != c)
                    .Any(p => Turn(a, b, p) >= 0 && Turn(b, c, p) >= 0 && Turn(c, a, p) >= 0);
                if (inside)
                    continue;
                yield return polygon with { Vertices = [a, b, c] };
                points.RemoveAt(i);
                cut = true;
                break;
            }

            if (!cut)
                throw new ArgumentException($"The face '{polygon.Tag}' is not a simple polygon.");
        }

        yield return polygon with { Vertices = [.. points] };
    }
}
