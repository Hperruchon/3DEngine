# Current state

What is built today. One short entry per shipped milestone. Reference ADRs by ID; do not restate decisions.

## v0.1 — Engine Runtime spine (P0, TASK-0001)

In-memory, in-process Engine Runtime. No transport, no clients, no persistence, no geometry backend.

**Built:**
- `Engine.Contracts` — `Command`, `CommandResult`, `Outputs`, `Diagnostic`, `ErrorDetail`, `Query`, `QueryResult<T>`, `EventRecord`, `Document`, `BodyHandle`, capability marker interfaces (`IGeometryBackend`, `IMeshOps`, `IGeometryQuery`, `IBRepOps`, `IFeatureIdMap`), handler abstractions.
- `Engine.Core` — `CommandBus` (serial, atomic commit-at-end), `QueryBus` (empty registry), `CommandRegistry`, `QueryRegistry`, `InMemoryEventSink` (bounded ring), `Replay`, `DiagnosticCodes`, `NoOpCommand` + handler.
- `Engine.Tests` — 9 tests covering command application, version-stale rejection, query rejection, monotonic Seq, ring eviction, replay determinism.

**Verified by:** `dotnet build` + `dotnet test` green.

**Decisions in force:** ADRs 0001–0008.

**Deferred:** HTTP/WS transport, CLI, concrete geometry backend, persistence, idempotency cache, schema endpoints, undo/redo. See V1 scope clamps in [CLAUDE.md](../CLAUDE.md).

**Pending decisions:** ADR-0009 (fate of `3DEngine.Core`).
