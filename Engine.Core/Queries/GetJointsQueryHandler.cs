using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;

namespace Engine.Core.Queries;

// ADR-0193 item 7 (phase P11a, TASK-0479). The query reads the records of the joints in the Document, so it needs no capability
// of the backend, gives no error and writes nothing to the log.
public sealed class GetJointsQueryHandler : IQueryHandler
{
    public string QueryName => "GetJoints";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } = new Dictionary<string, FieldSchema>();

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["joints"] = new("array"),
        };

    // Per ADR-0016.
    public Query Create(QueryInput input) => new GetJointsQuery { QueryId = input.QueryId };

    public Task<QueryHandlerResult> Handle(Query query, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var joints = GetSheetsQueryHandler.InLogOrder(document, document.Joints.ToDictionary(joint => joint.Id)).ToList();
        return Task.FromResult(QueryHandlerResult.Success(new JointsInfo(joints)));
    }
}
