---
date: 2026-11-07
commit: d233679
ledger: v0.252
previous: 2026-11-07-codebase-review.md
---

# Codebase review — 2026-11-07 (b)

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

The review examines `main` at `d233679`, the merge of v0.252. Four product milestones follow the review of v0.243: P9c
(v0.247), P9d (v0.248), P9e (v0.249) and R16d (v0.252), with nine ledger entries; the next product milestone would make
the review late. Three agents examined three areas at `3815dc6`, the close of R16d, whose tree is the tree of `d233679`.
The session checked the high findings in the code. No review draft of Codex exists for v0.243 (the rule of the owner of
2026-11-07). The reviews of Codex of v0.244 to v0.249 gave ten leads: eight are confirmed (V125, V126, V127, V132 in
part, E172, E174, E175 and T172), and two are refuted as current findings.

## Summary

- **The fixes of the last review hold.** E167, E168, E169, E171, V119 to V123 and T167 are fixed. E170, V124, T165 and
  T166 are fixed for their forms: E173, E172, T169 and T168 hold the forms that a probe found after the fix.
  **[Observed]**
- **Five new findings are high.**
  - V125: the import of a chosen STL is not bound to its request of a new part, so an open by an agent at the same time,
    or a new part session that does not start, gives the import to a named document. **[Observed]** by a probe.
  - V126: a recent file of the home screen opens under the home screen again, and no control leads to the document.
    **[Observed]** by a probe.
  - E172: a stale session of the phone takes the foreground event of a new activity and keeps the window, against
    ADR-0079 item 4. **[Observed]** by reading; the order of the events of SDL is inferred.
  - E173: the fix of E170 in TASK-0263 changed the result of the STL reader for files that v0.243 read: a facet on the
    line of `solid` is lost with no message, and a file of one line with a line end is refused. A recorded import reads
    its asset again at each replay, so such a document replays to another body. **[Observed]** by a probe that compares the
    two readers.
  - E174: criterion 9 of the test sheet of the STL demonstration fails as written, because the camera, the selection and
    the line "Size" go at each trip into the background (ADR-0073). **[Observed]** by reading.
- **Five new findings are medium.** V127 (a failed open from the home screen shows no message), V128 (two chips under the
  rows go to one place), V129 (the state of a hidden body hides the name in the rows), E175 (a lost surface in the frame of
  a chosen STL gives an empty new part) and T168 (`3DEngine.Android` itself treats CA1416 as a warning, and the two open
  questions of TASK-0263 are in no register entry). **[Observed]**
- **Thirteen new findings are low:** V130 to V133, E176 to E179 and T169 to T173. **[Observed]**
- **A record was false.** The entry v0.244 and the review of 2026-11-07 say "fifteen" new low findings; the list holds
  eleven (T172). The entry of this review corrects the ledger, and the earlier review stays as it is. **[Observed]**
- **Open findings: 0 critical, 9 high, 19 medium and 32 low.** **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 17 | `.csproj` lines in `3DEngine.sln`. The last review gave 17; `3DEngine.Android` is outside the solution. |
| Production source lines | 32,271 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests`, `3DEngine.ImageTests` and `3DEngine.VisionTests`, 301 files. The last review gave 31,521 in 296 files. |
| Test source lines | 37,689 | `Engine.Tests` (26,064 in 120 files), `3DEngine.Tests` (3,616 in 22 files), `3DEngine.ImageTests` (7,944 in 52 files) and `3DEngine.VisionTests` (65 in 1 file). The last review gave 36,758. |
| Tests | 2,042 | `dotnet test 3DEngine.sln` at the change of R16d: 1,410, 337, 292 and 3 passed, 0 failed, 0 skipped. The last review gave 2,014. An agent counted 298 in `3DEngine.ImageTests` while the six probe tests of another agent were in the same tree. |
| Gate classes | 20 | Tracked files whose name ends in `GateTests.cs` (new: `AffectsCitationGateTests`). `eng/repo-gates` gives 470 runs of 163 checks. The last review gave 19, and 468 runs of 161 checks. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 15 | Section "Open" of `docs/register.md`; the limit is 15. The last review gave 15. |
| ADRs | 76 | `docs/adr/0001` to `0061` and `0070` to `0084`, with 0003 and 0011 in `docs/adr/archive/`. In force: 74 (32 accepted, 42 amended). None is proposed. The last review gave 75. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 9 | The same method: V2, V3, T4, T31, V125, V126, E172, E173, E174. The last review gave 6; E167 is fixed, and V124 is fixed for its forms. |
| Findings: medium | 19 | The same method: E5, E6, E12, E13, E19, E21, E22, V4, T2, T5, T10, T17, T32, T37, V127, V128, V129, E175, T168. The last review gave 15; T166 is fixed for its forms. |
| Findings: low | 32 | The same method: E17, E24, E34, E39, E57, E84, E160, V7, V15, V29, V84, V88, T7, T13, T23, T25, T143, C1, P1, V130 to V133, E176 to E179, T169 to T173. The last review gave 30; E168, E169, E171, V119 to V123 and T167 are fixed, and E170 and T165 are fixed for their forms. |
| Lines in the rule files | 2,978 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The last review gave 2,960. |
| Sentences with "must" | 59 | `grep -o -w -i must` on the same ten files. The last review gave 59. |
| Sentences with "do not" | 55 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 55. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found. E13, T32, E84, E160 and V84 are fixed in part and count as open. A finding that the owner accepted counts as closed.

## Findings of the previous review

The lines up to T164 are the lines of the review of 2026-11-07; this review did not examine them again, and no task of
this window aimed at them. Two files that they cite changed for other findings, and this review did not examine those
lines again: `Engine.Cli/Cli.cs` (E13) for the option `--folder` (E171, TASK-0263), and `3DEngine.Android/PhoneHost.cs`
(E39) for the new part and the lost surface (TASK-0260, TASK-0265). The lines of E167 to E171, V119 to V124 and T165 to
T167 follow them, with the evidence of this review.

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

## 1 Current architecture

**[Observed]** The demonstration "Open the same STL on desktop and Android" is complete in the code: the desktop and the
phone have a home screen with "Open file", "New part" and the recent files (ADR-0083 item 7). A chosen STL is checked
with the reader of the engine, put in the store, and imported into a new part document as the first command of the next
session; the static `DesktopDocument` and the `PhoneFiles` of the process carry the request and its import from one session
to the next. The phone ends a session whose surface is lost as the background does, and a session whose turn is stale
(ADR-0079 item 4). The redesign has the rows of the selected body at the upper right (R16d, ADR-0084), over the tool ring
and the chips. A new gate holds the affects field of each ADR from 0082 against the files that cite it, and a step of the
pipeline builds a probe call in each project that the phone reaches and requires the error CA1416.

## 2 Patterns in use

- **A request that passes between sessions is not bound to its target.** The import of a chosen STL waits in a static slot
  for "the next session", and a different session can take it: a named document on the desktop (V125), or the old session
  before an empty new part on the phone (E175). **[Observed]**
- **A state of the process outlives the reason for it.** The home screen stays after an open by name (V126), and the flag
  of an open dialog stays when its callback does not come (E179). **[Observed]** and **[Inferred]**
- **A fix of a reader changes an accepted result.** The fix of E170 read more files, and it also changed the result of two
  forms that v0.243 read, with no rule about replay (E173). **[Observed]** by a probe.
- **A check of one place misses the next place again.** The wait of the phone checks its turn on a quiet wait only (E172);
  the warning rule holds the projects that the phone reaches, not the host itself (T168); the affects rule reads no shader
  (T171). **[Observed]**
- **A record states a property that the code does not have.** The test sheet promises the view after the background
  (E174), and the entry v0.244 counts fifteen findings for eleven (T172). **[Observed]**

## 3 Review of the code

### The phone and the engine

#### E172 · High · A stale session in the wait for the foreground takes the foreground event of a new activity and keeps the window · Confirmed by reading

Codex lead 1. The code path is confirmed by reading. The order of the events of SDL is inferred.

- **[Observed]** `WaitForForeground` checks `SessionTurns.Phone.IsLast(turn)` only when `SDL_WaitEventTimeout` returns
  `false`, that is after 500 ms with no event (`3DEngine.Android/PhoneHost.cs:207` to `216`). A `DID_ENTER_FOREGROUND`
  event returns `true` with no check of the turn (`PhoneHost.cs:222` to `223`). Each other event, for example a window
  event or a background event, starts a new wait of 500 ms, so a stream of events postpones the check.
- **[Observed]** The new create takes its turn in `OnCreate` (`3DEngine.Android/MainActivity.cs:284`). Its session does
  not read events until its window exists. It waits in `HoldWindow` for 10 seconds and then throws `TimeoutException`
  (`PhoneHost.cs:55` to `56`). The SDL event queue is one queue for the process, so the stale session is the only reader
  of the events in that time.
- **[Observed]** After the return `true`, the loop calls `Foreground` again with the window of the stale session
  (`PhoneHost.cs:75` to `77`, `:66`). Neither `Foreground` nor the loop checks the turn again.
- **[Observed]** ADR-0079 item 4 and its history line of TASK-0265 say that "the wait for the foreground ends a session
  whose turn is stale" (`docs/adr/0079-the-activity-may-start-again.md`, History, 2026-11-07, TASK-0265). The code ends it
  only when the wait is quiet for 500 ms. The decision and the code disagree.
- **[Inferred]** A new create resumes in less than 500 ms (onCreate, onStart, onResume). If its `DID_ENTER_FOREGROUND`
  arrives before a quiet 500 ms, the stale session resumes and keeps the only window (`3DEngine.App/SessionTurns.cs:25`).
  The new session ends with `TimeoutException` after 10 seconds. Its `Main` returns, and the new activity closes. The
  emulator job then fails as in V124. If SDL queues `SDL_EVENT_QUIT` at the destroy and before the new events, the wait
  returns `false` first, and no fault occurs. The source of SDL is not in this repository. The package holds
  `SDL3-CS.aar`, and its inner jar uses a compression that `unzip` and `tar` of this computer do not read, so I could not
  read `SDLActivity.onDestroy`.
- **[Recommended]** Check the turn at each return of the wait, also for an event: return `false` when
  `turn != 0 && !IsLast(turn)`, before the switch at `PhoneHost.cs:218`. Check it again at the top of each pass of the
  loop at `PhoneHost.cs:64`. Then the code agrees with ADR-0079 item 4.

#### E173 · High · The new name rule of the STL reader drops a facet with no message, and it refuses a one-line file that has a line end · Confirmed by a probe

- **[Observed]** The text reader takes the words after the first line end (`Engine.Core/Import/StlReader.cs:80` to `83`).
  The words of the first line, other than `solid`, are the name. Only a file with no line end uses the old rule "the name
  ends at the first `facet` or `endsolid`" (`StlReader.cs:84` to `85`).
- **[Observed]** Probe `reader`, with the reader of HEAD and the reader of `e903bef` (v0.243) compiled side by side:
  - `solid part facet ... endfacet` on line 1, then a second facet on its own lines: HEAD gives 1 triangle and no error.
    v0.243 gives 2 triangles. The first triangle is lost with no message.
  - A file of one line with a final LF (form 7) or CRLF (form 8): HEAD gives `E-STL-SYNTAX` "The text STL file ends
    before "endsolid"". v0.243 reads 1 triangle.
  - `solid facet normal ...` with the facets on line 1 and a final LF (form 9): HEAD gives `E-STL-SYNTAX`. v0.243 reads it.
- **[Observed]** The comment at `StlReader.cs:78` to `79` says that "a file of one line has no end of that line". A file
  of one line usually ends with a line end, as `echo` and most editors write it. The comment is false for that file.
- **[Observed]** The handler reads the asset again at each apply, open and replay (`Engine.Core/Commands/ImportStlCommandHandler.cs:69`
  to `75`, ADR-0083 item 2). `ImportStl` keeps the schema version 1 (`ImportStlCommandHandler.cs:21`).
- **[Inferred]** A document that recorded an import of such a file between v0.243 and v0.249 now replays to another
  body (one triangle less), or it does not open (`E-IO-LOAD-FAILED` with `E-STL-SYNTAX`). Each change of the reader
  changes the replay of an import that was accepted before it. ADR-0083 does not say how a change of the reader is
  versioned, so this is the "stop and ask" of `CLAUDE.md` (an ADR is not clear about replay determinism). The common
  forms read correctly: "solid" alone, CRLF, CR only, tabs, leading blank lines, a UTF-8 name, a non-breaking space
  (forms 1 to 6, 10 to 12, 14, 17, 19 to 21). The forms of the probe are rare, so the probability is low and the effect
  is silent.
- **[Recommended]** Keep the old rule on the first line: the name ends at the line end, or at a word `facet` that
  `normal` follows, whichever comes first. A line with no word after the name then gives the old result for each form
  above. Add forms 7, 8 and the mixed file as tests that compare with the reader of v0.243. Ask the owner for a rule in
  ADR-0083: a change of the reader that changes the result of an accepted file must keep the old result for the
  recorded version, or it must have a new schema version of `ImportStl`.

#### E174 · High · Criterion 9 of the STL test sheet fails as written: the view, the selection and the line "Size" go at each trip into the background · Confirmed by reading

Codex lead 3, which the main session confirmed already. I confirm it, and I add the selection and the size line.

- **[Observed]** `docs/demo-stl-test-sheet.md:47` to `48` says "The bracket and the view are still there". Criterion 9
  (`demo-stl-test-sheet.md:65`) says "the part and the view are the same after 10 seconds in the background".
- **[Observed]** ADR-0073, Consequences: "the selection, the open pad and the camera go at each trip into the
  background" (`docs/adr/0073-the-phone-cuts-and-keeps-its-document.md:79`).
- **[Observed]** Each foreground makes a new engine and loads `SessionScene.Create()` (`3DEngine.Android/PhoneHost.cs:143`
  to `145`). That camera has the distance 40, the yaw 30 and the pitch 20 (`3DEngine.App/SessionScene.cs:13`).
- **[Observed]** Yes, the part is framed again after the return. The new `SceneRenderer` starts with no body, and the
  first `Update` frames the visible bodies when their count goes from 0 to more than 0
  (`3DEngine.App/SceneRenderer.cs:63`, `:101` to `110`). `Camera.Frame` sets the target and the distance only
  (`3DEngine.Core/Models/Camera.cs:193` to `209`), so the view is the framed view from yaw 30 and pitch 20. The turn and
  the zoom of step 4 go. After a "Frame" at the end of step 4, the direction of the view goes too.
- **[Observed]** The selection is in `BodyActions` of the engine (`3DEngine.App/NativeThreeDEngine.cs:79`, `:111`), so it
  goes. `DesktopUi` is new, and `_dimensions` is false by default (`3DEngine.App/Ui/DesktopUi.cs:280`). Only the import
  turns it on (`NativeThreeDEngine.cs:139`). So the line "Size" with "mm (assumed)" of step 3 does not show after
  the return.
- **[Observed]** `demo-stl-test-sheet.md:95` to `96` says that the emulator showed the quiet end of a session at a new
  start. The emulator showed the end before the window (run 42323457861, the last review, V124). The quiet end after a
  lost surface did not run (`docs/CURRENT-STATE.md`, entry v0.246, "What stays").
- **[Inferred]** The owner will tick "fail" on criterion 9 on the S23. By `demo-stl-test-sheet.md:52` to `53` that makes
  a correction task, and the row P9d does not close.
- **[Recommended]** Change the sheet, or change the code, by a decision of the owner. The sheet must say: "The bracket
  is still there and shows whole. The app frames it again from the start view, so the turn and the zoom of step 4 go.
  The selection and the line Size go too (ADR-0073, Consequences). Select the body to see its size again." Criterion 9:
  "Phone only: the same part shows whole, framed, after 10 seconds in the background." Change line 95 to 96 to say that
  the end after a lost surface ran on no emulator and no device. If the owner wants the view to stay, keep the camera
  of the session in `PhoneHost` across engines. That is a change of ADR-0073.

#### E175 · Medium · A lost surface in the frame that queues an STL import gives the import to the old document and then an empty new part · Confirmed by reading

Codex lead 2.

- **[Observed]** `ApplyPanel` runs inside `Render`, before `presenter.RenderFrame` (`3DEngine.App/NativeThreeDEngine.cs:725`,
  `:734`). `ApplyPanel` calls `TakeOpenedFile` (`NativeThreeDEngine.cs:351`). A good file calls
  `_document.RequestNew(import)` (`NativeThreeDEngine.cs:176` to `177`), which sets the import and the request
  (`3DEngine.App/PhoneFiles.cs:233` to `237`).
- **[Observed]** When `RenderFrame` throws `ErrorSurfaceLostKHR`, `Foreground` returns `Session.Background`
  (`PhoneHost.cs:181` to `185`) before it reads `files.TakeRequestedNew()` (`PhoneHost.cs:188`). The request stays.
- **[Observed]** At the return, the loop keeps the same `host` (`PhoneHost.cs:75` to `77`). The new engine takes the
  import in `Initialize` and applies it to the old session (`NativeThreeDEngine.cs:119` to `122`). After the first frame,
  `TakeRequestedNew()` is true (`PhoneHost.cs:188` to `189`). The loop disposes that session and makes an empty one
  (`PhoneHost.cs:67` to `72`). The import was taken one time (`PhoneFiles.cs:242`), and `Home` is false
  (`NativeThreeDEngine.cs:176`), so the new part is empty and shows no message.
- **[Inferred]** The trigger is a lost surface in the one frame that takes the result of the picker. That frame comes
  just after the return from the picker, which is also a time when Android changes the surface. The person sees an empty
  part for one good file. The work-around is to open the file again. If the activity was destroyed, the next session has
  a new `PhoneFiles` (`PhoneHost.cs:47`), so the import is lost and the home screen shows. That is correct.
- **[Recommended]** In `Foreground`, read `files.TakeRequestedNew()` also before the return of `Session.Background` at
  `PhoneHost.cs:184`, and return `Session.NewPart` when it is true. Or move `TakeOpenedFile` out of `Render`, after the
  present. Add a test with no GPU: a request, then a background, then the next foreground applies the import to a new
  session.

#### E176 · Low · A stale session and a timeout of the window do not dispose the session host and its native backend · Confirmed by reading

- **[Observed]** At v0.243 the host was `using var host` (`git diff e903bef..HEAD -- 3DEngine.Android/PhoneHost.cs`).
  TASK-0260 made it `var host` (`PhoneHost.cs:48`), with a `try` and `finally` that start after the window
  (`PhoneHost.cs:62`, `:80` to `83`).
- **[Observed]** The returns of `Stale(turn)` at `PhoneHost.cs:53` to `54` and `:57` to `58`, and the
  `TimeoutException` at `:55` to `56`, leave `Run` before that `try`. `LocalSessionHost.Dispose` (`3DEngine.App/LocalSessionHost.cs:120`
  to `124`) does not run, so the `DocumentSession` does not close and the `ManifoldGeometryBackend` is not freed.
- **[Inferred]** With the hint of ADR-0079 the process continues, so each stale session leaks one native backend. The
  leak is small, and a stale session is rare.
- **[Recommended]** Put the `try` and `finally` directly after `PhoneHost.cs:48`, or make the host after the second
  `Stale` check.

#### E177 · Low · The check of the storage permission in check-apk.sh passes when node or the manifest is absent · Confirmed by a probe

- **[Observed]** The check is the condition of an `if` (`eng/android/check-apk.sh:61`). `set -e` does not act on the
  condition of an `if`. When `node` is not found (exit 127), or `readFileSync` throws because the manifest is absent
  (exit 1), the condition is false, and the check passes.
- **[Observed]** Probe `apk`: a manifest with `READ_EXTERNAL_STORAGE` in UTF-16 is refused. A manifest path that does
  not exist passes. The same command with `node` not on `PATH` passes.
- **[Inferred]** Each runner of the pipeline has node, and each APK has `AndroidManifest.xml`, so the risk is low. The
  rule of `CLAUDE.md` says "an absent tool gives an empty result, and an empty result is not evidence".
- **[Recommended]** Give the node script three exit codes: 0 no permission, 1 a permission, 2 an error. Fail the check
  on each code other than 0. Check `[ -f "$work/AndroidManifest.xml" ]` first.

#### E178 · Low · Three forms of a file get a code of the reader that ADR-0083 item 3 does not give · Confirmed by a probe

- **[Observed]** A text file with a Latin-1 byte in its name (form 13), or with a zero byte after `endsolid` (form 15),
  gives `E-STL-TRUNCATED` with the message "a binary STL file with the count of its header has 40371717134" bytes
  (`StlReader.cs:34` to `39`, `:125` to `126`). v0.243 gave `E-STL-SYNTAX` for form 13. The name is not read, so a byte of
  the name that is not UTF-8 refuses a file whose triangles read.
- **[Observed]** A cut binary file of 83 bytes whose header starts with "solid" (form 26) goes to the text reader,
  because the rule needs 84 bytes (`StlReader.cs:34`), and it gives `E-STL-SYNTAX`. ADR-0083 item 3 gives
  `E-STL-TRUNCATED` for a binary file of the wrong length.
- **[Inferred]** Each file is refused, as the ADR wants, but the message speaks of a binary count for a text file. The
  forms are rare.
- **[Recommended]** For a file that starts with `solid` and is not valid text, give `E-STL-SYNTAX` with "is not UTF-8
  text" when it has no zero byte in its first 80 bytes, else `E-STL-TRUNCATED`. Or skip the bytes of the name line in the
  UTF-8 check. Add the three forms as tests.

#### E179 · Low · The flag of an open dialog is cleared only by the callback of SDL, so a lost callback blocks "Open file" for the life of the process · Confirmed by reading

- **[Observed]** `_dialogOpen` and `_dialogResult` are static (`3DEngine.Vulkan/Window.FileDialog.cs:13` to `15`), so
  they pass from a session to the next session of the process. Only the callback `Chosen` clears `_dialogOpen`
  (`Window.FileDialog.cs:57`). While it is set, `OpenFile` returns false (`Window.FileDialog.cs:23` to `24`), and the
  engine shows "A file dialog is open already." (`NativeThreeDEngine.cs:377` to `379`).
- **[Inferred]** If Android destroys the activity while the picker is open, and SDL does not call the callback for the
  new activity, each later session of the process cannot open a file. The work-around is to stop the app. The source of
  SDL is not in this repository, so I could not check if the callback comes.
- **[Recommended]** Clear the static state of the dialog when a session of the phone starts (a new turn), or keep the
  flag in the `Window` instance. Add a line to the log when a dialog result arrives for an earlier session.

#### Probes run of this section

All probes are in `C:\Users\Henri\AppData\Local\Temp\claude\5c362f61631fddd6\fce9d0dc-a16b-4db8-aa8b-1ad5b21f4416\scratchpad\review29\probe-engine`.
They use copies of the DLLs that the checkout built at 10:45 (after HEAD at 10:41), or the `engine.exe` of the checkout.
- `reader`: a console program with `Engine.Core.dll` of HEAD and the source of `StlReader.cs` of `e903bef` compiled as
  `OldReader`. 26 forms and one mixed file. HEAD reads forms 1 to 6, 10 to 12, 14, 17, 19 to 22 and 24. HEAD refuses forms
  7, 8 and 9, which v0.243 reads (E173). The mixed file gives 1 triangle with HEAD and 2 with v0.243 (E173). Forms 13, 15,
  18 and 26 give a code that item 3 does not give (E178). The text values 0.1, 0.2 and 0.3 give the bits `3dcccccd`,
  `3e4ccccd` and `3e99999a`, the nearest floats.
- `cli`: `engine asset put bracket.stl --folder <scratch>`, then `engine apply ImportStl --kind part --folder <scratch>`:
  Applied, version 1. A second `--folder`: usage error. `export stl a.3de b.stl --folder` with no path: exit 2 (E171 fixed).
- `apk`: the node command of `check-apk.sh:61` on a manifest with the permission (refused), a manifest that does not
  exist (passed), and with no node on `PATH` (passed) (E177).
- `aar`: an attempt to read `SDLActivity` from `SDL3-CS.aar` for E172. The inner jar uses a compression that `unzip`
  and `tar` do not read; no result.
- `git -C C:\Users\Henri\source\repos\3DEngine-review29 status --short` gave no line after the probes.

**No finding.** I checked these items and found them correct.
Determinism of the reader: the new code uses `StartsWith`, `IndexOf`, `Utf8.IsValid`, `TrimStart`, `IndexOfAny` and
`Split` with no separator (`StlReader.cs:33`, `:80` to `82`, `:125` to `134`). None is a call of rules 1, 3 or 4, and
none depends on the culture or on ICU: `char.IsWhiteSpace` uses the tables of .NET. A text file and a binary file cannot
collide: a text file has no zero byte, so its count at bytes 80 to 83 is at least `0x09090909`, and the length that the
count gives is more than 7 GB (`StlReader.cs:24` to `28`).
The handler of the import: it reads the asset at each apply and replay, it refuses with `E-STL-ASSET` with no store, and
the reader gives its own code (`ImportStlCommandHandler.cs:65` to `75`). No change since v0.243.
`PhoneFiles`: `RequestNew`, `TakeRequestedNew` and `TakeImport` use `Volatile` and `Interlocked`, so a request and its
import pass from one session to the next (`PhoneFiles.cs:233` to `242`). The phone has no folder, so the panel has no
file controls (`NativeThreeDEngine.cs:588`).
The new part path: `Foreground` disposes its engine before the loop disposes the old host and makes the new one
(`PhoneHost.cs:143`, `:67` to `72`), and the `finally` disposes the last host (`PhoneHost.cs:80` to `83`).
`ISessionHost.cs` and `LocalSessionHost.cs`: the new members (`Recent`, `RequestNew`, `TakeImport`, `Home`, `HasFolder`)
and the store parameter (`LocalSessionHost.cs:104` to `111`) agree with ADR-0083 items 2 and 7.
`check-apk.sh:61`: the binary manifest holds its strings in UTF-16LE, and the check finds each `*_EXTERNAL_STORAGE` and
`READ_MEDIA_*` permission in both encodings.
E168, E171 and E167 are fixed, as the status lines say.

### The desktop and the UI

#### V125 · High · The import of a chosen STL is not bound to its request of a new part, so a named document can receive it · Confirmed by a probe

- **[Observed]** `RequestNew` writes the static `_import` and then `_requested` with no check of `_pending` (`3DEngine/DesktopDocument.cs:47`, `:65-69`). `Reopen` takes `_pending`, awaits the check, and then writes `_requested = path` (`DesktopDocument.cs:87-101`). Nothing clears `_import` when `_requested` changes.
- **[Observed]** A request of a new part from the window does not set `DocumentFolder.OpenAccepted` (`Engine.Api.Http/DocumentFolder.cs:43-52`; only `Folder.Open` sets it). So the surface still accepts `POST /document/open` and each command after the request (`Engine.Api.Http/Endpoints/DocumentEndpoint.cs:52-60`).
- **[Observed]** The host reads `RequestedOpen` only after the window loop ends and the engine is disposed, while the surface still runs (`3DEngine/DesktopEngineHost.cs:97-105`). The next session takes the import in `Initialize` whatever its file is (`3DEngine.App/NativeThreeDEngine.cs:119-122`), and applies `ImportStl` to it (`NativeThreeDEngine.cs:133-135`).
- **[Observed]** Probe `Probe1`: session 1 on `first.3de` calls `RequestNew(bracket.stl)`, then an HTTP `POST /document/open {"name":"plate"}`. `OpenAccepted` was `False` after `RequestNew`. The reply was 202. Session 2 held `plate.3de`, and `TakeImport()` gave the import of `bracket.stl`.
- **[Observed]** Probe `Probe6`, a second path: the start of the new part session fails (an injected failure, for example a fixed port that another program took). `RunSessions` starts the previous file again (`DesktopEngineHost.cs:83-90`, finding E150). That session on `first.3de` got the import from `TakeImport()`.
- **[Inferred]** In the program, `Initialize` applies `ImportStl` to `plate.3de` (path 1) or to the previous document (path 2). The journal of a named file then holds a command that the person did not ask for in that file. Path 1 needs the check of the agent to end between the frame of the choice and `DesktopEngineHost.cs:104`, so it is a narrow race. The opposite order is also possible: an open accepted during the frame, then `RequestNew` in `TakeOpenedFile` overwrites `_requested` (`NativeThreeDEngine.cs:145`, `:176-177`). The agent then got 202 for `plate.3de`, and a new part opens.
- **[Recommended]** Keep the request and its import as one record, written with one `Interlocked.CompareExchange` on the same slot as `_pending`, so the first request wins and the second one gets a refusal. Set `OpenAccepted` for a request of a new part too. Take the import only in the session that the request of the import made (compare the requested path), and clear it when the host starts another file. Add both probes as tests of `StlOpenTests`.

#### V126 · High · A recent file of the home screen opens under the home screen again, and a tap on it opens it again with no end · Confirmed by a probe

- **[Observed]** A recent row gives `OpenDocumentAction(name)` (`3DEngine.App/Ui/DesktopUi.Home.cs:52-56`). The host applies it with `OpenDocument`, which does not change `Home` (`NativeThreeDEngine.cs:373-374`, `:602-611`). Only "New part" and a chosen STL set `Home = false` (`NativeThreeDEngine.cs:176`, `:381-383`). `_home` is static and starts true (`3DEngine/DesktopDocument.cs:48`).
- **[Observed]** `Initialize` shows the home screen when no import is waiting and `Home` is true (`NativeThreeDEngine.cs:123-124`). No code calls `ShowHome(null)`, and the home window has no close control (`DesktopUi.Home.cs:40`). While `_home` is not null, the panel, the ring, the chips and the rows do not show (`3DEngine.App/Ui/DesktopUi.cs:533-539`).
- **[Observed]** Probe `Probe2`: session 1 opened `plate` by name, as the recent row does. Session 2 held `plate.3de`, and `Home` was still `True`.
- **[Inferred]** The person taps "plate" and sees the home screen again, now over `plate.3de`. A second tap on "plate" passes the check (the held file, `DesktopDocument.cs:97`), ends the session, and shows the home screen again. No control of the home screen leads to the document. The work-around is "New part", then "Open" by name in the panel. The comment at `DesktopDocument.cs:73` says "a choice there ends it for the program", which the code does not do for a recent file. The same holds for an open by an agent during the first session, and for a start with `--file` (the first session always shows the home screen). Codex graded this medium; I grade it high because the document opens where the person cannot reach it.
- **[Recommended]** Set `Home = false` when the host applies an `OpenDocumentAction` that the check accepts (or in `Reopen` on acceptance), and hide the home screen of the current session. Decide with the owner if `--file` skips the home screen; ADR-0083 item 7 does not say. Add a test of the recent row through `RunSessions`.

#### V127 · Medium · A failed open of a recent file shows no message, because the home screen hides the line of the document · Confirmed by a probe

- **[Observed]** `OpenDocument` gives its refusal to `_documentMessage` (`NativeThreeDEngine.cs:373-374`, `:610`). That text shows only in the File section of the panel (`DesktopUi.Panel.cs:308-312`), and `Frame` does not draw the panel while the home screen shows (`DesktopUi.cs:533-542`). The message window of the home screen takes only `ShowMessage` (`DesktopUi.Home.cs:31`, `DesktopUi.cs:559-560`).
- **[Observed]** Probe `Probe3`: with the panel, the line "E-IO-LOAD-FAILED: ..." had the rectangle (30, 182, 871, 208). With the home screen, the line had no rectangle, and `Message` was null.
- **[Inferred]** A recent file that another program holds, or that does not load, gives no reaction to the tap. The text "A file dialog is open already." (`NativeThreeDEngine.cs:378-379`) is lost on the home screen in the same way. The work-around is "New part" and the panel.
- **[Recommended]** While the home screen shows, give a refusal of an open to `ShowMessage` with its code, as the STL path does. Add a test that taps a recent row of a file that does not open.

#### V128 · Medium · Two chips that ADR-0084 item 4 moves under the rows go to the same place, and a tap opens the pad of the hidden chip · Confirmed by a probe

- **[Observed]** `OutOfRows` moves each chip on the rows to `area.W + 8` at the same x (`3DEngine.App/Ui/DesktopUi.Rows.cs:127-133`). It runs after the placement that keeps the chips apart (`DesktopUi.Values.cs:44-51`), so it can put two chips on one place. The chips are drawn in their order, so the last one is on top (`DesktopUi.Values.cs:62-75`), but `ChipAt` gives the first one (`DesktopUi.Values.cs:78-79`).
- **[Observed]** Probe `Probe5` at 1280 by 720: a distance chip at (1100, 40) and a length chip at (1100, 110), both on the rows (which end at 148). Both chips went to (1053, 156, 1147, 200). A touch tap on the centre of the visible chip, the length "60 mm", gave `EditDistanceAction`, and no pad "Value".
- **[Inferred]** The person sees the length and gets the pad of the distance. The title of the pad says "Set distance", so a careful person sees the error; an apply sets the wrong value. This needs two value edges near the upper right of the display, for example a body near the upper right with the rows over it.
- **[Recommended]** Run the separation of `DimensionOverlay.Place` again after the move, or give the area of the rows to the placement as a blocked area. Add the probe as a row of `BodyRowsTests`.

#### V129 · Medium · The state of a hidden body is wider than the column of the rows, so the name shows as "..." and the state crosses the left edge · Confirmed by a probe

- **[Observed]** The title row draws the state at `right − stateWidth` and fits the name into `RowsWidth − stateWidth − 12` (`DesktopUi.Rows.cs:86-89`). `Fit` gives "..." when the width is too small or negative (`DesktopUi.Rows.cs:111-123`). Nothing limits the state.
- **[Observed]** Probe `Probe4`, column 904 to 1264 at 1280 by 720: "Extrude · correct" is 238 px and the name is "Plate". "Extrude · correct · hidden" is 365 px, it starts at 899, and the name is "...". "Mesh · correct · hidden" is 323 px and the name is "...". "Extrude · failed E-FEATURE-REBUILD · hidden" is 603 px and starts at 661, 243 px left of the column, over the "..." of the name.
- **[Observed]** The whole state is drawn in red for a fault, also the kind and "hidden" (`DesktopUi.Rows.cs:88`). ADR-0084 item 2 says "failed" and the code are red. `The_State_Gives_Correct_Or_Failed_With_The_Code_And_Hidden` checks the text only, not its width (`3DEngine.ImageTests/BodyRowsTests.cs:86-92`).
- **[Inferred]** For each hidden body, the first row has no name, against ADR-0084 item 2 ("The first row gives the name of the body"). The panel still shows the name. The ledger v0.252 says the first picture showed the state past the right edge; that case is corrected, and this case is the left edge.
- **[Recommended]** Put the state on a second line when it does not fit, or fit the state first and give the name a minimum width. Draw only "failed" and the code in red. Add a width check for the hidden and the failed states to `BodyRowsTests`.

#### V130 · Low · The home screen leaves the gizmo, the scene taps and the keys live on the document behind it, and the tools offer Move for a mesh body · Confirmed by reading

- **[Observed]** The home screen hides the panel, the ring, the chips and the rows only (`DesktopUi.cs:533-539`). The home window is a small rectangle in the middle (`DesktopUi.Home.cs:37-40`), so a tap outside it goes to the intents. `GizmoLayout` has no check of the home screen (`NativeThreeDEngine.cs:282-289`). The keys of Undo and Redo reach the intents (`NativeThreeDEngine.cs:258-263`).
- **[Observed]** An open of an STL selects the body (`NativeThreeDEngine.cs:136-138`). The ring always holds Move (`DesktopUi.Ring.cs:114`), and the gizmo shows on each selected visible body. `Translate` refuses a mesh body with `E-MESH-UNSUPPORTED` (`Engine.Core/Commands/TranslateCommandHandler.cs:75-77`).
- **[Inferred]** In step 5 of `docs/demo-stl-test-sheet.md` ("Home" after the bracket), the arrows of the gizmo show on the bracket beside the home screen. A drag on them, or Ctrl+Z, acts on the document behind the home screen, and its message goes to the hidden panel. On a mesh body, Move of the ring and the gizmo give only a refusal. The step still passes.
- **[Recommended]** Give no gizmo and no scene intent while the home screen shows. Leave Move out of the ring and the gizmo for a mesh body, as ADR-0083 item 4 permits the view only.

#### V131 · Low · A file dialog that fails gives no message · Confirmed by reading

- **[Observed]** `Chosen` gives a null path both when the person closes the dialog and when SDL reports an error (`files` null) (`3DEngine.Vulkan/Window.FileDialog.cs:50-57`). `TakeOpenedFile` of the engine returns with no message for a null path (`NativeThreeDEngine.cs:151-153`). The comment says this at `Window.FileDialog.cs:37-38`.
- **[Inferred]** On a system where the dialog does not open (for example Linux with no portal and no zenity), "Open file" does nothing visible. Steps 2 and 6 of the test sheet would fail with no text. Not run.
- **[Recommended]** Keep `SDL_GetError` when `files` is null, and show it with `ShowMessage`.

#### V132 · Low · `HomeImageTests` checks only "Open file", although its name names "New part" and the recent files · Confirmed by reading

- **[Observed]** The test reads one rectangle, "Open file", and checks one pixel in it (`3DEngine.ImageTests/HomeImageTests.cs:42`, `:53-59`). Its name says "Open_File_New_Part_And_The_Recent_Files" (`HomeImageTests.cs:19`).
- **[Observed]** `StlOpenTests.The_Home_Screen_Gives_Open_File_New_Part_And_A_Recent_File` checks the rectangles and the actions of all three with no GPU (`3DEngine.ImageTests/StlOpenTests.cs:48-66`). So the gap is the picture only. Codex lead 4 is true for the picture test, and the actions have a test.
- **[Recommended]** Check one pixel of "New part" and of a recent row too, or rename the test.

#### V133 · Low · Three comments say something false · Confirmed by reading

- **[Observed]** `BodyRowsTests.cs:42` says the rows are "320 pixels wide"; `RowsWidth` is 360 (`DesktopUi.Rows.cs:13`), and the test asserts 360 through the constant.
- **[Observed]** `DesktopUi.Rows.cs:19-21` says `FailedColour` is "the red of the theme for a fault". `Theme.cs` has no such colour; the value is a literal of this file only.
- **[Observed]** `DesktopDocument.cs:73` says "a choice there ends it for the program". A recent row is a choice there, and it does not end it (V126).
- **[Recommended]** Correct the three comments; the third one with the correction of V126.

#### Probes run of this section

All probes are in `C:\Users\Henri\AppData\Local\Temp\claude\5c362f61631fddd6\fce9d0dc-a16b-4db8-aa8b-1ad5b21f4416\scratchpad\review29\probe-desktop\ProbeReview29DesktopTests.cs`. I copied the file into `3DEngine.ImageTests` of the checkout, ran `dotnet test 3DEngine.ImageTests --filter FullyQualifiedName~ProbeReview29` (6 passed, local, Windows), removed the file, and `git -C C:\Users\Henri\source\repos\3DEngine-review29 status --short` gave no line.

- `Probe1`: `RunSessions` with the real `Start` on port 0; session 1 calls `RequestNew(import)` and then `POST /document/open {"name":"plate"}`. It gave `OpenAccepted = False`, HTTP 202, session 2 on `plate.3de` with the import of `bracket.stl` (V125).
- `Probe6`: `RunSessions` with a start that fails for the new part only. The previous file `first.3de` started again and held the import (V125).
- `Probe2`: `RunSessions`; session 1 opens `plate` by name. Session 2 on `plate.3de` had `Home = True` (V126).
- `Probe3`: `DesktopUi` with a document line that holds an error; the line had a rectangle with the panel and none with the home screen; `Message` was null (V127).
- `Probe4`: the width of `RowState` for five bodies against the column (V129).
- `Probe5`: two chips on the rows at 1280 by 720; both moved to (1053, 156, 1147, 200); a tap on the visible length chip gave `EditDistanceAction` (V128).

**No finding.**

- ADR-0084 item 1: the column is 360 px at the upper right, 16 px inside the safe area, only on a landscape display, only with a selected body, no active sketch and no home screen (`DesktopUi.Rows.cs:43-62`, `DesktopUi.cs:504`, `:533-546`).
- ADR-0084 item 2: each row is 44 px (`RowHeight = ValueSize`, `DesktopUi.Rows.cs:14`, `DesktopUi.Values.cs:12`), with a line under it (`DesktopUi.Rows.cs:90`, `:105`), the labels "Distance", "Length" and "Radius" (`3DEngine.App/EdgeValues.cs:52`, `:63-67`), and the value text of the chip (`DesktopUi.Rows.cs:103-104`).
- ADR-0084 item 3: a row tap needs no pad and no window under the pointer, and the press and the release on one row (`DesktopUi.Rows.cs:73-75`); the ring and the chips skip a release on a value row (`DesktopUi.Ring.cs:130`, `DesktopUi.Values.cs:56-57`). The rows are a part of the panels for a tap (`DesktopUi.cs:382-385`). The title row is not a value row, and I found no ring layout that reaches it: the top of the ring at its first segment is at least `reach − 0.78·(reach − 16)` from the top, more than 60 px for each inner radius of `Layout` (`DesktopUi.Ring.cs:91-94`).
- One tap reaches one tool between the ring, the chips and the rows, apart from V128.
- `StlOpening.Prepare` checks with the reader before the store, and `Import` applies one `ImportStl` (`3DEngine.App/StlOpening.cs:18-40`). `FileName` decodes a content URI (`StlOpening.cs:44-50`).
- `Window.OpenFile` refuses a second dialog, keeps the filter memory until the callback, and frees it there (`3DEngine.Vulkan/Window.FileDialog.cs:19-35`, `:50-63`). `LoadFile` frees the SDL buffer (`Window.FileDialog.cs:67-85`).
- `DesktopSession.Open` maps `NewDocument` to a document with no file, whatever the setting "file" (`3DEngine/DesktopSession.cs:51-53`).
- The size of a mesh body says "mm (assumed)" (`NativeThreeDEngine.cs:677-685`), and the first visible body frames the camera (`3DEngine.App/SceneRenderer.cs:63`, `:105-107`), as step 3 of the test sheet asks.
- "Frame" and "Home" are on the row of the anchors, and the status line follows them (`DesktopUi.Fixed.cs:39`, `:44-48`, `DesktopUi.Home.cs:94-105`).
- The message window draws over the home screen, and "Choose another file" closes it and opens the dialog again (`DesktopUi.cs:559-560`, `DesktopUi.Home.cs:65-91`), as steps 5 and 6 of the test sheet ask.

Not verified: no run on a GPU, no run of the program, no device. The timing of path 1 of V125 in the real window loop is inferred from the code, not measured.

### The gates and the records

#### T168 · Medium · 3DEngine.Android treats CA1416 as a warning, no step fails on it, and the question of TASK-0263 about it is in no register entry · Confirmed by injection

- **[Observed]** `dotnet msbuild 3DEngine.Android/3DEngine.Android.csproj -getProperty:TreatWarningsAsErrors` gives `false`
  (target `net10.0-android`). The project file does not set it, and the repository has no `Directory.Build.props`.
- **[Observed]** Injection: `3DEngine.Android/Rev29AndroidProbe.cs` sets `Console.ForegroundColor`, with no silence.
  `dotnet build 3DEngine.Android` gave `Rev29AndroidProbe.cs(4,35): warning CA1416`, 1 warning, 0 errors, exit 0. I
  removed the file.
- **[Observed]** No gate reads the host itself. The gate walks the projects that each host reaches, not the host
  (`DependencyDirectionGateTests.cs:604` to `617`, `HostsWithNoSurface` at line `1156`). The step lists 7 projects and not
  `3DEngine.Android` (`check-platforms.js:19` to `27`). The APK build has no `-warnaserror` (`.github/workflows/ci.yml:247`),
  and no file of `.github/` or `eng/` uses `-warnaserror`.
- **[Observed]** The entry v0.250 names the gap (`docs/CURRENT-STATE.md:7539` to `7540`) and says "the two questions are
  in the final report of order 2, not in the register" (line `7543`), because the register holds 15 of 15 open entries.
  TASK-0263 says the same in its Weakest line (`tasks/TASK-0263-...md:86` to `87`). `CLAUDE.md` makes "each open question in
  `docs/register.md` with a date and a time limit" a condition of the end of a session. `docs/register.md:40` to `42`
  says that a new entry needs the close of another one first. A chat report is not a record of the repository.
- **[Inferred]** The failure class of TASK-0229 (a call that Android does not have, found on the emulator at run time)
  stays open in the host. The host is the most changed file of the phone in this window: `3DEngine.Android/PhoneHost.cs`
  changed in TASK-0260 and TASK-0265. The second question of v0.250 (the affects rule before ADR-0082) has no due date
  either, so no gate brings either question back.
- **[Recommended]** Make the APK build fail on CA1416: for example `-warnaserror:CA1416` on the step of line `247`, or a
  line in `check-platforms.js` that builds the host with its probe and fails on each `CA1416` in the output. Prove it with
  the injection above. Put the two questions in the register through an exit of an existing entry.

#### T169 · Low · An attribute SuppressMessage over two lines, or under an alias, silences CA1416 at one call, and the gate and the step pass; the step says that a silence of any form fails it · Confirmed by injection

- **[Observed]** The source rule finds a line only when that line holds `CA1416` and `#pragma` or `SuppressMessage`
  (`DependencyDirectionGateTests.cs:662` to `664`). The probe of the step is a file of its own (`check-platforms.js:33` to
  `42`), so a silence on one member does not reach it.
- **[Observed]** Injection: `3DEngine.Vulkan/Rev29SplitSuppress.cs` has `[SuppressMessage("Interoperability",` on one line
  and `"CA1416:..."` on the next. `3DEngine.Core/Rev29AliasSuppress.cs` has `using Quiet =
  System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;` and `[Quiet("Interoperability", "CA1416")]`. Each one is
  above a call of `Console.ForegroundColor`. `dotnet build --no-incremental` of each project gave 0 warnings and 0
  errors. `Each_Project_That_The_Phone_Reaches_Supports_Android` passed (1 of 1). The step gave exit 0, with "the probe
  gives the error CA1416" for both projects. `eng/repo-gates` gave "470 runs of 163 checks, 0 failed". I removed both
  files.
- **[Observed]** `check-platforms.js:10` says "So a silence of any form, in a property, an analyzer file or a switch,
  fails the step." The comment of the step in `.github/workflows/ci.yml:248` to `249` still describes T162 only and does
  not name the probe build. The entry v0.250 (`docs/CURRENT-STATE.md:7537` to `7538`) and TASK-0263 (lines `85` to `86`)
  name the two-line form. They do not name the alias form, and no register entry holds either form.
- **[Observed]** No `.cs` file of the 7 projects of the phone holds the text `CA1416` today (`git grep` gives no line).
- **[Recommended]** Refuse each line that holds `CA1416` in a `.cs` file of a project of the phone. That rule has no
  false offence today and catches both injections. Correct the comment of the step and of `ci.yml`.

#### T170 · Low · The local rule of TreatWarningsAsErrors reads one element as text, so four project forms pass it, and only the step of the pipeline refuses them · Confirmed by injection

- **[Observed]** The rule passes when any element `TreatWarningsAsErrors` with no `Condition` attribute of its own holds
  `true` (`DependencyDirectionGateTests.cs:649` to `651`). It does not read the `Condition` of the parent
  `PropertyGroup`, a later element with `false`, or another property. The rule of `SupportedPlatform` reads each ancestor
  (line `638`).
- **[Observed]** Injection, with a script that kept a copy of each file: `3DEngine.App.csproj` with
  `CodeAnalysisTreatWarningsAsErrors` = `false`; `3DEngine.Core.csproj` with its `true` line in a `PropertyGroup` under
  `Condition="'$(Rev29)' == 'yes'"`; `3DEngine.Vulkan.csproj` with a later `TreatWarningsAsErrors` = `false`. In a second
  run, `Engine.Kernel.csproj` with `AnalysisModeInteroperability` = `None`. The gate passed each time. The step failed each
  time (exit 1): "the evaluated property WarningsNotAsErrors holds CA1416" for the App, "TreatWarningsAsErrors is not
  true" for the Core and the Vulkan project, and "gave no error CA1416 at the probe" for all four. The restore matched
  each copy (`sha256sum -c` gave `OK`).
- **[Observed]** `docs/templates.md:270` says that the gate refuses a project that "does not set
  `TreatWarningsAsErrors` to true". The comment at `DependencyDirectionGateTests.cs:633` to `636` says that the text rule
  "gives the same answer on the computer, before the pipeline"; that is true for the forms of T162 only.
- **[Inferred]** No such form reaches `main`, because the job `android-apk` runs the step at each push. But the two
  commands that `CLAUDE.md` requires before a commit pass, so the failure comes only after the push.
- **[Recommended]** Read the ancestors of `TreatWarningsAsErrors` as line `638` does, refuse a `false` value, and refuse
  `CodeAnalysisTreatWarningsAsErrors`, `AnalysisMode*` and `AnalysisLevel*` in the files of a project of the phone.

#### T171 · Low · The affects rule reads no shader and no PowerShell file, and passes a citation of an ADR that has no file · Confirmed by injection

- **[Observed]** The rule reads `.cs`, `.csproj`, `.js`, `.sh`, `.yml`, `.props` and `.targets` only
  (`Engine.Tests/Governance/AffectsCitationGateTests.cs:20`). Each of the 9 tracked shaders cites an ADR
  (`3DEngine.Vulkan/Shaders/*.frag` and `*.vert`, line 3 or 4), and `eng/manifold-native/build.ps1:9` cites ADR-0014.
- **[Observed]** A citation of a number that has no ADR file passes (`AffectsCitationGateTests.cs:65`, `continue` when
  `TryGetValue` fails).
- **[Observed]** Injection: `// ADR-0084 item 1` at the end of `3DEngine.Vulkan/Shaders/ui.frag`;
  `Engine.Kernel/Rev29CiteAbsent.cs` with `// ADR-0099 item 1`; `3DEngine.App/Rev29CiteForms.cs` with `ADR 0084` and
  `ADR-84`; and the control `3DEngine.Core/Rev29CiteControl.cs` with `// ADR-0084 item 1`. The gate failed with one
  offence only, the control. The shader matched its hash after the restore.
- **[Observed]** `docs/templates.md:266` says that the gate reads "each code file outside `docs/`, `tasks/` and the test
  projects".
- **[Observed]** No tracked code file uses the forms `ADR 0084` or `ADR-84` today, so I do not count them as a likely form.
- **[Inferred]** The rule reads citations, not commits, so a file that changes for an ADR with no citation is outside it
  by design. In this window two files changed for ADR-0083 with no citation and are not in its affects field:
  `Engine.Cli/Usage.cs` (TASK-0263, E171) and `3DEngine/DesktopEngineHost.cs` (TASK-0259). Both change printed text only.
  The rule does catch the nine files of T167: each one cited ADR-0083 or ADR-0082 at `e903bef`.
- **[Recommended]** Add `.frag`, `.vert` and `.ps1` (or read each tracked text file outside `docs/` and `tasks/`). Make a
  citation of an absent ADR number an offence. Correct the row of `docs/templates.md`.

#### T172 · Low · The entry v0.244 and the summary of the review of 2026-11-07 say "fifteen" new low findings, and the list holds eleven · Confirmed by reading

- **[Observed]** `docs/CURRENT-STATE.md:7361` to `7362`: "Fifteen are low: E168 to E171, V119 to V123, T165 and T167".
  `docs/reviews/2026-11-07-codebase-review.md:31` to `32` says the same. The list is 4 + 5 + 2 = 11.
- **[Observed]** The Measures row of the same review (line `59`) gives 30 low findings, and that total agrees with 11 new
  ones. So only the word "fifteen" is false.
- **[Recommended]** Correct it in the section "A correction" of the next ledger entry. Do not change the review
  (`docs/templates.md`, section 7: "Do not change an earlier review").

#### T173 · Low · The list "Shipped" of the roadmap is no longer in the order of the ledger · Confirmed by reading

- **[Observed]** Up to v0.243 the list follows the ledger versions (`docs/roadmap.md:213` to `219`). Now R16d (v0.252) is at
  line `218`, after R16c (v0.241) and before P9a (v0.242). P9c to P9e (v0.247 to v0.249) are at lines `221` to `223`,
  before the review (v0.244) at line `224`. The last line is P0.107 (v0.251, line `229`), not R16d.
- **[Observed]** No rule and no gate gives the order of the list. TASK-0267 added the line of R16d (`3815dc6`).
- **[Recommended]** Put each line at the end, in the order of the ledger, or state the order in the header of the
  roadmap.

#### Probes run of this section

- `cites.js` (Node.js, a mirror of `AffectsCitationGateTests` at a revision with `git show`): 27 offences at `31c3204`,
  17 at `e903bef`, 0 at HEAD; 379 citations of an earlier ADR outside its affects field at `31c3204` and at HEAD; 525
  code files at `31c3204`, 529 at HEAD.
- `check-platforms.js` on the clean tree: 7 projects, each probe gives `error CA1416`, exit 0, 9 s.
- Citation forms (shader, absent ADR, two number forms, control): the gate failed for the control only.
- Two `SuppressMessage` forms (two lines, alias): builds 0 warnings; gate passed; step exit 0; `eng/repo-gates` 470 runs of
  163 checks, 0 failed.
- `twae.js` (three project forms) and a fourth run with `AnalysisModeInteroperability`: gate passed; step exit 1 each.
- An unrelated compile error (`Engine.Kernel/Rev29Broken.cs`, an unknown name): the step still saw `error CA1416` at the
  probe and gave exit 0 for that project. The step does not read the exit code of the probe build
  (`check-platforms.js:71` to `76`). This is not a hole: the job `gate` and the APK build fail on that error first.
- The two T165 forms of the review: the gate failed (both offences), the step failed (Engine.Kernel).
- Unsilenced probe in `3DEngine.Android`: `dotnet build 3DEngine.Android` gave 1 warning CA1416 and exit 0.
- `dotnet run --project eng/write-set-check -- --range 86517c8`: each of the 19 commits passes, exit 0.
- `dotnet build 3DEngine.sln --no-incremental` (two times): 17 projects, 0 warnings, 0 errors.

**No finding.** I checked these items and found them correct.
- The step of the platforms runs after the APK build in the same job (`.github/workflows/ci.yml:246` to `252`). The APK
  build builds each referenced project in `Debug`, which the probe build with `BuildProjectReferences=false` needs
  (`check-platforms.js:13`, `71`). When the probe build fails for a cause that stops the compiler (for example a missing
  reference), no `error CA1416` line comes, and the step fails with a message that names a silence; it fails closed. The
  step deletes the probe in `finally` (line `74`). A probe that stays after a killed local run makes the next build of that
  project fail with `error CA1416`, so it cannot stay unseen.
- The number 379 in `AffectsCitationGateTests.cs:11` and in `docs/CURRENT-STATE.md:7541` is true: 379 pairs of a code
  file and an ADR before 0082 whose affects field does not match the file, at `31c3204` and at HEAD. "525 code files" is
  true at `31c3204` (the date of the comment); HEAD has 529. The comment says "such citations"; it means the citations
  outside the affects field, not all 846 of them.
- The affects rule reads the block list form of the front matter (`eng/write-set-check/TaskFiles.cs:36` to `47`), and the
  `id` field is the bare number (`docs/adr/0084-...md:2`), so the dictionary keys agree with the citations. Each test
  project name ends in `Tests` (`3DEngine.sln`), so `IsTest` (`AffectsCitationGateTests.cs:76`) skips exactly the four
  test projects.
- The ADRs of the window. ADR-0084 amends ADR-0081 and ADR-0082 (`docs/adr/0084-...md:9`), and both give
  `amended-by: ['0084']` with status `Amended`; `docs/adr/README.md` agrees. ADR-0015, ADR-0072, ADR-0079 and ADR-0080 have
  a history line for each change and no change of status. ADR-0083 history lines name TASK-0258, 0259, 0260, 0262 and 0263.
  Each code file of the product commits of this window is in the affects field of the ADR that its ledger entry names,
  except the two printed-text files of T171. The decision of ADR-0084 items 1 to 4 agrees with
  `3DEngine.App/Ui/DesktopUi.Rows.cs` (width 360 at line `13`, inset 16 at lines `49` and `51`, row height at line `14`,
  the tap at lines `72` to `75`, the chip under the rows at lines `127` to `132`). I did not examine the desktop behaviour
  further; that is the area of another reviewer.
- The tasks TASK-0259 to TASK-0267: each status is `Done`, each Outcome names its version, and each changed file is in the
  write set at the close (my comparison and the judge above). TASK-0259, TASK-0260 and TASK-0266 each keep one open box
  for the run of the owner; that is the practice of R14e and of earlier tasks, and the roadmap rows say "the row waits for
  the owner".
- The roadmap rows P9c, P9d, P9e and R16d name their tasks (`docs/roadmap.md:363` to `365`, `412`), and each has a shipped
  line with its version and task.
- The counts of the entries v0.244 to v0.252 (`docs/CURRENT-STATE.md:7373` to `7612`). "New tests" adds up from 2014 to
  2042. Each "The test list holds" agrees with `eng/test-floor.txt` at the close commit of its entry (the Linux count of
  `3DEngine.ImageTests`: 269, 270, 270, 276, 278, 278, 278, 284, 292; `Engine.Tests` 1405 to 1410 at v0.250). The count
  "N product milestones and M entries" is 0/1, 0/2, 0/3, 1/4, 2/5, 3/6, 3/7, 3/8, 4/9: P9c, P9d, P9e and R16d count, and the
  P0 phases and the review do not (`docs/templates.md:332` to `338`). The review is due now: four product milestones
  follow v0.243, and the next one would be the fifth.
- I did not verify the run numbers of the entries (42324736514 to 42360390053): the GitHub API is out of the scope of
  this review.

**The Codex leads.**
1. Confirmed (T172).
2. Refuted as a current finding. At `1ab66ce` the write set of TASK-0263 did not name 18 of the files that it later
   changed. Commit `45f416d` widened the write set and changed `Ready` to `Active` in the same commit, so the judge, which
   reads the task at the commit, passed it. Today the write set names each of the 25 files of `45f416d` and `60a9ea1`, and
   `write-set-check --range 86517c8` passes each commit. No rule forbids a task to widen its own write set.
3. Refuted as a current finding. The ledger order is v0.245 P0.105, v0.246 P0.108, v0.247 P9c, so both high findings came
   before the next product phase, as `docs/templates.md`, section 7, requires. TASK-0259 depends on TASK-0265, which held
   that order. The rows of track 0 list P0.106 and P0.107 above P0.108 (`docs/roadmap.md:340` to `342`), and TASK-0263
   depends on TASK-0262 only, so "the first Ready task in roadmap order" would have taken P0.106 first; the session
   followed the review instead. Each of these tasks is `Done`, so no later task acts on that order today.

## 4 The next planned task

The next product phase is the redesign slice "the sketch under its body" (R16), after which K5 comes (the order of the
owner of 2026-11-07). Five findings are high, so two corrections come before it, in this order: P0.109 (TASK-0269) holds
E172, E173 and E174, and P0.110 (TASK-0270) holds V125 and V126. By `docs/templates.md`, section 7, the medium and low
findings go after it: P0.111 (TASK-0271) holds V127 to V133, and P0.112 (TASK-0272) holds E175 to E179 and T168 to
T173. Each task names in its write set the files that its findings name, and the chain of `depends-on` holds the order,
so the rule "the first Ready task in roadmap order" gives the high findings first (the leads 1 and 2 of Codex of v0.244).

## 5 Research for that task

**[Observed]** E172: `WaitForForeground` returns at a foreground event with no check of the turn
(`3DEngine.Android/PhoneHost.cs:207` to `223`), and `SessionTurns.Phone.IsLast(turn)` is the check that ADR-0079 item 4
names. E173: the reader of v0.243 ends the name at the first word `facet` or `endsolid`
(`git show e903bef:Engine.Core/Import/StlReader.cs`); the reader of HEAD takes the words after the first line end
(`Engine.Core/Import/StlReader.cs:80` to `85`). E174: `docs/demo-stl-test-sheet.md:47` to `48` and `:65`, and ADR-0073,
Consequences. V125: `DesktopDocument.RequestNew` and `Reopen` (`3DEngine/DesktopDocument.cs:65` to `101`). V126:
`NativeThreeDEngine.OpenDocument` does not clear `Home` (`3DEngine.App/NativeThreeDEngine.cs:373` to `374`).

**[Inferred]** E173: the rule "the name is the rest of the line of `solid` when the next word is `facet` or `endsolid`,
else the rule of v0.243" reads each form that v0.243 read with the result of v0.243, and it keeps the three forms of E170.
A file that v0.243 refused and that HEAD reads changes from a refusal to a body, which no recorded document holds,
because a rejected command writes no entry (ADR-0015, `docs/adr/0015-command-log-persistence.md:63` to `64`).

## 6 Recommended approach

**[Recommended]** In this order:

1. P0.109 (TASK-0269): E172, a check of the turn at each return of the wait and at each pass of the loop; E173, the rule
   above, with tests that compare each form with the result of v0.243, and an item of ADR-0083 that a change of the reader
   keeps the result of each file that an earlier reader accepted, or takes a new schema version of `ImportStl`; E174, the
   text of the test sheet. 1 to 2 agent hours.
2. P0.110 (TASK-0270): V125, the request and its import as one record that the target session takes; V126, the home
   screen ends at an accepted open by name. 1 to 2 agent hours.
3. The redesign slice "the sketch under its body".
4. P0.111 (TASK-0271) and P0.112 (TASK-0272): the medium and low findings, each with a test or an injection before the
   change.

## Questions for the owner

Two findings ask for a decision. The standing order of 2026-11-05 gives the recommended answer; the owner can change it.

1. **E173: how a change of the STL reader is versioned.** ADR-0083 does not say. **A (recommended):** a change of the
   reader keeps the result of each file that an earlier reader accepted; a change that cannot keep it takes a new schema
   version of `ImportStl`. **B:** each change of the reader takes a new schema version. A keeps each recorded document
   and needs no new version for a reader that only accepts more files.
2. **E174: the view after the background on the phone.** **A (recommended):** the test sheet says what the code does: the
   part shows whole and framed again, and the turn, the zoom, the selection and the line "Size" go (ADR-0073). **B:** the
   phone keeps the camera of the session across the background, a change of ADR-0073.

Two questions of TASK-0263 have no register entry, because the register holds 15 of 15 open entries (T168): the affects
rule for the ADRs before 0082, and `TreatWarningsAsErrors` for `3DEngine.Android`. P0.112 puts them in the register.
