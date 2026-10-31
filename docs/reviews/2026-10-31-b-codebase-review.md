---
date: 2026-10-31
commit: 296ae13
ledger: v0.141
previous: 2026-10-31-codebase-review.md
---

# Codebase review — 2026-10-31 (b)

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the sixteenth codebase review. The review of 2026-10-31 examined v0.130. After it came 1 product milestone
(R11a) and 11 ledger entries (v0.131 to v0.141). The limits are four and twelve, and R11b (TASK-0134) waits on its
branch for the visual check of the owner, so its entry would be the twelfth. This review comes before it.
`Engine.Tests/Governance/CodebaseReviewGateTests.cs` holds the rule, and `docs/templates.md`, section 7, gives the
form.

The owner told the session on 2026-10-31 to work alone until the morning and to use its recommendation for each
question. So each question below has a recommended answer, and the tasks of this review use it; the owner can change
an answer later.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is the
authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this file does not.

| Item | Value |
|---|---|
| Code state | `main` at `296ae13`, the merge of v0.141. Its tree is the tree of `eb8c06f`, the tip of the branch of P0.59 |
| Branch of this review | `codebase-review-2026-10-31-b` |
| Tests | 1,375 on this computer (1,006 in `Engine.Tests`, 225 in `3DEngine.Tests`, 144 in `3DEngine.ImageTests`) |
| Pipeline | The run on `296ae13`: green on Ubuntu, Windows and macOS |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner decides.

**Method.** Eleven tasks changed code or records after `4f252bf`, the tree of the last review: TASK-0129 (the review),
TASK-0130 (P0.51), TASK-0131 (R11a), TASK-0132 (P0.52), TASK-0133 (P0.53), and TASK-0135 to TASK-0140 (P0.54 to
P0.59). The change is small: about 190 lines in the engine and the hosts, 170 in the desktop and 180 in the gates. Two
review agents worked in parallel at `eb8c06f`, each one in its own git worktree: the engine with the hosts, and the
desktop with the gates. Each agent wrote probes that only its worktree holds. I ran the twelve probes of the engine
again and read their output, injected the form of T110 with its control, and read the cited lines of V63 and V64. A
line that says "an agent ran it" means that I read the cited code and did not repeat the run.

## Summary

- **The fixes of this period hold for the forms that they named.** E26, E28 to E32, E35, E37, E38, E102 to E105, V56
  to V62 and T103 to T109 are fixed for their forms, and E18 is fixed by the answer Q4 of 2026-10-04. E33 had only the
  cause of E18, so it is fixed with it. **[Observed]**
- **A condition `#if` hides code from three gates (T110, medium).** The reader of constants, the scanner of codes and
  the check of the tasks parse with no preprocessor symbol, and the build defines `NET`. I added a constant
  `"E-CLI-" + "HIDDEN"` inside `#if NET` to `Engine.Cli`: the diagnostics gate passed; with no `#if` it named
  `E-CLI-HIDDEN`. **[Observed]**
- **The fixes of the replay left their siblings in the open (E106, E107, E109, low).** A cancellation of an open is a
  load failure, its message has "..", and its reader takes the last value of a repeated member. **[Observed]** I ran
  the probes.
- **The add accepts a radius that the solver refuses (E108, low).** A circle of radius 5e-10 is applied and shows as
  not solved. **[Observed]**
- **One comment of P0.56 is false (E110, low).** It says that an open has no cache; an open does not look in the cache,
  and its results go into it. **[Observed]**
- **No high finding is new.** Open findings: 0 critical, 5 high, 17 medium and 31 low. **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 14 | `.csproj` lines in `3DEngine.sln`. The last review gave 14. Git tracks 15 project files: the native package is not in the solution. |
| Production source lines | 20,702 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests` and `3DEngine.ImageTests`, 212 files. The last review gave 20,403 in 212 files. |
| Test source lines | 23,973 | `Engine.Tests` (17,786 in 96 files), `3DEngine.Tests` (2,563 in 17 files) and `3DEngine.ImageTests` (3,624 in 20 files). The last review gave 23,298. |
| Tests | 1,375 | `dotnet test 3DEngine.sln`: 1,006, 225 and 144 passed, 0 failed, 0 skipped. The last review gave 1,318. |
| Gate classes | 19 | Tracked files whose name ends in `GateTests.cs`. `eng/repo-gates` gives 339 runs of 138 checks. The last review gave 19. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 14 | Section "Open" of `docs/register.md`; the limit is 15. This review adds none. The last review gave 14. |
| ADRs | 41 | `docs/adr/0001` to `0041`, with 0003 and 0011 in `docs/adr/archive/`. In force: 39 (18 accepted, 21 amended). None is proposed. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 5 | The same method: V2, V3, T1, T4, T31. The last review gave 7. |
| Findings: medium | 17 | The same method: E5, E6, E12, E13, E19, E21, E22, E25, E27, V4, T2, T5, T10, T17, T32, T37, T110. The last review gave 25. |
| Findings: low | 31 | The same method: E17, E24, E34, E39, E57, E84, E106 to E112, V7, V15, V25, V29, V63, V64, T7, T13, T23, T25, T111 to T116, C1, P1. The last review gave 34. |
| Lines in the rule files | 2,493 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The last review gave 2,457. |
| Sentences with "must" | 58 | `grep -o -w -i must` on the same ten files. The last review gave 57. |
| Sentences with "do not" | 54 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 55. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found. E13, T32 and E84 are fixed in part and count as open.

## Findings of the previous review

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. No change: `Engine.Api.Http/Endpoints/JsonParameters.cs` has no commit since `4f252bf`. Register entry R-0027.
- **E6** — Open. No change: `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` has no commit since `4f252bf`. Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. No change.
- **E8** — Fixed. TASK-0044, v0.37. No change.
- **E9** — Fixed, and it holds. No change.
- **E10** — Fixed. TASK-0035, v0.43. No change.
- **E11** — Fixed. TASK-0115, v0.115. No change.
- **E12** — Open. No ledger entry since v0.130 names it. Register entry R-0027.
- **E13** — Fixed in part. No change.
- **E14** — Fixed. TASK-0115, v0.115. No change.
- **E15** — Fixed. TASK-0044, v0.37. No change.
- **E16** — Fixed. TASK-0115, v0.115. No change.
- **E17** — Open, and smaller. `Engine.Core/CommandBus.cs` changed in two comments only (TASK-0137, TASK-0140). Extrude takes the guard of a repeated identifier (E99), so each sketch command and the extrude refuse a live command that the cache forgot; the other commands do not. Register entry R-0028.
- **E18** — Fixed for the sinks of `Engine.Core` and `Engine.Api.Http`. TASK-0140, v0.141. The gate keeps classes only and reads two assemblies (T112).
- **E19** — Open, and smaller. The entry v0.141 names it as open only. Register entry R-0033.
- **E20** — Fixed, and it holds. No change.
- **E21** — Open, and wider by decision. The entry v0.141 names it as open only. Register entry R-0033.
- **E22** — Open. `Engine.Core/CommandBus.cs` changed in two comments only (TASK-0137, TASK-0140): the bus still stores each result. Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. No change.
- **E24** — Open. No change: `Engine.Api.Http/WebSockets/WireMessage.cs`, `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` have no commit since `4f252bf`. Register entry R-0033.
- **E25** — Open. TASK-0137 left it: the correction needs a decision on the handover of the bus of a replay. No production code calls `Replay.ReplayLog`; the open of a document file (ADR-0015) is the load path. Q1. Register entry R-0036.
- **E26** — Fixed. TASK-0135, v0.136. The view follows the order of the list, and `IList.Clear` throws `NotSupportedException`.
- **E27** — Open. `Engine.Core/Replay.cs` changed for E28 to E31 (TASK-0135 to TASK-0137); the replay still does not check that the backend holds no body, because `IGeometryBackend` has no member for it. Q2. Register entry R-0036.
- **E28** — Fixed. TASK-0136, v0.137.
- **E29** — Fixed for the replay. TASK-0135, v0.136. An open with a cancelled token still gives a load failure (E106).
- **E30** — Fixed. TASK-0136, v0.137.
- **E31** — Fixed for the replay. TASK-0137, v0.138. The message of an open still has ".." (E107), and the new comment of the two backends is false for an open (E110).
- **E32** — Fixed. TASK-0135, v0.136. `GetBodyInfo` and `GetSketch` do not call the backend.
- **E33** — Fixed with E18. TASK-0140, v0.141. A sink must not throw, and a test holds each sink to it, so the ring holds each event that the bus gives, and the `seq` of a reset follows the bus. `Engine.Api.Http/WebSockets/EventBroadcaster.cs` has no commit since `4f252bf`.
- **E34** — Open. `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs` changed for E31 and E38 only (TASK-0137, TASK-0138); the version is still the constant `NativeVersion`, and no code reads a version from the library. Register entry R-0036.
- **E35** — Fixed for the routes of `MapGet`. TASK-0139, v0.140. The gate reads one form of a route (T111), and `EngineHost` still takes the name and the version as two free strings (E111).
- **E36** — Fixed. TASK-0079, v0.85. No change.
- **E37** — Fixed. TASK-0137, v0.138. A bad parameter with no library gives the exit code 2.
- **E38** — Fixed. TASK-0138, v0.139. The list of the platforms is the folder `runtimes` of the package 3.5.2.1.
- **E39** — Open, and wider. `Engine.Cli/Cli.cs` and `Engine.Api.Http.Server/ServerEngine.cs` changed for E37 and E35 only; each holds its own selection of the backend, and `3DEngine/DesktopSurface.cs:109` holds a third. Register entry R-0036.
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
- **E57** — Open. No change: `Engine.Tests/Http/DesktopSurfaceTests.cs` has no commit since `4f252bf`. Register entry R-0039.
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
- **V2** — Open. No change: `3DEngine.Vulkan/OverlayRenderer.cs` has no commit since `4f252bf`. Register entry R-0022.
- **V3** — Open. No change: `3DEngine.Vulkan/OverlayRenderer.cs` has no commit since `4f252bf`. Register entry R-0022.
- **V4** — Open. No ledger entry since v0.130 names it. Register entry R-0027.
- **V5** — Fixed. TASK-0076, v0.74. No change.
- **V6** — Fixed. TASK-0076, v0.74. No change.
- **V7** — Open. No change: `3DEngine.Vulkan/Ui/UiRenderer.cs`, `3DEngine.Vulkan/Ui/UiTexture.cs`, `3DEngine.Vulkan/GpuBuffer.cs` have no commit since `4f252bf`. Register entry R-0030.
- **V8** — Fixed. TASK-0074, v0.75. No change.
- **V9** — Fixed. TASK-0074, v0.75. No change.
- **V10** — Fixed. TASK-0074, v0.75. No change.
- **V11** — Fixed in part. TASK-0074, v0.75. No change.
- **V12** — Fixed, and changed by decision. No change.
- **V13** — Fixed for its parts. No change.
- **V14** — Fixed. TASK-0074, v0.75. No change.
- **V15** — Open by decision, and smaller. `3DEngine/SessionBodies.cs` changed for V55 only (TASK-0124): a creation and its changes in one frame give one mesh. It still waits for the device for each consumed or removed body and for each new geometry key.
- **V16** — Fixed. TASK-0079, v0.85. No change.
- **V17** — Fixed. TASK-0079, v0.85. No change.
- **V18** — Fixed for its forms. TASK-0090, v0.93. No change.
- **V19** — Fixed, with no injection. TASK-0079, v0.85. No change.
- **V20** — Fixed. TASK-0079, v0.85. No change.
- **V21** — Fixed by reading, with no test. TASK-0079, v0.85. No change.
- **V22** — Fixed for its forms. TASK-0079, v0.85. No change.
- **V23** — Fixed. TASK-0079, v0.85. No change.
- **V24** — Fixed. TASK-0079, v0.85. No change.
- **V25** — Open. No change: `3DEngine.Vulkan/3DEngine.Vulkan.csproj` has no commit since `4f252bf`. Register entry R-0020.
- **T1** — Open in one part of three. No ledger entry since v0.130 names it. Register entry R-0023.
- **T2** — Open in one part of five. No ledger entry since v0.130 names it. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. No change.
- **T4** — Open. No change: `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs` has no commit since `4f252bf`. Register entry R-0031.
- **T5** — Open in one part of three. No ledger entry since v0.130 names it. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Open in four parts of five. No ledger entry since v0.130 names it. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. No change.
- **T10** — Open. No ledger entry since v0.130 names it. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51. No change.
- **T12** — Fixed. TASK-0059, v0.55. No change.
- **T13** — Open. No change: `.github/workflows/ci.yml` has no commit since `4f252bf`. Register entry R-0031.
- **T14** — Fixed. TASK-0053, v0.51. No change.
- **T15** — Fixed. TASK-0050, v0.47. No change.
- **T16** — Fixed. TASK-0066, v0.67. No change.
- **T17** — Open. No ledger entry since v0.130 names it. Register entry R-0031.
- **T18** — Fixed. TASK-0049, v0.46. No change.
- **T19** — Fixed. TASK-0049, v0.46. No change.
- **T20** — Fixed. TASK-0049, v0.46. No change.
- **T21** — Fixed. TASK-0049, v0.46. No change.
- **T22** — Fixed. TASK-0049, v0.46. No change.
- **T23** — Open. No ledger entry since v0.130 names it. Register entry R-0031.
- **T24** — Fixed. No change.
- **T25** — Open in part. No ledger entry since v0.130 names it. Register entry R-0031.
- **T26** — Fixed. TASK-0057, v0.54. No change.
- **T27** — Fixed. TASK-0058, v0.56. No change.
- **T28** — Fixed. No change.
- **T29** — Fixed. TASK-0057, v0.54. No change.
- **T30** — Fixed. TASK-0057, v0.54. No change.
- **T31** — Open. No change: `eng/repo-gates/RepoGates.csproj` has no commit since `4f252bf`. Register entry R-0031.
- **T32** — Fixed in part. No change.
- **T33** — Fixed. TASK-0057, v0.54. No change.
- **T34** — Fixed. TASK-0057, v0.54. No change.
- **T35** — Fixed. TASK-0057, v0.54. No change.
- **C1** — Open. No change: `Engine.Contracts/Geometry/IBRepOps.cs`, `Engine.Contracts/Geometry/IFeatureIdMap.cs` have no commit since `4f252bf`. Register entry R-0027.
- **P1** — Open, and smaller. No ledger entry since v0.130 names it. Register entry R-0027.
- **T36** — Fixed. TASK-0061, v0.61. No change.
- **T37** — Open in part. No ledger entry since v0.130 names it. Register entry R-0031.
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
- **V29** — Open. No ledger entry since v0.130 names it.
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
- **T94** — Fixed for the full names. TASK-0114, v0.118. The check binds no implicit global using, so `TaskCreationOptions.LongRunning` with no full name passes in a real gate file (T103).
- **T95** — Fixed for the full names. TASK-0114, v0.118. `new TaskCompletionSource()` with no full name passes in a real gate file (T103).
- **E97** — Fixed for its forms. TASK-0121, v0.123. A radius at the noise level, as 5.6e-17 from a coincident centre and a tangent, still shows as solved (E102).
- **E98** — Fixed. TASK-0123, v0.125.
- **E99** — Fixed. TASK-0123, v0.125.
- **E100** — Fixed for its form. TASK-0123, v0.125. The refusals that TASK-0121 and TASK-0123 added at an add also act on an entry of an open (E103).
- **E101** — Fixed. TASK-0123, v0.125.
- **V51** — Fixed. TASK-0124, v0.126.
- **V52** — Fixed. TASK-0124, v0.126.
- **V53** — Fixed for a started drag. TASK-0124, v0.126. A press on a handle before the slop still lets the wheel and undo act (V58).
- **V54** — Fixed. TASK-0124, v0.126.
- **V55** — Fixed. TASK-0124, v0.126.
- **T96** — Fixed for its forms. TASK-0123, v0.125. A heading with "version", a setext heading and a heading in a quote pass (T106).
- **T97** — Fixed. TASK-0123, v0.125.
- **T98** — Fixed. TASK-0123, v0.125.
- **T99** — Fixed for its four forms. TASK-0123, v0.125. More spellings of a call pass (T109).
- **T100** — Fixed for the full names. TASK-0123, v0.125. A semaphore, a continuation and a cast with no full name pass in a real gate file (T103).
- **T101** — Fixed for the item Compile. TASK-0123, v0.125. An output of a task with the item name Compile passes (T107).
- **T102** — Fixed for its bytes. TASK-0123, v0.125. Other characters that do not show pass (T108).
- **E102** — Fixed for the solver. TASK-0130, v0.132. The add of a circle and of a constraint `radius` still accepts a radius at the noise level (E108).
- **E103** — Fixed. TASK-0132, v0.134. The rule also acts for a replay of plain commands, which a fixture or a script gives (E112).
- **E104** — Fixed. TASK-0132, v0.134.
- **E105** — Fixed for the routes. TASK-0132, v0.134. The reader of a document file takes the last value of a repeated member (E109).
- **V56** — Fixed. TASK-0133, v0.135. The pad cannot give NaN, an infinity or an exponent.
- **V57** — Fixed. TASK-0133, v0.135.
- **V58** — Fixed for the wheel and undo. TASK-0133, v0.135. Escape before the slop does not cancel the press (V64).
- **V59** — Fixed. TASK-0133, v0.135.
- **V60** — Fixed. TASK-0133, v0.135.
- **V61** — Fixed for an end on the ray of the start. TASK-0133, v0.135. An end within a pixel of the start, and not on the ray, still makes a second point (V63).
- **V62** — Fixed. TASK-0133, v0.135.
- **T103** — Fixed for the implicit usings. TASK-0130, v0.132. A condition `#if` hides code from the check (T110).
- **T104** — Fixed for the implicit usings. TASK-0132, v0.134. A condition `#if` hides a code from the reader (T110).
- **T105** — Fixed. TASK-0132, v0.134.
- **T106** — Fixed for its forms. TASK-0132, v0.134. A heading in an ordered list and a setext heading in a quote pass (T114).
- **T107** — Fixed for a literal item name. TASK-0132, v0.134. An item name through a property passes (T115).
- **T108** — Fixed for its characters. TASK-0132, v0.134. Other selectors and fillers pass (T113).
- **T109** — Fixed for its forms. TASK-0132, v0.134. A severity in a `const char` passes (T116).

## 1 Current architecture

**[Observed]** A replay is strict. An exception of an entry is a `ReplayDivergenceException` with the entry, the
command and the exception (E28), a cancellation of the caller stays a cancellation (E29), and the caller names the
backend (E30). No production code calls `Replay.ReplayLog`: the tests and the gates replay, and a host opens a document
file (ADR-0015). The log of the Document is a read-only view (E26).

**[Observed]** A tap of the sketch snaps to the midpoint of a line, then to the nearest point on a line or a curve,
then to the grid, and adds no constraint (R11a, ADR-0041). The Value pad of an angle takes degrees (V56). The command
line checks a name before the backend loads (E37), `/schema/backend` gives the identity that the log records (E35),
and a sink of events must not throw (E18).

## 2 Patterns in use

- **A fix of the replay leaves its sibling in the open.** The replay and the open apply a log in two loops with two
  messages and two readers, so E29, E31 and E105 were fixed in one loop only (E106, E107, E109). **[Observed]**
- **A gate reads the text of a source, not the program that the build makes.** A condition `#if` hides code from three
  gates (T110), and the gate of the routes reads one form of a call (T111). **[Observed]** for T110; by an agent for
  T111.
- **An exact comparison stands for a tolerance.** The add of a circle tests `radius > 0`, and the solver tests a
  radius against 1e-9 (E108); the tool Arc ends the curve within 1e-9 of the radius, not within a pixel (V63).
  **[Observed]**
- **Each correction holds for its forms, and a probe finds the next form.** E106 follows E29, E107 follows E31, E108
  follows E102, E109 follows E105, E112 follows E103, V63 follows V61, V64 follows V58, T110 follows T103 and T104,
  T112 follows E18, T113 follows T108, T114 follows T106, T115 follows T107 and T116 follows T109. **[Observed]** for
  E106 to E109 and T110; by an agent for the others.

## 3 Review of the code

### The engine and the hosts

#### E106 · Low · An open with a cancelled token gives a load failure, not a cancellation

`Engine.Core/Persistence/DocumentFile.cs:210` refuses each result that is not `Applied`, and does not look at the
token. A file with one entry whose handler cancels the source of the caller gives `DocumentFileException:
E-IO-LOAD-FAILED: The entry of version 1 (TestCancelling) gave Cancelled`. No handler of the catalog observes the token
today, so this is latent, as E29 was. **[Observed]** I ran the probe of the agent. **[Recommended]** The guard of
`Replay.cs`: a result Cancelled with a cancelled token throws `OperationCanceledException`.

#### E107 · Low · The message of an open has ".." after an error message that ends with a full stop

`DocumentFile.cs:212-214` puts `{error.Message}` and then ". Each entry". A file that holds one box line two times
gives `… under handle 'e2946ec0-…'.. Each entry was applied one time`. **[Observed]** I ran the probe.
**[Recommended]** One helper for the clause, which the replay and the open share.

#### E108 · Low · The adds accept a radius that the solver says is no radius

`Engine.Core/Commands/AddSketchCircleCommandHandler.cs:73` and `Engine.Core/Sketch/SketchConstraints.cs:87` compare a
radius with 0, and `SketchSolver.CurvesHaveRadius` compares it with `Tolerance`, 1e-9 (E102). A circle of radius 5e-10
is applied and shows as not solved; a constraint `radius` of 5e-10 is applied, and the radius stays 2. This is the
disagreement of the add and the solver that E98 removed for 0. **[Observed]** I ran the probe. **[Recommended]** A live
add of a circle, a live add of a constraint `radius` and a live change of its value refuse a value that is not greater
than `SketchSolver.Tolerance`; under `ApplyingEntry` the rule of E103 stays.

#### E109 · Low · The reader of a document file takes the last value of a repeated member

`Engine.Core/Persistence/EntryCodec.cs:106`, `:120` and `:151` call `JsonDocument.Parse(line)` with the default
options. A box line edited to `"sizeX":1,"sizeX":7` opens, and the command has `sizeX = 7`. The engine writes the file,
so only a person or a damage can give a repeated member; E78 set the rule that the reader refuses what the writer
cannot write. **[Observed]** I ran the probe. **[Recommended]** `JsonDocumentOptions { AllowDuplicateProperties =
false }` in the three calls; the `JsonException` is caught already.

#### E110 · Low · The comment of the two backends says that an open has no cache

`Engine.Core/Geometry/InProcessMeshBackend.cs:24-26` and `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs:110`
say "a replay and an open have no cache". `CommandBus.cs:103-106` says that an open does not look in the cache, and its
result goes into the cache. TASK-0137 wrote the false text. **[Observed]** **[Recommended]** "An open does not look in
the cache (`ApplyEntry`), and a replay has none."

#### E111 · Low · `EngineHost` takes the name and the version of the backend as two free strings

`Engine.Api.Http/EngineHost.cs:33`. The two hosts give the identity today (E35, `3DEngine/DesktopSurface.cs:117`), but
a new host can give a `/schema/backend` that is not the identity of the log; many tests give `"managed", "test"`, and
the managed backend records `("managed", "1")`. **[Observed]** by an agent; I read the lines. **[Recommended]**
`EngineHost` reads `backend.Identity`, and the two parameters go.

#### E112 · Low · The rule of E103 also acts for a replay of plain commands

`CommandBus.cs:133` marks an entry for each bus with no cache, and `Replay.ReplayLog(IEnumerable<Command>)` is for a
fixture or a script (`Replay.cs:23-24`). So a script replays an arc with its start on its centre, which a live session
refuses. Only one test reaches the path. **[Observed]** by an agent. **[Recommended]** Say it in the comment of
`Replay`, or mark an entry only for the log entries.

### The render side and the desktop

#### V63 · Low · An end of an arc within a pixel of the start makes a second point

`3DEngine.Core/Sketching/SketchTool.cs:318` ends the curve when the new end is within `1e-9 * radius` of the start. At
8 pixels for each millimetre, the centre (0, 0), the start (1000, 1) and a tap at (1003, 1) add a point 0.003 mm from
the start and an arc of nearly a full turn. **[Observed]** by an agent; I read the line. **[Recommended]** End the curve
when the new end is within `HitPixels` of the start, in pixels.

#### V64 · Low · Escape before the slop does not cancel a press on a handle

`3DEngine.Core/Input/IntentMachine.cs:340` cancels a press with `Started: true` only. A press on a handle, Escape, a
wheel step and a motion past the slop give `GizmoStart, GizmoMove, GizmoEnd`. The comment of `CancelGizmo` says "the
rest of the press gives nothing". **[Observed]** by an agent; I read the lines. **[Recommended]** `CancelGizmo` marks a
press that has not started as cancelled, and gives `GizmoCancelIntent` only for a drag that started.

### Tests, gates and the pipeline

#### T110 · Medium · A condition `#if` hides code from three gates · Confirmed by a run

`Engine.Tests/Governance/ConstantStrings.cs:45`, `Engine.Tests/Diagnostics/DiagnosticsScanner.cs:101` and
`eng/repo-gates/DiscardedTasks.cs:46` parse with no preprocessor symbol, and the build defines `NET`. Code inside
`#if NET` is disabled text for the gate and code for the build. `DeterminismCallGateTests` refuses disabled text, for
the three projects of the log only. I added `#if NET` `internal const string Code = "E-CLI-" + "HIDDEN";` `#endif` to
`Engine.Cli`: `DiagnosticsRegistryGateTests` passed (29 of 29); with no condition two tests failed and named
`E-CLI-HIDDEN`. An agent did the same with `Task.Run` in a gate file and the check of the tasks. **[Observed]**
**[Recommended]** Refuse disabled text in each file that the three gates read, as `DeterminismCallGateTests` does.

#### T111 · Low · The gate of the routes reads one form of a call

`Engine.Tests/Http/SchemaEndpointGateTests.cs:153` matches `app.MapGet("/schema…"` only. `app.MapMethods`, `MapGroup`,
`"/Schema/…"` and a space after the bracket pass, and the routing of ASP.NET does not regard case. The live
`EndpointDataSource` of the test host gives the seven routes of ADR-0008 today. **[Observed]** by two agents.
**[Recommended]** Read the routes from the `EndpointDataSource` of the test host, and compare them with no regard to
case.

#### T112 · Low · The gate of the sinks keeps classes only, and reads two assemblies

`Engine.Tests/EventSinkContractTests.cs:19-21`. A struct that implements `IEventSink` and throws passes, and
`DocumentSession` takes a sink from each host, so a sink in `3DEngine`, `Engine.Cli` or `Engine.Api.Http.Server`
escapes. No such sink exists today. **[Observed]** by an agent. **[Recommended]** Each type that is not abstract and not
an interface, in each assembly of the solution that the test project references.

#### T113 · Low · More characters that do not show pass the control byte gate

`Engine.Tests/Governance/DependencyDirectionGateTests.cs:252` refuses FE00 to FE0F only. U+E0100, U+180B, U+034F and
U+2800 pass. **[Observed]** by an agent; no tracked file holds one. **[Recommended]** Add E0100 to E01EF and 180B to
180F; U+034F and U+2800 are characters that a text can use, so keep them.

#### T114 · Low · Two forms of a heading of a version pass the counter of the review

`Engine.Tests/Governance/CodebaseReviewGateTests.cs:370`. `1. ## v0.11 …`, `1) ## v0.11 …` and a setext heading in a
quote give no problem. **[Observed]** by an agent. **[Recommended]** Add an ordered list marker to the class of markers,
and remove a leading `>` before the check of a setext underline.

#### T115 · Low · An item name `Compile` through a property passes the check of T107

`DependencyDirectionGateTests.cs:504` compares the literal value of `ItemName`. `ItemName="$(I)"` with `<I>Compile</I>`
gives no offence. **[Observed]** by an agent. **[Recommended]** Refuse an `ItemName` that holds `$(` or `@(`, and trim
the value.

#### T116 · Low · A severity in a `const char` passes the scanner of parts of codes

`Engine.Tests/Diagnostics/DiagnosticsScanner.cs:112` accepts a character literal only. `const char S = 'E';` and
`S + "-x" + Tag` give no code and no part. **[Observed]** by an agent. **[Recommended]** Take the constant value of the
left operand from the semantic model.

### Correct

**[Observed]** by the agents, with the probes that passed:

- A cancellation that a handler makes on its own token, with the token of the caller not cancelled, is a divergence
  with Cancelled. `Clause` of a message of dots and spaces does not throw. The view of the log follows the list.
- A bad type and a missing parameter give the exit code 2 with no library; the command line and the session take one
  catalog. Only `GetBoundingBox` and `GetTessellation` call the backend.
- The snap at a line of length zero takes the existing point; two midpoints at one distance give the later line, in
  the same way each time; a curve with a radius that is not finite is skipped.
- `CosineAndSine` of −0, 360, −360, −90 and 1e308 gives a unit pair, and the pad cannot give NaN or an exponent.
- A ledger heading with a tab, a zero-width space or an indent of four fails the counter.
- The only new call of a function of `Math` in the diff is `Math.Sqrt`. Serial commit and event order do not change.

**Not verified.** E106 and E112 against a real file of v0.117 to v0.124; the open of an old file with a radius at the
noise level after E102; the routes of the two hosts; a probe on Linux or macOS.

## 4 The next planned task

R11b (TASK-0134), the mark of the snap, is done on its branch, and its pipeline is green; it waits for the visual check
of the owner. It is the next product phase. `docs/templates.md`, section 7, puts the medium and low findings of this
review after it: P0.60 (TASK-0142) holds those of the engine and the hosts, P0.61 (TASK-0143) those of the gates, and
P0.62 (TASK-0144) those of the desktop. No high finding is new, so no task comes before R11b.

After R11b the roadmap has no product phase that the owner chose. The rest of R11 needs the identity of a face and of
an edge (K4). Question Q3 asks the owner.

## 5 Research for that task

**[Observed]** I read the cited code.

- **The open and the replay apply a log in two loops.** `DocumentFile.cs:200-216` and `Replay.cs:67-91` each have a
  catch, a check of the status and a message. One helper for the clause closes E107; the guard of the token in the
  open closes E106.
- **The reader of the file is the only parser of the engine with default options.** The surface passes
  `AllowDuplicateProperties = false` since TASK-0132; `EntryCodec` has three calls.
- **The disabled text is a kind of trivia.** `DeterminismCallGateTests` finds `DisabledTextTrivia` in the three
  projects of the log; the three gates of T110 can use the same test.
- **The test host has the routes.** `WebApplicationFactory<Program>` gives an `EndpointDataSource`, and each
  `RouteEndpoint` has its `RoutePattern.RawText`.

## 6 Recommended approach

**[Recommended]** In this order, after R11b:

1. P0.60 (TASK-0142): E106 to E112, and the records of Q1 A and Q2 A in register entry R-0036. 2 to 3 agent hours.
2. P0.61 (TASK-0143): T110 to T116, each with an injection before the change. 2 to 3 agent hours.
3. P0.62 (TASK-0144): V63 and V64, with tests of the tool and of the intent machine. 1 agent hour.

## Questions for the owner

**Q1. A session on a replayed Document (E25).**
- A: The owner accepts E25 with this reason: no production code calls `Replay.ReplayLog`, and a host continues a
  document through an open of its file (ADR-0015), which applies each entry on the bus of the session.
- B: The replay gives its bus and its next sequence number to the caller, and a test builds a session on the
  replayed Document.

**Recommended: A.** B adds a path that no host needs, and an open already gives a live document. The standing answer of
the owner of 2026-10-31 takes A.

**Q2. A backend that holds bodies at the start of a replay (E27).**
- A: The owner accepts E27 with this reason: ADR-0012 §2 says that a replay uses a fresh backend, and no production code
  replays. The comment of the parameter `backend` says it.
- B: `IGeometryBackend` gains a member that tells the count of its bodies, and the replay refuses a backend that holds
  one. This changes `Engine.Contracts` and needs an ADR.

**Recommended: A.** B changes the contract of each backend for a caller that does not exist. The standing answer of the
owner of 2026-10-31 takes A.

**Q3. The product phase after R11b.**
- A: The owner chooses it. Until then the session takes no new product phase, and it does the tasks of this review.
- B: K1a, an exact sign of the orientation test of `SketchProfile`. It changes the answer for a profile near a
  collinear case, so it needs an ADR on the files that an earlier engine wrote.
- C: V0, the verification spike of a vision library on the three platforms.

**Recommended: A.** The choice of the next product track is a direction of the project, and the roadmap is a menu. The
standing answer of the owner of 2026-10-31 takes A.
