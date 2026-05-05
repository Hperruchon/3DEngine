# TASK-0001 — Create the Engine Runtime spine

## Status

Done — shipped in commit `721c16f`. See `docs/CURRENT-STATE.md` v0.1.

## Context

The repository currently contains `3DEngine.Core` (POCOs and abstractions for scene/mesh/material), a Vulkan desktop host, a Blazor shell, and Vortice samples. There is no command bus, no Document concept, and no event stream. The architecture defined in ADRs 0001–0004 requires a load-bearing spine before any feature work begins.

This task creates that spine. Nothing else.

## Goal

Stand up `Engine.Contracts` and `Engine.Core` with the minimum types and runtime needed to:

- Define `Command`, `CommandResult`, `Query`, `QueryResult<T>`, `Document`, and `EventRecord` per the contracts in ADRs 0005, 0006, and 0008.
- Apply commands through a `CommandBus` returning structured `CommandResult`.
- Run queries through a `QueryBus` returning structured `QueryResult<T>` (registry stays empty in this task).
- Produce events on application, with monotonic per-Document `Seq`.
- Replay a command log into an equivalent Document.

No UI, no kernel integration, no persistence beyond what's needed to prove replay, no concrete query handlers.

## Scope (in)

1. **New project: `Engine.Contracts`**
   - `Command` — abstract record / discriminated base, with `CommandId : Guid`, `Name : string`, `SchemaVersion : int`, optional `ExpectedDocumentVersion : long?`.
   - `CommandResult` — full structure per ADR 0008 §2: `CommandId`, `CommandName`, `Status ∈ { Applied, Rejected, Cancelled }`, `AppliedAtSeq : long?`, `DocumentVersion : long`, `Outputs`, `Diagnostics : Diagnostic[]`, `Error : ErrorDetail?`, `DurationMs : long`.
   - `Outputs` — typed wrapper around `IReadOnlyDictionary<string, object?>` with safe `Get<T>(key)` accessor.
   - `Diagnostic` — `Severity : Info|Warning|Error`, `Code : string`, `Message : string`, `Path : string?`, `Data : IReadOnlyDictionary<string, object?>?`.
   - `ErrorDetail` — `Code`, `Message`, `Cause : ErrorDetail?`, `Retriable : bool`.
   - `Query` — abstract record / discriminated base, with `QueryId : Guid`, `Name : string`, `SchemaVersion : int`.
   - `QueryResult<T>` — `QueryName`, `AsOfDocumentVersion : long`, `Result : T?`, `Diagnostics`, `Error?`, `DurationMs`.
   - `EventRecord` — `Seq : long`, `Timestamp : DateTime UTC`, `DocumentId : Guid`, `CauseCommandId : Guid?`, `Kind : string`, `Payload : object?` (per ADR 0005 §1).
   - `Document` — ordered command log + opaque materialized state placeholder + metadata header (`DocumentId`, project id, schema version, created/updated, current `Version : long` mirroring last emitted Seq).
   - `BodyHandle` — opaque `record BodyHandle(Guid Id)`. Reserved for future kernel work.
   - Capability marker interfaces (declared, empty bodies): `IGeometryBackend`, `IMeshOps`, `IGeometryQuery`, `IBRepOps`, `IFeatureIdMap`. Reserved per ADR 0001.
   - Handler abstractions: `ICommandHandler`, `IQueryHandler` — minimal shapes; concrete impls live in `Engine.Core` only for the `NoOp` test command.
   - **No JSON serialization, no schema generation in this task.** Both arrive in a later task.

2. **New project: `Engine.Core`**
   - `CommandBus` with `Task<CommandResult> Apply(Command, CancellationToken)`. Serial execution per Document (ADR 0006 §2). Atomic commit-at-end (ADR 0006 §1). Idempotency cache for `CommandId` not required in this task (deferred to TASK with HTTP transport).
   - `QueryBus` with `Task<QueryResult<T>> Query<T>(Query, CancellationToken)`. Empty `QueryRegistry` — no concrete query in this task. The bus exists, returns `Rejected` with `Error.Code = "E-QRY-UNKNOWN"` for any submitted query.
   - `Document` runtime instance: append-on-apply command log, monotonic event sequence, immutable header, `Version` advances on each emitted event.
   - `CommandRegistry` — registry mapping `(name, schemaVersion)` to handler. `QueryRegistry` — same shape, separate instance.
   - Event stream: `IEventSink`, default `InMemoryEventSink` with bounded ring (default capacity 10_000 per ADR 0005 §3); subscribers obtain a snapshot of buffered events plus a live cursor. No transport, no reconnect protocol in this task — the in-memory subscription is enough to test ordering and ring eviction.
   - Diagnostic code constants (`DiagnosticCodes` static class): seed entries used by the spine — `E-CMD-UNKNOWN`, `E-QRY-UNKNOWN`, `E-CMD-VERSION-STALE`, `E-CMD-BUS-BUSY`. Update `docs/diagnostics.md` with these.
   - A built-in `NoOpCommand` + handler. Only command in this task. On apply, returns `Applied`, emits one `command.applied` event, `Outputs` contains a single key `"echo"` with the value passed in. This proves `Outputs` round-trips end-to-end.
   - `Replay(IEnumerable<Command>) → Document` reconstructs an equivalent Document from a log.

3. **Engine Runtime boundary documented in code**
   - `Engine.Core` has no reference to UI assemblies, kernel backends, or HTTP layers.
   - `Engine.Contracts` has no dependencies beyond `System.*`.

4. **Tests project: `Engine.Tests`**
   - Unit: applying `NoOpCommand` returns `Applied`, `AppliedAtSeq` equals the emitted event's `Seq`, `Outputs["echo"]` matches input, log appended, exactly one `command.applied` event emitted.
   - Unit: applying an unknown command returns `Rejected` with `Error.Code = "E-CMD-UNKNOWN"` and `Status == Rejected`. Document is unchanged. One `command.rejected` event is emitted.
   - Unit: `Status == Applied ⇒ Error == null`; `Status == Rejected ⇒ Error != null`. Property test over a small generated set.
   - Unit: applying a command with mismatched `ExpectedDocumentVersion` returns `Rejected` with `Error.Code = "E-CMD-VERSION-STALE"`. Document unchanged.
   - Unit: querying through the empty `QueryBus` returns `Rejected` with `Error.Code = "E-QRY-UNKNOWN"`. No event emitted (queries do not appear in the stream — ADR 0008 §6).
   - Scenario: apply N `NoOpCommand`s; assert log length, event count, monotonic strictly-increasing `Seq`, `Document.Version` mirrors last `Seq`.
   - Replay: take a Document's command log, replay it on a fresh Document via `Replay(...)`, assert equality of log, materialized state, and emitted event sequence (modulo `Timestamp` and `DocumentId`).
   - Ring eviction: write `capacity + 1` events into `InMemoryEventSink`, assert oldest is dropped and the surviving range is contiguous in `Seq`.

## Scope (out)

- Manifold integration. Reserved capability interfaces are declared but unimplemented.
- Any geometry command (`CreateBox`, etc.) or query (`GetEntity`, etc.).
- Concrete query handlers. The query bus and registry exist; they are empty.
- Persistence to disk (`commands.jsonl`, `project.json`). In-memory only for this task.
- HTTP API, WebSocket surface, reconnect/cursor protocol.
- Schema generation / `/schema/*` endpoints.
- Idempotency cache by `CommandId` (deferred to the transport task).
- Desktop or Blazor wiring.
- Undo/redo.
- Save/load file format.
- Renderer changes.
- Project restructuring of existing `3DEngine`, `3DEngine.Core`, Vortice samples, or Blazor projects. They remain untouched.

## Inputs

- ADR 0001 (kernel capabilities)
- ADR 0002 (headless-first)
- ADR 0003 (Blazor as thin viewer)
- ADR 0004 (Engine Runtime authority)
- ADR 0005 (event stream and replay) — `EventRecord`, `Seq` semantics, in-memory ring
- ADR 0006 (command execution model) — serial execution, atomic commit, `Status` triad
- ADR 0007 (UI ephemeral state boundary) — informs what does *not* belong in `Engine.*`
- ADR 0008 (Command/Query/Event triad) — `CommandResult`, `QueryResult<T>`, `Diagnostic`, `ErrorDetail` shapes
- `docs/architecture/engine-runtime-boundaries.md`

## Outputs

- `Engine.Contracts` project compiles, referenced only by `System.*`.
- `Engine.Core` project compiles, references only `Engine.Contracts`.
- `Engine.Tests` project passes all listed tests.
- Solution file updated to include the three new projects.
- Existing projects unchanged.

## Files

**Created:**
- `Engine.Contracts/Engine.Contracts.csproj`
- `Engine.Contracts/Command.cs`
- `Engine.Contracts/CommandResult.cs`
- `Engine.Contracts/Outputs.cs`
- `Engine.Contracts/Diagnostic.cs`
- `Engine.Contracts/ErrorDetail.cs`
- `Engine.Contracts/Query.cs`
- `Engine.Contracts/QueryResult.cs`
- `Engine.Contracts/EventRecord.cs`
- `Engine.Contracts/Document.cs`
- `Engine.Contracts/BodyHandle.cs`
- `Engine.Contracts/Handlers/ICommandHandler.cs`
- `Engine.Contracts/Handlers/IQueryHandler.cs`
- `Engine.Contracts/Geometry/IGeometryBackend.cs`
- `Engine.Contracts/Geometry/IMeshOps.cs`
- `Engine.Contracts/Geometry/IGeometryQuery.cs`
- `Engine.Contracts/Geometry/IBRepOps.cs`
- `Engine.Contracts/Geometry/IFeatureIdMap.cs`
- `Engine.Core/Engine.Core.csproj`
- `Engine.Core/CommandBus.cs`
- `Engine.Core/QueryBus.cs`
- `Engine.Core/CommandRegistry.cs`
- `Engine.Core/QueryRegistry.cs`
- `Engine.Core/DiagnosticCodes.cs`
- `Engine.Core/IEventSink.cs`
- `Engine.Core/InMemoryEventSink.cs`
- `Engine.Core/Replay.cs`
- `Engine.Core/Commands/NoOpCommand.cs`
- `Engine.Core/Commands/NoOpCommandHandler.cs`
- `Engine.Tests/Engine.Tests.csproj`
- `Engine.Tests/CommandBusTests.cs`
- `Engine.Tests/QueryBusTests.cs`
- `Engine.Tests/EventSinkTests.cs`
- `Engine.Tests/ReplayTests.cs`
- `docs/diagnostics.md` — initial registry with the four seed codes.

**Modified:**
- `3DEngine.sln` — add the three new projects.

**Do not touch:**
- `3DEngine/`, `3DEngine.Core/`, `BlazorApp/`, `Vortice.Vulkan.*`.
- Any existing project file or source.

## Tests

- `CommandBusTests.Apply_NoOpCommand_Returns_Applied_With_Echo_Output_And_Emits_Event`
- `CommandBusTests.Apply_Unknown_Command_Returns_Rejected_With_E_CMD_UNKNOWN`
- `CommandBusTests.Applied_Implies_No_Error_And_Rejected_Implies_Error`
- `CommandBusTests.Stale_ExpectedDocumentVersion_Is_Rejected_With_E_CMD_VERSION_STALE`
- `CommandBusTests.Apply_N_Commands_Yields_Monotonic_Sequence_And_Document_Version`
- `QueryBusTests.Query_Unknown_Returns_Rejected_With_E_QRY_UNKNOWN_And_Emits_No_Event`
- `EventSinkTests.Ring_Evicts_Oldest_When_Capacity_Exceeded`
- `EventSinkTests.Buffered_Range_Is_Contiguous_In_Seq_After_Eviction`
- `ReplayTests.Replaying_Log_Reconstructs_Equivalent_Document`
- `ReplayTests.Replay_Emits_Same_Event_Sequence_Modulo_Timestamp_And_DocumentId`

## Acceptance criteria

1. `dotnet build` succeeds on Windows and Linux.
2. `dotnet test` passes all listed tests.
3. `Engine.Contracts` has no project references; `Engine.Core` references only `Engine.Contracts`. CI/dependency check confirms this.
4. No file under `3DEngine/`, `3DEngine.Core/`, `BlazorApp/`, or `Vortice.Vulkan.*` is modified.
5. `IGeometryBackend`, `IMeshOps`, `IGeometryQuery`, `IBRepOps`, `IFeatureIdMap` exist as declared, unimplemented contracts.
6. `NoOpCommand` is the only concrete command in the task. The `QueryBus` exists with an empty `QueryRegistry` and zero concrete queries.
7. `CommandResult` and `QueryResult<T>` shapes match ADR 0008 §2 and §6 exactly.
8. Replay test demonstrates Document equality after reconstruction.
9. `docs/diagnostics.md` exists and lists the four seed codes (`E-CMD-UNKNOWN`, `E-QRY-UNKNOWN`, `E-CMD-VERSION-STALE`, `E-CMD-BUS-BUSY`) with stable meanings.

## Notes for the implementer

- This task defines load-bearing contracts. If a contract decision feels uncertain, **stop and ask** rather than choose. Subsequent tasks will be hard to refactor without breaking projects on disk.
- Keep `Document` immutable from the outside. The bus is the only mutator. Queries never write.
- Event sequence numbers are 64-bit monotonic, never reset within a Document instance. `Document.Version` mirrors the last emitted `Seq`.
- `CommandResult.Status == Cancelled` is reserved; not exercised here. The enum value must exist; cancellation token is accepted by the API surface but no command in this task observes it.
- The `QueryBus` exists in V1 even though no query handler is registered yet. This is intentional — it freezes the contract surface before any handler bakes assumptions into the API.
- `Outputs` is a thin wrapper, not a serialization format. JSON serialization arrives with the persistence/transport task; do not pre-decide the wire format here.
- Diagnostic codes are append-only forever. Adding a new code = registering it in `docs/diagnostics.md` in the same PR. No code without a registry entry.
- The HTTP/WS reconnect-cursor protocol (ADR 0005) is **not** implemented in this task. The `InMemoryEventSink` exposes a buffered snapshot + live cursor sufficient for in-process tests; that is the interface the future transport will adapt to, not replace.
- Do not introduce JSON serialization, schema generation, or schema endpoints in this task. Persistence and `/schema/*` come in later tasks.
