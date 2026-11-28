---
date: 2026-11-28
commit: 1a11223912ee77d06a021a215625f6522eeae9ea
ledger: v0.458
previous: 2026-11-25-codebase-review.md
---

# Codebase review — 2026-11-28

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

The review examines `1a112239`, the merge of P11.0 (TASK-0474) at v0.458. Four product milestones (R32b, R33, P21 and P11.0)
and seven entries follow v0.451, the review of 2026-11-25, which examined v0.450. So the next product milestone needs this review
first. The review of 2026-11-25 examined `1975089e`, a commit from before the rewrite of the history of 2026-11-26; the map
`docs/history-map-2026-11-26.txt` gives its new identifier `7efe89ce`. The range is `7efe89ce..1a112239`: 33 commits (25 of
the tasks and 8 merges) and 123 files. The review runs under the standing order of the owner of 2026-11-28 ("same as every
previous week").

Two agents read parts of the range and changed no file in the repository. One read the engine: the placements of P11.0, the
reversal of a failed group of P0.191, and the queries of P21. One read the window, the tests and the gates: R32b, R33, the
corrections of P0.191 in the window, and the tests of P11.0. The session read the measures, the ledger entries v0.452 to v0.458,
the register and the runs of the pipeline. It read each finding of the agents in the code at its line. Four findings have a
probe that ran as a test in a scratch worktree of `c885ea90` (the tip of P11.0 before its close, the same code as the merge):
E246, E248, E249 and V240. The other findings rest on a reading of the code at their lines. No file of the probes is in the
repository.

## Summary

- **P0.189 and P0.191 fixed ten findings of the last review and P0.190 fixed V238.** E242, T197, E244, E245, V237, T198, T199,
  T200, T201 and T202 are fixed; V236 is fixed for two of its three pipelines, and R-0077 holds the third. E243 is open, and
  register entry R-0082 holds it on the choice of the owner of 2026-11-27. **[Observed]**
- **P11.0 gives a body a placement, and the measure of K10a shows the gain.** 150 turns with `PlaceBody` give a numerator of 19
  digits (2576 with `Rotate`). Each reader of the engine applies the placement, and `PlaceBodyTests` compares each reader with
  the same scene moved by `Rotate` and `Translate`. **[Observed]**
- **One new finding is high.** E246: the design mass of a cut gives a wrong centre when a body of the chain is a `Subtract` by a
  tool with no curve and has its own `PlaceBody`: the probe gives x = 203.0 mm where the same scene with a `Translate` gives
  99.6 mm. The volume is right. **[Observed]** by the probe.
- **Two new findings are medium.** V239: the window keeps the faces and the edges of the selected body by its key only, so after
  a `PlaceBody`, or an `Undo` of one, it draws the selected face and the values of its edges at the old place. E247: ADR-0037
  item 8 still tells a client to read a mesh again only when the key changes. **[Observed]**
- **Seven new findings are low.** E248: the JSON of a placement has an eighth member `isIdentity`. E249: two large quaternions
  that `PlaceBody` accepts make a boolean fail with a false code of the backend. E250: when a handler throws in a group and the
  reversal does not apply, the record says that nothing applied. V240: a creation and a `PlaceBody` in one frame give two
  meshes. T203: a test of a failed start passes with no check when the validation layer is absent. T204: two tests of
  P11.0 compare less than their comments say. T205: the window test of the section view checks the key, not the cut.
  **[Observed]**
- **The change of `Engine.Contracts` has its ADR in the same commit.** `BodyPlacement` and `BodyRecord.Placement` came with
  ADR-0191 in `c885ea90`, and the new meaning of `GroupId` on an `Undo` with ADR-0190 in `da6d32d5`. The log gets the new
  command `PlaceBody` with seven doubles and no angle; no new transcendental function came. **[Observed]**
- **The pipeline since v0.451 has one red run on `main`, which the ledger names.** Run 45901171956 of `main` at `a715d982`, the
  first run after the rewrite of the history, is red in the job "Write set of the governing task" only, as v0.455 says. No
  branch run failed. **[Observed]** from the ledger entries v0.452 to v0.458, which each read the API.
- **Open findings: 0 critical, 2 high, 8 medium and 25 low.** **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source line is a line of a
`.cs` or `.razor` file that git tracks. The script `measures.js` of the last review reads each file through `git show` at one
commit; at `7efe89ce` it gives each value of the review of 2026-11-25 except the lines of the files that the rewrite removed.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 18 | `.csproj` lines in `3DEngine.sln`. The last review gave 18; `3DEngine.Android` is outside the solution. |
| Production source lines | 58,578 (419 files) | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests`, `3DEngine.ImageTests` and `3DEngine.VisionTests`. The last review gave 57,564 in 411 files. |
| Test source lines | 71,188: Engine.Tests (42,469 in 176 files), 3DEngine.Tests (4,245 in 25 files), 3DEngine.ImageTests (24,408 in 109 files) and 3DEngine.VisionTests (66 in 1 file) | The four test projects. The last review gave 69,834. |
| Tests | 3,371 | `dotnet test` of the solution at `c885ea90` in the worktree of P11.0: 1,981, 391, 996 and 3 passed, 0 failed, 0 skipped. `eng/test-floor.txt` gives the same counts. The last review gave 3,330. |
| Gate classes | 21 | Tracked files whose name ends in `GateTests.cs`; none is new. `eng/repo-gates` gives 526 runs of 173 checks, 0 failed. The last review gave 21, and 526 runs of 173 checks. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`: 0 warnings, 0 errors. |
| Open register entries | 13 | Section "Open" of `docs/register.md`; the limit is 15. The last review gave 13; R-0079, R-0080 and R-0082 opened, R-0075, R-0076 and R-0078 closed, and R-0081 opened and closed. |
| ADRs | 183 | `docs/adr/0001` to `0061` and `0070` to `0191`, with 0003 and 0011 in `docs/adr/archive/`. In force: 181 (66 accepted, 115 amended). None is proposed. The last review gave 180. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 2 | The same method: V141, E246. The last review gave 1. |
| Findings: medium | 8 | The same method: E6, E13, E19, E21, T32, T37, V239, E247. The last review gave 8; E242 and T197 are fixed. |
| Findings: low | 25 | The same method: E17, E24, E34, E39, E57, E84, E160, V15, V29, V84, V88, T143, C1, P1, V143, V144, E243, V236, E248, E249, E250, V240, T203, T204, T205. The last review gave 27; nine are fixed, and V236 is fixed in part. |
| Lines in the rule files | 4,389 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The last review gave 4,249. |
| Sentences with "must" | 63 | `grep -o -w -i must` on the same ten files. The last review gave 62. |
| Sentences with "do not" | 68 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 68. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe found. E13, T32,
E84, E160, V84 and V141 are fixed in part and count as open, and V236 too. A finding that the owner accepted, or that an ADR
closes by a decision, counts as closed.

## Findings of the previous review

The lines up to V235 are the lines of the review of 2026-11-25, with a new state for E6, E19, E21, E24, E57 and V15, whose
files changed in this range; each of the six stays open. No other file that an open finding of those lines names changed in
this range. The files of eight fixed findings changed too: E200, E239 and E241 (the placements of P11.0 in the kernel), T192
and T195 (the tests of P0.191), V229, V231 and V233 (the key of a mesh of P11.0). The test of each passes at `c885ea90`, so
each fix holds. The lines of E242 to T202, the findings of the review of 2026-11-25, follow them.

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Fixed. TASK-0321, v0.304. A whole JSON number stays a long, so an integer field gets the exact value above 2^53.
- **E6** — Open in four parts of five. The owner answered Q2 B on 2026-11-10 (TASK-0331): part (d), the five planned event kinds,
  stays by decision. `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` changed only for the field `placement` of
  `body.created` and `body.changed` (P11.0, TASK-0474). Register entry R-0027. **[Observed]**
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
- **E19** — Open, and smaller. `Engine.Core/DocumentSession.cs` changed only for the reversal of ADR-0190 and the index of a command
  that throws (P0.191, TASK-0473); `Engine.Core/CommandBus.cs` changed for `Reverse`, the refusal of an `Undo` with a group from
  a client, and the member `placement` of the events (P0.191, P11.0). Register entry R-0033. **[Observed]**
- **E20** — Fixed, and it holds. No change.
- **E21** — Open. The session reverses a failed group with one `Undo` that has the group (`Engine.Core/DocumentSession.cs` lines 198
  to 209, ADR-0190), so a group needs no rollback. A handler that throws after it changed the backend still leaves its solid,
  and ADR-0188 item 5 says that the bus has no rollback. E250 is a new form at the same lines. Register entry R-0033.
  **[Observed]**
- **E22** — Fixed. TASK-0321, v0.304. Only a cancellation of the caller is Cancelled, and a cancelled result is not cached.
- **E23** — Fixed. TASK-0051, v0.48. No change.
- **E24** — Open in one part of nine, and one part accepted. `Engine.Core/CommandBus.cs` changed for the mark of a reversal (ADR-0190)
  and for the member `placement` of the events (ADR-0191); part 2 (one bus for each Document) did not change. TASK-0044
  accepted part 8. Register entry R-0033. **[Observed]**
- **E25** — Accepted by the owner. TASK-0142, v0.144. No change.
- **E26** — Fixed. TASK-0135, v0.136. No change.
- **E27** — Accepted by the owner. TASK-0142, v0.144. No change.
- **E28** — Fixed. TASK-0136, v0.137. No change.
- **E29** — Fixed for the replay. TASK-0135, v0.136. No change.
- **E30** — Fixed. TASK-0136, v0.137. No change.
- **E31** — Fixed for the replay. TASK-0137, v0.138. No change.
- **E32** — Fixed. TASK-0135, v0.136. No change.
- **E33** — Fixed with E18. TASK-0140, v0.141. No change.
- **E34** — Open. `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs` changed for `Rotate` (K8) and for `Union` and `Intersect` (K16, TASK-0433); `/schema/backend` did not change. Register entry R-0036.
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
- **E57** — Open. `Engine.Tests/Http/DesktopSurfaceTests.cs` changed only for a guard of 60 s (T201, P0.189, TASK-0468); its comment on
  TASK-0054 and ADR-0019 did not change. Register entry R-0039.
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
- **V15** — Open. `3DEngine.App/SessionBodies.cs` changed only for the placement of a body (P11.0, TASK-0474): another placement takes
  the mesh again. The wait for an idle device at each consumed body and the memory of `GpuBuffer` did not change. V240 is a
  new form of the same merge of V55.
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
- **T4** — Fixed. TASK-0451 (P0.181), v0.435. The dispatch gate reads each host of the classes of `DependencyDirectionGateTests`
  (`HostProjects`, `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs` line 24), the desktop host `3DEngine` and the phone host
  `3DEngine.Android` too. The two injections of a command name in those hosts fail the gate (TASK-0451, rows T4-disp-a and
  T4-disp-c). Test `The_Gate_Reads_Each_Host_Of_The_Dependency_Gate`. R-0031 is closed. **[Observed]**
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
- **V196** — Fixed. TASK-0410, v0.399. "Add to sheet" and a new sheet from the pad go to the queue (`NativeThreeDEngine.cs` lines 579 to 585), and an event of a sheet does not start a new read of the lines. Tests `SheetAddTests.The_Frame_Sends_The_Commands_Of_A_Sheet_To_The_Queue_With_The_Box_Of_The_Drawn_Bodies` (it reads the text of the source file) and `FlatViewLinesTests.An_Event_Of_A_Sheet_Does_Not_Start_A_New_Read`. **[Observed]**
- **V197** — Fixed. TASK-0410, v0.399. `SessionBodies.SheetBox` and `SheetPlacement.Drawn` give the box of each live body that is not a mesh. Test `SheetAddTests.The_Box_Of_A_New_View_Is_The_Box_Of_The_Bodies_That_The_Sheet_Draws`. No test runs `SheetBox` with a session. **[Observed]**
- **V198** — Fixed. TASK-0410, v0.399. `GizmoLayout` gives null while the paper shows (`NativeThreeDEngine.cs` line 425). Test `FlatViewLinesTests.The_Gizmo_Does_Not_Show_While_The_Paper_Shows`. **[Observed]**
- **V199** — Fixed. TASK-0408, v0.393, ADR-0168 item 6. The router holds the keys while the pad of a sheet or the sheet page
  shows (`DesktopUi.cs` line 478), so the Escape that closes the pad reaches no intent. Test
  `SheetPageTests.The_Page_And_The_Pad_Hold_Their_Keys`. **[Observed]**
- **V200** — Fixed. TASK-0410, v0.399. The comment of `SheetPlacement.cs` (line 127) and ADR-0167 item 3 give the order of the code. Test `SheetAddTests.A_View_Goes_Next_To_Each_View_With_A_Shared_Axis_In_Their_Order`. **[Observed]**
- **V201** — Fixed. TASK-0410, v0.399. `ViewLines.Notes` gives the text of each skipped body, and `DesktopUi.Views.cs` lines 331 to 333 draw it. Test `FlatViewLinesTests.The_Paper_Has_The_Centre_Lines_And_Names_Each_Skipped_Body`. **[Observed]**
- **E221** — Fixed. TASK-0416, v0.405. `SheetLayout` skips a cut line whose place on the paper is not finite (`Engine.Core/Drawing/SheetLayout.cs` line 174). Test `SheetSectionTests.A_Cut_Line_At_A_Place_That_Is_Not_Finite_Is_Not_Given`. **[Observed]**
- **E222** — Fixed. TASK-0445 (P0.178), v0.429, and TASK-0446 (P0.179), v0.430. `PlanarBoolean.Cut` cuts each polygon by the plane in the owned kernel with one cap (ADR-0057 item 10), and `SectionCut` uses it; `ViewBreps` keeps one B-Rep for each body of a document. The first read of a torus of radii 30 and 10 with a section went from 33.2 s to 1.73 s (Release, entry v0.429), and the sphere cut splits 10,220 times in place of 6,132,711. Tests `SectionCutTests` and `PlaneCutTests`, which hold the counts. **[Observed]**
- **E223** — Fixed. TASK-0416, v0.405, Q1 A of the owner. `AxisSide` takes an edge in one plane with the axis (`ViewEdges.cs` line 952). Test `SheetSectionTests.A_Section_Through_The_Axis_Of_A_Cone_Shows_The_Axis_In_Each_Look`. **[Observed]**
- **E224** — Fixed. TASK-0416, v0.405. The SVG and the PDF file write the size of the font Size / 0.718 (`Engine.Core/Export/SvgExport.cs` lines 32 and 106), and the letter is half its height under its place (`SheetLayout.cs` line 196). Test `SheetSectionTests.The_Size_Of_A_Text_Is_The_Height_Of_A_Capital_In_The_Three_Files`. **[Observed]**
- **E225** — Fixed. TASK-0416, v0.405. MoveSheetViews refuses a move to the place of the view (`Engine.Core/Commands/MoveSheetViewsCommandHandler.cs` line 67), and the page sends no move for a drag that ends at that place (`DesktopUi.SheetPage.cs` line 532). Test `SheetMoveTests.A_Move_To_The_Place_Of_The_View_Is_Refused`. **[Observed]**
- **V202** — Fixed. TASK-0415, v0.398. `SheetPage.Drag` gives null for a view that is not in the read (`3DEngine.App/SheetPage.cs` lines 124 to 125), a read without the dragged view ends the drag (`DesktopUi.SheetPage.cs` lines 489 to 490), and its release gives nothing. Test `SheetPageTests.A_Read_Without_The_Dragged_View_Ends_The_Drag`. A drag that a read ended can take the next tap (V215). **[Observed]**
- **V203** — Fixed. TASK-0416, v0.405. The router reads once, before the watchers, if a pad holds the keys (`DesktopUi.cs` lines 491 to 494), and the page takes no key then (`DesktopUi.SheetPage.cs` line 216). Test `SheetPageTests.The_Escape_Of_A_Pad_Does_Not_Reach_The_Page`. **[Observed]**
- **V204** — Fixed. TASK-0416, v0.405. A read without the view clears the section and the selection, and a change of the sheet clears the section and the row of the formats. Test `SheetSectionPageTests.A_Section_Ends_When_Its_View_Or_Its_Sheet_Goes`. **[Observed]**
- **V205** — Fixed. TASK-0416, v0.405. "Export" ends a section and "Section" closes the formats. Test `SheetSectionPageTests.The_Formats_And_A_Section_Are_Not_Open_Together`. **[Observed]**
- **V206** — Fixed for its forms. TASK-0416, v0.405. The page keeps the values that it sent until a read that is not pending follows a pending read (`DesktopUi.SheetPage.cs` line 62, and `ShowPage` at lines 154 to 165). Test `SheetPageTests.Two_Changes_Of_One_View_Before_The_Read_Keep_The_First`. A race in the host gives the same fault in one frame: V210. **[Observed]**
- **V207** — Fixed. TASK-0416, v0.405. The paper keeps room for the highest row of the header that changes (`PageStateRows`). Test `SheetPageTests.The_Paper_Keeps_Its_Place_While_The_Header_Changes`. **[Observed]**
- **V208** — Fixed. TASK-0416, v0.405. "Section" shows only when `FlatOf` gives a frame. Test `SheetSectionPageTests.Section_Shows_Only_For_A_View_That_The_Engine_Can_Cut`. **[Observed]**
- **V209** — Fixed for its form. TASK-0416, v0.405. `SheetExports.FileName` decodes the last segment of a content URI (`3DEngine.App/SheetExports.cs` lines 99 to 106). Test `SheetExportTests.The_Text_Of_An_Export_On_The_Phone_Names_The_File`. A URI of the Downloads folder still gives a number: V216. **[Observed]**
- **E226** — Fixed. TASK-0426, v0.410. `SectionCut` keeps one cut for each pair of a view and a body, the cuts of the last 64 views (`ViewLimit`), and `Keep` drops the pairs that the last read of a view did not use (`Engine.Core/Drawing/SectionCut.cs` lines 109 to 183; `ViewEdges.cs` lines 177, 199 and 234). Test `SheetSectionTests.A_Read_Keeps_The_Cut_Of_Each_Body_Also_Past_Sixteen_Bodies`. The cache has no limit of bytes; for each of 64 views it keeps the cut and the B-Rep of each body for the life of the process. **[Observed]**
- **T183** — Fixed for its form. TASK-0426, v0.410, Q8 A. `Progress.EndsWhileItMoves` fails the test only when no operation ends for 60 s, and each request stops after 60 s (`Engine.Tests/Http/HttpConcurrencyTests.cs` lines 19 to 24 and 207 to 211). Test `A_Work_That_Ends_Operations_Is_No_Deadlock_And_A_Work_That_Stops_Is`. A deadlock still fails the test, but the equal limits can give a cancel in place of the text of a deadlock: T185. **[Observed]**
- **T184** — Fixed. TASK-0426, v0.410. On Windows the step "Test" adds `--blame-crash --blame-crash-dump-type mini`, and a failed job keeps each `*.dmp` as an artifact (`.github/workflows/ci.yml` lines 116 to 142). Test `Engine.Tests/Pipeline/TestStepTests.cs`, which reads the step as text. No run has crashed since, so no dump shows it. **[Observed]**
- **V210** — Fixed. TASK-0426, v0.410. `Busy` is true in the frame of the `Poll` that ended a work (`3DEngine.App/SheetPage.cs` line 241), so the host gives a pending read in the frame of the race (`NativeThreeDEngine.cs` line 1112). Test `SheetPageTests.The_Frame_In_Which_A_Command_Ends_Gives_A_Pending_Read`; it tests the queue and the cache, not `ShowPage`. **[Observed]**
- **V211** — Fixed. TASK-0426, v0.410. `SetNextWindowFocus` in the first frame of the menu (`DesktopUi.Part.cs` line 186). Test `PartRingTests.The_File_Menu_Opens_Over_The_Structure_View_After_A_Tap_On_The_View`. **[Observed]**
- **V212** — Fixed from the third frame of a pad. TASK-0426, v0.410. `PlacePad` decides from the top that `PadWindow` uses (`PadPlace`); in its first two frames a pad reads the sketch window of the last frame, as the task records. Test `ResponsiveLayoutTests.Each_Key_Of_A_Pad_Of_A_Sketch_Shows_On_A_Phone`. **[Observed]**
- **V213** — Fixed. TASK-0426, v0.410. `FileMenuTakes` keeps the press and the release of a tap outside the menu from the library (`DesktopUi.Part.cs` line 60). Test `PartRingTests.A_Tap_Outside_The_File_Menu_Gives_No_Other_Action`. **[Observed]**
- **V214** — Fixed. TASK-0426, v0.410. `_ringBoxes = []` comes before each early return (`DesktopUi.Ring.cs` line 338). Test `BodyRowsTests.The_Rows_Take_Their_Full_Width_Again_When_The_Ring_Goes`. **[Observed]**
- **V215** — Fixed. TASK-0426, v0.410. A new press clears the mark of a lost drag. Test `SheetPageTests.A_Tap_After_A_Drag_That_A_Frame_With_No_Paper_Ended_Acts`. **[Observed]**
- **V216** — Fixed. TASK-0426, v0.410 (`SheetExports.cs` line 112). Test `SheetExportTests.The_Text_Of_An_Export_Names_No_File_For_A_Number`. **[Observed]**
- **V217** — Fixed. TASK-0426, v0.410 (`SheetActions.cs` line 173). Test `SheetValueTests.The_Text_Of_Put_On_Sheet_Gives_The_Value_Of_The_Design`. A radius loses its "R" in the text, as the task records. **[Observed]**
- **V218** — Closed by record. TASK-0426, v0.410, Q1 A. ADR-0178 item 1 says one SetBodyVisibility for each hidden body, and `BodyActions.ShowAll` does that. Test `ShowHiddenTests.A_Tap_On_Show_Hidden_Gives_The_Action_Of_Show_All`. **[Observed]**
- **V219** — Closed by record for its form. TASK-0426, v0.410, Q2 A. ADR-0178 item 2 gives "N hidden", the cut name keeps its chevron, and the list opens under the path line. Item 2 does not say what shows while a body is selected; V226 holds that form. **[Observed]**
- **V220** — Fixed. TASK-0426, v0.410. `StateText` fits the state to the room less the mark (`DesktopUi.Panel.cs` lines 110, 117 and 202). Test `RowDensityTests.The_First_Row_Ends_Before_Its_Mark_In_A_Narrow_Structure_View`. **[Observed]**
- **V221** — Fixed. TASK-0426, v0.410. While the page shows, the arc draws on the foreground list (`DesktopUi.Proposal.cs` line 500), and the paper skips a release whose press or release is on Reject or Accept of the last frame (`DesktopUi.SheetPage.cs` line 513). Test `ProposedSheetTests.On_The_Page_The_Arc_Is_Over_The_Paper_And_Accept_Takes_The_Tap` at each size. **[Observed]**
- **V222** — Fixed for the lines. TASK-0426, v0.410, Q3 A. The arc keeps the line of each command that the page does not show (`DesktopUi.Proposal.cs` line 164). Test `ProposedSheetTests.On_The_Page_The_Arc_Keeps_The_Line_Of_Each_Command_That_The_Page_Does_Not_Show`. Register entry R-0071 holds the amber value of a proposed dimension. **[Observed]**
- **E227** — Fixed. TASK-0431, v0.416, Q6 A. SetSheetTitle refuses a block equal to the block of the sheet (`Engine.Core/Commands/SetSheetTitleCommandHandler.cs` lines 95 to 99); null to empty stays a change. Test `SheetTitleTests.A_SetSheetTitle_That_Changes_Nothing_Is_Refused`. **[Observed]**
- **E228** — Fixed for the escape, and closed by record for the font. TASK-0431, v0.416. The block writes "%" as "%%%" in the DXF file (`DxfExport.cs` line 99), and ADR-0179 "Bad" names the font of the CAD program. Test `A_Percent_Sign_Of_The_Block_Stays_A_Percent_Sign_In_The_Dxf_File`. **[Observed]**
- **E229** — Fixed. TASK-0431, v0.416. ADR-0179 says that the view goes over the block, and a character with no width takes 1015 (`TitleBlock.cs` line 193). Test `A_Character_With_No_Width_In_The_Table_Is_As_Wide_As_The_Widest_Character`. **[Observed]**
- **T185** — Fixed. TASK-0431, v0.416. A request stops after 90 s, past the stall limit of 60 s and before the hang limit of 2 minutes (`HttpConcurrencyTests.cs` line 28). Test `A_Request_Stops_After_The_Stall_Limit_And_Before_The_Hang_Limit`, which reads ci.yml. **[Observed]**
- **T186** — Fixed. TASK-0431, v0.416, Q7 A. `eng/bookcase-compare` compares the builder with the file of the owner when the owner runs it; it references the projects of a headless client only, the solution builds it, and no workflow runs it. Test `BookcaseCaseTests.The_Comparison_With_The_File_Of_The_Owner_Is_A_Program_And_No_Test`. **[Observed]**
- **V223** — Fixed. TASK-0431, v0.416 (`3DEngine.App/SessionSheets.cs` line 57). Test `SheetAddTests.The_Window_Keeps_The_Title_Block_Of_Each_Sheet`. **[Observed]**
- **V224** — Fixed. TASK-0431, v0.416. Each text of the block is clipped to its cell (`DesktopUi.SheetPage.cs` lines 585 to 592). Test `SheetTitlePageTests.Each_Text_Of_The_Block_Is_Clipped_To_Its_Cell`. **[Observed]**
- **V225** — Fixed. TASK-0465 (R32a), v0.449, the answer Q3 A of the owner of 2026-11-25. Where the room cannot hold the file
  name at its floor, its chevron, the gap and "N hidden", the part leaves the path line, and the ring of the part keeps "Show
  hidden" (`3DEngine.App/Ui/DesktopUi.Fixed.cs` lines 171 to 175, ADR-0178 item 2). Test
  `ShowHiddenTests.With_A_Sheet_The_Path_Line_And_The_Top_Bar_End_Left_Of_Views` at 360, 390 and 412 pixels. Register entry
  R-0072 is closed. **[Observed]**
- **V226** — Fixed. TASK-0431, v0.416, Q5 A. The path line counts the hidden bodies only with no selection (`DesktopUi.Fixed.cs` line 514), and ADR-0178 item 2 says so. Test `ShowHiddenTests.With_A_Selected_Body_The_Path_Line_Does_Not_Count_The_Hidden_Bodies`. **[Observed]**
- **V227** — Fixed. TASK-0431, v0.416 (`DesktopUi.Fixed.cs` line 253). Test `PartRingTests.A_Long_Folder_After_The_File_Name_Ends_Left_Of_The_Command_Line`. **[Observed]**
- **V228** — Fixed. TASK-0431, v0.416. The arc draws each ground before each dot (`DesktopUi.History.cs` lines 140 to 141). Test `DimensionLabelsTests.Each_Ground_Of_The_History_Arc_Comes_Before_Each_Dot`. **[Observed]**
- **E230** — Closed by record. TASK-0438, v0.423, Q6 A. ADR-0181 states the union of two bodies that touch on an edge. Test `Two_Cubes_That_Touch_On_An_Edge_Join_In_A_Solid_With_An_Edge_Of_Four_Faces`. **[Observed]**
- **E231** — Closed by record. TASK-0438, v0.423. ADR-0181 item 6 says that faces of the two operands on one plane stay two faces with an edge between them. **[Observed]**
- **E232** — Fixed. TASK-0438, v0.423. The comments and ADR-0180 give the range [0, 2π], about ten units of the last place, and the decisions that use doubles. Test `The_Arctangent_Is_At_Most_Ten_Units_Of_The_Last_Place_From_The_Angle_And_The_Range_Ends_At_Two_Pi`. The header of `RevolvedDistance.cs` repeats the old form: E238. **[Observed]**
- **E233** — Fixed for its form. TASK-0438, v0.423, Q5 A. `E-CHECK-WORK-LIMIT` refuses a check above a bound of work before its first pair (`CheckInterferenceQueryHandler.cs` lines 69 to 80); the token reaches `Bsp.Build`, `Clip`, `ClipTo`, the outer loop of `NearestPoints.Nearest` and each body; `PlanarBoolean.Intersect(a, b, ct)` is new. Tests `A_Check_Whose_Work_Is_Above_The_Limit_Is_Refused_Before_Its_First_Pair` and `A_Cancel_Stops_The_Gap_And_The_Intersection_Inside_One_Pair`. The resolution of a mesh body before the bound is E237. **[Observed]**
- **E234** — Fixed for its form. TASK-0438, v0.423. The check joins the triangles of a mesh into shells and refuses outer shells that share a volume, by an exact intersection. Test `A_Mesh_Whose_Shells_Share_A_Volume_Is_Refused_And_Shells_Apart_Or_A_Void_Are_Checked`. An island in a void is refused with no fault: E236. **[Observed]**
- **T187** — Fixed. TASK-0438, v0.423, Q7 A. The two tests bound the count of the status at 1.5 times the count of today, beside the ratio (`Engine.Tests/Sketch/FixedLengthsTests.cs` lines 30, 32, 104 and 137). **[Observed]**
- **T188** — Fixed. TASK-0438, v0.423. Test `A_Box_In_A_Pocket_Touches_Or_Is_Apart_While_The_Boxes_Of_The_Bodies_Share_A_Volume` (`CheckInterferenceQueryTests.cs` line 248). **[Observed]**
- **E235** — Fixed. TASK-0449 (P0.180), v0.434, Q6 A. `ArcOf` sets `Inward` from an exact sign of a facet of the side
  (`Engine.Core/Queries/MeasureCurve.cs` lines 179 and 186 to 191), and `AboutAxis` turns the sector of a bite
  (`GetAngleQueryHandler.cs` lines 117 and 119). `The_Distance_And_The_Angle_Of_The_Side_Of_An_Arc` gives (1, 0) for +x and
  (0, 1) for −x, the probe of the review. Only `GetAngle` reads `Inward`, so the distance does not change. **[Observed]**
- **E236** — Fixed. TASK-0449, v0.434. Each void leaves the volume that two outer shells share
  (`CheckInterferenceQueryHandler.cs` lines 194 to 203), and `ShellWork` counts each subtract in the bound. Test
  `A_Mesh_With_An_Island_In_A_Void_Is_Checked` (`CheckInterferenceQueryTests.cs` line 359). The new subtract takes no token:
  E241. **[Observed]**
- **E237** — Fixed. TASK-0449, v0.434. The work of the pairs counts the triangles of a mesh body and applies the bound before
  the solid of the mesh (`CheckInterferenceQueryHandler.cs` lines 71 to 79); the work of the shells follows. Test
  `The_Bound_Of_The_Work_Comes_Before_The_Solid_Of_A_Mesh_Body` (line 383). **[Observed]**
- **E238** — Fixed. TASK-0449, v0.434. `PlanarBoolean.Cut` names the square of the edges of the cap
  (`Engine.Kernel/PlanarBoolean.cs` lines 86 to 88), the header of `RevolvedDistance` says that `Better` compares doubles
  (lines 13 to 15), and `ViewBreps` says that its counts stay until the document leaves (lines 22 to 25). **[Observed]**
- **V229** — Fixed. TASK-0449, v0.434. `Prepare` narrows the positions and finds the edge lines and the hatch in the task of
  the cut, and `Upload` only builds the `GpuMesh` objects (`3DEngine.App/SectionView.cs`). ADR-0121 names `SectionView.cs`.
  Test `The_Edge_Lines_And_The_Hatch_Of_A_Cut_Are_Found_In_The_Cut_And_Not_On_The_Thread_Of_The_Frame`. The test reads the hook
  `Searched` that `Prepare` calls, so a second search in `Upload` also passes it. **[Observed]**
- **V230** — Fixed for its form. TASK-0449, v0.434. While the entry of a body lives, a new plane or a new mesh waits for the cut
  that runs (`SectionView.cs` lines 167 to 187). A body that leaves the section and comes back starts a second cut: V233. The
  kept solid of each mesh is out of scope (TASK-0449, "Scope (out)"). **[Observed]**
- **V231** — Fixed. TASK-0449, v0.434. `SectionBodies` takes the visible bodies only (`3DEngine.App/SessionBodies.cs` line 138).
  Test `The_Section_Cuts_The_Visible_Bodies_Only`. **[Observed]**
- **V232** — Fixed. TASK-0449, v0.434. `SectionPlane.Datum` keeps the identity of the datum, and `ShowSectionDatums` follows it
  and goes back to XZ when it is gone (`3DEngine.App/Ui/DesktopUi.Section.cs` lines 47 to 58, ADR-0184 item 2). Test
  `A_Section_On_A_Datum_Follows_The_Datum_And_Goes_Back_To_XZ_When_It_Is_Gone`. **[Observed]**
- **T189** — Fixed. TASK-0449, v0.434. `SectionViewTests` holds cuts with `RunCut`: the old cut stays until the new one, a new
  mesh shows whole, and a body that goes keeps no mesh. `ResponsiveLayoutTests` has the state `SixSegments`; its one exception is
  R-0076. A body that comes back is not tested (V233). **[Observed]**
- **T190** — Fixed. TASK-0449, v0.434. Register entry R-0075 (due 2027-02-19) holds the two gaps of the surface, and
  `AgentGuideExamplesTests` refuses an indented fence and a fence of tildes (line 37). An indented block with no fence still
  passes with no check; the guide has none today. **[Observed]**
- **T191** — Fixed. TASK-0449, v0.434. `eng/report-failed-tests.js` puts the running test of a stop first (lines 219 to 235).
  Test `CrashReportTests.With_Ten_Failed_Tests_The_Running_Test_Of_A_Stop_Has_The_First_Annotation`. **[Observed]**
- **E239** — Fixed for the kept booleans. TASK-0461 (P0.187), v0.447. `DocumentFork` opens each session of a fork with
  `KernelProjection.BeginFork` and ends it with `EndFork` (`Engine.Core/Preview/DocumentFork.cs` lines 152 and 195). A fork
  reads its own store and then the store of its source under `Gate`, and keeps its new results in its own store
  (`Engine.Core/Features/KernelProjection.cs` lines 84 to 100). ADR-0185 items 11 and "Bad" name the store. The kept solids of
  `OfRecord` have the same form: E243. **[Observed]**
- **E240** — Fixed. TASK-0461, v0.447. A proposal that leaves the queue gives its last preview or null, and the route gives
  HTTP 404; a stop of the host gives HTTP 503 with `E-API-HOST-STOPPED` (`Engine.Api.Http/Endpoints/ProposalsEndpoint.cs` lines
  122 to 133). Tests `A_Proposal_That_Leaves_The_Queue_While_A_Caller_Waits_Gives_Not_Found` and
  `The_Stop_Of_The_Host_During_A_Fork_Frees_Its_Backend_And_The_Route_Gives_Host_Stopped`. **[Observed]**
- **E241** — Fixed. TASK-0461, v0.447. `PlanarBoolean.Subtract(a, b, CancellationToken)` (`Engine.Kernel/PlanarBoolean.cs` line
  15), and the subtract of a void passes `ct` (`Engine.Core/Queries/CheckInterferenceQueryHandler.cs` line 206). **[Observed]**
- **T192** — Fixed. TASK-0461, v0.447. Five new tests of `Engine.Tests/Http/ProposalPreviewTests.cs` (lines 930, 967, 996, 1021
  and 1073) hold ADR-0185 items 2, 3, 7 and 10, and item 7 and the comment of `StopPreviews` say where the fork reads the token.
  Some of these tests wait with a limit of 30 s: T198. **[Observed]**
- **V233** — Fixed. TASK-0461, v0.447. `Drop` and `Clear` move a running cut to `_leftCuts`, and a body that comes back waits for
  it (`3DEngine.App/SectionView.cs` lines 114, 145 to 146 and 173). The test of ADR-0184 item 5
  (`SectionViewTests.A_Body_Runs_One_Cut_At_A_Time_And_Keeps_Its_Last_Cut_Until_The_New_One`, line 475) holds the case with the
  same plane and with a new plane. **[Observed]**
- **V234** — Fixed for its form. TASK-0461, v0.447. `GroundPipeline` and `EdgePipeline` make their modules inside the `try` and
  free the layout when the pipeline fails; `PipelineStartTests` fails the start at each step. R-0077 holds `ImageRenderer`.
  Three other pipelines keep the form: V236. **[Observed]**
- **T193** — Fixed. TASK-0461, v0.447. The test of the address 0.0.0.0 and `Exited` assert no "Now listening on" after both
  outputs end (`Engine.Tests/Http/HostGuardTests.cs` lines 160 and 657), and ADR-0173 item 4 states the limit for an idle host
  that uses processor time. **[Observed]**
- **T194** — Fixed for its forms. TASK-0461, v0.447. The gate judges the innermost type that names a shared member
  (`Engine.Tests/Governance/KeptBooleanCollectionGateTests.cs` lines 103 to 113), with rows for a nested test class and a
  helper. **[Observed]**
- **T195** — Fixed for its form. TASK-0461, v0.447. `PathPatternWorkTests` also bounds the expressions by the distinct patterns
  that the gate asks for in the same run (`Engine.Tests/Governance/PathPatternWorkTests.cs` line 75). A warm cache still passes
  with 0 expressions, and the comment does not agree with the code: T199. **[Observed]**
- **T196** — Fixed. TASK-0461, v0.447. `MarkerGateTests` reads each tracked text file outside `docs/`, `tasks/` and
  `Engine.Tests/Governance/`, except a Markdown document (`Engine.Tests/Governance/MarkerGateTests.cs` lines 146 to 168), and the
  row of `docs/templates.md` section 5 says so. An agent of this review listed the tracked files with a script and found no text
  file that the gate skips by error. **[Observed]**
- **V235** — Fixed. TASK-0461, v0.447, the answer Q8 A of the owner of 2026-11-24 (the title of the window ran into "AGENT IS
  WAITING"). The path of the top bar leaves the room of the word (`3DEngine.App/Ui/DesktopUi.Fixed.cs` lines 77 to 78), and
  `WaitingWordTests` checks the word at four sizes. The test holds a real folder of the computer of the owner: V238.
  **[Observed]**
- **E242** — Fixed. TASK-0473, v0.457, ADR-0190 (the answers Q2 A of 2026-11-26 and R-0081 A of 2026-11-27). The `Undo` that
  reverses a failed group has the group of the proposal; the bus takes it only from `CommandBus.Reverse` and from an entry of
  the log, and `StepHistory.Reversed`, `FeatureList`, `SketchRebuild` and `StepTexts` put nothing on the redo list.
  `A_Redo_After_A_Failed_Group_Applies_Nothing` holds the live session, the walkers, a replay and an open. **[Observed]**
- **E243** — Open. Register entry R-0082 holds it with the probe and the correction of the last review as its exit, on the
  choice of the owner of 2026-11-27 ("Move E243, merge"). `Engine.Core/Features/KernelProjection.cs` changed for the
  placements (P11.0): the kept solids hold the placement in their key, and a fork still pushes them out. **[Observed]**
- **E244** — Fixed. TASK-0468, v0.456. Section 12 of the guide for an agent says what a known `commandId` gives, and the row of
  `E-API-PROPOSAL-COMMAND-KNOWN` in `docs/diagnostics.md` has a dated note. **[Observed]**
- **E245** — Fixed. TASK-0473, v0.457. An `Undo` of a group that does not apply gives `E-API-PROPOSAL-NOT-REVERSED`, and a
  handler that throws gives the index of its command (`DocumentSession.FailedIndexKey`). The case of a reversal that does not
  apply after a throw is E250. **[Observed]**
- **V236** — Fixed in part. TASK-0473, v0.457. `RenderPipeline` and `OverlayRenderer` free each object at a failed start, and
  `PipelineStartTests` fails each start at each object; `UiRenderer` makes a set layout, a pool and a sampler before its modules,
  and register entry R-0077 holds it with `ImageRenderer`. T203 is a new form of the check of these tests. **[Observed]**
- **V237** — Fixed by decision. TASK-0473, v0.457. The comment of the accept in `NativeThreeDEngine` and ADR-0188 item 9 say
  that the version is the version of the preview of the last frame; the code did not change. **[Observed]**
- **V238** — Fixed. TASK-0472, v0.455. The rewrite of the history of 2026-11-26 removes the folder of the owner from each
  version of each file, with the map `docs/history-map-2026-11-26.txt`. **[Observed]**
- **T197** — Fixed. TASK-0468, v0.456, the answer Q3 A of the owner. The test holds the fork with `BeforeFork` until the
  proposal is `Applying`, so the accept reads the preview that the person saw in each run; R-0078 closed. **[Observed]**
- **T198** — Fixed. TASK-0473, v0.457. The test of a stale group releases the fork when the accept waits at the version 2,
  with no sleep, and the new tests wait 60 s. **[Observed]**
- **T199** — Fixed. TASK-0473, v0.457. The gate of `governed-by` gives the texts that it can ask for, and the test asks that
  each asked text is one of them. **[Observed]**
- **T200** — Fixed. TASK-0473, v0.457. `Each_Pair_That_The_Bound_Refuses_Shares_No_Volume` intersects the two bodies of each of
  the 8 refused pairs of K10b. **[Observed]**
- **T201** — Fixed. TASK-0468, v0.456. Each guard of an event that must come waits 60 s (ADR-0173 item 4), 16 guards in 16
  files. **[Observed]**
- **T202** — Fixed. TASK-0468, v0.456. `eng/lost-runner.js` also knows "Failed to FinalizeArtifact" with "ECONNRESET", and
  `A_Reset_Of_The_Artifact_Service_Gets_A_Re_Run` holds it with two near texts. **[Observed]**

## 1 Current architecture

The authority diagram of `CLAUDE.md` holds. `Engine.*` keeps the design truth, and `3DEngine.Core` the scene of the render;
no project of the one side references the other side. `DependencyDirectionGateTests` passes. **[Observed]**

Three changes of the range touch the shape of the engine:

- **The placement of a body (P11.0, ADR-0191).** `Engine.Contracts/BodyPlacement.cs` is a value of seven doubles, and
  `BodyRecord` holds it as `Placement`, null at the identity. The solid under the geometry key is the shape in its own frame.
  `Engine.Core/Placements.cs` holds the arithmetic with `+ - * /` only: the relative placement of two bodies, the inverse, the
  product, and the map of a point, a direction, a mesh, a box and an exact solid. The feature list keeps the `PlaceBody` steps
  of each body (`FeatureList.BodyPlacement`), so a walk of the log knows the placement of a consumed body at the step that
  consumed it. `KernelProjection`, `DatumResolver.Chain`, `Regeneration` and the handlers of a move and of a boolean apply the
  same placements. **[Observed]**
- **The mark of a reversal (P0.191, ADR-0190).** An `Undo` with a group is the reversal of a failed group, which only
  `CommandBus.Reverse` and an entry of the log apply; it puts nothing on the redo list. **[Observed]**
- **The lists of P21 (ADR-0189).** `GetSheets` and `GetBodies` give the live sheets and bodies in the order of the log, from the
  feature list. The snapshot of `/events` and `CheckInterference` still give the order of a .NET `Dictionary` (R-0080, row
  K25). **[Observed]**

## 2 Patterns in use

- **One scene in two forms.** Each test of a reader in `PlaceBodyTests` builds a scene with `Rotate` and `Translate`, and again
  with one `PlaceBody` of the same seven numbers, and compares the two answers. The exact solid is the same in the two forms, so
  the readers of the owned kernel agree bit for bit; the readers of the backend agree within 1e-9 mm. The form finds a reader
  that forgets the placement, and nine injections showed it. It does not find a reader that has no test, and V239 is such a
  reader. **[Observed]**
- **A placement as a step of a chain.** `DatumResolver.Chain` gives each placement of a body on the chain of a datum as a
  `PlaceBodyCommand` with no identifier, so the walkers of the chain in doubles and in exact numbers need one more case and no
  new type. **[Observed]**
- **A key that holds the placement.** The cache of the solids, the cache of `GetFaces`, the plan of a preview, the mesh of the
  window and its section view each key on the geometry key and the placement. V239 is the one cache of the window that keys on
  the geometry key alone. **[Observed]**
- **The measure in the test.** The measure of K10a again with `PlaceBody` is a test that asserts the box, the bodies, the replay
  and a bound of the size of the numbers, and writes the times; no time decides it (ADR-0173 item 4). **[Observed]**

## 3 Review of the code

#### E246 · High · The design mass of a cut moves the centre two times for a placed body after a cut by a tool with no curve · Confirmed

- **Where.** `Engine.Core/Queries/CutMass.cs` lines 165 to 180 (`Walk`), 230 (the case of a `Subtract` with a curved tool) and
  240 (`default`).
- **What.** `Walk` takes the cut of `Steps` and moves it from the placement that the step of the creator gives to the placement
  of the body now. For a `Subtract` it takes the placement of the minuend at the step. But `Steps` takes the case of line 230
  only when the subtrahend is a curved tool. For a `Subtract` by a box, `Steps` goes to `default`, which reads
  `KernelProjection.Of(document, handle)`: that solid already has the placement of the body now. When the result of that
  `Subtract` has its own `PlaceBody` and a later `Subtract` by a cylinder takes it as its minuend, `Walk` moves the cut a second
  time. `GetMassProperties` then gives the right volume and a wrong centre, with no error. **[Observed]**
- **Probe.** A box of 20 by 20 by 10 less a box of 5 by 5 by 20 at (8, 8, 0); a `PlaceBody` of the result by (100, 0, 0); a
  cylinder of the radius 2 at (100, 0, 0) cut from it. The same scene with a `Translate` by (100, 0, 0) in place of the
  `PlaceBody` gives the centre x = 99.573 mm; the scene with the `PlaceBody` gives x = 202.995 mm. The probe ran as a test at
  `c885ea90`. **[Observed]**
- **Correction.** Move the cut only when `Steps` took the case of a curved tool: the switch of `Walk` takes a `Subtract` only
  when `ToolOf` of its subtrahend is not null, as the case of line 230 does; or `Steps` gives the placement of the cut that it
  built. Add the probe to `PlaceBodyTests`, with the same scene for a `Union` and an `Intersect` before the cut.
  **[Recommended]**

#### V239 · Medium · The window keeps the faces and the edges of the selected body by its key only, so a placement leaves them at the old place · Confirmed

- **Where.** `3DEngine.App/SessionBodies.cs` line 438 (`KeyOf` gives the key and drops the placement);
  `3DEngine.App/BodyActions.cs` lines 236 to 246 (`_kept` and `Info`) and 254 to 258 (`_keptDistance`); the callers in
  `3DEngine.App/NativeThreeDEngine.cs` lines 466, 480, 1022 to 1025 and 1068 to 1070.
- **What.** `BodyActions` keeps the result of `GetFaces` under the body and the geometry key, and gives the kept result while
  both are the same. A `PlaceBody`, an `Undo` of one and a `Redo` of one keep the key, so the window draws the outline of the
  selected face, the selected edge and the values of an extrude on its edges at the old place, and a tap on an edge hits the old
  edges. A client of the surface or the person (`Undo` of a placement of an agent) can do it; no tool of the window places a
  body yet. P11.0 added the placement to the key of the mesh and of the section, and not to this key. **[Observed]** for the
  code; the effect on the screen is **[Inferred]**: computer use on the window is denied.
- **Probe.** In an image test: a box, `SessionBodies.Apply`, a selection of the face `+z` in `BodyActions`; then a `PlaceBody`
  by (20, 0, 0) and `Apply`; `SelectedFace(bodies.KeyOf(box))!.Point.X` does not change, and it must grow by 20.
- **Correction.** `KeyOf` gives the key and the placement (the `MeshKey` of `SectionView.cs`), and `_kept` and `_keptDistance`
  key on it. Add the probe as a test, with an injection that drops the placement from the key. **[Recommended]**

#### E247 · Medium · ADR-0037 item 8 still tells a client to read a mesh again only when the key changes · Confirmed

- **Where.** `docs/adr/0037-rebuild-of-an-extrude.md` lines 90 to 92; ADR-0191 item 8 amends item 8 only for the new member.
- **What.** Since P11.0, `GetTessellation` gives the triangles with the placement, and a `PlaceBody` keeps the key. The rule of
  item 8 is "a client reads the mesh again when `geometry` differs from the key that it holds", so a client that follows the
  ADR keeps a mesh at the old place. The window has its own rule (ADR-0191 item 7). **[Observed]** for the text; the effect on a
  client is **[Inferred]**.
- **Probe.** A client that keeps the triangles of `GetTessellation` under `geometry`, then a `PlaceBody`: `body.changed` has the
  same `geometry`, so the client does not read the mesh again.
- **Correction.** ADR-0191 item 8 says that a client reads the mesh again when `geometry` or `placement` differs, with a line in
  the history of ADR-0037, and the guide for an agent says it. **[Recommended]**

#### E248 · Low · The JSON of a placement has an eighth member `isIdentity` · Confirmed

- **Where.** `Engine.Contracts/BodyPlacement.cs` line 14; the answers of `GetBodyInfo` and `GetBodies` and the snapshot of
  `/events` (`Engine.Api.Http/WebSockets/WireMessage.cs`), which serialize the value.
- **What.** `IsIdentity` is a public property, so `System.Text.Json` writes it. The events build an object of the seven numbers
  (`CommandBus.Placed`), and ADR-0191 item 8 says "an object of the seven numbers". **[Observed]**
- **Probe.** `JsonSerializer.Serialize(new BodyPlacement(1, 0, 0, 0, 5, 0, 0), camel case)` gives
  `{"qw":1,"qx":0,"qy":0,"qz":0,"dx":5,"dy":0,"dz":0,"isIdentity":false}`. The probe ran as a test. **[Observed]**
- **Correction.** Make `IsIdentity` a static function of `Placements`, and add a test of the members of `placement` in the answer
  of `GetBodyInfo` over HTTP. **[Recommended]**

#### E249 · Low · Two large quaternions that PlaceBody accepts make a boolean fail with a false code of the backend · Confirmed

- **Where.** `Engine.Core/Placements.cs` lines 37 to 49 (`Relative`), 63 (`Compose`), 149 to 156 (`Apply` of an exact solid) and
  182 (the throw of `Placed`).
- **What.** `PlaceBody` accepts a quaternion with a sum of squares from 2^-969 to the largest double. `Relative` multiplies two
  such quaternions, and the product can leave the range where `Rotation.FromQuaternion` gives a turn. `Placed` then throws, and
  the boolean is refused with `E-GEOM-NATIVE-OP`, a code of a fault of the backend. `Apply` of an exact solid skips the turn
  with no error in the same case. **[Observed]**
- **Probe.** Two boxes placed by (1e80, 1e80, 0, 0) and (1e80, 0, 1e80, 0), then a `Subtract`: it is refused with
  `E-GEOM-NATIVE-OP` "The placement has a quaternion that gives no turn". The probe ran as a test. **[Observed]**
- **Correction.** Scale each quaternion by an exact power of two before a product (`Math.ILogB` and `Math.ScaleB`, which are
  exact), so that each accepted placement composes; `Apply` throws when a turn is null. **[Recommended]**

#### E250 · Low · When a handler throws in a group and the reversal does not apply, the record says that nothing applied · Confirmed

- **Where.** `Engine.Core/DocumentSession.cs` lines 198 to 203; `Engine.Api.Http/Proposals.cs` lines 186 to 200.
- **What.** The catch of `ApplyGroup` calls `ReverseGroup` and drops its result. When that `Undo` does not apply, for example
  when the journal fails to write it, the proposal closes as failed with no code, and the commands before the one that threw
  stay applied. `E-API-PROPOSAL-NOT-REVERSED` comes only on the path with no throw (E245). **[Observed]** by reading; no probe
  ran, because it needs a journal that fails at one write and a handler that throws.
- **Correction.** Put the result of the reversal on the exception (`exception.Data`, as `FailedIndexKey`), and give
  `E-API-PROPOSAL-NOT-REVERSED` in the catch of `Proposals` when it did not apply. **[Recommended]**

#### V240 · Low · A creation and a PlaceBody of one body in one frame give two meshes · Confirmed

- **Where.** `3DEngine.App/SessionBodies.cs` line 188 (the merge of finding V55).
- **What.** The creation takes the state of the last change of the body in the frame, but not its placement. The change then
  sees another placement, and the window uploads the mesh a second time and waits for an idle device again. The final state is
  right. An accepted proposal or an agent gives such a pair. **[Observed]**
- **Probe.** A `CreateBox` and a `PlaceBody` of the box before the first `Apply`: `Meshes` is 2, and the rule of V55 is 1. The
  probe ran as an image test. **[Observed]**
- **Correction.** Add `Placement = last.Placement` to the merge, and the probe as a test. **[Recommended]**

#### T203 · Low · A test of a failed start passes with no check when the validation layer is absent · Confirmed

- **Where.** `3DEngine.ImageTests/PipelineStartTests.cs` lines 15 to 40; `3DEngine.Vulkan/GraphicsDevice.cs` line 81.
- **What.** The test of a leak asks that the count of the messages of the validation layer does not grow. The device turns the
  validation on only when the layer is installed, and no test asks for `ValidationEnabled`. On a computer with no layer the new
  cases of `RenderPipeline` and `OverlayRenderer` pass whatever the code frees. The pipeline installs the layer, so the risk is a
  local result only. **[Observed]**
- **Correction.** Assert `device.ValidationEnabled` in the tests that read the count, or skip them with a reason when the layer
  is absent. **[Recommended]**

#### T204 · Low · Two tests of P11.0 compare less than their comments say · Confirmed

- **Where.** `Engine.Tests/Commands/PlaceBodyTests.cs` lines 147 to 161 (`The_Faces_Of_A_Placed_Body_Are_In_Its_Place`) and 297
  (`The_Lines_Of_A_View_Of_A_Placed_Body_Are_In_Its_Place`).
- **What.** The test of the faces says "each face and each edge", and it compares the area, the point and the normal of each
  face, and only the sum of the lengths of the edges, which a placement does not change; the outline of a face and the segments
  of an edge, which the window draws, are not compared. The test of the view lines has a box only; ADR-0191 item 7 names it for
  the views and the sections of a sheet, and the section and the curves of a curved side have no test with a placed body. Each
  of them reads the placed solid today, so no result is wrong. **[Observed]**
- **Correction.** Compare the outline of each face and the segments of each edge within 1e-9 mm, and add a placed cylinder and a
  section to the test of the view lines. **[Recommended]**

#### T205 · Low · The window test of the section view checks the key of the mesh, not the cut · Confirmed

- **Where.** `3DEngine.ImageTests/DesktopFormsTests.cs` lines 241 to 262; `3DEngine.App/SectionView.cs` lines 71 and 186.
- **What.** The test asks that `SectionBodies[0].Geometry` changes after a `PlaceBody`, and runs no `SectionView`. A change of
  line 186 that compares `Geometry.Key` would keep a stale cut, and the implicit conversion from `Guid` to `MeshKey` lets such a
  change compile. **[Observed]** for the code; the stale cut is **[Inferred]**.
- **Correction.** Run one frame of `SectionView` before and after the `PlaceBody`, and ask that a new cut starts; remove the
  implicit conversion if no caller needs it. **[Recommended]**

## 4 The next planned task

E246 is high, so its correction comes before the next product phase: row P0.192 holds it (TASK-0476). The medium and low
findings come after the next product phase: row P0.193 (TASK-0477) holds V239, E247, E248, E249, E250, V240, T203, T204 and
T205.

The order of the roadmap after P11.0 is R34, R35 and K25, "unless the owner moves them" (the order of 2026-11-26). R34 gives the
phone project files through the document picker of Android, and R35 reads two values on the phone. Each needs a run on the phone
or on the emulator that a person checks; computer use on the window is denied, and no person checks the phone this weekend.
K25 needs no phone: it gives the snapshot of `/events` and `CheckInterference` the order of the log, and it closes register entry
R-0080, a risk due on 2026-12-26. Question Q1 asks the owner for the order.

## 5 Research for that task

For K25, read in the repository:

- `Engine.Core/Queries/GetBodiesQueryHandler.cs` and ADR-0189 item 3: the order of the log comes from the feature list, the
  order of the commands that made the bodies. **[Observed]**
- `Engine.Api.Http/WebSockets/WireMessage.cs` (`SnapshotProjector.Project`): the snapshot takes `doc.Bodies`, the values of a
  `Dictionary`. **[Observed]**
- `Engine.Core/Queries/CheckInterferenceQueryHandler.cs`: with no `bodyIds` the query takes the live bodies in the same order,
  and the pairs follow it. **[Observed]**
- Register entry R-0080: after a command removes a record, the next record takes the free place of the `Dictionary`, so the
  order changes with the history. A replay with one runtime gives the same order; a new version of .NET can change it.
  **[Observed]**

## 6 Recommended approach

1. P0.192: correct E246 in `Walk` with the probe of section 3 as a test, and the same scene for a `Union` and an `Intersect`. An
   injection that moves the cut for each `Subtract` makes the test fail.
2. K25 (Q1 A): one function gives the live bodies in the order of the log, as `GetBodies` gives them, and the snapshot and
   `CheckInterference` take it. The ADR comes in the same commit, because the order of an existing answer changes. A test removes
   a body and creates one, and asks for the order of the log in the snapshot and in the pairs.
3. P0.193: the medium and low findings of section 3, each with its probe as a test and an injection.
4. Then R34 and R35, when the owner can check the phone. **[Recommended]**

## Questions for the owner

1. **Q1 (the next product phase).** The order after P11.0 is R34, R35 and K25, and R34 and R35 need a check on the phone.
   **A (recommended):** K25 first, then R34 and R35, so that the next product phase needs no person and closes R-0080 before its
   date. **B:** R34 first, as the order says; it waits for a check on the phone before its merge.
2. **Q2 (the correction of E249).** **A (recommended):** scale each quaternion by an exact power of two before a product, so each
   placement that `PlaceBody` accepts composes. **B:** `PlaceBody` refuses a quaternion with a sum of squares outside a range, as
   2^-100 to 2^100, with a new code.
