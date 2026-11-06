---
date: 2026-11-06
commit: 3379675
ledger: v0.223
previous: 2026-11-06-c-codebase-review.md
---

# Codebase review — 2026-11-06 (d)

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the twenty-sixth codebase review. The review of 2026-11-06 (c) examined v0.214. After it came 4 product
milestones (R14f3, R15, R12a and K5a) and 9 ledger entries (v0.215 to v0.223). The next product milestone would be the
fifth, so this review is due before it. `docs/templates.md`, section 7, gives the form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is the
authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this file does not.

| Item | Value |
|---|---|
| Code state | `3379675`, the merge of K5a, v0.223 |
| Branch of this review | `codebase-review-2026-11-06-d` |
| Tests | 1,944 on this computer (1,376 in `Engine.Tests`, 335 in `3DEngine.Tests`, 230 in `3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`) |
| Pipeline | The run of `main` at `3379675` (42273772389): green on Ubuntu, Windows and macOS, on the APK and on the emulator, and the four determinism hashes are equal |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner decides.

**Method.** Nine tasks changed code or records after `df11d6e`, the commit of the last review: TASK-0230 (the review),
TASK-0233 (R14f3), TASK-0231 (P0.91), TASK-0232 (P0.92), TASK-0234 (R15), TASK-0235 (P0.93), TASK-0236 (R12a),
TASK-0237 (P0.94) and TASK-0238 (K5a). Two review agents worked in parallel at `3379675`, each one in its own git
worktree: the engine with the gates and the pipeline, and the desktop with the application and the program of the
phone. Each agent wrote probes and injections that only its worktree held, and removed them. I read the cited lines of
the medium findings V104, T152 and T159, and of the two findings of the audit of Codex, and I measured the values below.

The reviews of Codex of v0.214 to v0.222 and two audits of Codex at `22143bd` give eight leads, and each one is a finding
here: V106 (v0.214), T155 (v0.217), T154 and T158 (v0.220), V107 (v0.221), E162 and E163 (the audit of the persistence and
the determinism). The audit of `Engine.Kernel` gives no finding. My own lead, the exit of the smoke test with no line of
the hash, is T157.

TASK-0239 (P0.95) merged at v0.224, after the commit of this review. It fixes T154 and two of the six calls of T158.

## Summary

- **The fixes of the last review hold.** E156, E157, V97 to V103, T147, T150 and T151 are fixed. T148 and T149 are
  fixed for their forms, and a probe found the next form of each (T155, T153). V101 is fixed by the answer Q1 B. V96 is
  fixed. **[Observed]**
- **One new finding is high.** `DocumentJournal.MoveTo` releases the lock of the file during the move of a save, so a
  second host can write a line there, and the document then does not open (E162). It comes from the audit of Codex;
  I confirmed it by reading. **[Observed]**
- **Six new medium findings.** A finger that Android cancels stays down in the intent layer (V104). A new start of the
  activity on a freshly booted emulator ends the process with no line (V105, inferred). A failure of the emulator job
  skips the comparison of the desktop hashes (T152). The smoke test passes a native crash after the start (T153), and
  it wrote more annotations than GitHub keeps (T154, fixed at v0.224). A null value of a required field gives a
  document that does not open (E163). **[Observed]** by probes and by reading, except V105.
- **Fifteen new low findings:** E158 to E161 in the engine, V106 to V111 in the desktop and the phone, T155 to T159 in
  the gates, the smoke test and the ledger. T159: the ledger counted four failures of the emulator job where there were
  seven. **[Observed]**
- **K5a and R12a are sound.** The cylinder uses `+ - * / sqrt` only, and its replay, its undo and redo, and its save and
  open work. The outline of R12a is correct for a hole, a split face and the facets of a curve. The split of R15
  changed no line of code. **[Observed]** by probes.
- **Open findings: 0 critical, 5 high, 20 medium and 33 low.** **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 17 | `.csproj` lines in `3DEngine.sln`. The last review gave 17; `3DEngine.Android` is outside the solution. |
| Production source lines | 29,817 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests`, `3DEngine.ImageTests` and `3DEngine.VisionTests`, 285 files. The last review gave 29,134 in 276 files. |
| Test source lines | 34,973 | `Engine.Tests` (25,157 in 117 files), `3DEngine.Tests` (3,593 in 22 files), `3DEngine.ImageTests` (6,158 in 38 files) and `3DEngine.VisionTests` (65 in 1 file). The last review gave 34,432. |
| Tests | 1,944 | `dotnet test 3DEngine.sln`: 1,376, 335, 230 and 3 passed, 0 failed, 0 skipped. The last review gave 1,895. |
| Gate classes | 19 | Tracked files whose name ends in `GateTests.cs`. `eng/repo-gates` gives 467 runs of 160 checks. The last review gave 19, and 450 runs of 157 checks. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 15 | Section "Open" of `docs/register.md`; the limit is 15. The last review gave 15. |
| ADRs | 68 | `docs/adr/0001` to `0061` and `0070` to `0076`, with 0003 and 0011 in `docs/adr/archive/`. In force: 66 (31 accepted, 35 amended). None is proposed. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 5 | The same method: V2, V3, T4, T31, E162. The last review gave 4. |
| Findings: medium | 20 | The same method: E5, E6, E12, E13, E19, E21, E22, E163, V4, V104, V105, T2, T5, T10, T17, T32, T37, T152, T153, T154. The last review gave 17; V97, T147 and T149 are fixed. |
| Findings: low | 33 | The same method: E17, E24, E34, E39, E57, E84, E158 to E161, V7, V15, V29, V84, V88, V106 to V111, T7, T13, T23, T25, T143, T155 to T159, C1, P1. The last review gave 30; E156, E157, V96, V98 to V103, T148, T150 and T151 are fixed. |
| Lines in the rule files | 2,902 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The last review gave 2,881. |
| Sentences with "must" | 59 | `grep -o -w -i must` on the same ten files. The last review gave 59. |
| Sentences with "do not" | 54 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 54. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found. E13, T32, E84 and V84 are fixed in part and count as open. A finding that the owner accepted counts as closed.

## Findings of the previous review

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. No change: `Engine.Api.Http/Endpoints/JsonParameters.cs` has no commit since `df11d6e`. Register entry R-0027.
- **E6** — Open. No change: `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` has no commit since `df11d6e`. Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. No change.
- **E8** — Fixed. TASK-0044, v0.37. No change.
- **E9** — Fixed, and it holds. No change.
- **E10** — Fixed. TASK-0035, v0.43. No change.
- **E11** — Fixed. TASK-0115, v0.115. No change.
- **E12** — Open, and smaller. No change: `Engine.Core/Hosting/ParameterBinder.cs` has no commit since `df11d6e`. Register entry R-0027.
- **E13** — Fixed in part. No change: `Engine.Api.Http/Endpoints/QueriesEndpoint.cs` has no change since `df11d6e`; the change of `Engine.Cli/Cli.cs` is the catch of E156 (TASK-0231).
- **E14** — Fixed. TASK-0115, v0.115. No change.
- **E15** — Fixed. TASK-0044, v0.37. No change.
- **E16** — Fixed. TASK-0115, v0.115. No change.
- **E17** — Open, and smaller. No change: `Engine.Core/CommandBus.cs` has no commit since `df11d6e`. Register entry R-0028.
- **E18** — Fixed for the sinks of `Engine.*` (TASK-0140, v0.141) and of the desktop host (T118, TASK-0150, v0.154). TASK-0140, v0.141. No change.
- **E19** — Open, and smaller. No change: `Engine.Core/CommandBus.cs`, `Engine.Core/DocumentSession.cs` have no commit since `df11d6e`. Register entry R-0033.
- **E20** — Fixed, and it holds. No change.
- **E21** — Open. No change: `Engine.Core/Commands/SubtractCommandHandler.cs` has no commit since `df11d6e`. Register entry R-0033.
- **E22** — Open. No change: `Engine.Core/CommandBus.cs` has no commit since `df11d6e`. Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. No change.
- **E24** — Open in seven parts of nine. No change: `Engine.Api.Http/WebSockets/WireMessage.cs`, `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` have no commit since `df11d6e`. Register entry R-0033.
- **E25** — Accepted by the owner. TASK-0142, v0.144. No change.
- **E26** — Fixed. TASK-0135, v0.136. No change.
- **E27** — Accepted by the owner. TASK-0142, v0.144. No change.
- **E28** — Fixed. TASK-0136, v0.137. No change.
- **E29** — Fixed for the replay. TASK-0135, v0.136. No change.
- **E30** — Fixed. TASK-0136, v0.137. No change.
- **E31** — Fixed for the replay. TASK-0137, v0.138. No change.
- **E32** — Fixed. TASK-0135, v0.136. No change.
- **E33** — Fixed with E18. TASK-0140, v0.141. No change.
- **E34** — Open. No change: `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs` has no change since `df11d6e`. Register entry R-0036.
- **E35** — Fixed for the routes of `MapGet`. TASK-0139, v0.140. No change.
- **E36** — Fixed. TASK-0079, v0.85. No change.
- **E37** — Fixed. TASK-0137, v0.138. No change.
- **E38** — Fixed. TASK-0138, v0.139. No change.
- **E39** — Open. No change of the three choices of the backend; `3DEngine.Android/PhoneHost.cs` changed for the probe (TASK-0233) and the hint of the pause (TASK-0232), with the same check of the native library. Register entry R-0036.
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
- **E57** — Open. No change: `Engine.Tests/Http/DesktopSurfaceTests.cs` has no commit since `df11d6e`. Register entry R-0039.
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
- **V2** — Open. No change: `3DEngine.Vulkan/Swapchain.cs`, `3DEngine.Vulkan/OverlayRenderer.cs` have no commit since `df11d6e`. Register entry R-0022.
- **V3** — Open. No change: `3DEngine.Vulkan/GraphicsDevice.cs` has no commit since `df11d6e`. Register entry R-0022.
- **V4** — Open. No ledger entry from v0.215 to v0.223 names it. Register entry R-0027.
- **V5** — Fixed. TASK-0076, v0.74. No change.
- **V6** — Fixed. TASK-0076, v0.74. No change.
- **V7** — Open. No change: `3DEngine.Vulkan/Ui/UiRenderer.cs`, `3DEngine.Vulkan/Ui/UiTexture.cs`, `3DEngine.Vulkan/GpuBuffer.cs` have no commit since `df11d6e`. Register entry R-0030.
- **V8** — Fixed. TASK-0074, v0.75. No change.
- **V9** — Fixed. TASK-0074, v0.75. No change.
- **V10** — Fixed. TASK-0074, v0.75. No change.
- **V11** — Fixed in part. TASK-0074, v0.75. No change.
- **V12** — Fixed, and changed by decision. No change.
- **V13** — Fixed for its parts. No change.
- **V14** — Fixed. TASK-0074, v0.75. No change.
- **V15** — Open. No change: `3DEngine.App/SessionBodies.cs` has no commit since `df11d6e`.
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
- **T2** — Open in one part of five. No ledger entry from v0.215 to v0.223 names it. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. No change.
- **T4** — Open. No change: `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs` has no commit since `df11d6e`. Register entry R-0031.
- **T5** — Open in one part of three. No ledger entry from v0.215 to v0.223 names it. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Open in four parts of five. No ledger entry from v0.215 to v0.223 names it. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. No change.
- **T10** — Open. No ledger entry from v0.215 to v0.223 names it. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51. No change.
- **T12** — Fixed. TASK-0059, v0.55. No change.
- **T13** — Open in part. `.github/workflows/ci.yml` changed only for the comparison of the determinism hashes (TASK-0233) and the annotations of the emulator job (TASK-0235), 30 lines in and 3 out. Register entry R-0031.
- **T14** — Fixed. TASK-0053, v0.51. No change.
- **T15** — Fixed. TASK-0050, v0.47. No change.
- **T16** — Fixed. TASK-0066, v0.67. No change.
- **T17** — Open. No change: `Engine.Tests/Governance/NativePackageGateTests.cs` and `nuget.config` have no change since `df11d6e`. Register entry R-0031.
- **T18** — Fixed. TASK-0049, v0.46. No change.
- **T19** — Fixed. TASK-0049, v0.46. No change.
- **T20** — Fixed. TASK-0049, v0.46. No change.
- **T21** — Fixed. TASK-0049, v0.46. No change.
- **T22** — Fixed. TASK-0049, v0.46. No change.
- **T23** — Open. No change: `eng/manifold-native/build.ps1` has no commit since `df11d6e`. Register entry R-0031.
- **T24** — Fixed. No change.
- **T25** — Open in part. No change: `eng/write-set-check` has no change since `df11d6e`. Register entry R-0031.
- **T26** — Fixed. TASK-0057, v0.54. No change.
- **T27** — Fixed. TASK-0058, v0.56. No change.
- **T28** — Fixed. No change.
- **T29** — Fixed. TASK-0057, v0.54. No change.
- **T30** — Fixed. TASK-0057, v0.54. No change.
- **T31** — Open. No change: `eng/repo-gates/RepoGates.csproj` has no commit since `df11d6e`. Register entry R-0031.
- **T32** — Fixed in part. No change: `eng/write-set-check/Program.cs` has no commit since `df11d6e`.
- **T33** — Fixed. TASK-0057, v0.54. No change.
- **T34** — Fixed. TASK-0057, v0.54. No change.
- **T35** — Fixed. TASK-0057, v0.54. No change.
- **C1** — Open. No change: `Engine.Contracts/Geometry/IBRepOps.cs` has no commit since `df11d6e`. Register entry R-0027.
- **P1** — Open. `3DEngine.App/NativeThreeDEngine.cs` changed for the start again (TASK-0232) and the face of R12a (TASK-0236), 31 lines in and 4 out. Register entry R-0027.
- **T36** — Fixed. TASK-0061, v0.61. No change.
- **T37** — Open in part. No change: `Engine.Tests/Governance/WriteSetRangeTests.cs` has no commit since `df11d6e`. Register entry R-0031.
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
- **V29** — Open. No check by touch from v0.215 to v0.223; the test sheet of the phone (`docs/android-test-sheet.md`) waits for the owner.
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
- **E84** — Fixed in part. No change: `Engine.Core/Persistence/DocumentJournal.cs` has no commit since `df11d6e`.
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
- **V84** — Fixed in part. `3DEngine.App/FaceArm.cs` changed only for the button "Sketch on face" (TASK-0237).
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
- **V88** — Open for the desktop, and fixed for the phone. No change of the build output of the desktop; `eng/android/check-apk.sh` changed only for the row of each library (T150, TASK-0231).
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
- **V96** — Fixed. TASK-0232, v0.218 corrected the two texts of V103.
- **T145** — Fixed. TASK-0224, v0.210. No change.
- **T146** — Fixed. TASK-0224, v0.210. No change.
- **E156** — Fixed. TASK-0231, v0.217; the tests cover the command line. The next form, the other readers of the kind and the unit of a file, is E158.
- **E157** — Fixed. TASK-0231, v0.217, for the final separator. The next form, a path in another case on Windows, is E159.
- **V97** — Fixed. TASK-0232, v0.218: `RunSessions` reads the path after the `using` of the session (`3DEngine/DesktopEngineHost.cs:96-113`).
- **V98** — Fixed. TASK-0232, v0.218: the list "Cut with" and Show all start a new line when they do not fit.
- **V99** — Fixed. TASK-0232, v0.218: the phone gets a `DocumentLine` with `Files: false`, which shows the message only.
- **V100** — Fixed. TASK-0232, v0.218: `FanOutSink.Announce` runs the report on the thread pool.
- **V101** — Fixed by the answer Q1 B. TASK-0232, v0.218 sets the hint before the first window and corrects ADR-0073 item 3. Two comments that the code does not prove are V110.
- **V102** — Fixed. TASK-0232, v0.218.
- **V103** — Fixed. TASK-0232, v0.218.
- **T147** — Fixed. TASK-0231, v0.217: the gate accepts R14f2, P8b1 and K5a and refuses P0.91; the theory has ten rows.
- **T148** — Fixed for its forms. TASK-0231, v0.217, for an `Update` with the name of the package. An escape or a property in the value of `Update` is T155.
- **T149** — Fixed for its forms. TASK-0231, v0.217, for the line "ended" and a fatal exception of Java. A native crash after the start is T153.
- **T150** — Fixed. TASK-0231, v0.217: each library has the checksum of its own row.
- **T151** — Fixed. TASK-0231, v0.217, for the entries v0.208 to v0.211. New false counts of the failures of the emulator job are T159.

## 1 Current architecture

**[Observed]** The engine has a second primitive: `CreateCylinder` makes the prism of the circle of ADR-0040, 64 plane
facets, through `IExtrudeOps`, and the owned kernel names its faces `+z`, `-z` and `side` (K5a, ADR-0076). A second tap
on the selected body selects the face under the point; `GetFaces` gives the outline of each face name, and the panel
gives a button "Sketch on face" (R12a, ADR-0075). The user interface of the desktop is six partial files, one for each
region (R15). The program of the phone runs the probe of the determinism hash, and the pipeline compares it with the
three desktop runners (R14f3, ADR-0074). The smoke test of the emulator gives its causes as public annotations (P0.93).

## 2 Patterns in use

- **A fix covers the path of its report, and the next form waits on a near path.** The catch of E156 covers the command
  line, and the other readers of the file still throw (E158); the compare of E157 has the separator and not the case
  (E159); the pin rule of T148 reads the name and not an escaped value (T155); the smoke test of T149 reads a Java
  exception and not a native signal (T153). **[Observed]**
- **The pipeline hides the cause of its own failures.** The annotations had a limit that the script did not respect
  (T154), a failure of the phone hides the comparison of the desktop hashes (T152), and the ledger counted fewer
  failures than the runs show (T159). **[Observed]**
- **One intent has two meanings.** `CancelIntent` is a lost pointer and the key Escape since v0.222, so a lost pointer
  clears a face (V108). **[Observed]**
- **The state of a resource is released during a change of its name.** The journal closes its file for the move of a
  save (E162). **[Observed]**

## 3 Review of the code

### The engine and the hosts

#### E158 · Low · DocumentFile.Check, ReadKind and ReadLengthUnit still throw ArgumentException for a path with a NUL character; the fix of E156 covers the command line only · Confirmed by a probe

- **[Observed]** TASK-0231 put the fix of E156 in `Engine.Cli/Cli.cs:219` to `226`: the `catch` of the read of the kind
  takes `ArgumentException` and `NotSupportedException`. `DocumentFile.Lines` (`Engine.Core/Persistence/DocumentFile.cs:262`
  to `283`) still converts only `IOException` and `UnauthorizedAccessException`. `DocumentFile.Check`
  (`DocumentFile.cs:87` to `106`) catches `DocumentFileException` only.
- **[Observed]** Probe `Probe_Check_Nul_Path`: `DocumentFile.Check("a\0b.3de", new InProcessMeshBackend())` threw
  `ArgumentException: Null character in path.` It did not return an `ErrorDetail`.
- **[Inferred]** The review of v0.214 recommended the fix in `DocumentFile.Lines`, so that each caller gets the code. Each
  other caller of `Check`, `ReadKind` and `ReadLengthUnit` (the desktop at `3DEngine/DesktopDocument.cs:57`, and
  `EngineHost` at `Engine.Api.Http/EngineHost.cs:55`) still gets the exception. The desktop review must judge if a person
  can give such a path.
- **[Recommended]** Convert `ArgumentException` and `NotSupportedException` into `DocumentFileException.Load` in
  `DocumentFile.Lines`. Add a test of `Check` with `"a\0b.3de"` that gives `E-IO-LOAD-FAILED`.

#### E159 · Low · EngineHost.FileName compares the unsaved folder with case, so a journal opened by a path of another case on Windows shows its journal name · Confirmed by a probe

- **[Observed]** `Engine.Api.Http/EngineHost.cs:100` to `102` compares the folder of the path with `_unsavedFolder` with
  `StringComparison.Ordinal`. TASK-0231 removed the final separator (`EngineHost.cs:47`) and kept the comparison with
  case. The review of v0.214 (E157) recommended `OrdinalIgnoreCase` on Windows and macOS too.
- **[Observed]** Probe `Probe_Unsaved_Folder_With_Another_Case`: a host with the unsaved folder `<temp>\Unsaved` made a
  journal. A second host opened that journal by its path in upper case, with the same unsaved folder:
  `File.Exists` gave true and `FileName` gave `6ae03635-e073-4830-bf67-5508b895768f.3de`. The same path with its own
  case gave `FileName` = null.
- **[Inferred]** The start again of V91 opens a journal of the unsaved folder by its path. A path that comes from a
  setting or a list with another case makes the journal a named document (ADR-0030 item 4).
- **[Recommended]** Compare with `OrdinalIgnoreCase` when the file system ignores case (Windows and macOS). Add a test
  with a path in another case.

#### E160 · Low · The reasons of DatumResolver for a feature that is not found name CreateBox and Extrude only, after ADR-0076 made the cylinder a feature · Confirmed by reading

- **[Observed]** `Engine.Core/Sketch/DatumResolver.cs:71`, `83`, `241` and `252` give "No CreateBox or Extrude in effect
  ... has the id ...". Since TASK-0238, `Form` and `Resolve` accept `CreateCylinderCommand` (`DatumResolver.cs:64` to
  `70`, `91`). The comment `Engine.Contracts/Sketch/DatumRecord.cs:3` says "FeatureId is a CreateBox or an Extrude in
  effect".
- **[Inferred]** An agent that reads the reason does not learn that a cylinder is a valid feature. Each later
  primitive of K5 adds one more false name.
- **[Recommended]** Say "No feature with faces (CreateBox, CreateCylinder or Extrude) in effect ...", or name no
  command. Correct the comment in the same change; a comment does not change the public shape.

#### E161 · Low · The probe of the phone holds no curve and no cylinder, so the hash of Android does not compare the polygon of ADR-0040 · Confirmed by reading

- **[Observed]** The log of `Engine.Core/Determinism/CrossPlatformProbe.cs:23` to `79` holds boxes, translates, a
  subtract, sketches of lines, extrudes, a datum on a side and references. It holds no circle, no arc and no
  `CreateCylinder`. The bits of the polygon of a curve have a fixed hash only in
  `Engine.Tests/Sketch/CurveProfileTests.cs:72` to `87`, which runs on the three desktop runners.
- **[Observed]** ADR-0076 Q3 says that the bits of the circle "are already tested on each runner". That is true for the
  three desktop runners only. Anti-objective 16 names Android on arm64.
- **[Inferred]** K5a makes the 64-gon a primitive of each document, so the phone replays it often. No comparison
  covers the bits of `SketchProfile.Turn` on Android. The pipeline compares hashes between platforms only, and no test
  pins the probe hash, so a new step in the log costs no migration.
- **[Recommended]** Add a circle extrude and a `CreateCylinder` with a translate and a cut to the log of the probe. Write
  the new hash in the ledger entry of that change.

#### E162 · High · A save to a new name releases the lock of the file during the move, so a second host can write a line that makes the document unreadable · Confirmed by reading

- **[Observed]** `DocumentJournal.MoveTo` (`Engine.Core/Persistence/DocumentJournal.cs:134` to `175`) flushes the
  stream, disposes it, moves the file with `File.Move`, and then opens the file again at its new name and seeks to
  its end. Between the dispose and the new open, no handle holds the file. `OpenFile` (`DocumentJournal.cs:186` to
  `187`) opens with `FileShare.Read`, so only an open handle stops a second writer.
- **[Observed]** After the new open, the journal appends at the end with no check of what is there. The bus writes
  the next version from the version in memory (`Engine.Core/CommandBus.cs:348`), and a read refuses a version that
  comes two times (`Engine.Core/Persistence/DocumentFile.cs:199` to `200`).
- **[Inferred]** A second host that opens the new name in that gap and appends one command, and then closes, leaves
  version 2 in the file. The first host then appends its own version 2. Both commands report success, and the open of
  the file fails. The gap is short, and on Linux and macOS the lock is advisory (ADR-0015 item 13), but the result is a
  document that does not open, which is a loss of data for the person.
- **[Observed]** The lead is finding 1 of the audit of Codex of the persistence and the determinism at `22143bd`. I read
  the cited lines; no probe ran the race.
- **[Recommended]** Keep a handle on the document during the whole save. Make the file at the new name with
  `FileMode.CreateNew` and no share for a write, copy the lines of the old file into it, flush it, and then delete the
  old file; the stream of the new file stays open. A test with a fake open function proves that no moment has no handle,
  and a test proves that a second open of the new name fails during the save.

#### E163 · Medium · A command with a null value of a required field is applied, and the document then does not open · Confirmed by reading

- **[Observed]** `NoOpCommandHandler` declares `echo` as required (`Engine.Core/Commands/NoOpCommandHandler.cs:20`), and
  `Handle` returns success with no check of the value (lines `53` to `58`). `EntryCodec` accepts a null value
  (`Engine.Core/Persistence/EntryCodec.cs:55`) and writes no field for it (line `83`). At the open, `ParameterBinder`
  refuses an entry with no required field (`Engine.Core/Hosting/ParameterBinder.cs:25` to `28`), and `EntryCodec` gives
  `E-IO-LOAD-FAILED` (lines `220` to `222`).
- **[Inferred]** A caller in the process, such as a test or a later agent of the desktop, can apply
  `new NoOpCommand { Echo = null! }`; the command is applied and written, and the next open of the document fails. The
  HTTP surface binds its parameters first, so it refuses a missing field. The same class can apply to each command with
  a required reference field whose handler does not check it.
- **[Observed]** The lead is finding 2 of the same audit of Codex. I read the cited lines; no probe ran it.
- **[Recommended]** The bus refuses a command whose encoded parameters lack a required field of its handler, before the
  handler runs, with the code of a missing parameter, so that each command has the same rule as a read.

**No finding.** I checked these items and found them correct.
K5a: `CreateCylinderCommandHandler.Circle` calls `SketchProfile.Turn` (`Engine.Core/Sketch/SketchProfile.cs:167` to
`198`), which uses `+ - * /`, `Math.Sqrt` and `Math.Max` only. The extrude goes from `-height / 2`, and `-h/2 + h` equals
`h/2` exactly, so the Manifold top and the datum frame of `+z` agree. The kernel names `+z`, `-z` and `side` with no
item (`Engine.Kernel/KernelSolid.cs:59` to `84`). `Form` refuses a datum on `side` and an `itemId`, and the reference
of a sketch refuses each element of a cylinder. A grep of `CreateBoxCommand` over `Engine.Core` gives seven places;
each one also names `CreateCylinderCommand` or does not apply to a cylinder (`BoxPoints`, `Box`, the edge reasons, and
the fixed probe log). `FeatureGraph` delegates to `FeatureList.Creates`. `DocumentKinds` gives a part only, so an
assembly refuses the command. No file of `Engine.Cli`, `Engine.Api.Http` or `Engine.Api.Http.Server` names the command.
The new diagnostic codes are 0. Probe `Probe_Cylinder_Replay_Undo_Redo`: a replay of the log gave the same bits of the
mesh as the live session, two undos left 0 bodies, and two redos gave the same bits and three faces. Probe
`Probe_Cylinder_Save_And_Open`: a host with a file wrote the line `CreateCylinder` with `radius` and `height`, and a
second host opened one body of the kind `Cylinder` with no warning. Probe `Probe_Extreme_Cylinders`: the radius 1e-300,
1e-200 (with the height 1e-200), 1e300, 1e-9 and 1e9, and the height 1e300, each gave 252 triangles and three faces;
the box of 1e-200 gave `E-GEOM-NATIVE-OP`, so the cylinder is not worse than the box.
R12a: `GetFacesQueryHandler.Entries` (`Engine.Core/Queries/GetFacesQueryHandler.cs:121` to `124`) takes each edge with
exactly one face of the tag. `Brep.Of` splits each edge at each vertex on it (step 2), merges the pieces of one plane
and one tag (step 3), and removes straight vertices (step 4), so a hole and a split face give their edges, and an edge
between two facets of a curve is inside. Probe `Probe_Split_Face_Outline`: a plate of 10 x 10 x 2 cut by a bar of
2 x 20 x 4 gave `+z` with 2 parts and 8 segments, and `+x` with 1 part and 4 segments. `FaceEntry` is in
`Engine.Core`, so `Engine.Contracts` did not change.
E156 and E157: the tests of TASK-0231 (`ContainerModelTests.cs:349` to `360`, `DocumentEndpointTests.cs:90` to `102`)
cover the command line and the final separator. TASK-0233: `CrossPlatformProbe` calls no function of the determinism
rules, and `DeterminismCallGateTests` passes. The ledger entries v0.215 to v0.223 give the true counts of the test
floor (`eng/test-floor.txt` at each merge) and the true count of 4 product milestones and 9 entries after v0.214.

**Not verified.** I did not build or run the APK, and I did not run the probe of the phone. I did not check the
bits of `SketchProfile.Turn` on arm64. I did not read the desktop callers of `DocumentFile.Check`.

### The desktop and the phone

#### V104 · Medium · A finger that Android cancels stays down in the intent layer, so the phone takes no tap after it · Confirmed by probe

- **[Observed]** `SdlInput.Translate` reads `SDL_EVENT_FINGER_DOWN`, `SDL_EVENT_FINGER_MOTION` and `SDL_EVENT_FINGER_UP`
  only (`3DEngine.Vulkan/SdlInput.cs:54-67`). The binding has `SDL_EVENT_FINGER_CANCELED`, and the SDL of the APK
  (3.5.0) holds the name of that event (a Node.js scan of `libSDL3.so` and `SDL3-CS.dll` of ppy.SDL3-CS 2026.1002.1).
  The intent layer ends a contact on `PointerPhase.Cancel` with `Lose` (`3DEngine.Core/Input/IntentMachine.cs:210`,
  `:272`, `:319-328`), but no code makes that phase from SDL.
- **[Observed]** Probe: a test in `3DEngine.Tests` gave `SdlInput` a finger down and then `SDL_EVENT_FINGER_CANCELED`
  for the same finger, and then two taps of a new finger, 2 s apart, through `IntentMachine.Step`. Result: the cancel
  gave 0 samples, and each of the two later taps gave 1 sample and 0 intents. Without the cancel, the same tap gives a
  `SelectIntent`.
- **[Inferred]** SDL 3 sends `SDL_EVENT_FINGER_CANCELED` for `ACTION_CANCEL` of Android, as for a gesture that the
  system takes (the back gesture from the edge, the notification shade) or a palm. I did not read that code of SDL on
  this disk. If that is true, after one such gesture each later finger is a second contact, so a tap, a long press and
  an orbit of one finger give nothing, until the program goes into the background and makes a new engine. The mouse
  has no such state, so this breaks objective 9 on the phone. The defect is older than this range (TASK-0082,
  TASK-0195); the phone makes it visible.
- **[Recommended]** Translate `SDL_EVENT_FINGER_CANCELED` into a touch sample with `PointerPhase.Cancel`, and add its
  type to the row of `A_Later_Session_Flushes_Each_Input_Event_That_SdlInput_Reads`. Make the probe a test of
  `SdlInputTests`: a cancelled finger and then a tap give one `SelectIntent`. Add a row to the test sheet: a back
  gesture that the person stops, then a tap on a body.

#### V105 · Medium · On a freshly booted emulator, a new start of the activity makes SDL end the process with exit 0 and no line · Inferred by reading

- **[Observed]** The ledger entry v0.222: the process started at 17:54:33.777, the launcher showed for the first time at
  17:54:33.811, and the runtime wrote "VM exiting with result code 0" at 17:54:35.650, with no line of the program.
  The smoke test waits for `sys.boot_completed` and then runs `am start -W` (`eng/android/smoke-emulator.sh:30-46`).
- **[Observed]** The class `SDLMain` of the SDL archive of the package calls `nativeInitMainThread`, `main`,
  `nativeCleanupMainThread`, and then `finish` of the activity. The class `SDLActivity` holds the strings
  `SDL main() finished`, `activity re-created`, `activity finished`, `nativeAllowRecreateActivity` and a call of
  `exit` (a Node.js extract of `SDL3-CS.aar`; its entry uses the compression number 65535, which is raw deflate).
  `libSDL3.so` holds the hint `SDL_ANDROID_ALLOW_RECREATE_ACTIVITY`. `PhoneHost` sets no such hint
  (`3DEngine.Android/PhoneHost.cs:41-43`).
- **[Inferred]** The order of these strings is the order of `SDLActivity.onCreate` of SDL 3: when an activity was
  created before in the same process, and the hint does not permit a new one, SDL writes "activity finished" and calls
  `System.exit(0)`. That gives "VM exiting with result code 0". Android starts the activity again for a change of the
  configuration that `ConfigurationChanges` does not name (`3DEngine.Android/MainActivity.cs:13-16` names eight; the
  density, the font scale, the locale, the layout direction, the colour mode and the touch screen are not there), and
  always for a change of the paths of the assets, as when the system applies an overlay after the boot. SDL starts its
  main thread only with the surface and the focus, so a launcher that takes the focus at the start also explains no
  line before the exit.
- **[Inferred]** The most likely causes of an exception between the probe and the first frame, in order, are:
  1. The activity pauses while the probe runs on the SDL thread. `SDL_CreateWindow` then gets no native window ("Could
     not fetch native window" is in `libSDL3.so`), and `Window` throws (`3DEngine.Vulkan/Window.cs:68-72`).
  2. The surface goes before `GraphicsDevice` makes the Vulkan surface. `SDL_Vulkan_CreateSurface` fails ("Android
     native window is not available for surface creation, usually because of backgrounding" is in `libSDL3.so`), and
     `Window.CreateSurface` throws (`Window.cs:203-209`).
  3. The surface goes after `Initialize` and before the first poll. Then
     `vkGetPhysicalDeviceSurfaceCapabilitiesKHR(...).CheckResult()` throws (`3DEngine.Vulkan/Presenter.cs:171`).
  Each of these goes to the `catch` of `Run`, which writes "the session of the phone ended" (`PhoneHost.cs:47-55`). A
  failure with no such line is the `System.exit(0)` above, not an exception.
- **[Recommended]** Write a line with the prefix `3DEngine:` in `OnCreate`, `OnDestroy` and `OnConfigurationChanged` of
  `MainActivity`, so that the next failure names a new start of the activity. Add the missing configuration changes
  to `ConfigurationChanges`. In the smoke test, wait until the launcher is the resumed activity before `am start`, and
  give the lines of the tags `SDL` and `ActivityTaskManager` (the agent of `eng/` judges the script). Do not set
  `SDL_ANDROID_ALLOW_RECREATE_ACTIVITY` until `PhoneHost.Run` can run two times in one process.

#### V106 · Low · The line "the first frame is drawn" comes also when RenderFrame draws nothing · Confirmed by reading

- **[Observed]** The lead of the review of Codex of v0.214 is true at `3379675`. `Presenter.RenderFrame` returns with no
  draw for a surface with no area, a failed remake, `ErrorOutOfDateKHR` at the acquire, and each other failed acquire
  (`3DEngine.Vulkan/Presenter.cs:90-112`). It returns `void`, and `NativeThreeDEngine.Render` gives no result either
  (`3DEngine.App/NativeThreeDEngine.cs:499-591`). `PhoneHost` writes the line after the first `Render`, with no
  condition (`3DEngine.Android/PhoneHost.cs:102-108`). The loop also renders after `WaitForEvent(100)` while the
  window has no area (`PhoneHost.cs:84-85`).
- **[Inferred]** The smoke test reads the line as the proof of a frame. A start where the swapchain is out of date at the
  first frame gives the line and no picture. The ledger entries v0.214, v0.217 and v0.218 state the line as "the line of
  the first frame"; that is a statement about a call, not about a picture.
- **[Recommended]** Make `RenderFrame` return true after a present, give that result from `Render`, and write the line
  at the first true. A test of `Presenter` is not possible with no GPU, so a test of the loop with a fake engine, or the
  text of the line ("the first frame is submitted"), must hold the rule.

#### V107 · Low · The outline of the selected face stays in place during a drag of the gizmo, and the face goes at the release · Confirmed by reading

- **[Observed]** The lead of the review of Codex of v0.221 (finding 3) is true. `GizmoActions.Follow` gives the offset to
  the renderer only (`3DEngine.App/GizmoActions.cs:82-89`), and `FaceOverlay.Of` gets the outline of `GetFaces` with no
  offset (`3DEngine.App/NativeThreeDEngine.cs:568-571`). The box of the dimension overlay comes from
  `SceneRenderer.BoxOf`, which holds the preview (`NativeThreeDEngine.cs:559-560`), so the dimensions follow the drag
  and the outline does not.
- **[Observed]** At the release, `BodyActions.Move` sets the new body as the selection, and the setter clears the face
  (`3DEngine.App/BodyActions.cs:22-30`, `:117-123`).
- **[Inferred]** During a drag the person sees the outline at the old place, over empty space, and after the release
  the face is gone. ADR-0075 item 2 says "each change of the selected body clears the face", so the clear is in the
  decision; the outline in the wrong place is not.
- **[Recommended]** Add `_gizmo.Offset` to each point of the outline while `_gizmo.Active` is not null for the selected
  body, or hide the outline during a drag. Decide in ADR-0075 whether a move keeps the face by its name, and test it.

#### V108 · Low · A lost pointer clears the selected face, because CancelIntent now has two meanings · Confirmed by reading

- **[Observed]** `Lose` gives `CancelIntent` when a contact or a mouse button is down, for a loss of the focus and for a
  pointer of the phase Cancel (`3DEngine.Core/Input/IntentMachine.cs:319-328`). Since P0.94 the key Escape gives the
  same intent (`IntentMachine.cs:299-300`). `BodyActions` clears the face for each `CancelIntent`
  (`3DEngine.App/BodyActions.cs:65-68`). The comment of the record says both meanings (`3DEngine.Core/Input/Intents.cs:26`).
- **[Inferred]** A loss of the focus during a press (a dialog of the system, Alt and Tab with a button down) clears the
  face, and the person must tap it again. The cost is small. The larger cost is the design: a later consumer of
  `CancelIntent`, for example an end of a sketch tool on Escape, also acts on a lost pointer. After V104 is corrected,
  each gesture that Android cancels also clears the face.
- **[Recommended]** Give Escape an intent of its own, for example `BackIntent`, and keep `CancelIntent` for a lost
  pointer. Add a row to `IntentParityTests`: a loss of the focus during a press gives no `BackIntent`.

#### V109 · Low · With a face selected, each frame waits for the serial section, so a long command of an agent stops the drawing · Confirmed by reading

- **[Observed]** `Render` calls `BodyActions.SelectedFace` in each frame (`3DEngine.App/NativeThreeDEngine.cs:568-571`).
  It runs `GetFaces` through `DocumentSession.Query`, which waits on the serial semaphore
  (`3DEngine.App/BodyActions.cs:127-136`, `Engine.Core/DocumentSession.cs:142-156`). The handler gives its cached result
  for the geometry key of the body, after a scan of the bodies (`Engine.Core/Queries/GetFacesQueryHandler.cs:56-69`).
  The first frame after each rebuild builds the B-Rep in rational numbers. `SketchOnSelectedFace` asks a second time
  in the frame of the button (`NativeThreeDEngine.cs:237-245`).
- **[Observed]** A stale face does not survive. An undo or a translate that removes the body makes `Forget` clear the
  selection, and the setter clears the face (`BodyActions.cs:179-183`, `:22-30`). A face name that the body no longer has
  clears the face (`:133-134`). The test `The_Selected_Face_Has_Its_Entry_Until_Its_Body_Is_Gone` holds the translate
  of an agent.
- **[Inferred]** The cost in a still frame is small: one wait, one lookup and one scan of the faces. The defect is the
  wait. While an agent runs a long Subtract, the window of the person stops for that time, only because a face is
  selected. The hover of the mode Sketch has the same wait (`SketchActions.Current`, `3DEngine.App/SketchActions.cs:87-88`,
  `:103-106`), so the pattern is older. Finding V87 and ADR-0056 item 6 chose one query for each change of a body for
  `GetBodyInfo` (`NativeThreeDEngine.cs:83-92`).
- **[Recommended]** Keep the entry with the body and its geometry key, which the renderer already holds for each live
  body, and ask `GetFaces` again only when the key changes, as `SizeOf` does. Pass the entry of the frame to
  `SketchOnSelectedFace`.

#### V110 · Low · Two comments in PhoneHost and in PhoneLifecycle give a default of SDL and an order of events of the background that the code does not prove · Confirmed by reading

- **[Observed]** The comment of `BlockOnPause` states as a fact that "SDL blocks the thread of the program in its event
  loop while the program is paused, so the loop sees WILL_ENTER_BACKGROUND only at the return". It also says that "SDL
  starts its video in SDL_CreateWindow, so the hint comes before the window" (`3DEngine.Android/PhoneHost.cs:58-61`).
  The header of `PhoneLifecycle` and the comment of `PhoneStep.Background` state the same order with no condition
  (`3DEngine.App/PhoneLifecycle.cs:8-10`, `:16`). ADR-0073 item 3 says the same
  (`docs/adr/0073-the-phone-cuts-and-keeps-its-document.md:70-71`).
- **[Observed]** The ledger entry v0.218 says "V101 has no run: the test of the owner shows the order of the events of
  the background" (`docs/CURRENT-STATE.md:6541`). No test reads the order of the events, and no code in
  `3DEngine.Android`, `3DEngine.Vulkan` or `3DEngine.App` calls `SDL_Init` or `SDL_InitSubSystem` before the window. The
  claim about the start of the video therefore rests on the source of SDL 3.5.0, which is not on this disk.
- **[Inferred]** The comments are probably true: the hint has the name of that behaviour, and finding V101 inferred the
  same order. But a comment that states an unproven behaviour of a library as a fact is the kind of text that the rule
  "Read a file before you cite it" warns against. If the order differs on the S23 Ultra, for example if SDL delivers
  `WILL_ENTER_BACKGROUND` before it blocks, the comments lead the next session to a false cause. A later SDL can also
  start its video in another call.
- **[Recommended]** Say in the two comments and in ADR-0073 item 3 that the order is expected and not yet seen, and name
  step 8 of the test sheet as the proof. Write the type of each event of the life cycle to the log with the prefix
  `3DEngine:`, so that the test of the owner shows the order. Remove the condition from the comments after that test.

#### V111 · Low · ADR-0075 and TASK-0237 do not name each other, and a comment of BodyActions describes the wrong method · Confirmed by reading

- **[Observed]** The affects field of ADR-0075 names `GetFacesQuery.cs`, `GetFacesQueryHandler.cs`, `BodyActions.cs` and
  `FaceOverlay.cs` (`docs/adr/0075-a-second-tap-selects-the-face.md:11-15`). Its items 2 and 3 also decide
  `IntentMachine.cs` (Escape), `FaceArm.cs`, `NativeThreeDEngine.cs`, `DesktopUi.Panel.cs`, `DesktopUi.Overlay.cs` and
  `PanelAction.cs`. TASK-0237 corrects item 2 of ADR-0075 (Escape), and its governed-by field does not hold 0075
  (`tasks/TASK-0237-escape-and-the-arm-after-a-face.md:8`). ADR-0075 has no History line for TASK-0237
  (`docs/adr/0075-a-second-tap-selects-the-face.md:106-110`).
- **[Observed]** In `BodyActions`, the comment "ADR-0049 items 7 and 8: the distance in effect of the extrude …" now
  stands above `FaceAt`, followed by the comment of `FaceAt`; `Distance` has no comment
  (`3DEngine.App/BodyActions.cs:149-166`).
- **[Inferred]** This is V102 again: the gate is content with ADR-0072 (`3DEngine.App/**`), so a later task that changes
  the gesture of a face does not read ADR-0075.
- **[Recommended]** Add the six paths to the affects field of ADR-0075, with a History line for TASK-0236 (the panel)
  and TASK-0237 (Escape, the arm). Move the comment of `Distance` back above `Distance`.

**No finding.** **[Observed]** The claim of R15 holds. A Node.js script compared `DesktopUi.cs` at `90b3625^` (1365
lines) with the six files at `90b3625`. As a multiset of lines, the only line of the old file that is not in the new
files is the class line; the new files add only usings, namespace lines, class lines, braces, blank lines and the
header comments of R15. In order, each new file is a sequence of contiguous runs of the old file (for example
`DesktopUi.Panel.cs` 11-204 = old 615-808 and 205-244 = old 1261-1300); the one-line run is `ClosePad`, old line 308.
So each comment stays with its member. The fixes of V97 to V101 hold: `RunSessions` reads the path after the `using`
of the session (`3DEngine/DesktopEngineHost.cs:96-113`), and `DocumentSession.FilePath` reads the journal with no
check of a closed session; the list "Cut with" and Show all start a new line when they do not fit, and the open list is
a rectangle of the panels; the phone gets a `DocumentLine` with `Files: false`, which shows the message only;
`FanOutSink.Announce` runs the report on the thread pool with `SuppressFlow` and a `try`; `PhoneHost` sets the hint
before the first `SDL_CreateWindow`. `Log.WriteColored` writes no colour on Android. The probe of the phone uses its
own backend in a `using`. The second tap: a hit on the selected body asks `GetFaceAt`, a hit elsewhere changes the body
and clears the face, the row of a body clears the face, a hidden body clears the selection. The button "Sketch on face"
shows only for an entry with a normal, ends the arm of Face (`3DEngine.App/FaceArm.cs:31`), and makes the sketch from
the point and the normal of the entry through the same `CreateOnFace` as the armed tap. The touch and the mouse give
the same face (`Two_Taps_Of_A_Finger_And_Two_Clicks_Give_The_Same_Face`). The overlay sends no command (ADR-0007), and
the client reads the face through a query only (ADR-0009). Escape while a text field or the pad is open goes to the
panel (`3DEngine.App/Ui/DesktopUi.cs:378`, `:498`). The ledger entries v0.215 to v0.223 agree with the code for the
desktop and the phone; v0.222 corrects the false statement of v0.221 about Escape. `dotnet test 3DEngine.Tests` gave
335 passed and `dotnet test 3DEngine.ImageTests` gave 230 passed, 0 failed, at `3379675` before the probe.

**Not verified.** No device and no emulator ran the APK, and nobody started the desktop, so no outline of a face, no
Escape and no "Sketch on face" was seen. The source of SDL 3.5.0 is not on this disk: that Android maps `ACTION_CANCEL`
to `SDL_EVENT_FINGER_CANCELED` (V104), the exact order of `SDLActivity.onCreate`, and the default of
`SDL_ANDROID_ALLOW_RECREATE_ACTIVITY` (V105). The configuration change, if any, on the emulator of run 42265352847: no
log of a job is public. The time of the probe on the emulator. Linux and macOS.

### The gates and the pipeline

#### T152 · Medium · A failure of the emulator job skips the comparison of the three desktop hashes, and seven runs of this range compared nothing · Confirmed by the public API

- **[Observed]** `.github/workflows/ci.yml:188` gives the job "Compare the determinism hashes" `needs: [gate,
  android-emulator]` and no `if:`. A job whose `needs` failed is skipped. `CrossPlatformHashTests` pins no hash
  (`Engine.Tests/ReplayDeterminism/CrossPlatformHashTests.cs:38` to `60`), so this job is the only comparison between
  platforms.
- **[Observed]** `api.github.com`: the job "Start the APK on the emulator" failed, and "Compare the determinism hashes"
  was skipped, in the runs 42166346364, 42168252840 (main, v0.217), 42168890096, 42170647555, 42172903090, 42265352847
  and 42270121672 (main, v0.222). In each of them but 42265352847, the three gate jobs passed and wrote their hashes.
- **[Inferred]** The emulator job failed in 7 of the 29 finished runs of the range with no change of the phone. On each
  such run a difference between Windows, Linux and macOS goes unseen. ADR-0074 says "a difference stops the merge"; a
  flaky job now hides it.
- **[Recommended]** Give the job `if: always() && needs.gate.result == 'success'`. Compare the three desktop files
  always, and compare `android-x64` when its artifact is present; give an error for its absence only when the emulator
  job passed. Prove it with a run whose emulator job fails on purpose.

#### T153 · Medium · The smoke test passes when the process of the program dies by a native signal after the line of the start · Confirmed by a probe

- **[Observed]** `eng/android/smoke-emulator.sh:74` to `79` fails on the line "the session of the phone ended" and on
  `FATAL EXCEPTION` with `Process: org.threedengine.phone` in the next three lines. A native crash writes
  `F libc : Fatal signal 11 (SIGSEGV) ... (threedengine.phone)` and a tombstone of `DEBUG`, and no `FATAL EXCEPTION`.
  The managed `catch` of `PhoneHost` does not run, so no line "ended" comes.
- **[Observed]** Probe: a fake `adb` in the scratchpad whose `logcat -d` gives the line of the start, the hash, one
  `determinism-line`, a `Fatal signal 11` of the process, a `DEBUG` line `>>> org.threedengine.phone <<<` and
  `ActivityManager: Process org.threedengine.phone (pid 3426) has died`. `bash eng/android/smoke-emulator.sh x.apk
  report.txt "" out/hash.txt` waited 120 seconds, printed `started=1 drawn=0`, the notice of the hash and a `::warning`,
  and gave exit 0.
- **[Observed]** The review of v0.214 (T149) recommended a failure on "a native crash of the package". The ledger v0.217
  says that the test fails "on a fatal exception of the process of the program"; a signal is such an end, and the
  script does not see it.
- **[Inferred]** A crash in `libmanifold.so`, `libSDL3.so` or the Vulkan driver after the probe passes the job. The job
  then proves only the start and the hash.
- **[Recommended]** Fail on `Fatal signal` or a `DEBUG` line that names `org.threedengine.phone`, and on an
  `ActivityManager` line "Process org.threedengine.phone ... has died" when no first frame came. Prove it with this
  fake `adb`.

#### T154 · Medium · The smoke test writes more annotations than GitHub keeps, so the cause of a failure is dropped · Confirmed by the public API

- **[Observed]** Run 42270121672, job 128073153660, step 7 "Run the smoke test": the API gives exactly 10 notices
  (7 `am start`, then 3 `Program`: the start, the hash and the first `determinism-line`) and exactly 10 warnings (1
  `Android` and 9 of the 12 `Emulator` lines). The step failed with "the session of the phone ended with an exception".
  The line "ended" with its exception came after about 25 `determinism-line` lines, so no annotation holds it. No line
  `adb devices` came either.
- **[Observed]** The script spends the notices first on the output of `am start` (`smoke-emulator.sh:46` to `47`), then
  on up to 60 lines of the program in the order of the log (`smoke-emulator.sh:60`). The warnings go first to up to 12
  `FATAL|AndroidRuntime` lines (line `61`), then to `Package`, `Process` and up to 40 lines of the broad filter
  (lines `65` to `68`), and last to the tail of the emulator and to `adb devices` in `fail` (lines `23` to `26`). The broad
  filter matches each line with `SDL`, `ActivityManager: Start`, `DOTNET` and others, so `head -40` keeps the first lines
  in time and not the cause.
- **[Observed]** The ledger v0.222 says that P0.93 "now gives a cause". In run 42270121672 it did not. The report file
  is an artifact, and an artifact needs a sign-in, which the owner's tools do not have.
- **[Inferred]** The limit is per step and per level, so each later line is lost, whatever its value. The job failed
  7 times in this range, and the cause of the last failure is unknown.
- **[Recommended]** Put the cause first and in one annotation: on a failure, write one `::error` whose message holds the
  line "ended" and the lines after it, joined by `%0A`. Do not write the `determinism-line` lines as notices. Keep each
  level under 10 lines in each step, or write the rest to `$GITHUB_STEP_SUMMARY`. A fix is in flight on the branch
  `p095-smoke-exception` (run 42274351754); I did not read it.

#### T155 · Low · An MSBuild escape or a property in the Update of a pinned package passes each gate for Engine.Geometry.Manifold.Native and OpenCvSharp4 · Confirmed by injection

- **[Observed]** `Each_Pinned_Package_Is_Named_In_The_File_Of_Its_Pin_Only` (`Engine.Tests/Governance/DependencyDirectionGateTests.cs:1417`)
  compares the literal value of `Include` and `Update` with the name of the package. `ReferenceOffences`
  (`DependencyDirectionGateTests.cs:858` to `865`) refuses `%`, `$(`, `@(` and `%(` in `Include` only, not in `Update`.
- **[Observed]** Injection 1: `Engine.Geometry.Manifold/Directory.Build.targets` with
  `<PackageReference Update="Engine.Geometry.Manifold%2ENative" Version="3.5.2.1" />`. `dotnet msbuild
  Engine.Geometry.Manifold/Engine.Geometry.Manifold.csproj -getItem:PackageReference` gave the version `3.5.2.1`, not the
  `3.5.2.2` of the project file. The governance tests gave 483 passed and 0 failed, and `eng/repo-gates` gave
  "467 runs of 160 checks, 0 failed".
- **[Observed]** Injection 2, in the same run: `3DEngine.VisionTests/Directory.Build.targets` with
  `Update="OpenCvSharp%34" Version="4.11.0.20250507"`, and the Manifold file with `<NativePin>` as a property and
  `Update="$(NativePin)"`. MSBuild gave `OpenCvSharp4` 4.11.0.20250507 and the native package 3.5.2.1. Each gate passed.
- **[Observed]** The same escape in `3DEngine.Vulkan` for `ppy.SDL3-CS` fails
  `Each_Project_Of_The_Desktop_Names_Only_The_Packages_Of_Its_List`, because the literal `ppy.SDL3%2DCS` is not in the
  list. The engine and the vision projects have no such list. Each probe file is removed.
- **[Observed]** An escape in the name of the attribute, such as `Upd%61te`, is not a valid XML name, so MSBuild and the
  gate both refuse the file. The lead of Codex of v0.217 is true for the value only.
- **[Inferred]** The pin of the native backend decides the bits of each replay (ADR-0017 rule 4, anti-objective 16). I
  did not restore 3.5.2.1, which `nuget/` does not hold, so the restore can fail for that version; OpenCvSharp4 comes
  from nuget.org.
- **[Recommended]** Decode `%XX` (as `IsItemName` does) and refuse `$(`, `@(` and `%(` in `Update` and `Remove`, as in
  `Include`. Or compare the version that `dotnet msbuild -getItem:PackageReference` gives for each project with the
  version of its pin in a pipeline step. Prove the change with these three injections.

#### T156 · Low · An item SupportedPlatform Remove="android" silences CA1416 in a project of the phone, and each gate passes · Confirmed by injection

- **[Observed]** `Each_Project_That_The_Phone_Reaches_Supports_Android` (`DependencyDirectionGateTests.cs:601` to `627`)
  reads the `Include` of each `SupportedPlatform`. It does not read `Remove`. `PermittedItems` (line `593`) permits the
  item with each attribute.
- **[Observed]** Injection: `Engine.Core/Directory.Build.targets` with `<SupportedPlatform Remove="android" />`.
  `dotnet msbuild Engine.Core/Engine.Core.csproj -getItem:SupportedPlatform` gave Linux, macOS and Windows only. The
  governance tests gave 483 passed and `eng/repo-gates` gave 0 failed. With the probe file
  `Engine.Core/Rev26AndroidProbe.cs`, which sets `Console.ForegroundColor`, `dotnet build Engine.Core --no-incremental`
  gave 0 warnings. Without the `Directory.Build.targets` the same build gave `error CA1416`. Both files are removed.
- **[Inferred]** The rule came from the third run of the emulator, which ended on `Console.ForegroundColor` (TASK-0229).
  A `Remove` brings that failure back with green gates.
- **[Recommended]** Evaluate the item with `dotnet msbuild -getItem:SupportedPlatform` in a pipeline step, or refuse a
  `SupportedPlatform` with `Remove` or `Update` in the gate. Prove it with this injection.

#### T157 · Low · The smoke test exits 1 with no error line when the program writes the hash and no line of the probe · Confirmed by a probe

- **[Observed]** `eng/android/smoke-emulator.sh:84` runs `{ echo "$hash"; grep -o "3DEngine: determinism-line .*" ... |
  sed ... | tr -d '\r'; } > "$hash_file"` under `set -euo pipefail` (line `13`). With no match, `grep` gives 1, the
  pipeline gives 1, and the shell exits.
- **[Observed]** Probe: a fake `adb` whose log gives the start, the hash and the first frame, and no `determinism-line`.
  The script printed `started=1 drawn=1` and gave exit 1, with no `::error`, no notice of the hash, and no call of
  `fail`. The hash file held the hash only.
- **[Observed]** The ledger v0.217, section "What stays", records this and gives it to this review.
- **[Inferred]** The program writes the lines today, so the case needs a change of `PhoneHost` or a loss in the buffer
  of logcat. A person then sees a failed step with no reason.
- **[Recommended]** End the `grep` with `|| true` inside the group, or call `fail "the program wrote the hash and no line
  of the probe"` when the file has one line only.

#### T158 · Low · Six calls of adb in the smoke test have no time limit, so a hung adb ends the job at 40 minutes with no reason · Confirmed by reading

- **[Observed]** `eng/android/smoke-emulator.sh` limits `wait-for-device`, `install`, `am start` and `logcat -d`
  (lines `30`, `42`, `46` and `53`). It does not limit `adb devices -l` in `fail` (line `26`), `getprop sys.boot_completed`
  (line `33`), `getprop ro.build.version.release` (line `40`), `logcat -c` (line `43`), `pm list packages` (line `65`) and
  `pidof` (line `66`). The job has `timeout-minutes: 40` (`.github/workflows/ci.yml:265`).
- **[Inferred]** The calls of lines `65` and `66` run only on a failure, when the device is most likely in a bad state. A
  hang there replaces the reason of `fail` with a cancelled job. In `fail`, the call `adb devices` comes after the
  `::error` line, so a hang there keeps the reason.
- **[Recommended]** Give each call `timeout 30`. A fix is in flight on another branch; at `3379675` the six calls have no
  limit.

#### T159 · Low · The ledger counts of the failures of the emulator job are false, and three entries are silent about a failed run · Confirmed by the public API

- **[Observed]** The ledger v0.222 (`docs/CURRENT-STATE.md:6655`) says that the emulator job of run 42265352847 "failed a
  fourth time". Before it, the job had failed in 42166346364, 42168252840, 42168890096, 42170647555 and 42172903090, so
  42265352847 was the sixth failure.
- **[Observed]** The entry v0.219 (line `6564`) cites run 42169584195, which passed; run 42170647555 of the close of R15
  failed in the emulator job. The entry v0.221 (line `6613`) cites run 42173547034; run 42172903090 of the same branch
  failed in the emulator job. The entry v0.223 (line `6685`) does not say that run 42270121672 of `main` at v0.222 failed
  in the emulator job, and that run 42270395707 of the first K5a branch failed the write-set job.
- **[Inferred]** The owner reads the ledger to judge if the emulator job is flaky. The count in the ledger is less than
  half of the true count, and the true rate in this range is 7 of 29 finished runs.
- **[Recommended]** Add a correction in the next ledger entry with the seven run numbers. Name each failed run of a
  branch or of `main` in the entry of its task, also when a later run passed.

**No finding.** I checked these items and found them correct. T147: `ProductPhase` (`Engine.Tests/Governance/CodebaseReviewGateTests.cs:303`
to `304`) accepts `R14f2`, `P8b1` and `K5a` and refuses `P0.91`; the theory has ten rows. T148: the injection of the
review of v0.214 (an `Update` of `ppy.SDL3-CS` in `3DEngine.Vulkan`) fails the new rule, by the task file. T149: the
lines "ended" and `FATAL EXCEPTION` of the process fail the script. T150: `eng/android/check-apk.sh:26` to `47` maps
`arm64-v8a` to `android-arm64` and `x86_64` to `android-x64`, adds `Engine.Native.DearImGui/` for `libcimgui.so`, and
the six rows of `THIRD-PARTY-NOTICES.md:95` to `107` have that form. `eng/compare-determinism.js` reads four folders and
refuses a first line that is not 64 hexadecimal digits. The upload name `determinism-android-x64` matches the download
pattern `determinism-*`. `DispatchSurfaceGateTests` reads only the host projects, so the exception of ADR-0076 item 6
needs no change of a gate. Lead 3 is T157; lead 2 is T154 and T158; lead 4 is T154. After the probes,
`dotnet build 3DEngine.sln` gave 0 warnings and 0 errors, and `git status --short` is empty.

**Not verified.** I did not read a log of a job or an artifact, because both need a sign-in. I did not run the
emulator. I did not read the branch `p095-smoke-exception`. I did not restore the injected versions of T155. I did not
check if a `Remove` of a `SupportedPlatform` in `Directory.Build.props` (before the project body) has an effect; the
injection of T156 used `Directory.Build.targets`.

## 4 The next planned task

The next product phase is K5b: the button of the cylinder on the desktop and on the phone, with a pad of two values
(ADR-0076, "Next"). E162 is high, so a correction comes before K5b: P0.96 (TASK-0241) holds E162. By `docs/templates.md`,
section 7, the medium and the low findings go after K5b: P0.97 (TASK-0242) holds E158 to E161, E163, T152, T153 and
T155 to T159, and P0.98 (TASK-0243) holds V104 to V111. T154 is fixed at v0.224 (TASK-0239).

## 5 Research for that task

**[Observed]** K5b adds a command to the panel, as the box of ADR-0032 item 4: `BodyActions.CreateBox` sends the
command, and the pad of R10 takes the values. **[Inferred]** V104 touches K5b on the phone: after a gesture that the
system takes, the phone takes no tap, so a person cannot press the new button until the program goes into the
background. **[Observed]** E162 touches each save of a document, so it comes first.

## 6 Recommended approach

**[Recommended]** In this order:

1. P0.96 (TASK-0241): E162, with a test that no moment of a save has no handle on the document. 1 agent hour.
2. K5b: the button of the cylinder on the desktop and on the phone.
3. P0.97 (TASK-0242): E158 to E161, E163, T152, T153 and T155 to T159, each with a test or an injection before the
   change. 2 agent hours.
4. P0.98 (TASK-0243): V104 to V111, each with a test or an injection before the change. 2 agent hours.
5. The test of the owner on the S23 Ultra.

## Questions for the owner

The standing order of 2026-11-05 answers each question with the recommended option. The owner checks these answers on
2026-11-08.

**Q1. The save to a new name (E162).**
- A: The journal makes the new file with no share for a write, copies the lines into it, and then deletes the old file.
  A handle holds the document at each moment.
- B: The journal opens its file with `FileShare.Delete`, so the move runs while the stream stays open.

**Recommended: A.** B lets a second host delete or rename the open document at any moment, which ADR-0015 item 13 does
not permit on Windows. A copies the file one time for each save to a new name, and a document is small.
