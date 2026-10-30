using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Contracts.Sketch;
using Engine.Core.Persistence;
using Engine.Core.Sketch;

namespace Engine.Core.Commands;

// ADR-0033 items 3, 5 and 6: a constraint of the kinds 1 to 5 on items of its sketch.
// The engine applies a constraint that the positions do not satisfy; GetSketch gives its
// residual, and the solver of K3a moves the points (item 7).
public sealed class AddSketchConstraintCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "AddSketchConstraint";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sketchId"] = new("guid", Required: true),
            ["kind"] = new("string", Required: true),
            ["first"] = new("guid", Required: true),
            ["second"] = new("guid", Required: false),
            ["third"] = new("guid", Required: false),
            ["value"] = new("number", Required: false),
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
        var typed = (AddSketchConstraintCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sketchId"] = typed.SketchId,
            ["kind"] = typed.Kind,
            ["first"] = typed.First,
            ["second"] = typed.Second,
            ["third"] = typed.Third,
            ["value"] = typed.Value,
            ["secondValue"] = typed.SecondValue,
        };
    }

    // Per ADR-0016.
    public Command Create(CommandInput input) => new AddSketchConstraintCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SketchId = (Guid)input.Parameters["sketchId"]!,
        Kind = (string)input.Parameters["kind"]!,
        First = (Guid)input.Parameters["first"]!,
        Second = input.Parameters.GetValueOrDefault("second") as Guid?,
        Third = input.Parameters.GetValueOrDefault("third") as Guid?,
        Value = input.Parameters.GetValueOrDefault("value") as double?,
        SecondValue = input.Parameters.GetValueOrDefault("secondValue") as double?,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var add = (AddSketchConstraintCommand)command;

        // Finding E86 of the codebase review of 2026-10-29: the item takes the identifier of the
        // command, so a command that the log holds is refused before it gives a second item.
        if (SketchResults.InUse(document, command) is { } inUse)
            return Task.FromResult(inUse);

        if (SketchModel.Find(document, add.SketchId) is not { } sketch)
            return Task.FromResult(SketchResults.NotFound(add.SketchId));

        if (add.Third is not null && add.Second is null || add.SecondValue is not null && add.Value is null)
            return Task.FromResult(SketchResults.Failure(
                DiagnosticCodes.SketchConstraintInvalid, "A constraint gives its items and its values in order: 'third' needs 'second', and 'secondValue' needs 'value'."));

        Guid[] items = [add.First, .. add.Second is { } second ? [second] : Array.Empty<Guid>(), .. add.Third is { } third ? [third] : Array.Empty<Guid>()];
        double[] values = [.. add.Value is { } value ? [value] : Array.Empty<double>(), .. add.SecondValue is { } secondValue ? [secondValue] : Array.Empty<double>()];

        foreach (var item in items)
        {
            if (SketchModel.KindOf(sketch, item) == SketchModel.ItemKind.None)
                return Task.FromResult(SketchResults.ItemNotFound(add.SketchId, item, "item"));
        }

        if (SketchConstraints.Refusal(sketch, add.Kind, items, values) is { } refusal)
            return Task.FromResult(SketchResults.Failure(DiagnosticCodes.SketchConstraintInvalid, refusal));

        var constraint = new SketchConstraint(add.CommandId, add.Kind, items, SketchConstraints.Recorded(add.Kind, values));
        return Task.FromResult(SketchResults.Changed(document, backend, command.CommandId, sketch with { Constraints = [.. sketch.Constraints, constraint] }, "constraintId", constraint.Id));
    }
}
