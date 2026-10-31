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
| `Engine.Cli/` | The approved embedded host and the approved test client. `apply` and `query` verbs, JSON in and out, exit codes (ADR-0002, ADR-0019). |
| `Engine.Api.Http/` | The HTTP and WebSocket surface as a library: `POST /commands`, `POST /queries`, `GET /events` (WebSocket), `GET /schema/*`. A host mounts it on a document session that it owns, through `Engine.Api.Http/EngineSurface.cs`, which also holds the address checks of each host. It references `Engine.Core` and `Engine.Contracts` only (ADR-0019, TASK-0038). |
| `Engine.Api.Http.Server/` | The program `engine-api-http`, the approved deployment process. It chooses the geometry backend, composes one session and mounts the surface library (ADR-0019 item 4, TASK-0038). |
| `Engine.Tests/` | The verifier of authority: unit tests and the gates. It may reference any `Engine.*` project. |
| `Engine.Tests/Governance/` | The gates that read the documents ([templates.md](templates.md), section 5). |
| `3DEngine.Core/` | Peer **render** kernel (`Scene`, `Entity`, the orbit `Camera`, `Light`, materials, the draw list in `3DEngine.Core/Rendering/`, and the intent layer in `3DEngine.Core/Input/`, ADR-0026). Not design truth. Never referenced by, and never references, an `Engine.*` project (ADR-0009, ADR-0023). |
| `3DEngine.Vulkan/` | First-party **Vulkan layer**: the core of the device (`GraphicsDevice`, with a window or none), the presentation to a window (`Presenter`, `Swapchain`, the SDL `Window` that owns the surface), the `OffscreenTarget`, the first pipeline (`RenderPipeline`, `RenderPasses`, `MeshVertex`, `GpuBuffer`), and in `3DEngine.Vulkan/Ui/` the renderer of the 2D draw data of the user interface (ADR-0027). Holds the only pin of `Vortice.Vulkan` and `Alimer.Bindings.SDL`. Only a host that draws, `3DEngine.Tests` and `3DEngine.ImageTests` reference it (ADR-0017, ADR-0022, ADR-0025). |
| `3DEngine.Vulkan/Shaders/` | The GLSL source, the SPIR-V and `shaders.lock` of each shader. `node 3DEngine.Vulkan/Shaders/compile.js` compiles each one with the Vulkan SDK; run it after each change of a shader (ADR-0022). |
| `3DEngine.Tests/` | The tests of the render side, with no GPU. It may reference `3DEngine.Core` and `3DEngine.Vulkan` only (ADR-0022). |
| `3DEngine.ImageTests/` | The image test of the first demonstration: a Vulkan device with no window draws the cut, and the test compares the picture by structure. It may reference the host `3DEngine` and the projects of that host. The pipeline runs it on Ubuntu with lavapipe (ADR-0025). |
| `3DEngine.VisionTests/` | The verification spike of the vision library (phase V0): OpenCvSharp4 loads on each runner and finds the contour and the edges of a synthetic image. It references no project, and it holds the only pins of OpenCvSharp4 (ADR-0042). |
| `scripts/first-demonstration.js` | Sends the four commands of the first demonstration over HTTP to the desktop: `node scripts/first-demonstration.js [address]` (ADR-0025 items 1 and 7). |
| `3DEngine/` | The Vulkan desktop host. Draws through `3DEngine.Vulkan`, which uses SDL3. It owns a document session and serves the surface library on 127.0.0.1, port 5180 by default, so that an agent works on the document that the person has open (ADR-0019, TASK-0054). `3DEngine/Ui/` holds the user interface: the only files that name the library Dear ImGui (ADR-0027, `UiLibraryGateTests`). Change it only when a task gives that scope. Track R changes it. |

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
| `eng/write-set-check/` | The judge of the write set of each commit. The pipeline builds it from `main`, and a person runs it with `--staged` before a commit (TASK-0053). With `--gate-sources` it tells the pipeline whether the gates come from `main` or from the judged commit (TASK-0066). `eng/write-set-check/Judge.cs` holds the rules, and `eng/write-set-check/TaskFiles.cs` reads the task files for the judge and for the gates. |
| `eng/repo-gates/` | The program that runs each gate class that reads only files of the repository. The pipeline builds it from `main` and runs it on the judged checkout, and a person runs it with `--root .` before a commit (TASK-0064). The gate files stay under `Engine.Tests/`, which also runs them as tests. |
| `eng/report-failed-tests.js` | Writes the name and the message of each failed test as a public annotation when the step "Test" of the pipeline fails (TASK-0045). |
| `eng/manifold-native/` | The packaging project for the native Manifold payload. |
| `eng/manifold-native/manifold-ref.txt` | The pin of the native build: the Manifold reference and the package version. A push that changes it starts `.github/workflows/build-manifold-native.yml`, which pushes the package to the branch native-package/<version> (TASK-0048). |
| `nuget/` | The local package feed for the native payload (register entry R-0007). |
| `THIRD-PARTY-NOTICES.md` | Each third-party component, its licence and its checksum. `NativePackageGateTests` reads the checksums. |
