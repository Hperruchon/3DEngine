using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Joints;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0194 item 2 (phase P11b, TASK-0489): the command that sets the state of a joint. The order of the checks: the joint, the
// state. A state that the joint has already changes nothing, so it is refused, as a move of a view to its own place is.
public sealed class SetJointStateCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "SetJointState";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["jointId"] = new("guid", Required: true),
            ["state"] = new("string", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["jointId"] = new("guid"),
        };

    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var set = (SetJointStateCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["jointId"] = set.JointId,
            ["state"] = set.State,
        };
    }

    public Command Create(CommandInput input) => new SetJointStateCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        JointId = (Guid)input.Parameters["jointId"]!,
        State = (string)input.Parameters["state"]!,
    };

    public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var set = (SetJointStateCommand)command;
        if (JointResults.Find(document, set.JointId) is not { } joint)
            return Task.FromResult(JointResults.NotFound(set.JointId));
        if (!JointRecord.States.Contains(set.State))
            return Task.FromResult(JointResults.Invalid($"The state '{set.State}' is not a state of a joint: tight or loose (ADR-0194 item 1)."));
        if (joint.State == set.State)
            return Task.FromResult(JointResults.Invalid($"The joint '{joint.Id}' is already {set.State}, so the command changes nothing (ADR-0194 item 2)."));

        var changed = joint with { State = set.State };
        var outputs = new Outputs(new Dictionary<string, object?> { ["jointId"] = joint.Id });
        return Task.FromResult(CommandHandlerResult.Success(
            outputs,
            changedBodies: JointResults.Moved(document, [.. document.Joints.Select(other => other.Id == joint.Id ? changed : other)], changed),
            changedJoints: [changed]));
    }
}
