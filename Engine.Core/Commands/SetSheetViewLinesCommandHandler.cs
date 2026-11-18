using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0165 item 2 (phase R30g, TASK-0405): a new record of the sheet with the two flags of the view, and the views in the same
// order. The command refuses a sheet that is not live and a view that the sheet does not have, as MoveSheetView does.
public sealed class SetSheetViewLinesCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "SetSheetViewLines";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sheetId"] = new("guid", Required: true),
            ["viewId"] = new("guid", Required: true),
            ["hiddenLines"] = new("boolean", Required: true),
            ["centreLines"] = new("boolean", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["viewId"] = new("guid"),
        };

    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (SetSheetViewLinesCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sheetId"] = typed.SheetId,
            ["viewId"] = typed.ViewId,
            ["hiddenLines"] = typed.HiddenLines,
            ["centreLines"] = typed.CentreLines,
        };
    }

    public Command Create(CommandInput input) => new SetSheetViewLinesCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SheetId = (Guid)input.Parameters["sheetId"]!,
        ViewId = (Guid)input.Parameters["viewId"]!,
        HiddenLines = (bool)input.Parameters["hiddenLines"]!,
        CentreLines = (bool)input.Parameters["centreLines"]!,
    };

    public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var set = (SetSheetViewLinesCommand)command;
        if (SheetResults.Find(document, set.SheetId) is not { } sheet)
            return Task.FromResult(SheetResults.NotFound(set.SheetId));
        var index = sheet.Views.ToList().FindIndex(view => view.Id == set.ViewId);
        if (index < 0)
            return Task.FromResult(SheetResults.ViewNotFound(sheet.Id, set.ViewId));

        var views = sheet.Views.ToList();
        views[index] = views[index] with { HiddenLines = set.HiddenLines, CentreLines = set.CentreLines };
        var outputs = new Outputs(new Dictionary<string, object?> { ["viewId"] = set.ViewId });
        return Task.FromResult(CommandHandlerResult.Success(outputs, changedSheets: [sheet with { Views = views }]));
    }
}
