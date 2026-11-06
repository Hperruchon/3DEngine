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
        var info = new FacesInfo(Entries(solid, Brep.Of(solid)));

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

            // ADR-0075 item 2: the outline, each edge with one face of the name, in the order of the edges of the B-Rep. An
            // edge between two facets of a curve, or between two faces of one name, is inside the name.
            var outline = brep.Edges
                .Where(edge => edge.Faces.Count(face => brep.Faces[face].Tag == tag) == 1)
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
