using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;

namespace Engine.Core.Queries;

// ADR-0071 item 6. The query reads the records of the Document, so it needs no capability of the backend. The order is
// the order of the log, because the collection of the Document gives no order.
public sealed class GetPartReferencesQueryHandler : IQueryHandler
{
    public string QueryName => "GetPartReferences";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>();

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["kind"] = new("string"),
            ["references"] = new("array"),
        };

    // Per ADR-0016.
    public Query Create(QueryInput input) => new GetPartReferencesQuery { QueryId = input.QueryId };

    public Task<QueryHandlerResult> Handle(
        Query query,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var order = document.Log.Select((entry, index) => (entry.Command.CommandId, index))
            .GroupBy(pair => pair.CommandId)
            .ToDictionary(group => group.Key, group => group.First().index);
        var references = document.References.OrderBy(reference => order.GetValueOrDefault(reference.Id, int.MaxValue)).ToList();
        return Task.FromResult(QueryHandlerResult.Success(new PartReferencesInfo(document.Kind, references)));
    }
}
