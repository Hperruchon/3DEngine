namespace Engine.Kernel;

public static class PlanarBoolean
{
    // TASK-0454 (phase P0.184): the count of the booleans of two solids on this thread, each Subtract, Union and Intersect, a
    // measure of the work of a command or a query for a test, beside the counts of Bsp. The projection of the engine keeps the
    // result of each boolean of the history of a body, so one Subtract and one read of its faces make one boolean.
    [ThreadStatic]
    private static long booleans;

    internal static long BooleansOnThisThread => booleans;

    // ADR-0057 item 5: a minus b, in the steps of csg.js: the parts of a outside b, and the parts of b inside a, turned.
    // Each part keeps the tag of its face. Null when the result is empty, which the caller refuses.
    public static KernelSolid? Subtract(KernelSolid a, KernelSolid b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        booleans++;
        var left = new Bsp(a.Polygons);
        var right = new Bsp(b.Polygons);
        left.Invert();
        left.ClipTo(right);
        right.ClipTo(left);
        right.Invert();
        right.ClipTo(left);
        right.Invert();
        left.Build(right.AllPolygons());
        left.Invert();
        var polygons = left.AllPolygons();
        if (polygons.Count == 0)
            return null;
        var result = new KernelSolid(polygons);
        return result.Volume().Sign > 0 ? result : null;
    }

    // ADR-0181 item 5 (phase K16, TASK-0433): a and b together, in the steps of the union of csg.js: the parts of a outside
    // b, and the parts of b outside a. A face that the two solids share on one plane with opposite normals goes, and a
    // face that they share with one normal stays one time. No part turns, and each part keeps the tag of its face.
    public static KernelSolid? Union(KernelSolid a, KernelSolid b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        booleans++;
        var left = new Bsp(a.Polygons);
        var right = new Bsp(b.Polygons);
        left.ClipTo(right);
        right.ClipTo(left);
        right.Invert();
        right.ClipTo(left);
        right.Invert();
        left.Build(right.AllPolygons());
        return Solid(left.AllPolygons());
    }

    // ADR-0181 item 5: the volume that a and b share, in the steps of the intersection of csg.js: the parts of a inside b,
    // and the parts of b inside a. No part turns, and each part keeps the tag of its face. Two solids that only touch, or
    // that are apart, share no volume, so the result is null, which the caller refuses, as an empty subtract is. TASK-0438
    // (finding E233): the pair check gives its token, and each walk that splits reads it at each node.
    public static KernelSolid? Intersect(KernelSolid a, KernelSolid b) => Intersect(a, b, CancellationToken.None);

    public static KernelSolid? Intersect(KernelSolid a, KernelSolid b, CancellationToken cancel)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        booleans++;
        var left = new Bsp(a.Polygons, cancel);
        var right = new Bsp(b.Polygons, cancel);
        left.Invert();
        right.ClipTo(left, cancel);
        right.Invert();
        left.ClipTo(right, cancel);
        right.ClipTo(left, cancel);
        left.Build(right.AllPolygons(), cancel);
        left.Invert();
        return Solid(left.AllPolygons());
    }

    // ADR-0057 item 10 (TASK-0445, finding E222): the part of the solid behind the plane through the point, whose normal
    // points to the side that goes, closed by the cap on the plane. Each polygon behind the plane stays, and each polygon
    // that crosses it splits at the exact point of each edge on the plane, as the partition of item 5 splits it; each part
    // keeps the tag of its face. A polygon on the plane stays when it looks along the normal, because the solid is behind
    // it, and goes when it looks the other way. The cap is the region of the plane that the edges of the kept parts on the
    // plane bound: each piece has the tag cap and looks along the normal. Null when no material stays, as for an empty
    // subtract. The result is the subtract of a box that holds the half space in front of the plane, with the cap in fewer
    // pieces and no partition of the solid. TASK-0449 (finding E238 of the third codebase review of 2026-11-21): the split of
    // the polygons grows with their count, but the tree of the walls of the cap costs about the square of the count of the
    // edges of the cap, as ADR-0057 item 10 says. The review counted 6,252 of the 10,220 splits of a sphere in the cap.
    public static KernelSolid? Cut(KernelSolid solid, Vector3R point, Vector3R normal, FaceTag cap)
    {
        ArgumentNullException.ThrowIfNull(solid);
        ArgumentNullException.ThrowIfNull(cap);
        if (normal.IsZero)
            throw new ArgumentException("A cut plane has a normal that is not zero.", nameof(normal));
        var plane = new PlaneR(normal, normal.Dot(point)).Canonical();
        var approximate = Bsp.Approximate.Of(plane);

        // 1. The parts behind the plane, in the order of the polygons. A part on the plane that looks along the normal is in
        // the list of the coplanar parts in front, which stays; one that looks the other way goes.
        var kept = new List<KernelPolygon>();
        var gone = new List<KernelPolygon>();
        foreach (var polygon in solid.Polygons)
        {
            Bsp.Split(plane, approximate, polygon, kept, gone, gone, kept);
            gone.Clear();
        }

        if (kept.Count == 0)
            return null;

        // 2. The edges of the kept parts that lie on the plane, in their direction. The cap uses each one in the other
        // direction, so the result is closed.
        var edges = new List<(Vector3R From, Vector3R To)>();
        foreach (var polygon in kept)
        {
            var sides = Bsp.Sides(plane, approximate, polygon.Vertices);
            for (var i = 0; i < sides.Length; i++)
            {
                var j = (i + 1) % sides.Length;
                if (sides[i] == 0 && sides[j] == 0)
                    edges.Add((polygon.Vertices[i], polygon.Vertices[j]));
            }
        }

        var boundary = CapBoundary(edges);
        if (boundary.Count == 0)
            return Solid(kept);

        // 3. The cap: a square on the plane that holds each edge of the boundary, clipped by a partition of the walls of the
        // boundary. A wall is the plane through an edge and the normal; it looks out of the cap, so each piece of the square
        // inside each wall is convex and inside the cap.
        var (u, v) = InPlane(normal);
        var extent = Rational.Zero;
        foreach (var (from, _) in boundary)
        {
            var d = from - point;
            foreach (var value in new[] { d.Dot(u) / u.Dot(u), d.Dot(v) / v.Dot(v) })
            {
                var size = value.Sign < 0 ? -value : value;
                if (size > extent)
                    extent = size;
            }
        }

        var a = extent + Rational.One;
        var square = new KernelPolygon(
            [point - (u * a) - (v * a), point + (u * a) - (v * a), point + (u * a) + (v * a), point - (u * a) + (v * a)], plane, cap);
        var walls = new List<KernelPolygon>(boundary.Count);
        foreach (var (from, to) in boundary)
        {
            var outward = (to - from).Cross(normal);
            walls.Add(new KernelPolygon([from, to, to + normal, from + normal], new PlaneR(outward, outward.Dot(from)).Canonical(), cap));
        }

        var inside = new Bsp(walls);
        inside.Invert();
        kept.AddRange(inside.Clip([square]));
        return Solid(kept);
    }

    // Two directions on a plane with the normal n: u = n x (the axis of the smallest part of n), and v = n x u, so u, v and n
    // turn as x, y and z.
    private static (Vector3R U, Vector3R V) InPlane(Vector3R n)
    {
        static Rational Size(Rational value) => value.Sign < 0 ? -value : value;
        var axis = Size(n.X) <= Size(n.Y) && Size(n.X) <= Size(n.Z) ? new Vector3R(Rational.One, Rational.Zero, Rational.Zero)
            : Size(n.Y) <= Size(n.Z) ? new Vector3R(Rational.Zero, Rational.One, Rational.Zero)
            : new Vector3R(Rational.Zero, Rational.Zero, Rational.One);
        var u = n.Cross(axis);
        return (u, n.Cross(u));
    }

    // The edges of the cap: the edges of the kept parts on the plane, each in the other direction. The edges on one line
    // split at each end of the others, and two opposite pieces cancel, as two kept parts that meet on the plane share their
    // edge. A run of pieces in one direction joins into one edge. The lines keep the order of their first edge.
    private static List<(Vector3R From, Vector3R To)> CapBoundary(List<(Vector3R From, Vector3R To)> edges)
    {
        var lines = new List<(Vector3R Direction, List<(Rational From, Rational To, Vector3R FromPoint, Vector3R ToPoint)> Edges)>();
        var index = new Dictionary<(Vector3R Direction, Vector3R Foot), int>();
        foreach (var (from, to) in edges)
        {
            // The direction of the line has its first part that is not zero equal to 1, so the two directions of an edge
            // give one line; the foot is the point of the line nearest to the origin.
            var d = to - from;
            if (d.IsZero)
                continue;
            var first = !d.X.IsZero ? d.X : !d.Y.IsZero ? d.Y : d.Z;
            var direction = d * (Rational.One / first);
            var foot = from - (direction * (from.Dot(direction) / direction.Dot(direction)));
            if (!index.TryGetValue((direction, foot), out var line))
            {
                line = lines.Count;
                index.Add((direction, foot), line);
                lines.Add((direction, []));
            }

            lines[line].Edges.Add((from.Dot(direction), to.Dot(direction), from, to));
        }

        var boundary = new List<(Vector3R From, Vector3R To)>();
        foreach (var (_, onLine) in lines)
        {
            // The ends on the line in the order along the direction, each once, with its point.
            var ends = new SortedDictionary<Rational, Vector3R>();
            foreach (var (from, to, fromPoint, toPoint) in onLine)
            {
                ends.TryAdd(from, fromPoint);
                ends.TryAdd(to, toPoint);
            }

            var keys = ends.Keys.ToList();
            var points = ends.Values.ToList();
            // The count of the kept edges along the direction minus the count against it, on each piece between two ends.
            var net = new int[keys.Count];
            foreach (var (from, to, _, _) in onLine)
            {
                var (low, high, sign) = from < to ? (from, to, 1) : (to, from, -1);
                net[keys.BinarySearch(low)] += sign;
                net[keys.BinarySearch(high)] -= sign;
            }

            for (var k = 1; k < net.Length; k++)
                net[k] += net[k - 1];
            for (var k = 0; k + 1 < keys.Count;)
            {
                var count = net[k];
                var end = k + 1;
                while (end + 1 < keys.Count && net[end] == count)
                    end++;
                // The cap goes against the kept edges: from the high end to the low end when they go along the direction.
                for (var n = 0; n < Math.Abs(count); n++)
                    boundary.Add(count > 0 ? (points[end], points[k]) : (points[k], points[end]));
                k = end;
            }
        }

        return boundary;
    }

    // The solid of the polygons, or null when they hold no volume.
    private static KernelSolid? Solid(List<KernelPolygon> polygons)
    {
        if (polygons.Count == 0)
            return null;
        var result = new KernelSolid(polygons);
        return result.Volume().Sign > 0 ? result : null;
    }
}
