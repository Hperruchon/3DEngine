namespace Engine.Contracts;

public sealed record QueryResult<T>(
    string QueryName,
    long AsOfDocumentVersion,
    T? Result,
    IReadOnlyList<Diagnostic> Diagnostics,
    ErrorDetail? Error,
    long DurationMs);
