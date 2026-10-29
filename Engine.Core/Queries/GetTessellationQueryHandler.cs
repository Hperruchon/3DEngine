using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;

namespace Engine.Core.Queries;

// ADR-0018 item 6. The result fields are those of Tessellation. The schema
// gives `positions` and `indices` the type "array" and no item type, because
// no client needs it now (ADR-0018 item 7, ADR-0013 §2).
public sealed class GetTessellationQueryHandler : IQueryHandler
{
    public string QueryName => "GetTessellation";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["vertexCount"] = new("integer"),
            ["triangleCount"] = new("integer"),
            ["positions"] = new("array"),
            ["indices"] = new("array"),
        };

    // Per ADR-0016.
    public Query Create(QueryInput input) => new GetTessellationQuery
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
        var tessellation = (GetTessellationQuery)query;

        var ops = backend.TryGet<ITessellationOps>();
        if (ops is null)
        {
            return Task.FromResult(QueryHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomCapMissing,
                    "Active backend does not implement ITessellationOps; cannot give the triangles of a body.")));
        }

        var body = document.Bodies.FirstOrDefault(b => b.Handle.Id == tessellation.BodyId);
        if (body is null)
        {
            return Task.FromResult(QueryHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomBodyNotFound,
                    $"No body with id '{tessellation.BodyId}' exists in the Document.")));
        }

        // ADR-0037 item 2: the solid of the body, under its geometry key.
        return Task.FromResult(QueryHandlerResult.Success(ops.Tessellate(new BodyHandle(body.GeometryKey))));
    }
}
