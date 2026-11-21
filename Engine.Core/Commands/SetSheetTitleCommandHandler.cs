using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0179 item 2 (phase R30i, TASK-0428). The handler needs no capability of the backend. It gives a new record of the sheet
// with the whole block: each field of the command, and an empty text for each field that the command does not give.
public sealed class SetSheetTitleCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "SetSheetTitle";
    public int SchemaVersion => 1;

    // ADR-0179 item 2: the names of the eleven fields, in the order of SheetTitleBlock.
    public static readonly IReadOnlyList<string> Fields =
    [
        "title", "identificationNumber", "revision", "issueDate", "sheetNumber", "legalOwner", "material", "creator",
        "generalTolerances", "approvalPerson", "documentType",
    ];

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } = Schema();

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sheetId"] = new("guid"),
        };

    private static Dictionary<string, FieldSchema> Schema()
    {
        var parameters = new Dictionary<string, FieldSchema> { ["sheetId"] = new("guid", Required: true) };
        foreach (var field in Fields)
            parameters[field] = new("string", Required: false);
        return parameters;
    }

    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (SetSheetTitleCommand)command;
        var values = Values(typed);
        var encoded = new Dictionary<string, object?>(StringComparer.Ordinal) { ["sheetId"] = typed.SheetId };
        for (var i = 0; i < Fields.Count; i++)
            encoded[Fields[i]] = values[i];
        return encoded;
    }

    public Command Create(CommandInput input)
    {
        string Text(string field) => input.Parameters.GetValueOrDefault(field) as string ?? "";
        return new SetSheetTitleCommand
        {
            CommandId = input.CommandId,
            ExpectedDocumentVersion = input.ExpectedDocumentVersion,
            SheetId = (Guid)input.Parameters["sheetId"]!,
            Title = Text("title"),
            IdentificationNumber = Text("identificationNumber"),
            Revision = Text("revision"),
            IssueDate = Text("issueDate"),
            SheetNumber = Text("sheetNumber"),
            LegalOwner = Text("legalOwner"),
            Material = Text("material"),
            Creator = Text("creator"),
            GeneralTolerances = Text("generalTolerances"),
            ApprovalPerson = Text("approvalPerson"),
            DocumentType = Text("documentType"),
        };
    }

    // ADR-0179 item 2: the sheet is live; then each field follows the rules of the name of a body (ADR-0028 item 3): at most 128
    // UTF-16 units, no control character, and well-formed UTF-16. An empty field is valid.
    public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
    {
        var set = (SetSheetTitleCommand)command;
        if (SheetResults.Find(document, set.SheetId) is not { } sheet)
            return Task.FromResult(SheetResults.NotFound(set.SheetId));

        var values = Values(set);
        for (var i = 0; i < Fields.Count; i++)
        {
            if (BodyNames.Refusal(values[i]) is { } refusal)
            {
                return Task.FromResult(SheetResults.Failure(DiagnosticCodes.SheetTitleInvalid,
                    $"The field '{Fields[i]}' of the title block follows the rules of a name (ADR-0179 item 2). {refusal}"));
            }
        }

        var block = new SheetTitleBlock(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7], values[8], values[9], values[10]);
        var outputs = new Outputs(new Dictionary<string, object?> { ["sheetId"] = sheet.Id });
        return Task.FromResult(CommandHandlerResult.Success(outputs, changedSheets: [sheet with { TitleBlock = block }]));
    }

    // The values of the fields in the order of Fields; a null is empty.
    private static string[] Values(SetSheetTitleCommand set) =>
    [
        set.Title ?? "", set.IdentificationNumber ?? "", set.Revision ?? "", set.IssueDate ?? "", set.SheetNumber ?? "",
        set.LegalOwner ?? "", set.Material ?? "", set.Creator ?? "", set.GeneralTolerances ?? "", set.ApprovalPerson ?? "",
        set.DocumentType ?? "",
    ];
}
