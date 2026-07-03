using Engine.Contracts.Geometry;
using Engine.Contracts.Schema;

namespace Engine.Contracts.Handlers;

// Non-generic query handler — mirrors ICommandHandler. Result is boxed
// (object?); QueryBus<T> unboxes to the caller's declared T. This keeps
// the registry single-typed and matches the command-side pattern.
//
// Per ADR-0008 §6: query handlers MUST NOT mutate Document or backend
// caches. The bus enforces nothing here; conformance is by convention +
// validation rule ADR-0008 §Validation 5.
public interface IQueryHandler
{
    string QueryName { get; }
    int SchemaVersion { get; }

    // Schema declarations per ADR-0013 §1. Symmetric to ICommandHandler.
    IReadOnlyDictionary<string, FieldSchema> Parameters { get; }
    IReadOnlyDictionary<string, FieldSchema> Result { get; }

    // Backend per ADR-0012 §2; same rationale as ICommandHandler.
    Task<QueryHandlerResult> Handle(
        Query query,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct);
}

public sealed record QueryHandlerResult(
    object? Result,
    IReadOnlyList<Diagnostic> Diagnostics,
    ErrorDetail? Error)
{
    public bool IsSuccess => Error is null;

    public static QueryHandlerResult Success(
        object? result,
        IReadOnlyList<Diagnostic>? diagnostics = null)
        => new(result, diagnostics ?? Array.Empty<Diagnostic>(), null);

    public static QueryHandlerResult Failure(
        ErrorDetail error,
        IReadOnlyList<Diagnostic>? diagnostics = null)
        => new(null, diagnostics ?? Array.Empty<Diagnostic>(), error);
}
