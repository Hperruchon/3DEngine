using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0153 items 1 to 3 and 6 (phase P10.1, TASK-0389). The handler needs no capability of the backend.
public sealed class CreateSheetCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "CreateSheet";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["name"] = new("string", Required: true),
            ["paper"] = new("string", Required: false),
            ["landscape"] = new("boolean", Required: false),
            ["thirdAngle"] = new("boolean", Required: false),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sheetId"] = new("guid"),
        };

    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (CreateSheetCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = typed.SheetName,
            ["paper"] = typed.Paper,
            ["landscape"] = typed.Landscape,
            ["thirdAngle"] = typed.ThirdAngle,
        };
    }

    public Command Create(CommandInput input) => new CreateSheetCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SheetName = (string)input.Parameters["name"]!,
        Paper = input.Parameters.GetValueOrDefault("paper") as string ?? "A4",
        Landscape = input.Parameters.GetValueOrDefault("landscape") as bool? ?? true,
        ThirdAngle = input.Parameters.GetValueOrDefault("thirdAngle") as bool? ?? false,
    };

    public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var create = (CreateSheetCommand)command;
        if (SheetResults.InUse(document, command) is { } inUse)
            return Task.FromResult(inUse);
        if (SheetResults.NameRefused(create.SheetName) is { } refused)
            return Task.FromResult(refused);
        if (!SheetRecord.Papers.Contains(create.Paper, StringComparer.Ordinal))
            return Task.FromResult(SheetResults.Failure(DiagnosticCodes.SheetInvalid,
                $"The paper of a sheet is one of {string.Join(", ", SheetRecord.Papers)}, and the command gives '{create.Paper}' (ADR-0153 item 2)."));

        var sheet = new SheetRecord(create.CommandId, create.SheetName, create.Paper, create.Landscape, create.ThirdAngle, [], []);
        var outputs = new Outputs(new Dictionary<string, object?> { ["sheetId"] = sheet.Id });
        return Task.FromResult(CommandHandlerResult.Success(outputs, createdSheets: [sheet]));
    }
}
