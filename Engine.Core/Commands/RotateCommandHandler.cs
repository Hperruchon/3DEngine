using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0112 (TASK-0335): the command that turns a body, in the form of TranslateCommandHandler.
public sealed class RotateCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "Rotate";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid", Required: true),
            ["qw"] = new("number", Required: true, Unit: UnitKinds.Quaternion),
            ["qx"] = new("number", Required: true, Unit: UnitKinds.Quaternion),
            ["qy"] = new("number", Required: true, Unit: UnitKinds.Quaternion),
            ["qz"] = new("number", Required: true, Unit: UnitKinds.Quaternion),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid"),
        };

    // ADR-0015 item 11: the parameters that Create reads.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
    {
        var typed = (RotateCommand)command;
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["bodyId"] = typed.BodyId,
            ["qw"] = typed.Qw,
            ["qx"] = typed.Qx,
            ["qy"] = typed.Qy,
            ["qz"] = typed.Qz,
        };
    }

    public Command Create(CommandInput input) => new RotateCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        BodyId = (Guid)input.Parameters["bodyId"]!,
        Qw = (double)input.Parameters["qw"]!,
        Qx = (double)input.Parameters["qx"]!,
        Qy = (double)input.Parameters["qy"]!,
        Qz = (double)input.Parameters["qz"]!,
    };

    // ADR-0112 item 2: the turn of the command, or null for a quaternion that gives no turn. Each reader of a Rotate
    // (this handler, the regeneration, the owned kernel and the frame of a datum) takes the turn from here.
    public static Rotation? Turn(RotateCommand rotate) => Rotation.FromQuaternion(rotate.Qw, rotate.Qx, rotate.Qy, rotate.Qz);

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var rotate = (RotateCommand)command;

        // The order of the checks of Translate: the operand, the kind of the body, the values, the capability.
        var source = document.Bodies.FirstOrDefault(b => b.Handle.Id == rotate.BodyId);
        if (source is null)
            return Task.FromResult(CommandHandlerResult.Failure(new ErrorDetail(
                DiagnosticCodes.GeomBodyNotFound, $"No body with id '{rotate.BodyId}' exists in the Document.")));

        // ADR-0112 item 1: a mesh body of an STL file does not move, so it does not turn.
        if (source.Kind == ImportStlCommandHandler.MeshKind)
            return Task.FromResult(CommandHandlerResult.Failure(new ErrorDetail(
                DiagnosticCodes.MeshUnsupported, $"The body '{rotate.BodyId}' is a mesh body of an STL file, which does not turn (ADR-0112 item 1).")));

        if (Turn(rotate) is not { } turn)
            return Task.FromResult(CommandHandlerResult.Failure(new ErrorDetail(
                DiagnosticCodes.GeomInvalidParam,
                "Rotate requires qw, qx, qy and qz finite and not all zero, with a finite turn (ADR-0112 item 2).")));

        if (backend.TryGet<ITransformOps>() is not { } transform)
            return Task.FromResult(CommandHandlerResult.Failure(new ErrorDetail(
                DiagnosticCodes.GeomCapMissing, "Active backend does not implement ITransformOps; cannot turn a body.")));

        var handle = new BodyHandle(rotate.CommandId);
        try
        {
            // ADR-0037 item 2: the solid of the body, under its geometry key.
            transform.Rotate(handle, new BodyHandle(source.GeometryKey), turn);
        }
        catch (Exception ex)
        {
            return Task.FromResult(CommandHandlerResult.Failure(new ErrorDetail(
                DiagnosticCodes.GeomNativeOp, $"Geometry backend failed to turn the body: {ex.Message}")));
        }

        var outputs = new Outputs(new Dictionary<string, object?>
        {
            ["bodyId"] = handle.Id,
        });

        return Task.FromResult(CommandHandlerResult.Success(
            outputs,
            createdBodies: new[] { new BodyRecord(handle, "Solid", source.Name, source.Visible) },
            consumedBodies: new[] { new BodyHandle(rotate.BodyId) }));
    }
}
