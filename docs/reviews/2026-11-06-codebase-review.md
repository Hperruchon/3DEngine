---
date: 2026-11-06
commit: ab55b07
ledger: v0.198
previous: 2026-11-05-b-codebase-review.md
---

# Codebase review — 2026-11-06

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the twenty-third codebase review. The review of 2026-11-05 (b) examined v0.190. After it came 4 product
milestones (R14a, R14b, R14c and K6a) and 8 ledger entries (v0.191 to v0.198). The next product milestone, K6b, would be
the fifth, so the gate makes this review due before it. `Engine.Tests/Governance/CodebaseReviewGateTests.cs` holds the
rule, and `docs/templates.md`, section 7, gives the form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is the
authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this file does not.

| Item | Value |
|---|---|
| Code state | `main` at `ab55b07`, the merge of v0.198; the agents examined `2c77991`, whose tree the merge holds |
| Branch of this review | `codebase-review-2026-11-06` |
| Tests | 1,818 on this computer (1,297 in `Engine.Tests`, 320 in `3DEngine.Tests`, 198 in `3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`) |
| Pipeline | The run of the branch `k6a-integration-3` (42131328887): green on Ubuntu, Windows and macOS, and the three determinism hashes are equal |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner decides.

**Method.** Nine tasks changed code or records after `d9292fa`, the commit of the last review: TASK-0189 (the review),
TASK-0190 (P0.82), TASK-0193 (R14a), TASK-0191 (P0.83), TASK-0192 (P0.84), TASK-0194 (R14b), TASK-0195 (R14c) and
TASK-0200 (K6a, from the engine lane of the order of 2026-11-05). Two review agents worked in parallel at `2c77991`, each
one in its own git worktree: the engine with the hosts, and the desktop with the gates. Each agent wrote probes and
injections that only its worktree held, and restored or deleted them. I read the cited lines of the medium findings
E142 and E146 and of E149 and E151, and I measured the values below. The reviews of Codex of v0.190 to v0.193 give one
finding, the form of V85 that P0.83 fixed.

## Summary

- **The fixes of the last review hold for their forms.** E134 to E141, V85 to V87 and T136 to T140 are fixed for their
  forms, and V25 is fixed for its parts by the binding of R14c. A probe found the next form of E137 to E139, V85, T136,
  T137 and T139. **[Observed]**
- **No new finding is critical or high.** **[Observed]**
- **Four new medium findings in the owned kernel and the STEP export.** A hole that touches its region at its first
  vertex has no owner on a face of two regions, so `Brep.Of` throws, `GetFaces` throws and the export refuses the body
  (E142). The export writes a solid pinched along an edge as manifold (E143), writes a sliver whose vertices round to
  one double (E146), and its reader in the tests checks no sense of a loop and no direction of a line (E147).
  **[Observed]** by probes.
- **Thirteen low findings:** E144, E145 and E148 to E151 in the kernel, the export and the session of the desktop; V88
  to V90 in the desktop; T141 to T144 in the gates. **[Observed]**
- **Open findings: 0 critical, 4 high, 18 medium and 29 low.** **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 16 | `.csproj` lines in `3DEngine.sln`. The last review gave 16. |
| Production source lines | 28,011 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests`, `3DEngine.ImageTests` and `3DEngine.VisionTests`, 264 files. The last review gave 27,532 in 262 files. |
| Test source lines | 33,111 | `Engine.Tests` (23,956 in 115 files), `3DEngine.Tests` (3,519 in 22 files), `3DEngine.ImageTests` (5,571 in 34 files) and `3DEngine.VisionTests` (65 in 1 file). The last review gave 32,034. |
| Tests | 1,818 | `dotnet test 3DEngine.sln`: 1,297, 320, 198 and 3 passed, 0 failed, 0 skipped. The last review gave 1,767. |
| Gate classes | 19 | Tracked files whose name ends in `GateTests.cs`. `eng/repo-gates` gives 438 runs of 153 checks. The last review gave 19, and 424 runs. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 14 | Section "Open" of `docs/register.md`; the limit is 15. The last review gave 14; R-0054 was opened and R-0020 closed. |
| ADRs | 62 | `docs/adr/0001` to `0061` and `0070`, with 0003 and 0011 in `docs/adr/archive/`. In force: 60 (27 accepted, 33 amended). None is proposed. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 4 | The same method: V2, V3, T4, T31. The last review gave 5; E134 is fixed. |
| Findings: medium | 18 | The same method: E5, E6, E12, E13, E19, E21, E22, E142, E143, E146, E147, V4, T2, T5, T10, T17, T32, T37. The last review gave 18; E135, V85, V86 and T139 are fixed. |
| Findings: low | 29 | The same method: E17, E24, E34, E39, E57, E84, E144, E145, E148 to E151, V7, V15, V29, V84, V88 to V90, T7, T13, T23, T25, T141 to T144, C1, P1. The last review gave 28; E136 to E141, V25, V87, T136 to T138 and T140 are fixed. |
| Lines in the rule files | 2,828 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The last review gave 2,779. |
| Sentences with "must" | 58 | `grep -o -w -i must` on the same ten files. The last review gave 58. |
| Sentences with "do not" | 54 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 54. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found. E13, T32, E84 and V84 are fixed in part and count as open. A finding that the owner accepted counts as closed.

## Findings of the previous review

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. No change: `Engine.Api.Http/Endpoints/JsonParameters.cs` has no commit since `d9292fa`. Register entry R-0027.
- **E6** — Open. No change: `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` has no commit since `d9292fa`. Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. No change.
- **E8** — Fixed. TASK-0044, v0.37. No change.
- **E9** — Fixed, and it holds. No change.
- **E10** — Fixed. TASK-0035, v0.43. No change.
- **E11** — Fixed. TASK-0115, v0.115. No change.
- **E12** — Open, and smaller. No change: `Engine.Core/Hosting/ParameterBinder.cs` has no commit since `d9292fa`. Register entry R-0027.
- **E13** — Fixed in part. No change: `QueriesEndpoint.cs` has no commit, and the change of `Engine.Cli/Cli.cs` touches the export only.
- **E14** — Fixed. TASK-0115, v0.115. No change.
- **E15** — Fixed. TASK-0044, v0.37. No change.
- **E16** — Fixed. TASK-0115, v0.115. No change.
- **E17** — Open, and smaller. No change: `Engine.Core/CommandBus.cs` has no commit since `d9292fa`. A query handler that throws also leaves `QueryBus` (E142). Register entry R-0028.
- **E18** — Fixed for the sinks of `Engine.*` (TASK-0140, v0.141) and of the desktop host (T118, TASK-0150, v0.154). Correction: the line of the last review lost the end of its code span. No change.
- **E19** — Open, and smaller. No change: `CommandBus.cs`, `DocumentSession.cs` and `EngineHost.cs` have no commit; the kept solids of a closed session stay until 64 newer ones push them out, a bounded memory. Register entry R-0033.
- **E20** — Fixed, and it holds. No change.
- **E21** — Open. No change: `SubtractCommandHandler.cs` has no commit since `d9292fa`. Register entry R-0033.
- **E22** — Open. No change: `Engine.Core/CommandBus.cs` has no commit since `d9292fa`. Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. No change.
- **E24** — Open in seven parts of nine. No change: `WireMessage.cs` and `SchemaEventsEndpoint.cs` have no commit since `d9292fa`. Register entry R-0033.
- **E25** — Accepted by the owner. TASK-0142, v0.144. Register entry R-0036. No change.
- **E26** — Fixed. TASK-0135, v0.136. No change.
- **E27** — Accepted by the owner. TASK-0142, v0.144. Register entry R-0036. No change.
- **E28** — Fixed. TASK-0136, v0.137. No change.
- **E29** — Fixed for the replay. TASK-0135, v0.136. No change.
- **E30** — Fixed. TASK-0136, v0.137. No change.
- **E31** — Fixed for the replay. TASK-0137, v0.138. No change.
- **E32** — Fixed. TASK-0135, v0.136. No change.
- **E33** — Fixed with E18. TASK-0140, v0.141. No change.
- **E34** — Open. No change: `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs` has no commit since `d9292fa`. Register entry R-0036.
- **E35** — Fixed for the routes of `MapGet`. TASK-0139, v0.140. No change.
- **E36** — Fixed. TASK-0079, v0.85. No change.
- **E37** — Fixed. TASK-0137, v0.138. No change.
- **E38** — Fixed. TASK-0138, v0.139. No change.
- **E39** — Open. The choice of the backend of the desktop moved from `DesktopSurface.cs` to `DesktopSession.cs:45-57` with the same code; `Cli.cs` and `ServerEngine.cs` keep theirs. Register entry R-0036.
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
- **E57** — Open. No change: `Engine.Tests/Http/DesktopSurfaceTests.cs` has no commit since `d9292fa`. R14b made more texts false (E151). Register entry R-0039.
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
- **V2** — Open. `Swapchain.cs:149` still has `currentExtent.width > 0`; `OverlayRenderer.cs` and `Swapchain.cs` have no commit since `d9292fa`. Register entry R-0022.
- **V3** — Open. No portability extension in `3DEngine.Vulkan`; `GraphicsDevice.cs` changed only for the list of instance extensions (R14c). Register entry R-0022.
- **V4** — Open. No ledger entry from v0.191 to v0.198 names it. Register entry R-0027.
- **V5** — Fixed. TASK-0076, v0.74. No change.
- **V6** — Fixed. TASK-0076, v0.74. No change.
- **V7** — Open. No change: `Ui/UiRenderer.cs`, `Ui/UiTexture.cs` and `GpuBuffer.cs` have no commit since `d9292fa`. Register entry R-0030.
- **V8** — Fixed. TASK-0074, v0.75. No change.
- **V9** — Fixed. TASK-0074, v0.75. No change.
- **V10** — Fixed. TASK-0074, v0.75. No change.
- **V11** — Fixed in part. TASK-0074, v0.75. No change.
- **V12** — Fixed, and changed by decision. No change.
- **V13** — Fixed for its parts. No change.
- **V14** — Fixed. TASK-0074, v0.75. No change.
- **V15** — Open. `SessionBodies.cs:275` still waits for the idle device for each removed mesh; the only change is `RestBoxOf`.
- **V16** — Fixed. TASK-0079, v0.85. No change.
- **V17** — Fixed. TASK-0079, v0.85. No change.
- **V18** — Fixed for its forms. TASK-0090, v0.93. No change.
- **V19** — Fixed, with no injection. TASK-0079, v0.85. No change.
- **V20** — Fixed. TASK-0079, v0.85. No change.
- **V21** — Fixed by reading, with no test. TASK-0079, v0.85. No change.
- **V22** — Fixed for its forms. TASK-0079, v0.85. No change.
- **V23** — Fixed. TASK-0079, v0.85. No change.
- **V24** — Fixed. TASK-0079, v0.85. No change.
- **V25** — Fixed for its parts. TASK-0195, v0.197: `ppy.SDL3-CS` ships `linux-arm64` and its native SDL is 3.5.0, not the preview 3.1.3; that SDL is a snapshot of development (R-0054). R-0020 is closed.
- **T1** — Fixed. TASK-0156, v0.158. Each run since then compares the three hashes. No change.
- **T2** — Open in one part of five. No ledger entry from v0.191 to v0.198 names it. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. No change.
- **T4** — Open. No change: `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs` has no commit since `d9292fa`. Register entry R-0031.
- **T5** — Open in one part of three. No ledger entry from v0.191 to v0.198 names it. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Open in four parts of five. No ledger entry from v0.191 to v0.198 names it. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. No change.
- **T10** — Open. No ledger entry from v0.191 to v0.198 names it. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51. No change.
- **T12** — Fixed. TASK-0059, v0.55. No change.
- **T13** — Open in part. No change: `.github/workflows/ci.yml` has no commit since `d9292fa`. Register entry R-0031.
- **T14** — Fixed. TASK-0053, v0.51. No change.
- **T15** — Fixed. TASK-0050, v0.47. No change.
- **T16** — Fixed. TASK-0066, v0.67. No change.
- **T17** — Open. No change: `NativePackageGateTests.cs` and `nuget.config` have no commit since `d9292fa`. Register entry R-0031.
- **T18** — Fixed. TASK-0049, v0.46. No change.
- **T19** — Fixed. TASK-0049, v0.46. No change.
- **T20** — Fixed. TASK-0049, v0.46. No change.
- **T21** — Fixed. TASK-0049, v0.46. No change.
- **T22** — Fixed. TASK-0049, v0.46. No change.
- **T23** — Open. No change: `build.ps1` has no commit since `d9292fa`. Register entry R-0031.
- **T24** — Fixed. No change.
- **T25** — Open in part. No change: `eng/write-set-check` has no commit since `d9292fa`. Register entry R-0031.
- **T26** — Fixed. TASK-0057, v0.54. No change.
- **T27** — Fixed. TASK-0058, v0.56. No change.
- **T28** — Fixed. No change.
- **T29** — Fixed. TASK-0057, v0.54. No change.
- **T30** — Fixed. TASK-0057, v0.54. No change.
- **T31** — Open. No change: `RepositoryFiles.cs` and `eng/repo-gates/RepoGates.csproj` have no commit since `d9292fa`. Register entry R-0031.
- **T32** — Fixed in part. No change: `eng/write-set-check/Program.cs` has no commit since `d9292fa`.
- **T33** — Fixed. TASK-0057, v0.54. No change.
- **T34** — Fixed. TASK-0057, v0.54. No change.
- **T35** — Fixed. TASK-0057, v0.54. No change.
- **C1** — Open. No change: `IBRepOps.cs` and `IFeatureIdMap.cs` have no commit; the only change of `Engine.Contracts` is `Schema/FieldSchema.cs`. Register entry R-0027.
- **P1** — Open, and smaller. V87 removes the query in each frame of a drag; `SceneBox` makes one small array for each image in each frame. Register entry R-0027.
- **T36** — Fixed. TASK-0061, v0.61. No change.
- **T37** — Open in part. No change: `WriteSetRangeTests.cs` has no commit since `d9292fa`. Register entry R-0031.
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
- **V29** — Open. No check by touch from v0.191 to v0.198, and the register has no entry for it.
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
- **E84** — Fixed in part. TASK-0099, v0.102. No change: `DocumentJournal.cs` has no commit since `d9292fa`.
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
- **V84** — Fixed in part. No change: `NativeThreeDEngine.cs:165` and `FaceArm.cs` keep the call of v0.183.
- **T133** — Fixed for its forms. TASK-0182, v0.184. No change.
- **T134** — Fixed for its forms. TASK-0182, v0.184. No change.
- **T135** — Fixed for its forms. TASK-0182, v0.184. No change.
- **E134** — Fixed for its forms. TASK-0190, v0.192 (`CreateDatumCommandHandler.cs:95-108`); a replay sets the entry too. No other handler refuses a datum with a fault. **[Observed]**
- **E135** — Fixed. TASK-0192, v0.195: the key is the identity of the Document and the geometry key (`KernelProjection.cs:54`). **[Observed]**
- **E136** — Fixed. TASK-0192, v0.195: 40,000 random quotients and products near the smallest normal double agree with IEEE 754. **[Observed]**
- **E137** — Fixed for its form. TASK-0192, v0.195: two holes and three holes at one vertex keep each hole. The next forms are E143 and E144. **[Observed]**
- **E138** — Fixed for its form. TASK-0192, v0.195. The next form is E142. **[Observed]**
- **E139** — Fixed for its form. TASK-0192, v0.195. The next form is E148. **[Observed]**
- **E140** — Fixed. TASK-0192, v0.195: an added member of `FieldSchema` fails the test of the converter. **[Observed]**
- **E141** — Fixed. TASK-0192, v0.195: `GetFaceAt` declares `length` and `direction`, and the gate reads the queries. **[Observed]**
- **V85** — Fixed for its form on Windows. TASK-0191, v0.194: the hook of `Window.cs` corrects a stale press, and `SdlInput` keeps the place of the event. The next form is V89. **[Observed]**
- **V86** — Fixed. TASK-0191, v0.194: `SceneRenderer.SceneBox` holds the images, with a test on a GPU. **[Observed]**
- **V87** — Fixed. TASK-0191, v0.194: `NativeThreeDEngine.SizeOf` keys the size on the box with no offset. **[Observed]**
- **T136** — Fixed for its form on the side of the engine. TASK-0192, v0.195. The next forms are T141 and T142. **[Observed]**
- **T137** — Fixed for its six forms. TASK-0192, v0.195. The next forms are T143. **[Observed]**
- **T138** — Fixed for its forms. TASK-0192, v0.195: a heading element anywhere on a line fails. **[Observed]**
- **T139** — Fixed for its form. TASK-0192, v0.195. The next form is T142. **[Observed]**
- **T140** — Fixed. TASK-0192, v0.195: the rule reads integer parameters and the parameters of queries. **[Observed]**

## 1 Current architecture

**[Observed]** The desktop host owns its document session (`DesktopSession`), and the HTTP and WebSocket surface mounts
on it with the second form of `AddEngineSurface`. The sessions run one after the other on one window, so an open of a
file stays in the same process (R14b). The SDL binding is `ppy.SDL3-CS` on each platform (R14c, ADR-0061 item 5), and a
hook of the Windows messages corrects a press that SDL gives at an old place (P0.83).

**[Observed]** The engine writes the planar B-rep of the owned kernel of each live body as a STEP file of AP214
(`StepExport`, ADR-0070, K6a), from `GET /export/step` and `engine export step`. The charter names Android as a platform
(R14a, ADR-0061). A second session in its own worktree, the engine lane, delivers slices that the main session merges.

## 2 Patterns in use

- **A rule that the kernel proves on one form breaks on the next form at the boundary.** The owner of a hole reads one
  vertex, which can lie on the boundary (E142); the walk of the loops keeps a hole apart and joins two regions that meet
  at a vertex (E144). **[Observed]**
- **An export trusts the validity of the kernel, and the kernel counts coedges only.** A pinched solid, a sliver and an
  edge of four faces each pass or fail with a false word (E143, E145, E146). **[Observed]**
- **A test that reads a file back checks the counts and not the senses** (E147). **[Observed]**
- **Each correction of a gate holds for its forms, and a probe finds the next form.** T141 and T142 follow T136, T143
  follows T137, and T144 follows the pin rule of R14c. **[Observed]**
- **A change of the flow of the desktop leaves the texts of the old flow** (E151). **[Observed]**

## 3 Review of the code

### The engine and the hosts

#### E142 · Medium · A hole that touches its region at its first vertex has no owner on a face of two regions, and Brep.Of throws · Confirmed

- **[Observed]** `Engine.Kernel/Brep.cs:266` tests only the first vertex of the hole, with a ray in +U and the strict test
  `x > point.U` (`:278`). When that vertex lies on the side of its own region, the count gives "outside" for each outer
  loop, `Owner` gives null, the hole goes to no face, and `FaceOf` throws "An edge has no face." (`:215`). With one outer
  loop the shortcut at `:259` hides the defect.
- **[Observed]** A plate of 20 by 10 by 2, less a slot at x = 0, less a triangle prism that touches x = 10 at (10, 0):
  two of the three orders of the vertices of the triangle throw. Through the engine each command gives Applied, then
  `GetFaces` throws and the STEP export refuses the body with "An edge has no face."
- **[Inferred]** `QueryBus` has no catch, so `POST /queries` with `GetFaces` gives HTTP 500.
- **[Recommended]** Test a point strictly inside the hole, for example the midpoint of an edge of the hole, add a test with
  each rotation of the profile, and give a reason, not an exception, when `Brep.Of` fails.

#### E143 · Medium · The STEP export writes a solid that is pinched along an edge as a MANIFOLD_SOLID_BREP · Confirmed

- **[Observed]** On the diamond of the test of E137, `Brep.Valid` is true, but the vertical edge at x = 5, y = 0 has two
  walls of the diamond only, and the one face +x of the plate has the edge inside its loop. The export wrote the file,
  and `StepReader` accepted it. A diamond that touches the four sides gives the same.
- **[Inferred]** A strict reader of ISO 10303-42 refuses a shell where a face holds another edge inside it; `Valid`
  (`Brep.cs:176`) counts coedges only.
- **[Recommended]** Refuse such a body in `StepExport.Check` with a reason, or split the face at the edge in `Brep.Of`. Q1.

#### E146 · Medium · Two vertices that differ below the precision of a double become one point, so the STEP file holds a solid of zero thickness · Confirmed

- **[Observed]** A box, a tool moved by 3 and then by −2⁻⁶⁰, and a subtract: the STEP file holds two solids and 16 vertex
  points with 12 distinct coordinates; the sliver solid has four pairs of identical points. The STL export gives one
  solid. `StepExport.Check` (`:138-143`) tests only that each coordinate is finite.
- **[Recommended]** Refuse a body when two vertices, or the two ends of an edge, round to the same double, with a test.

#### E147 · Medium · The reader of the STEP tests checks no sense of a loop and no direction of a line · Confirmed by injection

- **[Observed]** With each direction of a LINE reversed, or each EDGE_LOOP turned, only the test of the hash failed;
  the other 16 tests passed. `StepReader.Solid` and `Matches` check the closed chains, one use in each sense, the plane
  of a vertex and the normal, and not the sense of an outer loop about the normal, the direction of a LINE from its start
  to its end, two distinct VERTEX_POINT, or a loop that repeats a vertex.
- **[Recommended]** Add the four checks, each proved by these injections.

#### E144 · Low · Two regions of one face that meet at a vertex become one face whose loop passes the vertex two times · Confirmed

- **[Observed]** `FirstCounterClockwise` (`Brep.cs:349-373`) keeps a hole apart from the outer loop and joins two outer
  regions that meet at a vertex: a plate less two slanted prisms gives the cap +z as one loop of 8 vertices that repeats
  a vertex. The comment at `:182-183` says that each region is a face.
- **[Recommended]** Split a loop that repeats a vertex into two loops at that vertex, with a test.

#### E145 · Low · A B-rep with an edge of four faces is refused with the false reason "not closed" · Confirmed

- **[Observed]** Two holes that touch at one vertex, three holes at one interior vertex and two quadrants that share an
  edge give `Valid` false and "its B-Rep in the owned kernel is not closed." (`StepExport.cs:127-128`); ADR-0070 item 3
  and `docs/diagnostics.md` use the same word.
- **[Recommended]** Give "an edge has more than two faces" for that case, with a test.

#### E148 · Low · A face-gone fault goes away when a later cut moved the end of its chain before the body fault · Confirmed

- **[Observed]** `FaceGone.cs:46-51` keeps the earlier fault only when it equals the fault of the end, and the message
  names the end. A second cut keeps the old message, so a bow tie in the tool of the second cut removes the fault.
- **[Recommended]** Keep the earlier face-gone fault whatever end it names while the end cannot be built. Q2.

#### E149 · Low · Two opens at the same time are both accepted · Confirmed

- **[Observed]** `DesktopDocument.cs:34-48` reads `RequestedOpen`, awaits the check and then writes, with no lock: two
  parallel opens were both accepted in 10 of 10 runs, and the second one won.
- **[Recommended]** Take the pending open with `Interlocked.CompareExchange` before the check, with a test.

#### E150 · Low · A next session that does not start ends the desktop · Confirmed

- **[Observed]** `DesktopEngineHost.cs:46-51` returns the exit code of a failed start: a file held after the check gave
  `exit=6`, and the window closed with the first document already closed. ADR-0030 item 5 says that a file that does not
  open keeps the person in the window.
- **[Recommended]** Start a session on the previous file again and give the error. Q3.

#### E151 · Low · Texts that R14b made false · Confirmed by reading

- **[Observed]** `DesktopDocument.cs:39-41`, `DocumentFile.cs:68-75`, `DiagnosticCodes.cs:24-25`, `docs/diagnostics.md`
  (`E-API-OPEN-PENDING`) and `DesktopEngineHost.cs:35` still describe a new program or the same port in each case.
- **[Recommended]** Correct each text.

**No finding.** **[Observed]** The conversion of a rational agrees with IEEE 754 near the smallest normal double; the
reals of STEP near the limits keep the form of ISO 10303-21; the loops with two and three holes at one vertex keep each
hole; `RunSessions` with a fixed port binds the same port at once, and its order of disposal holds on each failure; ADR-0059,
ADR-0060 and ADR-0070 items 1 to 5 agree with the code, except the word of E145; the bytes of a STEP file depend on no
order of a dictionary.

**Not verified.** A reader of STEP outside the repository; `GetFaces` over HTTP; a command from the window in flight at
an open; Linux, macOS and arm64.

### The desktop

#### V88 · Low · The build output of the desktop holds native files of 32-bit x86, and no gate holds ADR-0061 item 5 · Confirmed

- **[Observed]** `3DEngine/bin/Debug/net10.0/runtimes/` holds `win-x86`, `linux-x86`, `android-x86`, `android-arm` and
  `linux-arm`. ADR-0061 item 5 says that no program ships a file of 32-bit x86 from the package, and the gate of rule 5
  reads the text of `RuntimeIdentifier` only.
- **[Inferred]** A `net10.0-android` project with no `RuntimeIdentifiers` takes a default list with `android-x86`.
- **[Recommended]** In R14e, a fact that the Android project names exactly `android-arm64;android-x64`; for the desktop,
  a gate on the output of a publish when a package exists.

#### V89 · Low · No test of Window.Correction has a place that differs on one axis only · Confirmed by injection

- **[Observed]** With `x == heldX || y == heldY` in `Window.cs:127`, each test passed.
- **[Recommended]** Add the two cases of one axis to the theory.

#### V90 · Low · The window pumps no events between two sessions · Inferred by reading

- **[Inferred]** Between two calls of `Show` (`DesktopEngineHost.cs:44-69`) nothing polls the window, for up to 2 s of the
  stop of the surface, the replay of the next file and a new Vulkan device; the first frame of the next session gets the
  input of that time, and Windows can show the window as not responding.
- **[Recommended]** Flush the events of the mouse, the keys and the fingers at the start of each later session.

**No finding.** **[Observed]** By reflection, `MSG` has the layout of Win32 on x64, `SDLBool` is one byte, the hook is a
function pointer with the convention cdecl, and the strings of `SDL_Vulkan_GetInstanceExtensions` are read at once;
`RunSessions` sets the static hook one time and destroys the old Vulkan surface before the next one.

**Not verified.** Linux, macOS and arm64; a display scale other than 1 for the hook; a pen.

### The gates and the tests

#### T141 · Low · The test of T136 reads the Engine assemblies only, so the render kernel can reference Engine.Contracts · Confirmed by injection

- **[Observed]** `AdditionalExplicitAssemblyReferences` with `Engine.Contracts.dll` in `3DEngine.Core.csproj`, and a use of
  `Engine.Contracts.CommandStatus`: the build passed, the metadata of `3DEngine.Core.dll` names Engine.Contracts, 513 of
  513 governance and diagnostics tests passed, and `eng/repo-gates` gave 0 failed. The comment of
  `BuiltAssemblyReferenceTests` says that the class is closed.
- **[Recommended]** Read `3DEngine.Core.dll` and `3DEngine.Vulkan.dll` too, or refuse the two properties in each file that
  MSBuild reads.

#### T142 · Low · Engine.Kernel can take an assembly of a third party with no PackageReference · Confirmed by injection

- **[Observed]** The same property with `Vortice.Mathematics.dll` in `Engine.Kernel.csproj`: the metadata names it, and
  each gate passed.
- **[Recommended]** For the assemblies of the path of the log, permit only the framework and the first-party assemblies
  that the graph reaches.

#### T143 · Low · The next forms of T137 hide a severity, and the rule refuses a normal search for a letter · Confirmed

- **[Observed]** An enumeration `Sev.E`, a cast `(char)69`, a conditional and a switch on a char, and an index of a
  string each gave 0 parts; `s.IndexOf('E')` gave 1 part, and the integration of K6a changed `StepExport.Real` for it.
- **[Recommended]** Make a diagnostic code a type whose instances are the static fields of the registry, so a code built
  at run time does not compile. Q4.

#### T144 · Low · The pin rule of the SDL binding reads Include only, and the rule of the old binding names one package · Confirmed by injection

- **[Observed]** A `PackageReference Update` of `ppy.SDL3-CS` in a root `Directory.Build.targets` changed the version, and a
  second binding `SDL3-CS` in the host passed each gate.
- **[Recommended]** Read `Update` in the pin rule, and a fact that the host names only the packages of a list.

**No finding.** **[Observed]** A `Compile` item from outside a project is still refused; the forms of T138 and T140 found
nothing new.

**Not verified.** Linux, macOS and arm64; the pipeline at the commit of the agents.

## 4 The next planned task

The next product phase is K6b (TASK-0201), the voids of the STEP export, from the engine lane; its merge waits for this
review. Then P8b1 (TASK-0202) and R14d (TASK-0196), whose native builds run on its branch now. No finding is critical or
high, so no correction comes before K6b. By `docs/templates.md`, section 7, the medium and the low findings go after it:
P0.85 (TASK-0197) holds E142 to E151, and P0.86 (TASK-0198) holds V88 to V90 and T141 to T144.

## 5 Research for that task

**[Observed]** E142 and E143 are in the code that K6b extends: K6b reads the shells of `Brep.Of` for its voids, so a
body of E142 also throws in K6b. P0.85 fixes the owner of a hole before a later slice of K6 reads more faces.
**[Inferred]** E147 lowers the confidence of each later test of the export, so P0.85 adds the four checks first.

## 6 Recommended approach

**[Recommended]** In this order:

1. K6b (TASK-0201), then P8b1 (TASK-0202), each at its merge.
2. P0.85 (TASK-0197): E142 to E151, each with a test or an injection before the change. 2 to 3 agent hours.
3. P0.86 (TASK-0198): V88 to V90 and T141 to T144, each with a test or an injection before the change. 1 to 2 agent hours.
4. R14d and R14e of the Android order.

## Questions for the owner

The standing order of 2026-11-05 answers each question with the recommended option. The owner checks these answers on
2026-11-08.

**Q1. A solid that is pinched along an edge (E143).**
- A: `StepExport.Check` refuses it with a reason.
- B: `Brep.Of` splits the face at the edge, so the body is not manifold and the export refuses it as for E145.

**Recommended: A.** It is local to the export and gives a reason now; B changes the faces that `GetFaces` gives.

**Q2. The face-gone fault of a datum while the end cannot be built (E148).**
- A: The datum keeps its earlier face-gone fault, whatever end the fault names.
- B: The fault records the end in its data, and the check compares the face only.

**Recommended: A.** It needs no change of the record of a fault.

**Q3. A next session that does not start (E150).**
- A: The host starts a session on the previous file again and gives the error.
- B: ADR-0061 item 9 says that the desktop ends.

**Recommended: A.** It keeps ADR-0030 item 5: a file that does not open leaves the person with the window.

**Q4. The diagnostic codes built at run time (T143).**
- A: A code is a type whose instances are the static fields of the registry.
- B: The scanner stays, and the register records the forms that it misses.

**Recommended: A.** Each correction of the scanner found a next form, and a type stops the class.
