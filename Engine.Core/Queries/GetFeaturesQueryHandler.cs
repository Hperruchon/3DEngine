using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Commands;
using Engine.Core.Features;

namespace Engine.Core.Queries;

// ADR-0054 items 2, 3 and 5. The query reads the feature list and the records of the Document, so it needs no capability
// of the backend and gives no error. The faults stay in the records of the Document (item 3); P8d2 decides their place.
public sealed class GetFeaturesQueryHandler : IQueryHandler
{
    public string QueryName => "GetFeatures";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } = new Dictionary<string, FieldSchema>();

    public IReadOnlyDictionary<string, FieldSchema> Result { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["features"] = new("array"),
        };

    // Per ADR-0016.
    public Query Create(QueryInput input) => new GetFeaturesQuery { QueryId = input.QueryId };

    public Task<QueryHandlerResult> Handle(
        Query query,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var list = FeatureList.Of(document);
        var features = list.Steps.Where(FeatureList.IsFeature).Select(command => Feature(document, list, command)).ToList();
        return Task.FromResult(QueryHandlerResult.Success(new FeaturesInfo(features)));
    }

    private static FeatureInfo Feature(Document document, FeatureList list, Command command)
    {
        var id = command.CommandId;
        switch (command)
        {
            case CreateDatumCommand create:
            {
                var fault = document.Datums.FirstOrDefault(datum => datum.Id == id)?.Fault;
                return Row([create.FeatureId], null, fault is null ? null : new FeatureFault(fault.Code, fault.Message));
            }

            case CreateSketchCommand create:
            {
                var record = document.Sketches.FirstOrDefault(sketch => sketch.Id == id);
                var references = record?.References ?? [];
                var fault = references.Select(reference => reference.Fault).FirstOrDefault(item => item is not null);
                Guid[] inputs = [create.DatumId, .. references.Select(reference => reference.Target.FeatureId).Distinct()];
                return Row(inputs, null, fault is null ? null : new FeatureFault(fault.Code, fault.Message));
            }

            case PlaceImageCommand place:
                return Row([place.DatumId], null, null);

            default:
            {
                Guid[] inputs = command is ExtrudeCommand extrude ? [extrude.SketchId] : [.. FeatureList.Operands(command)];
                if (list.Consumer(id) is { } consumer)
                    return Row(inputs, consumer.CommandId, null);
                var fault = document.Bodies.FirstOrDefault(body => body.Handle.Id == id)?.Fault;
                return Row(inputs, null, fault is null ? null : new FeatureFault(fault.Code, fault.Message));
            }
        }

        // Item 3: a consumed body is consumed; a body with a fault failed; a datum or a sketch with a fault is unresolved.
        FeatureInfo Row(IReadOnlyList<Guid> inputs, Guid? consumedBy, FeatureFault? fault)
        {
            var state = consumedBy is not null ? Consumed
                : fault is null ? Correct
                : FeatureList.Creates(command) ? Failed
                : Unresolved;
            return new FeatureInfo(id, command.Name, inputs, consumedBy, state, fault);
        }
    }

    public const string Correct = "correct";
    public const string Failed = "failed";
    public const string Consumed = "consumed";
    public const string Unresolved = "unresolved";
}
