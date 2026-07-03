# TASK-0012 — Manifold geometry backend swap-in (P7b)

## Status

Ready — ADR-0014 accepted 2026-07-03. Not started. Prerequisite for implementation: a pinned, checksummed native `manifoldc` artifact (see §6 and Notes).

## Context

P7a (TASK-0011, v0.11) shipped the geometry *posture* — the capability surface (`IGeometryBackend.TryGet<T>()`, `IMeshOps.CreateBox`, `IGeometryQuery.GetBoundingBox`, `BackendCapabilities`), handler-to-backend access, `Document.Bodies`, deterministic body identity, `body.created`, and the `subscription.reset` snapshot extension — behind an in-process managed stub (`InProcessMeshBackend`). ADR-0012 §7 shipped that stub explicitly as "the wiring slice … not the Manifold backend."

P7b swaps a real Manifold-backed backend in behind the *same* surface. Nothing about the capability surface is reopened; a Manifold backend implements the interfaces already shipped in v0.11 and replaces `new InProcessMeshBackend()` at the host composition roots.

One ADR pre-decides every load-bearing native-interop call:

- **ADR-0014** — Manifold backend native-interop posture. Binding = hand-authored `[LibraryImport]` P/Invoke against Manifold's official `manifoldc` C API (the repo's first hand-authored native binding); lifecycle = explicit `IDisposable`, backend owns all native handles, eager per-op frees; threading = single-threaded via the existing `CommandBus` serial commit, most-serial Manifold config; placement = new `Engine.Geometry.Manifold` project referencing only `Engine.Contracts`, native deps kept out of `Engine.Core`; distribution = `runtimes/<rid>/native`, RID-agnostic build, pinned native artifact.

ADR-0001's rules (capability-typed backends, opaque `BodyHandle`, kernel API internal to handlers, replay against a fresh backend) and ADR-0012's wiring remain in force.

## Goal

Ship `ManifoldGeometryBackend` in a new `Engine.Geometry.Manifold` project, bound to native `manifoldc` via P/Invoke, wired as the default backend at the CLI and HTTP hosts. `CreateBox` produces a real Manifold solid; `GetBoundingBox` returns the Manifold-computed AABB — through every surface (CLI, HTTP, WebSocket), byte-parity with the stub for the axis-aligned box case.

After this TASK, the engine's default geometry is native Manifold, the managed `InProcessMeshBackend` remains as the deterministic default/replay backend, and the canonical replay-determinism gate is unchanged (it stays on the stub; the native path gets its own pinned-RID round-trip test).

## Scope (in)

### 1. New project `Engine.Geometry.Manifold` (gated by ADR-0014 §4, §5)

- `Engine.Geometry.Manifold/Engine.Geometry.Manifold.csproj`:
  - `TargetFramework` = `net10.0` (single TFM, house convention). **No** `RuntimeIdentifier`/`RuntimeIdentifiers` (ADR-0014 §5, Validation rule 3).
  - `ProjectReference` to `Engine.Contracts` **only** (Validation rule 2). No reference to `Engine.Core`.
  - `AllowUnsafeBlocks` = `true` if pointer marshalling needs it (prefer `[LibraryImport]` marshalling to minimise unsafe surface).
  - Consumes the native `manifoldc` payload via the standard `runtimes/<rid>/native/` layout (see §6).
  - Match `Engine.Core`'s warnings-as-errors / nullable posture.
- Register the project in `3DEngine.sln`.

### 2. Native binding layer (gated by ADR-0014 §1, §2)

- `Engine.Geometry.Manifold/Native/ManifoldNative.cs` — source-generated `[LibraryImport("manifoldc")]` declarations for the **minimal** surface P7b needs. Exact export names verified against `manifoldc.dll` (ManifoldNET 1.0.7 payload); confirm parameter order + return types against the pinned `manifoldc.h`:
  - `manifold_manifold_size()` / `manifold_box_size()` — allocation sizes (the **caller-allocates-memory** convention: each constructor takes a `void* mem` buffer sized by its companion `*_size()` call — bind it faithfully, this is not a `malloc`-style API).
  - `manifold_cube(void* mem, double x, double y, double z, int center)` — build the box; pass `center = 1` so the AABB is origin-centered and matches the stub.
  - `manifold_bounding_box(void* mem, ManifoldManifold* m)` → `ManifoldBox*`; then `manifold_box_min(ManifoldBox*)` / `manifold_box_max(ManifoldBox*)` → `ManifoldVec3` for the AABB corners.
  - `manifold_delete_manifold(...)` / `manifold_delete_box(...)` — frees.
  - `manifold_status(...)` (error check) and `manifold_is_empty(...)` (degenerate detection) → map failures/degenerates to `E-GEOM-NATIVE-OP`.
  - Marshal `ManifoldVec3` as a blittable 3×`double` struct.
- `Engine.Geometry.Manifold/Native/ManifoldSafeHandle.cs` (or equivalent) — `SafeHandle` wrappers so every native object has a single owner and deterministic release; reverse-order teardown (ADR-0014 §2).

### 3. `ManifoldGeometryBackend` (gated by ADR-0014 §2, §3, §4)

- `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs` — `sealed class ManifoldGeometryBackend : IGeometryBackend, IMeshOps, IGeometryQuery, IDisposable`:
  - `Capabilities => BackendCapabilities.Mesh | BackendCapabilities.Query` (parity with the stub).
  - `TryGet<T>()` returns `this as T` for `IMeshOps` / `IGeometryQuery`, else null.
  - `IMeshOps.CreateBox(handle, params)` — builds a centered Manifold cube of `SizeX×SizeY×SizeZ`, stores the native handle keyed by `handle.Id`; throws if the handle already exists (programmer error, mirrors the stub's contract). Wrap native failures / degenerate results in `E-GEOM-NATIVE-OP`.
  - `IGeometryQuery.GetBoundingBox(handle)` — computes the Manifold bounding box, returns `Aabb`; throws `KeyNotFoundException` for an unknown handle (the query handler already maps that to `E-GEOM-BODY-NOT-FOUND`).
  - `Dispose()` frees all retained native handles in reverse order; idempotent.
  - Force Manifold's most-serial configuration at construction (ADR-0014 §3); on native load/init/version-mismatch failure, surface `E-GEOM-BACKEND-INIT`.
  - No engine-level lock — all calls run inside the bus's serial commit (ADR-0014 §3).

### 4. Host wiring (gated by ADR-0014 §4)

- `Engine.Api.Http/EngineHost.cs`:
  - Add a `ProjectReference` from `Engine.Api.Http` to `Engine.Geometry.Manifold`.
  - Construct `ManifoldGeometryBackend` instead of `InProcessMeshBackend`; pass to `CommandBus` + `QueryBus`.
  - Generalise the `Backend` property type from `InProcessMeshBackend` to `IGeometryBackend`.
  - **Dispose** the backend on host shutdown (register with DI as a disposable singleton / hook application stopping).
- `Engine.Cli/Cli.cs` `BuildEngine()`:
  - Add the `ProjectReference`; construct `ManifoldGeometryBackend`; pass to both buses. Dispose it before the process exits (the CLI builds one engine per invocation).
- Client dependency rules unchanged: `Engine.Cli` / `Engine.Api.Http` still reference `Engine.Core` + `Engine.Contracts`, now additionally `Engine.Geometry.Manifold` as a host composition root (a backend provider, not a client). `Engine.Core` gains **no** reference to the Manifold project.

### 5. Diagnostic codes registered (three places)

Per ADR-0014 Consequences; house rule = same PR as the code that raises them:

- `docs/diagnostics.md` — append rows for `E-GEOM-BACKEND-INIT` and `E-GEOM-NATIVE-OP` (GEOM subsystem, already in §Conventions).
- `Engine.Core/DiagnosticCodes.cs` — add the two constants. (Codes live in the shared registry class even though the raising code is in `Engine.Geometry.Manifold`; the backend references the constants — decide whether the backend takes a dependency on the constants or re-declares; prefer referencing `Engine.Contracts`-level constants if that is where shared codes belong. If `DiagnosticCodes` currently lives in `Engine.Core`, and the Manifold project cannot reference `Engine.Core`, **move the diagnostic-code constants to `Engine.Contracts`** or introduce a contracts-level home — flag this in review; it is the one structural wrinkle.)
- `Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs` — mirror the two new codes so `/schema/diagnostics` stays complete.

### 6. Native artifact: build + distribution (gated by ADR-0014 §5)

**Portability reframe (ADR-0011).** The native `manifoldc` only has to exist where the engine *executes* geometry — the `engine-api-http` process — not on any client. Clients reach the engine over HTTP/WS and need nothing native, so the multi-RID concern below applies only to the engine host and to dev/CI machines, not to consumers.

**Produce the binaries** — no official prebuilt `manifoldc` exists (elalish/manifold ships source + Python wheels only; confirmed via GitHub releases and discussion [#1087](https://github.com/elalish/manifold/discussions/1087)). Build from source, serial, **once per RID**:

- **Build recipe.** Clone `elalish/manifold` at the pinned tag (**v3.5.2** — latest release as of 2026-06-27, confirmed double-precision; the workflow default) and configure:
  ```
  cmake -DMANIFOLD_CROSS_SECTION=ON -DMANIFOLD_CBIND=ON -DMANIFOLD_PAR=OFF \
        -DMANIFOLD_TEST=OFF -DBUILD_SHARED_LIBS=ON -B build   # add -A x64 on Windows
  cmake --build build --config Release
  ```
  Flags verified against v3.5.2's `CMakeLists.txt`. **`MANIFOLD_CROSS_SECTION=ON` is mandatory** — `MANIFOLD_CBIND` is a `cmake_dependent_option` forced OFF when cross-section is OFF, so disabling cross-section silently drops the `manifoldc` target (this sank the first CI run). `MANIFOLD_PAR=OFF` (a boolean in v3.5.2; default) is the serial, no-TBB backend per ADR-0014 §3. `MANIFOLD_TEST=OFF` skips the test suite. There is no `MANIFOLD_EXPORT` in v3.5.2 (assimp isn't pulled by default). Cross-section pulls Clipper2 (small, auto-fetched via `MANIFOLD_DOWNLOADS=ON`); a shared build emits `manifoldc` + core `manifold` (+ Clipper2) — vendor whatever set the build produces into `runtimes/<rid>/native/`. Record the exact commit + SHA256. Pin `bindings/c/include/manifold/manifoldc.h` as the binding's source of truth.
- **Prototype-only shortcut (NOT the pinned artifact): the `ManifoldNET` 1.0.7-alpha nupkg** already contains `runtimes/win-x64/native/manifoldc.dll` (1.5 MB) + `tbb12.dll`. Handy to validate the P/Invoke surface in an afternoon, but it is win-x64 only, **single-precision (`float`)**, and a **TBB/parallel** build (violates ADR-0014 §3's most-serial mandate) with alpha/opaque provenance — do not ship it as production. Its DLL is what §2's export names were verified against.

**Distribute — a multi-RID NuGet built by a CI matrix (the "usable from any machine" decision).** The idiomatic .NET path and the repo's own precedent (Vortice.Vulkan, Alimer.Bindings.SDL are consumed this way):

- A CI **matrix** builds `manifoldc` per RID — `windows-latest` → `win-x64`, `ubuntu-latest` → `linux-x64` (**required**: CI runs on ubuntu, so linux-x64 is critical-path, not a fast-follow), `macos-latest` → `osx-arm64` (optional first cut).
- Pack all RIDs into **one** NuGet under `runtimes/<rid>/native/`, owned by us (ManifoldNET is unmaintained), versioned to the pinned Manifold commit + recorded SHA256s.
- `Engine.Geometry.Manifold` consumes it via `PackageReference`; .NET native-probing selects the platform's binary at publish/run — **no `RuntimeIdentifier` in any csproj** (ADR-0014 §5 holds), and no consumer toolchain. This resolves the Linux-CI collision: the `build-and-test` job gets the linux-x64 payload from the package. (Fallback while the package isn't ready: keep the host default on the stub, or skip Manifold tests when the native lib is absent — see §7 — until the runner can load Manifold.)
- Feed choice (nuget.org / private feed / committed local `.nupkg`) is a smaller sub-decision; a private or local feed is fine to bootstrap.

**Interim bootstrap.** Until the NuGet exists, committing per-RID binaries under `runtimes/<rid>/native/` (git, or LFS if size bites) is an acceptable stopgap — same native-probing, weaker versioning.

**Deployment.** For the server itself, a container image with the linux-x64 native baked in makes "any machine runs the engine" = "any machine runs the container."

**Pipeline status (2026-07-03).** The build+pack pipeline is drafted: `.github/workflows/build-manifold-native.yml` (manual `workflow_dispatch`; per-RID matrix build → multi-RID NuGet pack) plus the packaging project `eng/manifold-native/Concr3de.Manifold.Native.csproj`. The **pack half is verified locally** (produces `runtimes/<rid>/native/…` correctly). The **build half is unverified** — it needs a runner/toolchain; confirm the `manifold_ref` tag is a real double-precision release and adjust the "Stage native libs" `find` if the output paths differ. Publish-to-feed is left disabled pending the feed choice.

### 7. Tests (in `Engine.Tests/` unless noted)

> **Native-availability gate.** CI is Linux (§6). Gate every Manifold-native test behind a platform/native-availability check (e.g. skip when `NativeLibrary.TryLoad("manifoldc", …)` fails) so a runner lacking the matching-RID payload stays green rather than erroring. The canonical replay gate stays on the stub and is unaffected.

**Manifold backend (pinned-RID; require the native artifact):**

- `Engine.Tests/Geometry/ManifoldGeometryBackendTests.cs`:
  - `Capabilities_Are_Mesh_And_Query`.
  - `TryGet_Returns_Self_For_Mesh_And_Query_Null_For_BRep`.
  - `CreateBox_Then_GetBoundingBox_Returns_Origin_Centered_Aabb` — asserts parity with the stub's exact `(-X/2..+X/2)` result within a tight tolerance.
  - `GetBoundingBox_For_Unknown_Handle_Throws_KeyNotFound`.
  - `Dispose_Releases_Native_Handles_And_Is_Idempotent`.
  - `Duplicate_Handle_CreateBox_Throws`.

**Native failure surfacing:**

- `E-GEOM-BACKEND-INIT` is returned/surfaced when the native library is unavailable or fails to initialise (simulate via a missing/renamed native probe path or an init guard).
- `E-GEOM-NATIVE-OP` is surfaced when a native op returns a degenerate/null result.

**End-to-end (now native through the host):**

- `Engine.Tests/Cli/CliCreateBoxScenarioTests.cs` and `Engine.Tests/Http/HttpCreateBoxScenarioTests.cs` — existing scenario tests now run against the Manifold-backed host; assert the same `Aabb` result. (These become the first host tests that need the native artifact.)

**Replay determinism (posture unchanged):**

- `Engine.Tests/ReplayDeterminism/…` — the canonical gate **stays on `InProcessMeshBackend`** (deterministic; no native FP dependency). Do not repoint it at Manifold.
- Add a **separate** `ManifoldReplayRoundTripTests.cs` (pinned-RID) that replays a `CreateBox` log against a fresh `ManifoldGeometryBackend` twice and asserts the two runs match — proving the native path is reconstructible without making the core gate depend on cross-platform native reproducibility (ADR-0014 §Open challenges).

### 8. Documentation

- `docs/CURRENT-STATE.md` — new `v0.14` entry (P7b, TASK-0012, ADR-0014). (No `v0.13`-numbered geometry entry exists; v0.12/v0.13 were docs-only governance. The next shipped-milestone number is v0.14.)
- `docs/roadmap.md` — move `P7b` from V1.x Pending to **Shipped** (`- P7b — Manifold backend (swap-in) (ADR-0014). v0.14, TASK-0012.`). When V1.x Pending empties, note the advance per the roadmap's own rule.
- `docs/diagnostics.md` — the two appended rows (also in §5).
- This TASK's `Status` flips to `Done — shipped in commit <hash>` in the close commit.

## Scope (out)

- **B-Rep, exact booleans, fillet/chamfer, feature-IDs.** Reserved by ADR-0001; each is a future capability under its own TASK (+ ADR amendment if it adds interface methods).
- **Removing / replacing `InProcessMeshBackend`.** The stub stays as the deterministic default and the replay-gate backend (ADR-0014 §4). This TASK adds a backend; it does not delete one.
- **RIDs beyond win-x64 + linux-x64.** `osx-arm64` and a full cross-platform matrix are a fast-follow TASK (ADR-0014 §Open challenges). Note: `linux-x64` is **not** deferrable here — CI runs on ubuntu, so it is in scope (§6); `win-x64` covers the dev machine.
- **WASM single-artifact binding.** Manifold ships a WebAssembly build (used by `manifold-3d`). One `manifold.wasm` via a .NET WASM host (e.g. Wasmtime) would be machine-independent with bit-identical results across OS/arch — but it **replaces ADR-0014 §1's P/Invoke binding**, so it is an ADR revision, not this TASK. Revisit if the per-RID matrix becomes a maintenance drag or cross-platform bit-identical replay becomes a hard requirement.
- **RID-aware backend fallback (Manifold-where-present-else-stub).** The capability design lets a host select the managed stub on RIDs without a native payload, so the engine still runs everywhere — but geometry then differs by machine (stub vs Manifold), colliding with replay/determinism. Deferred; worth a `docs/open-questions.md` entry as a host-composition + determinism decision, not this slice.
- **Cross-platform byte-identical replay guarantee.** V1 replay is validated on one pinned RID (ADR-0014 §Open challenges). The core gate stays on the stub.
- **Exploiting Manifold/TBB parallelism for throughput.** Most-serial config only; relaxing it is ADR-worthy (ADR-0014 §3).
- **Tessellation / preview meshes for render clients.** Deferred by ADR-0012 §Open challenges until a client consumes them.
- **Multi-backend selection policy.** One backend per process (ADR-0011); V2.
- **New geometry commands** (translate, boolean, etc.). Out of phase.

## Inputs

- **ADR-0014 — Manifold backend native-interop posture** (Accepted 2026-07-03). Primary; pre-decides binding, lifecycle, threading, placement, distribution.
- ADR-0012 — geometry backend wiring; the capability surface this backend implements, `body.created`, snapshot `bodies`.
- ADR-0001 — geometry kernel posture (capability-typed backends, opaque `BodyHandle`, replay against fresh backend).
- ADR-0011 — server-default deployment; one backend per process; CLI/HTTP parity.
- ADR-0005 — replay invariants the native round-trip test respects.
- TASK-0011 — the stub, handlers, tests, and host wiring this swaps behind.

## Outputs

- `engine apply CreateBox --sizeX 10 --sizeY 20 --sizeZ 30 && engine query GetBoundingBox --bodyId <guid>` returns the Manifold-computed AABB via the CLI.
- `POST /commands` (`CreateBox`) + `POST /queries` (`GetBoundingBox`) return the same result over HTTP (parity per ADR-0011).
- A WebSocket subscriber reconnecting after a `CreateBox` still receives a `subscription.reset` whose `snapshot.bodies` includes the body (unchanged behaviour, now native-backed).
- `/schema/diagnostics` mirrors `E-GEOM-BACKEND-INIT` and `E-GEOM-NATIVE-OP`.
- `Engine.Core` has no dependency on `Engine.Geometry.Manifold`; no csproj sets a RID.
- The canonical replay-determinism gate is unchanged (stub-backed); a separate pinned-RID Manifold round-trip test passes.
- `dotnet build` + `dotnet test` green on a runner with the pinned `manifoldc` artifact.
- `docs/CURRENT-STATE.md` v0.14 entry; `docs/roadmap.md` shows P7b Shipped.

## Files

**Created:**
- `Engine.Geometry.Manifold/Engine.Geometry.Manifold.csproj`
- `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs`
- `Engine.Geometry.Manifold/Native/ManifoldNative.cs`
- `Engine.Geometry.Manifold/Native/ManifoldSafeHandle.cs`
- `Engine.Tests/Geometry/ManifoldGeometryBackendTests.cs`
- `Engine.Tests/ReplayDeterminism/ManifoldReplayRoundTripTests.cs`
- `runtimes/win-x64/native/manifoldc.<ext>` (vendored/pinned native payload; per the distribution approach chosen in §6)
- `.github/workflows/build-manifold-native.yml` — per-RID matrix build + multi-RID NuGet pack (already drafted; manual-only).
- `eng/manifold-native/Concr3de.Manifold.Native.csproj` (+ `_Package.cs`) — the packaging project (already created; pack verified locally).
- `tasks/TASK-0012-manifold-backend-swap-in.md` (this file)

**Modified:**
- `3DEngine.sln` — add the new project.
- `Engine.Api.Http/EngineHost.cs` — construct + dispose Manifold backend; `Backend` typed `IGeometryBackend`; add project reference.
- `Engine.Api.Http/Engine.Api.Http.csproj` — `ProjectReference` to `Engine.Geometry.Manifold`.
- `Engine.Cli/Cli.cs` — construct + dispose Manifold backend in `BuildEngine()`.
- `Engine.Cli/Engine.Cli.csproj` — `ProjectReference` to `Engine.Geometry.Manifold`.
- `Engine.Core/DiagnosticCodes.cs` — two new constants (or their new home; see §5 wrinkle).
- `Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs` — mirror the two codes.
- `Engine.Tests/Cli/CliCreateBoxScenarioTests.cs`, `Engine.Tests/Http/HttpCreateBoxScenarioTests.cs` — now native-backed (assertions unchanged; may need artifact fixture).
- `Engine.Tests/Engine.Tests.csproj` — reference `Engine.Geometry.Manifold`; ensure the native artifact is copied to the test output.
- `.github/workflows/ci.yml` — provision the pinned `manifoldc` artifact for test jobs.
- `docs/diagnostics.md`, `docs/CURRENT-STATE.md`, `docs/roadmap.md`.
- `tasks/TASK-0012-manifold-backend-swap-in.md` — Status flip in the close commit.

**Do not touch:**
- `Engine.Core/Geometry/InProcessMeshBackend.cs` — the stub stays as-is (default/replay backend).
- The capability interfaces in `Engine.Contracts/Geometry/` — unchanged (Validation rule 1). Any change triggers stop-and-ask.
- The canonical replay-determinism gate test — stays stub-backed.
- ADRs 0001–0013 (in force; not amended).
- `3DEngine/`, `BlazorApp/`, `3DEngine.Core/`, `Vortice.Vulkan.*` (CLAUDE.md "Do not touch").
- The WebSocket transport, command/query handlers, and `Document` projection — the swap is backend-only.

## Tests

(Listed under §7. Existing 70 + the new Manifold backend, native-failure, and round-trip tests. The existing scenario/E2E tests are retargeted onto the native backend rather than added.)

## Acceptance criteria

1. `dotnet build` succeeds (with the new project in the solution).
2. `dotnet test` passes on a runner provisioned with the pinned `manifoldc` artifact.
3. `ManifoldGeometryBackend` implements `IGeometryBackend`/`IMeshOps`/`IGeometryQuery`/`IDisposable`; `CreateBox` → `GetBoundingBox` returns the origin-centered AABB with stub parity.
4. `Engine.Core` has **no** project reference to `Engine.Geometry.Manifold`; `Engine.Geometry.Manifold` references **only** `Engine.Contracts` (dependency-direction gate green).
5. No `RuntimeIdentifier`/`RuntimeIdentifiers` appears in any csproj.
6. The canonical replay-determinism gate runs against `InProcessMeshBackend` and is unchanged; a separate pinned-RID Manifold round-trip test passes.
7. `E-GEOM-BACKEND-INIT` and `E-GEOM-NATIVE-OP` are registered in all three places and mirrored by `/schema/diagnostics`.
8. The Manifold backend is disposed on host shutdown (HTTP) and per-invocation (CLI) — native handles are released.
9. `Engine.Contracts` public shape is unchanged (contract-gate green, no `Engine.Contracts/**` diff).
10. `docs/CURRENT-STATE.md` v0.14 entry exists; `docs/roadmap.md` shows P7b Shipped.

## Notes for the implementer

- **The native artifact is the critical-path risk, not the C#.** Resolve `manifoldc` acquisition (§6) *first* — a pinned build with `MANIFOLD_CBIND=ON`, most-serial parallelism, checksum recorded. Everything else is straightforward once the binary and its header are in hand. If acquisition proves heavy, that is worth surfacing before the C# work, not after.
- **Bind to the pinned header, not from memory.** `manifoldc` uses a caller-allocates-memory convention (`void* mem` + `*_size()`), opaque `ManifoldManifold*`/`ManifoldBox*` pointers, and `ManifoldVec3` returns. Read the actual header of the pinned commit; do not guess symbol names or signatures. Keep the P/Invoke surface minimal — only what `CreateBox` + `GetBoundingBox` need.
- **Binding spike already ran (2026-07-03; P/Invoke against the vendored ManifoldNET DLL, scratchpad) — the surface is validated end-to-end and three findings carry into the real backend:**
  1. **It works.** `manifold_manifold_size`/`box_size` → `manifold_cube(mem, x, y, z, center=1)` → `manifold_bounding_box(mem, m)` → `manifold_box_min`/`max` (returned `Vec3` by value) produced the exact AABB `(-5,-10,-15)..(5,10,15)` for a centered 10×20×30 box, with `manifold_status == 0`. Symbol names, the caller-allocates convention, struct-by-value return marshalling, and `center=1` origin-centering all confirmed on win-x64.
  2. **Precision is version-sensitive — pin accordingly.** The 2024-era ManifoldNET DLL's C API is **single-precision** (`float` x/y/z; `Vec3 { float, float, float }`); Manifold **master is double-precision** (matching the §2 signatures). Target a Manifold version whose C API is `double` so `BoxParameters`/`Aabb` (both `double`) map 1:1 with no narrowing. Concretely justifies ADR-0014 §5's commit-pinning.
  3. **Memory ownership: `manifold_delete_*` frees the caller buffer itself.** Verified by isolation: delete-only → clean (exit 0); delete **+** `Marshal.FreeHGlobal` → **double-free crash** (exit 127); free-only → leaks. So the `SafeHandle.ReleaseHandle` must call the native delete and must **not** separately free the `mem` buffer. Prefer a design where one owner (the SafeHandle) holds both the buffer and the native object as a unit — or drop `AllocHGlobal` entirely if the pinned API exposes a self-allocating constructor. Re-verify against the pinned source, since a double-precision build may differ again.
- **Skeleton already scaffolded (2026-07-03); builds green (0 warnings / 0 errors under `TreatWarningsAsErrors`).** `Engine.Geometry.Manifold/` exists with: the `[LibraryImport]` layer (`AllowUnsafeBlocks=true` is **required** — LibraryImport's struct-by-value return triggers `SYSLIB1062` without it, exactly as ADR-0014 §1 foresaw); `ManifoldSolidHandle : SafeHandle` encoding the delete-only ownership; `ManifoldGeometryBackend` implementing the capability surface (double-precision API, throws plain exceptions pending the diagnostic-code home). It compiles standalone (`dotnet build Engine.Geometry.Manifold`). It is deliberately **not** added to `3DEngine.sln`, **not** host-wired, and **not** tested yet — steps 4–7 integrate those together so the shared solution/CI never sits half-wired.
- **Centered cube for stub parity.** Create the Manifold cube centered at the origin so its AABB is `(-X/2..+X/2)` — matching the stub's convention and letting the E2E/scenario tests keep their expected values.
- **Diagnostic-code home (the one structural wrinkle).** `DiagnosticCodes` lives in `Engine.Core`, which `Engine.Geometry.Manifold` cannot reference. Either (a) move the shared code constants to `Engine.Contracts` (a public-shape change to Contracts — **stop-and-ask before doing it**, per CLAUDE.md), or (b) have the backend raise codes as string literals validated by the existing diagnostics gate, or (c) expose the constants from a contracts-level home. Decide this explicitly and flag it in the PR — it is the only place this TASK bumps a boundary.
- **Hosts default to Manifold ⇒ host tests need the native lib.** Swapping the default backend means the CLI/HTTP scenario tests exercise the native path. Ensure the test project copies `runtimes/<rid>/native` into its output and CI provisions the artifact, or those tests fail on a bare runner. This is expected per ADR-0014; call it out in the PR so reviewers know CI grew a native dependency.
- **Do not touch the core replay gate.** Keeping the canonical determinism gate on the deterministic stub is a deliberate ADR-0014 decision — it insulates the gate from native FP variance. The native reconstructibility is proven by its own separate test.
- **One implementation commit, then one close commit**, matching the repo cadence. The close commit flips `Status` to `Done — shipped in <impl-hash>` and adds the `CURRENT-STATE.md` v0.14 entry.
- **No `Engine.Contracts` changes beyond the diagnostic-code-home decision, if taken.** The capability surface is settled. If any other need arises, stop and flag — it is ADR-gated.
