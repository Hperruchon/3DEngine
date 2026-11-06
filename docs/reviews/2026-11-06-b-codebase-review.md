---
date: 2026-11-06
commit: 41db916
ledger: v0.207
previous: 2026-11-06-codebase-review.md
---

# Codebase review — 2026-11-06 (b)

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the twenty-fourth codebase review. The review of 2026-11-06 examined v0.198. After it came 4 product
milestones (K6b, P8b1, R14d and R14e1) and 9 ledger entries (v0.199 to v0.207). The next product milestone, R14e2,
would be the fifth, so the gate makes this review due before it. `Engine.Tests/Governance/CodebaseReviewGateTests.cs`
holds the rule, and `docs/templates.md`, section 7, gives the form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is the
authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this file does not.

| Item | Value |
|---|---|
| Code state | `main` at `41db916`, the merge of v0.207; the agents examined `ff59f82`, whose tree the merge holds |
| Branch of this review | `codebase-review-2026-11-06-b` |
| Tests | 1,863 on this computer (1,328 in `Engine.Tests`, 332 in `3DEngine.Tests`, 200 in `3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`) |
| Pipeline | The run of the branch `r14e1-drawing-library` (42147730036): green on Ubuntu, Windows and macOS, and the three determinism hashes are equal |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner decides.

**Method.** Nine tasks changed code or records after `ab55b07`, the commit of the last review: TASK-0199 (the review),
TASK-0220 (P0.87), TASK-0201 (K6b), TASK-0202 (P8b1), TASK-0196 (R14d), TASK-0197 (P0.85), TASK-0221 (P0.88),
TASK-0198 (P0.86) and TASK-0222 (R14e1). Two review agents worked in parallel at `ff59f82`, each one in its own git
worktree: the engine with the gates, and the desktop with the render side. Each agent wrote probes and injections that
only its worktree held, and removed them. I read the cited lines of the medium findings E152, T145 and V91, and I
measured the values below. The reviews of Codex of v0.199 to v0.205 give four findings: two that P0.88 fixed, and the
leads of E152 and E153, which the engine agent confirmed. The lead of v0.196 is V92.

## Summary

- **The fixes of the last review hold.** E142 to E151, V89, V90, T141, T142 and T144 are fixed. V88 and T143 stay
  open. **[Observed]**
- **No new finding is critical or high.** **[Observed]**
- **Three new medium findings.** The test of a void refuses a void whose first vertex lies on the plane of any face of
  an outer shell, with a false reason (E152). The rule of R14e1 reads the direct framework references of the
  application only, so the web SDK or a framework reference in `Engine.Core` gives it ASP.NET Core, and each gate
  passes (T145). The start again of E150 opens a document of the unsaved folder as a named file, or ends the window
  (V91). **[Observed]** by probes and injections.
- **Nine new low findings:** E153 to E155 in the replay, the old engine and the export of an assembly; V92 to V96 in
  the desktop; T146 in the gates. **[Observed]**
- **Open findings: 0 critical, 4 high, 17 medium and 27 low.** **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 17 | `.csproj` lines in `3DEngine.sln`. The last review gave 16; R14e1 added `3DEngine.App`. |
| Production source lines | 28,741 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests`, `3DEngine.ImageTests` and `3DEngine.VisionTests`, 272 files. The last review gave 28,011 in 264 files. |
| Test source lines | 33,941 | `Engine.Tests` (24,698 in 116 files), `3DEngine.Tests` (3,544 in 22 files), `3DEngine.ImageTests` (5,634 in 34 files) and `3DEngine.VisionTests` (65 in 1 file). The last review gave 33,111. |
| Tests | 1,863 | `dotnet test 3DEngine.sln`: 1,328, 332, 200 and 3 passed, 0 failed, 0 skipped. The last review gave 1,818. |
| Gate classes | 19 | Tracked files whose name ends in `GateTests.cs`. `eng/repo-gates` gives 443 runs of 155 checks. The last review gave 19, and 438 runs of 153 checks. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 15 | Section "Open" of `docs/register.md`; the limit is 15. The last review gave 14; R-0055 was opened. |
| ADRs | 64 | `docs/adr/0001` to `0061` and `0070` to `0072`, with 0003 and 0011 in `docs/adr/archive/`. In force: 62 (27 accepted, 35 amended). None is proposed. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 4 | The same method: V2, V3, T4, T31. The last review gave 4. |
| Findings: medium | 17 | The same method: E5, E6, E12, E13, E19, E21, E22, E152, V4, V91, T2, T5, T10, T17, T32, T37, T145. The last review gave 18; E142, E143, E146 and E147 are fixed. |
| Findings: low | 27 | The same method: E17, E24, E34, E39, E57, E84, E153 to E155, V7, V15, V29, V84, V88, V92 to V96, T7, T13, T23, T25, T143, T146, C1, P1. The last review gave 29; E144, E145, E148 to E151, V89, V90, T141, T142 and T144 are fixed. |
| Lines in the rule files | 2,863 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The last review gave 2,828. |
| Sentences with "must" | 59 | `grep -o -w -i must` on the same ten files. The last review gave 58. |
| Sentences with "do not" | 54 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 54. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found. E13, T32, E84 and V84 are fixed in part and count as open. A finding that the owner accepted counts as closed.

## Findings of the previous review

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. No change: `Engine.Api.Http/Endpoints/JsonParameters.cs` has no commit since `ab55b07`. Register entry R-0027.
- **E6** — Open. `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` changed only for the two events of the references of P8b1 (TASK-0202). Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. No change.
- **E8** — Fixed. TASK-0044, v0.37. No change.
- **E9** — Fixed, and it holds. No change.
- **E10** — Fixed. TASK-0035, v0.43. No change.
- **E11** — Fixed. TASK-0115, v0.115. No change.
- **E12** — Open, and smaller. No change: `Engine.Core/Hosting/ParameterBinder.cs` has no commit since `ab55b07`. Register entry R-0027.
- **E13** — Fixed in part. No change: `Engine.Api.Http/Endpoints/QueriesEndpoint.cs` has no commit since `ab55b07`; the changes of `Engine.Cli/Cli.cs` touch the export and the option `--kind` only.
- **E14** — Fixed. TASK-0115, v0.115. No change.
- **E15** — Fixed. TASK-0044, v0.37. No change.
- **E16** — Fixed. TASK-0115, v0.115. No change.
- **E17** — Open, and smaller. `Engine.Core/CommandBus.cs` changed only for the rule of the kinds and the references of P8b1 (TASK-0202); a query handler that throws still leaves `QueryBus`. Register entry R-0028.
- **E18** — Fixed for the sinks of `Engine.*` (TASK-0140, v0.141) and of the desktop host (T118, TASK-0150, v0.154). TASK-0140, v0.141. No change.
- **E19** — Open, and smaller. `CommandBus.cs`, `DocumentSession.cs` and `EngineHost.cs` changed only for the kind of P8b1 (TASK-0202); the kept solids of a closed session stay until 64 newer ones push them out. Register entry R-0033.
- **E20** — Fixed, and it holds. No change.
- **E21** — Open. No change: `Engine.Core/Commands/SubtractCommandHandler.cs` has no commit since `ab55b07`. Register entry R-0033.
- **E22** — Open. `Engine.Core/CommandBus.cs` changed only for the kinds and the references of P8b1 (TASK-0202). Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. No change.
- **E24** — Open in seven parts of nine. `WireMessage.cs` and `SchemaEventsEndpoint.cs` changed only for the kind and the references of P8b1 (TASK-0202). Register entry R-0033.
- **E25** — Accepted by the owner. TASK-0142, v0.144. No change.
- **E26** — Fixed. TASK-0135, v0.136. No change.
- **E27** — Accepted by the owner. TASK-0142, v0.144. No change.
- **E28** — Fixed. TASK-0136, v0.137. No change.
- **E29** — Fixed for the replay. TASK-0135, v0.136. No change.
- **E30** — Fixed. TASK-0136, v0.137. No change.
- **E31** — Fixed for the replay. TASK-0137, v0.138. No change.
- **E32** — Fixed. TASK-0135, v0.136. No change.
- **E33** — Fixed with E18. TASK-0140, v0.141. No change.
- **E34** — Open. No change: `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs` has no commit since `ab55b07`. Register entry R-0036.
- **E35** — Fixed for the routes of `MapGet`. TASK-0139, v0.140. No change.
- **E36** — Fixed. TASK-0079, v0.85. No change.
- **E37** — Fixed. TASK-0137, v0.138. No change.
- **E38** — Fixed. TASK-0138, v0.139. No change.
- **E39** — Open. No change: `3DEngine/DesktopSession.cs`, `Engine.Cli/Cli.cs` for the backend, and `Engine.Api.Http.Server/ServerEngine.cs` keep the same three choices of the backend. Register entry R-0036.
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
- **V2** — Open. No change: `3DEngine.Vulkan/Swapchain.cs` and `3DEngine.Vulkan/OverlayRenderer.cs` have no commit since `ab55b07`. Register entry R-0022.
- **V3** — Open. No change: `3DEngine.Vulkan/GraphicsDevice.cs` has no commit since `ab55b07`. Register entry R-0022.
- **V4** — Open. No ledger entry from v0.199 to v0.207 names it. Register entry R-0027.
- **V5** — Fixed. TASK-0076, v0.74. No change.
- **V6** — Fixed. TASK-0076, v0.74. No change.
- **V7** — Open. No change: `3DEngine.Vulkan/Ui/UiRenderer.cs`, `3DEngine.Vulkan/Ui/UiTexture.cs` and `3DEngine.Vulkan/GpuBuffer.cs` have no commit since `ab55b07`. Register entry R-0030.
- **V8** — Fixed. TASK-0074, v0.75. No change.
- **V9** — Fixed. TASK-0074, v0.75. No change.
- **V10** — Fixed. TASK-0074, v0.75. No change.
- **V11** — Fixed in part. TASK-0074, v0.75. No change.
- **V12** — Fixed, and changed by decision. No change.
- **V13** — Fixed for its parts. No change.
- **V14** — Fixed. TASK-0074, v0.75. No change.
- **V15** — Open. `3DEngine.App/SessionBodies.cs:274` still waits for the idle device for each removed mesh; R14e1 moved the file and changed its type of host only.
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
- **T2** — Open in one part of five. No ledger entry from v0.199 to v0.207 names it. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. No change.
- **T4** — Open. No change: `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs` has no commit since `ab55b07`. Register entry R-0031.
- **T5** — Open in one part of three. No ledger entry from v0.199 to v0.207 names it. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Open in four parts of five. No ledger entry from v0.199 to v0.207 names it. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. No change.
- **T10** — Open. No ledger entry from v0.199 to v0.207 names it. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51. No change.
- **T12** — Fixed. TASK-0059, v0.55. No change.
- **T13** — Open in part. No change: `.github/workflows/ci.yml` has no commit since `ab55b07`. Register entry R-0031.
- **T14** — Fixed. TASK-0053, v0.51. No change.
- **T15** — Fixed. TASK-0050, v0.47. No change.
- **T16** — Fixed. TASK-0066, v0.67. No change.
- **T17** — Open. No change: `Engine.Tests/Governance/NativePackageGateTests.cs` and `nuget.config` have no commit since `ab55b07`. Register entry R-0031.
- **T18** — Fixed. TASK-0049, v0.46. No change.
- **T19** — Fixed. TASK-0049, v0.46. No change.
- **T20** — Fixed. TASK-0049, v0.46. No change.
- **T21** — Fixed. TASK-0049, v0.46. No change.
- **T22** — Fixed. TASK-0049, v0.46. No change.
- **T23** — Open. No change: `eng/manifold-native/build.ps1` has no commit since `ab55b07`. Register entry R-0031.
- **T24** — Fixed. No change.
- **T25** — Open in part. No change: `eng/write-set-check` has no commit since `ab55b07`. Register entry R-0031.
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
- **C1** — Open. No change: `IBRepOps.cs` and `IFeatureIdMap.cs` have no commit; the change of `Engine.Contracts` is the kind and the references of P8b1 (ADR-0071). Register entry R-0027.
- **P1** — Open, and smaller. No change since `ab55b07` other than the move of R14e1. Register entry R-0027.
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
- **V29** — Open. No check by touch from v0.199 to v0.207, and the register has no entry for it.
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
- **E84** — Fixed in part. TASK-0099, v0.102. `Engine.Core/Persistence/DocumentJournal.cs` changed only for the kind in the header of an assembly (TASK-0202).
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
- **V84** — Fixed in part. R14e1 moved `NativeThreeDEngine.cs` and `FaceArm.cs` into `3DEngine.App/` with the same call of v0.183.
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
- **E142** — Fixed. TASK-0197, v0.204: the owner reads the midpoint of an edge of the hole; the test of three rotations failed for two before the change. **[Observed]**
- **E143** — Fixed. TASK-0197, v0.204 (Q1 A): `StepExport.Plan` refuses an edge inside a face that does not hold it. **[Observed]**
- **E146** — Fixed. TASK-0197, v0.204: two vertices that round to one double are refused. **[Observed]**
- **E147** — Fixed. TASK-0197, v0.204: four checks of `StepReader`, each proved by an injection. **[Observed]**
- **E144** — Fixed. TASK-0197, v0.204: a loop that holds a vertex two times is two loops. **[Observed]**
- **E145** — Fixed. TASK-0197, v0.204: an edge of more than two faces has its own reason. **[Observed]**
- **E148** — Fixed. TASK-0197, v0.204 (Q2 A). **[Observed]**
- **E149** — Fixed. TASK-0197, v0.204: one open at a time, three runs of three. **[Observed]**
- **E150** — Fixed. TASK-0197, v0.204 (Q3 A): a session on the previous file starts again. **[Observed]**
- **E151** — Fixed. TASK-0197, v0.204. **[Observed]**
- **V88** — Open. TASK-0198 left it to R14e, and R14e1 made no Android program. See the desktop section.
- **V89** — Fixed. TASK-0198, v0.206: two cases of one axis fail with the injected test. **[Observed]**
- **V90** — Fixed. TASK-0198, v0.206: `Window.FlushInput` at the start of each later session; no test runs it in a window. **[Observed]**
- **T141** — Fixed. TASK-0198, v0.206: the test reads the render assemblies; the injection fails it. **[Observed]**
- **T142** — Fixed. TASK-0198, v0.206: on the path of the log only the shared framework and the reached first-party assemblies. **[Observed]**
- **T143** — Open. TASK-0198 left it open: Q4 A changes `Diagnostic.Code` and `ErrorDetail.Code` of `Engine.Contracts`, with 497 uses in 102 files, which the rule "Stop and ask" holds for the owner. The register is full.
- **T144** — Fixed. TASK-0198, v0.206: the package rules read `Update`, and each project of the desktop names only its list. **[Observed]**

## 1 Current architecture

**[Observed]** The drawing code, the user interface and the actions are the library `3DEngine.App` (R14e1, ADR-0072).
It reaches the session through `ISessionHost` and the files through `IDocumentFiles`; the desktop program gives
`HostedSession`, an adapter of its `EngineHost`, and `DesktopDocument`. A probe APK that references the library builds
for `android-arm64` with zero warnings, and holds `libSDL3.so` and no native library of cimgui or Manifold. The
native builds of Manifold 3.5.2.2 and cimgui 1.91.6.1 for `android-arm64` and `android-x64` run on GitHub Actions, and
their packages are on the branches `native-package/3.5.2.2` and `native-package-cimgui/1.91.6.1` (R14d).

**[Observed]** A Document is a part or an assembly (P8b1, ADR-0071). An assembly records references to part files of
the store at a fixed version, and the engine does not resolve them (register entry R-0055). The STEP export writes a
void as a `BREP_WITH_VOIDS` (K6b), and it refuses a pinched solid, a sliver and an edge of four faces with a reason
(P0.85). An export of the command line refuses its own document file, through a link of each part of the path (P0.87,
P0.88).

## 2 Patterns in use

- **A rule of a gate reads the direct form, and the class holds the transitive form.** The rule of R14e1 reads the
  framework references of one project, and a framework reference flows through each project reference and through
  the SDK (T145). The list of the packages of the desktop names four projects by hand (T146). **[Observed]**
- **A retry that keeps a path loses the kind of the document.** The start again of E150 keeps the path of a journal
  of the unsaved folder, and opens it as a named file (V91). **[Observed]**
- **A test of a rule reads the table, not the call.** The test of V90 reads the ranges, and no test fails when the
  flush goes (V94). **[Observed]**
- **A new kind of document reaches the replay, the export and the old engine last.** The replay of TASK-0001, the export
  of the command line and an older engine each treat an assembly as a part (E153 to E155). **[Observed]**
- **A ray test that skips a degenerate case by a new direction fails for a case that each direction shares.** A point
  on a plane is on that plane for each ray (E152). **[Observed]**

## 3 Review of the code

### The engine and the hosts

#### E152 · Medium · A void whose first vertex lies on the plane of a face of an outer shell is refused for each ray, with a false reason · Confirmed

- **[Observed]** `Engine.Core/Export/StepExport.cs:185` takes the first vertex of the first loop of the first face of
  the void as the point of the test. `Crossings` computes `height = Offset - Normal·point` for each face of the outer
  shell, and `StepExport.cs:237` returns null when `t` is zero. `t` is zero exactly when `height` is zero, and `height`
  does not depend on the direction. The branch `along.IsZero && height.IsZero` also returns null. So when the point
  lies on the plane of any face of a candidate outer shell, each of the eight directions returns null, also when the
  point is far outside that face. `Inside` then returns null, and `Plan` refuses the body at `StepExport.cs:192`.
- **[Observed]** Probe `Probe_Void_First_Vertex_On_A_Distant_Plane`: an L-shaped body, `Box(20,10,10)` minus
  `Box(10,10,10)` moved by (5, 0, 5), has a step face at z = 0 for x in [0, 10]. `StepExport.Inside(L, shell, (-5,0,0))`
  gave null, and `(-5,0,-1)` gave true. The L minus `Box(2,2,2)` moved by (-5, 0, -1) (a void from z = -2 to z = 0, at
  x = -6 to -4, far from the step face) has 2 shells and the first void vertex (-4, -1, 0). `StepExport.Write` refused
  it: "no ray of the test of a void passes clear of the edges of a shell." The same void moved to z = 0..2 or
  z = -4..-2 (first vertex not on a plane of the L) was written.
- **[Observed]** ADR-0070 item 2 (`docs/adr/0070-step-export-of-the-owned-kernel.md:57`) and the ledger entry v0.201
  say that "a ray that meets ... the plane of a face at the point goes to the next of eight fixed directions". That
  rule cannot succeed for a plane: each direction starts on the same plane. The reason of the refusal names edges, and
  the cause is a plane.
- **[Inferred]** A stepped part with a cavity whose top or bottom is level with a step, a shoulder or a face of a second
  solid of the body, cannot be exported. A person meets a refusal with a false reason. A level face is common in a
  designed part, because a person dimensions both faces from the same datum.
- **[Recommended]** In `Crossings`, when `height` is zero, call `InFace(face, point)`. When it is false, skip the face
  (for `along` not zero the ray leaves the plane at once; for `along` zero the ray lies in the plane, so return null for
  this direction only when the ray can meet the face). When it is true or null, the void touches the outer shell; give a
  separate reason. As a second defence, try the next vertex of the void before a refusal. Add a test with the L body
  and the void at z = -2..0, which must give a `BREP_WITH_VOIDS`, and a test of `Crossings` for a point on the plane of a
  distant face. Correct the sentence of ADR-0070 item 2 and the reason text.

#### E153 · Low · Replay.ReplayLog makes a part, so a replay of an assembly log diverges at its first reference · Confirmed

- **[Observed]** `Engine.Core/Replay.cs:68` makes `new Document()`, which is a part. `ReplayLog` has no parameter for
  the kind, and `LogEntry` (`Engine.Contracts/LogEntry.cs:7`) records no kind; only the file header holds it.
- **[Observed]** Probe `Probe_Replay_Of_An_Assembly_Log`: an assembly session applied `AddPartReference`, and
  `Replay.ReplayLog(log, registry, new InProcessMeshBackend())` threw `ReplayDivergenceException` "at entry 0 ...
  AddPartReference ... gave Rejected with E-CMD-KIND-REFUSED: The command 'AddPartReference' does not apply in a
  document of the kind 'part'".
- **[Observed]** No production code calls `ReplayLog`; the open of a file (`DocumentFile.Load`) replays through a
  session of the kind of the header, and that path works (`ContainerModelTests.An_Assembly_File_Opens_Again_Only_As_An_Assembly`).
  The callers are the replay tests, the replay determinism gate and the cross-platform hash.
- **[Inferred]** The replay function of TASK-0001, which the determinism gate and the cross-platform hash use, cannot
  replay any assembly. So no determinism gate can cover the assembly events, and a later tool that replays a log (a
  check, an export, an agent) gives a false divergence for each assembly.
- **[Recommended]** Give `ReplayLog` a `kind` parameter (default `Document.Part`) and pass it to `new Document(kind:)`.
  Add a test that replays an assembly log with `AddPartReference`, `Undo` and `Redo` and compares the references and the
  events with the live session.

#### E154 · Low · An engine before TASK-0202 opens an assembly with no reference as a part, writes part entries to it, and the new engine then refuses the file; ADR-0071 says the opposite · Confirmed by reading

- **[Observed]** At `ab55b07`, `EntryCodec.ReadHeader` reads `format`, `formatVersion` and `lengthUnit` only, and
  ignores another member (`git show ab55b07:Engine.Core/Persistence/EntryCodec.cs`, lines 121 to 139). The format
  version stays 1 for an assembly. `DocumentJournal.Create` writes the header at once
  (`Engine.Core/Persistence/DocumentJournal.cs:40`), so `engine-api-http --kind assembly --file a.3de` leaves a file with
  a header and no entry before the first command.
- **[Observed]** ADR-0071 item 4 (`docs/adr/0071-container-model.md:70`) says that an older engine "refuses
  `AddPartReference` ... so it reads no assembly as a part". The ledger entry v0.202 (`docs/CURRENT-STATE.md:6051`) says
  that an older engine "opens an assembly with no reference as an empty part". The two texts disagree, and the ledger is
  correct.
- **[Inferred]** An older engine (or an older desktop) opens such a file as an empty part. Its session appends
  `CreateBox` and other part entries after the assembly header. The new engine then reads the kind `assembly`, the bus
  refuses `CreateBox` with `E-CMD-KIND-REFUSED`, and the open fails with `E-IO-LOAD-FAILED`. The person keeps the bytes,
  but the current engine cannot open the work. A save-as in the older engine writes its own header, with no kind, so the
  assembly becomes a part with no report. An assembly that holds only `NoOp` entries acts the same way.
- **[Recommended]** The owner decides. Option A: write `formatVersion` 2 for an assembly only, so an older engine
  refuses each assembly at the header and a part file keeps its bytes. Option B: keep the format and correct ADR-0071
  item 4 so that it agrees with the ledger. With option A, add a test that the header of an assembly has the version 2
  and that a part header keeps the version 1.

#### E155 · Low · engine export makes a part session, so an export of an assembly gives a load failure whose reason says "the Document is a part" · Confirmed

- **[Observed]** `Engine.Cli/Cli.cs:215` calls `BuildSession(backend, stderr)` with the default kind `part`, and does
  not call `DocumentFile.ReadKind`. ADR-0071 item 4 says "A host makes the Document with the kind of the header of its
  file (`DocumentFile.ReadKind`)".
- **[Observed]** Probe `Probe_Cli_Export_Of_An_Assembly`: `engine export step a.3de a.step` on an assembly file with one
  reference gave exit 1 and `E-IO-LOAD-FAILED` "The file is a document of the kind assembly, and the Document is a part
  (ADR-0071 item 4)."
- **[Inferred]** No file is damaged. An agent reads that the document file is wrong, but the cause is that the export
  does not apply to an assembly in P8b1.
- **[Recommended]** Read the kind of the header in `Export`. For an assembly, give `E-EXPORT-BODY-FAILED` or a clear
  refusal "an assembly has no bodies until a reference resolves (ADR-0071 item 7)". Add a test of the command line.

**No finding.** **[Observed]** The void writer of K6b chooses outer shells and voids by the sign of the volume, gives a
void to the smallest outer shell that holds it, turns the faces of a void, and writes the solids in the order of the
shells, so the bytes are deterministic; its arithmetic is exact, with `sqrt` only in `Unit`. The refusals of P0.85, the
owner of a hole and `Split` of `Brep.cs`, `FaceGone.IsGone`, the rule of the kinds in `CommandBus`, the kind in the
headers of `EntryCodec` and `DocumentJournal`, the kind of `EngineHost`, and `Cli.SameFile` with `Resolve` agree with
their ADRs. An export to the 8.3 short name of its own document is refused, because `Path.GetFullPath` expands it.

**Not verified.** An engine before TASK-0202 with an assembly file (E154) was read, not run. No reader of STEP outside
the repository read a `BREP_WITH_VOIDS` file. The Android NDK builds of R14d set no `-ffp-contract=off`; ADR-0014 keeps
the reproducibility of the native library out of the canonical gate. Linux, macOS and Android.

### The desktop

#### V91 · Medium · The start again on the previous file of E150 does not keep a document of the unsaved folder · Confirmed by probe

- **[Observed]** `DesktopSession.Open` always gives an unsaved folder (`3DEngine/DesktopSession.cs:50-51`).
  `DocumentJournal.Unsaved` sets the path `<unsaved>/<id>.3de` at once and makes the file only at the first command
  (`Engine.Core/Persistence/DocumentJournal.cs:52-53`, `:91-94`). Thus `Session.FilePath` is never null in the desktop.
  `RunSessions` keeps that path in `previous` (`3DEngine/DesktopEngineHost.cs:81`). After a next session that does not
  start, it starts `start(args, previous)` (`:66-68`). That path becomes the setting "file", so `EngineHost` treats
  it as a named file and sets no unsaved folder (`Engine.Api.Http/EngineHost.cs:42`, `:96-99`).
- **[Observed]** A probe test in `3DEngine.ImageTests` ran `RunSessions` with no `--file`. Session 1 opened
  `second.3de`, and the fake start refused `second.3de`, as in the test of E150. The results:
  - With one box and no unsaved folder before the run: session 2 had the box, but `FileName` changed from null to
    `<id>.3de`. The document in the unsaved folder became a named document with a GUID name. Exit 0.
  - With no command and no unsaved folder: the start again failed with
    `E-IO-LOAD-FAILED: The file …\unsaved\<id>.3de cannot be made: Could not find a part of the path`. `RunSessions`
    returned 6, so the window closed. That is the result that E150 was to remove.
  - With no command and an existing unsaved folder (the normal case after the first use): session 2 had
    `FileName = <id>.3de`, the host wrote "did not exist, so the host made a new empty document file there", and an
    empty file of 67 bytes stayed in the unsaved folder.
- **[Observed]** The text "the document of the start" (`DesktopEngineHost.cs:66`) never shows, because `previous` is
  never null after a session.
- **[Inferred]** The common first use is a start with no file and an open at once. If the open then does not start
  (another program takes the file, or the fixed port is taken in the gap), the person loses the window, or gets a
  document with a GUID name in the folder of unsaved documents. No data is lost: the journal with commands opens.
- **[Recommended]** Keep the kind of the previous document, not only its path. When `FileName` was null, start again
  as an unsaved document: resume the journal when it exists, with the unsaved folder set, and else start with no file.
  Add the three probe cases above to `DesktopSessionTests` as facts: the body count, `FileName` null, and no new file.

#### V92 · Low · A window that SDL cannot create ends the desktop with an unhandled exception · Confirmed by probe

- **[Observed]** The lead of the external reviewer (v0.196) is true. `Run` makes the `Window` in the lambda of
  `RunSessions` (`3DEngine/DesktopEngineHost.cs:16-24`), outside the `try` of `Show` (`:95-145`). The constructor
  throws `Exception("SDL: failed to create window")` when `SDL_CreateWindow` gives null (`3DEngine.Vulkan/Window.cs:68-72`).
  `RunSessions` and `Program.Main` catch nothing (`3DEngine/Program.cs`).
- **[Observed]** A probe called `RunSessions` with a show that throws that exception. The exception left
  `RunSessions` (`Record.Exception` gave it). The `using` blocks disposed the surface and the session.
- **[Observed]** The change came with R14b (`a4c145f`, TASK-0194). Before it, `new Window` was inside the `try`, and the
  desktop wrote `Error: …` and ended. The review of `ab55b07` did not find it.
- **[Inferred]** A computer with no Vulkan loader or no display gets a stack trace and the exit code of the runtime, not
  a message. No document is lost. Also, `Show` catches each other failure, writes it on the standard output, and the
  run then returns 0 (`:142-145`, `:85-86`), so a device that does not start gives the exit code 0.
- **[Recommended]** Make the window inside a `try` that writes the message on the standard error and returns a new exit
  code (for example "the window did not open"), registered in the exit codes of `DesktopSurface`. Give a failure of
  `Show` a code that is not 0. Test: `RunSessions` with a show that throws gives that code and no exception.

#### V93 · Low · The flush of V90 runs before the new device, so the input of that time still reaches the first frame · Confirmed by reading

- **[Observed]** `Window.FlushInput` runs in the lambda before `Show` (`3DEngine/DesktopEngineHost.cs:22-23`). `Show`
  then makes `NativeThreeDEngine`, and `Initialize` makes the `GraphicsDevice`, the swapchain, three renderers, the font
  atlas and the session projections (`3DEngine.App/NativeThreeDEngine.cs:100-116`). `LoadScene` and `window.Show()`
  follow. Only then does the first `SDL_PollEvent` run.
- **[Observed]** The comment of `InputRanges` says that the gap holds "a new device" and that the flush comes "before
  its first frame" (`3DEngine.Vulkan/Window.cs:119-122`). A probe made a Vulkan device with validation and no window
  three times: 106 ms, 49 ms and 47 ms. The device of the window also makes a swapchain.
- **[Inferred]** A click or a key of the time of `Initialize` (about 0.05 s to 0.2 s) still goes to the first frame of
  the next session. The fix of V90 covers the stop of the surface and the replay, not the new device. The comment is
  partly false.
- **[Recommended]** Call `Window.FlushInput()` after `engine.Initialize()` and `engine.LoadScene(scene)`, before the
  loop, for a later session only, or correct the comment. A test of V94 can hold the order.

#### V94 · Low · No test fails when the flush of V90 is removed · Confirmed by injection

- **[Observed]** The new theory `A_Later_Session_Flushes_Each_Input_Event_That_SdlInput_Reads`
  (`3DEngine.Tests/SdlInputTests.cs:261-280`, method at `:275`) reads the table `InputRanges` only. Injection: the body of `FlushInput`
  lost its `SDL_FlushEvents` loop, and `DesktopEngineHost.Run` lost its call of `Window.FlushInput()`.
  `dotnet test 3DEngine.Tests` gave 332 passed, and `dotnet test 3DEngine.ImageTests` gave 204 passed (with 4 probes).
- **[Inferred]** A later change can remove the flush or move it, and no test sees it.
- **[Recommended]** Add a test with no window: `SDL_Init(SDL_INIT_EVENTS)`, push a key, a button and a quit event with
  `SDL_PushEvent`, call `FlushInput`, and assert that only the quit stays (`SDL_PeepEvents`). For the call site, give
  `RunSessions` a hook for "a later session starts", or a test that reads the order of the calls.

#### V95 · Low · Two file actions of the application do not go through IDocumentFiles · Confirmed by reading

- **[Observed]** ADR-0072 item 3 says that `IDocumentFiles` gives Save, Open and Export a path. The placement of an
  image reads a path that the person types, with `File.Exists`, `FileInfo` and `File.ReadAllBytes`
  (`3DEngine.App/ImageActions.cs:150-168`). The STL export writes with `File.WriteAllBytes` on the path of `Resolve`
  (`3DEngine.App/NativeThreeDEngine.cs:435`).
- **[Inferred]** The ADR says that the phone gives the picker of Android. A picker gives a content URI and a stream, not
  a path of the file system, so `IDocumentFiles` with a string path and these two direct calls do not fit it. The
  consequence "an Android program can reference the whole application" holds for the build, not for these actions.
- **[Recommended]** In R14e, before the picker slice, decide in ADR-0072 how the phone reads an image and writes an
  export: a stream through `IDocumentFiles`, or a host that gives a path in the private storage of the application.
  Name the image placement in item 3.

#### V96 · Low · ADR-0072 gives a wrong count of the test projects that name the application · Confirmed by reading

- **[Observed]** ADR-0072, Consequences, says "Four test projects and the program now name the application". Only
  `3DEngine/3DEngine.csproj` references `3DEngine.App`. Outside `docs/` and `tasks/`, the name occurs in four files of
  one test project (`Engine.Tests`: `EventSinkContractTests.cs`, `BuiltAssemblyReferenceTests.cs`,
  `DependencyDirectionGateTests.cs`, `UiLibraryGateTests.cs`) and in `3DEngine.sln`. `3DEngine.ImageTests` uses its types
  through `3DEngine`. The other count of the record, "fifteen ADRs", is true: sixteen ADRs name `3DEngine.App` in
  their front matter, ADR-0072 included.
- **[Recommended]** Correct the text to "four test files of `Engine.Tests`" in a later task, with a History line.

**No finding.** **[Observed]** The move of R14e1 changes no behaviour that a probe found: 22 files moved, 17 with no
change and 5 with the type of the host only. `HostedSession` gives the same session, name and events as `EngineHost`,
and `DesktopDocument` forwards `Resolve` and `Open` to its folder. The fix of E149 holds: the injected race failed its
test five times of five. **V88 is still true:** a build of `3DEngine.csproj` into a new folder gave native files of
`win-x86`, `linux-x86` and `android-x86`.

**Not verified.** No session change was seen in a window, because nobody started the desktop. Linux, macOS and Android.
A lead for R14e2: the `Window` constructor calls `SDL_StartTextInput`, and on Android SDL can show the screen keyboard
for that call.

### The gates and the tests

#### T145 · Medium · The rule of R14e1 reads FrameworkReference items only, so the web SDK or a FrameworkReference in Engine.Core gives the application ASP.NET Core, and each gate passes · Confirmed by injection

- **[Observed]** `The_Application_References_No_Surface` (`Engine.Tests/Governance/DependencyDirectionGateTests.cs:1045`
  to `1051`) checks the direct project references of `3DEngine.App` and the `FrameworkReference` items of the files
  that MSBuild reads for `3DEngine.App` only. `PermittedSdks` (`DependencyDirectionGateTests.cs:696`) permits
  `Microsoft.NET.Sdk.Web` for each project. No gate refuses a `FrameworkReference` in `Engine.Core`, `Engine.Contracts`
  or the render side. A FrameworkReference flows to each project that references the project.
- **[Observed]** Injection 1: `3DEngine.App/3DEngine.App.csproj` with `Sdk="Microsoft.NET.Sdk.Web"` and
  `<OutputType>Library</OutputType>`. The solution built with 0 errors and 0 warnings, and
  `3DEngine.App/obj/project.assets.json` listed `Microsoft.AspNetCore.App` in its `frameworkReferences`.
  `dotnet test 3DEngine.sln --no-build` passed 1331, 332, 200 and 3 tests, 0 failed. `eng/repo-gates` gave "443 runs of
  155 checks, 0 failed".
- **[Observed]** Injection 2: `<FrameworkReference Include="Microsoft.AspNetCore.App" />` in `Engine.Core/Engine.Core.csproj`.
  The restore of `3DEngine.App` listed `Microsoft.AspNetCore.App` as a framework reference of `Engine.Core/1.0.0`. The
  solution built, `dotnet test 3DEngine.sln --no-build` passed 1332, 332, 200 and 3, 0 failed, and `eng/repo-gates`
  gave 0 failed. `BuiltAssemblyReferenceTests` does not see it, because the metadata of a built assembly holds only the
  assemblies that the code uses.
- **[Observed]** `.github/workflows/ci.yml` builds no Android target of the application. Only the native workflows
  (`build-manifold-native.yml`, `build-cimgui-native.yml`) name Android.
- **[Inferred]** ADR-0072 item 1 exists so that an Android program can reference the application (NETSDK1082). Both
  forms break that build, and no step of the pipeline finds it before R14e2.
- **[Recommended]** Make the rule read the SDK of each file that MSBuild reads for the application and for each
  project that it reaches, and refuse `Microsoft.NET.Sdk.Web` and each `FrameworkReference` there (the transitive
  closure of `Graph()`, not the direct references). Add one theory row for each injection above. A stronger form: a
  pipeline step that builds `3DEngine.App` for `net10.0-android` when R14e2 adds the workload.

#### T146 · Low · The package list of the desktop names four projects by hand, so a new project of the desktop has no package rule · Confirmed by reading

- **[Observed]** `Each_Project_Of_The_Desktop_Names_Only_The_Packages_Of_Its_List`
  (`DependencyDirectionGateTests.cs:1266`) is a theory with four `InlineData` rows: `3DEngine`, `3DEngine.App`,
  `3DEngine.Vulkan` and `3DEngine.Core`. No test checks that each project of `RenderSide`, `HostsThatDraw` and
  `Applications` has a row.
- **[Inferred]** The next host that draws (the phone program of R14e2) enters `HostsThatDraw`, and a second binding of
  SDL in it passes, which is the form of T144 again.
- **[Recommended]** Give the list as a dictionary from project to packages, and add a fact that the keys equal
  `[.. RenderSide, .. HostsThatDraw, .. Applications]`. Prove it with a project in `HostsThatDraw` that has no key.

**No finding.** **[Observed]** `BuiltAssemblyReferenceTests` reads the render assemblies from their own output folders,
and the pipeline builds the solution before the tests. The package rules read `Update`.

**Not verified.** The pipeline on Ubuntu and macOS for the injections of T145.

## 4 The next planned task

The next product phase is R14e2 (TASK-0226), the Android program: a `net10.0-android` project, an activity that
subclasses `SDLActivity`, a session of its own, and the APK. No finding is critical or high, so no correction comes
before R14e2. By `docs/templates.md`, section 7, the medium and the low findings go after it: P0.89 (TASK-0224) holds
E152 to E155, T145 and T146, and P0.90 (TASK-0225) holds V91 to V96. V88 goes into R14e2 with the runtime identifiers
of the Android project.

## 5 Research for that task

**[Observed]** T145 touches the work of R14e2: a framework reference that reaches the application through a project or
an SDK breaks the build of the APK. R14e2 builds the APK, so its build finds such a reference, but no step of the
pipeline builds it before R14f. **[Observed]** The probe APK of this review holds no native library of cimgui or
Manifold, so R14e2 takes the packages of R14d. **[Inferred]** V95 does not stop R14e2, because the picker comes in a
later slice; that slice decides how the phone reads an image and writes an export.

## 6 Recommended approach

**[Recommended]** In this order:

1. R14e2 (TASK-0226): the Android program, with the runtime identifiers `android-arm64` and `android-x64` only (V88).
2. P0.89 (TASK-0224): E152 to E155, T145 and T146, each with a test or an injection before the change. 2 to 3 agent
   hours.
3. P0.90 (TASK-0225): V91 to V96, each with a test or an injection before the change. 1 to 2 agent hours.
4. The next slices of R14e.

## Questions for the owner

The standing order of 2026-11-05 answers each question with the recommended option. The owner checks these answers on
2026-11-08.

**Q1. An older engine and an assembly file (E154).**
- A: The header of an assembly has `formatVersion` 2, so an older engine refuses each assembly at its header. A part
  file keeps its bytes, and an assembly file of version 1 still opens.
- B: The format stays, and ADR-0071 item 4 says that an older engine opens an assembly with no reference as an empty
  part.

**Recommended: A.** An older engine that writes part entries after an assembly header makes a file that the current
engine refuses; a version of the header stops that before a write.

**Q2. The start again after a next session that does not start (V91).**
- A: The host keeps the kind of the previous document: a named file opens again by its path, and a document of the
  unsaved folder starts again as an unsaved document with its journal.
- B: The host starts again with no file, so a document of the unsaved folder with commands is not shown, and it stays
  in its folder.

**Recommended: A.** It keeps ADR-0030 item 5 for each document, and the person sees the same work again.
