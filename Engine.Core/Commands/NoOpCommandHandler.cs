using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;

namespace Engine.Core.Commands;

public sealed class NoOpCommandHandler : ICommandHandler
{
    public string CommandName => "NoOp";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["echo"] = new("string", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["echo"] = new("string"),
        };

    // Per ADR-0016.
    public Command Create(CommandInput input) => new NoOpCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        Echo = (string)input.Parameters["echo"]!,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var noop = (NoOpCommand)command;
        var outputs = new Outputs(new Dictionary<string, object?>
        {
            ["echo"] = noop.Echo,
        });
        return Task.FromResult(CommandHandlerResult.Success(outputs));
    }
}
