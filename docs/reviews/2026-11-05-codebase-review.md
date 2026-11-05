---
date: 2026-11-05
commit: 3cd76af
ledger: v0.181
previous: 2026-11-03-b-codebase-review.md
---

# Codebase review — 2026-11-05

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the twenty-first codebase review. The review of 2026-11-03 (b) examined v0.176. After it came 1 product
milestone (R13) and 5 ledger entries (v0.177 to v0.181). The limits are four and twelve, so this review is not due by
the gate: on 2026-11-05 the owner gave the order "start the codebase review", and later "merge the review when green".
`Engine.Tests/Governance/CodebaseReviewGateTests.cs` holds the rule, and `docs/templates.md`, section 7, gives the form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is the
authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this file does not.

| Item | Value |
|---|---|
| Code state | `main` at `3cd76af`, the merge of v0.181 |
| Branch of this review | `codebase-review-2026-11-05` |
| Tests | 1,687 on this computer (1,188 in `Engine.Tests`, 308 in `3DEngine.Tests`, 188 in `3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`) |
| Pipeline | The run on `3cd76af` (41981194015): green on Ubuntu, Windows and macOS, and the three determinism hashes are equal |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner decides.

**Method.** Five tasks changed code or records after `db28439`, the commit of the last review: TASK-0175 (the review),
TASK-0176 (P0.75), TASK-0178 (R13), TASK-0179 (P0.77) and TASK-0177 (P0.76). The change is about 2,400 lines in 38
files, 1,100 of them in 24 C# files: the repeated walk of the planes, the dimension overlay, the far plane of the
camera, and the fixes of the last review. Two review agents worked in parallel at `3cd76af`, each one in its own git
worktree: the engine with the hosts, and the desktop with the gates. Each agent wrote probes and injections that only
its worktree held, and restored or deleted them. I read the cited lines of the new finding of the level medium and of
E131 and E133, and I measured the values below.

## Summary

- **The fixes of the last review hold for the forms that they named.** E128, E130, V77 and T130 to T132 are fixed for
  their forms, V76 is fixed, and E129 is fixed with no test that reaches the fix (E132). A probe found the next form of
  E130, T130, T131 and T132. **[Observed]**
- **After a hide, the far plane can drop below a visible body far from the target (V78, medium).** The rule of P0.77
  holds a box only while the target is inside it, and a hide makes the box smaller. **[Observed]** by a probe.
- **ADR-0056 items 2 and 7 disagree with the code (V83, low).** The toggles are on a line of their own, and a box
  draws up to 18 segments. Q2 asks which side is correct. **[Observed]**
- **Five more low findings of the dimension overlay and of the arm (V79 to V82, V84):** labels of a thin board that
  overlap, a label outside the window, a union of each box in each frame, lines that do not follow a drag, and a call of
  the arm that no test drives. **[Observed]**
- **Three low findings of the engine (E131 to E133)** and **the next forms of three gates (T133 to T135, low).** T133
  lets `Engine.Core` reference `3DEngine.Core` through an item `ReferencePath`. **[Observed]** by probes and injections.
- **No new finding is critical or high.** **[Observed]**
- **Open findings: 0 critical, 4 high, 15 medium and 28 low.** **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 15 | `.csproj` lines in `3DEngine.sln`. The last review gave 15. |
| Production source lines | 25,949 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests`, `3DEngine.ImageTests` and `3DEngine.VisionTests`, 251 files. The last review gave 25,547 in 248 files. |
| Test source lines | 30,397 | `Engine.Tests` (21,824 in 109 files), `3DEngine.Tests` (3,430 in 22 files), `3DEngine.ImageTests` (5,078 in 31 files) and `3DEngine.VisionTests` (65 in 1 file). The last review gave 29,785. |
| Tests | 1,687 | `dotnet test 3DEngine.sln`: 1,188, 308, 188 and 3 passed, 0 failed, 0 skipped. The last review gave 1,636. |
| Gate classes | 19 | Tracked files whose name ends in `GateTests.cs`. `eng/repo-gates` gives 398 runs of 145 checks. The last review gave 19, and 384 runs. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 14 | Section "Open" of `docs/register.md`; the limit is 15. The last review gave 14; R-0051 was opened and closed after it. |
| ADRs | 56 | `docs/adr/0001` to `0056`, with 0003 and 0011 in `docs/adr/archive/`. In force: 54 (27 accepted, 27 amended). None is proposed. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 4 | The same method: V2, V3, T4, T31. The last review gave 4. |
| Findings: medium | 15 | The same method: E5, E6, E12, E13, E19, E21, E22, V4, V78, T2, T5, T10, T17, T32, T37. The last review gave 18; E128, E129, V76 and V77 are fixed. |
| Findings: low | 28 | The same method: E17, E24, E34, E39, E57, E84, E131 to E133, V7, V15, V25, V29, V79 to V84, T7, T13, T23, T25, T133 to T135, C1, P1. The last review gave 20; E130 and T130 to T132 are fixed. |
| Lines in the rule files | 2,706 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The last review gave 2,673. |
| Sentences with "must" | 58 | `grep -o -w -i must` on the same ten files. The last review gave 58. |
| Sentences with "do not" | 54 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 54. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found. E13, T32 and E84 are fixed in part and count as open. A finding that the owner accepted counts as closed.

## Findings of the previous review

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. No change: `Engine.Api.Http/Endpoints/JsonParameters.cs` has no commit since `db28439`. Register entry R-0027.
- **E6** — Open. No change: `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` has no commit since `db28439`, and it still lists `document.loaded`, `document.replayed` and `document.saved`. Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. No change.
- **E8** — Fixed. TASK-0044, v0.37. No change.
- **E9** — Fixed, and it holds. No change.
- **E10** — Fixed. TASK-0035, v0.43. No change.
- **E11** — Fixed. TASK-0115, v0.115. No change.
- **E12** — Open, and smaller. No change: `Engine.Core/Hosting/ParameterBinder.cs` has no commit since `db28439`. Register entry R-0027.
- **E13** — Fixed in part. No change.
- **E14** — Fixed. TASK-0115, v0.115. No change.
- **E15** — Fixed. TASK-0044, v0.37. No change.
- **E16** — Fixed. TASK-0115, v0.115. No change.
- **E17** — Open, and smaller. No change: `Engine.Core/CommandBus.cs` has no commit since `db28439`. Register entry R-0028.
- **E18** — Fixed for the sinks of `Engine.*` (TASK-0140, v0.141) and of the desktop host (T118, TASK-0150, v0.154). Correction: the line of the last review lost the end of its code span. No change.
- **E19** — Open, and smaller. No ledger entry since v0.176 names it. Register entry R-0033.
- **E20** — Fixed, and it holds. No change.
- **E21** — Open, and wider by decision. No ledger entry since v0.176 names it. Register entry R-0033.
- **E22** — Open. No change: `Engine.Core/CommandBus.cs` has no commit since `db28439`. Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. No change.
- **E24** — Open in seven parts of nine. No change: `WireMessage.cs` and `SchemaEventsEndpoint.cs` have no commit since `db28439`. Register entry R-0033.
- **E25** — Accepted by the owner. TASK-0142, v0.144. Register entry R-0036. No change.
- **E26** — Fixed. TASK-0135, v0.136. No change.
- **E27** — Accepted by the owner. TASK-0142, v0.144. Register entry R-0036. No change.
- **E28** — Fixed. TASK-0136, v0.137. No change.
- **E29** — Fixed for the replay. TASK-0135, v0.136. No change.
- **E30** — Fixed. TASK-0136, v0.137. No change.
- **E31** — Fixed for the replay. TASK-0137, v0.138. No change.
- **E32** — Fixed. TASK-0135, v0.136. No change.
- **E33** — Fixed with E18. TASK-0140, v0.141. No change.
- **E34** — Open. No change: `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs` has no commit since `db28439`. Register entry R-0036.
- **E35** — Fixed for the routes of `MapGet`. TASK-0139, v0.140. No change.
- **E36** — Fixed. TASK-0079, v0.85. No change.
- **E37** — Fixed. TASK-0137, v0.138. No change.
- **E38** — Fixed. TASK-0138, v0.139. No change.
- **E39** — Open. No change: `Engine.Cli/Cli.cs`, `Engine.Api.Http.Server/ServerEngine.cs` and `3DEngine/DesktopSurface.cs` have no commit since `db28439`. Register entry R-0036.
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
- **E57** — Open. No change: `Engine.Tests/Http/DesktopSurfaceTests.cs` has no commit since `db28439`. Register entry R-0039.
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
- **V2** — Open. No change: `3DEngine.Vulkan/OverlayRenderer.cs` has no commit since `db28439`. Register entry R-0022.
- **V3** — Open. No change: `3DEngine.Vulkan/OverlayRenderer.cs` has no commit since `db28439`. Register entry R-0022.
- **V4** — Open. No ledger entry since v0.176 names it. Register entry R-0027.
- **V5** — Fixed. TASK-0076, v0.74. No change.
- **V6** — Fixed. TASK-0076, v0.74. No change.
- **V7** — Open. No change: `3DEngine.Vulkan/Ui/UiRenderer.cs`, `3DEngine.Vulkan/Ui/UiTexture.cs`, `3DEngine.Vulkan/GpuBuffer.cs` have no commit since `db28439`. Register entry R-0030.
- **V8** — Fixed. TASK-0074, v0.75. No change.
- **V9** — Fixed. TASK-0074, v0.75. No change.
- **V10** — Fixed. TASK-0074, v0.75. No change.
- **V11** — Fixed in part. TASK-0074, v0.75. No change.
- **V12** — Fixed, and changed by decision. No change.
- **V13** — Fixed for its parts. No change.
- **V14** — Fixed. TASK-0074, v0.75. No change.
- **V15** — Open. `3DEngine/SessionBodies.cs` gained two read accessors of the boxes for R13 (TASK-0178), and the wait for the idle device of each removed mesh stays (`SessionBodies.cs:270`).
- **V16** — Fixed. TASK-0079, v0.85. No change.
- **V17** — Fixed. TASK-0079, v0.85. No change.
- **V18** — Fixed for its forms. TASK-0090, v0.93. No change.
- **V19** — Fixed, with no injection. TASK-0079, v0.85. No change.
- **V20** — Fixed. TASK-0079, v0.85. No change.
- **V21** — Fixed by reading, with no test. TASK-0079, v0.85. No change.
- **V22** — Fixed for its forms. TASK-0079, v0.85. No change.
- **V23** — Fixed. TASK-0079, v0.85. No change.
- **V24** — Fixed. TASK-0079, v0.85. No change.
- **V25** — Open. No change: `3DEngine.Vulkan/3DEngine.Vulkan.csproj` has no commit since `db28439`. Register entry R-0020.
- **T1** — Fixed. TASK-0156, v0.158. Each run since then compares the three hashes. No change.
- **T2** — Open in one part of five. No ledger entry since v0.176 names it. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. No change.
- **T4** — Open. No change: `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs` has no commit since `db28439`. Register entry R-0031.
- **T5** — Open in one part of three. No ledger entry since v0.176 names it. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Open in four parts of five. No ledger entry since v0.176 names it. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. No change.
- **T10** — Open. No ledger entry since v0.176 names it. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51. No change.
- **T12** — Fixed. TASK-0059, v0.55. No change.
- **T13** — Open in part. No change: `.github/workflows/ci.yml` has no commit since `db28439`. The part of the marker gate stays. Register entry R-0031.
- **T14** — Fixed. TASK-0053, v0.51. No change.
- **T15** — Fixed. TASK-0050, v0.47. No change.
- **T16** — Fixed. TASK-0066, v0.67. No change.
- **T17** — Open. No ledger entry since v0.176 names it. Register entry R-0031.
- **T18** — Fixed. TASK-0049, v0.46. No change.
- **T19** — Fixed. TASK-0049, v0.46. No change.
- **T20** — Fixed. TASK-0049, v0.46. No change.
- **T21** — Fixed. TASK-0049, v0.46. No change.
- **T22** — Fixed. TASK-0049, v0.46. No change.
- **T23** — Open. No ledger entry since v0.176 names it. Register entry R-0031.
- **T24** — Fixed. No change.
- **T25** — Open in part. No ledger entry since v0.176 names it. Register entry R-0031.
- **T26** — Fixed. TASK-0057, v0.54. No change.
- **T27** — Fixed. TASK-0058, v0.56. No change.
- **T28** — Fixed. No change.
- **T29** — Fixed. TASK-0057, v0.54. No change.
- **T30** — Fixed. TASK-0057, v0.54. No change.
- **T31** — Open. No change: `eng/repo-gates/RepoGates.csproj` has no commit since `db28439`. Register entry R-0031.
- **T32** — Fixed in part. No change.
- **T33** — Fixed. TASK-0057, v0.54. No change.
- **T34** — Fixed. TASK-0057, v0.54. No change.
- **T35** — Fixed. TASK-0057, v0.54. No change.
- **C1** — Open. No change: `Engine.Contracts/Geometry/IBRepOps.cs`, `Engine.Contracts/Geometry/IFeatureIdMap.cs` have no commit since `db28439`. Register entry R-0027.
- **P1** — Open, and smaller. No ledger entry since v0.176 names it. Register entry R-0027.
- **T36** — Fixed. TASK-0061, v0.61. No change.
- **T37** — Open in part. No ledger entry since v0.176 names it. Register entry R-0031.
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
- **V29** — Open. No ledger entry since v0.176 names it.
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
- **E120** — Fixed for its forms. TASK-0172, v0.174. No change.
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
- **E125** — Fixed for its forms. TASK-0169, v0.171. The next form was E130. No change.
- **E126** — Fixed. TASK-0169, v0.171. No change.
- **E127** — Fixed. TASK-0169, v0.171. No change.
- **V74** — Fixed. TASK-0170, v0.172. No change.
- **V75** — Fixed for its form. TASK-0170, v0.172. No change.
- **T127** — Fixed for its forms. TASK-0170, v0.172. The next form was T130. No change.
- **T128** — Fixed for its forms. TASK-0170, v0.172. The next forms were T131. No change.
- **T129** — Fixed for its forms. TASK-0170, v0.172. The next forms were T132. No change.
- **E128** — Fixed for its forms. TASK-0176, v0.178: `DatumResolver.Rebuild` repeats its walk while a reference solves a sketch again (`DatumResolver.cs:134-140`), and each walk clears the datums and the warnings (`:148-149`). A probe of the layout of the review gave the same state after a redo and after an open, and a chain of eight later references converged with no exception. **[Observed]**
- **E129** — Fixed, and no test reaches the fix. TASK-0176, v0.178 (`SketchResults.cs:37`, `:49`): with the line of v0.176 put back, each test of `Engine.Tests` passes (E132). **[Observed]**
- **E130** — Fixed for its form. TASK-0177, v0.181 (`DatumResolver.cs:33`, `:335`): a side along an axis whose sum of squares is subnormal has unit axes, and each normal sum keeps its bits; `<` is correct at 2^-1022. The next form is E131. **[Observed]**
- **V76** — Fixed. TASK-0177, v0.181: the side comes from the plane (`SketchCamera.cs:24`, `NativeThreeDEngine.cs:395-402`), and the side from the target fails `The_Camera_Faces_A_Face_From_The_Side_Of_Its_Plane_Where_The_Eye_Is`. **[Observed]**
- **V77** — Fixed for its forms. TASK-0177, v0.181 (`FaceArm.cs:21-35`): only `Create` and `Edit` make a sketch active, and only the four actions that end the arm call them; no key and no HTTP path does. No test drives the call of the host (V84). **[Observed]**
- **T130** — Fixed for its forms. TASK-0177, v0.181 (`ItemOutputOffences`, `DependencyDirectionGateTests.cs:564-567`). The next form is T133. **[Observed]**
- **T131** — Fixed for its forms. TASK-0177, v0.181 (`DiagnosticsRegistryGateTests.cs:174-180`). The next forms are T134. **[Observed]**
- **T132** — Fixed for its forms. TASK-0177, v0.181 (`CodebaseReviewGateTests.cs:389`): a table and a rule after a blank line give no problem, and the ledger passes. The next forms are T135. **[Observed]**

## 1 Current architecture

**[Observed]** The walk of the planes (`DatumResolver.Rebuild`, the first walk of the regeneration of ADR-0055)
repeats while a reference solves a sketch again, so a datum or a reference before a later reference of its sketch
follows it at the same edit. The edit of a sketch takes the record of the walk for itself.

**[Observed]** The desktop draws the dimensions of the box of the visible bodies, or of each live body, and of the
selected body, as lines with labels in the scene (ADR-0056, R13). A toggle of the panel turns them on; nothing enters
the log. The far plane of the camera follows the box of the visible bodies at each change of the bodies (P0.77).

**[Observed]** The camera of a sketch on a face takes its side from the plane of the face, and `FaceArm` holds the arm
of the button Face. Each `Output` of an MSBuild task with an item name is a gate offence.

## 2 Patterns in use

- **A rule that holds under a condition states the condition in a comment, and the caller does not keep it.** The
  far plane holds a box with the target inside it (`Camera.cs:59-61`), and a hide breaks that (V78). **[Observed]**
- **A guard that no state reaches stays, and no test proves it** (E132), as the bound of the walk does. **[Observed]**
- **The text of an ADR and the code part at the first build, and the ledger records the reason while the ADR does
  not** (V83). **[Observed]**
- **Each correction of a gate holds for its forms, and a probe finds the next form.** T133 follows T130, T134 follows
  T131, and T135 follows T132. **[Observed]**

## 3 Review of the code

### The engine and the hosts

#### E131 · Low · A datum on a diagonal side whose components are subnormal gets axes whose length is not 1 · Confirmed

- **[Observed]** The scaled branch (`Engine.Core/Sketch/DatumResolver.cs:335-339`) computes the length from the
  scaled components, and `:342` divides the components `tx` and `ty`, which are subnormal, by it, so the axis keeps
  the error of a subnormal number.
- **[Observed]** A probe of `CreateDatum` on the side (0, 0) to (L, L): L = 1e-310 gave `|U| = 1.0000000000000084`,
  1e-315 gave 1.0000000008645558, 1e-320 gave 1.0001286688480588, and 5e-324 gave `U = (1, 1, 0)`, of the length
  1.4142135623730951. A sketch of 10 by 10 on the last datum, extruded by 1, gave a box of 11 by 10 by 10.
- **[Inferred]** A sketch on such a datum is scaled by up to the square root of 2, against ADR-0051 item 3. Each
  platform gives the same bits.
- **[Recommended]** In the scaled branch, divide the scaled components by their own root, and add a diagonal case to
  the theory of E130.

#### E132 · Low · No test fails when the fix of E129 is removed · Confirmed

- **[Observed]** With `changedSketches: [solved, .. sketches]` put back in `SketchResults.cs:49`, each of the 1,197
  tests of `Engine.Tests` of the probe passed. After the fix of E128 the walk does not solve the edited sketch again in
  any state that the probes found, and the ledger of v0.178 records that the guard has no test.
- **[Inferred]** If the guard acts, the record of the walk comes from `SketchSolver.Solve` with no hold
  (`DatumResolver.cs:196`), so a held point could move with no `W-SKETCH-HOLD-DROPPED`. No probe reached that case.
- **[Recommended]** A test of `SketchResults.Changed` on a Document with a stale reference that the test builds, or a
  register entry that names the guard as unreachable.

#### E133 · Low · The sketches that the walk of the planes changes are not in the order of the log, which the comment states · Confirmed

- **[Observed]** `DatumResolver.cs:95-97` says that the outcome gives each sketch that changes "in the order of the
  log", and `changedSketches` keeps the order of the first solve over all walks (`:198-199`).
- **[Observed]** A probe with the wall created before the base gave `sketch.changed` in the order post, base, wall.
- **[Inferred]** The order is the same in a replay; only the comment and the expectation of a client are wrong.
- **[Recommended]** Sort the changed sketches by the order of their command in the list, or correct the comment.

**No finding.** **[Observed]** The bound of the repeated walk never throws for a document that the cycle check
permits: each walk that returns true needs a distinct later reference. Each warning comes from the last walk only, a
held move of a corner of the base keeps its point, and the changed code calls `Math.Sqrt`, `Max` and `Abs` only. The
threshold of E130 keeps the bits of each normal sum. No file of `Engine.Contracts`, `Engine.Api.Http*`, `Engine.Cli` or
`Engine.Geometry.Manifold` changed.

### The desktop

#### V78 · Medium · After a hide, the far plane can drop below a visible body far from the target · Confirmed

- **[Observed]** `SceneRenderer.Update` (`3DEngine/SceneRenderer.cs:87-89`) sets `FarPlaneFor` of the box of the
  visible bodies, and `FarPlaneFor` (`3DEngine.Core/Models/Camera.cs:59-61`) holds each point only while the target is
  inside that box.
- **[Observed]** A probe: a box A of 100 at the origin, framed at the distance 173.2, and a box B of 100 at x = 5000.
  The far plane of the union is 40815.7. The eye turns to B (yaw −90), and B is drawn. A hide of A gives the far plane
  1385.6, and B lies beyond it, so B is not drawn.
- **[Inferred]** A pan or a zoom in has the same effect. A person sees a body vanish after a hide of another body.
- **[Recommended]** Compute the far plane from the eye: the distance from the eye to the centre of the box plus its
  radius, with a margin, after each change of the camera or in each frame. Add a test of this hide.

#### V79 · Low · The labels of a selected thin board overlap · Confirmed

- **[Observed]** `3DEngine/DimensionOverlay.cs:27` and `:58` put each label at one offset from the middle of its line,
  and nothing keeps two labels apart. With the camera of the image test and the font of 26 pixels, "Y 18 mm" and
  "Z 370 mm" overlap by 96 by 7 pixels. The ledger of v0.179 records that the labels "can touch".
- **[Recommended]** Move a label that meets another one, or hide the label of a line that is shorter than its text.

#### V80 · Low · A dimension line partly behind the eye shows with no label · Confirmed

- **[Observed]** `SketchOverlay.Frame.Segment` (`3DEngine/SketchOverlay.cs:314-326`) clips to a band of one window
  on each side, and the label is at the middle of the clipped segment (`DimensionOverlay.cs:58`). A probe with a close
  camera put the label "X 1200 mm" at (38, 844) in a window of 1280 by 720, while the line shows.
- **[Recommended]** Put the label at the middle of the part of the line inside the window.

#### V81 · Low · The overlay makes the union of each box in each frame · Confirmed

- **[Observed]** `3DEngine/NativeThreeDEngine.cs:528` calls `Dimensions.Union(_renderer.Boxes(...))` in each frame.
  ADR-0056 item 7 names a fixed cost. A probe in a Debug build gave 0.039 ms for 10 boxes, 0.544 ms for 1000 and 2.067
  ms for 10000.
- **[Recommended]** Keep the union, and make it again only when `Update` reports a change or a toggle changes.

#### V82 · Low · The dimension lines do not follow a drag of the gizmo · Confirmed by reading

- **[Observed]** `Preview` (`3DEngine/SessionBodies.cs:302-305`) moves the entity only, and `Boxes` and `BoxOf`
  (`:323-329`) read the boxes of the meshes, which change when the `Translate` applies.
- **[Inferred]** During a drag with the dimensions on, the lines stay at the old place while the body moves.
- **[Recommended]** Add the offset of the preview to the box of the dragged body, or hide its lines during a drag.

#### V83 · Low · ADR-0056 items 2 and 7 disagree with the code · Confirmed

- **[Observed]** Item 2 (`docs/adr/0056-dimension-overlay.md:48`) puts "Dimensions" on the line of "Show all"; the code
  puts both toggles on a line of their own before the list of the bodies (`3DEngine/Ui/DesktopUi.cs:623-629`), and the
  ledger of v0.179 gives the reason. Item 7 (`:67`) says six to twelve segments; a box draws three dimension lines and
  six extension lines, so two boxes draw up to 18, as `DimensionOverlayTests` counts.
- **[Recommended]** Q2: amend items 2 and 7 to the code, with a history line.

#### V84 · Low · No test drives the call of FaceArm in the host, so the button Face can lose its function with no failure · Confirmed

- **[Observed]** An injection replaced `NativeThreeDEngine.cs:250` with a comment, so the button never arms; 308 tests
  of `3DEngine.Tests` and 188 of `3DEngine.ImageTests` passed. The ledger of v0.181 records the gap.
- **[Recommended]** Move the loop of the actions of the panel, or at least the call of the arm, into a class that a test
  drives.

**No finding.** **[Observed]** In each of the eight octants of the eye, at the origin and at 1e6, each dimension line is
outside the box on the side of the eye. The thickness follows Q1 A, and the distance of the selected body is read once
for each change. `SceneRenderer.Update` frames in the cases of ADR-0029 item 8, `Zoom` stays below half the far plane,
and the depth buffer is `D32Sfloat`. Only the four actions that end the arm make a sketch active.

### The gates and the tests

#### T133 · Low · An item `ReferencePath` in a target makes Engine.Core reference 3DEngine.Core, and each gate passes · Confirmed

- **[Observed]** An injection of `<Target AfterTargets="ResolveAssemblyReferences"><ItemGroup><ReferencePath
  Include="..\3DEngine.Core\bin\Debug\net10.0\3DEngine.Core.dll" /></ItemGroup></Target>` in `Engine.Core/Engine.Core.csproj`
  and a file with `typeof(ThreeDEngine.Core.Picking.Picker)`: the build passed, 474 of 474 tests of the gates passed,
  and `eng/repo-gates` gave 0 failed. The control, the same element named `Reference`, failed
  `Each_File_That_MsBuild_Reads_Names_Its_References_As_Text`.
- **[Recommended]** Refuse each item element outside a short list in each file that MSBuild reads, or read the
  referenced assemblies of each built `Engine.*` assembly.

#### T134 · Low · A string call after ToString hides a severity from the scanner · Confirmed

- **[Observed]** `PartsOfCodes` gave 0 parts for `S.ToString().ToUpperInvariant() + rest`, `S.ToString().Trim() + rest`,
  `S.ToString().Insert(1, rest)`, `new StringBuilder(S.ToString()).Append(rest).ToString()`,
  `rest.PadLeft(rest.Length + 1, S)`, `string.Concat(Enumerable.Repeat(S, 1)) + rest` and `string.Create(1, S, ...) +
  rest`, and 1 part for each control of T131. `Outer` (`Engine.Tests/Diagnostics/DiagnosticsScanner.cs:137-148`) stops
  at a member call other than `ToString`.
- **[Recommended]** Walk through each string call on the node, or record the remaining calls as a rule in text.

#### T135 · Low · An ATX heading with no v, at an indent of four or more in a list item, is not a heading problem · Confirmed

- **[Observed]** `HeadingProblems` gave 0 problems for `"- An entry\n    ## 0.11 — …"`, `"- An entry\n\n    ## Version
  0.11 — …"` and `"10. An entry\n     ## 0.11 — …"`, and 1 for the controls `"## 0.11 — …"` and `"> - An entry\n>
  ## 0.11 — …"`. `heading` takes at most three spaces (`CodebaseReviewGateTests.cs:372`), and `nested` needs the list
  marker on the same line (`:375`).
- **[Recommended]** A `#` at any indent after a list item is a heading problem, as the setext rule of T132 is.

**No finding.** **[Observed]** `eng/test-floor.txt` agrees with the counts with and without `CI=true`, and
`ItemOutputOffences` leaves `PropertyName` free. A table and a rule after a blank line give no heading problem.

**Not verified.** Linux, macOS and arm64; the desktop window for V76 to V84; timings in a Release build; V82 by a run;
T133 in the pipeline; the widths of V79 with another font atlas.

## 4 The next planned task

The roadmap order holds no ready task. The open product rows are P8b, K4 to K7 and R12, which waits for K4, and the
owner chose a direction for a redesign of the desktop on 2026-11-05, which has no row yet. Q1 asks for the next product
phase. By `docs/templates.md`, section 7, the medium and the low findings go after it: P0.78 (TASK-0181) holds V78 to
V84, and P0.79 (TASK-0182) holds E131 to E133 and T133 to T135.

## 5 Research for that task

**[Observed]** V78 and V81 have one place: the frame of the desktop reads the boxes and the camera. A far plane from the
eye (V78) needs the box and the eye in each frame, and a kept union (V81) gives the box at no cost, so P0.78 fixes them
together. V79, V80 and V82 change `DimensionOverlay` only. **[Inferred]**

**[Observed]** The redesign of the desktop changes the panel and the overlay that P0.78 touches. If Q1 is A, the
findings of the overlay can move into the redesign, and P0.78 keeps V78 and V84. **[Inferred]**

## 6 Recommended approach

**[Recommended]** In this order:

1. The phase of Q1, with its ADR and its questions.
2. P0.78 (TASK-0181): V78 to V84, each with a test or an injection before the change, and the amendment of ADR-0056 of
   Q2. 2 to 3 agent hours.
3. P0.79 (TASK-0182): E131 to E133 and T133 to T135, each with a test or an injection before the change. 1 to 2 agent
   hours.

## Questions for the owner

**Q1. The next product phase.**
- A: The redesign of the desktop, in the direction of 2026-11-05, as a new row of track R with its ADR.
- B: K4, planar faces with a boolean and provenance in the kernel.
- C: P8b, the container model.

**Recommended: A.** The owner chose its direction on 2026-11-05, and it replaces the panel and the overlay that the
findings V79 to V84 touch, so those findings can go into it.

**Q2. ADR-0056 items 2 and 7 against the code (V83).**
- A: The code is correct: the toggles stay on a line of their own, and item 7 says up to 18 segments. P0.78 amends the
  items with a history line.
- B: The ADR is correct: the toggles go on the line of "Show all".

**Recommended: A.** At the font size of ADR-0027 the line of "Show all" does not hold the two toggles, and a long list
of bodies pushed them out of the panel (the ledger of v0.179).
