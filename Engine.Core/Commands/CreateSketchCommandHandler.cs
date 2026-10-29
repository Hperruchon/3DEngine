using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Contracts.Sketch;
using Engine.Core.Persistence;
using Engine.Core.Sketch;

namespace Engine.Core.Commands;

// ADR-0033 items 1, 4 and 6. The datum is one of the three origin planes. A name follows
// the rules of the name of a body (ADR-0028 item 3); an empty name is no name. The
// handler needs no capability of the backend: a sketch is not a body.
public sealed class CreateSketchCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "CreateSketch";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["datumId"] = new("guid", Required: true),
            ["name"] = new("string", Required: false),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sketchId"] = new("guid"),
        };

    // ADR-0015 item 11: the parameters that Create reads.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (CreateSketchCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["datumId"] = typed.DatumId,
            ["name"] = typed.SketchName,
        };
    }

    // Per ADR-0016.
    public Command Create(CommandInput input) => new CreateSketchCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        DatumId = (Guid)input.Parameters["datumId"]!,
        SketchName = input.Parameters.GetValueOrDefault("name") as string,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var create = (CreateSketchCommand)command;

        // Finding E86 of the codebase review of 2026-10-29: the item takes the identifier of the
        // command, so a command that the log holds is refused before it gives a second item.
        if (SketchResults.InUse(document, command) is { } inUse)
            return Task.FromResult(inUse);

        if (Datums.Find(create.DatumId) is null)
            return Task.FromResult(SketchResults.Failure(
                DiagnosticCodes.SketchDatumNotFound,
                $"No datum with id '{create.DatumId}' exists. The datums are the origin planes XY, XZ and YZ (ADR-0033 item 1)."));

        if (create.SketchName is { } name && BodyNames.Refusal(name) is { } refusal)
            return Task.FromResult(SketchResults.Failure(DiagnosticCodes.SketchNameInvalid, refusal));

        var sketch = SketchModel.New(create.CommandId, create.DatumId, string.IsNullOrEmpty(create.SketchName) ? null : create.SketchName);
        var outputs = new Outputs(new Dictionary<string, object?> { ["sketchId"] = sketch.Id });
        return Task.FromResult(CommandHandlerResult.Success(outputs, createdSketches: [sketch]));
    }
}
