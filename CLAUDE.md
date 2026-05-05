# CLAUDE.md

You are working in a multi-project 3D Engine solution.

## Navigation

1. `docs/adr/README.md` — index of architectural decisions. Find the one relevant ADR; do not read all.
2. `docs/CURRENT-STATE.md` — what is built today. Authoritative for "does X exist yet."
3. `tasks/` — sized work units. Read only the active task.
4. `docs/diagnostics.md` — diagnostic code registry. Append-only.

Find the minimum information. Do not read all ADRs or all tasks.

## Authority diagram

```
Engine.Contracts → defines truth
Engine.Core      → enforces truth
Clients          → consume truth (CLI, UI, HTTP, agents)
```

Direction is one-way. No client may be a dependency.

## Dependency rules

- `Engine.Contracts` has zero project references.
- `Engine.Core` references only `Engine.Contracts`.
- Clients (`Engine.Cli`, `Engine.Api.Http`, `BlazorApp`, etc.) reference only `Engine.Core` and `Engine.Contracts`. They do not reference each other.
- `Engine.Tests` may reference any `Engine.*` project. Test projects are *verifiers of authority*, not clients — client rules do not apply.
- `3DEngine.Core` is **pre-architecture** and outside the authority graph. Do not reference it from any `Engine.*` project. Do not migrate code into it. Its fate is decided by ADR-0009 (pending).

## Do not touch

- `3DEngine/` — Vulkan/SDL3 desktop host
- `3DEngine.Core/` — pre-architecture POCOs
- `BlazorApp/`, `BlazorApp.Client/` — placeholder shell
- `Vortice.Vulkan.Sample/`, `Vortice.Vulkan.SampleFramework/` — sample code

These projects are unrelated to the engine spine. Do not modify them.

## Triad vocabulary

- **Command** — mutates state, is logged, replayable, surfaces in the event stream.
- **Query** — reads state. Not logged. Not replayed. Never appears in the event stream.
- **Event** — observation of what happened. Derived from commands. Surfaces only via the event stream.

All persistent state changes go through commands. Queries must not mutate.

## V1 scope clamps (until-when)

- **No HTTP/WS transport** — until ADR + sized TASK introduce it.
- **No CLI** — until P1 sized TASK introduces `Engine.Cli`.
- **No concrete geometry backend** — until ADR-0009 + sized TASK.
- **No persistence** — in-memory only until persistence ADR + TASK.
- **No idempotency cache, no schema endpoints** — deferred to transport task.

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

## Anti-patterns

- Putting business logic in `BlazorApp/` or `3DEngine/`.
- Bypassing the `CommandBus` to mutate `Document` directly.
- Treating `3DEngine.Core` POCOs as canonical scene state.
- Adding diagnostic codes inline without registry update.
- "Future-proof" abstractions without need.
- Feature work outside the active TASK's scope.
