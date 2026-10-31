using Engine.Contracts;
using Engine.Contracts.Handlers;
using Engine.Contracts.Images;
using Engine.Core.Sketch;

namespace Engine.Core.Commands;

// The refusals that the two image commands share (ADR-0045 item 6).
internal static class ImageResults
{
    public static CommandHandlerResult Failure(string code, string message)
        => CommandHandlerResult.Failure(new ErrorDetail(code, message));

    // As SketchResults.InUse does for an item of a sketch (findings E86 and E95): the image
    // takes the identifier of its command, so a command that the log holds is refused. A
    // command that changes an image is refused only outside an open or a replay.
    public static CommandHandlerResult? InUse(Document document, Command command, bool createsImage)
        => (createsImage || !SketchResults.ApplyingEntry.Value) && document.Log.Any(entry => entry.Command.CommandId == command.CommandId)
            ? Failure(DiagnosticCodes.ImageIdInUse, $"The log holds a command with the id '{command.CommandId}', and an image takes the id of its command.")
            : null;

    public static ImageRecord? Find(Document document, Guid id)
        => document.Images.FirstOrDefault(image => image.Id == id);

    public static CommandHandlerResult NotFound(Guid id)
        => Failure(DiagnosticCodes.ImageNotFound, $"No live image with id '{id}' exists in the document.");

    public static CommandHandlerResult ScaleInvalid(double scale)
        => Failure(DiagnosticCodes.ImageScaleInvalid, $"The scale of an image is a finite number of millimetres for each pixel, greater than 0, and the command gives {scale}.");
}
