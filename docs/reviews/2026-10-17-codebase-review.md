---
date: 2026-10-17
commit: 2b8e928
ledger: v0.76
previous: 2026-10-13-codebase-review.md
---

# Codebase review — 2026-10-17

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the ninth codebase review. The review of 2026-10-13 examined v0.70, and v0.76 is the sixth entry
after it, so the next ledger entry had to be a review. The next review must come before ledger entry
v0.83. `Engine.Tests/Governance/CodebaseReviewGateTests.cs` holds that rule, and `docs/templates.md`,
section 7, gives the form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is
the authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this
file does not.

| Item | Value |
|---|---|
| Code state | `main` at `2b8e928`, the merge of v0.76 |
| Branch of this review | `codebase-review-2026-10-17` |
| Tests | 461 on this computer (415 in `Engine.Tests`, 39 in `3DEngine.Tests`, 7 in `3DEngine.ImageTests`) |
| Pipeline | Run 39011064150 on `main`: green on Ubuntu, Windows and macOS |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner
decides.

**Method.** Five tasks changed code after `2cc08b6`, the commit of the last review: TASK-0073 (the gates),
TASK-0076 (R6), TASK-0074 (the hosts and the render side), TASK-0067 (the hosts), and the record of
TASK-0075. Four review agents worked in parallel, each one in its own git worktree at `2b8e928`: the gates
with the judge and the pipeline, the engine with the hosts, the render side, and the research for R7. The
agents ran probes in scratch copies and removed each one. I read each line that this review cites, on
disk. I ran again the first half of T54. A line that says "a review agent ran it" means that I read the
cited code and did not repeat the run.

## Summary

- **The first objective is complete, and its fixes hold.** R6 shows the cut, and an image test with no
  window holds it on Ubuntu. Each fix of the gates (T48 to T53), of the subscription (E58 to E65) and of
  the render side (V5, V6, V8 to V14) held against the probes of the agents, for the forms that it named.
  **[Observed]**
- **Two files that are not gate files can stop each test of `Engine.Tests` with a green run (T54, high).**
  A helper file with its own `FactAttribute` in the namespace `Engine.Tests` builds with no warning and
  leaves no test; I ran it. A second file in another test project then writes a result file that beats
  the floor of executed tests. **[Observed]**
- **The gates compare MSBuild names with case, and MSBuild does not (T55, high).** A lower-case
  `projectreference`, a lower-case property that imports a file, and a lower-case `compile` item each
  passed the gates while MSBuild used them. **[Observed]** in runs of a review agent.
- **A stop can free the geometry backend while a command still runs (E66, medium).** The shorter stop of
  TASK-0067 makes the race easier to meet. **[Observed]** with a slow test backend.
- **The research for R7 gives eight decisions for the owner, and one conflict between records.** The
  roadmap makes selection a command (R8), and ADR-0007 makes it a state of the client. The native SDL of
  the binding is the 3.1.3 preview. **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A
source line is a line of a `.cs` or `.razor` file that git tracks. The script of this review gives 7,973
production lines in 121 files at `2cc08b6`, which are the values of the last review. The test lines now
also count `3DEngine.ImageTests`, which did not exist at the last review.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 14 | `.csproj` lines in `3DEngine.sln`. The last review gave 13; TASK-0076 added `3DEngine.ImageTests`. Git tracks 15 project files: the native package is not in the solution. |
| Production source lines | 8,664 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests` and `3DEngine.ImageTests`, 127 files. The last review gave 7,973 in 121 files. |
| Test source lines | 10,608 | `Engine.Tests` (9,575 in 70 files), `3DEngine.Tests` (670 in 7 files) and `3DEngine.ImageTests` (363 in 3 files). The last review gave 9,477. |
| Tests | 461 | `dotnet test 3DEngine.sln --no-build`: 415, 39 and 7 passed, 0 failed, 0 skipped. The last review gave 402. The Windows and macOS runners skip the image test. |
| Gate classes | 17 | Tracked files whose name ends in `GateTests.cs`. Fourteen of them also run in `eng/repo-gates`. The last review gave 17. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 15 | Section "Open" of `docs/register.md`, which is the limit. This review adds none. The last review gave 14. |
| ADRs | 25 | `docs/adr/0001` to `0025`. In force: 23 (11 accepted, 12 amended). |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 7 | The same method: V2, V3, T1, T4, T31, T54, T55. The last review gave 8. |
| Findings: medium | 26 | The same method: E5, E6, E11, E12, E13, E14, E16, E18, E19, E21, E22, E25, E27, E28, E33, E66, V4, T2, T5, T10, T17, T32, T37, T56, T57, T58. The last review gave 28. |
| Findings: low | 41 | The same method: E17, E24, E26, E29 to E32, E34 to E39, E46, E57, E67 to E71, V7, V15, V16 to V25, T7, T13, T23, T25, T59 to T61, C1, P1. The last review gave 38. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later
probe found.

## Findings of the previous review

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. The backend choice moved into `Engine.Api.Http.Server/ServerEngine.cs` and `3DEngine/DesktopSurface.cs`; each stops when the native library does not load.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. `Engine.Api.Http/Endpoints/JsonParameters.cs:44` is unchanged. Register entry R-0027.
- **E6** — Open. No change. Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. E27, E28 and E30 stay open.
- **E8** — Fixed. TASK-0044, v0.37. TASK-0059 changed the method of its test, and the defect still fails it.
- **E9** — Fixed, and it holds. TASK-0071 changed `Engine.Api.Http/WebSockets/EventBroadcaster.cs`, and `AttachAndPrime` did not change; the endpoint still calls it inside `Session.Read`.
- **E10** — Fixed. TASK-0035, v0.43. E26 stays open.
- **E11** — Open, and wider. `Engine.Core/Hosting/ParameterBinder.cs:67-76` is unchanged. A review agent applied `CreateBox` with `sizeX` "Infinity" over HTTP: Applied; `GetBoundingBox` on that body then gave HTTP 500, because the JSON writer cannot write an infinity, and `GetTessellation` gave 0 triangles and no error. Register entry R-0028.
- **E12** — Open. No change. Register entry R-0027.
- **E13** — Fixed in part. No change; `3DEngine/SessionBodies.cs` still reads the typed error of the query. Register entry R-0028.
- **E14** — Open. `Engine.Geometry.Manifold/Native/ManifoldSolidHandle.cs` and `GetBoundingBox` still give memory of `Marshal.AllocHGlobal` to a C++ `delete`. TASK-0028 used the destructor and `Marshal.FreeHGlobal` for the new mesh, and `manifold_destruct_manifold` and `manifold_destruct_box` are exported, so the fix needs no native build. A review agent read the import table of `manifoldc.dll` (the C runtime heap) and ran 100,000 create and dispose cycles with no crash. Register entry R-0028.
- **E15** — Fixed. TASK-0044, v0.37. TASK-0058 and TASK-0038 moved and extended the checks.
- **E16** — Open. No change. Register entry R-0028.
- **E17** — Open in ten parts of thirteen. TASK-0062 changed `Engine.Api.Http/Endpoints/EventsEndpoint.cs` but not the cited parts: its comment still says 1003 (line 18), and it sends 1007 (line 130). Register entry R-0028.
- **E18** — Open, and wider. The comment of `Engine.Core/CommandBus.cs` still promises a reset; a throw of the report of a removed action now also stops the events of a commit (E67). Register entry R-0033.
- **E19** — Open. No change. Register entry R-0033.
- **E20** — Fixed, and it holds. A review agent called the session from a subscription of TASK-0071: the guard refused it, with no deadlock (E62 gives the effect).
- **E21** — Open. No change. Register entry R-0033.
- **E22** — Open. No change. Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. TASK-0058 closed E40 and E41.
- **E24** — Open in seven parts of nine. TASK-0062 added `Subscriber.Stop` and changed no cited part; the constructor still starts the heartbeat. Register entry R-0033.
- **E25** — Open. `Engine.Core/CommandBus.cs` still registers the bus of a replay. Register entry R-0036.
- **E26** — Open. `Engine.Contracts/Document.cs` still returns the list itself. Register entry R-0036.
- **E27** — Open. No change. Register entry R-0036.
- **E28** — Open. No change. Register entry R-0036.
- **E29** — Open. No change. Register entry R-0036.
- **E30** — Open. No change. Register entry R-0036.
- **E31** — Open. Each cited comment is still present. Register entry R-0036.
- **E32** — Open, and smaller. `Engine.Core/Queries/GetTessellationQueryHandler.cs` has no change, but the desktop no longer ends on its throw (E65 is fixed). Register entry R-0036.
- **E33** — Open. No change. Register entry R-0036.
- **E34** — Open. No change. Register entry R-0036.
- **E35** — Open. No change. Register entry R-0036.
- **E36** — Open, and smaller. Exit code 5 is documented for each other failure of the start (`Engine.Api.Http.Server/Program.cs:15-18`, `3DEngine/Documentation/Running.md`), but `engine-api-http` still crashes for a configuration file that it cannot read (E68). Register entry R-0036.
- **E37** — Open. No change. Register entry R-0036.
- **E38** — Open. No change. Register entry R-0036.
- **E39** — Open, and worse. The backend choice now exists three times: `Engine.Cli/Cli.cs:214`, `Engine.Api.Http.Server/ServerEngine.cs:33` and `3DEngine/DesktopSurface.cs:80`. Register entry R-0036.
- **E40** — Fixed. The start check reads each address with `BindingAddress.Parse`, the parser of Kestrel, and the two forms with a user part failed the test before the correction. TASK-0058, v0.56.
- **E41** — Fixed. The endpoint configuration does not reload, and a process test that writes `appsettings.json` after the start failed before the correction. TASK-0058, v0.56.
- **E42** — Fixed as recommended. The comment in `Engine.Core/DocumentSession.cs` gives the limit and `ExecutionContext.SuppressFlow`. TASK-0058, v0.56.
- **E43** — Fixed as recommended. The same comment gives the limit and the rule for a sink; no code holds the rule. TASK-0058, v0.56.
- **E44** — Fixed. The bus refuses a created handle that it created before or that the same command consumes, and the three cases failed before the correction. TASK-0058, v0.56.
- **E45** — Fixed. The three texts agree with the code. TASK-0058, v0.56.
- **E46** — Open. TASK-0055 (P0.22).
- **E47** — Fixed. The glossary term "live body" says that the order is not history. TASK-0058, v0.56.
- **V1** — Fixed. TASK-0074, v0.75: a SUBOPTIMAL acquire continues the frame, and a SUBOPTIMAL or OUT_OF_DATE present makes the swapchain again (`3DEngine.Vulkan/Presenter.cs`). A review agent read it; no run forced the result.
- **V2** — Open, and with two rules now. `3DEngine.Vulkan/Swapchain.cs` still tests `currentExtent.width > 0`, while `Presenter.SurfaceExtent` reads the special value 0xFFFFFFFF as "the size of the window". Register entry R-0022.
- **V3** — Open. No source of `3DEngine.Vulkan/` names the portability extension. Register entry R-0022.
- **V4** — Open in four parts of five. A failure of the window still goes to standard output with exit code 0 (`3DEngine/DesktopEngineHost.cs`), the results of the framebuffer, of the submit and of the fence wait are still ignored, the log gives the requested version and not the version of the device, and the fallback layer lists stay. Register entry R-0027.
- **V5** — Fixed. TASK-0076, v0.74: the core of the device throws with a message when no device fits, and TASK-0074 names each device and its reason. A review agent required version 1.9 and saw the message for both devices of this computer.
- **V6** — Fixed. TASK-0076, v0.74: each name goes to Vulkan from native memory (`3DEngine.Vulkan/NativeStrings.cs`), which `Dispose` frees.
- **V7** — Open in each part, and wider. A failure at the device creation or in the presenter leaks the instance, the messenger and the surface; `OffscreenTarget` and `DepthBuffer` also leak when they fail part way. Register entry R-0030.
- **T1** — Open in one part of three. No gate compares the native output of two platforms. R-0023.
- **T2** — Open in one part of five. No change. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. TASK-0057 kept the rules in the judge.
- **T4** — Open in two parts of four. The dispatch gate still misses the desktop host (`Engine.Tests/Hosting/DispatchSurfaceGateTests.cs:20`), and the marker gate accepts a closed identifier. The determinism part is fixed for the forms of T45 and T49; T57 gives new forms. Register entry R-0031.
- **T5** — Open in one part of three. No change. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. The job of T6 no longer exists (TASK-0057).
- **T7** — Open in four parts of five. No change. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. T48 shows other files that change the test step with no gate file.
- **T10** — Open. No change. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51.
- **T12** — Fixed. The heartbeat test counts frames, the deadlock test starts its clock after the host is up, and the collection "real host" runs alone; injected waits failed the old tests and passed the new ones. TASK-0059, v0.55. Register entry R-0034 is closed.
- **T13** — Open in part. Correction: the reviews of 2026-10-11 and 2026-10-12 said that `eng/report-failed-tests.js` reads one outcome only; it reads Failed, Error, Timeout and Aborted and the errors of the run since commit 1ef57eb (2026-10-04), which I read. The part of the marker gate stays. Register entry R-0031.
- **T14** — Fixed. TASK-0053, v0.51 and v0.52. T41 is a new path around "the judge comes from `main`".
- **T15** — Fixed. TASK-0050, v0.47.
- **T16** — Fixed. TASK-0066, v0.67: the dependency gate refuses an import with a property or a wildcard. T50 gives forms outside an import element.
- **T17** — Open. No change. Register entry R-0031.
- **T18** — Fixed. TASK-0049, v0.46.
- **T19** — Fixed. TASK-0049, v0.46.
- **T20** — Fixed. TASK-0049, v0.46.
- **T21** — Fixed. TASK-0049, v0.46.
- **T22** — Fixed. TASK-0049, v0.46.
- **T23** — Open. No change. Register entry R-0031.
- **T24** — Fixed. Correction: the progress line of R-0031 of 2026-10-12 listed T24 as open; that was a text error.
- **T25** — Open in part. With no git the judge now fails (`eng/write-set-check/Program.cs:20-23`), so that part is fixed. The check before a commit still reads the index and HEAD, and a change that is not staged gives a misleading message. R-0031.
- **T26** — Fixed. The contract rule is a rule of the judge for each commit, and the job "Contract-touched-needs-ADR" is gone; a test of a scratch repository failed before the correction. TASK-0057, v0.54.
- **T27** — Fixed. The process test asserts that no address was bound, and a probe that bound before an injected refusal failed it. TASK-0058, v0.56.
- **T28** — Fixed. TASK-0066 closed its last part (T41); the condition of the workflow file stays accepted (R-0038).
- **T29** — Fixed. The judge runs git by a full path from `PATH` outside the checkout, with `-C`. TASK-0057, v0.54.
- **T30** — Fixed. A commit with no parent fails, and a merge is judged against its first parent; two tests of a scratch repository failed before the correction. TASK-0057, v0.54.
- **T31** — Open in part, and worse. The three gates that run engine code stay in the test process, and T54 takes them out of the run with files that hold none of the four refused texts. Register entry R-0031.
- **T32** — Fixed in part. A branch is judged from its merge base with `main`. On `main` the job judges the commits of the push only (`.github/workflows/ci.yml:199`), so a green later push still hides a red earlier push there. R-0031.
- **T33** — Fixed. The judge reads the task files at the commit. TASK-0057, v0.54.
- **T34** — Fixed. The documented check joins its two commands with `&&`. TASK-0057, v0.54.
- **T35** — Fixed. A failed git command is a failure, the judge reads the paths with `-z`, `WriteSetRangeTests` covers the steps, a task file with no front matter at the commit fails, and `docs/templates.md` describes the new steps. TASK-0057, v0.54.
- **C1** — Open in two parts. `IBRepOps.cs` and `IFeatureIdMap.cs` remain, and no production code reads `BackendCapabilities`; TASK-0028 added a fifth flag. Register entry R-0027.
- **P1** — Open, and wider. TASK-0074 measured one run (7,194 frames each second, no frame limit), and nothing in the repository measures. The broadcaster copies two arrays for each event, and the render loop waits for the device for each consumed body. Register entry R-0027.
- **E48** — Fixed. TASK-0062, v0.62. A review agent compared 49 forms with the parser of Kestrel: no form that the check accepts binds a foreign address or each interface.
- **E49** — Fixed for a WebSocket subscriber. TASK-0062, v0.62: a review agent measured 0.00 s before the subscribe frame and 0.02 s for the desktop with one subscriber. An open HTTP request still holds the stop (E58).
- **E50** — Fixed by reading. TASK-0062, v0.62: the loop of `3DEngine/DesktopEngineHost.cs` ends on `Stopping`. Nobody sent Ctrl+C to a desktop process.
- **E51** — Fixed for a port in use. TASK-0062, v0.62: exit code 4 from each host (a review agent ran both). A port that the system refuses is E59.
- **E52** — Fixed. TASK-0062, v0.62. A review agent saw the backend disposed after `Dispose` with no request.
- **E53** — Fixed. TASK-0062, v0.62. The clause is gone (`Engine.Core/CommandBus.cs:184`), and a test gives one handle two times.
- **E54** — Fixed. TASK-0062, v0.62. A review agent ran `--port`, `--port=` and `Desktop__Port`; the variable `PORT` has no effect.
- **E55** — Fixed. TASK-0062, v0.62. The messages say "The surface".
- **E56** — Fixed. TASK-0062, v0.62. `EngineHost.Backend` is internal.
- **E57** — Open in part, and smaller. `3DEngine.ImageTests` runs `SessionBodies` and `SceneRenderer` through the image test (TASK-0076), on Ubuntu in the pipeline only; no test calls `DesktopSurface` or `DesktopEngineHost`. Register entry R-0039.
- **T36** — Fixed. TASK-0061, v0.61. A review agent removed the union and two tests failed. T42 is a nearby form.
- **T37** — Open in part. A review agent injected a skip on a failed read of a test file in `eng/write-set-check/Judge.cs`, and each of the 70 tests of the judge passed.
- **T38** — Fixed. TASK-0063, v0.60: the progress line of R-0031, and R-0038 is accepted.
- **T39** — Fixed. TASK-0061, v0.61. The fallback left `.github/workflows/ci.yml`.
- **T40** — Fixed. TASK-0061, v0.61. The test gap of a failed read of a test file is in T37.
- **E58** — Fixed. TASK-0067, v0.76: the surface sets a stop limit; a review agent saw a stop of 3.01 s with a command of 5 s in flight, and 0.01 s with an open subscriber (E70 gives the wait).
- **E59** — Fixed for the start. TASK-0067, v0.76: an excluded port, a bad endpoint, a port above 65535 and an absent certificate each gave exit code 5 in a run of a review agent. A configuration file that the server cannot read still crashes it (E68).
- **E60** — Fixed. TASK-0067, v0.76: `Engine.Api.Http/Json/ApiJson.cs` writes compact JSON, and each endpoint uses those options.
- **T41** — Fixed. TASK-0066, v0.67. A review agent planted a `global.json` with a message: `dotnet build` from its folder stopped, and the judge and the gate program, run from the same folder, ignored it.
- **T42** — Fixed. TASK-0066, v0.67. A review agent resolved a conflict to the side version, and the judge failed the merge; `WriteSetRangeTests` holds it. T51 gives a form with a rename.
- **T43** — Fixed. TASK-0066, v0.67. `WriteSetRangeTests` holds it.
- **T44** — Fixed for its forms. TASK-0066, v0.67: `--gate-sources` gave "main" for the files that are not gate files. T48 gives other inputs.
- **T45** — Fixed for its forms. TASK-0066, v0.67: a review agent saw each of the eight forms fail. T49 gives new forms.
- **T46** — Fixed for its forms. TASK-0066, v0.67 (reading only). T50 gives new forms.
- **T47** — Fixed for its forms. TASK-0066, v0.67: a theory with no row fails. T52 gives a new form.
- **E61** — Fixed. TASK-0074, v0.75. A review agent made a body before the start and a body that a command consumed before `Apply`: the queue gave the first one first, and the second gave no warning. No test holds it (R-0039).
- **E62** — Fixed. TASK-0074, v0.75: a removed action is reported once, with `W-API-SUBSCRIPTION-REMOVED`. E67 gives a gap of the report.
- **E63** — Fixed. TASK-0074, v0.75: each payload is a `ReadOnlyDictionary` of strings, integers and identifiers.
- **E64** — Fixed. TASK-0074, v0.75: each subscription is one entry, removed by reference once.
- **E65** — Fixed. TASK-0074, v0.75: a review agent gave `Apply` no device, and each body gave one warning while the loop continued. No test holds it (R-0039).
- **V8** — Fixed. TASK-0074, v0.75. V18 and V19 give two gaps of the new path, and V16 says that synchronization validation never checks it.
- **V9** — Fixed. TASK-0074, v0.75: a device must give Vulkan 1.3 and a depth attachment of `D32_SFLOAT`.
- **V10** — Fixed. TASK-0074, v0.75: a review agent saw a negative scale refused in the constructor and in `with`.
- **V11** — Fixed in part. TASK-0074, v0.75 checks the planes, the field of view and the aspect of a projection; V22 gives the gaps that remain.
- **V12** — Fixed. TASK-0076, v0.74: the camera frames the live bodies at each change. V22 gives a box that the far plane cannot hold.
- **V13** — Fixed for its parts. TASK-0074 tests the block of the SPIR-V and `WritePushConstants`, and TASK-0076 runs synchronization validation in the image test. V24: a swap at the call in `Push` passes the unit tests.
- **V14** — Fixed. TASK-0074, v0.75: a review agent added a byte order mark, and `compile.js` removed it and kept both hashes.
- **V15** — Open by decision. TASK-0074 measured it and changed nothing; V23 gives one more cost.
- **T48** — Fixed for its forms. TASK-0073, v0.73: the four kinds of file are gate files, and a floor of executed tests is the second line. T54 and T58 get past the floor.
- **T49** — Fixed for its forms. TASK-0073, v0.73. T55 and T57 give new forms.
- **T50** — Fixed for its forms. TASK-0073, v0.73. T55 and T56 give new forms.
- **T51** — Fixed. TASK-0073, v0.73: the judge runs `git merge-tree` with `-X no-renames`, and a range test holds it.
- **T52** — Fixed for its form. TASK-0073, v0.73. T59 gives a variant.
- **T53** — Fixed. TASK-0073, v0.73: a second review on one day can carry a suffix after the date, and the order of two reviews comes from their ledger versions. T61 gives a nearby gap.

## 1 Current architecture

**[Observed]** The desktop host draws the live bodies of the session that it owns, and frames them with
the camera at each change. `SceneRenderer` draws the scene into one target: the swapchain of the window,
or an offscreen target of a device with no window (ADR-0025). `GraphicsDevice` is the core of the device;
`Presenter` holds the presentation and makes the swapchain again when the window changes.

**[Observed]** `3DEngine.ImageTests` sends the first demonstration to a session, draws it with the renderer
of the desktop, and compares the picture by structure, with synchronization validation on. The pipeline
runs it on Ubuntu with lavapipe, and skips it on Windows and macOS (R-0040).

**[Observed]** The judge and the gate program come from `main`. The step "Count the tests" fails a run
with fewer executed tests than a floor of 437. The three gates that run engine code stay in the test
process.

**[Observed]** Input is read in the host: a left drag of the mouse orbits the camera and the wheel zooms it
(`3DEngine/NativeThreeDEngine.cs`, `HandleEvent`). Roadmap phase R7 replaces this with an intent layer.

## 2 Patterns in use

- **Each correction holds for its forms, and a probe finds the next form.** T55 to T58 follow T49 and T50,
  which followed T45 and T46. A gate that reads MSBuild files or names by hand misses what MSBuild or the
  compiler evaluates: case, wildcards, intrinsics, a shadow type. **[Observed]**
- **The floor of tests is one number for each run.** It caught the probe of T48, and it misses a project
  that leaves the run while another project grows (T58), or a forged result file (T54). **[Observed]**
- **A comment states more than the code does.** E69, E70 and V24 are three comments of this period that
  do not agree with the code, and one of them (E69) is a broken line from an edit script of mine. No gate
  reads a comment. **[Observed]**
- **The render side is checked offscreen, and the window is checked by a picture.** Synchronization
  validation runs only in the image test, so the present path and the remake of the swapchain have never
  run under it (V16). **[Observed]**

## 3 Review of the code

### The engine and the hosts

#### E66 · Medium · A stop frees the geometry backend while a command still runs · Confirmed by a run

`3DEngine/DesktopSurface.cs` stops the app and then disposes it, and the container disposes `EngineHost`,
which disposes the backend. The stop limit of TASK-0067 ends the wait for a request after about 3 s. A
review agent sent a command to a test backend whose `CreateBox` takes 5 s: the stop returned after 3.01 s,
the disposal ran while `CreateBox` still ran, the command then committed, and Kestrel logged "Cannot
access a disposed object". **[Observed]** With Manifold, native memory can be freed under a running
native call. **[Inferred]** **[Recommended]** Before the backend goes, refuse new commands and wait until
the serial section of the session is free.

#### E67 · Low · The report of a removed action runs inside the commit, with no catch · Confirmed by a run

`Engine.Api.Http/WebSockets/EventBroadcaster.cs:143` calls `Report` inside the `catch` block, during the
commit. A review agent made `Report` throw: `Apply` threw, the Document held the body, and the other
actions and the ring missed the later events of that commit. A `Report` that blocked for 1.5 s held a read
for 1.41 s. **[Observed]** The default writes to the error output, which blocks only on a full pipe.
**[Inferred]** **[Recommended]** Put `Report` in its own `try`, or report after the commit.

#### E68 · Low · engine-api-http crashes for a configuration file that it cannot read · Confirmed by a run

`Engine.Api.Http.Server/Program.cs:20` (`CreateBuilder`) and `:32` (`Build`) are outside each `try`. A
review agent put a bad `appsettings.json` in the working folder: the server ended with "Unhandled
exception" and the exit code of a crash; the desktop gives 5 for the same file. **[Observed]**
**[Recommended]** The outer catch of `3DEngine/DesktopEngineHost.cs`, and a process test.

#### E69 · Low · A broken comment line in the broadcaster · Confirmed by reading

`Engine.Api.Http/WebSockets/EventBroadcaster.cs:91` reads `// An a    // The report of an action that a
throw removed`. My edit script of commit 335965e cut the old comment in its middle. **[Observed]**
**[Recommended]** Remove the fragment.

#### E70 · Low · The comment of the stop limit gives the wrong wait · Confirmed by a run

`Engine.Api.Http/EngineSurface.cs:30-33` calls 2 s "the longest wait of a stop for an open request". A
review agent measured 3.01 s, and the test permits 5 s. **[Observed]** **[Recommended]** Give the measured
wait in the comment.

#### E71 · Low · Two start paths return with the app not disposed · Confirmed by reading

`3DEngine/DesktopSurface.cs:89` resolves `EngineHost` before the `try` of the start, so a throw there gives
code 5 through the outer catch of the desktop with no disposal of the app; `Engine.Api.Http.Server/Program.cs:73-75`
returns 1 after `StopAsync` with no `DisposeAsync`. **[Observed]** by reading; no run. **[Recommended]**
Dispose the app on each path that returns early.

### The render side

#### V16 · Low · The present path never runs under synchronization validation · Confirmed by reading

The desktop turns on the core validation layer only (`3DEngine/NativeThreeDEngine.cs:38`), and the image
test draws offscreen only. The semaphores of the frames, the dependencies of the render pass of the
swapchain, the depth image that frames share and `Presenter.Recreate` were never checked for hazards. The
ledger of v0.75 says that 634 remakes gave "no validation message"; that run had the core layer only.
**[Observed]** **[Recommended]** A switch that runs the desktop with synchronization validation, for one
check by the owner.

#### V17 · Low · An image test that fails on validation does not give the message · Confirmed by a run

The callback writes each message to the console only (`3DEngine.Vulkan/GraphicsDevice.cs`), and the
assertion gives a count (`3DEngine.ImageTests/FirstDemonstrationImageTests.cs`). A review agent injected a
hazard and saw "gave 1 messages" and no text. The annotation of the pipeline reads the result file, so a
person with no sign-in sees no message. **[Observed]** **[Recommended]** Collect the messages and put them
in the assertion.

#### V18 · Low · A failed remake of the swapchain breaks the presenter · Confirmed by reading

`3DEngine.Vulkan/Presenter.cs:163` destroys the resources of the frames before `:165` makes the new
swapchain. A throw there leaves a retired swapchain and no frames, and the desktop ends with code 0.
**[Inferred]** **[Recommended]** Make the new swapchain first, and keep the old state when it fails.

#### V19 · Low · A failed submit hangs the desktop · Confirmed by reading

`3DEngine.Vulkan/Presenter.cs:131` ignores the result of `vkQueueSubmit`, and the next acquire of that
image waits for ever on a fence that the failed submit never signals. **[Inferred]** **[Recommended]**
Check the result, and wait with a limit.

#### V20 · Low · The swapchain does not check the composite alpha · Confirmed by reading

`3DEngine.Vulkan/Swapchain.cs:62` asks for `Opaque` and does not read `supportedCompositeAlpha`. Some
surfaces on Linux refuse it. **[Inferred]** It belongs with R-0022. **[Recommended]** Choose a mode that the
surface gives.

#### V21 · Low · The messenger is made when debug utils is absent · Confirmed by reading

`3DEngine.Vulkan/GraphicsDevice.cs:66-70` looks for `VK_EXT_debug_utils` in the extensions of the
implementation only, and `:115` and `:139` chain and create the messenger whenever the layer is on.
**[Inferred]** **[Recommended]** Read the extensions of the layer as well, as the code does for the
validation features.

#### V22 · Low · The camera still accepts values that draw nothing · Confirmed by a run

A review agent saw: a distance of 0 gives a view of NaN; `Camera.Frame` with an aspect of NaN gives a
distance of NaN, because NaN passes `aspect <= 0`; with a near plane of 1 and a far plane of 1.5 the
distance is 2, beyond the far plane; a box of 3,000 units gives a distance clamped to 500, with the eye
inside the body. **[Observed]** **[Recommended]** Check the distance and NaN, and report a box that the far
plane cannot hold.

#### V23 · Low · A minimised window takes one processor core · Confirmed by reading

`Presenter.RenderFrame` returns at once while the surface has no area, and the loop of
`3DEngine/DesktopEngineHost.cs` polls again with no wait. **[Inferred]** **[Recommended]** Wait for an event
while the window has no area.

#### V24 · Low · The comment of the push constant test claims a swap in `Push` · Confirmed by a run

`3DEngine.Tests/PushConstantInterfaceTests.cs:7` says that a swap of the two matrices in `Push` failed no
test before it. A review agent swapped the arguments of the call at `3DEngine.Vulkan/RenderPipeline.cs:138`:
the 39 unit tests passed, and only the image test failed. **[Observed]** **[Recommended]** Correct the
comment, or test `Push` itself.

#### V25 · Low · The SDL binding gives a preview SDL and no Linux arm64 · Confirmed by reading

The native `SDL3.dll` of `Alimer.Bindings.SDL` 3.9.2 is version 3.1.3, a preview of SDL 3; I read the file
version. The package gives `linux-x64`, `osx`, `win-arm64` and `win-x64`, and no `linux-arm64`.
**[Observed]** The desktop therefore cannot run on Linux on arm64, which objective 8 and anti-objective 16
name as a platform. **[Inferred]** **[Recommended]** Check for a binding with SDL 3.2 or later before R7
(question Q2, D7).

### Tests, gates and the pipeline

#### T54 · High · Two files that are not gate files stop each test of `Engine.Tests`, and the run stays green · Confirmed by a run

A file in `Engine.Tests` with the namespace `Engine.Tests` and its own `FactAttribute`, `TheoryAttribute`
and `InlineDataAttribute` shadows the attributes of xUnit, because C# looks in the enclosing namespace
before a global `using`. I ran it: the build gave 0 warnings, `dotnet test` said that no test is available
in `Engine.Tests.dll`, and the gate program passed. **[Observed]** The floor of 437 then fails the run. A
review agent added a test in `3DEngine.Tests` that writes a result file with `executed="1000"` into the
folder of results: `eng/count-tests.js` adds each result file (`:30-38`) and reported 1,046 tests.
**[Observed]** by the agent. Neither file is a gate file, and neither holds one of the four refused texts
of the judge. The three gates that run engine code stop with the rest (T31). **[Recommended]** Name the
attributes of xUnit with `global::` in the gate files, refuse a type with those names outside `Xunit`,
count each test assembly against its own floor from the runner's own result files, and refuse a result
file that the runner did not write.

#### T55 · High · The gates compare MSBuild names with case, and MSBuild does not · Confirmed by a run

`Engine.Tests/Governance/DependencyDirectionGateTests.cs:44` compares the name of an item with `==`, and
`Engine.Tests/Governance/DeterminismCallGateTests.cs:251` matches `"Compile"` and `"Analyzer"` exactly.
MSBuild reads names with no regard to case. A review agent wrote `<projectreference>` to `3DEngine.Core` in
`Engine.Cli.csproj`, a lower-case property that imports a file of targets, and a lower-case `<compile>` of
a file that calls `Math.Sin`: MSBuild used each one, and the gate program passed each one. **[Observed]**
by the agent; I read the comparisons. **[Recommended]** Compare each MSBuild name with no regard to case, or
read the evaluated items.

#### T56 · Medium · A wildcard in a project reference passes the dependency gate · Confirmed by a run

A review agent wrote `Include="..\3DEngine.Vulkan\3DEngine.Vulka?.csproj"` in `Engine.Tests.csproj`: the
graph took the name `3DEngine.Vulka?`, MSBuild expanded it to the Vulkan project, and the gates passed.
**[Observed]** by the agent. **[Recommended]** Refuse `*` and `?` in an `Include` of a reference.

#### T57 · Medium · The determinism gate misses six forms that compile · Confirmed by a run

A review agent compiled each of these in `Engine.Core`, and the gate passed: `Sse.ReciprocalSqrt`,
`Fma.MultiplyAdd`, `Vector128.ConvertToInt32Native`, `double.ClampNative`, reflection through
`GetTypeInfo().DeclaredMethods`, and a `DllImport` of `sin` from `libm`. A control with `Math.Sin` failed.
**[Observed]** by the agent. **[Recommended]** Refuse the members of `System.Runtime.Intrinsics.X86` and
`.Arm` except a permitted list, each name that ends in `Native`, `DeclaredMethods`, and each `DllImport` or
`LibraryImport` in the log path outside the Manifold binding.

#### T58 · Medium · The floor of tests is old and is one total · Confirmed by a run

`eng/test-floor.txt` holds 437, the count at v0.73; the runs now execute 460 on Windows and macOS and 461
on Ubuntu, so 23 tests can leave a run that stays green. A review agent set `IsTestProject` to false in
`3DEngine.ImageTests.csproj` and shadowed `FactAttribute` in one folder of `Engine.Tests`: the run passed
with 448 tests, and `ReplayDeterminismGateTests` did not run. `3DEngine.ImageTests/VulkanImageFactAttribute.cs`
is not a gate file either, so an edit of it can skip the image test on Ubuntu. **[Observed]** by the agent.
**[Recommended]** A floor for each test project, raised by each task that adds tests, and a check that the
floor is not below the count of `main`.

#### T59 · Low · An async lambda in a void check passes the gate runner · Confirmed by a run

`eng/repo-gates/Program.cs:71` refuses an `async void` method. A review agent wrote a check that runs
`Action a = async () => { await Task.Delay(30000); Assert.Fail(); }; a();`, and the runner printed "pass".
**[Observed]** by the agent. **[Recommended]** Refuse an async lambda converted to `Action` in a gate file.

#### T60 · Low · The workflow keeps the default token, and lavapipe has no pin · Confirmed by reading

`.github/workflows/ci.yml` has no `permissions:` block, the checkout keeps its token, and the step of
lavapipe installs the current packages of a moving image. A failing test can write any file into the
folder that the run keeps. **[Observed]** by reading. **[Recommended]** `permissions: contents: read`,
`persist-credentials: false`, and the version of Mesa in the step "Report the runner".

#### T61 · Low · The review gate trusts the field `ledger` of a review · Confirmed by reading

`Engine.Tests/Governance/CodebaseReviewGateTests.cs` takes the highest declared version and does not check
that a ledger heading of that version exists; a review that declares `v0.99` silences the overdue rule.
**[Inferred]** **[Recommended]** Require that the version exists in `docs/CURRENT-STATE.md`.

## 4 The next planned task

The owner set the order on 2026-10-16: TASK-0074, TASK-0067, TASK-0055, TASK-0040, this review, then R7.
The review came before TASK-0055, because v0.77 must be a review. Two new tasks own the findings of this
review: TASK-0078 (P0.30) the gates and the pipeline, with two high findings, and TASK-0079 (P0.31) the
hosts and the render side. The next phase of track R is R7: "Device-tagged pointer samples. The intent
layer and the parity test." It serves objective 9 of `docs/CHARTER.md`: each operation works by touch, and
by mouse and keyboard.

## 5 Research for that task

**[Observed]** in the binding, by a review agent with a reflection tool; I read the file version and the
folders.

- SDL 3 gives mouse, touch, pen and keyboard events through `Alimer.Bindings.SDL` 3.9.2: `SDL_EventType`
  `MouseMotion`, `MouseButtonDown`, `MouseWheel`, `FingerDown`, `FingerUp`, `FingerMotion`, `PenDown`,
  `PenMotion`, `PenAxis`, `KeyDown`, and others. Each event has a timestamp and a device identifier
  (`SDL_MouseID`, `SDL_TouchID` with `SDL_FingerID`, `SDL_PenID`). The binding has no event that cancels a
  finger and no pinch event.
- A touch also gives mouse events with `which == SDL_TOUCH_MOUSEID`, unless the hint
  `SDL_HINT_TOUCH_MOUSE_EVENTS` is off. One finger therefore already orbits the camera today, through the
  synthetic mouse events; two fingers do nothing. An intent layer that reads both kinds would count one
  touch two times. **[Inferred]**
- The native SDL is the 3.1.3 preview (V25). The binding has no pen hint and no pen query.
- This computer has no touch digitizer: the agent read `SM_DIGITIZER` 0 and `SM_MAXIMUMTOUCHES` 0, and found
  two mice and no HID digitizer. The runners have none either.
- `3DEngine.Core` has no project reference, so the samples, the intent state machine and the intents can
  live there with no SDL; the translation of SDL events into samples belongs next to `Window` in
  `3DEngine.Vulkan`, which `3DEngine.Tests` can test. **[Inferred]**
- `Camera` has `Orbit`, `Zoom` and `Frame`, and no `Pan`. **[Observed]**
- **A conflict between records.** `docs/roadmap.md`, row R8, says "Selection, name and visibility as
  commands". ADR-0007 lists the selection set as client ephemeral state, and `docs/CHARTER.md:110` says that
  a host owns "the selection". **[Observed]** The `Select` intent of R7 needs one answer.

## 6 Recommended approach

**[Recommended]** A record `PointerSample` (the kind of device, the device, the pointer, the position, the
pressure, the buttons, the modifiers, the time, the phase) and a pure state machine in `3DEngine.Core` that
turns the samples of one frame into intents with no device kind: orbit, pan, zoom, select and cancel.
Touch and mouse map to the same intents: one finger or a left drag orbits; two fingers or a middle drag or
Shift and a left drag pan; a pinch or the wheel zooms; a tap or a click selects. The translator drops each
synthetic mouse event of a touch. The parity test runs in `3DEngine.Tests`, with no window and no GPU: for
each kind of intent, a touch script and a mouse script give the same intents, and a coverage check fails
when a kind of intent has no touch script. R7 writes no command, so the comparison of logs of
anti-objective 7 starts with R8. A size of 5 to 7 evenings, without an upgrade of SDL. **[Inferred]**

## Questions for the owner

**Q1. Must TASK-0078 come before R7?** It holds two high findings: two files can stop each test of
`Engine.Tests` (T54), and the gates read MSBuild names with case (T55).
- A: yes, TASK-0078 first, then R7 (recommended: the gates guard each later task).
- B: R7 first, TASK-0078 after it.

**Q2. The decisions of R7.** Each one gives the options and a recommendation.
- **D1, touch hardware.** No computer of the project has a touch screen. A: synthetic scripts only, and a
  check by touch later (recommended for R7, with the touch path reported as unverified until a check). B: a
  touch monitor or a tablet first. C: samples recorded on another computer.
- **D2, pen.** A: no pen in R7. B: a pen takes the path of one finger (recommended: the record keeps the
  kind Pen). C: full pen support.
- **D3, location.** A: a folder `Input` in `3DEngine.Core`, and the translator in `3DEngine.Vulkan`
  (recommended: no gate change). B: a new project, with a gate change and an ADR.
- **D4, gestures.** A: the map of section 6, with both the middle drag and Shift and a left drag for pan
  (recommended). B: another map.
- **D5, parity.** A: the same intents from touch and mouse scripts, and a coverage check (recommended). B:
  A, and a helper that compares logs for R8. C: recorded sessions.
- **D6, keyboard alone.** A: not required. B: required for the camera only (recommended). C: required for
  each intent.
- **D7, SDL.** A: stay on the 3.1.3 preview. B: first a task that moves to a binding with SDL 3.2 or later,
  if one exists (recommended: the agent did not check which versions exist).
- **D8, synthetic mouse events.** A: turn the hint off. B: drop the events by `SDL_TOUCH_MOUSEID`. C: both
  (recommended).

**Q3. Selection: a command or a state of the client?** `docs/roadmap.md` (R8) and ADR-0007 disagree.
- A: a state of the client, as ADR-0007 says; the roadmap row of R8 changes (recommended: a selection in one
  client must not change the selection of another).
- B: a command, as the roadmap says; a new ADR amends ADR-0007.

**Q4. The estimate of R7.** The roadmap gives 4 to 7 evenings. The research gives 5 to 7, and 1 to 2 more
for the upgrade of D7. A: accept 5 to 7 (and the upgrade as its own task). B: another estimate.
