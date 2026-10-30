using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Contracts.Sketch;
using Engine.Core.Persistence;
using Engine.Core.Sketch;

namespace Engine.Core.Commands;

// ADR-0033 items 2, 5 and 6: a point of a live sketch moves to a finite position. The
// origin of the frame does not move. The solver holds the moved point (ADR-0035 item 7).
public sealed class MoveSketchPointCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "MoveSketchPoint";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sketchId"] = new("guid", Required: true),
            ["pointId"] = new("guid", Required: true),
            ["x"] = new("number", Required: true),
            ["y"] = new("number", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["pointId"] = new("guid"),
        };

    // ADR-0015 item 11: the parameters that Create reads.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (MoveSketchPointCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sketchId"] = typed.SketchId,
            ["pointId"] = typed.PointId,
            ["x"] = typed.X,
            ["y"] = typed.Y,
        };
    }

    // Per ADR-0016.
    public Command Create(CommandInput input) => new MoveSketchPointCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SketchId = (Guid)input.Parameters["sketchId"]!,
        PointId = (Guid)input.Parameters["pointId"]!,
        X = (double)input.Parameters["x"]!,
        Y = (double)input.Parameters["y"]!,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var move = (MoveSketchPointCommand)command;

        // Finding E95 of the codebase review of 2026-10-30 (TASK-0114): a command sent again after
        // the cache forgot it reused the key of a rebuild and gave a false fault. Each sketch
        // command refuses an identifier that the log holds, as E86 gave the commands that add.
        if (SketchResults.InUse(document, command) is { } inUse)
            return Task.FromResult(inUse);

        if (SketchModel.Find(document, move.SketchId) is not { } sketch)
            return Task.FromResult(SketchResults.NotFound(move.SketchId));

        if (SketchModel.KindOf(sketch, move.PointId) != SketchModel.ItemKind.Point || SketchResults.IsOrigin(sketch, move.PointId))
            return Task.FromResult(SketchResults.ItemNotFound(move.SketchId, move.PointId, "point that a command can move"));

        if (!SketchResults.AreFinite(move.X, move.Y))
            return Task.FromResult(SketchResults.Failure(
                DiagnosticCodes.GeomInvalidParam, "The position of a point of a sketch is a pair of finite numbers."));

        var points = sketch.Points.Select(point => point.Id == move.PointId ? point with { X = move.X, Y = move.Y } : point).ToList();
        return Task.FromResult(SketchResults.Changed(document, backend, command.CommandId, sketch with { Points = points }, "pointId", move.PointId, held: move.PointId));
    }
}
