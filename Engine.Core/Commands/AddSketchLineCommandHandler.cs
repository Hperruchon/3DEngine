using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Contracts.Sketch;
using Engine.Core.Persistence;
using Engine.Core.Sketch;

namespace Engine.Core.Commands;

// ADR-0033 items 2 (Q3), 5 and 6: a line references two different points of its sketch.
// Two points at one position are two points, so a line of length zero is valid; the
// solver of K3a decides the positions.
public sealed class AddSketchLineCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "AddSketchLine";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sketchId"] = new("guid", Required: true),
            ["startPointId"] = new("guid", Required: true),
            ["endPointId"] = new("guid", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["lineId"] = new("guid"),
        };

    // ADR-0015 item 11: the parameters that Create reads.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (AddSketchLineCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sketchId"] = typed.SketchId,
            ["startPointId"] = typed.StartPointId,
            ["endPointId"] = typed.EndPointId,
        };
    }

    // Per ADR-0016.
    public Command Create(CommandInput input) => new AddSketchLineCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SketchId = (Guid)input.Parameters["sketchId"]!,
        StartPointId = (Guid)input.Parameters["startPointId"]!,
        EndPointId = (Guid)input.Parameters["endPointId"]!,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var add = (AddSketchLineCommand)command;

        // Finding E86 of the codebase review of 2026-10-29: the item takes the identifier of the
        // command, so a command that the log holds is refused before it gives a second item.
        if (SketchResults.InUse(document, command, createsItem: true) is { } inUse)
            return Task.FromResult(inUse);

        if (SketchModel.Find(document, add.SketchId) is not { } sketch)
            return Task.FromResult(SketchResults.NotFound(add.SketchId));

        foreach (var id in new[] { add.StartPointId, add.EndPointId })
        {
            if (SketchModel.KindOf(sketch, id) != SketchModel.ItemKind.Point)
                return Task.FromResult(SketchResults.ItemNotFound(add.SketchId, id, "point"));
        }

        if (add.StartPointId == add.EndPointId)
            return Task.FromResult(SketchResults.Failure(
                DiagnosticCodes.SketchLineInvalid, $"A line references two different points, and both ends are the point '{add.StartPointId}'."));

        var line = new SketchLine(add.CommandId, add.StartPointId, add.EndPointId);
        return Task.FromResult(SketchResults.Changed(document, backend, command.CommandId, sketch with { Lines = [.. sketch.Lines, line] }, "lineId", line.Id));
    }
}
