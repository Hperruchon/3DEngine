---
date: 2026-11-03
commit: db28439
ledger: v0.176
previous: 2026-11-03-codebase-review.md
---

# Codebase review — 2026-11-03 (b)

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the twentieth codebase review. The review of 2026-11-03 examined v0.168. After it came 4 product milestones
(K0h, K0i, P8d1 and P8d2) and 8 ledger entries (v0.169 to v0.176). The limits are four and twelve, so the next product
milestone would make the review late. On 2026-11-03 the owner told the session: "merge when green, then start the
codebase review". `Engine.Tests/Governance/CodebaseReviewGateTests.cs` holds the rule, and `docs/templates.md`,
section 7, gives the form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is the
authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this file does not.

| Item | Value |
|---|---|
| Code state | `main` at `db28439`, the merge of v0.176 |
| Branch of this review | `codebase-review-2026-11-03-b` |
| Tests | 1,636 on this computer (1,168 in `Engine.Tests`, 291 in `3DEngine.Tests`, 174 in `3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`) |
| Pipeline | The run on `db28439` (41731142307): green on Ubuntu, Windows and macOS, and the three determinism hashes are equal |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner decides.

**Method.** Seven tasks changed code or records after `1c3fb7c`, the commit of the last review: TASK-0168 (the review),
TASK-0167 (K0h), TASK-0169 and TASK-0170 (P0.72 and P0.73), TASK-0171 (K0i), TASK-0172 (P0.74), TASK-0173 (P8d1) and
TASK-0174 (P8d2). The change is about 5,100 lines in 81 files, 3,000 of them in 53 C# files: the references of a sketch
to the edges and the vertices of a feature, the fixes of the last review, a sketch on a face in the desktop, the wait
of a put, the ordered feature list and the regeneration. Two review agents worked in parallel at `db28439`, each one in
its own git worktree: the engine with the hosts, and the desktop with the gates. Each agent wrote probes and
injections that only its worktree held, and restored or deleted them. I read the cited lines of each new finding of
the level medium, and I measured the values below.

## Summary

- **The fixes of the last review hold for the forms that they named.** E126, E127 and V74 are fixed. E120, E125, V75
  and T127 to T129 are fixed for their forms, and a probe found the next form of E125, T127, T128 and T129.
  **[Observed]**
- **The walk of the planes is one pass in the order of the log, so a datum or a reference before a later reference of
  its sketch keeps a stale frame (E128, medium).** An edit that has no relation to it then moves it, and each solid on
  it, with no warning. **[Observed]** by a probe.
- **While a reference of a sketch is stale, each edit of that sketch throws an exception and gives no envelope (E129,
  medium).** **[Observed]** by a probe.
- **In the desktop, a tap on a face can turn the camera behind the body, and the arm of the button Face survives a new
  sketch (V76, V77, medium).** **[Observed]** by a probe and by reading.
- **Four low findings: a side whose square is subnormal (E130), and the next forms of three gates (T130 to T132).**
  T130 lets `Engine.Core` reference `3DEngine.Core` through a computed `ProjectReference`. **[Observed]** by injections.
- **No new finding is critical or high.** **[Observed]**
- **Open findings: 0 critical, 4 high, 18 medium and 20 low.** **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 15 | `.csproj` lines in `3DEngine.sln`. The last review gave 15. |
| Production source lines | 25,547 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests`, `3DEngine.ImageTests` and `3DEngine.VisionTests`, 248 files. The last review gave 24,461 in 239 files; the same script gives that value at `1c3fb7c`. |
| Test source lines | 29,785 | `Engine.Tests` (21,705 in 109 files), `3DEngine.Tests` (3,290 in 21 files), `3DEngine.ImageTests` (4,725 in 28 files) and `3DEngine.VisionTests` (65 in 1 file). The last review gave 28,303. |
| Tests | 1,636 | `dotnet test 3DEngine.sln`: 1,168, 291, 174 and 3 passed, 0 failed, 0 skipped. The last review gave 1,588. |
| Gate classes | 19 | Tracked files whose name ends in `GateTests.cs`. `eng/repo-gates` gives 384 runs of 145 checks. The last review gave 19, and 370 runs. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 14 | Section "Open" of `docs/register.md`; the limit is 15. The last review gave 14. |
| ADRs | 55 | `docs/adr/0001` to `0055`, with 0003 and 0011 in `docs/adr/archive/`. In force: 53 (26 accepted, 27 amended). None is proposed. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 4 | The same method: V2, V3, T4, T31. The last review gave 4. |
| Findings: medium | 18 | The same method: E5, E6, E12, E13, E19, E21, E22, E128, E129, V4, V76, V77, T2, T5, T10, T17, T32, T37. The last review gave 14. |
| Findings: low | 20 | The same method: E17, E24, E34, E39, E57, E84, E130, V7, V15, V25, V29, T7, T13, T23, T25, T130 to T132, C1, P1. The last review gave 25; E120, E125 to E127, V74, V75 and T127 to T129 are fixed. |
| Lines in the rule files | 2,673 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The same files give 2,630 at `1c3fb7c`, the value of the last review. |
| Sentences with "must" | 58 | `grep -o -w -i must` on the same ten files. The last review gave 58. |
| Sentences with "do not" | 54 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 54. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found. E13, T32 and E84 are fixed in part and count as open. A finding that the owner accepted counts as closed.

## Findings of the previous review

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. No change: `Engine.Api.Http/Endpoints/JsonParameters.cs` has no commit since `1c3fb7c`. Register entry R-0027.
- **E6** — Open. No change: `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` has no commit since `1c3fb7c`, and it still lists `document.loaded`, `document.replayed` and `document.saved`. Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. No change.
- **E8** — Fixed. TASK-0044, v0.37. No change.
- **E9** — Fixed, and it holds. No change.
- **E10** — Fixed. TASK-0035, v0.43. No change.
- **E11** — Fixed. TASK-0115, v0.115. No change.
- **E12** — Open, and smaller. No change: `Engine.Core/Hosting/ParameterBinder.cs` has no commit since `1c3fb7c`. Register entry R-0027.
- **E13** — Fixed in part. No change.
- **E14** — Fixed. TASK-0115, v0.115. No change.
- **E15** — Fixed. TASK-0044, v0.37. No change.
- **E16** — Fixed. TASK-0115, v0.115. No change.
- **E17** — Open, and smaller. No change: `Engine.Core/CommandBus.cs` has no commit since `1c3fb7c`. Register entry R-0028.
- **E18** — Fixed for the sinks of `Engine.*` (TASK-0140, v0.141) and of the desktop host (T118, TASK-0150, v0.154). Correction: the line of the last review lost the end of its code span. No change.
- **E19** — Open, and smaller. No ledger entry since v0.168 names it. Register entry R-0033.
- **E20** — Fixed, and it holds. No change.
- **E21** — Open, and wider by decision. No ledger entry since v0.168 names it. Register entry R-0033.
- **E22** — Open. No change: `Engine.Core/CommandBus.cs` has no commit since `1c3fb7c`. Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. No change.
- **E24** — Open in seven parts of nine. No change: `WireMessage.cs` and `SchemaEventsEndpoint.cs` have no commit since `1c3fb7c`. Register entry R-0033.
- **E25** — Accepted by the owner. TASK-0142, v0.144. Register entry R-0036. No change.
- **E26** — Fixed. TASK-0135, v0.136. No change.
- **E27** — Accepted by the owner. TASK-0142, v0.144. Register entry R-0036. No change.
- **E28** — Fixed. TASK-0136, v0.137. No change.
- **E29** — Fixed for the replay. TASK-0135, v0.136. No change.
- **E30** — Fixed. TASK-0136, v0.137. No change.
- **E31** — Fixed for the replay. TASK-0137, v0.138. No change.
- **E32** — Fixed. TASK-0135, v0.136. No change.
- **E33** — Fixed with E18. TASK-0140, v0.141. No change.
- **E34** — Open. No change: `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs` has no commit since `1c3fb7c`. Register entry R-0036.
- **E35** — Fixed for the routes of `MapGet`. TASK-0139, v0.140. No change.
- **E36** — Fixed. TASK-0079, v0.85. No change.
- **E37** — Fixed. TASK-0137, v0.138. No change.
- **E38** — Fixed. TASK-0138, v0.139. No change.
- **E39** — Open. No change: `Engine.Cli/Cli.cs`, `Engine.Api.Http.Server/ServerEngine.cs` and `3DEngine/DesktopSurface.cs` have no commit since `1c3fb7c`. Register entry R-0036.
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
- **E57** — Open. No change: `Engine.Tests/Http/DesktopSurfaceTests.cs` has no commit since `1c3fb7c`. Register entry R-0039.
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
- **V2** — Open. No change: `3DEngine.Vulkan/OverlayRenderer.cs` has no commit since `1c3fb7c`. Register entry R-0022.
- **V3** — Open. No change: `3DEngine.Vulkan/OverlayRenderer.cs` has no commit since `1c3fb7c`. Register entry R-0022.
- **V4** — Open. No ledger entry since v0.168 names it. Register entry R-0027.
- **V5** — Fixed. TASK-0076, v0.74. No change.
- **V6** — Fixed. TASK-0076, v0.74. No change.
- **V7** — Open. No change: `3DEngine.Vulkan/Ui/UiRenderer.cs`, `3DEngine.Vulkan/Ui/UiTexture.cs`, `3DEngine.Vulkan/GpuBuffer.cs` have no commit since `1c3fb7c`. Register entry R-0030.
- **V8** — Fixed. TASK-0074, v0.75. No change.
- **V9** — Fixed. TASK-0074, v0.75. No change.
- **V10** — Fixed. TASK-0074, v0.75. No change.
- **V11** — Fixed in part. TASK-0074, v0.75. No change.
- **V12** — Fixed, and changed by decision. No change.
- **V13** — Fixed for its parts. No change.
- **V14** — Fixed. TASK-0074, v0.75. No change.
- **V15** — Open. No change: `3DEngine/SessionBodies.cs` has no commit since `1c3fb7c`.
- **V16** — Fixed. TASK-0079, v0.85. No change.
- **V17** — Fixed. TASK-0079, v0.85. No change.
- **V18** — Fixed for its forms. TASK-0090, v0.93. No change.
- **V19** — Fixed, with no injection. TASK-0079, v0.85. No change.
- **V20** — Fixed. TASK-0079, v0.85. No change.
- **V21** — Fixed by reading, with no test. TASK-0079, v0.85. No change.
- **V22** — Fixed for its forms. TASK-0079, v0.85. No change.
- **V23** — Fixed. TASK-0079, v0.85. No change.
- **V24** — Fixed. TASK-0079, v0.85. No change.
- **V25** — Open. No change: `3DEngine.Vulkan/3DEngine.Vulkan.csproj` has no commit since `1c3fb7c`. Register entry R-0020.
- **T1** — Fixed. TASK-0156, v0.158. Each run since then compares the three hashes. No change.
- **T2** — Open in one part of five. No ledger entry since v0.168 names it. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. No change.
- **T4** — Open. No change: `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs` has no commit since `1c3fb7c`. Register entry R-0031.
- **T5** — Open in one part of three. No ledger entry since v0.168 names it. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Open in four parts of five. No ledger entry since v0.168 names it. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. No change.
- **T10** — Open. No ledger entry since v0.168 names it. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51. No change.
- **T12** — Fixed. TASK-0059, v0.55. No change.
- **T13** — Open in part. No change: `.github/workflows/ci.yml` has no commit since `1c3fb7c`. The part of the marker gate stays. Register entry R-0031.
- **T14** — Fixed. TASK-0053, v0.51. No change.
- **T15** — Fixed. TASK-0050, v0.47. No change.
- **T16** — Fixed. TASK-0066, v0.67. No change.
- **T17** — Open. No ledger entry since v0.168 names it. Register entry R-0031.
- **T18** — Fixed. TASK-0049, v0.46. No change.
- **T19** — Fixed. TASK-0049, v0.46. No change.
- **T20** — Fixed. TASK-0049, v0.46. No change.
- **T21** — Fixed. TASK-0049, v0.46. No change.
- **T22** — Fixed. TASK-0049, v0.46. No change.
- **T23** — Open. No ledger entry since v0.168 names it. Register entry R-0031.
- **T24** — Fixed. No change.
- **T25** — Open in part. No ledger entry since v0.168 names it. Register entry R-0031.
- **T26** — Fixed. TASK-0057, v0.54. No change.
- **T27** — Fixed. TASK-0058, v0.56. No change.
- **T28** — Fixed. No change.
- **T29** — Fixed. TASK-0057, v0.54. No change.
- **T30** — Fixed. TASK-0057, v0.54. No change.
- **T31** — Open. No change: `eng/repo-gates/RepoGates.csproj` has no commit since `1c3fb7c`. Register entry R-0031.
- **T32** — Fixed in part. No change.
- **T33** — Fixed. TASK-0057, v0.54. No change.
- **T34** — Fixed. TASK-0057, v0.54. No change.
- **T35** — Fixed. TASK-0057, v0.54. No change.
- **C1** — Open. No change: `Engine.Contracts/Geometry/IBRepOps.cs`, `Engine.Contracts/Geometry/IFeatureIdMap.cs` have no commit since `1c3fb7c`. Register entry R-0027.
- **P1** — Open, and smaller. No ledger entry since v0.168 names it. Register entry R-0027.
- **T36** — Fixed. TASK-0061, v0.61. No change.
- **T37** — Open in part. No ledger entry since v0.168 names it. Register entry R-0031.
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
- **V29** — Open. No ledger entry since v0.168 names it.
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
- **E119** — Fixed. TASK-0160, v0.163. No change.
- **E120** — Fixed for its forms. TASK-0172, v0.174: a put reads the name again after a failed move and tries again up to 12 times, about 1.3 s, and it does not replace a name that it cannot read (`AssetStore.cs:83-97`). A probe held the good file with `FileShare.None` for 1,000 ms, and the put applied after 1,177 ms; for 2,000 ms it gave `E-ASSET-WRITE-FAILED`, and the good file stayed. **[Inferred]** Linux and macOS take the same bounded loop; no run shows it. Register entry R-0050 is closed. **[Observed]**
- **E121** — Fixed for its forms. TASK-0160, v0.163. No change.
- **E122** — Fixed. TASK-0160, v0.163. No change.
- **E123** — Fixed for its forms. TASK-0160, v0.163. No change.
- **E124** — Fixed. TASK-0160, v0.163. No change.
- **V68** — Fixed. TASK-0159, v0.161. No change.
- **V69** — Fixed for its forms. TASK-0162, v0.165. No change.
- **V70** — Fixed. TASK-0162, v0.165. No change.
- **V71** — Fixed. TASK-0162, v0.165. No change.
- **V72** — Fixed for its forms. TASK-0162, v0.165. No change.
- **V73** — Fixed for its forms. TASK-0162, v0.165. No change.
- **T123** — Fixed. TASK-0161, v0.164. No change.
- **T124** — Fixed for its forms. TASK-0161, v0.164. No change.
- **T125** — Fixed for its forms. TASK-0161, v0.164. No change.
- **T126** — Fixed for its forms. TASK-0161, v0.164. No change.
- **E125** — Fixed for its forms. TASK-0169, v0.171: `Resolve` refuses a frame that is not finite or has an axis of the length zero (`DatumResolver.cs:84-89`), `Positions` refuses a point that is not finite (`:224-226`), and a side whose square is 0 or infinite is scaled (`:298-306`). A side of 1e-170 gives `|U| = 1`. The next form is E130. **[Observed]** by a probe.
- **E126** — Fixed. TASK-0169, v0.171: the warning gives `datumId` (`Regeneration.cs:176-183`), and the rows `docs/diagnostics.md:41`, `:61` and `:70` name each target, as Q3 A says. **[Observed]**
- **E127** — Fixed. TASK-0169, v0.171: a fault of a datum or of a reference that stays gives no new warning (`DatumResolver.cs:135-141`, `:157-161`). **[Observed]**
- **V74** — Fixed. TASK-0170, v0.172: `MarkPoint` gives null while `Calibrating` is set (`NativeThreeDEngine.cs:435-436`). The injection `=> hover;` fails `No_Mark_Of_The_Snap_Shows_During_A_Calibration`. **[Observed]**
- **V75** — Fixed for its form. TASK-0170, v0.172: `Crossing` skips a line whose box in pixels is far from the tap, with no iterator (`SketchTool.cs:218`, `:258-264`). A probe of `SketchActions.Mark` over 200 lines gave 0.107 ms and 84,552 bytes in a Debug build; the last review gave 3.190 ms and 4,778,742 bytes for the snap. **[Observed]**
- **T127** — Fixed for its forms. TASK-0170, v0.172 (`PackageOutputOffences`, `DependencyDirectionGateTests.cs:559-562`): the injection of the last review fails `No_Output_Of_A_Task_Adds_A_Package`. The next form is T130. **[Observed]**
- **T128** — Fixed for its forms. TASK-0170, v0.172 (`Outer`, `DiagnosticsScanner.cs:134-141`): the five forms of the last review each give a part. The next forms are T131. **[Observed]**
- **T129** — Fixed for its forms. TASK-0170, v0.172 (`CodebaseReviewGateTests.cs:394`): "V0.11" and "v 0.11" are problems. The next forms are T132. **[Observed]**

## 1 Current architecture

**[Observed]** `AddSketchReference` records a reference of a sketch to an edge or a vertex of a feature (ADR-0052). The
points of the reference are projected onto the plane of the sketch, the solver holds them fixed, and
`DatumResolver.DependsOn` refuses a cycle. The desktop starts a sketch on a datum of a face that a tap names, through
the query `GetFaceAt` and the command `CreateDatum`, and it draws each sketch on a datum of the Document (ADR-0053).

**[Observed]** `FeatureList` (`Engine.Core/Features/`) is a fold of the log for each Document: each entry applies one
time, and an Undo and a Redo move the last step as `StepHistory` does (ADR-0054). The engine reads the steps in effect,
the creator, the consumer and the distance in effect from it, and the query `GetFeatures` gives each feature with its
inputs and its state.

**[Observed]** `Regeneration` (`Engine.Core/Features/`) evaluates the features again after an edit, in two walks of the
steps in effect: the planes (`DatumResolver.Rebuild`), then the solids, with each end in the log order of the command
that creates it (ADR-0055). The faults stay in the records of the Document, and anti-objective 4 of the charter says
that those records are the cache.

**[Observed]** A put of an asset reads the name again after a failed move, and waits for a name that another program
holds (TASK-0172).

## 2 Patterns in use

- **A walk in the order of the log is not a walk in the order of the inputs.** A reference that comes later in the log
  can change a sketch that an earlier datum, reference or extrude reads. ADR-0055 corrects it for the extrudes only
  (E128). **[Observed]**
- **A handler that joins two lists of changed records gives one record two times when the two sources meet** (E129).
  **[Observed]**
- **A state of the desktop that a button arms has no owner that a test drives** (V77), as V70 and V74 had. **[Observed]**
- **Each correction of a gate holds for its forms, and a probe finds the next form.** T130 follows T127, T131 follows
  T128, and T132 follows T129. **[Observed]**

## 3 Review of the code

### The engine and the hosts

#### E128 · Medium · The walk of the planes is one pass in log order, so a datum or a reference before a later reference of its sketch keeps a stale frame · Confirmed

- **[Observed]** `DatumResolver.Rebuild` (`Engine.Core/Sketch/DatumResolver.cs:123-172`) resolves each `CreateDatum`
  (`:127`) and each `AddSketchReference` (`:150-151`) one time, in the order of the log, and a reference changes its
  sketch at `:166`. A datum or a reference before that place in the log read the sketch before the change.
  `Regeneration.cs:64-71` corrects the extrudes of a sketch that the walk solves again (`resketched`), not an earlier
  datum or reference.
- **[Observed]** A probe: a rectangle S1 on XY, its extrude E1 by 5, a datum D on the side of E1 at the line L1, an
  extrude E3 of a sketch S3, then a reference of S1 to the vertex `startVertex` S3.P0 of E3, with S1.P1 coincident to
  it. Then S1.P1 = (20, 0) and D has the origin (20, 0, 0). A `MoveSketchPoint` of S3.P0 to (22, −1) moved S1.P1 to
  (22, −1), and D stayed at (20, 0, 0) with the same U. An `AddSketchPoint` to a new sketch on YZ then moved D to
  (22, −1, 0) with no warning.
- **[Observed]** The same probe with a reference of a sketch in place of the datum: the reference stayed at (20, 0)
  after its target moved, and an unrelated edit moved it.
- **[Inferred]** The state of a document depends on an unrelated later edit, and each solid on D or on a sketch with a
  stale reference keeps a wrong solid until then. A replay gives the same state, so determinism holds.
- **[Recommended]** Walk the planes again until no datum and no sketch changes. The cycle check of ADR-0052 item 7 keeps
  the graph acyclic, so the count of datums and references bounds the walks. A warning comes once, from the last walk.
  Add a test with the layout of the probe.

#### E129 · Medium · An edit of a sketch whose own reference the walk of the planes moves throws InvalidOperationException · Confirmed

- **[Observed]** `SketchResults.cs:44` gives `changedSketches: [solved, .. sketches]`. When the walk solves the edited
  sketch again (`DatumResolver.cs:150-168`, where `SketchOf` gives the solved record of the edit), the list holds that
  sketch two times, and `CommandBus.cs:409-417` throws.
- **[Observed]** A probe in the stale state of E128: an `AddSketchPoint` to the sketch of the stale reference threw
  `InvalidOperationException`, "… changes sketch …, which is in its list two times …". The Document did not change.
- **[Inferred]** While the reference is stale, each edit of that sketch fails with an exception and not with an
  envelope; over HTTP that is probably status 500. No probe ran it over HTTP.
- **[Recommended]** Take the record of the walk for the edited sketch in place of `solved` when the walk gives one, and
  add a test. The fix of E128 removes the known cause, and this fix keeps the next cause from the exception.

#### E130 · Low · A datum on a side whose square is subnormal gets axes whose length is not 1 · Confirmed

- **[Observed]** `DatumResolver.cs:296-306` scales only when the length is 0 or infinite. When `tx * tx + ty * ty` is
  subnormal, `Math.Sqrt` loses precision.
- **[Observed]** A probe of `CreateDatum` on a side of length L: L = 3e-162 gave `|U| = |N| = 0.9543637356285588`, and
  L = 1e-158 gave 1.0000000081701428; L = 1e-150 and L = 1e-170 gave 1. The command applied with no fault.
- **[Inferred]** A sketch on that datum is scaled by about 0.95 along two axes, against ADR-0051 item 3. Each platform
  gives the same bits.
- **[Recommended]** Scale also when the sum of the squares is below `double.MinNormal * 2^52`, as E123 does.

**No finding.** **[Observed]** `FeatureList` agrees with `StepHistory` for each Undo and Redo, a refused Undo is not in
the log, and each caller of `FeatureList.Of` runs inside the serial boundary of `DocumentSession`. `GetFeatures` gives
each state of ADR-0054 item 3. No new file of the engine calls a transcendental function, and each order of the walks
comes from the list. A `SetExtrudeDistance` moves a reference to that extrude, and a `Subtract` of a box and a consumed
extrude evaluates each step one time. Each of the four new codes of K0h and K0i has its row and its entry in
`SchemaDiagnosticsEndpoint.cs`.

### The desktop

#### V76 · Medium · After a tap on a face, the camera can turn to look at the face from behind the body · Confirmed

- **[Observed]** `SketchCamera.Face` (`3DEngine.Core/Sketching/SketchCamera.cs:18-20`) takes the side from
  `(eye − target) · n`, not from the plane of the face, and the sketch on a face calls it with the normal of the frame.
- **[Observed]** A probe with the real `Picker` and `CreateOnFace` on Manifold: a box of 10 × 6 × 4 moved by dx = −40,
  a camera with the target (0, 0, 0), the distance 100 and the yaw −12°. A tap hit the face +x at (−35, 0, 1.69), with
  the eye 14.209 on the outer side. After `Face` the eye is at (−100, 0, 0), 65.000 on the inner side, behind the body.
- **[Inferred]** It occurs each time that the target is further out along the normal than the eye. ADR-0034 item 6
  says "from the side of the plane where the eye is now".
- **[Recommended]** Take the side from `(eye − frame.Origin) · n` for a datum of the Document, and move the target onto
  the face. Add a test with this camera.

#### V77 · Medium · The arm of the button Face survives a new sketch, and a later tap to select starts a sketch on a face · Confirmed

- **[Observed]** `_faceArmed` has three writes (`3DEngine/NativeThreeDEngine.cs:55`, `:157`, `:328`). The start of a
  new sketch, the edit of a sketch and the accept of a contour do not clear it, and the armed branch (`:155-160`) takes
  each `SelectIntent` of the mode Scene.
- **[Inferred]** Face, then "Sketch XY", then Finish: the next tap that selects a body creates a datum and a sketch.
  ADR-0053 item 6 says that a tap or a refusal ends the arm. The ledger of v0.173 records that the armed tap has no
  test.
- **[Recommended]** Clear the arm at each start of the mode Sketch and at each change of mode, and move it into a class
  that a test drives.

**No finding.** **[Observed]** The pick gives the correct face of a moved body in perspective, the wall of a hole gives
`E-DATUM-FACE-NOT-FOUND` as ADR-0053 item 2 says, and an undo and a redo of a distance move the frame of a sketch on a
face. The overlay, the pick and the sketch on a face read the queries, the events and the commands only. No new cost of
each frame grows with the scene.

### The gates and the tests

#### T130 · Low · An `Output` of a task with `ItemName="ProjectReference"` makes Engine.Core reference 3DEngine.Core, and each gate passes · Confirmed

- **[Observed]** `PackageOutputOffences` (`Engine.Tests/Governance/DependencyDirectionGateTests.cs:561`) reads the
  package items, `CompileOffences` (`:533`) reads `Compile`, and the graph reads the elements `ProjectReference` only
  (`:813`, `:852`).
- **[Observed]** An injection in `Engine.Core/Engine.Core.csproj`: a `Target` before `AssignProjectConfiguration` with
  `CreateItem Include="..\3DEngine.Core\3DEngine.Core.csproj"` and `Output ItemName="ProjectReference"`, and a file
  with `typeof(ThreeDEngine.Core.Picking.Picker)`. The build compiled it, 513 of 513 tests of the gates passed, and
  `eng/repo-gates` gave 0 failed. The control, a literal `ProjectReference` in the same `Target`, failed
  `Engine_Core_References_Only_Engine_Contracts`. An `Output` with `ItemName="Reference"` also passes.
- **[Recommended]** Refuse each `Output` whose item name is an item that a gate reads, or each item name outside a short
  list, and add the forms to the theory.

#### T131 · Low · The next spellings of T128 give no part of a code · Confirmed

- **[Observed]** `PartsOfCodes` gave 0 parts for `new string(['E']) + rest`,
  `S.ToString(CultureInfo.InvariantCulture) + rest`, `char.ToString(S) + rest`, `Convert.ToString(S) + rest` and
  `new StringBuilder(rest).Insert(0, S).ToString()`, and 1 part for the controls `$"{S}{rest}"` and
  `$"{S.ToString()}{rest}"`. `Outer` (`Engine.Tests/Diagnostics/DiagnosticsScanner.cs:137-139`) takes a `ToString` with
  no argument and an array initializer only.
- **[Recommended]** Take a collection expression and a `ToString` with arguments, record the other calls as a rule in
  text or take them too, and add the forms to `A_Part_Of_A_Code_Is_An_Offence`.

#### T132 · Low · A setext heading of a list item with "0.11", "Version 0.11" or "v.0.11" is not a problem · Confirmed

- **[Observed]** The rule (`Engine.Tests/Governance/CodebaseReviewGateTests.cs:394`) needs a `v`, a `V` or a letter
  that is not ASCII before the digits. The forms `"- 0.11 — …\n    ==="`, `"- Version 0.11 — …"` and `"- v.0.11 — …"`
  gave no problem, and the controls of T129 gave one.
- **[Inferred]** Markdown shows each one as a heading, and the counter does not count it.
- **[Recommended]** In a list item or a quote, refuse each underline after a paragraph, with no condition on the
  version, as the rule at an indent of 0 to 3 does.

**No finding.** **[Observed]** `eng/test-floor.txt` agrees with the counts of the test run with `CI=true`. The
determinism gate and the scanner of the codes read `Engine.Core/Features/`, because they read each folder of the
project. The injections of V74, V75 and T127 to T129 each fail their tests.

**Not verified.** Linux, macOS and arm64 for E120 and E128 to E130; the HTTP status of E129; the desktop window for V76
and V77; timings in a Release build; a build of the form `Reference` of T130.

## 4 The next planned task

The owner gave the order on 2026-11-03: "merge, fix E128 and E129, then R13". By `docs/templates.md`, section 7, a
medium finding goes after the next product phase; the owner put E128 and E129 before it. P0.75 (TASK-0176) holds E128
and E129 and comes first. R13, the dimension overlay of the desktop, is a new phase that its first task adds to the
roadmap with its ADR. P0.76 (TASK-0177) holds V76, V77, E130 and T130 to T132, after R13.

## 5 Research for that task

**[Observed]** E128 and E129 have one cause: the walk of the planes reads a sketch in a state that a later reference
in the same walk changes. `DatumResolver.Rebuild` is the first walk of the regeneration (ADR-0055 item 1), and
`SketchResults.Changed` joins its sketches to the record of the edit. A repeated walk changes ADR-0055 item 1 and the
reason that it gives; an ADR history line records it.

**[Inferred]** The repeated walk gives the frames that a walk in the order of the inputs gives, because the graph of
the datums and the references has no cycle. Each walk is O(steps), and the count of the walks is at most the depth of
that graph plus one.

## 6 Recommended approach

**[Recommended]** In this order:

1. P0.75 (TASK-0176): a test of the layout of E128 and a test of E129, each one before the change; then the repeated
   walk, with the warnings of the last walk only, and the record of the walk for the edited sketch. 1 to 2 agent hours.
2. R13: an ADR of the dimension overlay, with the questions of the owner, then the code.
3. P0.76 (TASK-0177): V76, V77, E130 and T130 to T132, each with a test or an injection before the change. 2 to 3 agent
   hours.

## Questions for the owner

None. The owner gave the order of the next work on 2026-11-03, and section 6 gives the approach of P0.75 within
ADR-0052 and ADR-0055.
