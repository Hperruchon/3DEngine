# CLAUDE.md

You work in a 3D Engine solution. This file gives your position and the rules that each session
must know. `docs/INDEX.md` gives the purpose of each file. `docs/CHARTER.md` gives the mission,
the objectives and the scope test.

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

Write for a reader who is not you. The owner reads the work three months later.

## Workflow

A session starts in this order:

1. Read this file.
2. Read the last entry in `docs/CURRENT-STATE.md`. It is the authority for "does X exist".
3. Take the first task in `docs/roadmap.md` order whose status is `Ready` and whose `depends-on`
   tasks are `Done`.
4. Read the ADRs in `governed-by` of that task. Do not read the other ADRs.
5. Read `docs/CHARTER.md` when you do not know if a request is in scope. It holds the scope test.

The charter says if you can act. This file says where. The relevant ADR says how. The ledger says
what exists. Find the minimum information.

A session ends only in these conditions:

- `docs/CURRENT-STATE.md` has a new entry. An entry is permanent. To correct one, add a new entry.
- The status in the task file agrees with the work.
- The build passes and the tests pass.
- You registered each new diagnostic code.
- You put each open question in `docs/register.md` with a date and a time limit.

A codebase review is due before the seventh milestone after the last review. The build fails when
it is late. `docs/templates.md`, section 7, gives the form. When evidence shows that an objective or
the roadmap needs a change, report it to the owner. Do not change an objective without the owner.

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
    Engine.Cli, Engine.Api.Http, 3DEngine, …
```

`Engine.*` is the authority for design truth: the commands, the queries and the events that
operate on the Document. `3DEngine.Core` is the render-side scene kernel. The two kernels do not
reference each other. A host that draws subscribes to `Engine.Core` events and projects them into
`3DEngine.Core` state (ADR-0009). `Engine.Tests/Governance/DependencyDirectionGateTests.cs` holds
each dependency rule.

The target topology is the hybrid topology of ADR-0019, which TASK-0038 builds. Today
`engine-api-http` is the only host with the HTTP and WebSocket surface, and the desktop host has no
path to the engine. Engine code knows nothing about HTTP and nothing about processes.

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

`Engine.Tests/Governance/DeterminismCallGateTests.cs` scans for the calls of rules 1, 3 and 4 and
for the runtime identifier of rule 5. Rules 2 and 6 need judgement.

## Diagnostic codes

Add each new `E-`, `W-` or `I-` code to `docs/diagnostics.md` in the same change. Do not remove a
code. Do not change the meaning of a code. Each code has a namespace.
`Engine.Tests/Diagnostics/DiagnosticsRegistryGateTests.cs` reads each `Engine.*` project.

## Stop and ask

Stop work and ask a question in these conditions:

- You must change the public shape of `Engine.Contracts/**`: a new required field, a new name, a
  removed field, a new meaning for a field, a new event kind. A change needs an ADR in the same
  commit.
- You must add a diagnostic code, but you cannot register it in `docs/diagnostics.md`.
- An ADR is not clear about replay determinism, event order or serial commit.
- The change crosses the authority boundary. An example is a client that reads internal state.
- A decision and the code disagree. Report both. The owner says which one is correct. Then the same
  task corrects the other.

A false statement about the repository is not a decision. Examples are a path that does not exist
and a count that is wrong. Correct it in the task that finds it, and say so in the ledger entry.

## Tests

While you work, run the tests for the file that you changed. Before each commit, run `dotnet test`
and the write-set check of `docs/templates.md`, section 2. Continuous integration on three
operating systems is the proof, and `.github/workflows/ci.yml` lists its jobs. Report a local
result as local. Do not report that work is complete before the gate reports green.

## Rules with no other home

- The quality limit for work by an agent is higher than for work by a person. If the owner cannot
  understand the work well enough to keep it, the work failed.
- Do not improve a thing that nobody asked you to improve. If you find a different problem, add a
  register entry.
- Report what you did not verify. Read a file before you cite it. A path, a line number and an
  identifier are things that a person can check.
- Do not do work outside the write set of the active task. `WriteSetGateTests` enforces it.
- Do not add an abstraction for a requirement that does not exist, if a later change can add it at
  a low cost. Add a property now if a later change must rewrite the log, rewrite each command
  payload, or migrate each saved document. Give the reason in an ADR.
- Do not add a command that changes a file outside `Engine.Core/Commands/` and
  `Engine.Core/Hosting/HandlerCatalog.cs`. `DispatchSurfaceGateTests` enforces it (ADR-0016).
- A rule in text only is a rule that an agent will break. Prefer a compile error, then a test, then
  a step in continuous integration, then text. `docs/templates.md`, section 6, gives the rule for
  rules.
