---
date: 2026-11-09
commit: 6f75607
ledger: v0.310
previous: 2026-11-09-codebase-review.md
---

# Codebase review — 2026-11-09 (b)

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

The review examines `main` at `6f75607`, the merge of v0.310. Six ledger entries follow the review of v0.304, with four
product milestones (V0b, R16q, R16r and R16s), so the next product milestone would make the review late. One agent examined
the code change from `3884adf` (v0.304) to `6f75607` and proved its main findings with probes in a scratch copy at `6f75607`.
The session ran each probe again and read each finding in the code. The review uses no lead of Codex.

## Summary

- **Seven open findings are fixed.** P0.136 (v0.307) fixed V152 to V157 and E197; the agent checked each fix in the code.
  **[Observed]**
- **One new finding is high.** V159: in the default window of 1280 by 720, the window of the live sketches covers Accept of
  the agent arc, and a tap on Accept does nothing. **[Observed]** by a probe.
- **Four new findings are medium.** **[Observed]** or **[Inferred]**, as each says.
  - V160: one tap on Accept also uses the tool of the ring under it.
  - E198: a command identifier that repeats in a proposal counts as applied, but nothing applied.
  - V161: the history arc waits for the session after each commit, also with no selection.
  - V162: on a portrait display the sheet of a proposal covers each tool of the ring.
- **Three new findings are low:** E199, V163 and T180.
- **Open findings: 0 critical, 5 high, 11 medium and 23 low.** **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 17 | `.csproj` lines in `3DEngine.sln`. The last review gave 17; `3DEngine.Android` is outside the solution. |
| Production source lines | 36,473 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests`, `3DEngine.ImageTests` and `3DEngine.VisionTests`, 315 files. The last review gave 35,122 in 309 files. |
| Test source lines | 43,295 | `Engine.Tests` (28,062 in 128 files), `3DEngine.Tests` (3,815 in 23 files), `3DEngine.ImageTests` (11,352 in 63 files) and `3DEngine.VisionTests` (66 in 1 file). The last review gave 41,562. |
| Tests | 2,310 | `dotnet test 3DEngine.sln` at `6f75607`: 1,528, 356, 423 and 3 passed, 0 failed, 0 skipped. The last review gave 2,244. |
| Gate classes | 20 | Tracked files whose name ends in `GateTests.cs`. `eng/repo-gates` gives 488 runs of 168 checks. The last review gave 20, and 488 runs of 168 checks. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 9 | Section "Open" of `docs/register.md`; the limit is 15. The last review gave 10. |
| ADRs | 95 | `docs/adr/0001` to `0061`, `0070` to `0099` and `0102` to `0105`, with 0003 and 0011 in `docs/adr/archive/`. In force: 93 (43 accepted, 50 amended). None is proposed: ADR-0092 is accepted. ADR-0100 and ADR-0101 are planned by R17a and R17b. The last review gave 92. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 5 | The same method: V3, T4, T31, V141, V159. The last review gave 4. |
| Findings: medium | 11 | The same method: E6, E13, E19, E21, T32, T37, V158, V160, E198, V161, V162. The last review gave 9; V152 and V153 are fixed. |
| Findings: low | 23 | The same method: E17, E24, E34, E39, E57, E84, E160, V7, V15, V29, V84, V88, T7, T23, T25, T143, C1, P1, V143, V144, E199, V163, T180. The last review gave 25; V154, V155, V156, V157 and E197 are fixed. |
| Lines in the rule files | 3,234 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The last review gave 3,208. |
| Sentences with "must" | 61 | `grep -o -w -i must` on the same ten files. The last review gave 61. |
| Sentences with "do not" | 56 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 56. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found. E13, T32, E84, E160, V84 and V141 are fixed in part and count as open. A finding that the owner accepted counts as
closed. V3 counts as open: its correction is in the code, and its check has not run.

## Findings of the previous review

The lines up to V151 are the lines of the review of 2026-11-09. Only P0.136 (v0.307) changed open findings from v0.305 to
v0.310, so this review did not examine the others again. The lines of V152 to V158 and E197 follow them.

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Fixed. TASK-0321, v0.304. A whole JSON number stays a long, so an integer field gets the exact value above 2^53.
- **E6** — Open. No change: `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` has no commit since `3379675`. Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. No change.
- **E8** — Fixed. TASK-0044, v0.37. No change.
- **E9** — Fixed, and it holds. No change.
- **E10** — Fixed. TASK-0035, v0.43. No change.
- **E11** — Fixed. TASK-0115, v0.115. No change.
- **E12** — Fixed. TASK-0321, v0.304. A number field takes a long or an int.
- **E13** — Fixed in part. No change: `Engine.Api.Http/Endpoints/QueriesEndpoint.cs`, `Engine.Cli/Cli.cs` have no commit since `3379675`.
- **E14** — Fixed. TASK-0115, v0.115. No change.
- **E15** — Fixed. TASK-0044, v0.37. No change.
- **E16** — Fixed. TASK-0115, v0.115, and TASK-0321, v0.304: a subscribe frame with an unknown member closes with 1007.
- **E17** — Open, and smaller. TASK-0321, v0.304: HTTP 415 gives the error envelope, a negative cursor gets a reset, the
  command line writes UTF-8, and a subscribe frame refuses an unknown member. Open: parts 1 and 2 (new codes), 3, 5, 6 and 9.
  Register entry R-0028.
- **E18** — Fixed for the sinks of `Engine.*` (TASK-0140, v0.141) and of the desktop host (T118, TASK-0150, v0.154). TASK-0140, v0.141. No change.
- **E19** — Open, and smaller. `Engine.Core/DocumentSession.cs` has no change since `3379675`; `Engine.Core/CommandBus.cs` changed only for E163 (TASK-0242). Register entry R-0033.
- **E20** — Fixed, and it holds. No change.
- **E21** — Open. No change: `Engine.Core/Commands/SubtractCommandHandler.cs` has no commit since `3379675`. Register entry R-0033.
- **E22** — Fixed. TASK-0321, v0.304. Only a cancellation of the caller is Cancelled, and a cancelled result is not cached.
- **E23** — Fixed. TASK-0051, v0.48. No change.
- **E24** — Open in one part of nine, and one part accepted. TASK-0321, v0.304, fixed parts 3, 4, 5, 6, 7 and 9; parts 1 and the
  first comment of part 9 were fixed before. Open: part 2 (one bus for each Document). TASK-0044 accepted part 8. Register
  entry R-0033.
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
- **V2** — Fixed. TASK-0319, v0.301. The swapchain takes the special extent only for 0xFFFFFFFF, and then the size in pixels.
- **V3** — Open, and corrected in the code. TASK-0319, v0.301: the portability enumeration and the portability subset when the
  loader and the device give them. Its check, a job on macOS through MoltenVK, has not run. Register entry R-0022.
- **V4** — Fixed. The exit code of a failed start and the results of Vulkan were fixed before; TASK-0319, v0.301, fixed the log line of the instance, the old layers and the two types with no caller.
- **V5** — Fixed. TASK-0076, v0.74. No change.
- **V6** — Fixed. TASK-0076, v0.74. No change.
- **V7** — Open in one part of six. TASK-0319, v0.301, fixed parts 1 to 5. Open: part 6 (`IList` in `3DEngine.Core`). Register
  entry R-0030.
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
- **T2** — Fixed. TASK-0321, v0.304. `CommandBusBackendFailureTests` holds a backend that throws through the command bus.
- **T3** — Fixed. TASK-0047, v0.42. No change.
- **T4** — Open in one part of three. The determinism part was fixed before; TASK-0320, v0.303, fixed the marker part. Open: the
  dispatch gate does not read the desktop host. Register entry R-0031.
- **T5** — Fixed. TASK-0320, v0.303. The gates strip the quotes and the brackets of an `affects` list and refuse what stays.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Open in two parts of five. TASK-0320, v0.303, fixed the amend columns of the index and `previous: none`. Open, for the
  owner to accept: the clock of the register gate, and the case of a path on Windows. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. No change.
- **T10** — Fixed. TASK-0320, v0.303. The register gate reads the exact heading of an entry and refuses a repeated field.
- **T11** — Fixed. TASK-0053, v0.51. No change.
- **T12** — Fixed. TASK-0059, v0.55. No change.
- **T13** — Fixed. TASK-0320, v0.303, fixed the marker part and the readers of the two tests of concurrency; the other parts were fixed before.
- **T14** — Fixed. TASK-0053, v0.51. No change.
- **T15** — Fixed. TASK-0050, v0.47. No change.
- **T16** — Fixed. TASK-0066, v0.67. No change.
- **T17** — Fixed for its form. TASK-0320, v0.303. The folder feed holds only the packages that the gate reads.
- **T18** — Fixed. TASK-0049, v0.46. No change.
- **T19** — Fixed. TASK-0049, v0.46. No change.
- **T20** — Fixed. TASK-0049, v0.46. No change.
- **T21** — Fixed. TASK-0049, v0.46. No change.
- **T22** — Fixed. TASK-0049, v0.46. No change.
- **T23** — Open in one part of two. TASK-0320, v0.303, finds the path of a Windows runner. Open, for the owner to accept: the
  provenance of the native binaries. Register entry R-0031.
- **T24** — Fixed. No change.
- **T25** — Open, for the owner to accept: the local mode of the write-set check reads the index only. Register entry R-0031.
- **T26** — Fixed. TASK-0057, v0.54. No change.
- **T27** — Fixed. TASK-0058, v0.56. No change.
- **T28** — Fixed. No change.
- **T29** — Fixed. TASK-0057, v0.54. No change.
- **T30** — Fixed. TASK-0057, v0.54. No change.
- **T31** — Open in part, for the owner to accept: the three gates that run engine code stay in the test process. Register entry R-0031.
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
  form with a cut that fails too is E168. Since TASK-0291 (v0.275, ADR-0090, the answer of the owner of 2026-11-08) the HTTP answer of a save lists the warning too.
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
  path (`docs/CURRENT-STATE.md`, entry v0.246, "What stays"). The second part is fixed since TASK-0286 (v0.271): the start of the engine has the guard of a frame; TASK-0287 (v0.272) makes the support query of the surface check its result, so the guard takes it. The reviews of 2026-11-07 (b) and (c) counted V124 as fixed while this part was open.
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
- **E180** — Fixed. TASK-0280, v0.265. `StlReader.Read` gives the result of the first of the rules of v0.243, of HEAD and of
  v0.250 that accepts a file; the forms with a zero byte are in `Engine.Tests/Import/StlImportTests.cs`.
- **E181** — Fixed. TASK-0280, v0.265. The names that only v0.250 read get their result again (the same test file).
- **E182** — Closed. The owner confirmed answer A on 2026-11-08 (the form after v0.297); TASK-0280 holds its code.
- **E183** — Fixed. TASK-0281, v0.266. The limit of a chosen STL is the limit of the store, and the read stops at it.
- **E184** — Fixed. TASK-0281, v0.266. A lost surface waits for the foreground (`PhoneLifecycle.AfterLostSurface`).
- **E185** — Fixed. TASK-0282, v0.267. The verb `asset` refuses a blank folder.
- **V134** — Fixed for its forms. TASK-0280 (v0.265) and TASK-0284 (v0.269, two leads of Codex: the windows after the ring,
  and a window of the sketch at the left).
- **V135** — Fixed. TASK-0281, v0.266. A release over a window is the tap of the window only.
- **V136** — Fixed. TASK-0281, v0.266. The portrait ring stays above the anchors.
- **V137** — Fixed. TASK-0282, v0.267. A test holds the close of the second ring by a new selection.
- **V138** — Fixed. TASK-0282, v0.267. The three comments say what the code does.
- **V139** — Fixed. TASK-0282, v0.267. `FileDialogState` gives each dialog its own number and filter. Its test raced with
  `WindowLifecycleTests`; TASK-0292 puts the two classes in one collection.
- **T174** — Fixed for its forms. TASK-0281 (v0.266) and TASK-0285 (v0.270, a lead of Codex: the switches of the analyzers,
  and the probe build in the host that TASK-0281 named and did not add).
- **E186** — Fixed. TASK-0295, v0.279. `CreateSolid` refuses a mesh whose volume is not positive; tests of the volume of
  the cone and of the refusal of reversed triangles (`Engine.Tests/Commands/CreateConeCommandTests.cs`).
- **E187** — Fixed. TASK-0296, v0.280. The comment of `KernelSolid.Closed` is above `Closed`.
- **E188** — Fixed. TASK-0296, v0.280, and TASK-0289, v0.282: the messages of `DatumResolver` and the header of
  `KernelProjection` name each creator of a body.
- **E189** — Fixed. TASK-0296, v0.280. The reader names its second rule "the rule of v0.257 and later".
- **E190** — Fixed. TASK-0295, v0.279. The client gives `E-ASSET-TOO-LARGE` before the read and at a stopped read.
- **E191** — Fixed. TASK-0296, v0.280. `CreatePerFrame` frees the objects of the earlier images before it throws again.
- **E192** — Fixed. TASK-0296, v0.280. `Dispose` ignores the result of the wait of a lost device and destroys each object.
- **V140** — Fixed. TASK-0296, v0.280. While the second ring is open, the window of the sketch holds the tools and Finish.
- **V141** — Fixed in part. TASK-0294, v0.277. The main function of a stale session waits and does not return (ADR-0079
  item 5), from the bytecode of SDL; the smoke test gives the lines of a tombstone. No run repeated the relaunch, so no run
  shows the fix, and the finding counts as open.
- **T175** — Fixed. TASK-0296, v0.280. The gate of the phone host refuses a mode or a level of the analysis and needs its
  `WarningsAsErrors`.
- **T176** — Fixed. TASK-0296, v0.280. Each place of a tool is on the display, and the window of the sketch is at most as
  wide as the display less its margins.
- **E193** — Fixed. TASK-0302, v0.287. A command that makes a sketch item refuses an identifier that an item of the sketch
  already holds.
- **E194** — Fixed. TASK-0302, v0.287. The backend refuses a volume below zero only, so a very small solid of an earlier
  document opens again.
- **E195** — Fixed. TASK-0303, v0.288. The filter gives NaN for a value whose double is not normal, so the exact sign
  decides; ADR-0089 item 6 says so.
- **E196** — Fixed. TASK-0303, v0.288. A comment of `GetFacesQueryHandler` says that its cache belongs to the registry of
  one document session, and why.
- **T177** — Fixed. TASK-0302, v0.287. A gate refuses the markers of a conflict of git in a tracked text file.
- **V142** — Fixed. TASK-0302, v0.287. The main panel is at most as wide as the display less its margins.
- **T178** — Fixed. TASK-0303, v0.288. The gate of the host reads each `WarningsAsErrors` and the last one of the host.
- **V143** — Open. TASK-0303, v0.288, names the race in ADR-0079 item 5 and in the comment of the activity; it is not fixed.
- **V144** — Open in part. TASK-0303, v0.288. ADR-0079 item 5 calls the cause of V141 inferred and names a second possible
  cause; no run shows the cause.
- **V145** — Fixed. TASK-0303, v0.288. `SceneRenderer.Dispose` goes on when the wait of a lost device fails.
- **T179** — Fixed. TASK-0303, v0.288. The comment of the test of V141 says that it holds the text of the rule.
- **V146** — Fixed. TASK-0310, v0.294. The status line leaves Frame and Home free on each display.
- **V147** — Fixed. TASK-0311, v0.296. The dimension lines take the up axis of the camera.
- **V148** — Fixed. TASK-0308, v0.295. A landscape ring of a sketch draws the icons of Select, Circle and Finish.
- **V149** — Fixed. TASK-0311, v0.296. The top bar cuts the name to the room left of the rows; since TASK-0313 left of the
  command line.
- **V150** — Fixed. TASK-0311, v0.296. The comment of the panel says that the person does not move it.
- **V151** — Fixed. TASK-0311, v0.296. The comments of the camera, the yaw and the pitch name the up axis of each frame.
- **V152** — Fixed. TASK-0324, v0.307. The window of the live sketches is at the right, under the command line, in each frame.
  V159 is a side effect of that place.
- **V153** — Fixed. TASK-0324, v0.307. Each command of a cut has the text of its row and its own identifier.
- **V154** — Fixed. TASK-0324, v0.307. The window of the images starts under the top bar.
- **V155** — Fixed. TASK-0324, v0.307. Only the save that the question opened runs or drops the waiting change.
- **V156** — Fixed. TASK-0324, v0.307. The top bar takes the width of the command line on a landscape display only.
- **V157** — Fixed. TASK-0324, v0.307. The request of the focus stops where the field does not show.
- **E197** — Fixed. TASK-0324, v0.307. A step of the desktop surface puts a file that a request gave the session into the
  recent projects.
- **V158** — Open. No change: the owner has not answered Q5 of the review of 2026-11-09.

## 1 Current architecture

No boundary changed from v0.305 to v0.310. **[Observed]** `Engine.Api.Http` gained the queue of the proposals of an agent
(ADR-0092): four routes and three codes, outside the log, with no change of `Engine.Contracts`. `Engine.Core` gained only
the constants of the three codes. The desktop gained the history arc (ADR-0104), the agent arc (ADR-0092) and the values of
an active sketch (ADR-0105) in `3DEngine.App`, and `3DEngine/HostedSession.cs` gives the queue to the window through
`IProposalDesk`. The vision spike moved to OpenCvSharp5 (ADR-0103), and the library stays out of each distributed program.

## 2 Patterns in use

- **Each new layer of the mix takes a place that a control already has.** V159, V160 and V162 are of the kind of V152: the
  arc of a proposal goes on a ring at the centre, where the window of the live sketches, the ring of a sketch or the thumb
  ring of a portrait display already are. The tests of each slice check its own control at one display size, 1440 by 900,
  and not the default window of 1280 by 720. **[Observed]**
- **A query of the session in the frame.** ADR-0104 keeps the frame away from the lock of the session, and the code waits
  on it after each commit (V161). The other reads of the frame use the events. **[Observed]**
- **Agents in worktrees, the session as the reviewer.** Each slice was made by an agent with no commit; the session reviewed
  each diff. The review of R16r found that the real window did not show a proposal, because `HostedSession` did not give
  the queue. A test now posts over HTTP and accepts through the window side. **[Observed]**

## 3 Review of the code

#### V159 · High · In the default window, the window of the live sketches covers Accept of the agent arc · Confirmed by a probe

- **Evidence.** With no selected body, the arc of a proposal uses a ring at the centre of the display
  (`DesktopUi.Proposal.cs:147-149`). Since TASK-0324 the window of the live sketches is at the right edge, under the command
  line, in each frame (`DesktopUi.Sketch.cs:35-36`). The default window of the desktop is 1280 by 720
  (`3DEngine/DesktopEngineHost.cs:27`). A probe with one live sketch, no selection and one proposal gave Accept at
  `<773,112,821,160>` and the window at `<784,68,1264,640>`; a tap on the middle of Accept gave no action. At 1440 by 900 and
  at 1920 by 1080 the same tap gave `AcceptProposalAction`. **[Observed]**
- **Impact.** In the window as it opens, a person cannot accept a proposal while the document has a live sketch and no body
  is selected. Reject is free: the same probe gave `RejectProposalAction` for a tap on Reject at 1280 by 720. So the person
  can only refuse. **[Observed]**
- **Correction.** Keep Accept and Reject out of each panel, or close the window of the live sketches while a proposal waits.
  Test the arc at 1280 by 720. **[Recommended]**

#### V160 · Medium · One tap on Accept also uses the tool of the ring under it · Confirmed by a probe

- **Evidence.** The tap of the ring (`DesktopUi.Ring.cs:238-239`) does not exclude the targets of a proposal, and the tap of
  the proposal (`DesktopUi.Proposal.cs:165-166`) does not exclude the ring. Two probes:
  - At 1440 by 900 with an active sketch, the arc uses the ring at the centre, on the ring of the sketch. A tap on Accept
    gave `SketchToolAction { Kind = Line }` and `AcceptProposalAction`.
  - At 1440 by 720 with a selected extrude and a face to sketch on, a tap 16 pixels right of the middle of Accept gave
    `AcceptProposalAction`, and the pad of Move opened. **[Observed]**
- **Impact.** One tap accepts the commands of the agent and starts a tool of the person. **[Observed]**
- **Correction.** Let the targets of a proposal take the tap before the ring, as the chips and the rows do. **[Recommended]**

#### E198 · Medium · A command identifier that repeats in a proposal counts as applied, but nothing applied · Confirmed by a probe

- **Evidence.** The post keeps the `commandId` of the agent with no check (`ProposalsEndpoint.cs:148`). The accept applies each
  command through the session (`Proposals.cs:106`), and the bus gives the cached result for an identifier that it knows
  (`Engine.Core/CommandBus.cs:127-128`). A probe posted two envelopes of `CreateBox` with one identifier: the proposal was
  `accepted` with 2 applied results, and the document had version 1 and 1 body. A later proposal with that identifier was
  `accepted` with 1 applied result and no change. **[Observed]**
- **Impact.** The status line says "2 commands applied, 2 steps of undo" for one step, so an undo of the person removes a
  command that the person did not expect. **[Inferred]**
- **Correction.** Refuse at the post an identifier that repeats in the proposal, with `E-API-BAD-REQUEST` and its index, and
  count a cached result as not applied by this proposal. **[Recommended]**

#### V161 · Medium · The history arc waits for the session after each commit, also with no selection · Confirmed by reading

- **Evidence.** ADR-0104 item 1 says that the arc counts the events through a subscription, "so the frame takes no lock of
  the session". The frame computes the steps when the version or the selection changes, and the computation runs
  `GetFeaturesQuery` through `Session.Query(...).GetAwaiter().GetResult()` (`NativeThreeDEngine.cs:833-835`). A query waits on
  the serial lock of the session (`Engine.Core/DocumentSession.cs:159-163`). `HistorySteps.Of` gives an empty list for no
  selection only after its argument, the query, ran. **[Observed]**
- **Impact.** During an accept of several commands, or a long command of an agent, the frame after each commit waits for the
  next command, and the window stops, also with no selection. **[Inferred]**; the probe needs the window.
- **Correction.** Do not run the query with no selection, and read the features off the thread of the frame.
  **[Recommended]**

#### V162 · Medium · On a portrait display the sheet of a proposal covers each tool of the ring · Confirmed by a probe

- **Evidence.** On a portrait display the sheet is as wide as the display and ends 8 pixels above the anchors
  (`DesktopUi.Proposal.cs:251-256`). A probe at 390 by 844 gave the sheet `<16,530,374,772>`, and the middle of each tool of
  the ring in it: Move at (194,733), Set distance at (229,657) and Hide at (295,605). ADR-0092 item 7 says that the tools of
  the ring stay, but dim, while a proposal waits. **[Observed]**
- **Impact.** On a portrait display no tool of the ring takes a tap while a proposal waits. **[Observed]**
- **Correction.** Put the sheet above the ring, or correct item 7 for a portrait display. **[Recommended]**

#### E199 · Low · The withdraw of a proposal that applies gives a wrong reason · Confirmed by reading

- **Evidence.** `DELETE /proposals/{id}` refuses each proposal that is not open with `E-API-PROPOSAL-CLOSED` and the text "the
  person accepted or rejected it, or it was withdrawn or failed" (`ProposalsEndpoint.cs:108-109`). A proposal in the state
  `applying` is none of these. The test `A_Proposal_That_Applies_Refuses_A_Second_Accept` checks only the code. **[Observed]**
- **Correction.** Name the state of the proposal in the text. **[Recommended]**

#### V163 · Low · A command of the person can run between the commands of an accept · Confirmed by reading

- **Evidence.** The window refuses a second accept while one runs (`NativeThreeDEngine.cs:463-464`), and each other action,
  for example Undo, still runs. The accept applies its commands one at a time on the thread pool. **[Observed]**
- **Impact.** An Undo between the first and the second command of an accept removes the first one, and the proposal still
  says `accepted`. **[Inferred]**
- **Correction.** Hold the actions that change the document while an accept runs, as the tools of the ring dim.
  **[Recommended]**

#### T180 · Low · The gate of the vision library still names OpenCvSharp4 · Confirmed by reading

- **Evidence.** The comment and the message of `Only_The_Vision_Spike_Pins_The_Vision_Library` say "OpenCvSharp4"
  (`Engine.Tests/Governance/DependencyDirectionGateTests.cs:1411` and `:1422`). Since V0b the spike pins OpenCvSharp5. The
  rule matches the prefix "OpenCvSharp", so the gate still holds. **[Observed]**
- **Correction.** Name the library with no version in the comment and the message. **[Recommended]**

## 4 The next planned task

The rule of section 7 of `docs/templates.md` puts V159, a high finding, into a task before the next product phase: TASK-0330
(P0.138). The next product phase is the pending chip of a proposal (ADR-0092 item 5), which the owner chose on 2026-11-09
after this review. TASK-0331 (P0.139) holds the medium and low findings after that phase. P0.137 (TASK-0328), the filters of
"Open project...", came on 2026-11-09 after the commit of this review, and this review does not examine it.

## 5 Research for that task

- **V159.** `DesktopUi.PanelRectangles` gives each panel of the frame, so the arc can test its two targets against them. The
  probe of this review gives the three display sizes.
- **The pending chip.** The chips of ADR-0082 and ADR-0105 have a key for each value: `EdgeValues.ConstraintKey`,
  `EdgeValues.DistanceKey` and the key of a sketch value. The commands that change a value are `SetSketchConstraintValue`
  (`sketchId`, `constraintId`, `value`) and `SetExtrudeDistance` (`extrudeId`, `distance`). A proposal holds its envelopes
  with their parameters, so the window can find the chip of each value that a proposal changes and show "C3  60 → 75" on
  it. The same names can replace the identifiers cut to 8 characters in the command lines of the arc, the weakest point of
  R16r.

## 6 Recommended approach

Fix V159 first, then the pending chip, which draws on the same chips and needs taps that reach them. V160 is medium, so it
waits for TASK-0331 by the rule of section 7, although it is a tap on the same targets; the fix of V159 must not move
Accept onto a tool of a ring. **[Recommended]**

## Questions for the owner

Questions Q1 to Q5 of the review of 2026-11-09 have no answer yet. This review repeats them with the same options and
recommendations.

- **Q1. The parts of R-0031 to accept.** A: accept each, and R-0031 closes when T4 is fixed; B: keep them open.
  Recommended: A.
- **Q2. Five event kinds that no code emits (E6 d).** A: an ADR removes them from the index until a phase emits them; B: keep
  them as planned kinds. Recommended: A.
- **Q3. The check of V3 on macOS.** A: accept that V3 is proved only when a window first runs on macOS; B: a job on the
  macOS runner that makes an instance and a device through MoltenVK. Recommended: A.
- **Q4. `IList` in `3DEngine.Core` (V7 part 6).** A: accept it; B: give read-only lists. Recommended: A.
- **Q5. The up axis of a document (V158).** A: a switch of the view between z up and y up, in the client only; B: the
  Document records its up axis in its header, y for a file with no field, so an older file opens upright (a change of the
  format, ADR-0015); C: turn the shelf by commands. Recommended: B.
