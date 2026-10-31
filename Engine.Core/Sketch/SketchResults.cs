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
    // holds the solved positions; a held point stays where the command put it when the solve
    // with the hold converges, and the result gives W-SKETCH-HOLD-DROPPED when the solver
    // moved it (ADR-0035 items 2 and 7, finding E87). Each extrude of the sketch then follows
    // the solved record (ADR-0037 item 5), so the command also gives each body that the
    // rebuild changes and the warnings of the rebuild.
    public static CommandHandlerResult Changed(
        Document document, IGeometryBackend backend, Guid edit, SketchRecord sketch, string output, Guid id, Guid? held = null)
    {
        var (solved, dropped) = SketchSolver.SolveWithHold(ReadOnly(sketch), held);
        var before = SketchModel.Find(document, solved.Id)!;
        var (bodies, rebuild) = SketchRebuild.Rebuild(document, backend, edit, before, solved);
        var diagnostics = dropped
            ? [new Diagnostic(
                Severity.Warning,
                DiagnosticCodes.SketchHoldDropped,
                $"The solver could not keep the point '{held}' at the position of the command, and moved it to satisfy the constraints.",
                Data: new Dictionary<string, object?> { ["pointId"] = held }), .. rebuild]
            : rebuild;
        return CommandHandlerResult.Success(
            new Outputs(new Dictionary<string, object?> { [output] = id }),
            diagnostics: diagnostics,
            changedBodies: bodies,
            changedSketches: [solved]);
    }

    // Finding E94 of the codebase review of 2026-10-30 (TASK-0114): a move or a change of a value
    // built a List of the handler, and a solve that ends at its first step returns that record, so
    // the Document held a list that a client could change through a cast. Each list of a record
    // that a command gives is read-only.
    private static SketchRecord ReadOnly(SketchRecord sketch) => sketch with
    {
        Points = sketch.Points.ToList().AsReadOnly(),
        Lines = sketch.Lines.ToList().AsReadOnly(),
        Circles = sketch.Circles.ToList().AsReadOnly(),
        Arcs = sketch.Arcs.ToList().AsReadOnly(),
        Constraints = sketch.Constraints.ToList().AsReadOnly(),
    };

    // Finding E86 of the codebase review of 2026-10-29: the refusal of a command whose
    // identifier the log holds. A client that sends a command again after the cache of the
    // bus forgot it (E17) got a second item with the same identifier, or an exception.
    //
    // Finding E100 of the codebase review of 2026-10-30 (b) (TASK-0123, answer Q1 A of the owner):
    // the refusal acts on a live command only. An open of a file and a replay of a log apply each
    // entry as the session that wrote it did (ADR-0015 item 8), and up to v0.117 a move that a
    // client sent again entered the log two times, so the open of that file failed. The bus sets
    // ApplyingEntry while a handler applies an entry of a log.
    //
    // Finding E104 of the codebase review of 2026-10-31 (TASK-0132): a command that creates an item
    // takes the identifier of its command for the item, so an entry of it that the log holds two times
    // gave two items with one identifier, or an exception of the solver. For such a command the
    // refusal also acts on an entry, and the open names the line; createsItem is false for a move, a
    // change of a value and a delete.
    internal static readonly AsyncLocal<bool> ApplyingEntry = new();

    public static CommandHandlerResult? InUse(Document document, Command command, bool createsItem)
        => (createsItem || !ApplyingEntry.Value) && document.Log.Any(entry => entry.Command.CommandId == command.CommandId)
            ? Failure(DiagnosticCodes.SketchIdInUse, $"The log holds a command with the id '{command.CommandId}', and a sketch item takes the id of its command.")
            : null;

    // ADR-0033 item 2: the origin of the frame has the identifier of its sketch, and no
    // command moves or deletes it.
    public static bool IsOrigin(SketchRecord sketch, Guid id) => sketch.Id == id;

    // A finite number, for a coordinate or a value.
    public static bool AreFinite(params double[] values) => values.All(double.IsFinite);
}
