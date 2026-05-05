using Engine.Contracts;
using Engine.Contracts.Handlers;

namespace Engine.Core.Commands;

public sealed class NoOpCommandHandler : ICommandHandler
{
    public string CommandName => "NoOp";
    public int SchemaVersion => 1;

    public Task<CommandHandlerResult> Handle(Command command, Document document, CancellationToken ct)
    {
        var noop = (NoOpCommand)command;
        var outputs = new Outputs(new Dictionary<string, object?>
        {
            ["echo"] = noop.Echo,
        });
        return Task.FromResult(CommandHandlerResult.Success(outputs));
    }
}
