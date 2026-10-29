using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Contracts.Sketch;
using Engine.Core.Persistence;
using Engine.Core.Sketch;

namespace Engine.Core.Commands;

// ADR-0033 item 6: a point goes with each line that references it, and a removed item
// takes each constraint that references it. The origin of the frame stays.
public sealed class DeleteSketchItemCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "DeleteSketchItem";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sketchId"] = new("guid", Required: true),
            ["itemId"] = new("guid", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["itemId"] = new("guid"),
        };

    // ADR-0015 item 11: the parameters that Create reads.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (DeleteSketchItemCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sketchId"] = typed.SketchId,
            ["itemId"] = typed.ItemId,
        };
    }

    // Per ADR-0016.
    public Command Create(CommandInput input) => new DeleteSketchItemCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SketchId = (Guid)input.Parameters["sketchId"]!,
        ItemId = (Guid)input.Parameters["itemId"]!,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var delete = (DeleteSketchItemCommand)command;

        if (SketchModel.Find(document, delete.SketchId) is not { } sketch)
            return Task.FromResult(SketchResults.NotFound(delete.SketchId));

        if (SketchModel.KindOf(sketch, delete.ItemId) is not (SketchModel.ItemKind.Point or SketchModel.ItemKind.Line or SketchModel.ItemKind.Constraint)
            || SketchResults.IsOrigin(sketch, delete.ItemId))
            return Task.FromResult(SketchResults.ItemNotFound(delete.SketchId, delete.ItemId, "item that a command can delete"));

        return Task.FromResult(SketchResults.Changed(document, backend, command.CommandId, SketchModel.Without(sketch, delete.ItemId), "itemId", delete.ItemId));
    }
}
