using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Contracts.Sketch;
using Engine.Core.Persistence;
using Engine.Core.Sketch;

namespace Engine.Core.Commands;

// ADR-0033 items 3, 5 and 6: the new values must form the same kind of constraint on the
// same items, so a value of a constraint with no value is refused.
public sealed class SetSketchConstraintValueCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "SetSketchConstraintValue";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sketchId"] = new("guid", Required: true),
            ["constraintId"] = new("guid", Required: true),
            ["value"] = new("number", Required: true),
            ["secondValue"] = new("number", Required: false),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["constraintId"] = new("guid"),
        };

    // ADR-0015 item 11: the parameters that Create reads.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (SetSketchConstraintValueCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sketchId"] = typed.SketchId,
            ["constraintId"] = typed.ConstraintId,
            ["value"] = typed.Value,
            ["secondValue"] = typed.SecondValue,
        };
    }

    // Per ADR-0016.
    public Command Create(CommandInput input) => new SetSketchConstraintValueCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SketchId = (Guid)input.Parameters["sketchId"]!,
        ConstraintId = (Guid)input.Parameters["constraintId"]!,
        Value = (double)input.Parameters["value"]!,
        SecondValue = input.Parameters.GetValueOrDefault("secondValue") as double?,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var set = (SetSketchConstraintValueCommand)command;

        // Finding E95 of the codebase review of 2026-10-30 (TASK-0114): a command sent again after
        // the cache forgot it reused the key of a rebuild and gave a false fault. Each sketch
        // command refuses an identifier that the log holds, as E86 gave the commands that add.
        if (SketchResults.InUse(document, command, createsItem: false) is { } inUse)
            return Task.FromResult(inUse);

        if (SketchModel.Find(document, set.SketchId) is not { } sketch)
            return Task.FromResult(SketchResults.NotFound(set.SketchId));

        if (sketch.Constraints.FirstOrDefault(candidate => candidate.Id == set.ConstraintId) is not { } constraint)
            return Task.FromResult(SketchResults.ItemNotFound(set.SketchId, set.ConstraintId, "constraint"));

        double[] values = [set.Value, .. set.SecondValue is { } second ? [second] : Array.Empty<double>()];
        if (SketchConstraints.Refusal(sketch, constraint.Kind, constraint.Items, values) is { } refusal)
            return Task.FromResult(SketchResults.Failure(DiagnosticCodes.SketchConstraintInvalid, refusal));

        var changed = constraint with { Values = SketchConstraints.Recorded(constraint.Kind, values) };
        var constraints = sketch.Constraints.Select(candidate => candidate.Id == changed.Id ? changed : candidate).ToList();
        return Task.FromResult(SketchResults.Changed(document, backend, command.CommandId, sketch with { Constraints = constraints }, "constraintId", changed.Id));
    }
}
