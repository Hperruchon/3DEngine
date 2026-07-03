# ADR 0014 — Manifold backend native-interop posture

## Status

Accepted — 2026-07-03

## Context

ADR-0012 pinned the V1 geometry wiring — the capability surface (`IGeometryBackend.TryGet<T>()`, `IMeshOps.CreateBox`, `IGeometryQuery.GetBoundingBox`, `BackendCapabilities`), handler-to-backend access, the `Document.Bodies` projection, and deterministic body identity — and shipped it in v0.11 against an in-process managed stub (`InProcessMeshBackend`). ADR-0012 was deliberately **backend-agnostic**: its Non-goals name *"Manifold-specific decisions (binding choice, native lifecycle, threading model). Separate ADR,"* and §7 states the stub "is *not* the Manifold backend — it is the wiring slice." This is that separate ADR.

The roadmap's P7b is gated on it: *"Replace the in-process managed stub with a real Manifold-backed `IGeometryBackend` behind the same capability interfaces. Needs a follow-on ADR for the native-interop posture (binding choice, native lifecycle, threading) before sizing."* Nothing about the capability surface is reopened here — a Manifold backend implements the interfaces already shipped in v0.11 and replaces `new InProcessMeshBackend()` at the host composition roots. What is open is purely how managed code reaches native Manifold and how that native dependency is contained, sequenced, and shipped.

One house-style tension frames the whole decision. Every native dependency in this solution to date (Vulkan via `Vortice.Vulkan`, SDL3 via `Alimer.Bindings.SDL`) is bound **exclusively through a mature managed NuGet package** — there is zero hand-authored P/Invoke in the tree. That precedent held because those libraries have first-class, RID-complete managed bindings. Manifold does not: the one managed binding on NuGet (`ManifoldNET`) is a single-maintainer `1.0.7-alpha` targeting `net8.0`/`netstandard2.0` with no credible multi-RID native payload. This ADR therefore makes a scoped, deliberate departure from the NuGet-only precedent, and confines the departure so it cannot leak into the core kernel.

## Decision

### 1. Binding: hand-authored `[LibraryImport]` P/Invoke against `manifoldc`

Bind to Manifold through its official C FFI wrapper (`manifoldc`, built with `MANIFOLD_CBIND=ON`) using source-generated `[LibraryImport]` P/Invoke. `manifoldc` is the blessed FFI entry point upstream — the official Python (`manifold3d`) and JS (`manifold-3d`) bindings sit on the same C core — so we bind at a stable, supported boundary rather than reverse-engineering the C++ ABI.

This is the repository's **first hand-authored native binding**. The ADR records that as an intentional exception to the NuGet-only native posture, justified because no mature managed Manifold binding exists. The exception is contained by §4: all P/Invoke lives in one backend project and touches nothing else.

- **Rejected — `ManifoldNET` NuGet.** Alpha (`1.0.7-alpha`, published 2024-08), single low-profile maintainer, wrong TFMs (`net8`/`netstandard2.0`/`net40`), and no evidence it ships the multi-RID native payload the engine needs. Betting the geometry kernel on it is higher risk than owning a thin binding over the stable C API. Revisit if it reaches a stable, RID-complete release.
- **Rejected — C++/CLI mixed assembly.** Windows-only; forecloses the cross-platform posture the engine keeps open (§5).
- **Rejected — out-of-process native sidecar (gRPC/pipe).** Massive overkill for V1's `CreateBox` + `GetBoundingBox`; reintroduces a transport where ADR-0011 already fixes one.

### 2. Native lifecycle: explicit `IDisposable`, backend owns every handle

The Manifold backend owns all native handles and implements `IDisposable` with reverse-order teardown, mirroring the house `GraphicsDevice.Dispose()` precedent. `manifoldc` returns `Manifold*` / `ManifoldMeshGL*` pointers the caller must free; each is wrapped in a `SafeHandle` or freed immediately after its managed value is extracted (e.g. build native solid → read `Aabb` → free). One backend instance per host, alive for the process lifetime, disposed on host shutdown.

- **Rejected — GC/finalizer-only cleanup.** Leaks native memory under load and violates the house explicit-dispose pattern.
- **Rejected — per-command native context spin-up/teardown.** Needless churn; the backend is a process-lifetime cache (ADR-0001 §"backends are caches").

### 3. Threading: single-threaded from the engine's view

The backend is treated as single-threaded. All Manifold calls execute inside `CommandBus`'s serial commit section, which already serialises via `SemaphoreSlim(1, 1)`, so concurrent backend entry is structurally impossible in V1 and no engine-level lock is added. Manifold parallelises internally via Intel TBB (`MANIFOLD_PAR`), which is the primary determinism hazard; this ADR mandates the **most-serial Manifold configuration available** (serial build or lowest-parallelism setting) and flags the resulting determinism guarantee as *unverified* rather than asserted (see Open challenges).

- **Rejected — exploiting TBB parallelism for throughput.** Reintroduces reduction-order nondeterminism and threatens the replay-determinism gate; unmotivated at V1 scale.
- **Rejected — a bespoke lock around the backend.** Redundant given the serial commit; adds contention machinery with no caller that needs it.

### 4. Project placement: a new `Engine.Geometry.Manifold`, native deps out of `Engine.Core`

The Manifold backend lives in a **new project `Engine.Geometry.Manifold`** that references only `Engine.Contracts` and is wired in at the host composition roots. `Engine.Core` gains **no** native dependency.

`Engine.Core` is the design-truth kernel every client references transitively. Pulling a native `.dll`/`.so`/`.dylib`, `AllowUnsafeBlocks`, and P/Invoke into it would contaminate the pure managed core and its RID-neutrality, and force the native payload onto every downstream consumer. A sibling backend project satisfies the same capability surface, slots in exactly where `new InProcessMeshBackend()` appears today, and confines the native blast radius. The managed `InProcessMeshBackend` **stays in `Engine.Core`** as the deterministic default and replay backend. Only the host composition roots (`Engine.Api.Http/EngineHost.cs`, `Engine.Cli/Cli.cs`, and the relevant test fixtures) add the reference — client dependency rules are unchanged.

- **Rejected — extending `Engine.Core/Geometry/`.** Folder-simpler, but injects native/unsafe code into the core kernel and every project that references it, against the spirit of the boundary rules (ADR-0004, CLAUDE.md dependency rules).

### 5. Cross-platform and native distribution: RID-agnostic build, pinned native artifact

The native `manifoldc` binary ships via the standard NuGet `runtimes/<rid>/native/` convention and is consumed by a **RID-agnostic build** — no `RuntimeIdentifier`/`RuntimeIdentifiers` in any csproj, preserving the repo's "no RID set anywhere" posture (.NET native probing resolves `runtimes/<rid>/native` at publish/run without pinning a RID). `win-x64` ships first; `linux-x64` is a fast-follow. Because no managed NuGet credibly ships `manifoldc` for all targets, the native payload is treated as a **repo-controlled, version-pinned, checksum-verified artifact tied to a specific Manifold commit**, so the *same* binary underlies the replay path on every CI runner and developer machine.

- **Rejected — committing a raw `.dll` into the source tree.** Unversioned, no RID story, no provenance.
- **Rejected — setting `RuntimeIdentifiers` in csproj.** Breaks the house convention and forces RID-specific builds throughout.
- **Rejected — relying on a system-installed `manifoldc`.** Non-hermetic; breaks CI reproducibility and the replay baseline.

## Consequences

Descriptive — the following are what the implementing task (TASK-0012) will touch, not commitments made by this ADR.

- **New project `Engine.Geometry.Manifold` + csproj.** References only `Engine.Contracts`; enables `AllowUnsafeBlocks` (or relies on `LibraryImport` marshalling); consumes native `manifoldc` via `runtimes/<rid>/native`; keeps the warnings-as-errors posture consistent with `Engine.Core`.
- **New `ManifoldGeometryBackend : IGeometryBackend, IMeshOps, IGeometryQuery, IDisposable`,** modelled on `InProcessMeshBackend`, storing bodies keyed by `BodyHandle.Id`.
- **Host wiring swaps `new InProcessMeshBackend()`.** `Engine.Cli/Cli.cs` `BuildEngine()` and `Engine.Api.Http/EngineHost.cs` construct the Manifold backend and add the project reference; `EngineHost.Backend` generalises from `InProcessMeshBackend` to `IGeometryBackend` and is disposed on host shutdown.
- **Replay gate posture.** The canonical `ReplayDeterminismGateTests` stays on the deterministic managed stub; the native path is exercised by a separate pinned-RID Manifold round-trip test, so the core determinism gate never depends on native floating-point reproducibility.
- **Two new diagnostic codes** (registered in TASK-0012's change, per the same-PR rule, not by this ADR): `E-GEOM-BACKEND-INIT` (native lib not found / load failure / version mismatch, raised at backend construction or first use) and `E-GEOM-NATIVE-OP` (a native Manifold operation failed or returned a degenerate/non-manifold result). The existing `E-GEOM-CAP-MISSING`, `E-GEOM-INVALID-PARAM`, and `E-GEOM-BODY-NOT-FOUND` are reused unchanged.
- **First native-resource tests in the repo.** The xunit fixture must dispose the backend; CI runners need the pinned `manifoldc` artifact for any test that instantiates the Manifold backend.
- **No `Engine.Contracts` change.** The capability surface is unchanged; `ManifoldGeometryBackend` implements the interfaces shipped in v0.11. Any proposed Contracts change during implementation triggers the CLAUDE.md stop-and-ask rule.

## Non-goals

- The actual Manifold backend implementation. That is TASK-0012, sized against this ADR's Consequences.
- B-Rep, exact booleans, fillet/chamfer, feature-IDs. Reserved by ADR-0001; not implemented here.
- Multi-backend selection policy (mesh + B-Rep concurrent). Deferred by ADR-0012; V2.
- Tessellation/preview meshes for render-capable clients. Deferred by ADR-0012 §Open challenges until a client consumes them.
- A cross-platform byte-identical replay guarantee. V1 replay is validated on one pinned RID + native build (see Open challenges).

## Validation rules

1. `Engine.Contracts` public shape is unchanged by the Manifold work. CI: contract-gate stays green with no `Engine.Contracts/**` diff.
2. `Engine.Core` gains no native dependency. Native code, `AllowUnsafeBlocks`, and P/Invoke live only in `Engine.Geometry.Manifold`. CI: dependency-direction check; `Engine.Core.csproj` has no `runtimes/`/native reference.
3. No `RuntimeIdentifier`/`RuntimeIdentifiers` appears in any csproj.
4. The canonical replay-determinism gate runs against the managed stub backend; the native path is covered by a separate, explicitly pinned-RID test.
5. Capability negotiation is unchanged: handlers reach Manifold only through `backend.TryGet<T>()`; a missing capability still surfaces `E-GEOM-CAP-MISSING` (ADR-0012 rule 1).

## Open challenges

- **Floating-point / replay determinism across OS + CPU.** Manifold's robustness comes from Shewchuk-style adaptive exact predicates — topological robustness, not documented bit-for-bit cross-platform reproducibility. V1 replay is validated on a single pinned RID + native build; cross-platform byte-identical replay is open.
- **TBB parallelism nondeterminism.** If the most-serial configuration is later relaxed for performance, reduction ordering may perturb output. Any such relaxation is ADR-worthy.
- **RID matrix beyond `win-x64`.** `linux-x64` and `osx-arm64`, and how each native artifact is built, signed, and pinned, are deferred.
- **Native-lib versioning / upgrade policy.** The native payload is pinned to a Manifold commit; upgrading it can shift geometry output and invalidate the replay baseline, so upgrades are ADR-worthy rather than routine dependency bumps.
- **B-Rep / booleans / fillets.** Reserved by ADR-0001; resurfaces when a capability beyond mesh is motivated.
- **Tessellation-for-clients.** Already flagged by ADR-0012; resurfaces once Manifold produces real meshes a client wants to draw.
