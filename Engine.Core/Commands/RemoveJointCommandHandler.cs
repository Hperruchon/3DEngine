using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0193 item 6 (phase P11a, TASK-0479): the removal of a joint, in the form of RemoveSheetCommandHandler. No body moves.
public sealed class RemoveJointCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "RemoveJoint";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["jointId"] = new("guid", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["jointId"] = new("guid"),
        };

    public IReadOnlyDictionary<string, object?> Encode(Command command)
        => new Dictionary<string, object?>(StringComparer.Ordinal) { ["jointId"] = ((RemoveJointCommand)command).JointId };

    public Command Create(CommandInput input) => new RemoveJointCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        JointId = (Guid)input.Parameters["jointId"]!,
    };

    public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var remove = (RemoveJointCommand)command;
        if (JointResults.Find(document, remove.JointId) is not { } joint)
            return Task.FromResult(JointResults.NotFound(remove.JointId));

        var outputs = new Outputs(new Dictionary<string, object?> { ["jointId"] = joint.Id });
        return Task.FromResult(CommandHandlerResult.Success(outputs, removedJoints: [joint.Id]));
    }
}
