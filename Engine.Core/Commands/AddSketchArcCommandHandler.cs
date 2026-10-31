using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Contracts.Sketch;
using Engine.Core.Persistence;
using Engine.Core.Sketch;

namespace Engine.Core.Commands;

// ADR-0033 items 2, 5 and 6 (phase K2d): an arc references three different points of its
// sketch: a centre, a start and an end, counter-clockwise from the start. The solver makes the
// two radii equal.
public sealed class AddSketchArcCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "AddSketchArc";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sketchId"] = new("guid", Required: true),
            ["centerPointId"] = new("guid", Required: true),
            ["startPointId"] = new("guid", Required: true),
            ["endPointId"] = new("guid", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["arcId"] = new("guid"),
        };

    // ADR-0015 item 11: the parameters that Create reads.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (AddSketchArcCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sketchId"] = typed.SketchId,
            ["centerPointId"] = typed.CenterPointId,
            ["startPointId"] = typed.StartPointId,
            ["endPointId"] = typed.EndPointId,
        };
    }

    // Per ADR-0016.
    public Command Create(CommandInput input) => new AddSketchArcCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SketchId = (Guid)input.Parameters["sketchId"]!,
        CenterPointId = (Guid)input.Parameters["centerPointId"]!,
        StartPointId = (Guid)input.Parameters["startPointId"]!,
        EndPointId = (Guid)input.Parameters["endPointId"]!,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var add = (AddSketchArcCommand)command;

        // Finding E86 of the codebase review of 2026-10-29: the item takes the identifier of the
        // command, so a command that the log holds is refused before it gives a second item.
        if (SketchResults.InUse(document, command, createsItem: true) is { } inUse)
            return Task.FromResult(inUse);

        if (SketchModel.Find(document, add.SketchId) is not { } sketch)
            return Task.FromResult(SketchResults.NotFound(add.SketchId));

        foreach (var id in new[] { add.CenterPointId, add.StartPointId, add.EndPointId })
        {
            if (SketchModel.KindOf(sketch, id) != SketchModel.ItemKind.Point)
                return Task.FromResult(SketchResults.ItemNotFound(add.SketchId, id, "point"));
        }

        if (add.CenterPointId == add.StartPointId || add.CenterPointId == add.EndPointId || add.StartPointId == add.EndPointId)
            return Task.FromResult(SketchResults.Failure(
                DiagnosticCodes.SketchCurveInvalid, "An arc references three different points: a centre, a start and an end."));

        // Finding E98 of the codebase review of 2026-10-30 (b) (TASK-0123): a start or an end at the
        // position of the centre gives an arc of radius 0, as a circle of radius 0, which the add of
        // a circle refuses. Until TASK-0123 the solve then moved the points of the person.
        // Finding E103 of the codebase review of 2026-10-31 (TASK-0132, Q1 A): v0.117 to v0.124 applied
        // such an arc, so an open and a replay of their files apply it; the solve then gives no solution.
        var (c, s, e) = (Position(sketch, add.CenterPointId), Position(sketch, add.StartPointId), Position(sketch, add.EndPointId));
        if (!SketchResults.ApplyingEntry.Value && (s == c || e == c))
            return Task.FromResult(SketchResults.Failure(
                DiagnosticCodes.SketchCurveInvalid, "The start and the end of an arc are away from its centre, and the command puts one of them on it."));

        var arc = new SketchArc(add.CommandId, add.CenterPointId, add.StartPointId, add.EndPointId);
        return Task.FromResult(SketchResults.Changed(document, backend, command.CommandId, sketch with { Arcs = [.. sketch.Arcs, arc] }, "arcId", arc.Id));
    }

    private static (double X, double Y) Position(SketchRecord sketch, Guid point)
    {
        var found = sketch.Points.First(item => item.Id == point);
        return (found.X, found.Y);
    }
}
