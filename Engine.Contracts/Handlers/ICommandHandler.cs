using Engine.Contracts.Geometry;
using Engine.Contracts.Schema;

namespace Engine.Contracts.Handlers;

public interface ICommandHandler
{
    string CommandName { get; }
    int SchemaVersion { get; }

    // Schema declarations per ADR-0013 §1. The handler is the single source
    // of truth; /schema/commands/{name}@{version} projects these directly.
    IReadOnlyDictionary<string, FieldSchema> Parameters { get; }
    IReadOnlyDictionary<string, FieldSchema> Outputs { get; }

    // Handle receives the active backend per ADR-0012 §2. Backends are
    // caches; the bus passes whatever the current backend is on each call.
    // Replay against a fresh backend is just a call with a different
    // backend argument. Handlers that don't need geometry ignore the
    // parameter; the bus always passes a non-null backend (typically
    // NullGeometryBackend when no real one is wired).
    Task<CommandHandlerResult> Handle(
        Command command,
        Document document,
        IGeometryBackend backend,
        CancellationToken ct);
}

public sealed record CommandHandlerResult(
    Outputs Outputs,
    IReadOnlyList<Diagnostic> Diagnostics,
    ErrorDetail? Error,
    IReadOnlyList<BodyRecord> CreatedBodies)
{
    public bool IsSuccess => Error is null;

    public static CommandHandlerResult Success(
        Outputs outputs,
        IReadOnlyList<Diagnostic>? diagnostics = null,
        IReadOnlyList<BodyRecord>? createdBodies = null)
        => new(
            outputs,
            diagnostics ?? Array.Empty<Diagnostic>(),
            null,
            createdBodies ?? Array.Empty<BodyRecord>());

    public static CommandHandlerResult Failure(
        ErrorDetail error,
        IReadOnlyList<Diagnostic>? diagnostics = null)
        => new(
            Outputs.Empty,
            diagnostics ?? Array.Empty<Diagnostic>(),
            error,
            Array.Empty<BodyRecord>());
}
