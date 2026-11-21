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
            var (body, error) = Resolve(document, backend, id);
            if (body is null)
                return Failure(error!);
            bodies.Add(body);
        }

        // Items 2 and 8: each pair once, in the order of the bodies.
        var pairs = new List<PairCheck>(ids.Count * (ids.Count - 1) / 2);
        for (var i = 0; i < bodies.Count; i++)
        {
            for (var j = i + 1; j < bodies.Count; j++)
            {
                ct.ThrowIfCancellationRequested();
                pairs.Add(Check(bodies[i], bodies[j], intended.Contains(Key(bodies[i].Id, bodies[j].Id))));
            }
        }

        return Task.FromResult(QueryHandlerResult.Success(new InterferenceInfo(pairs)));
    }

    // One body of the check: its solid in the owned kernel, its exact box, its pieces for the gap, and the mark of item 4.
    private sealed record Body(Guid Id, KernelSolid Solid, Vector3R Low, Vector3R High, IReadOnlyList<NearestPoints.Piece> Pieces, bool MeshOnly);

    // Items 3 and 8: the class of one pair. Boxes that share no volume cannot hold an overlap, so the intersection of the
    // kernel runs only for boxes that share a volume. With no shared volume, the nearest pair of the pieces gives the gap: 0
    // is a contact on a face, an edge or a point, and above 0 the bodies are apart. A body cannot lie inside the other with no
    // shared volume, so the gap needs no test of inside.
    private static PairCheck Check(Body a, Body b, bool intended)
    {
        var meshOnly = a.MeshOnly || b.MeshOnly;
        if (ShareVolume(a, b) && PlanarBoolean.Intersect(a.Solid, b.Solid) is { } overlap)
        {
            var (low, high) = BoxOf(overlap);
            var (minX, minY, minZ) = low.ToDoubles();
            var (maxX, maxY, maxZ) = high.ToDoubles();
            return new PairCheck(a.Id, b.Id, PairClass.Overlapping, overlap.Volume().ToDouble(), new Aabb(minX, minY, minZ, maxX, maxY, maxZ),
                0, null, null, intended, !intended, meshOnly, CheckBasis.Guaranteed);
        }

        var pair = NearestPoints.Nearest(a.Pieces, b.Pieces)!.Value;
        var (ax, ay, az) = pair.A.ToDoubles();
        var (bx, by, bz) = pair.B.ToDoubles();
        return new PairCheck(a.Id, b.Id, pair.Squared.IsZero ? PairClass.Touching : PairClass.Apart, 0, null,
            Math.Sqrt(pair.Squared.ToDouble()), new DatumPoint(ax, ay, az), new DatumPoint(bx, by, bz), intended, false, meshOnly, CheckBasis.Guaranteed);
    }

    // True when the boxes of two bodies share a length above zero on each axis.
    private static bool ShareVolume(Body a, Body b)
    {
        static bool Axis(Rational lowA, Rational highA, Rational lowB, Rational highB)
            => (highA < highB ? highA : highB) > (lowA > lowB ? lowA : lowB);

        return Axis(a.Low.X, a.High.X, b.Low.X, b.High.X)
            && Axis(a.Low.Y, a.High.Y, b.Low.Y, b.High.Y)
            && Axis(a.Low.Z, a.High.Z, b.Low.Z, b.High.Z);
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
        bool meshOnly;
        if (record.Kind == Commands.ImportStlCommandHandler.MeshKind)
        {
            (solid, var error) = MeshSolid(backend, record);
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
        return (new Body(id, solid, low, high, [.. solid.Polygons.Select(polygon => new NearestPoints.Piece(polygon.Vertices))], meshOnly), null);
    }

    // Item 4: the triangles of a mesh body as an exact solid of the kernel. Each double of the backend enters exactly. The
    // solid is valid when each edge has one coedge in each direction (Brep.Valid) and its volume is above zero.
    private static (KernelSolid? Solid, ErrorDetail? Error) MeshSolid(IGeometryBackend backend, BodyRecord record)
    {
        if (backend.TryGet<ITessellationOps>() is not { } ops)
            return (null, new ErrorDetail(DiagnosticCodes.GeomCapMissing, "Active backend does not implement ITessellationOps; cannot give the triangles of a mesh body."));
        Tessellation mesh;
        try
        {
            mesh = ops.Tessellate(new BodyHandle(record.GeometryKey));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (null, new ErrorDetail(DiagnosticCodes.GeomNativeOp, $"The backend could not give the triangles of the body '{record.Handle.Id}': {ex.GetType().Name}: {ex.Message}"));
        }

        var vertices = new Vector3R[mesh.VertexCount];
        for (var i = 0; i < vertices.Length; i++)
            vertices[i] = Vector3R.FromDoubles(mesh.Positions[3 * i], mesh.Positions[(3 * i) + 1], mesh.Positions[(3 * i) + 2]);
        var open = new ErrorDetail(DiagnosticCodes.MeshUnsupported,
            $"The triangles of the mesh body '{record.Handle.Id}' do not close a solid, so the pair check cannot give its overlap.");
        try
        {
            var solid = KernelSolid.Mesh(record.Handle.Id, "mesh", vertices, [.. mesh.Indices.Select(index => (int)index)]);
            return solid.Volume().Sign > 0 && Brep.Of(solid).Valid ? (solid, null) : (null, open);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return (null, open);
        }
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
