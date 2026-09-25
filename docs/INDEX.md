# Repo map

One-page map of where things live. Paths + one-line purpose. This page **points**; it does not explain. For *why*, read the relevant ADR; for *what exists today*, read [CURRENT-STATE.md](CURRENT-STATE.md); for *boundaries and rules*, read [CLAUDE.md](../CLAUDE.md).

The canonical boundary diagram is the section "Authority diagram" in [CLAUDE.md](../CLAUDE.md). An older one-page map is archived at [archive/engine-runtime-boundaries.md](archive/engine-runtime-boundaries.md); it is not correct and it must not be used. Register entry R-0010 recorded that condition and TASK-0025 closed it.

## Engine spine (the projects you change)

| Path | Purpose |
|---|---|
| `Engine.Contracts/` | Design-truth contracts — the wire/record types every consumer shares. Zero project references. Changing its public shape needs an ADR (see [conventions.md](conventions.md)). |
| `Engine.Contracts/Geometry/` | Opaque geometry handles and capability interfaces (`IGeometryBackend`, `IMeshOps`, `IGeometryQuery`, `ITransformOps`, `IBooleanOps`, reserved `IBRepOps`/`IFeatureIdMap`, `BackendCapabilities`). |
| `Engine.Contracts/Handlers/` | Handler abstractions (`ICommandHandler`, `IQueryHandler`) and their result records. |
| `Engine.Contracts/Schema/` | `FieldSchema` — the handler-declared schema vocabulary projected by the `/schema` endpoints. |
| `Engine.Core/` | Design-truth scene kernel — `CommandBus`, `QueryBus`, registries, event sink, `IdempotencyCache`, `Replay`, `DiagnosticCodes`. References only `Engine.Contracts`. |
| `Engine.Core/Commands/` · `Engine.Core/Queries/` | One file per command/query + its handler (see [conventions.md](conventions.md)). |
| `Engine.Core/Geometry/` | In-process backends (`InProcessMeshBackend`, `NullGeometryBackend`). |
| `Engine.Geometry.Manifold/` | Native geometry backend — `ManifoldGeometryBackend` over the `manifoldc` C API via `[LibraryImport]` P/Invoke. References only `Engine.Contracts`; wired in at host composition roots (keeps native deps out of `Engine.Core`). (ADR-0014.) |
| `Engine.Cli/` | Canonical embedded host and canonical test client. `apply` / `query` verbs, JSON in/out, exit codes. References `Engine.Core` + `Engine.Contracts` + `Engine.Geometry.Manifold`. (ADR-0002, ADR-0011.) |
| `Engine.Api.Http/` | Canonical deployment process — `POST /commands`, `POST /queries`, `GET /events` (WebSocket), `GET /schema/*`. References `Engine.Core` + `Engine.Contracts`, plus `Engine.Geometry.Manifold` as a host composition root (ADR-0014 §4). (ADR-0011.) |
| `Engine.Tests/` | Verifier of authority — unit tests plus the CI gates (diagnostics registered, schema parity, replay determinism). May reference any `Engine.*`. |
| `3DEngine.Core/` | Peer **render** kernel (POCO scene: `Scene`, `Entity`, `Camera`, `Light`, materials). Not design truth. Mutually unreferenceable with `Engine.*`. (ADR-0009.) |
| `3DEngine.Vulkan/` | First-party **Vulkan layer** — `GraphicsDevice`, `Swapchain`, and the SDL `Window` that owns the surface. References no `Engine.*` project. Holds the only pin of `Vortice.Vulkan` and `Alimer.Bindings.SDL`. Only a host that draws references it. (ADR-0017.) |

## Docs and process

| Path | Purpose |
|---|---|
| `docs/CHARTER.md` | Mission, the twenty objectives, target consumers, non-goals against anti-objectives, the kernel boundary, the agent scope test. Read first if you do not know whether to act. |
| `docs/INDEX.md` | This map. |
| `docs/working-agreement.md` | How to behave. Scope, the record, accuracy, decisions, time, parallel agents. |
| `docs/templates.md` | The forms for a register entry, an ADR, a task and a commit. |
| `docs/register.md` | Known problems, questions and temporary mechanisms. Each entry has a date and a time limit. Replaces `open-questions.md`. |
| `docs/glossary.md` | Canonical vocabulary — each term one line + a pointer to its defining file/ADR. |
| `docs/conventions.md` | File/naming/grammar conventions that make grep cheap. |
| `docs/adr/` | Architectural decisions (the *why*). Start at [adr/README.md](adr/README.md); read only the one that applies. |
| `docs/reviews/` | Dated reviews of the code, one file for each review. A review is a record and not the authority: `CURRENT-STATE.md` says what exists. A task, a register entry or an ADR closes a finding; an edit to the review does not. A review is due before the seventh milestone after the last one ([templates.md](templates.md) section 7). |
| `docs/architecture/` | Boundary view. **`engine-runtime-boundaries.md` is not current — see register entry R-0010.** |
| `docs/CURRENT-STATE.md` | What is built today. Authoritative for "does X exist yet." |
| `docs/diagnostics.md` | Diagnostic code registry. Append-only. |
| `docs/roadmap.md` | The tracks and the phases. Read when no task has the status Ready. |
| `tasks/` | Sized work units. Read only the active task (Status: Active or Ready). |
| `.github/` | PR template, CODEOWNERS, and `workflows/ci.yml` (build+test, headless smoke, contract-gate). |

## Not part of the engine spine

Per [CLAUDE.md](../CLAUDE.md) "Projects outside the engine spine": do not change these projects
unless a task gives you that scope. Track R changes `3DEngine/`, and each such change needs a task
that declares it.

| Path | What it is |
|---|---|
| `3DEngine/` | Vulkan/SDL3 desktop host. Draws through `3DEngine.Vulkan`. |
