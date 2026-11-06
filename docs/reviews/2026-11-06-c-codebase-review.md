---
date: 2026-11-06
commit: df11d6e
ledger: v0.214
previous: 2026-11-06-b-codebase-review.md
---

# Codebase review — 2026-11-06 (c)

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the twenty-fifth codebase review. The review of 2026-11-06 (b) examined v0.207. After it came 4 product
milestones (R14e2, R14e3, R14f1 and R14f2) and 7 ledger entries (v0.208 to v0.214). The next product milestone, R14f3,
would be the fifth, so this review is due before it. The gate does not count these milestones (T147); the count here is
by hand, and `docs/templates.md`, section 7, gives the form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is the
authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this file does not.

| Item | Value |
|---|---|
| Code state | `df11d6e`, the close of R14f2 on its branch; the merge of v0.214 holds its tree |
| Branch of this review | `codebase-review-2026-11-06-c` |
| Tests | 1,895 on this computer (1,341 in `Engine.Tests`, 333 in `3DEngine.Tests`, 218 in `3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`) |
| Pipeline | The run of the branch `r14f1-apk-job` (42157390245): green on Ubuntu, Windows and macOS, the three determinism hashes are equal, and the APK builds |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner decides.

**Method.** Seven tasks changed code or records after `41db916`, the commit of the last review: TASK-0223 (the review),
TASK-0226 (R14e2), TASK-0224 (P0.89), TASK-0225 (P0.90), TASK-0227 (R14e3), TASK-0228 (R14f1) and TASK-0229 (R14f2).
Two review agents worked in parallel at `df11d6e`, each one in its own git worktree: the engine with the gates and the
pipeline, and the desktop with the application and the program of the phone. Each agent wrote probes and injections
that only its worktree held, and removed them. I read the cited lines of the medium findings T147, T149 and V97, and I
measured the values below. The reviews of Codex of v0.206 to v0.211 give four leads, and each one is a finding here:
T147, T148, E156 and V97.

## Summary

- **The fixes of the last review hold.** E152 to E154, V92 to V95, T145 and T146 are fixed. E155 and V91 are fixed for
  their forms, and a probe found the next form of each (E156, V97). V88 is fixed for the phone. **[Observed]**
- **No new finding is critical or high.** **[Observed]**
- **Three new medium findings.** The review gate counts no product phase with a letter and then a digit, so it counted
  none of R14e1 to R14f2 (T147). The smoke test of the emulator passes when the session of the phone ends with an
  exception after its start (T149). The start again after a next session that does not start chooses the previous
  document before the surface stops, so a command of the stop leaves the next session empty (V97). **[Observed]** by
  probes.
- **Eleven new low findings:** E156 and E157 in the command line and the host; V98 to V103 in the panel, the phone and
  the texts; T148, T150 and T151 in the gates, the check of the APK and the ledger. **[Observed]**
- **Open findings: 0 critical, 4 high, 17 medium and 30 low.** **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 17 | `.csproj` lines in `3DEngine.sln`. The last review gave 17; `3DEngine.Android` is outside the solution. |
| Production source lines | 29,134 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests`, `3DEngine.ImageTests` and `3DEngine.VisionTests`, 276 files. The last review gave 28,741 in 272 files. |
| Test source lines | 34,432 | `Engine.Tests` (24,912 in 116 files), `3DEngine.Tests` (3,576 in 22 files), `3DEngine.ImageTests` (5,879 in 37 files) and `3DEngine.VisionTests` (65 in 1 file). The last review gave 33,941. |
| Tests | 1,895 | `dotnet test 3DEngine.sln`: 1,341, 333, 218 and 3 passed, 0 failed, 0 skipped. The last review gave 1,863. |
| Gate classes | 19 | Tracked files whose name ends in `GateTests.cs`. `eng/repo-gates` gives 450 runs of 157 checks. The last review gave 19, and 443 runs of 155 checks. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 15 | Section "Open" of `docs/register.md`; the limit is 15. The last review gave 15. |
| ADRs | 65 | `docs/adr/0001` to `0061` and `0070` to `0073`, with 0003 and 0011 in `docs/adr/archive/`. In force: 63 (28 accepted, 35 amended). None is proposed. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 4 | The same method: V2, V3, T4, T31. The last review gave 4. |
| Findings: medium | 17 | The same method: E5, E6, E12, E13, E19, E21, E22, V4, V97, T2, T5, T10, T17, T32, T37, T147, T149. The last review gave 17; E152, V91 and T145 are fixed. |
| Findings: low | 30 | The same method: E17, E24, E34, E39, E57, E84, E156, E157, V7, V15, V29, V84, V88, V96, V98 to V103, T7, T13, T23, T25, T143, T148, T150, T151, C1, P1. The last review gave 27; E153 to E155, V92 to V95 and T146 are fixed. |
| Lines in the rule files | 2,881 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The last review gave 2,863. |
| Sentences with "must" | 59 | `grep -o -w -i must` on the same ten files. The last review gave 59. |
| Sentences with "do not" | 54 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 54. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found. E13, T32, E84, V84 and V96 are fixed in part and count as open. A finding that the owner accepted counts as closed.

## Findings of the previous review

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. No change: `Engine.Api.Http/Endpoints/JsonParameters.cs` has no commit since `ab55b07`. Register entry R-0027.
- **E6** — Open. No change: `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` has no commit since `ab55b07`. Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. No change.
- **E8** — Fixed. TASK-0044, v0.37. No change.
- **E9** — Fixed, and it holds. No change.
- **E10** — Fixed. TASK-0035, v0.43. No change.
- **E11** — Fixed. TASK-0115, v0.115. No change.
- **E12** — Open, and smaller. No change: `Engine.Core/Hosting/ParameterBinder.cs` has no commit since `ab55b07`. Register entry R-0027.
- **E13** — Fixed in part. No change: `Engine.Api.Http/Endpoints/QueriesEndpoint.cs` has no commit since `41db916`; the change of `Engine.Cli/Cli.cs` is the refusal of an assembly (E155).
- **E14** — Fixed. TASK-0115, v0.115. No change.
- **E15** — Fixed. TASK-0044, v0.37. No change.
- **E16** — Fixed. TASK-0115, v0.115. No change.
- **E17** — Open, and smaller. No change: `Engine.Core/CommandBus.cs` has no commit since `ab55b07`. Register entry R-0028.
- **E18** — Fixed for the sinks of `Engine.*` (TASK-0140, v0.141) and of the desktop host (T118, TASK-0150, v0.154). TASK-0140, v0.141. No change.
- **E19** — Open, and smaller. `Engine.Core/CommandBus.cs` and `Engine.Core/DocumentSession.cs` have no commit since `41db916`; `EngineHost.cs` changed only for the unsaved folder of V91. Register entry R-0033.
- **E20** — Fixed, and it holds. No change.
- **E21** — Open. No change: `Engine.Core/Commands/SubtractCommandHandler.cs` has no commit since `ab55b07`. Register entry R-0033.
- **E22** — Open. No change: `Engine.Core/CommandBus.cs` has no commit since `ab55b07`. Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. No change.
- **E24** — Open in seven parts of nine. No change: `Engine.Api.Http/WebSockets/WireMessage.cs` and `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` have no commit since `41db916`. Register entry R-0033.
- **E25** — Accepted by the owner. TASK-0142, v0.144. No change.
- **E26** — Fixed. TASK-0135, v0.136. No change.
- **E27** — Accepted by the owner. TASK-0142, v0.144. No change.
- **E28** — Fixed. TASK-0136, v0.137. No change.
- **E29** — Fixed for the replay. TASK-0135, v0.136. No change.
- **E30** — Fixed. TASK-0136, v0.137. No change.
- **E31** — Fixed for the replay. TASK-0137, v0.138. No change.
- **E32** — Fixed. TASK-0135, v0.136. No change.
- **E33** — Fixed with E18. TASK-0140, v0.141. No change.
- **E34** — Open. `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs` changed only for the platforms of the package 3.5.2.2 (TASK-0226). Register entry R-0036.
- **E35** — Fixed for the routes of `MapGet`. TASK-0139, v0.140. No change.
- **E36** — Fixed. TASK-0079, v0.85. No change.
- **E37** — Fixed. TASK-0137, v0.138. No change.
- **E38** — Fixed. TASK-0138, v0.139. No change.
- **E39** — Open. No change of the three choices of the backend; `3DEngine.Android/PhoneHost.cs` adds a fourth, with the same check of the native library. Register entry R-0036.
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
- **E57** — Open. No change: `Engine.Tests/Http/DesktopSurfaceTests.cs` has no commit since `ab55b07`. Register entry R-0039.
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
- **V2** — Open. No change: `3DEngine.Vulkan/Swapchain.cs`, `3DEngine.Vulkan/OverlayRenderer.cs` have no commit since `ab55b07`. Register entry R-0022.
- **V3** — Open. No change: `3DEngine.Vulkan/GraphicsDevice.cs` has no commit since `ab55b07`. Register entry R-0022.
- **V4** — Open. No ledger entry from v0.208 to v0.214 names it. Register entry R-0027.
- **V5** — Fixed. TASK-0076, v0.74. No change.
- **V6** — Fixed. TASK-0076, v0.74. No change.
- **V7** — Open. No change: `3DEngine.Vulkan/Ui/UiRenderer.cs`, `3DEngine.Vulkan/Ui/UiTexture.cs`, `3DEngine.Vulkan/GpuBuffer.cs` have no commit since `ab55b07`. Register entry R-0030.
- **V8** — Fixed. TASK-0074, v0.75. No change.
- **V9** — Fixed. TASK-0074, v0.75. No change.
- **V10** — Fixed. TASK-0074, v0.75. No change.
- **V11** — Fixed in part. TASK-0074, v0.75. No change.
- **V12** — Fixed, and changed by decision. No change.
- **V13** — Fixed for its parts. No change.
- **V14** — Fixed. TASK-0074, v0.75. No change.
- **V15** — Open. No change: `3DEngine.App/SessionBodies.cs` has no commit since `ab55b07`.
- **V16** — Fixed. TASK-0079, v0.85. No change.
- **V17** — Fixed. TASK-0079, v0.85. No change.
- **V18** — Fixed for its forms. TASK-0090, v0.93. No change.
- **V19** — Fixed, with no injection. TASK-0079, v0.85. No change.
- **V20** — Fixed. TASK-0079, v0.85. No change.
- **V21** — Fixed by reading, with no test. TASK-0079, v0.85. No change.
- **V22** — Fixed for its forms. TASK-0079, v0.85. No change.
- **V23** — Fixed. TASK-0079, v0.85. No change.
- **V24** — Fixed. TASK-0079, v0.85. No change.
- **V25** — Fixed for its parts. TASK-0195, v0.197. No change.
- **T1** — Fixed. TASK-0156, v0.158. No change.
- **T2** — Open in one part of five. No ledger entry from v0.208 to v0.214 names it. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. No change.
- **T4** — Open. No change: `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs` has no commit since `ab55b07`. Register entry R-0031.
- **T5** — Open in one part of three. No ledger entry from v0.208 to v0.214 names it. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Open in four parts of five. No ledger entry from v0.208 to v0.214 names it. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. No change.
- **T10** — Open. No ledger entry from v0.208 to v0.214 names it. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51. No change.
- **T12** — Fixed. TASK-0059, v0.55. No change.
- **T13** — Open in part. `.github/workflows/ci.yml` changed only for the two jobs of Android (TASK-0228, TASK-0229). Register entry R-0031.
- **T14** — Fixed. TASK-0053, v0.51. No change.
- **T15** — Fixed. TASK-0050, v0.47. No change.
- **T16** — Fixed. TASK-0066, v0.67. No change.
- **T17** — Open. `Engine.Tests/Governance/NativePackageGateTests.cs` changed for the second package only; `nuget.config` has no commit since `41db916`. Register entry R-0031.
- **T18** — Fixed. TASK-0049, v0.46. No change.
- **T19** — Fixed. TASK-0049, v0.46. No change.
- **T20** — Fixed. TASK-0049, v0.46. No change.
- **T21** — Fixed. TASK-0049, v0.46. No change.
- **T22** — Fixed. TASK-0049, v0.46. No change.
- **T23** — Open. No change: `eng/manifold-native/build.ps1` has no commit since `ab55b07`. Register entry R-0031.
- **T24** — Fixed. No change.
- **T25** — Open in part. No change: `eng/write-set-check` has no commit since `41db916`. Register entry R-0031.
- **T26** — Fixed. TASK-0057, v0.54. No change.
- **T27** — Fixed. TASK-0058, v0.56. No change.
- **T28** — Fixed. No change.
- **T29** — Fixed. TASK-0057, v0.54. No change.
- **T30** — Fixed. TASK-0057, v0.54. No change.
- **T31** — Open. No change: `eng/repo-gates/RepoGates.csproj` has no commit since `ab55b07`. Register entry R-0031.
- **T32** — Fixed in part. No change: `eng/write-set-check/Program.cs` has no commit since `ab55b07`.
- **T33** — Fixed. TASK-0057, v0.54. No change.
- **T34** — Fixed. TASK-0057, v0.54. No change.
- **T35** — Fixed. TASK-0057, v0.54. No change.
- **C1** — Open. No change: `Engine.Contracts/Geometry/IBRepOps.cs` and `IFeatureIdMap.cs` have no commit since `41db916`. Register entry R-0027.
- **P1** — Open, and smaller. `3DEngine.App/NativeThreeDEngine.cs` changed only for the action of the cut (TASK-0227). Register entry R-0027.
- **T36** — Fixed. TASK-0061, v0.61. No change.
- **T37** — Open in part. No change: `Engine.Tests/Governance/WriteSetRangeTests.cs` has no commit since `ab55b07`. Register entry R-0031.
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
- **V29** — Open. No check by touch from v0.208 to v0.214, and the register has no entry for it. The test sheet of the phone (TASK-0227) holds the first one.
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
- **E84** — Fixed in part. No change: `Engine.Core/Persistence/DocumentJournal.cs` has no commit since `ab55b07`.
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
- **E115** — Fixed. TASK-0149, v0.153. No change.
- **E116** — Fixed. TASK-0149, v0.153. No change.
- **E117** — Fixed. TASK-0149, v0.153. No change.
- **E118** — Fixed. TASK-0149, v0.153. No change.
- **V65** — Fixed. TASK-0151, v0.155. No change.
- **V66** — Fixed. TASK-0151, v0.155. No change.
- **V67** — Fixed. TASK-0151, v0.155. No change.
- **T117** — Fixed for `PackageReference`. TASK-0150, v0.154. No change.
- **T118** — Fixed. TASK-0150, v0.154. No change.
- **T119** — Fixed. TASK-0150, v0.154. No change.
- **T120** — Fixed for a `const char` that is an operand. TASK-0150, v0.154. No change.
- **T121** — Fixed. TASK-0150, v0.154. No change.
- **T122** — Fixed for one line above the underline. TASK-0150, v0.154. No change.
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
- **E125** — Fixed for its forms. TASK-0169, v0.171. No change.
- **E126** — Fixed. TASK-0169, v0.171. No change.
- **E127** — Fixed. TASK-0169, v0.171. No change.
- **V74** — Fixed. TASK-0170, v0.172. No change.
- **V75** — Fixed for its form. TASK-0170, v0.172. No change.
- **T127** — Fixed for its forms. TASK-0170, v0.172. No change.
- **T128** — Fixed for its forms. TASK-0170, v0.172. No change.
- **T129** — Fixed for its forms. TASK-0170, v0.172. No change.
- **E128** — Fixed for its forms. TASK-0176, v0.178. No change.
- **E129** — Fixed, and no test reaches the fix. TASK-0176, v0.178. No change.
- **E130** — Fixed for its form. TASK-0177, v0.181. No change.
- **V76** — Fixed. TASK-0177, v0.181. No change.
- **V77** — Fixed for its forms. TASK-0177, v0.181. No change.
- **T130** — Fixed for its forms. TASK-0177, v0.181. No change.
- **T131** — Fixed for its forms. TASK-0177, v0.181. No change.
- **T132** — Fixed for its forms. TASK-0177, v0.181. No change.
- **E131** — Fixed for its forms. TASK-0182, v0.184. No change.
- **E132** — Fixed. TASK-0182, v0.184. No change.
- **E133** — Fixed. TASK-0182, v0.184. No change.
- **V78** — Fixed for its forms. TASK-0181, v0.183. No change.
- **V79** — Fixed. TASK-0181, v0.183. No change.
- **V80** — Fixed. TASK-0181, v0.183. No change.
- **V81** — Fixed. TASK-0181, v0.183. No change.
- **V82** — Fixed. TASK-0181, v0.183. No change.
- **V83** — Fixed. TASK-0181, v0.183. No change.
- **V84** — Fixed in part. No change: `3DEngine.App/FaceArm.cs` has no commit since `41db916`.
- **T133** — Fixed for its forms. TASK-0182, v0.184. No change.
- **T134** — Fixed for its forms. TASK-0182, v0.184. No change.
- **T135** — Fixed for its forms. TASK-0182, v0.184. No change.
- **E134** — Fixed for its forms. TASK-0190, v0.192. No change.
- **E135** — Fixed. TASK-0192, v0.195. No change.
- **E136** — Fixed. TASK-0192, v0.195. No change.
- **E137** — Fixed for its form. TASK-0192, v0.195. No change.
- **E138** — Fixed for its form. TASK-0192, v0.195. No change.
- **E139** — Fixed for its form. TASK-0192, v0.195. No change.
- **E140** — Fixed. TASK-0192, v0.195. No change.
- **E141** — Fixed. TASK-0192, v0.195. No change.
- **V85** — Fixed for its form on Windows. TASK-0191, v0.194. No change.
- **V86** — Fixed. TASK-0191, v0.194. No change.
- **V87** — Fixed. TASK-0191, v0.194. No change.
- **T136** — Fixed for its form on the side of the engine. TASK-0192, v0.195. No change.
- **T137** — Fixed for its six forms. TASK-0192, v0.195. No change.
- **T138** — Fixed for its forms. TASK-0192, v0.195. No change.
- **T139** — Fixed for its form. TASK-0192, v0.195. No change.
- **T140** — Fixed. TASK-0192, v0.195. No change.
- **E142** — Fixed. TASK-0197, v0.204. No change.
- **E143** — Fixed. TASK-0197, v0.204. No change.
- **E146** — Fixed. TASK-0197, v0.204. No change.
- **E147** — Fixed. TASK-0197, v0.204. No change.
- **E144** — Fixed. TASK-0197, v0.204. No change.
- **E145** — Fixed. TASK-0197, v0.204. No change.
- **E148** — Fixed. TASK-0197, v0.204. No change.
- **E149** — Fixed. TASK-0197, v0.204. No change.
- **E150** — Fixed. TASK-0197, v0.204. No change.
- **E151** — Fixed. TASK-0197, v0.204. No change.
- **V88** — Open for the desktop, and fixed for the phone. The APK holds the ABIs `arm64-v8a` and `x86_64` only, and `eng/android/check-apk.sh` refuses another ABI (TASK-0228); the build output of the desktop still holds native files of `win-x86`, `linux-x86` and `android-x86`.
- **V89** — Fixed. TASK-0198, v0.206. No change.
- **V90** — Fixed. TASK-0198, v0.206. No change.
- **T141** — Fixed. TASK-0198, v0.206. No change.
- **T142** — Fixed. TASK-0198, v0.206. No change.
- **T143** — Open. No change; it waits for the owner (Q4 of the review of 2026-11-06).
- **T144** — Fixed. TASK-0198, v0.206. No change.
- **E152** — Fixed. TASK-0224, v0.210: `Crossings` skips a face whose plane holds the point and that does not hold it; an injection fails the test. **[Observed]**
- **E153** — Fixed. TASK-0224, v0.210: `Replay.ReplayLog` takes the kind. **[Observed]**
- **E154** — Fixed. TASK-0224, v0.210 (Q1 A): the header of an assembly has the version 2. **[Observed]**
- **E155** — Fixed for its form. TASK-0224, v0.210. The next form is E156. **[Observed]**
- **V91** — Fixed for its form. TASK-0225, v0.211 (Q2 A). The next form is V97. **[Observed]**
- **V92** — Fixed. TASK-0225, v0.211: `ExitWindowFailed` (7). **[Observed]**
- **V93** — Fixed. TASK-0225, v0.211: the flush runs after `Initialize` and `LoadScene`; no test holds the order. **[Observed]**
- **V94** — Fixed. TASK-0225, v0.211: a test with the queue of SDL; an injection fails it. **[Observed]**
- **V95** — Fixed as a text. TASK-0225, v0.211: ADR-0072 item 3 names the two actions. **[Observed]**
- **V96** — Fixed in part. TASK-0225, v0.211; the corrected count is false again (V103). **[Observed]**
- **T145** — Fixed. TASK-0224, v0.210: the rules read the frameworks and the SDK of each reached project; two injections fail them. **[Observed]**
- **T146** — Fixed. TASK-0224, v0.210. **[Observed]**

## 1 Current architecture

**[Observed]** The program of the phone, `3DEngine.Android`, composes `LocalSessionHost` of the library `3DEngine.App`
on the native backend, with no surface and no file, and draws it in one window (R14e2). It keeps its session through the
background, with a new engine at each return to the foreground, and stays in portrait (R14e3, ADR-0073). A person cuts a
body in the panel with the list "Cut with", on the desktop and on the phone. The pipeline builds the APK at each push,
checks its ABIs, its native libraries and its assemblies, and starts it on the x86-64 emulator (R14f1, R14f2).
`docs/android-test-sheet.md` holds the test of the owner on the S23 Ultra.

**[Observed]** An assembly file has the header version 2 (P0.89). The export of the command line refuses an assembly.
The desktop starts the previous document again, with its kind, after a next session that does not start (P0.90).

## 2 Patterns in use

- **A fix of a path adds a step outside the handler of that path.** The read of the kind of E155 is outside the
  handler of the export (E156); the choice of the previous document of V91 comes before the stop of the surface (V97).
  **[Observed]**
- **A counter of the gates reads a form of a name that the names outgrew.** The review gate reads `R14`, `R14e`, but
  not `R14e1` (T147). **[Observed]**
- **A check that reports a fact as a warning passes the case that it exists for.** The smoke test gives a warning when
  no frame comes, also after an exception (T149); the check of the APK takes a checksum from any row (T150).
  **[Observed]**
- **A text of a change describes the plan, not the run.** The background of ADR-0073 item 3 follows the order of the
  events of SDL that a blocked thread does not see (V101), and two counts are false (V103, T151). **[Inferred]**

## 3 Review of the code

### The engine and the hosts

#### E156 · Low · engine export calls DocumentFile.ReadKind outside its handler, so a document path with a NUL character or a device name throws out of Cli.Run · Confirmed

- **[Observed]** `Engine.Cli/Cli.cs:215` to `224` (the change of E155) calls `DocumentFile.ReadKind(document)` before the
  `try` of the read, and catches `DocumentFileException` only. `DocumentFile.Lines` (`Engine.Core/Persistence/DocumentFile.cs:262`
  to `285`) converts only `IOException` and `UnauthorizedAccessException` into a `DocumentFileException`. So an
  `ArgumentException` or a `NotSupportedException` of the `FileStream` leaves `Cli.Export` and `Cli.Run`.
- **[Observed]** At `41db916` the first read of the document file was `DocumentFile.Read` inside the `try` at
  `Cli.cs:242`, whose `catch` at `Cli.cs:257` takes `ArgumentException` and `NotSupportedException` and gives
  `E-IO-LOAD-FAILED` (`git show 41db916:Engine.Cli/Cli.cs`). So this is a regression of TASK-0224.
- **[Observed]** Probe `Probe_Cli_Export_Nul_Path`: `Cli.Run(["export", "step", "a\0b.3de", <temp>/rev25.step])` threw
  `System.ArgumentException: Null character in path.`, with the stack `FileStream..ctor` < `DocumentFile.Lines` <
  `DocumentFile.ReadKind` < `Cli.Export` (`Cli.cs:220`) < `Cli.Run`. No JSON came on the standard output.
- **[Observed]** Probe `Probe_Cli_Export_Other_Paths` on Windows: the document path `CON` threw
  `System.NotSupportedException: Stream does not support seeking.` out of `Cli.Run`. The paths `a:b:c.3de`, `\\?\`,
  `\\.\pipe\rev25-none` and a path of 40,000 characters each gave exit 1 and `E-IO-LOAD-FAILED`, which is correct.
- **[Inferred]** An agent that gives such a path gets an unhandled exception and the exit code of the runtime, not the
  JSON error and exit 1 of ADR-0008. `DocumentFile.Check` (`DocumentFile.cs:86` to `107`) also catches
  `DocumentFileException` only, so the same exceptions can leave it; the desktop calls it (`3DEngine/DesktopDocument.cs:57`),
  and the desktop review must judge if a person can give such a path there.
- **[Recommended]** Make `DocumentFile.Lines` convert `ArgumentException` and `NotSupportedException` into
  `DocumentFileException.Load`, so that each caller of `ReadKind`, `ReadLengthUnit` and `Check` gets the code. Add a
  theory to the export tests of the command line over `"a\0b.3de"` and, on Windows, `"CON"`: exit 1 and `E-IO-LOAD-FAILED`.

#### E157 · Low · EngineHost.FileName compares the unsaved folder with a trailing separator, so a document of the unsaved folder shows the name of its journal · Confirmed

- **[Observed]** `Engine.Api.Http/EngineHost.cs:45` keeps `_unsavedFolder = Path.GetFullPath(unsaved)`, which keeps a
  trailing separator. `FileName` (`EngineHost.cs:98` to `101`) compares it, with `StringComparison.Ordinal`, with
  `Path.GetDirectoryName(Path.GetFullPath(path))`, which has no trailing separator. The change of V91 (TASK-0225) now
  applies the same comparison when the host gives a path too.
- **[Observed]** Probe `Probe_Unsaved_Folder_With_A_Trailing_Separator`: `new EngineHost(new InProcessMeshBackend(),
  new DocumentFileOptions(null, folder + "\\"))` and one `NoOp` gave the journal
  `<folder>\9aa5a3d3-....3de` and `FileName` = `9aa5a3d3-7978-48bd-9820-b1caac30e866.3de`. The same folder with no
  trailing separator gave `FileName` = null.
- **[Inferred]** The desktop reads the folder from the setting `Desktop:UnsavedFolder` (`3DEngine/DesktopSession.cs:51`).
  A value that ends with a separator makes each unsaved document a named document for `FileName` (ADR-0030 item 4), so
  the title and the save treat the journal as a named file. On Windows a difference of case gives the same result.
- **[Recommended]** Compare the two folders after `Path.TrimEndingDirectorySeparator`, and with
  `OrdinalIgnoreCase` on Windows and macOS. Add a test of `EngineHost` with a trailing separator, with and without a path.

**No finding.** **[Observed]** The fixes of E152 to E155 hold: the test of a point on the plane of a distant face, the
replay of an assembly, the header of the version 2 and the refusal of an export of an assembly. An export to the 8.3
short name of its own document is refused. The theory of the native packages reads each file of both packages.

**Not verified.** An engine before TASK-0202 with a header of the version 2 was read, not run. Linux and macOS for the
probes of this section.

### The desktop and the phone

#### V97 · Medium · The start again chooses the previous document before the surface stops, so a command of the stop leaves the next session empty · Confirmed by probe

- **[Observed]** The lead of the external reviewer is true. `RunSessions` reads `FileName` and `File.Exists(path)` and
  sets `previous` inside `using (surface)` (`3DEngine/DesktopEngineHost.cs:104-107`). The surface stops only at the end
  of that block (`DesktopSurface.Dispose`, `3DEngine/DesktopSurface.cs:172-175`), and the session closes after it. A
  command that starts before `Session.Close` still commits (`Engine.Core/DocumentSession.cs:243-256`).
- **[Observed]** Probe: a test in `3DEngine.ImageTests` ran `RunSessions` with no `--file`. Session 1 had no command,
  registered on `surface.Stopping` a callback that applied a box, and opened `second.3de`; the fake start refused
  `second.3de`, as in the test of V91. Result: exit 0; the late command gave `Applied`; the error output gave
  "3DEngine opens the document of the start again"; session 1 had the journal `7d94…3de`, session 2 had a new journal
  `fe12…3de` with 0 bodies. The unsaved folder held `7d94…3de`, 305 bytes, 1 body.
- **[Inferred]** An agent that sends its first command while the person accepts an open, and a next session that
  does not start, give the person a new empty document. The command stays in a journal of the unsaved folder that the
  window does not show. No data is lost on the disk. The case needs three events at one time, so it is rare.
- **[Recommended]** Choose `previous` after the stop of the surface, for example after the outer `using`, when
  `Session.Close` has returned and no client can reach the session. Make the probe a fact of `DesktopSessionTests`: a
  command in `surface.Stopping` and a refused next start give a second session with 1 body and `FileName` null.

#### V98 · Low · For an extruded body, the list "Cut with" lies outside the panel · Confirmed by probe

- **[Observed]** `Selected` puts the list on the line of Hide, Move and, for an extrude, Set distance, with a width of
  120 pixels and its label (`3DEngine.App/Ui/DesktopUi.cs:738-778`). The panel is 460 pixels wide
  (`DesktopUi.cs:619`). A probe drew the panel at 1024 x 768 with a selected body and one other visible body. Panel:
  x 16 to 476. Kind `Box`: "Cut with" at x 214 to 450, inside. Kind `Extrude`: Set distance at x 214 to 406, and
  "Cut with" at x 418 to 654, so 178 pixels lie outside the panel, the label included.
- **[Observed]** `CutUiTests.The_List_Of_A_Cut_Shows_Only_With_Another_Visible_Body` checks a box only
  (`3DEngine.ImageTests/CutUiTests.cs:18`, `:58-60`).
- **[Inferred]** A pad of a sketch gives an extrude, so a common body shows a list that the panel clips. The person
  sees a part of the frame, with no label and no arrow. ADR-0073 item 1 says that the list is "on the line of Move",
  and the comment says that the line keeps Show all in the panel; for an extrude the line itself leaves the panel.
- **[Recommended]** Put the list on a new line when `ImGui.GetContentRegionAvail().X` is less than its width, or always
  on a line of its own, and check the vertical budget of V50 again. Add the kind `Extrude` to the test of the list.

#### V99 · Low · On the phone, a rejected cut gives no text, because the line of the document shows only with IDocumentFiles · Confirmed by reading and probe

- **[Observed]** ADR-0073 item 2 says that "a rejection goes to the line of the document". `NativeThreeDEngine` puts
  the rejection of a cut in `_documentMessage` (`3DEngine.App/NativeThreeDEngine.cs:291-292`), but it gives the panel
  a `DocumentLine` only when it has an `IDocumentFiles` (`:546`). The panel draws the message inside `File` only
  (`3DEngine.App/Ui/DesktopUi.cs:623-624`, `:1252-1253`). `PhoneHost` gives no `IDocumentFiles`
  (`3DEngine.Android/PhoneHost.cs:44`).
- **[Observed]** Probe on a `LocalSessionHost`: a plate of 10 x 10 x 2 and a tool of 20 x 20 x 20. `BodyActions.Cut`
  gave `E-GEOM-NATIVE-OP: … Subtraction produced an empty solid … (the subtrahend fully contains the minuend).`, and the
  document kept its 2 bodies. The phone shows nothing and writes nothing to the log.
- **[Inferred]** On the phone a tap on a tool that holds the body does nothing visible. The same is true for each other
  message of that line: an Undo with no history, a Redo, a sketch on a face. If step 5 of the test sheet fails, the owner
  gets no reason on the screen and no line in the log.
- **[Recommended]** Draw the message line also with no `IDocumentFiles`, for example a `DocumentLine` with no file
  controls, or write each rejection to the standard error on the phone. Test: a frame with no file and a message shows
  the message in the panel.

#### V100 · Low · FanOutSink writes its report inside the commit, the defect that E67 removed from EventBroadcaster · Confirmed by probe

- **[Observed]** `FanOutSink.Append` calls `Console.Error.WriteLine` in its `catch`, inside the commit
  (`3DEngine.App/LocalSessionHost.cs:48-66`). Its comment says "as for a subscription of EngineHost". `EventBroadcaster`
  sends the same report to the thread pool with a `try`, because a report that threw stopped the commit (finding E67,
  `Engine.Api.Http/WebSockets/EventBroadcaster.cs`, method `Announce`).
- **[Observed]** Probe: `Console.SetError` with a writer that throws, one action that throws, and one `Append`. `Append`
  threw `IOException`, after the inner sink held the event (count 1). `DesktopSinkContractTests` lists `FanOutSink` as
  tested, but `LocalSessionHostTests` does not test a report that throws.
- **[Inferred]** A sink that throws breaks the contract of the sinks: the commit stops after the document changed, and
  each later action misses the rest of that commit. The cost is small, because the standard error of Android seldom
  throws. A report that blocks also holds the serial section.
- **[Recommended]** Send the report out of the commit as `Announce` does, with `ExecutionContext.SuppressFlow` and a
  `try`. Make the probe a test of `LocalSessionHostTests`: a throwing error output and a throwing action give no
  exception from `Append`.

#### V101 · Low · With the default of SDL, the engine does not end "before Android destroys its surface" · Inferred by reading

- **[Observed]** `PhoneLifecycle` says that the host ends the engine "at the first sign of the background", and that
  the step Background ends the engine "before Android destroys its surface" (`3DEngine.App/PhoneLifecycle.cs:5-8`,
  `:14`). `PhoneHost` reads these events with `SDL_PollEvent` in its frame loop (`3DEngine.Android/PhoneHost.cs:58-66`)
  and sets no hint. The SDL of the package is 3.5.0 (`SDL-3.5.0-ea7a2da` in `libSDL3.so` of ppy.SDL3-CS 2026.1002.1),
  and it holds the hint `SDL_ANDROID_BLOCK_ON_PAUSE` (a scan of the strings of the file with Node.js).
- **[Inferred]** The documentation of SDL 3 says that the event loop blocks while the program is paused, by default, and
  that the application events "must be handled in a callback set with SDL_AddEventWatch()". I did not read that text on
  this disk. If that is true, the thread of the program blocks inside `SDL_PollEvent` after SDL queues the events of the
  background. The program sees `WILL_ENTER_BACKGROUND` only after the return, together with the events of the
  foreground. The engine then ends at once and a new one starts, so the life cycle still works, but by another path
  than the text says. With a hint of 0, a frame can run after Android destroys the surface, and
  `vkGetPhysicalDeviceSurfaceCapabilitiesKHR(...).CheckResult()` (`3DEngine.Vulkan/Presenter.cs:171`) can throw. The
  catch of `Run` then ends the session, and the phone has no file of the document.
- **[Recommended]** Set `SDL_HINT_ANDROID_BLOCK_ON_PAUSE` to "1" in `PhoneHost`, so that the life cycle does not rest on
  a default. Correct the comments and ADR-0073 item 3 to say that the engine ends at the return. Add one row to the
  test sheet: in step 8 the log line "goes into the background" comes at the return, just before "in the foreground
  again".

#### V102 · Low · ADR-0073 does not name the files of its items 1 and 2 in its affects field · Confirmed by reading

- **[Observed]** The affects field of ADR-0073 names `3DEngine.App/PhoneLifecycle.cs` and `3DEngine.Android/**` only
  (`docs/adr/0073-the-phone-cuts-and-keeps-its-document.md:11-13`). Items 1 and 2 decide the list "Cut with" in
  `3DEngine.App/Ui/DesktopUi.cs`, `CutBodyAction` in `3DEngine.App/Ui/PanelAction.cs` and `BodyActions.Cut` in
  `3DEngine.App/BodyActions.cs`. `docs/templates.md` (the row `affects`) says that `TaskGovernanceGateTests` compares
  these patterns with the write set of each open task, and that the result is the list of ADRs that the agent must read.
- **[Inferred]** A later task that changes the list "Cut with" or `BodyActions.Cut` must name ADR-0072 (`3DEngine.App/**`),
  and the gate does not ask for ADR-0073. The agent of that task does not read the decision that it changes.
- **[Recommended]** Add `3DEngine.App/Ui/DesktopUi.cs`, `3DEngine.App/Ui/PanelAction.cs` and `3DEngine.App/BodyActions.cs`
  to the affects field, with a History line, and correct the `governed-by` of the open tasks that the gate then names.

#### V103 · Low · Two texts are false after R14e2 and R14f2 · Confirmed by reading

- **[Observed]** ADR-0072, Consequences, as TASK-0225 corrected it for V96: "The program references the application …
  and fifteen ADRs name its paths" (`docs/adr/0072-the-drawing-code-is-a-library.md:83-84`). Two projects reference
  `3DEngine.App.csproj`: `3DEngine/3DEngine.csproj` and `3DEngine.Android/3DEngine.Android.csproj`. The Android project
  existed already at the commit of TASK-0225 (`8186e8f`). The affects fields of 17 ADRs name `3DEngine.App`: 0007, 0019,
  0027, 0029, 0030, 0031, 0032, 0034, 0038, 0041, 0043, 0046, 0047, 0053, 0056, 0072 and 0073, so 16 other than ADR-0072.
- **[Observed]** The test sheet says "No device and no emulator ran the program before this test"
  (`docs/android-test-sheet.md:6-7`). TASK-0229 (status Active) adds the job "Start the APK on the emulator"
  (`.github/workflows/ci.yml:252-258`), and its write set does not hold the test sheet.
- **[Inferred]** The correction of V96 was false at its own commit. The sheet becomes false at the first green run of
  the emulator job. A count in a text drifts at each new project or ADR.
- **[Recommended]** Write "two programs" and remove the count of ADRs, or give no counts. Put the test sheet into the
  write set of TASK-0229, and say what the emulator job proves and what it does not prove (no touch, no background).

**No finding.** **[Observed]** The adapter of R14e1 and the session of the phone give the same events in the order of
the commits. The APK of `df11d6e` holds its assemblies and the libraries of R14d for both ABIs, and it builds with zero
warnings. The fix of E149 holds.

**Not verified.** No device and no emulator ran on this computer; the order of the events of SDL in the background is
from the code of the binding, not from a run.

### The gates and the pipeline

#### T147 · Medium · The review gate does not count a product phase with a letter and then a digit, so R14e1 to R14f2 and P8b1 count as entries only · Confirmed by a probe

- **[Observed]** `Engine.Tests/Governance/CodebaseReviewGateTests.cs:302` to `303` gives
  `ProductPhase = ^(?:[RKV]\d+|P(?!0\.)\d+)(?:[a-z]|\.\d+)?$`. `MilestonesAfter` (`CodebaseReviewGateTests.cs:323`) counts
  an entry as a product milestone only when its heading phase, or the first cell of a roadmap row that names its task
  (`ProductRowPhase`, line `343`), matches it. Each slice since P8b1 has the form letter, digit, letter, digit.
- **[Observed]** Probe `Probe_Milestones` (reflection on the private method, with the real `docs/CURRENT-STATE.md` and
  `docs/roadmap.md`): after v0.207 the gate gives (0 product, 6 entries); the true count is 3 (R14e2, R14e3, R14f1).
  After v0.198 it gives (2, 15); the true count is 4 (K6b, P8b1, R14d, R14e1). `ProductPhase` matches `K6b` and `R14d`,
  and does not match `P8b1`, `R14e1`, `R14e2`, `R14f1` or `R14g1`. Six synthetic entries `(R14g1, TASK-0002)`, with a
  roadmap row `| R14g1 | ... TASK-0002. |`, give (0, 6), so `A_Review_Is_Not_Overdue` passes after six product milestones.
- **[Observed]** `UnnamedProductTasks` (`CodebaseReviewGateTests.cs:478`) uses the same expression, so the checks of T66,
  T73 and T84 do not apply to these phases either.
- **[Observed]** The ledger v0.210 (`docs/CURRENT-STATE.md`, section "What stays") records the lead from the review of
  Codex of v0.208 and gives it to this review. The entry v0.210 also corrects the false sentence of v0.207 and v0.208.
- **[Inferred]** Only the limit of 12 entries can make a review due now. The rule of four product milestones in
  `CLAUDE.md` has no gate for the current form of the roadmap.
- **[Recommended]** Accept any number of letter and digit groups after the first number, for example
  `^(?:[RKV]\d+|P(?!0\.)\d+)(?:[a-z]\d*)*(?:\.\d+)?$`. Add the cases `P8b1`, `R14e1` and `R14f2` (true) and `P0.89` (false) to
  the theory of the phase, and a case of five `R14g1` entries that is overdue.

#### T148 · Low · A PackageReference Update in a Directory.Build.targets inside 3DEngine.Vulkan changes the version of the pin, and each gate passes · Confirmed by injection

- **[Observed]** `Each_Binding_Has_One_Pin_In_The_Vulkan_Layer` (`DependencyDirectionGateTests.cs:1353` to `1367`)
  counts the projects whose files name the package (`PackageReferences`, line `454`, which reads `Include` and `Update`
  in each file of `FilesThatMsBuildReads`). It checks that the only such project is `3DEngine.Vulkan`. No gate reads the
  version, and a file inside the folder of `3DEngine.Vulkan` belongs to that project.
- **[Observed]** Injection: `3DEngine.Vulkan/Directory.Build.targets` with
  `<PackageReference Update="ppy.SDL3-CS" Version="2025.920.0" />`. `dotnet msbuild 3DEngine.Vulkan/3DEngine.Vulkan.csproj
  -getItem:PackageReference` gave `ppy.SDL3-CS` with `"Version": "2025.920.0"`, not the `2026.1002.1` of the project file.
  `dotnet run --project eng/repo-gates -- --root .` gave "450 runs of 157 checks, 0 failed". `dotnet test Engine.Tests
  --filter Governance` gave 465 passed and 1 failed, `BuiltAssemblyReferenceTests`, which failed only because the render
  assemblies were not built yet (its message: "The built assembly of 3DEngine.Core is not at ..."). File removed.
- **[Observed]** The ledger v0.210 records the lead from the review of Codex of v0.206 and gives it to this review.
- **[Inferred]** The project file says one version and the build uses another, so "which version does the repository
  use" has two answers again (ADR-0017 rule 4). The same form applies to `ImGui.NET` in `3DEngine.App`.
- **[Recommended]** Refuse each `Version`, `VersionOverride` or `Update` of a pinned package outside the one project
  file of its pin, or read the version that MSBuild evaluates (`-getItem:PackageReference`) in a pipeline step. Prove
  the gate with this injection.

#### T149 · Medium · The smoke test of the emulator passes when the session of the phone ends with an exception · Confirmed by a probe

- **[Observed]** `eng/android/smoke-emulator.sh:32` stops the wait at the line "3DEngine: the session of the phone
  ended", and line `39` fails only when the line of the start is absent. `3DEngine.Android/PhoneHost.cs:28` writes the
  line of the start before `new LocalSessionHost(new ManifoldGeometryBackend())` and before `new Window(...)`, and
  `PhoneHost.cs:37` writes the line "ended" only in the `catch` of an exception.
- **[Observed]** Probe: a fake `adb` in the scratchpad whose `logcat -d` gives the line of the start and the line
  "3DEngine: the session of the phone ended: System.TypeInitializationException: ... HandlerCatalog ...". The command
  `ANDROID_HOME=<fake> bash eng/android/smoke-emulator.sh dummy.apk report.txt` printed `started=1 drawn=0`, a
  `::warning`, and gave exit 0.
- **[Inferred]** A failure of the composition of the engine, of the native backend or of the window on Android passes
  the job "Start the APK on the emulator". The job then proves only that .NET and the activity of SDL started. The
  first frame is a report fact by design (ADR-0061 item 3), but an exception is not the Vulkan floor.
- **[Recommended]** Fail the script when the line "ended" is in the log, or when the log holds `FATAL EXCEPTION` or a
  native crash of the package. If a Vulkan floor of the emulator is an expected failure, give it its own line in
  `PhoneHost` and accept that line only. Prove the script with the fake `adb` of this probe. TASK-0229 is still Active,
  so its close can take this.

#### T150 · Low · The check of the APK accepts a library whose checksum is any row of the notices, so the libraries of arm64 in the folder x86_64 pass · Confirmed by a probe

- **[Observed]** `eng/android/check-apk.sh:35` accepts a library when its checksum is anywhere in
  `THIRD-PARTY-NOTICES.md` (`grep -q "\`$sum\`"`). It does not check that the row names the same ABI and the same file.
- **[Observed]** Probe: an APK made with `tar -a` in the scratchpad, whose two folders `lib/arm64-v8a` and `lib/x86_64`
  hold the `android-arm64` files `libmanifold.so`, `libmanifoldc.so` and `libcimgui.so` of the two packages, a
  `libSDL3.so` and each `lib_<assembly>.dll.so`. `bash eng/android/check-apk.sh swapped.apk` gave exit 0. The header
  comment says that the APK "holds the bytes of the packages of R14d and no other library of the same name".
- **[Inferred]** The emulator runs x86-64 only, so a library of the folder `arm64-v8a` with the bytes of another ABI or
  of another file passes both jobs and fails on the phone only.
- **[Recommended]** Map each `lib/<abi>/<library>` to its row: `arm64-v8a` to `runtimes/android-arm64/native/`, and
  `x86_64` to `runtimes/android-x64/native/`, with the prefix `Engine.Native.DearImGui/` for `libcimgui.so`. Compare
  the checksum with that row only. Prove it with this APK.

#### T151 · Low · The ledger entries v0.208 to v0.211 give one entry too few after the review of v0.207 · Confirmed by reading

- **[Observed]** The gate counts each heading after the version of the `ledger` field of the review, v0.207
  (`CodebaseReviewGateTests.cs:318` to `322`), so v0.208 is the first entry. The ledger gives "0 entries" at v0.208
  (`docs/CURRENT-STATE.md:6230`), 1 at v0.209 (line `6261`), 2 at v0.210 (line `6295`) and 3 at v0.211 (line `6324`). The
  gate gives 1, 2, 3 and 4. The entries v0.212 (5) and v0.213 (6) are correct, and the probe of T147 gave 6 at v0.213.
  The previous cycle counts the review entry: v0.199 says "1 entry" (`docs/CURRENT-STATE.md:5978`).
- **[Inferred]** The sentence is a count by hand that a person reads to know when a review is due. Four entries are
  false. No entry is permanent text that a gate reads.
- **[Recommended]** Add a correction in the next ledger entry, as `CLAUDE.md` says, and let the gate print the count in
  its message so that an entry can copy it.

**No finding.** **[Observed]** The rules of T145 read the frameworks and the SDK of each reached project, and the fact of
T146 compares the rows of the list with the classes of the projects.

**Not verified.** The job of the emulator ran in the pipeline only; no probe ran it here.

## 4 The next planned task

The next product phase is R14f3, the hash of Android: the pipeline compares the determinism hash of the program on the
emulator with the hashes of the three desktop runners (ADR-0061 item 8). No finding is critical or high, so no correction
comes before R14f3. By `docs/templates.md`, section 7, the medium and the low findings go after it: P0.91 (TASK-0231)
holds E156, E157 and T147 to T151, and P0.92 (TASK-0232) holds V97 to V103.

## 5 Research for that task

**[Observed]** T149 touches R14f3: the hash of Android comes from the program on the emulator, and the smoke test that
starts it passes after an exception. R14f3 needs a condition that the session ran, not only that it started.
**[Inferred]** T147 lets a fifth product milestone merge with no review; until P0.91 the count stays by hand.

## 6 Recommended approach

**[Recommended]** In this order:

1. R14f3: the hash of Android, with a smoke test that fails on the line of an ended session.
2. P0.91 (TASK-0231): E156, E157 and T147 to T151, each with a test or an injection before the change. 2 agent hours.
3. P0.92 (TASK-0232): V97 to V103, each with a test or an injection before the change. 2 agent hours.
4. The test of the owner on the S23 Ultra, then the next slices of R14e.

## Questions for the owner

The standing order of 2026-11-05 answers each question with the recommended option. The owner checks these answers on
2026-11-08.

**Q1. The engine of the phone in the background (V101).**
- A: The host turns off the block on a pause of SDL (`SDL_HINT_ANDROID_BLOCK_ON_PAUSE` 0), so its loop sees the
  background at once and ends the engine before Android destroys the surface.
- B: The block stays, and ADR-0073 item 3 says that the engine ends at the return to the foreground.

**Recommended: B until the test of the owner.** No device ran the program; the test of step 8 of the test sheet shows
which order the phone gives, and A changes the life of the thread with no run to prove it.
