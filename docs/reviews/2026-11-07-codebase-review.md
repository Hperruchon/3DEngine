---
date: 2026-11-07
commit: 86517c8
ledger: v0.243
previous: 2026-11-06-e-codebase-review.md
---

# Codebase review — 2026-11-07

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

The review examines `main` at `86517c8`, the merge of v0.243. Four product milestones follow the review of v0.233: R16b2
(v0.236), R16c (v0.241), P9a (v0.242) and P9b (v0.243), with ten ledger entries; the next product milestone, P9c, would
make the review late. Three agents examined three areas at the same tree, and the session checked their findings and set
two grades. No review draft of Codex exists for v0.233 (the rule of the owner of 2026-11-07); the review of Codex of v0.237
gave three leads, and E168, E169 and T165 confirm them.

## Summary

- **The fixes of the last review hold.** E165, E166, V112, V113, V115 to V117, T160, T161 and T163 are fixed; E164, V114,
  V118, T162 and T164 are fixed for their forms, and E168, V123, V122, T165 and T166, and T167 hold the forms that a probe
  found after the fix. **[Observed]**
- **One new finding is high.** The check of a document file before an open makes its scratch session with no store of the
  assets, so the desktop refuses each saved document with a mesh body: the recent files of the home screen of ADR-0083, an
  open by name, and an open by an agent (E167). The agent graded it medium; the session grades it high, because it breaks
  ADR-0083 item 2 and the home screen of D6, which the next product phase P9c ships. **[Observed]** by a probe.
- **One new finding is medium.** Three projects that the phone reaches do not treat warnings as errors, and the pipeline
  counts no warning, so a call that Android does not have gives a warning and each gate passes: the failure class of
  TASK-0229 (T166). The agent graded it low; the session grades it medium, because the emulator found that failure at run
  time. **[Observed]** by injection.
- **Fifteen new low findings:** E168 to E171 in the persistence, a test of the faces, the STL reader and the command line;
  V119 to V123 in the value chips, the tool ring and the panel; T165 and T167 in a gate of the platforms and the affects
  fields. **[Observed]**
- **The demonstration holds in the pipeline.** Windows, Linux, macOS and the emulator give one hash, 06d832c3...4ea7, with
  the export and the import of the bracket (criterion 10 of the page of the demonstration). **[Observed]** from the API.
- **The run of `main` at v0.243 failed, and the session added V124 (high).** Android destroyed the activity a second
  time, 3.6 seconds after the first frame of the new session, and that session ended with `ErrorSurfaceLostKHR`.
  **[Observed]** from the annotations of run 42323457861.
- **Open findings: 0 critical, 6 high, 15 medium and 30 low.** **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 17 | `.csproj` lines in `3DEngine.sln`. The last review gave 17; `3DEngine.Android` is outside the solution. |
| Production source lines | 31,521 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests`, `3DEngine.ImageTests` and `3DEngine.VisionTests`, 296 files. The last review gave 30,368 in 287 files. |
| Test source lines | 36,758 | `Engine.Tests` (25,829 in 119 files), `3DEngine.Tests` (3,616 in 22 files), `3DEngine.ImageTests` (7,248 in 47 files) and `3DEngine.VisionTests` (65 in 1 file). The last review gave 35,567. |
| Tests | 2,014 | `dotnet test 3DEngine.sln`: 1,405, 337, 269 and 3 passed, 0 failed, 0 skipped. The last review gave 1,967. |
| Gate classes | 19 | Tracked files whose name ends in `GateTests.cs`. `eng/repo-gates` gives 468 runs of 161 checks. The last review gave 19, and 468 runs of 161 checks. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 15 | Section "Open" of `docs/register.md`; the limit is 15. The last review gave 15. |
| ADRs | 75 | `docs/adr/0001` to `0061` and `0070` to `0083`, with 0003 and 0011 in `docs/adr/archive/`. In force: 73 (33 accepted, 40 amended). None is proposed. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 6 | The same method: V2, V3, T4, T31, E167, V124. The last review gave 5; V112 is fixed. |
| Findings: medium | 15 | The same method: E5, E6, E12, E13, E19, E21, E22, V4, T2, T5, T10, T17, T32, T37, T166. The last review gave 14. |
| Findings: low | 30 | The same method: E17, E24, E34, E39, E57, E84, E160, E168 to E171, V7, V15, V29, V84, V88, V119 to V123, T7, T13, T23, T25, T143, T165, T167, C1, P1. The last review gave 33; E164 to E166, V113 to V118 and T160 to T164 are fixed or fixed for their forms. |
| Lines in the rule files | 2,960 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The last review gave 2,927. |
| Sentences with "must" | 59 | `grep -o -w -i must` on the same ten files. The last review gave 59. |
| Sentences with "do not" | 55 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 55. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found. E13, T32, E84, E160 and V84 are fixed in part and count as open. A finding that the owner accepted counts as closed.

## Findings of the previous review

The lines up to T159 are the lines of the review of 2026-11-06 (e); this review did not examine them again, and no task
of this window aimed at them. The lines of E164 to E166, V112 to V118 and T160 to T164 follow them, with the evidence of
this review.

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. No change: `Engine.Api.Http/Endpoints/JsonParameters.cs` has no commit since `3379675`. Register entry R-0027.
- **E6** — Open. No change: `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` has no commit since `3379675`. Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. No change.
- **E8** — Fixed. TASK-0044, v0.37. No change.
- **E9** — Fixed, and it holds. No change.
- **E10** — Fixed. TASK-0035, v0.43. No change.
- **E11** — Fixed. TASK-0115, v0.115. No change.
- **E12** — Open, and smaller. No change: `Engine.Core/Hosting/ParameterBinder.cs` has no commit since `3379675`. Register entry R-0027.
- **E13** — Fixed in part. No change: `Engine.Api.Http/Endpoints/QueriesEndpoint.cs`, `Engine.Cli/Cli.cs` have no commit since `3379675`.
- **E14** — Fixed. TASK-0115, v0.115. No change.
- **E15** — Fixed. TASK-0044, v0.37. No change.
- **E16** — Fixed. TASK-0115, v0.115. No change.
- **E17** — Open, and smaller. `Engine.Core/CommandBus.cs` changed only for the refusal of a null required field (E163, TASK-0242), 9 lines in. Register entry R-0028.
- **E18** — Fixed for the sinks of `Engine.*` (TASK-0140, v0.141) and of the desktop host (T118, TASK-0150, v0.154). TASK-0140, v0.141. No change.
- **E19** — Open, and smaller. `Engine.Core/DocumentSession.cs` has no change since `3379675`; `Engine.Core/CommandBus.cs` changed only for E163 (TASK-0242). Register entry R-0033.
- **E20** — Fixed, and it holds. No change.
- **E21** — Open. No change: `Engine.Core/Commands/SubtractCommandHandler.cs` has no commit since `3379675`. Register entry R-0033.
- **E22** — Open. `Engine.Core/CommandBus.cs` changed only for E163 (TASK-0242). Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. No change.
- **E24** — Open in seven parts of nine. No change: `Engine.Api.Http/WebSockets/WireMessage.cs`, `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` have no commit since `3379675`. Register entry R-0033.
- **E25** — Accepted by the owner. TASK-0142, v0.144. No change.
- **E26** — Fixed. TASK-0135, v0.136. No change.
- **E27** — Accepted by the owner. TASK-0142, v0.144. No change.
- **E28** — Fixed. TASK-0136, v0.137. No change.
- **E29** — Fixed for the replay. TASK-0135, v0.136. No change.
- **E30** — Fixed. TASK-0136, v0.137. No change.
- **E31** — Fixed for the replay. TASK-0137, v0.138. No change.
- **E32** — Fixed. TASK-0135, v0.136. No change.
- **E33** — Fixed with E18. TASK-0140, v0.141. No change.
- **E34** — Open. No change: `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs` has no commit since `3379675`. Register entry R-0036.
- **E35** — Fixed for the routes of `MapGet`. TASK-0139, v0.140. No change.
- **E36** — Fixed. TASK-0079, v0.85. No change.
- **E37** — Fixed. TASK-0137, v0.138. No change.
- **E38** — Fixed. TASK-0138, v0.139. No change.
- **E39** — Open. No change of the three choices of the backend; `3DEngine.Android/PhoneHost.cs` changed for the line of the first frame (V106, TASK-0243) and the hint of a new start (TASK-0247), with the same check of the native library. Register entry R-0036.
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
- **E57** — Open. No change: `Engine.Tests/Http/DesktopSurfaceTests.cs` has no commit since `3379675`. Register entry R-0039.
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
- **V2** — Open. No change: `3DEngine.Vulkan/Swapchain.cs`, `3DEngine.Vulkan/OverlayRenderer.cs` have no commit since `3379675`. Register entry R-0022.
- **V3** — Open. No change: `3DEngine.Vulkan/GraphicsDevice.cs` has no commit since `3379675`. Register entry R-0022.
- **V4** — Open. No ledger entry from v0.224 to v0.233 names it. Register entry R-0027.
- **V5** — Fixed. TASK-0076, v0.74. No change.
- **V6** — Fixed. TASK-0076, v0.74. No change.
- **V7** — Open. No change: `3DEngine.Vulkan/Ui/UiRenderer.cs`, `3DEngine.Vulkan/Ui/UiTexture.cs`, `3DEngine.Vulkan/GpuBuffer.cs` have no commit since `3379675`. Register entry R-0030.
- **V8** — Fixed. TASK-0074, v0.75. No change.
- **V9** — Fixed. TASK-0074, v0.75. No change.
- **V10** — Fixed. TASK-0074, v0.75. No change.
- **V11** — Fixed in part. TASK-0074, v0.75. No change.
- **V12** — Fixed, and changed by decision. No change.
- **V13** — Fixed for its parts. No change.
- **V14** — Fixed. TASK-0074, v0.75. No change.
- **V15** — Open. `3DEngine.App/SessionBodies.cs` changed only for `KeyOf` (V109, TASK-0243), 4 lines in.
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
- **T2** — Open in one part of five. No ledger entry from v0.224 to v0.233 names it. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. No change.
- **T4** — Open. No change: `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs` has no commit since `3379675`. Register entry R-0031.
- **T5** — Open in one part of three. No ledger entry from v0.224 to v0.233 names it. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Open in four parts of five. No ledger entry from v0.224 to v0.233 names it. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. No change.
- **T10** — Open. No ledger entry from v0.224 to v0.233 names it. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51. No change.
- **T12** — Fixed. TASK-0059, v0.55. No change.
- **T13** — Open in part. `.github/workflows/ci.yml` changed only for the comparison when the emulator job fails (T152, TASK-0242), 4 lines in and 1 out. Register entry R-0031.
- **T14** — Fixed. TASK-0053, v0.51. No change.
- **T15** — Fixed. TASK-0050, v0.47. No change.
- **T16** — Fixed. TASK-0066, v0.67. No change.
- **T17** — Open. No change: `Engine.Tests/Governance/NativePackageGateTests.cs` has no commit since `3379675`. Register entry R-0031.
- **T18** — Fixed. TASK-0049, v0.46. No change.
- **T19** — Fixed. TASK-0049, v0.46. No change.
- **T20** — Fixed. TASK-0049, v0.46. No change.
- **T21** — Fixed. TASK-0049, v0.46. No change.
- **T22** — Fixed. TASK-0049, v0.46. No change.
- **T23** — Open. No change: `eng/manifold-native/build.ps1` has no commit since `3379675`. Register entry R-0031.
- **T24** — Fixed. No change.
- **T25** — Open in part. No change: `eng/write-set-check` has no change since `3379675`. Register entry R-0031.
- **T26** — Fixed. TASK-0057, v0.54. No change.
- **T27** — Fixed. TASK-0058, v0.56. No change.
- **T28** — Fixed. No change.
- **T29** — Fixed. TASK-0057, v0.54. No change.
- **T30** — Fixed. TASK-0057, v0.54. No change.
- **T31** — Open. No change: `eng/repo-gates/RepoGates.csproj` has no commit since `3379675`. Register entry R-0031.
- **T32** — Fixed in part. No change: `eng/write-set-check/Program.cs` has no commit since `3379675`.
- **T33** — Fixed. TASK-0057, v0.54. No change.
- **T34** — Fixed. TASK-0057, v0.54. No change.
- **T35** — Fixed. TASK-0057, v0.54. No change.
- **C1** — Open. No change: `Engine.Contracts/Geometry/IBRepOps.cs` has no commit since `3379675`. Register entry R-0027.
- **P1** — Open. `3DEngine.App/NativeThreeDEngine.cs` changed for the edges of R12b (TASK-0245), the list "New" (TASK-0244) and the cache of V109 (TASK-0243), 31 lines in and 5 out. Register entry R-0027.
- **T36** — Fixed. TASK-0061, v0.61. No change.
- **T37** — Open in part. No change: `Engine.Tests/Governance/WriteSetRangeTests.cs` has no commit since `3379675`. Register entry R-0031.
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
- **V29** — Open. No check by touch from v0.224 to v0.233; the test sheet of the phone (`docs/android-test-sheet.md`) waits for the owner.
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
- **E84** — Fixed in part. Since TASK-0241 (v0.226) a save to a new name no longer breaks the journal when the new file does not open; the cut of a failed write is unchanged.
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
- **V84** — Fixed in part. No change: `3DEngine.App/FaceArm.cs` has no commit since `3379675`.
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
- **V88** — Open for the desktop, and fixed for the phone. No change: `eng/android/check-apk.sh` has no commit since `3379675`.
- **V89** — Fixed. TASK-0198, v0.206. No change.
- **V90** — Fixed. TASK-0198, v0.206. No change.
- **T141** — Fixed. TASK-0198, v0.206. No change.
- **T142** — Fixed. TASK-0198, v0.206. No change.
- **T143** — Open. No change; it waits for the owner (Q4 of the review of 2026-11-06).
- **T144** — Fixed. TASK-0198, v0.206. No change.
- **E152** — Fixed. TASK-0224, v0.210. No change.
- **E153** — Fixed. TASK-0224, v0.210. No change.
- **E154** — Fixed. TASK-0224, v0.210. No change.
- **E155** — Fixed for its form. TASK-0224, v0.210. No change.
- **V91** — Fixed for its form. TASK-0225, v0.211. No change.
- **V92** — Fixed. TASK-0225, v0.211. No change.
- **V93** — Fixed. TASK-0225, v0.211. No change.
- **V94** — Fixed. TASK-0225, v0.211. No change.
- **V95** — Fixed as a text. TASK-0225, v0.211. No change.
- **V96** — Fixed. TASK-0232, v0.218. No change.
- **T145** — Fixed. TASK-0224, v0.210. No change.
- **T146** — Fixed. TASK-0224, v0.210. No change.
- **E156** — Fixed. TASK-0231, v0.217. No change.
- **E157** — Fixed. TASK-0231, v0.217. No change.
- **V97** — Fixed. TASK-0232, v0.218. No change.
- **V98** — Fixed. TASK-0232, v0.218. No change.
- **V99** — Fixed. TASK-0232, v0.218. No change.
- **V100** — Fixed. TASK-0232, v0.218. No change.
- **V101** — Fixed by the answer Q1 B. TASK-0232, v0.218. No change.
- **V102** — Fixed. TASK-0232, v0.218. No change.
- **V103** — Fixed. TASK-0232, v0.218. No change.
- **T147** — Fixed. TASK-0231, v0.217. No change.
- **T148** — Fixed for its forms. TASK-0231, v0.217. No change.
- **T149** — Fixed for its forms. TASK-0231, v0.217. No change.
- **T150** — Fixed. TASK-0231, v0.217. No change.
- **T151** — Fixed. TASK-0231, v0.217. No change.
- **E158** — Fixed. TASK-0242, v0.229: each reader of a document file gives `E-IO-LOAD-FAILED` for a path that is not valid.
- **E159** — Fixed. TASK-0242, v0.229: the host compares the unsaved folder with no case on Windows and macOS.
- **E160** — Fixed in part. TASK-0242, v0.229, for the reasons of `DatumResolver`; the comment of `Engine.Contracts/Sketch/DatumRecord.cs` stays, because the task forbade `Engine.Contracts`.
- **E161** — Fixed. TASK-0242, v0.229: the probe holds a circle, a cylinder, a move and a cut; run 42290937312 gave `1c56cbdd...30ba` on the four platforms.
- **E162** — Fixed. TASK-0241, v0.226: a save to a new name copies the lines into the new file and then deletes the old one.
- **E163** — Fixed. TASK-0242, v0.229: the bus refuses a null required field with `E-CMD-FIELD-MISSING`.
- **V104** — Fixed. TASK-0243, v0.228: a cancelled finger is a touch sample with the phase Cancel.
- **V105** — Fixed for its form. TASK-0243, v0.228 showed the cause (a new start of the activity, run 42289169473), and TASK-0247, v0.231 sets the hint of a new start; a new start during a session is V112.
- **V106** — Fixed. TASK-0243, v0.228: the line of the first frame comes after a submitted frame; no test, because the presenter needs a surface.
- **V107** — Fixed. TASK-0243, v0.228: the outline of the face follows the offset of a drag.
- **V108** — Fixed. TASK-0243, v0.228: only `CancelIntent.ByKey` goes back from the face.
- **V109** — Fixed. TASK-0243, v0.228: the entry of the face is kept by the geometry key of the body.
- **V110** — Fixed. TASK-0243, v0.228: the comments say that no run shows the order of the background.
- **V111** — Fixed. TASK-0243, v0.228: ADR-0075 names the files of its items 2 and 3.
- **T152** — Fixed. TASK-0242, v0.229: the comparison runs when the gate passed; run 42289169473 compared the three desktop hashes with the emulator job failed.
- **T153** — Fixed. TASK-0242, v0.229: the smoke test fails on a native crash of the program.
- **T154** — Fixed. TASK-0239, v0.224: the start is one notice, and the line "ended" is the text of the failure.
- **T155** — Fixed. TASK-0242, v0.229: the pin rule decodes escapes, and a property in an Update or a Remove is an offence.
- **T156** — Fixed. TASK-0242, v0.229: a Remove or an Update of a SupportedPlatform is an offence.
- **T157** — Fixed. TASK-0242, v0.229: a hash with no line of the probe fails with its reason.
- **T158** — Fixed. TASK-0239, v0.224 and TASK-0242, v0.229: each call of adb has a limit.
- **T159** — Fixed. The entry v0.225 names the seven runs; each later entry names its failed runs.
- **E164** — Fixed in part. TASK-0251, v0.237. `MoveTo` gives the path of a kept old file
  (`Engine.Core/Persistence/DocumentJournal.cs:191`), and `SaveAs` adds `W-IO-OLD-FILE-KEPT`
  (`Engine.Core/DocumentSession.cs:108` to `113`). A failed copy cuts the new file first (`DocumentJournal.cs:177`). The
  form with a cut that fails too is E168.
- **E165** — Fixed. TASK-0251, v0.237. `Edges` gives each pair of names of an edge (`Engine.Core/Queries/GetFacesQueryHandler.cs:160`
  to `170`). The outline takes only an edge with a face of the name and a face of another name (`GetFacesQueryHandler.cs:131`).
  ADR-0077 item 6 (`docs/adr/0077-a-tap-near-an-edge-selects-the-edge.md:99` to `102`) decides it. The probe gives 6 edges
  on the z axis. Its test is weak (E169).
- **E166** — Fixed. TASK-0251, v0.237. `Engine.Contracts/Sketch/DatumRecord.cs:3` to `6` names `CreateCylinder`.
- **V112** — Fixed. TASK-0250, v0.235. `Window` is `IDisposable` and destroys its SDL window one time
  (`3DEngine.Vulkan/Window.cs:27-45`); `PhoneHost.Run` disposes it at the end of the session (`3DEngine.Android/PhoneHost.cs:56`);
  `WindowLifecycleTests.cs:13-30` holds it. TASK-0255 (v0.238) adds the turns for a new create during a session.
- **V113** — Fixed. TASK-0252, v0.240. `Forget` removes the kept entry of a body with a fault (`3DEngine.App/BodyActions.cs:273-278`).
- **V114** — Fixed for an open panel. TASK-0252, v0.240. A size constraint and `KeepAboveAnchors` hold it
  (`3DEngine.App/Ui/DesktopUi.Panel.cs:20`, `:30`, `:256-263`). A collapsed panel still covers Undo (V123).
- **V115** — Fixed. TASK-0252, v0.240. The unit comes before the name (`DesktopUi.Panel.cs:268-271`).
- **V116** — Fixed. TASK-0252, v0.240. While a list is open, each point belongs to the panel (`3DEngine.App/Ui/DesktopUi.cs:379-382`).
- **V117** — Fixed. TASK-0252, v0.240. The host asks for the edge before the pick (`BodyActions.cs:79-83`).
- **V118** — Fixed for the panel, the anchors and the status line. TASK-0252, v0.240. `Window.SafeInsets` reads
  `SDL_GetWindowSafeArea` (`Window.cs:203-213`), and `DesktopUi.Fixed.cs:29`, `:45` and `DesktopUi.Panel.cs:19` read the
  inset. The tool ring does not read it (V122).
- **T160** — Fixed. TASK-0251, v0.237. The correction of the entry v0.237 (`docs/CURRENT-STATE.md:7135` to `7145`) names 42280512471 the ninth failure and moves the later numbers by one. The API gives three failed runs after 42298165211: 42308415060, 42310351172 and 42313242154. The entries v0.237, v0.238 and v0.239 name each of them.
- **T161** — Fixed. TASK-0251, v0.237. `DependencyDirectionGateTests.cs:1522` refuses `*` and `?` in the `Update` and the `Remove` of a package item. The comparison with the evaluated version stays open, as the entry v0.237 says (line `7131`).
- **T162** — Fixed for its forms. TASK-0251, v0.237. `DependencyDirectionGateTests.cs:639`, `643`, `656` to `658` and `667`, and the new step `eng/android/check-platforms.js` (`.github/workflows/ci.yml:251` to `252`). Two new silences pass each gate (T165). Three projects of the phone do not need a silence (T166).
- **T163** — Fixed. TASK-0251, v0.237. `eng/android/native-crash.ere` holds three patterns, `eng/android/smoke-emulator.sh:127` gives them to `grep -E -f`, and line `76` reads the log again after 5 seconds. My `grep -E -f` probe matched the line of libc with `(eedengine.phone)` and the line `Cmdline:`, and did not match the line of `com.android.systemui`.
- **T164** — Fixed for its forms. TASK-0251, v0.237. The affects fields of ADR-0076 (`docs/adr/0076-the-cylinder.md:11` to `23`) and ADR-0077 (`docs/adr/0077-a-tap-near-an-edge-selects-the-edge.md:11` to `20`) name the eight files. No gate compares an affects field with the commit of its task, and the defect came back in ADR-0082 and ADR-0083 (T167).

## 1 Current architecture

**[Observed]** An STL file opens as a mesh body of a new part through one command, `ImportStl`, which reads the bytes of
its asset at each apply and replay through `IAssetSource`, the store that each host gives to the engine (ADR-0083). The
mesh body is a feature with no input, held as floats in both backends, and each other operation refuses it with
`E-MESH-UNSUPPORTED`. The probe of the determinism hash exports and imports the bracket of the demonstration. The
redesign has a tool ring that follows the selection (R16b2, ADR-0081) and the values of an extrude on its edges (R16c,
ADR-0082). The phone ends a session of a destroyed activity before its window (P0.103) and names the cause of a failure of
its window (P0.104).

## 2 Patterns in use

- **A new reader of the store misses a scratch session.** The handler of `ImportStl` reads the store, and the hosts give
  it to the engine; the check before an open makes its own session with no store (E167), and the command line gives the
  default store only (E171). **[Observed]**
- **Two layers of the redesign act on one tap.** The ring and the chips each take the same release, and the placement of
  the chips does not know the ring (V120); a pad of another tool does not clear the pending chip (V119). **[Observed]**
- **A gate of text misses the next form again.** After T162, a blanket pragma and a category severity silence CA1416
  (T165), and three projects do not treat a warning as an error (T166). **[Observed]** by injection.
- **The affects field lags its task again.** T164 corrected ADR-0076 and ADR-0077; ADR-0082 and ADR-0083 repeat the defect
  (T167). **[Observed]**

## 3 Review of the code

### The engine and the hosts

#### E167 · High · The check of an open on the desktop has no store of the assets, so a document with a mesh body does not open there · Confirmed by a probe

- **[Observed]** `DocumentFile.Check` makes its scratch session with `EngineHosting.CreateDefault(backend, unit, kind)`
  and no store (`Engine.Core/Persistence/DocumentFile.cs:92`). The handler of `ImportStl` then refuses with `E-STL-ASSET`
  (`Engine.Core/Commands/ImportStlCommandHandler.cs:65` to `66`).
- **[Observed]** The desktop calls `Check` before each open of a file (`3DEngine/DesktopDocument.cs:57`), through
  `DocumentFolder.Open` (`DesktopDocument.cs:25`). The session after the check gets the store
  (`3DEngine/DesktopSession.cs:52` to `58`).
- **[Observed]** Probe `check`: a document file with one `ImportStl`. `DocumentFile.Check` gave `E-IO-LOAD-FAILED`, "The
  entry of version 1 (ImportStl) gave Rejected, with E-STL-ASSET: This host has no store". `DocumentFile.Open` with the
  store applied it, version 1, no warning.
- **[Inferred]** ADR-0083 item 2 says that an open of a document file reads the asset. On the desktop, the open list, the
  recent files of item 7 (P9c) and an open by an agent refuse each document with a mesh body, also when the store holds
  its asset.
- **[Recommended]** Give `Check` the store of the host (an `IAssetSource` parameter), and pass it from `DesktopDocument`.
  Add the probe as a test.

#### E168 · Low · A failed copy whose cut and delete fail too leaves a second document that opens, with no report · Confirmed by a probe

- **[Observed]** `CutQuietly` swallows a failed `SetLength` (`Engine.Core/Persistence/DocumentJournal.cs:209` to `218`).
  `DeleteQuietly(target)` then swallows a failed delete (`DocumentJournal.cs:179`). The error of the save gives only the
  message of the first exception (`Engine.Core/DocumentSession.cs:118`). It does not name the file that stays.
- **[Observed]** ADR-0015 item 9 (`docs/adr/0015-command-log-persistence.md:104` to `105`) and the comment at
  `DocumentJournal.cs:149` to `150` say "a new file that stays does not open". The test (`Engine.Tests/Persistence/DocumentFileTests.cs:760`)
  has a cut that works (`DocumentFileTests.cs:809`).
- **[Observed]** Probe `journal`: two commands; the stream of the new name failed its `Flush` and its `SetLength`; the
  delete of the new name failed. `SaveAs` gave `E-IO-SAVE-FAILED` with no warning. The new file had 489 bytes.
  `DocumentFile.Check` gave null, and `DocumentFile.Open` opened it at the version 2.
- **[Inferred]** It needs three failures in sequence, for example a network drive that goes away. Then the person has
  two documents and does not know it.
- **[Recommended]** When the cut and the delete fail, add the path of the new file to the error of the save. Change the
  sentence of ADR-0015 item 9 and the comment to agree.

#### E169 · Low · The test of an edge of four faces does not check that each of the six pairs is an edge · Confirmed by reading

- **[Observed]** `Engine.Tests/Features/GetFacesTests.cs:293` asserts that one edge has a segment on the z axis. The
  outline check (`GetFacesTests.cs:295` to `300`) asserts that each segment of an outline is in some edge of its face.
- **[Inferred]** A handler that gives two pairs only, for example `+x|+y` and `-x|-y`, passes both asserts. The name of
  the test says "an edge of each pair", which ADR-0077 item 6 decides.
- **[Observed]** Probe `faces`: the handler gives 28 edges and 6 on the axis (`+x|+y`, `+x|-x`, `+x|-y`, `+y|-x`, `+y|-y`,
  `-x|-y`). So the code is correct, and the test does not hold it.
- **[Recommended]** Assert that exactly six edges hold the axis segment, one for each pair of the four names.

#### E170 · Low · The STL reader gives a code that ADR-0083 item 3 does not give for three forms of a file · Confirmed by a probe

- **[Observed]** A file whose length does not agree with its count goes to the text reader when its first bytes are
  "solid" (`Engine.Core/Import/StlReader.cs:24` to `32`). Probe `reader`: a binary file of four triangles with the header
  "solid exported by a program", cut to 150 bytes, gave `E-STL-SYNTAX` ("is not UTF-8 or ASCII text",
  `StlReader.cs:72`). ADR-0083 item 3 gives `E-STL-TRUNCATED` for a binary file of the wrong length. The same file with
  a header of zeros gave `E-STL-TRUNCATED`.
- **[Observed]** The name of the solid ends at the first word "facet" or "endsolid" (`StlReader.cs:78`). Probe: `solid old
  endsolid copy` with one triangle gave `E-STL-EMPTY`. `solid facet` with one triangle gave `E-STL-SYNTAX`.
- **[Observed]** A text file with a UTF-8 byte order mark gave `E-STL-TRUNCATED` (`StlReader.cs:114` to `119` skips
  white space only).
- **[Inferred]** Some programs write "solid" at the start of a binary header. A cut file of such a program gives a
  message about text, and the demonstration asks for a clear message. The tests use a header with no "solid" for the cut
  file (`Engine.Tests/Import/StlImportTests.cs:79` and `85`).
- **[Recommended]** Give `E-STL-TRUNCATED` when the text read fails and the length agrees with no count. Read the name to
  the end of its line. Skip a byte order mark. Add the three files as tests.

#### E171 · Low · The command line reads the asset of an import from the default store only · Confirmed by a probe

- **[Observed]** `BuildSession` gives `new AssetStore(AssetStore.Default)` (`Engine.Cli/Cli.cs:486`). `engine asset put`
  takes `--folder` (`Cli.cs:76` to `84`), but `apply`, `query` and `export` have no such option
  (`Engine.Cli/Usage.cs:14` to `21`). The desktop and the surface read `Engine:AssetFolder`
  (`3DEngine/DesktopSession.cs:52`, `Engine.Api.Http/EngineSurface.cs:82` to `83`).
- **[Observed]** Probe `cli`: `asset put --folder <scratch>` gave the name; a document with that import applied with that
  store. `engine export stl` of the document gave exit 1 and `E-IO-LOAD-FAILED` with `E-ASSET-NOT-FOUND` in the default
  store.
- **[Recommended]** Give `export`, `apply` and `query` the option `--folder`, as `asset` has it, or say in the usage that
  they read the default store only.

**No finding.** I checked these items and found them correct.
Determinism: the new code of the reader, the handler, the two backends and the probe calls no function of rules 1, 3
and 4. A binary coordinate is a copy of its bits (`StlReader.cs:53`). A text number is `float.TryParse` with the
invariant culture, which rounds correctly on each platform. The box of a mesh uses comparisons only. The triangles go to
the backend, and the command records the asset name (rule 6). The store checks the SHA-256 of the bytes at each read
(`Engine.Core/Assets/AssetStore.cs:161` to `163`), so a changed asset cannot give another replay.
Replay with a missing asset: `DocumentFile.Apply` stops the open with `E-IO-LOAD-FAILED` and names `E-STL-ASSET`
(`DocumentFile.cs:235` to `239`), as ADR-0083 item 2 says.
The reader: a file of 84 bytes with the count 0 gives `E-STL-EMPTY`; a name with spaces and CR/LF read; `1e39` gives
`E-STL-NOT-FINITE`; the count `0xFFFFFFFF` in 84 bytes gives `E-STL-TRUNCATED` with no overflow (`StlReader.cs:27`).
The time of a text file of 16 MiB (195,083 triangles) was 359 ms, and 372 ms with `System.Linq.Enumerable.IsSizeOptimized`
on, the default of Android (`Microsoft.Android.Sdk.DefaultProperties.targets:135`). So the `Skip` at `StlReader.cs:103` is not quadratic.
Refusals: `Translate`, `Subtract`, `CreateDatum`, `AddSketchReference`, `GetFaces`, `GetFaceAt` and the STEP export refuse
a mesh with `E-MESH-UNSUPPORTED`; the HTTP and CLI exports use the same `StepExport`. Probe `ops`: `CreateDatum` on a
hidden mesh gave `E-MESH-UNSUPPORTED`; `SetExtrudeDistance` gave `E-FEATURE-NOT-FOUND`; Undo and Redo of the import
applied. An assembly refuses `ImportStl` (`Engine.Core/Commands/DocumentKinds.cs:22` to `26`).
The event: `body.created` gives the kind `Mesh` and the geometry key, as the schema says
(`Engine.Core/CommandBus.cs:574` to `582`). No event kind is new.

#### Probes run of this section

A console program outside the repository, with the assembly name `Engine.Tests` and the built DLLs of the worktree.
- `reader`: twelve files. Three gave a code that ADR-0083 does not give (E170).
- `time`: 16 MiB text, 359 ms; with size-optimized LINQ, 372 ms.
- `check`: `Check` gave `E-IO-LOAD-FAILED`; `Open` with the store gave version 1 (E167).
- `ops`: the refusals above; Undo and Redo applied.
- `journal`: a cut and a delete that fail; the new file opened at version 2 (E168).
- `faces`: 6 edges on the axis (E165 fixed, E169).
- `cli`: `export stl` gave `E-IO-LOAD-FAILED` for an asset in another folder (E171).

### The desktop and the phone

#### V119 · Low · A pad of another tool from the panel keeps the chip pending, and the chips stay · Confirmed by a probe

- **[Observed]** `Values` clears `_pendingValue` only when no pad is open, and it hides the chips only when a pad is open
  and nothing is pending (`3DEngine.App/Ui/DesktopUi.Values.cs:192-195`). `OpenPad` replaces the pad and does not clear
  `_pendingValue` (`3DEngine.App/Ui/DesktopUi.Pad.cs:43-47`). The button Move of the panel works while a pad is open; only
  the text fields are disabled (`DesktopUi.Panel.cs:210-211`, `DesktopUi.cs:553`).
- **[Observed]** Probe at 1280 by 720: a tap on a length chip opened the pad "Value" with the chip pending. A tap on Move of
  the panel then gave the pad "Move", the pending key of the length stayed, the chip stayed at (653, 278, 747, 322), and
  `OverPanel` on the chip gave true.
- **[Inferred]** ADR-0082 item 1 says no chip shows while the pad of another tool is open, and item 4 says that the chip
  is pending while the pad of that chip is open (`docs/adr/0082-*.md:68`, `:85`). The person sees a dashed chip for a
  pad that is not open. A tap on that chip does nothing (`DesktopUi.Values.cs:216`) and does not reach the scene.
 
- **[Recommended]** Clear `_pendingValue` in `OpenPad`, and set it after the open in `Use` and `OpenDistancePad`. Add the
  probe as a row of `EdgeValueTests`.

#### V120 · Low · A chip that lies on a segment of the ring gives two tools for one tap · Confirmed by a probe

- **[Observed]** `Frame` calls `Ring` and then `Values` (`DesktopUi.cs:532`, `:534`). Each one reads the same release:
  the ring at `DesktopUi.Ring.cs:119-121`, the chip at `DesktopUi.Values.cs:216-218`. The placement of the chips gets
  no ring (`DesktopUi.Values.cs:205-207`). The inner radius is 24 pixels more than half of the projected diagonal of the
  box (`DesktopUi.Ring.cs:83`, `3DEngine.App/NativeThreeDEngine.cs:620`). A chip centre is 24 pixels outside its edge,
  and a chip is at least 44 pixels (`3DEngine.App/EdgeValues.cs:18`, `DesktopUi.Values.cs:173`).
- **[Observed]** Probe with the plate of `EdgeValueTests` (10 by 6 by 3), yaw 30, pitch 25, at 1280 by 720. At a camera
  distance of 16, the ring had its centre at (640, 360) and an inner radius of 240. The chips "6 mm" and "3 mm" lay
  partly on Hide. At distance 13, "3 mm" lay on Move. With the body to the right (target x of -6), "3 mm" lay on
  Distance. One touch tap at (859, 466) on the chip "6 mm" gave `SetVisibleAction(false)` and opened the pad "Value".
- **[Inferred]** A tap on a value hides the body. The hide clears the selection, and the pad closes. On the Distance segment, a length chip opens its pad, and the `EditDistanceAction` then
  replaces it with the pad of the distance.
- **[Recommended]** Let one release give one tool: the ring skips a release on a chip, or the placement moves each
  chip out of the band. Add a test with the real plate.

#### V121 · Low · On a portrait display, the fourth segment of the ring goes past the top and out of the display · Confirmed by a probe

- **[Observed]** The portrait ring starts at π and goes 0.46 radians for each segment, with its centre 16 pixels inside
  the corner (`DesktopUi.Ring.cs:35-36`, `:80`). Four labels occur for an extrude with a selected face that has a plane
  (`DesktopUi.Ring.cs:106-111`). ADR-0081 item 1 says that "its segments go from the left up to the top"
  (`docs/adr/0081-*.md:56-57`).
- **[Observed]** Probe at 1080 by 2316: "Sketch on face" goes from 4.522 to 4.942 radians, and straight up is 4.712.
  The middle of its label is at x 1067.5, and the outer end is at x 1113.5, which is outside the display. At 390 by 844
  the label middle is at x 377.5 of 390. `The_Ring_Of_A_Portrait_Display_Is_In_The_Lower_Right_Corner` has three labels
  only (`3DEngine.ImageTests/ToolRingTests.cs:67`).
- **[Inferred]** On the phone, about 40 % of the segment and half of its label are outside the display. Its target and
  its text are cut.
- **[Recommended]** Make the angles of the portrait ring fit π to 3π/2 for each count, or make the segments narrower
  there. Add a row with four labels to the test.

#### V122 · Low · The tool ring of a portrait display does not read the safe area, and no register entry holds it · Confirmed by reading

- **[Observed]** The portrait centre is `(display.X - 16, display.Y - 16)` (`DesktopUi.Ring.cs:80`). `Ring` and `Layout`
  get no inset (`DesktopUi.cs:532`). The ledger entry v0.240 says, under "What stays", that the navigation bar can cover
  the ring and that a later slice moves it. `docs/register.md` has no entry for it.
- **[Inferred]** The segment Move lies between 16 and 105 pixels above the bottom edge (probe of V121). With the
  navigation of three buttons, the bar covers it, and the system takes the tap. Step 11 of `docs/android-test-sheet.md`
  tests Undo only. `CLAUDE.md` says that each open question goes into the register with a time limit.
- **[Recommended]** Give `_inset` to `Layout` now, which is one line. If not, add a register entry with a limit.

#### V123 · Low · A collapsed panel is not kept above the anchors and covers Undo; two comments of the clamp are false · Confirmed by a probe

- **[Observed]** `KeepAboveAnchors` runs inside `if (ImGui.Begin(...))` (`DesktopUi.Panel.cs:28-30`). `Begin` gives false
  for a collapsed window, so the clamp does not run.
- **[Observed]** Probe at 1280 by 720: a double click on the title bar collapsed the panel to (16, 16, 476, 60). A drag
  of the title bar to (60, 690) put it at (-124, 680, 336, 724), over Undo at (16, 656, 64, 704). A mouse click on the
  centre of Undo then gave no action.
- **[Observed]** The comment at `DesktopUi.Panel.cs:253` names the title, but `KeepAboveAnchors` is below it, and
  `Title` is at `:268`. The comment at `:254-255` says that the panel stays 16 pixels inside the left inset. The code
  keeps it inside the inset only (`:261`).
- **[Inferred]** This is V114 with one more step. The panel can also leave the display on the left.
- **[Recommended]** Call the clamp after `Begin` in each case. Move the comment of line 253 to `Title`, and make the left
  clamp `16 + _inset.X` or correct the comment. Add a collapsed row to `FixedControlsTests`.

**No finding.** **[Observed]** A tap on a chip does not reach the scene: the chips are in `OverPanel`
(`DesktopUi.cs:381-382`), and `EdgeValueTests.cs:128-130` holds it. A change of the selection closes the pad at the start
of the frame (`DesktopUi.cs:322-329`), and `Values` clears the pending key in the same frame (`DesktopUi.Values.cs:192-193`).
Two lengths on one line give two chips with two keys (`EdgeValues.cs:58-73`), and the placement moves them apart.
**[Observed]** The ring does not cover the anchors. At 1280 by 720 with four segments, it overlaps the right edge of the
panel by 3 pixels, where the panel window takes the tap (`DesktopUi.Ring.cs:119`). The inside of a portrait ring is the
scene, as ADR-0081 item 2 says. With the insets (0, 40, 0, 48), the clamp keeps the open panel above Undo at heights of
540, 360 and 240. Only at 160 does it cover Undo, which is not a real display. **[Observed]** In `PhoneHost.Run`,
`held` comes before `window` (`PhoneHost.cs:50`, `:56`). If the session throws, C# disposes the window first and then
releases the hold. After `SDL_CreateWindow`, no line of the constructor throws (`Window.cs:87-106`), so a failed
constructor cannot keep a window. `Window.Error` and the two messages agree with the ledger entry v0.239
(`Window.cs:90`, `:246`).

#### Probes run of this section

- A temporary file `3DEngine.ImageTests/ZzReview28Probes.cs` at `e64efcd`, run with `dotnet test --filter`. I deleted it, and
  `git status --short` gave no line.
- P1, the real plate and the ring at six camera places: the chips lay on Hide, Move or Distance at four of them (V120).
- P1b and P1c: one tap on a chip over a segment gave both tools, by touch and by mouse (V120).
- P2: the panel Move after a chip kept the pending key and the chip (V119).
- P3: the portrait layout with four labels at 1080 by 2316 and 390 by 844 (V121).
- P4: a collapsed panel dragged over Undo, and a click on Undo gave no action (V123).
- P5: the open panel at four heights with insets (no finding).
- `dotnet test 3DEngine.ImageTests` with the classes `EdgeValueTests`, `ToolRingTests`, `FixedControlsTests`,
  `SessionTurnsTests`, `BodyActionsTests` and `EdgeSelectionTests`: 44 passed, 0 failed, after I deleted the probe file.
- No device ran. V122 rests on the layout and the ledger.

#### V124 · High · A destroy of the activity after the first frame ends the session with a lost surface, and the emulator job fails · Confirmed by the public API

- **[Observed]** Run 42323457861 (`main` at `86517c8`, the merge of v0.243) failed in the job "Start the APK on the
  emulator". The annotations give this order: the activity was created at 01:23:20.956 and destroyed at 01:23:22.823; the
  session of the destroyed activity ended before its window at 01:24:12.577, as TASK-0255 makes it; the new session drew
  its first frame at 01:24:12.970; the activity was destroyed again at 01:24:16.555; and that session ended at
  01:24:16.683 with `VkException: [-1000000000] ErrorSurfaceLostKHR` in `Presenter.SurfaceExtent`, through
  `Presenter.RenderFrame`, `NativeThreeDEngine.Render` and `PhoneHost.Foreground`. The hashes agreed.
- **[Observed]** The lines of a relaunch that TASK-0256 added give "Schedule relaunch activity" of the launcher at
  01:24:11.920 and "finishDrawing of relaunch" of the activity at 01:23:22.654; no line names the change of the
  configuration.
- **[Inferred]** `PhoneHost.Foreground` (`3DEngine.Android/PhoneHost.cs`) treats each exception of a frame as the end of
  the session. A destroy of the activity loses the surface before the next event of SDL reaches the loop, so the loop
  meets the lost surface first. The two other forms of the new start (V112, the window of TASK-0255) are fixed; this is the
  third, after the first frame.
- **[Recommended]** In `Foreground`, treat a lost surface as the background: end the engine and wait for the foreground,
  or end the session quietly when its turn is stale (ADR-0079 item 4). Write `IsChangingConfigurations` and
  `ChangingConfigurations` of the activity at its destroy, so the next run names the change. The session added this finding from the run of `main` after the commit of the review.

### The gates and the pipeline

#### T165 · Low · A blanket pragma, or the severity of the category Interoperability in an analyzer file, silences CA1416 in a project of the phone, and each gate passes · Confirmed by injection

- **[Observed]** The rule of the code (`Engine.Tests/Governance/DependencyDirectionGateTests.cs:656` to `658`) finds a line only when that line holds `CA1416` and `#pragma` or `SuppressMessage`. The rule of the analyzer files (line `667`) finds a file only when its text holds `CA1416`. `eng/android/check-platforms.js:23` to `24` reads `NoWarn`, `WarningsNotAsErrors` and three switches of the analyzers. It reads no source file and no analyzer file.
- **[Observed]** Control: `Engine.Kernel/Rev28ConfigProbe.cs` sets `Console.ForegroundColor`. `dotnet build Engine.Kernel --no-incremental` gave `error CA1416`.
- **[Observed]** Injection 1: `Engine.Kernel/.editorconfig` with `dotnet_analyzer_diagnostic.category-Interoperability.severity = none`, and the same probe. Injection 2: `Engine.Core/Rev28PragmaProbe.cs` with `#pragma warning disable` (no code) above the same call. `dotnet build Engine.Core --no-incremental` built both projects with 0 warnings and 0 errors, although `TreatWarningsAsErrors` is true in both (`Engine.Core/Engine.Core.csproj:7`, `Engine.Kernel/Engine.Kernel.csproj:9`).
- **[Observed]** With both injections, the `Governance` tests of `Engine.Tests` gave 494 passed and 0 failed. `node eng/android/check-platforms.js` gave "Each of the 7 projects supports android, and no property silences CA1416", exit 0. `eng/repo-gates` gave "468 runs of 161 checks, 0 failed". I removed the three files, and `git status` showed none of them.
- **[Observed]** The entry v0.237 (`docs/CURRENT-STATE.md:7112` to `7114`) says that "a pragma or an attribute in the code" is an offence. That is true only for a pragma that names CA1416 on its line.
- **[Inferred]** A `SuppressMessage` attribute whose arguments continue on a second line, a `.globalconfig` with the category form, and the property `AnalysisModeInteroperability` escape the same rules. I did not test them. The external lead of the Codex review (line `656`) is confirmed.
- **[Recommended]** Refuse `#pragma warning disable` with no code, and each `dotnet_analyzer_diagnostic` severity line that is not a single rule, in the projects of the phone. Better, build the projects of the phone in the step with an injected probe file, and require `CA1416` in the output. Prove it with these two injections.

#### T166 · Medium · In 3DEngine.App, 3DEngine.Core and 3DEngine.Vulkan, CA1416 is a warning only, and no step of the pipeline fails on a warning · Confirmed by injection

- **[Observed]** `dotnet msbuild -getProperty:TreatWarningsAsErrors` gives `false` for `3DEngine.App`, `3DEngine.Core`, `3DEngine.Vulkan` and `3DEngine.Android`. Their project files do not set it, and the repository has no `Directory.Build.props`. The four `Engine.*` projects of the phone set it to `true`.
- **[Observed]** `.github/workflows/ci.yml:80` runs `dotnet build`, and line `247` builds the APK. No step uses `-warnaserror` or counts warnings. No governance test reads `TreatWarningsAsErrors`. The comment at `DependencyDirectionGateTests.cs:597` to `599` says that "the rule of zero warnings refuses such an API". That rule is the text of each ledger entry.
- **[Observed]** Injection: `3DEngine.App/Rev28WarningProbe.cs` sets `Console.ForegroundColor`, with no silence. `dotnet build 3DEngine.App --no-incremental` gave 1 warning `CA1416`, 0 errors and exit 0. `Each_Project_That_The_Phone_Reaches_Supports_Android` and `check-platforms.js` passed. I removed the file.
- **[Inferred]** The rule of TASK-0229 came from this exact call in `3DEngine.Vulkan/Log.cs:39` to `40` (`docs/CURRENT-STATE.md:6396`). Without the guard at line `33`, the call now gives a warning only, and each gate passes. The smoke test on the emulator finds it only if the call runs at the start.
- **[Recommended]** Set `TreatWarningsAsErrors` in the three projects, or add `-warnaserror` to the two builds of the pipeline. Add `TreatWarningsAsErrors` to the properties that `check-platforms.js` evaluates. Prove it with this injection.

#### T167 · Low · The affects fields of ADR-0082 and ADR-0083 do not name nine files that their decisions change · Confirmed by reading

- **[Observed]** Commit `e903bef` (TASK-0258) raises `E-MESH-UNSUPPORTED` for ADR-0083 item 4 in `Engine.Core/Commands/TranslateCommandHandler.cs:77`, `SubtractCommandHandler.cs:79`, `CreateDatumCommandHandler.cs:79`, `AddSketchReferenceCommandHandler.cs:77`, `Engine.Core/Queries/GetFacesQueryHandler.cs:64`, `GetFaceAtQueryHandler.cs:63` and `Engine.Core/Export/StepExport.cs:32`. It gives the store of item 2 in `Engine.Core/Hosting/EngineHosting.cs`. The affects field of ADR-0083 (`docs/adr/0083-the-mesh-body-and-the-stl-import.md:11` to `22`) names none of these eight files.
- **[Observed]** Commit `5c3df9a` (TASK-0254) changes `3DEngine.App/Ui/DesktopUi.Pad.cs:53` to `58` for ADR-0082 item 4, the pending state. The affects field of ADR-0082 (`docs/adr/0082-the-values-on-the-edges-of-the-redesign.md:11` to `18`) does not name it.
- **[Observed]** The affects field of ADR-0081 names the three code files of commit `904c3a0`. The affects field of ADR-0079 names `SessionTurns.cs`. It does not name `3DEngine.Vulkan/Window.cs`, which commits `de88699` and `7e24c53` change for its consequence; ADR-0080 names that file.
- **[Observed]** `docs/diagnostics.md:78` names `DatumResolver` as a place that raises `E-MESH-UNSUPPORTED`. The code is in `CreateDatumCommandHandler` and `AddSketchReferenceCommandHandler`. `DatumResolver.cs` adds a message only.
- **[Inferred]** This is the defect of T164 again, one review later. A later task that changes the move or the cut is not required to read ADR-0083.
- **[Recommended]** Add the nine paths. Add the gate of T164: each code file of the commit that creates an ADR, or of the task that it governs, matches its affects field or is named in a list of exceptions in the task.

**No finding.** I checked these items and found them correct.
- The `.stl` type in `BinaryExtensions` (`DependencyDirectionGateTests.cs:279`). Git sees the two tracked files as binary (`git diff --numstat` gives `-`). Git tracks no text STL file. The control-byte rule still reads each other type.
- The seven codes of ADR-0083 agree in `docs/diagnostics.md:72` to `78`, `Engine.Core/DiagnosticCodes.cs:123` to `129` and `Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs:86` to `92`. `E-STL-UNIT` was in the first form of the ADR only, never in the register or the code, so no code is removed. `W-IO-OLD-FILE-KEPT` is registered.
- The counts of the entries v0.234 to v0.243. The test counts add up from entry to entry and agree with `eng/test-floor.txt`. "4 product milestones and 10 entries" (v0.243) agrees with R16b2, R16c, P9a and P9b and the roadmap rows `docs/roadmap.md:212` to `219`. The statuses of TASK-0250 to TASK-0258 are `Done`. TASK-0259 and TASK-0260 do not exist.
- Each named run exists and has the stated result. 42302467567, 42306849941 and 42311258959 passed on each of the seven jobs. 42304302535, 42315180361, 42316096251, 42317291934, 42319809463 and 42320677685 passed. 42308415060 and 42310351172 failed in step 7 of the emulator job. 42313242154 failed in the Windows gate (step 7, "Test") and the emulator job. The annotations of the comparison job of 42320677685 give `06d832c3…654ea7` for the four platforms, as v0.243 and `docs/android-test-sheet.md:59` say. 42320677685 tested `9125da4`, not the merged `e903bef`. `git diff 9125da4 e903bef` changes four documents only.

**Probes run.**
- `dotnet build Engine.Kernel` with an unsilenced probe: `error CA1416` (control).
- Category severity in `Engine.Kernel/.editorconfig` and a blanket pragma in `Engine.Core`: build 0 warnings; `Governance` tests 494 passed; `check-platforms.js` exit 0; `eng/repo-gates` 468 runs, 0 failed.
- Unsilenced probe in `3DEngine.App`: 1 warning, exit 0; gate and step passed.
- `dotnet msbuild -getProperty:TreatWarningsAsErrors` on four projects: `false`.
- `grep -E -f native-crash.ere` on three lines: 2 matches, as expected.
- Public API: 9 requests (1 list of runs, the jobs of 7 runs, 1 list of annotations).
- I removed each probe file. At the end, `git status --porcelain` gives no line. During the work it showed `3DEngine.ImageTests/ZzReview28Probes.cs`, a probe of another agent in the same worktree; I did not touch it.

## 4 The next planned task

The next product phase is P9c, the desktop part of the demonstration "Open the same STL on desktop and Android" (the order
of the owner of 2026-11-07), whose change passed the pipeline and whose close waits for this review. E167 and V124 are
high, so two corrections come before the close of P9c: P0.105 (TASK-0262) holds E167, and P0.108 (TASK-0265) holds V124. By `docs/templates.md`, section 7, the medium
and low findings go after P9c: P0.106 (TASK-0263) holds E168 to E171 and T165 to T167, and P0.107 (TASK-0264) holds V119
to V123.

## 5 Research for that task

**[Observed]** `DocumentFile.Check` (`Engine.Core/Persistence/DocumentFile.cs:92`) composes `EngineHosting.CreateDefault`
with no store, and `DesktopDocument.Reopen` (`3DEngine/DesktopDocument.cs:57`) calls it before each open. The desktop
has its store in `DesktopSession`. **[Inferred]** A parameter of the store on `Check`, which the desktop document gives,
lets the check read the asset as the next session does; a test opens a saved document with a mesh body through the check.

## 6 Recommended approach

**[Recommended]** In this order:

1. P0.105 (TASK-0262): E167, with a test that the check of a saved document with a mesh body passes with the store and
   gives `E-STL-ASSET` with no store. 1 agent hour.
2. P0.108 (TASK-0265): V124, with the change of the configuration in the log of the next run. 1 agent hour.
3. P9c and P9d: the close of the desktop and of the phone parts, which keep their rows open until the run of the owner.
4. P0.106 (TASK-0263) and P0.107 (TASK-0264): the medium and low findings, each with a test or an injection before the
   change.

## Questions for the owner

None that blocks a task. The session set two grades in the place of the owner, under the standing order of 2026-11-05:
E167 is high (the agent gave medium) and T166 is medium (the agent gave low). The owner can change them.
