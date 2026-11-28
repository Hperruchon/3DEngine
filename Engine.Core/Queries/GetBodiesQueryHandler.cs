using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Features;

namespace Engine.Core.Queries;

// ADR-0189 items 3 to 5 (phase P21, TASK-0471). The query reads the records of the bodies in the Document, so it needs no
// capability of the backend, gives no error and writes nothing to the log. A live body is a body of Document.Bodies: a
// consumed body is not live (ADR-0021 item 1), and a hidden body and a body with a fault are live, as in the snapshot.
public sealed class GetBodiesQueryHandler : IQueryHandler
{
    public string QueryName => "GetBodies";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } = new Dictionary<string, FieldSchema>();

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodies"] = new("array"),
        };

    // Per ADR-0016.
    public Query Create(QueryInput input) => new GetBodiesQuery { QueryId = input.QueryId };

    public Task<QueryHandlerResult> Handle(Query query, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        // Item 5: each item is the answer of GetBodyInfo, with the distance in effect of an extrude (ADR-0049 item 7). Item 3:
        // the order of the log of the command that made each body.
        var features = FeatureList.Of(document);
        // ADR-0192 (phase K25, TASK-0478): the order of the log, from the one function that the snapshot and CheckInterference read.
        var bodies = LogOrder.Bodies(document)
            .Select(body => new BodyInfo(body.Handle.Id, body.Kind, body.Name, body.Visible, features.DistanceInEffect(body.Handle.Id), body.Placement))
            .ToList();
        return Task.FromResult(QueryHandlerResult.Success(new BodiesInfo(bodies)));
    }
}
