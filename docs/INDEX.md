# Repo map

One page that says where things live. Each row gives a path and one line of purpose. This page
**points**; it does not explain. For *why*, read the relevant ADR. For *what exists today*, read
[CURRENT-STATE.md](CURRENT-STATE.md). For *your position and the rules*, read
[CLAUDE.md](../CLAUDE.md). `Engine.Tests/Governance/DocumentPathGateTests.cs` fails when a path on
this page does not exist.

The canonical boundary diagram is the section "Authority diagram" in [CLAUDE.md](../CLAUDE.md). An
older one-page map is archived at [archive/engine-runtime-boundaries.md](archive/engine-runtime-boundaries.md);
it is not correct and it must not be used.

## Engine spine (the projects you change)

| Path | Purpose |
|---|---|
| `Engine.Contracts/` | Design-truth contracts: the wire and record types that every consumer shares. Zero project references. A change to its public shape needs an ADR ([templates.md](templates.md), section 1). |
| `Engine.Contracts/Geometry/` | Opaque geometry handles and capability interfaces (`IGeometryBackend`, `IMeshOps`, `IGeometryQuery`, `ITransformOps`, `IBooleanOps`, reserved `IBRepOps` and `IFeatureIdMap`, `BackendCapabilities`). |
| `Engine.Contracts/Handlers/` | Handler abstractions (`ICommandHandler`, `IQueryHandler`) and their result records. |
| `Engine.Contracts/Schema/` | `FieldSchema`: the handler-declared schema vocabulary that the `/schema` endpoints project. |
| `Engine.Core/` | Design-truth scene kernel: `CommandBus`, `QueryBus`, registries, event sink, `IdempotencyCache`, `Replay`, `DiagnosticCodes`. References only `Engine.Contracts`. |
| `Engine.Core/Commands/` and `Engine.Core/Queries/` | One file for each command or query, with its handler ([templates.md](templates.md), section 4). |
| `Engine.Core/Geometry/` | In-process backends (`InProcessMeshBackend`, `NullGeometryBackend`). |
| `Engine.Core/Hosting/` | `HandlerCatalog`, the one list of handlers, and `ParameterBinder` (ADR-0016). |
| `Engine.Geometry.Manifold/` | Native geometry backend: `ManifoldGeometryBackend` over the `manifoldc` C API. References only `Engine.Contracts`; wired in at a host composition root (ADR-0014). |
| `Engine.Cli/` | The approved embedded host and the approved test client. `apply` and `query` verbs, JSON in and out, exit codes (ADR-0002, ADR-0011). |
| `Engine.Api.Http/` | The approved deployment process: `POST /commands`, `POST /queries`, `GET /events` (WebSocket), `GET /schema/*` (ADR-0011, ADR-0019). |
| `Engine.Tests/` | The verifier of authority: unit tests and the gates. It may reference any `Engine.*` project. |
| `Engine.Tests/Governance/` | The gates that read the documents ([templates.md](templates.md), section 5). |
| `3DEngine.Core/` | Peer **render** kernel (`Scene`, `Entity`, `Camera`, `Light`, materials). Not design truth. Never referenced by, and never references, an `Engine.*` project (ADR-0009). |
| `3DEngine.Vulkan/` | First-party **Vulkan layer**: `GraphicsDevice`, `Swapchain`, and the SDL `Window` that owns the surface. Holds the only pin of `Vortice.Vulkan` and `Alimer.Bindings.SDL`. Only a host that draws references it (ADR-0017). |
| `3DEngine/` | The Vulkan desktop host. Draws through `3DEngine.Vulkan`, which uses SDL3. Change it only when a task gives that scope. Track R changes it. |

## Docs and process

| Path | Purpose |
|---|---|
| `CLAUDE.md` | Your position: the language, the workflow, the authority diagram, the determinism rules, the stop conditions, the tests, and the rules with no other home. |
| `docs/CHARTER.md` | Mission, the twenty objectives, target consumers, non-goals against anti-objectives, the kernel boundary, the agent scope test. Read first if you do not know whether to act. |
| `docs/INDEX.md` | This map. |
| `docs/templates.md` | The forms for an ADR, a task, a commit and a codebase review; the code conventions; the gate table; the rule for rules. |
| `docs/register.md` | Known problems, questions, accepted compromises and temporary mechanisms. Each entry has a date and a time limit. |
| `docs/glossary.md` | The approved terms. Each term has one line and a pointer to its source. |
| `docs/diagnostics.md` | The diagnostic code registry. A code is never removed and never changes meaning. |
| `docs/roadmap.md` | The tracks and the phases. The menu, not the ledger. Read it to find the next task. |
| `docs/CURRENT-STATE.md` | What is built today. The authority for "does X exist". |
| `docs/adr/` | Architectural decisions, the *why*. Start at [adr/README.md](adr/README.md); read only the ones that `governed-by` names. |
| `docs/reviews/` | Dated reviews of the code and of the rules, one file for each review. A review is a record and not the authority. A task, a register entry or an ADR closes a finding; an edit to the review does not. |
| `docs/proposals/` | Analyses that precede a decision. Not a rule. |
| `docs/archive/` | Records that are not rules and must not be used as rules. Each file has a header that says so. |
| `tasks/` | Sized work units. Read only the task that you advance. |
| `.github/workflows/ci.yml` | The gate: build, test and two smoke tests on three operating systems; the contract gate; the write-set gate. |
| `.github/PULL_REQUEST_TEMPLATE.md` and `.github/CODEOWNERS` | The pull request form, and the owner of each surface. |
| `eng/write-set-cutoff.txt` | The commit after which the write-set gate applies. |
| `eng/manifold-native/` | The packaging project for the native Manifold payload. |
| `nuget/` | The local package feed for the native payload (register entry R-0007). |
| `THIRD-PARTY-NOTICES.md` | Each third-party component, its licence and its checksum. `NativePackageGateTests` reads the checksums. |
