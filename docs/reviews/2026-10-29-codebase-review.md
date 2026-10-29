---
date: 2026-10-29
commit: 730e508
ledger: v0.105
previous: 2026-10-28-b-codebase-review.md
---

# Codebase review — 2026-10-29

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the twelfth codebase review. The second review of 2026-10-28 examined v0.96. After it came 4 product
milestones (K2a, K2b, K3a, K0a) and 9 ledger entries (v0.97 to v0.105). The limit is four product milestones, so
K0b, the next product phase, would make the gate fail at its ledger entry. The owner chose on 2026-10-29 to do the
review before K0b. `Engine.Tests/Governance/CodebaseReviewGateTests.cs` holds the rule, and `docs/templates.md`,
section 7, gives the form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is the
authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this file does not.

| Item | Value |
|---|---|
| Code state | `main` at `730e508`, the merge of v0.105 |
| Branch of this review | `codebase-review-2026-10-29` |
| Tests | 1,058 on this computer (764 in `Engine.Tests`, 190 in `3DEngine.Tests`, 104 in `3DEngine.ImageTests`) |
| Pipeline | Run 40869048102 on `730e508`: green on Ubuntu, Windows and macOS |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner decides.

**Method.** Nine tasks changed code or records after `c0fbb64`, the commit of the last review: TASK-0095 (the
review), TASK-0096 (P0.37), TASK-0097 (P0.38), TASK-0100 (K2a), TASK-0101 (K2b), TASK-0099 (P0.40), TASK-0102
(K3a), TASK-0098 (P0.39) and TASK-0103 (K0a). `git log c0fbb64..730e508` gives 48 commits. Four review agents
worked in parallel, each one in its own git worktree at `730e508`: the gates with the judge and the pipeline, the
engine with the hosts, the render side with the desktop, and the research for K0b. The agents ran probes in their
worktrees and removed each one. I read the cited lines on disk. I ran two findings again in a scratch worktree:
E86 with a probe test, and T79 with a build, the gate program and a control. I read the red run of T87 from the
public API. A line that says "an agent ran it" means that I read the cited code and did not repeat the run.

## Summary

- **The fixes of the last review held for the forms that they named.** E75 to E85, V31 to V39 and T68 to T78 are
  fixed or fixed for their forms; E84 is fixed in part. **[Observed]**
- **A test framework still reaches a test project past the gate (T79, high).** A test project that moves its
  output folder with `BaseOutputPath` compiles its root `bin/` folder, and the gate skips that folder by name. A
  `.csproj.user` file is an MSBuild file that the gate does not read. An agent made each failed test of
  `3DEngine.Tests` pass with the exact count of 190 in both ways. I ran the central part again. **[Observed]**
- **The sketch trusts that each command identifier is new (E86, medium).** A client that sends a sketch command
  again after the cache forgot it (E17) gives a sketch with two items of one identifier and no error, or an
  exception of the solver and HTTP 500. I ran it. **[Observed]**
- **Two pads of the sketch act on a state that changed (V40 and V41, medium).** The Next point pad keeps its field
  names when the chain ends, so an offset becomes a position. The Extrude pad of K0a stays open after Finish and
  extrudes the sketch that is active at the apply. **[Observed]** by an agent.
- **The run of the merge of v0.104 on `main` is red on Windows, and no record names it (T87).** A test reads a file
  before the killed host releases it. I read the job and its annotation. **[Observed]**
- **K0b needs six decisions of the owner.** The bus refuses a handle that it created before, an undo calls no
  backend, and a step holds no geometry, so a solid cannot follow its sketch today. The research recommends one
  handle with a new geometry key for each rebuild, and a fault on the body when its profile breaks. **[Observed]**
  for the code; **[Recommended]** for the design.

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks. The script of this review gives 13,749 production
lines in 165 files and 15,972 test lines at `c0fbb64`, which are the values of the last review.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 14 | `.csproj` lines in `3DEngine.sln`. The last review gave 14. Git tracks 15 project files: the native package is not in the solution. |
| Production source lines | 17,580 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests` and `3DEngine.ImageTests`, 199 files. The last review gave 13,749 in 165 files. |
| Test source lines | 19,223 | `Engine.Tests` (14,659 in 86 files), `3DEngine.Tests` (2,110 in 16 files) and `3DEngine.ImageTests` (2,454 in 14 files). The last review gave 15,972. |
| Tests | 1,058 | `dotnet test 3DEngine.sln`: 764, 190 and 104 passed, 0 failed, 0 skipped. The last review gave 829. The Windows and macOS runners execute 99 tests of `3DEngine.ImageTests` (`eng/test-floor.txt`). |
| Gate classes | 19 | Tracked files whose name ends in `GateTests.cs`. Sixteen of them also run in `eng/repo-gates` (238 runs of 118 checks). The last review gave 19, with 16 in the gate program. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 14 | Section "Open" of `docs/register.md`; the limit is 15. This review adds R-0044, so the count becomes 15. The last review gave 13. |
| ADRs | 36 | `docs/adr/0001` to `0036`, with 0003 and 0011 in `docs/adr/archive/`. In force: 34 (17 accepted, 17 amended). None is proposed. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 6 | The same method: V2, V3, T1, T4, T31, T79. The last review gave 8. |
| Findings: medium | 28 | The same method: E5, E6, E11, E12, E13, E14, E16, E18, E19, E21, E22, E25, E27, E28, E33, E86, E87, V4, V40, V41, T2, T5, T10, T17, T32, T37, T80, T81. The last review gave 29. |
| Findings: low | 40 | The same method: E17, E24, E26, E29 to E32, E34, E35, E37 to E39, E57, E84, E88 to E93, V7, V15, V25, V29, V42 to V45, T7, T13, T23, T25, T82 to T87, C1, P1. The last review gave 44. |
| Lines in the rule files | 2,346 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. At `c0fbb64` the same script gives 2,269; the last review gave 2,291, and I could not reproduce that value at its commit. |
| Sentences with "must" | 56 | `grep -o -w -i must` on the same ten files. At `c0fbb64` the script gives 53, which is the value of the last review. |
| Sentences with "do not" | 54 | `grep -o -i -E '\bdo not\b'` on the same ten files. At `c0fbb64` the script gives 51, which is the value of the last review. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found. E84 is fixed in part and counts as low.

## Findings of the previous review

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. `Engine.Api.Http/Endpoints/JsonParameters.cs:64-65` did not change, so "-0" becomes +0. The number fields of the sketch commands and of `Extrude` have the same form. Register entry R-0027.
- **E6** — Open. `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs:56-58` still lists `document.loaded`, `document.replayed` and `document.saved`. Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. No change.
- **E8** — Fixed. TASK-0044, v0.37. No change.
- **E9** — Fixed, and it holds. No change.
- **E10** — Fixed. TASK-0035, v0.43. No change.
- **E11** — Open. `Engine.Core/Hosting/ParameterBinder.cs:71-80` did not change. Each sketch handler and `Extrude` refuse a number that is not finite, so the new commands do not make it wider. Register entry R-0028.
- **E12** — Open. No change. An agent sent `"x":10` to `AddSketchPoint` over HTTP and got Applied (E5). Register entry R-0027.
- **E13** — Fixed in part. No change.
- **E14** — Open, and wider. The extrude adds three calls of `manifold_delete_manifold` on a buffer from `Marshal.AllocHGlobal` (`Engine.Geometry.Manifold/ManifoldGeometryBackend.cs:234`, `:243`, `:248`); the call at `:243` runs at each extrude. Register entry R-0028.
- **E15** — Fixed. TASK-0044, v0.37. No change.
- **E16** — Open. No change: `CommandsEndpoint.cs` reads `CommandRequest` with the default rule for unknown members. Register entry R-0028.
- **E17** — Open, and worse. `Engine.Core/CommandBus.cs:118` and `:129` did not change, and E86 gives its new effect on a sketch. Register entry R-0028.
- **E18** — Open. The comment of `Engine.Core/CommandBus.cs:121-127` still promises a reset. Register entry R-0033.
- **E19** — Open, and wider. E91 gives the form of a sketch record. Register entry R-0033.
- **E20** — Fixed, and it holds. No change.
- **E21** — Open, and wider by decision. The solid of an extrude stays in the backend after a failed write and after an undo (ADR-0036 item 8, ADR-0031). Register entry R-0033.
- **E22** — Open. `Engine.Core/CommandBus.cs:129` stores each result. Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. No change.
- **E24** — Open in seven parts of nine. The cited files of the open parts did not change, apart from additive lines in `WireMessage.cs`; nobody checked each part again. Register entry R-0033.
- **E25** — Open. No change: `Engine.Core/CommandBus.cs:88-90`. Register entry R-0036.
- **E26** — Open. `Engine.Contracts/Document.cs:28` still returns the list. E91 is the same defect in a sketch record. Register entry R-0036.
- **E27** — Open. No change: `Engine.Core/Replay.cs` has no commit in the period. Register entry R-0036.
- **E28** — Open, and changed. The form of an open is fixed (E82, `Engine.Core/Persistence/DocumentFile.cs:201-208`). The live bus still catches only a cancellation (`Engine.Core/CommandBus.cs:179`), so an exception of the solver gives HTTP 500 (E86). Register entry R-0036.
- **E29** — Open. No change in `Replay.cs`. An open reports a cancelled result as `E-IO-LOAD-FAILED` (`DocumentFile.cs:210-214`). Register entry R-0036.
- **E30** — Open, and smaller. No change. Register entry R-0036.
- **E31** — Open, and changed. The three false comments of the last review are correct now (`DocumentFile.cs:95-97`, `DocumentJournal.cs:117-119`, `StepHistory.cs:16-21`). E87 gives a new false comment (`Engine.Core/Sketch/SketchResults.cs:21-22`). Register entry R-0036.
- **E32** — Open, and smaller. No change. Register entry R-0036.
- **E33** — Open. No change: `WireMessage.cs` gained the field `sketches` only. Register entry R-0036.
- **E34** — Open. No change: `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs:44` and `:57` hold the constant "3.5.2". Register entry R-0036.
- **E35** — Open. No change. Register entry R-0036.
- **E36** — Fixed. TASK-0079, v0.85. No change.
- **E37** — Open. No change: `Engine.Cli` has no commit in the period. Register entry R-0036.
- **E38** — Open. No change. Register entry R-0036.
- **E39** — Open. No change: `3DEngine/DesktopSurface.cs:117`, `Engine.Api.Http.Server/ServerEngine.cs:29` and `:33`. Register entry R-0036.
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
- **E57** — Open. No change: `Engine.Tests/Http/DesktopSurfaceTests.cs` has no commit in the period. Register entry R-0039.
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
- **V2** — Open. `3DEngine.Vulkan/Swapchain.cs:149` still tests `currentExtent.width > 0`. Register entry R-0022.
- **V3** — Open. No source of `3DEngine.Vulkan/` names the portability extension. Register entry R-0022.
- **V4** — Open, and smaller. `3DEngine.Vulkan/Swapchain.cs:98` now checks `vkCreateFramebuffer` (TASK-0099). A failure of the window still goes to standard output (`3DEngine/DesktopEngineHost.cs:89`) with exit code 0 (`:101`). Register entry R-0027.
- **V5** — Fixed. TASK-0076, v0.74. No change.
- **V6** — Fixed. TASK-0076, v0.74. No change.
- **V7** — Open. No change: `UiRenderer.cs`, `UiTexture.cs` and `GpuBuffer.cs` have no commit in the period. Register entry R-0030.
- **V8** — Fixed. TASK-0074, v0.75. No change.
- **V9** — Fixed. TASK-0074, v0.75. No change.
- **V10** — Fixed. TASK-0074, v0.75. No change.
- **V11** — Fixed in part. TASK-0074, v0.75. No change.
- **V12** — Fixed, and changed by decision. No change.
- **V13** — Fixed for its parts. No change.
- **V14** — Fixed. TASK-0074, v0.75. No change.
- **V15** — Open by decision. No change: `3DEngine/SessionBodies.cs:143` waits for the device for each consumed or removed body, and an undo of an extrude now takes this path too.
- **V16** — Fixed. TASK-0079, v0.85. No change.
- **V17** — Fixed. TASK-0079, v0.85. No change.
- **V18** — Fixed for its forms. TASK-0090, v0.93. No change.
- **V19** — Fixed, with no injection. TASK-0079, v0.85. No change.
- **V20** — Fixed. TASK-0079, v0.85. No change.
- **V21** — Fixed by reading, with no test. TASK-0079, v0.85. No change.
- **V22** — Fixed for its forms. TASK-0079, v0.85. No change.
- **V23** — Fixed. TASK-0079, v0.85. No change.
- **V24** — Fixed. TASK-0079, v0.85. No change.
- **V25** — Open in part. No change: `3DEngine.Vulkan/3DEngine.Vulkan.csproj:25` pins `Alimer.Bindings.SDL` 3.9.8. Register entry R-0020.
- **T1** — Open in one part of three, and wider. The fixture gained a sketch (TASK-0100) and still has no `Translate`, `Subtract`, `Undo`, `Redo` or `Extrude`. The replay gate uses the managed backend, which refuses `Extrude` (`Engine.Tests/ReplayDeterminism/ReplayDeterminismGateTests.cs:38`), so no gate compares an extrude between platforms. Register entry R-0023.
- **T2** — Open in one part of five. No change. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. No change.
- **T4** — Open in two parts of four, and wider. `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs:20` still leaves out the desktop. An agent put the text "Extrude" in a host file: the gate failed in `Engine.Api.Http.Server/` and passed in `3DEngine/`. The desktop now builds eight command types (`3DEngine/SketchActions.cs`). Register entry R-0031.
- **T5** — Open in one part of three. No change. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Open in four parts of five. `AdrGateTests` gained the archive check of T75 only. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. No change.
- **T10** — Open. No change. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51. No change.
- **T12** — Fixed. TASK-0059, v0.55. No change.
- **T13** — Open in part. No change: `.github/workflows/ci.yml` has no commit in the period. Register entry R-0031.
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
- **T25** — Open in part. `eng/write-set-check/Judge.cs` changed only its list of test project files. Register entry R-0031.
- **T26** — Fixed. TASK-0057, v0.54. No change.
- **T27** — Fixed. TASK-0058, v0.56. No change.
- **T28** — Fixed. No change.
- **T29** — Fixed. TASK-0057, v0.54. No change.
- **T30** — Fixed. TASK-0057, v0.54. No change.
- **T31** — Open in part. `eng/repo-gates/RepoGates.csproj:10-12` still keeps three gates in the test process, and T79 makes them pass a broken rule again. Register entry R-0031.
- **T32** — Fixed in part. No change.
- **T33** — Fixed. TASK-0057, v0.54. No change.
- **T34** — Fixed. TASK-0057, v0.54. No change.
- **T35** — Fixed. TASK-0057, v0.54. No change.
- **C1** — Open in two parts. No change: `IBRepOps.cs` and `IFeatureIdMap.cs` still exist. Register entry R-0027.
- **P1** — Open, and wider. Each sketch command runs a dense solve in the serial section (E89), and the desktop reads a sketch once for each event (V44). Register entry R-0027.
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
- **E72** — Fixed. TASK-0090, v0.93. No change.
- **E73** — Fixed. TASK-0090, v0.93. No change.
- **E74** — Fixed by reading. TASK-0090, v0.93. No change.
- **V26** — Fixed. TASK-0090, v0.93. No change.
- **V27** — Fixed. TASK-0090, v0.93. No change.
- **V28** — Fixed. TASK-0090, v0.93. No change.
- **V29** — Open, and wider. The sketch tools of K2b and the Extrude pad of K0a have synthetic proof by touch only (TASK-0101, "Weakest"). No touch screen ran.
- **V30** — Fixed for its form. TASK-0090, v0.93. No change.
- **T62** — Fixed for its forms. TASK-0088, v0.90. No change.
- **T63** — Fixed. TASK-0089, v0.94. No change.
- **T64** — Fixed for its forms. TASK-0089, v0.94. No change.
- **T65** — Fixed for its form. TASK-0089, v0.94. No change.
- **T66** — Fixed for its form. TASK-0089, v0.94. No change.
- **T67** — Fixed for its forms. TASK-0089, v0.94. No change.
- **E75** — Fixed. TASK-0096, v0.98: `Engine.Core/CommandBus.cs:107` and `:118` apply an entry with no look-up in the cache, `Engine.Core/Persistence/DocumentFile.cs:98-100` refuses a session with commands, and `:215-218` requires the version of each result. By reading; nobody repeated the run.
- **E76** — Fixed. TASK-0099, v0.102: `DocumentFile.cs:167` cuts the last line only when it is not JSON. By reading.
- **E77** — Fixed as the first option of the recommendation. TASK-0099, v0.102: the start writes `NewFileMessage` (`Engine.Api.Http/EngineHost.cs:71`). A save still moves the file.
- **E78** — Fixed. TASK-0099, v0.102: `Engine.Core/Persistence/EntryCodec.cs:178-180` refuses a number that is not finite.
- **E79** — Fixed. TASK-0099, v0.102: `Engine.Api.Http/DocumentFolder.cs:18`.
- **E80** — Fixed. TASK-0099, v0.102: `Engine.Api.Http/Endpoints/DocumentEndpoint.cs:91` gives `E-API-BAD-REQUEST`.
- **E81** — Fixed for its forms. TASK-0099, v0.102. E93 gives new forms.
- **E82** — Fixed. TASK-0099, v0.102: `DocumentFile.cs:39-42` and `:201-208`.
- **E83** — Fixed. TASK-0099, v0.102: `DocumentFile.cs:105-116` holds the file before an entry applies.
- **E84** — Fixed in part. TASK-0099, v0.102: a failure of the first open and of the open after a move break the journal. A write that succeeds with a flush and a cut that fail still keeps a rejected line, as the message of `Engine.Core/Persistence/DocumentJournal.cs:120-123` says.
- **E85** — Fixed. TASK-0099, v0.102: HTTP 501 with `E-API-NOT-SUPPORTED`.
- **V31** — Fixed for its form. TASK-0099, v0.102: `3DEngine/DesktopDocument.cs:38-43` checks with the hold of the journal. ADR-0030 records the time after the check and Linux and macOS.
- **V32** — Fixed for its forms. TASK-0099, v0.102: a pad of a body or of a sketch selection closes when its key changes (`3DEngine/Ui/DesktopUi.cs:223-238`). V40 and V41 give a new form.
- **V33** — Fixed. TASK-0099, v0.102: the text fields are disabled while a pad is open.
- **V34** — Fixed. TASK-0099, v0.102: a second apply waits until the pad ends.
- **V35** — Fixed for its form. TASK-0099, v0.102: the pad takes its place and size from the display. V42 gives a new form.
- **V36** — Fixed. TASK-0099, v0.102: `3DEngine.Core/Input/IntentMachine.cs:119` counts the most contacts down at one time. Synthetic proof only (V29).
- **V37** — Fixed for the HTTP surface, by reading. TASK-0099, v0.102: `E-API-OPEN-PENDING`. A tap in the frame that requests the open can still change the old document. [Inferred]
- **V38** — Fixed. TASK-0099, v0.102: `3DEngine.Core/Models/Camera.cs:60-66` and `:84-88`.
- **V39** — Fixed by reading. TASK-0099, v0.102: one `try` over the constructor of `Swapchain`. No run forced a late failure.
- **T68** — Fixed for its form. TASK-0097, v0.99: the gate leaves out `bin/` and `obj/` at the root of a project only. T79 gives a new form, high.
- **T69** — Fixed for its forms. TASK-0097, v0.99: the gate reads each test project file. T79 gives a new form in a `.csproj.user` file.
- **T70** — Fixed for its forms. TASK-0098, v0.104. T82 gives two new forms.
- **T71** — Fixed for its forms. TASK-0098, v0.104. No agent found a new form.
- **T72** — Fixed for its form. TASK-0098, v0.104. T80 gives a new form.
- **T73** — Fixed for its forms. TASK-0098, v0.104. T84 gives a new form.
- **T74** — Fixed for its forms. TASK-0098, v0.104. T83 gives five new forms.
- **T75** — Fixed for its form. TASK-0098, v0.104. T85 gives a new form.
- **T76** — Fixed by the record of the last review. T87 is a new red run of `main`.
- **T77** — Fixed for its form. TASK-0098, v0.104. T86 gives new forms.
- **T78** — Fixed. TASK-0098, v0.104: `Engine.Tests/Governance/CodebaseReviewGateTests.cs:115-130` requires the three rule measures.

## 1 Current architecture

**[Observed]** The Document holds sketches beside bodies (ADR-0033). A sketch record lies on one of three origin
planes with fixed identifiers (`Engine.Contracts/Sketch/Datums.cs`), and holds points, lines and five kinds of
constraint. Each item has the identifier of the command that adds it. Seven commands change a sketch, each with
scalar fields only, and the step history records the sketch before and after, so an undo needs no inverse. The
events `sketch.created`, `sketch.changed` and `sketch.removed` come after the body events of a commit.

**[Observed]** Each sketch command gives its record through the solver (ADR-0035, `Engine.Core/Sketch/SketchSolver.cs`).
The solver is a Newton iteration with the step of minimum norm, with `+ - * / sqrt` only, in one fixed order. The
Document holds the solved positions, and the log holds the inputs only. A set with no solution keeps the positions
of the command. `GetSketch` gives `solved` and `freedom`.

**[Observed]** `Extrude` (ADR-0036) records the identifier of a sketch and a signed distance. The handler reads the
solved record, makes one counter-clockwise loop of all its lines (`Engine.Core/Sketch/SketchProfile.cs`), and calls
the new capability `IExtrudeOps`. Manifold extrudes the polygon along +Z and moves it into the frame of the datum
with a matrix of 0 and ±1 entries. The body has the kind `Extrude`, and the sketch stays live.

**[Observed]** The desktop has a sketch mode (ADR-0034). The intent layer gives a tap or a click on the plane of
the sketch, the tool Line makes a chain of points and lines, and the window "Sketch" holds the tools, the
constraint buttons, the pads and the status of the solver. The overlay draws the sketch as lines of the UI layer.

## 2 Patterns in use

- **Each correction still holds for its forms, and a probe still finds the next form.** T79 follows T68 and T69,
  T80 follows T72, T82 follows T70, T83 follows T74, T84 follows T73, T85 follows T75, T86 follows T77, V40 and V41
  follow V32, V42 follows V35, and E93 follows E81. For T71 no agent found a new form. **[Observed]**
- **A rule about `bin/` still exists in two meanings.** TASK-0098 moved two gates to the rule of the root of a
  project. The diagnostics scanner and the check of the runtime identifier still skip `bin/` at each depth (T81),
  and the test framework gate skips the root `bin/` by name while MSBuild skips `$(BaseOutputPath)` (T79).
  **[Observed]**
- **The new code trusts the cache of the bus.** The sketch handlers assume that the identifier of a command is new,
  and the cache forgets after 1,024 results (E86, E17). A body command meets the same case as an exception of the
  bus, not as a wrong record. **[Observed]**
- **A pad acts on the state at its apply, and only some pads close when that state changes.** The fix of V32 gave a
  scope to the pads of a selection. The Next point pad and the Extrude pad have no scope (V40, V41). **[Observed]**
- **The solver costs more than its ADR says.** ADR-0035 says that a dense solve of a few hundred equations takes
  less than a millisecond. An agent measured seconds for a sketch of 100 to 200 points in a Debug build, in the
  serial section (E89), and the desktop reads a sketch once for each event (V44). **[Observed]** by agents.
- **I did not read the run on `main` after the merge of v0.104.** The branch run was green, and the next task
  started. The merge run was red on Windows (T87). **[Observed]**

## 3 Review of the code

### The engine and the hosts

#### E86 · Medium · A sketch command that a client sends again after the cache forgets it gives two items with one identifier, or HTTP 500 · Confirmed by a run

The live bus applies a command identifier again after 1,024 other results (E17, `Engine.Core/CommandBus.cs:118`
and `:129`). The sketch handlers do not check that the identifier of a new item is free:
`Engine.Core/Commands/AddSketchPointCommandHandler.cs:68` adds `new SketchPoint(add.CommandId, ...)` to the list.
ADR-0033 item 5 says that an identifier is given one time. I ran it with the managed backend: three points, then
`AddSketchLine` with identifier L, then 1,024 `Redo`, then L again. The output was "again: Applied v6; lines 2,
with the id 2". The same resend of `AddSketchPoint` threw "IndexOutOfRangeException: Index was outside the bounds
of the array": `Engine.Core/Sketch/SketchSolver.cs:147` gives a repeated identifier the index `2 * _index.Count`,
and `:149` sets `Variables` from the same count. **[Observed]** An agent also ran it on Manifold: a valid extrude was
then refused ("The point ... is the end of 3 lines"), one `DeleteSketchItem` removed both lines, a resend of
`CreateSketch` threw from the bus, and HTTP gave status 500 with an empty body. **[Observed]** by the agent. An open
applies each entry again, so the file gives the same wrong record. **[Inferred]** The case needs a client that sends
one identifier again after 1,024 other results, so I rate it medium and not high. **[Recommended]** Each sketch
handler refuses an identifier that its sketch holds; the task of E17 closes the cause.

#### E87 · Medium · A move of a point gives Applied, and the point does not go where the person put it, although a solution with the hold exists · Confirmed by a run

`Engine.Core/Commands/MoveSketchPointCommandHandler.cs:76` gives the point as held. `SketchSolver.Solve`
(`Engine.Core/Sketch/SketchSolver.cs:26-35`) solves again with no hold when the held solve does not converge in 50
steps. The comment of `Engine.Core/Sketch/SketchResults.cs:21-22` says that a held point stays where the command put
it "when a solution exists". An agent made a chain of n points with a length of 10 on each line and point 0 fixed,
then moved point n/2 to (3, 3): for n = 4, 10 and 30 the point went to the target; for n = 100 it went to (484.6,
7.2), solved, in 831 ms. Fifty lines of length 10 from (0, 0) can reach (3, 3). **[Observed]** by the agent. The
command gives no diagnostic. **[Observed]** **[Recommended]** Damp the step of the held solve, or give a warning when
the solver drops the hold; correct the comment.

#### E88 · Low · `GetSketch` over HTTP gives HTTP 500 for a residual that is not finite · Confirmed by a run

`AddSketchPoint` accepts each finite position, and a `coincident` constraint between points at (1.5e308, 0.5) and
(−1.5e308, 0.5) has an infinite residual (`Engine.Core/Sketch/SketchConstraints.cs:120`). `ApiJson.Options`
(`Engine.Api.Http/Json/ApiJson.cs:18-28`) has no rule for a number that is not finite. An agent sent the three
commands, each with 200, and `GetSketch` gave 500 with an empty body. **[Observed]** by the agent. **[Recommended]**
Limit the positions to a range whose squares are finite, or give a residual that is not finite as null.

#### E89 · Low · The dense solve runs in the serial section, and a sketch of 100 to 200 points takes seconds for each command · Confirmed by a run

Each sketch command solves in the commit path (`SketchResults.cs:23-26`), and each step builds J·Jᵀ
(`SketchSolver.cs:231-245`). An agent measured a chain with a length on each line, in a Debug build: 100 points,
562 ms for an add with a conflict and 762 ms for a move with none; 200 points, 6,335 ms for a move. **[Observed]** by
the agent. ADR-0035, Context, says that "a dense linear solve of a few hundred equations takes less than a
millisecond". No Release build was measured, and each other client waits during the solve. **[Inferred]**
**[Recommended]** Measure a Release build, and record the limit in ADR-0035 or index the items.

#### E90 · Low · A sketch more than about 10⁷ length units from the origin never solves, so the extrude refuses it · Confirmed by a run

The tolerance is absolute, 1e-9 (`SketchSolver.cs:15`), and the spacing of doubles near 10⁷ is about 1.9e-9. An
agent added two points near (s, s) with a distance of 10.123456789: up to s = 10⁶ the sketch solved; at 10⁷ and 10⁸
it gave "solved=False residual=2,526E+000". **[Observed]** by the agent. `Extrude` then refuses it
(`Engine.Core/Commands/ExtrudeCommandHandler.cs:73-75`). **[Observed]** **[Recommended]** A tolerance relative to the
size of the coordinates, or the limit in ADR-0035.

#### E91 · Low · A list of a sketch record leaves the session, and a cast of it changes the Document with no command · Confirmed by a run

`SketchSolver.EquationSystem.Record` (`SketchSolver.cs:196-201`) and `SketchModel.Without`
(`Engine.Core/Sketch/SketchModel.cs:57-62`) give `List<T>`, and `GetSketchQueryHandler` returns those lists. An agent
cast the points of a `GetSketch` result in process, changed one and added one: the next `GetSketch` gave the changed
point, and the version did not change. **[Observed]** by the agent. HTTP serializes the result, so only a client in
the process can do it. **[Inferred]** This is E26 for a sketch. **[Recommended]** Build each record with read-only
lists.

#### E92 · Low · The profile rule refuses two points that coincide with a wrong reason, and accepts a solid that has almost no height · Confirmed by a run

Two consecutive points at one position make a line of length zero, and `Engine.Core/Sketch/SketchProfile.cs:60-70`
then finds that two lines touch. An agent extruded the square (0,0), (10,0), (10,0), (10,10), (0,10): "The lines ...
cross or touch". A distance of 5e-324 gave Applied with a volume of 5.0e-322 (`ExtrudeCommandHandler.cs:64` checks
only finite and not zero). **[Observed]** by the agent. No probe found a loop that the rule accepts and Manifold
refuses. **[Observed]** by the agent. **[Recommended]** A reason of its own for points that coincide, and a decision
in ADR-0036 about a smallest height and area.

#### E93 · Low · More reserved names of Windows pass the document folder · Confirmed by a run

An agent gave `DocumentFolder.Resolve` (`Engine.Api.Http/DocumentFolder.cs:57-59`, `:91`) the names "COM¹", "LPT²",
"COM0", "LPT0", "CONIN$" and "CONOUT$": each gave a path. "CON" was refused. **[Observed]** by the agent. This is a new
form of E81. **[Recommended]** Add these names to `ReservedNames`.

### The render side and the desktop

#### V40 · Medium · The Next point pad keeps its field names when the chain changes, so an offset becomes a position · Confirmed by a run

`3DEngine/Ui/DesktopUi.cs:202-206` names the fields `dx` and `dy`, or `x` and `y`, from the chain when the pad
opens, and `OpenPad` (`:208-217`) gives it no scope. `SketchTool.Next` (`3DEngine.Core/Sketching/SketchTool.cs:88-95`)
decides at the apply, from `Last`, whether the numbers are an offset. End, Close, the tool Select, a tap in the
scene and an undo change the chain while the pad stays open. An agent ended the chain with two undos while the pad
showed dx and dy: dx = 10, dy = 0 gave the point (10, 0) and no line. After a tap in the scene, a pad that showed x =
40, y = 0 gave (72, 16) and a line. **[Observed]** by the agent. **[Recommended]** A scope of the sketch, the chain and
the tool for this pad, with a test for End, an undo and a tap.

#### V41 · Medium · The Extrude pad stays open after Finish, and its apply extrudes the sketch that is active then · Confirmed by a run

`3DEngine/Ui/DesktopUi.cs:582-583` opens the Extrude pad with no scope, and `SketchActions.Extrude`
(`3DEngine/SketchActions.cs:131-140`) acts on the sketch that is active at the apply. With no active sketch it gives
no rejection, and the pad closes with no message. An agent opened the pad, pressed Finish, pressed Edit of a second
sketch, and applied: the log held one `ExtrudeCommand` on the second sketch. **[Observed]** by the agent; I read the
lines, and I wrote them in TASK-0103. ADR-0036 item 9 gives the pad no scope rule. **[Recommended]** The scope of the
active sketch for this pad, and the rule in ADR-0036 item 9.

#### V42 · Low · On a portrait tablet display a pad of the sketch covers End and Close · Confirmed by a run

`DesktopUi.cs:719-720` says that a pad of the sketch opens to the left of the window of the sketch "so that End and
Close stay visible". At a width of 768 the place is less than 16 and `:725-727` clamp it to 16. An agent drew it at
768 × 1024: "endUnderPad=True closeUnderPad=True". At 1024 × 768 and 1280 × 720 nothing covered them. **[Observed]**
by the agent. **[Recommended]** Below the buttons of the sketch window on a narrow display, with a test.

#### V43 · Low · The window offers Fixed, Move point and Delete for the origin, and Delete fails with no message · Confirmed by a run

`DesktopUi.cs:667` and `:692-700` show Fixed and Move point for each selected point, and `:712` shows Delete. The
engine refuses a move and a delete of the origin (`MoveSketchPointCommandHandler.cs:68`,
`DeleteSketchItemCommandHandler.cs:62-64`), and `SketchActions.Delete` hides `E-SKETCH-ITEM-NOT-FOUND`
(`SketchActions.cs:152`). An agent selected O: Delete gave no message, and Fixed at (5, 5) gave a sketch that is not
solved. **[Observed]** by the agent. **[Recommended]** Hide the three buttons for O.

#### V44 · Low · The desktop reads a changed sketch once for each event, and each read runs the status of the solver · Confirmed by a run

`3DEngine/SessionSketches.cs:56-72` runs one `GetSketch` for each queued change, and `GetSketchQueryHandler` calls
`SketchSolver.Status`. An agent queued 20 moves of a sketch of 100 points: 141.5 ms for `Apply` in a Release build.
**[Observed]** by the agent. **[Recommended]** Read each distinct sketch one time in `Apply`.

#### V45 · Low · A large finite position draws at an infinite pixel, and vertices that are not finite go to the GPU · Confirmed by a run

`3DEngine/SketchOverlay.cs:146-151` casts to `float` with no check. An agent drew a point at (1e308, 1e308):
"points: <640 360> <∞ -∞>", and `DesktopUi.Frame` gave 28 vertices that are not finite. No crash. **[Observed]** by
the agent. **[Recommended]** Drop a point or a segment whose pixel is not finite.

### Tests, gates and the pipeline

#### T79 · High · A project property or a `.csproj.user` file brings a test framework past the gate, and each failed test passes with the exact count · Confirmed by a run

`Engine.Tests/Governance/TestAttributeGateTests.cs:242-244` leaves out the folder `<project>/bin/` by name, and
MSBuild leaves out `$(BaseOutputPath)`. I put `<BaseOutputPath>out\</BaseOutputPath>` in
`3DEngine.Tests/3DEngine.Tests.csproj` in a scratch worktree. A file `3DEngine.Tests/bin/Probe79.cs` with a type error
failed the build ("error CS0029"), so MSBuild compiles it. The same file with `[assembly: Xunit.TestFramework(...)]`
passed the gate program ("238 runs of 118 checks, 0 failed"), and the attribute in `3DEngine.Tests/Probe79.cs` failed
it ("FAIL TestAttributeGateTests.No_Test_Source_Extends_The_Runner_Of_Xunit"). **[Observed]** An agent put a full
framework of T62 there and `Assert.Equal(1, 2)` in a test: `3DEngine.Tests` gave 0 failed of 190, the exact count of
`eng/test-floor.txt`. Twelve `<Using Alias>` items in `3DEngine.Tests/3DEngine.Tests.csproj.user`, a file that the
gate does not read, gave the same result. **[Observed]** by the agent. Both forms need `git add -f`, and the judge
does not refuse it. **[Inferred]** **[Recommended]** Refuse `BaseOutputPath`, `OutputPath` and `DefaultItemExcludes`
in a test project, read each MSBuild file of a test project, or read the compile items that MSBuild evaluates.

#### T80 · Medium · `TargetName` gives a client the assembly name `Engine.Tests` and the internals of the kernel · Confirmed by a run

`Engine.Tests/Governance/DependencyDirectionGateTests.cs:69-98` reads only `AssemblyName`. An agent set
`<TargetName>Engine.Tests</TargetName>` in `3DEngine/3DEngine.csproj` and read the internal `CommandBus.Journal` from
the desktop: the build made `Engine.Tests.dll` with 0 errors, and the gate program passed. Without the property, the
build failed with CS1061. **[Observed]** by the agent. This is T72 by another property. **[Recommended]** Refuse
`TargetName`, or compare the name of each built assembly with its project.

#### T81 · Medium · Two more users of the skip of `bin/` at each depth: the diagnostics scanner and the check of the runtime identifier · Confirmed by a run

`Engine.Tests/Diagnostics/DiagnosticsScanner.cs:79-90` and `DeterminismCallGateTests.cs:313-314` skip `bin/` and
`obj/` at each depth. An agent put a code in `Engine.Core/Sketch/bin/ProbeHidden.cs`, which compiled, and the
registry gate did not name it. An import of `Build/bin/rid.props` with `win-x86` in `Engine.Cli.csproj` gave that
runtime identifier to MSBuild, and the gate program passed; the same file in `Build/` failed it. **[Observed]** by
the agent. **[Recommended]** The rule of the root of a project in both.

#### T82 · Low · A raw string and `nameof` hide `cimgui` in a native import outside `3DEngine/Ui/` · Confirmed by a run

`UiLibraryGateTests.cs:177` reads tokens of the kind `StringLiteralToken` only. An agent wrote
`[DllImport("""cimgui""")]` and `[DllImport(nameof(cimgui))]` in `3DEngine/`: the build and the gate program passed.
**[Observed]** by the agent. **[Recommended]** Read the constant value of each argument with the semantic model.

#### T83 · Low · Five more forms of a check that does not wait pass the gate runner · Confirmed by a run

`eng/repo-gates/DiscardedTasks.cs:56-76` refuses a statement or a local of a task type. An agent passed five checks
that each start a task that throws: a lambda converted to `Action`, `List<Task>.Add`, a tuple assignment, a local
that `GC.KeepAlive` reads, and a void method with an expression body. **[Observed]** by the agent. **[Recommended]**
Make the runner wait for each task that a check starts, and add no more forms of syntax.

#### T84 · Low · The review counter does not read the phase in the ledger heading · Confirmed by a run

`CodebaseReviewGateTests.cs:321` and `:329-333` take the phase from the roadmap row only. An agent added a fifth
product entry and the gate failed; then the phase of TASK-0103 became `P0.41` and its row lost the task, and the gate
passed while the heading of v0.105 still said "(K0a, TASK-0103)". **[Observed]** by the agent. The limit of twelve
entries still holds. **[Inferred]** **[Recommended]** Count a heading that names a product phase, and require the row
and the task file to agree with it.

#### T85 · Low · An archived ADR passes with two empty headings · Confirmed by a run

`AdrGateTests.cs:296` requires the two headings and no text below them. An agent replaced ADR-0001 with an archive
file that holds front matter and two empty headings: the gate program passed. **[Observed]** by the agent.
**[Recommended]** Require a paragraph under each heading, or compare with the last version in git.

#### T86 · Low · The control byte gate reads `.cs` files only and passes a bidirectional control · Confirmed by a run

`DependencyDirectionGateTests.cs:155-168` reads `*.cs` files, and `:147` refuses only bytes below 0x20 and 0x7F. An
agent put a NUL byte in a task file and U+202E in a `.cs` comment: the gate program passed. **[Observed]** by the
agent. TASK-0098, "Weakest", names the first form. **[Recommended]** Scan each tracked text file, and refuse U+202A to
U+202E and U+2066 to U+2069.

#### T87 · Low · The run of the merge of v0.104 on `main` is red on Windows, and no record names it · Confirmed by a run

Run 40839708477 on `8fe8d0b` failed in the job "Gate on windows-latest"; the jobs on Ubuntu and macOS passed. The
annotation gives "System.IO.IOException : The process cannot access the file ... plate.3de' because it is being used
by another process". **[Observed]** I read the jobs and the annotation from the public API. The test kills the host
and reads its file at `Engine.Tests/Http/HostGuardTests.cs:416` before the process releases it. **[Inferred]** The
branch run 40839163079 on `f8fae97` was green, the ledger of v0.104 cites no run, and I started TASK-0103 without
reading the run on `main`. **[Observed]** The run 40869048102 on `730e508` passed. **[Observed]** The ledger entry of
this review records the red run. **[Recommended]** Make the test wait until the file opens, and read the run on
`main` after each merge.

## 4 The next planned task

The owner set the order on 2026-10-27 (KD2) and confirmed it on 2026-10-29: K0a, then K0b. `docs/templates.md`,
section 7, puts a critical or high finding into a task before the next product phase, so TASK-0105 (P0.41) holds T79
before K0b. TASK-0106 (P0.42) holds E86 to E93 and V40 to V45, and TASK-0107 (P0.43) holds T80 to T87; both come
after K0b. V41 touches the pad of the extrude, which K0b changes; TASK-0106 can take it into K0b if the owner wants.
T87 closes with the ledger entry of this record, and its test goes into TASK-0107.

The next product phase is K0b: "The extrude references its sketch, and a rebuild makes the solid again after an
edit of the sketch. An ADR resolves the conflict with ADR-0021 (KD5)" (`docs/roadmap.md`, track K). It completes the
milestone "CAD on one part" of objective 11.

## 5 Research for that task

**[Observed]** by the research agent; I read each cited line that this section gives with a number.

- **The life of a body.** ADR-0021 makes a body live from the command that creates it to the command that consumes
  it. ADR-0031 says: "The backend never changes and never frees a body. Each operation writes a new body under a new
  handle" (`docs/adr/0031-undo-and-redo.md:44-45`), and an undo therefore needs no call of the backend. A step
  records body and sketch records and no geometry.
- **The bus refuses a handle again.** `Engine.Core/CommandBus.cs:332-336` throws when a handler creates a body that
  the bus created before. So a rebuild cannot consume a body and create it again under the same handle.
- **No link from a body to its sketch.** `ExtrudeCommandHandler.cs` gives `new BodyRecord(handle, "Extrude")`, and a
  body record has a handle, a kind, a name and a visibility only. The link is in the log only.
- **The sketch commands touch no body.** Each sketch handler ends in `SketchResults.Changed`, which gives the
  changed sketch only and has no backend.
- **The event of a change carries no shape.** `body.changed` holds `bodyId`, `name` and `visible`
  (`CommandBus.cs:459-469`), and the desktop changes only the name and the visibility on it
  (`3DEngine/SessionBodies.cs:147-153`). The mesh is read one time, at `body.created`.
- **A replacement in place breaks an undo.** An undo of an edit would give back the sketch record, and the backend
  would keep the new solid, because the step holds no geometry. The Document and the solid would disagree with no
  error. **[Inferred]**
- **The open forbids a new rejection.** An open refuses each entry that it does not apply
  (`Engine.Core/Persistence/DocumentFile.cs:210-214`), and ADR-0035 says: "A command that the engine accepted at the
  save must not get a rejection at the open" (`docs/adr/0035-sketch-solver.md:37-38`). A rule that refuses an edit
  that breaks the profile of an extrude would make a file of v0.105 impossible to open.
- **The charter puts a rebuild error in the cache.** Anti-objective 4 says that the rule of a complete command
  "does not apply to a rebuild", and that "an error lives in the cache and never in the log"
  (`docs/CHARTER.md:206-210`).
- **A consumed extrude.** The button Move sends `Translate`, which consumes its source, so a person who extrudes,
  moves and then edits the sketch has a consumed Extrude body. To make the moved solid follow, the engine must run
  the Translate again: a regeneration of a chain (roadmap P8d and K0). **[Observed]**
- **The places that read a solid by its body identifier.** `Translate`, `Subtract`, `GetTessellation` and
  `GetBoundingBox` call the backend with the body identifier. A design with a geometry key must change each one,
  and a missed one reads the first solid with no error, because the backend never frees it. **[Inferred]**
- **A test holds the behaviour of today.** `Engine.Tests/Sketch/ExtrudeTests.cs:262-263` asserts that the box does
  not change after `MoveSketchPoint`. K0b turns it round.

## 6 Recommended approach

**[Recommended]** One handle and a new geometry key for each rebuild. Each sketch command, after the solve, finds
each live body of the kind `Extrude` whose `ExtrudeCommand` names the sketch, in the order of the log. It computes
the parameters with the code of the extrude, and when they differ it calls `Extrude` under a new key: a name-based
identifier of the edit command and the body, as ADR-0012 item 4 plans for a command that makes several solids. The
body record gains the key and a fault, so the step of the history records the old and the new key, and an undo
stays a swap of records with no call of the backend. `body.changed` gains the key and the fault, and the desktop
reads the mesh again when the key changes. Each Extrude solid, the first one too, goes under a derived key that is
never the body identifier, so a missed call site fails loudly. A profile that becomes invalid, or a sketch that is
not solved, keeps the last good solid with a fault and a new W- code, and the edit applies (anti-objective 4). A
consumed extrude does not rebuild, and the edit gives a W- warning; the chain waits for K0. The split: K0b1, the
engine and HTTP (4 to 6 evenings), then K0b2, the desktop (2 to 4 evenings). `Engine.Contracts` changes
(`BodyRecord`), which is a stop-and-ask condition, and the ADR amends ADR-0012, ADR-0021, ADR-0028, ADR-0031 and
ADR-0036. **[Inferred]** for the sizes.

## Questions for the owner

**Q1. The model of a rebuild.**
- A: one handle and a new geometry key in the body record; each live extrude of the sketch rebuilds inside the
  sketch command (recommended: references, the selection and old files stay valid, and an undo needs no backend).
- B: a new handle for each rebuild: the sketch command consumes the old body and creates a new one.
- C: a regeneration of the whole chain now (the work of K0 and P8d).

**Q2. A profile that is not valid, or a sketch that is not solved, after an edit.**
- A: the edit applies; the body keeps its last solid and gets a fault in its record, the result gives a new W- code,
  and the desktop shows the fault (recommended: anti-objective 4, and the fault survives an open).
- B: as A, and the body shows no solid while it has a fault.
- C: refuse the edit (a file of v0.105 can then fail to open).

**Q3. An Extrude body that a later command consumed, for example by Move.**
- A: no rebuild; the edit applies with a W- warning that names each consumed extrude, and K0 gives the chain
  (recommended: the milestone needs sketch, extrude and edit).
- B: refuse the edit with a new E- code.
- C: regenerate the chain in K0b (6 to 8 evenings more).

**Q4. Where the engine finds the extrudes of a sketch.**
- A: in the log: for each live body of the kind `Extrude`, its `ExtrudeCommand`, in the order of the log
  (recommended: no contract field, and no change to a saved file).
- B: the body record gains the sketch identifier and the distance.
- C: a feature list in the Document now (the start of P8d).

**Q5. What tells a client to read the mesh again.**
- A: `body.changed` gains the geometry key and the fault, and a client reads again when the key differs
  (recommended: additive, and an undo gives the same event).
- B: a new event kind `body.rebuilt`.

**Q6. The split of K0b.**
- A: K0b1, the engine and HTTP, then K0b2, the desktop (recommended: each half ends with something to observe, and
  each fits one session).
- B: one phase.
