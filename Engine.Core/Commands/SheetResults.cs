using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Contracts.Handlers;

namespace Engine.Core.Commands;

// ADR-0153 item 6 (phase P10.1, TASK-0389): the shared results of the commands of a sheet.
internal static class SheetResults
{
    public static CommandHandlerResult Failure(string code, string message)
        => CommandHandlerResult.Failure(new ErrorDetail(code, message));

    // As ImageResults.InUse: a sheet takes the identifier of its command, so a command that the log holds is refused.
    public static CommandHandlerResult? InUse(Document document, Command command)
        => Features.FeatureList.Of(document).Holds(command.CommandId)
            ? Failure(DiagnosticCodes.SheetIdInUse, $"The log holds a command with the id '{command.CommandId}', and a sheet takes the id of its command.")
            : null;

    public static SheetRecord? Find(Document document, Guid id)
        => document.Sheets.FirstOrDefault(sheet => sheet.Id == id);

    public static CommandHandlerResult NotFound(Guid id)
        => Failure(DiagnosticCodes.SheetNotFound, $"No live sheet with id '{id}' exists in the document.");

    // A name of a sheet is not empty, and follows the rule of the name of a body.
    public static CommandHandlerResult? NameRefused(string name)
        => string.IsNullOrWhiteSpace(name)
            ? Failure(DiagnosticCodes.SheetInvalid, "A sheet has a name that is not empty.")
            : BodyNames.Refusal(name) is { } refusal ? Failure(DiagnosticCodes.SheetInvalid, refusal) : null;
}
