---
date: 2026-11-06
commit: 4667e4b
ledger: v0.233
previous: 2026-11-06-d-codebase-review.md
---

# Codebase review — 2026-11-06 (e)

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the twenty-seventh codebase review. The review of 2026-11-06 (d) examined v0.223. After it came 4 product
milestones (K5b, R12b, R16a and R16b1) and 10 ledger entries (v0.224 to v0.233). The next product milestone would be the
fifth, so this review is due before it. `docs/templates.md`, section 7, gives the form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is the
authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this file does not.

| Item | Value |
|---|---|
| Code state | `4667e4b`, the close of R16b1 on its branch; the merge of v0.233 holds its tree |
| Branch of this review | `codebase-review-2026-11-06-e` |
| Tests | 1,967 on this computer (1,384 in `Engine.Tests`, 337 in `3DEngine.Tests`, 243 in `3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`) |
| Pipeline | The run of `main` at v0.233 (42300002450): green on Ubuntu, Windows and macOS, on the APK and on the emulator, and the four determinism hashes are equal |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner decides.

**Method.** Ten tasks changed code or records after `3379675`, the commit of the last review: TASK-0239 (P0.95),
TASK-0240 (the review), TASK-0241 (P0.96), TASK-0244 (K5b), TASK-0243 (P0.98), TASK-0242 (P0.97), TASK-0245 (R12b),
TASK-0247 (P0.99), TASK-0246 (R16a) and TASK-0248 (R16b1). Two review agents worked in parallel at `4667e4b`, each one in
its own git worktree. Each agent wrote probes and injections that only its worktree held, and removed them. I read the
cited code of the high finding V112 and checked the run of T160 with the public API. The reviews of Codex gave no file
after v0.224. The brief of each agent asked it to write only the findings that it examined.

## Summary

- **The fixes of the last review hold.** E158, E159, E161 to E163, V104, V106 to V111 and T152 to T159 are fixed; V105 is
  fixed for its form, and its fix opened V112. E160 is fixed in part (E166). **[Observed]**
- **One new finding is high.** With the hint of P0.99, a new start of the activity during a session runs `PhoneHost.Run`
  a second time in its process, and `Window` has no `Dispose` and no call of `SDL_DestroyWindow`, so the second run cannot
  make its window; before P0.99 the process ended and the next start worked (V112). **[Observed]** by reading the package
  and the code; no device ran it.
- **No new medium finding.**
- **Fourteen new low findings:** E164 to E166 in the persistence, the faces and a comment; V113 to V118 in the cache of
  the faces, the fixed controls, the list "New", the edges and the safe area of the phone; T160 to T164 in the ledger,
  two gates, the smoke test and the affects fields. T160: the run of `main` at v0.225 failed in the emulator job, and no
  entry named it. **[Observed]**
- **Open findings: 0 critical, 5 high, 14 medium and 33 low.** **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 17 | `.csproj` lines in `3DEngine.sln`. The last review gave 17; `3DEngine.Android` is outside the solution. |
| Production source lines | 30,368 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests`, `3DEngine.ImageTests` and `3DEngine.VisionTests`, 287 files. The last review gave 29,817 in 285 files. |
| Test source lines | 35,567 | `Engine.Tests` (25,385 in 117 files), `3DEngine.Tests` (3,616 in 22 files), `3DEngine.ImageTests` (6,501 in 41 files) and `3DEngine.VisionTests` (65 in 1 file). The last review gave 34,973. |
| Tests | 1,967 | `dotnet test 3DEngine.sln`: 1,384, 337, 243 and 3 passed, 0 failed, 0 skipped. The last review gave 1,944. |
| Gate classes | 19 | Tracked files whose name ends in `GateTests.cs`. `eng/repo-gates` gives 468 runs of 161 checks. The last review gave 19, and 467 runs of 160 checks. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 15 | Section "Open" of `docs/register.md`; the limit is 15. The last review gave 15. |
| ADRs | 72 | `docs/adr/0001` to `0061` and `0070` to `0080`, with 0003 and 0011 in `docs/adr/archive/`. In force: 70 (33 accepted, 37 amended). None is proposed. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 5 | The same method: V2, V3, T4, T31, V112. The last review gave 5; E162 is fixed. |
| Findings: medium | 14 | The same method: E5, E6, E12, E13, E19, E21, E22, V4, T2, T5, T10, T17, T32, T37. The last review gave 20; E163, V104, V105, T152, T153 and T154 are fixed. |
| Findings: low | 33 | The same method: E17, E24, E34, E39, E57, E84, E160, E164 to E166, V7, V15, V29, V84, V88, V113 to V118, T7, T13, T23, T25, T143, T160 to T164, C1, P1. The last review gave 33; E158, E159, E161, V106 to V111 and T155 to T159 are fixed. |
| Lines in the rule files | 2,927 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The last review gave 2,902. |
| Sentences with "must" | 59 | `grep -o -w -i must` on the same ten files. The last review gave 59. |
| Sentences with "do not" | 55 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 54. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found. E13, T32, E84, E160 and V84 are fixed in part and count as open. A finding that the owner accepted counts as closed.

## Findings of the previous review

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

## 1 Current architecture

**[Observed]** The selection escalates from the body to a face and to an edge: an edge is the pair of the names of its
two faces, and `GetFaces` gives the edges with their lengths and segments (R12b, ADR-0077). The panel makes a box or a
cylinder from the list "New" (K5b). The redesign started: the desktop and the phone take the theme "Proposed
(harmonized)" with a casing under each mark of the viewport (R16a, ADR-0078), and the title of the document, Undo, Redo
and the status line are fixed controls (R16b1, ADR-0080). A save to a new name keeps a handle on its document (P0.96).
The activity of the phone may start again in its process (P0.99, ADR-0079). The probe of the determinism hash holds a
cylinder, and the four platforms give one hash.

## 2 Patterns in use

- **A fix of one form opens the next form on a path that the fix did not run.** The hint of P0.99 keeps the process for a
  new start before a session, and a new start during a session now meets a window that is never destroyed (V112). The
  copy of P0.96 removes the moment with no handle, and a delete that fails now leaves a second document that opens
  (E164). **[Observed]**
- **A cache keyed on one field misses a change of another.** The kept entry of `GetFaces` has the geometry key, which a
  fault keeps (V113). **[Observed]**
- **The ledger still undercounts the failures of a job that the session does not wait for.** The run of `main` at v0.225
  was not read before the next push (T160). **[Observed]**
- **A gate that reads text misses a form that MSBuild evaluates.** A wildcard in an Update (T161), a condition or a
  NoWarn (T162). **[Observed]** by injection.

## 3 Review of the code

### The engine and the hosts

#### E164 · Low · DocumentJournal.MoveTo leaves a second document file that opens, with no report, when a delete fails · Confirmed by a probe

- **[Observed]** `Engine.Core/Persistence/DocumentJournal.cs:167` to `173`: a failure of the copy disposes the new
  stream and calls `DeleteQuietly(target)`. `DocumentJournal.cs:182` calls `DeleteQuietly(old)` after the switch.
  `DeleteQuietly` (`DocumentJournal.cs:185` to `195`) swallows `IOException` and `UnauthorizedAccessException`. No
  diagnostic, no return value and no log line tells the caller that a file stayed.
- **[Observed]** Probe `Probe_Delete_Of_The_Old_File_Fails`: a session with a file and one command; a second handle
  held the old file with `FileShare.ReadWrite` (no `FileShare.Delete`) during `SaveAs`. `SaveAs` gave null (a
  success). The old file stayed with 2 lines, the new file had 3 lines after one more command, and the old file opened
  as a document at the version 1.
- **[Observed]** Probe `Probe_Copy_Fails_And_The_Partial_Target_Stays`: the stream of the new name failed after 120
  bytes, and a second handle held that file. `SaveAs` gave `E-IO-SAVE-FAILED`, and the journal stayed whole (the next
  command applied, the old file had 5 lines). The partial file of 120 bytes stayed at the new name.
  `DocumentFile.Check` gave null for it, and `DocumentFile.Open` opened it as a document with a shorter history.
- **[Observed]** Before TASK-0241, `File.Move` failed in the first case, so the save gave `E-IO-SAVE-FAILED` and no second
  file came. The ledger v0.226 (`docs/CURRENT-STATE.md:6769`) says "A delete of the old file that fails leaves that
  file". ADR-0015 item 9 (`docs/adr/0015-command-log-persistence.md:101` to `102`) says only that the journal "closes and
  deletes the old file".
- **[Inferred]** The header of a file has no identity of the document (`Engine.Core/Persistence/EntryCodec.cs:29` to
  `37`), and no host reads the unsaved folder as a list, so neither file is lost and neither is two times in one list.
  But a person or an agent can open the old name, or a later save to the partial name gives "exists", and the file at
  that name is a valid document with a shorter history. A virus scanner or an indexer can hold a file without the share
  of a delete on Windows.
- **[Recommended]** After a failed delete of the old file, give a warning to the caller (for example a `Diagnostic`
  of the save) and say so in ADR-0015 item 9. After a failed delete of a partial target, cut the target to length 0
  before the dispose, so that no partial document opens. Add the two probes as tests.

#### E165 · Low · GetFaces gives the outline of a face a segment that no edge entry holds, at an edge of more than two faces · Confirmed by a probe

- **[Observed]** `Engine.Core/Queries/GetFacesQueryHandler.cs:146` takes only a B-Rep edge with `Faces.Count == 2`
  for `Edges`. The outline of a face (`GetFacesQueryHandler.cs:122` to `125`) takes each edge with exactly one face of
  the name, whatever the count of its faces. ADR-0077 item 1 names "each B-Rep edge with two faces of different names"
  and says nothing about an edge with one face or more than two.
- **[Observed]** Probe `Probe_Non_Manifold_Edge` (Manifold backend): a plate of 10 x 10 x 2 with the quadrants
  `(+x, +y)` and `(-x, -y)` cut away by two boxes of 5 x 5 x 4. The two blocks touch along the z axis. `GetFaces` gave
  10 faces and 22 edges. The outlines of the four faces of the tools (`-x` and `-y` of one tool, `+x` and `+y` of the
  other) each held one segment on the z axis. No edge entry held a segment on the z axis.
- **[Inferred]** The overlay draws the mark of such a face with a side that a tap cannot select (ADR-0077 item 3), and
  a later reference of an edge has no name for it. The case needs a cut that leaves two solids that touch along a line.
- **[Recommended]** Decide in ADR-0077 what an edge of one face or of more than two faces is: either each pair of
  different names of its faces, or no edge and no segment of an outline. Make the outline and `Edges` agree, and add
  this body as a test.

#### E166 · Low · The false comment of DatumRecord.cs stays with no register entry and no task · Confirmed by reading

- **[Observed]** `Engine.Contracts/Sketch/DatumRecord.cs:3` still says "FeatureId is a CreateBox or an Extrude in
  effect". Since TASK-0238 a cylinder is also a feature (`Engine.Core/Sketch/DatumResolver.cs:68` to `71`).
- **[Observed]** TASK-0242 kept the comment because the task forbids `Engine.Contracts`
  (`tasks/TASK-0242-engine-and-pipeline-forms-of-the-twenty-sixth-review.md:84` to `86`, ledger v0.229 at
  `docs/CURRENT-STATE.md:6862` to `6863`). A search of `docs/register.md` for `DatumRecord` and `E160` gives nothing, and
  no open task names the comment.
- **[Inferred]** A comment does not change the public shape, so a task that may write `Engine.Contracts/Sketch/**` can
  correct it with no ADR. With no entry, the next task does not know about it.
- **[Recommended]** Add a register entry with a date and a time limit, or put the line in the write set of the next task
  that writes `Engine.Contracts`.

**No finding.** I checked these items and found them correct.
Lead 1, other paths: a journal with no stream makes the file at the new name and writes the header
(`DocumentJournal.cs:151` to `157`), as before. A copy to another volume is a copy, so it works where `File.Move` also
worked. A failure of the open of the new name comes before the `try`, so nothing is left and the journal stays whole
(the test `A_New_Name_That_Does_Not_Open_Fails_The_Save_And_The_Journal_Stays_At_Its_Old_Name`). The test
`A_Save_To_A_New_Name_Holds_A_Handle_On_The_Document_At_Each_Moment` (`Engine.Tests/Persistence/DocumentFileTests.cs`)
records the count of the live streams of the journal at each close; the old form closes before the open and gives
`[0]`, the new form gives `[1]`. It proves the order of the handles of this journal. It does not prove that a second
host is kept out, which the ledger v0.226 says.
Lead 2 (E163): the check is in `Decide` (`Engine.Core/CommandBus.cs:188` to `195`), inside the serial section of
`Apply` (`CommandBus.cs:122` to `152`), after the look-up, the broken journal and the kind, and before the version
check and the handler. Each of the 25 handlers of `Engine.Core/Commands` implements `ICommandEncoder`, and in each one
the keys of `Encode` are the keys of `Parameters`. The values of a required number or `Guid` cannot be null, so only a
required string is checked. `Undo` and `Redo` have no parameter, so the check does nothing for them; a replay and an
open apply entries that an open of a file already refused with a null value. Probe
`Probe_Optional_Null_Fields_Pass_And_Required_Null_Is_Refused`: `CreateSketch` with no name applied, `RenameBody` with a
null name gave `E-CMD-FIELD-MISSING`, and an `Undo` applied. `docs/diagnostics.md:21` registers the code.
Lead 5: the pair order uses `Guid.CompareTo`, then `string.CompareOrdinal`, then `Nullable.Compare`
(`GetFacesQueryHandler.cs:191` to `200`). Probe `Probe_Guid_Order_Against_Text`: for four pairs with a high bit in the
first, second and fourth groups, `Guid.CompareTo` gave the sign of the ordinal compare of the text, so the order is the
"text with no culture" of ADR-0077 item 1. The vertices of the B-Rep are unique by value (`Engine.Kernel/Brep.cs:37`),
so the union-find by vertex index counts chains. The length is a sum in the order of the B-Rep edges of
`Math.Sqrt` of an exact square, so it is the same on each platform. An edge between two faces of one name is skipped
(`GetFacesQueryHandler.cs:149` to `150`), as ADR-0077 item 1 says. `EdgeEntry` is in `Engine.Core`, so
`Engine.Contracts` did not change.
E158, E159, E160 and E161: the `catch` of `DocumentFile.Lines` takes `ArgumentException` and `NotSupportedException`
(`Engine.Core/Persistence/DocumentFile.cs:285`), and `Check` reads the unit first, so a NUL path gives
`E-IO-LOAD-FAILED`. `EngineHost.FolderCase` ignores case on Windows and macOS (`Engine.Api.Http/EngineHost.cs:100` to
`102`). The four reasons of `DatumResolver` name CreateCylinder. The probe log (`Engine.Core/Determinism/CrossPlatformProbe.cs:79`
to `88`) adds a circle extrude, a cylinder, a translate and a subtract, with no call of the determinism rules.

**Not verified.** I did not run a second process against a save. I did not probe `DocumentFile.Open` and
`DocumentFile.Create` with a NUL path; `Load` holds the file before it reads it and catches `IOException` and
`UnauthorizedAccessException` only (`DocumentFile.cs:123` to `130`), so they can still throw `ArgumentException`. I did
not read the desktop and phone code of R12b, K5b, R16a and R16b1.

### The desktop and the phone

#### V112 · High · With the new hint, a second start of the activity in a process runs PhoneHost.Run again, and its window cannot be made · Confirmed by reading

- **[Observed]** `MainActivity.OnCreate` sets `SDL_ANDROID_ALLOW_RECREATE_ACTIVITY` after `base.OnCreate`
  (`3DEngine.Android/MainActivity.cs:34-38`, `3DEngine.Android/PhoneHost.cs:66-74`). The bytecode of
  `SDLActivity.onCreate` of ppy.SDL3-CS 2026.1002.1 (javap of the jar in `SDL3-CS.aar`) reads
  `nativeAllowRecreateActivity` two times: at offset 71-128, when `mSDLMainFinished` or `mActivityCreated` is set, and at
  offset 344-393, after `nativeCheckSDLThreadCounter`. With the hint, the new activity goes on, calls `SDL.initialize`
  (which sets `mSDLThread` to null) and later starts a new `SDLMain` thread, so `Main` and `PhoneHost.Run` run again in
  the same process.
- **[Observed]** `PhoneHost.Run` makes `new Window(...)` and never destroys it (`PhoneHost.cs:43`). `Window` has no
  `Dispose` and no call of `SDL_DestroyWindow`, and no code in the scope calls `SDL_Quit`
  (`3DEngine.Vulkan/Window.cs:24-88`). In `libSDL3.so` (android-x64) of the package, only `Android_CreateWindow` writes
  the global `Android_Window`, only `Android_DestroyWindow` clears it, and `Android_CreateWindow` holds the string
  "Android only supports one window" (a Node.js scan of the ELF symbols and of the RIP-relative references).
- **[Inferred]** So when a session ran before the new start, the second `new Window` gets null from `SDL_CreateWindow`
  and throws "SDL: failed to create window" (`Window.cs:68-72`). The `catch` of `Run` writes "the session of the phone
  ended", `Main` returns, and `SDLMain.run` calls `finish` on the activity (bytecode of `SDLMain.run`, offset 33-62). The
  process stays alive with the old window, so each later tap on the icon gives the same failure, until Android or the
  person ends the process. Before P0.99 the same new start ended the process with the code 0, and the next start was a
  new process that worked. The emulator case of run 42289169473 is different: there `Main` had not started, so no
  window existed.
- **[Inferred]** The old session ends at the new start: `onDestroy` of the old activity calls `nativeSendQuit`, waits
  1000 ms for the thread, and then calls `nativeQuit` (bytecode of `onDestroy`, offset 39-80), and `PhoneLifecycle` maps
  `SDL_EVENT_QUIT` and `SDL_EVENT_TERMINATING` to `Quit` (`3DEngine.App/PhoneLifecycle.cs:33`). If the dispose of the
  engine takes more than 1000 ms, `SDLMain` can call `finish` on the new activity, because `mSingleton` then names it.
- **[Observed]** ADR-0079 says "A keeps the program on the screen" and, in item 3, "a new start of the activity starts
  the program again in its process" (`docs/adr/0079-the-activity-may-start-again.md:39-40`, `:53-54`). For a new start
  during a session, the code does not do that. The ledger entry v0.231 says correctly that no run had a new start.
- **[Recommended]** Make one `Window` and one `LocalSessionHost` for the process (static fields of `PhoneHost`), and let a
  second `Run` take them. This also keeps the document through a new start, which ADR-0079 says is lost. Alternatively,
  destroy the window at the end of `Run`. Add a step to the smoke test or to the test sheet of the phone that forces a new
  start during a session, for example the developer option "Do not keep activities" (`always_finish_activities`) and a
  return to the program, and read the line "the session of the phone starts" two times. Correct the text of ADR-0079.

#### V113 · Low · The kept entry of GetFaces keeps the face and the edge of a body that has a fault, because a fault keeps the geometry key · Confirmed by probe

- **[Observed]** `BodyActions.Info` gives the kept `FacesInfo` when the body and the geometry key are the same
  (`3DEngine.App/BodyActions.cs:165-171`). A rebuild that fails keeps the key and sets only the fault
  (`Engine.Core/Features/Regeneration.cs:287-295`), and `SessionBodies` changes the key only when the event gives another
  key (`3DEngine.App/SessionBodies.cs:213-216`). `GetFaces` refuses a body with a fault with `E-GEOM-BODY-FAULTED`
  (`Engine.Core/Queries/GetFacesQueryHandler.cs:61-63`).
- **[Observed]** Probe: an extrude of a rectangle, two selects on its top face, then `DeleteSketchItem` of one line. The
  body got `W-SKETCH-REBUILD-FAILED` with the same key. `SelectedFace(key)` still gave the face `end` with its normal;
  `SelectedFace(null)` gave null and cleared the face.
- **[Inferred]** Until the body changes again, the panel shows the face line, the outline, a selected edge and the
  button "Sketch on face" for a body whose faces are not known. ADR-0075 item 3 and the comment of `SelectedFace` say
  that such a face is cleared (`docs/adr/0075-a-second-tap-selects-the-face.md:96`, `BodyActions.cs:191-192`). Before
  V109 the query cleared it. The button then asks `GetFaceAt` again (`3DEngine.App/SketchActions.cs:44-50`), so no wrong
  command follows, only a message. The ledger entry v0.228 says that the entry "is kept by its body, its face and the
  geometry key"; the code keeps it by the body and the key only.
- **[Recommended]** Put the fault in the condition of the kept entry: clear `_kept` when `LiveBody.Fault` of the body is
  not null, or keep the fault code with the key. Make the probe a test of `BodyActionsTests`.

#### V114 · Low · The panel can cover the anchors of Undo and Redo, and then a tap on them does nothing · Confirmed by probe

- **[Observed]** The panel window has no flag against a move or a resize, and its place and size have
  `ImGuiCond.FirstUseEver` (`3DEngine.App/Ui/DesktopUi.Panel.cs:13`, `:18`, `:21`). The anchors are a window of their own
  (`3DEngine.App/Ui/DesktopUi.Fixed.cs:27-39`). ADR-0080 item 2 says "The panel ends above them".
- **[Observed]** Probe: two frames at 1280 by 1024 and then frames at 1280 by 720 kept the panel at (16, 16, 476, 716);
  Undo was at (16, 656, 64, 704). After one tap on the panel, a touch tap and a mouse click on the centre of Undo each gave
  no action and no sample to the scene.
- **[Inferred]** The desktop window is not resizable (`3DEngine/DesktopEngineHost.cs:23`) and each engine of the phone
  makes a new context, so the change of the display size is rare. A person who drags the title bar of the panel down,
  or its corner, makes the same overlap on the desktop and on the phone. The anchors then do not work until the person
  moves the panel again. `FixedControlsTests` holds the first frame only.
- **[Recommended]** Limit the panel with `SetNextWindowSizeConstraints` to the space above `AnchorRow`, or give the panel
  `ImGuiWindowFlags.NoMove` with a place that is set each frame. Add a row to `FixedControlsTests`: a panel that the
  person drags down does not cover Undo.

#### V115 · Low · The title bar cuts a file name of about 14 characters or more, and the unit goes first · Confirmed by probe

- **[Observed]** The title is "3DEngine | " and the file name and " | " and the unit (`DesktopUi.Panel.cs:245-249`), in
  a panel of 460 pixels (`:18`). Probe with `TextSize`: "3DEngine | plate.3de | mm" is 351 pixels, and
  "3DEngine | bracket-v2-final.3de | mm" is 505 pixels.
- **[Inferred]** The title bar also holds the arrow of the collapse and the padding, so the text has less than 460 pixels.
  For a usual file name, the person does not see the unit, and ADR-0080 removed the line "File: name" that showed the full
  name. A name with "##" in it also ends the shown title there, because Dear ImGui hides the text after "##".
- **[Recommended]** Put the unit before the name, or give the panel a minimum width from the title. Add a row to
  `FixedControlsTests` with a long name.

#### V116 · Low · While the list "New" is open, a tap on the scene goes to the scene, and the list stays open · Confirmed by probe

- **[Observed]** The router gives a press to the panel only when the point is over a rectangle of the last frame
  (`3DEngine.App/Ui/UiInputRouter.cs:66-67`). The open list adds its own rectangle (`DesktopUi.Panel.cs:40`, `:122`).
- **[Observed]** Probe at 1280 by 720: a touch tap opened the list. A touch tap and a mouse click at (900, 400) each gave
  two samples to the intent layer, and the entry "Box" stayed at (36, 245, 160, 299). A tap on the panel, outside the
  list, closed the list with no action.
- **[Inferred]** Dear ImGui does not see a press that the router gives to the scene, so it cannot close the popup. The
  person sees the list open over the panel while a tap selects or hides a body. Touch and mouse do the same, so
  objective 9 holds. The list "Cut with" has the same path.
- **[Recommended]** While a list is open, give a press outside each rectangle to the panel too, or close the list on such
  a press, and decide if the tap also reaches the scene. Add a test for each list.

#### V117 · Low · A tap just outside the silhouette, near an edge of the selected face, clears the body in place of the edge selection · Confirmed by reading

- **[Observed]** `BodyActions.Apply` asks `edgeAt` only when the pick hits the selected body; a miss sets the selection to
  null, and the setter clears the face and the edge (`BodyActions.cs:75-88`, `:22-30`). ADR-0077 item 3 says "A select
  intent on the selected body" (`docs/adr/0077-a-tap-near-an-edge-selects-the-edge.md:85-88`).
- **[Inferred]** For an edge on the silhouette, as the far edges of the top face of a box, the 12 pixels of reach are on
  one side only. A finger that lands 5 pixels outside loses the body, the face and the edge, and the person must tap three
  times again. A mouse lands more exactly, so the cost is higher for touch.
- **[Recommended]** With a face selected, ask `edgeAt` before the pick, so that a tap within 12 pixels of an edge selects
  it on each side. Record the change in ADR-0077 item 3 and add a row to `EdgeSelectionTests`.

#### V118 · Low · The fixed controls do not read the safe area of the phone, and SDL draws the surface under the system bars · Inferred by reading

- **[Observed]** `SDLActivity.onCreate` calls `setDecorFitsSystemWindows(false)` on API 30 and later (bytecode, offset
  55-68), and `SDLSurface` reads the insets of the system bars, the gestures and the cutout and gives them to
  `onNativeInsetsChanged` (javap of `SDLSurface.onApplyWindowInsets`). No code in the scope reads `SDL_GetWindowSafeArea`
  or an inset. The anchors are 16 pixels above the bottom edge (`DesktopUi.Fixed.cs:27`), and the title bar of the
  panel is 16 pixels below the top edge (`DesktopUi.Panel.cs:13`).
- **[Inferred]** If the system bars are visible, the anchors lie under the navigation bar and the title under the
  status bar and the cutout. With three-button navigation the system takes the taps there. I did not find if SDL makes
  the window fullscreen on Android, which hides the bars; `PhoneHost` asks for no fullscreen flag (`PhoneHost.cs:43`).
- **[Recommended]** Read `SDL_GetWindowSafeArea` in the host and give the inset to `DesktopUi`, so the fixed controls and
  the panel start inside it. Add a step to the test sheet of the phone: tap Undo with three-button navigation.

**No finding.** **[Observed]** The fixes of V104 to V111 hold. `SdlInput` gives `PointerPhase.Cancel` for
`SDL_EVENT_FINGER_CANCELED` (`3DEngine.Vulkan/SdlInput.cs:57-65`), and `A_Cancelled_Finger_Ends_And_A_Later_Tap_Selects`
holds it. `Presenter.Drew` is set false at the start of `RenderFrame` and true after the submit (`Presenter.cs:93`,
`:158`), and `PhoneHost` writes the line at the first true (`PhoneHost.cs:120-126`). The outline and the edge take the
offset of the gizmo (`NativeThreeDEngine.cs:591-596`). Only `BodyActions` reads `CancelIntent`, and only with `ByKey`
(`BodyActions.cs:93-98`). `Distance` has its comment again, and ADR-0075 names the six files and TASK-0237, TASK-0243 and
TASK-0245. The edge search has no offset (`NativeThreeDEngine.cs:254`), but **[Inferred]** a `SelectIntent` comes only
from the tap gesture (`3DEngine.Core/Input/IntentMachine.cs:401-407`) and a press on a handle is a drag, so no tap comes
during a drag; the release of a move changes the body and clears the face. A selected edge whose pair the body no
longer has is cleared (`BodyActions.cs:181-191`), and each change of the face clears the edge (`:37-46`). The key is
written at each change of a body and removed at a consume (`SessionBodies.cs:201`, `:213-216`, `:224`); a rename and a
change of the visibility keep the faces, a hidden body clears the selection, and an undo gives the earlier key. The
casing is under each line, point, ring and square of a sketch, the outline, the edge and each line of the dimensions
(`3DEngine.App/Ui/DesktopUi.Overlay.cs:36-49`, `:59-77`, `:105`); the arrows and the labels have none, as ADR-0078 item 2
says, and the gizmo is drawn by the renderer, which item 2 does not name. The casing is a draw only, so no hit area
changes. The image tests that read the clear compare exact bytes for the silhouette (`FirstDemonstrationImageTests.cs:296-301`)
and a band of 2 for the quads, so a face of `#313a47` is not the clear; the snap test reads magenta. On a display of 1440
by 3088 the panel ends at 716, the anchors are at 3024 to 3072, and the empty status line of the phone is at 140
(probe). The fixed rectangles are in `OverPanel` (`DesktopUi.cs:371-372`), so a tap on them does not reach the scene,
and the anchors window does not take the focus when it appears. The list "New" opens and its entries act by touch and
by mouse (`NumericPadUiTests.cs:220-232`, probe), and each entry is 44 pixels high. The hint is set early enough: a
later `onCreate` reads it, and `SDL_SetHintWithPriority` holds a reference to its name (xref scan), which is the special
path of SDL for this hint. `ConfigurationChanges` holds the eight changes of v0.228 and the two of v0.231. The ledger
entries v0.224 to v0.233 agree with the code for the desktop and the phone, except the two statements in V112, V113 and
V114; the counts of the test lists agree with the local runs. `dotnet test 3DEngine.Tests` gave 337 passed and
`dotnet test 3DEngine.ImageTests` gave 243 passed, 0 failed, at `4667e4b` before the probes.

**Not verified.** No device and no emulator ran the APK, and nobody started the desktop, so no anchor, no edge, no
theme and no new start of the activity was seen. The source of SDL 3.5.0 is not on this disk: the effect of
`nativeSendQuit` on a thread that the pause holds, the use of the token by `finish` after a new start (V112), and
whether SDL makes the window fullscreen on Android (V118). Whether One UI starts the activity again for a change of the
wallpaper colours during a session. Linux and macOS.

### The gates and the pipeline

#### T160 · Low · The ledger again counts the failures of the emulator job too low, and one failed run of main is not named · Confirmed by the public API

- **[Observed]** `api.github.com`: run 42280512471 (`main` at `28c89a2`, the merge of v0.225) failed in the job "Start
  the APK on the emulator", step 7 "Run the smoke test", and the comparison was skipped. No file of `docs` or `tasks`
  names this run.
- **[Observed]** The ledger v0.226 (`docs/CURRENT-STATE.md:6780`) names 42279267538 the eighth failure; that is true. The
  entry v0.227 (line `6813`) names 42281585857 the ninth, and the entry v0.228 (line `6848`) names 42285027469 the
  eleventh. With 42280512471 they are the tenth and the twelfth. The runs 42289169473 and 42291677527 then are the
  thirteenth and the fourteenth; the entries v0.229 and v0.231 give no number.
- **[Observed]** The entry v0.228 (line `6823`) says that the emulator job "failed in 3 of the 4 runs before it". The four
  runs before 42285931818 are 42282815764 (passed), 42282849670 (passed), 42283808971 (failed) and 42285027469
  (failed): 2 of 4.
- **[Observed]** Each other run that the entries v0.224 to v0.233 name exists and has the stated result: 42274351754,
  42280536874, 42282849670, 42285931818, 42290937312, 42291699658, 42292342902, 42293786790, 42296524985 and 42298401536
  passed; 42279267538, 42281585857, 42283808971, 42285027469, 42289169473, 42291677527 and 42297666769 failed. The jobs
  of 42285027469, 42289169473 and 42291677527 show the emulator job as the failed job.
- **[Inferred]** This is the defect of T159 again, one review later. The owner reads the count to judge V105 and
  ADR-0079; the count is low by one from v0.227.
- **[Recommended]** Add a correction in the next ledger entry: 42280512471 is the ninth failure, and the later numbers
  move by one. Before each ledger entry, list the failed runs since the last entry from the API, not from memory.

#### T161 · Low · An Update with a wildcard changes the version of a pinned package, and each gate passes · Confirmed by injection

- **[Observed]** `Each_Pinned_Package_Is_Named_In_The_File_Of_Its_Pin_Only`
  (`Engine.Tests/Governance/DependencyDirectionGateTests.cs:1426` to `1442`) compares the decoded text of each
  `Include` and `Update` with the name of the package. `No_Package_Item_Updates_Or_Removes_By_A_Property` (line `1448`)
  refuses `$(`, `@(` and `%(` only. No rule refuses `*` or `?` in the `Update` or the `Remove` of a package item.
- **[Observed]** Injection: `Engine.Geometry.Manifold/Directory.Build.targets` with
  `<PackageReference Update="Engine.Geometry.Manifold.Nativ?" Version="3.5.2.1" />`. `dotnet msbuild
  Engine.Geometry.Manifold/Engine.Geometry.Manifold.csproj -getItem:PackageReference` gave
  `Engine.Geometry.Manifold.Native` with the version `3.5.2.1`, not the `3.5.2.2` of the project file
  (`Engine.Geometry.Manifold/Engine.Geometry.Manifold.csproj:21`). The governance tests gave 483 passed; the one failure
  was `BuiltAssemblyReferenceTests`, because the solution was not built (the dll of `3DEngine.Core` was absent).
  `eng/repo-gates` gave "468 runs of 161 checks, 0 failed". The file is removed.
- **[Inferred]** The pin of the native backend decides the bits of each replay (ADR-0017 rule 4). The fix of T155
  closed the escape and the property and left the wildcard.
- **[Recommended]** Refuse `*` and `?` in the `Update` and the `Remove` of `PackageReference`, `GlobalPackageReference`
  and `PackageVersion` in the new rule. Better, compare the version that `dotnet msbuild -getItem:PackageReference`
  gives for each project with the version of its pin in a pipeline step, so that no text form escapes the gate.

#### T162 · Low · A condition on the SupportedPlatform item or a NoWarn of CA1416 silences the analyzer, and each gate passes · Confirmed by injection

- **[Observed]** `Each_Project_That_The_Phone_Reaches_Supports_Android` (`DependencyDirectionGateTests.cs:622`) reads the
  `Include` of each `SupportedPlatform` and ignores its `Condition` and the `Condition` of its `ItemGroup`. The new lines
  of T156 (`DependencyDirectionGateTests.cs:625` to `632`) refuse a `Remove` and an `Update` only. No gate reads `NoWarn`
  for `CA1416`.
- **[Observed]** Injection 1: `Engine.Kernel/Engine.Kernel.csproj:20` with `Condition="false"` on
  `<SupportedPlatform Include="android" />`, and `Engine.Kernel/Rev27AndroidProbe.cs`, which sets
  `Console.ForegroundColor`. `dotnet msbuild -getItem:SupportedPlatform` gave Linux, macOS and Windows only, and `dotnet
  build Engine.Kernel --no-incremental` gave 0 warnings and 0 errors.
- **[Observed]** Injection 2, in the same run: `Engine.Core/Directory.Build.props` with `<NoWarn>$(NoWarn);CA1416</NoWarn>`
  and `Engine.Core/Rev27AndroidProbe.cs` with the same call. `dotnet build Engine.Core --no-incremental` gave 0 warnings
  and 0 errors. The review of v0.223 showed that the same call without a silence gives `error CA1416`.
- **[Observed]** With both injections, the governance tests and `eng/repo-gates` gave the results of T161. The project
  file is restored from a copy, and its SHA-256 is the same; the other files are removed.
- **[Inferred]** The rule came from the third run of the emulator, which ended on `Console.ForegroundColor`
  (TASK-0229). Each of these silences brings that failure back with green gates. An `.editorconfig` severity of
  `CA1416` is a third route; I did not test it.
- **[Recommended]** Evaluate the item with `dotnet msbuild -getItem:SupportedPlatform` for each project that the phone
  reaches, in a pipeline step, and refuse `CA1416` in each `NoWarn`, `WarningsNotAsErrors` and `.editorconfig` that the
  gate reads. Prove it with these two injections.

#### T163 · Low · Two of the three patterns of the native crash cannot match the line that Android writes, and the test proved them with a line that Android does not write · Confirmed by a probe

- **[Observed]** `eng/android/smoke-emulator.sh:109` fails on
  `Fatal signal .*org\.threedengine\.phone|Fatal signal .*threedengine\.phone|DEBUG +: .*>>> org\.threedengine\.phone`.
  The review of v0.223 (T153) gave the form `(threedengine.phone)` in its fake `adb`, and TASK-0242 proved the fix with
  that fake (ledger v0.229, `docs/CURRENT-STATE.md:6877`).
- **[Observed]** Probe: `grep -E` with this pattern on three lines. The line `F libc : Fatal signal 11 (SIGSEGV), ...,
  pid 3410 (eedengine.phone)` did not match. The line `F DEBUG : pid: 3410, tid: 3426, name: SDLThread  >>>
  org.threedengine.phone <<<` matched. The line of the fake with `(threedengine.phone)` matched.
- **[Inferred]** The name after the pid in the line of `libc` is the name of the main thread, which holds 15
  characters at most; for a dotted name Android keeps the last 15, so `org.threedengine.phone` gives
  `eedengine.phone`. I know this from the source of Android (`AndroidRuntime` and the signal handler of bionic) and from
  logs of other packages, not from a run of this program. Then only the tombstone line of `DEBUG` can fail the job.
- **[Inferred]** The wait loop ends at the first frame (`eng/android/smoke-emulator.sh:65` to `72`), so a native crash
  after the last `logcat -d` passes the job, as T153 said.
- **[Recommended]** Match `Fatal signal .*\(eedengine\.phone\)` and the pid of the program, or match the `DEBUG` line and
  `Cmdline: org.threedengine.phone` only. Use a line of a real tombstone in the fake `adb`. Read the log one more time
  a few seconds after the first frame.

#### T164 · Low · The affects fields of ADR-0076 and ADR-0077 do not name four files each that their decisions change · Confirmed by reading

- **[Observed]** Commit `87a3a28` (TASK-0245) changes `3DEngine.App/Ui/DesktopUi.cs`, `DesktopUi.Panel.cs`,
  `DesktopUi.Overlay.cs` and `PanelAction.cs` for ADR-0077 item 4 (the panel line "Edge" and the mark). The affects
  field of ADR-0077 (`docs/adr/0077-a-tap-near-an-edge-selects-the-edge.md:10` to `15`) names none of them.
- **[Observed]** Commit `f70f55f` (TASK-0244) changes `3DEngine.App/BodyActions.cs` (`CreateCylinder`),
  `NativeThreeDEngine.cs`, `Ui/DesktopUi.cs` and `Ui/PanelAction.cs` for ADR-0076 item 7. The affects field of ADR-0076
  (`docs/adr/0076-the-cylinder.md:11` to `19`) adds `Ui/DesktopUi.Panel.cs` only.
- **[Observed]** TASK-0243 corrected the same defect in ADR-0075 for finding V111 of the review of v0.223. The affects
  field of ADR-0079 names the two files of its change, and that of ADR-0080 names the three files of its change.
- **[Inferred]** `TaskGovernanceGateTests` (`Engine.Tests/Governance/TaskGovernanceGateTests.cs:70`) requires a task to
  name each ADR whose affects field meets its write set. A later task that changes the mark of an edge or the list
  "New" in those files is then not required to read ADR-0077 or ADR-0076. No gate compares the files of the commit of
  an ADR with its affects field, so the defect came back in the next two ADRs.
- **[Recommended]** Add the eight paths to the two affects fields. Add a gate: each code file in the write set of a task
  that is named in the History of an ADR it creates must match the affects field of that ADR, or be named in a list of
  exceptions in the task.

**No finding.** I checked these items and found them correct.
Lead 3 (T152): `.github/workflows/ci.yml:191` runs the comparison when the gate passed, also when the emulator job
failed, was cancelled or was skipped. `eng/compare-determinism.js:26` to `29` requires the phone when the job gave
`success` or when its file exists, and else compares the three desktop files with a warning. The upload of the phone
hash comes after the smoke test with no `if: always()`, so a failed smoke test gives no file. An empty file of the phone
fails the check of the first line (`compare-determinism.js:42` to `45`). The API shows the comparison passed in runs
42289169473 and 42291677527, whose emulator job failed, and in run 42290937312, whose emulator job passed.
Lead 4: the new rules pass on each file of today, so they have no false positive. `Uri.UnescapeDataString` decodes the
`%2E` and `%34` of the injections of T155. A `Remove` of a `SupportedPlatform` in a target is a descendant, so the gate
reads it.
T157 and T158: the script calls `fail` for a hash with no line of the probe, and each call of `adb` has `timeout 30` or
more. The script fails on the line "ends the process" of `PhoneHost.AllowNewStart`, which writes that text when the hint
is refused.
Lead 6: the test counts of the entries v0.224 to v0.233 (1944, 1944, 1945, 1948, 1953, 1957, 1963, 1963, 1965, 1967) are
the sums of `eng/test-floor.txt` at each merge, with the Linux count of `3DEngine.ImageTests`. The counts of milestones
and entries after the review of v0.223 are true: K5b, R12b, R16a and R16b1 are 4 product milestones, and v0.224 to v0.233
are 10 entries, so the next review is due before the fifth product milestone.
Lead 7: ADR-0079 agrees with `3DEngine.Android/MainActivity.cs` and `PhoneHost.AllowNewStart` (the hint after
`base.OnCreate`, the two configuration changes, one line). ADR-0077 items 1 and 2 agree with `GetFacesQueryHandler.Edges`.
ADR-0015 item 9 agrees with `MoveTo`, apart from the failed delete of E164.

**Not verified.** I did not read a log of a job, an artifact or the annotations of the hashes, because they need a
sign-in or more requests than the limit of the API permits; for the comparison of runs 42289169473, 42290937312 and
42291677527 I read the result of the job only. I did not run the emulator. I did not test an `.editorconfig` severity of
`CA1416`. I did not count the open entries of `docs/register.md` ("15 of 15").

## 4 The next planned task

The next product phase is R16b2, the tool ring that follows the selection, with the tools that exist today (the order of
the owner of 2026-11-06, evening). V112 is high, so a correction comes before R16b2: P0.100 (TASK-0250) holds V112. By
`docs/templates.md`, section 7, the low findings go after R16b2: P0.101 (TASK-0251) holds E164 to E166 and T160 to T164,
and P0.102 (TASK-0252) holds V113 to V118.

## 5 Research for that task

**[Observed]** `Window` (`3DEngine.Vulkan/Window.cs`) makes its SDL window in its constructor and has no `Dispose`. The
desktop runs one window for each process; the phone can now run `PhoneHost.Run` twice in one process. **[Inferred]** A
`Dispose` that calls `SDL_DestroyWindow`, and a `using` in `PhoneHost.Run`, let a second run make its window; a test of
`Window` needs SDL with a video driver, which the image tests have.

## 6 Recommended approach

**[Recommended]** In this order:

1. P0.100 (TASK-0250): V112, with a test that a second window can be made after the first is disposed. 1 agent hour.
2. R16b2: the tool ring.
3. P0.101 (TASK-0251) and P0.102 (TASK-0252): the low findings, each with a test or an injection before the change.

## Questions for the owner

None. The standing order of 2026-11-05 covers each choice of this review.
