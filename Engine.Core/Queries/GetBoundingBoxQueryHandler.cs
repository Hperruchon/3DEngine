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

    // Per ADR-0016.
    public Query Create(QueryInput input) => new GetBoundingBoxQuery
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
        var bbox = (GetBoundingBoxQuery)query;

        var geom = backend.TryGet<IGeometryQuery>();
        if (geom is null)
        {
            return Task.FromResult(QueryHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomCapMissing,
                    "Active backend does not implement IGeometryQuery; cannot compute bounding box.")));
        }

        var body = document.Bodies.FirstOrDefault(b => b.Handle.Id == bbox.BodyId);
        if (body is null)
        {
            return Task.FromResult(QueryHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomBodyNotFound,
                    $"No body with id '{bbox.BodyId}' exists in the Document.")));
        }

        // ADR-0037 item 2: the solid of the body, under its geometry key.
        // Finding E32 of the codebase review of 2026-10-08 (TASK-0135): a body that the backend does not
        // hold threw KeyNotFoundException through the query bus, and a host answered HTTP 500. A failure
        // of the backend gives E-GEOM-NATIVE-OP, as for a command.
        try
        {
            return Task.FromResult(QueryHandlerResult.Success(geom.GetBoundingBox(new BodyHandle(body.GeometryKey))));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Task.FromResult(QueryHandlerResult.Failure(new ErrorDetail(
                DiagnosticCodes.GeomNativeOp, $"The backend could not give the bounding box of the body '{body.Handle.Id}': {ex.GetType().Name}: {ex.Message}")));
        }
    }
}
