using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;

namespace Engine.Core.Commands;

public sealed class CreateBoxCommandHandler : ICommandHandler
{
    public string CommandName => "CreateBox";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["sizeX"] = new("number", Required: true),
            ["sizeY"] = new("number", Required: true),
            ["sizeZ"] = new("number", Required: true),
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
        var create = (CreateBoxCommand)command;

        if (!(create.SizeX > 0 && create.SizeY > 0 && create.SizeZ > 0))
        {
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomInvalidParam,
                    "CreateBox requires sizeX, sizeY, sizeZ all strictly greater than zero.")));
        }

        var mesh = backend.TryGet<IMeshOps>();
        if (mesh is null)
        {
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomCapMissing,
                    "Active backend does not implement IMeshOps; cannot create a box.")));
        }

        // ADR-0012 §4: handle is deterministic from CommandId. Replay against
        // a fresh backend produces identical state.
        var handle = new BodyHandle(create.CommandId);
        mesh.CreateBox(handle, new BoxParameters(create.SizeX, create.SizeY, create.SizeZ));

        var outputs = new Outputs(new Dictionary<string, object?>
        {
            ["bodyId"] = handle.Id,
        });

        return Task.FromResult(CommandHandlerResult.Success(
            outputs,
            createdBodies: new[] { new BodyRecord(handle, "Box") }));
    }
}
