# Engine Runtime Boundaries

This document is the one-page map of the system. It collapses ADRs 0001–0004 into the boundary view a contributor (human or AI agent) needs before touching code.

## The diagram

```
                    ┌──────────────────────────────────────────┐
                    │              CLIENTS (thin)              │
                    │                                          │
                    │  Desktop UI │ Blazor WASM │ CLI │ HTTP   │
                    │             clients │ AI agents          │
                    └─────────────────┬────────────────────────┘
                                      │
                              Commands │ Events │ State reads
                                      │
                    ┌─────────────────▼────────────────────────┐
                    │           ENGINE RUNTIME (authority)     │
                    │                                          │
                    │   CommandBus  ──►  Document              │
                    │      ▲           (log + scene + meta)    │
                    │      │              │                    │
                    │   Validation        │                    │
                    │      │              ▼                    │
                    │   Event stream  ◄── Save/Load · Replay   │
                    │                                          │
                    └─────────────────┬────────────────────────┘
                                      │
                            Capability calls (IMeshOps, …)
                                      │
                    ┌─────────────────▼────────────────────────┐
                    │      GEOMETRY BACKEND (replaceable)      │
                    │                                          │
                    │   V1: Manifold (IMeshOps, IGeometryQuery)│
                    │   Future: OCCT, custom (BRep + FeatureId)│
                    └──────────────────────────────────────────┘
```

## Roles, in one sentence each

- **Engine Runtime** — the authoritative process/component. Owns the command bus, the Document, the geometry backend, save/load, replay, and the event stream. The only place business logic lives. (ADR 0004)
- **Document** — ordered command log + materialized scene + metadata. The data source of truth. Reproducible by replaying its log on a fresh backend.
- **Command** — the only mutator. Serializable, versioned, replayable. Issued identically by any client.
- **Event stream** — the observability surface. Sequence-numbered, replayable from a cursor, consumed by Blazor, CLI `--follow`, and AI agents.
- **Geometry backend** — a replaceable capability provider. Owns geometry data behind opaque `BodyHandle`s. Selected by capability negotiation, not by project flag. (ADR 0001)
- **Clients** — UI, Blazor, CLI, HTTP API, AI agents. Equal citizens. Submit commands, subscribe to events, render results. No business logic. (ADRs 0002, 0003, 0004)

## What lives where

| Concern | Engine Runtime | Client |
|---|---|---|
| Document state | ✅ authority | ❌ |
| Command application | ✅ authority | ❌ submits only |
| Geometry data | ✅ via backend | ❌ may render tessellated previews pushed by engine |
| Validation, debug reports | ✅ | ❌ displays only |
| Event sequence + retention | ✅ | ❌ subscribes |
| Save / load / replay | ✅ | ❌ triggers via command |
| Camera, selection, hover, drag preview | ❌ | ✅ ephemeral UI state |
| Theme, layout, panels | ❌ | ✅ |
| Schema (`/schema`) | ✅ publishes | ✅ consumes |

## Boundary tests (what CI enforces)

1. `Engine.Core` does not reference any UI/client assembly.
2. No client assembly references `Engine.Kernel.*` directly.
3. Every registered command has a CLI scenario test.
4. PRs touching `Engine.Contracts` reference an ADR.
5. The headless smoke job runs the engine + CLI without building any UI binary, and replays all fixtures deterministically.

If any of those break, the boundary has been violated and the architecture has begun to drift.

## Stable contracts (the things you may not change without an ADR)

- `Command`, `CommandResult`, `EventRecord` shapes in `Engine.Contracts`.
- The `IGeometryBackend` capability contract and reserved capability interfaces.
- The project file format header (`project.json` versioning).
- The HTTP/WS surface shape (routes, framing, sequence numbering).

Everything else inside the Engine Runtime is implementation detail and may be refactored freely, provided the contracts and CI gates above hold.

## How a new feature lands

1. Define the command(s) in `Engine.Contracts`. Bump schema version if needed.
2. Implement the handler(s) in `Engine.Core`, requesting required capabilities from the backend.
3. Add CLI scenario test(s) and replay fixture(s).
4. Add UI bindings (desktop or Blazor) — input → command translation only.
5. Open PR. CI checks: contract diff, capability declaration, CLI test presence, fixture replay determinism.

If step 4 needs anything not exposed by step 1, the feature is *not* implementable as scoped — return to step 1, do not work around it client-side.

## Cross-references

- ADR 0001 — Geometry Kernel Abstraction by Capabilities
- ADR 0002 — Headless-First, CLI as Canonical Client
- ADR 0003 — Blazor as Thin Viewer
- ADR 0004 — Engine Runtime is the Authoritative Application Controller
