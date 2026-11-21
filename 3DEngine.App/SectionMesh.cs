using Engine.Kernel;

namespace ThreeDEngine.Desktop;

// ADR-0184 (phase R26, TASK-0447): the cut of the triangles of one body by the plane of a section view. The section is
// presentation (ADR-0007): the client cuts the tessellation that it holds already, and no command, event or Document holds
// the cut. A solid body goes through the cut of the owned kernel by a plane (ADR-0057 item 10), the same cut as the section
// of a sheet, so the cap is a real face of the kept part: convex pieces on the plane that close it. A mesh
// body can be open, so its triangles are clipped and it gets no cap. The plane goes through Point, and Away points to the
// side that goes. Each number of the cut of a solid is exact; the result goes to the GPU in doubles.
internal static class SectionMesh
{
    // How the plane meets a body: it misses the body and the body shows whole, the body is on the side that goes and does
    // not show, or the plane cuts it.
    internal enum Side
    {
        Whole,
        Gone,
        Cut,
    }

    // The cut of one body: the kept triangles, and the triangles and the convex pieces of the cap. Each list is empty for a
    // body that is whole or gone, and the cap is empty for a mesh body.
    internal sealed record Result(
        Side Side,
        double[] Positions,
        uint[] Indices,
        double[] CapPositions,
        uint[] CapIndices,
        IReadOnlyList<IReadOnlyList<(double X, double Y, double Z)>> CapPieces)
    {
        public static Result Of(Side side) => new(side, [], [], [], [], []);
    }

    // The tag of each piece of a cap.
    private static readonly FaceTag CapTag = new(Guid.Empty, "section");

    // The cut of a body: its triangles, three positions for each vertex and three indices for each triangle, by the plane
    // through the point whose side Away goes. Solid is true for a body of the kernel, which is closed, and false for a mesh
    // body. The side of each vertex is exact, so a body that touches the plane only is whole or gone.
    internal static Result Cut(double[] positions, uint[] indices, (double X, double Y, double Z) point, (double X, double Y, double Z) away, bool solid)
    {
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentNullException.ThrowIfNull(indices);
        var normal = Vector3R.FromDoubles(away.X, away.Y, away.Z);
        if (normal.IsZero)
            throw new ArgumentException("A section plane has a normal that is not zero.", nameof(away));
        var at = Vector3R.FromDoubles(point.X, point.Y, point.Z);
        var plane = new PlaneR(normal, normal.Dot(at));
        var vertices = new Vector3R[positions.Length / 3];
        var (behind, front) = (false, false);
        for (var i = 0; i < vertices.Length; i++)
        {
            vertices[i] = Vector3R.FromDoubles(positions[3 * i], positions[(3 * i) + 1], positions[(3 * i) + 2]);
            var side = plane.Side(vertices[i]);
            behind |= side < 0;
            front |= side > 0;
        }

        if (!front)
            return Result.Of(Side.Whole);
        if (!behind)
            return Result.Of(Side.Gone);
        return solid ? CutSolid(vertices, indices, at, normal) : Clip(positions, indices, point, away);
    }

    // A solid body: the solid of the kernel of its triangles, cut by the plane with a cap. A triangle of zero area has no
    // plane, so it does not go into the solid; its edges lie on one line, and the cut cancels such edges (ADR-0057 item 10).
    private static Result CutSolid(Vector3R[] vertices, uint[] indices, Vector3R point, Vector3R normal)
    {
        var triangles = new List<int>(indices.Length);
        for (var t = 0; t + 2 < indices.Length; t += 3)
        {
            var (a, b, c) = (vertices[indices[t]], vertices[indices[t + 1]], vertices[indices[t + 2]]);
            if ((b - a).Cross(c - a).IsZero)
                continue;
            triangles.AddRange([(int)indices[t], (int)indices[t + 1], (int)indices[t + 2]]);
        }

        if (triangles.Count == 0)
            return Result.Of(Side.Whole);
        var cut = PlanarBoolean.Cut(KernelSolid.Mesh(Guid.Empty, "surface", vertices, triangles), point, normal, CapTag);
        if (cut is null)
            return Result.Of(Side.Gone);

        var (kept, cap) = (new Fan(), new Fan());
        var pieces = new List<IReadOnlyList<(double X, double Y, double Z)>>();
        foreach (var polygon in cut.Polygons)
        {
            var points = polygon.Vertices.Select(v => (v.X.ToDouble(), v.Y.ToDouble(), v.Z.ToDouble())).ToArray();
            if (polygon.Tag == CapTag)
            {
                cap.Add(points);
                pieces.Add(points);
            }
            else
            {
                kept.Add(points);
            }
        }

        return new Result(Side.Cut, [.. kept.Positions], [.. kept.Indices], [.. cap.Positions], [.. cap.Indices], pieces);
    }

    // A mesh body: each triangle clipped to the side that stays, in doubles, with no cap. A mesh need not be closed, so a cap
    // has no boundary that is sure to close.
    private static Result Clip(double[] positions, uint[] indices, (double X, double Y, double Z) point, (double X, double Y, double Z) away)
    {
        double Distance(uint index)
            => ((positions[3 * index] - point.X) * away.X) + ((positions[(3 * index) + 1] - point.Y) * away.Y) + ((positions[(3 * index) + 2] - point.Z) * away.Z);
        (double X, double Y, double Z) At(uint index) => (positions[3 * index], positions[(3 * index) + 1], positions[(3 * index) + 2]);

        var kept = new Fan();
        for (var t = 0; t + 2 < indices.Length; t += 3)
        {
            var corners = new[] { indices[t], indices[t + 1], indices[t + 2] };
            var points = new List<(double X, double Y, double Z)>(4);
            for (var i = 0; i < 3; i++)
            {
                var (p, q) = (corners[i], corners[(i + 1) % 3]);
                var (dp, dq) = (Distance(p), Distance(q));
                if (dp <= 0)
                    points.Add(At(p));
                if ((dp < 0 && dq > 0) || (dp > 0 && dq < 0))
                {
                    var s = dp / (dp - dq);
                    var (a, b) = (At(p), At(q));
                    points.Add((a.X + ((b.X - a.X) * s), a.Y + ((b.Y - a.Y) * s), a.Z + ((b.Z - a.Z) * s)));
                }
            }

            if (points.Count >= 3)
                kept.Add(points);
        }

        return new Result(Side.Cut, [.. kept.Positions], [.. kept.Indices], [], [], []);
    }

    // The triangles of convex polygons, as a fan from the first vertex of each, in the turn of the polygon.
    private sealed class Fan
    {
        public List<double> Positions { get; } = [];

        public List<uint> Indices { get; } = [];

        public void Add(IReadOnlyList<(double X, double Y, double Z)> polygon)
        {
            var first = (uint)(Positions.Count / 3);
            foreach (var (x, y, z) in polygon)
                Positions.AddRange([x, y, z]);
            for (var i = 1; i + 1 < polygon.Count; i++)
                Indices.AddRange([first, first + (uint)i, first + (uint)i + 1]);
        }
    }

    // ADR-0184 item 5: the hatch of a cap, lines at 45 degrees to the two axes of the plane, Spacing apart, as segments. A
    // line of the hatch is the set of points whose part along w = (u - v) / sqrt(2) is a whole multiple of the spacing, and
    // each convex piece of the cap gives one segment of each such line that crosses it. The axes of the plane come from its
    // normal only, so the hatch of two bodies on one plane is one pattern.
    internal static IReadOnlyList<((double X, double Y, double Z) A, (double X, double Y, double Z) B)> Hatch(
        IReadOnlyList<IReadOnlyList<(double X, double Y, double Z)>> pieces, (double X, double Y, double Z) normal, double spacing)
    {
        ArgumentNullException.ThrowIfNull(pieces);
        if (!(spacing > 0) || !double.IsFinite(spacing))
            throw new ArgumentOutOfRangeException(nameof(spacing), spacing, "The spacing of a hatch is a positive number.");
        var (u, v) = Axes(normal);
        var w = Unit((u.X - v.X, u.Y - v.Y, u.Z - v.Z));
        double Along((double X, double Y, double Z) p) => (p.X * w.X) + (p.Y * w.Y) + (p.Z * w.Z);

        var segments = new List<((double X, double Y, double Z) A, (double X, double Y, double Z) B)>();
        foreach (var piece in pieces)
        {
            if (piece.Count < 3)
                continue;
            var values = piece.Select(Along).ToArray();
            var (low, high) = (values.Min(), values.Max());
            for (var k = Math.Ceiling(low / spacing); k * spacing <= high; k++)
            {
                var line = k * spacing;
                var crossings = new List<(double X, double Y, double Z)>(2);
                for (var i = 0; i < piece.Count; i++)
                {
                    var j = (i + 1) % piece.Count;
                    var (a, b) = (values[i] - line, values[j] - line);
                    if (a == 0)
                        crossings.Add(piece[i]);
                    else if ((a < 0 && b > 0) || (a > 0 && b < 0))
                    {
                        var s = a / (a - b);
                        var (p, q) = (piece[i], piece[j]);
                        crossings.Add((p.X + ((q.X - p.X) * s), p.Y + ((q.Y - p.Y) * s), p.Z + ((q.Z - p.Z) * s)));
                    }
                }

                // A convex piece meets a line in a segment: its two ends are the first and the last crossing along the line.
                if (crossings.Count < 2)
                    continue;
                var direction = Unit((u.X + v.X, u.Y + v.Y, u.Z + v.Z));
                var ordered = crossings.OrderBy(p => (p.X * direction.X) + (p.Y * direction.Y) + (p.Z * direction.Z)).ToList();
                if (ordered[0] != ordered[^1])
                    segments.Add((ordered[0], ordered[^1]));
            }
        }

        return segments;
    }

    // Two unit axes of a plane: u = n x (the axis of the smallest part of n), v = n x u, as the cut of the kernel makes them,
    // so the hatch of a plane of the origin is at 45 degrees to its two axes.
    private static ((double X, double Y, double Z) U, (double X, double Y, double Z) V) Axes((double X, double Y, double Z) normal)
    {
        var n = Unit(normal);
        var (ax, ay, az) = (Math.Abs(n.X), Math.Abs(n.Y), Math.Abs(n.Z));
        (double X, double Y, double Z) axis = ax <= ay && ax <= az ? (1, 0, 0) : ay <= az ? (0, 1, 0) : (0, 0, 1);
        var u = Unit(Cross(n, axis));
        return (u, Cross(n, u));
    }

    private static (double X, double Y, double Z) Cross((double X, double Y, double Z) a, (double X, double Y, double Z) b)
        => ((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z), (a.X * b.Y) - (a.Y * b.X));

    private static (double X, double Y, double Z) Unit((double X, double Y, double Z) a)
    {
        var length = Math.Sqrt((a.X * a.X) + (a.Y * a.Y) + (a.Z * a.Z));
        if (!(length > 0))
            throw new ArgumentException("A direction has a length that is not zero.", nameof(a));
        return (a.X / length, a.Y / length, a.Z / length);
    }
}
