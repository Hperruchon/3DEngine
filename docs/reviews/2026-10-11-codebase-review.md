---
date: 2026-10-11
commit: de35f51
ledger: v0.58
previous: 2026-10-09-codebase-review.md
---

# Codebase review — 2026-10-11

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the sixth codebase review. The next review must come before ledger entry v0.65: the build fails
when a seventh milestone follows v0.58. `Engine.Tests/Governance/CodebaseReviewGateTests.cs` holds that
rule, and `docs/templates.md`, section 7, gives the form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is
the authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this
file does not.

| Item | Value |
|---|---|
| Code state | `main` at `de35f51`, the merge of v0.58 |
| Branch of this review | `codebase-review-2026-10-11` |
| Tests | 321 on this computer |
| Pipeline | Run 38163980708 on `main`: green on Ubuntu, Windows and macOS |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner
decides.

**Method.** Five tasks changed code after `f9521de`, the commit of the last review: TASK-0057 (the
judge and the pipeline), TASK-0059 (the tests of timing), TASK-0058 (the guards), TASK-0038 (the
surface library and the program) and TASK-0054 (the desktop host). Two review agents read them in
parallel, each one in its own git worktree: the hosts with the surface and the desktop, and the judge
with the pipeline and the gates. The agents ran probes and removed each one; the agent of the judge
built the old judge from `f9521de` and ran both judges on the same scratch repositories. I read each
line that this review cites, on disk. A line that says "a review agent ran it" means that I read the
cited code and did not repeat the run.

## Summary

- **Sixteen earlier findings are fixed, and four more in part.** E40, E41, E44, E45, E47, T12, T24,
  T26, T27, T29, T30, T33, T34 and T35 in full, E42 and E43 as recommended, and T13, T25, T28 and T32
  in part. **[Observed]**
- **The judge of TASK-0057 lost one rule of the old judge (T36, high).** A merge is judged on its own
  change minus the files that the side of its second parent changed. A merge of `main` into a branch
  can therefore rewrite any file that `main` changed, and the old judge, which read a merge with
  `--cc`, failed the same commit. A review agent ran both judges. **[Observed]**
- **T31 is open again (high).** The four texts of option A have six bypasses, two of which ran in a
  test process. Question Q1 asks the owner for option B, which the owner kept for a fifth form.
  **[Observed]** in runs of a review agent.
- **The address check before the start has a third gap (E48, medium).** Seven forms with brackets pass
  it, and Kestrel binds each interface for them. The check after the start then stops the host.
  **[Observed]** in a run of a review agent.
- **The desktop host does not stop well (E49, E50).** A subscriber holds the stop for 30 s, and Ctrl+C
  stops the surface and leaves the window. **[Observed]** in runs of a review agent.
- **Two owner tasks, no new register entry.** TASK-0061 holds the judge and the pipeline, and TASK-0062
  holds the hosts. The register stays at 13 of 15.
- **Sixty-seven findings are open: no critical, eight high, twenty-nine medium and thirty low.**
  Fifteen findings are new. **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A
source line is a line of a `.cs` or `.razor` file that git tracks. The script of this review gives
6,093 production lines in 102 files at `f9521de`, which are the values of the last review.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 10 | `Project(` lines in `3DEngine.sln`. The last review gave 9; `Engine.Api.Http.Server` is new. Git tracks 12 project files: the judge and the native package are not in the solution. |
| Production source lines | 6,503 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, 105 files. The last review gave 6,093 in 102 files. |
| Test source lines | 8,124 | `Engine.Tests`, 65 files. The last review gave 7,539 in 62 files. |
| Tests | 321 | `dotnet test 3DEngine.sln --no-build`: 321 passed, 0 failed, 0 skipped. The last review gave 302. |
| Gate classes | 17 | Test classes whose name ends in `GateTests`. The last review gave 17. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 13 | Section "Open" of `docs/register.md`. This review adds none. The last review gave 14; R-0034 closed. |
| ADRs | 21 | `docs/adr/0001` to `0021`. In force: 19 (12 accepted, 7 amended). |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 8 | The same method: V1, V2, V3, V5, T1, T4, T31, T36. The last review gave 9. |
| Findings: medium | 29 | The same method: E5, E6, E11, E12, E13, E14, E16, E18, E19, E21, E22, E25, E27, E28, E33, E48, E49, E50, V4, V6, T2, T5, T10, T16, T17, T28, T32, T37, T38. The last review gave 29. |
| Findings: low | 30 | The same method: E17, E24, E26, E29 to E32, E34 to E39, E46, E51 to E57, V7, T7, T13, T23, T25, T39, T40, C1, P1. The last review gave 30. |

Two more measures. The lines of each production folder: `Engine.Core` 1,648, `Engine.Api.Http` 1,301,
`3DEngine.Vulkan` 1,080, `eng` 759, `Engine.Cli` 424, `Engine.Contracts` 415, `Engine.Geometry.Manifold`
297, `3DEngine` 247, `3DEngine.Core` 221, `Engine.Api.Http.Server` 111. The tests of each folder of
`Engine.Tests`: `Governance` 90, `Http` 74, `Hosting` 31, `Geometry` 30, `Commands` 28, the top folder
28, `Cli` 20, `Diagnostics` 9, `ReplayDeterminism` 8, `Queries` 3.

## Findings of the previous review

Each line gives the state at `de35f51` and the evidence. "No change" means that no file that the
finding cites changed after `f9521de`, the commit of the previous review.

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. The backend choice moved into `Engine.Api.Http.Server/ServerEngine.cs` and `3DEngine/DesktopSurface.cs`; each stops when the native library does not load.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. `Engine.Api.Http/Endpoints/JsonParameters.cs:44` is unchanged. Register entry R-0027.
- **E6** — Open. No change. Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. E27, E28 and E30 stay open.
- **E8** — Fixed. TASK-0044, v0.37. TASK-0059 changed the method of its test, and the defect still fails it.
- **E9** — Fixed. TASK-0034, v0.39. No change.
- **E10** — Fixed. TASK-0035, v0.43. E26 stays open.
- **E11** — Open. `Engine.Core/Hosting/ParameterBinder.cs:67-76` is unchanged. Register entry R-0028.
- **E12** — Open. No change. Register entry R-0027.
- **E13** — Open. No change in the cited calls. TASK-0028.
- **E14** — Open. No change. Register entry R-0028.
- **E15** — Fixed. TASK-0044, v0.37. TASK-0058 and TASK-0038 moved and extended the checks.
- **E16** — Open. No change. Register entry R-0028.
- **E17** — Open in ten parts of thirteen. No change. Register entry R-0028.
- **E18** — Open. `Engine.Core/CommandBus.cs` still promises a reset in the comment of the commit. Register entry R-0033.
- **E19** — Open. No change. Register entry R-0033.
- **E20** — Fixed. TASK-0051, v0.48. TASK-0058 gave its two limits in the comment (E42, E43).
- **E21** — Open. No change. Register entry R-0033.
- **E22** — Open. No change. Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. TASK-0058 closed E40 and E41.
- **E24** — Open in seven parts of nine. No change. Register entry R-0033.
- **E25** — Open. `Engine.Core/CommandBus.cs` still registers the bus of a replay. Register entry R-0036.
- **E26** — Open. `Engine.Contracts/Document.cs` still returns the list itself. Register entry R-0036.
- **E27** — Open. No change. Register entry R-0036.
- **E28** — Open. No change. Register entry R-0036.
- **E29** — Open. No change. Register entry R-0036.
- **E30** — Open. No change. Register entry R-0036.
- **E31** — Open. Each cited comment is still present. Register entry R-0036.
- **E32** — Open. No change. Register entry R-0036.
- **E33** — Open. No change. Register entry R-0036.
- **E34** — Open. No change. Register entry R-0036.
- **E35** — Open. No change. Register entry R-0036.
- **E36** — Open in part. `Engine.Api.Http.Server/Program.cs:14` and `3DEngine/Documentation/Running.md` now give the exit codes; no document under `docs/` gives them, and no test runs a host to see exit code 3. Register entry R-0036.
- **E37** — Open. No change. Register entry R-0036.
- **E38** — Open. No change. Register entry R-0036.
- **E39** — Open, and worse. The backend choice now exists three times: `Engine.Cli/Cli.cs:212`, `Engine.Api.Http.Server/ServerEngine.cs:32` and `3DEngine/DesktopSurface.cs:65`. Register entry R-0036.
- **E40** — Fixed. The start check reads each address with `BindingAddress.Parse`, the parser of Kestrel, and the two forms with a user part failed the test before the correction. TASK-0058, v0.56.
- **E41** — Fixed. The endpoint configuration does not reload, and a process test that writes `appsettings.json` after the start failed before the correction. TASK-0058, v0.56.
- **E42** — Fixed as recommended. The comment in `Engine.Core/DocumentSession.cs` gives the limit and `ExecutionContext.SuppressFlow`. TASK-0058, v0.56.
- **E43** — Fixed as recommended. The same comment gives the limit and the rule for a sink; no code holds the rule. TASK-0058, v0.56.
- **E44** — Fixed. The bus refuses a created handle that it created before or that the same command consumes, and the three cases failed before the correction. TASK-0058, v0.56.
- **E45** — Fixed. The three texts agree with the code. TASK-0058, v0.56.
- **E46** — Open. TASK-0055 (P0.22).
- **E47** — Fixed. The glossary term "live body" says that the order is not history. TASK-0058, v0.56.
- **V1** — Open. No file of `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0022.
- **V2** — Open. No file of `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0022.
- **V3** — Open. No file of `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0022.
- **V4** — Open in each of its five parts. `3DEngine/` changed in TASK-0054, and `3DEngine/DesktopEngineHost.cs:62-68` still writes a window failure to standard output and returns 0. The desktop still draws the fixed sample scene. A review agent read each part. Register entry R-0027.
- **V5** — Open. No file of `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0030.
- **V6** — Open. No file of `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0030.
- **V7** — Open in each part. The frame loop still uses `DateTime.UtcNow`, and no source calls `SDL_DestroyWindow` or `SDL_Quit`. A review agent searched. Register entry R-0030.
- **T1** — Open in one part of three. No gate compares the native output of two platforms. R-0023.
- **T2** — Open in one part of five. No change. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. TASK-0057 kept the rules in the judge.
- **T4** — Open in three parts of four, and wider. The dispatch gate still reads its host projects by name (`Engine.Tests/Hosting/DispatchSurfaceGateTests.cs:20`), and since TASK-0054 it misses the desktop host `3DEngine`, which owns a session: a review agent put `"CreateBox"` into a file of `3DEngine/`, and the gate passed. Register entry R-0031.
- **T5** — Open in one part of three. No change. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. The job of T6 no longer exists (TASK-0057).
- **T7** — Open in four parts of five. No change. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. T16 stays open.
- **T10** — Open. No change. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51.
- **T12** — Fixed. The heartbeat test counts frames, the deadlock test starts its clock after the host is up, and the collection "real host" runs alone; injected waits failed the old tests and passed the new ones. TASK-0059, v0.55. Register entry R-0034 is closed.
- **T13** — Open in part. The part about `git diff-tree --cc` is fixed, because TASK-0057 removed `--cc` from the judge. `eng/report-failed-tests.js:54` still reads `UnitTestResult` only. R-0031.
- **T14** — Fixed. TASK-0053, v0.51 and v0.52.
- **T15** — Fixed. TASK-0050, v0.47.
- **T16** — Open. No change. Register entry R-0031.
- **T17** — Open. No change. Register entry R-0031.
- **T18** — Fixed. TASK-0049, v0.46.
- **T19** — Fixed. TASK-0049, v0.46.
- **T20** — Fixed. TASK-0049, v0.46.
- **T21** — Fixed. TASK-0049, v0.46.
- **T22** — Fixed. TASK-0049, v0.46.
- **T23** — Open. No change. Register entry R-0031.
- **T24** — Fixed. The remaining part, the statement about `--cc`, went with `--cc` (TASK-0057).
- **T25** — Open in part. With no git the judge now fails (`eng/write-set-check/Program.cs:20-23`), so that part is fixed. The check before a commit still reads the index and HEAD, and a change that is not staged gives a misleading message. R-0031.
- **T26** — Fixed. The contract rule is a rule of the judge for each commit, and the job "Contract-touched-needs-ADR" is gone; a test of a scratch repository failed before the correction. TASK-0057, v0.54.
- **T27** — Fixed. The process test asserts that no address was bound, and a probe that bound before an injected refusal failed it. TASK-0058, v0.56.
- **T28** — Fixed in part. The job fails when the base holds no judge or the previous tip of `main` is not known. The workflow still comes from the judged commit, and a protection against a force push does not change that (T38). R-0031.
- **T29** — Fixed. The judge runs git by a full path from `PATH` outside the checkout, with `-C`. TASK-0057, v0.54.
- **T30** — Fixed. A commit with no parent fails, and a merge is judged against its first parent; two tests of a scratch repository failed before the correction. TASK-0057, v0.54.
- **T31** — Open again. The judge refuses four texts in a changed file under `Engine.Tests/` (option A, TASK-0057), and a review agent passed it with six forms: an escaped name of the attribute, `AppDomain.SetData`, `Environment.CurrentDirectory`, a call on two lines, a helper outside `Engine.Tests/`, and a symbolic link. The escaped attribute and `AppDomain.SetData` ran in a test process and moved `AppContext.BaseDirectory`. A module initializer in a referenced project also runs in the test process. Question Q1. R-0031.
- **T32** — Fixed in part. A branch is judged from its merge base with `main`. On `main` the job judges the commits of the push only (`.github/workflows/ci.yml:194`), so a green later push still hides a red earlier push there. R-0031.
- **T33** — Fixed. The judge reads the task files at the commit. TASK-0057, v0.54.
- **T34** — Fixed. The documented check joins its two commands with `&&`. TASK-0057, v0.54.
- **T35** — Fixed. A failed git command is a failure, the judge reads the paths with `-z`, `WriteSetRangeTests` covers the steps, a task file with no front matter at the commit fails, and `docs/templates.md` describes the new steps. TASK-0057, v0.54.
- **C1** — Open in two parts. No change. Register entry R-0027.
- **P1** — Open. Nothing is measured. The set of created handles of TASK-0058 is one more cost that grows with the scene. Register entry R-0027.

## 1 Current architecture

**[Observed]** The topology of ADR-0019 exists. `Engine.Api.Http` is the surface library, with the
public entry point `EngineSurface` and the public `EngineHost`; it references `Engine.Core` and
`Engine.Contracts` only. Two hosts mount it on a session that each one owns: the program
`engine-api-http` (`Engine.Api.Http.Server`) and the desktop host `3DEngine`, on 127.0.0.1, port 5180 by
default. The dependency gate has a class "surface library", and each client may reference it.

**[Observed]** The judge of the write set runs git by a full path, judges a merge against its first
parent, refuses four texts in a test file, and holds the contract rule for each commit. The job of the
contract gate is gone.

**[Observed]** The desktop still draws the fixed sample scene; no event of the session reaches the
render state yet. Track R does that.

## 2 Patterns in use

- **Each test fails first, or an injection shows that it can fail.** Each task of this period records
  it. **[Observed]**
- **A fix of a gate that the review then breaks.** T31, T36 and E48 are each a path around a fix of the
  last review. Each fix closed the form that the review named, and a nearby form stayed open.
  **[Observed]**
- **Tests that start the real program.** The address guard and the judge are each tested on a real
  process or a real repository. **[Observed]**

## 3 Review of the code

The earlier findings keep their headings and their evidence in the earlier reviews. The section
"Findings of the previous review" gives the state of each one. This section gives the new findings.

### The hosts and the engine

#### E48 · Medium · Seven forms with brackets pass the address check before the start · Confirmed by a run

- **Evidence.** `Engine.Api.Http/EngineSurface.cs:190-192` removes the brackets before
  `IPAddress.TryParse`. Kestrel parses the host with its brackets, and when that fails it binds each
  interface. A review agent called the check and the parser of Kestrel through reflection, with no
  bind: `http://[127.0.0.1]:5000`, `http://[[::1]]:5000`, `http://[::1:5000` and four other forms gave
  "accept" from the check and `[::]:5000` from Kestrel. **[Observed]** in a run of a review agent.
- **Impact.** A value in `--urls`, a variable or `Kestrel:Endpoints` binds each interface until the check
  after the start stops the host. A `Kestrel:Endpoints` value also overrides the port of the desktop.
  The comment of the process test of T27 is false for these forms. **[Inferred]**
- **Correction.** **[Recommended]** Accept brackets only as one pair around an IPv6 address, and add
  the seven forms to the tests.
- **Owner.** TASK-0062.

#### E49 · Medium · A subscriber holds the stop of a host for 30 s · Confirmed by a run

`3DEngine/DesktopSurface.cs:88` stops the host, and the stop waits for the open `/events` WebSocket
until the time limit of the host. A review agent measured 31.0 s with one subscriber. The desktop
process stays alive for that time after the person closes the window, and `engine-api-http` waits the
same time at Ctrl+C. **[Observed]** in a run of a review agent. **[Recommended]** End each subscriber
when the host stops. Owner: TASK-0062.

#### E50 · Medium · Ctrl+C stops the surface of the desktop and leaves the window · Confirmed in part

The desktop starts the surface with the console lifetime of the framework
(`3DEngine/DesktopSurface.cs:73`), which turns Ctrl+C into a stop of the surface; nothing ends the loop
of the window (`3DEngine/DesktopEngineHost.cs`). A review agent saw the lifetime type and did not send
the signal. **[Observed]** for the type; **[Inferred]** for the effect. **[Recommended]** End the loop
when the host stops. Owner: TASK-0062.

#### E51 · Low · A port in use ends the desktop with an exception and no documented code · Confirmed by a run

Nothing catches the exception of the start (`3DEngine/DesktopSurface.cs:73`), so a second desktop on
port 5180 ends with the code of a crash, and the session is not disposed. A review agent ran it.
**[Observed]** Owner: TASK-0062.

#### E52 · Low · The backend of the desktop is disposed only when a request came · Confirmed by a run

`AddEngineSurface` registers a function, so the container owns the session only after a service asks
for it; `3DEngine/DesktopSurface.cs:68-69` makes the session first, and its `Dispose` (`:86-90`) does not
dispose it. A review agent printed "no request: backend disposed = False". **[Observed]** Owner:
TASK-0062.

#### E53 · Low · Two clauses of the check of created handles have no test, and one cannot fire · Confirmed by a run

`Engine.Core/CommandBus.cs:180`: a consumed handle is live, so it is in the set of created handles,
and the clause `consumed.Contains` never decides. No test gives a handle two times in the created list.
A review agent removed both clauses, and each test passed. **[Observed]** Owner: TASK-0062.

#### E54 · Low · The setting `port` of the desktop reads the variable PORT · Confirmed by a run

`3DEngine/DesktopSurface.cs:59` reads the key `port`, which a variable `PORT` also gives; tools often
set it. A review agent printed `configuration["port"] = 4321` with `PORT=4321`. **[Observed]**
**[Recommended]** A prefixed key. Owner: TASK-0062.

#### E55 · Low · The refusal messages name `engine-api-http` in the desktop · Confirmed by reading

`Engine.Api.Http/EngineSurface.cs` says "engine-api-http binds to a loopback address only" and advises
`--urls`, which the desktop does not use. **[Observed]** Owner: TASK-0062.

#### E56 · Low · `EngineHost.Backend` is a public path past the bus · Confirmed by reading

`Engine.Api.Http/EngineHost.cs:57`. A host can call the backend outside the bus, against ADR-0019
item 2, and no gate holds it. **[Observed]** **[Recommended]** Make it internal. Owner: TASK-0062.

#### E57 · Low · Texts that the topology made false · Confirmed by reading

`docs/adr/0019-hybrid-deployment-topology.md:85-87` says that TASK-0038 mounts the surface in the
desktop, and names a section "Deployment topology" of `CLAUDE.md` that does not exist. The comment
of `Engine.Tests/Http/DesktopSurfaceTests.cs` says that TASK-0054 is to come. No test calls
`DesktopSurface`. **[Observed]** Owner: TASK-0062.

### Tests, gates and the pipeline

#### T36 · High · A merge can rewrite a file that its second parent changed, and the judge does not see it · Confirmed by a run

- **Evidence.** `eng/write-set-check/Judge.cs:201-215` judges a merge on the files that differ from its
  first parent, minus the files that the side of its second parent changed. A review agent made a
  merge of `main` into a branch that wrote another content into a file of `main` that the task of the
  branch forbids: the new judge passed it on the branch and after the merge on `main`, and the old
  judge of `f9521de`, which read a merge with `--cc`, failed it. In a second run a merge changed again a
  test file and a contract file that its second parent changed: the new judge passed it, with no check
  of the four texts and of the contract rule. **[Observed]** in runs of a review agent.
- **Impact.** A lost rule. A conflict resolution, or a deliberate edit inside a merge, carries a change
  outside each write set onto `main`. `docs/templates.md` says that the judge fails each contract
  change with no ADR, which is false for a merge. **[Inferred]**
- **Correction.** **[Recommended]** Judge a merge also on each file that differs from both parents,
  the set of `--cc`, so that a file that equals neither parent needs a task.
- **Owner.** TASK-0061.

#### T37 · Medium · The tests of the judge do not hold each rule · Confirmed by an injection

`Engine.Tests/Governance/WriteSetRangeTests.cs:36-54` passes on the refusal of the commit with no
parent alone: a review agent removed the refusal of a merge of unrelated history, and each of the ten
tests passed. No test covers T36, `--staged` or the steps of the workflow. **[Observed]** Owner:
TASK-0061.

#### T38 · Medium · Two texts say that a branch protection closes the hole of the workflow · Confirmed by reading

`docs/register.md:263-264` and the Method of TASK-0057 say that a branch protection on GitHub closes the
case of a task that changes `.github/workflows/ci.yml`. A protection against a force push does not: a
commit that edits the workflow changes the job of the same run. **[Observed]** The register line is my
own text. This review corrects it with a new progress line. Question Q2 gives the options.

#### T39 · Low · The fallback for exit code 2 is dead code that weakens the job · Confirmed by reading

`.github/workflows/ci.yml:198-202`. Since TASK-0057 is on `main`, the judge of `main` answers 2 only
for unknown arguments, and the workflow gives fixed arguments. If anything reaches the fallback, a
branch is judged on the commits of the push only (T32). **[Observed]** **[Recommended]** Remove it.
Owner: TASK-0061.

#### T40 · Low · Other gaps of the judge · Confirmed, except where the line says so

`Judge.FindGit` compares paths as text, so a junction outside the checkout that points into it passes;
a review agent ran it. A failed `git show` of the task file before the commit skips the rule for a task
that is Done, and a failed `git show` of a test file skips the four texts (reading only). **[Observed]**
Owner: TASK-0061.

## 4 The next planned task

**[Observed]** The roadmap gives P0.22 (TASK-0055), which the first objective does not need, and then
R2 (TASK-0028). This review puts TASK-0061 and TASK-0062 before them as P0.23 and P0.24: T36 is a lost
rule of the judge, and E48 is a third gap of the address check of each host.

## 5 Research for that task

**[Observed]** T36: `git diff-tree --cc --name-only <merge>` lists each file that differs from each
parent. The old judge used it, and it found the edit of a merge; it did not find a merge that takes the
tree of one parent, which the current rule finds. The two sets together cover both cases.

**[Observed]** T31: the tests of `Engine.Tests` run with the build of the judged commit, and each
referenced project can run code at load. Only a check that does not run that build can close the class:
option B of Q1 of the review of 2026-10-08, each repository gate in a program that the pipeline builds
from `main`.

## 6 Recommended approach

1. **[Recommended]** TASK-0061: T36 first, then T37, T39 and T40, and the answer of the owner to Q1.
2. TASK-0062: E48 first, then E49 to E57.
3. Then R2 (TASK-0028).

## Questions for the owner

**Q1. Must the repository gates move out of the test assembly (T31)?** On 2026-10-08 the owner chose
option A, the refusal of four texts, and kept option B for a fifth form. A review agent found six forms,
and two of them ran in a test process. Option B: each gate that reads the repository becomes a part of
the judge program, which the pipeline builds from `main`; `Engine.Tests` keeps the tests of the engine.
Recommendation: option B, in a task of its own after TASK-0061, because it moves 17 gate classes.

**Q2. How must the hole of the workflow file close (T28, T38)?** A commit that names
`.github/workflows/ci.yml` exactly can change the job that judges it. Option A: accept the condition,
with the reason that the exact permit and the diff show each such change. Option B: a required review of
`.github/workflows/**` on GitHub (a CODEOWNERS file with a required review), which needs pull requests
and changes how the owner merges. Recommendation: option A, and an entry under "Accepted compromises".
