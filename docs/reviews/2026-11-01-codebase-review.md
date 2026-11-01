---
date: 2026-11-01
commit: 3fd8b9b
ledger: v0.159
previous: 2026-10-31-c-codebase-review.md
---

# Codebase review — 2026-11-01

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the eighteenth codebase review. The review of 2026-10-31 (c) examined v0.149. After it came 4 product
milestones (V2a, V2b, V3 and P8g) and 10 ledger entries (v0.150 to v0.159). The limits are four and twelve, so the
next product milestone would make the review late. The owner told the session on 2026-11-01: "do the review".
`Engine.Tests/Governance/CodebaseReviewGateTests.cs` holds the rule, and `docs/templates.md`, section 7, gives the
form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is the
authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this file does not.

| Item | Value |
|---|---|
| Code state | `main` at `3fd8b9b`, the merge of v0.159 |
| Branch of this review | `codebase-review-2026-11-01` |
| Tests | 1,505 on this computer (1,070 in `Engine.Tests`, 275 in `3DEngine.Tests`, 157 in `3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`) |
| Pipeline | The run on `3fd8b9b`: green on Ubuntu, Windows and macOS, and the three determinism hashes are equal |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner decides.

**Method.** Ten tasks changed code or records after `e851093`, the commit of the last review: TASK-0148 (the review),
TASK-0152 and TASK-0153 (V2a, V2b), TASK-0149 to TASK-0151 (P0.63 to P0.65), TASK-0154 (P0.66), TASK-0155 (V3),
TASK-0156 (P0.67) and TASK-0157 (P8g). The change is about 6,000 lines in 98 files: an image on a datum with its
calibration, a PNG decoder and a contour proposal in the desktop, a hash of a native replay on each runner, an STL
export, and the fixes of the last review. Two review agents worked in parallel at `3fd8b9b`, each one in its own git
worktree: the engine with the hosts, and the desktop with the gates. Each agent wrote probes that only its worktree
holds. I ran the probes of the two agents again and read their output. I injected T123 and T124 with their controls,
and read the cited lines of each finding.

## Summary

- **The fixes of the last review hold for the forms that they named.** E113, E114, E116 to E118, V65 to V67, T118,
  T119 and T121 are fixed. T1, the oldest high finding, is fixed: the three runners give one hash of a native replay
  (TASK-0156). **[Observed]**
- **A PNG file that asks for a preset dictionary stops the desktop at each start (V68, high).** The decoder lets
  `ZLibException` escape, and the render loop ends at the first frame that draws the image. An agent can place such
  an image, and a saved document keeps it. **[Observed]** I ran the probes.
- **A region with a part of one pixel gives a sketch that does not extrude (V69, medium).** The trace goes out and
  back along the part, so the polygon touches itself. **[Observed]**
- **With an active sketch, the taps of a calibration go to the sketch (V70, medium).** Only a select reaches the
  calibration, and the mode Sketch gives a point. **[Observed]** for the intent; **[Inferred]** for the window.
- **The export of the command line holds the document file for a write (E119, medium).** It fails while the desktop
  holds the file, and it cuts an incomplete last line from the document. **[Observed]**
- **Each other new finding is low (E120 to E124, V71 to V73, T123 to T126).** The fix of E115 makes parallel puts
  of the same bytes fail on Windows, the comparison of the hashes passes with three empty files, and three fixes of the
  gates have a next form. **[Observed]**, except V73.
- **Open findings: 0 critical, 5 high, 17 medium and 28 low.** **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 15 | `.csproj` lines in `3DEngine.sln`. The last review gave 15. Git tracks 16 project files: the native package is not in the solution. |
| Production source lines | 23,210 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests`, `3DEngine.ImageTests` and `3DEngine.VisionTests`, 230 files. The last review gave 21,363 in 216 files. |
| Test source lines | 26,665 | `Engine.Tests` (19,162 in 101 files), `3DEngine.Tests` (3,065 in 21 files), `3DEngine.ImageTests` (4,373 in 26 files) and `3DEngine.VisionTests` (65 in 1 file). The last review gave 24,813. |
| Tests | 1,505 | `dotnet test 3DEngine.sln`: 1,070, 275, 157 and 3 passed, 0 failed, 0 skipped. The last review gave 1,420. |
| Gate classes | 19 | Tracked files whose name ends in `GateTests.cs`. `eng/repo-gates` gives 364 runs of 141 checks. The last review gave 19, and 353 runs. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 14 | Section "Open" of `docs/register.md`; the limit is 15. The last review gave 14, and 15 after its first run. |
| ADRs | 48 | `docs/adr/0001` to `0048`, with 0003 and 0011 in `docs/adr/archive/`. In force: 46 (24 accepted, 22 amended). None is proposed. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 5 | The same method: V2, V3, V68, T4, T31. The last review gave 5; T1 is fixed. |
| Findings: medium | 17 | The same method: E5, E6, E12, E13, E19, E21, E22, E119, V4, V69, V70, T2, T5, T10, T17, T32, T37. The last review gave 16. |
| Findings: low | 28 | The same method: E17, E24, E34, E39, E57, E84, E120 to E124, V7, V15, V25, V29, V71 to V73, T7, T13, T23, T25, T123 to T126, C1, P1. The last review gave 29. |
| Lines in the rule files | 2,583 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The last review gave 2,520. |
| Sentences with "must" | 58 | `grep -o -w -i must` on the same ten files. The last review gave 58. |
| Sentences with "do not" | 54 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 54. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found. E13, T32 and E84 are fixed in part and count as open. A finding that the owner accepted counts as closed.

## Findings of the previous review

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. No change: `Engine.Api.Http/Endpoints/JsonParameters.cs` has no commit since `e851093`. Register entry R-0027.
- **E6** — Open. `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` gained the three kinds of an image (ADR-0045 item 8, TASK-0152) and still lists `document.loaded`, `document.replayed` and `document.saved` (`:73-75`). Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. No change.
- **E8** — Fixed. TASK-0044, v0.37. No change.
- **E9** — Fixed, and it holds. No change.
- **E10** — Fixed. TASK-0035, v0.43. No change.
- **E11** — Fixed. TASK-0115, v0.115. No change.
- **E12** — Open, and smaller. TASK-0152 (v0.151) accepts a whole double up to 2^53 for an integer field (`Engine.Core/Hosting/ParameterBinder.cs:95`). The case `number` (`:77`) still refuses a `long` and an `int`; E5 still hides it. Register entry R-0027.
- **E13** — Fixed in part. No change.
- **E14** — Fixed. TASK-0115, v0.115. No change.
- **E15** — Fixed. TASK-0044, v0.37. No change.
- **E16** — Fixed. TASK-0115, v0.115. No change.
- **E17** — Open. `Engine.Core/CommandBus.cs` changed for the images only (TASK-0152). `PlaceImage` and `CalibrateImage` take the guard of a repeated identifier (`ImageResults.InUse`), as the sketch commands and the extrude do; the other commands do not. Register entry R-0028.
- **E18** — Fixed for the sinks of `Engine.*` (TASK-0140, v0.141) and of the desktop host (T118, TASK-0150, v0.154). Correction: the line of the last review lost the end of its code span.
- **E19** — Open, and smaller. No ledger entry since v0.149 names it. Register entry R-0033.
- **E20** — Fixed, and it holds. No change.
- **E21** — Open, and wider by decision. No ledger entry since v0.149 names it. Register entry R-0033.
- **E22** — Open. `Engine.Core/CommandBus.cs` changed for the images only (TASK-0152): the bus still stores each result. Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. No change.
- **E24** — Open in seven parts of nine. The cited files of the open parts changed only by additive lines for the images (`WireMessage.cs`, `SchemaEventsEndpoint.cs`, TASK-0152). Register entry R-0033.
- **E25** — Accepted by the owner. TASK-0142, v0.144. Register entry R-0036. No change.
- **E26** — Fixed. TASK-0135, v0.136. No change.
- **E27** — Accepted by the owner. TASK-0142, v0.144. Register entry R-0036. No change.
- **E28** — Fixed. TASK-0136, v0.137. No change.
- **E29** — Fixed for the replay. TASK-0135, v0.136. No change.
- **E30** — Fixed. TASK-0136, v0.137. No change.
- **E31** — Fixed for the replay. TASK-0137, v0.138. No change.
- **E32** — Fixed. TASK-0135, v0.136. No change.
- **E33** — Fixed with E18. TASK-0140, v0.141. No change.
- **E34** — Open. No change: `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs` has no commit since `e851093`. Register entry R-0036.
- **E35** — Fixed for the routes of `MapGet`. TASK-0139, v0.140. No change.
- **E36** — Fixed. TASK-0079, v0.85. No change.
- **E37** — Fixed. TASK-0137, v0.138. No change.
- **E38** — Fixed. TASK-0138, v0.139. No change.
- **E39** — Open. `Engine.Cli/Cli.cs` changed for the verb `export` (TASK-0157), which uses the one selection of the command line (`BuildSession`, `Cli.cs:348-366`). `Engine.Api.Http.Server/ServerEngine.cs:26-43` and `3DEngine/DesktopSurface.cs:113-121` hold the other two. Register entry R-0036.
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
- **E57** — Open. No change: `Engine.Tests/Http/DesktopSurfaceTests.cs` has no commit since `e851093`. Register entry R-0039.
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
- **V2** — Open. No change: `3DEngine.Vulkan/OverlayRenderer.cs` has no commit since `e851093`. Register entry R-0022.
- **V3** — Open. No change: `3DEngine.Vulkan/OverlayRenderer.cs` has no commit since `e851093`. Register entry R-0022.
- **V4** — Open. No ledger entry since v0.149 names it. Register entry R-0027.
- **V5** — Fixed. TASK-0076, v0.74. No change.
- **V6** — Fixed. TASK-0076, v0.74. No change.
- **V7** — Open. No change: `3DEngine.Vulkan/Ui/UiRenderer.cs`, `3DEngine.Vulkan/Ui/UiTexture.cs`, `3DEngine.Vulkan/GpuBuffer.cs` have no commit since `e851093`. Register entry R-0030.
- **V8** — Fixed. TASK-0074, v0.75. No change.
- **V9** — Fixed. TASK-0074, v0.75. No change.
- **V10** — Fixed. TASK-0074, v0.75. No change.
- **V11** — Fixed in part. TASK-0074, v0.75. No change.
- **V12** — Fixed, and changed by decision. No change.
- **V13** — Fixed for its parts. No change.
- **V14** — Fixed. TASK-0074, v0.75. No change.
- **V15** — Open. No change: `3DEngine/SessionBodies.cs` has no commit since `e851093`.
- **V16** — Fixed. TASK-0079, v0.85. No change.
- **V17** — Fixed. TASK-0079, v0.85. No change.
- **V18** — Fixed for its forms. TASK-0090, v0.93. No change.
- **V19** — Fixed, with no injection. TASK-0079, v0.85. No change.
- **V20** — Fixed. TASK-0079, v0.85. No change.
- **V21** — Fixed by reading, with no test. TASK-0079, v0.85. No change.
- **V22** — Fixed for its forms. TASK-0079, v0.85. No change.
- **V23** — Fixed. TASK-0079, v0.85. No change.
- **V24** — Fixed. TASK-0079, v0.85. No change.
- **V25** — Open. No change: `3DEngine.Vulkan/3DEngine.Vulkan.csproj` has no commit since `e851093`. Register entry R-0020.
- **T1** — Fixed. TASK-0156, v0.158: each runner of the job "Gate" replays one log with a cut, two moves and an extrude of a solved sketch on the native backend, and the job "Compare the determinism hashes" compares the three hashes. The three runners gave one hash. Register entry R-0023 is closed.
- **T2** — Open in one part of five. No ledger entry since v0.149 names it. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. No change.
- **T4** — Open. No change: `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs` has no commit since `e851093`. Register entry R-0031.
- **T5** — Open in one part of three. No ledger entry since v0.149 names it. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Open in four parts of five. No ledger entry since v0.149 names it. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. No change.
- **T10** — Open. No ledger entry since v0.149 names it. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51. No change.
- **T12** — Fixed. TASK-0059, v0.55. No change.
- **T13** — Open in part. `.github/workflows/ci.yml` changed for the hash of the replay only (TASK-0156). The part of the marker gate stays. Register entry R-0031.
- **T14** — Fixed. TASK-0053, v0.51. No change.
- **T15** — Fixed. TASK-0050, v0.47. No change.
- **T16** — Fixed. TASK-0066, v0.67. No change.
- **T17** — Open. No ledger entry since v0.149 names it. Register entry R-0031.
- **T18** — Fixed. TASK-0049, v0.46. No change.
- **T19** — Fixed. TASK-0049, v0.46. No change.
- **T20** — Fixed. TASK-0049, v0.46. No change.
- **T21** — Fixed. TASK-0049, v0.46. No change.
- **T22** — Fixed. TASK-0049, v0.46. No change.
- **T23** — Open. No ledger entry since v0.149 names it. Register entry R-0031.
- **T24** — Fixed. No change.
- **T25** — Open in part. No ledger entry since v0.149 names it. Register entry R-0031.
- **T26** — Fixed. TASK-0057, v0.54. No change.
- **T27** — Fixed. TASK-0058, v0.56. No change.
- **T28** — Fixed. No change.
- **T29** — Fixed. TASK-0057, v0.54. No change.
- **T30** — Fixed. TASK-0057, v0.54. No change.
- **T31** — Open. No change: `eng/repo-gates/RepoGates.csproj` has no commit since `e851093`. Register entry R-0031.
- **T32** — Fixed in part. No change.
- **T33** — Fixed. TASK-0057, v0.54. No change.
- **T34** — Fixed. TASK-0057, v0.54. No change.
- **T35** — Fixed. TASK-0057, v0.54. No change.
- **C1** — Open. No change: `Engine.Contracts/Geometry/IBRepOps.cs`, `Engine.Contracts/Geometry/IFeatureIdMap.cs` have no commit since `e851093`. Register entry R-0027.
- **P1** — Open, and smaller. No ledger entry since v0.149 names it. Register entry R-0027.
- **T36** — Fixed. TASK-0061, v0.61. No change.
- **T37** — Open in part. No ledger entry since v0.149 names it. Register entry R-0031.
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
- **V29** — Open. No ledger entry since v0.149 names it.
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
- **E86** — Fixed. TASK-0106, v0.110. No change.
- **E87** — Fixed. TASK-0106, v0.110. No change.
- **E88** — Fixed. TASK-0106, v0.110. No change.
- **E89** — Fixed by record. TASK-0106, v0.110. No change.
- **E90** — Fixed by record. TASK-0106, v0.110. No change.
- **E91** — Fixed. TASK-0106, v0.110. No change.
- **E92** — Fixed. TASK-0106, v0.110. No change.
- **E93** — Fixed. TASK-0106, v0.110. No change.
- **V40** — Fixed. TASK-0106, v0.110. No change.
- **V41** — Fixed. TASK-0106, v0.110. No change.
- **V42** — Fixed. TASK-0106, v0.110. No change.
- **V43** — Fixed. TASK-0106, v0.110. No change.
- **V44** — Fixed. TASK-0106, v0.110. No change.
- **V45** — Fixed. TASK-0106, v0.110. No change.
- **T79** — Fixed for its forms. TASK-0105, v0.107. No change.
- **T80** — Fixed for its form. TASK-0107, v0.111. No change.
- **T81** — Fixed for its forms. TASK-0107, v0.111. No change.
- **T82** — Fixed for its forms. TASK-0107, v0.111. No change.
- **T83** — Fixed for its forms. TASK-0107, v0.111. No change.
- **T84** — Fixed for its form. TASK-0107, v0.111. No change.
- **T85** — Fixed for its form. TASK-0107, v0.111. No change.
- **T86** — Fixed for its forms. TASK-0107, v0.111. No change.
- **T87** — Fixed. TASK-0107, v0.111. No change.
- **E94** — Fixed. TASK-0114, v0.118. No change.
- **E95** — Fixed. TASK-0114, v0.118. No change.
- **E96** — Fixed. TASK-0114, v0.118. No change.
- **V46** — Fixed. TASK-0118, v0.120. No change.
- **V47** — Fixed. TASK-0118, v0.120. No change.
- **V48** — Fixed. TASK-0118, v0.120. No change.
- **V49** — Fixed. TASK-0118, v0.120. No change.
- **V50** — Fixed. TASK-0118, v0.120. No change.
- **T88** — Fixed. TASK-0112, v0.114. No change.
- **T89** — Fixed. TASK-0114, v0.118. No change.
- **T90** — Fixed. TASK-0114, v0.118. No change.
- **T91** — Fixed. TASK-0114, v0.118. No change.
- **T92** — Fixed. TASK-0114, v0.118. No change.
- **T93** — Fixed. TASK-0114, v0.118. No change.
- **T94** — Fixed for the full names. TASK-0114, v0.118. No change.
- **T95** — Fixed for the full names. TASK-0114, v0.118. No change.
- **E97** — Fixed for its forms. TASK-0121, v0.123. No change.
- **E98** — Fixed. TASK-0123, v0.125. No change.
- **E99** — Fixed. TASK-0123, v0.125. No change.
- **E100** — Fixed for its form. TASK-0123, v0.125. No change.
- **E101** — Fixed. TASK-0123, v0.125. No change.
- **V51** — Fixed. TASK-0124, v0.126. No change.
- **V52** — Fixed. TASK-0124, v0.126. No change.
- **V53** — Fixed for a started drag. TASK-0124, v0.126. No change.
- **V54** — Fixed. TASK-0124, v0.126. No change.
- **V55** — Fixed. TASK-0124, v0.126. No change.
- **T96** — Fixed for its forms. TASK-0123, v0.125. No change.
- **T97** — Fixed. TASK-0123, v0.125. No change.
- **T98** — Fixed. TASK-0123, v0.125. No change.
- **T99** — Fixed for its four forms. TASK-0123, v0.125. No change.
- **T100** — Fixed for the full names. TASK-0123, v0.125. No change.
- **T101** — Fixed for the item Compile. TASK-0123, v0.125. No change.
- **T102** — Fixed for its bytes. TASK-0123, v0.125. No change.
- **E102** — Fixed for the solver. TASK-0130, v0.132. No change.
- **E103** — Fixed. TASK-0132, v0.134. No change.
- **E104** — Fixed. TASK-0132, v0.134. No change.
- **E105** — Fixed for the routes. TASK-0132, v0.134. No change.
- **V56** — Fixed. TASK-0133, v0.135. No change.
- **V57** — Fixed. TASK-0133, v0.135. No change.
- **V58** — Fixed for the wheel and undo. TASK-0133, v0.135. No change.
- **V59** — Fixed. TASK-0133, v0.135. No change.
- **V60** — Fixed. TASK-0133, v0.135. No change.
- **V61** — Fixed for an end on the ray of the start. TASK-0133, v0.135. No change.
- **V62** — Fixed. TASK-0133, v0.135. No change.
- **T103** — Fixed for the implicit usings. TASK-0130, v0.132. No change.
- **T104** — Fixed for the implicit usings. TASK-0132, v0.134. No change.
- **T105** — Fixed. TASK-0132, v0.134. No change.
- **T106** — Fixed for its forms. TASK-0132, v0.134. No change.
- **T107** — Fixed for a literal item name. TASK-0132, v0.134. No change.
- **T108** — Fixed for its characters. TASK-0132, v0.134. No change.
- **T109** — Fixed for its forms. TASK-0132, v0.134. No change.
- **E106** — Fixed. TASK-0142, v0.144. No change.
- **E107** — Fixed. TASK-0142, v0.144. No change.
- **E108** — Fixed for the circle and the constraint `radius`. TASK-0142, v0.144. No change.
- **E109** — Fixed. TASK-0142, v0.144. No change.
- **E110** — Fixed. TASK-0142, v0.144. No change.
- **E111** — Fixed. TASK-0142, v0.144. No change.
- **E112** — Fixed. TASK-0142, v0.144. No change.
- **V63** — Fixed. TASK-0144, v0.146. No change.
- **V64** — Fixed for the drag. TASK-0144, v0.146. No change.
- **T110** — Fixed. TASK-0143, v0.145. No change.
- **T111** — Fixed. TASK-0143, v0.145. No change.
- **T112** — Fixed for the assemblies in the folder of the test project. TASK-0143, v0.145. No change.
- **T113** — Fixed for its characters. TASK-0143, v0.145. No change.
- **T114** — Fixed for its forms. TASK-0143, v0.145. No change.
- **T115** — Fixed for a property. TASK-0143, v0.145. No change.
- **T116** — Fixed for the left operand of an addition. TASK-0143, v0.145. No change.
- **E113** — Fixed. TASK-0149, v0.153.
- **E114** — Fixed. TASK-0149, v0.153.
- **E115** — Fixed. TASK-0149, v0.153. The replace of the move makes parallel puts of the same bytes fail on Windows (E120).
- **E116** — Fixed. TASK-0149, v0.153.
- **E117** — Fixed. TASK-0149, v0.153.
- **E118** — Fixed. TASK-0149, v0.153.
- **V65** — Fixed. TASK-0151, v0.155.
- **V66** — Fixed. TASK-0151, v0.155.
- **V67** — Fixed. TASK-0151, v0.155.
- **T117** — Fixed for `PackageReference`. TASK-0150, v0.154. A `GlobalPackageReference` passes (T124).
- **T118** — Fixed. TASK-0150, v0.154.
- **T119** — Fixed. TASK-0150, v0.154.
- **T120** — Fixed for a `const char` that is an operand. TASK-0150, v0.154. A lower-case or an indexed character passes, and a comparison with a constant character is a false part (T126).
- **T121** — Fixed. TASK-0150, v0.154.
- **T122** — Fixed for one line above the underline. TASK-0150, v0.154. A list item of two lines and a Greek letter pass (T125).

## 1 Current architecture

**[Observed]** The Document holds an image on a datum: the name of its asset, its datum, its origin, its size in
pixels and its scale (`Engine.Contracts/Images/ImageRecord.cs`, ADR-0045). `PlaceImage` and `CalibrateImage` change it,
the bus gives `image.created`, `image.changed` and `image.removed`, and an undo swaps the record. A calibration
records two points in pixels and a distance, and the engine computes the scale with `+ - * / sqrt`.

**[Observed]** The desktop decodes a PNG file with its own reader (`3DEngine.Core/Imaging/PngReader.cs`, ADR-0046),
draws each live image on its datum, and calibrates it by two taps. It proposes the contour of the largest region of
an image with its own code (`3DEngine.Core/Vision/ContourProposal.cs`, ADR-0047), and an accept sends one
`CreateSketch` and the points and the lines of the contour. The vision library stays in its test project (R-0048).

**[Observed]** `StlExport` in `Engine.Core/Export` writes the live bodies as one binary STL file (ADR-0048). The
surface gives `GET /export/stl`, the command line gives `engine export stl`, and the desktop gives a button. Each
runner of the job "Gate" hashes a native replay, and the job "Compare the determinism hashes" compares the three
(TASK-0156).

## 2 Patterns in use

- **A reader of outside bytes trusts the list of its own exceptions.** The PNG reader catches one exception of the
  decompressor and lets the next one escape, and the projection of the images catches only the exception that the
  ADR names (V68). **[Observed]**
- **A new verb reuses a path that was made for another purpose.** The export of the command line opens the document
  file as an edit does (E119), and it gives the codes of a load and of a save (E122). **[Observed]**
- **Each correction holds for its forms, and a probe finds the next form.** E120 follows E115, T124 follows T117,
  T125 follows T122 and T126 follows T120. **[Observed]**

## 3 Review of the code

### The engine and the hosts

#### E119 · Medium · The export of the command line holds the document file for a write · Confirmed by a run

`Engine.Cli/Cli.cs:152` calls `DocumentFile.Open`, which attaches a journal and holds the file with write access. A
document file whose last line is not complete gives exit 0, and the file goes from 326 bytes to 308: the export cut
the line. A read-only file and a file that another session holds, as the desktop does, each give exit 1 with
`E-IO-LOAD-FAILED`. The warning `W-IO-TRUNCATED` of the open is not in the output. ADR-0048 item 5 says that the verb
opens the document file and writes the STL file, and nothing more. **[Observed]** I ran the probe on Windows.
**[Recommended]** Load the file with no journal and with read access only, as `DocumentFile.Check` does with
`attach: false`, and give the warnings of the open in the output.

#### E120 · Low · Parallel puts of the same new bytes fail on Windows since the fix of E115 · Confirmed by a run

`Engine.Core/Assets/AssetStore.cs:79` moves the scratch file with `overwrite: true`. While another put reads or
replaces the file, Windows refuses the replace, and `HoldsName` (`:110`) cannot read the file, so the filter at `:81`
gives false. The put gives `E-ASSET-WRITE-FAILED`, which is a 500 from `PUT /assets`; the file is correct after it.
In 40 rounds of 8 parallel puts of 2 MB, the put of v0.159 failed 20 of 320 times, and the put of v0.149 in the same
run failed 0 times. A good file that another program holds with `FileShare.None` also gives the error now.
**[Observed]** I ran the probe on Windows; Linux and macOS are not verified. **[Recommended]** Move with no overwrite
first; replace only a file that does not hash to its name, and read the name again before a failure.

#### E121 · Low · The STL export writes infinite floats, a normal that is not a number, and an overflow · Confirmed by a run

`Engine.Core/Export/StlExport.cs:76` narrows each coordinate to a float. After `Translate` with `dx = 1e39`, the
export of a box gives 12 triangles with 36 infinite floats. A triangle of 1e200 gives the normal `0 0 NaN` (`:71`).
At 43 million triangles, `84 + 50 * triangles` (`:38`) throws `OverflowException` outside the try at `:32`.
ADR-0048 item 1 promises a unit normal, and items 4 and 5 promise the envelope and an exit code. **[Observed]** for
the bytes and the exception; **[Inferred]** for the 500 and the exit code. **[Recommended]** Refuse a vertex or a
normal that is not finite as a float and a count over the limit of the file, each with a code.

#### E122 · Low · The export gives two codes of the document file with a meaning that their rows do not hold · Confirmed by a run

`Engine.Cli/Cli.cs:164` gives `E-IO-SAVE-FAILED` when the STL file cannot be written, and `E-IO-LOAD-FAILED` for an
empty path of the document, with the message "The STL file of  could not be made at …". The row of
`E-IO-SAVE-FAILED` in `docs/diagnostics.md` names a save of a document file to a new name only, and the rows of
`E-GEOM-CAP-MISSING` and `E-GEOM-NATIVE-OP` do not name `StlExport`. `CLAUDE.md` says "Do not change the meaning of a
code." **[Observed]** I ran the probe. **[Recommended]** Add the export to the three rows, or register a code of the
export, and give an empty path its own message.

#### E123 · Low · A calibration refuses two different finite points when the square of their distance underflows or overflows · Confirmed by a run

`Engine.Core/Commands/CalibrateImageCommandHandler.cs:77-78` computes `Math.Sqrt(dx * dx + dy * dy)`. The points 0
and 1e-170 and the points 0 and 1e160 each give `E-IMAGE-CALIBRATION-INVALID`, whose message asks for "two different
points … with finite coordinates". A test holds the overflow at the limit of a double, so that side was a choice; the
underflow side and the message are not. No real image has such points. **[Observed]** I ran the probe.
**[Recommended]** Divide `dx` and `dy` by the larger of their magnitudes before the root, or give the limit in
ADR-0045 item 6 and in the message.

#### E124 · Low · The comment of the route says that a failed body is not a fault of the store, and the route gives 500 · Confirmed by a run

`Engine.Api.Http/Endpoints/AssetsEndpoint.cs:49-50`. A chunked body with a bad size gives a 500 with
`E-ASSET-READ-FAILED` and the message "Bad chunk size data". The row of the code says 500, so only the comment is
false. **[Observed]** I ran the probe on Kestrel at 127.0.0.1. **[Recommended]** Give 400 for a body that the server
cannot read, or correct the comment.

### The render side and the desktop

#### V68 · High · A PNG file that asks for a preset dictionary stops the desktop at each start · Confirmed by a run

`3DEngine.Core/Imaging/PngReader.cs:107-123` (`Inflate`) catches `EndOfStreamException` only. A zlib header with the
flag FDICT (`78 BB` and four bytes) makes `ZLibStream` throw `ZLibException`, an `IOException`, and not the
`InvalidDataException` of ADR-0046 item 1. `3DEngine/SessionImages.cs:116` and `3DEngine/ImageActions.cs:64` catch
`InvalidDataException` only. An agent can put such bytes through `PUT /assets` and send `PlaceImage`; then
`SessionImages.Apply` throws in `Render`, and the catch of `3DEngine/DesktopEngineHost.cs:87`, outside the loop of the
frames, writes "Error:" and ends the window. The document keeps the image, so the next start stops at its first frame.
The controls, a bad Adler sum, a window of 64 KiB, random data and an empty stream, each give `InvalidDataException`.
**[Observed]** I ran the probes; **[Inferred]** for a live window, from the catch of the host. **[Recommended]**
`Inflate` gives `InvalidDataException` for each exception of the decompressor, and a test holds the flag FDICT.

#### V69 · Medium · A region with a neck or a spur of one pixel gives a sketch that does not extrude · Confirmed by a run

`3DEngine.Core/Vision/ContourProposal.cs:160-199` (`Trace`) is a Moore trace of eight neighbours, so it goes out and
back along a part of the region that is one pixel wide, and the simplification keeps the tip. A square with a spur of
one pixel gives seven vertices with (60.5, 25.5) two times; the accept is applied, and `Extrude` gives
`E-SKETCH-PROFILE-INVALID`, "cross or touch". Two squares with a neck of one pixel give the same. The union of three
random discs gave 2 polygons that are not simple in 400, and discs with bars of 1 to 3 pixels gave 16 in 300. The
control, a rectangle, extrudes. **[Observed]** I ran the probes. **[Recommended]** See Q3.

#### V70 · Medium · With an active sketch, the taps of a calibration go to the sketch · Confirmed in part by a run

`3DEngine/NativeThreeDEngine.cs:147` gives `ImageActions.Tap` a `SelectIntent` only. In the mode Sketch a click gives
`SketchHoverIntent` and `SketchPointIntent`, and no select; the point goes to the tool (`:173`). An accept makes its
sketch active with the tool Line (`3DEngine/SketchActions.cs:52`), so Calibrate after an accept adds two points and a
line to the Document, and the window still shows "0 of 2". ADR-0046 item 4 says that the next two taps are points of
the image. **[Observed]** for the intents, by a probe; **[Inferred]** for the window, from the routing.
**[Recommended]** A calibration takes the next two point intents of each kind before the tool, or the start of a
calibration ends the active sketch.

#### V71 · Low · Two taps on one pixel lock the calibration · Confirmed by a run

`3DEngine/ImageActions.cs:204`. The engine refuses the calibration, the desktop keeps its two points, and each next
tap goes nowhere; the message does not say that Cancel is necessary. **[Observed]** I ran the probe.
**[Recommended]** Clear the points when the engine refuses the calibration.

#### V72 · Low · The name "part.stl" gives the file "part.stl.stl" · Confirmed by a run

`3DEngine/NativeThreeDEngine.cs:376-378`: `Resolve` adds `.3de`, and `ChangeExtension` then gives `.stl.stl`. The
names "part" and "part.3de" give "part.stl". **[Observed]** I ran the probe. **[Recommended]** Remove a final `.stl`
from the name before `Resolve`.

#### V73 · Low · An accept that fails between its commands leaves a part of a sketch · Inferred

`3DEngine/ImageActions.cs:77` (`Accept`) sends 1 + 2n commands and does not undo the applied ones when one fails, for
example when an agent removes the sketch between two of them. The Document then keeps a sketch with some points and
no lines, and the desktop makes it active. ADR-0047 item 4 names the undo of many steps, and no partial result.
**[Inferred]** from the code; no probe forced the order. **[Recommended]** A test with a fake session that refuses one
command, and an accept that stops and says which items exist.

### Tests, gates and the pipeline

#### T123 · Low · The comparison of the hashes passes with three empty files · Confirmed by an injection

`eng/compare-determinism.js:27-32` removes each empty line, so an empty file gives `undefined` as its hash, and three
`undefined` are equal. I injected three empty files: "The three runners give one hash.", exit 0. The control, one
different hash, gave exit 1. With one empty file the script blames "a mesh or its indices". The test always writes
the hash first, so no run reaches it now. **[Observed]** **[Recommended]** Refuse a first line that is not 64
hexadecimal digits, and name the runner.

#### T124 · Low · A `GlobalPackageReference` pins the vision library into the engine, and the gate passes · Confirmed by an injection

`Engine.Tests/Governance/DependencyDirectionGateTests.cs:927` and `:1081` read the items `PackageReference` only. I
added `Engine.Core/Directory.Packages.props` with central versions and a `GlobalPackageReference` of `OpenCvSharp4`:
`dotnet msbuild -getItem:PackageReference` gave `OpenCvSharp4`, and `DependencyDirectionGateTests` passed 118 of 118.
The control, a `PackageReference` in the same file, failed `Only_The_Vision_Spike_Pins_The_Vision_Library`. The rule
of the Vulkan binding reads in the same way. **[Observed]** **[Recommended]** The two rules read
`GlobalPackageReference` too, or refuse it.

#### T125 · Low · Two next forms pass the rule of a setext heading of a version · Confirmed by a run

`Engine.Tests/Governance/CodebaseReviewGateTests.cs:381` looks for a version on the line just above the underline. A
list item of two lines with the version on its first line, and `ν0.11` with a Greek nu, each give no problem; the
control gives one. **[Observed]** by a probe of the agent, which I ran. **[Recommended]** Read the paragraph back to
its first line, and refuse a letter that is not ASCII before the digits of a version.

#### T126 · Low · The scanner of parts of codes misses two next forms and finds a false part · Confirmed by a run

`Engine.Tests/Diagnostics/DiagnosticsScanner.cs:99`. `char.ToUpperInvariant('e')` and `"EWI"[0]` in an interpolation
give no part, and `c == 'E'` in a parser of numbers gives the part "E". The comment of the scanner says that another
spelling stays a rule in text. **[Observed]** by a probe of the agent, which I ran. **[Recommended]** Count a constant
character only as an operand of a concatenation, an interpolation or a call that joins text, and keep the other
spellings as a rule in text.

### Correct

**[Observed]** by the agents, with the probes that passed:

- A log with two places, a calibration, three undos, a redo and more gives the same images in the same order live and
  after an open. An undo of `PlaceImage` gives `image.removed`, and a redo gives `image.created`.
- A distance of infinity, 0, a negative number or NaN is refused by `CalibrateImage`. The scale uses `+ - * / sqrt`.
- Each of the eight new codes is in `docs/diagnostics.md` and in `/schema/diagnostics`, and each one is reachable.
- E114, E116 and E118 hold: a `Content-Length` of 31 MB with no body and a chunked body of 17 MiB each give 413 with
  `E-ASSET-TOO-LARGE`, and a put cancelled during its read leaves no scratch file. No other `$` ends a pattern in the
  engine. E117 is safe for NaN.
- A degenerate triangle gives the normal `0 0 0`. 30 parallel boxes and 30 parallel exports give files of whole
  boxes. Hidden bodies are in the file, in the order of the handles.
- The upload of a hash uses `if-no-files-found: error`, and the script reads the three runners.
- The PNG reader refuses each malformed form that the tests list, and the inflate reads at most one byte more than
  the rows, so a bomb of zlib stops. A picture of 4096 × 4096 reads in 980 ms.
- The proposal gives no contour, and no exception, for one pixel, one grey, a checkerboard of 2 × 2 and one pixel of
  another class. A gear of 4096 pixels gives a simple polygon.
- T118: a throwing sink in `3DEngine` fails `DesktopSinkContractTests`. T119: an escape in an item name is refused.
  V65 to V67: Escape, a second finger, a change of mode and a loss of focus each end a press with nothing.
- `ContourImageTests` is in the collection of Dear ImGui. No new file calls a function of rule 1, 3 or 4 of
  `CLAUDE.md`. Serial commit and event order do not change.

**Not verified.** E120 on Linux and macOS; V68 and V70 in a live window; whether `Trace` drops its start pixel when it
passes through it; a crossing that the simplification makes alone; the runs of GitHub Actions on the injections.

## 4 The next planned task

No product phase is named. The order of the owner of 2026-10-31 ends with this review, and no task is `Ready`.
`docs/templates.md`, section 7, puts a high finding before the next product phase, so P0.68 (TASK-0159) holds V68 and
comes first. P0.69 (TASK-0160) holds the findings of the engine, P0.70 (TASK-0161) those of the gates, and P0.71
(TASK-0162) those of the desktop; they come after the next product phase. Q1 asks the owner for that phase.

## 5 Research for that task

**[Observed]** I read the cited code for the recommended answer of Q1.

- **No command edits an applied feature, apart from a sketch.** The commands are `CreateBox`, `Translate`,
  `Subtract`, `Extrude`, the sketch commands, `RenameBody`, `SetBodyVisibility`, the image commands, `Undo` and
  `Redo`. Objective 11 asks for editable parametric CAD; today a distance of an extrude changes only by an undo.
- **The rebuild of ADR-0039 reads the distance from the log.** `Engine.Core/Sketch/SketchRebuild.cs:71` and `:212`
  read `extrude.Distance` of the `ExtrudeCommand` in effect. `FeatureGraph.Of` gives the creator and the consumer of
  each handle, so a seed can be an edited extrude in place of an edited sketch.
- **The log holds the inputs (ADR-0033, Q6).** A command that records the identifier of the extrude and the new
  distance is an input, and a replay gives the same rebuild. An undo stays a swap of records (ADR-0031).
- **No change of `Engine.Contracts` is necessary.** `ExtrudeCommand` is in `Engine.Core/Commands/`, and
  `ExtrudeParameters` in `Engine.Contracts/Geometry` already takes a `Height`. A new command needs an ADR, because it
  changes the rule of ADR-0039 item 2 for a seed.

## 6 Recommended approach

**[Recommended]** In this order:

1. P0.68 (TASK-0159): V68, with a test of the flag FDICT and of each exception of the decompressor. 1 agent hour.
2. The phase of Q1. With A: K0f, an ADR and a command that changes the distance of a live extrude, with the rebuild
   of each descendant, then the pad of the desktop (objective 9).
3. P0.69 (TASK-0160): E119 to E124, each with a test before the change. 2 to 3 agent hours.
4. P0.70 (TASK-0161): T123 to T126, each with an injection before the change. 1 to 2 agent hours.
5. P0.71 (TASK-0162): V69 to V73, with V69 after Q3. 2 to 3 agent hours.

## Questions for the owner

**Q1. The next product phase.**
- A: K0f. A command changes the distance of a live extrude, and the bus rebuilds the extrude and each descendant, as
  K0d does for an edit of a sketch. The desktop gives the pad of the distance.
- B: R11, the rest of the escalated selection and the snap engine.
- C: A phase V4 of machine vision, with a content that the owner names.
- D: P8d, the three layers: the log, an ordered list of features and a cache of the regeneration.

**Recommended: A.** It is the smallest step to objective 11 after the milestone "CAD on one part", it reuses the
graph and the rebuild of ADR-0039, and it ends in one session (objective 12). P8d and the rest of K0 are larger.

**Q2. What the edit of K0f records.**
- A: One command, for example `SetExtrudeDistance`, that records the identifier of the extrude and the new distance.
  The rebuild reads the last distance in effect for each extrude.
- B: No new command: the client sends an undo of the extrude and a new extrude.

**Recommended: A.** B loses each descendant of the extrude, and a new extrude gives a new handle, so a later
command that names the old handle fails.

**Q3. What the proposal does with a part of one pixel (V69).**
- A: The proposal removes each part of the region that is one pixel wide (an opening with a cross of 3 × 3) before
  the trace, and refuses a polygon that is still not simple, with a reason.
- B: The proposal refuses a polygon that is not simple, with a reason, and changes nothing else.

**Recommended: A.** A drawn outline of a part often has a thin line at its edge, so B refuses many real images; A
changes only parts that cannot be a wall of a solid.
