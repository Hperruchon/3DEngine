---
date: 2026-10-31
commit: 661eb8a
ledger: v0.130
previous: 2026-10-30-b-codebase-review.md
---

# Codebase review — 2026-10-31

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the fifteenth codebase review. The review of 2026-10-30 (b) examined v0.121. After it came 4 product milestones
(K2e, K2f, K2g, K0e) and 9 ledger entries (v0.122 to v0.130). The limits are four and twelve, so the next product
milestone makes a review due, and this review comes before it. `Engine.Tests/Governance/CodebaseReviewGateTests.cs`
holds the rule, and `docs/templates.md`, section 7, gives the form.

The owner told the session on 2026-10-31 to work alone until the morning and to use its recommendation for each
question. So each question below has a recommended answer, and the tasks of this review use it; the owner can change
an answer later.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is the
authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this file does not.

| Item | Value |
|---|---|
| Code state | `main` at `661eb8a`, the merge of v0.130. Its tree is the tree of `4f252bf`, the tip of the branch of K0e |
| Branch of this review | `codebase-review-2026-10-31` |
| Tests | 1,318 on this computer (960 in `Engine.Tests`, 217 in `3DEngine.Tests`, 141 in `3DEngine.ImageTests`) |
| Pipeline | The run on `661eb8a`: green on Ubuntu, Windows and macOS |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner decides.

**Method.** Ten tasks changed code or records after `6e065d5`, the tree of the last review: TASK-0120 (the review),
TASK-0121 (P0.48), TASK-0122 (K2e), TASK-0123 (P0.49), TASK-0124 (P0.50), TASK-0125 (K2f), TASK-0127 (K2g),
TASK-0128 (the row K3) and TASK-0126 (K0e). Three review agents worked in parallel at `4f252bf`, each one in its own
git worktree: the engine and the HTTP surface, the desktop, and the gates. Each agent wrote probes that only its
worktree holds. I ran the failing probes of the engine again, injected the form of T103 with its control, and read the
cited lines of the desktop findings. A line that says "an agent ran it" means that I read the cited code and did not
repeat the run.

## Summary

- **The fixes of the last review hold for the forms that they named, and three gates hold for full names only.**
  E97 to E101, V51 to V55 and T96 to T102 are fixed for their forms. **[Observed]**
- **The check of the tasks of a gate does not bind the real gate files (T103, high).** Its compilation has no implicit
  global using, so `new Thread(...)`, `Task.Run` and `SemaphoreSlim` with no full name pass, and the self check passes
  because each of its forms has explicit usings. I injected `new Thread(() => { }).Start();` into a gate file: the gate
  program passed; with `System.Threading.Thread` it failed. **[Observed]**
- **A radius at the noise level passes the fix of E97 (E102, high).** A coincident centre and a tangent give a circle of
  radius 5.6e-17, and the sketch shows as solved; an extrude would send a polygon of that size. **[Observed]** I ran
  the probe.
- **The new refusals at an add break earlier files (E103, medium).** The arc at its centre (E98) and the tangent
  through the centre (E97) are refused also for an entry of an open, so a file of v0.117 to v0.124 with such an entry
  does not open. This is the class of E100. **[Observed]** by an agent; I read the code.
- **The Value pad of an angle shows its cosine and its sine in millimetres (V56, medium).** A person cannot change an
  angle in practice. **[Observed]** by reading.
- **Five forms pass a gate (T104 to T107, medium; T108, T109, low).** The reader of constants has no implicit using
  either, and grants each internal to each project; the ledger counter misses a heading with "version"; a task output
  with the item name `Compile` passes. **[Observed]** by an agent.

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 14 | `.csproj` lines in `3DEngine.sln`. The last review gave 14. Git tracks 15 project files: the native package is not in the solution. |
| Production source lines | 20,403 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests` and `3DEngine.ImageTests`, 212 files. The last review gave 19,877 in 212 files. |
| Test source lines | 23,298 | `Engine.Tests` (17,269 in 94 files), `3DEngine.Tests` (2,464 in 17 files) and `3DEngine.ImageTests` (3,565 in 20 files). The last review gave 22,181. |
| Tests | 1,318 | `dotnet test 3DEngine.sln`: 960, 217 and 141 passed, 0 failed, 0 skipped. The last review gave 1,248. |
| Gate classes | 19 | Tracked files whose name ends in `GateTests.cs`. `eng/repo-gates` gives 310 runs of 136 checks. The last review gave 19. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 14 | Section "Open" of `docs/register.md`; the limit is 15. This review adds none. The last review gave 14. |
| ADRs | 40 | `docs/adr/0001` to `0040`, with 0003 and 0011 in `docs/adr/archive/`. In force: 38 (18 accepted, 20 amended). None is proposed. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 7 | The same method: V2, V3, T1, T4, T31, E102, T103. The last review gave 6. |
| Findings: medium | 25 | The same method: E5, E6, E12, E13, E18, E19, E21, E22, E25, E27, E28, E33, E103, V4, V56, T2, T5, T10, T17, T32, T37, T104, T105, T106, T107. The last review gave 29. |
| Findings: low | 34 | The same method: E17, E24, E26, E29 to E32, E34, E35, E37 to E39, E57, E84, E104, E105, V7, V15, V25, V29, V57 to V62, T7, T13, T23, T25, T108, T109, C1, P1. The last review gave 30. |
| Lines in the rule files | 2,457 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The last review gave 2,425. |
| Sentences with "must" | 57 | `grep -o -w -i must` on the same ten files. The last review gave 57. |
| Sentences with "do not" | 55 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 55. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found. E13, T32 and E84 are fixed in part and count as open.

## Findings of the previous review

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. No change: `Engine.Api.Http/Endpoints/JsonParameters.cs` has no commit since `6e065d5`. Register entry R-0027.
- **E6** — Open. No change: `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` has no commit since `6e065d5`. Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. No change.
- **E8** — Fixed. TASK-0044, v0.37. No change.
- **E9** — Fixed, and it holds. No change.
- **E10** — Fixed. TASK-0035, v0.43. No change.
- **E11** — Fixed. TASK-0115, v0.115. No change.
- **E12** — Open. No ledger entry since v0.121 names it. Register entry R-0027.
- **E13** — Fixed in part. No change.
- **E14** — Fixed. TASK-0115, v0.115. No change.
- **E15** — Fixed. TASK-0044, v0.37. No change.
- **E16** — Fixed. TASK-0115, v0.115. No change.
- **E17** — Open, and smaller. `Engine.Core/CommandBus.cs` changed for E100 only (TASK-0123). Extrude takes the guard of a repeated identifier (E99), so each sketch command and the extrude refuse a live command that the cache forgot; the other commands do not. Register entry R-0028.
- **E18** — Open. `Engine.Core/CommandBus.cs` changed for E100 only (TASK-0123); the comment of the reset did not change. Register entry R-0033.
- **E19** — Open, and smaller. No ledger entry since v0.121 names it. Register entry R-0033.
- **E20** — Fixed, and it holds. No change.
- **E21** — Open, and wider by decision. No ledger entry since v0.121 names it. Register entry R-0033.
- **E22** — Open. `Engine.Core/CommandBus.cs` changed for E100 only (TASK-0123): the bus marks an entry of a log; it still stores each result. Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. No change.
- **E24** — Open. No change: `Engine.Api.Http/WebSockets/WireMessage.cs`, `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` have no commit since `6e065d5`. Register entry R-0033.
- **E25** — Open. `Engine.Core/CommandBus.cs` changed for E100 only (TASK-0123). Register entry R-0036.
- **E26** — Open. No change: `Engine.Contracts/Document.cs` has no commit since `6e065d5`. Register entry R-0036.
- **E27** — Open. No change: `Engine.Core/Replay.cs` has no commit since `6e065d5`. Register entry R-0036.
- **E28** — Open. `Engine.Core/CommandBus.cs` changed for E100 only (TASK-0123); the live bus still catches only a cancellation. Register entry R-0036.
- **E29** — Open. No change: `Engine.Core/Replay.cs` has no commit since `6e065d5`. Register entry R-0036.
- **E30** — Open. No ledger entry since v0.121 names it. Register entry R-0036.
- **E31** — Open, and smaller. `Engine.Core/Sketch/SketchResults.cs` changed for E100 only (TASK-0123); the comments of the other parts did not change. Register entry R-0036.
- **E32** — Open. No ledger entry since v0.121 names it. Register entry R-0036.
- **E33** — Open. No change: `Engine.Api.Http/WebSockets/WireMessage.cs` has no commit since `6e065d5`. Register entry R-0036.
- **E34** — Open. No change: `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs` has no commit since `6e065d5`. Register entry R-0036.
- **E35** — Open. No ledger entry since v0.121 names it. Register entry R-0036.
- **E36** — Fixed. TASK-0079, v0.85. No change.
- **E37** — Open. No change: `Engine.Cli` has no commit since `6e065d5`. Register entry R-0036.
- **E38** — Open. No ledger entry since v0.121 names it. Register entry R-0036.
- **E39** — Open. No change: `3DEngine/DesktopSurface.cs` has no commit since `6e065d5`. Register entry R-0036.
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
- **E57** — Open. No change: `Engine.Tests/Http/DesktopSurfaceTests.cs` has no commit since `6e065d5`. Register entry R-0039.
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
- **V2** — Open. No change: `3DEngine.Vulkan/OverlayRenderer.cs` has no commit since `6e065d5`. Register entry R-0022.
- **V3** — Open. No change: `3DEngine.Vulkan/OverlayRenderer.cs` has no commit since `6e065d5`. Register entry R-0022.
- **V4** — Open. No ledger entry since v0.121 names it. Register entry R-0027.
- **V5** — Fixed. TASK-0076, v0.74. No change.
- **V6** — Fixed. TASK-0076, v0.74. No change.
- **V7** — Open. No change: `3DEngine.Vulkan/Ui/UiRenderer.cs`, `3DEngine.Vulkan/Ui/UiTexture.cs`, `3DEngine.Vulkan/GpuBuffer.cs` have no commit since `6e065d5`. Register entry R-0030.
- **V8** — Fixed. TASK-0074, v0.75. No change.
- **V9** — Fixed. TASK-0074, v0.75. No change.
- **V10** — Fixed. TASK-0074, v0.75. No change.
- **V11** — Fixed in part. TASK-0074, v0.75. No change.
- **V12** — Fixed, and changed by decision. No change.
- **V13** — Fixed for its parts. No change.
- **V14** — Fixed. TASK-0074, v0.75. No change.
- **V15** — Open by decision, and smaller. `3DEngine/SessionBodies.cs` changed for V55 only (TASK-0124): a creation and its changes in one frame give one mesh. It still waits for the device for each consumed or removed body and for each new geometry key.
- **V16** — Fixed. TASK-0079, v0.85. No change.
- **V17** — Fixed. TASK-0079, v0.85. No change.
- **V18** — Fixed for its forms. TASK-0090, v0.93. No change.
- **V19** — Fixed, with no injection. TASK-0079, v0.85. No change.
- **V20** — Fixed. TASK-0079, v0.85. No change.
- **V21** — Fixed by reading, with no test. TASK-0079, v0.85. No change.
- **V22** — Fixed for its forms. TASK-0079, v0.85. No change.
- **V23** — Fixed. TASK-0079, v0.85. No change.
- **V24** — Fixed. TASK-0079, v0.85. No change.
- **V25** — Open. No change: `3DEngine.Vulkan/3DEngine.Vulkan.csproj` has no commit since `6e065d5`. Register entry R-0020.
- **T1** — Open in one part of three. No ledger entry since v0.121 names it. Register entry R-0023.
- **T2** — Open in one part of five. No ledger entry since v0.121 names it. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. No change.
- **T4** — Open. No change: `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs` has no commit since `6e065d5`. Register entry R-0031.
- **T5** — Open in one part of three. No ledger entry since v0.121 names it. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Open in four parts of five. No ledger entry since v0.121 names it. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. No change.
- **T10** — Open. No ledger entry since v0.121 names it. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51. No change.
- **T12** — Fixed. TASK-0059, v0.55. No change.
- **T13** — Open. No change: `.github/workflows/ci.yml` has no commit since `6e065d5`. Register entry R-0031.
- **T14** — Fixed. TASK-0053, v0.51. No change.
- **T15** — Fixed. TASK-0050, v0.47. No change.
- **T16** — Fixed. TASK-0066, v0.67. No change.
- **T17** — Open. No ledger entry since v0.121 names it. Register entry R-0031.
- **T18** — Fixed. TASK-0049, v0.46. No change.
- **T19** — Fixed. TASK-0049, v0.46. No change.
- **T20** — Fixed. TASK-0049, v0.46. No change.
- **T21** — Fixed. TASK-0049, v0.46. No change.
- **T22** — Fixed. TASK-0049, v0.46. No change.
- **T23** — Open. No ledger entry since v0.121 names it. Register entry R-0031.
- **T24** — Fixed. No change.
- **T25** — Open in part. No ledger entry since v0.121 names it. Register entry R-0031.
- **T26** — Fixed. TASK-0057, v0.54. No change.
- **T27** — Fixed. TASK-0058, v0.56. No change.
- **T28** — Fixed. No change.
- **T29** — Fixed. TASK-0057, v0.54. No change.
- **T30** — Fixed. TASK-0057, v0.54. No change.
- **T31** — Open. No change: `eng/repo-gates/RepoGates.csproj` has no commit since `6e065d5`. Register entry R-0031.
- **T32** — Fixed in part. No change.
- **T33** — Fixed. TASK-0057, v0.54. No change.
- **T34** — Fixed. TASK-0057, v0.54. No change.
- **T35** — Fixed. TASK-0057, v0.54. No change.
- **C1** — Open. No change: `Engine.Contracts/Geometry/IBRepOps.cs`, `Engine.Contracts/Geometry/IFeatureIdMap.cs` have no commit since `6e065d5`. Register entry R-0027.
- **P1** — Open, and smaller. No ledger entry since v0.121 names it. Register entry R-0027.
- **T36** — Fixed. TASK-0061, v0.61. No change.
- **T37** — Open in part. No ledger entry since v0.121 names it. Register entry R-0031.
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
- **V29** — Open. No ledger entry since v0.121 names it.
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
- **E84** — Fixed in part. TASK-0099, v0.102. No change.
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
- **T94** — Fixed for the full names. TASK-0114, v0.118. The check binds no implicit global using, so `TaskCreationOptions.LongRunning` with no full name passes in a real gate file (T103).
- **T95** — Fixed for the full names. TASK-0114, v0.118. `new TaskCompletionSource()` with no full name passes in a real gate file (T103).
- **E97** — Fixed for its forms. TASK-0121, v0.123. A radius at the noise level, as 5.6e-17 from a coincident centre and a tangent, still shows as solved (E102).
- **E98** — Fixed. TASK-0123, v0.125.
- **E99** — Fixed. TASK-0123, v0.125.
- **E100** — Fixed for its form. TASK-0123, v0.125. The refusals that TASK-0121 and TASK-0123 added at an add also act on an entry of an open (E103).
- **E101** — Fixed. TASK-0123, v0.125.
- **V51** — Fixed. TASK-0124, v0.126.
- **V52** — Fixed. TASK-0124, v0.126.
- **V53** — Fixed for a started drag. TASK-0124, v0.126. A press on a handle before the slop still lets the wheel and undo act (V58).
- **V54** — Fixed. TASK-0124, v0.126.
- **V55** — Fixed. TASK-0124, v0.126.
- **T96** — Fixed for its forms. TASK-0123, v0.125. A heading with "version", a setext heading and a heading in a quote pass (T106).
- **T97** — Fixed. TASK-0123, v0.125.
- **T98** — Fixed. TASK-0123, v0.125.
- **T99** — Fixed for its four forms. TASK-0123, v0.125. More spellings of a call pass (T109).
- **T100** — Fixed for the full names. TASK-0123, v0.125. A semaphore, a continuation and a cast with no full name pass in a real gate file (T103).
- **T101** — Fixed for the item Compile. TASK-0123, v0.125. An output of a task with the item name Compile passes (T107).
- **T102** — Fixed for its bytes. TASK-0123, v0.125. Other characters that do not show pass (T108).

## 1 Current architecture

**[Observed]** An extrude takes one closed loop of lines and arcs, or one circle (ADR-0040). `SketchProfile.Turn`
computes the polygon of a curve with `+ - * / sqrt` only, by halvings of the sweep and turns of the step, and a bit
test holds the polygon on the three runners. The rebuild of ADR-0037 and ADR-0039 uses the same code, so an edit of a
curve rebuilds the solid.

**[Observed]** The desktop draws and adds circles and arcs (K2e), and the window of the sketch offers each kind of
ADR-0033 item 3: the kinds on a curve, On line, Midpoint, Equal, Parallel, Perpendicular, Angle in degrees, and
Symmetric with a selection of three items (K2f, K2g). The solver keeps each radius greater than 0 (P0.48). An open and a
replay apply an entry whose command identifier came before (P0.49, Q1 A of the last review).

## 2 Patterns in use

- **A new rule of the live session also acts on an open.** E100 was the guard of a repeated identifier; E103 is two
  refusals at an add that later tasks made. Each new refusal of an add breaks the files that an earlier engine wrote.
  **[Observed]**
- **A compilation of a gate is not the compilation of the build.** The check of the tasks and the reader of constants
  have no implicit global using (T103, T104), and the reader grants internals that the build does not (T105). A gate
  then reads less, or more, than the build. **[Observed]** for T103; by an agent for the others.
- **An exact comparison stands for a tolerance.** E97 tests `radius > 0`, and the solver stops within 1e-9, so a
  radius of 1e-17 passes (E102). **[Observed]**
- **Each correction holds for its forms, and a probe finds the next form.** E102 follows E97, E103 follows E100, V58
  follows V53, T103 follows T94, T95 and T100, T106 follows T96, T107 follows T101, T108 follows T102, and T109 follows
  T99. **[Observed]** for E102 and T103; by an agent for the others.

## 3 Review of the code

### The engine and the hosts

#### E102 · High · A radius at the noise level passes the check of E97, and the sketch shows as solved · Confirmed by a run

`Engine.Core/Sketch/SketchSolver.cs:255` and `:260` (`CurvesHaveRadius`) test `radius > 0` and an exact inequality of
positions, and the solver stops when each equation is within 1e-9. A circle of radius 5 about (0, 0), a line from
(1, 1) to (6, 1), `coincident` of the start of the line and the centre, and `tangent` of the line and the circle give
`Status` (True, 4) and the radius 5.55e-17. The same with an arc gives 3.7e-10. `SketchProfile.Find` then gives a
loop of 64 vertices for the circle. The refusal of a tangent through the centre (`SketchConstraints.cs:76`) compares
identifiers only, so the coincident constraint goes round it. **[Observed]** I ran the probes of the agent.
**[Recommended]** A radius, |s − c| and |e − c| must be greater than `SketchSolver.Tolerance` in `Converge` and
`Status`.

#### E103 · Medium · The refusals that TASK-0121 and TASK-0123 added at an add break files that v0.117 to v0.124 wrote · Confirmed by a run

`Engine.Core/Commands/AddSketchArcCommandHandler.cs:87-90` refuses an arc with its start or end at its centre (E98),
and `SketchConstraints.cs:76` refuses a tangent through the centre (E97). Both act also when `SketchResults.ApplyingEntry`
is true. A replay of [a sketch, a centre (0, 0), a start (0, 0), an end (0, 5), the arc] gives `ReplayDivergenceException`
at entry 4 with `E-SKETCH-CURVE-INVALID`, and a replay of a tangent whose line starts at the centre diverges at entry 5.
v0.117 applied both. **[Observed]** by an agent; I read the handlers. Q1 asks the owner; the standing answer takes
the recommendation.

#### E104 · Low · An entry of an add that the log holds two times gives two items with one identifier, or an exception

`Engine.Core/Sketch/SketchResults.cs:71-72` lets each command through when `ApplyingEntry` is true, also an add. A replay
of a repeated `AddSketchLine` gives two lines with one identifier, and `ApplyEntry` of a repeated `AddSketchPoint` throws
`IndexOutOfRangeException` from `SketchSolver.cs:230`; an open turns it into a load error with the text of the
exception (`DocumentFile.cs:203-208`). Only a file of v0.109 or earlier, or a file that a person wrote, holds it.
**[Observed]** by an agent. **[Recommended]** Under `ApplyingEntry`, an add whose item identifier the sketch holds
gives a refusal with its own message, so the open names the line.

#### E105 · Low · The routes of open and save accept a repeated member

`Engine.Api.Http/Endpoints/DocumentEndpoint.cs:78` parses the body with `JsonDocument.ParseAsync` and default options,
so `{"name":"a.3de","name":"b.3de"}` takes `b.3de`. E101 closed the envelopes of commands and queries only.
**[Observed]** by an agent. **[Recommended]** `JsonDocumentOptions { AllowDuplicateProperties = false }`.

### The render side and the desktop

#### V56 · Medium · The Value pad of an angle shows its cosine and its sine in millimetres · Confirmed by reading

`3DEngine/Ui/DesktopUi.cs:817-825` names the two values of each constraint "x" and "y" with the unit of the document.
For `angle` the pad shows `x = 0.866025403784439` and `y = 0.5` in mm, and the engine refuses each pair that is not a
unit vector to 1e-9, so a person cannot change an angle in practice. The tangent of two curves shows `length = 1`.
**[Observed]** I read the lines; an agent ran the probe. **[Recommended]** The Value pad of an angle takes degrees
(`SketchText.Angle`, `CosineAndSine`), and a tangent of two curves has no Value button.

#### V57 · Low · A pending circle or arc uses the positions of its first taps, not the current positions

`3DEngine.Core/Sketching/SketchTool.cs:228`, `:252` and `:254`. A centre that a person moves between the two taps gives
a radius from its old position, and the new end of an arc is not on the circle of a moved start. **[Observed]** by an
agent. **[Recommended]** Read the centre and the start from the picture at each tap.

#### V58 · Low · During a press on a handle, before the slop, the wheel and undo still act

`3DEngine.Core/Input/IntentMachine.cs:290`: `Dragging` needs `Started`. A press on the arrow X, a wheel step, Control+Z,
then a motion past the slop give `Zoom, Undo, GizmoStart, GizmoMove, GizmoEnd`. **[Observed]** I read the line; an agent
ran the probe. **[Recommended]** Test `_gizmo is { Cancelled: false }`.

#### V59 · Low · The window offers On curve and Tangent where the engine refuses them

`DesktopUi.cs:760-761`. A point that defines the curve (its centre, start or end) gets On curve, and a line with an end
at the centre gets Tangent; the tap gives `E-SKETCH-CONSTRAINT-INVALID`. **[Observed]** by an agent.
**[Recommended]** The fit rules of the window take the item checks of `SketchConstraints.Refusal`.

#### V60 · Low · `CosineAndSine` computes the general case from the degrees, not from the turn

`DesktopUi.cs:847`. 1e20 degrees gives (−0.952, 0.306), and the turn of 280 degrees gives (0.174, −0.985); 1e308 gives
(NaN, NaN). **[Observed]** I read the line. **[Recommended]** Use `turn`, and refuse a value that is not finite.

#### V61 · Low · An end of an arc on the ray of the start makes a second point at the position of the start

`SketchTool.cs:254`. Taps at (0, 0), (5, 0) and (10, 0) give two points at (5, 0) and an arc of a full turn.
**[Observed]** by an agent. **[Recommended]** A new end in the direction of the start ends the curve.

#### V62 · Low · A line within 22 pixels takes the pick before a nearer curve

`SketchTool.cs:102`: `NearestPoint ?? NearestLine ?? NearestCurve`. Where a line is tangent to a circle, a tap on the
circle selects the line. **[Observed]** by reading. **[Recommended]** Take the nearest of the lines and the curves.

### Tests, gates and the pipeline

#### T103 · High · The check of the tasks of a gate does not bind the real gate files · Confirmed by a run

`eng/repo-gates/DiscardedTasks.cs:50` compiles the gate files with no implicit global using, and no gate file names
`System.Threading`. So `Task`, `Thread`, `SemaphoreSlim` and `TaskCreationOptions` do not bind, and the checks of T74,
T94, T95 and T100 see nothing in a real file. The self check (`:161`) gives each form explicit usings, so it passes. I
added `private static void ProbeThread() => new Thread(() => { }).Start();` to a gate file: 310 runs, 0 failed. With
`System.Threading.Thread` the run failed. **[Observed]** **[Recommended]** Add the implicit usings of the SDK and of
the test project to the compilation, fail a run whose compilation has an error, and give the self check the same
preamble as a real file.

#### T104 · Medium · The reader of constants has no implicit global using either

`Engine.Tests/Governance/ConstantStrings.cs:47`. `"E" + ("-SKETCH" + ("-" + nameof(GC)))` in an engine source passes
both diagnostics gates; with `nameof(System.GC)` they give `E-SKETCH-GC`. **[Observed]** by an agent.
**[Recommended]** The same preamble as T103.

#### T105 · Medium · The grant of internals to each project makes a name ambiguous that the build resolves

`ConstantStrings.cs:37`. An internal and a public type `Tag` in two namespaces of `Engine.Core`, both imported by
`Engine.Cli`, give CS0104 in the gate and no constant; the build compiles, because `Engine.Core` grants internals to
`Engine.Tests` only. **[Observed]** by an agent. **[Recommended]** Grant what the items `InternalsVisibleTo` of each
project grant.

#### T106 · Medium · The counter of the review misses a heading of a version in other forms

`Engine.Tests/Governance/CodebaseReviewGateTests.cs:348-349` reads a heading with `#`, `v` and a digit. `## version
0.130`, a setext heading, `## **v0.130**`, a heading in a quote or a list, `<h2>`, a soft hyphen, a Greek nu and a
full-width v pass. **[Observed]** by an agent. **[Recommended]** Refuse each heading line of the ledger that is not
`## v` or a level-three heading, and each setext underline.

#### T107 · Medium · An output of a task with the item name `Compile` passes the check of T101

`Engine.Tests/Governance/DependencyDirectionGateTests.cs:453` reads the elements `Compile` only. A target with
`<CreateItem Include="ProbeUi2.txt"><Output TaskParameter="Include" ItemName="Compile"/></CreateItem>` adds the file,
and each gate passes. **[Observed]** by an agent. **[Recommended]** Refuse each `Output` whose item name is `Compile`,
as T88 refuses `IntermediateAssembly`.

#### T108 · Low · The control byte gate permits other characters that do not show

`DependencyDirectionGateTests.cs:216-224`. U+00AD, U+200C, U+200D, U+2061 to U+2064, U+206A, U+180E, U+FE0F, U+3164, a
tag character, a lone byte 0x81 and an overlong form pass. **[Observed]** by an agent; no tracked file holds one.
**[Recommended]** Decode with a strict UTF-8 decoder and refuse each character of the category Format after offset 0.

#### T109 · Low · The scanner of parts of codes reads the syntax, not the values, of `string.Join`

`Engine.Tests/Diagnostics/DiagnosticsScanner.cs:98-110`. `string.Join("-", new[] { "E", "SKETCH", Tag })`, a constant
severity, `string.Concat("E", "-", "SKETCH-X")` and `'E' + "-SKETCH"` pass. **[Observed]** by an agent.
**[Recommended]** Read the constant values of the arguments with the semantic model.

### Correct

**[Observed]** by the agents, with the probes that passed:

- The polygon of a curve near a sweep of 0, of π and of 2π, for radii of 1e-6 and 1e9, and after 31 turns at 1e6:
  each vertex is on the circle to 1e-9, and no value is NaN. Loops of two arcs, a lens, a slot, a pie walked
  backwards and a nearly full arc closed by a line are correct.
- A negative radius gives "not solved" and an unchanged record; `SetSketchConstraintValue` of a radius −3 is refused.
- The flag of an entry is false after `ApplyEntry`, a live repeat after the cache forgot it is refused, and a replay
  and then a live command in one flow restore the value of the caller.
- A repeated parameter, a repeated member in a nested value, a repeated `name` and a second `commandId` give HTTP 400.
- The arc and circle tools, the selection of three items, Symmetric with O, the clip of V52 for each boundary, and V51
  over five cameras.

**Not verified.** An extrude of the circle of E102 on Manifold; the open of the files of E103 and E104 end to end; the
desktop loop with a person; a cycle of project references in `ConstantStrings`.

## 4 The next planned task

The plan of 2026-10-30 and the answer "1 3 4" are complete. `docs/templates.md`, section 7, puts the high findings E102
and T103 into a task before the next product phase: P0.51 (TASK-0130). The next product row is R11, "Escalated
selection and a snap engine", 4 to 6 hours, with no slice. This review proposes its first slice R11a (TASK-0131), the
snap of a tap of the sketch. P0.52 (TASK-0132) holds the medium and low findings of the engine and the gates after
R11a, and P0.53 (TASK-0133) holds those of the desktop.

## 5 Research for that task

**[Observed]** I read the cited code.

- **A tap of the sketch has one position.** `3DEngine/SketchActions.cs:70` gives the tool the position of the ray on
  the plane, rounded to the step of the grid (ADR-0034 item 3), and `SketchTool.cs:65` uses a point within 22 pixels.
  ADR-0034 says that the step is presentation, not a snap engine (R11).
- **The picture has what a snap needs.** `SketchOverlay.Picture` gives the pixel of each point, the points of each
  line, and the segments of each curve, so the tool can find a midpoint, a point on a line or a curve, and a centre,
  with no new query.
- **A snap is presentation.** It gives a position to `AddSketchPoint`, as the grid does; the command records the
  numbers, so the log holds no snap (ADR-0007).
- **Escalated selection needs a face and an edge identity**, which the engine does not have before a B-Rep (K4). It
  stays out of R11a.

## 6 Recommended approach

**[Recommended]** R11a in this order, under the answer Q2 A:

1. `SketchTool` takes, for a tap with no point within 22 pixels: the midpoint of a line within 22 pixels, then the
   nearest point on a line or a curve within 22 pixels, then the position of the grid. The tap adds a point there,
   and no constraint.
2. The overlay draws a mark at the snap of the pointer, so a person sees it before the tap.
3. ADR-0041 holds the order and the tolerance. The estimate is 3 to 4 agent hours, with a picture of the GPU.

## Questions for the owner

**Q1. The refusals at an add for an entry of an open (E103).**
- A: Under `ApplyingEntry`, the refusals of a tangent through the centre and of an arc at its centre do not act. The
  solve then gives no solution, and the sketch shows as not solved, as the rule of E97 says.
- B: An open refuses such a file, and the error names the line and the reason.

**Recommended: A.** It keeps each file that an earlier engine wrote, as Q1 A of the last review did. The standing
answer of the owner of 2026-10-31 takes A.

**Q2. The first slice of R11.**
- A: R11a, the snap of a tap of the sketch to a midpoint, a point on a line or a curve, and the grid, with no new
  constraint (sections 5 and 6).
- B: R11a with a constraint: a snap on a line adds `pointOn`, and a snap on a midpoint adds `midpoint`.
- C: No R11 now; the owner chooses the next product phase.

**Recommended: A.** A constraint from a snap changes the sketch more than the person asks, and B can come later with
a setting. The standing answer of the owner of 2026-10-31 takes A.
