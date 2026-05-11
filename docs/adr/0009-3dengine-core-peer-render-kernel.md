# ADR 0009 — `3DEngine.Core` is a peer render kernel

## Status

Accepted — 2026-05-11

## Context

CLAUDE.md has flagged `3DEngine.Core` as "pre-architecture POCOs" and "outside the authority graph" since the engine spine was specified in TASK-0001. The decision on its fate has been listed in `docs/CURRENT-STATE.md` as the only pending architectural decision since v0.1, and in `docs/adr/README.md` as the only pending ADR.

`3DEngine.Core` today contains render-side scene types — `Scene`, `Entity`, `Camera`, `Light`, `Transform`, `MeshDefinition`, `MaterialDefinition`, `TextureDefinition` — plus lifecycle abstractions (`IThreeDEngine`, `ISceneLoader`, `IResourceCatalog`) and helpers (`ResourceCatalog`, `SampleSceneFactory`). It is referenced by `3DEngine/` (the Vulkan/SDL3 desktop host) and one file in `BlazorApp.Client/`. No `Engine.*` project references it.

Three options were considered (per `docs/roadmap.md`, P3):

(a) **Deprecate.** Delete the project. Cost: the Vulkan host and `BlazorApp.Client` lose the types they depend on; each either re-adds them locally (POCO duplication) or those features die. No current need motivates the deletion.

(b) **Fold the POCOs into `Document`** (i.e. into `Engine.Contracts`). Cost: large migration, and a direct violation of the V1 clamp "No concrete geometry backend" plus a reversal of ADR 0001 (geometry is abstracted by *capabilities*, not concrete POCOs). It also drags render-side state — textures, lights, camera pose — into the design-truth boundary, where it does not belong.

(c) **Keep `3DEngine.Core` and define its role as a peer render kernel.** Any host that draws can use it. `Engine.*` does not reference it; it does not reference `Engine.*`. The host owns the link: subscribe to `Engine.Core`'s event stream, project events into `3DEngine.Core` state, draw.

Option (c) is the cheapest, matches what the code already does, and crystallises a distinction the rest of the architecture already assumes: design state vs render state.

## Decision

`3DEngine.Core` is a **peer render kernel** to `Engine.Core`, not a client and not a pre-architecture artifact. Its scope is render-side scene representation. The boundary between it and `Engine.*` is one-way at the behavioural level (events flow out of `Engine.Core`, projected into `3DEngine.Core` state by host code) and zero at the reference level (the two kernels are mutually unreferenceable).

### 1. Authority diagram

```
Engine.Contracts             3DEngine.Core
(design truth)               (render-side scene kernel)
     │                              │
     ▼                              │
Engine.Core                         │
(CommandBus, Document,              │
 events, queries)                   │
     │                              │
     └──────────────┬───────────────┘
                    ▼
                 Clients
    Engine.Cli, Engine.Api.Http, 3DEngine, BlazorApp, …
```

Both kernels are independent of each other. Clients reference whichever they need.

### 2. Reference rules

- `3DEngine.Core` has zero project references. (Same rule as `Engine.Contracts`.)
- `Engine.*` projects (`Engine.Contracts`, `Engine.Core`, `Engine.Cli`, future `Engine.Api.Http`) MUST NOT reference `3DEngine.Core`.
- `3DEngine.Core` MUST NOT reference any `Engine.*` project.
- A client that renders (today: `3DEngine`, `BlazorApp.Client`) MAY reference `3DEngine.Core`.
- A client that does not render (today: `Engine.Cli`) MUST NOT reference `3DEngine.Core` — there is no reason for it to.
- `Engine.Tests` may reference `3DEngine.Core` only if a future test verifies kernel-level behaviour in it; ordinarily it does not.

### 3. Design truth vs render state

|  | Design truth (`Engine.*`) | Render state (`3DEngine.Core`) |
|---|---|---|
| What it models | Intent, history, authoritative state | What is drawn on screen |
| Mutated by | Commands via `CommandBus` | Host code, in response to engine events |
| Source of truth? | Yes — replayable, persistable | No — derivable from the event stream |
| Crosses processes? | Yes (HTTP transport in V1.x) | No — render state lives in the host |
| Examples | `Document.Version`, `BodyHandle`, feature ids, command/event log | `Camera` pose, `Light` rig, active material, current `Scene` graph |

"Design truth" and "render state" become the canonical vocabulary. Where existing docs (CLAUDE.md, earlier ADRs) say only "truth," the distinction is additive and the existing text still applies.

### 4. How the two kernels connect

The two kernels never share a type. The link is *behavioural*, owned by each rendering host:

1. Host subscribes to `Engine.Core`'s event stream (ADR 0005).
2. For each event kind it cares about, the host projects the event into a mutation on its `3DEngine.Core` state. Example: `command.applied` with a new `BodyHandle` in `Outputs` → host adds an `Entity` to its `Scene`.
3. Host draws from `3DEngine.Core`.

This is the projection pattern of ADR 0005 (events are the public surface; clients project them). The projection lives in the host — in `3DEngine/`, `BlazorApp.Client/`, or, when a second host arrives, in a shared host-side library. It does NOT live in either kernel. Neither kernel knows about the other.

### 5. Migration

None. No code moves. The decision is documentation:

- CLAUDE.md drops the "pre-architecture" and "Do not touch" framing of `3DEngine.Core`, replacing them with the peer-kernel description and the reference rules of §2.
- `docs/CURRENT-STATE.md` removes ADR-0009 from "Pending decisions."
- `docs/adr/README.md` indexes this ADR and clears the "Pending" entry for ADR-0009.

The existing references (Vulkan host, Blazor client) continue to work unchanged. No `Engine.*` project gains a reference. No new diagnostic codes.

## Consequences

- **Two kernels exist.** Future strategic decisions about rendering, asset pipeline, or scene file format live in `3DEngine.Core`'s own ADRs — not in the `Engine.*` ADR stream.
- **CLAUDE.md scope shrinks.** The "Do not touch" list loses `3DEngine.Core`. Ordinary additions to it (new render-side types, a real `ISceneLoader`, etc.) are governed by engineering judgement; they do not need an ADR unless they cross a boundary.
- **Host-side projection is real work, deferred.** The host code that subscribes to `Engine.Core` events and updates `3DEngine.Core` state does not exist today. It is in scope only when a host needs to *see* what the engine has done — no earlier than P7 (Manifold backend + first geometry command), when there is something for the renderer to draw.
- **`Engine.Cli` stays render-free.** This is mechanical: `Engine.Cli` does not reference `3DEngine.Core`, and the rules in §2 forbid it from gaining that reference.

## Non-goals

- Folding `3DEngine.Core` types into `Engine.Contracts`. Explicitly rejected (option b).
- Removing `3DEngine.Core`. Explicitly rejected (option a).
- Specifying the projection mechanism in detail. The mechanism is host code; the only contract is the event stream (ADR 0005). No new contract is created here.
- A typed adapter library between the kernels. Premature; it would couple them.
- Renaming `3DEngine.Core`. Out of scope.
- Migrating any code today. None moves.

## Validation rules

1. `Engine.Contracts.csproj`, `Engine.Core.csproj`, `Engine.Cli.csproj`, and any future `Engine.Api.Http.csproj` MUST NOT contain a `<ProjectReference>` to `3DEngine.Core.csproj`.
2. `3DEngine.Core.csproj` MUST NOT contain a `<ProjectReference>` to any `Engine.*` project.
3. The above rules apply transitively. If any `Engine.*` project ever pulls in `3DEngine.Core` via NuGet or another path, the dependency-direction gate (CLAUDE.md gates §3) fails.

Mechanical enforcement of these rules is out of phase scope — the dependency-direction gate already lives in CLAUDE.md as a CI gate slot; wiring it to scan project references is its own task.

## Open challenges

- **Shared projection library.** When a second rendering host actually arrives, the projection logic likely wants to be shared between `3DEngine` and `BlazorApp.Client`. That suggests a host-side shared library; size it then.
- **Selection / hover state.** Per ADR 0007, UI ephemeral state stays in the client. But "selection in 3D space" feels like it could live in `3DEngine.Core` (so multiple host UIs reuse the type). Today, each host owns its selection. Decide later.
- **Asset pipeline.** `MeshDefinition`, `MaterialDefinition`, `TextureDefinition` are skeletal. A real asset pipeline (streaming, hashing, deduplication, references) is its own design effort, separate from this ADR.
