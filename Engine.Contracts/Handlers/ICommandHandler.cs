namespace Engine.Contracts.Handlers;

public interface ICommandHandler
{
    string CommandName { get; }
    int SchemaVersion { get; }
    Task<CommandHandlerResult> Handle(Command command, Document document, CancellationToken ct);
}

public sealed record CommandHandlerResult(
    Outputs Outputs,
    IReadOnlyList<Diagnostic> Diagnostics,
    ErrorDetail? Error)
{
    public bool IsSuccess => Error is null;

    public static CommandHandlerResult Success(Outputs outputs, IReadOnlyList<Diagnostic>? diagnostics = null)
        => new(outputs, diagnostics ?? Array.Empty<Diagnostic>(), null);

    public static CommandHandlerResult Failure(ErrorDetail error, IReadOnlyList<Diagnostic>? diagnostics = null)
        => new(Outputs.Empty, diagnostics ?? Array.Empty<Diagnostic>(), error);
}
