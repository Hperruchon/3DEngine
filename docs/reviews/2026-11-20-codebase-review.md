---
date: 2026-11-20
commit: b3b9b6e
ledger: v0.407
previous: 2026-11-18-b-codebase-review.md
---

# Codebase review — 2026-11-20

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

The review examines `b3b9b6e`, the close of R20d (TASK-0423) at v0.407 on the branch `r20d-history-arc`. Its code is the code
of `main` after two merges that wait. Four product milestones (R31a, R31b, R30e and R20d) and eleven entries follow v0.396, the
version that the review of 2026-11-18 (b) examined, so the next product milestone needs this review first. The range is
`8b2619a..b3b9b6e`: 40 commits and 124 files. Two agents read the window side and changed no file. One read the sheet page and
"Put on sheet" (TASK-0410, TASK-0415, TASK-0416, TASK-0421). One read the layout, the ring of the part and the rows (TASK-0418,
TASK-0419, TASK-0420, ADR-0177). The session examined the engine, the cache of the cuts, the pipeline, R20d and R30f. It read
each finding of the agents in the code at its line. No probe ran in the repository: each probe
below is a test as text or a trace with its lines. The review uses no lead of Codex.

## Summary

- **Of the thirteen findings of the last review, eleven are fixed, and two are fixed for their forms.** TASK-0415 (P0.166)
  fixed the high finding V202. TASK-0416 (P0.167) fixed E221, E223, E224, E225 and V203 to V208, and fixed V206 and V209 for
  their forms. E222 is fixed in part: a cache of the cuts. TASK-0410 (P0.165) fixed each open finding of the first review of
  2026-11-18 (E218, E219, E220, V194, V196, V197, V198, V200, V201). **[Observed]**
- **No new finding is critical or high.** **[Observed]**
- **Four new findings are medium.** T183: the concurrency test of the HTTP host decides its result with a time limit of 11 s,
  and it failed on Windows at v0.355, v0.376, v0.400 and v0.405 with progress in each run. V210: a race of one frame in the host
  brings back V206. V211: the file menu can open behind the structure view, so on a phone "Open project..." takes no tap. V212: a
  pad of a sketch on a phone measures its room at one top and opens at another, so its keys and Apply can scroll out of view.
  **[Observed]** by reading, and **[Inferred]** where the finding says so.
- **Eleven new findings are low.** E226: the cache of the cuts misses each cut when a read needs more than 16. T184: a crash of
  the test host gives no cause. V213 to V221: the menu tap, the stale boxes of the ring, a lost drag, two message texts, two
  statements of ADR-0177, the texts of the history arc that have no ground, and a first row that is not cut. **[Observed]**
- **The engine changes hold.** The key of the cache of the cuts compares the exact content, so a kept cut is the cut that a new
  cut gives. The test of the whole circle, the axis of a cone, the finite check, the size of a text and the refusal of a move
  with no change agree with their findings. No contract changed in the range. **[Observed]**
- **Open findings: 0 critical, 2 high, 11 medium and 27 low.** **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source line is a line of a
`.cs` or `.razor` file that git tracks. The script `measures.js` of this review reads each file through `git show` at one commit;
at `8b2619a` it gives each value of the last review.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 17 | `.csproj` lines in `3DEngine.sln`. The last review gave 17; `3DEngine.Android` is outside the solution. |
| Production source lines | 48,975 (373 files) | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests`, `3DEngine.ImageTests` and `3DEngine.VisionTests`. The last review gave 47,741 in 370 files. |
| Test source lines | 57,907: Engine.Tests (32,961 in 147 files), 3DEngine.Tests (4,228 in 25 files), 3DEngine.ImageTests (20,652 in 100 files) and 3DEngine.VisionTests (66 in 1 file) | The four test projects. The last review gave 56,031. |
| Tests | 2,962 | `dotnet test` of the solution at `b3b9b6e` in the copy of the review: 1,714, 390, 855 and 3 passed, 0 failed, 0 skipped. `eng/test-floor.txt` gives the same counts. The last review gave 2,883. |
| Gate classes | 20 | Tracked files whose name ends in `GateTests.cs`. `eng/repo-gates` gives 488 runs of 168 checks, 0 failed. The last review gave 20, and 488 runs of 168 checks. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`: 0 warnings, 0 errors. |
| Open register entries | 11 | Section "Open" of `docs/register.md`; the limit is 15. The last review gave 14. |
| ADRs | 169 | `docs/adr/0001` to `0061` and `0070` to `0177`, with 0003 and 0011 in `docs/adr/archive/`. In force: 167 (58 accepted, 109 amended). None is proposed. The last review gave 163. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 2 | The same method: T4, V141. The last review gave 3; V202 is fixed. |
| Findings: medium | 11 | The same method: E6, E13, E19, E21, T32, T37, E222, T183, V210, V211, V212. The last review gave 17; ten are fixed. |
| Findings: low | 27 | The same method: E17, E24, E34, E39, E57, E84, E160, V15, V29, V84, V88, T143, C1, P1, V143, V144, E226, T184, V213, V214, V215, V216, V217, V218, V219, V220, V221. The last review gave 26; ten are fixed. |
| Lines in the rule files | 3,902 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The last review gave 3,832. |
| Sentences with "must" | 61 | `grep -o -w -i must` on the same ten files. The last review gave 61. |
| Sentences with "do not" | 63 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 63. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe found. E13, T32,
E84, E160, V84, V141 and E222 are fixed in part and count as open. A finding that the owner accepted, or that an ADR closes by a
decision, counts as closed.

## Findings of the previous review

The lines up to V201 are the lines of the review of 2026-11-18 (b), with a new state for T4, V15 and E218 to V201. No other
file that an open finding of those lines names changed in this range: the change from `8b2619a` to `b3b9b6e` is in the drawing,
the export, the sheet commands and the queries of the sheet of `Engine.Core`, in the window of `3DEngine.App`, in its tests, in
the tests of the sheet, of the host and of the pipeline in `Engine.Tests`, in `eng/lost-runner.js` and in the new workflow `.github/workflows/rerun-lost-runner.yml`. The lines
of E221 to V209, the findings of the review of 2026-11-18 (b), follow them.

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Fixed. TASK-0321, v0.304. A whole JSON number stays a long, so an integer field gets the exact value above 2^53.
- **E6** — Open in four parts of five. The owner answered Q2 B on 2026-11-10 (TASK-0331): part (d), the five planned event kinds,
  stays by decision. `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` has no commit since `3379675`. Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. No change.
- **E8** — Fixed. TASK-0044, v0.37. No change.
- **E9** — Fixed, and it holds. No change.
- **E10** — Fixed. TASK-0035, v0.43. No change.
- **E11** — Fixed. TASK-0115, v0.115. No change.
- **E12** — Fixed. TASK-0321, v0.304. A number field takes a long or an int.
- **E13** — Fixed in part. No change: `Engine.Api.Http/Endpoints/QueriesEndpoint.cs` has no commit since `3379675`, and
  `Engine.Cli/Cli.cs` has no commit in this range. **[Observed]**
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
- **E34** — Open. `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs` changed only for the member `Rotate` (K8, TASK-0335), 29 lines
  in. Register entry R-0036.
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
- **V3** — Accepted by the owner. Q3 A, 2026-11-10 (TASK-0331): V3 is corrected in the code and is proved when a window first runs
  on macOS. R-0022 is an accepted compromise.
- **V4** — Fixed. The exit code of a failed start and the results of Vulkan were fixed before; TASK-0319, v0.301, fixed the log line of the instance, the old layers and the two types with no caller.
- **V5** — Fixed. TASK-0076, v0.74. No change.
- **V6** — Fixed. TASK-0076, v0.74. No change.
- **V7** — Accepted by the owner for part 6, and fixed for parts 1 to 5. Q4 A, 2026-11-10 (TASK-0331): only the render loop writes
  `Scene` and `Entity`. R-0030 is an accepted compromise.
- **V8** — Fixed. TASK-0074, v0.75. No change.
- **V9** — Fixed. TASK-0074, v0.75. No change.
- **V10** — Fixed. TASK-0074, v0.75. No change.
- **V11** — Fixed in part. TASK-0074, v0.75. No change.
- **V12** — Fixed, and changed by decision. No change.
- **V13** — Fixed for its parts. No change.
- **V14** — Fixed. TASK-0074, v0.75. No change.
- **V15** — Open. `3DEngine.App/SessionBodies.cs` changed only for the box of the drawn bodies (`SheetBox`, V197, TASK-0410). The wait for an idle device at each consumed body and the memory of `GpuBuffer` did not change.
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
- **T4** — Open in one part of three. No change: the dispatch gate does not read the desktop host. The host changed in this range for the sheet commands of the queue (TASK-0410) and for "Put on sheet" (R30e); the gate did not change. R-0031 closes when this part is fixed.
- **T5** — Fixed. TASK-0320, v0.303. The gates strip the quotes and the brackets of an `affects` list and refuse what stays.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Accepted by the owner for the clock of the register gate and the case of a path on Windows (Q1 A, 2026-11-10,
  TASK-0331); the other parts are fixed. Register entry R-0031.
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
- **T23** — Accepted by the owner for the provenance of the native binaries (Q1 A, 2026-11-10, TASK-0331); the other part is fixed.
  Register entry R-0031.
- **T24** — Fixed. No change.
- **T25** — Accepted by the owner. Q1 A, 2026-11-10 (TASK-0331): the local mode of the write-set check reads the index only; the
  pipeline judges each commit. Register entry R-0031.
- **T26** — Fixed. TASK-0057, v0.54. No change.
- **T27** — Fixed. TASK-0058, v0.56. No change.
- **T28** — Fixed. No change.
- **T29** — Fixed. TASK-0057, v0.54. No change.
- **T30** — Fixed. TASK-0057, v0.54. No change.
- **T31** — Accepted by the owner. Q1 A, 2026-11-10 (TASK-0331): the three gates that run engine code stay in the test process.
  Register entry R-0031.
- **T32** — Fixed in part. No change: `eng/write-set-check/Program.cs` has no commit since `3379675`.
- **T33** — Fixed. TASK-0057, v0.54. No change.
- **T34** — Fixed. TASK-0057, v0.54. No change.
- **T35** — Fixed. TASK-0057, v0.54. No change.
- **C1** — Open. No change: `Engine.Contracts/Geometry/IBRepOps.cs` has no commit since `3379675`. Register entry R-0027.
- **P1** — Open. `Engine.Api.Http/WebSockets/EventBroadcaster.cs` has no commit in this range, so the snapshot stays under the lock of
  the broadcaster. Register entry R-0027.
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
- **E160** — Fixed in part. The comment of `Engine.Contracts/Sketch/DatumRecord.cs` has no change; `DatumResolver` changed only for the
  turn of a datum on a turned face (K8, TASK-0335).
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
- **V120** — Fixed. TASK-0264, v0.251. The ring skips a release on a chip of the last frame and on a row (`DesktopUi.Ring.cs:386`); R20a (TASK-0341) kept the check and added that the press and the release are on the target of one segment. The chip skips a release on a row (`DesktopUi.Values.cs:55-58`). Test `A_Tap_On_A_Chip_Over_A_Segment_Gives_The_Chip_Only` (`ToolRingTests.cs`). Note: the ring reads the chips of the last frame (`Ring` runs before `Values`, `DesktopUi.cs:548-550`), so a chip that moves in this frame is one frame late. I found no case where this gives two tools.
- **V121** — Fixed. TASK-0264, v0.251. The portrait segment is `min(0.42, (π/2 − (n−1)·0.04)/n)` (`DesktopUi.Ring.cs:97`). Theory `Each_Segment_Of_A_Portrait_Ring_Is_Between_The_Left_And_The_Top` (`ToolRingTests.cs`).
- **V122** — Fixed. TASK-0264, v0.251. `Layout` takes the inset (`DesktopUi.Ring.cs:92`, `:101`), and `Ring` passes `_inset` (`DesktopUi.Ring.cs:357`). Test `The_Portrait_Ring_Is_Inside_The_Safe_Area` (`ToolRingTests.cs`). No device ran it.
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
- **V158** — Closed by the answer Q5 C of the owner (2026-11-10, TASK-0331): commands turn the shelf. K8 (TASK-0335, v0.318) gives
  the command `Rotate`, and R-0057 is closed. The shelf of the owner is not turned yet; it is the file of the owner.
- **V159** — Fixed. TASK-0330, v0.313 (ADR-0107). With no ring of a body, the arc is in the room right of the panel, and the
  window of the live sketches does not show while a proposal waits. `ResponsiveLayoutTests` (R19) also holds Accept out of
  each panel at 13 sizes.
- **V160** — Fixed. TASK-0331, v0.315. The ring skips the targets of a proposal of the last frame
  (`DesktopUi.Ring.cs:246-247`). A probe with an active sketch at 1440 by 900, 1280 by 720 and 1024 by 640 gave only the
  action of the proposal for each tap.
- **E198** — Fixed. TASK-0331, v0.315. The post refuses a repeated `commandId` (`ProposalsEndpoint.cs:71-81`), and the accept
  stops with `E-API-PROPOSAL-COMMAND-KNOWN` on a cached result (`Proposals.cs:112-122`).
- **V161** — Fixed. TASK-0331, v0.315. No read with no selection, and the read runs on the thread pool
  (`HistorySteps.cs:115-142`).
- **V162** — Fixed. TASK-0331, v0.315. The sheet ends above the ring when it fits there; else it covers the ring, as ADR-0111
  item 4 permits.
- **E199** — Fixed. TASK-0331, v0.315. The text of the refusal names the state of the proposal.
- **V163** — Fixed for its forms. TASK-0331, v0.315. The window holds each listed action and intent while an accept runs.
  V165 holds a new form: the tap of the path line, which came later with R17a.
- **T180** — Fixed. TASK-0331, v0.315. The gate names the vision library with no version.
- **E200** — Fixed. TASK-0338, v0.320. `KernelProjection.ExactFrame` and `ExtrudeFace` (`Engine.Core/Features/KernelProjection.cs`)
  build the frame of a datum on a face in rationals, with the steps of the solid: the carrier frame of the face, each
  Translate, each Rotate through the nine numbers of the kernel, and the turned flip. The theories
  `A_Pocket_From_A_Datum_On_A_Moved_Or_Turned_Face_Opens_It` (each probe of the last review) and
  `A_Pocket_On_The_End_And_On_A_Side_Of_A_Post_Opens_Each_Face` run in the pipeline. **[Observed]**
- **V164** — Fixed. TASK-0339, v0.324. `ProposedValues` names only a distance or a radius as a length; an angle shows in
  degrees, and another kind keeps the line of its command. Test: `PendingChipTests`
  (`The_Line_Shows_An_Angle_In_Degrees_And_Names_No_Other_Kind_As_A_Length`). The angle is presentation in the client, so
  rule 1 of the determinism rules does not apply. **[Observed]**
- **E201** — Fixed. TASK-0339, v0.324. `Rotation.FromQuaternion` refuses a sum of squares under 2^-969, and the command
  gives E-GEOM-QUATERNION-TOO-SMALL. A probe of 2,000,000 random quaternions with parts from 2^-484 to 2^-620 accepted 17,368,
  and for each of them |RᵀR − I| was at most 7.8e-16, so no accepted turn scales a body. ADR-0114 item 5 records that an old log with
  such a turn now fails at replay. **[Observed]**
- **V165** — Fixed. TASK-0339, v0.324. `AcceptHold.Holds` frees `PathAction`, `FocusAction` and `EndFocusAction`. Test:
  `The_Path_Line_And_The_Focus_Are_Free_During_An_Accept`. **[Observed]**
- **T181** — Fixed. TASK-0339, v0.324. The host calls `AcceptHold.Held` and `AcceptHold.Pass`, which a test reads
  (`The_Host_Holds_Only_While_An_Accept_Runs`); `ResponsiveLayoutTests` checks each pair of windows, the sheet excepted. The
  armed tap on a face still reads a local flag in the host, with the same decision as before. **[Observed]**
- **V166** — Fixed. TASK-0345, v0.332. `EdgeLines.Feature` skips a triangle whose normal is zero (`EdgeLines.cs:60`). Test
  `A_Triangle_Of_Zero_Area_Across_A_T_Vertex_Adds_No_Edge` (`BodyEdgesImageTests.cs`). A sliver whose normal is very small
  but not zero still adds its edge. A T-vertex inside a closed flat face still gives false lines: V171. **[Observed]**
- **V167** — Fixed. TASK-0345, v0.332, ADR-0121. A mesh of more than 100 000 triangles (`EdgeLines.MostTrianglesAtUpload`)
  goes to the GPU with no lines, and the search runs on the thread pool; a rebuild or a removal drops the search of the old
  mesh. Tests `A_Mesh_Above_The_Count_Of_The_Upload_Finds_Its_Edges_On_The_Thread_Pool` and
  `A_Rebuild_Drops_The_Search_Of_The_Old_Mesh`. No test runs a real mesh of 100 000 triangles or more. **[Observed]**
- **V168** — Fixed. TASK-0350, v0.335, ADR-0123. The sketch that made a body is found along the first input of each step
  of the chain, and the history gives that step after the six newest when it is not one of them (`HistoryStep.MadeFrom`).
  Tests in `HistoryArcTests`: a plate cut two and three times, and the sketch of the cut body, not of the tool. **[Observed]**
- **V169** — Fixed. TASK-0350, v0.335. The ring skips a release anywhere on the rows. Two tests in `RingTapTests`, at 640
  by 360 and 1024 by 576. **[Observed]**
- **V170** — Fixed. TASK-0350, v0.335. The red state of a failed step and the plate of a wide label go through
  `GetColorU32`, so they fade. Test in `RingTapTests`. **[Observed]**
- **V171** — Fixed. TASK-0350, v0.335. A sliver joins its two short edges to the triangle across its long edge, when each
  of the three has one triangle. Two tests in `BodyEdgesImageTests`: a flat face and a crease. **[Observed]**
- **V172** — Fixed. TASK-0348, v0.334. ADR-0119 gives the span of four segments as 1.80 radians, as `RingSpan` does.
  **[Observed]**
- **V173** — Fixed. TASK-0356, v0.342, ADR-0129 item 2. The covers of the plates hold the panels and the fixed rectangles of
  the last frame. Test `DimensionLabelsTests.No_Plate_Is_Under_The_Window_Of_The_Live_Sketches`. V179 holds a second form:
  the arc of a proposal on a landscape display is no window. **[Observed]**
- **V174** — Fixed. TASK-0356, v0.342, ADR-0129 item 1. When the rows of the list of the hidden bodies do not fit, "Show
  all" comes first, then the bodies that fit, then "N more". Theory in `HiddenBodiesTests`. **[Observed]**
- **V175** — Fixed for its forms. TASK-0356, v0.342, ADR-0129 item 4. A chain of collinear sides of one triangle each from end
  to end joins a side of one triangle; the area of a sliver is zero within 1e-9 of the square of its longest side. The agent
  of this review found T-vertices on both sides of a crack at different places ("a zipper") still give lines inside a flat
  face, as before the fix; ADR-0129 does not claim that form, and no probe showed that the backend gives it. A grid of
  960 000 triangles with T-vertices took 925 ms. **[Observed]**
- **V176** — Fixed. TASK-0356, v0.342, ADR-0129 item 2. Test `DimensionLabelsTests.A_Label_That_No_Move_Frees_Stays_At_Its_Place`.
  **[Observed]**
- **V177** — Fixed. TASK-0356, v0.342, ADR-0129 item 3. The window expects the text of a step for the identifier of its
  command. The agent of this review gave a second form, a value edit of the window that clears the texts of a running
  accept; the host holds each value edit while an accept runs (`AcceptHold.Held`), so it does not occur, as the review of
  2026-11-14 found for the first form. **[Observed]**
- **V178** — Fixed. TASK-0362, v0.348. The host gives the count of the log at the end of an accept, and the line shows "Undo"
  only while the count of the status is that count. Theory `ProposalWindowTests.A_Step_After_An_Accept_Takes_Undo_Away`. V182
  holds a later form: the host reads the count in the frame after the accept. **[Observed]**
- **V179** — Fixed. TASK-0362, v0.348. The arc of a landscape display keeps the boxes of its circles, its words, its caption,
  the plate of its lines and "AGENT IS WAITING", and the plates of the next frame take them as covers. Test
  `DimensionLabelsTests.No_Plate_Is_Under_The_Arc_Of_A_Proposal`. **[Observed]**
- **V180** — Fixed. TASK-0362, v0.348. The mix test checks Reject and Accept at each size. **[Observed]**
- **V181** — Fixed. TASK-0367, v0.353, ADR-0136 item 3. The whole caption under the arc wraps at its spaces into lines that fit
  the chord. Theory `ProposalCaptionTests.A_Caption_Longer_Than_The_Chord_Shows_Whole_Under_The_Arc`. **[Observed]**
- **V182** — Fixed. TASK-0367, v0.353. The step texts keep the count of the log after each applied command, and the line takes
  the count after the last command of the proposal. Test
  `FixedStatusTests.The_Count_After_A_Proposal_Is_The_Count_At_Its_Last_Command`. **[Observed]**
- **V183** — Fixed. TASK-0367, v0.353. The left limit tests the edge of the plate farther from the centre. Theory
  `ProposalPlaceTests.A_Block_That_Moves_Left_Stays_Inside_The_Inner_Circle`. **[Observed]**
- **V184** — Fixed for its forms. TASK-0367, v0.353, ADR-0136 item 1. Accept and its word lie on no tool at 1440 by 900. V186
  holds two later forms: Reject is not tested, and on a small landscape display no angle frees Accept. **[Observed]**
- **V185** — Fixed. TASK-0373, v0.357, ADR-0139 item 1. `SketchCamera.Face` calls `Camera.LeaveFlat` first. Test in
  `FlatViewTests`. **[Observed]**
- **V186** — Fixed. TASK-0373, v0.357, ADR-0139 item 2. Reject searches its angle, and a tool under an end of the arc or its
  word hides while the proposal waits. Theory in `ProposalArcTests` at five landscape sizes. **[Observed]**
- **V187** — Fixed. TASK-0373, v0.357. The test of V160 runs at 640 by 360 with its control. **[Observed]**
- **V188** — Fixed. TASK-0374, v0.359, ADR-0141 item 1. A flat view has a scale of its own. Test
  `FlatViewTests.A_Zoom_In_A_Flat_View_Keeps_The_Eye_Out_Of_The_Box`. **[Observed]**
- **V189** — Fixed. TASK-0374, v0.359, ADR-0141 item 2. The far plane of a flat view is behind the box. Test
  `FlatViewTests.The_Far_Plane_Of_A_Flat_View_Is_Behind_The_Box`. **[Observed]**
- **V190** — Fixed. TASK-0374, v0.359, ADR-0141 item 3. The floor of the rows is the middle of the display while a proposal
  waits on a portrait display. Theory `ProposalPortraitTests.In_The_First_Frame_Of_A_Proposal_The_Rows_Are_Under_The_Sheet`.
  **[Observed]**
- **E202** — Fixed. TASK-0379, v0.364. Each end of an outline goes to its exact place along the axis. Test
  `ViewPlaceTests.The_Outline_Of_A_Hole_Goes_From_Face_To_Face`. **[Observed]**
- **E203** — Fixed. TASK-0379, v0.364. The end of a piece comes from its exact place along the axis, and a line of no length
  or with a value that is not finite keeps the facets. Theory
  `ViewPlaceTests.A_Cylinder_Turned_By_180_Degrees_Is_One_Circle_From_The_Top` at the origin and moved. **[Observed]**
- **V191** — Fixed. TASK-0379, v0.364. The free part leaves a tall window at the right edge. Theory
  `FrameFreePartWindowTests.The_Free_Part_Leaves_The_Window_Of_The_Live_Sketches`. **[Observed]**
- **E204** — Fixed. TASK-0379, v0.364 (it moved from TASK-0380). An edge at one place along the axis gets the circle of the
  side there. Tests `ViewPlaceTests.A_Cut_Cone_From_The_Top_Shows_The_Circle_Of_Its_Cut` and
  `ViewPlaceTests.The_Outline_Of_A_Cut_Cone_Ends_At_The_Cut`. **[Observed]**
- **T182** — Fixed. TASK-0380, v0.366. The test of the circles of a Document asserts the ends of its hidden lines.
  **[Observed]**
- **E205** — Fixed. TASK-0380, v0.366. An ellipse with a minor half axis near zero is an end across the view. Test
  `ViewPlaceTests.A_Cylinder_That_Two_Turns_Laid_On_Its_Side_Shows_Each_End_As_One_Line`. **[Observed]**
- **E206** — Fixed. TASK-0391, v0.376, ADR-0154. One elimination of J·Jᵀ takes J·g of each line as one more column, and
  `GetSketch` takes the status and the fixed lengths from it. Test
  `FixedLengthsTests.The_Status_And_The_Fixed_Lengths_Of_A_Stair_Of_100_Lines_Take_Less_Than_One_Second`. **[Observed]**
- **E207** — Fixed. TASK-0392, v0.378, ADR-0156 item 1. `NormaliseRows` divides each row of J by its length before the
  elimination of the status and of the analysis. Theory `FixedLengthsTests.The_Freedom_And_The_Fixed_Lengths_Do_Not_Depend_On_The_Size`.
  **[Observed]**
- **E208** — Fixed. TASK-0392, v0.378, and P10.2 (TASK-0393, v0.377). The history of ADR-0142 records that R22 read no line
  of K9, and `Engine.Core/Drawing/SheetLayout.cs` line 50 reads `ViewEdges.Of` for each view of a sheet. **[Observed]**
- **E209** — Fixed. TASK-0391, v0.376. `ProductWith` is no longer in `SketchSolver.cs`, so the comment of J·Jᵀ is above its
  one method. **[Observed]**
- **E210** — Fixed. TASK-0391, v0.376. `GetSketchQueryHandler.cs` has no pattern `is var`. **[Observed]**
- **V192** — Fixed. TASK-0392, v0.378. One helper of `3DEngine.App/EdgeValues.cs` gives the ends of an edge: the two points of
  its segments that are farthest apart. Test `FlatViewValuesTests.The_Ends_Of_An_Edge_In_Two_Parts_Are_Its_Farthest_Points`.
  **[Observed]**
- **V193** — Fixed. TASK-0392, v0.378. `3DEngine.Vulkan/SdlInput.cs` keeps the keys of each modifier that are down, and the
  flag holds while one of them is down. Theory `SdlInputTests.A_Modifier_Stays_While_One_Of_Its_Keys_Is_Down`. **[Observed]**
- **E211** — Fixed. TASK-0400, v0.388, ADR-0164 item 1. A frame of any length gives the circles and the ellipses of the design.
  Theory `SheetViewTests.A_View_Of_Any_Length_Gives_The_Curves_Of_The_Design`. **[Observed]**
- **E212** — Fixed. TASK-0400, v0.388, ADR-0164 item 1. The length of a frame never squares a double. The same theory, with the
  frames of 1e-200, 1e200 and an eye of 3e200. **[Observed]**
- **E213** — Fixed. TASK-0400, v0.388, ADR-0164 item 5. The back-substitution of several right sides takes the entries that are
  not zero. Test `FixedLengthsTests.The_Fixed_Lengths_Of_A_Stair_Of_600_Lines_Take_A_Time_Near_The_Status`. **[Observed]**
- **E214** — Closed by a decision. TASK-0400, v0.388: ADR-0164 item 6 and ADR-0156 item 1 record the limit of a constraint that
  joins sizes in a ratio above about 1e5; the code does not change. **[Observed]**
- **E215** — Fixed. TASK-0400, v0.388, ADR-0164 item 2. A number that is not finite is 0 in the SVG and the DXF file. Tests
  `DxfExportTests.A_Text_And_A_Number_Are_Ascii` and `SvgExportTests.A_Name_With_A_Character_That_Xml_Does_Not_Permit_Gives_Xml`.
  **[Observed]**
- **E216** — Fixed. TASK-0400, v0.388, ADR-0164 item 3. The DXF chain of an ellipse has a stop at each quarter turn. Test
  `DxfExportTests.An_Almost_Closed_Arc_Of_An_Ellipse_Goes_Around_The_Centre`. **[Observed]**
- **E217** — Fixed. TASK-0400, v0.388, ADR-0164 item 4. A character that XML does not permit is U+FFFD in the SVG file. Test
  `SvgExportTests.A_Name_With_A_Character_That_Xml_Does_Not_Permit_Gives_Xml`. **[Observed]**
- **E218** — Fixed. TASK-0410, v0.399. `ViewEdges.Whole` (`Engine.Core/Drawing/ViewEdges.cs` line 898) gives a cross when the arcs of a circle, visible and hidden, close the turn. Test `SheetViewTests.A_Hole_That_A_Body_Covers_In_Part_Keeps_Its_Cross`. **[Observed]**
- **E219** — Fixed. TASK-0410, v0.399. The comment of `SheetDimensionInfo` names "diameter" (`Engine.Core/Queries/GetSheetQuery.cs` line 82). **[Observed]**
- **E220** — Fixed. TASK-0410, v0.399, with the answer Q1 A. `GetViewLinesQueryHandler.Read` adds the centre lines with their 3 mm (`Engine.Core/Queries/GetViewLinesQueryHandler.cs` line 69). Tests in `Engine.Tests/Drawing/ViewLinesTests.cs` (lines 55 and 87). **[Observed]**
- **V194** — Fixed. TASK-0410, v0.399. `SheetActions.Create` checks each place with `SheetPlacement.Fits` and names each view that does not fit (`3DEngine.App/SheetActions.cs` lines 74 to 89). Test `SheetAddTests.A_New_Sheet_Names_Each_View_That_Does_Not_Fit`. **[Observed]**
- **V195** — Fixed. TASK-0408, v0.393, ADR-0168 item 6. The home screen closes the sheet page and the pad of a sheet
  (`DesktopUi.Home.cs`, `ShowHome`), and the keys of the pad rest while the home screen shows. Test
  `SheetPageTests.The_Page_And_The_Pad_Hold_Their_Keys`. **[Observed]**
- **V196** — Fixed. TASK-0410, v0.399. "Add to sheet" and a new sheet from the pad go to the queue (`NativeThreeDEngine.cs` lines 576 to 583), and an event of a sheet does not start a new read of the lines. Tests `SheetAddTests.The_Frame_Sends_The_Commands_Of_A_Sheet_To_The_Queue_With_The_Box_Of_The_Drawn_Bodies` (it reads the text of the source file) and `FlatViewLinesTests.An_Event_Of_A_Sheet_Does_Not_Start_A_New_Read`. **[Observed]**
- **V197** — Fixed. TASK-0410, v0.399. `SessionBodies.SheetBox` and `SheetPlacement.Drawn` give the box of each live body that is not a mesh. Test `SheetAddTests.The_Box_Of_A_New_View_Is_The_Box_Of_The_Bodies_That_The_Sheet_Draws`. No test runs `SheetBox` with a session. **[Observed]**
- **V198** — Fixed. TASK-0410, v0.399. `GizmoLayout` gives null while the paper shows (`NativeThreeDEngine.cs` line 433). Test `FlatViewLinesTests.The_Gizmo_Does_Not_Show_While_The_Paper_Shows`. **[Observed]**
- **V199** — Fixed. TASK-0408, v0.393, ADR-0168 item 6. The router holds the keys while the pad of a sheet or the sheet page
  shows (`DesktopUi.cs` line 478), so the Escape that closes the pad reaches no intent. Test
  `SheetPageTests.The_Page_And_The_Pad_Hold_Their_Keys`. **[Observed]**
- **V200** — Fixed. TASK-0410, v0.399. The comment of `SheetPlacement.cs` (line 127) and ADR-0167 item 3 give the order of the code. Test `SheetAddTests.A_View_Goes_Next_To_Each_View_With_A_Shared_Axis_In_Their_Order`. **[Observed]**
- **V201** — Fixed. TASK-0410, v0.399. `ViewLines.Notes` gives the text of each skipped body, and `DesktopUi.Views.cs` lines 331 to 333 draw it. Test `FlatViewLinesTests.The_Paper_Has_The_Centre_Lines_And_Names_Each_Skipped_Body`. **[Observed]**
- **E221** — Fixed. TASK-0416, v0.405. `SheetLayout` skips a cut line whose place on the paper is not finite (`Engine.Core/Drawing/SheetLayout.cs` line 174). Test `SheetSectionTests.A_Cut_Line_At_A_Place_That_Is_Not_Finite_Is_Not_Given`. **[Observed]**
- **E222** — Fixed in part, so open. TASK-0416, v0.405. `SectionCut.Cut` keeps the last 16 cuts with their B-Rep (`Engine.Core/Drawing/SectionCut.cs` lines 105 to 148), so a read after a command that changes no body makes no new cut. The first read after each change of a body still cuts with a box subtract under the serial lock, and a read that needs more than 16 cuts gets no gain (E226). Test `SheetSectionTests.A_Read_Keeps_The_Cut_Until_A_Body_Changes`. **[Observed]**
- **E223** — Fixed. TASK-0416, v0.405, Q1 A of the owner. `AxisSide` takes an edge in one plane with the axis (`ViewEdges.cs` line 952). Test `SheetSectionTests.A_Section_Through_The_Axis_Of_A_Cone_Shows_The_Axis_In_Each_Look`. **[Observed]**
- **E224** — Fixed. TASK-0416, v0.405. The SVG and the PDF file write the size of the font Size / 0.718 (`Engine.Core/Export/SvgExport.cs` lines 32 and 106), and the letter is half its height under its place (`SheetLayout.cs` line 196). Test `SheetSectionTests.The_Size_Of_A_Text_Is_The_Height_Of_A_Capital_In_The_Three_Files`. **[Observed]**
- **E225** — Fixed. TASK-0416, v0.405. MoveSheetViews refuses a move to the place of the view (`Engine.Core/Commands/MoveSheetViewsCommandHandler.cs` line 67), and the page sends no move for a drag that ends at that place (`DesktopUi.SheetPage.cs` line 477). Test `SheetMoveTests.A_Move_To_The_Place_Of_The_View_Is_Refused`. **[Observed]**
- **V202** — Fixed. TASK-0415, v0.398. `SheetPage.Drag` gives null for a view that is not in the read (`3DEngine.App/SheetPage.cs` lines 124 to 125), a read without the dragged view ends the drag (`DesktopUi.SheetPage.cs` lines 434 to 435), and its release gives nothing. Test `SheetPageTests.A_Read_Without_The_Dragged_View_Ends_The_Drag`. A drag that a read ended can take the next tap (V215). **[Observed]**
- **V203** — Fixed. TASK-0416, v0.405. The router reads once, before the watchers, if a pad holds the keys (`DesktopUi.cs` lines 491 to 494), and the page takes no key then (`DesktopUi.SheetPage.cs` line 175). Test `SheetPageTests.The_Escape_Of_A_Pad_Does_Not_Reach_The_Page`. **[Observed]**
- **V204** — Fixed. TASK-0416, v0.405. A read without the view clears the section and the selection, and a change of the sheet clears the section and the row of the formats. Test `SheetSectionPageTests.A_Section_Ends_When_Its_View_Or_Its_Sheet_Goes`. **[Observed]**
- **V205** — Fixed. TASK-0416, v0.405. "Export" ends a section and "Section" closes the formats. Test `SheetSectionPageTests.The_Formats_And_A_Section_Are_Not_Open_Together`. **[Observed]**
- **V206** — Fixed for its forms. TASK-0416, v0.405. The page keeps the values that it sent until a read that is not pending follows a pending read (`DesktopUi.SheetPage.cs` lines 62 to 75 and 118 to 129). Test `SheetPageTests.Two_Changes_Of_One_View_Before_The_Read_Keep_The_First`. A race in the host gives the same fault in one frame: V210. **[Observed]**
- **V207** — Fixed. TASK-0416, v0.405. The paper keeps room for the highest row of the header that changes (`PageStateRows`). Test `SheetPageTests.The_Paper_Keeps_Its_Place_While_The_Header_Changes`. **[Observed]**
- **V208** — Fixed. TASK-0416, v0.405. "Section" shows only when `FlatOf` gives a frame. Test `SheetSectionPageTests.Section_Shows_Only_For_A_View_That_The_Engine_Can_Cut`. **[Observed]**
- **V209** — Fixed for its form. TASK-0416, v0.405. `SheetExports.FileName` decodes the last segment of a content URI (`3DEngine.App/SheetExports.cs` lines 99 to 106). Test `SheetExportTests.The_Text_Of_An_Export_On_The_Phone_Names_The_File`. A URI of the Downloads folder still gives a number: V216. **[Observed]**

## 1 Current architecture

No contract changed in this range: `git diff 8b2619a..b3b9b6e` has no file of `Engine.Contracts`. **[Observed]**

The engine has these changes:

- a static cache of the last 16 cuts of a section in `SectionCut` (E222);
- a whole circle from its arcs (`ViewEdges.Whole`, E218);
- the centre lines in GetViewLines (E220);
- the refusal of a move of a view to its own place (E225).

The window has these changes:

- the ring of the part, the file menu and a structure view in the place of the panel (R31a, ADR-0174);
- rows of 30 pixels on a desktop (R31b, ADR-0175);
- a pad that is compact or low on a small display (TASK-0418, ADR-0172);
- "Put on sheet" in the pad of a value (R30e, ADR-0176);
- covers and a ground for the history arc (R20d).

"Put on sheet" reads GetSheet and sends PlaceSheetDimension through the queue of the sheet page. No client reads an internal
state of the engine. The pipeline has a second workflow, `rerun-lost-runner.yml`. It runs `eng/lost-runner.js` of the default
branch with a token that can start a re-run (ADR-0173). **[Observed]**

## 2 Patterns in use

- **A guard on one path and not on its twin.** The page keeps its sent values until a read after the send, but the host can give
  a read of the old version after the end of the command (V210). The file menu takes its release from the ring and the scene,
  but not from the controls that draw before it (V213). **[Observed]**
- **A decision from one place, and a draw at another place.** `PlacePad` decides "compact" from the top of the panel, and
  `PadWindow` opens a pad of a sketch under the sketch window (V212). **[Observed]**
- **A cache of a fixed count in front of work whose count grows with the document.** The cache of the cuts holds 16 cuts, and
  one read needs one cut for each pair of a section and a body (E226). **[Observed]**
- **A time limit as the result of a test.** ADR-0173 item 4 removed the deciding limit of one test and gave the reason for each
  test: "a slower runner can pass any limit". The class `HttpConcurrencyTests` still decides with a limit (T183). **[Observed]**
- **A record that states a property of the code that it did not read.** ADR-0177 item 1 says that "Show all" is one step of
  undo; the code sends one command for each hidden body (V218). **[Observed]**

## 3 Review of the code

#### E226 · Low · The cache of the cuts misses each cut when a read needs more than 16 cuts · Confirmed

- **Where.** `Engine.Core/Drawing/SectionCut.cs` lines 105 and 136 to 145; `Engine.Core/Drawing/ViewEdges.cs` lines 175 and 196.
- **What.** One read of a section calls `SectionCut.Cut` for each body in the order of its identifier. A new cut goes first,
  and the last one leaves when the list holds more than 16. With 17 pairs of a section and a body, the second read looks for the
  cut of the first body, which the cut of the seventeenth body removed. Its new cut then removes the cut of the second body, and
  so on: each of the 17 lookups misses. A sheet with three sections of a part of six bodies needs 18 cuts. **[Observed]** by
  reading; no run.
- **Probe.** A test as text, in the form of `SheetSectionTests.A_Read_Keeps_The_Cut_Until_A_Body_Changes`: make a plate and 16
  boxes that the plane of the section cuts, one sheet with the Top view and one section of it; read the sheet two times; assert that
  `SectionCut.CutsOf(section)` after the second read is the count after the first read. The assert fails with 34 against 17.
- **Impact.** For such a sheet the fix of E222 gives no gain, and each read after each command cuts again under the serial lock.
  The static cache also keeps up to 16 input solids, 16 cut solids and their B-Reps for the life of the process, after their
  document closes. **[Inferred]**
- **Correction.** Keep one cut for each pair of a view and a body, the last one, and drop the pairs that the last read did not
  use; or give the limit from the count of cuts of the last read. Add the probe as a test. **[Recommended]**

The rest of the cache holds. The key compares each polygon, its plane, its tag and its exact vertices, the point, the frame and
the view (`SectionCut.cs` lines 174 to 186). `Rational` is in its lowest terms, so equal values give equal hashes, and a hash
that differs can only miss, not give a wrong cut. `KernelSolid` holds its polygons only, so the key holds each input of `Apply`.
The gate guards each read and write of the list; two equal misses at one time make two equal entries, which costs one place.
`Brep` and `KernelSolid` expose read-only lists, so two documents can share a kept cut. **[Observed]**

**What stays of E222.** The first read after each change of a body still subtracts a box of the kernel and builds its B-Rep under
the serial lock of the session (11.7 s and 10.9 s for a torus of 4,096 polygons in the last review). A cut of each polygon by the
plane, with one cap from the cut segments, needs the kernel. The view without a section (`Brep.Of`, `ViewEdges.cs` line 206)
has no cache. **[Observed]**

#### T183 · Medium · The concurrency test of the HTTP host decides its result with a time limit · Confirmed

- **Where.** `Engine.Tests/Http/HttpConcurrencyTests.cs` line 19 (`OperationLimit`, 10 s) and lines 200 to 204
  (`Subscriptions_And_Commands_In_Parallel_For_One_Second_Do_Not_Deadlock`, 11 s).
- **What.** The test fails when the work does not end in 11 s, and the message says "Two threads wait for each other". The failure
  of main run 44814556105 (job 136559399617, windows-latest, `8dbaafb`) says "73 commands and 48 subscriptions completed", so the
  work made progress; a slow runner and a deadlock give the same result. Each of the six loops starts a new operation until 1 s,
  and each operation has its own limit of 10 s, so an operation that starts at 0.99 s can end after the limit of the whole work
  with no fault. The ledger names a failure of this class on Windows at v0.355, v0.376 and v0.400; v0.405 is the fourth. The
  other tests of the class use the same `OperationLimit` for each request, and two of those failures were a cancel at that
  limit. **[Observed]** by reading and from the annotations of the API.
- **Impact.** A run of `main` goes red with no fault of the code, and the owner must start a re-run. ADR-0173 item 4 removed a
  deciding limit for the same reason and rejected a larger limit: "a slower runner can pass any limit". **[Inferred]**
- **Correction.** Decide a deadlock by the absence of progress: count each ended operation, and fail only when the count does not
  change for 60 s. A deadlock stops the count at once, so the injection of the opposite order of the locks still fails the test.
  Give the requests of the class no cancel at 10 s, or a limit that only stops a test that waits for ever, as ADR-0173 item 4
  does. Record the rule in ADR-0173. **[Recommended]**

#### T184 · Low · A crash of the test host gives no cause · Confirmed

- **Where.** `.github/workflows/ci.yml` lines 124 to 126.
- **What.** The step "Test" has `--blame-hang-timeout 2m --blame-hang-dump-type none` and no `--blame-crash`. In attempt 1 of run
  44758589835 (job 136364804280) the test host of `3DEngine.ImageTests` crashed. The annotation names the test that did not end,
  `ResponsiveLayoutTests.Each_Size_Of_A_Resize_From_The_Minimum_Keeps_Each_Control_Free`, and gives the output of other tests, not
  the cause. The entry v0.406 does not name that test. Five local runs and attempt 2 passed. **[Observed]**
- **Impact.** That test makes about 7,000 interfaces of Dear ImGui (496 sizes and 14 states), each with its own context and
  font atlas, and the class runs
  in the collection of Dear ImGui (`3DEngine.ImageTests/DearImGui.cs`). The review found no cause by reading. A second crash
  will give no more data. **[Inferred]**
- **Correction.** Add `--blame-crash --blame-crash-dump-type mini` on Windows, keep the dump as an artifact of a failed job, and
  name the test that did not end in the ledger. **[Recommended]**

#### V210 · Medium · A race of one frame in the host brings back V206 · Confirmed

- **Where.** `3DEngine.App/NativeThreeDEngine.cs` lines 905, 1087 and 1092 to 1094; `3DEngine.App/Ui/DesktopUi.SheetPage.cs`
  lines 118 to 129; `SheetReadCache.Read` (`3DEngine.App/SheetPage.cs` lines 202 to 223).
- **What.** The frame reads the count of the changes at line 905. A command of the queue can end on the thread pool after that
  line: its events raise the count, and `Poll` at line 1087 then ends the work, so `Busy` is false. `Read` with the old count
  finds its key asked and its read done, and gives the old sheet with "not pending". `ShowPage` then clears the sent values. In
  the next frame the new count starts a new read, and the page shows the old place of the view until that read ends.
  **[Observed]** by reading.
- **Probe.** A test as text, in the form of `SheetPageTests`: release a drag of the Top view, so the page sends one move. Call
  `ui.ShowPage(sheet, true)` and a frame, then `ui.ShowPage(sheet, false)` and a frame, with the same old `sheet`, as the host
  does in the frame of the race. Assert that the Top view shows at the sent place. The assert fails.
- **Impact.** The view jumps back for the time of a read, which can be seconds (R-0069), and a second switch in that time sends
  the first flag back. That is the fault of V206. **[Inferred]**
- **Correction.** Read the count again after `Poll` and give that count to `_sheetRead.Read`, or give a pending read in a frame
  in which `Poll` ended a work. **[Recommended]**

#### V211 · Medium · The file menu can open behind the structure view · Confirmed

- **Where.** `3DEngine.App/Ui/DesktopUi.Part.cs` lines 135 to 138 and 172; `3DEngine.App/Ui/DesktopUi.Fixed.cs` lines 19 to 21;
  `3DEngine.App/Ui/DesktopUi.Panel.cs` lines 240 to 241.
- **What.** The menu has the flags of a fixed control less the ground, so it has `NoFocusOnAppearing`. The structure view has no
  `NoBringToFrontOnFocus`. Dear ImGui keeps the order of a window that shows again, and a click on a window brings it to the
  front. After one tap on the structure view, the menu opens behind it. The menu goes under the folded view by design: the
  comment of `FileMenuFlags` says that its entries drew over the rows of the open structure view. **[Observed]** by reading;
  the order of the windows is **[Inferred]** from the rules of Dear ImGui, with no run.
- **Probe.** A test as text, in the form of `PartRingTests`, at the size of the phone: tap the file name two times (the menu
  shows and closes), tap the first row of the structure view two times, tap the file name, then tap "Open project...". Assert
  `[new OpenProjectAction()]`. The tap goes to the structure view.
- **Impact.** On a phone the menu is the only way to "Open project..." and "Save as...". **[Inferred]**
- **Correction.** Call `ImGui.SetNextWindowFocus()` before `Begin` of the menu while it is open, or give the structure view and
  the panels `NoBringToFrontOnFocus`. Add the probe to `PartRingTests`. **[Recommended]**

#### V212 · Medium · A pad of a sketch on a phone measures its room at one top and opens at another · Confirmed

- **Where.** `3DEngine.App/Ui/DesktopUi.Pad.cs` lines 251 to 260 (`PlacePad`) and lines 84 to 100 (`PadWindow`).
- **What.** `PlacePad` decides "compact" and "low" from the room under `PanelTop`. `PadWindow` then puts a pad of a sketch under
  the sketch window (`top = sketch.W + 8f`) when it has no room beside it, and a landscape pad under the command line
  (`CommandBottom`). The decision does not see the new top. While a pad is open the ring does not show, so the sketch window
  shows each tool and is tall. **[Observed]** by reading.
- **Probe.** A test as text, in the form of `ResponsiveLayoutTests`: a state with a sketch panel (tool Line) and
  `ui.OpenNextPointPad(false)` in frame 1, at 390 by 844 and 412 by 915. Assert that the rectangle of `PadItem("Apply")` is inside
  the rectangle of the pad window. Neither the layout test nor `Each_Field_And_Key_Of_The_Pad_Shows_With_No_Scroll` opens a pad
  of a sketch.
- **Impact.** The keys and Apply scroll out of view, against ADR-0172 item 2 ("a pad with no room is compact"). **[Inferred]**
- **Correction.** Give `PlacePad` the top that `PadWindow` uses, and decide both states from it. **[Recommended]**

#### V213 · Low · A tap outside the file menu also acts on the control under it · Confirmed

- **Where.** `3DEngine.App/Ui/DesktopUi.Part.cs` lines 163 to 167; `3DEngine.App/Ui/DesktopUi.Ring.cs` line 482.
- **What.** ADR-0174 item 4 says: "A tap outside the menu closes it, and that tap gives no other action". Only the ring reads
  `_menuTookRelease`, and the scene reads the windows. Undo, Redo, "Views", the path and the structure view draw before the menu,
  and their buttons act on the same release. **[Observed]** by reading.
- **Probe.** Open the menu, tap `UndoLabel`, and assert that `ui.TakeActions()` is empty. It gives `UndoAction`.
- **Correction.** Keep "the menu is open" from the last frame, and skip the release in each control while it is true; or make
  the menu a popup of Dear ImGui. The decision and the code disagree, so the owner says which one is correct. **[Recommended]**

#### V214 · Low · The rows can stay narrow for a ring that is no longer there · Confirmed

- **Where.** `3DEngine.App/Ui/DesktopUi.Ring.cs` lines 345 and 444; `3DEngine.App/Ui/DesktopUi.Rows.cs` lines 222 and 237.
- **What.** `_ringBoxes` is set only after a ring is laid out. The early return at line 345 (no centre from the host, or an open
  pad) keeps the boxes of the last ring, and the rows keep away from them. **[Observed]** by reading.
- **Probe.** At 640 by 560, three frames with a selection and a centre, then three frames with `ringCentre: null`; assert that
  the width of the rows comes back to its full width. By the trace, it does not.
- **Correction.** Set `_ringBoxes = []` at the start of `Ring`. **[Recommended]**

#### V215 · Low · A drag that a read ended can take the next tap · Confirmed

- **Where.** `3DEngine.App/Ui/DesktopUi.SheetPage.cs` lines 241 to 244, 434 to 438 and 467 to 473.
- **What.** When the sheet of the page changes during a drag, the page shows "Reading the sheet" for a frame and draws no paper.
  Dear ImGui then ends the active item with no deactivation of the paper. When the new read comes, line 435 sets
  `_pageDragLost`. The next press sets a new `_pageDrag` at line 438 and keeps the flag, so its release gives nothing.
  **[Observed]** by reading; the end of the active item is **[Inferred]** from the rules of Dear ImGui.
- **Correction.** Clear `_pageDragLost` when a new drag starts at line 438, and clear the drag in the branch of line 244.
  **[Recommended]**

#### V216 · Low · The message of an export can name a number in place of the file · Confirmed

- **Where.** `3DEngine.App/SheetExports.cs` lines 99 to 106.
- **What.** `FileName("content://com.android.providers.downloads.documents/document/msf%3A1000018")` gives "1000018", so the
  message is "Exported Sheet 1 as PDF to 1000018". The Downloads folder gives such a URI. **[Observed]** by reading; no run on
  the phone.
- **Correction.** Name no file when the decoded identifier has no "/" and no extension, or read the display name on the side of
  Android. **[Recommended]**

#### V217 · Low · The message of "Put on sheet" can show the proposed value · Confirmed

- **Where.** `3DEngine.App/SheetActions.cs` line 167; `3DEngine.App/Ui/DesktopUi.Values.cs` line 78;
  `3DEngine.App/Ui/DesktopUi.Pad.cs` line 235.
- **What.** `SheetValue` takes the value from the chips of the frame. While a proposal changes that value, the text of its chip
  is the pending text ("10.000 → 12.000 mm"), so the message says "Put 10.000 → 12.000 mm on the front view". The sheet gets the
  value of the design. **[Observed]** by reading.
- **Correction.** Build the message from the number of the value and the unit, not from the text of the chip. **[Recommended]**

#### V218 · Low · ADR-0177 item 1 says that "Show all" is one step of undo · Confirmed

- **Where.** `docs/adr/0177-show-hidden-on-the-ring-of-the-part-and-the-path-line.md`, item 1; `3DEngine.App/BodyActions.cs`
  lines 163 to 168.
- **What.** Item 1 says that "Show hidden" sends "Show all", "one step of undo, as the button of ADR-0124 item 3". `ShowAll`
  sends one `SetBodyVisibility` for each hidden body, and ADR-0124 item 3 says "the same commands as the button of the panel".
  Item 3 of ADR-0177 forbids an engine change, and the engine has no group of commands. **[Observed]**
- **Impact.** TASK-0424 (R20e) is Ready and reads item 1. Its implementer must break item 1 or item 3. **[Inferred]**
- **Correction.** The decision and the code disagree, so the owner says which one is correct (question Q1). **[Recommended]**

#### V219 · Low · ADR-0177 item 2 does not say what gives way on the path line of a phone · Confirmed

- **Where.** ADR-0177 item 2; `3DEngine.App/Ui/DesktopUi.Fixed.cs` lines 255 to 270 (`PathLine`) and 309 to 312 (`HiddenText`).
- **What.** At 390 pixels the path line has the width of the display less the margins and the button "Views" (line 265). The
  part "1 hidden · Show" at the font of 26 pixels needs most of it, before the file name and its chevron of 20 pixels, which
  is the target of the file menu (ADR-0174 item 4). Item 2 does not say which one is cut, if the part shows while a body is
  selected (then the file name is not the last segment), or where the list of ADR-0124 item 3 opens, which that item places
  above the status line. ADR-0124 item 2 still says "the panel keeps Show all", and the `affects` field of ADR-0177 does not
  name `DesktopUi.Part.cs`, where `PartRingLabels` must change. **[Observed]** by reading; the widths are **[Inferred]**.
- **Correction.** The owner answers question Q2, and the first commit of TASK-0424 records it in ADR-0177. **[Recommended]**

#### V220 · Low · Two texts of the history arc have no ground and are no cover · Confirmed

- **Where.** `3DEngine.App/Ui/DesktopUi.History.cs` lines 170 to 183 (the pending node) and 186 to 199 (the caption);
  `3DEngine.App/Ui/DesktopUi.Overlay.cs` line 201.
- **What.** ADR-0125 item 4, as R20d amends it, says: "Each text of the history arc has the ground of the scene behind it". The
  amber text of a pending step ("C1 to 75 ?", line 181) and the caption "HISTORY" have no ground. The covers take the items whose
  key starts with "History"; the pending step gives the box of its node only (line 182), and the caption gives no item, so a
  plate of a dimension can lie on either text. **[Observed]** by reading.
- **Probe.** A test as text, in the form of `DimensionLabelsTests`: a selected body with history steps and a proposal card
  with `SetExtrudeDistance` for its extrude; read the draw data of the background list; assert a rectangle of the ground under
  the rectangle of the pending text. No such rectangle is there.
- **Correction.** Give the pending text and the caption the ground, and add their rectangles to `_items` with a key that
  starts with "History". **[Recommended]**

The rest of R20d holds. The arc goes from 3π/4 to π/2, so each of the 12 chords spans 3.75 degrees, and the arc lies less than
0.1 pixel from its chord; the half width of 5 pixels covers it. The ground and the texts go through `GetColorU32`, so they fade
with the arc during a gesture. Each step text has its ground before its words, and its width is the width of the words and the
state of a failed step together. **[Observed]**

#### V221 · Low · The compact first row of the structure view is not cut to its width · Confirmed

- **Where.** `3DEngine.App/Ui/DesktopUi.Panel.cs` lines 102 to 118.
- **What.** The text of the compact first row, and the second line of the first row "For touch", do not go through `Fit`. Near
  the narrowest structure view (about 316 pixels at 560 by 560) a text such as "Assembly  mm · 12 bodies · 12 failed" runs under
  the mark and is cut at the edge of the window. **[Observed]** by reading; the widths are **[Inferred]**.
- **Correction.** Fit the state part to the width less the mark. **[Recommended]**

The agents found no fault in these areas:

- the place of "Put on sheet": `SheetValuePlace.Paper` is the place of `SheetLayout` and the inverse of
  `SheetPage.DesignPoint`, 10 mm along the normal away from the centre of the view, with + − × ÷ and sqrt only;
- the choice of the view of the same frame with no section, and the source of a distance;
- the queue, which only the frame thread uses, and a refused command, which clears the sent values at the next read;
- the identifiers of Dear ImGui of the new windows;
- the setting "Compact" or "For touch", which ADR-0175 item 2 keeps for the session only;
- the hit tests of the rows of 30 pixels, and the anchors under the structure view;
- the ring of the part, whose actions need no selection;
- the images as a fixed part, and the low pad, which hides the anchors as ADR-0172 item 2 says.

The session found no fault in `eng/lost-runner.js`. A failed step, a failed test, another workflow and a second attempt get no
re-run. The workflow checks out the default branch with no credentials. **[Observed]**

**ADR-0177 against ADR-0124 and ADR-0174.** The reading of the answer is coherent with both records except for V218 and V219.
"Show hidden" shows only with no selection, which agrees with ADR-0124 item 1 (after a hide the selection goes to a neighbour,
so the ring of the part shows only when no body stays visible or the person ends the selection) and with the conditions of
ADR-0174 item 1. One ring of five tools fits the rules of ADR-0172. The record says that the owner can correct the reading, and
the answer named "a tap on the path line that opens a ring", which the record does not give. **[Observed]**

## 4 The next planned task

No new finding is critical or high, so the next product phase can follow this review. TASK-0424 (R20e, "Show hidden" on the
ring of the part) is the only task with the status Ready, and V218 and V219 are questions on its record. The owner names R30f
as the next planned task of this review: "The proposal of the agent on the sheet page: amber boxes and amber values, Accept and
Reject on the arc (ADR-0162 item 8)". Roadmap row R30f has no task file yet. By the rule of `docs/templates.md`, section 7, a
task after the next product phase holds the four medium and the eleven low findings. E222 stays open until the kernel cuts with
a plane. **[Observed]**

## 5 Research for that task

- **A proposal can hold each sheet command.** The post of a proposal finds the handler of each command in the registry of the
  host (`Engine.Api.Http/Endpoints/ProposalsEndpoint.cs` line 152), and `Engine.Api.Http/Proposals.cs` has no list of the
  permitted kinds. **[Observed]**
- **The window knows a value of two commands only.** `ProposedValues.Of` (`3DEngine.App/ProposedValues.cs`) gives a value for
  `SetSketchConstraintValue` and `SetExtrudeDistance` and null for each other command. A sheet command shows only its line on the
  arc. **[Observed]**
- **No preview of a result exists.** ADR-0092 Q2 A chose the text of the commands, and option B, a dry run on a copy of the
  session, was not chosen. ADR-0162 item 8 says "No preview enters the log". So the amber box and the amber value come from the
  parameters of the commands, in the client. **[Observed]**
- **The parameters are enough for a box and a value.** `AddSheetViewCommand` gives the sheet, the nine numbers of the frame, the
  scale and the place of the centre (`Engine.Core/Commands/AddSheetViewCommand.cs` lines 15 to 28), and the identifier of the
  view is the identifier of the command (line 5, and `AddSheetViewCommandHandler.cs` line 95). `ProposalCard.CommandIds` holds that identifier, so a
  `PlaceSheetDimension` of the same proposal on the new view can find its amber box. `PlaceSheetDimensionCommand` gives the sheet,
  the view, the source, its kind and the place (lines 15 to 21). **[Observed]**
- **The box of a new view exists in the window.** "Add to sheet" places a view from the box of the drawn bodies through the
  frame and the scale (`SessionBodies.SheetBox`, `SheetPlacement.Drawn`, V197). The same box gives the amber box of a proposed
  view with no query. The lines of the view need GetViewLines, which takes the nine numbers, off the frame thread. **[Observed]**
- **The page hides the arc and takes its taps.** `PageWindow` (`3DEngine.App/Ui/DesktopUi.SheetPage.cs` lines 195 to 217) fills
  the display on the background list and is a window over the scene. The arc of a proposal draws later on the background list
  (`DesktopUi.cs` lines 644 and 735), under the window of the page and its paper. Accept and Reject act only when
  `OverWindowAtRelease` is false (`DesktopUi.Proposal.cs` line 480), and it is true over the page. On a portrait display the
  proposal is a window (`ProposalSheet`, `DesktopUi.Proposal.cs` line 713), which can draw over the page, because the page has
  `NoBringToFrontOnFocus`. **[Observed]** by reading; the portrait case is **[Inferred]**, with no run.
- **The amber.** `DimensionColour` (`DesktopUi.Overlay.cs` line 136) is the amber of a value and of a pending step. **[Observed]**
- **A proposal that makes a sheet.** `CreateSheet` makes a sheet that the page cannot show before the accept. The page shows one
  live sheet. **[Inferred]**

## 6 Recommended approach

1. The first commit records the answers to Q3, Q4 and Q5 in a new ADR that amends ADR-0162 item 8.
2. A pure function of the client gives the amber items of a proposal for one sheet: for each `AddSheetView` of that sheet, a box
   from `SheetPlacement` at its place and scale; for each `PlaceSheetDimension`, a value at its place, with the number of the
   design, or the proposed number when the same proposal changes it (`ProposedValues`). A test of the function needs no window.
3. The page draws the amber items over the paper, with the dash of a proposal, and takes no tap on them.
4. On a landscape display the arc of the proposal draws on the list of the page, in the free part right of the paper, and the
   page skips a release on the targets of the arc. A test in the form of `ProposalArcTests` taps Accept and Reject with the page
   open at the sizes of ADR-0172.
5. On a portrait display a test shows that the proposal window is over the page and takes its taps.
6. No contract change and no engine change: the amber items are presentation (ADR-0007), as ADR-0162 item 8 says. **[Recommended]**

## Questions for the owner

1. **Q1 (V218).** "Show hidden" on the ring of the part: one step of undo, or one step for each hidden body? **A (recommended):**
   one command for each hidden body, as the button "Show all" does; ADR-0177 item 1 says so, and no engine change follows.
   **B:** one step of undo; a new command or a group of commands, with a contract change and its ADR.
2. **Q2 (V219).** On the path line of a phone, the file name and "N hidden · Show" do not both fit. What gives way? **A
   (recommended):** the part shortens to "N hidden", and the file name is cut with its chevron kept; a tap on the part opens the
   list under the path line. **B:** the part goes on a second line under the path line. **C:** the phone shows the count on the
   segment of the ring only ("Show hidden (N)").
3. **Q3 (R30f).** Which commands of a proposal does the sheet page show in amber? **A (recommended):** `AddSheetView` as a box and
   `PlaceSheetDimension` as a value, as ADR-0162 item 8 says; each other command shows its line on the arc only. **B:** also
   `MoveSheetViews`, `ScaleSheetView` and `AddSheetSection`, as a box at the new place or size.
4. **Q4 (R30f).** Where are Accept and Reject while the sheet page shows? **A (recommended):** on the arc of ADR-0092, in the free
   part right of the paper, over the page. **B:** in the header of the page. **C:** a proposal closes the page and shows the
   scene.
5. **Q5 (R30f).** What is the amber box of a proposed view? **A (recommended):** the box of the drawn bodies through the frame
   and the scale, as "Add to sheet" uses, with no query. **B:** the lines of the view from GetViewLines, in amber, read off the
   frame thread.
6. **Q6 (T183).** How does the concurrency test find a deadlock? **A (recommended):** by no progress for 60 s; the time of the
   work is not a result. **B:** the test sets no limit, and the hang limit of 2 minutes of the step "Test" stops a deadlock.
