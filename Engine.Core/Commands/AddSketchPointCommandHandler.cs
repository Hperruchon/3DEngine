using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Contracts.Sketch;
using Engine.Core.Persistence;
using Engine.Core.Sketch;

namespace Engine.Core.Commands;

// ADR-0033 items 2, 5 and 6: a new point of a live sketch, at a finite position.
public sealed class AddSketchPointCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "AddSketchPoint";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sketchId"] = new("guid", Required: true),
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
        var typed = (AddSketchPointCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sketchId"] = typed.SketchId,
            ["x"] = typed.X,
            ["y"] = typed.Y,
        };
    }

    // Per ADR-0016.
    public Command Create(CommandInput input) => new AddSketchPointCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        SketchId = (Guid)input.Parameters["sketchId"]!,
        X = (double)input.Parameters["x"]!,
        Y = (double)input.Parameters["y"]!,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var add = (AddSketchPointCommand)command;

        if (SketchModel.Find(document, add.SketchId) is not { } sketch)
            return Task.FromResult(SketchResults.NotFound(add.SketchId));

        if (!SketchResults.AreFinite(add.X, add.Y))
            return Task.FromResult(SketchResults.Failure(
                DiagnosticCodes.GeomInvalidParam, "The position of a point of a sketch is a pair of finite numbers."));

        var point = new SketchPoint(add.CommandId, add.X, add.Y);
        return Task.FromResult(SketchResults.Changed(sketch with { Points = [.. sketch.Points, point] }, "pointId", point.Id));
    }
}
