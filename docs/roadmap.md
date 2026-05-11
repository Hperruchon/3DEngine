# Roadmap

Strategic phase plan for the engine. Phases here are candidates; once a phase is chosen, it becomes a sized TASK in `tasks/`. The TASK file's `## Status` is the source of truth for whether a phase has shipped — this file is the menu, not the ledger.

When a phase ships, move its bullet from "Pending" to "Shipped" (one-line entry referencing the version and TASK id).

When the V1 Pending list is empty, advance to V1.x. When V2 Pending is empty, ask for next strategic goals before sizing anything.

## Shipped

- P0 — Engine Runtime spine. v0.1, TASK-0001.
- P1 — Engine.Cli scaffold. v0.2, TASK-0002.
- P2 — Diagnostics registry CI gate. v0.3, TASK-0003.
- P3 — `3DEngine.Core` peer render kernel (ADR-0009). v0.4, TASK-0004.

## Pending — V1

- **P4 — Replay determinism CI gate.** Hand-authored command-log fixture replayed via `Replay(...)`; assert Document equality. Catches non-determinism (`DateTime.Now`, unseeded GUIDs, ordering on hash buckets) at PR time.

- **P5 — Workflow gates.** PR template, CODEOWNERS, contract-touched-needs-ADR check, headless-smoke job. Process discipline only; no `Engine.*` code changes.

## Pending — V1.x (after V1 ships)

- **P6 — `Engine.Api.Http`.** HTTP/WebSocket transport mirroring the in-process surface per ADR-0005 §5. Adds idempotency cache by `CommandId`, reconnect-cursor protocol, schema endpoints. Out of scope until P0–P5 are green.

- **P7 — Manifold backend wiring.** First concrete `IGeometryBackend` plus first geometry command (e.g. `CreateBox`). Enables the first non-trivial CLI scenario test.

## Pending — V2

- **P8 — Persistence, multi-Document, undo/redo.** Per ADR-deferred items.
