---
date: 2026-10-30
commit: a839bb7
ledger: v0.112
previous: 2026-10-29-codebase-review.md
---

# Codebase review — 2026-10-30

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the thirteenth codebase review. The review of 2026-10-29 examined v0.105. After it came 3 product
milestones (K0b, K0c, K2c) and 7 ledger entries (v0.106 to v0.112). The limits are four and twelve, so the review is
not due. The owner approved the plan of 2026-10-30 (answer Q1 A), which puts the review here, so that the research
for K0d reaches the owner early and the next product phases fit before the next review.
`Engine.Tests/Governance/CodebaseReviewGateTests.cs` holds the rule, and `docs/templates.md`, section 7, gives the
form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is the
authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this file does not.

| Item | Value |
|---|---|
| Code state | `main` at `a839bb7`, the merge of v0.112 |
| Branch of this review | `codebase-review-2026-10-30` |
| Tests | 1,145 on this computer (839 in `Engine.Tests`, 190 in `3DEngine.Tests`, 116 in `3DEngine.ImageTests`) |
| Pipeline | Run 41108036605 on `a839bb7`: green on Ubuntu, Windows and macOS |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner decides.

**Method.** Seven tasks changed code or records after `730e508`, the commit of the last review: TASK-0104 (the
review), TASK-0105 (P0.41), TASK-0108 (K0b), TASK-0109 (K0c), TASK-0106 (P0.42), TASK-0107 (P0.43) and TASK-0110
(K2c). Four review agents worked in parallel: the engine and the hosts, the render side and the desktop, the gates
and the pipeline, each one in its own git worktree, and the research for K0d, which read only. The engine agent
worked at `5d1492e`, the first push of K2c, before its merge. It found three defects of K2c there, and TASK-0110
corrected them before the merge (the ledger entry of v0.112, "A correction"); this review records them in the
Summary and gives them no identifier, because the reviewed commit holds the correction. I read the cited lines on
disk. I ran T88 again in a scratch worktree, with a build, the gate program and a control. A line that says "an
agent ran it" means that I read the cited code and did not repeat the run.

## Summary

- **The fixes of the last review held for the forms that they named.** E86 to E93, V40 to V45 and T79 to T87 are
  fixed or fixed for their forms. **[Observed]**
- **K2c had two high defects at its first push, and the merge holds their correction.** From an exact parallel or
  normal start, `parallel` and `perpendicular` took the second line to one point and reported the sketch as solved,
  and `angle` did the same from the opposite direction. The equations grew with the length of the line. The
  equations of a direction now use the unit directions only. **[Observed]** by an agent at `5d1492e`, and by the
  tests of the correction.
- **An item `IntermediateAssembly` gives a client the assembly name `Engine.Tests` (T88, high).** A target of
  `3DEngine/3DEngine.csproj` replaced the item before the compile, the desktop read an internal type of
  `Engine.Core`, and the gate program passed. This is the form of T80 by another element. I ran it. **[Observed]**
- **Six more forms pass a gate of P0.43 (T89 to T91, T93 to T95, medium).** A `.CS` file, two types of one name in
  two projects, a code built at run time, a heading with a third item, a task on its own thread, and a task that
  never ends. **[Observed]** by an agent.
- **A pad of the sketch can still apply a value that it does not show (V46, medium).** The scope of a pad of a
  selection is the selection, not the values. When an agent or an undo moves the point while the pad is open, the
  apply moves it back. **[Observed]** by an agent.
- **K0d needs three decisions of the owner.** The chain of a consumed extrude is a projection of the log, and the
  four answers of 2026-10-30 decide most of the design. They do not decide an operand of a `Subtract` that comes
  from another sketch, or whether the fault code can name a body of the chain. **[Observed]** for the code;
  **[Recommended]** for the design.

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 14 | `.csproj` lines in `3DEngine.sln`. The last review gave 14. Git tracks 15 project files: the native package is not in the solution. |
| Production source lines | 18,359 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests` and `3DEngine.ImageTests`, 204 files. The last review gave 17,580 in 199 files. |
| Test source lines | 20,707 | `Engine.Tests` (15,832 in 90 files), `3DEngine.Tests` (2,110 in 16 files) and `3DEngine.ImageTests` (2,765 in 16 files). The last review gave 19,223. |
| Tests | 1,145 | `dotnet test 3DEngine.sln`: 839, 190 and 116 passed, 0 failed, 0 skipped. The last review gave 1,058. |
| Gate classes | 19 | Tracked files whose name ends in `GateTests.cs`. Sixteen of them also run in `eng/repo-gates` (263 runs of 126 checks). The last review gave 19. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 14 | Section "Open" of `docs/register.md`; the limit is 15. This review adds R-0045, so the count becomes 15. The last review gave 14. |
| ADRs | 37 | `docs/adr/0001` to `0037`, with 0003 and 0011 in `docs/adr/archive/`. In force: 35 (16 accepted, 19 amended). None is proposed. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 6 | The same method: V2, V3, T1, T4, T31, T88. The last review gave 6. |
| Findings: medium | 29 | The same method: E5, E6, E11, E12, E13, E14, E16, E18, E19, E21, E22, E25, E27, E28, E33, V4, V46, T2, T5, T10, T17, T32, T37, T89, T90, T91, T93, T94, T95. The last review gave 28. |
| Findings: low | 32 | The same method: E17, E24, E26, E29 to E32, E34, E35, E37 to E39, E57, E84, E94 to E96, V7, V15, V25, V29, V47 to V50, T7, T13, T23, T25, T92, C1, P1. The last review gave 40. |
| Lines in the rule files | 2,388 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The last review gave 2,346. |
| Sentences with "must" | 57 | `grep -o -w -i must` on the same ten files. The last review gave 56. |
| Sentences with "do not" | 54 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 54. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found. E13, T32 and E84 are fixed in part and count as open.

## Findings of the previous review

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. No change: `Engine.Api.Http/Endpoints/JsonParameters.cs` has no commit since `730e508`. Register entry R-0027.
- **E6** — Open. `SchemaEventsEndpoint.cs` gained the fields `geometry` and `fault` (ADR-0037 item 8) and still lists `document.loaded`, `document.replayed` and `document.saved`. Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. No change.
- **E8** — Fixed. TASK-0044, v0.37. No change.
- **E9** — Fixed, and it holds. No change.
- **E10** — Fixed. TASK-0035, v0.43. No change.
- **E11** — Open. No change: `Engine.Core/Hosting/ParameterBinder.cs` has no commit since `730e508`. The plan of 2026-10-30 puts it in P0.46. Register entry R-0028.
- **E12** — Open. No change. Register entry R-0027.
- **E13** — Fixed in part. No change.
- **E14** — Open. No change: `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs` has no commit since `730e508`, and a rebuild now runs the extrude path at each edit of a sketch with an extrude. The plan of 2026-10-30 puts it in P0.46. Register entry R-0028.
- **E15** — Fixed. TASK-0044, v0.37. No change.
- **E16** — Open. No change: `CommandsEndpoint.cs` has no commit since `730e508`. The plan of 2026-10-30 puts it in P0.46. Register entry R-0028.
- **E17** — Open. `Engine.Core/CommandBus.cs:118` and `:129` did not change. E86 closed its effect on a sketch (TASK-0106, `E-SKETCH-ID-IN-USE`); the cache still forgets a command after 1,024 results. Register entry R-0028.
- **E18** — Open. No change: the comment of `Engine.Core/CommandBus.cs:121-127` still promises a reset. Register entry R-0033.
- **E19** — Open, and smaller. TASK-0106 made the lists of a solved or a reduced sketch record read-only (E91). The other forms stay. Register entry R-0033.
- **E20** — Fixed, and it holds. No change.
- **E21** — Open, and wider by decision. A rebuild stores a new solid under a new key at each edit, and each old solid stays in the backend (ADR-0037 item 9). Register entry R-0033.
- **E22** — Open. No change: `Engine.Core/CommandBus.cs:129` stores each result. Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. No change.
- **E24** — Open in seven parts of nine. The cited files of the open parts changed only by additive lines (`WireMessage.cs`, `SchemaEventsEndpoint.cs`). Register entry R-0033.
- **E25** — Open. No change: `Engine.Core/CommandBus.cs:88-90`. Register entry R-0036.
- **E26** — Open. No change: `Engine.Contracts/Document.cs` has no commit since `730e508`. Register entry R-0036.
- **E27** — Open. No change: `Engine.Core/Replay.cs` has no commit since `730e508`. Register entry R-0036.
- **E28** — Open, and smaller. The exception of the solver of E86 is gone (TASK-0106). The live bus still catches only a cancellation (`Engine.Core/CommandBus.cs:179`). Register entry R-0036.
- **E29** — Open. No change in `Replay.cs`. Register entry R-0036.
- **E30** — Open. No change. Register entry R-0036.
- **E31** — Open, and smaller. The false comment of E87 is correct now (`Engine.Core/Sketch/SketchResults.cs:21-26`, TASK-0106). Register entry R-0036.
- **E32** — Open. No change. Register entry R-0036.
- **E33** — Open. No change: `WireMessage.cs` gained `Geometry` and `Fault` in `SnapshotBodyEntry` only. Register entry R-0036.
- **E34** — Open. No change: `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs` has no commit since `730e508`. Register entry R-0036.
- **E35** — Open. No change. Register entry R-0036.
- **E36** — Fixed. TASK-0079, v0.85. No change.
- **E37** — Open. No change: `Engine.Cli` has no commit since `730e508`. Register entry R-0036.
- **E38** — Open. No change. Register entry R-0036.
- **E39** — Open. No change: `3DEngine/DesktopSurface.cs` has no commit since `730e508`. Register entry R-0036.
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
- **E57** — Open. No change: `Engine.Tests/Http/DesktopSurfaceTests.cs` has no commit since `730e508`. Register entry R-0039.
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
- **V2** — Open. No change: `3DEngine.Vulkan/` has no commit since `730e508`. Register entry R-0022.
- **V3** — Open. No change. Register entry R-0022.
- **V4** — Open. No change. Register entry R-0027.
- **V5** — Fixed. TASK-0076, v0.74. No change.
- **V6** — Fixed. TASK-0076, v0.74. No change.
- **V7** — Open. No change: `UiRenderer.cs`, `UiTexture.cs` and `GpuBuffer.cs` have no commit since `730e508`. Register entry R-0030.
- **V8** — Fixed. TASK-0074, v0.75. No change.
- **V9** — Fixed. TASK-0074, v0.75. No change.
- **V10** — Fixed. TASK-0074, v0.75. No change.
- **V11** — Fixed in part. TASK-0074, v0.75. No change.
- **V12** — Fixed, and changed by decision. No change.
- **V13** — Fixed for its parts. No change.
- **V14** — Fixed. TASK-0074, v0.75. No change.
- **V15** — Open by decision, and wider. `3DEngine/SessionBodies.cs` waits for the device for each consumed or removed body, and now also for each body whose geometry key changes (TASK-0109).
- **V16** — Fixed. TASK-0079, v0.85. No change.
- **V17** — Fixed. TASK-0079, v0.85. No change.
- **V18** — Fixed for its forms. TASK-0090, v0.93. No change.
- **V19** — Fixed, with no injection. TASK-0079, v0.85. No change.
- **V20** — Fixed. TASK-0079, v0.85. No change.
- **V21** — Fixed by reading, with no test. TASK-0079, v0.85. No change.
- **V22** — Fixed for its forms. TASK-0079, v0.85. No change.
- **V23** — Fixed. TASK-0079, v0.85. No change.
- **V24** — Fixed. TASK-0079, v0.85. No change.
- **V25** — Open in part. No change: `3DEngine.Vulkan/3DEngine.Vulkan.csproj` pins `Alimer.Bindings.SDL` 3.9.8. Register entry R-0020.
- **T1** — Open in one part of three. No change: the replay fixture has no `Translate`, `Subtract`, `Undo`, `Redo` or `Extrude`, and no gate compares a rebuild between platforms. The bit tests of the solver now hold twelve kinds (TASK-0110). Register entry R-0023.
- **T2** — Open in one part of five. No change. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. No change.
- **T4** — Open in two parts of four. No change: `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs:20` still leaves out the desktop. Register entry R-0031.
- **T5** — Open in one part of three. No change. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Open in four parts of five. `AdrGateTests` gained the paragraph check of T85 only. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. No change.
- **T10** — Open. No change. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51. No change.
- **T12** — Fixed. TASK-0059, v0.55. No change.
- **T13** — Open in part. No change: `.github/workflows/ci.yml` has no commit since `730e508`. Register entry R-0031.
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
- **T25** — Open in part. No change: `eng/write-set-check/` has no commit since `730e508`. Register entry R-0031.
- **T26** — Fixed. TASK-0057, v0.54. No change.
- **T27** — Fixed. TASK-0058, v0.56. No change.
- **T28** — Fixed. No change.
- **T29** — Fixed. TASK-0057, v0.54. No change.
- **T30** — Fixed. TASK-0057, v0.54. No change.
- **T31** — Open in part. `eng/repo-gates/RepoGates.csproj` still keeps three gates in the test process. TASK-0105 closed T79, so they no longer pass a broken rule through it. Register entry R-0031.
- **T32** — Fixed in part. No change.
- **T33** — Fixed. TASK-0057, v0.54. No change.
- **T34** — Fixed. TASK-0057, v0.54. No change.
- **T35** — Fixed. TASK-0057, v0.54. No change.
- **C1** — Open in two parts. No change: `IBRepOps.cs` and `IFeatureIdMap.cs` still exist. Register entry R-0027.
- **P1** — Open, and smaller. The desktop reads each changed sketch one time (V44, TASK-0106). Each sketch command still runs a dense solve in the serial section (E89, ADR-0035 History). Register entry R-0027.
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
- **V29** — Open. No change: the tools of the sketch and the Extrude pad have synthetic proof by touch only. No touch screen ran.
- **V30** — Fixed for its form. TASK-0090, v0.93. No change.
- **T62** — Fixed for its forms. TASK-0088, v0.90. No change.
- **T63** — Fixed. TASK-0089, v0.94. No change.
- **T64** — Fixed for its forms. TASK-0089, v0.94. No change.
- **T65** — Fixed for its form. TASK-0089, v0.94. No change.
- **T66** — Fixed for its form. TASK-0089, v0.94. No change.
- **T67** — Fixed for its forms. TASK-0089, v0.94. No change.
- **E75** — Fixed. TASK-0096, v0.98. No change.
- **E76** — Fixed. TASK-0099, v0.102. No change.
- **E77** — Fixed as the first option of the recommendation. TASK-0099, v0.102. No change.
- **E78** — Fixed. TASK-0099, v0.102. No change.
- **E79** — Fixed. TASK-0099, v0.102. No change.
- **E80** — Fixed. TASK-0099, v0.102. No change.
- **E81** — Fixed for its forms. TASK-0099, v0.102. No change.
- **E82** — Fixed. TASK-0099, v0.102. No change.
- **E83** — Fixed. TASK-0099, v0.102. No change.
- **E84** — Fixed in part. TASK-0099, v0.102. No change.
- **E85** — Fixed. TASK-0099, v0.102. No change.
- **V31** — Fixed for its form. TASK-0099, v0.102. No change.
- **V32** — Fixed for its forms. TASK-0099, v0.102. No change.
- **V33** — Fixed. TASK-0099, v0.102. No change.
- **V34** — Fixed. TASK-0099, v0.102. No change.
- **V35** — Fixed for its form. TASK-0099, v0.102. No change.
- **V36** — Fixed. TASK-0099, v0.102. No change.
- **V37** — Fixed for the HTTP surface, by reading. TASK-0099, v0.102. No change.
- **V38** — Fixed. TASK-0099, v0.102. No change.
- **V39** — Fixed by reading. TASK-0099, v0.102. No change.
- **T68** — Fixed for its form. TASK-0097, v0.99. No change.
- **T69** — Fixed for its forms. TASK-0097, v0.99. No change.
- **T70** — Fixed for its forms. TASK-0098, v0.104. No change.
- **T71** — Fixed for its forms. TASK-0098, v0.104. No change.
- **T72** — Fixed for its form. TASK-0098, v0.104. No change.
- **T73** — Fixed for its forms. TASK-0098, v0.104. No change.
- **T74** — Fixed for its forms. TASK-0098, v0.104. No change.
- **T75** — Fixed for its form. TASK-0098, v0.104. No change.
- **T76** — Fixed by the record of the last review. No change.
- **T77** — Fixed for its form. TASK-0098, v0.104. No change.
- **T78** — Fixed. TASK-0098, v0.104. No change.
- **E86** — Fixed. TASK-0106, v0.110: the four commands that add a sketch item refuse an identifier that the log holds with `E-SKETCH-ID-IN-USE`.
- **E87** — Fixed. TASK-0106, v0.110: a move that the solver cannot hold gives `W-SKETCH-HOLD-DROPPED`.
- **E88** — Fixed. TASK-0106, v0.110: a residual that is not finite is null in `GetSketch`.
- **E89** — Fixed by record. TASK-0106, v0.110: the History of ADR-0035 gives the measured times. P1 keeps the cost.
- **E90** — Fixed by record. TASK-0106, v0.110: the History of ADR-0035 gives the limit of about 10⁶ units from the origin.
- **E91** — Fixed. TASK-0106, v0.110: the lists of a solved or a reduced sketch record are read-only.
- **E92** — Fixed. TASK-0106, v0.110: a line of length zero gives its own reason.
- **E93** — Fixed. TASK-0106, v0.110: COM0, LPT0, the superscript ports, CONIN$ and CONOUT$ are reserved names.
- **V40** — Fixed. TASK-0106, v0.110: the Next point pad has the scope of the active sketch, the tool and the chain.
- **V41** — Fixed. TASK-0106, v0.110: the Extrude pad has the scope of the active sketch.
- **V42** — Fixed. TASK-0106, v0.110: the pad opens below the row of End and Close on a narrow display.
- **V43** — Fixed. TASK-0106, v0.110: a selection with the origin gives no Fixed, Move point or Delete.
- **V44** — Fixed. TASK-0106, v0.110: the desktop reads each changed sketch one time.
- **V45** — Fixed. TASK-0106, v0.110: the overlay drops a pixel that is not finite.
- **T79** — Fixed for its forms. TASK-0105, v0.107: the test framework gate reads the output path and each MSBuild file of a test project.
- **T80** — Fixed for its form. TASK-0107, v0.111: no MSBuild file sets `TargetName`. A `Directory.Build.rsp` gives a new form (R-0031).
- **T81** — Fixed for its forms. TASK-0107, v0.111: the scanner and the identifier check use the rule of the project root, and the scanner reads constant values.
- **T82** — Fixed for its forms. TASK-0107, v0.111: the UI gate takes the value of each constant string from the compiler.
- **T83** — Fixed for its forms. TASK-0107, v0.111: the runner waits for the work of each check and fails a task that failed with no reader.
- **T84** — Fixed for its form. TASK-0107, v0.111: the counter counts a heading that names a product phase.
- **T85** — Fixed for its form. TASK-0107, v0.111: an archived ADR holds text under each heading.
- **T86** — Fixed for its forms. TASK-0107, v0.111: the gate reads each tracked text file and refuses the bidirectional controls.
- **T87** — Fixed. TASK-0107, v0.111: `HostGuardTests` reads the file of a killed host for up to 10 s. The run of v0.111 on `main` passed on Windows.

## 1 Current architecture

**[Observed]** A body record holds a geometry key and a fault (ADR-0037). The key is the handle for a body of
v0.107 and earlier, and a name-based guid of the command and the body for an extrude
(`Engine.Core/GeometryKeys.cs`). Each read of a solid uses the key. An edit of a sketch rebuilds each live extrude
of it in log order under a new key (`Engine.Core/Sketch/SketchRebuild.cs`), and a profile that breaks gives the
fault `W-SKETCH-REBUILD-FAILED` and keeps the last solid. A consumed extrude gives `W-SKETCH-EXTRUDE-CONSUMED`. An
undo swaps the records and calls no backend.

**[Observed]** The desktop follows the key: `3DEngine/SessionBodies.cs` meshes a body again when its key changes,
and the panel shows "(fault)" and the reason. The sketch has twelve kinds of constraint (K2c): the five of K3a,
and `pointOn`, `midpoint`, `parallel`, `perpendicular`, `equal`, `symmetric` and `angle` on points and lines. The
desktop panel offers the five kinds of K3a only.

**[Observed]** The gate runner `eng/repo-gates` waits after each check until the thread pool and the timers are
idle, and fails a check whose task failed with no reader (`eng/repo-gates/PendingTasks.cs`). The UI gate and the
diagnostics scanner take constant values from one compilation of the files (`Engine.Tests/Governance/ConstantStrings.cs`).

## 2 Patterns in use

- **Each correction still holds for its forms, and a probe still finds the next form.** T88 follows T80, T89 and
  T90 follow T82, T91 follows T81, T92 follows T86, T93 follows T84, T94 and T95 follow T83, V46 and V47 follow
  V40, V49 follows V45, and E94 follows E91. **[Observed]**
- **A gate that reads text sees one spelling, and the build accepts more.** The extension `.CS` (T89), an element
  other than `TargetName` (T88) and a heading with a third item (T93) each pass. **[Observed]** by an agent.
- **A merge without a probe holds a defect that a test of the happy path does not see.** The first push of K2c
  passed 19 tests and 12 injections on three runners, and a probe from an exact start found two high defects.
  The tests now hold each start that the probe found. **[Observed]**
- **A command identifier that the cache forgets reaches more handlers.** E86 closed the four add commands. A move
  sent again now reuses a key of the backend and gives a false fault (E95). **[Observed]** by an agent.

## 3 Review of the code

### The engine and the hosts

#### E94 · Low · A move and a change of a value leave lists in the Document that a client can change through a cast · Confirmed by a run

`Engine.Core/Commands/MoveSketchPointCommandHandler.cs:74` builds the points with `ToList()`, and
`Engine.Core/Commands/SetSketchConstraintValueCommandHandler.cs:76` builds the constraints the same way.
`SketchSolver.SolveWithHold` returns the input record when the solve ends at its first step or fails
(`Engine.Core/Sketch/SketchSolver.cs:36-43`), so the Document holds a `List<T>`. An agent cast `Points` of a sketch
with no constraint after a move and added a point: the count went from 2 to 3. **[Observed]** by an agent; I read
the code. This is E91 by two more paths. **[Recommended]** `SketchResults.Changed` makes the three lists read-only.

#### E95 · Low · A move that a client sends again after the cache forgets it gives a false fault on an extrude · Confirmed by a run

`SketchResults.InUse` guards the four commands that add an item. A `MoveSketchPoint` sent again after 1,024 other
results applies a second time, and its rebuild asks the backend for `GeometryKeys.Derive(move, body)`, which it
holds. An agent saw the fault `W-SKETCH-REBUILD-FAILED` "Backend already holds a body under handle …" on a valid,
solved profile. **[Observed]** by an agent. **[Inferred]** `SetSketchConstraintValue` and `DeleteSketchItem` do the
same. **[Recommended]** Guard each sketch command, or close E17 in the bus (R-0028).

#### E96 · Low · The comment of `SubtractCommand` says that both operands stay · Confirmed by reading

`Engine.Core/Commands/SubtractCommand.cs:6-7` says that the operands "are left intact". Since ADR-0021 item 3 a
`Subtract` consumes both. **[Observed]** by the research agent; I read the line. **[Recommended]** Correct the comment.

### The render side and the desktop

#### V46 · Medium · A pad of a selection can apply a value that it does not show, and undo a change of another client · Confirmed by a run

The pads Distance, Fixed, Move point and Value have the scope of the active sketch and the selection
(`3DEngine/Ui/DesktopUi.cs:705`, `:709`, `:721-725`, `:731-735`). An agent opened Move point on A at (3, 4), then
gave the panel A at (7, 8) with the same selection: the pad stayed open with 3 and 4, and Enter gave
`MoveSketchPointAction(A, 3, 4)`. **[Observed]** by an agent. **[Inferred]** An agent edit or an undo while the pad is
open is undone by the apply. The defect is older than `730e508`, and the ledger of v0.110 says that the pads follow
the state that they act on. **[Recommended]** The key of the pad holds the values that it shows.

#### V47 · Low · An Enter in the frame where the scope of a pad changes still applies the pad · Confirmed by a run

The host routes the input (`3DEngine/NativeThreeDEngine.cs:108`) before it applies the session and follows the
chain (`:313`, `:318`), and the scope check runs in `Frame` (`3DEngine/Ui/DesktopUi.cs:396`). An agent opened the
Next point pad, gave Enter, and then a frame with the chain closed: the pad closed, and the actions still held
`NextPointAction`, whose dx and dy then act as a position. **[Observed]** by an agent. This is V40 for one frame.
**[Recommended]** The action carries the key of its pad, and the host compares it with the state at the apply.

#### V48 · Low · Twenty changes of one body in one frame give twenty uploads of the same mesh · Confirmed by a run

`3DEngine/SessionBodies.cs:163-168` meshes a body for each `body.changed` with a new key. An agent measured one
upload in 0.5 ms for one event and twenty uploads in 4.4 ms for twenty events of a profile of four lines.
**[Observed]** by an agent. **[Recommended]** Keep the last change of each body, as `SessionSketches.Apply` does (V44).

#### V49 · Low · A point at about 10³⁷ mm gives finite pixels and vertices that are not finite · Confirmed by a run

`3DEngine/SketchOverlay.cs:165` and `:182` drop a pixel that is not finite (V45). A line between points at ±3·10³⁷
mm gives finite pixels of about ±1.9·10³⁸, and the line code of the UI library gives 4 vertices of 806 that are not
finite. **[Observed]** by an agent. **[Recommended]** Drop a pixel whose magnitude is more than a limit, for example
10⁷, or clip each segment in double before the cast.

#### V50 · Low · The text of a fault pushes Show all out of the panel on a display of 1024 × 768 · Confirmed by a run

With two bodies and a fault with a full identifier, the text of the fault is 208 pixels high, and Show all moves
to 708–752 on a panel that ends at 716 (`3DEngine/Ui/DesktopUi.cs:526`). The comment of the panel (`:445`) says that two
bodies and Show all fit with no scroll bar. **[Observed]** by an agent. **[Recommended]** One line for the code and
the full message on demand.

### Tests, gates and the pipeline

#### T88 · High · An item `IntermediateAssembly` gives a client the assembly name `Engine.Tests` and the internals of the kernel · Confirmed by a run

`Engine.Tests/Governance/DependencyDirectionGateTests.cs:87` refuses the element `TargetName`. I put a target with
`BeforeTargets="CoreCompile"` in `3DEngine/3DEngine.csproj` that removes `@(IntermediateAssembly)` and adds
`obj\Debug\net10.0\Engine.Tests.dll`, and a file that names the internal type `Engine.Core.StepHistory`. The build
gave 0 errors and `3DEngine/obj/Debug/net10.0/Engine.Tests.dll`, and the gate program passed with 263 runs. Without
the target, the build failed with CS0122. **[Observed]** `ReferenceOffences` reads `$(`, `@(` and `%(` in an
`Include` only, not in a `Remove`. **[Observed]** **[Recommended]** Compare the name of each built assembly with its
project after the build, in the pipeline; refuse `IntermediateAssembly` and an `Output` element that sets a name.

#### T89 · Medium · A source file with the extension `.CS` passes the UI gate · Confirmed by a run

`Engine.Tests/Governance/UiLibraryGateTests.cs:230`, `:169` and `:202` compare the extension `.cs` with case. An
agent put `3DEngine/ProbeUpper.CS` with `using ImGuiNET;` and `[DllImport("cimgui")]`: the build on Windows
compiled it, and the gate program passed. **[Observed]** by an agent. **[Recommended]** Compare with no case, or read
the items `Compile` that MSBuild evaluates.

#### T90 · Medium · Two types of one name in two projects hide a joined code from the diagnostics gate · Confirmed by a run

`Engine.Tests/Governance/ConstantStrings.cs:17` makes one compilation of all files. An agent declared
`Engine.Probe.Shared` in `Engine.Core` and in `Engine.Cli`, and joined `Shared.Part + "-DUP"` into a code: the gate
program passed. With two namespaces, the gate named the code. **[Observed]** by an agent. **[Inferred]** The UI gate
has the same gap. **[Recommended]** One compilation for each project, with the projects that it references.

#### T91 · Medium · A diagnostic code built at run time passes the diagnostics gate · Confirmed by a run

`Engine.Tests/Diagnostics/DiagnosticsScanner.cs:54` reads constant values. `"E-SKETCH-" + Tag` with a
`static readonly` field, and `string.Concat("W-SKETCH-", "JOINED")`, passed. **[Observed]** by an agent.
**[Recommended]** Refuse a string that starts with the form of a code and is not a full code, or require each code
to be one constant of `DiagnosticCodes`.

#### T92 · Low · U+2028 in a comment starts code that a reader sees as a comment · Confirmed by a run

`Engine.Tests/Governance/DependencyDirectionGateTests.cs:173` refuses U+202A to U+202E and U+2066 to U+2069. An
agent wrote a comment with U+2028 and then a class on the same visible line: the class compiled into
`Engine.Core.dll`, and the gate passed. U+200F and U+061C passed in a string. **[Observed]** by an agent.
**[Recommended]** Also refuse U+2028, U+2029, U+0085, U+200E, U+200F and U+061C.

#### T93 · Medium · A heading with a third item hides its product phase from the review counter · Confirmed by a run

`Engine.Tests/Governance/CodebaseReviewGateTests.cs:335` takes the phase from a heading that ends with
`(<phase>, TASK-nnnn)`. An agent wrote `(K0b, TASK-0108, ADR-0037)`, gave TASK-0108 the phase `governance`, and
removed the task from its row: the gate program passed and did not count K0b. **[Observed]** by an agent.
**[Inferred]** A point after the parenthesis has the same effect. **[Recommended]** Refuse each heading after the
review that does not have the exact form.

#### T94 · Medium · A task on its own thread fails after the check, and the runner does not see it · Confirmed by a run

`eng/repo-gates/PendingTasks.cs:62` waits for the thread pool and the timers. An agent started a task with
`TaskCreationOptions.LongRunning` that failed after 600 seconds: the gate program passed. **[Observed]** by an agent.
**[Inferred]** A shorter task fails a later check. **[Recommended]** Refuse `LongRunning` and `new Thread` in a gate
file, or count the tasks that each check starts.

#### T95 · Medium · A check whose assertion waits for a task that never ends passes · Confirmed by a run

An agent kept a task that awaits a `TaskCompletionSource` with no result and then fails: no task failed, and the
gate program passed. **[Observed]** by an agent. **[Recommended]** Fail a check that leaves a task of it that is not
complete.

## 4 The next planned task

The plan of 2026-10-30 gives the order after this review: P0.44, R9, P0.45, P0.46, K0d, K2d. `docs/templates.md`,
section 7, puts a high finding into a task before the next product phase, so P0.44 (TASK-0112) holds T88 before
R9. P0.45 (TASK-0114) holds E94 to E96, V46 to V50 and T89 to T95 after R9. P0.46 (TASK-0115) holds E11, E14,
E16 and the test of R-0043, as the plan says. K0d (TASK-0116) needs the answers to the questions below.

## 5 Research for that task

**[Observed]** by the research agent; I read the cited code of `SketchRebuild.cs`, `TranslateCommandHandler.cs` and
`SubtractCommandHandler.cs`.

- **The chain is in the log.** Only `Translate` and `Subtract` consume a body, and the handle of their result is
  the identifier of the command. One body has one consumer in effect at most (ADR-0021 item 1), so the steps in
  effect (`SketchRebuild.InEffect`) give one path from each consumed extrude to one live end. Two extrudes of one
  sketch can meet in one `Subtract`.
- **Each step runs again on the backend.** `Translate` needs its three distances, and `Subtract` its two operands.
  The rebuild calls `ITransformOps.Translate` and `IBooleanOps.Subtract` under `GeometryKeys.Derive(edit, body)`, as
  it calls `IExtrudeOps` now. The handlers need live operands, so they are not used. The backend refuses a key that
  it holds, so each body of the chain runs once for each edit.
- **The commit holds the live end only.** The bus refuses a changed record of a body that is not live, so the end
  gets its new key and the bodies between have a solid in the backend only. An undo swaps the record back and calls
  no backend. No public shape of `Engine.Contracts` changes: the record has `Geometry` and `Fault`, and
  `body.changed` gives both.
- **A failure gives a fault.** An empty `Subtract` throws in Manifold; the rebuild catches it, and the end keeps its
  key and gets a fault. A replay walks the log in one order with keys of SHA-1, so each platform gives the same keys.
- **The desktop needs no change.** `3DEngine/SessionBodies.cs` meshes a body again when its key changes.

## 6 Recommended approach

**[Recommended]** ADR-0039 with these items, under KQ1 to KQ4 A of 2026-10-30 and the answers below:

1. `FeatureGraph.Of(log)` in `Engine.Core/Sketch/`, a pure function of the steps in effect, gives the command that
   creates each body and the command that consumes it. No new field of the Document.
2. The seeds are the consumed extrudes of the edited sketch; the ends are their live descendants, in log order.
3. For each end, the rebuild evaluates its upstream steps in log order, once each, under `Derive(edit, body)`. An
   extrude of the edited sketch uses the solved record, and an extrude of another sketch uses its live record
   (Q1 A). A part with no extrude keeps its key and calls no backend.
4. A failed or unresolved step gives the end its fault, and the edit applies (KQ3 A, Q2).
5. `ChangedBodies` holds the live ends only. An undo stays a swap of records.
6. The tests: a translate, a subtract with the extrude as each operand, a chain of four steps, an undo and a redo,
   an empty result, an operand of another sketch, two extrudes in one subtract, and an open of a file. The estimate
   is 6 to 9 agent hours.

## Questions for the owner

**Q1. An operand of a `Subtract` in the chain that comes from another sketch** (for example `Subtract(X, Y)` where
Y is an extrude of a second sketch).
- A: The rebuild evaluates the full upstream of the end from the live sketches, so Y follows its own sketch too.
- B: The rebuild takes the key of Y at its consumption from the history of the bus. It needs an internal accessor,
  and it is wrong after an earlier edit of the second sketch.
- C: The end gets a fault when an operand from outside the chain has an extrude upstream.

**Recommended: A.** It gives the solid that the log describes in each case. It runs steps that do not descend from
the edited extrude, so the owner confirms it.

**Q2. The code of a failed step of the chain.** `CLAUDE.md` says: "Do not change the meaning of a code."
- A: `W-SKETCH-REBUILD-FAILED` gets the wider text "the rebuild of an extrude of the sketch, or of a body of its
  chain, failed". KQ3 A names this code.
- B: A new code `W-SKETCH-CHAIN-FAILED` for a step of the chain, and `W-SKETCH-REBUILD-FAILED` keeps its text.

**Recommended: B.** A new code keeps the rule of `CLAUDE.md` with no judgement, and an agent can tell a broken
profile from a broken boolean. The answer KQ3 A then reads as "a fault on the result body".

**Q3. The code `W-SKETCH-EXTRUDE-CONSUMED`.**
- A: It stays registered and is raised only for a consumer that the graph does not know, which is none today.
- B: Its row says that the engine no longer raises it since TASK-0116.

**Recommended: A.** A later command that consumes a body, for example a fillet, starts with this warning until
the graph knows it.
