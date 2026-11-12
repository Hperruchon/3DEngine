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
        var slivers = new List<(int A, int B, int C)>();
        for (var t = 0; t + 2 < indices.Length; t += 3)
        {
            var (a, b, c) = (canonical[indices[t]], canonical[indices[t + 1]], canonical[indices[t + 2]]);
            if (a == b || b == c || c == a)
                continue;
            var normal = Normal(positions, a, b, c);

            // Finding V166 of the codebase review of 2026-11-11 (TASK-0345): a triangle of zero area across a T-vertex has
            // three different corners, but its long edge is not an edge of the surface. It adds no edge.
            if (normal == default)
            {
                slivers.Add((a, b, c));
                continue;
            }

            foreach (var (p, q) in new[] { (a, b), (b, c), (c, a) })
            {
                var key = Key(p, q);
                edges[key] = edges.TryGetValue(key, out var seen)
                    ? (seen.Count + 1, seen.First, seen.Count == 1 ? normal : seen.Second)
                    : (1, normal, default);
            }
        }

        // Finding V171 of the codebase review of 2026-11-12 (TASK-0350): a sliver closes a T-vertex M on the long side P-Q, so the
        // triangle across P-Q is also the triangle across P-M and across M-Q. When each of the three sides has one triangle,
        // the two short sides take the triangle of the long side, and the long side is not an edge. So a T-vertex inside a
        // flat face draws no line, and a crease along P-Q still draws its two parts.
        foreach (var (a, b, c) in slivers)
        {
            var (p, m, q) = Ends(positions, a, b, c);
            var (pm, mq, pq) = (Key(p, m), Key(m, q), Key(p, q));
            if (edges.TryGetValue(pq, out var across) && across.Count == 1
                && edges.TryGetValue(pm, out var near) && near.Count == 1 && edges.TryGetValue(mq, out var far) && far.Count == 1)
            {
                edges[pm] = (2, near.First, across.First);
                edges[mq] = (2, far.First, across.First);
                edges.Remove(pq);
            }
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

    // The ends P and Q of the longest side of a triangle of zero area, and its middle corner M, which is on that side.
    private static (int P, int M, int Q) Ends(ReadOnlySpan<double> positions, int a, int b, int c)
    {
        var (ab, bc, ca) = (Square(positions, a, b), Square(positions, b, c), Square(positions, c, a));
        return ab >= bc && ab >= ca ? (a, c, b) : bc >= ca ? (b, a, c) : (c, b, a);
    }

    // The square of the distance between two vertices, with no square root.
    private static double Square(ReadOnlySpan<double> positions, int a, int b)
    {
        var (x, y, z) = (positions[3 * b] - positions[3 * a], positions[(3 * b) + 1] - positions[(3 * a) + 1], positions[(3 * b) + 2] - positions[(3 * a) + 2]);
        return (x * x) + (y * y) + (z * z);
    }

    // The unit normal of a triangle, or zero for a triangle with no area.
    private static (double X, double Y, double Z) Normal(ReadOnlySpan<double> positions, int a, int b, int c)
    {
        var (ux, uy, uz) = (positions[3 * b] - positions[3 * a], positions[(3 * b) + 1] - positions[(3 * a) + 1], positions[(3 * b) + 2] - positions[(3 * a) + 2]);
        var (vx, vy, vz) = (positions[3 * c] - positions[3 * a], positions[(3 * c) + 1] - positions[(3 * a) + 1], positions[(3 * c) + 2] - positions[(3 * a) + 2]);
        var (nx, ny, nz) = ((uy * vz) - (uz * vy), (uz * vx) - (ux * vz), (ux * vy) - (uy * vx));
        var length = Math.Sqrt((nx * nx) + (ny * ny) + (nz * nz));
        return length > 0 ? (nx / length, ny / length, nz / length) : (0, 0, 0);
    }

    private static double Dot((double X, double Y, double Z) p, (double X, double Y, double Z) q) => (p.X * q.X) + (p.Y * q.Y) + (p.Z * q.Z);
}
