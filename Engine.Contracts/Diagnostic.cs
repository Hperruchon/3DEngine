namespace Engine.Contracts;

public enum Severity
{
    Info,
    Warning,
    Error
}

public sealed record Diagnostic(
    Severity Severity,
    string Code,
    string Message,
    string? Path = null,
    IReadOnlyDictionary<string, object?>? Data = null);
