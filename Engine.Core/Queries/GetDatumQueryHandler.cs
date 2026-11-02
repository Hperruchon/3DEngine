using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;

namespace Engine.Core.Queries;

// ADR-0051 item 9. The query reads the record in the Document, so it needs no capability of the
// backend. An origin plane is not a record of the Document: its frame is fixed (ADR-0033 item 1).
public sealed class GetDatumQueryHandler : IQueryHandler
{
    public string QueryName => "GetDatum";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["datumId"] = new("guid", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["id"] = new("guid"),
            ["name"] = new("string"),
            ["reference"] = new("object"),
            ["frame"] = new("object"),
            ["fault"] = new("object"),
        };

    // Per ADR-0016.
    public Query Create(QueryInput input) => new GetDatumQuery
    {
        QueryId = input.QueryId,
        DatumId = (Guid)input.Parameters["datumId"]!,
    };

    public Task<QueryHandlerResult> Handle(
        Query query,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var get = (GetDatumQuery)query;
        return Task.FromResult(document.Datums.FirstOrDefault(datum => datum.Id == get.DatumId) is { } found
            ? QueryHandlerResult.Success(found)
            : QueryHandlerResult.Failure(new ErrorDetail(
                DiagnosticCodes.DatumNotFound, $"No datum in effect with id '{get.DatumId}' exists in the Document.")));
    }
}
