---
date: 2026-10-31
commit: e851093
ledger: v0.149
previous: 2026-10-31-b-codebase-review.md
---

# Codebase review — 2026-10-31 (c)

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the seventeenth codebase review. The review of 2026-10-31 (b) examined v0.141. After it came 4 product
milestones (R11b, V0, V1b and V1a) and 8 ledger entries (v0.142 to v0.149). The limits are four and twelve, so the
next product milestone would make the review late. The owner told the session on 2026-10-31: "do the review, then
start V2". `Engine.Tests/Governance/CodebaseReviewGateTests.cs` holds the rule, and `docs/templates.md`, section 7,
gives the form.

The owner told the session on 2026-10-31 to work alone until 2026-11-01 10:00 and to use its recommendation for each
question. So each question below has a recommended answer, and the tasks of this review use it; the owner can change
an answer later.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is the
authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this file does not.

| Item | Value |
|---|---|
| Code state | `main` at `e851093`, the merge of v0.149. Its tree is the tree of `b410a59`, the tip of the branch of V1a |
| Branch of this review | `codebase-review-2026-10-31-c` |
| Tests | 1,420 on this computer (1,033 in `Engine.Tests`, 237 in `3DEngine.Tests`, 147 in `3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`) |
| Pipeline | The run on `e851093`: green on Ubuntu, Windows and macOS |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner decides.

**Method.** Eight tasks changed code or records after `296ae13`, the commit of the last review: TASK-0141 (the review),
TASK-0142 to TASK-0144 (P0.60 to P0.62), TASK-0134 (R11b), TASK-0145 (V0), TASK-0147 (V1b) and TASK-0146 (V1a). The
change is about 1,700 lines in 66 code files: a store of assets in the engine with two routes and a verb, an image on a
quad in the renderer, a spike of the vision library, and the fixes of the last review. Two review agents worked in
parallel at `b410a59`, each one in its own git worktree: the engine with the hosts, and the desktop with the gates.
Each agent wrote probes that only its worktree holds. I ran the ten probes of the engine and the thirteen probes of the
desktop and the gates again, and read their output. I injected T117 with its control, and read the cited lines of each
finding.

## Summary

- **The fixes of the last review hold for the forms that they named.** E106 to E112, V63, V64 and T110 to T116 are
  fixed. The owner accepted E25 and E27 (TASK-0142). **[Observed]**
- **The store of the assets takes a name that ends in a line feed (E113, medium).** The pattern `^[0-9a-f]{64}$`
  matches before a final `\n`. On Windows the read then throws: the route gives a 500 with no body, and the command line
  throws with no exit code. **[Observed]** I ran the probes.
- **An imported file pins the vision library into the engine, and the gate passes (T117, medium).** The rule of ADR-0042
  reads the project file and not its imports. I injected it: 112 of 112 passed; the control in the project file failed.
  **[Observed]**
- **The store has four smaller faults at its edges (E114 to E116, E118, low).** An input or output error that is not
  "not found" escapes, a put does not repair a damaged file, a cancelled put leaves its scratch file, and a body over
  the limit of the server gives a 413 with no code. **[Observed]**
- **The fixes of the last review left their siblings in the open (E117, V65, T119 to T122, low).** The add of an arc
  keeps an exact test, a release after Escape still selects, and four forms pass four gates. **[Observed]**
- **The mark of the snap has two faults (V66, V67, low).** At the end of an arc it is not where the point goes, and it
  stays after a finger lifts. **[Observed]** for the first; by an agent and a read of the code for the second.
- **No high finding is new.** Open findings: 0 critical, 5 high, 16 medium and 29 low. **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 15 | `.csproj` lines in `3DEngine.sln`. The last review gave 14; V0 added `3DEngine.VisionTests`. Git tracks 16 project files: the native package is not in the solution. |
| Production source lines | 21,363 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests`, `3DEngine.ImageTests` and `3DEngine.VisionTests`, 216 files. The last review gave 20,702 in 212 files; with its three test projects only, this commit gives 21,428 in 217 files. |
| Test source lines | 24,813 | `Engine.Tests` (18,249 in 97 files), `3DEngine.Tests` (2,699 in 18 files), `3DEngine.ImageTests` (3,800 in 21 files) and `3DEngine.VisionTests` (65 in 1 file). The last review gave 23,973. |
| Tests | 1,420 | `dotnet test 3DEngine.sln`: 1,033, 237, 147 and 3 passed, 0 failed, 0 skipped. The last review gave 1,375. |
| Gate classes | 19 | Tracked files whose name ends in `GateTests.cs`. `eng/repo-gates` gives 353 runs of 141 checks. The last review gave 19. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 14 | Section "Open" of `docs/register.md`; the limit is 15. This review adds none. The last review gave 14. |
| ADRs | 44 | `docs/adr/0001` to `0044`, with 0003 and 0011 in `docs/adr/archive/`. In force: 42 (21 accepted, 21 amended). None is proposed. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 5 | The same method: V2, V3, T1, T4, T31. The last review gave 5. |
| Findings: medium | 16 | The same method: E5, E6, E12, E13, E19, E21, E22, E113, V4, T2, T5, T10, T17, T32, T37, T117. The last review gave 17. |
| Findings: low | 29 | The same method: E17, E24, E34, E39, E57, E84, E114 to E118, V7, V15, V25, V29, V65 to V67, T7, T13, T23, T25, T118 to T122, C1, P1. The last review gave 31. |
| Lines in the rule files | 2,520 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The last review gave 2,493. |
| Sentences with "must" | 58 | `grep -o -w -i must` on the same ten files. The last review gave 58. |
| Sentences with "do not" | 54 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 54. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found. E13, T32 and E84 are fixed in part and count as open. A finding that the owner accepted counts as closed.

## Findings of the previous review

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. No change: `Engine.Api.Http/Endpoints/JsonParameters.cs` has no commit since `296ae13`. Register entry R-0027.
- **E6** — Open. No change: `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` has no commit since `296ae13`. Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. No change.
- **E8** — Fixed. TASK-0044, v0.37. No change.
- **E9** — Fixed, and it holds. No change.
- **E10** — Fixed. TASK-0035, v0.43. No change.
- **E11** — Fixed. TASK-0115, v0.115. No change.
- **E12** — Open. No ledger entry since v0.141 names it. Register entry R-0027.
- **E13** — Fixed in part. No change.
- **E14** — Fixed. TASK-0115, v0.115. No change.
- **E15** — Fixed. TASK-0044, v0.37. No change.
- **E16** — Fixed. TASK-0115, v0.115. No change.
- **E17** — Open. No change: `Engine.Core/CommandBus.cs` has no commit since `296ae13`. Register entry R-0028.
- **E18** — Fixed for the sinks of `Engine. TASK-0140, v0.141. No change.
- **E19** — Open, and smaller. No ledger entry since v0.141 names it. Register entry R-0033.
- **E20** — Fixed, and it holds. No change.
- **E21** — Open, and wider by decision. No ledger entry since v0.141 names it. Register entry R-0033.
- **E22** — Open. No change: `Engine.Core/CommandBus.cs` has no commit since `296ae13`. Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. No change.
- **E24** — Open. No change: `Engine.Api.Http/WebSockets/WireMessage.cs`, `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` have no commit since `296ae13`. Register entry R-0033.
- **E25** — Accepted by the owner. TASK-0142, v0.144: no production code replays a log, and the open of a document file is the load path that continues live. Register entry R-0036.
- **E26** — Fixed. TASK-0135, v0.136. No change.
- **E27** — Accepted by the owner. TASK-0142, v0.144: its refusal needs a change of `Engine.Contracts`, and only a test or a script replays a log. Register entry R-0036.
- **E28** — Fixed. TASK-0136, v0.137. No change.
- **E29** — Fixed for the replay. TASK-0135, v0.136. No change.
- **E30** — Fixed. TASK-0136, v0.137. No change.
- **E31** — Fixed for the replay. TASK-0137, v0.138. No change.
- **E32** — Fixed. TASK-0135, v0.136. No change.
- **E33** — Fixed with E18. TASK-0140, v0.141. No change.
- **E34** — Open. `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs` changed for a comment only (E110, TASK-0142); the version is still the constant `NativeVersion` at line 57. Register entry R-0036.
- **E35** — Fixed for the routes of `MapGet`. TASK-0139, v0.140. No change.
- **E36** — Fixed. TASK-0079, v0.85. No change.
- **E37** — Fixed. TASK-0137, v0.138. No change.
- **E38** — Fixed. TASK-0138, v0.139. No change.
- **E39** — Open. `Engine.Cli/Cli.cs` changed for the verb `asset` (TASK-0147) and `Engine.Api.Http.Server/ServerEngine.cs` for E111 (TASK-0142); each still holds its own selection of the backend (`Cli.cs:305-309`, `ServerEngine.cs:31-37`), and `3DEngine/DesktopSurface.cs:109` holds a third. Register entry R-0036.
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
- **E57** — Open. `Engine.Tests/Http/DesktopSurfaceTests.cs` changed for the constructor of `EngineHost` only (E111, TASK-0142). Register entry R-0039.
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
- **V2** — Open. No change: `3DEngine.Vulkan/OverlayRenderer.cs` has no commit since `296ae13`. Register entry R-0022.
- **V3** — Open. No change: `3DEngine.Vulkan/OverlayRenderer.cs` has no commit since `296ae13`. Register entry R-0022.
- **V4** — Open. No ledger entry since v0.141 names it. Register entry R-0027.
- **V5** — Fixed. TASK-0076, v0.74. No change.
- **V6** — Fixed. TASK-0076, v0.74. No change.
- **V7** — Open. No change: `3DEngine.Vulkan/Ui/UiRenderer.cs`, `3DEngine.Vulkan/Ui/UiTexture.cs`, `3DEngine.Vulkan/GpuBuffer.cs` have no commit since `296ae13`. Register entry R-0030.
- **V8** — Fixed. TASK-0074, v0.75. No change.
- **V9** — Fixed. TASK-0074, v0.75. No change.
- **V10** — Fixed. TASK-0074, v0.75. No change.
- **V11** — Fixed in part. TASK-0074, v0.75. No change.
- **V12** — Fixed, and changed by decision. No change.
- **V13** — Fixed for its parts. No change.
- **V14** — Fixed. TASK-0074, v0.75. No change.
- **V15** — Open. No change: `3DEngine/SessionBodies.cs` has no commit since `296ae13`.
- **V16** — Fixed. TASK-0079, v0.85. No change.
- **V17** — Fixed. TASK-0079, v0.85. No change.
- **V18** — Fixed for its forms. TASK-0090, v0.93. No change.
- **V19** — Fixed, with no injection. TASK-0079, v0.85. No change.
- **V20** — Fixed. TASK-0079, v0.85. No change.
- **V21** — Fixed by reading, with no test. TASK-0079, v0.85. No change.
- **V22** — Fixed for its forms. TASK-0079, v0.85. No change.
- **V23** — Fixed. TASK-0079, v0.85. No change.
- **V24** — Fixed. TASK-0079, v0.85. No change.
- **V25** — Open. No change: `3DEngine.Vulkan/3DEngine.Vulkan.csproj` has no commit since `296ae13`. Register entry R-0020.
- **T1** — Open in one part of three. No ledger entry since v0.141 names it. Register entry R-0023.
- **T2** — Open in one part of five. No ledger entry since v0.141 names it. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. No change.
- **T4** — Open. No change: `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs` has no commit since `296ae13`. Register entry R-0031.
- **T5** — Open in one part of three. No ledger entry since v0.141 names it. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Open in four parts of five. No ledger entry since v0.141 names it. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. No change.
- **T10** — Open. No ledger entry since v0.141 names it. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51. No change.
- **T12** — Fixed. TASK-0059, v0.55. No change.
- **T13** — Open. No change: `.github/workflows/ci.yml` has no commit since `296ae13`. Register entry R-0031.
- **T14** — Fixed. TASK-0053, v0.51. No change.
- **T15** — Fixed. TASK-0050, v0.47. No change.
- **T16** — Fixed. TASK-0066, v0.67. No change.
- **T17** — Open. No ledger entry since v0.141 names it. Register entry R-0031.
- **T18** — Fixed. TASK-0049, v0.46. No change.
- **T19** — Fixed. TASK-0049, v0.46. No change.
- **T20** — Fixed. TASK-0049, v0.46. No change.
- **T21** — Fixed. TASK-0049, v0.46. No change.
- **T22** — Fixed. TASK-0049, v0.46. No change.
- **T23** — Open. No ledger entry since v0.141 names it. Register entry R-0031.
- **T24** — Fixed. No change.
- **T25** — Open in part. No ledger entry since v0.141 names it. Register entry R-0031.
- **T26** — Fixed. TASK-0057, v0.54. No change.
- **T27** — Fixed. TASK-0058, v0.56. No change.
- **T28** — Fixed. No change.
- **T29** — Fixed. TASK-0057, v0.54. No change.
- **T30** — Fixed. TASK-0057, v0.54. No change.
- **T31** — Open. No change: `eng/repo-gates/RepoGates.csproj` has no commit since `296ae13`. Register entry R-0031.
- **T32** — Fixed in part. No change.
- **T33** — Fixed. TASK-0057, v0.54. No change.
- **T34** — Fixed. TASK-0057, v0.54. No change.
- **T35** — Fixed. TASK-0057, v0.54. No change.
- **C1** — Open. No change: `Engine.Contracts/Geometry/IBRepOps.cs`, `Engine.Contracts/Geometry/IFeatureIdMap.cs` have no commit since `296ae13`. Register entry R-0027.
- **P1** — Open, and smaller. No ledger entry since v0.141 names it. Register entry R-0027.
- **T36** — Fixed. TASK-0061, v0.61. No change.
- **T37** — Open in part. No ledger entry since v0.141 names it. Register entry R-0031.
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
- **V29** — Open. No ledger entry since v0.141 names it.
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
- **E106** — Fixed. TASK-0142, v0.144.
- **E107** — Fixed. TASK-0142, v0.144.
- **E108** — Fixed for the circle and the constraint `radius`. TASK-0142, v0.144. The add of an arc keeps an exact test of its centre (E117).
- **E109** — Fixed. TASK-0142, v0.144.
- **E110** — Fixed. TASK-0142, v0.144.
- **E111** — Fixed. TASK-0142, v0.144.
- **E112** — Fixed. TASK-0142, v0.144.
- **V63** — Fixed. TASK-0144, v0.146.
- **V64** — Fixed for the drag. TASK-0144, v0.146. A release with no motion after Escape still selects (V65).
- **T110** — Fixed. TASK-0143, v0.145.
- **T111** — Fixed. TASK-0143, v0.145.
- **T112** — Fixed for the assemblies in the folder of the test project. TASK-0143, v0.145. The desktop host is not in that folder (T118).
- **T113** — Fixed for its characters. TASK-0143, v0.145. U+17B4, U+2065 and U+FFF0 pass (T121).
- **T114** — Fixed for its forms. TASK-0143, v0.145. A setext heading inside a list item passes (T122).
- **T115** — Fixed for a property. TASK-0143, v0.145. An escape of MSBuild in the item name passes (T119).
- **T116** — Fixed for the left operand of an addition. TASK-0143, v0.145. A `const char` in an interpolation or a call passes (T120).

## 1 Current architecture

**[Observed]** The engine keeps each asset under the SHA-256 of its bytes, in one folder of the account
(`Engine.Core/Assets/AssetStore.cs`, ADR-0044). A put writes a scratch file and moves it to its name, and a read computes
the name again. The surface gives `PUT /assets` and `GET /assets/{name}` outside the triad of ADR-0008, and the command
line gives `engine asset put` and `engine asset get`. No command and no record of the Document names an asset yet.

**[Observed]** The render side draws an image on a parallelogram of the scene after the bodies, with their depth test
(`3DEngine.Core/Rendering/ImageQuad.cs`, `3DEngine.Vulkan/ImageRenderer.cs`, ADR-0043). The desktop window draws no
quad yet. The vision library is in one test project only, `3DEngine.VisionTests` (ADR-0042), and two gates hold it
there. The open and the replay share the clause of their messages, the reader of a document file refuses a repeated
member, and `EngineHost` reads the identity of its backend (TASK-0142).

## 2 Patterns in use

- **A new store takes the errors of the file system at its edges.** The pattern of the name, the catch of the read, the
  early return of a put, the catch of a write and the limit of the server each miss one case (E113 to E116, E118).
  **[Observed]**
- **A gate reads the text of one file, not what the build reads.** The pin rule of the vision library reads the project
  file and not its imports (T117), and the gate of the sinks reads the assemblies in the folder of its own test project
  (T118). **[Observed]**
- **Each correction holds for its forms, and a probe finds the next form.** E117 follows E108, V65 follows V64, T119
  follows T115, T120 follows T116, T121 follows T113 and T122 follows T114. **[Observed]**

## 3 Review of the code

### The engine and the hosts

#### E113 · Medium · The store of the assets takes a name that ends in a line feed

`Engine.Core/Assets/AssetStore.cs:120` holds the pattern `^[0-9a-f]{64}$`, and in .NET `$` also matches before a final
`\n`. So `IsName` (`:32`) gives true for a valid name and a line feed. On Windows the read then throws `IOException`:
`GET /assets/<name>%0A` gives a 500 with no body, and `engine asset get` throws with no JSON and no exit code. On
Linux a line feed is legal in a file name, so the read probably gives "not found". **[Observed]** I ran the probes on
Windows. **[Recommended]** `\z` in place of `$`, and a test of the line feed.

#### E114 · Low · The read and the put of the store let other errors of the file system escape

`AssetStore.cs:98` catches `FileNotFoundException` and `DirectoryNotFoundException` only. A folder under a valid name
gives a 500 with no body from the route, and `UnauthorizedAccessException` from the command line. A stream that throws
during a put escapes the same way. **[Observed]** I ran the probes. **[Recommended]** `Read` and `Put` give a coded
error for `IOException` and `UnauthorizedAccessException`, and the command line gives an exit code for each one.

#### E115 · Low · A put of the correct bytes does not repair a damaged file under the same name

`AssetStore.cs:53` returns at once when a file with the name exists. After a change of one byte of the stored file, a
second put of the original bytes gives the name and no error, and the next read gives `E-ASSET-CORRUPT`. **[Observed]**
I ran the probe. **[Recommended]** When the existing file does not hash to its name, the put writes it again through a
scratch file and replaces it.

#### E116 · Low · A cancelled put leaves its scratch file in the store

`AssetStore.cs:78` catches `IOException` and `UnauthorizedAccessException`, and `TryDelete` runs only there. A
cancellation during the write throws `TaskCanceledException`, and the file `<name>.<guid>.tmp` stays. Over HTTP the
token is `RequestAborted`, so a client that disconnects can leave one. **[Observed]** I ran the probe.
**[Recommended]** Delete the scratch file in a `finally` when the move did not occur.

#### E117 · Low · The add of an arc accepts a radius that the solver says is no radius

`Engine.Core/Commands/AddSketchArcCommandHandler.cs:90` refuses a start or an end equal to the centre, and the solver
needs a distance greater than `SketchSolver.Tolerance`, 1e-9. An arc with its start 5e-10 from its centre is applied,
and the sketch shows as not solved; a circle of 5e-10 is refused since E108. **[Observed]** I ran the probe.
**[Recommended]** A live add of an arc refuses a start or an end whose distance from the centre is not greater than
`SketchSolver.Tolerance`; under `ApplyingEntry` the rule of 0 stays.

#### E118 · Low · A body over the limit of the server gives a 413 with no code

`Engine.Api.Http/Endpoints/AssetsEndpoint.cs:19` lets the `BadHttpRequestException` of Kestrel escape `store.Put`. A
body of 31 MB with a length gives a 413 with an empty body, and a body of 17 MB gives a 413 with `E-ASSET-TOO-LARGE`.
ADR-0044 items 3 and 4 promise the envelope. **[Observed]** I ran the probe. **[Recommended]** Refuse a
`Content-Length` over `MaximumBytes` before the read, and map the 413 of the server to `E-ASSET-TOO-LARGE`.

### The render side and the desktop

#### V65 · Low · A release after Escape on a handle still selects

`3DEngine.Core/Input/IntentMachine.cs:262` for the mouse and `:185` for a touch give a click or a tap for a press with
no motion. A press on a handle, Escape and a release with no motion give `SelectIntent`. The comment of `CancelGizmo`
says that the rest of the press gives nothing. **[Observed]** I ran the probes. **[Recommended]** A press that Escape
cancelled gives no click and no tap.

#### V66 · Low · At the end of an arc the mark of the snap is not where the point goes

`3DEngine.Core/Sketching/SketchTool.cs:119-125` (`Target`) gives the snapped position, and `PlaceCurve` then projects the
end onto the circle. With the centre (0, 0) and the start (10, 0), a tap at (0, 5) shows the mark at (0, 5), and the
point goes to (0, 10). **[Observed]** I ran the probe. **[Recommended]** `Target` projects onto the circle for the end
of an arc, as `PlaceCurve` does. ADR-0041 item 6 needs one sentence for it.

#### V67 · Low · The mark of the snap stays after a finger lifts, and the comment of the hover is false

`3DEngine.Core/Input/Intents.cs:38-40` says that the hover comes with no button down and that a touch gives none. A
touch down in the mode Sketch gives `SketchHoverIntent`, which ADR-0041 item 6 wants. `3DEngine/NativeThreeDEngine.cs:163`
is the only assignment of `_hover`, so the mark stays after the finger lifts, and ADR-0041 item 6 says "while it is
down". **[Observed]** for the intent, by a probe; **[Inferred]** for the mark, from the code. **[Recommended]** Correct
the comment, and clear `_hover` on a lift, a cancel and a change of mode.

### Tests, gates and the pipeline

#### T117 · Medium · An imported file pins the vision library into the engine, and the gate passes · Confirmed by a run

`Engine.Tests/Governance/DependencyDirectionGateTests.cs:893` reads the `PackageReference` items of each project file
with `Includes`, and not through `FilesThatMsBuildReads`. I added `Engine.Core/vision.props` with the reference
`OpenCvSharp4` and an `Import` of it in `Engine.Core.csproj`: `dotnet msbuild -getItem:PackageReference` gave
`OpenCvSharp4`, and `DependencyDirectionGateTests` passed 112 of 112. The control, the same reference in the project
file, failed `Only_The_Vision_Spike_Pins_The_Vision_Library`. The older rule `Each_Binding_Has_One_Pin_In_The_Vulkan_Layer`
reads in the same way. **[Observed]** **[Recommended]** The two pin rules read each file that MSBuild reads.

#### T118 · Low · The gate of the sinks does not read the desktop host

`Engine.Tests/EventSinkContractTests.cs:50` keeps a project only when its assembly is in the folder of `Engine.Tests`.
`Engine.Tests` does not reference `3DEngine`, so the gate skips it with no message. A class in `3DEngine` that implements
`IEventSink` and throws passes. **[Observed]** by an agent. **[Recommended]** Fail when a project of the solution that is
not a test has no assembly in the folder, or read `3DEngine` from `3DEngine.Tests`.

#### T119 · Low · An escape of MSBuild in an item name passes the check of T115

`DependencyDirectionGateTests.cs:518` (`IsCompileName`) reads the literal value. `ItemName="Comp%69le"` gives the item
`Compile` to MSBuild, and no offence. **[Observed]** by an agent. **[Recommended]** Decode each `%XX` before the check.

#### T120 · Low · A severity in a `const char` outside an addition passes the scanner of parts of codes

`Engine.Tests/Diagnostics/DiagnosticsScanner.cs:105` reads the left operand of an addition only. An interpolated string
`$"{S}-…"` and `string.Concat(S, "-", …)` with `const char S = 'E'` give no part. **[Observed]** by an agent.
**[Recommended]** Treat each constant `char` E, W or I as a part of a severity, as `IsSeverity` does for a string.

#### T121 · Low · Three more characters that do not show pass the control byte gate

`DependencyDirectionGateTests.cs:252-255`. U+17B4, U+2065 and U+FFF0 give no offence; U+17B4 is valid inside a C#
identifier. **[Observed]** by an agent; no tracked file holds one. **[Recommended]** Refuse the Unicode set
`Default_Ignorable_Code_Point`, with U+034F and U+2800 as exceptions.

#### T122 · Low · A setext heading of a version inside a list item passes the counter of the review

`Engine.Tests/Governance/CodebaseReviewGateTests.cs:377` accepts an underline with at most three spaces before it.
`- v0.11 …` with a next line `    ===` gives no problem; in CommonMark the indent of a list item moves the limit.
**[Observed]** by an agent; no Markdown parser rendered it. **[Recommended]** Refuse a line of `=` or `-` only after a
line that holds a version, at any indent.

### Correct

**[Observed]** by the agents, with the probes that passed:

- 32 parallel puts of the same bytes give one name and one file with no scratch file. A body of exactly 16 MiB is
  accepted, and one more byte is refused with nothing kept. A path or an upper-case name gives `E-ASSET-BAD-NAME`.
- A changed file gives `E-ASSET-CORRUPT`. The five codes of `ASSET` are in `docs/diagnostics.md` and in
  `/schema/diagnostics`, and each one is reachable. The new routes are behind the filter of the host, on 127.0.0.1.
- E106, E107, E109 to E112: the open throws a cancellation, the open and the replay share the clause, the reader refuses
  a repeated member in the header and in a nested object, and each caller of `EngineHost` gives only the backend.
- `ImageQuad.ModelView` subtracts the eye in double precision; at 1e7 the test passes. Row 0 of an image is the top of
  the quad. A wrong length of the pixels throws before an allocation; the seventeenth image throws.
- The push constants are 128 bytes, the two matrices of `image.vert`. A degenerate quad gives no message of the
  validation layer. The disposal frees the textures first.
- V63 ends an arc within `HitPixels`, T110 refuses disabled text in each file that the three gates read, T111 reads the
  routes of the test host, and `The_Vision_Spike_References_No_Project` reads the imports.
- No new file calls a function of rule 1, 3 or 4 of `CLAUDE.md`. Serial commit and event order do not change.

**Not verified.** E113 on Linux and macOS; `File.Move` on a file system with no hard links; a width over
`maxImageDimension2D`; a leak of `ImageRenderer` when its constructor fails; the mark of V67 in a live window.

## 4 The next planned task

V2 is the next product phase: "Place an image on a datum. Calibrate it to real units." The owner told the session on
2026-10-31 to start it after this review. `docs/templates.md`, section 7, puts the medium and low findings of this review
after it: P0.63 (TASK-0149) holds those of the engine and the hosts, P0.64 (TASK-0150) those of the gates, and P0.65
(TASK-0151) those of the desktop. No high finding is new, so no task comes before V2.

## 5 Research for that task

**[Observed]** I read the cited code.

- **A datum is a record of the contracts.** `Engine.Contracts/Sketch/Datums.cs` gives `Datum(Id, Name, U, V, Normal)`
  and the three origin planes with fixed identifiers. A sketch records the identifier of its datum (ADR-0033).
- **The Document holds no image.** A record of an image, its commands and its event kinds change the public shape of
  `Engine.Contracts`, so V2 needs an ADR in the same commit (`CLAUDE.md`, section "Stop and ask").
- **The log holds the inputs only (ADR-0033, Q6).** A calibration can record two points of the image in pixels and a
  real distance; the scale is the distance divided by a square root of a sum of squares, which rule 1 of `CLAUDE.md`
  permits. An angle in the plane would record a cosine and a sine (rule 2).
- **No project decodes an image file.** The desktop references `ImGui.NET`, `Alimer.Bindings.SDL` and `Vortice.Vulkan`
  only; ADR-0042 holds the vision library in one test project.
- **A copied document file does not carry its assets (ADR-0044 item 6).** So a replay must not need the bytes of an
  asset: the command records the width and the height of the image in pixels.

## 6 Recommended approach

**[Recommended]** In this order:

1. V2 (TASK-0152): an ADR for the record of an image on a datum and its calibration, with the commands, the events and
   the projection to `ImageQuad` in the desktop. It can split into V2a (the engine) and V2b (the desktop).
2. P0.63 (TASK-0149): E113 to E118, each with a test before the change. 2 to 3 agent hours.
3. P0.64 (TASK-0150): T117 to T122, each with an injection before the change. 2 to 3 agent hours.
4. P0.65 (TASK-0151): V65 to V67, with tests of the intent machine and of the tool, and one sentence in ADR-0041.
   1 to 2 agent hours.

## Questions for the owner

**Q1. Where an image of V2 lives.**
- A: The Document holds one record for each placed image: the name of its asset, the identifier of its datum, its
  origin in the datum, its width and its height in pixels, and its scale. New commands and new event kinds, with an
  ADR (a change of `Engine.Contracts`).
- B: The desktop keeps the placement of an image as a state of the client, and the Document does not know it.

**Recommended: A.** A calibrated image is the reference of V3, whose proposal becomes a sketch of the Document; a state
of the client is lost on a save and is not on the surface. The standing answer of the owner of 2026-10-31 takes A.

**Q2. What a calibration records.**
- A: Two points of the image in pixels and the real distance between them. The engine computes the scale.
- B: The scale as one number, which the client computes.

**Recommended: A.** The log holds the inputs (ADR-0033, Q6), and the person gives two points and a distance. The
standing answer of the owner of 2026-10-31 takes A.

**Q3. What decodes an image file.**
- A: The client decodes the pixels, and the command records the width and the height. V2 takes PNG files, through one
  managed decoder in the desktop, pinned in one project.
- B: The engine decodes the file of the asset at the apply of the command.

**Recommended: A.** A replay must not need the bytes of an asset (ADR-0044 item 6), and the engine needs no decoder
for a record. The standing answer of the owner of 2026-10-31 takes A.
