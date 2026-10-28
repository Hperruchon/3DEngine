using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core.Persistence;

namespace Engine.Core.Commands;

// ADR-0031 item 7: the handler asks for the move only. The command bus computes the
// bodies from its history and gives the output commandId, the step that it applied again.
public sealed class RedoCommandHandler : ICommandHandler, ICommandEncoder
{
    public string CommandName => "Redo";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>();

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["commandId"] = new("guid"),
        };

    // ADR-0015 item 11: the command has no parameter.
    public IReadOnlyDictionary<string, object?> Encode(Command command)
        => new Dictionary<string, object?>(StringComparer.Ordinal);

    public Command Create(CommandInput input) => new RedoCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
        => Task.FromResult(CommandHandlerResult.Success(Engine.Contracts.Outputs.Empty) with { History = HistoryMove.Redo });
}
