---
date: 2026-11-05
commit: d9292fa
ledger: v0.190
previous: 2026-11-05-codebase-review.md
---

# Codebase review — 2026-11-05 (b)

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the twenty-second codebase review. The review of 2026-11-05 examined v0.181. After it came 4 product
milestones (K4a, K4b, K4c and P8h) and 9 ledger entries (v0.182 to v0.190). The limits are more than four and more
than twelve, so the gate does not make this review due yet: on 2026-11-05 the owner gave the order "fix R-0043 first,
then the review". `Engine.Tests/Governance/CodebaseReviewGateTests.cs` holds the rule, and `docs/templates.md`,
section 7, gives the form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is the
authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this file does not.

| Item | Value |
|---|---|
| Code state | `main` at `d9292fa`, the merge of v0.190 |
| Branch of this review | `codebase-review-2026-11-05-b` |
| Tests | 1,767 on this computer (1,257 in `Engine.Tests`, 312 in `3DEngine.Tests`, 195 in `3DEngine.ImageTests`, 3 in `3DEngine.VisionTests`) |
| Pipeline | The run on `d9292fa` (42112013571): green on Ubuntu, Windows and macOS, and the three determinism hashes are equal |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner decides.

**Method.** Eight tasks changed code or records after `3cd76af`, the commit of the last review: TASK-0180 (the review),
TASK-0181 (P0.78), TASK-0182 (P0.79), TASK-0183 to TASK-0185 (K4a to K4c), TASK-0187 (P0.80), TASK-0186 (P8h) and
TASK-0188 (P0.81). The change is about 3,460 lines in 63 C# files: the owned kernel and its boolean, the faces of a
body by name, the unit of each number parameter, a gate of the ADR numbers, the place of a mouse press, and the fixes of
the last review. Two review agents worked in parallel at `d9292fa`, each one in its own git worktree: the engine with
the hosts, and the desktop with the gates. Each agent wrote probes and injections that only its worktree held, and
restored or deleted them. I read the cited lines of the new findings of the levels high and medium (E134, E135, V85,
V86 and T139), and I measured the values below.

## Summary

- **The fixes of the last review hold for their forms.** E131 to E133, V78 to V83 and T133 to T135 are fixed for their
  forms, and V84 is fixed in part. A probe found the next form of V78, T133, T134 and T135. **[Observed]**
- **An older document can fail to open (E134, high).** Since K4c a new datum on a face that a cut removed is refused,
  and the refusal also acts in the replay of a log that a client of v0.186 or earlier wrote. **[Observed]** by a probe.
- **The kept kernel solids are static for the process (E135, medium).** Two sessions in one process with the same
  command identifier read the solid of the other session. **[Observed]** by a probe.
- **The fix of R-0043 moves a click of a hand that moves on (V85, medium).** A press now takes the place of the cursor
  at the poll, not at the press. **[Observed]** by a probe on `SdlInput`.
- **The far plane ignores the images (V86, medium)**, and **a package in the owned kernel passes each gate (T139,
  medium).** **[Observed]**
- **Eleven low findings:** E136 to E141 in the kernel, the faces and the schema, V87 in the desktop, and T136 to T138
  and T140 in the gates. **[Observed]**
- **No new finding is critical.** **[Observed]**
- **Open findings: 0 critical, 5 high, 18 medium and 28 low.** **[Observed]**

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 16 | `.csproj` lines in `3DEngine.sln`. The last review gave 15; `Engine.Kernel` is new. |
| Production source lines | 27,532 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests`, `3DEngine.ImageTests` and `3DEngine.VisionTests`, 262 files. The last review gave 25,949 in 251 files; the same script gives that value at `3cd76af`. |
| Test source lines | 32,034 | `Engine.Tests` (23,057 in 112 files), `3DEngine.Tests` (3,523 in 22 files), `3DEngine.ImageTests` (5,389 in 32 files) and `3DEngine.VisionTests` (65 in 1 file). The last review gave 30,397. |
| Tests | 1,767 | `dotnet test 3DEngine.sln`: 1,257, 312, 195 and 3 passed, 0 failed, 0 skipped. The last review gave 1,687. |
| Gate classes | 19 | Tracked files whose name ends in `GateTests.cs`. `eng/repo-gates` gives 424 runs of 151 checks. The last review gave 19, and 398 runs. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 14 | Section "Open" of `docs/register.md`; the limit is 15. The last review gave 14; R-0052 and R-0053 were opened, and R-0052 and R-0043 were closed. |
| ADRs | 60 | `docs/adr/0001` to `0060`, with 0003 and 0011 in `docs/adr/archive/`. In force: 58 (26 accepted, 32 amended). None is proposed. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 5 | The same method: V2, V3, T4, T31, E134. The last review gave 4. |
| Findings: medium | 18 | The same method: E5, E6, E12, E13, E19, E21, E22, E135, V4, V85, V86, T2, T5, T10, T17, T32, T37, T139. The last review gave 15; V78 is fixed. |
| Findings: low | 28 | The same method: E17, E24, E34, E39, E57, E84, E136 to E141, V7, V15, V25, V29, V84, V87, T7, T13, T23, T25, T136 to T138, T140, C1, P1. The last review gave 28; E131 to E133, V79 to V83 and T133 to T135 are fixed. |
| Lines in the rule files | 2,779 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The last review gave 2,706. |
| Sentences with "must" | 58 | `grep -o -w -i must` on the same ten files. The last review gave 58. |
| Sentences with "do not" | 54 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 54. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found. E13, T32 and E84 are fixed in part and count as open, and V84 too. A finding that the owner accepted counts as
closed.

## Findings of the previous review

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. No change: `Engine.Api.Http/Endpoints/JsonParameters.cs` has no commit since `3cd76af`. Register entry R-0027.
- **E6** — Open. No change: `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` has no commit since `3cd76af`, and it still lists `document.loaded`, `document.replayed` and `document.saved` (lines 79 to 81). Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. No change.
- **E8** — Fixed. TASK-0044, v0.37. No change.
- **E9** — Fixed, and it holds. No change.
- **E10** — Fixed. TASK-0035, v0.43. No change.
- **E11** — Fixed. TASK-0115, v0.115. No change.
- **E12** — Open, and smaller. No change: `Engine.Core/Hosting/ParameterBinder.cs` has no commit since `3cd76af`. Register entry R-0027.
- **E13** — Fixed in part. No change: `QueriesEndpoint.cs` and `Engine.Cli/Cli.cs` have no commit since `3cd76af`.
- **E14** — Fixed. TASK-0115, v0.115. No change.
- **E15** — Fixed. TASK-0044, v0.37. No change.
- **E16** — Fixed. TASK-0115, v0.115. No change.
- **E17** — Open, and smaller. No change: `Engine.Core/CommandBus.cs` has no commit since `3cd76af`. Register entry R-0028.
- **E18** — Fixed for the sinks of `Engine.*` (TASK-0140, v0.141) and of the desktop host (T118, TASK-0150, v0.154). Correction: the line of the last review lost the end of its code span. No change.
- **E19** — Open, and smaller. No change: `CommandBus.cs` and `DocumentSession.cs` have no commit since `3cd76af`. The static cache of `KernelProjection` holds kernel solids, not a Document or a sink (E135). Register entry R-0033.
- **E20** — Fixed, and it holds. No change.
- **E21** — Open, and wider. **[Inferred]** `SubtractCommandHandler.cs:125` runs the kernel after the backend made the solid, and `Regeneration.cs:76-105` runs the check of the faces after the walk of the solids, so an exception in either place gives the orphan of E21. No probe found an input that makes the kernel throw. Register entry R-0033.
- **E22** — Open. No change: `Engine.Core/CommandBus.cs` has no commit since `3cd76af`. Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. No change.
- **E24** — Open in seven parts of nine. No change: `WireMessage.cs` and `SchemaEventsEndpoint.cs` have no commit since `3cd76af`. Register entry R-0033.
- **E25** — Accepted by the owner. TASK-0142, v0.144. Register entry R-0036. No change.
- **E26** — Fixed. TASK-0135, v0.136. No change.
- **E27** — Accepted by the owner. TASK-0142, v0.144. Register entry R-0036. No change.
- **E28** — Fixed. TASK-0136, v0.137. No change.
- **E29** — Fixed for the replay. TASK-0135, v0.136. No change.
- **E30** — Fixed. TASK-0136, v0.137. No change.
- **E31** — Fixed for the replay. TASK-0137, v0.138. No change.
- **E32** — Fixed. TASK-0135, v0.136. No change.
- **E33** — Fixed with E18. TASK-0140, v0.141. No change.
- **E34** — Open. No change: `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs` has no commit since `3cd76af`. Register entry R-0036.
- **E35** — Fixed for the routes of `MapGet`. TASK-0139, v0.140. No change.
- **E36** — Fixed. TASK-0079, v0.85. No change.
- **E37** — Fixed. TASK-0137, v0.138. No change.
- **E38** — Fixed. TASK-0138, v0.139. No change.
- **E39** — Open. No change: `Engine.Cli/Cli.cs`, `Engine.Api.Http.Server/ServerEngine.cs` and `3DEngine/DesktopSurface.cs` have no commit since `3cd76af`. Register entry R-0036.
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
- **E57** — Open. No change: `Engine.Tests/Http/DesktopSurfaceTests.cs` has no commit since `3cd76af`. Register entry R-0039.
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
- **V2** — Open. No change: `3DEngine.Vulkan/OverlayRenderer.cs` has no commit since `3cd76af`. Register entry R-0022.
- **V3** — Open. No change: `3DEngine.Vulkan/OverlayRenderer.cs` has no commit since `3cd76af`. Register entry R-0022.
- **V4** — Open. No ledger entry from v0.182 to v0.190 names it. Register entry R-0027.
- **V5** — Fixed. TASK-0076, v0.74. No change.
- **V6** — Fixed. TASK-0076, v0.74. No change.
- **V7** — Open. No change: `3DEngine.Vulkan/Ui/UiRenderer.cs`, `3DEngine.Vulkan/Ui/UiTexture.cs` and `3DEngine.Vulkan/GpuBuffer.cs` have no commit since `3cd76af`. Register entry R-0030.
- **V8** — Fixed. TASK-0074, v0.75. No change.
- **V9** — Fixed. TASK-0074, v0.75. No change.
- **V10** — Fixed. TASK-0074, v0.75. No change.
- **V11** — Fixed in part. TASK-0074, v0.75. No change.
- **V12** — Fixed, and changed by decision. No change.
- **V13** — Fixed for its parts. No change.
- **V14** — Fixed. TASK-0074, v0.75. No change.
- **V15** — Open. `3DEngine/SessionBodies.cs` changed for V81, and the wait for the idle device of each removed mesh stays (`SessionBodies.cs:275`).
- **V16** — Fixed. TASK-0079, v0.85. No change.
- **V17** — Fixed. TASK-0079, v0.85. No change.
- **V18** — Fixed for its forms. TASK-0090, v0.93. No change.
- **V19** — Fixed, with no injection. TASK-0079, v0.85. No change.
- **V20** — Fixed. TASK-0079, v0.85. No change.
- **V21** — Fixed by reading, with no test. TASK-0079, v0.85. No change.
- **V22** — Fixed for its forms. TASK-0079, v0.85. No change.
- **V23** — Fixed. TASK-0079, v0.85. No change.
- **V24** — Fixed. TASK-0079, v0.85. No change.
- **V25** — Open. No change: `3DEngine.Vulkan/3DEngine.Vulkan.csproj` has no commit since `3cd76af`. Register entry R-0020.
- **T1** — Fixed. TASK-0156, v0.158. Each run since then compares the three hashes. No change.
- **T2** — Open in one part of five. No ledger entry from v0.182 to v0.190 names it. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. No change.
- **T4** — Open. No change: `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs` has no commit since `3cd76af`. Register entry R-0031.
- **T5** — Open in one part of three. No ledger entry from v0.182 to v0.190 names it. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Open in four parts of five. No ledger entry from v0.182 to v0.190 names it. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. No change.
- **T10** — Open. No ledger entry from v0.182 to v0.190 names it. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51. No change.
- **T12** — Fixed. TASK-0059, v0.55. No change.
- **T13** — Open in part. No change: `.github/workflows/ci.yml` has no commit since `3cd76af`. Register entry R-0031.
- **T14** — Fixed. TASK-0053, v0.51. No change.
- **T15** — Fixed. TASK-0050, v0.47. No change.
- **T16** — Fixed. TASK-0066, v0.67. No change.
- **T17** — Open. No change: `NativePackageGateTests.cs` and `nuget.config` have no commit since `3cd76af`. Register entry R-0031.
- **T18** — Fixed. TASK-0049, v0.46. No change.
- **T19** — Fixed. TASK-0049, v0.46. No change.
- **T20** — Fixed. TASK-0049, v0.46. No change.
- **T21** — Fixed. TASK-0049, v0.46. No change.
- **T22** — Fixed. TASK-0049, v0.46. No change.
- **T23** — Open. No change: `build.ps1` has no commit since `3cd76af`. Register entry R-0031.
- **T24** — Fixed. No change.
- **T25** — Open in part. No change: `eng/write-set-check` has no commit since `3cd76af`. Register entry R-0031.
- **T26** — Fixed. TASK-0057, v0.54. No change.
- **T27** — Fixed. TASK-0058, v0.56. No change.
- **T28** — Fixed. No change.
- **T29** — Fixed. TASK-0057, v0.54. No change.
- **T30** — Fixed. TASK-0057, v0.54. No change.
- **T31** — Open. No change: `Engine.Tests/Governance/RepositoryFiles.cs` and `eng/repo-gates/RepoGates.csproj` have no commit since `3cd76af`. Register entry R-0031.
- **T32** — Fixed in part. No change: `eng/write-set-check/Program.cs` has no commit since `3cd76af`.
- **T33** — Fixed. TASK-0057, v0.54. No change.
- **T34** — Fixed. TASK-0057, v0.54. No change.
- **T35** — Fixed. TASK-0057, v0.54. No change.
- **C1** — Open. No change: `Engine.Contracts/Geometry/IBRepOps.cs` and `Engine.Contracts/Geometry/IFeatureIdMap.cs` have no commit since `3cd76af`. Register entry R-0027.
- **P1** — Open, and smaller. V81 removes a cost in each frame, and V87 adds a query in each frame of a drag. Register entry R-0027.
- **T36** — Fixed. TASK-0061, v0.61. No change.
- **T37** — Open in part. No change: `WriteSetRangeTests.cs` has no commit since `3cd76af`. Register entry R-0031.
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
- **V29** — Open. No ledger entry from v0.182 to v0.190 names it, and the register has no entry for it.
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
- **E84** — Fixed in part. TASK-0099, v0.102. No change: `Engine.Core/Persistence/DocumentJournal.cs` has no commit since `3cd76af`.
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
- **E131** — Fixed for its forms. TASK-0182, v0.184 (`DatumResolver.cs:343-352`): the scaled branch takes the axis from the scaled components and their own root. A side whose difference overflows gives an infinite component, and the check `Finite` gives a reason. **[Observed]**
- **E132** — Fixed. TASK-0182, v0.184: with `changedSketches: [solved, .. sketches]` put back in `SketchResults.cs:49`, `An_Edit_Of_A_Sketch_With_A_Stale_Reference_Takes_The_Record_Of_The_Walk` fails. **[Observed]**
- **E133** — Fixed. TASK-0182, v0.184: with the sort `OrderBy(graph.Order)` removed at `DatumResolver.cs:150`, `The_Sketches_That_The_Walk_Changes_Come_In_The_Order_Of_The_Log` fails. The changed datums come in the order of the log too. **[Observed]**
- **V78** — Fixed for its forms. TASK-0181, v0.183 (`Camera.cs:67-76`, `SceneRenderer.cs:87-103`): the far plane holds the box from the eye and is set in each frame. The next form is V86. **[Observed]**
- **V79** — Fixed. TASK-0181, v0.183 (`DimensionOverlay.Place`). **[Observed]**
- **V80** — Fixed. TASK-0181, v0.183 (`DimensionOverlay.Inside`, a clip of each line to the window). **[Observed]**
- **V81** — Fixed. TASK-0181, v0.183: `Apply` (`SessionBodies.cs:140-142`) and `Preview` (`:312`) clear the kept unions, and no other path changes the boxes. **[Observed]**
- **V82** — Fixed. TASK-0181, v0.183 (`Shifted`, `SessionBodies.cs:346`). The fix gives V87. **[Observed]**
- **V83** — Fixed. TASK-0181, v0.183: ADR-0056 items 2 and 7 agree with `DesktopUi.cs:648-656`. **[Observed]**
- **V84** — Fixed in part, as the ledger of v0.183 says. With the call of `FaceArm.Take` (`NativeThreeDEngine.cs:160`) and the message of the arm (`:249`) removed, each test of `3DEngine.Tests` and `3DEngine.ImageTests` passes. **[Observed]**
- **T133** — Fixed for its forms. TASK-0182, v0.184: the injection of the review now fails `Each_Item_Of_A_Project_Is_One_That_A_Gate_Reads` (`DependencyDirectionGateTests.cs:606`). The next forms are T136. **[Observed]**
- **T134** — Fixed for its forms. TASK-0182, v0.184: the forms of the review each give one part. The next forms are T137. **[Observed]**
- **T135** — Fixed for its forms. TASK-0182, v0.184: the form of the review gives one problem. The next forms are T138. **[Observed]**

## 1 Current architecture

**[Observed]** `Engine.Kernel` is the owned kernel of identity (ADR-0057). It holds exact rational numbers, a BSP
subtract of planar solids and a B-rep whose faces keep their provenance. It references no project. `Engine.Core`
references it (ADR-0058): `KernelProjection` builds the kernel solid of a body from the feature graph, keeps the solids
of the committed geometry keys, and `GetFaces` gives the faces of a body with their names. A datum on a face that a cut
removed gets the fault `W-DATUM-REFERENCE-FAILED`, and a new datum on such a face is refused (ADR-0059).

**[Observed]** Each number parameter of a command declares its unit kind in the schema (ADR-0060), and the wire
writes `unit` and `unitBy` only when a field declares them. A gate fails when an open task plans an ADR number that
another record has. On Windows a mouse press and a release take the place of the cursor (P0.81).

## 2 Patterns in use

- **A new refusal of a command does not ask whether it runs in a replay.** The precedent of E103 exists
  (`SketchResults.ApplyingEntry`), and the refusal of K4c does not use it (E134). **[Observed]**
- **A cache that is static for the process takes a key that is unique in one Document only** (E135). **[Observed]**
- **A fix of one input path changes the normal path too.** P0.81 corrects a press with no motion before it, and it
  also moves the press of a hand that moves on (V85). **[Observed]**
- **Each correction of a gate holds for its forms, and a probe finds the next form.** T136 follows T133, T137 follows
  T134, and T138 follows T135. **[Observed]**
- **A rule of an ADR in words has no gate for its second half.** ADR-0057 item 1 says "no project and no package",
  and the gate reads the projects (T139). **[Observed]**

## 3 Review of the code

### The engine and the hosts

#### E134 · High · An older document with a CreateDatum on a face that a cut removed no longer opens · Confirmed

- **[Observed]** `Engine.Core/Commands/CreateDatumCommandHandler.cs:90-98` refuses a datum on a face that a cut removed,
  with `E-DATUM-REFERENCE-INVALID`. It does not test `SketchResults.ApplyingEntry`, as the refusals of E103 do
  (`SetExtrudeDistanceCommandHandler.cs:62`).
- **[Observed]** `Engine.Core/Persistence/DocumentFile.cs:221-225` fails an open when an entry does not give `Applied`.
- **[Observed]** Before K4c (v0.187) `CreateDatum` resolved a face from the parameters of the feature only, so the
  HTTP surface and the CLI accepted a datum on a face that a cut had removed. A probe of a log with a plate, a tool, a
  move, a subtract that removes the face `+x`, and a `CreateDatum` on `+x` gave at the open:
  `E-IO-LOAD-FAILED: The entry of version 5 (CreateDatum) gave Rejected, with E-DATUM-REFERENCE-INVALID`.
- **[Inferred]** The CLI export reads through the same path, so it fails too. ADR-0059, "Older documents", covers only
  the turn of the frame.
- **[Recommended]** In a replay (`ApplyingEntry`), skip the refusal and give the datum the fault
  `W-DATUM-REFERENCE-FAILED`, as a subtract does. Add a replay test and a line in ADR-0059. Q1.

#### E135 · Medium · The kept kernel solids are static for the process, under a key that is unique in one Document only · Confirmed

- **[Observed]** `Engine.Core/Features/KernelProjection.cs:22-24` holds `Kept` and `KeptOrder` as static fields, and
  `OfRecord` (`:45-71`) reads them by `GeometryKey`. For a box the key is the command identifier (`BodyRecord.cs:22`),
  and a client chooses it (`CommandsEndpoint.cs:58`). ADR-0059 item 2 says "a committed key names one solid".
- **[Observed]** A probe: session 1 holds a box of the width 10 under the command identifier X, read one time by
  `GetFaceAt`. Session 2 holds a box of the width 40 under X, a datum on `+x` and a tool at x 3 to 7. The subtract of
  session 2 gave `W-DATUM-REFERENCE-FAILED`. The same steps under a new identifier gave no warning.
- **[Inferred]** One process holds two sessions in the tests and in the scratch session of `DocumentFile.Check`. A
  replay in a new process does not give the fault, so the Document depends on another session.
- **[Recommended]** Keep the solids in the session or key them by the identity of the Document and the key.

#### E136 · Low · Rational.ToDouble rounds two times below the smallest normal double · Confirmed

- **[Observed]** `Engine.Kernel/Rational.cs:69-99` rounds to 53 bits, then `Math.ScaleB` rounds again. A probe:
  (1/2 + 2⁻⁶⁰)·2⁻¹⁰⁷⁴ gave 0, and (3/2 − 2⁻⁶⁰)·2⁻¹⁰⁷⁴ gave 1E-323; the nearest double of each is 5E-324. The comment
  and ADR-0057 item 2 say "one rounding to the nearest".
- **[Inferred]** Each platform gives the same bits, and the rounding stays monotone, so the vertex filter of ADR-0058
  item 5 stays sound.
- **[Recommended]** In the subnormal range, compute the shift for the target exponent first and round one time. Q5.

#### E137 · Low · A hole that touches the outer boundary at a vertex merges into the outer loop on one face only · Confirmed

- **[Observed]** `Engine.Kernel/Brep.cs:313-336` takes "the first edge that leaves" a vertex with two outgoing edges. A
  plate of 10 by 10 by 2 less a diamond prism whose vertex (5, 0) touches the side `+x` gave the holes 1 on `+z` and 0
  on `-z` for the same hole (`GetFacesQueryHandler.cs:107`). Both areas are 96, which is correct.
- **[Recommended]** At a vertex with more than one outgoing edge, take the edge that turns most to the left, with a test
  of the touching case.

#### E138 · Low · A hole in an island goes to the outer region of the face · Confirmed

- **[Observed]** `Brep.cs:255-282`: `Owner` gives the first outer loop that holds the hole, not the innermost one. A
  plate of 20 by 20 with a moat around an island of 8 by 8 and a pin through the island gave the holes 2 on the outer
  region and 0 on the island.
- **[Inferred]** `GetFaces` sums the holes and the areas over the regions of a name, so its values are correct;
  `Brep.Faces` is wrong.
- **[Recommended]** Take the outer loop of the smallest area that holds the hole, with a test.

#### E139 · Low · A face-gone fault goes away while the end of its chain has a body fault · Confirmed

- **[Observed]** `DatumResolver.Rebuild` sets `Fault = null` on each datum that resolves, `Regeneration.cs:94` gives
  no names for an end with a fault, and `FaceGone.cs:187` then makes no check. A probe: a datum on `+x`, cut by a tool,
  has the fault. A move that makes the profile of the tool a bow tie gave the body `W-SKETCH-REBUILD-FAILED`, and the
  fault of the datum became none, while the old solid still has no `+x`.
- **[Inferred]** ADR-0059 item 3 says that the datum "has no check", not that the fault goes.
- **[Recommended]** Keep the previous face-gone fault while the names of the end are not known. Q4.

#### E140 · Low · A new member of FieldSchema leaves the wire with no failure · Confirmed

- **[Observed]** `FieldMapConverter` (`Engine.Api.Http/Schema/SchemaTypes.cs:42-77`) writes four named members, and the
  gate (`SchemaEndpointGateTests.cs:347-349`) lists the same four. With `string? Note = null` added to `FieldSchema`
  and set on `sizeX` of `CreateBox`, each of the 18 tests of `SchemaEndpointGateTests` passed, and the wire had no note.
- **[Recommended]** A test that compares the members of `FieldSchema` and `UnitByField` with the members that the
  converter writes.

#### E141 · Low · ADR-0060 is silent about the number parameters of queries · Confirmed by reading

- **[Observed]** `GetFaceAtQueryHandler.cs:21-23` declares x, y and z as "number" with no unit, and
  `GetBodyInfoQueryHandler.cs:30` declares `distance` with no unit. ADR-0060 item 7 leaves out only query results and
  command outputs, and the gate reads commands only.
- **[Recommended]** Give the query parameters their units with the same gate, or add them to item 7.

**No finding.** **[Observed]** A touching face, edge, vertex and strip, a disjoint tool, a flush corner, slab and slot,
an inside pocket and a half overlap each gave the correct volume and a valid B-rep. Two regions of one face that meet
at a vertex give a non-manifold B-rep, which is correct. The default `Rational` equals zero with the same hash, −0.0
gives +0, and the order of negative numbers is correct. The determinism gate catches `Math.FusedMultiplyAdd` and
`double.MultiplyAddEstimate` in `Engine.Kernel/Geometry.cs`. The maps of `AddSketchConstraint` agree with each kind of
constraint that takes values. No dictionary of the kernel or of `GetFaces` gives an order. Six cuts by rotated prisms
take 9 to 43 ms each.

**Not verified.** The claims of v0.185 to v0.187 that random models agree with Manifold; the depth of the BSP
recursion for a profile of many lines; Linux, macOS and arm64.

### The desktop

#### V85 · Medium · A press and a release take the place of the cursor at the poll, so a hand that moves on moves the click · Confirmed

- **[Observed]** `3DEngine.Vulkan/SdlInput.cs:51` reads the source of the cursor when it translates the event, and
  `Window.Cursor` (`Window.cs:90`) reads `GetCursorPos` at that time. The host polls one time for each frame, after
  `Render` (`DesktopEngineHost.cs:64`).
- **[Observed]** The events of a hand come with each motion before its press, so SDL gave the true place of a press
  before P0.81. A probe on `SdlInput` and `IntentMachine`: a click at (500, 300), then motions to (560, 300) in the same
  poll, gave `SelectIntent { X = 500, Y = 300 }` with the place of the event and `X = 560` with the place of the cursor.
  A fast start of a drag lost 30 pixels and turned the camera back one time.
- **[Inferred]** After a slow frame a click can reach the wrong body or the wrong key, and a release at a moved place
  can lose a click of a button. The ledger of v0.190 does not state this cost.
- **[Recommended]** Take the place of the cursor only when no motion came after the last button event, with a test of a
  click and a motion in one poll. Q2.

#### V86 · Medium · The far plane ignores the images, so an image larger than about 1000 units is cut · Confirmed

- **[Observed]** `SceneRenderer.Update` (`3DEngine/SceneRenderer.cs:87-103`) returns when no body is visible, and the
  box of the far plane comes from the bodies only (`SessionBodies.Union`). An image has the size of its pixels times its
  scale (`ImagePlacement.cs:18`), and the default scale is 1. A probe on `Camera`: an image of 4000 by 3000, zoomed out
  40 times, gave the far plane 1000 and the farthest corner at 2549.5.
- **[Inferred]** A person who places a photo before any body sees it cut. This is the next form of R-0051 and V78.
- **[Recommended]** Put the quads of the live images in the box of the far plane and of the frame, and keep the
  dimension overlay on the bodies only. Add a test of an image alone.

#### V87 · Low · During a drag of the gizmo with the dimensions on, the desktop queries GetBodyInfo in each frame · Confirmed by reading

- **[Observed]** `NativeThreeDEngine.cs:528-533` queries `_actions.Distance` when the box changes, and since V82
  `Shifted` (`SessionBodies.cs:346`) moves the box with each preview. ADR-0056 item 6 says that the distance is read
  "not in each frame".
- **[Inferred]** `BodyActions.Distance` is a synchronous query of the session, so a long command of an HTTP client can
  stop the frames of a drag.
- **[Recommended]** Key the size by the body and the box with no shift, or skip the query while a preview is active.

**No finding.** **[Observed]** No path changes the boxes outside `Apply` and `Preview`, and both clear the kept unions.
With no handle the source of the cursor gives null, and a touch is dropped before the cursor is read. `Window` has no
`Dispose`, so a disposed window cannot reach `Window.Cursor`.

**Not verified.** The desktop window; a display scale other than 1 and two monitors of different scales for
`Window.Cursor`; the rate of slow frames, which sets how often V85 shows; the pen of SDL 3.2 on Windows; the frame with
the old far plane after `ApplyPanel` moves the camera.

### The gates and the tests

#### T136 · Low · A property makes Engine.Core reference 3DEngine.Core, and each gate passes · Confirmed

- **[Observed]** `ItemOffences` (`DependencyDirectionGateTests.cs:591`) reads item elements only, and `ImportProperty`
  (`:686`) lists the import properties only. With `<AdditionalExplicitAssemblyReferences>` that names
  `3DEngine.Core.dll` in `Engine.Core.csproj` and a file that uses `ThreeDEngine.Core.Picking.Picker`, the build passed,
  the built `Engine.Core.dll` holds the reference, 500 of 500 governance and diagnostics tests passed, and
  `eng/repo-gates` gave 0 failed. A property `CompilerResponseFile` with a file `/reference:` did the same.
- **[Inferred]** The severity stays low, as for T133: the property needs a built file of the other project and a
  deliberate edit of a project file.
- **[Recommended]** Read the assembly references of each built `Engine.*` assembly with `System.Reflection.Metadata`
  and compare them with the graph. That closes the class.

#### T137 · Low · The next forms of T134 hide a severity from the scanner · Confirmed

- **[Observed]** `DiagnosticsScanner.cs:121-164` gave 0 parts for a local variable (`var s = S.ToString(); return s +
  rest;`), `("X" + rest).Replace('X', S)`, `string.Concat(rest.Prepend(S))`, `string.Concat(new[] { S }.Concat(rest))`,
  `new string(a) + rest` and `(b ? S : S).ToString() + rest`. The controls gave 1 part each.
- **[Recommended]** Stop the chase of spellings: each `const char` `'E'`, `'W'` or `'I'` in `Engine.*` is a part unless
  it is an operand of `==` or `!=`.

#### T138 · Low · An HTML heading in another HTML element is not a heading problem of the ledger · Confirmed

- **[Observed]** `CodebaseReviewGateTests.cs:379` needs `<h` at the start of the line. `<div><h2>…</h2></div>`, a
  comment before `<h2>` and `<details><summary><h2>…</h2></summary></details>` gave 0 problems; the control gave 1.
- **[Recommended]** A `<h1>` to `<h6>` anywhere on a line is a problem.

#### T139 · Medium · Engine.Kernel can take a package, and each gate passes · Confirmed

- **[Observed]** ADR-0057 item 1 says that `Engine.Kernel` "references no project and no package".
  `The_Owned_Kernel_References_No_Project` (`DependencyDirectionGateTests.cs:927`) reads the project references only.
  With a `PackageReference` to `Microsoft.CodeAnalysis.CSharp` in `Engine.Kernel.csproj`, the restore took it, 500 of
  500 governance and diagnostics tests passed, and `eng/repo-gates` gave 0 failed.
- **[Inferred]** A package of the kernel is code that the determinism scanner does not read. `Engine.Core` and
  `Engine.Contracts` have no package either, and no gate holds that.
- **[Recommended]** A fact that `Engine.Kernel`, `Engine.Core` and `Engine.Contracts` have no `PackageReference`.

#### T140 · Low · The unit rule reads the type "number" only, so the pixel size of PlaceImage has no unit · Confirmed by reading

- **[Observed]** `PlaceImageCommandHandler.cs:25-26` declares `width` and `height` as `integer`, in pixels
  (`ImagePlacement.cs:18` multiplies them by `scale`). The rule of `SchemaEndpointGateTests.cs:142-146` reads `number`
  only, and ADR-0060 item 6 agrees with the code. A client cannot learn from the schema that the two numbers are pixels.
- **[Recommended]** Apply the rule to `integer` too, so `width` and `height` get `pixel`, and amend ADR-0060. Q3.

**No finding.** **[Observed]** The injection of T133 fails `Each_Item_Of_A_Project_Is_One_That_A_Gate_Reads`. The gate
of P0.80 catches a planned file with a number and two files of one number. The unit gate fails a key that does not
agree and an extra member.

**Not verified.** Linux, macOS and arm64; `Directory.Build.rsp` as a form of T136; the full test run inside each
worktree of the agents.

## 4 The next planned task

On 2026-11-05 the owner confirmed two orders: the Android order (phases a to g in track R) and a standing order for an
autonomous run until 2026-11-08. The next product phase is phase a of the Android order: the charter change, the
closure of R-0020, the ADR of the Android platform and the amendments of ADR-0017 and ADR-0027. By `docs/templates.md`,
section 7, the high finding goes before it: P0.82 (TASK-0190) holds E134. The medium and the low findings go after it:
P0.83 (TASK-0191) holds V85 to V87, and P0.84 (TASK-0192) holds E135 to E141 and T136 to T140.

## 5 Research for that task

**[Observed]** A verification of 2026-11-05 read the packages on disk, as the Android order requires before its ADR:

- `ppy.SDL3-CS` 2026.1002.1 holds `lib/net8.0` and `lib/net8.0-android34.0`, a `libSDL3.so` for `android-arm64` and
  `android-x64` with pages of 16 KB, and the Java class `org.libsdl.app.SDLActivity`. A throwaway `net10.0-android` app
  with the minimum API 33 and an activity that subclasses `SDLActivity` built with 0 warnings and 0 errors, in Debug
  and in Release. The native library is "SDL-3.5.0-ea7a2da". The package also ships 32-bit x86 files, which
  `CLAUDE.md`, determinism rule 5, forbids.
- The API of `ppy.SDL3-CS` differs from `Alimer.Bindings.SDL` 3.9.8: enumeration names in C form, a window as a
  pointer, and pointer arguments. `Window.cs` and `SdlInput.cs` alone gave 69 distinct compile errors.
- `ImGui.NET` 1.91.6.1 has no Android library in any of its 68 versions, and NuGet puts its glibc `libcimgui.so` of
  `linux-x64` into the APK for `x86_64`.
- `Vortice.Vulkan` 3.2.3 tries `libvulkan.so.1`, then `libvulkan.so`, on a platform that is not Windows or macOS.
- `Alimer.Bindings.SDL` has no Android file in any of its 56 versions, and no release after 2025-04-04.
- This computer has the Android workload, the SDK (platforms 35 and 36) and a JDK, and no NDK and no cmake on the path.

**[Inferred]** No fact blocks phase a. Phase d (the native builds of Manifold and cimgui) needs the NDK, so it runs on
GitHub Actions. Two facts need a decision in the ADR: SDL 3.5.0 is probably a development version, and the 32-bit x86
files must not ship.

## 6 Recommended approach

**[Recommended]** In this order:

1. P0.82 (TASK-0190): E134, with a replay test before the change and the line of ADR-0059. 1 agent hour.
2. Phase a of the Android order, with its ADR and its questions.
3. P0.83 (TASK-0191): V85 to V87, each with a test or an injection before the change. 1 to 2 agent hours.
4. P0.84 (TASK-0192): E135 to E141 and T136 to T140, each with a test or an injection before the change. 2 to 3
   agent hours.

## Questions for the owner

The standing order of 2026-11-05 answers each question with the recommended option. The owner checks these answers on
2026-11-08.

**Q1. A replay of an older CreateDatum on a face that a cut removed (E134).**
- A: In a replay the refusal does not act; the datum gets the fault `W-DATUM-REFERENCE-FAILED`, as after a subtract.
- B: A migration of older documents that removes such an entry.

**Recommended: A.** B rewrites the log, which the standing order forbids, and A gives the state that a later subtract
gives for the same datum.

**Q2. The place of a press (V85).**
- A: The press takes the place of the cursor only when no motion came after the last button event.
- B: A hook of the Windows messages reads the place of each button message.

**Recommended: A.** A keeps the code free of native hooks and keeps the place of the event for each press of a hand.

**Q3. The unit of an integer parameter (T140).**
- A: The rule reads `integer` too, and `width` and `height` of `PlaceImage` get `pixel`; ADR-0060 is amended.
- B: ADR-0060 records that an integer has no unit.

**Recommended: A.** The purpose of ADR-0060 is that a client holds no table of units.

**Q4. The fault of a datum while the end of its chain has a body fault (E139).**
- A: The datum keeps its previous face-gone fault until the names of the end are known.
- B: ADR-0059 says that the fault goes.

**Recommended: A.** The old solid still has no such face, so a fault that goes is false.

**Q5. The rounding of Rational.ToDouble below the smallest normal double (E136).**
- A: One rounding in the subnormal range, as ADR-0057 item 2 says.
- B: The comment and ADR-0057 item 2 state the double rounding.

**Recommended: A.** The change is local to one method, and each platform keeps the same bits.
