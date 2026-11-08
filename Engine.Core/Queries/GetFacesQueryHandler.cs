using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Contracts.Sketch;
using Engine.Core.Features;
using Engine.Kernel;

namespace Engine.Core.Queries;

// ADR-0058 items 3 to 5. The query builds the B-Rep of a live body in the owned kernel when it first asks for it, and
// keeps the result under the geometry key of the body: a rebuild gives a body a new key (ADR-0037 item 2), so a key
// names one solid. The query needs no capability of the backend. A body that is not live gives E-GEOM-BODY-NOT-FOUND,
// a body with a fault gives E-GEOM-BODY-FAULTED, and a body that the kernel cannot build gives E-GEOM-FACES-FAILED.
public sealed class GetFacesQueryHandler : IQueryHandler
{
    // Finding E196 of the codebase review of 2026-11-08 (b) (TASK-0303): the cache keys a result by the geometry key only,
    // which comes from the identifier of a command, so the cache belongs to the registry of one document session. Each host
    // makes its own registry for its session (EngineHosting.CreateDefault, Cli); a registry shared by two documents would
    // give the faces of the other document.
    // Item 5: the count of the results that the cache keeps; the oldest goes first.
    public const int CacheSize = 64;

    private readonly object _gate = new();
    private readonly Dictionary<Guid, FacesInfo> _cache = [];
    private readonly Queue<Guid> _order = new();

    public string QueryName => "GetFaces";
    public int SchemaVersion => 1;

    // The count of the B-Reps that the handler built, for a test of the cache.
    internal int BrepsMade { get; private set; }

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["faces"] = new("array"),
            ["edges"] = new("array"),
        };

    // Per ADR-0016.
    public Query Create(QueryInput input) => new GetFacesQuery
    {
        QueryId = input.QueryId,
        BodyId = (Guid)input.Parameters["bodyId"]!,
    };

    public Task<QueryHandlerResult> Handle(
        Query query,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var faces = (GetFacesQuery)query;
        if (document.Bodies.FirstOrDefault(body => body.Handle.Id == faces.BodyId) is not { } record)
            return Task.FromResult(QueryHandlerResult.Failure(new ErrorDetail(
                DiagnosticCodes.GeomBodyNotFound, $"No live body with id '{faces.BodyId}' exists in the Document.")));

        // ADR-0083 item 4 (TASK-0258): a mesh body has no named faces.
        if (record.Kind == Commands.ImportStlCommandHandler.MeshKind)
            return Task.FromResult(QueryHandlerResult.Failure(new ErrorDetail(
                DiagnosticCodes.MeshUnsupported, $"The body '{faces.BodyId}' is a mesh body of an STL file, which has no named faces (ADR-0083 item 4).")));

        // Item 3: a body with a fault keeps the solid of an earlier state, which the steps in effect no longer give.
        if (record.Fault is { } fault)
            return Task.FromResult(QueryHandlerResult.Failure(new ErrorDetail(DiagnosticCodes.GeomBodyFaulted,
                $"The body '{faces.BodyId}' has the fault {fault.Code}, so its solid comes from an earlier state and its faces are not known.")));

        lock (_gate)
        {
            if (_cache.TryGetValue(record.GeometryKey, out var kept))
                return Task.FromResult(QueryHandlerResult.Success(kept));
        }

        var (solid, reason) = KernelProjection.Of(document, faces.BodyId);
        if (solid is null)
            return Task.FromResult(QueryHandlerResult.Failure(new ErrorDetail(DiagnosticCodes.GeomFacesFailed, reason!)));
        var brep = Brep.Of(solid);
        var info = new FacesInfo(Entries(solid, brep), Edges(brep));

        lock (_gate)
        {
            BrepsMade++;
            if (_cache.TryAdd(record.GeometryKey, info))
            {
                _order.Enqueue(record.GeometryKey);
                if (_order.Count > CacheSize)
                    _cache.Remove(_order.Dequeue());
            }
        }

        return Task.FromResult(QueryHandlerResult.Success(info));
    }

    // Item 4: one entry for each tag, in the order of its first face. The faces of one tag that share an edge are one
    // part, so the facets of a curve are one part and a face that a cut splits has two.
    internal static IReadOnlyList<FaceEntry> Entries(KernelSolid solid, Brep brep)
    {
        var tags = brep.Faces.Select(face => face.Tag).Distinct().ToList();
        var entries = new List<FaceEntry>();
        foreach (var tag in tags)
        {
            var members = Enumerable.Range(0, brep.Faces.Count).Where(i => brep.Faces[i].Tag == tag).ToList();

            // The parts: the faces of the tag, joined by each edge whose faces all have the tag.
            var part = members.ToDictionary(i => i, i => i);
            int Root(int i) => part[i] == i ? i : part[i] = Root(part[i]);
            foreach (var edge in brep.Edges.Where(edge => edge.Faces.Count == 2 && edge.Faces.All(part.ContainsKey)))
                part[Root(edge.Faces[0])] = Root(edge.Faces[1]);
            var parts = members.Select(Root).Distinct().Count();

            var holes = members.Sum(i => brep.Faces[i].Loops.Count(loop => !loop.Outer));
            var area = members.Sum(i => Area(brep, brep.Faces[i]));
            var plane = brep.Faces[members[0]].Plane;
            var normal = members.All(i => brep.Faces[i].Plane == plane) ? Unit(plane.Normal) : null;

            // A point inside the face: the mean of the vertices of its first convex piece.
            var piece = solid.Polygons.First(polygon => polygon.Tag == tag).Vertices;
            var sum = piece.Aggregate(new Vector3R(Rational.Zero, Rational.Zero, Rational.Zero), (total, p) => total + p);
            var count = (Rational)piece.Count;
            var point = new Vector3R(sum.X / count, sum.Y / count, sum.Z / count).ToDoubles();

            // ADR-0075 item 2: the outline, each edge between a face of the name and a face of another name, in the order of
            // the edges of the B-Rep. An edge between two facets of a curve, or between two faces of one name, is inside the
            // name. ADR-0077 item 6 (finding E165, TASK-0251): so the outline holds the segments of the edges of the name and
            // no other; until TASK-0251 it took each edge with one face of the name, also an edge of one face or of more
            // than two faces, which no edge held.
            var outline = brep.Edges
                .Where(edge => edge.Faces.Any(face => brep.Faces[face].Tag == tag) && edge.Faces.Any(face => brep.Faces[face].Tag != tag))
                .Select(edge => new FaceSegment(Position(brep.Vertices[edge.From]), Position(brep.Vertices[edge.To])))
                .ToList();

            entries.Add(new FaceEntry(
                new DatumReference(tag.Feature, tag.Element, tag.Item),
                parts,
                holes,
                area,
                new DatumPoint(point.X, point.Y, point.Z),
                normal,
                outline));
        }

        return entries;
    }

    // ADR-0077 items 1 and 2: one entry for each pair of names that a B-Rep edge bounds. An edge between two faces of one
    // name is inside the name. The parts are the chains of the segments of the pair that share a vertex.
    //
    // ADR-0077 item 6 (finding E165 of the codebase review of 2026-11-06 (e), TASK-0251): a B-Rep edge of more than two
    // faces, where two solids touch along a line, is an edge of each pair of different names of its faces; an edge of one
    // face is no edge. Until TASK-0251 an edge of more than two faces gave no entry.
    internal static IReadOnlyList<EdgeEntry> Edges(Brep brep)
    {
        var order = new List<(FaceTag First, FaceTag Second)>();
        var members = new Dictionary<(FaceTag First, FaceTag Second), List<BrepEdge>>();
        foreach (var edge in brep.Edges)
        {
            var names = edge.Faces.Select(face => brep.Faces[face].Tag).Distinct().Order(Comparer<FaceTag>.Create(Compare)).ToList();
            for (var i = 0; i < names.Count; i++)
                for (var j = i + 1; j < names.Count; j++)
                {
                    var pair = (names[i], names[j]);
                    if (!members.TryGetValue(pair, out var list))
                    {
                        members[pair] = list = [];
                        order.Add(pair);
                    }
                    list.Add(edge);
                }
        }

        var entries = new List<EdgeEntry>();
        foreach (var pair in order)
        {
            var edges = members[pair];
            var part = new Dictionary<int, int>();
            int Root(int v) => part[v] == v ? v : part[v] = Root(part[v]);
            foreach (var edge in edges)
            {
                part.TryAdd(edge.From, edge.From);
                part.TryAdd(edge.To, edge.To);
                part[Root(edge.From)] = Root(edge.To);
            }

            var length = edges.Sum(edge =>
            {
                var d = brep.Vertices[edge.To] - brep.Vertices[edge.From];
                return Math.Sqrt(d.Dot(d).ToDouble());
            });
            entries.Add(new EdgeEntry(
                Reference(pair.First),
                Reference(pair.Second),
                part.Keys.Select(Root).Distinct().Count(),
                length,
                [.. edges.Select(edge => new FaceSegment(Position(brep.Vertices[edge.From]), Position(brep.Vertices[edge.To])))]));
        }

        return entries;
    }

    // Item 1: the order of two names: the identifier of the feature, then the element as text with no culture, then the
    // item, with no item first.
    internal static int Compare(FaceTag a, FaceTag b)
    {
        var byFeature = a.Feature.CompareTo(b.Feature);
        if (byFeature != 0)
            return byFeature;
        var byElement = string.CompareOrdinal(a.Element, b.Element);
        if (byElement != 0)
            return byElement;
        return Nullable.Compare(a.Item, b.Item);
    }

    private static DatumReference Reference(FaceTag tag) => new(tag.Feature, tag.Element, tag.Item);

    // The area of a face: half the length of the sum of the cross products of each loop. A hole turns the other way, so
    // its part of the sum is negative. The exact square becomes a double before its root.
    private static double Area(Brep brep, BrepFace face)
    {
        var total = new Vector3R(Rational.Zero, Rational.Zero, Rational.Zero);
        foreach (var loop in face.Loops)
        {
            for (var i = 0; i < loop.Vertices.Count; i++)
                total += brep.Vertices[loop.Vertices[i]].Cross(brep.Vertices[loop.Vertices[(i + 1) % loop.Vertices.Count]]);
        }
        return Math.Sqrt(total.Dot(total).ToDouble()) / 2;
    }

    private static DatumPoint Position(Vector3R vertex)
    {
        var (x, y, z) = vertex.ToDoubles();
        return new DatumPoint(x, y, z);
    }

    private static DatumAxis Unit(Vector3R normal)
    {
        var (x, y, z) = normal.ToDoubles();
        var length = Math.Sqrt((x * x) + (y * y) + (z * z));
        return new DatumAxis(x / length, y / length, z / length);
    }
}
