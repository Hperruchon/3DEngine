---
date: 2026-10-08
commit: 9ad025b
ledger: v0.46
previous: 2026-10-04-codebase-review.md
---

# Codebase review — 2026-10-08

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the fourth codebase review. The next review must come before ledger entry v0.53: the build
fails when a seventh milestone follows v0.46. `Engine.Tests/Governance/CodebaseReviewGateTests.cs`
holds that rule, and `docs/templates.md`, section 7, gives the form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is
the authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this
file does not.

| Item | Value |
|---|---|
| Code state | `main` at `9ad025b`, the merge of v0.46 |
| Branch of this review | `codebase-review-2026-10-08` |
| Tests | 260 on this computer |
| Pipeline | Run 37697577307 on `main`: green on Ubuntu, Windows and macOS. The native build, run 37697577184, built and checked the three platforms and pushed nothing, because the version exists. |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner
decides.

**Method.** No file changed in `3DEngine/`, `3DEngine.Vulkan/` or `3DEngine.Core/` after `ae5c521`, the
commit of the last review. Three review agents read the code in parallel, each one in its own git
worktree, so that an injection could not touch the working tree of the session: the engine kernel and
the two hosts at `a9c7283`, and the tests, the gates and the native build at `e66e330`. Between those
commits and the reviewed merge only `eng/manifold-native/` and `.github/workflows/build-manifold-native.yml`
changed, and TASK-0049 corrected five findings of the third agent there before the merge. The agents
ran probes outside the repository, ran the real host, replayed 92 commits through the write-set gate,
and injected violations and removed each one. I read each line that this review cites, on disk, and I
repeated the checks that the summary names. A line that says "a review agent ran it" means that I read
the cited code and did not repeat the run.

## Summary

- **Seven earlier findings are fixed.** E1, E3, E7 and E10 (TASK-0035, TASK-0036), and T3, T8 and T9
  (TASK-0047). The replay rebuilds the version, a host stops when the native library is absent, a hang
  stops after two minutes with a name, and a project file cannot turn a gate off. **[Observed]**
- **The native backend now runs on each platform.** TASK-0036 found that it had never loaded on Linux
  or macOS, and TASK-0048 rebuilt the package. The native tests can no longer skip in the pipeline.
  **[Observed]**
- **A test file can switch off the write-set gate (T14, high).** A module initializer in any file under
  `Engine.Tests/` can clear the variable that the gate reads, so the commit judges itself. Three open
  tasks permit a path under `Engine.Tests/`. **[Observed]** in a run of a review agent.
- **A replayed Document can never get a command bus (E25).** The replay registers its bus for the
  Document, so a load path, the plan of TASK-0019, cannot continue live. **[Observed]** in a run of a
  review agent.
- **The new replay exception has gaps.** A handler exception escapes it raw (E28), a backend keeps the
  bodies after a divergence (E27), and the default backend makes each log with geometry diverge (E30).
  **[Observed]**
- **The register gives the owner a decision soon.** Five entries reach their limit on 2026-12-24.
- **Fifty-five findings are open: no critical, seven high, twenty-seven medium and twenty-one low.**
  Twenty-seven findings are new, and five of them were corrected before the merge (T18 to T22).
  **[Observed]**
- **Two of my own statements were false.** TASK-0036 says that `WebApplicationFactory` stops
  `Program.cs` at its build; a run shows that the start check runs in each test of the factory. The
  notices name a package that `nuget/` no longer holds. This review corrects both. **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. The
values are for the reviewed merge. A source line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 9 | `Project(` lines in `3DEngine.sln`. The last review gave 9. |
| Production source lines | 5,361 | Each project except `Engine.Tests`, 99 files. The last review gave 5,179 in 98 files. |
| Test source lines | 7,338 | `Engine.Tests`, 59 files. The last review gave 6,804 in 56 files. |
| Tests | 260 | `dotnet test 3DEngine.sln --no-build`: 260 passed, 0 failed, 0 skipped. The last review gave 242. |
| Gate classes | 17 | Test classes whose name ends in `GateTests`. The last review gave 17. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 14 | Section "Open" of `docs/register.md`, with the entry that this review adds. The count before the review was 13. |
| ADRs | 21 | `docs/adr/0001` to `0021`. In force: 19. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 1. |
| Findings: high | 7 | The same method: V1, V2, V3, V5, T1, T4, T14. The last review gave 11. |
| Findings: medium | 27 | The same method: E5, E6, E11, E12, E13, E14, E16, E18 to E23, E25, E27, E28, E33, V4, V6, T2, T5, T10, T11, T12, T15, T16, T17. The last review gave 21. |
| Findings: low | 21 | The same method: E17, E24, E26, E29 to E32, E34 to E39, V7, T7, T13, T23, T24, T25, C1, P1. The last review gave 7. |

Two more measures. The lines of each production project: `Engine.Core` 1,528, `Engine.Api.Http` 1,269,
`3DEngine.Vulkan` 1,080, `Engine.Cli` 424, `Engine.Contracts` 398, `Engine.Geometry.Manifold` 297,
`3DEngine.Core` 221, `3DEngine` 140, `eng` 4. The tests of each folder of `Engine.Tests`: `Governance` 60, `Http` 58, `Hosting` 31, `Geometry` 30, the top folder 25, `Cli` 20, `Commands` 16, `Diagnostics` 9, `ReplayDeterminism` 8, `Queries` 3.

## Findings of the previous review

Each line gives the state at the reviewed merge and the evidence.

- **E1** — Fixed. `Engine.Contracts/Document.cs:19` gives `Version => _log.Count`, and
  `ReplayVersionTests` replays a live session with a rejection to the same version. TASK-0035, v0.43.
- **E2** — Fixed. TASK-0034, v0.39. E19 holds the paths around the session.
- **E3** — Fixed. Each host stops with `E-GEOM-BACKEND-INIT` and exit code 3 when the library is absent;
  a review agent ran both programs from a copy with no native library. TASK-0036, v0.44.
- **E4** — Fixed. TASK-0034, v0.39.
- **E5** — Open. `Engine.Api.Http/Endpoints/JsonParameters.cs:44` gives a `double` for each number,
  because the conditional expression has the type `double`. Register entry R-0027.
- **E6** — Open, in each of its five parts. Register entry R-0027.
- **E7** — Fixed. `Engine.Core/Replay.cs:37-39` throws `ReplayDivergenceException` at the first result
  that is not `Applied`. TASK-0035, v0.43. E27, E28 and E30 give its gaps.
- **E8** — Fixed. TASK-0044, v0.37.
- **E9** — Fixed. TASK-0034, v0.39; the test reads the field `seq` since TASK-0035.
- **E10** — Fixed. The version has no setter, and a second bus is refused. TASK-0035, v0.43. E26 gives a
  cast that still moves the version back.
- **E11** — Open. `Engine.Core/Hosting/ParameterBinder.cs:67-76`. A review agent ran it again: `1e999`
  gives `Applied`, and the next query gives HTTP 500. Register entry R-0028.
- **E12** — Open. Register entry R-0027.
- **E13** — Open. `QueriesEndpoint.cs:84` and `Engine.Cli/Cli.cs:167` still ask for `Aabb`. TASK-0028.
- **E14** — Open. Register entry R-0028.
- **E15** — Fixed. TASK-0044, v0.37.
- **E16** — Open. Register entry R-0028.
- **E17** — Open in ten parts of thirteen. Fixed since the last review: the replay through the cache
  (TASK-0035). Register entry R-0028.
- **E18** — Open. `Engine.Core/CommandBus.cs:100-101`; the comment at `:94-96` still promises a reset.
  Register entry R-0033.
- **E19** — Open. Register entry R-0033.
- **E20** — Open. `Engine.Core/DocumentSession.cs` has no check for a second entry. A review agent ran
  it: a sink that called the session waited until its time limit. Register entry R-0033.
- **E21** — Open. Register entry R-0033.
- **E22** — Open. Register entry R-0033.
- **E23** — Open. `Engine.Api.Http/Program.cs:15` reads `urls` only. Register entry R-0033.
- **E24** — Open in seven parts of nine. Fixed: the check of `TakeSeqs` (TASK-0035) and the comment of
  `EngineHost.cs` that cited the clamp (TASK-0036). Register entry R-0033.
- **V1** — Open. No file of `3DEngine/`, `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0022.
- **V2** — Open. No file of `3DEngine/`, `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0022.
- **V3** — Open. No file of `3DEngine/`, `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0022.
- **V4** — Open. No file of `3DEngine/`, `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0027.
- **V5** — Open. No file of `3DEngine/`, `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0030.
- **V6** — Open. No file of `3DEngine/`, `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0030.
- **V7** — Open. No file of `3DEngine/`, `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Register entry R-0030.
- **T1** — Fixed in two parts of three. A native test cannot skip when `CI` is `true`
  (`Engine.Tests/Geometry/ManifoldGeometryBackendTests.cs:15-25`), and a replay reports a rejection.
  Open: no gate compares the native output of two platforms. Register entry R-0023.
- **T2** — Open in one part of five. No test makes the backend throw through the bus. R-0027.
- **T3** — Fixed. A task that is `Done` before a commit and after it governs nothing
  (`WriteSetGateTests.cs:297-329`). A review agent staged a progress line on TASK-0048, which is `Done`,
  and the gate failed. TASK-0047, v0.42.
- **T4** — Open in three parts of four. A review agent injected the four forms into `Engine.Core`, and
  the determinism gate passed them. Register entry R-0031.
- **T5** — Open in one part of three. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36.
- **T7** — Open in four parts of five. Fixed: the codebase review gate refuses an empty measure value.
  Register entry R-0031.
- **T8** — Fixed. Each job has a time limit, and a hang stops after two minutes with a name. A review
  agent injected a hang with a limit of 15 seconds, and the script named the test. TASK-0047, v0.42.
- **T9** — Fixed. A review agent injected a `Directory.Build.props` with a reference to `3DEngine.Core`
  and `win-x86`, and both gates failed. TASK-0047, v0.42. T16 gives a form that still passes.
- **T10** — Open. Register entry R-0031.
- **T11** — Fixed in part: the gate reads the task file at the commit. The gate code still comes from
  the tip, and T14 is the worst result of that. Register entry R-0031.
- **T12** — Open. Register entry R-0034, limit 2026-11-03.
- **T13** — Open in three parts of four. Fixed: the report script reads a stopped run. R-0031 and R-0034.
- **C1** — Open in two parts. The empty markers and the flags that nothing reads. R-0027.
- **P1** — Open. Nothing is measured. R-0027.

## 1 Current architecture

**[Observed]** The projects and their references are those of the last review. Three changes in the
behaviour of the engine and its hosts: `Document.Version` is the count of the log, and the event
sequence is a separate counter of the bus (ADR-0020); a replay stops at a divergence; each host
requires the native backend and refuses to start without it.

**[Observed]** The native package is `nuget/Engine.Geometry.Manifold.Native.3.5.2.1.nupkg`. One recipe
builds it: `eng/manifold-native/build.sh` and `build.ps1`, which the workflow and a local build call.
Only a run on `main` publishes a package, to a branch per version that nothing overwrites.

**[Observed]** Three records still wait for their code: ADR-0019 (TASK-0038), ADR-0021 (TASK-0037)
and ADR-0018 (TASK-0028).

## 2 Patterns in use

- **Each test fails first.** Each task of this period records a failure before its change. **[Observed]**
- **A gate is proved by an injection.** Each new rule of TASK-0047 failed on an injection first.
  **[Observed]**
- **A public name for each failure.** The pipeline and the native build write each failure as a public
  annotation. The session read each red run of this period with no sign-in. **[Observed]**
- **A gate that runs inside the code it judges.** The write-set gate is a test in the assembly that a
  task may change (T14). **[Observed]**

## 3 Review of the code

Each finding has an identifier, a severity, a title and a state. An identifier of an earlier review
stays with its finding. The earlier reviews give the evidence of each earlier finding.

### Correctness of the engine

#### E1 · Critical · A replay can rebuild a different Document · Fixed

TASK-0035, v0.43. **[Observed]**

#### E2 · Critical · Queries read shared state while a command writes it · Fixed

TASK-0034, v0.39. **[Observed]**

#### E3 · High · The backend changes with no message · Fixed

TASK-0036, v0.44. **[Observed]**

#### E4 · High · The commit uses a cancellable token after the log append · Fixed

TASK-0034, v0.39. **[Observed]**

#### E5 · Medium · An integer from HTTP JSON can never bind · Open

Register entry R-0027. **[Observed]**

#### E6 · Medium · Other engine findings · Open

Register entry R-0027. **[Observed]**

#### E7 · High · A replay discards each result, so a divergence is silent · Fixed

TASK-0035, v0.43. **[Observed]**

#### E8 · High · An idle subscriber receives 1,025 heartbeat frames and not one · Fixed

TASK-0044, v0.37. **[Observed]**

#### E9 · High · A subscriber that connects during a commit can lose an event or receive it two times · Fixed

TASK-0034, v0.39. **[Observed]**

#### E10 · Medium · A second bus on one Document starts the sequence at 1 again · Fixed

TASK-0035, v0.43. **[Observed]**

#### E11 · Medium · A value that is not finite enters the log, and the host answers with HTTP 500 · Open

Register entry R-0028. **[Observed]** in the code.

#### E12 · Medium · The binder refuses a whole number for a number field · Open

Register entry R-0027. **[Observed]**

#### E13 · Medium · A failed query returns a box of zeros and not null · Open

TASK-0028. **[Observed]**

#### E14 · Medium · The Manifold wrapper frees a caller buffer with the C++ `delete` · Open

Register entry R-0028. **[Observed]** in the code.

#### E15 · Medium · The host has no bind rule, no Origin check and no Host check · Fixed

TASK-0044, v0.37. **[Observed]**

#### E16 · Medium · The request envelope ignores an unknown member · Open

Register entry R-0028. **[Observed]**

#### E17 · Low · Other engine and host findings · Open in ten parts of thirteen

Register entry R-0028. **[Observed]** in the code; the runs are by a review agent.

#### E18 · Medium · A sink that throws leaves a gap in the sequence, and nothing sends a reset · Open

The owner decided the correction on 2026-10-04: a sink must not throw. Register entry R-0033.
**[Observed]**

#### E19 · Medium · A reference to the Document or to the sink can leave the session · Open

Register entry R-0033. **[Observed]**

#### E20 · Medium · A call back into the session blocks every client · Open

Register entry R-0033. TASK-0038 must not ship before this correction. **[Observed]**

#### E21 · Medium · A handler that fails after it changed the backend leaves an orphan solid · Open

Register entry R-0033. **[Observed]**

#### E22 · Medium · A cancellation from a handler is cached, so a retry never runs · Open

Register entry R-0033. **[Observed]**

#### E23 · Medium · The bind guard reads one source of addresses, and the surface has no authentication · Open

Register entry R-0033. TASK-0038 must not ship before this correction. **[Observed]**

#### E24 · Low · Other engine and host findings of the third review · Open in seven parts of nine

Register entry R-0033. **[Observed]**

#### E25 · Medium · A replayed Document can never get a command bus or a session · Confirmed by a run

- **Evidence.** `Replay.ReplayLog` builds its bus with `CommandBus.ForReplay`
  (`Engine.Core/Replay.cs:32`), which calls the private constructor (`Engine.Core/CommandBus.cs:56`).
  That constructor registers the bus for the Document in a static table (`:72`). **[Observed]** A review
  agent ran it: a `DocumentSession` on the replayed Document threw "The Document already has a command
  bus".
- **Impact.** A load path cannot continue live after a replay, and TASK-0019 plans exactly that. The next
  sequence number stays inside the dead replay bus. No production caller exists today. **[Inferred]**
- **Correction.** **[Recommended]** `ForReplay` does not register the Document, or the replay hands its
  bus and its next sequence number to the caller. A test builds a session on `ReplayResult.Document`.
- **Owner.** Register entry R-0036.

#### E26 · Low · The version can go back through a cast of the log · Confirmed by a run

- **Evidence.** `Engine.Contracts/Document.cs:21` returns the list itself as `IReadOnlyList`. A review
  agent ran it: inside `Read`, a cast to `List<Command>` and `Clear()` moved the version from 2 to 0.
  **[Observed]** in the code.
- **Correction.** **[Recommended]** Return one `AsReadOnly()` view. The public shape does not change.
- **Owner.** Register entry R-0036.

#### E27 · Medium · After a divergence the backend keeps the bodies, and a second replay names a wrong cause · Confirmed by a run

- **Evidence.** `Engine.Core/Replay.cs:30-41` has no cleanup and does not check that the backend is
  empty. A review agent ran it: a replay that diverged at entry 2 left two bodies in the backend, and a
  second replay on it diverged at entry 0 with `E-GEOM-NATIVE-OP`. **[Observed]** in the code.
- **Impact.** A host that retries on the same backend reports the wrong entry and the wrong cause.
  `ReplayDivergenceException` has no diagnostic code, so each host must map it. **[Inferred]**
- **Correction.** **[Recommended]** Refuse a backend that holds a body, and register a code when a host
  first surfaces a divergence.
- **Owner.** Register entry R-0036.

#### E28 · Medium · A handler exception during a replay escapes raw · Confirmed by a run

- **Evidence.** `Replay.cs:37` has no catch, and the bus catches a cancellation only
  (`CommandBus.cs:137-144`). A review agent ran it: an `InvalidCastException` left `ReplayLog` with no
  entry and no command. The test at `ReplayVersionTests.cs:84-85` accepts any
  `InvalidOperationException`, so it cannot see the difference. **[Observed]** in the code.
- **Correction.** **[Recommended]** Wrap the exception in `ReplayDivergenceException`, and assert the
  exact type in the test.
- **Owner.** Register entry R-0036.

#### E29 · Low · A cancellation during a replay is reported as a divergence · Confirmed by a run, latent

The bus turns each `OperationCanceledException` into `Cancelled` (E22), and the replay then throws a
divergence. No handler observes the token today. A review agent ran it. **[Observed]** in the code.
Register entry R-0036.

#### E30 · Low · The default backend of a replay is the null backend · Confirmed by a run

`Replay.cs:24` defaults to `NullGeometryBackend`, so each log with geometry now diverges at its first
box. Finding E7 recommended a required parameter, and question Q1 of the last review did not include
it. **[Observed]** Register entry R-0036.

#### E31 · Low · Comments that TASK-0035 left false · Confirmed by reading

`CommandBus.cs:94-96` (the reset of E18, and a cached result after any time),
`ManifoldGeometryBackend.cs:84-86` and `InProcessMeshBackend.cs:19-22` (the cache stops each duplicate:
false for a replay), `Replay.cs:16-18` (a duplicate is "applied two times": true for `NoOp` only), and a
message that ends in "..". **[Observed]** Register entry R-0036.

#### E32 · Low · A query of a body that the backend does not hold throws through the query bus · Confirmed by a run

`GetBoundingBoxQueryHandler.cs:62` has no catch, and `QueryBus.cs:43` has none. A review agent ran it:
`KeyNotFoundException` left the bus, which a host answers with HTTP 500. **[Observed]** in the code.
Register entry R-0036.

### The hosts

#### E33 · Medium · The `seq` of the reset snapshot comes from the ring and not from the bus · Confirmed in the code, latent

`Engine.Api.Http/WebSockets/EventBroadcaster.cs` takes the highest `Seq` of the ring. After a sink that
throws (E18), the ring ends one event early, and a reset gives a `seq` that the next live event does
not follow. The normal case is correct. **[Inferred]** Correct E18 first. Register entry R-0036.

#### E34 · Low · `/schema/backend` gives the pinned version and not the version that loaded · Confirmed in the code

The version is the constant `NativeVersion`, and no code reads a version from the library.
`docs/diagnostics.md` says that `E-GEOM-BACKEND-INIT` covers a "version mismatch", which no code checks.
**[Observed]** Register entry R-0036.

#### E35 · Low · `/schema/backend` is in no list of the surface · Confirmed by reading

ADR-0008 §9 lists the schema endpoints, and no gate reads the new one. The managed backend reports the
assembly version of `Engine.Core`, which says nothing. **[Observed]** Register entry R-0036.

#### E36 · Low · The exit codes of the HTTP host have no document · Confirmed by a run

`Program.cs` returns 1 for a refused address and 3 for an absent backend, and other start failures give
an unhandled exception. In the command line, 1 means "rejected". No test runs the process to see exit
code 3. A review agent ran each case. **[Observed]** in the code. Register entry R-0036.

#### E37 · Low · With no library, the command line reports the backend for a misspelled command · Confirmed by a run

`Cli.cs` builds the session before it looks up the handler, so `apply CreateBx` gives exit code 3 and
not `E-CMD-UNKNOWN`. **[Observed]** Register entry R-0036.

#### E38 · Low · The message of `E-GEOM-BACKEND-INIT` does not separate a missing file from a platform with no package · Confirmed by a run

On `win-x64` it says that the package holds `win-x64`. **[Observed]** Register entry R-0036.

#### E39 · Low · The backend selection exists two times · Confirmed by reading

`Engine.Cli/Cli.cs` and `Engine.Api.Http/EngineHost.cs` each hold the same selection and the same
option record, so the two can drift. **[Observed]** Register entry R-0036.

### The Vulkan layer and the desktop host

No file of `3DEngine/`, `3DEngine.Vulkan/` or `3DEngine.Core/` changed after `ae5c521`. Each finding
keeps its state and its evidence. **[Observed]**

#### V1 · High · `VK_SUBOPTIMAL_KHR` is handled as a failure · Open

Register entry R-0022.

#### V2 · High · The special extent value passes the size check · Open

Register entry R-0022.

#### V3 · High · No portability flag for macOS · Open

Register entry R-0022.

#### V4 · Medium · Other desktop findings · Open

Register entry R-0027.

#### V5 · High · The device selection continues with a null device when no device is suitable · Open

Register entry R-0030.

#### V6 · Medium · A pointer into managed memory goes to Vulkan with no pin · Open

Register entry R-0030.

#### V7 · Low · Other findings in the Vulkan layer and the render kernel · Open

Register entry R-0030.

### Tests, gates and the pipeline

#### T1 · High · The pipeline cannot see three kinds of loss · Open in one part of three

Register entry R-0023. **[Observed]**

#### T2 · Medium · Tests that cannot fail, and gates with a short list · Open in one part of five

Register entry R-0027. **[Observed]**

#### T3 · High · The write-set gate has three holes · Fixed

TASK-0047, v0.42. **[Observed]**

#### T4 · High · Four gates read text with one expression and miss the other forms · Open in three parts of four

Register entry R-0031. **[Observed]** in the code; the injections are by a review agent.

#### T5 · Medium · A parser that returns nothing makes its gate pass · Open in one part of three

Register entry R-0031. **[Observed]**

#### T6 · Medium · Two jobs of the workflow hide a failed git command · Fixed

TASK-0043, v0.36. **[Observed]**

#### T7 · Low · Other findings in the gates · Open in four parts of five

Register entry R-0031. **[Observed]**

#### T8 · High · A test that hangs holds the pipeline for six hours and gives no name · Fixed

TASK-0047, v0.42. **[Observed]**

#### T9 · High · A project file can turn the gates off, and a props file gets past two gates · Fixed

TASK-0047, v0.42. **[Observed]**

#### T10 · Medium · A register heading with a wrong form hides an overdue entry · Open

Register entry R-0031. **[Observed]** in the code.

#### T11 · Medium · The write-set job reads the rules at the tip and not at each commit · Fixed in part

Register entry R-0031. **[Observed]**

#### T12 · Medium · Three tests depend on timing on a slow runner · Open

Register entry R-0034, limit 2026-11-03. **[Observed]** in the code.

#### T13 · Low · Other findings in the tests and the pipeline · Open in three parts of four

Register entries R-0031 and R-0034. **[Observed]**

#### T14 · High · A test file can switch off the write-set gate for its own commit · Confirmed by an injection

- **Evidence.** The dynamic test of `Engine.Tests/Governance/WriteSetGateTests.cs` returns when the
  variable `WRITE_SET_FILES` is empty (`:167-168`), and the pipeline runs it from the build of the tip
  (`.github/workflows/ci.yml`). A review agent added a file under `Engine.Tests/` with a module
  initializer that clears the variable, and the gate passed for a forbidden path and for a gate file.
  With the file removed, the same input failed. **[Observed]** in the code.
- **Impact.** Any task that permits a path under `Engine.Tests/` can turn off the write-set gate with
  one file that is not a gate file: today TASK-0037 and TASK-0038 (`Engine.Tests/**`) and TASK-0028
  (`Engine.Tests/Cli/**`). **[Inferred]**
- **Correction.** **[Recommended]** Run the dynamic check outside the test assembly, for example as a
  small program that the workflow builds from `main`, or refuse a module initializer and a write to an
  environment variable in `Engine.Tests`.
- **Owner.** Register entry R-0031.

#### T15 · Medium · TASK-0038 lost a permit that it needs · Confirmed by a run

TASK-0047 made `Engine.Tests/Engine.Tests.csproj` a gate file, and TASK-0038 permits `Engine.Tests/**`
only, while its scope moves `Program` into a new project that the tests must reference. A review agent
ran the gate. **[Observed]** This review adds the exact path to the write set of TASK-0038.

#### T16 · Medium · An import through a property gets past the dependency gate and the x86 gate · Confirmed by an injection

`DependencyDirectionGateTests.cs` skips an import path that holds `$(`. A review agent imported
`$(MSBuildThisFileDirectory)` with a forbidden reference and `win-x86`, and both gates passed.
**[Observed]** in the code. **[Recommended]** Read the items that MSBuild evaluates. Register entry R-0031.

#### T17 · Medium · The package gate reads one fixed file, and the package has no source mapping · Confirmed by an injection

`NativePackageGateTests.cs:18` names one file, and `nuget.config` maps no package to the local folder.
A second package in `nuget/` passed the gate. **[Observed]** in the code. **[Recommended]** Read the
version from the project file, require one package in `nuget/`, and map the package ID to the local
source. Register entry R-0031.

#### T18 · Medium · A push of any branch could claim a package version · Fixed

Found by a review agent on the branch of TASK-0049, and corrected there before the merge: only `main`
publishes a package, and a tag push starts no run. **[Observed]**

#### T19 · Low · The write token stayed on disk during the pack job · Fixed

TASK-0049: the checkout keeps no token, and only the push step gets it. **[Observed]**

#### T20 · Low · The pin values reached the scripts unchecked · Fixed

TASK-0049: the pin job checks strict forms, and the pack step reads the values from `env:`.
**[Observed]**

#### T21 · Medium · A second local build into one staging folder failed · Fixed

TASK-0049: the script empties the files of the staging folder first. **[Observed]**

#### T22 · Low · The Windows script stopped at a CMake warning in PowerShell 5.1 · Fixed

TASK-0049: the exit code decides, and a line on the error output does not. A test under redirection
continued after a warning and stopped on exit code 5. **[Observed]**

#### T23 · Low · Windows has no build-path check, and nothing proves where a package came from · Confirmed by reading

`build.ps1` checks no path, and the gate looks for the folders of a GitHub runner only. A library built
on this computer would pass. **[Observed]** Register entry R-0031.

#### T24 · Low · Two false statements about the repository · Confirmed by reading

`THIRD-PARTY-NOTICES.md` names `nuget/Engine.Geometry.Manifold.Native.3.5.2.nupkg` in its section 1, and
`nuget/` holds 3.5.2.1 only; this review corrects it. The comment about `--cc` in `ci.yml` stays false.
**[Observed]** Register entry R-0031 for the comment.

#### T25 · Low · The local write-set check reads the index and HEAD, and a missing git turns a rule off · Confirmed by an injection

A change in the working tree that is not staged gives a misleading message, and with no git the rule
for a `Done` task does not apply. The pipeline always gives the hash. **[Observed]** in the code.
Register entry R-0031.

### Complexity and dead code

#### C1 · Low · Code that serves no objective · Open in two parts

**[Observed]** The two empty markers and the flags that nothing reads.

### Performance

#### P1 · Low now · Three costs that grow with the scene · Open

**[Inferred]** Nothing is measured. A query that names a body scans the body list, and the snapshot of
a reset runs inside the session.

## 4 The next planned task

**[Observed]** The roadmap gives phase **P0.16**: "An operation consumes its operands" (TASK-0037,
ADR-0021). The order of the owner of 2026-10-08 puts one smaller task first: the corrections of E20 and
E23, which TASK-0038 needs. TASK-0037 then follows.

## 5 Research for that task

**[Observed]** E20: `Engine.Core/DocumentSession.cs` has one `SemaphoreSlim` and no check for a second
entry from the same flow. An `AsyncLocal<bool>` that each entry sets, and that a second entry finds,
detects it with no cost on the normal path. **[Recommended]** It throws `InvalidOperationException`
with a message that names the rule, and a test calls the session from a sink and from a read.

**[Observed]** E23: `Engine.Api.Http/Program.cs:15` reads the key `urls` before the build. The addresses
that Kestrel binds come from several sources: `urls`, each `Kestrel:Endpoints:<name>:Url`, and the
variables `ASPNETCORE_HTTP_PORTS` and `ASPNETCORE_HTTPS_PORTS`, which bind each interface.
**[Recommended]** Check each of these sources before the start, so that the host never binds a foreign
address, not even for a moment. Then check the addresses that the server bound, through
`IServerAddressesFeature`, for a source that the first check does not know. Each refusal stops the
host with exit code 1. A test starts the real host with `--Kestrel:Endpoints:E1:Url=` and a foreign
address, and with `ASPNETCORE_HTTP_PORTS`.

## 6 Recommended approach

1. **[Recommended]** One task for E20 and E23, each test first, with the real host for E23.
2. TASK-0037, which changes the commit of the bus (`ConsumedBodies`) and the snapshot.
3. **[Recommended]** A governance task for T14 before the next task that permits `Engine.Tests/**`
   has a reason to touch it. The risk is a deliberate act, not an accident, so it can follow TASK-0037.

## Questions for the owner

**Q1. Where must the dynamic write-set check run (T14)?** Option A: a small program in `eng/` that the
workflow builds from `main`, so that a change on a branch cannot change the judge. Option B: keep the
test, and add a gate that refuses a module initializer and a write to an environment variable in
`Engine.Tests`. Recommendation: A. It removes the class of defect, and B removes one form of it.
