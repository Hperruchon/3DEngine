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

## Pending — V1

_None. V1 is complete; advance to V1.x._

## Pending — V1.x (after V1 ships)

P6 splits into four sequenced sub-phases. ADR-0005, ADR-0006, and ADR-0008 cover the contract; the wire format is implementation choice (ADR-0005 §1).

- **P6.3 — WebSocket event stream + reconnect cursor.** Implement ADR-0005 §§4–5. **Blocker:** the snapshot format for `subscription.reset` is "blocked on the persistence ADR" per ADR-0005; that may need a new ADR first.
- **P6.4 — Schema endpoints.** `/schema/{commands,queries,events,diagnostics}` per ADR-0008 §9, generated from the engine's registries.

- **P7 — Manifold backend wiring.** First concrete `IGeometryBackend` plus first geometry command (e.g. `CreateBox`). Enables the first non-trivial CLI scenario test.

## Pending — V2

- **P8 — Persistence, multi-Document, undo/redo.** Per ADR-deferred items.
