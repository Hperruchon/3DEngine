# Proposal — Render host direction (options analysis)

> **Recovered 2026-09-20 by TASK-0018.** This file existed only as an untracked file inside
> `stash@{0}`. No commit held it. Register entry R-0013 recorded that condition. The analysis text
> is unchanged.
>
> Two statements in it are now out of date. First, the founding line "we do not build a 3D app" is
> replaced. See `docs/CHARTER.md`. Second, the open decisions below are partly settled. See
> `docs/roadmap.md`, track R. Read this file as a record of the analysis, and not as the current plan.

**Status: Proposal / options analysis — NOT an accepted ADR.** This records a strategic
options analysis (a 7-agent fan-out plus an adversarial critique) for building our own
cross-platform renderer. It exists to frame the decisions; the actual commitments become
their own ADRs + roadmap phases once the open decisions below are made.

## Why

The engine (v0.15) can build and combine solids but can show nothing — no mesh ever leaves
the server, no host draws bodies. The next major goal under consideration is to build our
**own** cross-platform renderer (Windows → macOS → Android), largely **from scratch**,
because the point of the project is learning how rendering actually works — not adopting a
ready-made high-level engine.

This is a **deliberate step past the founding "we do not build a 3D app" line.** It is only
legitimate *as a render host* (ADR-0009): a well-behaved client that observes the engine and
draws, never a second source of truth. It requires its own founding ADR(s) + tasks.

## The enabling insight

A render host consumes the engine **over the wire** (`GET /events` for changes; a future
tessellation query for triangles). The JSON event/query/command surface is the *only*
coupling — so the renderer can be a separate process in **any language**. Language and
graphics-API choice are therefore free to optimize for **learning and quality**, not interop.

## Recommended direction (per fork)

| Fork | Lean | Rationale |
|---|---|---|
| **Graphics API** | **Own thin abstraction over Vulkan 1.3** (dynamic rendering + VMA-style allocator); **MoltenVK** on macOS | One modern explicit API covers Win+Android natively, macOS via translation; you hand-write swapchain/sync/memory/descriptors — maximum "learn the metal." Keep the seam thin so a native Metal backend can be added later. |
| **Connection model** | **Out-of-process over HTTP/WS** (ADR-0011 canonical topology) | Keeps language free; multi-client; renderer restarts independently; references neither kernel. |
| **Windowing** | **SDL3** on all three | The only clean Win+Mac+Android option; abstracts *windowing*, not rendering. (Already used by the repo's existing C# host.) |
| **Platform order** | **Windows → macOS → Android** | Follow the difficulty gradient; each platform inherits a proven core and adds one bounded problem. |
| **Language** | **OPEN — the pivotal decision.** See below. | The critique reopened this; it is genuinely the owner's call. |

## Honest corrections from the critique (these change the picture)

1. **You already own a working C# Vulkan + SDL3 renderer.** `Vortice.Vulkan.SampleFramework/`
   (`GraphicsDevice`/`Swapchain`/`Window`/`Application`, on Vortice.Vulkan + Alimer SDL3) and
   `3DEngine/NativeThreeDEngine.cs` already stand up an instance/device/swapchain/render-pass/
   command-buffer loop **with validation on**. The "~1000-line minimal Vulkan viewer" is not
   fresh work in C# — it exists today. Vortice is a thin ~1:1 binding, so you still hand-write
   mesh upload, pipelines, descriptors, and sync (the GPU learning is fully preserved) — you
   just don't re-pay for plumbing. Under the stated priority *(learning first, then quality,
   then effort)* this is more GPU-learning-per-hour, not less.
2. **The "Rust makes GPU crashes into rendering bugs" rationale is largely false.** Every
   `ash` Vulkan call is `unsafe`; the bugs that break renderers (resource use-after-free,
   missing barriers, wrong image layout, fence/semaphore races) are GPU/driver-side and caught
   by the **validation layers** — identical across C#, C++, Rust. Rust also fights hardest
   exactly where Vulkan is global-shared-mutable (`Arc<Device>`, `Drop` ordering vs
   child-before-parent destruction). Rust is defensible **only if learning Rust is itself a
   goal** — label it a *second* learning goal that taxes the first, not a graphics win. If raw
   Vulkan with the best literature/tooling is the aim, **C++** dominates (every tutorial,
   RenderDoc guide, NDK/MoltenVK sample is C/C++).
3. **R0 (tessellation query) is a real engine phase, not "renderer step one."** It needs:
   extending the `manifoldc` binding with MeshGL export (`manifold_get_meshgl`,
   `manifold_calculate_normals`), a new `ITessellate` capability + `BackendCapabilities`
   flag, a double→float precision contract, schema projection (ADR-0013), a diagnostics
   entry, and its own ADR + TASK passing the replay/schema gates. **Nothing draws until it
   ships.** It is native-backend-only: on the managed stub `GetMesh` of a `Solid` returns
   `E-GEOM-CAP-MISSING`.
4. **Android has real rendering walls, not just a build config.** Surface destruction on
   backgrounding (the `VkSurfaceKHR` becomes *invalid* — rebuild swapchain **and** surface on
   resume); pre-rotation (bake `surfaceTransform` into projection); validation layers ship
   *inside the APK*; and `localhost` on the phone is **not** the dev machine — the engine must
   be reached over **LAN**, with cleartext HTTP blocked by default on Android 9+.
5. **No Android Manifold build exists.** The native RID matrix is win-x64 + linux-x64 +
   osx-arm64. Android tessellation of `Solid`s needs a new **android-arm64 Manifold from-source
   build** (NDK, `MANIFOLD_PAR=OFF`) — its own prerequisite phase before Android rendering.
6. **A foreign-language renderer orphans `3DEngine.Core`.** ADR-0009 designates it as *the*
   peer render kernel. A Rust/C++ host reimplementing scene/camera/material is a second,
   out-of-repo render kernel → needs its own ADR to supersede/deprecate `3DEngine.Core`. A C#
   host **reuses** `3DEngine.Core` and avoids this entirely (another quiet point for C#).
7. **Charter-clean, if done right:** a tessellation **query result** carrying
   `{positions, indices, normals}` is an allowed export DTO (ADR-0012 §Non-goals reserves
   "tessellated previews via explicit queries") — **provided** the DTO lives only in the
   query's `Result` schema and never appears on any event payload or command output.
8. **The carve won't be visible naively.** Ops accumulate and operands stay (no `body.removed`),
   so a Subtract draws A, B, *and* A−B co-located (z-fighting). The fix is a **host-only
   "show latest result" view filter** — legal ephemeral UI (ADR-0007) — but it must be a
   *view over `Document.Bodies`*, never a host-held "deleted" set treated as truth (that would
   be a second source of truth, the top anti-objective).

## Phased plan (thin, demoable slices — reconciled)

- **R0 — Tessellation query (engine phase, own ADR + TASK).** `GetMesh@1` + `ITessellate` +
  flag + double→float contract + schema + diagnostics; MeshGL binding. Witness: headless
  "body X → N triangles", no GPU. Native-backend-gated.
- **R1a — Vulkan bring-up.** Triangle, fixed size, no engine — or *extend the existing Vortice
  host*. (Splitting out because "resizable" already means swapchain + depth recreation.)
- **R1b — Box from the wire.** Subscribe `/events`, project `reset` + `body.created`, fetch the
  mesh, upload, draw one shaded box, resizable + orbit. Acceptance: geometry is not hardcoded.
- **R2 — Multiple bodies + live sync + reconnect.** Carve appears live (native-gated); cursor
  reconnect (`reset` vs `resume`); host-only "show latest" view filter to see the cut.
- **R3 — Flexibility + materials + testability.** Small render-graph + second pass
  (wireframe/outline); material system; **NullRHI draw-list assertions as the gate**;
  golden-image only as best-effort smoke.
- **R4 — Picking → command round-trip.** Click a body → `BodyHandle` → highlight →
  `POST /commands`. Closes the observe/propose loop; renderer stays a pure client.
- **R5 — macOS.** MoltenVK packaging + portability-subset handling; verify Vulkan 1.3 dynamic
  rendering on a pinned MoltenVK (it's the spine, not an optional feature).
- **Pre-R6 — Manifold android-arm64 native build.** New engine/build phase.
- **R6 — Android.** SDL3 app over the identical core; the real walls from correction #4.
- **R7 — "Genuinely good" (stretch).** Handle-cached uploads (every op mints a new immutable
  handle → cache never invalidates), frustum culling, frame-fence deletion queue, optional
  bindless (**Windows/Android-only fidelity tier** — not on MoltenVK).

## Key decisions (the forks)

1. **Language — pivotal.** C# (reuse the existing Vortice/SDL3 host, keep `3DEngine.Core`,
   fastest to pixels, in-process debug loop free) · C++ (best raw-Vulkan literature/tooling;
   leaves C#) · Rust (only if learning Rust is *also* a goal). Recommend deciding via two
   ~1-day spikes if unsure.
2. **API: own thin Vulkan layer vs adopt `wgpu`.** Lean own-Vulkan (learning-first; `wgpu`
   caps the "genuinely good" ceiling — no bindless/RT/mesh shaders).
3. **Connection: out-of-process (any language) vs in-process (C# only).** Lean out-of-process;
   in-process C# is a free bonus debug path only if the renderer is C#.
4. **Mesh wire format.** Lean **base64 little-endian f32/u32 in the JSON envelope from v1**
   (you hit JSON bloat/precision loss at the *second* body), with flat host-computed normals
   and an explicit Vulkan Y-down / depth-[0,1] / Manifold-CCW winding contract in R0's DTO.

## Risks

Double learning curve if a new language is chosen; macOS fidelity ceiling (MoltenVK: no
mesh/geometry shaders, no exposed HW ray tracing); Android is several distinct walls;
tessellation-of-Solids and Android both require native Manifold on that RID; debug tooling
fractures (RenderDoc: Win+Android yes, macOS no). The determinism/replay + two-kernel +
wire-surface design *help*: they give a free record-and-replay test model, keep the renderer
honest (Document always wins), and make language free.

## Next steps (once the language fork is decided)

1. Founding ADR: **render host** (adopt ADR-0009 posture; if foreign-language, supersede
   `3DEngine.Core`).
2. ADR + TASK: **tessellation query** (R0) — the concrete first buildable step, engine-side.
3. Roadmap: open a **V2 rendering track** (R0…R7) alongside/instead of P8 (persistence).
