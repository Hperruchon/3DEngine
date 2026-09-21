# ADR 0015 — Handler-owned command construction (amends ADR-0013)

## Status

Proposed

## Context

ADR-0013 (Accepted 2026-05-27) placed command and query *schema declarations* on the handler. `ICommandHandler.Parameters` / `Outputs` are now the single source of truth for what a command takes and returns; `SchemaCommandsEndpoint` projects them directly with no per-command branching. A gate test (`SchemaCommandsEndpoint_Source_Contains_No_Per_Command_Branching`) enforces the no-switch invariant on the schema endpoint.

ADR-0013 stopped short of the parallel question: **who owns the construction of a typed `Command` from external input?** As of v0.12, three surfaces dispatch on `(name, version)` to build typed `Command` instances from external data:

1. `Engine.Cli/Cli.cs` — `apply` verb. Takes `Dictionary<string, string>` from CLI args; per-name switch builds `NoOpCommand` / `CreateBoxCommand`.
2. `Engine.Api.Http/Endpoints/CommandsEndpoint.cs` — `POST /commands`. Takes `Dictionary<string, JsonElement>` from JSON body; per-name switch builds the same typed Commands via `BuildNoOp` / `BuildCreateBox`.
3. **Forthcoming:** `Engine.Core/Persistence/CommandCodec.cs` (TASK-0012). Takes `JsonObject` from the on-disk log; per-name switch reconstructs typed Commands on replay.

Each surface duplicates the per-command knowledge. When a third command lands (or a fourth, fifth), every site needs a parallel update. ADR-0013's first amendment-prompting concern — flagged during TASK-0012 review and confirmed by the user — is that "schema lives on the handler" was the right call but stopped one step short. The natural extension is: **schema and construction both live on the handler.**

This ADR settles that extension. With it landed, a new command lands in one file (its handler); the three dispatch sites become uniform `registry.TryFind → handler.Create` calls; the gate test extends across all three sites.

This ADR is a precondition for TASK-0012 resumption — `CommandCodec.DeserializeCommand` should use the handler-owned construction from day one rather than adding a third per-name switch that would then be deleted.

## Decision

### 1. `ICommandHandler` gains `Create`

`Engine.Contracts/Handlers/ICommandHandler.cs`:

```csharp
public interface ICommandHandler
{
    string CommandName { get; }
    int SchemaVersion { get; }
    IReadOnlyDictionary<string, FieldSchema> Parameters { get; }
    IReadOnlyDictionary<string, FieldSchema> Outputs { get; }

    // NEW: construct a typed Command from a parameters object. The handler
    // is the single source of truth for "given these parameters, build my
    // Command." Caller provides CommandId and ExpectedDocumentVersion;
    // handler reads parameter values from the JsonElement using its
    // declared Parameters schema.
    Command Create(
        JsonElement parameters,
        Guid commandId,
        long? expectedDocumentVersion);

    Task<CommandHandlerResult> Handle(...);  // unchanged
}
```

Semantics:

- **`parameters` is a `JsonElement` of `ValueKind.Object`** containing per-parameter values keyed by camelCase parameter name (matching `handler.Parameters` schema keys). For primitives the handler reads via `element.GetProperty(name).GetString()` / `.GetDouble()` / `.GetGuid()` etc.
- **`commandId` is used verbatim** in the returned Command. Handlers MUST NOT call `Guid.NewGuid()` inside `Create` — this preserves ADR-0005 replay determinism and ADR-0012 §4's deterministic body handles.
- **`expectedDocumentVersion`** flows through to `Command.ExpectedDocumentVersion`.
- **Throws `InvalidCommandParametersException`** (new, in `Engine.Contracts/Handlers/`) for missing required parameters, type mismatches, or out-of-range values that prevent constructing a valid Command. The exception carries a structured `Reason` string. Validation that is cheap and statically obvious belongs in `Create`; semantic validation that requires Document or backend state stays in `Handle` (where it can return `E-GEOM-INVALID-PARAM` etc. via `CommandHandlerResult.Failure`).
- **Returns a fully-constructed, typed `Command` subclass.** The returned Command is then submitted through `CommandBus.Apply` exactly as today.

### 2. Three dispatch sites lose their per-command switches

After this ADR ships:

- **`Engine.Cli/Cli.Apply`** drops the `case "NoOp" / case "CreateBox"` switch. It builds a `JsonObject` from `Dictionary<string, string>` args (using `handler.Parameters[name].Type` to decide the conversion: `"string"` → `JsonValue.Create(raw)`, `"number"` → `double.Parse(raw, InvariantCulture)`, `"guid"` → `JsonValue.Create(raw)`), serializes to `JsonElement`, then calls `handler.Create(...)`. The string-to-typed conversion is a CLI helper that uses schema info — it is NOT per-command. A new command works without CLI changes.
- **`Engine.Api.Http/Endpoints/CommandsEndpoint`** drops `BuildNoOp` / `BuildCreateBox`. It already has `Dictionary<string, JsonElement>` from request parsing; wraps to a single `JsonElement` object and calls `handler.Create(...)`. On `InvalidCommandParametersException`, returns HTTP 400 with `E-API-BAD-REQUEST` and the reason string.
- **`Engine.Core/Persistence/CommandCodec.DeserializeCommand`** (built fresh in TASK-0012) takes `JsonObject` from the log, converts to `JsonElement`, calls `handler.Create(...)`. On `InvalidCommandParametersException`, raises `PersistenceLoadException` (`E-IO-LOAD-FAILED`).

After migration, each of the three sites contains the same five-line pattern:

```csharp
if (!registry.TryFind(name, version, out var handler))
    return RejectUnknown(...);
Command command;
try { command = handler.Create(parameters, commandId, expectedVersion); }
catch (InvalidCommandParametersException ex) { return RejectInvalid(ex); }
// submit command through the bus
```

### 3. Gate test extends

TASK-0011 introduced `SchemaCommandsEndpoint_Source_Contains_No_Per_Command_Branching` — a textual gate that asserts the schema endpoint source contains no string literal matching a registered command name. This ADR extends the principle:

- `Engine.Cli/Cli.cs` source MUST NOT contain a string literal matching a registered command name (post-migration).
- `Engine.Api.Http/Endpoints/CommandsEndpoint.cs` source MUST NOT contain a string literal matching a registered command name (post-migration).
- `Engine.Core/Persistence/CommandCodec.cs` source MUST NOT contain a string literal matching a registered command name (when it lands in TASK-0012).

The gate scans each file as text and fails if any literal `"NoOp"` / `"CreateBox"` / future-name appears. The registry's `Registered` accessor (already exposed per TASK-0009) supplies the name list.

### 4. JsonElement, not IParameterBag, for V1

Considered (and rejected for V1): an `IParameterBag` interface with per-caller adapters (`StringDictionaryParameterBag` for CLI, `JsonElementDictionaryParameterBag` for HTTP, `JsonObjectParameterBag` for codec). Rationale for rejection:

- Three adapters add ~100 lines of code to abstract a problem that `JsonElement` already handles uniformly for V1.x's primitive parameter types (string, double, GUID, bool).
- The abstraction doesn't generalize until commands grow parameter types that `JsonElement` handles poorly (e.g., binary blobs, nested objects with their own schema). V1.x has none.
- Promoting `JsonElement` to the handler interface is honest: the wire format is JSON; the on-disk format is JSON; the CLI converts to JSON for transit. `JsonElement` is the natural lingua franca for V1.

If a future workload introduces parameter types `JsonElement` handles poorly, a follow-on ADR introduces `IParameterBag` (or equivalent) and migrates the handler interface. The migration is local — `Create` signature change, three call-site updates — and not load-bearing on anything else.

### 5. No `CommandBus.ApplyJson`

Considered (and rejected for V1): a `CommandBus.ApplyJson(name, version, parameters, commandId, expectedVersion)` shortcut that internalizes lookup + construction. Rationale for rejection:

- The bus's primary API stays typed (`Apply(Command)`). Adding a JSON entry point creates a parallel API that internal callers (tests, future programmatic submitters) would have to choose between.
- The savings (one call per dispatch site) are small at V1.x scale.
- If JSON submission becomes the dominant path, a future ADR can elevate it to the bus.

### 6. Amends ADR-0013

ADR-0013 §1 stated: *"Schema lives on the handler. The handler is the single source of truth; endpoints project these directly."* This ADR extends the principle to construction: *"Schema and construction both live on the handler. Dispatch sites project to `handler.Create` rather than per-command switches."*

The amendment is additive. ADR-0013's validation rules continue to hold. The gate-test extension in §3 layers on the original `SchemaCommandsEndpoint` gate — both are in force.

### 7. Query handler construction is out of scope

`IQueryHandler` has the same shape problem (CLI and HTTP both dispatch on query name to build typed Queries). The fix is symmetric: `IQueryHandler.Create`. V1.x has one query (`GetBoundingBox`); the pressure is low. A sibling ADR amends ADR-0013 for queries when the second query lands or when the CLI/HTTP query-construction duplication becomes a real problem. Keeping this ADR scoped to commands matches Q2's framing and avoids speculative interface change.

## Consequences

- **`Engine.Contracts/Handlers/ICommandHandler.cs` evolves.** New `Create` method; new `InvalidCommandParametersException` (in same namespace). Gated by this ADR per CLAUDE.md.
- **`NoOpCommandHandler` and `CreateBoxCommandHandler` each gain a `Create` method** (~10 lines each). Implementations read parameters from `JsonElement` with standard `GetProperty(...).GetString()` / `.GetDouble()` calls; missing/wrong-type fields throw `InvalidCommandParametersException`.
- **CLI, HTTP endpoint, and `CommandCodec` (in TASK-0012) drop per-command switches.** Net code reduction. Adding a new command becomes a single-file change (the new handler).
- **Gate test extends** to the three dispatch sites. CI fails if a per-name string literal is reintroduced.
- **No new diagnostic codes.** Existing codes cover all error cases (CLI usage errors stay as exit code 2; HTTP returns `E-API-BAD-REQUEST`; codec raises `E-IO-LOAD-FAILED`).
- **TASK-0012 resumes with this in force.** The rebuilt `CommandCodec.DeserializeCommand` uses `handler.Create` from the start — no third per-name switch, no later cleanup pass.
- **Direct typed-Command callers (tests, future programmatic submitters)** continue to work. `new NoOpCommand { Echo = "..." }` is unchanged. `Create` is an additional construction path, not a replacement.
- **CLI's string-to-JsonElement conversion helper** lives in `Engine.Cli/` and uses `FieldSchema.Type` to drive type-per-parameter conversion. Only the CLI needs this helper (HTTP and codec already have `JsonElement` / `JsonObject`).
- **Replay determinism is preserved.** `Create`'s `CommandId` argument is used verbatim per §1. The replay-determinism fixture continues to pass without modification.

## Non-goals

- `IParameterBag` abstraction. §4.
- `CommandBus.ApplyJson` entry point. §5.
- Query handler construction (`IQueryHandler.Create`). §7.
- Auto-generated parameter validation from `handler.Parameters` schema. Handlers still hand-check (e.g., `if (sizeX <= 0) throw`). A schema-driven validator is its own ADR if the duplication becomes painful.
- Migrating `ApiErrorEnvelope`, CLI usage-error formatting, or `PersistenceLoadException` shapes. Each caller's error reporting stays its own concern; only the dispatch-and-construct contract is unified.
- Removing the `CommandRequest` DTO in `Engine.Api.Http`. The DTO continues to deserialize the wire body; only the per-name switch on top of it goes.
- Renaming `Engine.Contracts.Schema.FieldSchema.Type` from `string` to an enum. Out of scope; ADR amendment if the V1.x string convention ("string", "number", "guid", "bool") proves insufficient.

## Validation rules

1. Every `ICommandHandler` implementation MUST have a `Create` method that returns a typed `Command` with the supplied `CommandId` (verbatim, not `Guid.NewGuid()`). CI: compile failure if any handler lacks the method; the replay-determinism fixture indirectly verifies the CommandId-verbatim invariant.
2. `Engine.Cli/Cli.cs` source MUST NOT contain a string literal matching any registered command name (post-migration). CI: textual gate (mirrors `SchemaCommandsEndpoint_Source_Contains_No_Per_Command_Branching`).
3. `Engine.Api.Http/Endpoints/CommandsEndpoint.cs` source MUST NOT contain a string literal matching any registered command name (post-migration). CI: same textual gate.
4. `Engine.Core/Persistence/CommandCodec.cs` (when it lands in TASK-0012) MUST use `handler.Create` for deserialization. CI: same textual gate scope.
5. `handler.Create` MUST throw `InvalidCommandParametersException` for missing required parameters or type mismatches. CI: per-handler unit test.
6. `InvalidCommandParametersException` MUST carry a non-empty `Reason` string suitable for surfacing to clients (HTTP returns it in the `E-API-BAD-REQUEST` envelope; CLI prints it as the usage error; codec wraps it in `PersistenceLoadException`).

## Open challenges

- **Query handler construction.** §7. Sibling ADR when motivated.
- **Schema-driven validation library.** Could auto-check `parameters` against `handler.Parameters` before `Create` is even called. Defer until duplication is painful.
- **`FieldSchema.Type` enum vs string.** Today string ("string", "number", "guid"). String is flexible but no IDE help. Revisit if the convention drifts.
- **CLI's primitive-type knowledge.** The CLI helper that converts `Dictionary<string, string>` → `JsonElement` hard-codes the conversion per `FieldSchema.Type` value. If a new type value lands (e.g., `"array"`, `"object"`), the helper needs an addition. Acceptable for V1.x; revisit if the type-set grows.
- **`InvalidCommandParametersException` placement.** Lives in `Engine.Contracts.Handlers` so handlers can throw it. Alternative: `Engine.Core.Commands` (would force `Engine.Contracts` to not throw, breaking the symmetry). Sticking with Contracts.
