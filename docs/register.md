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
Progress 2026-10-16: TASK-0076 corrected V5 and V6 in the split of the device. With no suitable device the
core throws with a message, and a device that presents must have the swapchain extension; each name
goes to Vulkan from native memory. The findings of V7 stay.

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
Progress 2026-10-09: the codebase review of 2026-10-09 corrects the line above: a later push and a push
to `main` are affected too, because the job checks a range and not each commit; one ADR change in the
range lets a contract change with no ADR pass (T26). The review adds T26 to T35, three of them high:
a program named `git` in the checkout replaces git for the judge (T29), a root commit or a merge
carries any change past the judge (T30), and the attack class of T14 is open for the other gates
(T31). TASK-0057 holds T26 and T28 to T35, and TASK-0058 holds T27.
Progress 2026-10-10: the owner answered the questions of that review. Q1, yes: the owner protects
`main` on GitHub against a force push, which closes one path of T28 when it is set. Q2, option A: the
judge refuses four calls under `Engine.Tests/` (T31), in TASK-0057.
Progress 2026-10-10: TASK-0057 corrected T26, T29 to T35 and the parts of T28 in the code: the judge
runs git by a full path, fails a commit with no parent, judges a merge against its first parent,
refuses the four calls, holds the contract rule for each commit, and comes from `main` or the job
fails. Tests run the real judge on scratch repositories. The workflow still comes from the judged
commit; the branch protection of the owner closes the force push. T4, T5, T7, T10, T13, T16, T17,
T23, T24 and T25 stay.
Progress 2026-10-10: TASK-0058 corrected T27: the process test of a port key asserts that no address
was bound, and an injected bind before the refusal failed it.
Progress 2026-10-11: the codebase review of 2026-10-11 corrects the line of 2026-10-09 that says a
branch protection on GitHub closes the case of a task that changes `.github/workflows/ci.yml`. It does
not: a protection against a force push leaves a commit that edits the workflow free to change the job
of the same run (T38); question Q2 of that review gives the options. The review also finds T31 open
again (six bypasses of the four texts), a lost rule of the judge for a merge (T36), tests that do not
hold each rule (T37), and two smaller gaps (T39, T40). TASK-0061 holds T36 to T40; question Q1 holds
T31.
Progress 2026-10-11: the owner answered Q1 and Q2 of that review. Q1, yes: TASK-0064 moves the gates
that read the repository into a program that the pipeline builds from `main` (T31). Q2, option A: the
condition of the workflow file is accepted, in entry R-0038 under "Accepted compromises" (T28, T38).
Progress 2026-10-11: TASK-0061 corrected T36, T37, T39 and T40. A merge is judged on each file that
differs from each parent and on each file that differs from its first parent and that no commit of
its second parent changed; the two forms of T36 failed the old rule and fail the new one. T31 stays
with TASK-0064.
Progress 2026-10-12: TASK-0064 corrected T31 for the fourteen gate classes that read only files of the
repository. The program `eng/repo-gates` compiles them from a build of `main` and reads the judged
checkout as data. The escaped attribute and `AppDomain.SetData` of the review passed the marker gate
as a test and failed it from the program. When the judged commits change a source of the program, its
gates come from the judged commit, with the condition of R-0038. `DispatchSurfaceGateTests`,
`SchemaEndpointGateTests` and `ReplayDeterminismGateTests` run engine code, so they stay in the test
process, and the attack class still reaches them; the four-text refusal of the judge stays as their
second line. T4, T5, T7, T10, T13, T16, T17, T23, T24 and T25 stay.
Progress 2026-10-12: the codebase review of 2026-10-12 finds that the checks from `main` still take
inputs of the build from the judged checkout: a `global.json` chooses the SDK that builds the judge and
the gates (T41), and files that are not gate files switch the gates to the judged commit and control its
packages (T44). A merge can undo a change of `main` with no judgement (T42). T41 is a defect of the step
of TASK-0064. The review also adds T43, T45, T46 and T47, and finds T31 fixed in part. TASK-0066 holds
T41 to T47.
Progress 2026-10-12: TASK-0066 corrected T41 to T47, and the part of T16 that skipped an import with a
property. Each build of a program of `main` runs outside the judged checkout, each input of a build is
a gate file, the judge answers the switch of the gates, a merge is judged against the automatic merge
of its parents, the determinism gate binds each call with Roslyn, the dependency gate refuses an import
that it cannot resolve, and the runner fails on a form that it cannot run. T4, T5, T7, T10, T13, T17,
T23, T24 and T25 stay.
Progress 2026-10-12: the codebase review of 2026-10-13 corrects the line above: T24 was fixed, and the
line listed it as open. It finds that four kinds of file that are not gate files can take `Engine.Tests`
out of the step "Test" with a green run (T48, high), that the determinism gate and the dependency gate miss
forms that MSBuild evaluates (T49, T50), that a rename carries a change into a gate file through a clean
merge (T51), and two small gaps (T52, T53). TASK-0073 holds T48 to T53.
Progress 2026-10-14: TASK-0073 corrected T48 to T53. Each input of a build that chooses the projects of a
run is a gate file, and the step "Test" fails below a floor of executed tests. The judge reads a merge with
no detection of renames. The determinism gate and the dependency gate refuse each form of the review, and
21 test cases hold those forms. The runner refuses an `async void` check, and two reviews may share a
date. T4, T5, T7, T10, T13, T17, T23 and T25 stay.
Progress 2026-10-17: the codebase review of 2026-10-17 finds the fixes of T48 to T53 held for their forms,
and new forms: two files that are not gate files can leave no test in `Engine.Tests` with a green run
(T54, high), the gates read MSBuild names with case (T55, high), and T56 to T61. T31 is worse through T54.
TASK-0078 holds T54 to T61.
Progress 2026-10-17: TASK-0078 corrected T54 to T61. A gate refuses a shadow of an attribute or of `Assert` of
xUnit, each test project has an exact count with one result file, the gates read MSBuild names with no regard
to case and refuse a wildcard reference, the determinism gate binds each name of the log path, the runner fails
an async lambda that returns void, the workflow reads the repository only, and the review gate checks the
declared ledger version. T31 stays open in part: the three gates that run engine code stay in the test process.
T4, T5, T7, T10, T13, T17, T23 and T25 stay.
Progress 2026-10-28: the codebase review of 2026-10-28 finds the fixes of T54 to T61 held for their forms, and
new forms: a test framework of xUnit in a file that is not a gate file makes each failed test pass with the exact
count (T62, high), a project under a `bin/` folder hides from the dependency and determinism gates (T63), the gate
of the UI library reads text (T64), and T65 to T67. T31 is worse through T62. TASK-0088 holds T62, and TASK-0089
holds T63 to T67. T4, T5, T7, T10, T13, T17, T23 and T25 stay.
Progress 2026-10-28: TASK-0088 corrected T62. The gate refuses in each test project each point where a file can
give xUnit a framework, a runner, a sink, an order or a discoverer, and a product type with a name of xUnit where
a test sees it. T31 stays open in part: the three gates that run engine code stay in the test process.
Progress 2026-10-28: TASK-0089 corrected T63 to T67. A reference must name a project that the dependency gate reads,
the gate of the UI library reads names, XML and imports, an archived ADR needs its file and its status, a product
task with no roadmap row is a problem, and the runner refuses a discarded task. T4, T5, T7, T10, T13, T17, T23 and T25
stay, and T31 stays open in part.
Progress 2026-10-28: the second codebase review of 2026-10-28 finds new forms. A test framework in a `bin/` folder
inside a test project (T68, high) and an alias in the project file of `3DEngine.Tests` (T69, high) each make each
failed test pass with the exact count. T70 to T75 and T77 are medium or low. T31 is worse through T68. TASK-0097
holds T68 and T69 before K2, and TASK-0098 holds the rest after K2. T4, T5, T7, T10, T13, T17, T23 and T25 stay.
Progress 2026-10-28: TASK-0097 corrected T68 and T69. The test framework gate reads each source that MSBuild compiles
and the project file of each test project, and the judge names those project files as gate files. T31 stays open in
part.
Progress 2026-10-30: TASK-0107 corrected T80 to T87 for their forms. It found a new form that no gate reads: MSBuild
reads each `Directory.Build.rsp` above a project at the command line, and a `/p:TargetName=` or `/p:AssemblyName=` in
it sets the property for each project. A probe build outside the repository gave `Engine.Tests.dll` from such a
file. No such file is in the repository. The next codebase review gives it a
finding. T31 stays open in part.
Progress 2026-10-30: TASK-0114 corrected T89 to T95 for their forms. PendingTasks still does not see a task that waits
for a signal that nobody gives with a type other than TaskCompletionSource, for example a semaphore. T31 stays open in
part.

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
Progress 2026-10-31: TASK-0140 corrects E18 by the answer Q4: `IEventSink` states that a sink must not throw, and
`Engine.Tests/EventSinkContractTests.cs` finds each sink of the engine and of the surface and tests it.

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
Progress 2026-10-17: TASK-0067 corrected the open part of E36: each failure of the start of a host has a
documented exit code (5 for each failure other than codes 1 to 4), and no failure of the start crashes.
Progress 2026-10-31: TASK-0135 corrects E26 (the log is a read-only view), E29 (a cancelled replay throws
`OperationCanceledException`) and E32 (a query of a body that the backend does not hold gives `E-GEOM-NATIVE-OP`).
Progress 2026-10-31: TASK-0136 corrects E28 (an exception of a handler in a replay is a `ReplayDivergenceException`)
and E30 (the backend of a replay is required). E27 stays: its refusal needs a member of `IGeometryBackend` that tells
if the backend holds a body, which is a change of `Engine.Contracts` and needs the owner and an ADR.
Progress 2026-10-31: TASK-0137 corrects E37 (with no library, a misspelled name gives its own error) and E31 (the
comments of a repeated identifier, and the ".." of the message of a divergence). E25 stays: its correction needs a
decision on the handover of the bus of a replay, and an open of a document file is the load path today.
Progress 2026-10-31: TASK-0138 corrects E38 (the reason of a library that does not load gives a missing file or a
platform with no payload).
Progress 2026-10-31: TASK-0139 corrects E35 (ADR-0008 §9 lists `/schema/backend`, a gate holds the routes equal to
the list, and the endpoint gives the identity of the backend).
Progress 2026-10-31: TASK-0140 corrects E18 of R-0033, the only cause of E33; the next review judges E33 again.
Progress 2026-10-31: the owner accepts E25 and E27 (questions Q1 A and Q2 A of the codebase review of 2026-10-31 (b),
by the standing answer of 2026-10-31; TASK-0142 records it). E25: no production code replays a log, and the open of
a document file is the load path that continues live. E27: its refusal needs a change of `Engine.Contracts`, and
only a test or a script replays a log. E34 and E39 stay.

### R-0039 · No test calls the surface of the desktop host
- class: debt
- opened: 2026-10-11
- due: 2027-04-09
- extended: no
- refs: `3DEngine/DesktopSurface.cs`, `3DEngine/DesktopEngineHost.cs`, TASK-0062, findings E50, E52 and E54 of `docs/reviews/2026-10-11-codebase-review.md`
`DesktopSurface` is internal to the desktop program, and no test project references that program. The
port setting, the disposal of the session and the end of the window loop at a stop have no automatic
test. TASK-0062 corrected the three by reading and by one run of the executable before its window.
Exit: a test calls `DesktopSurface`, or the owner accepts the gap and gives the reason.
Progress 2026-10-16: TASK-0076 gave the program a test project, `3DEngine.ImageTests`, which tests
`SessionBodies` and `SceneRenderer` through the image test. No test calls `DesktopSurface` yet.

### R-0040 · The image test does not run on the Windows and macOS runners
- class: interim
- opened: 2026-10-14
- due: 2027-10-14
- extended: no
- refs: `3DEngine.ImageTests/VulkanImageFactAttribute.cs`, ADR-0025 item 4, TASK-0076, R-0022
The images of `windows-latest` and `macos-latest` give no usable Vulkan device, so the image test skips
there by decision of the owner (question Q2 of the codebase review of 2026-10-13, D3). A defect of the
Vulkan layer that only Windows or macOS shows passes the pipeline. The computer of the owner runs the test
on Windows with a GPU, and no computer runs it on macOS.
Exit: a runner of Windows or macOS gives a Vulkan device and the pipeline requires the test there, or the
owner accepts Ubuntu alone and moves this entry to the accepted compromises.
### R-0043 · The first click into the desktop window can reach the panel at a wrong place
- class: risk
- opened: 2026-10-28
- due: 2026-11-27
- extended: no
- refs: `3DEngine/Ui/UiInputRouter.cs`, `3DEngine.Vulkan/SdlInput.cs`, TASK-0101
In the visual check of K2b, a synthetic click into a window without the focus, with no motion before it, dragged
the panel once and turned the camera once. Clicks within one frame of a new pad also hit other keys. A hand moves
the mouse before a click, so a person may never see it, but a wrong button of a pad gives a wrong number.
Exit: a test of the order of the samples of a focus click explains it, and a change or a recorded reason closes it.
Progress 2026-10-30: TASK-0118 built a probe that logs each SDL event of the mouse and of the focus, each sample that
the router gives to the panel or to the intents, and each intent. A focus to another window gave one
`FocusLostSample` to both. Its click reached the window above the desktop, and the log held motions of a mouse that
the probe did not move, so the run stopped. The next run needs a time when nobody else uses the mouse.

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

### R-0038 · A commit that names the workflow file exactly can change the job that judges it
- class: risk
- opened: 2026-10-11
- due: 2026-11-10
- extended: no
- refs: `.github/workflows/ci.yml`, `docs/reviews/2026-10-11-codebase-review.md` findings T28 and T38, TASK-0063
GitHub runs the workflow of the pushed commit. The judge comes from `main`, but the job that builds and
runs it comes from the judged commit, so a commit can change that job. A protection against a force
push does not change that.
Exit: a required review of `.github/workflows/**` on GitHub, or the owner accepts the condition.
Accepted 2026-10-11 · the owner answered question Q2 of the review of 2026-10-11 with option A
The project keeps the condition. A workflow file is a gate file, so only a task that names
`.github/workflows/ci.yml` exactly may change it, and the diff of each such commit shows the change of
the job. A required review needs pull requests, which changes how the owner merges, for a risk that
needs a deliberate act.


---

## Closed

### R-0047 · The lag test of the events depends on the speed of the runner
- class: risk
- opened: 2026-10-31
- due: 2026-11-30
- extended: no
- refs: `Engine.Tests/Http/EventsEndpointLagTests.cs:53-79`, run 41214590078, TASK-0148
The slow subscriber overflows only when twelve posts arrive faster than its pump delay of 200 ms. In run 41214590078
on Windows, of a commit that changed documents only, no close came, and the read stopped at its limit of 10 s with
`OperationCanceledException`. A red run with no cause in the change costs a run and hides a true failure.
Exit: the test makes the overflow sure, for example with a pump that waits for a signal of the test, or the owner
accepts it with a reason.
Closed 2026-10-31 · resolved · TASK-0154. `SubscriberOptions.PumpGate` replaces the delay: the test holds the pump,
and four events overflow a channel of two on each runner. With a pause of 500 ms between the posts the old test failed
and the new one passed.

### R-0046 · An open of an earlier file and other input during a drag have no decision
- class: risk
- opened: 2026-10-30
- due: 2026-11-29
- extended: no
- refs: `docs/reviews/2026-10-30-b-codebase-review.md`, questions Q1 and Q2; TASK-0123; TASK-0124
The codebase review of 2026-10-30 (b) found that an open refuses a file in which a client sent a move again (E100),
because the guard of E95 also acts on an open, and ADR-0015 item 8 says that an open applies such an entry. ADR-0038
does not say what a wheel, a key of the camera, an undo or a hide does during a drag of the gizmo (V53). A decision
and the code disagree for E100, so the owner decides (`CLAUDE.md`, "Stop and ask").
Exit: the owner answers Q1 and Q2, and the first commits of TASK-0123 and TASK-0124 record the answers.
Progress 2026-10-30: the owner answered A to Q1 and Q2. TASK-0123 had its first commit before the answer, so its next
commit records Q1 A; the first commit of TASK-0124 records Q2 A and closes this entry.
Closed 2026-10-30 · resolved · TASK-0124. The owner answered A to Q1 and Q2 on 2026-10-30. The second commit of
TASK-0123 records Q1 A (E99, E100), and the first commit of TASK-0124 records Q2 A in ADR-0038 item 5 (V53).

The register keeps each closed entry. Do not delete an entry. Move it here and give the outcome.

Use one of these three lines:

```
Closed YYYY-MM-DD · resolved · commit <hash>
Closed YYYY-MM-DD · decided  · ADR-nnnn
Closed YYYY-MM-DD · accepted · the reason to keep the condition
```

### R-0045 · K0d has no decisions for an operand of another sketch and for the code of a failed step
- class: risk
- opened: 2026-10-30
- due: 2026-11-29
- extended: no
- refs: `docs/reviews/2026-10-30-codebase-review.md`, questions Q1 to Q3; `docs/roadmap.md`, row K0d; TASK-0116
The owner decided the first slice of K0 on 2026-10-30 (KQ1 to KQ4 A). The research of the codebase review of
2026-10-30 found three points that the answers do not decide: an operand of a `Subtract` that comes from another
sketch, the code of a failed step of the chain, and the future of `W-SKETCH-EXTRUDE-CONSUMED`. A wider meaning of a
code needs the owner (`CLAUDE.md`, "Diagnostic codes"). TASK-0116 stays Deferred until the answers.
Exit: the owner answers Q1 to Q3, and the first commit of TASK-0116 records the answers.
Closed 2026-10-30 · resolved · TASK-0116. The owner answered A to Q1, Q2 and Q3 on 2026-10-30. The first commit of
TASK-0116 records the answers in ADR-0039, which KQ4 A accepts, and in the rows of `docs/diagnostics.md`.

### R-0044 · K0b has no decisions for the rebuild of a solid after an edit of its sketch
- class: risk
- opened: 2026-10-29
- due: 2026-11-28
- extended: no
- refs: `docs/reviews/2026-10-29-codebase-review.md`, questions Q1 to Q6; `docs/roadmap.md`, row K0b; TASK-0104
The bus refuses a handle that it created before, an undo calls no backend, and a step holds no geometry, so a solid
cannot follow its sketch today. A rebuild changes the public shape of `Engine.Contracts`, which needs the owner and
an ADR. The codebase review of 2026-10-29 gives six decisions for K0b. K0b cannot start before the answers.
Exit: the owner answers Q1 to Q6, and the first commit of the K0b task records the answers.
Closed 2026-10-29 · resolved · TASK-0108. The owner answered A to each question on 2026-10-29. The first commit of
TASK-0108 records the answers, as `docs/templates.md`, section 7, requires: the roadmap rows K0b and K0c hold Q1 to
Q6. The ADR of K0b gives the contract, and the owner accepts it before the code.

### R-0042 · K2 has no decisions for its scope, its model and its datum
- class: risk
- opened: 2026-10-28
- due: 2026-11-27
- extended: no
- refs: `docs/reviews/2026-10-28-b-codebase-review.md`, questions Q1 to Q10; `docs/roadmap.md`, rows K2 and K0a; TASK-0095
The roadmap row of K2 names five entities and fourteen constraints, and no document gives the list. The sketch
model changes the public shape of `Engine.Contracts`, which needs the owner and an ADR. The second codebase review
of 2026-10-28 gives nine decisions for K2 and K0a, and one decision on the rule of the rules review (T78). K2 cannot
start before the answers.
Exit: the owner answers Q1 to Q10, and the first commit of the K2 task records the answers.
Closed 2026-10-28 · resolved · TASK-0096. The owner answered A to each question on 2026-10-28. The first commit of
TASK-0096, the next task, records the answers, as `docs/templates.md`, section 7, requires: the roadmap rows K2a,
K2b, K2 and K0a hold Q1 to Q9, and TASK-0098 holds Q10. The ADR of K2a gives the contract, and the owner accepts it
before the code.

### R-0041 · The desktop has no key for an undo or a redo
- class: question
- opened: 2026-10-28
- due: 2027-01-26
- extended: no
- refs: ADR-0031 item 11, ADR-0026 items 2 and 5, `3DEngine.Core/Input/InputSamples.cs`, TASK-0093
The desktop gives an undo and a redo through two buttons only. Ctrl+Z, Ctrl+Y and Ctrl+Shift+Z need the keys Z
and Y in the input layer and two new intents, and each intent needs a touch gesture and a parity script. No
computer of the project has a touch screen. A person who uses the keyboard expects these keys in a CAD program.
Exit: a task adds the two intents with their keys and touch gestures and amends ADR-0026, or the owner accepts the
buttons alone and moves this entry to the accepted compromises.
Closed 2026-10-28 · resolved · TASK-0094. ADR-0032 item 9 gives Control and Z, Control and Y, and Control, Shift
and Z, with a tap of three fingers and a tap of four fingers, through the intent layer. Z and Y come from the key
code, so the letter of the layout counts.

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
Closed 2026-10-28 · decided · ADR-0015. The owner accepted the rewritten ADR-0015 on 2026-10-28 (KD4): the document
file is the command log in JSON Lines, which the desktop opens and saves. TASK-0091 builds it in the engine and the
server, and P8f in the desktop.

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
Closed 2026-10-27 · decided · ADR-0027. The owner chose decision KD1 of the decision sheet of 2026-10-27: text and
panels through Dear ImGui in the desktop window, drawn by a first-party renderer. TASK-0084 builds it.

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
Progress 2026-10-10: two tests of T12 failed in one run, 37997004258, of a commit that changed two
documents only: the heartbeat test on Windows (heartbeat 2 came 1 ms after heartbeat 1), and on macOS
the deadlock test of `HttpConcurrencyTests` ("No command completed"). T12 failed in three of the last
four runs, so each merge now depends on a run without it.
Closed 2026-10-10 · resolved · TASK-0059, v0.55
Each part of the exit is met. TASK-0047 gave each job a time limit and a stop for a hang that names
the test. TASK-0059 corrected the three tests of T12: the heartbeat test counts the frames, the
deadlock test starts its clock after the host is up, and the collection "real host" runs alone. An
injected slow runner failed the old tests and passed the new ones. The abort of one local run of
2026-10-08 did not come back in at least 26 later runs; a second abort gets a new entry. The parts of T13 that
this entry held stay with R-0031.
