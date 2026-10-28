---
date: 2026-10-28
commit: e0a2c98
ledger: v0.88
previous: 2026-10-17-codebase-review.md
---

# Codebase review — 2026-10-28

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the tenth codebase review. The review of 2026-10-17 examined v0.76. After it came 4 product
milestones (R7, R7b, R8a, R8b) and 12 ledger entries (v0.77 to v0.88), so the next entry had to be a review
(TASK-0083). The next review is due when more than four product milestones or more than twelve entries follow
v0.88. `Engine.Tests/Governance/CodebaseReviewGateTests.cs` holds that rule, and `docs/templates.md`, section
7, gives the form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is the
authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this file does not.

| Item | Value |
|---|---|
| Code state | `main` at `e0a2c98`, the merge of v0.88 |
| Branch of this review | `codebase-review-2026-10-28` |
| Tests | 636 on this computer (507 in `Engine.Tests`, 89 in `3DEngine.Tests`, 40 in `3DEngine.ImageTests`) |
| Pipeline | Run 40658502430 on the last commit of v0.88: green on Ubuntu, Windows and macOS |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner decides.

**Method.** Ten tasks changed code or records after `2b8e928`, the commit of the last review: TASK-0078 (the
gates), TASK-0055 (the event schema), TASK-0083 (the review rule), TASK-0081 (SDL), TASK-0082 (R7), TASK-0040
(the ADR archive), TASK-0079 (the hosts and the render side), TASK-0084 (R7b), TASK-0085 (R8a) and TASK-0086
(R8b). Four review agents worked in parallel, each one in its own git worktree at `e0a2c98`: the gates with the
judge and the pipeline, the engine with the hosts, the render side with the desktop client, and the research for
P8a. The agents ran probes in their worktrees and removed each one. I read each line that this review cites, on
disk. I ran T62 again in a scratch worktree. A line that says "a review agent ran it" means that I read the
cited code and did not repeat the run.

## Summary

- **Each fix of the period held for the forms that it named.** E66 to E71, V16 to V24, T55 to T61, and E46
  held against the probes of the agents. A stop of a host now waits for a running command before the backend
  goes. **[Observed]**
- **A file that is not a gate file can make each failed test pass, and the exact count stays (T62, high).** A
  test framework of xUnit in a support file turns each failure into a pass. I ran it: the dispatch gate failed
  without the file and passed with it, and the gate program passed. **[Observed]**
- **The desktop has two medium defects.** A click on the scene does not end a text field, so the keys go to the
  field and Rename can rename the wrong body (V26). Each frame costs time in the square of the count of bodies
  (V27). **[Observed]** in runs of a review agent.
- **Three gates read text, and forms pass them.** A project under a `bin/` folder hides from the dependency and
  determinism gates (T63), and four forms pass the gate of the UI library (T64). **[Observed]** in runs of a
  review agent.
- **The research for P8a gives seven decisions for the owner.** No file log exists. ADR-0015 disagrees with P8a
  in each of its main points. KD3 and KD4 can each change the public shape of `Engine.Contracts`, which is a
  stop-and-ask condition. The research gives 5 to 7 evenings, and the roadmap gives 3 to 5. **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks. The script of this review gives 8,664 production
lines in 127 files and 10,608 test lines at `2b8e928`, which are the values of the last review.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 14 | `.csproj` lines in `3DEngine.sln`. The last review gave 14. Git tracks 15 project files: the native package is not in the solution. |
| Production source lines | 11,494 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests` and `3DEngine.ImageTests`, 147 files. The last review gave 8,664 in 127 files. |
| Test source lines | 13,311 | `Engine.Tests` (10,957 in 77 files), `3DEngine.Tests` (1,355 in 12 files) and `3DEngine.ImageTests` (999 in 8 files). The last review gave 10,608. |
| Tests | 636 | `dotnet test 3DEngine.sln`: 507, 89 and 40 passed, 0 failed, 0 skipped. The last review gave 461. The Windows and macOS runners execute 38 tests of `3DEngine.ImageTests` (`eng/test-floor.txt`). |
| Gate classes | 19 | Tracked files whose name ends in `GateTests.cs`. Sixteen of them also run in `eng/repo-gates`. The last review gave 17, with 14 in the gate program. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 14 | Section "Open" of `docs/register.md`; the limit is 15. This review adds none. The last review gave 15. |
| ADRs | 27 | `docs/adr/0001` to `0029`, with 0003 and 0011 in `docs/adr/archive/` (TASK-0040). In force: 26 (13 accepted, 13 amended); ADR-0015 is proposed. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 6 | The same method: V2, V3, T1, T4, T31, T62. The last review gave 7. |
| Findings: medium | 26 | The same method: E5, E6, E11, E12, E13, E14, E16, E18, E19, E21, E22, E25, E27, E28, E33, V4, V26, V27, T2, T5, T10, T17, T32, T37, T63, T64. The last review gave 26. |
| Findings: low | 32 | The same method: E17, E24, E26, E29 to E32, E34, E35, E37 to E39, E57, E72 to E74, V7, V15, V18, V25, V28 to V30, T7, T13, T23, T25, T65 to T67, C1, P1. The last review gave 41. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found.

## Findings of the previous review

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. `Engine.Api.Http/Endpoints/JsonParameters.cs:43-44` is unchanged. Register entry R-0027.
- **E6** — Open, and wider. The command line can never find a body, so `RenameBody`, `SetBodyVisibility` and
  `GetBodyInfo` can never succeed there either; `Engine.Tests/Cli/CliBodyNameTests.cs` records it as the
  behaviour of a one-shot session (ADR-0019 item 7). `/schema/events` still lists five kinds that no source
  emits (`Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs:48-53`). Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. No change.
- **E8** — Fixed. TASK-0044, v0.37. No change.
- **E9** — Fixed, and it holds. No change.
- **E10** — Fixed. TASK-0035, v0.43. No change.
- **E11** — Open. `Engine.Core/Hosting/ParameterBinder.cs` still accepts "Infinity" and "NaN" for a number. Register entry R-0028.
- **E12** — Open. No change. Register entry R-0027.
- **E13** — Fixed in part. No change. Register entry R-0028.
- **E14** — Open. No change. Register entry R-0028.
- **E15** — Fixed. TASK-0044, v0.37. No change.
- **E16** — Open. No change. Register entry R-0028.
- **E17** — Open in ten parts of thirteen. No change. Register entry R-0028.
- **E18** — Open. The comment of `Engine.Core/CommandBus.cs:96-102` still promises a reset. Register entry R-0033.
- **E19** — Open. No change. Register entry R-0033.
- **E20** — Fixed, and it holds. No change.
- **E21** — Open. No change. Register entry R-0033.
- **E22** — Open. No change. Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. No change.
- **E24** — Open in seven parts of nine. No change. Register entry R-0033.
- **E25** — Open. `Engine.Core/CommandBus.cs:78-80` still registers the bus of a replay. Register entry R-0036.
- **E26** — Open. `Engine.Contracts/Document.cs` still returns the list itself; TASK-0085 added `ReplaceBody` only. Register entry R-0036.
- **E27** — Open. No change. Register entry R-0036.
- **E28** — Open. No change. Register entry R-0036.
- **E29** — Open. No change. Register entry R-0036.
- **E30** — Open. No change. Register entry R-0036.
- **E31** — Open. TASK-0085 corrected a false comment in `Engine.Core/Commands/TranslateCommand.cs`; nobody checked each cited comment again. Register entry R-0036.
- **E32** — Open, and smaller. No change. Register entry R-0036.
- **E33** — Open. No change. Register entry R-0036.
- **E34** — Open. No change. Register entry R-0036.
- **E35** — Open. No change. Register entry R-0036.
- **E36** — Fixed. E68 was its last part: `engine-api-http` gives exit code 5 for a configuration file that it cannot read (TASK-0079, v0.85).
- **E37** — Open. No change. Register entry R-0036.
- **E38** — Open. No change. Register entry R-0036.
- **E39** — Open. The backend choice still exists three times; the desktop one is now `3DEngine/DesktopSurface.cs:96`. Register entry R-0036.
- **E40** — Fixed. TASK-0058, v0.56. No change.
- **E41** — Fixed. TASK-0058, v0.56. No change.
- **E42** — Fixed as recommended. TASK-0058, v0.56. No change.
- **E43** — Fixed as recommended. TASK-0058, v0.56. No change.
- **E44** — Fixed. TASK-0058, v0.56. No change.
- **E45** — Fixed. TASK-0058, v0.56. No change.
- **E46** — Fixed. TASK-0055, v0.80: each kind gives its payload fields, and `EventPayloadSchemaTests` covers `body.changed`. The inner fields of `snapshot` are only "object" (`SchemaEventsEndpoint.cs:56`).
- **E47** — Fixed. TASK-0058, v0.56. No change.
- **E48** — Fixed. TASK-0062, v0.62. No change.
- **E49** — Fixed for a WebSocket subscriber. TASK-0062, v0.62. No change.
- **E50** — Fixed by reading. TASK-0062, v0.62. No change.
- **E51** — Fixed for a port in use. TASK-0062, v0.62. No change.
- **E52** — Fixed. TASK-0062, v0.62. No change.
- **E53** — Fixed. TASK-0062, v0.62. No change.
- **E54** — Fixed. TASK-0062, v0.62. No change.
- **E55** — Fixed. TASK-0062, v0.62. No change.
- **E56** — Fixed. TASK-0062, v0.62. No change.
- **E57** — Open. No test calls `DesktopSurface` or `DesktopEngineHost`; `BodyActions` has tests in `3DEngine.ImageTests` only. Register entry R-0039.
- **E58** — Fixed. TASK-0067, v0.76. No change.
- **E59** — Fixed for the start. TASK-0067, v0.76; E68 closed the last form.
- **E60** — Fixed. TASK-0067, v0.76. No change.
- **E61** — Fixed. TASK-0074, v0.75. No change.
- **E62** — Fixed. TASK-0074, v0.75. No change.
- **E63** — Fixed. TASK-0074, v0.75. No change.
- **E64** — Fixed. TASK-0074, v0.75. No change.
- **E65** — Fixed. TASK-0074, v0.75. No change.
- **E66** — Fixed. TASK-0079, v0.85: `Engine.Core/DocumentSession.cs:154-160` closes the session, `:174-185` refuses a later command, and `Engine.Api.Http/EngineHost.cs:82-86` closes it before the backend goes. A review agent ran a slow `CreateBox`, a queued command, a queued read and a stop: the backend went after the command, and the two queued calls each got "DocumentSession is closed". The client of the slow command gets no answer, because the container is disposed when the reply is written.
- **E67** — Fixed. TASK-0079, v0.85: `Engine.Api.Http/WebSockets/EventBroadcaster.cs:144` calls `Announce`, which reports on the thread pool with a catch.
- **E68** — Fixed. TASK-0079, v0.85: `Engine.Api.Http.Server/Program.cs:27-36` and `:48-56` give exit code 5, and a process test holds it.
- **E69** — Fixed. TASK-0079, v0.85. The fragment is gone.
- **E70** — Fixed. TASK-0079, v0.85: `Engine.Api.Http/EngineSurface.cs:30-37` gives 3.01 s to 3.02 s.
- **E71** — Fixed for its forms. TASK-0079, v0.85: the resolve is in the `try` (`3DEngine/DesktopSurface.cs:110`), and `Program.cs:101` disposes before it returns. E74 gives two more paths. No test.
- **V1** — Fixed. TASK-0074, v0.75. No change.
- **V2** — Open. `3DEngine.Vulkan/Swapchain.cs:113` still tests `currentExtent.width > 0`. Register entry R-0022.
- **V3** — Open. No source of `3DEngine.Vulkan/` names the portability extension. Register entry R-0022.
- **V4** — Open in four parts of five, and smaller. The submit and the fence wait are checked now. A failure of the window still goes to standard output with exit code 0 (`3DEngine/DesktopEngineHost.cs:85-91`), `vkCreateFramebuffer` is not checked (`Swapchain.cs:87`), the log gives the requested version (`GraphicsDevice.cs:160`), and the fallback layer lists stay. Register entry R-0027.
- **V5** — Fixed. TASK-0076, v0.74. No change.
- **V6** — Fixed. TASK-0076, v0.74. No change.
- **V7** — Open, and wider. The new code repeats the defect: the constructors of `UiRenderer` (`3DEngine.Vulkan/Ui/UiRenderer.cs:44-139`), `UiTexture` (`UiTexture.cs:42-84`) and `GpuBuffer` (`GpuBuffer.cs:15-43`, changed by TASK-0084) have no `try`, `CreateTexture` leaks its texture when the descriptor set fails (`UiRenderer.cs:161-165`), and the highlight pipeline uses the pattern of `RenderPipeline`. Register entry R-0030.
- **V8** — Fixed. TASK-0074, v0.75. No change.
- **V9** — Fixed. TASK-0074, v0.75. No change.
- **V10** — Fixed. TASK-0074, v0.75. No change.
- **V11** — Fixed in part. TASK-0074, v0.75; TASK-0079 closed the parts of V22.
- **V12** — Fixed, and changed by decision. The camera now frames the bodies only when the visible set goes from empty to not empty (KD6, TASK-0086).
- **V13** — Fixed for its parts. TASK-0074 and TASK-0076. V24 is fixed.
- **V14** — Fixed. TASK-0074, v0.75. No change.
- **V15** — Open by decision, and wider. `3DEngine/SessionBodies.cs:139` still waits for the device for each consumed body; V27 adds a cost to each frame.
- **V16** — Fixed. TASK-0079, v0.85: the setting `Desktop:SynchronizationValidation` reaches `3DEngine.Vulkan/GraphicsDevice.cs:93-99`, and the ledger of v0.85 records a run with no message.
- **V17** — Fixed. TASK-0079, v0.85: the callback keeps the messages (`GraphicsDevice.cs:447-448`), and the image tests put them in the assertion.
- **V18** — Open in part. TASK-0079 makes the new swapchain first (`3DEngine.Vulkan/Presenter.cs:185-211`). A retry after a failed remake passes `old.Handle` again (`:195`), although the comment at `:181-183` says that the driver retired it; the rule VUID-VkSwapchainCreateInfoKHR-oldSwapchain-01933 forbids a retired swapchain there. The constructor makes the depth image (`Swapchain.cs:42`) before the swapchain, so a throw leaks it. **[Observed]** by reading; no run forced the failure. **[Recommended]** Pass a null handle after a failed remake, and free the depth image on a throw.
- **V19** — Fixed, with no injection. TASK-0079, v0.85: `Presenter.cs:150-152` checks the submit, and each wait has a limit.
- **V20** — Fixed. TASK-0079, v0.85: `Swapchain.ChooseCompositeAlpha`, with a test.
- **V21** — Fixed by reading, with no test. TASK-0079, v0.85.
- **V22** — Fixed for its forms. TASK-0079, v0.85. V30 gives a new form.
- **V23** — Fixed. TASK-0079, v0.85: `Window.HasNoArea` and a wait for an event.
- **V24** — Fixed. TASK-0079, v0.85: the comment of `3DEngine.Tests/PushConstantInterfaceTests.cs:7-15` is correct.
- **V25** — Open in part, and wider. TASK-0081 moved to SDL 3.2.10. No package of the desktop gives `linux-arm64`: SDL does not, and ImGui.NET does not either (ADR-0027). ImGui.NET also copies `runtimes/win-x86/native/cimgui.dll` into the output of the desktop; the gate of rule 5 reads the MSBuild files only. Register entry R-0020.
- **T1** — Open in one part of three. No change. R-0023.
- **T2** — Open in one part of five. No change. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. No change.
- **T4** — Open in two parts of four. `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs:20` still leaves out the desktop host `3DEngine`, which now sends commands (`3DEngine/BodyActions.cs`); the marker gate has no change. Register entry R-0031.
- **T5** — Open in one part of three. No change. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Open in four parts of five. No change. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. No change.
- **T10** — Open. No change. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51. No change.
- **T12** — Fixed. TASK-0059, v0.55. No change.
- **T13** — Open in part. No change. Register entry R-0031.
- **T14** — Fixed. TASK-0053, v0.51 and v0.52. No change.
- **T15** — Fixed. TASK-0050, v0.47. No change.
- **T16** — Fixed. TASK-0066, v0.67. No change.
- **T17** — Open. No change. Register entry R-0031.
- **T18** — Fixed. TASK-0049, v0.46. No change.
- **T19** — Fixed. TASK-0049, v0.46. No change.
- **T20** — Fixed. TASK-0049, v0.46. No change.
- **T21** — Fixed. TASK-0049, v0.46. No change.
- **T22** — Fixed. TASK-0049, v0.46. No change.
- **T23** — Open. No change. Register entry R-0031.
- **T24** — Fixed. No change.
- **T25** — Open in part. `eng/write-set-check/` has no change. R-0031.
- **T26** — Fixed. TASK-0057, v0.54. No change.
- **T27** — Fixed. TASK-0058, v0.56. No change.
- **T28** — Fixed. No change.
- **T29** — Fixed. TASK-0057, v0.54. No change.
- **T30** — Fixed. TASK-0057, v0.54. No change.
- **T31** — Open in part, and worse. The three gates that run engine code stay in the test process (`eng/repo-gates/RepoGates.csproj:10-12`), and T62 makes each of them pass. Register entry R-0031.
- **T32** — Fixed in part. No change: on `main` the job judges the commits of the push only (`.github/workflows/ci.yml:255`). R-0031.
- **T33** — Fixed. TASK-0057, v0.54. No change.
- **T34** — Fixed. TASK-0057, v0.54. No change.
- **T35** — Fixed. TASK-0057, v0.54. No change.
- **C1** — Open in two parts. No change. Register entry R-0027.
- **P1** — Open, and wider. `3DEngine/BodyActions.cs:59` and `:65` wait on the render loop for the serial section at each hide, show or rename, and `ShowAll` sends one command for each hidden body, so a long command of an agent stops the window. **[Inferred]** Register entry R-0027.
- **T36** — Fixed. TASK-0061, v0.61. No change.
- **T37** — Open in part. No change. R-0031.
- **T38** — Fixed. TASK-0063, v0.60. No change.
- **T39** — Fixed. TASK-0061, v0.61. No change.
- **T40** — Fixed. TASK-0061, v0.61. No change.
- **T41** — Fixed. TASK-0066, v0.67. No change.
- **T42** — Fixed. TASK-0066, v0.67. No change.
- **T43** — Fixed. TASK-0066, v0.67. No change.
- **T44** — Fixed for its forms. TASK-0066, v0.67. No change.
- **T45** — Fixed for its forms. TASK-0066, v0.67. No change.
- **T46** — Fixed for its forms. TASK-0066, v0.67. No change.
- **T47** — Fixed for its forms. TASK-0066, v0.67. No change.
- **T48** — Fixed for its forms. TASK-0073, v0.73. No change.
- **T49** — Fixed for its forms. TASK-0073, v0.73. No change.
- **T50** — Fixed for its forms. TASK-0073, v0.73. No change.
- **T51** — Fixed. TASK-0073, v0.73. No change.
- **T52** — Fixed for its form. TASK-0073, v0.73. No change.
- **T53** — Fixed. TASK-0073, v0.73. No change.
- **T54** — Fixed for its forms. TASK-0078, v0.79: `Engine.Tests/Governance/TestAttributeGateTests.cs:24-30` refuses a shadow of each name of xUnit, and `eng/count-tests.js` reads an exact count for each project. The overwrite of the result file of another project, which TASK-0078 gives as its weakest point, stays. T62 is a new form.
- **T55** — Fixed. TASK-0078, v0.79. A review agent put a lower-case `<projectreference>` in `Engine.Cli.csproj`, and two rules failed it.
- **T56** — Fixed. TASK-0078, v0.79. A review agent put `3DEngine.Vulka?.csproj` in a reference, and the wildcard rule failed it.
- **T57** — Fixed for its forms. TASK-0078, v0.79: the six cases are in `DeterminismCallGateTests`. T63 gives a path around the gate.
- **T58** — Fixed. TASK-0078, v0.79: `eng/test-floor.txt` gives an exact count for each project, and a skip of the image test on Ubuntu changes the count of 40.
- **T59** — Fixed for its form. TASK-0078, v0.79. T67 gives two variants.
- **T60** — Fixed. TASK-0078, v0.79: `.github/workflows/ci.yml:35-36` gives a read-only token, and the checkouts keep no credential.
- **T61** — Fixed. TASK-0078, v0.79. A review can still declare the newest version that exists, which no gate can check.

## 1 Current architecture

**[Observed]** The desktop host owns a document session, mounts the HTTP surface on it, and draws its live
bodies. Input goes from SDL through `SdlInput` to samples, and from samples through the intent machine of
`3DEngine.Core/Input/` to intents (ADR-0026). `UiInputRouter` gives a gesture to the panel or to the intents
at its start. The panel is Dear ImGui through ImGui.NET, and a first-party renderer draws its draw data
(ADR-0027); only `3DEngine/Ui/` names the library.

**[Observed]** A body has a name and a visibility in the engine (ADR-0028). The desktop keeps the selection, a
state of the client (ADR-0007), and sends `SetBodyVisibility` and `RenameBody` to its own session, so the log
records each change (ADR-0029). The pick of `3DEngine.Core/Picking/Picker.cs` finds the nearest visible body
under a pixel, and a second pipeline draws the selected body again.

**[Observed]** The log of the Document is a list of commands in memory (`Engine.Contracts/Document.cs:19-21`).
No code writes it to a file.

## 2 Patterns in use

- **Each correction holds for its forms, and a probe finds the next form.** T62 follows T54, T63 follows T57,
  and T67 follows T59. A gate that reads text misses what the compiler, MSBuild or xUnit evaluates. **[Observed]**
- **The three gates that run engine code have no protection of their own.** The gate program protects the gates
  that read files. The test process runs the dispatch, schema and replay gates, and T62 changes their result
  with one file. **[Observed]**
- **A desktop defect shows only in a run with a person.** V26 and V27 pass each test, and the agent found them
  with probes in the collection "Dear ImGui" and with a count of bodies that no test uses. **[Observed]**
- **The text input of the engine accepts more than the rule says.** A JSON number, a boolean, an object and a
  lone surrogate each become a name (E72, E73). P8a puts the name into a JSON file. **[Observed]**

## 3 Review of the code

### The engine and the hosts

#### E72 · Low · The binder accepts each kind of JSON value for a string · Confirmed by a run

`Engine.Core/Hosting/ParameterBinder.cs:63-65` turns any supplied value into a string, and
`Engine.Api.Http/Endpoints/JsonParameters.cs:40` passes the raw text of an object or an array. A review agent
sent `RenameBody` over HTTP: `"name":42` gave the name "42", `"name":true` gave "True", and an object and an
array gave their JSON text. Each one was applied and went into the log. The comment of `JsonParameters.cs:37-39`
says that the binder reports an object or an array as unsupported, which is false for a string. **[Observed]**
`RenameBody` is the first command with a string field. **[Recommended]** Accept only a JSON string for a field of
type string, keep the text path for the command line, and test it through `JsonParameters`.

#### E73 · Low · A name with a lone surrogate is accepted, and each view changes it · Confirmed by a run

`Engine.Core/Commands/BodyNames.cs:19-23` checks only `char.IsControl`, so a lone surrogate passes. A review
agent applied `RenameBody` with "a\uD800b": `GetBodyInfo`, the snapshot and `body.changed` each gave "a�b", so
the Document and each JSON view disagree. Over HTTP, `"name":"\ud800"` gave HTTP 500 with no body, because
`JsonElement.GetString` throws at `JsonParameters.cs:31`, outside the catch of malformed JSON; the same applies
to each string field. **[Observed]** by the agent. The desktop cannot give this name, because the text of
Dear ImGui is UTF-8. **[Inferred]** **[Recommended]** Refuse a lone surrogate in `BodyNames.Refusal` (an amendment
of ADR-0028), and map the exception to `E-API-BAD-REQUEST`.

#### E74 · Low · engine-api-http does not dispose its app at a normal stop · Confirmed by reading

`Engine.Api.Http.Server/Program.cs:105-106` waits for the shutdown and returns 0 with no `DisposeAsync`, so
`EngineHost.Dispose` and `DocumentSession.Close` do not run at a Ctrl+C of the server. `:61-70` catches only
`BackendUnavailableException`, so another exception of the composition ends the process with the app not
disposed. **[Observed]** by reading. A command longer than the stop limit then ends with the process and not by
the rule of E66; no memory is freed under it. **[Inferred]** **[Recommended]** Dispose the app before `return 0`
and on each other exception, as the desktop does, with a process test.

### The render side and the desktop

#### V26 · Medium · A click on the scene does not end a text field, and Rename can rename the wrong body · Confirmed by a run

`3DEngine/Ui/UiInputRouter.cs:66-67` gives a press that starts outside the panel to the intents only, so Dear
ImGui never sees the click away and the text field stays active; `3DEngine/Ui/DesktopUi.cs:215` keeps
`_textActive` true. A review agent typed "a" into the filter, clicked the scene and pressed H: H did not reach
the intents, and the filter became "ah". In a second probe the agent selected body A, typed "X" into the name,
clicked body B and pressed Rename: the action was a rename of B to "AlphaX". The active field keeps its own
buffer, so the reset at `DesktopUi.cs:270-271` has no effect. **[Observed]** by the agent; I read the lines.
After a person types in the panel, H, Shift and H, the arrows and the keys of the pad do nothing until a click in
the panel. **[Inferred]** **[Recommended]** Give a press outside the panel to Dear ImGui too, or clear the active
item when a gesture of the scene starts, with the two probes as tests.

#### V27 · Medium · Each frame costs time in the square of the count of bodies · Confirmed by a run

`3DEngine/SceneRenderer.cs:65` reads `VisibleCount` at each frame, before it knows that anything changed.
`3DEngine/SessionBodies.cs:93` calls `IsVisible` (`:197-198`) for each entity, and `IsVisible` searches the
live list and makes a string from each identifier. A review agent ran the same expression in a test: 4.3 ms and
1.9 MB for 200 bodies, and 90.7 ms and 48.1 MB for 1,000 bodies, at each call. **[Observed]** by the agent. The
desktop then draws about 10 frames each second with 1,000 bodies. **[Inferred]** `Bounds` (`:205`) and
`PickBodies` (`:97`) use the same search at each change and each pick. **[Observed]** **[Recommended]** Keep the
visibility in a dictionary keyed by the identifier, and count only after `Apply` reports a change.

#### V28 · Low · No test sees the highlight · Confirmed by a run

A review agent set the depth comparison of the highlight to `Never` (`3DEngine.Vulkan/RenderPipeline.cs:77`), so
the highlight drew nothing: the 89 tests of `3DEngine.Tests` and the 40 tests of `3DEngine.ImageTests` passed on
this computer, with a GPU and no skip. **[Observed]** by the agent. The highlight needs the same depth from two
pipelines, and `mesh.vert` does not declare `invariant gl_Position`, so a driver can give the tint gaps.
**[Inferred]** **[Recommended]** An image test that selects a body and counts the tinted pixels, and
`invariant gl_Position` with a new lock of the SPIR-V.

#### V29 · Low · The touch path has never run on a touch screen · Confirmed by reading

R7, R7b and R8b prove the touch path with synthetic samples only, because no computer of the project has a touch
screen (decision D1 of the review of 2026-10-17). On 2026-10-27 the owner chose that the next codebase review
carries this, because the register was at its limit (TASK-0082, "Weakest"). The long press, the tap of two
fingers and the panel by touch are new since then. **[Observed]** **[Recommended]** One check on a touch screen
before the milestone "CAD on one part", or an owner decision that accepts the gap.

#### V30 · Low · A pitch set to 90 degrees gives a view of NaN · Confirmed by reading

`3DEngine.Core/Models/Camera.cs:77` gives `PitchDegrees` a public setter with no clamp, while `Orbit` (`:83`)
clamps to 89 degrees. At 90 degrees the forward vector equals the up vector of `CreateLookAt` (`:181`), and the
view is NaN. **[Observed]** by a review agent; I read the lines. No caller sets 90 today. **[Inferred]**
**[Recommended]** Clamp or refuse the value in the setter.

### Tests, gates and the pipeline

#### T62 · High · A file that is not a gate file makes each failed test pass, and the count stays · Confirmed by a run

A support file in `Engine.Tests` can declare `[assembly: Xunit.TestFramework(...)]` with a framework that derives
from `XunitTestFramework` and gives each `ITestFailed` message as `TestPassed`. I ran it in a scratch worktree,
with a probe file in `Engine.Cli` that holds the name "RenameBody": without the framework file
`DispatchSurfaceGateTests` failed, and with it the gate passed, 5 of 5. The gate program passed the same tree
(87 checks, 0 failed). **[Observed]** A review agent saw `Engine.Tests` pass 507 of 507, the exact count. The file
is not a gate file (`eng/write-set-check/Judge.cs:83-97`), it holds none of the refused texts (`:50-56`), and
`TestAttributeGateTests` does not look for a framework attribute. **[Observed]** The analyzer of xUnit requires the
sink to derive from `LongLivedMarshalByRefObject`, and it gives no other error. **[Observed]** The file hides each
product test and the three gates that run engine code (T31). **[Inferred]** **[Recommended]** Refuse
`TestFramework`, `TestCaseOrderer`, `TestCollectionOrderer` and each type that derives from a type of `Xunit.Sdk`
in a test project, and run the three gates that run engine code from a build of `main`.

#### T63 · Medium · A project under a `bin/` folder hides from the dependency and determinism gates · Confirmed by a run

`Engine.Tests/Governance/DependencyDirectionGateTests.cs:24-27` skips each path that holds `/bin/` or `/obj/`,
and the graph keys a project by its file name. A review agent wrote `shim/bin/Engine.Contracts.csproj`, which
references `3DEngine.Core` and calls `Math.Sin`, and pointed `Engine.Core.csproj` at it: the build gave 0 errors,
and the gate program failed nothing. **[Observed]** by the agent. The determinism gate skips the call that it
cannot bind. A commit needs `git add -f`, because `.gitignore` ignores `bin/`, and the judge does not refuse it.
**[Inferred]** **[Recommended]** Resolve each reference to its full path, and refuse a reference to a project
outside the scanned set or into a `bin/` or `obj/` folder.

#### T64 · Medium · The gate of the UI library reads text, and four forms pass it · Confirmed by a run

`Engine.Tests/Governance/UiLibraryGateTests.cs:23` matches the namespace as text, `:27-29` match a package
reference with double quotes only, and `:130-133` read `.csproj`, `.props` and `.targets` files only. A review
agent compiled each of these, and the gate passed it: `using ImGuiNET;` with a Unicode escape, a package
reference with single quotes, a package reference in an imported `.xml` file, and a `DllImport` of `cimgui` in
`3DEngine/`. A plain `ImGuiNET` failed. **[Observed]** by the agent. **[Recommended]** Bind the names with Roslyn,
as the determinism gate does, read the evaluated package items, and refuse a native import of `cimgui`.

#### T65 · Low · The ADR gate accepts an archived record that has no file · Confirmed by a run

`Engine.Tests/Governance/AdrGateTests.cs:221-224` checks that an archived record left `docs/adr/`, and not that
its file is in `docs/adr/archive/` with the status Withdrawn or Superseded. A review agent deleted the file of
ADR-0001 and moved its row under "Archived": the gate program passed. **[Observed]** by the agent. The write-set
gate caught the deletion only because a task names the file. **[Recommended]** Require the file in the archive
and its status.

#### T66 · Low · The review counter can lose a product milestone with no change to a gate file · Confirmed by reading

`Engine.Tests/Governance/CodebaseReviewGateTests.cs:291-299` counts a product milestone only when the heading
names the task last, the task file gives a product phase, and the roadmap row of that phase names the task.
`docs/roadmap.md`, `docs/CURRENT-STATE.md` and the task files are not gate files, so a roadmap row without its
task makes a milestone count as an entry only. The limit of twelve entries still holds. **[Observed]** TASK-0083
gives the same weakness. **[Recommended]** Take the phase from the task file only, and make the link from a
heading to its task a check.

#### T67 · Low · Two forms of a check that does not wait pass the gate runner · Confirmed by a run

`eng/repo-gates/Program.cs:65-74` refuses an `async void` method and an async lambda converted to `Action`. A
review agent wrote `Func<Task> f = async () => ...; _ = f();` and `_ = Task.Run(() => Assert.Fail())`, and the
runner printed "pass" for each one. **[Observed]** by the agent. **[Recommended]** Refuse a discarded task in a
gate file.

## 4 The next planned task

The owner set the order on 2026-10-27 (KD2): R7b, R8a, R8b, this review, P8a, P8c, R10, K2, K3a, K0a, then K0b.
`docs/templates.md`, section 7, puts a critical or high finding into a task before the next product phase, so
TASK-0088 (P0.34) holds T62 and comes before P8a. TASK-0089 (P0.35) holds T63 to T67, and TASK-0090 (P0.36)
holds E72 to E74, V18, V26 to V28 and V30; both come after P8a. V29 has no task: the owner chose that the review
carries it. The next product phase is P8a, the document file. It serves anti-objective 7 (a replay of the log
gives the same document) and objective 12 (the work of a person survives a stop).

## 5 Research for that task

**[Observed]** by the research agent; I read each cited line.

- **What exists.** `Document.Log` is a list of commands in memory (`Engine.Contracts/Document.cs:21`), and the
  bus adds to it in `Commit` (`Engine.Core/CommandBus.cs:231`). No code of `Engine.Core`, `Engine.Contracts` or
  `Engine.Cli` writes a file or calls `JsonSerializer`. JSON goes one way only: from the request to the binder and
  `handler.Create`. No code turns a command back into its parameters, and reflection cannot do it, because
  `RenameBodyCommand.NewName` binds to the parameter "name".
- **The replay gate.** `ReplayDeterminismGateTests` compares commands in memory: the version, the identifiers and
  names of the log, the sequence, kind and cause of each event, and the handle, kind, name and visibility of each
  body. It checks no bytes of a file and no number of the geometry.
- **ADR-0015 against P8a (R-0025).** ADR-0015 is still Proposed (`docs/adr/0015-command-log-persistence.md:4`).
  It covers `engine-api-http` only and forbids a file for the command line (`:135`, `:199`). Its header has no unit,
  and no entry has a backend. It says that `appliedAtSeq` increments by 1 (`:108`, `:197`), which is false: a
  rejection also takes a sequence number. It says that the replay fills the idempotency cache again (`:69`,
  `:129`), which is false since E17, and that a load goes through `Replay.ReplayLog` (`:122`), which cannot work,
  because the code refuses a second bus for a Document (`CommandBus.cs:78-80`). It says that `Engine.Contracts`
  does not change (`:169`), which P8a cannot keep.
- **The place of the write.** The write fits in `Decide`, after `RefuseWrongBodyLists` and before `Commit`
  (`CommandBus.cs:155-156`): encode, write and flush to disk, commit, store the result, then the events. A failed
  write rejects the command, so the events of ADR-0005 have no gap and the version of ADR-0020 does not change.
  The handler has already made its body in the backend, so a failed write leaves an orphan body there, a leak and
  not a wrong Document. **[Inferred]**
- **Changes of public shape.** `IGeometryBackend` has no name (`Engine.Contracts/Geometry/IGeometryBackend.cs`),
  and the host gives the name as a string. KD3 needs a backend identity on the interface, or a name given to the
  bus. KD4 needs a unit on the Document, or a unit kept in the session. An open that keeps the identifier of the
  document needs a constructor of `Document` that takes it. An encoder on `ICommandHandler` changes the contract;
  an encoder for each handler in `Engine.Core/Commands/` does not. Each change of `Engine.Contracts` is a
  stop-and-ask condition and needs an ADR in the same commit.
- **The unit.** No command, field or query carries a unit today.
- **Flush to disk.** `RandomAccess.FlushToDisk` exists in .NET 10. The agent read in the runtime source that the
  native flush calls `fcntl(F_FULLFSYNC)` on macOS first, and `fsync` if that fails. It did not verify that
  `FileStream.Flush(true)` uses the same path, nor the flush of the folder after a create on Linux.
- **A torn last line.** A valid record is a complete line that ends in a line feed and parses. A bad last line
  can be cut off with a warning; a bad line before the last line refuses the file. **[Recommended]**
- **The place of the code.** The dependency gate checks project references only, and ADR-0019 item 8 does not
  name files. `JsonParameters.cs:12-13` says that `Engine.Core` needs no `System.Text.Json`, with the archived
  ADR-0011 as the reason. The desktop makes one `EngineHost` at the start (`3DEngine/DesktopSurface.cs:96`), and
  the host always makes a new Document, so an open at the start is simple and an open at run time replaces the
  host. The panel already has a name field.
- **Determinism.** `Utf8JsonWriter` writes a double in the shortest form that reads back to the same value (the
  agent read the documentation; not verified in this session). `JsonParameters.AsNumber` reads "-0" as the
  integer 0 (`:43-44`), so the sign is lost: the reader of the file must read each number as a double. JSON cannot
  hold NaN or an infinity, which E11 still accepts, so the encoder must refuse such a command before the write.
  E73 gives the same need for a name that is not well-formed UTF-16. **[Inferred]**

## 6 Recommended approach

**[Recommended]** Write ADR-0015 again in Simplified Technical English. The document file is JSON Lines: one
header (the schema version, the length unit with the millimetre as default, and the time of creation), then one
line for each applied command (the version of the Document after it, the name and the version of the backend, and
the command: name, schema version, identifier, expected version and parameters). The journal, the codec and the
file code go into `Engine.Core/Persistence/`, with one encoder for each handler in `Engine.Core/Commands/`, read
back through `handler.Create`. The bus encodes, writes and flushes before `Commit`; a failure rejects the command
with a new `IO` code and stops each later command of the session. An open applies the lines through the bus of the
session before the surface starts, gives a new document identifier, and refuses a backend name that does not
agree. The desktop opens the file that its command line names, and "Save" moves the journal to the name typed in
the panel. A gate holds that `Create(Encode(c))` gives `c` for each handler, and that -0, a subnormal, 0.1 and the
largest double keep their bits. The research gives 5 to 7 evenings, and a split into P8a1 (the engine and the
server, 3 to 4) and P8a2 (the desktop and the endpoint, 2 to 3). **[Inferred]**

## Questions for the owner

**Q1. The identifier of a document at an open.**
- A: a new identifier at each open (recommended: the sequence numbers change in a replay, and ADR-0005 §1
  already says so; an old cursor would resume at the wrong event).
- B: keep the identifier of the header.

**Q2. The identity of the backend (KD3).**
- A: a name and a version on `IGeometryBackend`, a change of `Engine.Contracts` with its ADR (recommended: no
  backend can then carry a wrong label; a different name refuses the open, and a different version gives a
  warning).
- B: a string given to the bus, with no change of `Engine.Contracts`.

**Q3. The length unit (KD4).**
- A: `Document.LengthUnit`, a change of `Engine.Contracts`, and a field of the snapshot (recommended, with the
  millimetre as the only permitted unit for now).
- B: the unit in the session and a query only.

**Q4. A document with no name.**
- A: a journal in a temporary file from the first command, and "Save" moves it (recommended: the only option that
  keeps "each applied command goes into the file before its result").
- B: the log in memory until the first save.
- C: a name is required at the start.

**Q5. The open in the desktop.**
- A: the file that the command line names, and an open in the window starts the program again (recommended).
- B: the session is replaced at run time, about 2 more evenings.

**Q6. Save and open by an agent (ADR-0002).**
- A: an HTTP save and open on the loopback address, limited to one folder (recommended: ADR-0002 requires that
  each operation of a person is available to an agent).
- B: the panel only.

**Q7. The place of the code.**
- A: `Engine.Core/Persistence/` (recommended: no gate change).
- B: a new project, with a new rule in the dependency gate.

**Q8. The size of P8a.** The roadmap gives 3 to 5 evenings, and the research gives 5 to 7.
- A: split P8a into P8a1 (the engine and the server, 3 to 4) and P8a2 (the desktop and the endpoint, 2 to 3)
  (recommended).
- B: one phase of 5 to 7 evenings.
