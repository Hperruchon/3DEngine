using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Joints;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0193 items 1 to 6 (phase P11a, TASK-0479): the command that adds a joint. The order of the checks: the identifier, the
// kind, the two bodies, the tree, the two connectors, the value, the limits. The handler changes no solid of the backend, so it
// needs no capability.
public sealed class AddJointCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "AddJoint";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["kind"] = new("string", Required: true),
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
            ["cos"] = new("number", Unit: UnitKinds.Cosine),
            ["sin"] = new("number", Unit: UnitKinds.Sine),
            ["distance"] = new("number", Unit: UnitKinds.Length),
            ["lowerCos"] = new("number", Unit: UnitKinds.Cosine),
            ["lowerSin"] = new("number", Unit: UnitKinds.Sine),
            ["upperCos"] = new("number", Unit: UnitKinds.Cosine),
            ["upperSin"] = new("number", Unit: UnitKinds.Sine),
            ["lowerDistance"] = new("number", Unit: UnitKinds.Length),
            ["upperDistance"] = new("number", Unit: UnitKinds.Length),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["jointId"] = new("guid"),
        };

    // ADR-0015 item 11: the parameters that Create reads.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var add = (AddJointCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["kind"] = add.Kind,
            ["parentBodyId"] = add.ParentBodyId,
            ["parentFeatureId"] = add.ParentFeatureId,
            ["parentElement"] = add.ParentElement,
            ["parentItemId"] = add.ParentItemId,
            ["parentDatumId"] = add.ParentDatumId,
            ["childBodyId"] = add.ChildBodyId,
            ["childFeatureId"] = add.ChildFeatureId,
            ["childElement"] = add.ChildElement,
            ["childItemId"] = add.ChildItemId,
            ["childDatumId"] = add.ChildDatumId,
            ["cos"] = add.Cos,
            ["sin"] = add.Sin,
            ["distance"] = add.Distance,
            ["lowerCos"] = add.LowerCos,
            ["lowerSin"] = add.LowerSin,
            ["upperCos"] = add.UpperCos,
            ["upperSin"] = add.UpperSin,
            ["lowerDistance"] = add.LowerDistance,
            ["upperDistance"] = add.UpperDistance,
        };
    }

    public Command Create(CommandInput input) => new AddJointCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        Kind = (string)input.Parameters["kind"]!,
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
        Cos = input.Parameters.GetValueOrDefault("cos") as double?,
        Sin = input.Parameters.GetValueOrDefault("sin") as double?,
        Distance = input.Parameters.GetValueOrDefault("distance") as double?,
        LowerCos = input.Parameters.GetValueOrDefault("lowerCos") as double?,
        LowerSin = input.Parameters.GetValueOrDefault("lowerSin") as double?,
        UpperCos = input.Parameters.GetValueOrDefault("upperCos") as double?,
        UpperSin = input.Parameters.GetValueOrDefault("upperSin") as double?,
        LowerDistance = input.Parameters.GetValueOrDefault("lowerDistance") as double?,
        UpperDistance = input.Parameters.GetValueOrDefault("upperDistance") as double?,
    };

    public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var add = (AddJointCommand)command;

        // Item 1: a joint takes the identifier of its command, so a command that the log holds is refused, as for a sheet.
        if (Features.FeatureList.Of(document).Holds(add.CommandId))
            return JointResults.Refused(DiagnosticCodes.JointIdInUse, $"The log holds a command with the id '{add.CommandId}', and a joint takes the id of its command.");

        if (!JointRecord.Kinds.Contains(add.Kind))
            return Task.FromResult(JointResults.Invalid($"The kind '{add.Kind}' is not a kind of a joint: fixed, pivot or slider (ADR-0193 item 1)."));

        var parent = document.Bodies.FirstOrDefault(body => body.Handle.Id == add.ParentBodyId);
        if (parent is null)
            return JointResults.Refused(DiagnosticCodes.GeomBodyNotFound, $"No body with id '{add.ParentBodyId}' exists in the Document.");
        var child = document.Bodies.FirstOrDefault(body => body.Handle.Id == add.ChildBodyId);
        if (child is null)
            return JointResults.Refused(DiagnosticCodes.GeomBodyNotFound, $"No body with id '{add.ChildBodyId}' exists in the Document.");
        if (parent.Handle.Id == child.Handle.Id)
            return Task.FromResult(JointResults.Invalid("A joint connects two bodies, and the parent and the child are the same body (ADR-0193 item 1)."));

        // Item 6: one parent joint for each body, and no loop. Each joint of the Document counts, also a joint of a body that a
        // later command consumed, because an Undo can make that body live again.
        if (JointTree.ParentOf(document.Joints, child.Handle.Id) is { } held)
            return JointResults.Refused(DiagnosticCodes.JointParentInUse, $"The body '{child.Handle.Id}' is the child of the joint '{held.Id}', and a body has one parent joint (ADR-0193 item 6).");
        if (JointTree.IsAncestor(document.Joints, child.Handle.Id, parent.Handle.Id))
            return JointResults.Refused(DiagnosticCodes.JointLoop, $"The body '{child.Handle.Id}' is above the body '{parent.Handle.Id}' in the tree of the joints, so the joint closes a loop (ADR-0193 item 6).");

        var (parentConnector, parentRefusal) = JointResults.Connector(document, parent, "parent", add.ParentFeatureId, add.ParentElement, add.ParentItemId, add.ParentDatumId);
        if (parentRefusal is not null)
            return Task.FromResult(parentRefusal);
        var (childConnector, childRefusal) = JointResults.Connector(document, child, "child", add.ChildFeatureId, add.ChildElement, add.ChildItemId, add.ChildDatumId);
        if (childRefusal is not null)
            return Task.FromResult(childRefusal);

        var (value, valueRefusal) = JointResults.Value(add.Kind, "value", add.Cos, add.Sin, add.Distance);
        if (valueRefusal is not null)
            return Task.FromResult(valueRefusal);
        var (limits, limitsRefusal) = JointResults.Limits(add.Kind, add.LowerCos, add.LowerSin, add.UpperCos, add.UpperSin, add.LowerDistance, add.UpperDistance);
        if (limitsRefusal is not null)
            return Task.FromResult(limitsRefusal);
        if (JointResults.OutsideLimits(add.Kind, value!, limits) is { } outside)
            return Task.FromResult(outside);

        var joint = new JointRecord(add.CommandId, add.Kind, parent.Handle.Id, parentConnector!, child.Handle.Id, childConnector!, value!, limits);
        var outputs = new Outputs(new Dictionary<string, object?> { ["jointId"] = joint.Id });
        return Task.FromResult(CommandHandlerResult.Success(
            outputs,
            changedBodies: JointResults.Moved(document, [.. document.Joints, joint], joint),
            createdJoints: [joint]));
    }
}
