---
date: 2026-10-13
commit: 2cc08b6
ledger: v0.70
previous: 2026-10-12-codebase-review.md
---

# Codebase review — 2026-10-13

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the eighth codebase review. The next review must come before ledger entry v0.77: the build fails
when a seventh milestone follows v0.70. `Engine.Tests/Governance/CodebaseReviewGateTests.cs` holds that
rule, and `docs/templates.md`, section 7, gives the form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is
the authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this
file does not.

| Item | Value |
|---|---|
| Code state | `main` at `2cc08b6`, the merge of v0.70 |
| Branch of this review | `codebase-review-2026-10-12b` |
| Tests | 402 on this computer (374 in `Engine.Tests`, 28 in `3DEngine.Tests`) |
| Pipeline | Run 38246787912 on `main`: green on Ubuntu, Windows and macOS |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner
decides.

**Method.** Six tasks changed code after `4a4450d`, the commit of the last review: TASK-0066 (the gates),
TASK-0069 (R3), TASK-0070 (R4), TASK-0071 (R5), and the records of TASK-0065 and TASK-0068. Four review
agents worked in parallel, each one in its own git worktree at `2cc08b6`: the gates with the judge and
the pipeline, the engine with the hosts, the render side, and the research for R6. The agents ran probes
in scratch copies and removed each one. I read each line that this review cites, on disk; several line
numbers in the reports of the agents did not match the files, so this review cites only lines that I
read. I ran again the probe of T48. A line that says "a review agent ran it" means that I read the cited
code and did not repeat the run.

**The date.** I wrote this review on 2026-10-12, the date of the review before it. The gate permits one
review for each date (T53), so the name and the field `date` give 2026-10-13.

## Summary

- **Five findings are fixed in full, and four more for the forms that they named.** T16, T28, T41, T42
  and T43 in full, and T44, T45, T46 and T47 for their forms. **[Observed]**
- **Four files that are not gate files can take `Engine.Tests` out of the test step (T48, high).** A
  `Directory.Solution.targets` with an empty `VSTest` target made `dotnet test` of the solution run no
  test and end with exit code 0; I ran it. The three gates that run engine code and each functional test
  then do not run, and the judge passes the commit. **[Observed]**
- **The gates of TASK-0066 miss new forms (T49, T50, T51, T52).** The determinism gate misses `#if`
  regions, a nested `obj/` folder, an outside `Compile` item, the creators of `System.Numerics` and
  reflection; the dependency gate misses references that MSBuild evaluates; a rename in one parent carries
  a change into a gate file through a clean merge. **[Observed]** in runs of a review agent.
- **The render side of R3 to R5 is sound in its core.** The depth image with three frames in flight ran
  clean under synchronization validation, and an injected weak dependency gave 17 hazards. **[Observed]**
  in runs of a review agent. Eight findings remain, two of them medium: no swapchain is made again after
  OUT_OF_DATE (V8), and the shaders need a Vulkan 1.3 device that nothing checks (V9).
- **The subscription of R5 holds under load, with five small gaps (E61 to E65).** 2,400 commands from 8
  threads gave the listener each event in order. **[Observed]** in runs of a review agent.
- **R6 needs six decisions of the owner.** The demonstration of today shows a box and not a cut, the
  camera does not frame the body, and no Vulkan device can exist with no window. Question Q2 gives the
  options; question Q3 gives a new estimate for R6.
- **Two corrections of earlier texts.** The reviews of 2026-10-11 and 2026-10-12 said that
  `eng/report-failed-tests.js` reads one outcome only, which is false (T13); the progress line of R-0031
  of 2026-10-12 listed T24 as open, which is false.
- **Two owner tasks, no new register entry.** TASK-0073 holds T48 to T53, and TASK-0074 holds E61 to E65
  and V8 to V15. The register stays at 14 of 15.
- **Seventy-four findings are open: no critical, eight high, twenty-eight medium and thirty-eight low.**
  Twenty findings are new. **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A
source line is a line of a `.cs` or `.razor` file that git tracks. The script of this review gives 7,018
production lines in 110 files at `4a4450d`, which are the values of the last review. Since TASK-0069 the
test lines also count `3DEngine.Tests`, which did not exist at the last review.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 13 | `.csproj` lines in `3DEngine.sln`. The last review gave 12; TASK-0069 added `3DEngine.Tests`. Git tracks 14 project files: the native package is not in the solution. |
| Production source lines | 7,973 | Each tracked `.cs` and `.razor` file outside `Engine.Tests` and `3DEngine.Tests`, 121 files. The last review gave 7,018 in 110 files. |
| Test source lines | 9,477 | `Engine.Tests` (9,064 in 70 files) and `3DEngine.Tests` (413 in 5 files). The last review gave 8,739 in 69 files. |
| Tests | 402 | `dotnet test 3DEngine.sln --no-build`: 374 and 28 passed, 0 failed, 0 skipped. The last review gave 353. |
| Gate classes | 17 | Tracked files whose name ends in `GateTests.cs`. Fourteen of them also run in `eng/repo-gates`. The last review gave 17. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 14 | Section "Open" of `docs/register.md`. This review adds none. The last review gave 14. |
| ADRs | 24 | `docs/adr/0001` to `0024`. In force: 22 (11 accepted, 11 amended). |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 8 | The same method: V1, V2, V3, V5, T1, T4, T31, T48. The last review gave 10. |
| Findings: medium | 28 | The same method: E5, E6, E11, E12, E13, E14, E16, E18, E19, E21, E22, E25, E27, E28, E33, V4, V6, V8, V9, T2, T5, T10, T17, T32, T37, T49, T50, T51. The last review gave 28. |
| Findings: low | 38 | The same method: E17, E24, E26, E29 to E32, E34 to E39, E46, E57 to E65, V7, V10 to V15, T7, T13, T23, T25, T52, T53, C1, P1. The last review gave 26. |

## Findings of the previous review

Each line gives the state at `2cc08b6` and the evidence. "No change" means that no file that the
finding cites changed after `4a4450d`, the commit of the previous review.

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
- **E13** — Fixed in part. No change in the cited calls; `3DEngine/SessionBodies.cs` reads the typed result and checks its error. Register entry R-0028.
- **E14** — Open. `Engine.Geometry.Manifold/Native/ManifoldSolidHandle.cs` and `GetBoundingBox` still give memory of `Marshal.AllocHGlobal` to a C++ `delete`. TASK-0028 used the destructor and `Marshal.FreeHGlobal` for the new mesh, and `manifold_destruct_manifold` and `manifold_destruct_box` are exported, so the fix needs no native build. A review agent read the import table of `manifoldc.dll` (the C runtime heap) and ran 100,000 create and dispose cycles with no crash. Register entry R-0028.
- **E15** — Fixed. TASK-0044, v0.37. TASK-0058 and TASK-0038 moved and extended the checks.
- **E16** — Open. No change. Register entry R-0028.
- **E17** — Open in ten parts of thirteen. TASK-0062 changed `Engine.Api.Http/Endpoints/EventsEndpoint.cs` but not the cited parts: its comment still says 1003 (line 18), and it sends 1007 (line 130). Register entry R-0028.
- **E18** — Open. The comment of `Engine.Core/CommandBus.cs` still promises a reset, and `SessionBodies` does not check the sequence either. Register entry R-0033.
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
- **E32** — Open, and wider. `Engine.Core/Queries/GetTessellationQueryHandler.cs` has no catch, and in the desktop a throw now ends the window (E65). Register entry R-0036.
- **E33** — Open. No change. Register entry R-0036.
- **E34** — Open. No change. Register entry R-0036.
- **E35** — Open. No change. Register entry R-0036.
- **E36** — Open in part. Exit code 4 is documented (`Engine.Api.Http.Server/Program.cs:15-17`, `3DEngine/Documentation/Running.md`), but other failures of the start still end each host with a crash (E59). Register entry R-0036.
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
- **V1** — Open. `3DEngine.Vulkan/GraphicsDevice.cs:327` still acquires again on SUBOPTIMAL, and the present path at :379 still does nothing for it; TASK-0069 to TASK-0071 changed the file elsewhere. Register entry R-0022.
- **V2** — Open. `3DEngine.Vulkan/Swapchain.cs:193` still tests `currentExtent.width > 0`; with the special value, the new depth image now fails first. Register entry R-0022.
- **V3** — Open. No source of `3DEngine.Vulkan/` names the portability extension. Register entry R-0022.
- **V4** — Open in four parts of five. A failure of the window still goes to standard output with exit code 0, three results of Vulkan calls are still ignored, the log of the version and the fallback layer lists stay. The desktop now draws the bodies of its session, so that part is fixed. Register entry R-0027.
- **V5** — Open. `3DEngine.Vulkan/GraphicsDevice.cs` still has no check for a null device after the selection loop (a review agent read it; this computer has two suitable GPUs). Register entry R-0030.
- **V6** — Open, now observed. A review agent made a `VkUtf8String` from a `byte[]` and saw it follow the array when the array changed; `GraphicsDevice.cs` still calls `GetBytes` for the extensions. Register entry R-0030.
- **V7** — Open in each part, and wider: more constructors now leak when they fail part way (`GpuBuffer`, `GpuMesh`, `RenderPipeline`, `Swapchain`). Register entry R-0030.
- **T1** — Open in one part of three. No gate compares the native output of two platforms. R-0023.
- **T2** — Open in one part of five. No change. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. TASK-0057 kept the rules in the judge.
- **T4** — Open in two parts of four. The dispatch gate still misses the desktop host (`Engine.Tests/Hosting/DispatchSurfaceGateTests.cs:20`), and the marker gate accepts a closed identifier. The determinism part is fixed for its named forms (TASK-0066); T49 gives new forms. Register entry R-0031.
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
- **T31** — Open in part, and wider through T48: the three gates that run engine code stay in the test process, and T48 can take them out of the test step. R-0031.
- **T32** — Fixed in part. A branch is judged from its merge base with `main`. On `main` the job judges the commits of the push only (`.github/workflows/ci.yml:199`), so a green later push still hides a red earlier push there. R-0031.
- **T33** — Fixed. The judge reads the task files at the commit. TASK-0057, v0.54.
- **T34** — Fixed. The documented check joins its two commands with `&&`. TASK-0057, v0.54.
- **T35** — Fixed. A failed git command is a failure, the judge reads the paths with `-z`, `WriteSetRangeTests` covers the steps, a task file with no front matter at the commit fails, and `docs/templates.md` describes the new steps. TASK-0057, v0.54.
- **C1** — Open in two parts. `IBRepOps.cs` and `IFeatureIdMap.cs` remain, and no production code reads `BackendCapabilities`; TASK-0028 added a fifth flag. Register entry R-0027.
- **P1** — Open, and wider. Nothing is measured in the repository. The render loop now waits for the serial section of the session in `SessionBodies` (reading only). Register entry R-0027.
- **E48** — Fixed. TASK-0062, v0.62. A review agent compared 49 forms with the parser of Kestrel: no form that the check accepts binds a foreign address or each interface.
- **E49** — Fixed for a WebSocket subscriber. TASK-0062, v0.62: a review agent measured 0.00 s before the subscribe frame and 0.02 s for the desktop with one subscriber. An open HTTP request still holds the stop (E58).
- **E50** — Fixed by reading. TASK-0062, v0.62: the loop of `3DEngine/DesktopEngineHost.cs` ends on `Stopping`. Nobody sent Ctrl+C to a desktop process.
- **E51** — Fixed for a port in use. TASK-0062, v0.62: exit code 4 from each host (a review agent ran both). A port that the system refuses is E59.
- **E52** — Fixed. TASK-0062, v0.62. A review agent saw the backend disposed after `Dispose` with no request.
- **E53** — Fixed. TASK-0062, v0.62. The clause is gone (`Engine.Core/CommandBus.cs:184`), and a test gives one handle two times.
- **E54** — Fixed. TASK-0062, v0.62. A review agent ran `--port`, `--port=` and `Desktop__Port`; the variable `PORT` has no effect.
- **E55** — Fixed. TASK-0062, v0.62. The messages say "The surface".
- **E56** — Fixed. TASK-0062, v0.62. `EngineHost.Backend` is internal.
- **E57** — Open in part. No test calls `DesktopSurface` or `SessionBodies`; register entry R-0039.
- **T36** — Fixed. TASK-0061, v0.61. A review agent removed the union and two tests failed. T42 is a nearby form.
- **T37** — Open in part. TASK-0061 added the tests of the staged check and of unrelated history. A failed read of a test file and the steps of the workflow still have no test (a review agent turned the failure into a skip, and no test failed). R-0031.
- **T38** — Fixed. TASK-0063, v0.60: the progress line of R-0031, and R-0038 is accepted.
- **T39** — Fixed. TASK-0061, v0.61. The fallback left `.github/workflows/ci.yml`.
- **T40** — Fixed. TASK-0061, v0.61. The test gap of a failed read of a test file is in T37.
- **E58** — Open. No change. TASK-0067.
- **E59** — Open. `3DEngine/DesktopEngineHost.cs:16` still has no `try` around the start of the surface. TASK-0067.
- **E60** — Open. No change. TASK-0067.
- **T41** — Fixed. TASK-0066, v0.67. A review agent planted a `global.json` with a message: `dotnet build` from its folder stopped, and the judge and the gate program, run from the same folder, ignored it.
- **T42** — Fixed. TASK-0066, v0.67. A review agent resolved a conflict to the side version, and the judge failed the merge; `WriteSetRangeTests` holds it. T51 gives a form with a rename.
- **T43** — Fixed. TASK-0066, v0.67. `WriteSetRangeTests` holds it.
- **T44** — Fixed for its forms. TASK-0066, v0.67: `--gate-sources` gave "main" for the files that are not gate files. T48 gives other inputs.
- **T45** — Fixed for its forms. TASK-0066, v0.67: a review agent saw each of the eight forms fail. T49 gives new forms.
- **T46** — Fixed for its forms. TASK-0066, v0.67 (reading only). T50 gives new forms.
- **T47** — Fixed for its forms. TASK-0066, v0.67: a theory with no row fails. T52 gives a new form.

## 1 Current architecture

**[Observed]** The desktop host draws each live body of the session that it owns. `EngineHost.Subscribe`
gives it the events in its process, `SessionBodies` fetches each mesh with `GetTessellation`, and each
entity of `3DEngine.Core` has an origin in double precision (ADR-0024). The render loop walks the scene
graph into a draw list relative to the eye of an orbit camera, and `3DEngine.Vulkan` draws each record
with reversed depth (ADR-0023). The shaders have their GLSL source, their SPIR-V and a lock of hashes
(ADR-0022).

**[Observed]** The judge and the gate program come from `main` and are built outside the judged checkout
(TASK-0066). The three gates that run engine code stay in the test process.

**[Observed]** A Vulkan device needs a window: `GraphicsDevice` takes a `Window`, creates a surface and
the swapchain, and `RenderPipeline` uses the render pass of the swapchain.

## 2 Patterns in use

- **Each step of the render side is seen in a picture.** The owner accepted pictures of R3, R4 and R5 that
  I took on the order of the owner. No test draws a frame yet. **[Observed]**
- **A gate of text, and then a gate of structure, each with a new escape.** T49 and T50 are the sixth and
  seventh forms that a gate of the repository misses after a correction (T4, T16, T45, T46). The gates
  that read MSBuild files by hand cannot see what MSBuild evaluates. **[Observed]**
- **A file outside the list of gate files changes what the pipeline runs.** T48 follows T9, T41 and T44:
  each correction named the files of its finding, and a nearby file stayed out. **[Observed]**

## 3 Review of the code

The earlier findings keep their headings and their evidence in the earlier reviews. The section
"Findings of the previous review" gives the state of each one. This section gives the new findings.

### The hosts and the engine

#### E61 · Low · The live bodies enter the queue after newer events, and the desktop warns for a body that a command consumes before the next frame · Confirmed by a run

`3DEngine/SessionBodies.cs:28` subscribes inside the read of the session, and `:31` puts the live bodies in
the queue only after the read. A review agent ran a copy of the constructor beside a loop of translations:
in 192 of 740 trials the queue held the consumption of a body before its creation. A body that a command
creates and consumes before the next frame gives "[WARN] No mesh for body" (`:60`). The scene is correct;
the warnings are false. **[Observed]** **[Recommended]** Put the live bodies in the queue inside the read,
and do not warn for `E-GEOM-BODY-NOT-FOUND`. Owner: TASK-0074.

#### E62 · Low · An action of a subscription that throws is removed with no report · Confirmed by a run

`Engine.Api.Http/WebSockets/EventBroadcaster.cs:127-129` removes the action and writes nothing. A review
agent made an action that calls the session: the guard of E20 refused it, and the action was removed in
silence after one event. The window of the desktop would stop following the session with no message.
**[Observed]** **[Recommended]** Report the removal with a registered warning code. Owner: TASK-0074.

#### E63 · Low · An action receives the payload that the ring keeps, and can change it · Confirmed by a run

`Engine.Core/CommandBus.cs` makes each payload as a mutable `Dictionary`, and the broadcaster gives the
same record to each action and to the replay of `/events`. A review agent set `bodyId` to an empty
`Guid` in an action, and the ring then held the changed value. **[Observed]** **[Recommended]** A read-only
payload. Owner: TASK-0074.

#### E64 · Low · The disposal of a subscription removes by delegate and does not stop an event in flight · Confirmed by a run

The `Listening` class (`EventBroadcaster.cs:139`) removes the action by delegate equality
(`EventBroadcaster.cs:136`). A review agent subscribed one static method two times and disposed the first
subscription two times: the second subscription lost its events. An event of a commit in progress also
arrived after the disposal returned. The comment of `EngineHost.Subscribe` promises the events "until
the result is disposed". **[Observed]** **[Recommended]** One entry for each subscription, removed once,
and a comment that an event in progress can still arrive. Owner: TASK-0074.

#### E65 · Low · A failure in `SessionBodies.Apply` ends the window and the session with exit code 0 · Confirmed by reading

`3DEngine/NativeThreeDEngine.cs:72` calls `Apply` with no catch, and a throw of the tessellation (E32) or of
an allocation in `GpuBuffer` ends the loop; the host then writes the error and returns 0 (V4). The change
already left the queue. **[Inferred]** **[Recommended]** Catch a failure for each body, warn, and continue.
Owner: TASK-0074.

### The render side

#### V8 · Medium · No swapchain is made again after OUT_OF_DATE · Confirmed by reading

`3DEngine.Vulkan/GraphicsDevice.cs:327-331` acquires again after SUBOPTIMAL or OUT_OF_DATE, and the present
path at `:379-382` does nothing; the swapchain, its framebuffers and the depth image keep the extent of their
creation. A minimise, or a move to a screen of another scale (the window has `HighPixelDensity`,
`3DEngine.Vulkan/Window.cs:32`), can make the surface out of date, and each later frame then fails with no
message. V1 covers SUBOPTIMAL only. **[Inferred]**; no window was run. **[Recommended]** Make the swapchain,
the framebuffers and the depth image again, and skip a frame with an extent of 0. Owner: TASK-0074.

#### V9 · Medium · The shaders need a Vulkan 1.3 device, and nothing checks the device · Confirmed by a run

`3DEngine.Vulkan/Shaders/compile.js:24` compiles for Vulkan 1.3, which gives SPIR-V 1.6 (a review agent read
the version with `spirv-dis`). The selection of the device reads no API version, and the depth image uses
`D32_SFLOAT` (`3DEngine.Vulkan/DepthBuffer.cs`) with no query of the format. A device below 1.3 cannot create
the modules. That is a second computer or a deferred platform (R-0022, R-0030). **[Observed]** for the
version; **[Inferred]** for the effect. **[Recommended]** Compile for Vulkan 1.1, or refuse a device below
the target with a message, and query the depth format. Owner: TASK-0074.

#### V10 · Low · A mirrored transform shows a body inside out · Confirmed by a run

`3DEngine.Vulkan/RenderPipeline.cs:21-22` always culls the back face of a counter-clockwise front face. A
negative scale gives a model matrix with a negative determinant (a review agent computed -1), so the
culling keeps the inside. No code sets a negative scale today. **[Observed]** for the determinant.
**[Recommended]** Refuse a negative scale, or set the front face for each draw. Owner: TASK-0074.

#### V11 · Low · The parameters of the camera are not checked · Confirmed by a run

A review agent saw: a near plane of 0 gives depth 0 for each point, so nothing is drawn; an aspect of 0
gives an infinite scale; `Camera.Zoom` throws from `Math.Clamp` (`3DEngine.Core/Models/Camera.cs:52`) when
two times the near plane is greater than half the far plane, and that throw in `HandleEvent` ends the
desktop with exit code 0. No caller sets such values today. **[Observed]** **[Recommended]** Check the
near plane, the far plane and the aspect. Owner: TASK-0074.

#### V12 · Low · The camera cannot reach a body far from the origin of the scene · Confirmed by reading

The target of the camera stays at the origin (`3DEngine/DesktopEngineHost.cs:32` sets only the distance and
the angles), and `Zoom` holds the distance under half the far plane. A body whose centre is more than about
1,000 units away never appears, with no message; the double origin of ADR-0024 shows only in the unit
tests. **[Inferred]** **[Recommended]** Frame the live bodies (question Q2, D5). Owner: TASK-0074, or the task
of R6.

#### V13 · Low · No test holds the interface between the shaders and the pipeline · Confirmed by reading

No test links the push constant block of `mesh.vert` (two matrices at offsets 0 and 64) to
`RenderPipeline.PushConstantSize` and to the order of the two matrices in `RenderPipeline.Push`; a swap of
the two matrices passes each test (reading). The host turns on the core validation layer only, and a review
agent showed that synchronization validation sees a weak dependency of the render pass. **[Inferred]**
**[Recommended]** Read the decorations of the SPIR-V in a test, and run synchronization validation in the
image test of R6. Owner: TASK-0074, or the task of R6.

#### V14 · Low · A shader source with a byte order mark fails the test of the lock for good · Confirmed by a run

`3DEngine.Vulkan/Shaders/compile.js:42` reads the source with its byte order mark, and
`3DEngine.Tests/ShaderSourceTests.cs:23` reads it without the mark, so the two hashes differ even directly
after a compile. A review agent saw the hashes differ. This gives a false failure, not a false pass.
**[Observed]** **[Recommended]** Remove a leading byte order mark in `compile.js`, or hash the bytes on both
sides. Owner: TASK-0074.

#### V15 · Low · Costs in the render loop · Confirmed by reading

`3DEngine/SessionBodies.cs:79` waits for the device to be idle for each consumed body; `GpuBuffer`
(`3DEngine.Vulkan/GpuBuffer.cs:32`) takes the first memory type that the host sees, which on this computer
is system memory, so the GPU reads each vertex over the bus at each frame; each buffer has its own
allocation; with the Mailbox present mode the loop has no frame limit. Nothing is slow at the size of
today. **[Inferred]** **[Recommended]** Measure first (P1), then defer destruction to the fence of the frame
and use memory of the device with a staging copy. Owner: TASK-0074.

### Tests, gates and the pipeline

#### T48 · High · Four files that are not gate files can take `Engine.Tests` out of the test step · Confirmed by a run

- **Evidence.** The step "Test" runs `dotnet test --no-build` on the solution
  (`.github/workflows/ci.yml:93`) and counts no tests. `Judge.IsGateFile` and `Judge.IsBuildInput`
  (`eng/write-set-check/Judge.cs:103-111`) do not cover `3DEngine.sln`, `Directory.Solution.props` and
  `.targets`, `after.3DEngine.sln.targets` and `before.3DEngine.sln.targets`, or
  `Engine.Tests/Engine.Tests.csproj.user`, which MSBuild imports. I put a `Directory.Solution.targets` with
  an empty target `VSTest` at the root and ran `dotnet test 3DEngine.sln --no-build`: it printed only the
  message of the probe and gave exit code 0. A review agent also removed `Engine.Tests` from the solution,
  and removed a gate from the compilation with a `.csproj.user` file; the judge passed each commit.
- **Impact.** Each functional test and the three gates that run engine code stop, on the three runners,
  with a green run. The gates of the repository still run from `main`.
- **[Recommended]** Make each of these files a gate file, and make the step fail below a minimum count of
  tests that `main` gives. Owner: TASK-0073.

#### T49 · Medium · The determinism gate misses five forms that compile · Confirmed by a run

`Engine.Tests/Governance/DeterminismCallGateTests.cs:65` parses with no preprocessor symbol, `:75` skips a
path with `/obj/` or `/bin/` at any depth, and the gate reads the folders of the three projects only and
checks names only. A review agent compiled, and the gate program of `main` passed: `Math.Sin` inside
`#if NET`, a file `Engine.Core/Geometry/obj/Trig.cs`, a `Compile Include` of a file outside the project,
`Matrix4x4.CreateRotationZ` and `Complex.FromPolarCoordinates`, and `typeof(Math).GetMethod(...)`. The log
path uses none of these today. **[Observed]** **[Recommended]** Parse with the symbols of each
configuration, read the evaluated `Compile` items, and forbid the creators of `System.Numerics` that call
a transcendental function, and reflection on `Math`. Owner: TASK-0073.

#### T50 · Medium · The dependency gate misses references that MSBuild evaluates · Confirmed by a run

`Engine.Tests/Governance/DependencyDirectionGateTests.cs:39-45` takes each `Include` as text, so
`$(RenderKernel)` becomes a name that no rule matches; the gate skips `obj/`, where MSBuild imports the
extension files of a project; it reads `Import` elements only. A review agent gave `Engine.Tests` and
`Engine.Contracts` a reference to `3DEngine.Core` in three ways (a property in a `.user` file, a file in
`obj/`, and `CustomAfterMicrosoftCommonTargets`), and `dotnet msbuild -getItem:ProjectReference` listed each
reference while the gate passed. **[Observed]** **[Recommended]** Read the evaluated references, or refuse
each `Include` with `$(` or `@(` and each property that names a file of targets. Owner: TASK-0073.

#### T51 · Medium · A rename in one parent carries a change into a gate file through a clean merge · Confirmed by a run

`eng/write-set-check/Judge.cs:277` runs `git merge-tree` with the detection of renames on. A review agent
renamed `docs/foo.cs` to `Engine.Tests/Governance/FooGateTests.cs` on a side, under an exact permit, and
changed `docs/foo.cs` on `main` under `docs/**`; the merge put the change of `main` into the gate file, and
the judge passed it as "a merge with no change of its own". With `-X no-renames` the file is a conflict, and
the judge reads the merge. **[Observed]** **[Recommended]** Run `merge-tree` with `-X no-renames`, after a
check of the version of git on the runner. Owner: TASK-0073.

#### T52 · Low · The runner of the gates passes an `async void` check that fails after an await · Confirmed by a run

`eng/repo-gates/Program.cs:70` accepts a method that returns `void`, and an `async void` method returns at
its first await. A review agent made such a probe gate: the program printed "pass" and exit code 0. No gate
of today has this form. **[Observed]** **[Recommended]** Refuse a method with
`AsyncStateMachineAttribute` that returns `void`. Owner: TASK-0073.

#### T53 · Low · The review gate permits one codebase review for each date · Confirmed by reading

`Engine.Tests/Governance/CodebaseReviewGateTests.cs:24-25` reads the date from the name of the file only,
and `:130` fails a review whose date is not later than the date of the review before it. A second review
on one day therefore needs a false date; this review carries the next day (section "The date").
**[Observed]** **[Recommended]** Permit a suffix after the date, and compare the ledger versions in place of
the dates. Owner: TASK-0073.

## 4 The next planned task

**[Observed]** The roadmap gives R6: "Observe a cut. The live body set of ADR-0021 gives the result. A
headless image test." No task file for R6 exists. **[Recommended]** TASK-0073 comes first as P0.28, because
T48 lets a commit stop each functional test with a green run (question Q1).

## 5 Research for that task

**[Observed]** The demonstration of the probes moves the second box by (5, 0, 0), and the cut body is a box
of 5 × 10 × 10 with 8 vertices and 12 triangles: a person cannot tell it from a box made directly. With
(5, 5, 5), a review agent saw 14 vertices and 24 triangles and a clear notch.

**[Observed]** The camera does not frame the body: the cut body covers about 2.7 % of the picture in a
probe of a review agent, and the shading has one colour, so a face parallel to an outer face has the same
shade.

**[Observed]** No Vulkan device can exist with no window: `GraphicsDevice` takes a `Window`, needs present
support and builds the swapchain, and `RenderPipeline` uses the render pass of the swapchain. A review agent
built a probe with no window on the GPU of this computer: a device with no surface, an offscreen colour and
depth image, the same pipeline state and SPIR-V, and a copy to memory that the host reads. Two runs gave the
same SHA-256 of the pixels.

**[Observed]** This computer has no software Vulkan driver. The image of `ubuntu-latest` lists none, and
the package `mesa-vulkan-drivers` of Ubuntu gives lavapipe; the image of `windows-latest` lists none, and
the options there need a download; on `macos-latest` no usable device is known. ADR-0022 item 3 says that
the tests of `3DEngine.Tests` create no Vulkan object, and `NativeManifoldFactAttribute` cannot skip in the
pipeline.

## 6 Recommended approach

1. **[Recommended]** TASK-0073: T48 first (the gate files and a minimum count of tests), then T51, T49, T50,
   T52 and T53.
2. The task of R6, with the answers of the owner to Q2 and Q3. Its first commit writes the decisions as an
   ADR.
3. TASK-0074 (E61 to E65, V8 to V15) after R6, or the parts that R6 needs inside it (V12, V13).

## Questions for the owner

**Q1. Must TASK-0073 come before R6?** T48 lets a commit stop each functional test and three gates with a
green run. Option A: TASK-0073 first, as P0.28, about 1 to 2 evenings. Option B: R6 first. Recommendation:
option A.

**Q2. Six decisions for R6.** Each one goes into an ADR in the first commit of R6.

- (D1) **The demonstration.** Option A: move the second box by (5, 5, 5), so the cut shows a notch. Option
  B: keep (5, 0, 0) and show the operands as ghosts, which needs a history that no task plans.
  Recommendation: A.
- (D2) **Where the image test lives.** Option A: a new project `3DEngine.ImageTests` in a new class of the
  dependency gate; it may reference what a host that draws references, and the internals of `3DEngine`, so
  it also tests `SessionBodies` and closes R-0039. Option B: amend ADR-0022 item 3, so that
  `3DEngine.Tests` creates Vulkan objects, with a fixed mesh. Recommendation: A.
- (D3) **Which runners run it.** Option A: install lavapipe on Ubuntu and require the test there; on Windows
  and macOS it skips with a reason in the register. Option B: require it on each runner, which needs a
  software driver for Windows and a decision for macOS. Option C: a local test only. Recommendation: A.
- (D4) **How the images compare.** Option A: a structural check (the background, the silhouette that the
  CPU projects from the tessellation, and the region of the cut), with the picture kept as an artifact on a
  failure. Option B: exact bytes for each driver. Recommendation: A.
- (D5) **The frame of the camera.** Option A: the camera frames the live bodies at each change. Option B:
  once at the start. Recommendation: A, which also corrects V12.
- (D6) **How the demonstration runs again.** Option A: a Node.js script in the repository that sends the
  commands over HTTP, which an agent can also run. Option B: a start option of the desktop.
  Recommendation: A.

**Q3. The estimate of R6.** The roadmap gives 4 to 6 evenings. The split of the device from the window,
the new test project and the software driver make 6 to 8 more likely. Option A: change it to 6 to 8.
Option B: keep it. Recommendation: A.
