# CLAUDE.md

You are working in a multi-project 3D Engine solution.

## Navigation

1. `docs/adr/README.md` — index of architectural decisions. Find the one relevant ADR; do not read all.
2. `docs/CURRENT-STATE.md` — what is built today. Authoritative for "does X exist yet."
3. `tasks/` — sized work units. Read only the active task (Status: Active or Ready).
4. `docs/diagnostics.md` — diagnostic code registry. Append-only.
5. `docs/open-questions.md` — deferred decisions. Read only if one blocks your task.
6. `docs/roadmap.md` — strategic phase plan. Read only when no Ready TASK exists in `tasks/`.

Find the minimum information. Do not read all ADRs or all tasks.

## Authority diagram

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

Two kernels. `Engine.*` is the authority for design truth (commands/queries/events on the Document). `3DEngine.Core` is the render-side scene kernel for hosts that draw. They do not reference each other. Hosts that render wire them: subscribe to `Engine.Core` events and project them into `3DEngine.Core` state. See ADR-0009.

## Deployment topology

`engine-api-http` is the canonical deployment — its own process, lifecycle independent of any client. UIs, AI agents, and remote clients reach it through the HTTP/WebSocket surface. Embedded mode (engine hosted in-process by the client) is a strict subset for solo-offline use: exactly one client, no observers, no persistence between invocations. `Engine.Cli` is the canonical embedded host. Engine code is topology-agnostic; the kernel knows nothing about HTTP or processes. See ADR-0011.

## Dependency rules

- `Engine.Contracts` has zero project references.
- `Engine.Core` references only `Engine.Contracts`.
- `3DEngine.Core` has zero project references. (Peer kernel; see ADR-0009.)
- `Engine.*` projects (`Engine.Contracts`, `Engine.Core`, `Engine.Cli`, future `Engine.Api.Http`) MUST NOT reference `3DEngine.Core`. `3DEngine.Core` MUST NOT reference any `Engine.*` project.
- Clients (`Engine.Cli`, `Engine.Api.Http`, `BlazorApp`, etc.) reference only `Engine.Core` and `Engine.Contracts`. They do not reference each other. Render-capable clients (`3DEngine`, `BlazorApp.Client`) additionally reference `3DEngine.Core`.
- `Engine.Tests` may reference any `Engine.*` project. Test projects are *verifiers of authority*, not clients — client rules do not apply.

## Do not touch

- `3DEngine/` — Vulkan/SDL3 desktop host
- `BlazorApp/`, `BlazorApp.Client/` — placeholder shell
- `Vortice.Vulkan.Sample/`, `Vortice.Vulkan.SampleFramework/` — sample code

These projects are unrelated to the engine spine. Do not modify them.

## Triad vocabulary

- **Command** — mutates state, is logged, replayable, surfaces in the event stream.
- **Query** — reads state. Not logged. Not replayed. Never appears in the event stream.
- **Event** — observation of what happened. Derived from commands. Surfaces only via the event stream.

All persistent state changes go through commands. Queries must not mutate.

## V1 scope clamps (until-when)

- **No persistence** — in-memory only until persistence ADR + TASK.

Do not add any of these unless an ADR + TASK explicitly introduces them.

## Diagnostic codes

Any `E-`/`W-`/`I-` code added to code must land in `docs/diagnostics.md` in the same change. No exceptions. Codes are stable, append-only, and namespaced.

## When unsure — stop and ask

- Before changing public shape of `Engine.Contracts/**` (adding required fields, renaming, removing, changing semantic meaning, adding event kinds).
- Before adding a new diagnostic code without registry entry.
- When ADR guidance is ambiguous on replay determinism, event ordering, or serial commit semantics.
- When the change crosses the authority boundary (client wants to read internal state, etc.).

## Test discipline

- **While iterating:** run only tests for the file/feature you touched.
- **Before declaring done:** hand off to the gate. Do not run the gate locally — that is CI's job.
- Never claim done because narrow tests passed.

The gate runs: `dotnet build`, `dotnet test`, dependency direction check, diagnostic codes registered, replay determinism fixture.

## Workflow

A session opens by reading CLAUDE.md and the last CURRENT-STATE entry, then advances the next Ready TASK.

A session closes only when:

- CURRENT-STATE.md has a new entry (or an existing entry updated)
- The TASK file's Status reflects reality
- Build + test green
- Any new diagnostic codes registered
- Open questions captured in a file, not chat history

Reevaluate the plan when:

- 3 milestones have shipped since the last reevaluation
- A session reads >5 files to orient itself
- An ADR and the code disagree
- Same question is asked twice
- A boundary rule is being pressed

## Anti-patterns

- Putting business logic in `BlazorApp/` or `3DEngine/`.
- Bypassing the `CommandBus` to mutate `Document` directly.
- Treating `3DEngine.Core` POCOs as canonical scene state.
- Adding diagnostic codes inline without registry update.
- "Future-proof" abstractions without need.
- Feature work outside the active TASK's scope.
