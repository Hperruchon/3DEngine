using Engine.Contracts;
using Engine.Contracts.Handlers;
using Engine.Contracts.Sketch;

namespace Engine.Core.Sketch;

// The results that the sketch commands share (ADR-0033 items 6 and 8).
public static class SketchResults
{
    public static CommandHandlerResult NotFound(Guid sketchId) => Failure(
        DiagnosticCodes.SketchNotFound, $"No live sketch with id '{sketchId}' exists in the Document.");

    public static CommandHandlerResult ItemNotFound(Guid sketchId, Guid itemId, string what) => Failure(
        DiagnosticCodes.SketchItemNotFound, $"The sketch '{sketchId}' has no {what} with id '{itemId}'.");

    public static CommandHandlerResult Failure(string code, string message)
        => CommandHandlerResult.Failure(new ErrorDetail(code, message));

    // A new record of a live sketch, and the identifier of the item that the command
    // added or changed as its output.
    public static CommandHandlerResult Changed(SketchRecord sketch, string output, Guid id)
        => CommandHandlerResult.Success(
            new Outputs(new Dictionary<string, object?> { [output] = id }),
            changedSketches: [sketch]);

    // ADR-0033 item 2: the origin of the frame has the identifier of its sketch, and no
    // command moves or deletes it.
    public static bool IsOrigin(SketchRecord sketch, Guid id) => sketch.Id == id;

    // A finite number, for a coordinate or a value.
    public static bool AreFinite(params double[] values) => values.All(double.IsFinite);
}
