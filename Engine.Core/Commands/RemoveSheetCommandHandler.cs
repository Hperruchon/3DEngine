using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0153 item 6 (phase P10.1, TASK-0389): the removal of a live sheet.
public sealed class RemoveSheetCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "RemoveSheet";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sheetId"] = new("guid", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sheetId"] = new("guid"),
        };

    public IReadOnlyDictionary<string, object?> Encode(Command command)
        => new Dictionary<string, object?>(StringComparer.Ordinal) { ["sheetId"] = ((RemoveSheetCommand)command).SheetId };

    public Command Create(CommandInput input) => new RemoveSheetCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SheetId = (Guid)input.Parameters["sheetId"]!,
    };

    public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var remove = (RemoveSheetCommand)command;
        if (SheetResults.Find(document, remove.SheetId) is not { } sheet)
            return Task.FromResult(SheetResults.NotFound(remove.SheetId));

        var outputs = new Outputs(new Dictionary<string, object?> { ["sheetId"] = sheet.Id });
        return Task.FromResult(CommandHandlerResult.Success(outputs, removedSheets: [sheet.Id]));
    }
}
