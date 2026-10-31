using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Contracts.Sketch;
using Engine.Core.Persistence;
using Engine.Core.Sketch;

namespace Engine.Core.Commands;

// ADR-0033 items 2, 5 and 6 (phase K2d): a circle references a point of its sketch as its centre
// and has a radius that is a positive finite number.
public sealed class AddSketchCircleCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "AddSketchCircle";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sketchId"] = new("guid", Required: true),
            ["centerPointId"] = new("guid", Required: true),
            ["radius"] = new("number", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["circleId"] = new("guid"),
        };

    // ADR-0015 item 11: the parameters that Create reads.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (AddSketchCircleCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sketchId"] = typed.SketchId,
            ["centerPointId"] = typed.CenterPointId,
            ["radius"] = typed.Radius,
        };
    }

    // Per ADR-0016.
    public Command Create(CommandInput input) => new AddSketchCircleCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SketchId = (Guid)input.Parameters["sketchId"]!,
        CenterPointId = (Guid)input.Parameters["centerPointId"]!,
        Radius = (double)input.Parameters["radius"]!,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var add = (AddSketchCircleCommand)command;

        // Finding E86 of the codebase review of 2026-10-29: the item takes the identifier of the
        // command, so a command that the log holds is refused before it gives a second item.
        if (SketchResults.InUse(document, command, createsItem: true) is { } inUse)
            return Task.FromResult(inUse);

        if (SketchModel.Find(document, add.SketchId) is not { } sketch)
            return Task.FromResult(SketchResults.NotFound(add.SketchId));

        if (SketchModel.KindOf(sketch, add.CenterPointId) != SketchModel.ItemKind.Point)
            return Task.FromResult(SketchResults.ItemNotFound(add.SketchId, add.CenterPointId, "point"));

        if (!(double.IsFinite(add.Radius) && add.Radius > 0))
            return Task.FromResult(SketchResults.Failure(
                DiagnosticCodes.SketchCurveInvalid, $"The radius of a circle is a positive finite number, and the command gives {add.Radius}."));

        var circle = new SketchCircle(add.CommandId, add.CenterPointId, add.Radius);
        return Task.FromResult(SketchResults.Changed(document, backend, command.CommandId, sketch with { Circles = [.. sketch.Circles, circle] }, "circleId", circle.Id));
    }
}
