using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0158 item 3 (phase P10.3, TASK-0395): a new record of the sheet with no such dimension, and the other dimensions in the
// same order. The command does not read the source, so a lost dimension can be removed.
public sealed class RemoveSheetDimensionCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "RemoveSheetDimension";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sheetId"] = new("guid", Required: true),
            ["dimensionId"] = new("guid", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["dimensionId"] = new("guid"),
        };

    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (RemoveSheetDimensionCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sheetId"] = typed.SheetId,
            ["dimensionId"] = typed.DimensionId,
        };
    }

    public Command Create(CommandInput input) => new RemoveSheetDimensionCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SheetId = (Guid)input.Parameters["sheetId"]!,
        DimensionId = (Guid)input.Parameters["dimensionId"]!,
    };

    public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var remove = (RemoveSheetDimensionCommand)command;
        if (SheetResults.Find(document, remove.SheetId) is not { } sheet)
            return Task.FromResult(SheetResults.NotFound(remove.SheetId));
        if (!sheet.Dimensions.Any(dimension => dimension.Id == remove.DimensionId))
            return Task.FromResult(SheetResults.DimensionNotFound(sheet.Id, remove.DimensionId));

        var changed = sheet with { Dimensions = [.. sheet.Dimensions.Where(dimension => dimension.Id != remove.DimensionId)] };
        var outputs = new Outputs(new Dictionary<string, object?> { ["dimensionId"] = remove.DimensionId });
        return Task.FromResult(CommandHandlerResult.Success(outputs, changedSheets: [changed]));
    }
}
