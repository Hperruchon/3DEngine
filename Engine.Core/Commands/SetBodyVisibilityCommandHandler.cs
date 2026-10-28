using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;

namespace Engine.Core.Commands;

// ADR-0028 items 2 and 6: the body must be live. The handler changes no geometry, so it needs
// no capability of the backend. A command that gives the visibility that the body has is
// applied, and its commit gives its event.
public sealed class SetBodyVisibilityCommandHandler : ICommandHandler
{
    public string CommandName => "SetBodyVisibility";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid", Required: true),
            ["visible"] = new("boolean", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid"),
        };

    // Per ADR-0016.
    public Command Create(CommandInput input) => new SetBodyVisibilityCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        BodyId = (Guid)input.Parameters["bodyId"]!,
        Visible = (bool)input.Parameters["visible"]!,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var visibility = (SetBodyVisibilityCommand)command;

        var body = document.Bodies.FirstOrDefault(b => b.Handle.Id == visibility.BodyId);
        if (body is null)
        {
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomBodyNotFound,
                    $"No body with id '{visibility.BodyId}' exists in the Document.")));
        }

        var outputs = new Outputs(new Dictionary<string, object?>
        {
            ["bodyId"] = visibility.BodyId,
        });

        return Task.FromResult(CommandHandlerResult.Success(
            outputs,
            changedBodies: new[] { body with { Visible = visibility.Visible } }));
    }
}
