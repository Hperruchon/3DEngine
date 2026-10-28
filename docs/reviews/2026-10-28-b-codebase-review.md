---
date: 2026-10-28
commit: c0fbb64
ledger: v0.96
previous: 2026-10-28-codebase-review.md
---

# Codebase review — 2026-10-28 (second)

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the eleventh codebase review. The review of 2026-10-28 examined v0.88. After it came 4 product
milestones (P8a, P8f, P8c, R10) and 8 ledger entries (v0.89 to v0.96). The limit is four product milestones, so
K2, the next product phase, would make the gate fail. The review therefore comes before K2. The last review has the
same date, so this file has the suffix `-b` (TASK-0073). `Engine.Tests/Governance/CodebaseReviewGateTests.cs`
holds the rule, and `docs/templates.md`, section 7, gives the form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is the
authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this file does not.

| Item | Value |
|---|---|
| Code state | `main` at `c0fbb64`, the merge of v0.96 |
| Branch of this review | `codebase-review-2026-10-28-b` |
| Tests | 829 on this computer (620 in `Engine.Tests`, 144 in `3DEngine.Tests`, 65 in `3DEngine.ImageTests`) |
| Pipeline | Run 40745912217 on `c0fbb64`: green on Ubuntu, Windows and macOS |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner decides.

**Method.** Seven tasks changed code or records after `e0a2c98`, the commit of the last review: TASK-0088 (P0.34),
TASK-0091 (P8a), TASK-0092 (P8f), TASK-0090 (P0.36), TASK-0089 (P0.35), TASK-0093 (P8c) and TASK-0094 (R10).
`git log e0a2c98..c0fbb64` gives 44 commits. Four review agents worked in parallel, each one in its own git
worktree at `c0fbb64`: the gates with the judge and the pipeline, the engine with the hosts, the render side with
the desktop, and the research for K2. The agents ran probes in their worktrees and removed each one. I read each
line that this review cites, on disk. I ran three findings again in a scratch worktree: E75, E76 and T68. A line
that says "a review agent ran it" means that I read the cited code and did not repeat the run.

## Summary

- **The fixes of the last review held for the forms that they named.** E72 to E74, V18, V26 to V28, V30 and T62
  to T67 held against the probes of the agents. Each high finding of the last review has a fix. **[Observed]**
- **An open of the document file can give a wrong Document with no error (E75, high).** The open applies each
  entry through the idempotency cache of the bus. An entry whose command identifier the cache holds is skipped. I
  ran it: the live Document had a hidden body, and the opened Document showed it, with no warning. The next command
  made the file impossible to open. **[Observed]**
- **Two new forms of T62 make each failed test pass with the exact count (T68 and T69, high).** The gate skips a
  `bin/` folder at each depth, and MSBuild compiles a `bin/` folder below the root of a project. I ran it: the gate
  program passed a test framework in `Engine.Tests/Http/bin/`, and the build compiled that folder. The project
  files of `3DEngine.Tests` and `3DEngine.ImageTests` are not gate files, and an alias there hides the framework.
  **[Observed]**
- **The document file and the desktop have four medium defects.** An open cuts a complete line that it cannot
  bind (E76), a save on a host with `--file` loses that file name in silence (E77), an open of a file that another
  desktop holds closes the window (V31), and the Move pad moves the body that was selected when it opened (V32).
  **[Observed]** in runs; I ran E76.
- **K2 needs ten decisions of the owner.** No document gives the "five entities and fourteen constraints" of the
  roadmap row. The sketch changes the public shape of `Engine.Contracts`, which is a stop-and-ask condition. The
  choice of K0a (an extrude that records its sketch, or its coordinates) is cheap now and expensive after the first
  saved file. **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks. The script of this review gives 11,494 production
lines in 147 files and 13,311 test lines at `e0a2c98`, which are the values of the last review.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 14 | `.csproj` lines in `3DEngine.sln`. The last review gave 14. Git tracks 15 project files: the native package is not in the solution. |
| Production source lines | 13,749 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests` and `3DEngine.ImageTests`, 165 files. The last review gave 11,494 in 147 files. |
| Test source lines | 15,972 | `Engine.Tests` (12,670 in 82 files), `3DEngine.Tests` (1,663 in 15 files) and `3DEngine.ImageTests` (1,639 in 11 files). The last review gave 13,311. |
| Tests | 829 | `dotnet test 3DEngine.sln`: 620, 144 and 65 passed, 0 failed, 0 skipped. The last review gave 636. The Windows and macOS runners execute 60 tests of `3DEngine.ImageTests` (`eng/test-floor.txt`). |
| Gate classes | 19 | Tracked files whose name ends in `GateTests.cs`. Sixteen of them also run in `eng/repo-gates` (183 runs of 100 checks). The last review gave 19, with 16 in the gate program. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 13 | Section "Open" of `docs/register.md`; the limit is 15. This review adds R-0042, so the count becomes 14. The last review gave 14. |
| ADRs | 32 | `docs/adr/0001` to `0032`, with 0003 and 0011 in `docs/adr/archive/`. In force: 30 (16 accepted, 14 amended). None is proposed: ADR-0015 is accepted (TASK-0091). |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 8 | The same method: V2, V3, T1, T4, T31, E75, T68, T69. The last review gave 6. |
| Findings: medium | 29 | The same method: E5, E6, E11, E12, E13, E14, E16, E18, E19, E21, E22, E25, E27, E28, E33, E76, E77, V4, V31, V32, T2, T5, T10, T17, T32, T37, T70, T71, T72. The last review gave 26. |
| Findings: low | 44 | The same method: E17, E24, E26, E29 to E32, E34, E35, E37 to E39, E57, E78 to E85, V7, V15, V25, V29, V33 to V39, T7, T13, T23, T25, T73 to T78, C1, P1. The last review gave 32. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found.

## Findings of the previous review

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. `Engine.Api.Http/Endpoints/JsonParameters.cs:63-64` still reads an integer before a double, so "-0" becomes the integer 0. Register entry R-0027.
- **E6** — Open, and wider. `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs:50-52` still lists `document.loaded`, `document.replayed` and `document.saved`, and the open and the save of P8a and P8f emit none of them. The command line has no change. Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. No change.
- **E8** — Fixed. TASK-0044, v0.37. No change.
- **E9** — Fixed, and it holds. No change.
- **E10** — Fixed. TASK-0035, v0.43. No change.
- **E11** — Open, and changed. `Engine.Core/Hosting/ParameterBinder.cs:71-80` still accepts "NaN" and "Infinity". A review agent ran both paths: with a document file, the bus rejects such a command with `E-IO-NOT-ENCODABLE`; with no file, the command is applied. E78 gives the form of the reader. Register entry R-0028.
- **E12** — Open. No change. Register entry R-0027.
- **E13** — Fixed in part. No change. Register entry R-0028.
- **E14** — Open. No change: `Engine.Geometry.Manifold` gained only `Identity`. Register entry R-0028.
- **E15** — Fixed. TASK-0044, v0.37. No change.
- **E16** — Open. No change. Register entry R-0028.
- **E17** — Open, and worse. The live bus still applies an identifier again after the cache forgets it, and an open now applies each entry through the cache, so E75 skips such an entry. Register entry R-0028; TASK-0096 can close it.
- **E18** — Open. The comment of `Engine.Core/CommandBus.cs:106-112` still promises a reset. Register entry R-0033.
- **E19** — Open. No change. Register entry R-0033.
- **E20** — Fixed, and it holds. No change.
- **E21** — Open, and wider by decision. The body of a command that a failed write rejects, and each body that an undo removes, stay in the backend (ADR-0015 and ADR-0031, "Bad"). Register entry R-0033.
- **E22** — Open. `Engine.Core/CommandBus.cs:114` still stores each result, a cancelled one too. Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. No change.
- **E24** — Open in seven parts of nine. No change in the cited files; nobody checked each part again. Register entry R-0033.
- **E25** — Open. `Engine.Core/CommandBus.cs:83-85` still refuses a second bus for a Document. An open applies through the session and not through `Replay`, so P8a did not make it worse. Register entry R-0036.
- **E26** — Open, and worse. `Engine.Contracts/Document.cs:28` returns the list itself. A review agent cleared the log through a cast in `Read`; the next command wrote a line whose version an open refuses. Register entry R-0036.
- **E27** — Open. No change. Register entry R-0036.
- **E28** — Open, with a new form: an exception of a handler during an open leaves `DocumentFile.Load` with no code (E82, by reading). Register entry R-0036.
- **E29** — Open. An open reports a cancellation as `E-IO-LOAD-FAILED`, in the same form. Register entry R-0036.
- **E30** — Open, and smaller. A replay of entries with no backend argument uses the null identity, so a log of real entries refuses with `BackendMismatchException` and does not diverge in silence; a review agent read it. The default stays the null backend. Register entry R-0036.
- **E31** — Open, and wider. The period adds false comments: `Engine.Core/Persistence/DocumentFile.cs:98` (E76), `Engine.Core/Persistence/DocumentJournal.cs:88` (E84) and `Engine.Core/StepHistory.cs:16` (E75). Register entry R-0036.
- **E32** — Open, and smaller. No change. Register entry R-0036.
- **E33** — Open. No change. Register entry R-0036.
- **E34** — Open, and wider. The backend version of each entry is a constant: "3.5.2" for Manifold (`Engine.Geometry.Manifold/ManifoldGeometryBackend.cs:42`, `:55`) and "1" for the managed backend (`Engine.Core/Geometry/InProcessMeshBackend.cs:15`), while `/schema/backend` gives the version of the assembly (`Engine.Api.Http.Server/ServerEngine.cs:30`). `W-IO-BACKEND-VERSION` therefore cannot see another native library. Register entry R-0036.
- **E35** — Open. No change. Register entry R-0036.
- **E36** — Fixed. TASK-0079, v0.85. No change.
- **E37** — Open. No change: `Engine.Cli` has no change in the period. Register entry R-0036.
- **E38** — Open. No change. Register entry R-0036.
- **E39** — Open, and wider. The name of the backend now also exists as `BackendIdentity.Name`, beside the name argument of `EngineHost` (`3DEngine/DesktopSurface.cs:117`, `Engine.Api.Http.Server/ServerEngine.cs:30` and `:34`). Register entry R-0036.
- **E40** — Fixed. TASK-0058, v0.56. No change.
- **E41** — Fixed. TASK-0058, v0.56. No change.
- **E42** — Fixed as recommended. TASK-0058, v0.56. No change.
- **E43** — Fixed as recommended. TASK-0058, v0.56. No change.
- **E44** — Fixed. TASK-0058, v0.56. No change.
- **E45** — Fixed. TASK-0058, v0.56. No change.
- **E46** — Fixed. TASK-0055, v0.80. No change.
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
- **E57** — Open, and smaller. `3DEngine.ImageTests/DesktopDocumentTests.cs` starts the desktop program and checks exit code 6. No test calls `DesktopSurface` or `DesktopEngineHost` in its process, and the restart of an open has no test (TASK-0092, "Weakest"). Register entry R-0039.
- **E58** — Fixed. TASK-0067, v0.76. No change.
- **E59** — Fixed for the start. TASK-0067, v0.76. No change.
- **E60** — Fixed. TASK-0067, v0.76. No change.
- **E61** — Fixed. TASK-0074, v0.75. No change.
- **E62** — Fixed. TASK-0074, v0.75. No change.
- **E63** — Fixed. TASK-0074, v0.75. No change.
- **E64** — Fixed. TASK-0074, v0.75. No change.
- **E65** — Fixed. TASK-0074, v0.75. No change.
- **E66** — Fixed. TASK-0079, v0.85. No change.
- **E67** — Fixed. TASK-0079, v0.85. No change.
- **E68** — Fixed. TASK-0079, v0.85. No change.
- **E69** — Fixed. TASK-0079, v0.85. No change.
- **E70** — Fixed. TASK-0079, v0.85. No change.
- **E71** — Fixed for its forms. TASK-0079, v0.85. No change.
- **V1** — Fixed. TASK-0074, v0.75. No change.
- **V2** — Open. `3DEngine.Vulkan/Swapchain.cs:123` still tests `currentExtent.width > 0`. Register entry R-0022.
- **V3** — Open. No source of `3DEngine.Vulkan/` names the portability extension. Register entry R-0022.
- **V4** — Open in four parts of five. A failure of the window still goes to standard output with exit code 0 (`3DEngine/DesktopEngineHost.cs:87-90`), and `vkCreateFramebuffer` is still not checked (`3DEngine.Vulkan/Swapchain.cs:97`). A restart that fails (V31) also ends with no message in the window. Register entry R-0027.
- **V5** — Fixed. TASK-0076, v0.74. No change.
- **V6** — Fixed. TASK-0076, v0.74. No change.
- **V7** — Open. No change to `UiRenderer`, `UiTexture` or `GpuBuffer`. Register entry R-0030.
- **V8** — Fixed. TASK-0074, v0.75. No change.
- **V9** — Fixed. TASK-0074, v0.75. No change.
- **V10** — Fixed. TASK-0074, v0.75. No change.
- **V11** — Fixed in part. TASK-0074, v0.75. No change.
- **V12** — Fixed, and changed by decision. No change.
- **V13** — Fixed for its parts. No change.
- **V14** — Fixed. TASK-0074, v0.75. No change.
- **V15** — Open by decision, and wider. `3DEngine/SessionBodies.cs:143` still waits for the device for each consumed or removed body, and each undo and redo now takes this path.
- **V16** — Fixed. TASK-0079, v0.85. No change.
- **V17** — Fixed. TASK-0079, v0.85. No change.
- **V18** — Fixed for its forms. TASK-0090, v0.93: a retry after a failed remake passes no old swapchain, and the constructor frees the depth image on a throw. V39 gives a later form.
- **V19** — Fixed, with no injection. TASK-0079, v0.85. No change.
- **V20** — Fixed. TASK-0079, v0.85. No change.
- **V21** — Fixed by reading, with no test. TASK-0079, v0.85. No change.
- **V22** — Fixed for its forms. TASK-0079, v0.85. No change.
- **V23** — Fixed. TASK-0079, v0.85. No change.
- **V24** — Fixed. TASK-0079, v0.85. No change.
- **V25** — Open in part. No change. Register entry R-0020.
- **T1** — Open in one part of three. The replay fixture still has no `Translate`, `Subtract`, `Undo` or `Redo`; undo and redo replay only in `Engine.Tests/Commands/UndoRedoTests.cs`. Register entry R-0023.
- **T2** — Open in one part of five. No change. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. No change.
- **T4** — Open in two parts of four. `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs:20` still leaves out the desktop host, which now sends `Undo` and `Redo` too. Register entry R-0031.
- **T5** — Open in one part of three. No change. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Open in four parts of five. `AdrGateTests` gained the archive check of T65 only. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. No change.
- **T10** — Open. No change. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51. No change.
- **T12** — Fixed. TASK-0059, v0.55. No change.
- **T13** — Open in part. No change. Register entry R-0031.
- **T14** — Fixed. TASK-0053, v0.51. No change.
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
- **T25** — Open in part. `eng/write-set-check/` has no change. Register entry R-0031.
- **T26** — Fixed. TASK-0057, v0.54. No change.
- **T27** — Fixed. TASK-0058, v0.56. No change.
- **T28** — Fixed. No change.
- **T29** — Fixed. TASK-0057, v0.54. No change.
- **T30** — Fixed. TASK-0057, v0.54. No change.
- **T31** — Open in part, and worse. The three gates that run engine code stay in the test process (`eng/repo-gates/RepoGates.csproj:10-12`), and T68 makes the dispatch gate pass a broken rule again. Register entry R-0031.
- **T32** — Fixed in part. No change: on main the job judges the commits of the push only. Register entry R-0031.
- **T33** — Fixed. TASK-0057, v0.54. No change.
- **T34** — Fixed. TASK-0057, v0.54. No change.
- **T35** — Fixed. TASK-0057, v0.54. No change.
- **C1** — Open in two parts. No change: `IBRepOps` and `IFeatureIdMap` still exist. Register entry R-0027.
- **P1** — Open, and wider. Each command now flushes the document file to disk, the step history has no limit, and an open in the desktop applies each command two times (TASK-0092). **[Inferred]** Register entry R-0027.
- **T36** — Fixed. TASK-0061, v0.61. No change.
- **T37** — Open in part. No change. Register entry R-0031.
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
- **T54** — Fixed for its forms. TASK-0078, v0.79. No change.
- **T55** — Fixed. TASK-0078, v0.79. No change.
- **T56** — Fixed. TASK-0078, v0.79. No change.
- **T57** — Fixed for its forms. TASK-0078, v0.79. No change.
- **T58** — Fixed. TASK-0078, v0.79. No change.
- **T59** — Fixed for its form. TASK-0078, v0.79. No change.
- **T60** — Fixed. TASK-0078, v0.79. No change.
- **T61** — Fixed. TASK-0078, v0.79. No change.
- **E72** — Fixed. TASK-0090, v0.93: a field of type string takes only a JSON string. E80 gives the form of the new endpoints of the document.
- **E73** — Fixed. TASK-0090, v0.93: `BodyNames` refuses a lone surrogate. E80 gives the form of a file name.
- **E74** — Fixed by reading. TASK-0090, v0.93: `Engine.Api.Http.Server/Program.cs:138` disposes at a normal stop, and `:83-99` at each exception of the composition. No test (TASK-0090, "Weakest").
- **V26** — Fixed. TASK-0090, v0.93. A review agent pressed the left button, the right button and a touch on the scene, and each time the next H reached the scene.
- **V27** — Fixed. TASK-0090, v0.93: `3DEngine/BodyVisibility.cs` keeps the count until a change. A review agent read it and did not repeat the timing with 1,000 bodies.
- **V28** — Fixed. TASK-0090, v0.93: an image test counts the tinted pixels, and the ledger of v0.93 records that an injection failed it. The test passed on this GPU before the invariant too, so no run proves the invariant.
- **V29** — Open, and wider. The taps of three and four fingers of R10 also have synthetic proof only, and V36 is a new form that no screen has checked.
- **V30** — Fixed for its form. TASK-0090, v0.93: the setter of the pitch clamps. V38 gives the yaw and the target.
- **T62** — Fixed for its forms. TASK-0088, v0.90. T68 and T69 are new forms, each high.
- **T63** — Fixed. TASK-0089, v0.94. A review agent found no form that passes, by reading; nobody tried a symbolic link.
- **T64** — Fixed for its forms. TASK-0089, v0.94. T70 and T71 are new forms.
- **T65** — Fixed for its form. TASK-0089, v0.94. T75 is a new form.
- **T66** — Fixed for its form. TASK-0089, v0.94. T73 gives three new forms.
- **T67** — Fixed for its forms. TASK-0089, v0.94. T74 gives five new forms.

## 1 Current architecture

**[Observed]** The log of the Document is now a document file (ADR-0015). Each entry holds the command and the
identity of the backend that applied it (`Engine.Contracts/LogEntry.cs`). The bus encodes the command, writes one
line and flushes it to disk before the commit (`Engine.Core/CommandBus.cs`, method `Write`). A failed write
rejects the command and breaks the journal, and the session refuses each later command. An open applies each entry
through the bus of a new session, then the session writes to the same file (`Engine.Core/Persistence/DocumentFile.cs`).
The header records the length unit, `Document.LengthUnit`, and the millimetre is the only unit.

**[Observed]** Undo and redo are commands of the log (ADR-0031). The bus records the body records that each step
created, consumed and changed (`Engine.Core/StepHistory.cs`), and an undo makes the lists of the commit from the
step, with no call of the backend. The new event kind `body.removed` gives a body that an undo removes.

**[Observed]** The desktop opens the file that `--file` names, or writes into a folder of unsaved documents
(ADR-0030). A save moves the file into one document folder. An open checks the file in a scratch session, then
the program starts again with the new `--file` on the same port. An agent saves and opens through `/document`.

**[Observed]** The numeric pad (ADR-0032) gives the sizes of a new box and the offsets of a move. The evaluator of
`3DEngine.Core/Numeric/Expression.cs` reads numbers, `+ - * /`, parentheses, `pi` and `sqrt` with the invariant
culture, and a command records the result number only. Control and Z, Control and Y, and the taps of three and four
fingers give an undo and a redo.

## 2 Patterns in use

- **Each correction still holds for its forms, and a probe still finds the next form.** T68 and T69 follow T62,
  T70 and T71 follow T64, T73 follows T66, T74 follows T67, T75 follows T65, V38 follows V30, V39 follows V18, and
  E80 follows E72 and E73. Five of the six fixes of gates of the last review have a new form; for T63 no agent found one. **[Observed]**
- **A rule about `bin/` exists in four copies, with two meanings.** `DeterminismCallGateTests` skips `bin/` and
  `obj/` at the root of a project only, as MSBuild does. `TestAttributeGateTests`, `UiLibraryGateTests`,
  `DispatchSurfaceGateTests` and `DependencyDirectionGateTests` skip them at each depth. T68 uses the difference.
  **[Observed]**
- **The document file trusts the bus of a live session.** The open uses the same bus as a person, with its cache
  (E75) and its body records (E26). A replay has a bus with no cache (`CommandBus.ForReplay`), and the open does not
  use it. **[Observed]**
- **The desktop adds behaviour faster than tests reach it.** The restart of an open, the loop that applies the
  intents and the pad, and each touch gesture have no automatic test (TASK-0092 and TASK-0094, "Weakest"). V31,
  V32 and V36 are in these parts. **[Observed]**

## 3 Review of the code

### The engine and the hosts

#### E75 · High · An open skips an entry whose command identifier the cache holds, and the Document is wrong with no error · Confirmed by a run

`Engine.Core/Persistence/DocumentFile.cs:121` applies each entry through `session.Apply`, and
`Engine.Core/CommandBus.cs:103-104` returns the cached result for a command identifier that the cache holds.
`DocumentFile.cs:122-126` checks only that the status is `Applied`, and not that the version of the result equals
the version of the entry, which ADR-0015 item 8 requires. The live bus writes the same identifier two times when a
client sends it again after 1,024 other results (E17). I ran it with the managed backend: `CreateBox`, then
`SetBodyVisibility` false with identifier X, then `SetBodyVisibility` true, then 1,024 rejected `Redo` commands,
then X again. The live session gave "Applied version 4" and a hidden body. The open gave 0 warnings, version 3 and
a visible body. The next `NoOp` gave "Applied version 4", and a third open gave "E-IO-LOAD-FAILED: Line 6 gives the
version 4 ... must be 5". **[Observed]** A review agent also ran an open into a session that had a command
(`DocumentFile.cs:42-43` says "must be new" and nothing checks it): the file versions became 1, 2 and 4, and the next
open failed. **[Observed]** by the agent. A person loses each command after the skipped entry. **[Inferred]**
**[Recommended]** Apply the entries with no look-up in the cache, as `CommandBus.ForReplay` does, require the version
of each result to equal the version of its entry, and refuse a session that has commands. TASK-0096.

#### E76 · Medium · An open cuts a complete last line that it cannot bind, and the file loses it · Confirmed by a run

`Engine.Core/Persistence/DocumentFile.cs:96-102` treats each `DocumentFileException` of the last line as a torn
line. That includes a command that the engine does not have and a parameter that does not bind. `:136` then calls
`DocumentJournal.Resume`, which cuts the file (`Engine.Core/Persistence/DocumentJournal.cs:47`). A complete line
with its line feed cannot come from a torn write, because `Append` writes the line and its line feed in one call. I
ran it: a third line with the command name "Fillet" gave only `W-IO-TRUNCATED`, the version 2, and a file of 485
bytes instead of 696. **[Observed]** A review agent got the same result for a newer schema version and for a
parameter of the wrong JSON type. **[Observed]** by the agent. A file that a newer engine wrote loses its last command
when an older engine opens it. **[Inferred]** **[Recommended]** Treat as torn only a tail with no line feed, or a line
that is not JSON, and refuse each other failure with `E-IO-LOAD-FAILED`.

#### E77 · Medium · A save on a host with `--file` moves that file, and the next start with the same `--file` makes an empty document · Confirmed by a run

`DocumentJournal.MoveTo` (`Engine.Core/Persistence/DocumentJournal.cs:97-127`) moves the file. At the next start,
`Engine.Api.Http/EngineHost.cs:45` finds no file and `:69` makes a new one, with no message. A review agent made a
host with `work.3de`, applied `CreateBox`, saved as `plate`, and made a new host with `work.3de`: "warnings=0
version=0". **[Observed]** by the agent. ADR-0030 item 4 says that a script starts `engine-api-http` with `--file`, so
a script that saves through HTTP and starts again works on an empty document. **[Inferred]** **[Recommended]** Say on
the error output that the start makes a new file, or make a save copy the file on a host started with `--file`.

#### E78 · Low · The reader of the file accepts a number that the writer refuses · Confirmed by a run

`Engine.Core/Persistence/EntryCodec.cs:161` calls `GetDouble`, which gives an infinity for `1e400`. A review agent
wrote `1e400` into a middle line, and the open applied a box of infinite size. **[Observed]** by the agent.
**[Recommended]** Refuse a number that is not finite with `E-IO-LOAD-FAILED`.

#### E79 · Low · A document folder with a final separator refuses each name · Confirmed by a run

`Engine.Api.Http/DocumentFolder.cs:15` keeps the final separator in `FullPath`, and `:47` compares it with
`GetDirectoryName`, which has none. A review agent gave the folder `<temp>\docs\`: each name gave "leaves the
document folder". **[Observed]** by the agent. **[Recommended]** `Path.TrimEndingDirectorySeparator`.

#### E80 · Low · A file name with a lone surrogate gives HTTP 500 · Confirmed by a run

`Engine.Api.Http/Endpoints/DocumentEndpoint.cs:62` calls `GetString`, and `:65` catches only `JsonException`. A
review agent sent `{"name":"\ud800"}` to `/document/save`: HTTP 500 with no body. **[Observed]** by the agent. This is
the form of E73 in the new endpoints. **[Recommended]** Give `E-API-BAD-REQUEST`.

#### E81 · Low · A file name can be a reserved or invalid name of Windows · Confirmed by a run

A review agent saved, in a scratch folder on Windows 11, the names "CON", "NUL", "COM1", "a " and ".3de" as files.
The characters `* ? < > | "` and a name of 300 characters each gave HTTP 409 with `E-IO-SAVE-FAILED`, not 400.
**[Observed]** by the agent. On Windows 10, `CON.3de` is still a device. **[Inferred]** **[Recommended]** Refuse the
invalid characters, the reserved names, a final space or dot, and a long name, with `E-API-BAD-REQUEST`.

#### E82 · Low · A `--file` that names a folder gives exit code 5, not 6 · Confirmed by a run

`EngineHost.cs:69` makes the file with `FileMode.CreateNew`, which throws `UnauthorizedAccessException` for a
folder. `Engine.Api.Http.Server/Program.cs:85` catches only `IOException`, so `:94-98` give exit code 5; ADR-0015
item 10 says 6. **[Observed]** by a review agent; I read the lines. An exception of a handler during an open also
leaves `DocumentFile.Load` with no code (E28). **[Inferred]** **[Recommended]** Give each failure of the file and of
an open as a `DocumentFileException`.

#### E83 · Low · A second open of a held file applies each entry, then fails with no code · Confirmed by a run

`DocumentFile.cs:149` reads with `FileShare.ReadWrite`, so the entries apply before `Resume` (`:136`) finds the lock.
A review agent opened a file that a session of the same process held: each entry applied, then a plain
`IOException` with no code. **[Observed]** by the agent. `engine-api-http` still gives exit code 6. **[Recommended]**
Take the lock before the entries, and give `E-IO-LOAD-FAILED`.

#### E84 · Low · Two failure paths of the journal leave a file that disagrees with the results · Confirmed by reading

`DocumentJournal.cs:121-126`: when the open again after a move fails, the journal has no stream and is not broken.
The next `Append` then fails outside its `try` (`:65-68`), so `Broken` stays false, and a later save makes a file with
a header only at the target (`:105-110`). `:78-88`: when the write succeeds and the flush and the cut both fail, the
file keeps a complete line of a command that its client saw rejected; the comment of `:88` says that an open cuts
it, which is false. **[Observed]** by reading; no run forced either failure. **[Recommended]** Break the journal at
each failure of `MoveTo` and of the first open.

#### E85 · Low · `engine-api-http` with no `--file` refuses a save with HTTP 409 · Confirmed by a run

A review agent sent `POST /document/save` to a server with no `--file`: `409 E-IO-SAVE-FAILED "The session has no
document file."`. **[Observed]** by the agent. ADR-0030 item 4 lists the save with no exception for this host, and the
open gives 501 for the same kind of limit. **[Recommended]** Give 501, or name the limit in ADR-0030 item 4.

### The render side and the desktop

#### V31 · Medium · An open of a file that another desktop holds passes the check, and then the window is lost · Confirmed by a run

`3DEngine/DesktopDocument.cs:35` checks the file with `DocumentFile.Check`, which reads with `FileShare.ReadWrite`
(`Engine.Core/Persistence/DocumentFile.cs:149`). The journal of another process holds the file with
`FileShare.Read` (`Engine.Core/Persistence/DocumentJournal.cs:138`), so the check passes and the restart fails. A
review agent saved a file in desktop A and opened it in desktop B, both on the loopback address: B replied 202,
started again, printed "The document file did not open: The process cannot access the file", and its port was
closed 8 s later. **[Observed]** by the agent. ADR-0030 item 5 says that the person keeps the window. The document of
B stays in the folder of unsaved documents. **[Inferred]** **[Recommended]** Check with the access and the share of
the journal, and start the old file again when the new program fails.

#### V32 · Medium · The Move pad moves the body that was selected when it opened · Confirmed by a run

`3DEngine/Ui/DesktopUi.cs:381` keeps `_padBody` when the pad opens, and `:460-462` applies to it. ADR-0032 item 6
lets a press on the scene change the selection while the pad is open, and item 4 says that the apply moves the
selected body. A review agent opened Move with body A, selected body B, typed 5 and applied: the action moved A.
After A left the list, the pad stayed open and its apply named A again. **[Observed]** by the agent. **[Recommended]**
Close the pad at a change of the selection, or name the body in the pad by decision, with a test.

#### V33 · Low · While the pad is open, it takes the text of a text field · Confirmed by a run

`DesktopUi.cs:202` routes each key to the pad while it is open, and `:206-210` give each key and each text to it.
The panel still lets a person press a text field. A review agent opened New box, pressed the file name field, typed
"plate.3de" and pressed Save as: the pad field took the text, and the save had an empty name. **[Observed]** by the
agent. **[Recommended]** Disable the text fields of the panel while the pad is open.

#### V34 · Low · Two Enter presses in one frame make two boxes · Confirmed by a run

Each Enter calls `ApplyPad` (`DesktopUi.cs:450`, `:455-462`), and the pad stays open until the host ends it. A
review agent sent one frame with two Enter presses: two `CreateBoxAction`. **[Observed]** by the agent. A key repeat
at a low frame rate gives the same. **[Inferred]** **[Recommended]** Ignore a second apply until the pad ends.

#### V35 · Low · The pad goes past the edge of a tablet display · Confirmed by a run

The pad window has a fixed first position and size. A review agent drew it at 768 × 1024 and at 1024 × 768: Apply,
Cancel, Delete and the field rows were outside the display. **[Observed]** by the agent. A person with touch only
cannot reach Apply on first use. **[Inferred]** **[Recommended]** Place and size the pad from the display, as the
panel does.

#### V36 · Low · A tap of two fingers with one bounce gives Undo · Confirmed by a run

`3DEngine.Core/Input/IntentMachine.cs:101` counts each touch-down of a gesture, not the most contacts down at one
time. A review agent sent two fingers down, one up, a third down, then all up: `UndoIntent`, where a clean tap gives
`ShowAllIntent`. **[Observed]** by the agent with synthetic samples (V29). **[Recommended]** Count the most contacts
down at one time.

#### V37 · Low · A command after the reply of an open changes the old document · Confirmed by a run

A review agent sent `CreateBox` right after the 202 of an open: "Applied", and the program started again 48 ms later.
**[Observed]** by the agent. `GET /document` cannot show that an open is pending. A desktop started with `--port 0`
starts again on a new random port, while `3DEngine/DesktopDocument.cs:14` says the same port. **[Inferred]**
**[Recommended]** Refuse each command with 503 after an accepted open, and name the port rule in ADR-0030.

#### V38 · Low · The yaw and the target of the camera accept NaN · Confirmed by a run

`3DEngine.Core/Models/Camera.cs:58` gives `Target` a setter with no check, and the yaw at `:74` has none either. A review
agent set the yaw to NaN and to infinity, called `Orbit(NaN, 0)`, and set a target of NaN: each view was NaN.
**[Observed]** by the agent. No path of the program gives NaN today. **[Inferred]** This is the form of V30 for the
other fields. **[Recommended]** Check the yaw and the target as the setter of the pitch does.

#### V39 · Low · A late failure in the swapchain constructor still leaks · Confirmed by reading

`3DEngine.Vulkan/Swapchain.cs:70-98`: a throw from `vkGetSwapchainImagesKHR` or `vkCreateImageView` after the
swapchain exists leaks the handle, the depth image and the views, and the retry passes a null old swapchain while
the first one still owns the surface. **[Observed]** by a review agent, by reading; no run forced it.
**[Recommended]** One `try` over the whole constructor.

### Tests, gates and the pipeline

#### T68 · High · A test framework in a `bin/` folder inside a test project passes the gate, and each failed test passes · Confirmed by a run

`Engine.Tests/Governance/TestAttributeGateTests.cs:229-233` skips each path with a segment `bin` or `obj`. MSBuild
skips only the `bin/` and `obj/` folders at the root of a project, as `DeterminismCallGateTests` already says. I ran
it in a scratch worktree: `[assembly: Xunit.TestFramework(...)]` in `Engine.Tests/Http/ProbeFramework.cs` failed the
gate program ("applies the attribute TestFramework"), and the same file in `Engine.Tests/Http/bin/` passed it ("183
runs of 100 checks, 0 failed"). A file with a type error in the same folder failed the build, so MSBuild compiles
it. **[Observed]** A review agent put a full framework of T62 there: `Engine.Tests` passed 620 of 620, the exact
count, with a probe that the dispatch gate fails without the file. **[Observed]** by the agent. `.gitignore` ignores
`bin/`, so a commit needs `git add -f`, and the judge does not refuse it. **[Inferred]** **[Recommended]** Use the rule
of `DeterminismCallGateTests.IsCompiled` in each gate, or read the compile items from MSBuild. TASK-0097.

#### T69 · High · An alias in the project file of `3DEngine.Tests` hides the test framework from the gate · Confirmed by a run

`eng/write-set-check/Judge.cs:83-97` names `Engine.Tests/Engine.Tests.csproj` as a gate file, and not the project
files of `3DEngine.Tests` and `3DEngine.ImageTests`. The gate reads the syntax of `.cs` files only. A review agent
added twelve `<Using Alias>` items to `3DEngine.Tests/3DEngine.Tests.csproj` and a framework file that
uses only the aliases: the gate program passed, and `3DEngine.Tests` passed 145 of 145 with a test that asserts
`1 == 2`. Without the framework file, the test failed. **[Observed]** by the agent; I read `Judge.cs`. The same
project file can add a `Compile` item from another folder. **[Inferred]** **[Recommended]** Make each test project
file a gate file, and read its `Using` and `Compile` items. TASK-0097.

#### T70 · Medium · A global using in `3DEngine/Ui/` opens the UI library to each file of the host · Confirmed by a run

`Engine.Tests/Governance/UiLibraryGateTests.cs:107-108` skips each file under `3DEngine/Ui/`, and the other files
are checked for the text "ImGuiNET" only. A review agent wrote `global using ImGuiNET;` in `3DEngine/Ui/` and a call
`ImGui.Text(...)` and a `DllImport("cim" + "gui")` outside it: the build and the gate program passed. **[Observed]** by
the agent. **[Recommended]** Refuse a global using of the library, and read the constant value of a native import.

#### T71 · Medium · A package name with an MSBuild escape passes both package gates · Confirmed by a run

`UiLibraryGateTests.cs:30-31` compares the text of `Include`, and `DependencyDirectionGateTests` refuses only
`$(`, `@(` and `%(` in it. A review agent
added `<PackageReference Include="ImGui%2ENET" .../>` to `3DEngine.Vulkan`: NuGet restored ImGui.NET, and the gate
program passed. **[Observed]** by the agent. **[Recommended]** Unescape the name before the compare, or read the
restored assets of each project.

#### T72 · Medium · A client can take the assembly name `Engine.Tests` and read the internals of the kernel · Confirmed by a run

`Engine.Core/Engine.Core.csproj:16` gives its internals to `Engine.Tests` (TASK-0091), as five other projects do. No
gate reads `AssemblyName`. A review agent set `<AssemblyName>Engine.Tests</AssemblyName>` in `3DEngine.csproj` and
read the internal `CommandBus.Journal` from the desktop: the build and the gate program passed; without the name,
the build failed with CS1061. **[Observed]** by the agent. This crosses the authority boundary of `CLAUDE.md`.
**[Recommended]** The dependency gate requires the assembly name of each project to equal the name of its file.

#### T73 · Low · The review counter loses a product milestone in three more ways · Confirmed by a run

`CodebaseReviewGateTests.cs:287` skips a repeated version, `:295` takes the last task of a heading, and `:296` takes
the phase from the task file, which is not a gate file. A review agent added a fifth milestone and the gate failed;
then a heading that names a second task last, a task phase changed to `P0.99`, and a second `## v0.96` each passed.
**[Observed]** by the agent. The limit of twelve entries still holds for the first and the third form. **[Inferred]**
**[Recommended]** Require versions that rise, one task in each heading, and the phase of each task that a product row
names.

#### T74 · Low · Five more forms of a check that does not wait pass the gate runner · Confirmed by a run

`eng/repo-gates/DiscardedTasks.cs:28-34` refuses a discard and a `Run` or `StartNew` statement on three receiver
texts. A review agent wrote `var task = Task.Run(...)`, `Task.Run(...).ConfigureAwait(false);`, a `ContinueWith`,
`global::System.Threading.Tasks.Task.Run(...)` and a call through a `Func<Task>`: each one passed. **[Observed]** by
the agent. **[Recommended]** Refuse each expression of type `Task` whose value a check does not await or return, or
make the runner wait for each pending task.

#### T75 · Low · An archived ADR passes with a file that holds front matter only · Confirmed by a run

`Engine.Tests/Governance/AdrGateTests.cs:290-293` reads only the status of the archived file. A review agent deleted
ADR-0001, wrote `docs/adr/archive/0001-stub.md` with `status: Withdrawn` only, and moved its row: the gate program
passed. **[Observed]** by the agent. **[Recommended]** Require the identifier, the title and the body.

#### T76 · Low · The run of the merge of v0.95 on `main` is red, and the ledger records a branch run · Confirmed by a run

Run 40725490160 on `307ab67` failed: the macOS job was "not acquired by Runner of type hosted even after multiple
attempts", and the other three jobs passed. **[Observed]** I read the jobs and the annotation from the public API.
The ledger of v0.95 cites the branch run 40720390838. `git diff 09f8cad 307ab67` is empty, and run 40745912217 on
`c0fbb64` passed on the three runners. **[Observed]** The ledger entry of this review records it, which closes the
finding.

#### T77 · Low · A test file holds a NUL byte, so git shows no diff of it · Confirmed by a run

`3DEngine.Tests/SdlInputTests.cs` holds one NUL byte at offset 3999, in a string literal, since TASK-0084. Git shows
the file as binary (`git diff --stat`: "Bin 6881 -> 8041 bytes"), so a person who reviews a change of it sees no
text. **[Observed]** The judge still reads a file with a NUL byte. **[Observed]** by a review agent.
**[Recommended]** Write `\0`, and refuse a control byte in a `.cs` file.

#### T78 · Low · No codebase review repeated the rules review · Confirmed by reading

`docs/templates.md`, section 6, rule 5 says: "A rules review repeats with each codebase review." The rules review of
2026-09-25 gave the measures; the nine codebase reviews after it, this one included, did not repeat them.
**[Observed]** `grep` finds the measures in `docs/reviews/2026-09-25-rules-review.md` only. Rule 3 of the same
section says that a text rule that an agent broke two times becomes a gate or is deleted. A part costs one command:
the ten rule files that still exist hold 2,291 lines, 53 "must" and 51 "do not" (the rules review gave 2,971 lines in
sixteen files, 52 and 80). The inventory of 124 rules costs a day. **[Observed]** **[Recommended]** Question Q10.

## 4 The next planned task

The owner set the order on 2026-10-27 (KD2): P8a, P8f, P8c, R10, K2, K3a, K0a, then K0b. `docs/templates.md`,
section 7, puts a critical or high finding into a task before the next product phase, so TASK-0096 (P0.37) holds
E75 and TASK-0097 (P0.38) holds T68 and T69, before K2. TASK-0098 (P0.39) holds T70 to T75 and T77, and TASK-0099
(P0.40) holds E76 to E85 and V31 to V39; both come after K2. T76 closes with this record. T78 waits for question
Q10. V29 stays with the review, as the owner chose on 2026-10-27.

The next product phase is K2, "A 2D sketch model. Five entities and fourteen constraints. A sketch attaches to a
datum" (`docs/roadmap.md`, track K). It serves objective 11 (a parametric part) through the milestone "CAD on one
part": a sketch with constraints, an extrude, and a solid that follows an edit of the sketch. K3a (the solver of
five constraints), K0a (the extrude of a closed profile of line segments) and K0b (the extrude follows its sketch,
with an ADR that resolves the conflict with ADR-0021) come after it.

## 5 Research for that task

**[Observed]** by the research agent; I read each cited line that this section gives with a number.

- **No document gives the list.** The row arrived with the charter rewrite (v0.17, P0.2) with no list, and
  `docs/CHARTER.md:234` cites a "sizing research" that the repository does not hold. The closed boundary of the
  kernel gives three curves: line, circle and ellipse (`docs/CHARTER.md:155`).
- **Nothing of a sketch exists.** `git grep -i sketch` finds documentation only. "plane" exists in code only for the
  near and far planes of the camera. The Document holds bodies and the log only (`Engine.Contracts/Document.cs`).
  A sketch as a `BodyRecord` with a kind "Sketch" breaks anti-objective 9 (no universal body type), the desktop
  (which asks a tessellation of each body) and the consumption of ADR-0021.
- **Each layer is shaped for bodies.** The events of a commit are body kinds (`CommandBus.cs`, `Commit`). The
  snapshot holds bodies only. A step of the history holds body records only (`Engine.Core/StepHistory.cs:8-12`), so
  ADR-0031, which says "a new command type gets undo with no work" (`docs/adr/0031-undo-and-redo.md:93`), holds only
  for a command that changes bodies. A sketch command gets no undo until the step grows.
- **A parameter is a scalar.** The binder refuses `object` and `array` (`Engine.Core/Hosting/ParameterBinder.cs:121-126`,
  ADR-0016), the file codec writes scalars, and the command line passes strings. A command with scalar fields fits
  today; a whole sketch in one payload needs an ADR that extends ADR-0016, the binder, the codec and the command line.
- **Identity.** A handle equals the identifier of the command that made the body (ADR-0012 §4). A sketch item can
  use the same rule if each item is its own command.
- **The gates.** The determinism gate reads `Engine.Contracts`, `Engine.Core` and `Engine.Geometry.Manifold` only
  (`Engine.Tests/Governance/DeterminismCallGateTests.cs:41-42`). Sketch code in a new project needs a change of that
  list, or the gate does not read it.
- **The renderer draws triangles only** (`3DEngine.Vulkan/RenderPipeline.cs:27`). No pipeline draws a line.
- **Changes of public shape.** K2 likely needs: a sketch collection on the Document, records of a sketch, an
  entity and a constraint, a list of changed sketches on the handler result, new event kinds (`sketch.created`,
  `sketch.changed`, `sketch.removed`), a field of the snapshot, an amendment of ADR-0031 for sketch records, an
  exception to the rule of `DispatchSurfaceGateTests` as ADR-0028 item 9 has, and new diagnostic codes. Each change
  of `Engine.Contracts` is a stop-and-ask condition and needs an ADR in the same commit.
- **Other sketchers.** SolveSpace, FreeCAD, Onshape and Fusion 360 all have coincident, horizontal, vertical,
  distance, fixed or lock, point on object, midpoint, parallel, perpendicular, equal, tangent, symmetric, radius or
  diameter, and angle. SolveSpace stores an angle in degrees and its solver calls a cosine; determinism rules 1 and
  2 forbid both here. (Web sources: `https://solvespace.com/ref.pl`, `https://cad.onshape.com/help/Content/constraints.htm`,
  `https://help.autodesk.com/cloudhelp/ENU/Fusion-Sketch/files/SKT-CONSTRAINTS.htm`; the agent got the FreeCAD list
  from a search summary only.)
- **Fourteen constraints with `+ - * / sqrt`.** Coincident, horizontal, vertical, distance, fixed (the set of K3a),
  point on object, midpoint, parallel (a cross product), perpendicular (a dot product), equal, tangent, symmetric,
  radius, and angle. An angle stores the pair (cos θ, sin θ) that the client computes, as ADR-0032 item 2 does for a
  typed number; its residual needs only `+` and `*`. **[Inferred]** by the agent.
- **An arc needs no sine to store.** A centre, a start point and an end point store an arc. An engine can also
  draw it with `sqrt` only, by bisection of a chord. So "an arc needs a sine and a cosine" (the row of K0a) holds
  only for a native circle function. **[Inferred]**; not verified against Manifold.
- **The conflict of K0b.** ADR-0021 makes a body live from the command that creates it to the command that
  consumes it, and the geometry is computed at the apply. The backend never changes or frees a body, and an undo
  depends on it (`docs/adr/0031-undo-and-redo.md:42-44`). The bus refuses a handle that it created before
  (`Engine.Core/CommandBus.cs:297`). An edit of a sketch that changes the solid of an extrude therefore needs a new
  handle (which breaks each later reference), a rebuild of each later operation (a regeneration, P8d), and a feature
  that can fail without a rejection (anti-objective 4), while ADR-0006 rejects a command as one unit. One fact helps:
  each body reference in the log is already the identifier of a command, so a rebuild can keep the log.
- **What K2 decides for K0b.** The identifier of each sketch item is the identifier of its command, and no edit
  changes it. An edit replaces the sketch record and keeps its identifier; a sketch is not consumed as a body is.
  The extrude of K0a records the identifier of its sketch and not its coordinates; otherwise a command of K0a that
  holds coordinates must stay readable in each saved file for ever. **[Inferred]**
- **The size.** P8a, P8f, P8c and R10 had 11 to 17 evenings in the roadmap and merged in about 7 hours of one day.
  The limit is the decisions and the visual checks of the owner, not the code. K2 is broad: one collection, three
  record types, two or three result lists, three event kinds, the snapshot, the undo, about nine commands and about
  ten codes, and a new way to draw. **[Observed]** for the times of the merges; **[Inferred]** for the rest.
- **The path to the milestone needs points and lines only.** K0a has no arc, and K3a has five constraints. The
  circle, the arc, a fifth entity and nine constraints are not on the path. **[Observed]** in the roadmap.

## 6 Recommended approach

**[Recommended]** Split K2 as KD5 split K3. K2a, the engine model, on the path to the milestone: the point, the
line segment that references two points, the fixed frame of a sketch, the five constraints of K3a, and the three
origin planes as datums with fixed identifiers. The ADR names all five entities and all fourteen constraints with
their records, so that the rest is additive; the rest goes into a row "K2 rest" beside "K3 rest". One command for
each item (`CreateSketch`, `AddSketchPoint`, `AddSketchLine`, `AddSketchConstraint`, `SetSketchConstraintValue`,
`MoveSketchPoint`, `DeleteSketchItem`), with scalar fields only, in `Engine.Core/Commands/` and a model in
`Engine.Core/Sketch/` (no gate change). The Document holds one sketch record for each sketch; a handler gives the
new record; the step of the history records the record before and after, so an undo needs no inverse. Three event
kinds and a query `GetSketch`. The positions are a projection of the record, and the log holds the inputs only, so
the solver of K3a does not change the meaning of a field. A residual check of each constraint, with `+ - * / sqrt`,
gives something to observe before the solver. The desktop draws the sketch as lines of the UI layer, read only; an
agent builds a sketch through HTTP. K2b then gives the tools of the desktop: a sketch mode in the intent layer, a ray
to the plane, a pad with four fields, and the constraint panel. The roadmap row of K0a changes: the extrude records
the identifier of its sketch and a distance. K2a takes about 4 to 6 evenings and K2b about 4 to 6. **[Inferred]**

## Questions for the owner

**Q1. The scope of K2.**
- A: K2a holds the point, the line, the frame of a sketch, the five constraints of K3a and the datums; the circle,
  the arc, a fifth entity and nine constraints go into "K2 rest", and the ADR names all of them now (recommended:
  the milestone uses only these, and the records stay additive).
- B: all five entities and fourteen constraints in K2.

**Q2. The command model.**
- A: one command for each item, one sketch record in the Document, an undo that restores the record before, and
  the events `sketch.created`, `sketch.changed` and `sketch.removed` with a query `GetSketch` (recommended: it fits
  the scalar binder, the file and the command line of today).
- B: one command that holds the whole sketch (an extension of ADR-0016 for objects and arrays).
- C: as A, with one event kind for each kind of item.

**Q3. A line and its end points.**
- A: a line references two point items (recommended: no child identifiers, and K0a reads a closed profile from the
  references, with no tolerance).
- B: a line owns its two end points, and a coincident constraint joins two lines.

**Q4. The datum.**
- A: the three origin planes, each with a fixed identifier, and a sketch records `datumId` (recommended: a later
  datum command is additive).
- B: also a command for a datum plane now, with a quaternion.
- C: an enumeration of three planes.

**Q5. The angle constraint.**
- A: the client gives the pair (cos θ, sin θ), and the command records the two numbers (recommended: rule 2 forbids
  an angle, and ADR-0032 item 2 is the precedent).
- B: a direction (dx, dy) with no normalisation.

**Q6. The solved positions.**
- A: a projection in the sketch record, recorded in each step of the history; the log holds the inputs only
  (recommended: an undo gives the exact view, and a replay gives the same record).
- B: a regeneration cache outside the Document (P8d).

**Q7. The extrude of K0a.**
- A: the extrude records the identifier of its sketch and a distance, and reads the current sketch (recommended: a
  command with coordinates must stay readable in each saved file for ever, and K0b then needs no new command).
- B: the extrude records the coordinates of its profile, as the row of K0a reads today.

**Q8. The split of K2.**
- A: K2a, the engine model with a drawing that is read only, then K2b, the tools of the desktop (recommended: each
  half ends with something a person can observe, and each fits one session).
- B: one phase K2 of 6 to 10 evenings.

**Q9. The drawing of a sketch in K2a.**
- A: lines of the UI layer over the scene, from points that the processor projects (recommended: the lowest cost).
- B: a Vulkan pipeline for lines (learning objective; wide lines are not verified on each device).

**Q10. The rules review (T78).**
- A: each codebase review gives three more measures, the lines of the rule files and the counts of "must" and "do
  not", and the review gate requires the rows; the inventory of rules stays a separate review when the owner asks
  for it (recommended: one command for each review, and a gate instead of a text rule).
- B: each codebase review repeats the full rules review, about one day more.
- C: delete rule 5 of `docs/templates.md`, section 6.
