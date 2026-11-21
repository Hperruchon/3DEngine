using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Contracts.Sketch;
using Engine.Kernel;

namespace Engine.Core.Queries;

// ADR-0182 (phase K11, TASK-0436): the pair check at one position. The query reads the solid of each body in the owned
// kernel, as the measure queries do, and computes each overlap with the exact intersection of the kernel in memory: it writes
// nothing to the log and makes no body. Each step that decides a class uses + - * / and comparisons on exact rationals; the
// gap leaves as the root of an exact square, and each other value as the nearest double to an exact value.
public sealed class CheckInterferenceQueryHandler : IQueryHandler
{
    public string QueryName => "CheckInterference";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyIds"] = new("string"),
            ["intendedPairs"] = new("string"),
        };

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["pairs"] = new("array"),
        };

    // Per ADR-0016.
    public Query Create(QueryInput input) => new CheckInterferenceQuery
    {
        QueryId = input.QueryId,
        BodyIds = input.Parameters.GetValueOrDefault("bodyIds") as string,
        IntendedPairs = input.Parameters.GetValueOrDefault("intendedPairs") as string,
    };

    public Task<QueryHandlerResult> Handle(
        Query query,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);
        var check = (CheckInterferenceQuery)query;
        var (ids, listError) = BodyList(document, check.BodyIds);
        if (ids is null)
            return Failure(listError!);
        var (intended, pairError) = Intended(check.IntendedPairs, ids);
        if (intended is null)
            return Failure(pairError!);

        var bodies = new List<Body>(ids.Count);
        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            var (body, error) = Resolve(document, backend, id);
            if (body is null)
                return Failure(error!);
            bodies.Add(body);
        }

        // Item 8 (TASK-0438, finding E233): the work of the check before its first step that splits or measures. Each pair
        // of bodies costs the product of the counts of their pieces, and each pair of outer shells of a mesh body whose boxes
        // share a volume the product of the counts of their polygons. Above the limit the check is refused, so it cannot
        // hold the session for minutes.
        var work = bodies.Sum(body => body.ShellWork);
        for (var i = 0; i < bodies.Count; i++)
        {
            for (var j = i + 1; j < bodies.Count; j++)
                work += (long)bodies[i].Pieces.Count * bodies[j].Pieces.Count;
        }

        if (work > WorkLimit)
        {
            return Failure(new ErrorDetail(DiagnosticCodes.CheckWorkLimit,
                $"The check needs {work} products of the pieces of its bodies, above the limit of {WorkLimit}. Give fewer bodies with bodyIds, or bodies with fewer facets."));
        }

        foreach (var body in bodies)
        {
            if (Crossing(body, ct) is { } crossing)
                return Failure(crossing);
        }

        // Items 2 and 8: each pair once, in the order of the bodies.
        var pairs = new List<PairCheck>(ids.Count * (ids.Count - 1) / 2);
        for (var i = 0; i < bodies.Count; i++)
        {
            for (var j = i + 1; j < bodies.Count; j++)
            {
                ct.ThrowIfCancellationRequested();
                pairs.Add(Check(bodies[i], bodies[j], intended.Contains(Key(bodies[i].Id, bodies[j].Id)), ct));
            }
        }

        return Task.FromResult(QueryHandlerResult.Success(new InterferenceInfo(pairs)));
    }

    // Item 8 (TASK-0438, finding E233): the greatest work of one check, as a sum of products of counts of pieces. On the
    // computer of the owner an intersection of a sphere of 3,968 triangles and a plate (23,808 products) took 3.2 s, and the
    // gap of two spheres apart (15,745,024 products) took 101 s. At this limit an intersection takes about 13 s and a gap
    // about 0.6 s. The 171 pairs of the bookcase of K10a need 6,156 products.
    internal const long WorkLimit = 100_000;

    // One body of the check: its solid in the owned kernel, its exact box, its pieces for the gap, and the mark of item 4. A
    // mesh body of more than one shell also gives the solid and the box of each outer shell, and the work of their test.
    private sealed record Body(Guid Id, KernelSolid Solid, Vector3R Low, Vector3R High, IReadOnlyList<NearestPoints.Piece> Pieces, bool MeshOnly)
    {
        public IReadOnlyList<Shell> Shells { get; init; } = [];

        public long ShellWork { get; init; }
    }

    // One outer shell of a mesh body, as a solid alone, with its exact box.
    private sealed record Shell(KernelSolid Solid, Vector3R Low, Vector3R High);

    // Items 3 and 8: the class of one pair. Boxes that share no volume cannot hold an overlap, so the intersection of the
    // kernel runs only for boxes that share a volume. With no shared volume, the nearest pair of the pieces gives the gap: 0
    // is a contact on a face, an edge or a point, and above 0 the bodies are apart. A body cannot lie inside the other with no
    // shared volume, so the gap needs no test of inside.
    private static PairCheck Check(Body a, Body b, bool intended, CancellationToken ct)
    {
        var meshOnly = a.MeshOnly || b.MeshOnly;
        if (ShareVolume(a.Low, a.High, b.Low, b.High) && PlanarBoolean.Intersect(a.Solid, b.Solid, ct) is { } overlap)
        {
            var (low, high) = BoxOf(overlap);
            var (minX, minY, minZ) = low.ToDoubles();
            var (maxX, maxY, maxZ) = high.ToDoubles();
            return new PairCheck(a.Id, b.Id, PairClass.Overlapping, overlap.Volume().ToDouble(), new Aabb(minX, minY, minZ, maxX, maxY, maxZ),
                0, null, null, intended, !intended, meshOnly, CheckBasis.Guaranteed);
        }

        var pair = NearestPoints.Nearest(a.Pieces, b.Pieces, ct)!.Value;
        var (ax, ay, az) = pair.A.ToDoubles();
        var (bx, by, bz) = pair.B.ToDoubles();
        return new PairCheck(a.Id, b.Id, pair.Squared.IsZero ? PairClass.Touching : PairClass.Apart, 0, null,
            Math.Sqrt(pair.Squared.ToDouble()), new DatumPoint(ax, ay, az), new DatumPoint(bx, by, bz), intended, false, meshOnly, CheckBasis.Guaranteed);
    }

    // True when two boxes share a length above zero on each axis.
    private static bool ShareVolume(Vector3R lowA, Vector3R highA, Vector3R lowB, Vector3R highB)
    {
        static bool Axis(Rational lowA, Rational highA, Rational lowB, Rational highB)
            => (highA < highB ? highA : highB) > (lowA > lowB ? lowA : lowB);

        return Axis(lowA.X, highA.X, lowB.X, highB.X)
            && Axis(lowA.Y, highA.Y, lowB.Y, highB.Y)
            && Axis(lowA.Z, highA.Z, lowB.Z, highB.Z);
    }

    // Item 7 (TASK-0438, finding E234): the outer shells of a mesh body share no volume, or the body is refused. The tree of
    // the kernel takes a solid that does not cross itself: with two shells that overlap, a point inside the mesh can fall in
    // a leaf outside it, and a box inside the mesh was "apart". Each pair of outer shells whose boxes share a volume runs the
    // exact intersection of the kernel. A void shell, and a shell that crosses itself, are not tested (ADR-0182 "Bad").
    private static ErrorDetail? Crossing(Body body, CancellationToken ct)
    {
        for (var i = 0; i < body.Shells.Count; i++)
        {
            for (var j = i + 1; j < body.Shells.Count; j++)
            {
                var (a, b) = (body.Shells[i], body.Shells[j]);
                if (ShareVolume(a.Low, a.High, b.Low, b.High) && PlanarBoolean.Intersect(a.Solid, b.Solid, ct) is not null)
                {
                    return new ErrorDetail(DiagnosticCodes.MeshUnsupported,
                        $"Two shells of the triangles of the mesh body '{body.Id}' share a volume, so the pair check cannot give its overlap.");
                }
            }
        }

        return null;
    }

    // The work of the test of the shells: the product of the counts of the polygons of each pair whose boxes share a volume.
    private static long ShellWork(IReadOnlyList<Shell> shells)
    {
        var work = 0L;
        for (var i = 0; i < shells.Count; i++)
        {
            for (var j = i + 1; j < shells.Count; j++)
            {
                var (a, b) = (shells[i], shells[j]);
                if (ShareVolume(a.Low, a.High, b.Low, b.High))
                    work += (long)a.Solid.Polygons.Count * b.Solid.Polygons.Count;
            }
        }

        return work;
    }

    // The exact box of a solid: the least and the greatest coordinate of its vertices on each axis.
    private static (Vector3R Low, Vector3R High) BoxOf(KernelSolid solid)
    {
        var first = solid.Polygons[0].Vertices[0];
        var (lx, ly, lz, hx, hy, hz) = (first.X, first.Y, first.Z, first.X, first.Y, first.Z);
        foreach (var polygon in solid.Polygons)
        {
            foreach (var v in polygon.Vertices)
            {
                lx = v.X < lx ? v.X : lx;
                ly = v.Y < ly ? v.Y : ly;
                lz = v.Z < lz ? v.Z : lz;
                hx = v.X > hx ? v.X : hx;
                hy = v.Y > hy ? v.Y : hy;
                hz = v.Z > hz ? v.Z : hz;
            }
        }

        return (new Vector3R(lx, ly, lz), new Vector3R(hx, hy, hz));
    }

    // Items 4 and 7: the solid of one body. A mesh body of an STL file gives the solid of its triangles, from the doubles of
    // the backend, when they close a solid; else it is refused. Each other body gives the solid of the measure queries, with
    // their refusals.
    private static (Body? Body, ErrorDetail? Error) Resolve(Document document, IGeometryBackend backend, Guid id)
    {
        var record = document.Bodies.First(body => body.Handle.Id == id);
        KernelSolid? solid;
        IReadOnlyList<Shell> shells = [];
        bool meshOnly;
        if (record.Kind == Commands.ImportStlCommandHandler.MeshKind)
        {
            (solid, shells, var error) = MeshSolid(backend, record);
            if (solid is null)
                return (null, error);
            meshOnly = true;
        }
        else
        {
            (solid, var error) = MeasureTarget.SolidOf(document, id);
            if (solid is null)
                return (null, error);
            meshOnly = MeasureTarget.HasCurve(solid);
        }

        var (low, high) = BoxOf(solid);
        return (new Body(id, solid, low, high, [.. solid.Polygons.Select(polygon => new NearestPoints.Piece(polygon.Vertices))], meshOnly)
        {
            Shells = shells,
            ShellWork = ShellWork(shells),
        }, null);
    }

    // Item 4: the triangles of a mesh body as an exact solid of the kernel. Each double of the backend enters exactly. The
    // solid is valid when each edge has one coedge in each direction (Brep.Valid) and its volume is above zero. Item 7: a
    // valid mesh of more than one shell also gives its outer shells, which the check tests before its pairs.
    private static (KernelSolid? Solid, IReadOnlyList<Shell> Shells, ErrorDetail? Error) MeshSolid(IGeometryBackend backend, BodyRecord record)
    {
        if (backend.TryGet<ITessellationOps>() is not { } ops)
            return (null, [], new ErrorDetail(DiagnosticCodes.GeomCapMissing, "Active backend does not implement ITessellationOps; cannot give the triangles of a mesh body."));
        Tessellation mesh;
        try
        {
            mesh = ops.Tessellate(new BodyHandle(record.GeometryKey));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (null, [], new ErrorDetail(DiagnosticCodes.GeomNativeOp, $"The backend could not give the triangles of the body '{record.Handle.Id}': {ex.GetType().Name}: {ex.Message}"));
        }

        var vertices = new Vector3R[mesh.VertexCount];
        var points = new int[mesh.VertexCount];
        var known = new Dictionary<(double, double, double), int>();
        for (var i = 0; i < vertices.Length; i++)
        {
            var (x, y, z) = (mesh.Positions[3 * i], mesh.Positions[(3 * i) + 1], mesh.Positions[(3 * i) + 2]);
            vertices[i] = Vector3R.FromDoubles(x, y, z);

            // Item 7: one number for each point, so two vertices of the backend at one point are one point of a shell. The
            // sum with +0 makes -0 equal to +0.
            if (!known.TryGetValue((x + 0.0, y + 0.0, z + 0.0), out points[i]))
                known[(x + 0.0, y + 0.0, z + 0.0)] = points[i] = known.Count;
        }

        var open = new ErrorDetail(DiagnosticCodes.MeshUnsupported,
            $"The triangles of the mesh body '{record.Handle.Id}' do not close a solid, so the pair check cannot give its overlap.");
        try
        {
            int[] indices = [.. mesh.Indices.Select(index => (int)index)];
            var solid = KernelSolid.Mesh(record.Handle.Id, "mesh", vertices, indices);
            return solid.Volume().Sign > 0 && Brep.Of(solid).Valid ? (solid, OuterShells(record.Handle.Id, vertices, indices, points), null) : (null, [], open);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return (null, [], open);
        }
    }

    // Item 7 (TASK-0438, finding E234): the outer shells of a closed mesh of more than one shell, each as a solid alone. Two
    // triangles with a common edge of two points are in one shell; the backend can give one point as several vertices.
    // A shell whose signed volume has the sign of the whole mesh is an outer shell; a shell of the other sign is a void. A mesh
    // of one shell gives no shell, because it has no pair to test.
    private static IReadOnlyList<Shell> OuterShells(Guid body, Vector3R[] vertices, int[] indices, int[] points)
    {
        var count = indices.Length / 3;
        var parent = Enumerable.Range(0, count).ToArray();
        int Root(int t)
        {
            while (parent[t] != t)
                t = parent[t] = parent[parent[t]];
            return t;
        }

        var edges = new Dictionary<(int, int), int>();
        for (var t = 0; t < count; t++)
        {
            for (var k = 0; k < 3; k++)
            {
                var (p, q) = (points[indices[(3 * t) + k]], points[indices[(3 * t) + ((k + 1) % 3)]]);
                if (edges.TryGetValue(p < q ? (p, q) : (q, p), out var other))
                    parent[Root(t)] = Root(other);
                else
                    edges[p < q ? (p, q) : (q, p)] = t;
            }
        }

        var groups = new List<List<int>>();
        var group = new Dictionary<int, int>();
        for (var t = 0; t < count; t++)
        {
            var root = Root(t);
            if (!group.TryGetValue(root, out var index))
            {
                group[root] = index = groups.Count;
                groups.Add([]);
            }

            groups[index].AddRange([indices[3 * t], indices[(3 * t) + 1], indices[(3 * t) + 2]]);
        }

        if (groups.Count < 2)
            return [];
        var signs = groups.Select(shell => Signed(vertices, shell).Sign).ToList();
        var whole = groups.Aggregate(Rational.Zero, (sum, shell) => sum + Signed(vertices, shell)).Sign;
        var shells = new List<Shell>();
        for (var g = 0; g < groups.Count; g++)
        {
            if (signs[g] != whole)
                continue;
            var solid = KernelSolid.Mesh(body, "mesh", vertices, groups[g]);
            var (low, high) = BoxOf(solid);
            shells.Add(new Shell(solid, low, high));
        }

        return shells;
    }

    // Six times the signed volume of the triangles of a shell, the sum of a · (b × c) over each triangle.
    private static Rational Signed(Vector3R[] vertices, List<int> shell)
    {
        var sum = Rational.Zero;
        for (var t = 0; t < shell.Count; t += 3)
            sum += vertices[shell[t]].Dot(vertices[shell[t + 1]].Cross(vertices[shell[t + 2]]));
        return sum;
    }

    // Item 2: the bodies of the check. With no text, each live body in the order of the Document; else two identifiers or
    // more, separated by commas, each of a live body and each once.
    private static (List<Guid>? Ids, ErrorDetail? Error) BodyList(Document document, string? text)
    {
        if (text is null)
            return ([.. document.Bodies.Select(body => body.Handle.Id)], null);

        var ids = new List<Guid>();
        foreach (var item in text.Split(','))
        {
            if (!Guid.TryParse(item.Trim(), out var id))
                return (null, Invalid($"The item '{item.Trim()}' of bodyIds is not an identifier of a body."));
            if (ids.Contains(id))
                return (null, Invalid($"The body '{id}' is twice in bodyIds."));
            ids.Add(id);
        }

        if (ids.Count < 2)
            return (null, Invalid("bodyIds gives one body only; a check needs two bodies or more, or no bodyIds for each live body."));
        foreach (var id in ids)
        {
            if (document.Bodies.All(body => body.Handle.Id != id))
                return (null, new ErrorDetail(DiagnosticCodes.GeomBodyNotFound, $"No live body with id '{id}' exists in the Document."));
        }

        return (ids, null);
    }

    // Items 2 and 5: the intended pairs, each as "a:b", separated by commas. The two bodies of a pair are two bodies of the check,
    // and each pair is given once, in either order.
    private static (HashSet<(Guid, Guid)>? Pairs, ErrorDetail? Error) Intended(string? text, List<Guid> ids)
    {
        var pairs = new HashSet<(Guid, Guid)>();
        if (text is null)
            return (pairs, null);

        foreach (var item in text.Split(','))
        {
            var parts = item.Split(':');
            if (parts.Length != 2 || !Guid.TryParse(parts[0].Trim(), out var a) || !Guid.TryParse(parts[1].Trim(), out var b))
                return (null, Invalid($"The item '{item.Trim()}' of intendedPairs is not two identifiers of bodies separated by a colon."));
            if (a == b)
                return (null, Invalid($"The intended pair '{item.Trim()}' names one body twice."));
            if (!ids.Contains(a) || !ids.Contains(b))
                return (null, Invalid($"The intended pair '{item.Trim()}' names a body that the check does not hold."));
            if (!pairs.Add(Key(a, b)))
                return (null, Invalid($"The intended pair '{item.Trim()}' is given twice."));
        }

        return (pairs, null);
    }

    // The key of a pair in either order.
    private static (Guid, Guid) Key(Guid a, Guid b) => a.CompareTo(b) < 0 ? (a, b) : (b, a);

    private static ErrorDetail Invalid(string message) => new(DiagnosticCodes.CheckPairInvalid, message);

    private static Task<QueryHandlerResult> Failure(ErrorDetail error) => Task.FromResult(QueryHandlerResult.Failure(error));
}
