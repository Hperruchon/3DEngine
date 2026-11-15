using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0153 item 6 (phase P10.1, TASK-0389): a new record of the sheet with the new name.
public sealed class RenameSheetCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "RenameSheet";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sheetId"] = new("guid", Required: true),
            ["name"] = new("string", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sheetId"] = new("guid"),
        };

    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (RenameSheetCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sheetId"] = typed.SheetId,
            ["name"] = typed.SheetName,
        };
    }

    public Command Create(CommandInput input) => new RenameSheetCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SheetId = (Guid)input.Parameters["sheetId"]!,
        SheetName = (string)input.Parameters["name"]!,
    };

    public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var rename = (RenameSheetCommand)command;
        if (SheetResults.Find(document, rename.SheetId) is not { } sheet)
            return Task.FromResult(SheetResults.NotFound(rename.SheetId));
        if (SheetResults.NameRefused(rename.SheetName) is { } refused)
            return Task.FromResult(refused);

        var outputs = new Outputs(new Dictionary<string, object?> { ["sheetId"] = sheet.Id });
        return Task.FromResult(CommandHandlerResult.Success(outputs, changedSheets: [sheet with { Name = rename.SheetName }]));
    }
}
