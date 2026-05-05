namespace Engine.Contracts;

public sealed record ErrorDetail(
    string Code,
    string Message,
    ErrorDetail? Cause = null,
    bool Retriable = false);
