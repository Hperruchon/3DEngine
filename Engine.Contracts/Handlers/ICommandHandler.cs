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

    // Per ADR-0016: the handler builds its own command from bound parameters.
    // The host finds the handler, binds the raw values against Parameters
    // above, then calls this. No host holds the name of a command.
    // ParameterBinder has already checked each required field and each type,
    // so an implementation reads the values directly.
    Command Create(CommandInput input);
}

// Per ADR-0016. A record, not three loose arguments, so a later field adds
// no second contract change.
public sealed record CommandInput(
    IReadOnlyDictionary<string, object?> Parameters,
    Guid CommandId,
    long? ExpectedDocumentVersion);

public sealed record CommandHandlerResult(
    Outputs Outputs,
    IReadOnlyList<Diagnostic> Diagnostics,
    ErrorDetail? Error,
    IReadOnlyList<BodyRecord> CreatedBodies)
{
    public bool IsSuccess => Error is null;

    // ADR-0021 item 2: the live bodies that the command consumes. The commit
    // removes each one from Document.Bodies and emits body.consumed. A handler
    // consumes only a live body, and it gives each handle one time; the bus
    // refuses a list that breaks the rule. Empty by default, so a handler that
    // gives no list consumes nothing.
    public IReadOnlyList<BodyHandle> ConsumedBodies { get; init; } = Array.Empty<BodyHandle>();

    // ADR-0028 item 6: the new record of each live body that the command changes,
    // for example its name or its visibility. The commit replaces each record in
    // Document.Bodies and emits body.changed after each body.consumed. A handler
    // changes only a live body that the same command neither creates nor
    // consumes, gives each one time, and keeps its kind; the bus refuses a list
    // that breaks the rule. Empty by default.
    public IReadOnlyList<BodyRecord> ChangedBodies { get; init; } = Array.Empty<BodyRecord>();

    public static CommandHandlerResult Success(
        Outputs outputs,
        IReadOnlyList<Diagnostic>? diagnostics = null,
        IReadOnlyList<BodyRecord>? createdBodies = null,
        IReadOnlyList<BodyHandle>? consumedBodies = null,
        IReadOnlyList<BodyRecord>? changedBodies = null)
        => new(
            outputs,
            diagnostics ?? Array.Empty<Diagnostic>(),
            null,
            createdBodies ?? Array.Empty<BodyRecord>())
        {
            ConsumedBodies = consumedBodies ?? Array.Empty<BodyHandle>(),
            ChangedBodies = changedBodies ?? Array.Empty<BodyRecord>(),
        };

    public static CommandHandlerResult Failure(
        ErrorDetail error,
        IReadOnlyList<Diagnostic>? diagnostics = null)
        => new(
            Outputs.Empty,
            diagnostics ?? Array.Empty<Diagnostic>(),
            error,
            Array.Empty<BodyRecord>());
}
