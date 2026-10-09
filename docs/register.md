# Deferred register

**Status: Accepted** — 2026-09-20, TASK-0015. It replaces `open-questions.md`.

This document holds each known problem, each open question, each accepted compromise and each
temporary mechanism. Each entry has a date of entry and a date of expiry. No entry can stay here
without a limit.

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section
"Language".

## The reason for this document

The Blender design document of the year 2000 listed a broken event system, a user interface that
nobody could configure, and no undo function. The document planned a release 2.5 to correct these
problems. Release 2.5 became stable in April 2011. Eleven years passed between the record of the
problem and the repair.

A record of a problem is not progress. A list with no expiry date becomes longer.

This repository has the same condition today. `nuget.config` calls its folder feed an interim
bootstrap. `ArgParser.cs` says that a later task replaces it. The native build workflow says
"DRAFT — not yet executed", but it ran successfully months before. Two open questions from
2026-05-06 have no change.

## The rules

1. Each entry must have an `opened` date and a `due` date. The gate fails if a date is absent.
2. If the `due` date passes, the build fails. This result is the meaning of a limit. It is not a
   reminder. It is a stop.
3. There are four exits. You must use one exit to make the gate pass again:
   - **Resolve** — correct the problem. Record the commit, the task or the ADR.
   - **Decide** — the entry was a decision, not a question. Write an ADR. Close the entry and
     point to the ADR.
   - **Accept** — keep the problem permanently. Give a reason in writing. Move the entry to the
     section "Accepted compromises". The entry stops to age. This exit is correct behaviour. It is
     not a failure.
   - **Extend** — give a new date, one time only. Give the reason. The gate refuses a second
     extension. Then you must use one of the three exits above.
4. A new entry has a cost. The maximum quantity of open entries is **15**. You cannot add entry 16
   until you close an entry. Growth is the failure condition. This limit controls growth. The limit
   was 20 until 2026-09-21. See the closed entry R-0016.
5. A temporary thing must have an entry. These words in tracked source or documentation must have a
   register identifier, for example `R-0007`: `TODO`, `FIXME`, `HACK`, `XXX`, `DRAFT`, `interim`,
   `temporary`, `for now`, `bootstrap`. The gate fails if an identifier is absent. This rule finds
   the compromise that becomes permanent.
6. Do not close an entry to make the gate pass. Use one of the four exits above. Deletion of an
   entry destroys the register.

## Default lifetimes

Select the class. The class gives the date. Use a different date only with a reason.

| Class | Lifetime | Use for |
|---|---|---|
| `risk` | 30 days | A problem that can cause damage soon, or that costs time now |
| `question` | 90 days | A decision that you do not need now, but that you will need |
| `debt` | 180 days | A known compromise in code that shipped |
| `interim` | 365 days | A mechanism that is temporary by decision |

## Entry format

```
### R-0000 · One line. State the problem. Do not state the repair.
- class: risk | question | debt | interim
- opened: YYYY-MM-DD
- due: YYYY-MM-DD
- extended: no
- refs: path:line, TASK-nnnn, ADR-nnnn
Give the problem and its cost in two or three sentences.
Exit: state the condition that closes this entry.
```

---

## Open

### R-0007 · Native packages come from a local folder feed
- class: interim
- opened: 2026-07-04
- due: 2027-07-04
- extended: no
- refs: `nuget.config:9-11`, `nuget/.gitignore:1-4`, `TASK-0012 §6`
The configuration calls this feed an interim bootstrap. The publish step in the native workflow is
inactive. A person commits each package by hand.
Exit: a real feed exists. Or: accept this mechanism permanently and give the reason.

### R-0020 · The platform list needs a study before objective 8 changes
- class: question
- opened: 2026-09-25
- due: 2026-12-24
- extended: no
- refs: `docs/CHARTER.md` objective 8 and anti-objective 16, `docs/reviews/2026-09-23-architecture-challenge.md` concern 4, R-0007
Objective 8 names Windows, Linux and macOS. Anti-objective 16 names x64 and arm64 for each. The
Manifold package covers `win-x64`, `linux-x64` and `osx-arm64` only. On 2026-09-25 the owner asked
for each platform, Android included, and added: "maybe we should check how to make it as simple as
possible". Android is not in the twenty objectives. A study must give the cost of each platform: the
native build, the runner, the Vulkan path, and the test from one computer.
Exit: the owner decides the platform list from the study, and objective 8 and anti-objective 16 agree
with it.

### R-0021 · The project has no decision on how to show the application
- class: question
- opened: 2026-09-25
- due: 2026-12-24
- extended: no
- refs: ADR-0003, ADR-0019, TASK-0033
On 2026-09-25 the owner said: "a browser client is not the main idea, we may need to rethink how to
show the app". TASK-0033 removes the Blazor pair, and the desktop window is then the only client that
draws. The means to show the application to a person is open: the desktop window only, a user
interface toolkit in the window, a remote view, or a recorded demonstration.
Exit: the owner decides, and a task or an ADR records the decision.

### R-0022 · Three Vulkan defects block the first render on Linux and on macOS
- class: debt
- opened: 2026-09-25
- due: 2027-03-24
- extended: no
- refs: `3DEngine.Vulkan/GraphicsDevice.cs:377-379`, `3DEngine.Vulkan/Swapchain.cs:174`, `docs/reviews/2026-09-23-codebase-review.md` findings V1, V2 and V3
V1: the host treats `VK_SUBOPTIMAL_KHR` as a failure and gives a semaphore back to the pool while its
signal is pending. V2: the special extent value `0xFFFFFFFF` passes the size check, and the fallback
reads the window size in logical units. V3: the instance and the device set no portability flag, so
a current loader on macOS does not list MoltenVK. The host only clears the screen today, therefore no
defect shows now.
Exit: each defect is corrected, with the check that the review gives for it, before the first render
phase that runs on Linux or on macOS.

### R-0023 · No gate compares the native output of one platform with another
- class: debt
- opened: 2026-09-25
- due: 2027-03-24
- extended: no
- refs: `Engine.Tests/ReplayDeterminism/ReplayDeterminismGateTests.cs`, `docs/reviews/2026-09-23-codebase-review.md` finding T1, anti-objective 16
The replay gate runs on the managed backend, and it compares no geometry value. Each runner compares
only with its own constants. A difference in the Manifold output between Windows, Linux and macOS
therefore passes. Anti-objective 16 requires the same result on each platform. The cost becomes real
when a saved document moves between platforms.
Exit: the repository holds a baseline of exact native outputs, such as the bits of a bounding box and
a hash of a mesh, and each runner compares its output with that baseline.

### R-0024 · The render projection may need to read `Engine.Contracts`
- class: question
- opened: 2026-09-25
- due: 2026-12-24
- extended: no
- refs: ADR-0009 §2 and §4, ADR-0021, `docs/roadmap.md` phase R5, `docs/reviews/2026-09-23-architecture-challenge.md` concern 1
ADR-0009 §2 forbids a reference in each direction between the two kernels, and ADR-0009 §4 puts the
projection from events to render state in the host. The desktop host needs a graphics processor, so
a runner with none cannot test a projection that lives there. The architecture challenge recommends
this: a projection with no graphics code may read `Engine.Contracts`, and design truth never reads
render state. The owner did not decide it.
Exit: the owner decides before phase R5 starts, and ADR-0009 is amended or kept.

### R-0025 · ADR-0015 describes the durability of a server and not a document file
- class: question
- opened: 2026-09-25
- due: 2026-12-24
- extended: no
- refs: ADR-0015 §5, ADR-0019, `CLAUDE.md` section "Scope clamps", objective 11, `docs/reviews/2026-09-23-architecture-challenge.md` concern 7
`CLAUDE.md` connects objective 11 to "a document that a person saves and opens". ADR-0015 has the
status `Proposed`, and its §5 limits persistence to one log file for each `engine-api-http` process,
with no open and no save by a person. Under ADR-0019 the desktop owns the session, therefore that
scope misses the product. The architecture challenge recommends a rewrite around a document file. The
owner did not decide it.
Exit: the owner accepts a rewritten ADR-0015, or refuses the rewrite and gives the reason.

### R-0027 · Six findings of the first codebase review have no task
- class: debt
- opened: 2026-09-30
- due: 2027-03-29
- extended: no
- refs: `docs/reviews/2026-09-23-codebase-review.md` findings E5, E6, V4, T2, C1 and P1, `docs/reviews/2026-09-30-codebase-review.md` finding E12
No task and no register entry named these findings until the second review. E5 is one line, and the
new finding E12 must be corrected with it: the binder refuses a whole number for a number field, so
the correction of E5 alone makes each HTTP request with a whole value fail. The open part of T2 is the
absent test for a backend that throws through the command bus.
Exit: each finding has a task, or it is corrected, or the owner accepts it and gives the reason.
Progress 2026-10-04: the third codebase review finds one part of C1 corrected: `Engine.Api.Http/Program.cs`
no longer cites the removed clamp. E5, E6, E12, V4, the open part of T2 and the other parts of C1
stay. P1 costs more, because the snapshot now also holds the session.
Progress 2026-10-06: TASK-0036 corrected the comment of `Engine.Api.Http/EngineHost.cs` that cited the
removed clamp, which is the last comment part of C1 (also listed in E24 of R-0033). The empty markers
and the flags that nothing reads stay.

### R-0028 · Nine new findings in the engine and the hosts have no task
- class: debt
- opened: 2026-09-30
- due: 2027-03-29
- extended: no
- refs: `docs/reviews/2026-09-30-codebase-review.md` findings E7, E9, E10, E11, E13, E14, E15, E16 and E17, TASK-0034, TASK-0038
A replay discards each result (E7). A subscriber that connects during a commit can lose an event or
receive it two times (E9). A second bus starts the sequence again (E10). A value that is not finite
enters the log (E11). A failed query returns a box of zeros (E13). The Manifold wrapper frees a caller
buffer with the C++ `delete` (E14). The host has no bind rule, no Origin check and no Host check
(E15). The request envelope ignores an unknown member (E16). TASK-0034 closes E9 and E10 when it adds
the tests that the review gives. E15 must be corrected before TASK-0038 ships, because the desktop
host then serves the surface while a person has a document open.
Exit: each finding has a task, or it is corrected, or the owner accepts it and gives the reason.
Progress 2026-09-30: TASK-0044 corrected E15. The host refuses an address that is not a loopback
address, a foreign Origin gets 403, and a foreign Host gets 400. The other findings stay.
Progress 2026-10-01: TASK-0034 corrected E9 and E10, and their tests pass. The first live `seq` after
a reset equals the snapshot version plus one, a second bus on one Document is refused, and the bus
refuses to move the version back. `Document.AdvanceVersion` still accepts a lower value, because
`Engine.Contracts` was outside that task; TASK-0035 modifies the file. E7, E11, E13, E14, E16 and
E17 stay.
Progress 2026-10-04: the third codebase review confirms E9 fixed and E10 fixed in part. Two parts
of E17 are corrected: the close status, which `docs/diagnostics.md` now gives as 1007, and the
subscriber that the handshake leaves, which a `finally` now disposes. E7, E11, E13, E14, E16 and
eleven parts of E17 stay.
Progress 2026-10-05: TASK-0035 corrected E7 (a replay stops at a divergence and names it), closed E10
(the version is the count of the log, and no code can set it), and corrected the replay part of E17 (a
replay applies each entry with no cache). E11, E13, E14, E16 and ten parts of E17 stay.

### R-0030 · The Vulkan layer continues with a null device and gives unpinned memory to Vulkan
- class: debt
- opened: 2026-09-30
- due: 2027-03-29
- extended: no
- refs: `3DEngine.Vulkan/GraphicsDevice.cs:149-170`, `3DEngine.Vulkan/GraphicsDevice.cs:636-639`, `docs/reviews/2026-09-30-codebase-review.md` findings V5, V6 and V7, R-0022
When no device is suitable, the host calls Vulkan with a null handle, and the process stops in native
code with no message (V5). Byte arrays and pointers from a `fixed` block go to Vulkan with no pin
(V6). Six smaller findings are in V7. The host only clears the screen on one computer today, therefore
no run shows a failure.
Exit: each finding is corrected before the first render phase that runs on a second computer. This is
the condition of R-0022 too.

### R-0031 · A gate can pass while its rule is broken
- class: debt
- opened: 2026-09-30
- due: 2026-12-24
- extended: no
- refs: `docs/reviews/2026-09-30-codebase-review.md` findings T3, T4, T5, T6 and T7, TASK-0039
Eight injected violations passed their gate on 2026-09-30. Examples: a reference from `Engine.Core` to
`3DEngine.Core` inside an `Include` with two paths, `double.Pow` in `Engine.Core`, a task with the
status `ready`, a register with an open code fence, and a change inside a merge commit. TASK-0039
tested that each gate can fail. It did not test the forms that a gate cannot see. The limit is
earlier than the class permits, because the code tasks start before that date and each one relies on
the gates.
Exit: each injected violation of the second review and of the third review fails its gate.
Progress 2026-09-30: TASK-0043 closed the holes of T3, T4 and T6, and the status and the fence parts of
T5. The `affects` parser, the reserve heading, the codebase review gate and the findings of T7 stay.
Progress 2026-10-04: the third codebase review finds T3 open in one part of three (a commit can name
a task that is `Done`) and T4 open in three parts of four (four forms pass the determinism gate, a
closed identifier passes the marker gate, and the dispatch gate reads two projects by name). The line
of 2026-09-30 said that TASK-0043 closed the holes of T3 and T4; it closed some of them. The review
adds T9 (the project file of the tests and a props file get past the gates), T10, T11 and two parts
of T13 to this entry, and the exit now names the injections of the third review too.
Progress 2026-10-04: TASK-0047 corrected T9: the project file of the tests, each `Directory.Build`
file, the cut-off and each workflow are gate files, and the dependency gate and the x86 gate read each
file that MSBuild reads. It corrected the open part of T3: a task that is `Done` before a commit and
after it governs nothing. It corrected the part of T11 for the task file, which the gate now reads at
the commit. Each change failed on an injection first. T4 (three parts), T5, T7, T10, the rest of T11
and two parts of T13 stay.
Progress 2026-10-08: the fourth codebase review adds T14, T16, T17, T23, T25 and the comment part of
T24. The worst is T14: a module initializer in a test file can clear the variable that the write-set
gate reads, so the commit judges itself. Question Q1 of that review asks where the check must run.
T15 is corrected in the review (the write set of TASK-0038), and TASK-0049 corrected T18 to T22.
Progress 2026-10-08: the owner answered Q1 of the fourth review with option A: the dynamic write-set
check runs as a program in `eng/` that the pipeline builds from `main`, outside the test assembly.
TASK-0053 holds it, and TASK-0038 depends on it, because TASK-0038 permits `Engine.Tests/**`.
Progress 2026-10-09: TASK-0053 corrected T14. The judge `eng/write-set-check` runs from a build of
`main`, and a local run of the pipeline steps failed the injection of the review and a commit that
changed the judge. The workflow still comes from the judged commit, so a task that names
`.github/workflows/ci.yml` exactly can change the job; a branch protection on GitHub closes that.
Progress 2026-10-09: run 37963719832 on a branch that was never merged proved the judge in the pipeline.
The same run found a new hole: the job "Contract-touched-needs-ADR" passed a commit that changed
`Engine.Contracts/Document.cs` with no ADR. On the first push of a branch the job compares the range
from the cut-off commit, and an ADR changed somewhere in that range, so the job passes each new
branch. A later push and a push to `main` compare from the previous tip and are not affected.

### R-0033 · Seven findings of the third codebase review in the engine and the hosts have no task
- class: debt
- opened: 2026-10-04
- due: 2027-04-02
- extended: no
- refs: `docs/reviews/2026-10-04-codebase-review.md` findings E18 to E24, TASK-0038
A sink that throws leaves a gap in the sequence, and nothing sends a reset (E18). A reference to the
Document or to the sink can leave the session (E19). A call back into the session blocks every client
(E20). A handler that fails after it changed the backend leaves an orphan solid (E21). A cancellation
from a handler is cached (E22). The bind guard reads one source of addresses (E23). E24 holds nine
small findings. E20 and E23 must be corrected before TASK-0038 ships, because the desktop host then
serves the surface while a person has a document open.
Exit: each finding has a task, or it is corrected, or the owner accepts it and gives the reason.
Progress 2026-10-04: the owner answered question Q4 of the review: a sink must not throw, by
contract, and a test holds each sink to it. That is the correction of E18. TASK-0047 records it.
Progress 2026-10-08: the fourth codebase review finds each of E18 to E23 open, and E24 open in seven
parts of nine (TASK-0035 and TASK-0036 corrected two). The order of the owner of 2026-10-08 puts the
corrections of E20 and E23 in the next task, before TASK-0037.
Progress 2026-10-08: TASK-0051 corrected E20 (a call back into the session throws at once) and E23
(the HTTP host checks each source of an address before the start and the bound addresses after it).
TASK-0038 now depends on TASK-0051. E18, E19, E21, E22 and seven parts of E24 stay.

### R-0036 · Fifteen findings of the fourth codebase review in the engine and the hosts have no task
- class: debt
- opened: 2026-10-08
- due: 2027-04-06
- extended: no
- refs: `docs/reviews/2026-10-08-codebase-review.md` findings E25 to E39, TASK-0019
A replayed Document can never get a command bus, so a load path cannot continue live (E25). After a
divergence the backend keeps its bodies, and a handler exception escapes the replay raw (E27, E28).
The version can go back through a cast of the log (E26). E29 to E39 are smaller defects in the replay,
the queries, `/schema/backend`, the exit codes and the backend selection.
Exit: each finding has a task, or it is corrected, or the owner accepts it and gives the reason.

### R-0034 · The pipeline can hang for six hours, and three tests depend on timing
- class: risk
- opened: 2026-10-04
- due: 2026-11-03
- extended: no
- refs: `.github/workflows/ci.yml`, `docs/reviews/2026-10-04-codebase-review.md` findings T8, T12 and T13
No job has a time limit, and `dotnet test` has no limit for a hang, so a test that hangs holds a
runner for 360 minutes and gives no annotation (T8). The heartbeat test measures a gap on the client,
the collection "real host" runs beside the concurrency tests, and the deadlock test can skip its work
on a slow runner (T12). The Windows run of 2026-10-01 failed in a test of this kind.
Exit: each job has a time limit, `dotnet test` stops a hang and names the test, and each test of T12
is corrected or the owner accepts it.
Progress 2026-10-04: TASK-0047 corrected T8. Each job has a time limit, the step "Test" stops a test
after two minutes, and the report script names the test that did not complete. Run 37189044285 on a
branch that is never merged stopped an injected hang after about 130 s on each runner and named it.
The script also reads a stopped run, which is a part of T13. T12 and the other part of T13 stay.
Progress 2026-10-08: during TASK-0051 one local run of the full suite ended with "test run aborted",
and its output is lost. Twenty later runs passed, thirteen with `--blame-crash`, and no run made a
crash dump. The cause is not known. TASK-0051 added two tests that start the real host. A second abort
must be read with `--blame-crash`, and it adds a part to T12.
Progress 2026-10-09: the heartbeat test of T12 failed again, on Windows in run 37963719832: heartbeat 3
came 0 ms after heartbeat 2, and the interval is 100 ms. The commits of that run did not touch the
code of the test or of the host. The limit of this entry is 2026-11-03.

## Accepted compromises

These are conditions that the project keeps permanently and by decision. They do not age.

### R-0008 · The CLI argument parser is a placeholder
- class: interim
- opened: 2026-05-11
- due: 2027-05-11
- extended: no
- refs: `Engine.Cli/ArgParser.cs:3-5`
The parser reads `--param k=v` pairs. The file says that the wire-format task replaces it with
JSON input.
Exit: JSON input dispatch exists. Or: accept the parser permanently.
Accepted 2026-09-22 - TASK-0025, v0.26
The project keeps the parser. The convention `--param k=v` is normal for a command line. ADR-0016
gave type coercion and validation to `ParameterBinder`, therefore the parser splits text and makes
no decision about a value. JSON input on the command line would copy the HTTP surface and give
nothing that the HTTP surface does not give. The comment in `Engine.Cli/ArgParser.cs` no longer
says that a later task replaces the file.


---

## Closed

The register keeps each closed entry. Do not delete an entry. Move it here and give the outcome.

Use one of these three lines:

```
Closed YYYY-MM-DD · resolved · commit <hash>
Closed YYYY-MM-DD · decided  · ADR-nnnn
Closed YYYY-MM-DD · accepted · the reason to keep the condition
```

### R-0001 · The preview SDK version prevents a build on a new computer
- class: risk
- opened: 2026-08-25
- due: 2026-09-24
- extended: no
- refs: `global.json:3-5`, `3DEngine/Documentation/Running.md:7`
`global.json` sets `rollForward` to `disable` for a preview SDK. A public feed does not supply
that SDK. A computer with only release SDKs cannot build the solution.
Exit: `global.json` gives a release version and uses `latestFeature`. Running.md agrees with
`global.json`.
Closed 2026-09-20 - resolved - TASK-0014, v0.16

### R-0003 · The replay determinism gate uses a different set of handlers
- class: risk
- opened: 2026-08-26
- due: 2026-09-25
- extended: no
- refs: `Engine.Tests/ReplayDeterminism/`, `Engine.Cli/Cli.cs:240-260`, `Engine.Api.Http/EngineHost.cs:36-50`
Seven positions in the solution register handlers. Therefore the determinism gate does not use the
set of handlers that the hosts use. The architecture depends on this one test.
Exit: the hosts and the tests use one handler catalog.
Closed 2026-09-20 - resolved - TASK-0016, ADR-0016, v0.18



### R-0004 · The dependency direction gate does not exist
- class: debt
- opened: 2026-08-25
- due: 2026-11-23
- extended: no
- refs: `CLAUDE.md`, `.github/PULL_REQUEST_TEMPLATE.md:16`, `docs/adr/0009-3dengine-core-peer-render-kernel.md:109`
`CLAUDE.md` lists this gate. No CI step examines project references. No test examines project
references. An agent in an unfamiliar area can break this rule, and no check finds the error.
Exit: a test reads each project file and verifies the rules. `dotnet test` runs the test.
Closed 2026-09-20 - resolved - TASK-0017, v0.19

### R-0006 · The ADR index does not record supersession or amendment
- class: debt
- opened: 2026-08-25
- due: 2026-10-24
- extended: no
- refs: `docs/adr/README.md:19,27`, `docs/adr/0004-engine-runtime-is-authority.md:38`, `docs/adr/0011-server-default-deployment-topology.md:23-25`, `docs/adr/0012-geometry-backend-wiring.md:193`
ADR-0011 contradicts the deployment default in ADR-0004, but the index gives no note. ADR-0012
contains Amendment 1, but the index does not give it. A person maintains this index by hand, and
it is now incorrect.
Exit: each ADR has front matter with reciprocal supersession. A tool generates the index. CI
verifies the index.
Closed 2026-09-20 - resolved - TASK-0017, v0.19
Note on the exit. The exit line asked for a tool that generates the index. The work used a test
that compares the index against the front matter instead. The protection is the same: drift fails
the build. The cost is much lower. TASK-0017, section Scope (out), records this decision.

### R-0012 · A branch contains duplicate identifiers
- class: risk
- opened: 2026-08-25
- due: 2026-09-24
- extended: no
- refs: branch `claude/happy-booth-1cef3f`
This branch contains a different ADR-0014, a different TASK-0012, a different TASK-0013 and a
different version v0.12. The main branch uses the same identifiers for different work. The branch
also contains a working engine hosting factory that nobody merged. The reference "ADR-0014" is
therefore not clear.
Exit: a person keeps the useful work and gives it new numbers. Or: a person records that the
project abandons the branch.
Closed 2026-09-20 - resolved - TASK-0018, v0.20
The project keeps the useful work and gives it new numbers. The hosting factory is merged, in an
adapted form. The persistence record is ADR-0015. The persistence task is TASK-0019. The duplicate
v0.12 label is recorded in the v0.20 ledger entry. The branch itself stays, because the owner
decides when to remove it.

### R-0013 · The renderer proposal exists only in a stash
- class: risk
- opened: 2026-08-25
- due: 2026-10-20
- extended: 2026-09-20 (milestone P0.5 salvages the branch and the stash together)
- refs: `stash@{0}`, file `docs/proposals/render-host-direction.md`
A large analysis of options exists in a stash. No commit contains it. The analysis disappears if a
person removes the stash.
Exit: a commit contains the file in `docs/proposals/`. Or: a person removes the analysis by
decision.
Closed 2026-09-20 - resolved - TASK-0018, v0.20
A commit holds `docs/proposals/render-host-direction.md`. The file came from the third parent of
the stash commit, where git keeps an untracked file. A header note records that two statements in
the analysis are out of date.

### R-0016 · Reduce the maximum quantity of open entries from 20 to 12
- class: question
- opened: 2026-08-27
- due: 2026-11-25
- extended: no
- refs: this file, rule 4
The limit is 20 to hold the entries from the architecture investigation. The limit controls growth.
A limit of 20 is too high to have an effect.
Exit: the limit becomes 12 after a person examines each imported entry. Or: 20 becomes the
permanent limit.

---
Closed 2026-09-21 - decided - the limit becomes 15
The owner chose 15 and not the 12 that this entry proposed. The register held 11 open entries on
that date. A limit of 12 gives one free slot, therefore the next new problem would force an exit
on an old one immediately. A limit of 15 gives four free slots. The limit has an effect and it
does not stop work. Rule 4 and `Engine.Tests/Governance/RegisterGateTests.cs` both give 15.

### R-0017 - No check verifies the write set of a task
- class: debt
- opened: 2026-09-20
- due: 2027-03-19
- extended: no
- refs: `docs/templates.md`, `tasks/TASK-0017-mechanical-governance.md:9-47`
Each task file declares a `writes` block with create, modify and forbid lists. Nothing reads that
block. An agent can change a forbidden file, and no check finds the error. The declaration is a
wish and not a rule.
Exit: a step in continuous integration compares the changed files against the write set of the
active task. A decision exists about a local hook.
Closed 2026-09-21 - resolved - TASK-0022, v0.22
`Engine.Tests/Governance/WriteSetGateTests.cs` reads the block. The job `write-set-gate` gives it
the list of changed paths. The hook before each commit is refused and not deferred: a person passes
a hook with one flag, and a hook needs an installation step on each computer. TASK-0022 records the
decision.

### R-0009 · The native build workflow says DRAFT
- class: debt
- opened: 2026-08-25
- due: 2026-10-24
- extended: no
- refs: `.github/workflows/build-manifold-native.yml:10`
The header says "DRAFT — not yet executed". The workflow ran successfully on the main branch, and
the repository contains its output. This comment gives incorrect information to the next reader.
Exit: the header agrees with the facts.
Closed 2026-09-22 - resolved - TASK-0017, v0.19; the entry moved here by TASK-0023
The marker gate of v0.19 found the false header and TASK-0017 corrected it. The Outcome of
TASK-0017 states that this entry is resolved, and nobody moved the entry. The register and the
repository now agree.

### R-0014 · The `.claude` directory is not in `.gitignore`
- class: question
- opened: 2026-05-06
- due: 2026-10-20
- extended: 2026-08-25 (imported from open-questions.md; the original lifetime had already passed)
- refs: this entry replaces OQ-0001
`git status` shows session metadata after each run.
Evidence 2026-09-20: the dependency direction gate must exclude this directory, because a worktree
under it holds a full copy of each project file. A stale copy shadowed the real one and the gate
reported a broken rule as satisfied.
Exit: `.gitignore` contains the directory. Or: the repository holds the directory by decision. Or:
accept the condition.
Closed 2026-09-22 - decided - `.gitignore` contains the directory - TASK-0023, v0.23
The directory holds settings, a transcript and a git worktree. Each one is host-specific and
regenerable. Nothing under it was ever tracked. A worktree under it held a full copy of each
project file and made the dependency direction gate report a broken rule as satisfied. The gate
keeps its filter as a second defence.

### R-0002 · CI never ran on Windows or on macOS
- class: risk
- opened: 2026-08-25
- due: 2026-09-24
- extended: no
- refs: `.github/workflows/ci.yml` (the three line numbers 11, 29 and 57 applied before v0.20)
Each job uses `ubuntu-latest`. A person verified the native geometry path on win-x64 by hand only.
The desktop host ran on Windows only. The project requires continuous verification on three
operating systems.
Progress 2026-09-20, TASK-0020: the workflow now uses a matrix of three runners and a smoke test
for CreateBox. The entry stays open until continuous integration reports a pass on each runner. A
local computer must not run the gate, therefore this session cannot close the entry.
Exit: the build, the tests and the headless smoke test pass on three runners.
Closed 2026-09-22 - resolved - TASK-0020, v0.24
Run 35786216373 reports a pass on ubuntu-latest, on windows-latest and on macos-latest. Each job
runs the build, each test, the smoke test for NoOp and the smoke test for CreateBox. The trigger
change of v0.23 made the report automatic: a push gives it and a person opens nothing.

### R-0005 · The native package has no licence notices
- class: risk
- opened: 2026-08-25
- due: 2026-09-24
- extended: no
- refs: `nuget/Engine.Geometry.Manifold.Native.3.5.2.nupkg`, `ADR-0014 §5`
The repository contains a binary file of 4.4 MB. Its nuspec file has no licence element. The
repository has no third-party notices file. ADR-0014 and TASK-0012 do not give a licence. ADR-0014
§5 also requires a checksum, but no checksum exists.
Exit: a file `THIRD-PARTY-NOTICES.md` lists each component and its licence. Checksums exist.
Closed 2026-09-22 - resolved - TASK-0024, v0.25
`THIRD-PARTY-NOTICES.md` names Manifold under the Apache License 2.0 and Clipper2 under the Boost
Software License 1.0, and it gives a SHA-256 value for the package and for each of the six distinct
native binaries. `Engine.Tests/Governance/NativePackageGateTests.cs` compares each value against
the file in both directions, therefore a checksum is a rule and not a decoration.

### R-0010 · The boundary document is not correct but the index calls it canonical
- class: debt
- opened: 2026-08-25
- due: 2026-10-24
- extended: no
- refs: `docs/architecture/engine-runtime-boundaries.md`, `docs/INDEX.md:5`
Nobody changed this document after the first ADR. It is older than ADR-0009 to ADR-0014. It gives a
`project.json` format that never existed. It says that Manifold is the V1 backend, but phase P7a
shipped a managed substitute. It lists CI gates that nobody built.
Exit: a person rewrites the document. Or: the document moves to an archive and `INDEX.md` points
to a different document.
Closed 2026-09-22 - decided - the document moves to an archive - TASK-0025, v0.26
The document is at `docs/archive/engine-runtime-boundaries.md` with a header that forbids its use.
`docs/INDEX.md` now points at the section "Authority diagram" in `CLAUDE.md`. A rewrite was the
other exit and it was refused: the content that is correct lives in that section and in the ADRs,
therefore a rewrite would give a second place for one decision.

### R-0011 · Two diagnostic codes have no source that emits them
- class: question
- opened: 2026-08-25
- due: 2026-11-23
- extended: no
- refs: `docs/diagnostics.md:17,26`
No inbound queue exists, therefore no code emits `E-CMD-BUS-BUSY`. The fallback backend removes
the need for `E-GEOM-BACKEND-INIT`. Reserved codes are correct. An unlimited quantity of reserved
codes makes the register unreliable.
Exit: a source emits each code. Or: each code becomes permanently reserved and gives a reason.
Closed 2026-09-22 - decided - each code is reserved permanently - TASK-0025, v0.26
`docs/diagnostics.md` gains a section "Permanently reserved codes" that names each code and gives
the reason. The section is an addition, because CLAUDE.md permits an addition to that file and
nothing else. `Engine.Tests/Governance/DiagnosticsReserveGateTests.cs` bounds the quantity at two.
The entry asked about two codes. The answer is a limit, because a decision about two codes returns
as the same question at the third one.

### R-0015 · The CLI escapes each apostrophe in JSON output
- class: question
- opened: 2026-05-06
- due: 2026-10-20
- extended: 2026-08-25 (imported from open-questions.md; the original lifetime had already passed)
- refs: this entry replaces OQ-0002
`JavaScriptEncoder.Default` writes each apostrophe as a Unicode escape. The output is correct JSON.
The tool `jq` reads it correctly. A person who reads the raw output sees noise.
`UnsafeRelaxedJsonEscaping` corrects this, but it escapes fewer characters.
Exit: the CLI uses the different encoder. Or: accept the output.
Closed 2026-09-22 - decided - the command line uses the relaxed encoder - TASK-0025, v0.26
`Engine.Cli/JsonRenderer.cs` uses `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`. A person reads that
output in a terminal. The word Unsafe names one risk: a page that writes JSON into HTML with no
further encoding. `Engine.Api.Http` keeps the default encoder for that reason, because a browser
client can put a response into a page.

### R-0019 · Five documents describe a repository state that no longer exists
- class: debt
- opened: 2026-09-23
- due: 2026-10-23
- extended: no
- refs: `docs/adr/README.md:7-10`, `docs/adr/README.md:51-54`, `docs/INDEX.md:37`, `docs/conventions.md:61`, `docs/templates.md:231-236`, `Engine.Tests/Governance/AdrGateTests.cs:17`, TASK-0027
TASK-0027 found six statements that were true once. The ADR index says that ADR-0015 exists only on
a branch that TASK-0023 deleted, and its legend gives four statuses of six, while a comment in the ADR
gate says that the legend agrees with the gate. `docs/INDEX.md` names `docs/architecture/`, which does
not exist. `docs/conventions.md` links to that directory and cites a `CLAUDE.md` section by an old
name. `docs/templates.md` says that two gates exist; eleven gate classes exist. An agent reads these
files first, therefore the limit is 30 days and not 180.
Exit: each statement agrees with the repository, and a gate fails when `docs/INDEX.md` names a path
that does not exist.
Progress 2026-09-25: TASK-0032 corrected the two statements in `docs/adr/README.md`. The legend now
gives the six statuses, and the section about ADR-0015 on a branch is gone.
Closed 2026-09-26 - resolved - TASK-0039, v0.34
Each of the fourteen statements that the rules review of 2026-09-25 lists in section 3.5 is
corrected or removed. `Engine.Tests/Governance/DocumentPathGateTests.cs` fails when `docs/INDEX.md`
names a path that does not exist, and when a gate class has no row in the gate table.

### R-0026 · A commit that touches a task file gets all the permits of that task
- class: risk
- opened: 2026-09-25
- due: 2026-10-25
- extended: no
- refs: `Engine.Tests/Governance/WriteSetGateTests.cs` (the test `Every_Changed_File_Is_Inside_The_Write_Set_Of_A_Changed_Task`), TASK-0032
The write-set gate lets each task file that a commit touches govern that commit. On 2026-09-25 the
change list of TASK-0032 received `Engine.Core/CommandBus.cs` as an injection, and the gate passed.
The reason: TASK-0034, TASK-0035 and TASK-0037 are new in the same commit, and each one permits that
file. A commit that plans work can therefore also change code, and no check sees it. A second
injection, a file that no task permits, failed as it must.
Exit: the gate limits the permits of a commit to the task that does the work of the commit. Or: the
owner accepts the risk and gives the reason.
Closed 2026-09-26 - resolved - TASK-0039, v0.34
The write-set gate reads the task that the commit trailer names, and only that task governs the
commit. A commit with no trailer that touches several task files fails. The injection of this
entry, the change list of v0.32 plus `Engine.Core/CommandBus.cs`, fails with TASK-0032 named and
fails with no task named.

### R-0029 · An idle subscriber receives 1,025 heartbeat frames and not one
- class: risk
- opened: 2026-09-30
- due: 2026-10-30
- extended: no
- refs: `Engine.Api.Http/WebSockets/Subscriber.cs:117-132`, `docs/reviews/2026-09-30-codebase-review.md` finding E8
The heartbeat loop writes a frame and starts again with no wait. A run on the real host gave 1,025
heartbeat frames at second 30 for one idle subscriber. While the channel is full of heartbeat frames,
the next event does not fit and the host disconnects the subscriber as lagged. No test waits for a
heartbeat.
Exit: the loop waits one interval after each frame, and a test with a short interval counts the
frames.
Closed 2026-09-30 - resolved - TASK-0044, v0.37
The loop moves the time of the last send when it writes a frame, so it waits one interval before
the next one. The run of the review, one idle subscriber for 36 seconds, gives 1 heartbeat frame
at second 30. A test with an interval of 100 ms counts at most eleven frames in one second.

### R-0032 · A failed test in the pipeline has no name that a person without a sign-in can read
- class: risk
- opened: 2026-10-01
- due: 2026-10-31
- extended: no
- refs: `.github/workflows/ci.yml` step "Test", TASK-0044, TASK-0034
The log of a job needs admin rights: the endpoint of the job log answered 403 on 2026-10-01. The
annotations of a check run are public. Two runs failed in the test step with no name: Ubuntu on
2026-09-30 (v0.38) and Windows on 2026-10-01 (run 36920665584). In each case the suite passed many
times on this computer, so the session could only guess the test.
Exit: the step "Test" writes the name and the message of each failed test as a public annotation, and
an injected failure shows it.
Closed 2026-10-03 - resolved - TASK-0045, v0.40
The step "Test" writes a .trx file, and `eng/report-failed-tests.js` writes each failed test as a
public annotation. On the branch `r0032-injection-2026-10-03`, run 37102594075, an injected failure
gave an annotation with its name on each of the three runners. The check-run API returned it with no
sign-in. On Windows the same run also named a real failure, which TASK-0034 then corrected.

### R-0018 - The native package records the commit of the wrong repository
- class: debt
- opened: 2026-09-22
- due: 2027-03-21
- extended: no
- refs: the nuspec inside `nuget/Engine.Geometry.Manifold.Native.3.5.2.nupkg`, `THIRD-PARTY-NOTICES.md` section 3, `ADR-0014 §5`
The nuspec holds `<repository type="git" commit="718eab0684178e4fdf7ef419cc4ff26484008705" />`. That
commit is not a Manifold commit. It is a commit in this repository, dated 2026-07-04, with the
subject "Merge pull request #8 from Hperruchon/p7b-finish". The description in the same nuspec says
"Version tracks the pinned Manifold commit", therefore the package gives incorrect information about
the origin of its binary. A reader cannot tell which source produced the payload.
Exit: a new build records the Manifold commit. The packing step in
`.github/workflows/build-manifold-native.yml` reads the commit of the Manifold checkout and not the
commit of this repository.
Closed 2026-10-07 - resolved - TASK-0048, v0.45
The pin job resolves the Manifold reference to its commit, the build checks it out, and the pack step
records it with the Manifold address and no query to git. The nuspec of
`nuget/Engine.Geometry.Manifold.Native.3.5.2.1.nupkg` holds `commit="11235e6b8ebea2dbed8aec4285685aafd3d95667"`
and no branch, and `NativePackageGateTests` holds it equal to the commit of the notices.

### R-0035 · The native package names a build folder as the search path of its dependency on Linux and macOS
- class: debt
- opened: 2026-10-06
- due: 2027-04-04
- extended: no
- refs: `.github/workflows/build-manifold-native.yml`, `nuget/Engine.Geometry.Manifold.Native.3.5.2.nupkg`, `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs`, TASK-0036
`libmanifoldc` stores `/home/runner/work/3DEngine/3DEngine/build/src` on Linux and
`/Users/runner/work/3DEngine/3DEngine/build/src` on macOS as the search path of `libmanifold`, and
not `$ORIGIN` or `@loader_path`. On another computer the system loader does not find the dependency,
so the native backend never loaded on the Linux and macOS runners: the native tests skipped and the
hosts took the managed backend. TASK-0036 found it, and it loads the dependency first by its full
path as a workaround. The rebuild needs a manual run of the build workflow, which needs a sign-in.
Exit: the build workflow sets `$ORIGIN` and `@loader_path`, the repository holds the rebuilt package
with its checksum, and the workaround in `ManifoldGeometryBackend.TryLoadNative` is removed.
Progress 2026-10-06: TASK-0048 holds the rebuild, with the status `Ready`. It also removes the two manual
steps of a rebuild, if the owner answers yes to its question Q1.
Closed 2026-10-07 - resolved - TASK-0048, v0.45
The build gives the search paths `$ORIGIN` and `@loader_path`, a step on macOS removes the absolute
entry, and a check in the build job fails on a path of the build machine. The package 3.5.2.1 replaced
3.5.2, the workaround in `ManifoldGeometryBackend` is removed, and run 37540059266 passed on the three runners
with no native test skipped. A push of the pin file or of the workflow starts a rebuild, and the
package arrives on a branch that git fetches with no sign-in.

### R-0037 · `/schema/events` gives the name of each event kind and not its payload fields
- class: question
- opened: 2026-10-08
- due: 2027-01-06
- extended: no
- refs: `Engine.Api.Http/Schema/SchemaTypes.cs:28`, ADR-0021 item 6, ADR-0008 §9, TASK-0037
TASK-0037 asks that `/schema/events` list `body.consumed` "with its payload field `bodyId`". Each entry
of the endpoint holds the kind only, and ADR-0021 item 6 says only that the endpoint lists the kind.
TASK-0037 followed the ADR. An agent or a client that reads the schema cannot learn the payload of any
event kind, and must read the ADRs.
Exit: the owner decides if each entry gives its payload fields. A yes needs an amendment of ADR-0008
§9 and a task; a no closes this entry with the reason.
Closed 2026-10-08 · decided · ADR-0008
The entry was a decision and not a question. ADR-0008 §9 already says that `/schema/events` gives
the event kinds and their payload schemas, so the code disagrees with the ADR. The exit line above is
false: no amendment is needed. On 2026-10-08 the owner confirmed the payload fields. TASK-0055 corrects
the code, after TASK-0054.
