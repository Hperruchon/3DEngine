using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Joints;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0194 item 4 (phase P11b, TASK-0489): the command that places a child by two connectors one time. The order of the checks:
// the two bodies, the tree, the two connectors. The child takes the placement of a fixed joint at the place of the parent now, and
// each body under the child through a tight joint follows it in the same step, as for a PlaceBody.
public sealed class PlaceByConnectorsCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "PlaceByConnectors";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["parentBodyId"] = new("guid", Required: true),
            ["parentFeatureId"] = new("guid"),
            ["parentElement"] = new("string"),
            ["parentItemId"] = new("guid"),
            ["parentDatumId"] = new("guid"),
            ["childBodyId"] = new("guid", Required: true),
            ["childFeatureId"] = new("guid"),
            ["childElement"] = new("string"),
            ["childItemId"] = new("guid"),
            ["childDatumId"] = new("guid"),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid"),
        };

    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var place = (PlaceByConnectorsCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["parentBodyId"] = place.ParentBodyId,
            ["parentFeatureId"] = place.ParentFeatureId,
            ["parentElement"] = place.ParentElement,
            ["parentItemId"] = place.ParentItemId,
            ["parentDatumId"] = place.ParentDatumId,
            ["childBodyId"] = place.ChildBodyId,
            ["childFeatureId"] = place.ChildFeatureId,
            ["childElement"] = place.ChildElement,
            ["childItemId"] = place.ChildItemId,
            ["childDatumId"] = place.ChildDatumId,
        };
    }

    public Command Create(CommandInput input) => new PlaceByConnectorsCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        ParentBodyId = (Guid)input.Parameters["parentBodyId"]!,
        ParentFeatureId = input.Parameters.GetValueOrDefault("parentFeatureId") as Guid?,
        ParentElement = input.Parameters.GetValueOrDefault("parentElement") as string,
        ParentItemId = input.Parameters.GetValueOrDefault("parentItemId") as Guid?,
        ParentDatumId = input.Parameters.GetValueOrDefault("parentDatumId") as Guid?,
        ChildBodyId = (Guid)input.Parameters["childBodyId"]!,
        ChildFeatureId = input.Parameters.GetValueOrDefault("childFeatureId") as Guid?,
        ChildElement = input.Parameters.GetValueOrDefault("childElement") as string,
        ChildItemId = input.Parameters.GetValueOrDefault("childItemId") as Guid?,
        ChildDatumId = input.Parameters.GetValueOrDefault("childDatumId") as Guid?,
    };

    public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var place = (PlaceByConnectorsCommand)command;

        var parent = document.Bodies.FirstOrDefault(body => body.Handle.Id == place.ParentBodyId);
        if (parent is null)
            return JointResults.Refused(DiagnosticCodes.GeomBodyNotFound, $"No body with id '{place.ParentBodyId}' exists in the Document.");
        var child = document.Bodies.FirstOrDefault(body => body.Handle.Id == place.ChildBodyId);
        if (child is null)
            return JointResults.Refused(DiagnosticCodes.GeomBodyNotFound, $"No body with id '{place.ChildBodyId}' exists in the Document.");
        if (parent.Handle.Id == child.Handle.Id)
            return Task.FromResult(JointResults.Invalid("PlaceByConnectors places one body on another, and the parent and the child are the same body (ADR-0194 item 4)."));

        // A tight joint gives the placement of its child, and the child of the command moves each body under it through a tight
        // joint, so the parent must not be under the child.
        if (JointTree.TightParentOf(document.Joints, child.Handle.Id) is { } held)
            return JointResults.Refused(DiagnosticCodes.JointPlaced, $"The body '{child.Handle.Id}' is the child of the tight joint '{held.Id}', and the joint gives its placement (ADR-0194 item 4).");
        if (JointTree.IsAncestor([.. document.Joints.Where(joint => joint.State == JointRecord.Tight)], child.Handle.Id, parent.Handle.Id))
            return JointResults.Refused(DiagnosticCodes.JointLoop, $"The body '{parent.Handle.Id}' is under the body '{child.Handle.Id}' through tight joints, so a move of the child moves the parent too (ADR-0194 item 4).");

        var (parentConnector, parentRefusal) = JointResults.Connector(document, parent, "parent", place.ParentFeatureId, place.ParentElement, place.ParentItemId, place.ParentDatumId);
        if (parentRefusal is not null)
            return Task.FromResult(parentRefusal);
        var (childConnector, childRefusal) = JointResults.Connector(document, child, "child", place.ChildFeatureId, place.ChildElement, place.ChildItemId, place.ChildDatumId);
        if (childRefusal is not null)
            return Task.FromResult(childRefusal);

        // The placement of a fixed joint that the Document does not hold.
        var snap = new JointRecord(place.CommandId, JointRecord.Fixed, parent.Handle.Id, parentConnector!, child.Handle.Id, childConnector!, JointValue.None, null);
        var outputs = new Outputs(new Dictionary<string, object?> { ["bodyId"] = child.Handle.Id });
        return Task.FromResult(CommandHandlerResult.Success(outputs, changedBodies: JointResults.Moved(document, document.Joints, snap)));
    }
}
