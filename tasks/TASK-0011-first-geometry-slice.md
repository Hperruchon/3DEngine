# TASK-0011 — First geometry slice: `CreateBox` end-to-end (P7a)

## Status

Done — shipped in commit `6114819`. See `docs/CURRENT-STATE.md` v0.11.

## Context

P7 (Manifold backend wiring) is the next phase per `docs/roadmap.md`. The work splits cleanly: P7a wires the *posture* (capability surface, handler-to-backend access, schema declaration, body projection, snapshot extension, replay determinism) behind a managed in-process stub backend; P7b later swaps Manifold in behind the same surface. This TASK is P7a.

Two ADRs land alongside this work and pre-decide every load-bearing call:

- **ADR-0012** — Geometry backend wiring (V1). Capability method shapes, handler-to-backend parameter, `Document.Bodies` projection, deterministic body handles, `body.created` event, `subscription.reset` snapshot extension, `E-GEOM-CAP-MISSING` diagnostic, V1.x first-backend = in-process managed stub.
- **ADR-0013** — Command/query schema declaration. Schema lives on the handler (`Parameters` + `Outputs`); `FieldSchema` moves to `Engine.Contracts/Schema/`; `SchemaCommandsEndpoint.Item` becomes a pure projection; gate tightens to compare endpoint vs. handler.

Both are Accepted on 2026-05-27. ADR-0001's existing rules (capability-typed backends, opaque `BodyHandle`, kernel API internal to handlers, replay against fresh backend) remain in force.

## Goal

Ship a single command (`CreateBox`) and a single query (`GetBoundingBox`) end-to-end through CLI + HTTP, with every load-bearing piece of ADR-0012 and ADR-0013 wired and gate-tested. Replay-determinism fixture extends to cover the new command. `subscription.reset` snapshot grows the `bodies` array.

After this TASK, the engine can create a box, observe its creation through the event stream, and answer "what's its bounding box?" — through every surface (CLI, HTTP, WebSocket).

## Scope (in)

### 1. Engine.Contracts changes (gated by ADR-0012 + ADR-0013)

- `Engine.Contracts/Geometry/IGeometryBackend.cs` — replace marker with:
  ```csharp
  public interface IGeometryBackend
  {
      BackendCapabilities Capabilities { get; }
      T? TryGet<T>() where T : class;
  }
  ```
- `Engine.Contracts/Geometry/BackendCapabilities.cs` — `[Flags]` enum: `None, Mesh, Query` (V1).
- `Engine.Contracts/Geometry/IMeshOps.cs` — add `void CreateBox(BodyHandle handle, BoxParameters parameters);`.
- `Engine.Contracts/Geometry/IGeometryQuery.cs` — add `Aabb GetBoundingBox(BodyHandle handle);`.
- `Engine.Contracts/Geometry/BoxParameters.cs` — `public readonly record struct BoxParameters(double SizeX, double SizeY, double SizeZ)`.
- `Engine.Contracts/Geometry/Aabb.cs` — `public readonly record struct Aabb(double MinX, double MinY, double MinZ, double MaxX, double MaxY, double MaxZ)`.
- `Engine.Contracts/BodyRecord.cs` — `public sealed record BodyRecord(BodyHandle Handle, string Kind)`.
- `Engine.Contracts/Document.cs` — add `Bodies` projection:
  ```csharp
  private readonly Dictionary<Guid, BodyRecord> _bodies = new();
  public IReadOnlyCollection<BodyRecord> Bodies => _bodies.Values;
  internal void AddBody(BodyRecord body);
  ```
- `Engine.Contracts/Schema/FieldSchema.cs` — move from `Engine.Api.Http/Schema/SchemaTypes.cs`:
  ```csharp
  public sealed record FieldSchema(string Type, bool Required = false);
  ```
- `Engine.Contracts/Handlers/ICommandHandler.cs`:
  - Add `Handle` `IGeometryBackend backend` parameter.
  - Add `IReadOnlyDictionary<string, FieldSchema> Parameters { get; }`.
  - Add `IReadOnlyDictionary<string, FieldSchema> Outputs { get; }`.
  - Extend `CommandHandlerResult` to carry `IReadOnlyList<BodyRecord> CreatedBodies` (defaults to empty). The bus appends these to `Document.Bodies` inside the commit section.
- `Engine.Contracts/Handlers/IQueryHandler.cs` — add `Parameters` + `Result` schema properties. (Handler signature is settled when the first query lands; this TASK declares the first query, so settle it here — see §4.)

### 2. Engine.Core changes

- `Engine.Core/CommandBus.cs`:
  - Constructor gains non-optional `IGeometryBackend backend` parameter.
  - `ApplyOnce` passes `backend` to `handler.Handle(command, _document, backend, ct)`.
  - Commit section: after `_document.AppendCommand(command)` and before `AdvanceVersion`, iterate `handlerResult.CreatedBodies` and call `_document.AddBody(record)`. For each body, emit a `body.created` event before `AdvanceVersion` (so all events for the commit are on consecutive Seqs, terminated by version advance).
- `Engine.Core/Replay.cs`:
  - `ReplayLog(log, document, registry, sink, backend)` — add backend parameter; pass through.
- `Engine.Core/Commands/NoOpCommand.cs` — unchanged.
- `Engine.Core/Commands/NoOpCommandHandler.cs`:
  - Add `Parameters = { "echo": new FieldSchema("string", Required: true) }`.
  - Add `Outputs = { "echo": new FieldSchema("string") }`.
  - Accept the new `IGeometryBackend backend` parameter; ignore it.
- `Engine.Core/Geometry/NullGeometryBackend.cs` — new. `Capabilities = None`, `TryGet<T>() => null`. The bus accepts this when no real backend is wired (e.g., NoOp-only tests).
- `Engine.Core/Geometry/InProcessMeshBackend.cs` — new, the V1.x first backend per ADR-0012 §7:
  - `Capabilities = Mesh | Query`.
  - `TryGet<T>()` returns `this as T` when `T` is `IMeshOps` or `IGeometryQuery`.
  - Internal `Dictionary<Guid, BoxRecord>` where `BoxRecord` is `(BodyHandle, BoxParameters)`.
  - `IMeshOps.CreateBox(handle, params)` stores the box; throws if handle is already present (programmer error from the bus side — `CommandId` collisions can't happen within a single replay; treat as unrecoverable).
  - `IGeometryQuery.GetBoundingBox(handle)` returns axis-aligned bbox from the stored params (centered at origin: `(-X/2, -Y/2, -Z/2)` to `(+X/2, +Y/2, +Z/2)`).
- `Engine.Core/Commands/CreateBoxCommand.cs` — new:
  ```csharp
  public sealed record CreateBoxCommand : Command
  {
      public override string Name => "CreateBox";
      public override int SchemaVersion => 1;
      public required double SizeX { get; init; }
      public required double SizeY { get; init; }
      public required double SizeZ { get; init; }
  }
  ```
- `Engine.Core/Commands/CreateBoxCommandHandler.cs` — new:
  - `Parameters = { "sizeX": new("number", Required: true), "sizeY": new("number", Required: true), "sizeZ": new("number", Required: true) }`.
  - `Outputs = { "bodyId": new("guid") }`.
  - `Handle`:
    1. Cast to `CreateBoxCommand`.
    2. Validate size > 0 on each axis; on failure, return `CommandHandlerResult.Failure(new ErrorDetail("E-GEOM-INVALID-PARAM", ...))`.
    3. `var mesh = backend.TryGet<IMeshOps>();` — if null, `Failure("E-GEOM-CAP-MISSING", ...)`.
    4. `var handle = new BodyHandle(command.CommandId);`
    5. `mesh.CreateBox(handle, new BoxParameters(SizeX, SizeY, SizeZ));`
    6. Return `Success` with `Outputs { "bodyId": handle.Id }` and `CreatedBodies = [ new BodyRecord(handle, "Box") ]`.
- `Engine.Core/Queries/GetBoundingBoxQuery.cs` — new query record + handler. Handler reads `IGeometryQuery` from the backend; returns `Aabb` via `QueryResult<Aabb>`.
- `Engine.Core/DiagnosticCodes.cs` — add constants for `E-GEOM-CAP-MISSING` and `E-GEOM-INVALID-PARAM`.

### 3. Engine.Cli changes

- `Engine.Cli/Program.cs` (or wherever bus is constructed): wire `InProcessMeshBackend` into the `CommandBus` constructor. Register `CreateBoxCommandHandler` and `GetBoundingBoxQueryHandler`.
- `engine apply CreateBox --sizeX 10 --sizeY 20 --sizeZ 30` produces JSON `CommandResult` with `Outputs.bodyId`.
- `engine query GetBoundingBox --bodyId <guid>` produces JSON `QueryResult<Aabb>`.

### 4. Engine.Api.Http changes

- `Engine.Api.Http/EngineHost.cs`: construct `InProcessMeshBackend`; pass to `CommandBus` constructor. Register the new command + query handlers.
- `Engine.Api.Http/Schema/SchemaTypes.cs`: delete the internal `FieldSchema` record (moved to contracts). `CommandSchemaItem` / `QuerySchemaItem` retype `Parameters` / `Outputs` to `Engine.Contracts.Schema.FieldSchema`.
- `Engine.Api.Http/Endpoints/SchemaCommandsEndpoint.cs`: delete the `if (name == "NoOp" && ...)` switch; project from handler:
  ```csharp
  if (!host.CommandRegistry.TryFind(name, version, out var handler))
      return ApiErrorEnvelope.NotFound(...);
  return Results.Json(new CommandSchemaItem(
      handler.CommandName, handler.SchemaVersion,
      handler.Parameters, handler.Outputs), ApiJson.Options);
  ```
- `Engine.Api.Http/Endpoints/SchemaQueriesEndpoint.cs`: same projection pattern; per-query item endpoint becomes registry-driven.
- `Engine.Api.Http/WebSockets/SnapshotProjector.cs`: extend the snapshot DTO with `bodies: [...]` per ADR-0012 §6 / ADR-0010 §3. Empty array when no bodies.
- `Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs`: mirror new codes `E-GEOM-CAP-MISSING`, `E-GEOM-INVALID-PARAM`.
- `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs`: append `body.created` to the hand-encoded event-kind list (per TASK-0009's "will become registry-driven when an event registry lands" — that registry is still not built; for now, this entry stays hand-encoded alongside the others).
- `CommandRegistry.TryFind` accessor must be public or have a sibling that returns the handler — needed by the new schema projection. If not yet public, expose it minimally; document in the same change.

### 5. Diagnostic codes registered (three places)

- `docs/diagnostics.md` — append rows for `E-GEOM-CAP-MISSING` and `E-GEOM-INVALID-PARAM`. Add `GEOM` subsystem token reference (already in §Conventions).
- `Engine.Core/DiagnosticCodes.cs` — add constants.
- `Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs` — mirror entries.

### 6. Tests (in `Engine.Tests/`)

**Geometry kernel:**

- `Engine.Tests/Geometry/InProcessMeshBackendTests.cs`:
  - `Capabilities_Are_Mesh_And_Query`.
  - `TryGet_Returns_Self_For_Mesh_Ops`.
  - `TryGet_Returns_Self_For_Geometry_Query`.
  - `TryGet_Returns_Null_For_BRep_Ops`.
  - `CreateBox_Stores_Body_Under_Given_Handle`.
  - `GetBoundingBox_Returns_Axis_Aligned_Box_Centered_At_Origin`.

**Command path:**

- `Engine.Tests/Commands/CreateBoxCommandTests.cs`:
  - `CreateBox_Succeeds_With_Valid_Params_Emits_Body_Created_Event`.
  - `CreateBox_Adds_Body_To_Document_Bodies`.
  - `CreateBox_With_Zero_Size_Returns_E_GEOM_INVALID_PARAM`.
  - `CreateBox_With_Negative_Size_Returns_E_GEOM_INVALID_PARAM`.
  - `CreateBox_When_Backend_Lacks_Mesh_Ops_Returns_E_GEOM_CAP_MISSING` (use `NullGeometryBackend`).
  - `Body_Handle_Equals_CommandId` — deterministic handle.

**Query path:**

- `Engine.Tests/Queries/GetBoundingBoxQueryTests.cs`:
  - `GetBoundingBox_Returns_Stored_Aabb_For_Existing_Body`.
  - `GetBoundingBox_For_Unknown_Body_Returns_Error_Detail`.

**Replay determinism:**

- `Engine.Tests/Replay/ReplayDeterminismFixtureTests.cs` — extend the existing fixture (per TASK-0005). Add a `CreateBox` step between two `NoOp`s; assert two replays produce byte-identical `Document.Bodies` AND identical event sequences.

**Schema declaration:**

- `Engine.Tests/Http/SchemaEndpointGateTests.cs` — REWRITE the `Every_Registered_Command_Has_A_Schema_Entry` test:
  - For each registered handler, fetch `/schema/commands/{name}@{version}`.
  - Assert returned JSON's `parameters` and `outputs` structurally equal `handler.Parameters` and `handler.Outputs`.
- Add `Every_Registered_Query_Has_A_Schema_Entry` mirror — for `GetBoundingBox`.
- Add `SchemaCommandsEndpoint_Source_Contains_No_Per_Command_Branching`:
  - Read `Engine.Api.Http/Endpoints/SchemaCommandsEndpoint.cs` as text.
  - Assert it contains no string literal matching `"CreateBox"` or `"NoOp"` or `"GetBoundingBox"`. The handler-driven projection has no per-command knowledge.

**Snapshot extension:**

- `Engine.Tests/Http/EventsEndpointWireShapeTests.cs` — extend `Subscription_Reset_Snapshot_Matches_ADR_0010_Shape`:
  - Add an assertion that `snapshot.bodies` is present (possibly empty array).
- Add `Subscription_Reset_After_CreateBox_Has_Bodies_Array_With_Created_Body`:
  - Apply a `CreateBox`, subscribe with a stale/null cursor, assert `snapshot.bodies` contains the body with correct `handle` and `kind: "Box"`.

**End-to-end:**

- `Engine.Tests/Cli/CliCreateBoxScenarioTests.cs`:
  - `Apply_CreateBox_Then_Query_GetBoundingBox_Returns_Expected_Aabb`.
- `Engine.Tests/Http/HttpCreateBoxScenarioTests.cs`:
  - `Apply_CreateBox_Via_Post_Then_Query_GetBoundingBox_Via_Post_Returns_Expected_Aabb`.

### 7. Documentation

- `CLAUDE.md` — drop the "No concrete geometry backend" V1 clamp. (The stub IS the concrete backend for V1.x; Manifold-vs-stub is an implementation choice for P7b.)
- `docs/CURRENT-STATE.md` — v0.11 entry.
- `docs/roadmap.md`:
  - Move `P7 — Manifold backend wiring` from V1.x Pending. P7a (this TASK) lands as Shipped under V1.x. Add a new V1.x Pending bullet `P7b — Manifold backend (swap-in)`.
- `docs/diagnostics.md` — appended rows for the two new codes.

## Scope (out)

- **Manifold native interop.** Deferred to P7b under a follow-on ADR. The stub backend satisfies the V1.x slice.
- **Boolean operations, transforms, fillets, tessellation-export.** Future capability additions; each lands under its own sized TASK + (if it adds methods to a capability interface) an ADR amendment.
- **Multi-body commands.** ADR-0012 §4 sketched `Guid.Create(commandId, ordinal)`; not exercised here because no V1 command produces multiple bodies.
- **Per-body metadata beyond `Kind`.** Names, layers, visibility — additive future TASKs.
- **Body deletion / mutation commands.** Out of phase. When they land, `Document.Bodies` mutation rules extend; the `BodyRecord` projection may grow.
- **Event-registry refactor for `/schema/events`.** Still hand-encoded; the new `body.created` Kind appends to the existing hand list. The registry-driven event surface is its own future TASK.
- **Backend hot-swap.** ADR-0012 §Open challenges.
- **Tessellated preview for clients.** ADR-0012 §Open challenges.
- **Test for the WebSocket heartbeat.** Tracked separately (TASK-0010 §Notes); not folded in here.
- **`E-CMD-BUS-BUSY` activation.** Still reserved.

## Inputs

- ADR-0001 — geometry kernel posture (capability-typed backends, opaque `BodyHandle`, kernel API internal).
- ADR-0005 — event stream; `body.created` is a new Kind under the existing contract.
- ADR-0006 — command execution; commit-at-end serial section is where `Document.Bodies` mutation happens.
- ADR-0008 — Command/Query/Event triad; `CommandResult.Outputs` shape; schema endpoint contract.
- ADR-0010 — snapshot format; `bodies` field is the §3 extension.
- ADR-0011 — server-default deployment; one backend per process.
- **ADR-0012 — Geometry backend wiring (V1)** (Accepted this set).
- **ADR-0013 — Command/query schema declaration** (Accepted this set).
- TASK-0001 — `CommandBus`, `Document`, replay.
- TASK-0005 — replay-determinism fixture extends.
- TASK-0007 — `Engine.Api.Http` scaffold; `EngineHost`, `ApiJson`.
- TASK-0009 — schema endpoints (hand-coded switch deleted here).
- TASK-0010 — `subscription.reset` snapshot (extended here).

## Outputs

- `engine apply CreateBox --sizeX 10 --sizeY 20 --sizeZ 30` returns Applied `CommandResult` with `Outputs.bodyId`.
- `engine query GetBoundingBox --bodyId <guid>` returns the bbox.
- `POST /commands` with a `CreateBox` body produces the same `CommandResult` as the CLI (parity per ADR-0011).
- `POST /queries` with `GetBoundingBox` produces the same `QueryResult`.
- `/schema/commands/CreateBox@1` returns the handler-declared schema. `/schema/commands/NoOp@1` continues to work (now also handler-projected).
- `/schema/queries/GetBoundingBox@1` returns the handler-declared schema.
- `/schema/diagnostics` mirrors the two new GEOM codes.
- `/schema/events` lists `body.created`.
- A WebSocket subscriber that reconnects after a `CreateBox` receives a `subscription.reset` whose `snapshot.bodies` includes the new body.
- The replay-determinism fixture covers `CreateBox`; two replays match byte-for-byte.
- `dotnet build` + `dotnet test` green.
- `docs/CURRENT-STATE.md` v0.11 entry.
- `docs/roadmap.md` shows P7a Shipped V1.x; P7b new Pending bullet.
- `CLAUDE.md` no longer mentions the geometry-backend V1 clamp.

## Files

**Created:**
- `Engine.Contracts/Geometry/BackendCapabilities.cs`
- `Engine.Contracts/Geometry/BoxParameters.cs`
- `Engine.Contracts/Geometry/Aabb.cs`
- `Engine.Contracts/BodyRecord.cs`
- `Engine.Contracts/Schema/FieldSchema.cs`
- `Engine.Core/Geometry/NullGeometryBackend.cs`
- `Engine.Core/Geometry/InProcessMeshBackend.cs`
- `Engine.Core/Commands/CreateBoxCommand.cs`
- `Engine.Core/Commands/CreateBoxCommandHandler.cs`
- `Engine.Core/Queries/GetBoundingBoxQuery.cs`
- `Engine.Core/Queries/GetBoundingBoxQueryHandler.cs`
- `Engine.Tests/Geometry/InProcessMeshBackendTests.cs`
- `Engine.Tests/Commands/CreateBoxCommandTests.cs`
- `Engine.Tests/Queries/GetBoundingBoxQueryTests.cs`
- `Engine.Tests/Cli/CliCreateBoxScenarioTests.cs`
- `Engine.Tests/Http/HttpCreateBoxScenarioTests.cs`
- `tasks/TASK-0011-first-geometry-slice.md` (this file)

**Modified:**
- `Engine.Contracts/Geometry/IGeometryBackend.cs` — methods.
- `Engine.Contracts/Geometry/IMeshOps.cs` — `CreateBox`.
- `Engine.Contracts/Geometry/IGeometryQuery.cs` — `GetBoundingBox`.
- `Engine.Contracts/Document.cs` — `Bodies` projection + `AddBody`.
- `Engine.Contracts/Handlers/ICommandHandler.cs` — schema properties + `IGeometryBackend` parameter on `Handle`; extend `CommandHandlerResult` with `CreatedBodies`.
- `Engine.Contracts/Handlers/IQueryHandler.cs` — schema properties + settle `Handle` signature.
- `Engine.Core/CommandBus.cs` — backend ctor param; pass to handler; commit `CreatedBodies` + emit `body.created`.
- `Engine.Core/Replay.cs` — backend parameter.
- `Engine.Core/CommandRegistry.cs` — public/internal accessor that exposes handlers for the schema projection.
- `Engine.Core/QueryRegistry.cs` — same.
- `Engine.Core/Commands/NoOpCommandHandler.cs` — schema declarations + new parameter.
- `Engine.Core/DiagnosticCodes.cs` — `E-GEOM-CAP-MISSING`, `E-GEOM-INVALID-PARAM`.
- `Engine.Cli/Program.cs` — wire `InProcessMeshBackend`, register handlers.
- `Engine.Api.Http/EngineHost.cs` — wire backend, register handlers.
- `Engine.Api.Http/Schema/SchemaTypes.cs` — delete `FieldSchema`; retype.
- `Engine.Api.Http/Endpoints/SchemaCommandsEndpoint.cs` — registry-driven projection.
- `Engine.Api.Http/Endpoints/SchemaQueriesEndpoint.cs` — same.
- `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` — append `body.created`.
- `Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs` — mirror new codes.
- `Engine.Api.Http/WebSockets/SnapshotProjector.cs` — extend with `bodies`.
- `Engine.Tests/Http/SchemaEndpointGateTests.cs` — rewrite per §6.
- `Engine.Tests/Http/EventsEndpointWireShapeTests.cs` — snapshot `bodies` assertions.
- `Engine.Tests/ReplayDeterminism*Tests.cs` (existing fixture file) — extend with `CreateBox`.
- `CLAUDE.md` — drop V1 geometry-backend clamp.
- `docs/diagnostics.md` — two new rows.
- `docs/CURRENT-STATE.md` — v0.11 entry.
- `docs/roadmap.md` — P7a shipped; P7b new Pending.
- `tasks/TASK-0011-first-geometry-slice.md` — Status flip in close commit.

**Do not touch:**
- ADRs 0001–0011 (in force; not amended).
- `3DEngine/`, `BlazorApp/`, `3DEngine.Core/`, `Vortice.Vulkan.*` (per CLAUDE.md "Do not touch").
- The WebSocket transport (`EventsEndpoint`, `EventBroadcaster`, etc.) — only `SnapshotProjector` extends; the broadcaster + handshake are unchanged.

## Tests

(Listed under §6. Existing 70 + ~14 new = ~84 total after this TASK.)

## Acceptance criteria

1. `dotnet build` succeeds.
2. `dotnet test` passes — all existing 70 plus the new tests.
3. `BodyHandle` for a given `CommandId` is deterministic and equal across replays.
4. `Document.Bodies` is mutated only inside `CommandBus.Apply`'s commit section (negative test or convention; review at PR time).
5. `subscription.reset.snapshot.bodies` is present (possibly empty) for every reset emitted after this TASK.
6. `/schema/commands/{name}@{version}` for every registered command equals the projection of the handler's declared `Parameters` / `Outputs`. Same for `/schema/queries/...`.
7. `SchemaCommandsEndpoint.cs` source contains no per-command name literals.
8. Two new diagnostic codes registered in the three required places.
9. `body.created` Kind appears in `/schema/events`.
10. CLAUDE.md no longer mentions the geometry-backend V1 clamp.
11. `docs/CURRENT-STATE.md` v0.11 entry exists.
12. `docs/roadmap.md` shows P7a Shipped V1.x; P7b is a new Pending bullet.

## Notes for the implementer

- **One implementation commit, then one close commit.** Same cadence as v0.1..v0.10. Close commit flips `Status` to `Done — shipped in <impl-hash>` and updates `CURRENT-STATE.md` reference.
- **Order of changes in the impl commit.** Probably: contracts first (compile breakage flares everywhere), then core, then handlers, then API/CLI hosts, then tests. The compile errors guide the order; nothing fancy.
- **Backend in the bus constructor.** Existing callers (`Engine.Cli`, `Engine.Api.Http`, all tests that build a bus) must pass *something*. `NullGeometryBackend.Instance` is fine for NoOp-only paths. The first slice ships only one real backend.
- **Replay determinism fixture.** The current fixture (TASK-0005) is short — extending it with one `CreateBox` between the two `NoOp`s is enough. Don't rebuild the fixture; append. The hand-authored snapshot grows by one `command.applied` + one `body.created` + one `command.applied` for the trailing NoOp.
- **`body.created` event ordering inside a commit.** ADR-0005 fixes monotonic Seq per Document; a single command commit can emit multiple events on consecutive Seqs. `CreateBox` emits `command.applied` first (preserves the existing pattern), then `body.created`. `Document.Version` advances once at end of commit, to the highest Seq emitted.
- **Snapshot serialization order.** `snapshot.bodies` should be ordered by insertion (Dictionary preserves insertion order in .NET); tests that depend on order should sort by `handle` explicitly to stay stable.
- **CLI argument parsing for doubles.** Current CLI parses string-typed params for NoOp. `CreateBox` needs numeric parsing — `double.TryParse` with invariant culture. Reject malformed input with exit code 2 (invalid usage).
- **HTTP body shape for `CreateBox`.** `{ "sizeX": 10, "sizeY": 20, "sizeZ": 30 }`. Standard JSON deserialization. Pre-existing `ApiJson.Options` handles it.
- **No new event-registry abstraction.** TASK-0009 hand-encoded the event kinds list. Append `body.created`; do not redesign. The registry refactor is its own future TASK when the event registry actually exists.
- **`CommandRegistry`'s `Registered` accessor returns `(Name, SchemaVersion)` tuples** (per TASK-0009). The schema projection needs the actual `ICommandHandler`. Add a public `TryFind` or sibling accessor that returns the handler; keep the surface minimal.
- **The hand-known switch deletion is gate-enforced.** `SchemaCommandsEndpoint_Source_Contains_No_Per_Command_Branching` is a textual gate; if a future change re-introduces an `if (name == "...")` branch in that file, the gate fails. Read it before editing the endpoint.
- **No `Engine.Contracts` changes beyond what ADR-0012 + ADR-0013 prescribe.** If a need arises, stop and flag — those are gated by ADR.
