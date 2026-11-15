using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0155 item 2 (phase P10.2, TASK-0393): a new record of the sheet with the view at its new place, and the views in the
// same order.
public sealed class MoveSheetViewCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "MoveSheetView";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sheetId"] = new("guid", Required: true),
            ["viewId"] = new("guid", Required: true),
            ["x"] = new("number", Required: true, Unit: UnitKinds.Length),
            ["y"] = new("number", Required: true, Unit: UnitKinds.Length),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["viewId"] = new("guid"),
        };

    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (MoveSheetViewCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sheetId"] = typed.SheetId,
            ["viewId"] = typed.ViewId,
            ["x"] = typed.X,
            ["y"] = typed.Y,
        };
    }

    public Command Create(CommandInput input) => new MoveSheetViewCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SheetId = (Guid)input.Parameters["sheetId"]!,
        ViewId = (Guid)input.Parameters["viewId"]!,
        X = (double)input.Parameters["x"]!,
        Y = (double)input.Parameters["y"]!,
    };

    public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var move = (MoveSheetViewCommand)command;
        if (SheetResults.Find(document, move.SheetId) is not { } sheet)
            return Task.FromResult(SheetResults.NotFound(move.SheetId));
        var index = sheet.Views.ToList().FindIndex(view => view.Id == move.ViewId);
        if (index < 0)
            return Task.FromResult(SheetResults.ViewNotFound(sheet.Id, move.ViewId));
        if (SheetResults.PlaceRefused(sheet, move.X, move.Y) is { } place)
            return Task.FromResult(place);

        var views = sheet.Views.ToList();
        views[index] = views[index] with { X = move.X, Y = move.Y };
        var outputs = new Outputs(new Dictionary<string, object?> { ["viewId"] = move.ViewId });
        return Task.FromResult(CommandHandlerResult.Success(outputs, changedSheets: [sheet with { Views = views }]));
    }
}
