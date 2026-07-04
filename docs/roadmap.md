# Roadmap

Strategic phase plan for the engine. Phases here are candidates; once a phase is chosen, it becomes a sized TASK in `tasks/`. The TASK file's `## Status` is the source of truth for whether a phase has shipped — this file is the menu, not the ledger.

When a phase ships, move its bullet from "Pending" to "Shipped" (one-line entry referencing the version and TASK id).

When the V1 Pending list is empty, advance to V1.x. When V2 Pending is empty, ask for next strategic goals before sizing anything.

## Shipped

- P0 — Engine Runtime spine. v0.1, TASK-0001.
- P1 — Engine.Cli scaffold. v0.2, TASK-0002.
- P2 — Diagnostics registry CI gate. v0.3, TASK-0003.
- P3 — `3DEngine.Core` peer render kernel (ADR-0009). v0.4, TASK-0004.
- P4 — Replay determinism CI gate. v0.5, TASK-0005.
- P5 — Workflow gates. v0.6, TASK-0006.
- P6.1 — Engine.Api.Http scaffold (HTTP commands/queries). v0.7, TASK-0007.
- P6.2 — Idempotency cache. v0.8, TASK-0008.
- P6.4 — Schema endpoints. v0.9, TASK-0009.
- P6.3 — WebSocket event stream + reconnect (ADRs 0010 & 0011). v0.10, TASK-0010.
- P7a — First geometry slice: `CreateBox` end-to-end (ADRs 0012 & 0013). v0.11, TASK-0011.
- P7b — Manifold backend (swap-in) (ADR-0014). v0.14, TASK-0012.

## Pending — V1

_None. V1 is complete; advance to V1.x._

## Pending — V1.x (after V1 ships)

_None. P6, P7a, and P7b are shipped; V1.x is complete. Advance to V2._

## Pending — V2

- **P8 — Persistence, multi-Document, undo/redo.** Per ADR-deferred items.
