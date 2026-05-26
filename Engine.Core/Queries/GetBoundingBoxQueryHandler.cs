using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;

namespace Engine.Core.Queries;

public sealed class GetBoundingBoxQueryHandler : IQueryHandler
{
    public string QueryName => "GetBoundingBox";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["minX"] = new("number"),
            ["minY"] = new("number"),
            ["minZ"] = new("number"),
            ["maxX"] = new("number"),
            ["maxY"] = new("number"),
            ["maxZ"] = new("number"),
        };

    public Task<QueryHandlerResult> Handle(
        Query query,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var bbox = (GetBoundingBoxQuery)query;

        var geom = backend.TryGet<IGeometryQuery>();
        if (geom is null)
        {
            return Task.FromResult(QueryHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomCapMissing,
                    "Active backend does not implement IGeometryQuery; cannot compute bounding box.")));
        }

        if (!document.Bodies.Any(b => b.Handle.Id == bbox.BodyId))
        {
            return Task.FromResult(QueryHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomBodyNotFound,
                    $"No body with id '{bbox.BodyId}' exists in the Document.")));
        }

        var aabb = geom.GetBoundingBox(new BodyHandle(bbox.BodyId));
        return Task.FromResult(QueryHandlerResult.Success(aabb));
    }
}
