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
            var normal = Normal(positions, a, b, c);
            foreach (var (p, q) in new[] { (a, b), (b, c), (c, a) })
            {
                var key = p < q ? (p, q) : (q, p);
                edges[key] = edges.TryGetValue(key, out var seen)
                    ? (seen.Count + 1, seen.First, seen.Count == 1 ? normal : seen.Second)
                    : (1, normal, default);
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
