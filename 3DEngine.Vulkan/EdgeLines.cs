namespace ThreeDEngine.Vulkan;

// The edges of a body as lines (ADR-0109 items 1 and 2, phase R18b, TASK-0333). The source is the tessellation that the
// renderer has already: an edge of a triangle is a feature edge when it has one triangle only (an open border), more than
// two, or two whose normals differ by more than the limit angle. So the edges of a box show, and the seams between the
// facets of a cylinder do not. The lines are presentation, as the tessellation is (ADR-0007): the Document holds none.
//
// Each edge goes to the GPU as two triangles of six vertices, and the shader edge.vert gives them a width in pixels, so
// the device needs no wide lines. A vertex has the two ends of its edge in the space of the mesh and its corner: the
// fraction along the edge (0 or 1) and the side (-1 or +1).
public static class EdgeLines
{
    // The floats of one vertex: the end A (three), the end B (three), and the corner (two).
    public const int FloatsPerVertex = 8;
    public const uint VertexStride = FloatsPerVertex * sizeof(float);
    public const int VerticesPerEdge = 6;

    // The cosine of the limit angle of 30 degrees between the normals of the two triangles of a feature edge. The value is
    // a constant, so no transcendental function runs.
    public const double CosineOfLimit = 0.8660254037844387;

    // The most edges that a body draws (ADR-0109 item 5). An edge takes 192 bytes on the GPU, so 100 000 edges take about
    // 18 MiB; a mesh with more creases, for example an imported scan, draws no lines.
    public const int MostEdges = 100_000;

    // The most triangles of a mesh whose edges are found at the upload, on the thread of the frame (ADR-0121, finding V167
    // of the codebase review of 2026-11-11, TASK-0345). The review measured 270 ms for 980 000 triangles, so the search
    // takes about 28 ms at this count. A larger mesh finds its edges on the thread pool.
    public const int MostTrianglesAtUpload = 100_000;

    // The corners of the two triangles of one edge: (fraction along the edge, side).
    private static readonly (float Along, float Side)[] Corners = [(0, -1), (1, -1), (1, 1), (0, -1), (1, 1), (0, 1)];

    // The feature edges of a triangle mesh, as pairs of vertex indices. Two vertices at the same position are one vertex,
    // so a mesh with a vertex for each corner of each face gives the same edges as a mesh that shares its vertices.
    public static IReadOnlyList<(int A, int B)> Feature(ReadOnlySpan<double> positions, ReadOnlySpan<uint> indices, double cosineOfLimit = CosineOfLimit)
    {
        var count = positions.Length / 3;
        var canonical = new int[count];
        var first = new Dictionary<(double, double, double), int>(count);
        for (var i = 0; i < count; i++)
        {
            var key = (positions[3 * i], positions[(3 * i) + 1], positions[(3 * i) + 2]);
            if (!first.TryGetValue(key, out var known))
                first[key] = known = i;
            canonical[i] = known;
        }

        // For each edge: the count of its triangles and the normals of the first two.
        var edges = new Dictionary<(int, int), (int Count, (double X, double Y, double Z) First, (double X, double Y, double Z) Second)>();
        for (var t = 0; t + 2 < indices.Length; t += 3)
        {
            var (a, b, c) = (canonical[indices[t]], canonical[indices[t + 1]], canonical[indices[t + 2]]);
            if (a == b || b == c || c == a)
                continue;

            // Finding V166 of the codebase review of 2026-11-11 (TASK-0345): a triangle of zero area across a T-vertex has
            // three different corners, but its long edge is not an edge of the surface. It adds no edge. Finding V175 of the
            // codebase review of 2026-11-14 (TASK-0356): the rounding of a turned body gives such a triangle a small area, so
            // the area is zero within a tolerance relative to the square of its longest side.
            if (Normal(positions, a, b, c) is not { } normal)
                continue;

            foreach (var (p, q) in new[] { (a, b), (b, c), (c, a) })
            {
                var key = Key(p, q);
                edges[key] = edges.TryGetValue(key, out var seen)
                    ? (seen.Count + 1, seen.First, seen.Count == 1 ? normal : seen.Second)
                    : (1, normal, default);
            }
        }

        // Finding V171 of the codebase review of 2026-11-12 (TASK-0350), and V175: a T-vertex M on the side P-Q of a triangle
        // splits the other side of the crack into P-M and M-Q, with a sliver or with no sliver. So the triangle across P-Q is
        // also the triangle across each part. When P-Q has one triangle and a chain of collinear edges of one triangle each
        // goes from P to Q, each part takes the triangle of P-Q, and P-Q is not an edge. So a T-vertex inside a flat face
        // draws no line, also two T-vertices side by side, and a crease along P-Q still draws its parts.
        var single = new Dictionary<int, List<int>>();
        foreach (var ((p, q), edge) in edges)
        {
            if (edge.Count != 1)
                continue;
            (single.TryGetValue(p, out var fromP) ? fromP : single[p] = []).Add(q);
            (single.TryGetValue(q, out var fromQ) ? fromQ : single[q] = []).Add(p);
        }

        foreach (var key in edges.Where(edge => edge.Value.Count == 1).Select(edge => edge.Key).Order().ToList())
        {
            if (!edges.TryGetValue(key, out var across) || across.Count != 1
                || Chain(positions, single, edges, key.Item1, key.Item2) is not { Count: > 1 } parts)
                continue;
            foreach (var part in parts)
                edges[part] = (2, edges[part].First, across.First);
            edges.Remove(key);
        }

        var result = new List<(int A, int B)>();
        foreach (var (key, edge) in edges)
        {
            if (edge.Count != 2 || Dot(edge.First, edge.Second) < cosineOfLimit)
                result.Add(key);
        }

        // The order of a dictionary is not part of its contract; the edges go out in the order of their indices.
        result.Sort();
        return result;
    }

    // The vertices of the feature edges of a mesh, or null when the mesh has more than MostEdges of them (ADR-0109 item 5).
    public static float[]? Lines(ReadOnlySpan<float> narrowed, ReadOnlySpan<double> positions, ReadOnlySpan<uint> indices)
    {
        var feature = Feature(positions, indices);
        return feature.Count <= MostEdges ? Vertices(narrowed, feature) : null;
    }

    // The vertices of the edges, from the narrowed positions of the mesh (MeshVertex.Narrow), so the edges and the mesh
    // have one origin and one model-view matrix.
    public static float[] Vertices(ReadOnlySpan<float> narrowed, IReadOnlyList<(int A, int B)> edges)
    {
        var result = new float[edges.Count * VerticesPerEdge * FloatsPerVertex];
        var at = 0;
        foreach (var (a, b) in edges)
        {
            foreach (var (along, side) in Corners)
            {
                result[at++] = narrowed[3 * a];
                result[at++] = narrowed[(3 * a) + 1];
                result[at++] = narrowed[(3 * a) + 2];
                result[at++] = narrowed[3 * b];
                result[at++] = narrowed[(3 * b) + 1];
                result[at++] = narrowed[(3 * b) + 2];
                result[at++] = along;
                result[at++] = side;
            }
        }

        return result;
    }

    private static (int, int) Key(int p, int q) => p < q ? (p, q) : (q, p);

    // V175 (TASK-0356): a cross product whose length is at most this part of the square of the longest side is zero, so the
    // three points are on one line. The rounding of a turned body gives about 1e-16 of it; a real face of a thin sliver
    // of 1e-9 of its length is a choice of the window, not of the model.
    internal const double Collinear = 1e-9;

    // The edges of one triangle each from P to Q along the segment P-Q, in their order, or null. Each step takes the
    // neighbour on the segment that is nearest after the last one, so a chain of two or more T-vertices is one chain.
    private static List<(int, int)>? Chain(ReadOnlySpan<double> positions, Dictionary<int, List<int>> single,
        Dictionary<(int, int), (int Count, (double X, double Y, double Z) First, (double X, double Y, double Z) Second)> edges, int p, int q)
    {
        var (dx, dy, dz) = Difference(positions, p, q);
        var length = (dx * dx) + (dy * dy) + (dz * dz);
        var parts = new List<(int, int)>();
        var (at, along) = (p, 0d);
        while (at != q && parts.Count < single.Count)
        {
            var next = -1;
            var nearest = double.MaxValue;
            foreach (var n in single.GetValueOrDefault(at) ?? [])
            {
                if (Key(at, n) == Key(p, q) || !edges.TryGetValue(Key(at, n), out var edge) || edge.Count != 1)
                    continue;
                var (ex, ey, ez) = Difference(positions, p, n);
                var t = ((ex * dx) + (ey * dy) + (ez * dz)) / length;
                var (cx, cy, cz) = ((ey * dz) - (ez * dy), (ez * dx) - (ex * dz), (ex * dy) - (ey * dx));
                var off = Math.Sqrt((cx * cx) + (cy * cy) + (cz * cz));
                if (t > along && (n == q || t < 1) && off <= Collinear * length && t < nearest)
                    (next, nearest) = (n, t);
            }

            if (next < 0)
                return null;
            parts.Add(Key(at, next));
            (at, along) = (next, nearest);
        }

        return at == q ? parts : null;
    }

    private static (double X, double Y, double Z) Difference(ReadOnlySpan<double> positions, int a, int b)
        => (positions[3 * b] - positions[3 * a], positions[(3 * b) + 1] - positions[(3 * a) + 1], positions[(3 * b) + 2] - positions[(3 * a) + 2]);

    // The unit normal of a triangle, or null for a triangle with no area: a cross product whose length is at most Collinear
    // times the square of the longest side (V175).
    private static (double X, double Y, double Z)? Normal(ReadOnlySpan<double> positions, int a, int b, int c)
    {
        var (ux, uy, uz) = Difference(positions, a, b);
        var (vx, vy, vz) = Difference(positions, a, c);
        var (wx, wy, wz) = Difference(positions, b, c);
        var (nx, ny, nz) = ((uy * vz) - (uz * vy), (uz * vx) - (ux * vz), (ux * vy) - (uy * vx));
        var length = Math.Sqrt((nx * nx) + (ny * ny) + (nz * nz));
        var longest = Math.Max((ux * ux) + (uy * uy) + (uz * uz), Math.Max((vx * vx) + (vy * vy) + (vz * vz), (wx * wx) + (wy * wy) + (wz * wz)));
        return length > Collinear * longest ? (nx / length, ny / length, nz / length) : null;
    }

    private static double Dot((double X, double Y, double Z) p, (double X, double Y, double Z) q) => (p.X * q.X) + (p.Y * q.Y) + (p.Z * q.Z);
}
