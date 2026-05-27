# TASK-0013 — Engine hosting factory (shared wiring)

## Status

Ready

## Context

`Engine.Cli/Cli.BuildEngine` and `Engine.Api.Http/EngineHost` both wire the engine the same way: construct `Document`, `CommandRegistry`, `QueryRegistry`, register the same set of handlers (`NoOpCommandHandler`, `CreateBoxCommandHandler`, `GetBoundingBoxQueryHandler`), construct `InMemoryEventSink`, construct `InProcessMeshBackend`, construct `CommandBus` and `QueryBus`. The only meaningful divergence is that the HTTP host additionally wraps the sink in `BroadcastingEventSink` (TASK-0010) before passing it to the bus.

Today the wiring is duplicated — two near-identical bring-up sequences in two clients. When a third client lands (e.g., the eventual `BlazorApp.Client` per ADR-0011 §4, or any future embedded host), it would copy-paste the same sequence. When a new default command lands (e.g., a future primitive), the registration call must be added to every client.

The duplication is **within the rules** of CLAUDE.md (clients reference only `Engine.Core` and `Engine.Contracts`; `Engine.Core.Geometry.InProcessMeshBackend` is a sub-namespace of `Engine.Core`), but it is a code smell flagged during the TASK-0012 review of `EngineHost`'s `using Engine.Core.Geometry` import. This TASK eliminates it before TASK-0012 (persistence) lands a third site that would amplify the smell.

This is a pure refactor: no behavioural change to commands, queries, events, schemas, or the wire surface. Existing tests pass without modification.

## Goal

Introduce a single shared engine-wiring factory in `Engine.Core` that both `Engine.Cli` and `Engine.Api.Http` consume. After this TASK:

- Adding a new default command/query handler updates one file (the factory's default registration), not two.
- A new client picks up the default wiring with one method call.
- `Engine.Api.Http/EngineHost` and `Engine.Cli/Cli.BuildEngine` shrink to a handful of lines each.
- The reference backend (`InProcessMeshBackend`) is still selected by the factory; client-specific backend swaps remain possible by constructing the bus directly from the factory's returned kit.

## Scope (in)

### 1. Engine.Core changes

- `Engine.Core/Hosting/EngineHosting.cs` — new static factory:
  ```csharp
  public static class EngineHosting
  {
      // V1.x default wiring. Builds a fresh Document, registries with all
      // V1.x default handlers, an in-memory event sink, and an
      // InProcessMeshBackend. Does NOT construct CommandBus/QueryBus —
      // callers do that themselves so they can optionally decorate the
      // sink (HTTP host wraps in BroadcastingEventSink) or swap the
      // backend.
      public static EngineKit CreateDefault();

      // Exposed separately so a host that wants to use its own
      // registries (e.g., to add host-specific commands) can call these
      // to install just the V1.x defaults.
      public static void RegisterDefaultCommands(CommandRegistry registry);
      public static void RegisterDefaultQueries(QueryRegistry registry);
  }

  public sealed record EngineKit(
      Document Document,
      CommandRegistry CommandRegistry,
      QueryRegistry QueryRegistry,
      InMemoryEventSink Events,
      InProcessMeshBackend Backend);
  ```
- The factory does NOT construct `CommandBus`/`QueryBus`. The caller composes them from the kit. Rationale:
  - HTTP host wraps `Events` in `BroadcastingEventSink` before passing to `CommandBus`.
  - CLI uses `Events` directly.
  - Future hosts may layer additional decorators (logging, metrics).
  - Returning a bus would force a fixed sink choice, defeating the abstraction.
- `RegisterDefaultCommands` / `RegisterDefaultQueries` are split out so a host that already has a registry (perhaps with host-specific handlers added) can call them to layer in V1.x defaults.

### 2. Engine.Cli changes

- `Engine.Cli/Cli.cs`:
  - Delete `using Engine.Core.Commands;`, `using Engine.Core.Geometry;`, `using Engine.Core.Queries;` from the `BuildEngine` perspective — those imports are still needed for the per-command switch (which Q2 in a future TASK will fix; this TASK does NOT touch that switch).
  - `BuildEngine` becomes:
    ```csharp
    private static (CommandBus Commands, QueryBus Queries) BuildEngine()
    {
        var kit = EngineHosting.CreateDefault();
        var commandBus = new CommandBus(kit.Document, kit.CommandRegistry, kit.Events, kit.Backend);
        var queryBus = new QueryBus(kit.Document, kit.QueryRegistry, kit.Backend);
        return (commandBus, queryBus);
    }
    ```
  - Net: ~10 lines deleted, ~5 lines added. The per-command construction switch (`case "NoOp": ... case "CreateBox": ...`) stays — that is Q2's concern, deferred to TASK-0012 resumption + ADR-0015 amendment.

### 3. Engine.Api.Http changes

- `Engine.Api.Http/EngineHost.cs`:
  - Remove the `using Engine.Core.Commands;`, `using Engine.Core.Geometry;`, `using Engine.Core.Queries;` lines — no longer needed in this file once the factory is the only construction site.
  - Constructor becomes:
    ```csharp
    public EngineHost(EventBroadcaster broadcaster)
    {
        var kit = EngineHosting.CreateDefault();
        Document = kit.Document;
        CommandRegistry = kit.CommandRegistry;
        QueryRegistry = kit.QueryRegistry;
        Events = kit.Events;
        Backend = kit.Backend;
        var broadcastingSink = new BroadcastingEventSink(kit.Events, broadcaster);
        CommandBus = new CommandBus(kit.Document, kit.CommandRegistry, broadcastingSink, kit.Backend);
        QueryBus = new QueryBus(kit.Document, kit.QueryRegistry, kit.Backend);
    }
    ```
  - The public properties (`Document`, `CommandRegistry`, etc.) are preserved so existing endpoint code is untouched.
  - The `Backend` property type stays `InProcessMeshBackend` (the concrete type). Endpoints don't currently use it polymorphically; if a future TASK switches it to `IGeometryBackend`, that's a separate concern.

### 4. Tests

- `Engine.Tests/Hosting/EngineHostingTests.cs` — new:
  - `CreateDefault_Returns_Fresh_Document_Each_Call` — assert two calls yield different `DocumentId`s.
  - `CreateDefault_Registers_NoOp_Handler`.
  - `CreateDefault_Registers_CreateBox_Handler`.
  - `CreateDefault_Registers_GetBoundingBox_Query_Handler`.
  - `CreateDefault_Provides_InProcessMeshBackend_With_Mesh_And_Query_Capabilities`.
  - `RegisterDefaultCommands_Adds_NoOp_And_CreateBox_To_Empty_Registry`.
  - `RegisterDefaultQueries_Adds_GetBoundingBox_To_Empty_Registry`.
- Existing tests stay green without modification (the refactor is behaviour-preserving).

### 5. Documentation

- `docs/CURRENT-STATE.md` — append v0.12 entry: "Engine hosting factory (refactor; no behavioural change; TASK-0013). New `Engine.Core/Hosting/EngineHosting.cs` exposes `CreateDefault()` + `RegisterDefaultCommands`/`RegisterDefaultQueries`. `Engine.Cli/Cli.BuildEngine` and `Engine.Api.Http/EngineHost` migrated; per-client bring-up sequences shrink to a kit destructuring + bus construction. From this point, TASK numbers no longer track CURRENT-STATE version numbers (TASK-0013 ships as v0.12; the pre-drafted TASK-0012 will ship later as v0.13). `dotnet build` + `dotnet test` green."
- No roadmap entry — refactors are housekeeping, not phase work.
- No ADR — pure `Engine.Core` change, no new contract, no boundary impact.
- No new diagnostic codes.
- No `Engine.Contracts/**` change.

## Scope (out)

- **Q2 (handler-owned command construction).** Deferred to TASK-0012 resumption + ADR-0015 (amends 0013). This TASK does NOT touch the per-command switches in `Cli.Apply`, `CommandsEndpoint.BuildXxx`, or my pending `CommandCodec.DeserializeCommand`. Q2 collapses all three; doing it here would expand scope and require an ADR.
- **Multi-backend selection.** The factory hard-wires `InProcessMeshBackend`. Future TASKs (P7b Manifold swap-in, or a hypothetical config-driven backend selection) override the backend by constructing the bus directly from the kit. The factory's job is the default; clients keep their override path.
- **DI container integration.** No new DI abstractions. `EngineHost` is registered via `AddSingleton<EngineHost>` (existing); the factory is called inside `EngineHost`'s constructor, not registered separately.
- **Renaming or moving `InProcessMeshBackend`.** Stays in `Engine.Core/Geometry/` per ADR-0012 §7. A future "extract backends into their own assembly" call is V2 territory.
- **Migrating tests** that construct buses directly (e.g., `CommandBusTests` builds its own bus with custom handlers). Those tests are isolated unit tests; the factory is a host-convenience, not a test-replacement.
- **CURRENT-STATE entry for the in-progress TASK-0012 work.** TASK-0012's Checkpoint 1 files (Engine.Core/Persistence/*.cs) stay on disk uncommitted across this TASK's commits. They are not referenced by anything yet; the refactor here does not touch them.

## Inputs

- ADR-0001 — geometry backend posture (the factory picks the V1.x reference backend).
- ADR-0009 — `3DEngine.Core` peer kernel boundary (unaffected; the factory is in `Engine.Core`, not `3DEngine.Core`).
- ADR-0011 — server-default deployment (the factory serves both server and embedded clients).
- ADR-0012 — geometry backend wiring (the factory's backend choice honors §7).
- TASK-0007 — `EngineHost` shape (migrated here).
- TASK-0010 — `BroadcastingEventSink` (still wrapped in `EngineHost`; unchanged).
- TASK-0011 — `CreateBoxCommandHandler`, `InProcessMeshBackend` (factory registers them).

## Outputs

- `Engine.Core/Hosting/EngineHosting.cs` exists and exposes `CreateDefault`, `RegisterDefaultCommands`, `RegisterDefaultQueries`.
- `Engine.Cli/Cli.BuildEngine` is ≤5 lines plus the bus construction.
- `Engine.Api.Http/EngineHost` constructor uses `EngineHosting.CreateDefault()`; `using Engine.Core.Geometry`, `using Engine.Core.Commands`, `using Engine.Core.Queries` removed from `EngineHost.cs`.
- `dotnet build` + `dotnet test` green. No behavioural change to any command/query/event/schema.
- `docs/CURRENT-STATE.md` v0.12 entry.
- No `Engine.Contracts/**` changes.
- No new diagnostic codes.

## Files

**Created:**
- `Engine.Core/Hosting/EngineHosting.cs`
- `Engine.Tests/Hosting/EngineHostingTests.cs`
- `tasks/TASK-0013-engine-hosting-factory.md` (this file)

**Modified:**
- `Engine.Cli/Cli.cs` — `BuildEngine` uses factory; redundant `using` lines for namespaces no longer referenced from `BuildEngine` removed (commands/queries `using` lines stay because the `case "NoOp"` / `case "CreateBox"` / `case "GetBoundingBox"` blocks still reference those types).
- `Engine.Api.Http/EngineHost.cs` — constructor uses factory; `using Engine.Core.Commands`, `using Engine.Core.Geometry`, `using Engine.Core.Queries` removed.
- `docs/CURRENT-STATE.md` — v0.12 entry.
- `tasks/TASK-0013-engine-hosting-factory.md` — Status flip in close commit.

**Do not touch:**
- `Engine.Contracts/**` — no contract change.
- ADRs — no decision change.
- `3DEngine/`, `BlazorApp/`, `3DEngine.Core/`, `Vortice.Vulkan.*` (CLAUDE.md "Do not touch").
- TASK-0012's Checkpoint 1 files (`Engine.Core/Persistence/*.cs`, the DiagnosticCodes additions) — they stay uncommitted on disk across this TASK's commits. They are not built into the kit; they have no consumers yet.
- The per-command switches in `Cli.Apply` and `CommandsEndpoint`. Q2 territory; deferred to TASK-0012 resumption.
- All endpoint code (`CommandsEndpoint`, `QueriesEndpoint`, `EventsEndpoint`, etc.) — unchanged.
- `docs/diagnostics.md` — no new codes.

## Tests

(Listed under §4. Existing ≈84 tests stay green; ≈7 new tests for the factory. Final count is a check, not a target.)

## Acceptance criteria

1. `dotnet build` succeeds.
2. `dotnet test` passes — all existing tests plus the new factory tests.
3. `Engine.Api.Http/EngineHost.cs` has no `using Engine.Core.Commands`, `using Engine.Core.Geometry`, or `using Engine.Core.Queries` lines.
4. `Engine.Cli/Cli.BuildEngine` does not call `new InProcessMeshBackend()` directly — it goes through the factory.
5. Two calls to `EngineHosting.CreateDefault()` produce two independent kits (different `DocumentId`s, separate registries).
6. The factory installs the same handler set that the pre-refactor `BuildEngine` / `EngineHost` constructors installed (`NoOp`, `CreateBox`, `GetBoundingBox`).
7. The HTTP host still wraps the kit's `Events` in `BroadcastingEventSink` before constructing `CommandBus`.
8. `docs/CURRENT-STATE.md` v0.12 entry exists and notes the TASK-number / version-number divergence.

## Notes for the implementer

- **One implementation commit, then one close commit.** Same cadence as v0.1..v0.11.
- **Order of changes.** Factory first (compiles standalone), then `EngineHost` migration, then `Cli.BuildEngine` migration, then factory tests, then CURRENT-STATE.
- **Sink decorator pattern.** Deliberately NOT introduced. The HTTP host knows it wants `BroadcastingEventSink` and wraps explicitly. Adding an `IEventSinkDecorator` abstraction would be premature — no second decorator exists.
- **`EngineKit` exposes concrete types, not interfaces.** `InProcessMeshBackend` (not `IGeometryBackend`), `InMemoryEventSink` (not `IEventSink`). Rationale: the kit is the *default* wiring; if a host wants polymorphic access, it has the concrete instance and can pass it where the interface is wanted. Premature abstraction is rejected per ADR-0012's "the capability `TryGet<T>()` already does the job."
- **The factory does NOT construct CommandBus / QueryBus.** Both hosts need slightly different bus construction (HTTP host wraps the sink). Returning a bus from the factory would force a single sink choice. Returning the kit lets each host compose.
- **TASK-0012's Checkpoint 1 files.** Untouched. They live in `Engine.Core/Persistence/` and `Engine.Core/DiagnosticCodes.cs` (the four IO constants). They reference no factory-controlled symbol. They are not used by any production code yet. They will be commited as part of TASK-0012's eventual ship.
- **CURRENT-STATE version-vs-TASK-number divergence.** Briefly noted in the v0.12 entry. From this point, `v0.N` reflects ship order; TASK-000N reflects authoring order. The two diverge here because we paused TASK-0012 mid-implementation to ship TASK-0013 first.
