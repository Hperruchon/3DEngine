---
date: 2026-11-03
commit: 1c3fb7c
ledger: v0.168
previous: 2026-11-01-codebase-review.md
---

# Codebase review — 2026-11-03

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the nineteenth codebase review. The review of 2026-11-01 examined v0.159. After it came 4 product milestones
(K0f, K1, R11c and K0g) and 9 ledger entries (v0.160 to v0.168). The limits are four and twelve, so the next product
milestone, K0h, would make the review late. On 2026-11-03 the owner told the session: "do the review, then close K0h".
`Engine.Tests/Governance/CodebaseReviewGateTests.cs` holds the rule, and `docs/templates.md`, section 7, gives the
form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is the
authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this file does not.

| Item | Value |
|---|---|
| Code state | `main` at `1c3fb7c`, the merge of v0.168 |
| Branch of this review | `codebase-review-2026-11-03` |
| Tests | 1,588 on this computer (1,127 in `Engine.Tests`, 289 in `3DEngine.Tests`, 169 in `3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`) |
| Pipeline | The run on `1c3fb7c` (41540743166): green on Ubuntu, Windows and macOS, and the three determinism hashes are equal |
| Code changed by the review | none |
| Not in this review | K0h (TASK-0167, ADR-0052), which is on its branch and closes after this review |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner decides.

**Method.** Nine tasks changed code or records after `3fd8b9b`, the commit of the last review: TASK-0158 (the review),
TASK-0159 to TASK-0162 (P0.68 to P0.71), TASK-0163 (K0f), TASK-0164 (K1), TASK-0165 (R11c) and TASK-0166 (K0g). The
change is about 5,300 lines in 95 files, 3,000 of them in 64 C# files: the edit of the distance of an extrude, exact
predicates for the profile of a sketch, the snap to an intersection, a datum on a face of a feature, and the fixes of
the last review. Two review agents worked in parallel at `1c3fb7c`, each one in its own git worktree: the engine with
the hosts, and the desktop with the gates. Each agent wrote probes that only its worktree held, and deleted them. I
read the cited lines of each finding, and I measured the values below.

## Summary

- **The fixes of the last review hold for the forms that they named.** V68, the high finding of the PNG reader, is
  fixed. E119, E122, E124, V70, V71 and T123 are fixed, and E121, E123, V69, V72, V73 and T124 to T126 are fixed for
  their forms. E120 is fixed for the parallel puts, and its form of a held file stays open. **[Observed]**
- **A datum on the side of a very short or a very long line gets a frame that is not finite (E125, low).** The command
  applies with no fault, the extrude on the datum fails in the backend, and `GetDatum` gives status 500 over HTTP.
  **[Observed]** by a probe.
- **A rebuild gives the code of a sketch edit to an extrude of another sketch on a moved datum, and the rows of three
  codes do not say so (E126, low).** **[Observed]**
- **A datum with a fault gives its warning again at each later edit, also an edit that has no relation to it (E127,
  low).** **[Observed]** by a probe.
- **In the desktop, the mark of the snap shows during a calibration, and the snap to an intersection costs 3.2 ms in
  each frame over a sketch of 200 lines (V74, V75, low).** **[Observed]** by reading and by a probe.
- **Three gates have a next form (T127 to T129, low).** A computed `PackageReference` pins the vision library into
  `Engine.Core`, two forms of a built code escape the scanner, and two spellings of a version escape the rule of a
  setext heading. **[Observed]** by injections.
- **No new finding is critical, high or medium.** **[Observed]**
- **Open findings: 0 critical, 4 high, 14 medium and 25 low.** **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 15 | `.csproj` lines in `3DEngine.sln`. The last review gave 15. |
| Production source lines | 24,461 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests`, `3DEngine.ImageTests` and `3DEngine.VisionTests`, 239 files. The last review gave 23,210 in 230 files. |
| Test source lines | 28,303 | `Engine.Tests` (20,412 in 105 files), `3DEngine.Tests` (3,250 in 21 files), `3DEngine.ImageTests` (4,576 in 27 files) and `3DEngine.VisionTests` (65 in 1 file). The last review gave 26,665. |
| Tests | 1,588 | `dotnet test 3DEngine.sln`: 1,127, 289, 169 and 3 passed, 0 failed, 0 skipped. The last review gave 1,505. |
| Gate classes | 19 | Tracked files whose name ends in `GateTests.cs`. `eng/repo-gates` gives 370 runs of 143 checks. The last review gave 19, and 364 runs. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 14 | Section "Open" of `docs/register.md`; the limit is 15. The last review gave 14. |
| ADRs | 51 | `docs/adr/0001` to `0051`, with 0003 and 0011 in `docs/adr/archive/`. In force: 49 (25 accepted, 24 amended). None is proposed. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 4 | The same method: V2, V3, T4, T31. The last review gave 5; V68 is fixed. |
| Findings: medium | 14 | The same method: E5, E6, E12, E13, E19, E21, E22, V4, T2, T5, T10, T17, T32, T37. The last review gave 17; E119, V69 and V70 are fixed. |
| Findings: low | 25 | The same method: E17, E24, E34, E39, E57, E84, E120, E125 to E127, V7, V15, V25, V29, V74, V75, T7, T13, T23, T25, T127 to T129, C1, P1. The last review gave 28. |
| Lines in the rule files | 2,630 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The last review gave 2,583. |
| Sentences with "must" | 58 | `grep -o -w -i must` on the same ten files. The last review gave 58. |
| Sentences with "do not" | 54 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 54. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found. E13, T32, E84 and E120 are fixed in part and count as open. A finding that the owner accepted counts as closed.

## Findings of the previous review

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. No change: `Engine.Api.Http/Endpoints/JsonParameters.cs` has no commit since `3fd8b9b`. Register entry R-0027.
- **E6** — Open. `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` gained the three kinds of a datum (ADR-0051 item 9, TASK-0166) and still lists `document.loaded`, `document.replayed` and `document.saved` (`:79-81`), which no source emits. Register entry R-0027.
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
- **E17** — Open, and smaller. `Engine.Core/CommandBus.cs` changed for the datums only (TASK-0166). `CreateDatum` refuses a repeated identifier with its own code (`E-DATUM-ID-IN-USE`), and `SetExtrudeDistance` with `E-FEATURE-ID-IN-USE` (TASK-0163); `CreateBox`, `Translate`, `Subtract`, `RenameBody` and `SetBodyVisibility` still do not. Register entry R-0028.
- **E18** — Fixed for the sinks of `Engine.*` (TASK-0140, v0.141) and of the desktop host (T118, TASK-0150, v0.154). Correction: the line of the last review lost the end of its code span. No change.
- **E19** — Open, and smaller. No ledger entry since v0.159 names it. Register entry R-0033.
- **E20** — Fixed, and it holds. No change.
- **E21** — Open, and wider by decision. No ledger entry since v0.159 names it. Register entry R-0033.
- **E22** — Open. `Engine.Core/CommandBus.cs` changed for the datums only (TASK-0166): the bus still stores each result in the idempotency cache (`:35`, `:63-64`). Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. No change.
- **E24** — Open in seven parts of nine. The cited files of the open parts changed only by additive lines for the images (`WireMessage.cs`, `SchemaEventsEndpoint.cs`, TASK-0152) and for the datums (the same two files, TASK-0166). Register entry R-0033.
- **E25** — Accepted by the owner. TASK-0142, v0.144. Register entry R-0036. No change.
- **E26** — Fixed. TASK-0135, v0.136. No change.
- **E27** — Accepted by the owner. TASK-0142, v0.144. Register entry R-0036. No change.
- **E28** — Fixed. TASK-0136, v0.137. No change.
- **E29** — Fixed for the replay. TASK-0135, v0.136. No change.
- **E30** — Fixed. TASK-0136, v0.137. No change.
- **E31** — Fixed for the replay. TASK-0137, v0.138. No change.
- **E32** — Fixed. TASK-0135, v0.136. No change.
- **E33** — Fixed with E18. TASK-0140, v0.141. No change.
- **E34** — Open. No change: `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs` has no commit since `3fd8b9b`. Register entry R-0036.
- **E35** — Fixed for the routes of `MapGet`. TASK-0139, v0.140. No change.
- **E36** — Fixed. TASK-0079, v0.85. No change.
- **E37** — Fixed. TASK-0137, v0.138. No change.
- **E38** — Fixed. TASK-0138, v0.139. No change.
- **E39** — Open. `Engine.Cli/Cli.cs` changed for the export (TASK-0160), and the verbs still use the one selection of the command line (`BuildSession`, `Cli.cs:150`, `:257`, `:311`). `Engine.Api.Http.Server/ServerEngine.cs` and `3DEngine/DesktopSurface.cs` hold the other two, with no commit since `3fd8b9b`. Register entry R-0036.
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
- **E57** — Open. No change: `Engine.Tests/Http/DesktopSurfaceTests.cs` has no commit since `3fd8b9b`. Register entry R-0039.
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
- **V2** — Open. No change: `3DEngine.Vulkan/OverlayRenderer.cs` has no commit since `3fd8b9b`. Register entry R-0022.
- **V3** — Open. No change: `3DEngine.Vulkan/OverlayRenderer.cs` has no commit since `3fd8b9b`. Register entry R-0022.
- **V4** — Open. No ledger entry since v0.159 names it. Register entry R-0027.
- **V5** — Fixed. TASK-0076, v0.74. No change.
- **V6** — Fixed. TASK-0076, v0.74. No change.
- **V7** — Open. No change: `3DEngine.Vulkan/Ui/UiRenderer.cs`, `3DEngine.Vulkan/Ui/UiTexture.cs`, `3DEngine.Vulkan/GpuBuffer.cs` have no commit since `3fd8b9b`. Register entry R-0030.
- **V8** — Fixed. TASK-0074, v0.75. No change.
- **V9** — Fixed. TASK-0074, v0.75. No change.
- **V10** — Fixed. TASK-0074, v0.75. No change.
- **V11** — Fixed in part. TASK-0074, v0.75. No change.
- **V12** — Fixed, and changed by decision. No change.
- **V13** — Fixed for its parts. No change.
- **V14** — Fixed. TASK-0074, v0.75. No change.
- **V15** — Open. No change: `3DEngine/SessionBodies.cs` has no commit since `3fd8b9b`.
- **V16** — Fixed. TASK-0079, v0.85. No change.
- **V17** — Fixed. TASK-0079, v0.85. No change.
- **V18** — Fixed for its forms. TASK-0090, v0.93. No change.
- **V19** — Fixed, with no injection. TASK-0079, v0.85. No change.
- **V20** — Fixed. TASK-0079, v0.85. No change.
- **V21** — Fixed by reading, with no test. TASK-0079, v0.85. No change.
- **V22** — Fixed for its forms. TASK-0079, v0.85. No change.
- **V23** — Fixed. TASK-0079, v0.85. No change.
- **V24** — Fixed. TASK-0079, v0.85. No change.
- **V25** — Open. No change: `3DEngine.Vulkan/3DEngine.Vulkan.csproj` has no commit since `3fd8b9b`. Register entry R-0020.
- **T1** — Fixed. TASK-0156, v0.158: each runner of the job "Gate" replays one log with a cut, two moves and an extrude of a solved sketch on the native backend, and the job "Compare the determinism hashes" compares the three hashes. The three runners gave one hash. Register entry R-0023 is closed. No change.
- **T2** — Open in one part of five. No ledger entry since v0.159 names it. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. No change.
- **T4** — Open. No change: `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs` has no commit since `3fd8b9b`. Register entry R-0031.
- **T5** — Open in one part of three. No ledger entry since v0.159 names it. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Open in four parts of five. No ledger entry since v0.159 names it. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. No change.
- **T10** — Open. No ledger entry since v0.159 names it. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51. No change.
- **T12** — Fixed. TASK-0059, v0.55. No change.
- **T13** — Open in part. No change: `.github/workflows/ci.yml` has no commit since `3fd8b9b`. The part of the marker gate stays. Register entry R-0031.
- **T14** — Fixed. TASK-0053, v0.51. No change.
- **T15** — Fixed. TASK-0050, v0.47. No change.
- **T16** — Fixed. TASK-0066, v0.67. No change.
- **T17** — Open. No ledger entry since v0.159 names it. Register entry R-0031.
- **T18** — Fixed. TASK-0049, v0.46. No change.
- **T19** — Fixed. TASK-0049, v0.46. No change.
- **T20** — Fixed. TASK-0049, v0.46. No change.
- **T21** — Fixed. TASK-0049, v0.46. No change.
- **T22** — Fixed. TASK-0049, v0.46. No change.
- **T23** — Open. No ledger entry since v0.159 names it. Register entry R-0031.
- **T24** — Fixed. No change.
- **T25** — Open in part. No ledger entry since v0.159 names it. Register entry R-0031.
- **T26** — Fixed. TASK-0057, v0.54. No change.
- **T27** — Fixed. TASK-0058, v0.56. No change.
- **T28** — Fixed. No change.
- **T29** — Fixed. TASK-0057, v0.54. No change.
- **T30** — Fixed. TASK-0057, v0.54. No change.
- **T31** — Open. No change: `eng/repo-gates/RepoGates.csproj` has no commit since `3fd8b9b`. Register entry R-0031.
- **T32** — Fixed in part. No change.
- **T33** — Fixed. TASK-0057, v0.54. No change.
- **T34** — Fixed. TASK-0057, v0.54. No change.
- **T35** — Fixed. TASK-0057, v0.54. No change.
- **C1** — Open. No change: `Engine.Contracts/Geometry/IBRepOps.cs`, `Engine.Contracts/Geometry/IFeatureIdMap.cs` have no commit since `3fd8b9b`. Register entry R-0027.
- **P1** — Open, and smaller. No ledger entry since v0.159 names it. Register entry R-0027.
- **T36** — Fixed. TASK-0061, v0.61. No change.
- **T37** — Open in part. No ledger entry since v0.159 names it. Register entry R-0031.
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
- **V29** — Open. No ledger entry since v0.159 names it.
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
- **E113** — Fixed. TASK-0149, v0.153. No change.
- **E114** — Fixed. TASK-0149, v0.153. No change.
- **E115** — Fixed. TASK-0149, v0.153. The replace of the move makes parallel puts of the same bytes fail on Windows (E120). No change.
- **E116** — Fixed. TASK-0149, v0.153. No change.
- **E117** — Fixed. TASK-0149, v0.153. No change.
- **E118** — Fixed. TASK-0149, v0.153. No change.
- **V65** — Fixed. TASK-0151, v0.155. No change.
- **V66** — Fixed. TASK-0151, v0.155. No change.
- **V67** — Fixed. TASK-0151, v0.155. No change.
- **T117** — Fixed for `PackageReference`. TASK-0150, v0.154. A `GlobalPackageReference` passes (T124). No change.
- **T118** — Fixed. TASK-0150, v0.154. No change.
- **T119** — Fixed. TASK-0150, v0.154. No change.
- **T120** — Fixed for a `const char` that is an operand. TASK-0150, v0.154. A lower-case or an indexed character passes, and a comparison with a constant character is a false part (T126). No change.
- **T121** — Fixed. TASK-0150, v0.154. No change.
- **T122** — Fixed for one line above the underline. TASK-0150, v0.154. A list item of two lines and a Greek letter pass (T125). No change.
- **E119** — Fixed. TASK-0160, v0.163: `engine export stl` reads the document file with `DocumentFile.Read` (no journal, read access, `FileShare.ReadWrite`), gives the warnings of the open, and `StlExportTests` covers a torn last line and a file that a live session holds. **[Observed]**
- **E120** — Fixed in part. TASK-0160, v0.163: a put moves with no overwrite first, so parallel puts of the same bytes each give the name (`AssetStore.cs:80-97`). A put while another handle holds the good file with `FileShare.None` still gives `E-ASSET-WRITE-FAILED`, the form that the last review named. **[Observed]** by a probe.
- **E121** — Fixed for its forms. TASK-0160, v0.163: a count over the limit gives `E-EXPORT-TOO-LARGE`, and a vertex or a normal outside the range of a float gives `E-EXPORT-RANGE`, with status 422. **[Inferred]** A triangle with edges near 1e-100 still gets the normal (0, 0, 0), because the square of its cross product underflows.
- **E122** — Fixed. TASK-0160, v0.163. **[Observed]**
- **E123** — Fixed for its forms. TASK-0160, v0.163: a calibration scales by the larger of |dx| and |dy| when the square underflows or overflows (`CalibrateImageCommandHandler.cs:84-89`). **[Inferred]** Two finite points whose difference overflows are still refused.
- **E124** — Fixed. TASK-0160, v0.163: the comment at `AssetsEndpoint.cs:49-50` agrees with the row. **[Observed]**
- **V68** — Fixed. TASK-0159, v0.161: `PngReader.cs:122-127` gives `InvalidDataException` for each `IOException` and each `InvalidDataException` of the decompressor, and `PngReaderTests` holds FDICT, the Adler sum, the window of 64 KiB, random data and an empty stream. **[Observed]**
- **V69** — Fixed for its forms. TASK-0162, v0.165: the opening of 3 × 3 and the check of a simple polygon (`ContourProposal.cs:133-171`). A probe of 300 random regions gave no polygon that is not simple, and each of the 300 proposals extruded on Manifold; the last review found 16 in 300. **[Observed]**
- **V70** — Fixed. TASK-0162, v0.165: `ImageActions.Tap` takes the select and the sketch point intents. The mark that stays is V74. **[Observed]**
- **V71** — Fixed. TASK-0162, v0.165 (`ImageActions.cs:244-248`, `A_Refused_Calibration_Clears_Its_Two_Points`). **[Observed]**
- **V72** — Fixed for its forms. TASK-0162, v0.165 (`NativeThreeDEngine.cs:401-407`). **[Inferred]** The name "part.stl.3de" still gives "part.stl.stl".
- **V73** — Fixed for its forms. TASK-0162, v0.165 (`ImageActions.cs:88-90`). There is no rollback, and the consequences of ADR-0047 record it. **[Observed]**
- **T123** — Fixed. TASK-0161, v0.164: three empty files give exit 1, three equal hashes exit 0, and a hash in upper case exit 1. **[Observed]** by a probe.
- **T124** — Fixed for its forms. TASK-0161, v0.164: the rule reads `GlobalPackageReference`. The next form is T127. **[Observed]**
- **T125** — Fixed for its forms. TASK-0161, v0.164. The next forms are T129. **[Observed]**
- **T126** — Fixed for its forms. TASK-0161, v0.164: `c == 'E'` is no longer a part, and `char.ToUpperInvariant('e')` and `"EWI"[0]` are parts. The same change opened the forms of T128. **[Observed]**

## 1 Current architecture

**[Observed]** `SetExtrudeDistance` records the identifier of an extrude and a new distance (ADR-0049). The rebuild
of ADR-0039 reads the distance in effect for each extrude, and the extrude and each step of its chain follow.

**[Observed]** `Engine.Core/Exact/Predicates.cs` gives `Orient2D` and `AreaSign` with a rounded filter and an exact
path in `BigInteger` (ADR-0050). `SketchProfile` uses them for the checks of a cross, a touch and the area, so each
sign is the sign of the true coordinates.

**[Observed]** A tap of the sketch takes the nearest intersection of two edges within 22 pixels before a midpoint
(ADR-0041 item 1, R11c). The picture of the overlay gives the pixel of a position of the plane, and the start and the
end of each arc.

**[Observed]** `CreateDatum` records a reference to a face of an extrude or a box (ADR-0051). `DatumResolver` gives its
frame at the place of the command in the log, from the current sketch, the distance in effect and the moves before
that place. The Document holds `Datums`, with three event kinds, a member of the snapshot and the query `GetDatum`.
A sketch can lie on a datum, and an edit of the base moves the datum and each extrude of a sketch on it. The rows K0g,
K0h and K0i of the roadmap hold the rest of K0.

## 2 Patterns in use

- **A new value in the space of the Document has no check of a finite result.** The frame of a side divides by a
  length that can be zero or overflow (E125), as the normal of a tiny triangle of the export can be zero (E121).
  **[Observed]**
- **A new kind of target widens the meaning of an old code, and the registry keeps the old text.** The rebuild gives
  `W-SKETCH-REBUILD-FAILED` to an extrude of another sketch, and `CreateSketch` takes a datum of the Document (E126).
  **[Observed]**
- **Each correction of a gate holds for its forms, and a probe finds the next form.** T127 follows T124, T128 follows
  T126, and T129 follows T125. **[Observed]**
- **A step of each frame of the render loop grows with the square of the sketch** (V75). **[Observed]**

## 3 Review of the code

### The engine and the hosts

#### E125 · Low · A datum on the side of a very short or very long line gets a frame that is not finite or has the length zero · Confirmed

- **[Observed]** `Engine.Core/Sketch/DatumResolver.cs:150-154` computes `length = Math.Sqrt(tx * tx + ty * ty)` and
  divides by it. Nothing checks the result: not `CreateDatumCommandHandler.cs:78-89`, not `ExtrudeProfile.cs:30-39`,
  and not `DatumResolver.Rebuild` (`:87-100`).
- **[Observed]** A probe extruded a pentagon with a side of length 1e-170 by 5, and sent `CreateDatum` on that side.
  The command applied with no fault, and the frame is `U = (-∞, NaN, NaN)` and `Normal = (NaN, NaN, NaN)`. A sketch on
  the datum applied, and its `Extrude` gave `E-GEOM-NATIVE-OP`, "manifold_transform failed with status 1".
- **[Observed]** A triangle (-1e160, 0), (1e160, 0), (0, 1e160) gave a datum on its first side with `U = (0, 0, 0)` and
  `Normal = (0, 0, 0)`. That frame is not right-handed, so it breaks ADR-0051 item 3.
- **[Observed]** Over HTTP, `GetDatum` of the first datum gave status 500 with an empty body: the serializer cannot
  write an infinity. A `MoveSketchPoint` that made the side 1e-170 long moved the datum to the same frame with no
  fault.
- **[Inferred]** The NaN of 0/0 has its sign bit set on x64 and clear on arm64, so a log with such a frame gives
  other bits on the three runners.
- **[Recommended]** Refuse a frame with an axis that is not finite or has the length zero: `E-DATUM-REFERENCE-INVALID`
  at `CreateDatum`, and `W-DATUM-REFERENCE-FAILED` with the last frame at a rebuild. Scale `t` as E123 does. Check the
  origin sums of `Moved` and of the end cap too. The projection of K0h (ADR-0052 item 3) needs the same check.

#### E126 · Low · A rebuild gives a sketch or feature code to an extrude of another sketch, and three rows of the registry do not say so · Confirmed

- **[Observed]** `SketchRebuild.cs:153-158` makes each extrude of a sketch on a moved datum a target of the edit, and
  `Fail` (`:214-222`) gives it the code of the edit. A probe moved a point of sketch S1, and the extrude of sketch S2,
  on a datum of the extrude of S1, got `W-SKETCH-REBUILD-FAILED`. The message names that extrude, not the datum.
- **[Observed]** The rows of `W-SKETCH-REBUILD-FAILED` (`docs/diagnostics.md:61`) and `W-FEATURE-REBUILD-FAILED`
  (`:70`) say "an extrude of the sketch, or a body of its chain" and "its extrude, or a body of its chain". The row of
  `E-SKETCH-DATUM-NOT-FOUND` (`:41`) still says "the datums are the origin planes XY, XZ and YZ", and
  `CreateSketchCommandHandler.cs:66-69` takes a datum of `Document.Datums`. `CLAUDE.md` says: "Do not change the
  meaning of a code."
- **[Recommended]** Q3: the rows name an extrude on a moved datum and the datums of `CreateDatum`, as the answer Q3 A of
  ADR-0040 did, and the warning gives the identifier of the datum in its data.

#### E127 · Low · A datum with a fault repeats W-DATUM-REFERENCE-FAILED at each later edit, also an edit of an unrelated sketch · Confirmed

- **[Observed]** `DatumResolver.Rebuild` (`:83-103`) resolves each datum in effect at each sketch edit and at each
  `SetExtrudeDistance`, and adds a warning each time that a datum does not resolve (`:95-97`).
- **[Observed]** A probe deleted the side line of a datum, so the datum got a fault. Then it sent `AddSketchPoint` to
  a new sketch on XZ, which has no relation to the datum, and the result gave `W-DATUM-REFERENCE-FAILED`.
- **[Inferred]** A body with a fault does not get its warning again from an edit that does not target it, so the two
  rules differ, and an agent can think that its own edit caused the fault.
- **[Recommended]** Q2: the warning comes only when the fault of a datum is new or changes. The record shows a fault that
  stays.

**No finding.** **[Observed]** ADR-0049: a probe moved a base with two datums in a chain from 5 to −8, the datums went
to z = −8 and z = −10, the top solid followed, and an undo and a redo gave each state back. ADR-0050: the filter bound
4ε is larger than (3 + 16ε)ε, an overflow or a NaN goes to the exact path, and the conversion to integers is exact for
a subnormal number. ADR-0051: each frame of item 3 is right-handed and faces out, `Moved` counts a Translate before the
place only, and the bus gives the datum events after the image events. **[Inferred]** An open gives the same frames:
a frame changes only by an edit, an undo or a redo, and each one records the datums in its commit.

### The desktop

#### V74 · Low · During a calibration in the mode Sketch, the mark of the snap shows a point that the tap does not take · Confirmed

- **[Observed]** `3DEngine/NativeThreeDEngine.cs:147-149` gives each point intent to the calibration before the sketch
  tool (the fix of V70). `SketchHoverIntent` is not a point intent, so `ImageActions.Tap` returns false for it
  (`ImageActions.cs:202-208`), and the loop sets `_hover` (`NativeThreeDEngine.cs:182`).
- **[Observed]** The render calls `sketchActions.Mark` at `NativeThreeDEngine.cs:456` and does not read `Calibrating`,
  so the overlay draws the square at the snap of the tool.
- **[Inferred]** The calibration takes the raw pixel under the finger (`ImageActions.cs:225-226`), so the mark is not
  where the tap goes, which breaks ADR-0041 item 6. No one opened the window.
- **[Recommended]** No mark while `Calibrating` is not null, with a test of an active sketch and a calibration.

#### V75 · Low · The snap to an intersection costs 3.2 ms and 4.8 MB in each frame of hover over a sketch of 200 lines · Confirmed

- **[Observed]** `3DEngine.Core/Sketching/SketchTool.cs:206-249` (`Crossing`) tests each pair of lines, and each line
  with each curve, with an iterator and `AddRange` for each pair. No test of a box comes first.
- **[Observed]** The render calls `Mark`, `Target` and `Snap` in each frame of hover in the mode Sketch, and an accept
  of a proposal gives a sketch of up to 200 lines (ADR-0047 item 4). A probe in a Debug build gave 3.190 ms and
  4,778,742 bytes for each snap over a polygon of 200 lines.
- **[Inferred]** At 60 frames a second this uses about 19 % of the frame and allocates about 290 MB a second.
- **[Recommended]** Skip a pair whose box in pixels is more than `HitPixels` from the tap, collect the candidates with
  no iterator, and add a test that limits the time or the allocation for 200 lines.

**No finding.** **[Observed]** by probes: an arc across ±π takes its intersection, and a line that meets the circle off
the arc gives none; the intersection is computed in the plane and then projected, so it is correct in perspective; a
point within 22 pixels comes first; and 300 random regions gave 300 simple polygons that extrude.

### The gates and the tests

#### T127 · Low · A `CreateItem` whose output is the item `PackageReference` pins the vision library into Engine.Core, and the gate passes · Confirmed

- **[Observed]** `Engine.Tests/Governance/DependencyDirectionGateTests.cs:451-452` reads the elements
  `PackageReference` and `GlobalPackageReference` only. `CompileOffences` (`:538`) refuses an `Output` whose
  `ItemName` is `Compile`, and no rule refuses one whose `ItemName` is `PackageReference`.
- **[Observed]** An injection added to `Engine.Core/Engine.Core.csproj` a `Target` before `CollectPackageReferences`
  with `CreateItem Include="OpenCvSharp4"` and `Output ItemName="PackageReference"`. The restore put `OpenCvSharp4` in
  `project.assets.json`, the build copied `OpenCvSharp.dll`, and `DependencyDirectionGateTests` passed 119 of 119.
  The control, a literal `PackageReference` in the same `Target`, failed
  `Only_The_Vision_Spike_Pins_The_Vision_Library`.
- **[Recommended]** Refuse an `Output` whose `ItemName` is `PackageReference`, `GlobalPackageReference` or a computed
  name, as `IsCompileName` does for `Compile`.

#### T128 · Low · The fix of T126 removes forms that T120 caught: `S.ToString()` and `new string(...)` · Confirmed

- **[Observed]** `Engine.Tests/Diagnostics/DiagnosticsScanner.cs:100` counts a constant severity char only when
  `JoinsText` (`:116-128`) is true, and `JoinsText` follows only parentheses and casts.
- **[Observed]** A probe of `PartsOfCodes` gave 0 parts for `$"{S.ToString()}{rest}"`, `S.ToString() + rest`,
  `new string(new[] { 'E' }) + rest` and `$"{char.ToUpperInvariant('e').ToString()}{rest}"`, and 1 part for the
  control `$"{S}{rest}"`. With `&& JoinsText(...)` removed, as before TASK-0161, the first three gave 1 part each.
- **[Recommended]** Let `JoinsText` go through a `.ToString()` call and through an array or a `new string(...)`, add
  the forms to `A_Part_Of_A_Code_Is_An_Offence`, and keep `c == 'E'` as the control.

#### T129 · Low · The rule for a setext heading of a version misses "V0.11" and "v 0.11" in a list item · Confirmed

- **[Observed]** `Engine.Tests/Governance/CodebaseReviewGateTests.cs:391` reads `(?:v|[\p{L}-[\x00-\x7F]])\d+\.\d+`, so
  a capital `V` and a space before the digits do not match. `NearLedgerEntry` takes both (T96).
- **[Observed]** `HeadingProblems` gave 0 problems for `"- V0.11 — An entry (K0b, TASK-0003)\n    ==="` and for
  `"- v 0.11 — ..."`, and 1 for the control `"- v0.11 — ..."`.
- **[Inferred]** Markdown shows such an entry as a heading, and the counter does not count it, so a product milestone
  can hide from the counter of this review.
- **[Recommended]** Use `[vV\p{L}-[\x00-\x7F]][^\S\n]*\d+\.\d+`, and add the two forms to
  `A_Heading_Of_A_Near_Form_Is_A_Problem`.

**No finding.** **[Observed]** The new tests of the desktop fail when their feature is removed, and
`CrossPlatformHashTests` hashes the bits of each frame of a datum; the upload uses `if-no-files-found: error`.

**Not verified.** Linux, macOS and arm64 for E125; a subscriber of the WebSocket against a datum with a fault; the
desktop window for V74; a run of the timings of V75 in a Release build; other spellings for T128 and other MSBuild forms
for T127.

## 4 The next planned task

The owner gave the order on 2026-11-03: "do the review, then close K0h". K0h (TASK-0167, ADR-0052) is complete on its
branch `k0h-edge-references`, and its run 41543581304 is green with equal hashes. It closes after this review as the
next product phase. No new finding is critical or high, so no task comes before it. P0.72 (TASK-0169) holds E125 to
E127, and P0.73 (TASK-0170) holds V74, V75 and T127 to T129, after K0h.

## 5 Research for that task

**[Observed]** K0h adds `AddSketchReference` and `SketchRecord.References`. Its resolver uses the same walk as K0g, so
E125 and E127 also reach a reference of a sketch: the projection of item 3 has no check of a finite result, and a
reference with a fault gives `W-SKETCH-REFERENCE-FAILED` at each later edit. P0.72 must cover both. **[Inferred]**

**[Observed]** After K0h the open rows of the product tracks are K0i (the desktop of K0g and K0h), P8b, P8d, R12 and
K4 to K7. K4 needs K0, and K0 ends with K0i. The desktop reads each datum with `Datums.Find` and draws no sketch on a
datum of the Document (`3DEngine/SketchOverlay.cs:59`, `:117`, `:219`).

## 6 Recommended approach

**[Recommended]** In this order:

1. Close K0h (TASK-0167) at v0.170.
2. P0.72 (TASK-0169): E125 to E127, for the datums and the references, each with a test before the change. 1 to 2 agent
   hours.
3. P0.73 (TASK-0170): V74, V75 and T127 to T129, each with a test or an injection before the change. 1 to 2 agent
   hours.
4. The phase of Q1. With A: K0i, an ADR for the desktop of a datum and a reference, then the code.

## Questions for the owner

**Q1. The next product phase after K0h.**
- A: K0i. The desktop draws a sketch on a datum of the Document and its references, and a tap on a face of a body
  starts a sketch on a datum of that face.
- B: P8d, the three layers: the log, an ordered list of features and a cache of the regeneration.
- C: P8b, the container model.

**Recommended: A.** It ends K0, which K4 needs (rule 3 of the order of the roadmap), and it gives a person the features
that an agent has since K0g (objective 9).

**Q2. The warning of a datum or a reference with a fault (E127).**
- A: The warning comes only when the fault is new or changes; the record shows a fault that stays.
- B: Each edit gives the warning again, as ADR-0051 item 6 says now.

**Recommended: A.** It is the rule of a body with a fault, and an agent then reads the warning of the edit that caused
it.

**Q3. The codes of a rebuild through a datum (E126).**
- A: The rows of `W-SKETCH-REBUILD-FAILED`, `W-FEATURE-REBUILD-FAILED` and `E-SKETCH-DATUM-NOT-FOUND` name an extrude on a
  moved datum and the datums of `CreateDatum`, and the warning gives the identifier of the datum in its data.
- B: A new code `W-DATUM-REBUILD-FAILED` for an extrude on a moved datum.

**Recommended: A.** The meaning stays "the edit applied and a solid did not follow it"; the set of solids grows. The
owner chose the same form for `E-SKETCH-PROFILE-INVALID` (ADR-0040 Q3 A).
