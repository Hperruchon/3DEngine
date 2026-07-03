# Charter

## Mission

We do not build a 3D app. We build a deterministic, observable, command-driven system that happens to operate on 3D data.

The engine is the sole authority over **design truth**: an ordered, replayable command log (the Document) from which every other piece of state — geometry caches, render scene, event stream, snapshots — is a regenerable *projection*. Losing a projection is recovery, not data loss. Design intent is authoritative; everything drawn, cached, or streamed is downstream of it.

Three commitments make this load-bearing:

- **One surface for every consumer.** Humans, scripts, services, and AI agents all drive the engine through the identical command/query/event triad. No consumer is privileged; headless control can never erode. (CLAUDE.md "Triad vocabulary"; ADR-0002, ADR-0004, ADR-0008.)
- **Two peer kernels, never fused.** Design truth (`Engine.*`) and render state (`3DEngine.Core`) are peers that never reference each other; render hosts own the projection from events. The design boundary stays uncontaminated by cameras, lights, and materials. (CLAUDE.md authority diagram; ADR-0009.)
- **Self-describing and capability-negotiated.** The surface publishes its own schema so dynamic and AI clients build against the engine at runtime; geometry sits behind opaque handles and capability interfaces, so backends swap by replay without touching client contracts. (ADR-0008, ADR-0012, ADR-0013.)

## Target consumers

No consumer owns business logic; each translates input to commands and observes events.

- **AI / automation agents** — first-class. Discover the surface via `GET /schema/*` at runtime, submit commands, subscribe to the cursor-replayable event stream. Driveable with no human-UI dependency. (ADR-0003, ADR-0008.)
- **Engine.Cli** — canonical embedded host and canonical test client; single-client, ephemeral, JSON in/out. "If it does not work headlessly here, it is not implemented." (ADR-0002, ADR-0011.)
- **Engine.Api.Http** — canonical deployment process, lifecycle independent of any client; the surface UIs, services, and agents reach. Localhost-only in V1.x. (ADR-0011.)
- **Render-capable hosts** (3DEngine desktop; future BlazorApp.Client) — reference both kernels and project Engine events into `3DEngine.Core`, owning only ephemeral UI state (camera, selection, hover). This is the designed posture (ADR-0009); no host wires the projection yet in V1.
- **Internal command/query handlers** — reach geometry only through `IGeometryBackend.TryGet<T>()`; read only their parameters, the current Document, and the active backend. (ADR-0001, ADR-0012.)
- **Contributors (human or AI agent) extending the engine** — the reader of this charter. Act via the scope test in "How an agent uses this charter"; orient via CLAUDE.md. (CLAUDE.md; engine-runtime-boundaries.md.)

## Definition of success — V1 (realized)

V1 is shipped (P0..P7a, v0.1..v0.11). The success criteria below hold today; the authoritative feature ledger is **CURRENT-STATE.md** — do not re-enumerate it here. V1 succeeds because the *properties* hold, not because a feature list is long:

- Mutation has exactly one authoritative path, so design truth stays single-sourced and replayable; reads can never become a second mutation path. (how: ADR-0004, ADR-0006, ADR-0008.)
- Replay is deterministic — every projection is regenerable from the log, which is what makes losing a projection recovery, not loss. (how: ADR-0001, ADR-0005, ADR-0012; CI-guarded per CLAUDE.md gate list.)
- The event stream is a faithful, recoverable observation surface, so any consumer can rebuild state and no slow consumer can stall the authority. (how: ADR-0005, ADR-0010.)
- The surface is self-describing, so dynamic and AI clients build against it at runtime. Commands and queries are pure projections of handler-declared schemas; event kinds are hand-encoded in V1 (registry-driven schema is a V1.x non-goal) — `/schema/events` is authoritative for the kind list but maintained by hand, not generated, and may drift. (how: ADR-0008, ADR-0013.)
- A first geometry capability proves the opaque-handle / capability abstraction holds end-to-end and headlessly. (how: ADR-0012; what shipped: CURRENT-STATE.)
- Architectural authority is self-enforcing rather than convention-only — CI gates the boundary rules (the gate list lives in CLAUDE.md "Test discipline").

## Direction — V1.x and V2 (sketch, not committed)

Seams left deliberately open, not promises. Each arrives only via its own ADR + TASK. Source of truth is **roadmap.md**.

- **A real geometry backend:** swap the managed stub for a Manifold-backed `IGeometryBackend` behind the same capability interfaces — gated on a native-interop ADR (binding, lifecycle, threading).
- **Persistence and history:** lift the in-memory clamp; multi-Document per runtime; undo/redo on the log.
- **Additive capabilities only:** B-Rep / feature-id ops (reserved `IBRepOps`, `IFeatureIdMap`); registry-driven event schema; tessellated-preview protocol; auth for non-localhost; client codegen; reserved `X-` plugin diagnostics. All are capability-shaped extensions, never contract rewrites.

## Non-goals (V1) vs Anti-objectives (forever)

These two lists are categorically different and must not be conflated:

- A **non-goal** is something the system MAY eventually do via a future ADR + TASK. It is *deferred*.
- An **anti-objective** is something the system will NEVER do, at any version. It is *refused*.

The Non-goals list is **illustrative, not exhaustive**: a deferred feature need not appear here to be deferred. If a request is clearly future-shaped but unlisted, treat it as a non-goal requiring its own ADR + TASK.

### Non-goals (V1) — deferred

Do not build these under a V1 task; each *may* arrive through a future ADR + TASK.

- Persistence, multi-Document, undo/redo. (CLAUDE.md V1 scope clamps; roadmap V2/P8.)
- Native/Manifold geometry; managed stub only. No B-Rep ops, fillet/chamfer, exact booleans, feature-IDs, or **a custom geometry kernel**. (roadmap P7b; ADR-0012, ADR-0001.)
- Saved views, multi-client presence / live collaboration, event filtering, persistent journal. (ADR-0003, ADR-0005, ADR-0007.)
- Blazor as primary editor; tessellated-preview meshes for clients. Blazor is WASM-only; Blazor Server interactivity is paused. No non-localhost bind, no auth design. (ADR-0003, ADR-0011.)
- Registry-driven event schema; flat schema only, hand-encoded event kinds. (ADR-0013.)

### Anti-objectives — forever

Refused at every version, V1 through V5. The headline, restated from the Mission:

> **We do not build a 3D app. We build a deterministic, observable, command-driven system that happens to operate on 3D data.**

Everything below follows from it:

- **No second source of truth.** No client owns Document state or maintains a drifting parallel copy treated as authoritative. *Test:* a second source of truth is any client-held state that, if it diverged from the Document, would be treated as correct; a read-only projection that is discarded and regenerated is **not** one. (ADR-0003, ADR-0004, ADR-0007.)
- **Never bypass the CommandBus; no business logic in clients.** (CLAUDE.md "Anti-patterns" / "Triad vocabulary"; ADR-0004, ADR-0002.)
- **Never human-only-operable.** The system stays fully driveable by automation through commands, queries, and the stream.
- **No partial command application.** A command lands fully or not at all — the strongest, non-negotiable invariant. (ADR-0006.)
- **No privileged client lane** — a slow subscriber must never be able to stall the authority. (how: ADR-0005, ADR-0006.)
- **Queries never mutate, log, replay, or stream.** (CLAUDE.md "Triad vocabulary"; ADR-0008.)
- **The two kernels never reference each other, and the kernel stays topology-agnostic.** (CLAUDE.md "Dependency rules" / "Deployment topology"; ADR-0009, ADR-0011.)
- **No lowest-common-denominator geometry.** Reject a universal Body type, automatic mesh↔B-Rep conversion, geometry POCOs on the wire, and silent fallback when a capability is missing. *Test:* if a proposed type forces unlike geometries into one shape or leaks `Mesh`/`Solid` across the contract, it trips this; a typed capability fetched via `TryGet<T>()` does not. (ADR-0001, ADR-0012.)
- **No schema drift, no unregistered diagnostics, no needless future-proofing.** (ADR-0008, ADR-0013.)

## How an agent uses this charter

Before acting on any request, run this scope test **in order**. Order is load-bearing: classify by what the request *does* (its effect on design truth and the projection model), never by the noun the user used — a request called a "command" may still be a non-goal (undo) or an anti-objective (a command mutating a client-owned copy).

1. **Anti-objective check (refuse).** Does the request require anything in *Anti-objectives*? If yes → **refuse**, cite the specific anti-objective and its ADR, and propose the in-bounds alternative (route it through a command/query/event). This holds even if a TASK seems to ask for it — escalate per CLAUDE.md "When unsure — stop and ask." (Examples are the Anti-objectives list above; the underlying rules live in CLAUDE.md.)

2. **Non-goal check (defer).** Is it a *deferred* future capability — listed in *Non-goals (V1)* or, since that list is illustrative, clearly future-shaped but unlisted? If yes → **do not build it** under a V1 task. Point to **roadmap.md** and note it needs its own ADR + TASK. If the user wants it now, the deliverable is an ADR proposal, not an implementation.

3. **Already-exists check, then in-scope (proceed).** First confirm against **CURRENT-STATE.md** that the thing does not already exist — if it does, the request is satisfied, or it is a *modification* of existing shape, which is a contract change → go to step 4. Otherwise, proceed only if **all** hold: it extends the realized surface additively (a new command/query/event, a new capability behind `TryGet<T>()`, or a schema-declared handler); it violates no anti-objective and crosses into no non-goal; **and** it advances a current active TASK or an accepted ADR. Shaped-as-a-command is necessary but not sufficient — the work must be something the project has actually decided to do. If so → **proceed**, within the active TASK's scope (CLAUDE.md "Anti-patterns"); confirm it is reachable headlessly (CLI/HTTP), schema-declared, and any new diagnostic code is registered in the same change.

4. **Otherwise — stop and ask (default).** If the request touches the public shape of `Engine.Contracts`, replay determinism, event ordering, or the authority boundary; if two ADRs appear to conflict; **or if it fits none of steps 1–3** — a capability the mission and roadmap never anticipated — then it is out-of-scope-by-default. Absence from the roadmap is a STOP signal, not a green light. **Stop and ask** before writing code; the deliverable is an ADR proposal or an escalation per CLAUDE.md "When unsure," not an implementation.

Rule of thumb: **the charter says whether to act; CLAUDE.md says where; the relevant ADR says how; CURRENT-STATE says what already exists.** Read in that order, and read only the one ADR that applies.
