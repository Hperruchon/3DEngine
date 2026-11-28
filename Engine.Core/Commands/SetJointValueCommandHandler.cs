using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0193 items 4 to 6 (phase P11a, TASK-0479): the command that sets the value of a joint. A fixed joint has no value, so it
// refuses each value. The order of the checks: the joint, the value, the limits.
public sealed class SetJointValueCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "SetJointValue";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["jointId"] = new("guid", Required: true),
            ["cos"] = new("number", Unit: UnitKinds.Cosine),
            ["sin"] = new("number", Unit: UnitKinds.Sine),
            ["distance"] = new("number", Unit: UnitKinds.Length),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["jointId"] = new("guid"),
        };

    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var set = (SetJointValueCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["jointId"] = set.JointId,
            ["cos"] = set.Cos,
            ["sin"] = set.Sin,
            ["distance"] = set.Distance,
        };
    }

    public Command Create(CommandInput input) => new SetJointValueCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        JointId = (Guid)input.Parameters["jointId"]!,
        Cos = input.Parameters.GetValueOrDefault("cos") as double?,
        Sin = input.Parameters.GetValueOrDefault("sin") as double?,
        Distance = input.Parameters.GetValueOrDefault("distance") as double?,
    };

    public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var set = (SetJointValueCommand)command;
        if (JointResults.Find(document, set.JointId) is not { } joint)
            return Task.FromResult(JointResults.NotFound(set.JointId));
        if (joint.Kind == Contracts.Joints.JointRecord.Fixed)
            return Task.FromResult(JointResults.Invalid($"The joint '{joint.Id}' is fixed, and a fixed joint has no value (ADR-0193 item 4)."));

        var (value, refusal) = JointResults.Value(joint.Kind, "value", set.Cos, set.Sin, set.Distance);
        if (refusal is not null)
            return Task.FromResult(refusal);
        if (JointResults.OutsideLimits(joint.Kind, value!, joint.Limits) is { } outside)
            return Task.FromResult(outside);

        var changed = joint with { Value = value! };
        var outputs = new Outputs(new Dictionary<string, object?> { ["jointId"] = joint.Id });
        return Task.FromResult(CommandHandlerResult.Success(
            outputs,
            changedBodies: JointResults.Moved(document, [.. document.Joints.Select(other => other.Id == joint.Id ? changed : other)], changed),
            changedJoints: [changed]));
    }
}
