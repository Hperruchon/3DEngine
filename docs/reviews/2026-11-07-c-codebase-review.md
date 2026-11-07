---
date: 2026-11-07
commit: db0138c
ledger: v0.263
previous: 2026-11-07-b-codebase-review.md
---

# Codebase review — 2026-11-07 (c)

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

The review examines `main` at `db0138c`, the merge of v0.263. Four product milestones follow the review of v0.252: R16e
(v0.256), R16f (v0.260), R16g (v0.261) and R16h (v0.263), with eleven ledger entries; the next product milestone would make
the review late. Two agents examined two areas at `031084e`, the change of R16h, whose code is the code of `db0138c`: the
desktop interface, and the reader, the phone, the CLI and the gates. Each agent proved its main findings with a probe test
that it deleted after. The session checked each high finding in the code. No review draft of Codex exists for v0.252 (the
rule of the owner of 2026-11-07), and the last review of Codex is the one of v0.254.

## Summary

- **The fixes of the last review hold.** Each of the 23 findings of the review of 2026-11-07 (b) is fixed. E173, E175,
  E179 and T168 are fixed for their forms; E180 to E182, E184, V139 and T174 hold the forms that this review found.
  **[Observed]**
- **Three new findings are high.**
  - E180: the reader refuses a text file with a zero byte, for example after `endsolid`, which the reader of v0.243 read.
    A document of v0.243 to v0.249 that imported such a file does not replay. **[Observed]** by a probe that compares the
    readers, and by reading.
  - E181: the reader refuses a name such as `solid facet normal 0 0 1 outer`, which only the reader of v0.250 read. A
    document of v0.250 to v0.253 that imported such a file does not replay. **[Observed]** by a probe and by reading.
  - V134: the ring of a sketch can be under the window of the sketch, or below the display, and since R16h the window then
    holds no Finish, no tool and no kind of a constraint. The person cannot end the sketch. ADR-0088 says that a tool
    always has a place. **[Observed]** by a probe of an agent and by reading.
- **Six new findings are medium.** E182 (two classes of files had two earlier results, v0.243 and v0.250, or v0.250 and
  v0.254, so no rule keeps both; a question for the owner), E183 (the limit of the client reads a file up to 146 MB, nine times the limit of the store),
  E184 (a lost surface with a new part waiting starts a new engine at once), V135 (a tap on a segment over a row of a
  window does two things), V136 (on a portrait display of 390 by 844, Line of the ring of a sketch is under "Frame") and
  T174 (a `NoWarn` of CA1416 in the host of the phone passes each check). **[Observed]**
- **Four new findings are low:** E185, V137, V138 and V139. **[Observed]**
- **Open findings: 0 critical, 7 high, 20 medium and 23 low.** **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 17 | `.csproj` lines in `3DEngine.sln`. The last review gave 17; `3DEngine.Android` is outside the solution. |
| Production source lines | 32,827 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests`, `3DEngine.ImageTests` and `3DEngine.VisionTests`, 302 files. The last review gave 32,271 in 301 files. |
| Test source lines | 38,524 | `Engine.Tests` (26,173 in 120 files), `3DEngine.Tests` (3,616 in 22 files), `3DEngine.ImageTests` (8,670 in 55 files) and `3DEngine.VisionTests` (65 in 1 file). The last review gave 37,689. |
| Tests | 2,077 | `dotnet test 3DEngine.sln` at the change of R16h: 1,414, 337, 323 and 3 passed, 0 failed, 0 skipped. The last review gave 2,042. |
| Gate classes | 20 | Tracked files whose name ends in `GateTests.cs`. `eng/repo-gates` gives 470 runs of 163 checks. The last review gave 20, and 470 runs of 163 checks. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 15 | Section "Open" of `docs/register.md`; the limit is 15. The last review gave 15. |
| ADRs | 80 | `docs/adr/0001` to `0061` and `0070` to `0088`, with 0003 and 0011 in `docs/adr/archive/`. In force: 78 (35 accepted, 43 amended). None is proposed. The last review gave 76. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 7 | The same method: V2, V3, T4, T31, E180, E181, V134. The last review gave 9; V125, V126, E172, E173 and E174 are fixed. |
| Findings: medium | 20 | The same method: E5, E6, E12, E13, E19, E21, E22, V4, T2, T5, T10, T17, T32, T37, E182, E183, E184, V135, V136, T174. The last review gave 19; V127, V128, V129, E175 and T168 are fixed. |
| Findings: low | 23 | The same method: E17, E24, E34, E39, E57, E84, E160, V7, V15, V29, V84, V88, T7, T13, T23, T25, T143, C1, P1, E185, V137, V138, V139. The last review gave 32; V130 to V133, E176 to E179 and T169 to T173 are fixed. |
| Lines in the rule files | 3,008 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The last review gave 2,978. |
| Sentences with "must" | 59 | `grep -o -w -i must` on the same ten files. The last review gave 59. |
| Sentences with "do not" | 55 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 55. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found. E13, T32, E84, E160 and V84 are fixed in part and count as open. A finding that the owner accepted counts as closed.

## Findings of the previous review

The lines up to T167 are the lines of the review of 2026-11-07 (b); this review did not examine them again, and no task of
this window aimed at them. Two files that they cite changed for other findings, and this review read those changes:
`3DEngine.Android/PhoneHost.cs` (E39) and `eng/android/check-apk.sh` (V88). The lines of E172 to E179, V125 to V133 and
T168 to T173 follow them, with the evidence of this review.

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
- **E39** — Open. No change of the three choices of the backend. `3DEngine.Android/PhoneHost.cs` changed for the wait (E172, TASK-0269), the lost surface (E175) and the host after the checks of the turn (E176, TASK-0272); the line that makes the backend moved, with the same backend. Register entry R-0036.
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
- **V88** — Open for the desktop, and fixed for the phone. `eng/android/check-apk.sh` changed only for the check of the storage permission (E177, TASK-0272).
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
- **E167** — Fixed. TASK-0262, v0.245. `DocumentFile.Check` takes an `IAssetSource` and gives it to the scratch session (`Engine.Core/Persistence/DocumentFile.cs:91`, `:96`). `DesktopDocument` keeps the store (`3DEngine/DesktopDocument.cs:24-29`) and passes it to the check (`DesktopDocument.cs:97`). `DesktopSession.Open` gives the store of the desktop (`3DEngine/DesktopSession.cs:48-49`). The test `The_Check_Of_An_Open_Reads_The_Asset_Of_An_Import_From_The_Store` holds the probe of review 28 (`3DEngine.ImageTests/DesktopDocumentTests.cs:135-161`).
- **E168** — Fixed. TASK-0263, v0.250. `MoveTo` keeps the results of the cut and the delete, and it throws an
  `IOException` that names the new file when both fail (`Engine.Core/Persistence/DocumentJournal.cs:178` to `186`,
  `:201` to `227`). `DocumentSession.SaveAs` gives that message in `E-IO-SAVE-FAILED` (`Engine.Core/DocumentSession.cs:108` and `:116`
  to `118`). ADR-0015 item 9 agrees (`docs/adr/0015-command-log-persistence.md:105` to `106`). The test
  `A_Failed_Copy_Whose_Cut_And_Delete_Fail_Names_The_New_File_In_The_Error` (`Engine.Tests/Persistence/DocumentFileTests.cs:796`)
  holds the form.
- **E169** — Fixed. TASK-0263, v0.250. The test of an edge of four faces requires six edges on the axis and six
  different pairs of the four names (`Engine.Tests/Features/GetFacesTests.cs:293` to `298`). An injected list of two axis
  edges passed the old assert and failed the new one (entry v0.250).
- **E170** — Fixed for its three forms, and the fix has a regression. TASK-0263, v0.250. Probe `reader`: a cut binary
  file with a "solid" header gives `E-STL-TRUNCATED` (form 23), a name "facet" or "old endsolid copy" reads (form 17 and
  the test at `Engine.Tests/Import/StlImportTests.cs:100`), and a byte order mark reads (form 12). The new rule "the name
  is the rest of the line" refuses a one-line file with a line end, and it silently drops a facet that is on the line of
  `solid` (E173).
- **E171** — Fixed. TASK-0263, v0.250. `apply` and `query` read `--kind` and `--folder` in any order before the first
  `--param` (`Engine.Cli/Cli.cs:473` to `501`), `export` reads `--folder` after its three words (`Cli.cs:198` to `207`),
  and `BuildSession` opens that store (`Cli.cs:521`). The usage says so (`Engine.Cli/Usage.cs:14` to `29`). Probe `cli`:
  `asset put --folder` and then `apply ImportStl --kind part --folder` gave `Applied`; a second `--folder` gave the usage
  error "'--folder' comes one time only"; `export stl a.3de b.stl --folder` with no path gave exit 2.
- **V119** — Fixed. TASK-0264, v0.251. `OpenPad` clears `_pendingValue` (`3DEngine.App/Ui/DesktopUi.Pad.cs:43-50`). `Use` sets it after the open (`DesktopUi.Values.cs:87-89`), and `OpenDistancePad` sets it after the open (`DesktopUi.Pad.cs:58-62`). Test `The_Pad_Of_Another_Tool_Ends_The_Pending_Chip` (`3DEngine.ImageTests/EdgeValueTests.cs`, added by TASK-0264).
- **V120** — Fixed. TASK-0264, v0.251. The ring skips a release on a chip of the last frame and on a row (`DesktopUi.Ring.cs:126-132`). The chip skips a release on a row (`DesktopUi.Values.cs:55-58`). Test `A_Tap_On_A_Chip_Over_A_Segment_Gives_The_Chip_Only` (`ToolRingTests.cs`). Note: the ring reads the chips of the last frame (`Ring` runs before `Values`, `DesktopUi.cs:548-550`), so a chip that moves in this frame is one frame late. I found no case where this gives two tools.
- **V121** — Fixed. TASK-0264, v0.251. The portrait segment is `min(0.42, (π/2 − (n−1)·0.04)/n)` (`DesktopUi.Ring.cs:86`). Theory `Each_Segment_Of_A_Portrait_Ring_Is_Between_The_Left_And_The_Top` (`ToolRingTests.cs`).
- **V122** — Fixed. TASK-0264, v0.251. `Layout` takes the inset (`DesktopUi.Ring.cs:81`, `:87`), and `Ring` passes `_inset` (`DesktopUi.Ring.cs:121`). Test `The_Portrait_Ring_Is_Inside_The_Safe_Area` (`ToolRingTests.cs`). No device ran it.
- **V123** — Fixed. TASK-0264, v0.251. `KeepAboveAnchors` runs after `Begin` for each state (`DesktopUi.Panel.cs:28-32`). The left clamp is `16 + _inset.X` (`DesktopUi.Panel.cs:264`). The comment is next to the clamp and agrees with it (`DesktopUi.Panel.cs:256-258`). Test `A_Collapsed_Panel_That_The_Person_Drags_Does_Not_Cover_Undo` (`FixedControlsTests.cs`).
- **V124** — Fixed in part. TASK-0265, v0.246. A lost surface in `engine.Render()` now gives `Session.Background`
  (`3DEngine.Android/PhoneHost.cs:177` to `185`), and the destroy line names `IsChangingConfigurations` and the mask
  (`3DEngine.Android/MainActivity.cs:303`). Two parts stay open. First, the wait for the foreground checks the turn only
  after 500 ms with no event, and a foreground event returns `true` with no check of the turn (`PhoneHost.cs:207` to
  `223`). ADR-0079 item 4 says that the wait ends a session whose turn is stale (E172). Second, the guard covers `Render`
  only: `engine.Initialize()` at `PhoneHost.cs:143` to `144` has no guard. The ledger says that no run executed the new
  path (`docs/CURRENT-STATE.md`, entry v0.246, "What stays").
- **T165** — Fixed. TASK-0263, v0.250. `Engine.Tests/Governance/DependencyDirectionGateTests.cs:666` to `669` refuse a
  `#pragma warning disable` with no code (the regex at line `689`). Lines `680` to `682` refuse each
  `dotnet_analyzer_diagnostic.` line in each `.editorconfig` and `.globalconfig` (the walk at lines `700` to `717`).
  `eng/android/check-platforms.js:33` to `42` and `66` to `78` build a probe call in each of the 7 projects and require
  `error CA1416` at the probe. Injection: the two forms of the review again, `Engine.Core/Rev29PragmaProbe.cs` with a
  blanket pragma and `Engine.Kernel/.editorconfig` with `dotnet_analyzer_diagnostic.category-Interoperability.severity =
  none`. The gate failed with both offences. The step failed with "Engine.Kernel: the build with CheckPlatformsProbe.cs
  gave no error CA1416", exit 1. A third form of the [Inferred] list, `AnalysisModeInteroperability` = `None`, also fails
  the step (exit 1). The form "a `SuppressMessage` over two lines" of the same [Inferred] list still escapes (T169).
- **T166** — Fixed for the three projects that it names. TASK-0263, v0.250. `3DEngine.App/3DEngine.App.csproj:15`,
  `3DEngine.Core/3DEngine.Core.csproj:10` and `3DEngine.Vulkan/3DEngine.Vulkan.csproj:16` set `TreatWarningsAsErrors`.
  The gate requires it (`DependencyDirectionGateTests.cs:647` to `651`), and the step reads the evaluated property
  (`check-platforms.js:48`, `63` to `64`). Control: on the clean tree the step gives "the probe gives the error CA1416" for
  each of the 7 projects, exit 0. `3DEngine.Android`, which T166 also measured as `false`, still gives a warning only, and
  the question is in no register entry (T168). The text rule of the gate misses four project forms that only the step
  refuses (T170).
- **T167** — Fixed. TASK-0263, v0.250. The affects field of ADR-0083 (`docs/adr/0083-the-mesh-body-and-the-stl-import.md:12`
  to `52`) names the eight files of T167, and ADR-0082 names `DesktopUi.Pad.cs` (`docs/adr/0082-...md:19`).
  `docs/diagnostics.md:78` names the handlers, not `DatumResolver`. The new gate `AffectsCitationGateTests` holds the
  rule. My Node.js mirror of the gate gives 27 offences at `31c3204` (the parent of the fix; 26 for ADR-0083 and 1 for
  ADR-0082, as the entry v0.250 says) and 0 at HEAD. Injection: `3DEngine.Core/Rev29CiteControl.cs` with
  `// ADR-0084 item 1` failed the gate, with that file as the only offence. The gate has holes of other forms (T171).
- **E172** — Fixed. TASK-0269, v0.254. `3DEngine.App/PhoneLifecycle.cs`, `AfterWait`, reads the turn at each return of the
  wait, and `3DEngine.ImageTests/PhoneLifecycleTests.cs` holds its eight cases. An agent read the wait loop of
  `PhoneHost.cs` again and found no fault.
- **E173** — Fixed for its forms. TASK-0269 (v0.254) and TASK-0274 (v0.257): the seven forms of v0.243 and the four forms
  of v0.250 in `Engine.Tests/Import/StlImportTests.cs` give their earlier results. A probe of this review found other
  forms: E180, E181 and E182.
- **E174** — Fixed. TASK-0269, v0.254, and TASK-0274, v0.257. `docs/demo-stl-test-sheet.md` says that the view, the
  selection and the line "Size" start again after the background, and how the size comes back.
- **E175** — Fixed for its form. TASK-0272, v0.259. A lost surface in the frame that took a chosen STL gives the new part
  (`PhoneHost.cs:198`). The new path has a risk that no run examined (E184).
- **E176** — Fixed. TASK-0272, v0.259. The host of a session comes after the checks of the turn and of the window, so each
  way out disposes it. An agent read each way out and found no fault.
- **E177** — Fixed. TASK-0272, v0.259. `eng/android/check-apk.sh` fails when the APK holds no manifest or when node
  cannot read it (exit code 2).
- **E178** — Fixed. TASK-0272, v0.259. `StlImportTests` holds a text file whose name is not UTF-8 and a cut binary file
  of fewer than 84 bytes.
- **E179** — Fixed for its form. TASK-0272, v0.259. Each new window starts with no open dialog
  (`3DEngine.Vulkan/Window.FileDialog.cs`, `ResetDialog`). A late callback of an earlier window is a new form (V139).
- **V125** — Fixed. TASK-0270, v0.255. A request of a new part takes the slot of an open
  (`3DEngine/DesktopDocument.cs`, `RequestNew`), and four tests of `StlOpenTests` hold the orders and the race.
- **V126** — Fixed. TASK-0270, v0.255. An accepted open ends the home screen. Its test raced with
  `DesktopSessionTests` on the runners; TASK-0278 (v0.262) runs the two classes in one collection.
- **V127** — Fixed. TASK-0271, v0.258. A failed open from the home screen shows its message. The engine needs a window, so
  no test holds it.
- **V128** — Fixed. TASK-0271, v0.258. Each chip under the rows has its own place; an agent broke the push of the chips,
  and `BodyRowsTests` failed.
- **V129** — Fixed. TASK-0271, v0.258. The state of a hidden body drops the kind first; `BodyRowsTests` holds it.
- **V130** — Fixed. TASK-0271, v0.258. The home screen takes the taps and the keys, and the ring offers no Move for a mesh
  body (`StlOpenTests`, `ToolRingTests`).
- **V131** — Fixed. TASK-0271, v0.258. A file dialog that fails gives a message. No test holds it.
- **V132** — Fixed. TASK-0271, v0.258. `HomeImageTests` checks "Open file", "New part" and the recent files.
- **V133** — Fixed. TASK-0271, v0.258. The three comments are true. Other false comments are a new finding (V138).
- **T168** — Fixed for its form. TASK-0272, v0.259. `3DEngine.Android/3DEngine.Android.csproj` makes CA1416 an error, and
  `eng/android/check-platforms.js` reads the property. A `NoWarn` in the host passes the step (T174).
- **T169** — Fixed. TASK-0272, v0.259. Each line of the code of the phone that names CA1416 is an offence of
  `DependencyDirectionGateTests`.
- **T170** — Fixed. TASK-0272, v0.259. The gate refuses each form of `TreatWarningsAsErrors` that T170 named.
- **T171** — Fixed. TASK-0272, v0.259. The affects gate reads the shaders and the PowerShell files; an agent put
  `// ADR-0083 item 9` into a `.vert` file, and the gate failed. A citation of an absent ADR is an offence.
- **T172** — Fixed. The entry v0.253 corrected the count of the ledger; the earlier review stays as it is.
- **T173** — Fixed. TASK-0272, v0.259. The list "Shipped" of the roadmap is in the order of the ledger, and each close of
  v0.260 to v0.263 added its line at the end.

## 1 Current architecture

**[Observed]** The redesign of the desktop has eight slices in the code. The ring holds the tools of the selected body
(ADR-0081), or, while a sketch is active, Line, Select, Circle, Arc, "Constrain" and Finish beside the sketch (ADR-0086,
ADR-0087). "Constrain" opens a second ring of the kinds of a constraint that fit the selection; one method,
`FittingConstraints`, gives those kinds to the ring and to the window of the sketch. While the ring of a sketch shows, the
window of the sketch holds no button that the ring holds (ADR-0088). The rows of the selected body are at the upper right
(ADR-0084), and the active sketch of a live extrude moves out of its body (ADR-0085).

**[Observed]** The demonstration "Open the same STL on desktop and Android" is complete in the code, and its rows P9c to P9e
wait for the run of the owner. The reader of the engine has had four rules for the name of a solid: v0.243, v0.250,
v0.254 and v0.257. A recorded import reads its asset again at each replay (ADR-0083 item 3).

**[Observed]** The next phase of K5, the cone, needs a new capability in `Engine.Contracts`: `IExtrudeOps` makes a straight
prism only, and a mesh body of `IMeshImportOps` takes no other operation (ADR-0083 item 4). The standing order recorded the
question for the owner (the entry v0.263).

## 2 Patterns in use

- **One rule for two places.** `FittingConstraints` (`3DEngine.App/Ui/DesktopUi.Sketch.cs`) gives the kinds and their
  actions to the window and to the ring, so the two cannot disagree. **[Observed]**
- **State of the immediate interface in fields.** The second ring keeps `_constrainOpen` with the key of the selection that
  it belongs to, and a new key closes it. An agent found no stale state across sketches, pads and selections. **[Observed]**
- **The order of the frame as a contract.** The ring comes before the window of the sketch, so the window reads the ring of
  the same frame (ADR-0088 item 3). The ring reads the windows of the frame before, so a tap on a segment over a window
  depends on the state of ImGui at the release (V135). **[Observed]**
- **State of the process and the tests.** The home screen and the pending import are static fields of
  `DesktopDocument`, so the tests that change them run in one collection, and `HomeStateCollectionTests` reads the source
  of each class (TASK-0278). **[Observed]**
- **A reader that must keep each earlier result.** ADR-0083 item 3 holds the reader to each result that an earlier reader
  accepted. A refused import is not in the log, so a file that each earlier reader refused is free. **[Inferred]** The
  rule needs a test that runs each earlier reader, not a list of forms (E180, E181).

## 3 Review of the code

### The reader, the phone, the CLI and the gates

#### E180 · High · The reader refuses a text file with a zero byte, which the reader of v0.243 read · Confirmed by a probe

**[Observed]** `Engine.Core/Import/StlReader.cs:37` to `42`: since TASK-0263 (v0.250), a file of 84 bytes or more that
starts with `solid` and holds a zero byte after its first 80 bytes is not text, so it gives `E-STL-TRUNCATED`. The reader
of v0.243 (`e903bef`) sent each file that starts with `solid` to the text reader, which decodes the whole file and stops at
`endsolid` (`git show e903bef:Engine.Core/Import/StlReader.cs`, lines 63 to 110). A zero byte is valid UTF-8.

**[Observed]** A probe of an agent ran the readers of `e903bef`, `45f416d` and `7959c98` and the reader of HEAD on two
files with one valid facet: `endsolid part` and then four zero bytes, and a name with a zero byte. The reader of v0.243
gives one triangle for each; the readers of v0.250 and later give `E-STL-TRUNCATED`.

**[Inferred]** A document of v0.243 to v0.249 that imported such a file does not replay: the import fails at the replay.
ADR-0083 item 3 forbids it. The comment at lines 128 to 129 and the name of the test
`Each_Text_File_That_The_Reader_Of_V0243_Read_Gives_Its_Result` say that each file that v0.243 read keeps its result; the
test has no form with a zero byte.

#### E181 · High · The reader refuses a file that only the reader of v0.250 read · Confirmed by a probe

**[Observed]** `StlReader.cs:134` to `154`, `NameEnd`: a whole facet on the line of `solid` ends the name. The file
`solid facet normal 0 0 1 outer`, then one facet and `endsolid` (and the same with `outer loop`), gives one triangle with
the reader of v0.250 to v0.253 (`45f416d`), because the name is the rest of the line. The reader of HEAD gives
`E-STL-SYNTAX`, because the name ends before `facet` and the facet has no `loop`. The readers of v0.243 (`e903bef`) and
of v0.254 (`7959c98`) refuse it too, so v0.250 is its only earlier result. **[Inferred]** A document of v0.250 to v0.253
that imported such a file does not replay.

#### E182 · Medium · Two earlier readers gave different results for one file, so no rule keeps both · Confirmed by a probe

**[Observed]** Two classes of files have two earlier results:

- A whole facet on the line of `solid`, and a name that holds no earlier `facet` or `endsolid`: v0.243 counts the facet,
  and v0.250 takes it as the name. An agent found 58,676 such files in 300,000 random text files. The reader of HEAD
  gives the result of v0.243 (TASK-0274).
- `solid endsolid` and a whole facet on the line of `solid`, then one facet and `endsolid`: v0.243 refuses it, v0.250
  gives one triangle, and v0.254 (`7959c98`, whose name ends at `facet normal` and a number) gives two, as HEAD does. The
  agent found 2,569 such files. The session found the result of v0.254 in the diff of `45f416d` and `7959c98`; the agent
  had compared v0.250 with HEAD only.

**[Observed]** So a document of v0.250 to v0.253 that imported such a file replays to another body. The comment at lines
131 to 133 names the first class only. ADR-0083 item 3 says that a change that cannot keep a result takes a new schema
version of `ImportStl`; TASK-0274 recorded the choice in a comment and made no new version. The decision and the code
disagree, so the owner says which one is correct (question Q1). **[Inferred]** No exporter writes a facet on the line of
`solid`, so a real file of these forms is rare; the question is the rule, not the risk.

#### E183 · Medium · The limit of the client reads a file up to 146 MB, nine times the limit of the store · Confirmed by a probe

**[Observed]** `3DEngine.App/StlOpening.cs:22`: `MaximumBytes = 84 + 512 × 300,000` = 153,600,084 bytes.
`Engine.Core/Assets/AssetStore.cs:15`: the store takes at most 16 MiB. So each file of 16 MiB to 146 MiB is read whole and
read by the reader, and then the store refuses it with `E-ASSET-TOO-LARGE`. A probe of an agent gave a text file of
153,600,023 bytes to `StlReader.Read`: it allocated 2,438 MiB, and gave `E-STL-TOO-LARGE`.

**[Inferred]** On the phone such a file can end the session for want of memory, which the limit of finding N2 was to
prevent. The comment at line 21 says that a text file at the limit has less than 512 bytes for each triangle; the reader
puts no bound on the bytes of a facet. Not verified: a size of -1 from SDL, which `TooLarge` lets through.

#### E184 · Medium · A lost surface with a new part waiting starts a new engine at once · Confirmed by reading

**[Observed]** `3DEngine.Android/PhoneHost.cs:198`: since TASK-0272 (E175), a lost surface with a request of a new part
gives `Session.NewPart`, and the loop at lines 78 to 83 makes the next engine at once, with no wait for the foreground. The
comment at lines 184 to 187 still says that a lost surface ends the engine "as the background does". **[Inferred]** The
next engine makes a device on a window whose surface was just lost; if that throws, the outer catch ends the run with an
exception, and the import is lost. No run on a phone examined this path.

#### E185 · Low · The verb `asset` of the CLI throws on a blank `--folder` · Confirmed by a run

**[Observed]** `Engine.Cli/Cli.cs:92` makes an `AssetStore` from the folder with no check; TASK-0272 added `ValidFolder` to
`export` and to the options of the other verbs only. A run of `engine asset put a.txt --folder " "` gave "Unhandled
exception. System.ArgumentException: The path is empty". The comment at lines 503 to 504 says that the form is fixed.

#### T174 · Medium · A `NoWarn` of CA1416 in the host of the phone passes each check · Confirmed by injection

**[Observed]** `eng/android/check-platforms.js:82` to `90` reads only `WarningsAsErrors` and `TreatWarningsAsErrors` of
`3DEngine.Android.csproj`; it reads no `NoWarn` and no `WarningsNotAsErrors`, and builds no probe in the host.
`DependencyDirectionGateTests` reads the projects that the host reaches, not the host. Injection of an agent:
`<NoWarn>$(NoWarn);CA1416</NoWarn>` in the host, and a call of `Console.ForegroundColor` in it, built with zero warnings;
the step gave exit 0, and `eng/repo-gates` gave 0 failed. Control: without the `NoWarn`, the build gave `error CA1416`.

### The desktop interface

#### V134 · High · The ring of a sketch can be under the window of the sketch or below the display, and the window then holds no Finish · Confirmed by a probe

**[Observed]** `3DEngine.App/Ui/DesktopUi.Ring.cs:105` to `113`, `SketchLayout`: the centre moves right as far as
`display.X - 16`, and the segments go down on the left of it, so a sketch right of about x 805 on a display of 1280 puts
segments under the window of the sketch (x 784 to 1264). The inner radius has no upper limit, and the clamp of y moves the
centre down when the ring is higher than the display. `DesktopUi.Sketch.cs:62`: since TASK-0277 (v0.263), the window
drops the tools, Finish and the kinds whenever a ring exists.

**[Observed]** Probes of an agent at 1280 by 720: a centre at (1000, 360) and a sketch of 60 pixels put Line and Finish
under the window, and a tap on them gives nothing; a sketch of 380 pixels puts Finish at y 836, below the display. At 390
by 844 (portrait), Circle, Arc, "Constrain" and Finish are under the window. In each case the window has no Finish, and
no key gives `FinishSketchAction`.

**[Observed]** ADR-0088 Q2 A says that a tool always has a place; the comment of `SketchLayout` and ADR-0086 item 1 say
that the band stays inside the display. ADR-0085 keeps the camera, so a sketch can be at any place on the screen in the
main flow. **[Inferred]** The person cannot end the sketch, except by an open pad that hides the ring.

#### V135 · Medium · A tap on a segment over a row of a window does two things · Confirmed by a probe

**[Observed]** `DesktopUi.Ring.cs:212` takes a release when `!ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow)`. While a
row of the window is the active item, that call gives false, and the ring comes before the window in the frame, so the
ring and the row both take the release. Probe of an agent: a centre at (900, 200) and a tap on Finish gave
`FinishSketchAction` and `ToggleSketchItemAction`. With `ImGuiHoveredFlags.AllowWhenBlockedByActiveItem` added, the same
taps gave the action of the row only. The same pattern is at `DesktopUi.Rows.cs:79` and `DesktopUi.Values.cs:76`; the
agent did not probe them.

#### V136 · Medium · On a portrait display of 390 by 844, Line of the ring of a sketch is under "Frame" · Confirmed by a probe

**[Observed]** `DesktopUi.Ring.cs:88` to `91`: the portrait ring of a sketch has an inner radius of at least 150 pixels in
the corner. Probe of an agent at 390 by 844: "Frame" is at x 140 to 234 and y 780 to 828, the middle of Line is at
(179, 800), and a tap on Line gives `FrameAction`. The window of the sketch has no Line while the ring shows. At 1080 by
2316 there is no overlap. Move of the ring of a body is under "Frame" too, from before v0.252. Not verified: the size that
the S23 Ultra gives through `SDL_GetWindowSize`.

#### V137 · Low · No test holds "a change of the selection closes the second ring" · Confirmed by injection

**[Observed]** `DesktopUi.Ring.cs:168`. An agent made the condition never true, and each of the 16 tests of `ToolRingTests`
passed. ADR-0087 item 3 gives the rule.

#### V138 · Low · Three comments say something false · Confirmed by reading

**[Observed]** `DesktopUi.Ring.cs:88` says "less than 44 pixels"; the code and ADR-0086 item 3 give 46. The head comment of
`DesktopUi.Ring.cs` describes the tools of a body only. `DesktopUi.Sketch.cs:74` says that Next point stays the second
row; while the ring shows, it is the first.

#### V139 · Low · A late callback of an earlier file dialog can free the filter of the next dialog · Confirmed by reading

**[Observed]** `3DEngine.Vulkan/Window.FileDialog.cs:56` to `63` and `70`: `ResetDialog` (E179) clears the flag of an open
dialog but leaves the earlier callback able to run, and `OpenFile` writes the new filter over the old one with no free.
**[Inferred]** A late `Chosen` of the earlier window frees the filter of the new dialog and clears its flag. Not verified:
whether SDL copies the filters, and whether a late callback occurs.

### Areas with no finding

**[Observed]** `PhoneLifecycle.AfterWait` (E172); the disposal of the host (E176); `PhoneFiles`;
`DocumentFolder.AcceptRequest`; `DesktopEngineHost.DropImport`; the affects gate with a shader; the short files of E178;
the state of the second ring across sketches, pads and selections; `SketchLift`; `DesktopDocument.RequestNew` and the home
screen; the layout of the rows. The new code calls no transcendental function in a path that writes to the log; the
`Sin`, `Cos` and `Atan2` calls are in code of the interface.

## 4 The next planned task

**[Observed]** E180, E181 and V134 are high, so a correction task comes before the next product phase (`docs/templates.md`,
section 7). The next product phase in the order of the owner is K5c, the cone, and it waits for the answer of question Q2.

## 5 Research for that task

- **The reader.** **[Inferred]** A refused import is not in the log, so the reader may give any result for a file that
  each earlier reader refused. A reader that gives each file the result of the first of these rules that accepts it keeps
  each choice of HEAD and gives back each lost result: the rule of v0.243, then the rule of HEAD, then the rule of v0.250.
  The rule of v0.254 gives the result of HEAD for each file that it accepted (the probe of the agent). **[Observed]** The
  earlier rules are short: the name of v0.243 ends at the first `facet` or `endsolid`, the name of v0.250 is the rest of
  the line, and the format choice of v0.243 sends each file that starts with `solid` to the text reader.
- **The ring of a sketch.** **[Observed]** The window of the sketch reads the ring of the same frame (ADR-0088 item 3), and
  the windows of the frame before are in `_panels`. **[Inferred]** The window can keep each button that a segment under a
  window or outside the display hides, and `SketchLayout` can keep the band inside the display and left of the window.

## 6 Recommended approach

**[Recommended]**
1. P0.115 (high, before the next product phase): E180 and E181 with a test that runs the rules of each earlier reader on
   each form and on random files, and V134 with a test of a sketch at the right edge and of a large sketch.
2. P0.116 and P0.117 (medium and low, after the next product phase): E183, E184, V135, V136, T174, then E185 and V137 to
   V139.
3. E182 waits for the answer of the owner (Q1).

## Questions for the owner

- **Q1 (E182).** A file with a whole facet on the line of `solid` was read by two earlier readers with different results.
  - **A.** Keep the results of HEAD, which are the results of v0.243 and of v0.254, and write in ADR-0083 item 3 that a
    document of v0.250 to v0.253 with such a file replays to those results. No exporter writes such a file.
  - **B.** A new schema version of `ImportStl` that records the rule of the reader in each new import, with a migration of
    the records of version 1.
  **Recommended: A.** B changes the log and `Engine.Contracts` for a form that no exporter writes. The standing order gives
  A, which changes no code; the owner checks it on 2026-11-08.
- **Q2 (K5c).** The cone, the sphere and the torus need a new capability in `Engine.Contracts`.
  - **A.** A capability that stores a closed triangle mesh as a solid of the kernel, with each vertex from the 64-gon of
    ADR-0040 (`+ - * /` and `sqrt` only), for the three primitives at once.
  - **B.** A revolve of Manifold, which computes the sine and the cosine inside, against determinism rule 1.
  **Recommended: A.** This is a change of the contract, so the standing order does not answer it.
