# Repo map

One-page map of where things live. Paths + one-line purpose. This page **points**; it does not explain. For *why*, read the relevant ADR; for *what exists today*, read [CURRENT-STATE.md](CURRENT-STATE.md); for *boundaries and rules*, read [CLAUDE.md](../CLAUDE.md).

The canonical boundary diagram is **[architecture/engine-runtime-boundaries.md](architecture/engine-runtime-boundaries.md)** — it is not redrawn here. The authority/two-kernel diagram lives in [CLAUDE.md](../CLAUDE.md) "Authority diagram".

## Engine spine (the projects you change)

| Path | Purpose |
|---|---|
| `Engine.Contracts/` | Design-truth contracts — the wire/record types every consumer shares. Zero project references. Changing its public shape needs an ADR (see [conventions.md](conventions.md)). |
| `Engine.Contracts/Geometry/` | Opaque geometry handles and capability interfaces (`IGeometryBackend`, `IMeshOps`, `IGeometryQuery`, reserved `IBRepOps`/`IFeatureIdMap`, `BackendCapabilities`). |
| `Engine.Contracts/Handlers/` | Handler abstractions (`ICommandHandler`, `IQueryHandler`) and their result records. |
| `Engine.Contracts/Schema/` | `FieldSchema` — the handler-declared schema vocabulary projected by the `/schema` endpoints. |
| `Engine.Core/` | Design-truth scene kernel — `CommandBus`, `QueryBus`, registries, event sink, `IdempotencyCache`, `Replay`, `DiagnosticCodes`. References only `Engine.Contracts`. |
| `Engine.Core/Commands/` · `Engine.Core/Queries/` | One file per command/query + its handler (see [conventions.md](conventions.md)). |
| `Engine.Core/Geometry/` | In-process backends (`InProcessMeshBackend`, `NullGeometryBackend`). |
| `Engine.Cli/` | Canonical embedded host and canonical test client. `apply` / `query` verbs, JSON in/out, exit codes. (ADR-0002, ADR-0011.) |
| `Engine.Api.Http/` | Canonical deployment process — `POST /commands`, `POST /queries`, `GET /events` (WebSocket), `GET /schema/*`. References only `Engine.Core` + `Engine.Contracts`. (ADR-0011.) |
| `Engine.Tests/` | Verifier of authority — unit tests plus the CI gates (diagnostics registered, schema parity, replay determinism). May reference any `Engine.*`. |
| `3DEngine.Core/` | Peer **render** kernel (POCO scene: `Scene`, `Entity`, `Camera`, `Light`, materials). Not design truth. Mutually unreferenceable with `Engine.*`. (ADR-0009.) |

## Docs and process

| Path | Purpose |
|---|---|
| `docs/CHARTER.md` | Mission, target consumers, non-goals vs anti-objectives, the agent scope test. Read first if unsure whether to act. |
| `docs/INDEX.md` | This map. |
| `docs/glossary.md` | Canonical vocabulary — each term one line + a pointer to its defining file/ADR. |
| `docs/conventions.md` | File/naming/grammar conventions that make grep cheap. |
| `docs/adr/` | Architectural decisions (the *why*). Start at [adr/README.md](adr/README.md); read only the one that applies. |
| `docs/architecture/` | Boundary view — [engine-runtime-boundaries.md](architecture/engine-runtime-boundaries.md) is the canonical diagram. |
| `docs/CURRENT-STATE.md` | What is built today. Authoritative for "does X exist yet." |
| `docs/diagnostics.md` | Diagnostic code registry. Append-only. |
| `docs/open-questions.md` | Deferred decisions. Read only if one blocks your task. |
| `docs/roadmap.md` | Strategic phase plan. Read when no Ready TASK exists. |
| `tasks/` | Sized work units. Read only the active task (Status: Active or Ready). |
| `.github/` | PR template, CODEOWNERS, and `workflows/ci.yml` (build+test, headless smoke, contract-gate). |

## Not part of the engine spine — do not modify

Per [CLAUDE.md](../CLAUDE.md) "Do not touch":

| Path | What it is |
|---|---|
| `3DEngine/` | Vulkan/SDL3 desktop host. |
| `BlazorApp/`, `BlazorApp.Client/` | Placeholder shell. |
| `Vortice.Vulkan.Sample/`, `Vortice.Vulkan.SampleFramework/` | Sample code. |
