using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0155 item 3 (phase P10.2, TASK-0393, Q6 A): a new record of the sheet with no such view and no dimension of that view,
// so one undo gives both back.
public sealed class RemoveSheetViewCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "RemoveSheetView";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sheetId"] = new("guid", Required: true),
            ["viewId"] = new("guid", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["viewId"] = new("guid"),
        };

    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (RemoveSheetViewCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sheetId"] = typed.SheetId,
            ["viewId"] = typed.ViewId,
        };
    }

    public Command Create(CommandInput input) => new RemoveSheetViewCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SheetId = (Guid)input.Parameters["sheetId"]!,
        ViewId = (Guid)input.Parameters["viewId"]!,
    };

    public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var remove = (RemoveSheetViewCommand)command;
        if (SheetResults.Find(document, remove.SheetId) is not { } sheet)
            return Task.FromResult(SheetResults.NotFound(remove.SheetId));
        if (!sheet.Views.Any(view => view.Id == remove.ViewId))
            return Task.FromResult(SheetResults.ViewNotFound(sheet.Id, remove.ViewId));

        var changed = sheet with
        {
            Views = [.. sheet.Views.Where(view => view.Id != remove.ViewId)],
            Dimensions = [.. sheet.Dimensions.Where(dimension => dimension.ViewId != remove.ViewId)],
        };
        var outputs = new Outputs(new Dictionary<string, object?> { ["viewId"] = remove.ViewId });
        return Task.FromResult(CommandHandlerResult.Success(outputs, changedSheets: [changed]));
    }
}
