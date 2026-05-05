namespace Engine.Contracts;

public enum CommandStatus
{
    Applied,
    Rejected,
    Cancelled
}

public sealed record CommandResult(
    Guid CommandId,
    string CommandName,
    CommandStatus Status,
    long? AppliedAtSeq,
    long DocumentVersion,
    Outputs Outputs,
    IReadOnlyList<Diagnostic> Diagnostics,
    ErrorDetail? Error,
    long DurationMs);
