using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Contracts.Handlers;
using Engine.Core.Drawing;

namespace Engine.Core.Commands;

// ADR-0153 item 6 (phase P10.1, TASK-0389): the shared results of the commands of a sheet.
internal static class SheetResults
{
    public static CommandHandlerResult Failure(string code, string message)
        => CommandHandlerResult.Failure(new ErrorDetail(code, message));

    // As ImageResults.InUse: a sheet, and since TASK-0393 a view, takes the identifier of its command, so a command that the
    // log holds is refused.
    public static CommandHandlerResult? InUse(Document document, Command command, string item = "a sheet")
        => Features.FeatureList.Of(document).Holds(command.CommandId)
            ? Failure(DiagnosticCodes.SheetIdInUse, $"The log holds a command with the id '{command.CommandId}', and {item} takes the id of its command.")
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

    // ADR-0155 item 6 (phase P10.2, TASK-0393, Q5 A): a view that is not on the sheet.
    public static CommandHandlerResult ViewNotFound(Guid sheet, Guid view)
        => Failure(DiagnosticCodes.SheetViewNotFound, $"The sheet '{sheet}' has no view with id '{view}'.");

    // ADR-0155 item 6: a direction, a scale or a place of a view that is not valid.
    public static CommandHandlerResult ViewInvalid(string message) => Failure(DiagnosticCodes.SheetViewInvalid, message);

    // ADR-0155 item 4 (Q4 A): the centre of a view is on the paper, with its orientation. A value that is not finite fails
    // each comparison, so it is refused too.
    public static CommandHandlerResult? PlaceRefused(SheetRecord sheet, double x, double y)
    {
        var (width, height) = SheetLayout.PaperSize(sheet);
        return x >= 0 && x <= width && y >= 0 && y <= height
            ? null
            : ViewInvalid($"The centre of a view is on the paper, from 0 to {width} mm across and from 0 to {height} mm up, and the command gives ({x}, {y}) (ADR-0155 item 4).");
    }
}
