using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;

namespace Engine.Core.Queries;

// ADR-0028 item 2. The query reads the record of the body in the Document, so it needs no
// capability of the backend. A hidden body is a body, so the query gives it as any other
// (ADR-0028 item 4); a consumed body is not live (ADR-0021 item 4).
public sealed class GetBodyInfoQueryHandler : IQueryHandler
{
    public string QueryName => "GetBodyInfo";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid"),
            ["kind"] = new("string"),
            ["name"] = new("string"),
            ["visible"] = new("boolean"),
        };

    // Per ADR-0016.
    public Query Create(QueryInput input) => new GetBodyInfoQuery
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
        var info = (GetBodyInfoQuery)query;

        var body = document.Bodies.FirstOrDefault(b => b.Handle.Id == info.BodyId);
        if (body is null)
        {
            return Task.FromResult(QueryHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomBodyNotFound,
                    $"No body with id '{info.BodyId}' exists in the Document.")));
        }

        return Task.FromResult(QueryHandlerResult.Success(new BodyInfo(body.Handle.Id, body.Kind, body.Name, body.Visible)));
    }
}
