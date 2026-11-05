namespace Engine.Kernel;

// One loop of a face: its vertices in order, counter-clockwise seen from the front of the face for the outer loop and
// clockwise for a hole.
public sealed record BrepLoop(IReadOnlyList<int> Vertices, bool Outer);

// One face: its plane, its tag (the provenance of ADR-0057 item 4), and its loops, the outer loop first.
public sealed record BrepFace(PlaneR Plane, FaceTag Tag, IReadOnlyList<BrepLoop> Loops);

// One edge between two vertices, the smaller index first, with the faces of its two coedges.
public sealed record BrepEdge(int From, int To, IReadOnlyList<int> Faces);

// ADR-0057 item 6: the topology of a solid of the kernel: the vertices, the edges and the faces with their loops, and
// the shells as sets of faces. Valid is true when each edge has one coedge in each direction, so the shells are closed.
public sealed class Brep
{
    public IReadOnlyList<Vector3R> Vertices { get; }

    public IReadOnlyList<BrepEdge> Edges { get; }

    public IReadOnlyList<BrepFace> Faces { get; }

    public IReadOnlyList<IReadOnlyList<int>> Shells { get; }

    public bool Valid { get; }

    // The count of the loops that are holes.
    public int Holes => Faces.Sum(face => face.Loops.Count(loop => !loop.Outer));

    private Brep(IReadOnlyList<Vector3R> vertices, IReadOnlyList<BrepEdge> edges, IReadOnlyList<BrepFace> faces, IReadOnlyList<IReadOnlyList<int>> shells, bool valid)
        => (Vertices, Edges, Faces, Shells, Valid) = (vertices, edges, faces, shells, valid);

    public static Brep Of(KernelSolid solid)
    {
        ArgumentNullException.ThrowIfNull(solid);

        // 1. Each vertex once, in the order of its first use. A dictionary finds a vertex; each order comes from a list.
        var vertices = new List<Vector3R>();
        var index = new Dictionary<Vector3R, int>();
        int Id(Vector3R p)
        {
            if (!index.TryGetValue(p, out var id))
            {
                id = vertices.Count;
                vertices.Add(p);
                index[p] = id;
            }
            return id;
        }

        var rings = solid.Polygons.Select(polygon => polygon.Vertices.Select(Id).ToList()).ToList();

        // ADR-0058 item 5: the rounded value of each vertex, for a filter before each exact test. Rounding to the nearest
        // keeps the order, so a vertex whose rounded value is outside the rounded box of an edge is outside its exact box.
        var rounded = vertices.Select(p => p.ToDoubles()).ToArray();

        // 2. Each edge of each polygon gets each vertex that lies on it exactly, so the pieces of the partition meet at
        // their vertices and no T-junction stays.
        for (var r = 0; r < rings.Count; r++)
        {
            var ring = rings[r];
            var split = new List<int>();
            for (var i = 0; i < ring.Count; i++)
            {
                var (a, b) = (ring[i], ring[(i + 1) % ring.Count]);
                split.Add(a);
                split.AddRange(Between(vertices, rounded, a, b));
            }
            rings[r] = split;
        }

        // 3. The pieces of one plane and one tag are one face; a pair of opposite edges inside it cancels.
        var groups = new List<(PlaneR Plane, FaceTag Tag, List<int> Rings)>();
        for (var r = 0; r < rings.Count; r++)
        {
            var polygon = solid.Polygons[r];
            var group = groups.FindIndex(g => g.Plane == polygon.Plane && g.Tag == polygon.Tag);
            if (group < 0)
                groups.Add((polygon.Plane, polygon.Tag, [r]));
            else
                groups[group].Rings.Add(r);
        }

        var faces = new List<(PlaneR Plane, FaceTag Tag, List<List<int>> Loops)>();
        foreach (var (plane, tag, members) in groups)
        {
            var directed = new List<(int From, int To)>();
            foreach (var r in members)
            {
                var ring = rings[r];
                for (var i = 0; i < ring.Count; i++)
                {
                    var edge = (ring[i], ring[(i + 1) % ring.Count]);
                    var opposite = directed.IndexOf((edge.Item2, edge.Item1));
                    if (opposite >= 0)
                        directed.RemoveAt(opposite);
                    else
                        directed.Add(edge);
                }
            }

            faces.Add((plane, tag, Loops(directed)));
        }

        // 4. A vertex where each loop that holds it goes straight on is not a corner: it goes, so an edge that the
        // partition split is one edge again. ADR-0058 item 5: each pass removes each straight vertex that it finds. A
        // removal does not change whether a neighbour goes straight on, because the neighbour keeps its direction.
        var removed = true;
        while (removed)
        {
            removed = false;
            var straightVertices = new List<int>();
            var uses = new Dictionary<int, List<(int Face, int Loop, int At)>>();
            for (var f = 0; f < faces.Count; f++)
            {
                for (var l = 0; l < faces[f].Loops.Count; l++)
                {
                    var loop = faces[f].Loops[l];
                    for (var i = 0; i < loop.Count; i++)
                    {
                        if (!uses.TryGetValue(loop[i], out var list))
                            uses[loop[i]] = list = [];
                        list.Add((f, l, i));
                    }
                }
            }

            foreach (var vertex in Enumerable.Range(0, vertices.Count))
            {
                if (!uses.TryGetValue(vertex, out var list) || list.Count == 0)
                    continue;
                var straight = list.All(use =>
                {
                    var loop = faces[use.Face].Loops[use.Loop];
                    var (prev, next) = (loop[(use.At + loop.Count - 1) % loop.Count], loop[(use.At + 1) % loop.Count]);
                    return loop.Count > 3 && (vertices[vertex] - vertices[prev]).Cross(vertices[next] - vertices[vertex]).IsZero;
                });
                if (straight)
                    straightVertices.Add(vertex);
            }

            if (straightVertices.Count == 0)
                continue;
            var gone = straightVertices.ToHashSet();
            foreach (var face in faces)
            {
                foreach (var loop in face.Loops)
                    loop.RemoveAll(gone.Contains);
            }
            removed = true;
        }

        // 5. The edges, with the faces of their coedges, and the check that each edge has one coedge in each direction.
        var edges = new List<(int From, int To, List<int> Faces, int Forward, int Backward)>();
        for (var f = 0; f < faces.Count; f++)
        {
            foreach (var loop in faces[f].Loops)
            {
                for (var i = 0; i < loop.Count; i++)
                {
                    var (a, b) = (loop[i], loop[(i + 1) % loop.Count]);
                    var (low, high) = a < b ? (a, b) : (b, a);
                    var e = edges.FindIndex(edge => edge.From == low && edge.To == high);
                    if (e < 0)
                    {
                        edges.Add((low, high, [], 0, 0));
                        e = edges.Count - 1;
                    }
                    var edge = edges[e];
                    edge.Faces.Add(f);
                    edges[e] = a < b ? edge with { Forward = edge.Forward + 1 } : edge with { Backward = edge.Backward + 1 };
                }
            }
        }

        var valid = edges.All(edge => edge.Forward == 1 && edge.Backward == 1);

        // The vertices that no loop holds after step 4 stay out of the index of the result.
        var used = faces.SelectMany(face => face.Loops.SelectMany(loop => loop)).Distinct().Order().ToList();
        var renumber = used.Select((old, i) => (old, i)).ToDictionary(pair => pair.old, pair => pair.i);

        // 6. A cut can split one face into regions that do not touch, with one plane and one tag. Each outer loop is a
        // face, with the holes that it contains, so each face of the result has one outer loop.
        var regions = new List<(int Group, BrepFace Face)>();
        for (var f = 0; f < faces.Count; f++)
        {
            var face = faces[f];
            var loops = face.Loops.Select(loop => (Loop: loop, Outer: Area(vertices, loop, face.Plane).Sign > 0)).ToList();
            var outers = loops.Where(loop => loop.Outer).Select(loop => loop.Loop).ToList();
            foreach (var outer in outers)
            {
                var holes = loops.Where(loop => !loop.Outer && Owner(vertices, outers, loop.Loop, face.Plane) == outer).Select(loop => loop.Loop);
                regions.Add((f, new BrepFace(face.Plane, face.Tag, [.. new[] { outer }.Concat(holes)
                    .Select((loop, i) => new BrepLoop([.. loop.Select(v => renumber[v])], i == 0))])));
            }
        }

        // The faces of an edge and of a shell are the faces after the split: the face whose loops hold the edge.
        int FaceOf(int group, int from, int to)
        {
            for (var i = 0; i < regions.Count; i++)
            {
                if (regions[i].Group != group)
                    continue;
                foreach (var loop in regions[i].Face.Loops)
                {
                    for (var j = 0; j < loop.Vertices.Count; j++)
                    {
                        var (a, b) = (loop.Vertices[j], loop.Vertices[(j + 1) % loop.Vertices.Count]);
                        if ((a == from && b == to) || (a == to && b == from))
                            return i;
                    }
                }
            }
            throw new InvalidOperationException("An edge has no face.");
        }

        var finalEdges = edges.Select(edge => new BrepEdge(renumber[edge.From], renumber[edge.To],
            [.. edge.Faces.Distinct().SelectMany(group => new[] { FaceOf(group, renumber[edge.From], renumber[edge.To]) }).Distinct()])).ToList();

        // 7. The shells: the faces after the split that share edges, in the order of their first face. A cut can split a
        // body into two solids, and one face of the input can then lie on both, so the shells come after step 6.
        var shellOf = new int[regions.Count];
        Array.Fill(shellOf, -1);
        var finalShells = new List<IReadOnlyList<int>>();
        for (var f = 0; f < regions.Count; f++)
        {
            if (shellOf[f] >= 0)
                continue;
            var shell = new List<int>();
            var stack = new Stack<int>([f]);
            shellOf[f] = finalShells.Count;
            while (stack.Count > 0)
            {
                var face = stack.Pop();
                shell.Add(face);
                foreach (var edge in finalEdges.Where(edge => edge.Faces.Contains(face)))
                {
                    foreach (var other in edge.Faces.Where(other => shellOf[other] < 0))
                    {
                        shellOf[other] = finalShells.Count;
                        stack.Push(other);
                    }
                }
            }
            shell.Sort();
            finalShells.Add(shell);
        }

        return new Brep([.. used.Select(i => vertices[i])], finalEdges, [.. regions.Select(item => item.Face)], finalShells, valid);
    }

    // The outer loop of the list that contains a vertex of the hole, by an exact count of the crossings of a ray in the
    // plane, or the first outer loop when the list has one.
    private static List<int>? Owner(List<Vector3R> vertices, List<List<int>> outers, List<int> hole, PlaneR plane)
    {
        if (outers.Count == 1)
            return outers[0];
        var n = plane.Normal;
        // The axis of the largest component of the normal drops out, so the projection keeps the order of the plane.
        var (ax, ay, az) = (Abs(n.X), Abs(n.Y), Abs(n.Z));
        var drop = ax >= ay && ax >= az ? 0 : ay >= az ? 1 : 2;
        (Rational U, Rational V) P(Vector3R p) => drop switch { 0 => (p.Y, p.Z), 1 => (p.Z, p.X), _ => (p.X, p.Y) };
        var point = P(vertices[hole[0]]);
        foreach (var outer in outers)
        {
            var inside = false;
            for (var i = 0; i < outer.Count; i++)
            {
                var (a, b) = (P(vertices[outer[i]]), P(vertices[outer[(i + 1) % outer.Count]]));
                if ((a.V > point.V) == (b.V > point.V))
                    continue;
                var x = a.U + ((b.U - a.U) * (point.V - a.V) / (b.V - a.V));
                if (x > point.U)
                    inside = !inside;
            }
            if (inside)
                return outer;
        }

        return null;
    }

    private static Rational Abs(Rational value) => value.Sign < 0 ? -value : value;

    // The vertices strictly between a and b on the segment ab, in their order from a. Only a vertex whose rounded value
    // is inside the rounded box of ab gets the exact test.
    private static IEnumerable<int> Between(List<Vector3R> vertices, (double X, double Y, double Z)[] rounded, int a, int b)
    {
        var (pa, pb) = (vertices[a], vertices[b]);
        var (ra, rb) = (rounded[a], rounded[b]);
        var (lx, hx) = (Math.Min(ra.X, rb.X), Math.Max(ra.X, rb.X));
        var (ly, hy) = (Math.Min(ra.Y, rb.Y), Math.Max(ra.Y, rb.Y));
        var (lz, hz) = (Math.Min(ra.Z, rb.Z), Math.Max(ra.Z, rb.Z));
        bool InBox(int i)
        {
            var r = rounded[i];
            return r.X >= lx && r.X <= hx && r.Y >= ly && r.Y <= hy && r.Z >= lz && r.Z <= hz;
        }

        var d = pb - pa;
        var length = d.Dot(d);
        return Enumerable.Range(0, vertices.Count)
            .Where(i => i != a && i != b && InBox(i) && d.Cross(vertices[i] - pa).IsZero)
            .Select(i => (Index: i, T: d.Dot(vertices[i] - pa)))
            .Where(item => item.T.Sign > 0 && item.T < length)
            .OrderBy(item => item.T)
            .Select(item => item.Index);
    }

    // The closed loops of a set of directed edges: from the first edge that no loop holds, along the first edge that
    // leaves the end of the last one.
    private static List<List<int>> Loops(List<(int From, int To)> directed)
    {
        var loops = new List<List<int>>();
        var left = new List<(int From, int To)>(directed);
        while (left.Count > 0)
        {
            var start = left[0];
            left.RemoveAt(0);
            var loop = new List<int> { start.From };
            var at = start.To;
            while (at != start.From)
            {
                loop.Add(at);
                var next = left.FindIndex(edge => edge.From == at);
                if (next < 0)
                    throw new InvalidOperationException("The boundary of a face is not closed.");
                at = left[next].To;
                left.RemoveAt(next);
            }
            loops.Add(loop);
        }

        return loops;
    }

    // The area of a loop along the normal of its plane, times two: positive for a counter-clockwise loop.
    private static Rational Area(List<Vector3R> vertices, List<int> loop, PlaneR plane)
    {
        var n = new Vector3R(Rational.Zero, Rational.Zero, Rational.Zero);
        for (var i = 0; i < loop.Count; i++)
            n += vertices[loop[i]].Cross(vertices[loop[(i + 1) % loop.Count]]);
        return n.Dot(plane.Normal);
    }
}
