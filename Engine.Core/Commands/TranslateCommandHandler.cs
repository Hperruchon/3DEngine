using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;

namespace Engine.Core.Commands;

public sealed class TranslateCommandHandler : ICommandHandler
{
    public string CommandName => "Translate";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid", Required: true),
            ["dx"] = new("number", Required: true),
            ["dy"] = new("number", Required: true),
            ["dz"] = new("number", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid"),
        };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var translate = (TranslateCommand)command;

        // 1. Operand must exist in the Document projection (backend-independent,
        // mirrors GetBoundingBox). This precedes the capability check so a bad
        // reference is reported the same way on any backend.
        if (!document.Bodies.Any(b => b.Handle.Id == translate.BodyId))
        {
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomBodyNotFound,
                    $"No body with id '{translate.BodyId}' exists in the Document.")));
        }

        // 2. Backend must support transforms (the managed stub does not).
        var transform = backend.TryGet<ITransformOps>();
        if (transform is null)
        {
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomCapMissing,
                    "Active backend does not implement ITransformOps; cannot translate a body.")));
        }

        // 3. Run the op. New body handle is deterministic from CommandId (ADR-0012 §4);
        // the source body is left intact.
        var handle = new BodyHandle(translate.CommandId);
        try
        {
            transform.Translate(
                handle,
                new BodyHandle(translate.BodyId),
                translate.Dx, translate.Dy, translate.Dz);
        }
        catch (Exception ex)
        {
            // Operand and capability are validated above, so a throw here is a
            // backend/native fault, surfaced structurally rather than crashing.
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomNativeOp,
                    $"Geometry backend failed to translate the body: {ex.Message}")));
        }

        var outputs = new Outputs(new Dictionary<string, object?>
        {
            ["bodyId"] = handle.Id,
        });

        return Task.FromResult(CommandHandlerResult.Success(
            outputs,
            createdBodies: new[] { new BodyRecord(handle, "Solid") }));
    }
}
