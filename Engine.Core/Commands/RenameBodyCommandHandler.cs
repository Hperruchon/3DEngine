using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;

namespace Engine.Core.Commands;

// ADR-0028 items 2, 3 and 6: the name must follow the rules of BodyNames, and the body must
// be live. The handler changes no geometry, so it needs no capability of the backend. A
// command that gives the name that the body has is applied, and its commit gives its event.
public sealed class RenameBodyCommandHandler : ICommandHandler
{
    public string CommandName => "RenameBody";
    public int SchemaVersion => 1;

    public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid", Required: true),
            ["name"] = new("string", Required: true),
        };

    public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } =
        new Dictionary<string, FieldSchema>
        {
            ["bodyId"] = new("guid"),
        };

    // Per ADR-0016.
    public Command Create(CommandInput input) => new RenameBodyCommand
    {
        CommandId = input.CommandId,
        ExpectedDocumentVersion = input.ExpectedDocumentVersion,
        BodyId = (Guid)input.Parameters["bodyId"]!,
        NewName = (string)input.Parameters["name"]!,
    };

    public Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct)
    {
        var rename = (RenameBodyCommand)command;

        if (BodyNames.Refusal(rename.NewName) is { } refusal)
        {
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(DiagnosticCodes.ValBodyNameInvalid, refusal)));
        }

        var body = document.Bodies.FirstOrDefault(b => b.Handle.Id == rename.BodyId);
        if (body is null)
        {
            return Task.FromResult(CommandHandlerResult.Failure(
                new ErrorDetail(
                    DiagnosticCodes.GeomBodyNotFound,
                    $"No body with id '{rename.BodyId}' exists in the Document.")));
        }

        var outputs = new Outputs(new Dictionary<string, object?>
        {
            ["bodyId"] = rename.BodyId,
        });

        return Task.FromResult(CommandHandlerResult.Success(
            outputs,
            changedBodies: new[] { body with { Name = rename.NewName.Length == 0 ? null : rename.NewName } }));
    }
}
