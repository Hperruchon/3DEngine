---
date: 2026-10-09
commit: f9521de
ledger: v0.52
previous: 2026-10-08-codebase-review.md
---

# Codebase review — 2026-10-09

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the fifth codebase review. The next review must come before ledger entry v0.59: the build
fails when a seventh milestone follows v0.52. `Engine.Tests/Governance/CodebaseReviewGateTests.cs`
holds that rule, and `docs/templates.md`, section 7, gives the form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is
the authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this
file does not.

| Item | Value |
|---|---|
| Code state | `main` at `f9521de`, the merge of v0.52 |
| Branch of this review | `codebase-review-2026-10-09` |
| Tests | 302 on this computer |
| Pipeline | Run 37964903188 on `main`: green on Ubuntu, Windows and macOS, with the judge built from the tip before the push |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner
decides.

**Method.** Only four areas changed after `9ad025b`, the commit of the last review: the session and the
address guard (TASK-0051), operand consumption (TASK-0037), and the judge of the write set with the
pipeline (TASK-0053). Two review agents read them in parallel, each one in its own git worktree: the
engine and the host, and the judge with the pipeline. The agents ran probes and removed each one. I
read each line that this review cites, on disk, and I repeated two runs: a root commit lists no file
for `git diff-tree` without `--root` (T30), and `System.Uri` gives the host `localhost` for
`http://evil@localhost:5000` (E40). A line that says "a review agent ran it" means that I read the
cited code and did not repeat the run.

## Summary

- **Four earlier findings are fixed.** E20 and E23 (TASK-0051), T14 and T11 (TASK-0053). T15 and the
  notices part of T24 were corrected in the last review. **[Observed]**
- **The new judge of the write set has three high holes, and two of them are older than it.** A
  program named `git` in the judged checkout replaces git for the judge (T29). A root commit, a merge
  of unrelated history, or a merge that takes the tree of an old commit passes any change, because
  the judge reads a merge with `git diff-tree --cc` (T30). The attack class of T14 is still open for
  each other gate in `Engine.Tests` (T31). Each one needs a deliberate act, as T14 did, except an
  accidental merge of an old branch. **[Observed]** in runs of a review agent.
- **The address guard of TASK-0051 has two medium gaps.** An address with a user part passes the start
  check, and Kestrel binds each interface until the check after the start stops the host (E40). A
  change to `appsettings.json` after the start binds new endpoints with no check (E41). **[Observed]**
  in runs of a review agent.
- **The contract gate checks a range and not a commit (T26).** One ADR change anywhere in a push lets
  a contract change with no ADR pass, and the first push of a branch always holds one. **[Observed]**
- **The register stays at 14 of 15.** Two new tasks own the new findings: TASK-0057 for the judge and
  the pipeline, and TASK-0058 for the guards of TASK-0051 and TASK-0037.
- **Sixty-eight findings are open: no critical, nine high, twenty-nine medium and thirty low.**
  Eighteen findings are new. **[Observed]**
- **R-0034 reaches its limit on 2026-11-03.** The heartbeat test failed again on Windows (T12).

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A
source line is a line of a `.cs` or `.razor` file that git tracks. The script of this review gives
5,361 production lines in 99 files at `9ad025b`, which are the values of the last review.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 9 | `Project(` lines in `3DEngine.sln`. The last review gave 9. Git tracks 11 project files: the judge and the native package are not in the solution. |
| Production source lines | 6,093 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, 102 files. The last review gave 5,361 in 99 files. |
| Test source lines | 7,539 | `Engine.Tests`, 62 files. The last review gave 7,338 in 59 files. |
| Tests | 302 | `dotnet test 3DEngine.sln --no-build`: 302 passed, 0 failed, 0 skipped. The last review gave 260. |
| Gate classes | 17 | Test classes whose name ends in `GateTests`. The last review gave 17. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 14 | Section "Open" of `docs/register.md`. This review adds none. The last review gave 14. |
| ADRs | 21 | `docs/adr/0001` to `0021`. In force: 19 (12 accepted, 7 amended). |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 9 | The same method: V1, V2, V3, V5, T1, T4, T29, T30, T31. The last review gave 7. |
| Findings: medium | 29 | The same method: E5, E6, E11, E12, E13, E14, E16, E18, E19, E21, E22, E25, E27, E28, E33, E40, E41, V4, V6, T2, T5, T10, T12, T16, T17, T26, T28, T32, T33. The last review gave 27. |
| Findings: low | 30 | The same method: E17, E24, E26, E29 to E32, E34 to E39, E42 to E47, V7, T7, T13, T23, T24, T25, T27, T34, T35, C1, P1. The last review gave 21. |

Two more measures. The lines of each production folder: `Engine.Core` 1,617, `Engine.Api.Http` 1,308,
`3DEngine.Vulkan` 1,080, `eng` 591, `Engine.Cli` 424, `Engine.Contracts` 415, `Engine.Geometry.Manifold`
297, `3DEngine.Core` 221, `3DEngine` 140. The tests of each folder of `Engine.Tests`: `Governance` 79,
`Http` 69, `Hosting` 31, `Geometry` 30, the top folder 28, `Commands` 25, `Cli` 20, `Diagnostics` 9,
`ReplayDeterminism` 8, `Queries` 3.

## Findings of the previous review

Each line gives the state at `f9521de` and the evidence. "No change" means that no file that the
finding cites changed after `9ad025b`, the commit of the previous review.

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. `Engine.Api.Http/Endpoints/JsonParameters.cs:44` is unchanged. Register entry R-0027.
- **E6** — Open. No change. Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. E27, E28 and E30 give its gaps, and they stay open.
- **E8** — Fixed. TASK-0044, v0.37. No change.
- **E9** — Fixed. TASK-0034, v0.39. No change.
- **E10** — Fixed. TASK-0035, v0.43. E26 stays open.
- **E11** — Open. `Engine.Core/Hosting/ParameterBinder.cs:67-76` is unchanged. Register entry R-0028.
- **E12** — Open. No change. Register entry R-0027.
- **E13** — Open. `QueriesEndpoint.cs:84` and `Engine.Cli/Cli.cs:167` still ask for `Aabb`. TASK-0028.
- **E14** — Open. No change. Register entry R-0028.
- **E15** — Fixed. TASK-0044, v0.37. TASK-0051 extended the address check (E23).
- **E16** — Open. No change. Register entry R-0028.
- **E17** — Open in ten parts of thirteen. No change. Register entry R-0028.
- **E18** — Open. `Engine.Core/CommandBus.cs:94-96` still promises a reset. Register entry R-0033.
- **E19** — Open. No change. Register entry R-0033.
- **E20** — Fixed. `Engine.Core/DocumentSession.cs` refuses a call from the flow that holds the
  section, and `DocumentSessionReentryTests` failed before the correction. TASK-0051, v0.48.
- **E21** — Open. TASK-0037 added a second form: the bus throws for a wrong consumed list after the
  handler called the backend. Register entry R-0033.
- **E22** — Open. No change. Register entry R-0033.
- **E23** — Fixed. `Engine.Api.Http/Program.cs` checks four configuration keys before the start and the
  bound addresses after it, and the process test with a `Kestrel:Endpoints` address failed before the
  correction. TASK-0051, v0.48.
- **E24** — Open in seven parts of nine. No change. Register entry R-0033.
- **E25** — Open. `Engine.Core/CommandBus.cs:72` still registers the bus of a replay. Register entry R-0036.
- **E26** — Open. `Engine.Contracts/Document.cs:21` still returns the list itself. Register entry R-0036.
- **E27** — Open. `Engine.Core/Replay.cs:30-41` has no cleanup. Register entry R-0036.
- **E28** — Open. `Replay.cs:37` has no catch. Register entry R-0036.
- **E29** — Open. No change. Register entry R-0036.
- **E30** — Open. `Replay.cs:32` still defaults to the null backend. Register entry R-0036.
- **E31** — Open. Each cited comment is still present (`CommandBus.cs:94-96`,
  `ManifoldGeometryBackend.cs:84-86`, `InProcessMeshBackend.cs:19-22`, `Replay.cs:16-18`). R-0036.
- **E32** — Open. No change. Register entry R-0036.
- **E33** — Open. No change. Register entry R-0036.
- **E34** — Open. No change. Register entry R-0036.
- **E35** — Open. No change. Register entry R-0036.
- **E36** — Open. TASK-0051 adds a second path to exit code 1, the check of the bound addresses; the
  codes still have no document. Register entry R-0036.
- **E37** — Open. No change. Register entry R-0036.
- **E38** — Open. No change. Register entry R-0036.
- **E39** — Open. No change. Register entry R-0036.
- **V1** — Open. No file of `3DEngine/`, `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0022.
- **V2** — Open. No file of `3DEngine/`, `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0022.
- **V3** — Open. No file of `3DEngine/`, `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0022.
- **V4** — Open. No file of `3DEngine/`, `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0027.
- **V5** — Open. No file of `3DEngine/`, `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0030.
- **V6** — Open. No file of `3DEngine/`, `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0030.
- **V7** — Open. No file of `3DEngine/`, `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0030.
- **T1** — Open in one part of three. No gate compares the native output of two platforms. R-0023.
- **T2** — Open in one part of five. No change. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. The rules moved into the judge with no change (TASK-0053).
- **T4** — Open in three parts of four. No gate file of the three parts changed. Register entry R-0031.
- **T5** — Open in one part of three. No change. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Open in four parts of five. No change. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. T16 stays open.
- **T10** — Open. No change. Register entry R-0031.
- **T11** — Fixed. The judge is built from `main`, so the rules no longer come from the tip of the
  judged branch. TASK-0053, v0.51. T26 gives the workflow, which still comes from the judged commit.
- **T12** — Open, and seen again. The heartbeat test failed on Windows in run 37963719832: heartbeat 3
  came 0 ms after heartbeat 2. A local run of the full suite also aborted once during TASK-0051, with
  no known cause. Register entry R-0034, limit 2026-11-03.
- **T13** — Open in three parts of four. The false statement about `git diff-tree --cc` moved with the
  steps into `eng/write-set-check/Program.cs:28-31` and the text at `:65`. R-0031 and R-0034.
- **T14** — Fixed. The judge runs from a build of `main`. Run 37963719832, on a branch that was never
  merged, failed the injection of the review and a judge that passed each change. TASK-0053, v0.51 and v0.52.
- **T15** — Fixed. `tasks/TASK-0038-hybrid-topology.md:18` names `Engine.Tests/Engine.Tests.csproj`.
  TASK-0050, v0.47.
- **T16** — Open. No change. Register entry R-0031.
- **T17** — Open. No change. Register entry R-0031.
- **T18** — Fixed. TASK-0049, v0.46. On `main`, run 37697577184 pushed nothing for an existing version.
- **T19** — Fixed. TASK-0049, v0.46. No change.
- **T20** — Fixed. TASK-0049, v0.46. No change.
- **T21** — Fixed. TASK-0049, v0.46. No change.
- **T22** — Fixed. TASK-0049, v0.46. No change.
- **T23** — Open. No change. Register entry R-0031.
- **T24** — Fixed in one part of two: TASK-0050 corrected the package name in the notices. The other
  part is the statement about `--cc`, which T13 holds.
- **T25** — Open. The judge keeps the rule: with no git, `Git` returns null and the rule for a `Done`
  task does not apply (`eng/write-set-check/Judge.cs`, `AtTheCommit`). Register entry R-0031.
- **C1** — Open in two parts. No change. Register entry R-0027.
- **P1** — Open. Nothing is measured. Register entry R-0027.

## 1 Current architecture

**[Observed]** The projects and their references are those of the last review, and one project is new:
`eng/write-set-check` (`WriteSetCheck`), the judge of the write set. It references no project, and
`Engine.Tests` references it for the reader of the task files. It is not in `3DEngine.sln`.

**[Observed]** Three changes in behaviour. A call into a document session from inside its own section
throws at once (TASK-0051). The HTTP host checks four configuration keys before the start and the bound
addresses after it (TASK-0051). Each operation consumes its operands, and the Document holds the live
bodies only (TASK-0037, ADR-0021).

**[Observed]** The pipeline builds the judge from `main`, or from the tip before the push for a push to
`main`, and runs it in the judged checkout (TASK-0053).

## 2 Patterns in use

- **Each test fails first.** TASK-0051 and TASK-0037 each recorded the failures of the new tests before
  the change. **[Observed]**
- **A proof on a branch that is never merged.** TASK-0053 proved the judge in run 37963719832 and
  deleted the branch. **[Observed]**
- **A guard is proved against the attack that it names, and not against the next one.** E40, E41, T29
  and T30 are each a second path around a guard of this period. **[Observed]**

## 3 Review of the code

The earlier findings keep their headings and their evidence in the earlier reviews. The section
"Findings of the previous review" gives the state of each one. This section gives the new findings.

### The engine and the hosts

#### E40 · Medium · An address with a user part passes the start check, and Kestrel binds each interface · Confirmed by a run

- **Evidence.** `LoopbackRefusal` reads the host with `System.Uri` (`Engine.Api.Http/Program.cs:124`),
  which gives `localhost` for `http://evil@localhost:5000`; I repeated that part. Kestrel parses the
  same text with its own parser and takes `evil@localhost` as a host name, which binds each interface.
  A review agent called that parser and printed `http://[::]:5000`. **[Observed]**
- **Impact.** The host listens on each interface from `StartAsync` (`Engine.Api.Http/Program.cs:103`) until the check
  after the start stops it. The comment at `Engine.Api.Http/Program.cs:19` says that the host never binds a foreign
  address from a known source, which is false for this form. **[Inferred]** for the time window.
- **Correction.** **[Recommended]** Refuse a URL with a user part, or read the address with the parser
  of Kestrel, and add the two forms to the tests.
- **Owner.** TASK-0058.

#### E41 · Medium · A change to `appsettings.json` after the start binds new endpoints with no check · Confirmed by a run

- **Evidence.** Kestrel reloads `Kestrel:Endpoints` when the file changes, and the check after the start
  (`Engine.Api.Http/Program.cs:105-106`) runs one time. A review agent started the host on 127.0.0.1, then wrote the
  file: the host logged "Config changed. Starting the following endpoints", and 127.0.0.2:5891 opened.
  **[Observed]** in a run of a review agent.
- **Impact.** A value of `0.0.0.0` in that file binds each interface, with no refusal. **[Inferred]**
- **Correction.** **[Recommended]** Turn off the reload of the endpoint configuration, and add a test
  that writes the file after the start.
- **Owner.** TASK-0058.

#### E42 · Low · The refusal of a call back depends on timing for a task that a sink does not wait for · Confirmed by a run

A task that a sink starts inherits the token (`Engine.Core/DocumentSession.cs:116-131`). A review agent
started 50 such tasks: 2 were refused with no delay, and 50 with a delay of 50 ms. Such a task cannot
wait for itself, so each refusal is a false positive. **[Observed]** in a run of a review agent.
**[Recommended]** Give the rule and `ExecutionContext.SuppressFlow` in the comment. Owner: TASK-0058.

#### E43 · Low · The check of a call back misses a call from a flow that has no execution context · Confirmed by a run

Inside `ExecutionContext.SuppressFlow`, a sink that waited for `Task.Run(() => session.Read(...))` waited
until its limit, with no refusal. A dedicated thread that a sink waits for has the same form.
**[Observed]** in a run of a review agent. **[Recommended]** State the limit, and tell a sink not to
wait for work on another thread. Owner: TASK-0058.

#### E44 · Low · A command that creates and consumes the same handle reports a body that does not exist · Confirmed by a run

`RefuseWrongConsumedList` (`Engine.Core/CommandBus.cs:157`) does not refuse a consumed handle that is
also created, or a created handle that is already live. The commit adds, then removes, the body
(`CommandBus.cs:195`, `:206`). A review agent applied `Translate` with the `CommandId` of its own
operand on a backend that does not check handles: `Applied`, and no live body. Only the native backend
stops it. `Engine.Contracts/Document.cs:27` says that a consumed body never becomes live again.
**[Observed]** in a run of a review agent. **[Recommended]** Refuse the overlap and a live created
handle in the bus. Owner: TASK-0058.

#### E45 · Low · Three texts claim more than the code gives · Confirmed by reading

`Engine.Api.Http/Program.cs:19` (see E40 and E41), `Document.cs:27` (see E44), and the refusal message at
`Engine.Api.Http/Program.cs:153`, which names `ASPNETCORE_<KEY>` only. **[Observed]** Owner: TASK-0058.

#### E46 · Low · `/schema/events` gives no payload fields, against ADR-0008 §9 · Confirmed by reading

ADR-0008 §9 gives "event kinds + payload schemas", and each entry holds the kind only. R-0037 closed as
decided on 2026-10-08. **[Observed]** Owner: TASK-0055.

#### E47 · Low · `Document.Bodies` is not in the order of creation after a removal · Confirmed by reading

The collection is the values of a dictionary (`Engine.Contracts/Document.cs`), and a new body can take
the slot of a removed one. The order is the same for the same commands, so a replay agrees. A client
must not read the order as history, and no text says so. **[Observed]** TASK-0037 recorded it. Owner:
TASK-0058.

### Tests, gates and the pipeline

#### T26 · Medium · The contract gate checks a range of commits and not each commit · Confirmed by a run

`.github/workflows/ci.yml:159` takes the range from the previous tip, or from the cut-off for a new
branch, and `:169` passes when any ADR changed in the range. Run 37963719832 passed a commit that changed
`Engine.Contracts/Document.cs` with no ADR, on the first push of a branch. A review agent showed the same
for two commits of one push. The progress line of 2026-10-09 on R-0031 says that a later push is not
affected, which is false; this review corrects it. **[Observed]** **[Recommended]** Check each commit,
as the judge does. Owner: TASK-0057.

#### T27 · Low · The process test for `ASPNETCORE_HTTP_PORTS` cannot see a loss of the start check · Confirmed by reading

`Engine.Tests/Http/HostGuardTests.cs:126`: without the start check, the check after the start still
gives exit code 1 and "loopback", after a bind on each interface. The unit test of the configuration
covers the start check. **[Observed]** **[Recommended]** Assert that no "Now listening on" line came.
Owner: TASK-0058.

#### T28 · Medium · The judge can come from the judged commit · Confirmed by reading

When the base holds no judge, the job builds the judge of the judged checkout and gives a notice only
(`ci.yml:221`). After a force push to `main`, the old tip is not known, and `origin/main` is the pushed
tip itself (`ci.yml:213-215`). The workflow file also comes from the judged commit, which TASK-0053
recorded. **[Observed]** for the code; **[Inferred]** for the force push. **[Recommended]** Fail when the
base holds no judge or the previous tip of `main` is not known; a branch protection on GitHub stops a
force push. Owner: TASK-0057.

#### T29 · High · A program named `git` in the judged checkout replaces git for the judge · Confirmed by a run

The judge starts `git` by its name (`eng/write-set-check/Program.cs:104`), with the judged checkout as
the current folder (`eng/write-set-check/Program.cs:16`, `ci.yml:235`). Windows and .NET on Linux search the current folder
before `PATH`. A review agent put a program named `git.exe` in the root on Windows: the judge printed
only the range and gave exit code 0 for a forbidden change. **[Observed]** in a run of a review agent on
Windows; **[Inferred]** for Linux, from the source of .NET. **[Recommended]** Run git by its full path
from outside the checkout, with `git -C`, and add a test. Owner: TASK-0057.

#### T30 · High · A root commit and a merge can carry any change past the judge · Confirmed by a run

`eng/write-set-check/Program.cs:71` runs `git diff-tree` without `--root`, so a root commit lists no file; I repeated that
part on the root commit of this repository (0 files, and 1 with `--root`). `eng/write-set-check/Program.cs:62` reads a merge
with `--cc`, which hides each file that equals one parent. A review agent passed an orphan commit with a
forbidden change and a merge of it, and a merge that took the tree of `9ad025b` and removed 43 files,
the judge among them. Each one printed "pass". The old workflow had the same steps. **[Observed]**
**[Recommended]** Refuse a commit with no parent after the cut-off, and judge a merge against its first
parent. Owner: TASK-0057.

#### T31 · High · The attack class of T14 is open for each other gate in `Engine.Tests` · Confirmed by a run

The gates find the root of the repository from `AppContext.BaseDirectory`
(`Engine.Tests/Governance/RepositoryFiles.cs:12`). A review agent added `Math.Sin` to a command, which
the determinism gate refused, then one test file whose module initializer points that value at a clean
copy: the gates passed. A task that permits `Engine.Tests/**` permits that file. **[Observed]** in a run
of a review agent; making the copy in the pipeline is **[Inferred]**. **[Recommended]** The judge from
`main` refuses a module initializer, `AppContext.SetData`, `Environment.SetEnvironmentVariable` and
`Directory.SetCurrentDirectory` under `Engine.Tests/`. Owner: TASK-0057.

#### T32 · Medium · A green later push hides a red earlier push of a branch · Confirmed by a run

The judge reads the commits from the previous tip only (`eng/write-set-check/Program.cs:32-35`). A review agent judged a
range after a failed commit and printed "pass". A push to `main` judges the whole range, so `main` finds
the problem after the merge. **[Observed]** **[Recommended]** On a branch, judge from the merge base with
`main`. Owner: TASK-0057.

#### T33 · Medium · A task file that is renamed fails each earlier commit of its task · Confirmed by a run

The judge finds a task by its file at the tip (`eng/write-set-check/Judge.cs:165`, `:174`), and a new
branch judges again from the cut-off. A review agent renamed the file of TASK-0053, and its four commits
failed. **[Observed]** **[Recommended]** Find the task by its identifier at the commit. Owner: TASK-0057.

#### T34 · Low · The documented check before a commit hides a failure of the judge · Confirmed by a run

`CLAUDE.md:137` and `docs/templates.md:150` give two commands on two lines with `set -o pipefail`, which
does not stop at the first failure. A review agent ran the block with a failing judge: exit code 0.
**[Observed]** **[Recommended]** Use `set -euo pipefail`, or join the two commands with `&&`. Owner:
TASK-0057.

#### T35 · Low · Other defects of the judge · Confirmed, except where the line says so

A failed git command counts as no change (`eng/write-set-check/Program.cs:23`, `:62`, `:71`; reading only). Git quotes a
non-ASCII path, and the judge reads a false name, which fails safe (a run). No test covers the steps of
`eng/write-set-check/Program.cs`. `eng/write-set-check/Judge.cs:225` uses the task file at the tip when the file at the commit has no front
matter. `docs/templates.md:279` says that the job judges each commit after the cut-off and the change
that a merge made itself, which T30 and T32 contradict. **[Observed]** Owner: TASK-0057.

## 4 The next planned task

**[Observed]** In the roadmap, P0.17 shipped (TASK-0053), and the roadmap did not record it; this
review adds the line. The next phase was P0.18, TASK-0038, which permits `Engine.Tests/**` and moves
the address checks into a library. T29 to T31 and E40 to E41 apply to both. This review therefore puts
TASK-0057 and TASK-0058 before it: P0.18 and P0.19, and TASK-0038, TASK-0054 and TASK-0055 move to P0.20
to P0.22.

## 5 Research for that task

**[Observed]** T29: `eng/write-set-check/Program.cs:104` starts `git` by its name. Git for Windows and the runners install
git in a folder of `PATH`. A full path from `where git` or `command -v git`, taken outside the
checkout, and `git -C <root>`, remove the search of the current folder.

**[Observed]** T30: `git diff-tree --root` lists the files of a root commit. `git diff-tree <merge>^1
<merge>` lists each file that the merge changed against its first parent; on `main`, that is the change
that the merge brings in, which the commits of the branch already carried, so the judge must accept a
file that a judged commit of the same push changed.

**[Observed]** T31: the four calls are text in a `.cs` file under `Engine.Tests/`, and the judge reads
the commit, so a text search in the judge is enough for the known forms.

## 6 Recommended approach

1. **[Recommended]** TASK-0057 first: T29, T30 and T28 are holes in the judge that TASK-0053 made the
   single proof of the write set.
2. A task for T12 (R-0034) before 2026-11-03, or an extension with a reason.
3. TASK-0058, then TASK-0038.

## Questions for the owner

**Q1. Will you protect `main` on GitHub (T28)?** A branch protection rule that refuses a force push to
`main` closes one path in T28. It is a setting of the repository, and only the owner can change it.
Recommendation: yes, with no other rule now.

**Q2. How must the judge close T31?** Option A: the judge refuses four calls under `Engine.Tests/`.
Option B: each repository gate moves into a program that the pipeline builds from `main`. Recommendation:
A now, because it is small and closes the known forms; B when a fifth form appears.
