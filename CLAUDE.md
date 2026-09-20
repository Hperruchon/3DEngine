# CLAUDE.md

You work in a 3D Engine solution. The solution contains many projects.

## Language

Write all documentation in Simplified Technical English (ASD-STE100). Write session replies in the
same language. The rules are:

- Write short sentences. Use a maximum of 20 words for an instruction. Use a maximum of 25 words
  for a description.
- Give one instruction in one sentence.
- Use the active voice.
- Use the present tense if you can.
- Do not use contractions. Write the two words in full.
- Do not use idioms, metaphors or slang.
- Use the same word for the same thing each time.
- Use "must" for a requirement.
- Use a vertical list for a sequence of steps.
- Use a maximum of three words in a noun cluster.
- Define an abbreviation at its first use.

See `docs/working-agreement.md` for the behaviour rules.

## Navigation

1. `docs/CHARTER.md` — the purpose of this engine and its limits. Read this file first if you do
   not know if a request is in scope. It contains the agent scope test.
2. `docs/INDEX.md` — the repository map. It gives a path and a purpose for each area. Read this
   file to find where a thing is.
3. `docs/adr/README.md` — the index of architecture decisions. Find the one relevant Architecture
   Decision Record (ADR). Do not read all of them.
4. `docs/CURRENT-STATE.md` — what exists today. This file is the authority for the question
   "does X exist".
5. `tasks/` — units of work. Read only the active task. An active task has the status `Active` or
   `Ready`.
6. `docs/glossary.md` — the approved terms. Each term has one line and a pointer to its source.
7. `docs/conventions.md` — the rules for files, names and grammar.
8. `docs/templates.md` — the forms to fill in for a register entry, an ADR, a task and a commit.
9. `docs/diagnostics.md` — the register of diagnostic codes. Add to this file only.
10. `docs/register.md` — known problems, questions and temporary mechanisms. Each entry has a
    date and a time limit.
11. `docs/roadmap.md` — the phase plan. Read this file only if no task has the status `Ready`.

Find the minimum information. Do not read all ADRs. Do not read all tasks.

Use this read order when you scope work:

1. CHARTER tells you if you can act.
2. CLAUDE.md tells you where to work.
3. The relevant ADR tells you how to work.
4. CURRENT-STATE tells you what exists.

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

The solution has two kernels:

- `Engine.*` is the authority for design truth. It holds the commands, the queries and the events
  that operate on the Document.
- `3DEngine.Core` is the render-side scene kernel. Hosts that draw use it.

The two kernels do not reference each other. A host that draws connects them. The host subscribes
to `Engine.Core` events. Then the host projects the events into `3DEngine.Core` state. See
ADR-0009.

## Deployment topology

`engine-api-http` is the approved deployment. It runs in its own process. Its lifecycle is
independent of each client. User interfaces, agents and remote clients use its HTTP interface and
its WebSocket interface.

Embedded mode is a subset of this topology. In embedded mode the client hosts the engine in its
own process. Embedded mode permits exactly one client. It permits no observers. It keeps no state
between invocations. Use embedded mode for offline work by one person. `Engine.Cli` is the
approved embedded host.

Engine code knows nothing about HTTP and nothing about processes. See ADR-0011.

## Dependency rules

- `Engine.Contracts` has no project references.
- `Engine.Core` references only `Engine.Contracts`.
- `3DEngine.Core` has no project references. It is a peer kernel. See ADR-0009.
- An `Engine.*` project must not reference `3DEngine.Core`.
- `3DEngine.Core` must not reference an `Engine.*` project.
- A client references only `Engine.Core` and `Engine.Contracts`. Clients do not reference each
  other. A client that draws also references `3DEngine.Core`.
- A host that uses the native geometry backend also references `Engine.Geometry.Manifold`. This
  reference is permitted only at the composition root of the host. See ADR-0014 §4.
- `Engine.Tests` can reference each `Engine.*` project. A test project verifies authority. It is
  not a client. The client rules do not apply to it.

## Projects outside the engine spine

- `3DEngine/` — the Vulkan desktop host. It uses SDL3.
- `BlazorApp/` and `BlazorApp.Client/` — a placeholder web shell.
- `Vortice.Vulkan.Sample/` and `Vortice.Vulkan.SampleFramework/` — sample code.

Do not change these projects unless a task gives you that scope.

## Triad vocabulary

- **Command** — a command changes state. The engine records it in the log. The engine can replay
  it. It appears in the event stream.
- **Query** — a query reads state. The engine does not record it. The engine does not replay it.
  It does not appear in the event stream.
- **Event** — an event is an observation of a change. A command causes it. It appears only in the
  event stream.

Each change to persistent state must use a command. A query must not change state.

## Scope clamps

_No clamp is active._

The persistence clamp is removed. Objective 11 needs a document that a person saves and opens.
Persistence arrives with ADR-0015 and its task. Until then the register holds the item.

Do not add a capability from the non-goal list in `docs/CHARTER.md` unless an ADR and a task
introduce it.

## Determinism rules

A replay must give the same result on Windows, Linux and macOS, and on x64 and arm64. Anti-objective
16 in `docs/CHARTER.md` gives the property. These rules keep it.

The operations `+`, `-`, `*`, `/` and `sqrt` give one correct result on each platform. Code that uses
only those operations and comparisons is safe.

1. Do not call a transcendental function in code that writes to the log. This covers `Sin`, `Cos`,
   `Tan`, `Atan2`, `Asin`, `Acos`, `Exp`, `Log`, `Pow`, `Cbrt` and each `MathF` equivalent. The
   documentation for `Math.Pow` states that the result can differ between operating systems and
   architectures. `MathF` is worse, not better.
2. Do not store a rotation as an angle. A command records a matrix or a quaternion. If the interface
   uses degrees, the client computes the sine and the cosine, and the command records those numbers.
3. Do not call `Math.FusedMultiplyAdd`. A documented defect gives an incorrect result on hardware
   without FMA3 support.
4. Do not call `MultiplyAddEstimate`, `MinNative`, `MaxNative`, `ShuffleNative` or
   `ConvertToIntegerNative`. Their guarantee covers one process only.
5. Do not ship a 32-bit x86 runtime identifier. This is a condition for determinism, not a packaging
   preference.
6. Do not put a tessellation in the Document. A tessellation is presentation. See ADR-0007.

## Geometry kernel rules

`docs/CHARTER.md` gives the closed feature boundary and the four refused rungs.

7. Do not give a tolerance value to an entity. Use one global length tolerance and one chord
   tolerance.
8. Do not write a general surface intersector. A pair of surfaces with a closed formula computes
   exactly. Each other pair refuses and falls back to Manifold.
9. Do not put a result, a field dataset or the output of a solver in the log. The log records that a
   job produced an artifact. A replay rebuilds the reference, never the bytes.

## Diagnostic codes

You must add each new `E-`, `W-` or `I-` code to `docs/diagnostics.md` in the same change. There
is no exception. Codes are stable. Add codes only. Each code has a namespace.

## Stop and ask

Stop work and ask a question in these conditions:

- You must change the public shape of `Engine.Contracts/**`. Examples are a new required field, a
  new name, a removed field, a new meaning for a field, and a new event kind.
- You must add a diagnostic code, but you cannot add a register entry for it.
- An ADR is not clear about replay determinism, event order or serial commit.
- The change crosses the authority boundary. An example is a client that reads internal state.
- The documentation and the code disagree. Report both. Do not correct either one.

## Test discipline

- While you work, run only the tests for the file that you changed.
- Before you report that work is complete, give the work to the gate. Do not run the gate on your
  computer. The gate is the work of continuous integration (CI).
- Do not report that work is complete if only the narrow tests passed.

The gate runs these checks:

1. `dotnet build`
2. `dotnet test`
3. The dependency direction check
4. The diagnostic code register check
5. The replay determinism fixture

## Workflow

A session starts in this order:

1. Read CLAUDE.md.
2. Read the last entry in CURRENT-STATE.md.
3. Advance the next task that has the status `Ready`.

A session ends only in these conditions:

- CURRENT-STATE.md has a new entry, or you updated an entry.
- The status in the task file agrees with the work.
- The build passes and the tests pass.
- You registered each new diagnostic code.
- You put each open question in `docs/register.md` with a date and a time limit.

Examine the plan again in these conditions:

- Three milestones shipped after the last examination.
- A session read more than five files to find its position.
- An ADR and the code disagree.
- A person asks the same question two times.
- A boundary rule is under pressure.
- **Evidence shows that an objective or the roadmap needs a change. Report this to the owner. Do not
  change an objective without a decision from the owner.**

## Anti-patterns

Do not do these things:

- Do not put business logic in `BlazorApp/` or in `3DEngine/`.
- Do not bypass the `CommandBus` to change the Document.
- Do not treat a `3DEngine.Core` object as the authority for scene state.
- Do not add a diagnostic code without a register entry.
- Do not add an abstraction for a requirement that does not exist, **if a later change can add it at
  a low cost**. Add a property now if a later change must rewrite the log, rewrite each command
  payload, or migrate each saved document. Give the reason in an ADR. Examples are the unit on a
  number, the precision of a coordinate, and the version of the command semantics.
- Do not do work outside the scope of the active task.
- Do not put a host projection in the design boundary. A host may hold the projection from events to
  render state, because ADR-0009 requires it. A host may not decide what the design truth is.

### Inactive until milestone P0.3

- Do not add a command that changes a file outside `Engine.Core/Commands/` and the handler manifest.

  A person cannot obey this rule today. Two dispatch switch statements hold each command name, and
  milestone P0.3 removes them. Register entry R-0003 holds this item. The rule becomes active, and a
  test enforces it, when P0.3 lands.
