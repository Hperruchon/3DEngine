---
id: 0012
title: Geometry backend wiring
status: Amended
topic: Boundary, geometry, contracts
date: 2026-05-27
supersedes: []
superseded-by: []
amends: []
amended-by: ['0018', '0021', '0028']
affects:
  - Engine.Contracts/Geometry/**
  - Engine.Core/Commands/**
enforced-by: Engine.Tests/Commands/CreateBoxCommandTests.cs
---

# ADR 0012 — Geometry backend wiring (V1)

## Status

Accepted — 2026-05-27

## Context

ADR-0001 set the geometry kernel shape: capability-typed backends, `BodyHandle` opaque, the kernel API internal to command handlers, replay against a fresh backend as the cache-recovery story. It deliberately left V1 implementation details open: the actual method shape of `IMeshOps` / `IGeometryQuery`, how a handler reaches the active backend, where `BodyHandle`s appear in `Document` state, and how body identity stays stable across replay.

As of v0.10 those interfaces are empty markers, `Document` is metadata + log with no body-side state, and `ICommandHandler.Handle(Command, Document, CancellationToken)` has no backend parameter. P7 cannot land a concrete command (`CreateBox`) without resolving each of these.

ADR-0010 §3 anticipated the consequence on the subscription-reset snapshot: "When a concrete geometry backend lands (P7), the snapshot extends with the authoritative geometry state — minimally, the list of `BodyHandle`s known to the Document." The shape of that extension is part of this ADR.

This ADR pins V1 wiring. It is **backend-agnostic** — the choice of native library (Manifold vs. alternatives) and its native-interop posture is deferred to a follow-on ADR. The V1.x first slice ships against an in-process managed stub that satisfies the same capability surface, per ADR-0001's "backends are caches, losing the cache is recoverable by replay."

## Decision

### 1. Capability surface (V1)

`IGeometryBackend` becomes the dispatch root specified in ADR-0001 §1:

```csharp
public interface IGeometryBackend
{
    BackendCapabilities Capabilities { get; }
    T? TryGet<T>() where T : class;
}

[Flags]
public enum BackendCapabilities
{
    None        = 0,
    Mesh        = 1 << 0,
    Query       = 1 << 1,
    Transform   = 1 << 2,
    Booleans    = 1 << 3,
    // BRep, ExactBooleans, FaceIds, Offset, Fillet reserved per ADR-0001.
}
```

Capability interfaces gain V1 methods. The V1.x first slice ships the minimum to support `CreateBox` end-to-end:

```csharp
public interface IMeshOps
{
    void CreateBox(BodyHandle handle, BoxParameters parameters);
}

public interface IGeometryQuery
{
    Aabb GetBoundingBox(BodyHandle handle);
}

public readonly record struct BoxParameters(double SizeX, double SizeY, double SizeZ);
public readonly record struct Aabb(double MinX, double MinY, double MinZ,
                                   double MaxX, double MaxY, double MaxZ);
```

Notes:

- `BoxParameters` and `Aabb` are POCO value types in `Engine.Contracts.Geometry`. They are NOT generic geometry POCOs (rejected by ADR-0001 §Non-goals); they are command-input and query-output value types.
- `IMeshOps.CreateBox` takes a pre-computed `BodyHandle` and is responsible for storing the body under it. The handler chooses the handle; see §4.
- A later capability gets its own interface when a backend can be unable to honour it, under its own task and a change to this ADR.
- `IBRepOps` and `IFeatureIdMap` remain reserved markers per ADR-0001 §1.

Two more capability interfaces hold the operations that need a real kernel:

```csharp
public interface ITransformOps
{
    void Translate(BodyHandle result, BodyHandle source, double dx, double dy, double dz);
}

public interface IBooleanOps
{
    void Subtract(BodyHandle result, BodyHandle minuend, BodyHandle subtrahend);
}
```

- They are their own interfaces, not members of `IMeshOps`. The managed stub of §7 stores box dimensions only, and it cannot represent a translated solid or a boolean solid. Because the interfaces are separate, the stub does not implement them: a handler gets null from `TryGet<T>()` and returns `E-GEOM-CAP-MISSING`. A stub that claimed the capability would be a silent fallback (ADR-0001 §4).
- These are the mesh booleans of Manifold. They are not the reserved B-Rep `ExactBooleans` (ADR-0001 §1).
- Only `ManifoldGeometryBackend` implements them, with `manifold_translate` and `manifold_difference`.

### 2. Handler-to-backend access

`ICommandHandler.Handle` grows an `IGeometryBackend` parameter:

```csharp
Task<CommandHandlerResult> Handle(
    Command command,
    Document document,
    IGeometryBackend backend,
    CancellationToken ct);
```

This is a breaking change to `Engine.Contracts`. Existing handlers (`NoOpCommandHandler`) accept the parameter and ignore it. The bus passes its configured backend on every call; rules:

- `CommandBus` constructor gains a non-optional `IGeometryBackend backend` parameter. There is always a backend — even if it is an empty `NullGeometryBackend` whose `Capabilities = None` and `TryGet<T>()` always returns null. This avoids null-checking inside handlers.
- A handler that needs `IMeshOps` calls `backend.TryGet<IMeshOps>()` and fails fast with a structured `ErrorDetail` (`E-GEOM-CAP-MISSING`) if null. No silent fallbacks (ADR-0001 §4).
- `Replay.ReplayLog(log, document, registry, sink, backend)` likewise takes the backend as a parameter. Replay against a fresh backend means passing a fresh backend instance.
- A handler checks in this order: each operand is in `Document.Bodies`, then the backend has the capability, then the handler runs the operation. The check of an operand does not depend on the backend, so a bad reference gives `E-GEOM-BODY-NOT-FOUND` on each backend, and the one-shot command line and the tests on each platform stay deterministic.
- `Translate` and `Subtract` add no diagnostic code. An unknown operand gives `E-GEOM-BODY-NOT-FOUND`, a missing capability gives `E-GEOM-CAP-MISSING`, and a native failure or an empty difference (the subtrahend contains the minuend) gives `E-GEOM-NATIVE-OP`.

Rationale: backends are caches. Constructor injection on handlers binds each handler instance to a specific backend at registration time; cache recovery via replay would require rebuilding the registry. Handle-parameter injection structurally aligns with ADR-0001's "switching backends is performed by replaying the command log against the new backend." It also makes the bus the single source of truth for "which backend is active" — no per-handler routing logic.

### 3. Document grows a body projection

`Document` gains a body collection:

```csharp
public sealed class Document
{
    // ...existing metadata...

    private readonly Dictionary<Guid, BodyRecord> _bodies = new();
    public IReadOnlyCollection<BodyRecord> Bodies => _bodies.Values;

    internal void AddBody(BodyRecord body);   // called by CommandBus on commit
}

public sealed record BodyRecord(BodyHandle Handle, string Kind, string? Name = null, bool Visible = true);
```

- `Kind` is a string discriminator; extends additively as new primitives land. `"Box"` is a box from `CreateBox`. `"Solid"` is a general manifold solid, the result of `Translate` and `Subtract`.
- `Name` and `Visible` are the name and the visibility of the body (ADR-0028): no name and visible until a command changes them.
- The Document holds the *handle list and minimum metadata*; the backend owns the *geometry data*. This matches ADR-0001 §3.
- Future fields (name, parent, hidden flag, etc.) extend `BodyRecord` additively under their own sized TASK.

### 4. Body identity is deterministic from `CommandId`

Body handles are computed by the handler, deterministically from inputs that survive replay:

```csharp
BodyHandle handle = new BodyHandle(command.CommandId);
```

For commands that create exactly one body (`CreateBox`, `Translate` and `Subtract`), the handle Guid equals the command Guid. `Translate` and `Subtract` each create one new body and emit the existing `body.created`; no new event kind. ADR-0021 decides what happens to their operands. For commands that create N bodies in a single commit (future), the handler derives stable child Guids via a versioned scheme (e.g. `Guid.Create(commandId, ordinal)` per RFC 4122 §4.3) — out of scope for V1, sized when the first such command lands.

Rationale: `Guid.NewGuid()` is non-deterministic across replay. `CommandId` is supplied by the client (or generated once at submission) and persisted in the command log. Replay against a new backend produces identical handles, identical Document state, and identical events — preserving ADR-0005's replay invariants.

### 5. New event kind: `body.created`

The bus emits a `body.created` event in addition to `command.applied` when a command produces a body:

```
EventRecord
  Kind: "body.created"
  CauseCommandId: <originating command>
  Payload:
    bodyId: Guid
    kind:   string  // "Box"
```

The event surfaces in the stream; the bus updates `Document.Bodies` on the same commit. The mechanism: command handlers return body-side outputs via a *committable* shape in `CommandHandlerResult` (specifically, an addition to that record — see §Validation rule 4).

### 6. The `subscription.reset` snapshot extends

Per ADR-0010 §3, the snapshot grows additively:

```
snapshot
  ...existing fields...
  bodies: [ { handle: Guid, kind: string, name: string?, visible: boolean }, ... ]   // name and visible: ADR-0028
```

Empty array when no bodies. Existing clients that ignore unknown fields continue to function. This closes ADR-0010's "Geometry snapshot shape" open challenge.

### 7. V1.x first-backend choice: in-process managed stub

The first sized TASK ships an in-process managed backend:

- Implements `IGeometryBackend` (capabilities `Mesh | Query`), `IMeshOps`, `IGeometryQuery`.
- Stores bodies in a `Dictionary<BodyHandle, BoxRecord>` where `BoxRecord` is the V1 mesh stub state (parameters only).
- Computes AABB from `BoxParameters` directly (axis-aligned, no transforms in V1).
- No native interop. No threading concerns beyond the bus's serial section.

- Does not implement `ITransformOps` or `IBooleanOps` (§1). On a host that uses the stub, for example a runtime identifier with no native payload, `Translate` and `Subtract` fail with `E-GEOM-CAP-MISSING`, and the engine still runs.

This is *not* the Manifold backend — it is the wiring slice. A follow-on TASK swaps Manifold in behind the same capability interfaces. Until then, V1.x has a working `CreateBox` round-trip and the project ships value.

## Consequences

- **`Engine.Contracts` evolves.** Capability interfaces gain methods; `ICommandHandler.Handle` gains a parameter; `Document` gains `Bodies`; `BodyRecord`, `BoxParameters`, `Aabb`, `BackendCapabilities` are new public types. All gated by this ADR per CLAUDE.md.
- **`CommandBus` and `Replay` evolve.** Both take a backend parameter. Existing call sites in `Engine.Cli`, `Engine.Api.Http`, and tests update to pass one (the V1.x first slice wires the stub; the NoOp-only paths get a `NullGeometryBackend`).
- **`NoOpCommandHandler` updates.** Adds the new parameter, ignores it. No behavioural change.
- **Replay determinism gate continues to hold.** Body handles are functions of `CommandId`; replay produces byte-identical events and Document state (modulo `Timestamp`/`DocumentId`). The canonical gate stays on the managed stub. `ManifoldReplayRoundTripTests` replays create, create, translate and subtract twice on the native backend and compares the state (ADR-0014 keeps the reproducibility of the native library across platforms out of the canonical gate).
- **Snapshot grows.** ADR-0010 §3 anticipated this; the snapshot-shape gate test in P6.3 updates to permit the new `bodies` array.
- **Manifold deferred.** The decision to use Manifold (vs. alternatives), the wrapper choice, native lifecycle, threading, build/distribution — all become a follow-on ADR sized when the stub backend has shipped. The V1.x slice does not block on it.
- **One backend per process for V1.x.** Per ADR-0011, `engine-api-http` is one process; one backend per process is the V1 deployment. Multi-backend (mesh + B-Rep concurrent) is V2 territory; the capability dispatch in `IGeometryBackend.TryGet<T>()` already allows it without further ADR change.

## Non-goals

- Manifold-specific decisions (binding choice, native lifecycle, threading model). Separate ADR.
- B-Rep capabilities. ADR-0001 reserves them; this ADR does not implement them.
- Multi-backend selection policy per command. V2.
- Tessellation parameters / preview meshes for clients. Out of scope until a client needs them.
- A separate `IBackendCatalog` abstraction over `IGeometryBackend`. Premature; the capability `TryGet<T>()` already does the job.
- Geometry POCOs (`Mesh`, `Solid`) on the wire. Rejected by ADR-0001 §Non-goals; clients still see only commands, events, and (eventually) tessellated previews via explicit queries.
- Per-body metadata beyond `Kind` (names, layers, visibility). Additive future TASKs.

## Validation rules

1. `IGeometryBackend` is the only top-level kernel type clients-of-the-kernel (i.e., command handlers) reference. They obtain capabilities through `TryGet<T>()`, not by type-asserting the backend.
2. Body handles are pure functions of `CommandId` (or `Guid.Create(commandId, ordinal)` for multi-body commands). CI: replay-determinism fixture extends to include a `CreateBox` step; two replays produce byte-identical event sequences and identical `Document.Bodies`.
3. `CommandBus` and `Replay` MUST accept an `IGeometryBackend` parameter. CI: compile failure if the constructor signature regresses (the test project links against the signature).
4. `CommandHandlerResult` extends to carry zero-or-more body additions (e.g. `IReadOnlyList<BodyRecord> CreatedBodies`); the bus appends them to `Document.Bodies` inside the serial commit section. CI: `Document.Bodies` is never mutated outside `CommandBus.Apply`'s commit step.
5. The `subscription.reset` snapshot includes `bodies` (possibly empty) at all times once this ADR ships. CI: WebSocket snapshot gate test asserts the field is present.
6. Capability-missing failures use `E-GEOM-CAP-MISSING` (new diagnostic code; registered in `docs/diagnostics.md`, `DiagnosticCodes.cs`, and `/schema/diagnostics` in the same change).
7. `InProcessMeshBackend` does not implement `ITransformOps` or `IBooleanOps`; `TryGet<T>()` returns null for both. CI: `InProcessMeshBackendTests`.
8. The result of `Translate` and of `Subtract` is a new body with the handle `== CommandId` and the kind `"Solid"`. CI: the native backend tests and the replay tests.

## Open challenges

- **Multi-body commands.** The `Guid.Create(commandId, ordinal)` sketch for handle derivation is uncontroversial but unproven — pin down when the first such command (e.g. boolean producing two pieces) lands.
- **Backend hot-swap.** Today's "swap by replay" is cold (build a new bus + replay). Hot-swap (running engine accepts a new backend mid-session) is plausible but unmotivated — defer until a real workload asks.
- **Concurrent backends.** A future workload may want mesh and B-Rep simultaneously. `IGeometryBackend.TryGet<T>()` accommodates it; the question is whether the bus passes a single backend or a catalog. Defer until V2 motivates it.
- **Tessellation-for-clients.** Render-capable clients (`3DEngine`, `BlazorApp.Client`) eventually need triangulated previews. Whether that lives on `IGeometryQuery.Tessellate(handle)` or `IMeshOps.Tessellate(handle)` is open; ADR-0001 §Open challenges flagged it. Decided when the first client actually consumes it.

## History

- 2026-07-05 · TASK-0013 · An in-file "Amendment 1" adds `ITransformOps`, `IBooleanOps`, the commands
  `Translate` and `Subtract`, and the kind `"Solid"`.
- 2026-10-27 · TASK-0040 · Amendment 1 folds into items 1, 2, 3, 4 and 7, the consequences and the
  validation rules, and its section goes. Item 1 no longer says that a later capability extends
  `IMeshOps`, because Amendment 1 chose separate interfaces. ADR-0018 and ADR-0021 still amend this
  record; their folds wait.
- 2026-10-28 · TASK-0085 · ADR-0028 amends §3 and §6: `BodyRecord` gains `Name` and `Visible`, and each body of
  the snapshot gives them.
