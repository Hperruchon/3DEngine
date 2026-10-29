using Engine.Contracts;
using Engine.Contracts.Geometry;
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
    // added or changed as its output. The record goes through the solver, so the Document
    // holds the solved positions; a held point stays where the command put it when a
    // solution exists (ADR-0035 items 2 and 7). Each extrude of the sketch then follows
    // the solved record (ADR-0037 item 5), so the command also gives each body that the
    // rebuild changes and the warnings of the rebuild.
    public static CommandHandlerResult Changed(
        Document document, IGeometryBackend backend, Guid edit, SketchRecord sketch, string output, Guid id, Guid? held = null)
    {
        var solved = SketchSolver.Solve(sketch, held);
        var before = SketchModel.Find(document, solved.Id)!;
        var (bodies, diagnostics) = SketchRebuild.Rebuild(document, backend, edit, before, solved);
        return CommandHandlerResult.Success(
            new Outputs(new Dictionary<string, object?> { [output] = id }),
            diagnostics: diagnostics,
            changedBodies: bodies,
            changedSketches: [solved]);
    }

    // ADR-0033 item 2: the origin of the frame has the identifier of its sketch, and no
    // command moves or deletes it.
    public static bool IsOrigin(SketchRecord sketch, Guid id) => sketch.Id == id;

    // A finite number, for a coordinate or a value.
    public static bool AreFinite(params double[] values) => values.All(double.IsFinite);
}
