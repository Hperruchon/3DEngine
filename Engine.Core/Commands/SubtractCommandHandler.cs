using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;

namespace Engine.Core.Commands;

public sealed class SubtractCommandHandler : ICommandHandler
{
    public string CommandName => "Subtract";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["minuendBodyId"] = new("guid", Required: true),
            ["subtrahendBodyId"] = new("guid", Required: true),
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
        var subtract = (SubtractCommand)command;

        // 1. Both operands must exist in the Document projection (backend-independent,
        // mirrors GetBoundingBox). Precedes the capability check so a bad reference is
        // reported the same way on any backend.
        if (!document.Bodies.Any(b => b.Handle.Id == subtract.MinuendBodyId))
        {
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomBodyNotFound,
                    $"No body with id '{subtract.MinuendBodyId}' exists in the Document.")));
        }
        if (!document.Bodies.Any(b => b.Handle.Id == subtract.SubtrahendBodyId))
        {
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomBodyNotFound,
                    $"No body with id '{subtract.SubtrahendBodyId}' exists in the Document.")));
        }

        // 2. Backend must support booleans (the managed stub does not).
        var boolean = backend.TryGet<IBooleanOps>();
        if (boolean is null)
        {
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomCapMissing,
                    "Active backend does not implement IBooleanOps; cannot subtract bodies.")));
        }

        // 3. Run the op. New body handle is deterministic from CommandId (ADR-0012 §4);
        // both operands are left intact.
        var handle = new BodyHandle(subtract.CommandId);
        try
        {
            boolean.Subtract(
                handle,
                new BodyHandle(subtract.MinuendBodyId),
                new BodyHandle(subtract.SubtrahendBodyId));
        }
        catch (Exception ex)
        {
            // Operands and capability are validated above, so a throw here is a
            // backend/native fault or a degenerate (empty) result, surfaced
            // structurally rather than crashing.
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomNativeOp,
                    $"Geometry backend failed to subtract the bodies: {ex.Message}")));
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
