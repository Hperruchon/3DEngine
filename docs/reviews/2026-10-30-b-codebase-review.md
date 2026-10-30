---
date: 2026-10-30
commit: dab1355
ledger: v0.121
previous: 2026-10-30-codebase-review.md
---

# Codebase review — 2026-10-30 (b)

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

This is the fourteenth codebase review. The review of 2026-10-30 examined v0.112. After it came 4 product milestones
(K0d, K2d, R9, P8e) and 9 ledger entries (v0.113 to v0.121). The limits are four and twelve, so the next product
milestone makes a review due. K2e is the next product phase, so the review comes before it.
`Engine.Tests/Governance/CodebaseReviewGateTests.cs` holds the rule, and `docs/templates.md`, section 7, gives the
form.

**Status: a dated record.** This review describes the code at one commit. `docs/CURRENT-STATE.md` is the
authority for what exists. A task, a register entry or an ADR closes a finding. An edit to this file does not.

| Item | Value |
|---|---|
| Code state | `main` at `dab1355`, the merge of v0.121. Its tree is the tree of `6e065d5`, the tip of the branch of P8e |
| Branch of this review | `codebase-review-2026-10-30-b` |
| Tests | 1,248 on this computer (910 in `Engine.Tests`, 210 in `3DEngine.Tests`, 128 in `3DEngine.ImageTests`) |
| Pipeline | Run 41125608049 on `14486f0` (v0.120) and the branch run of `6e065d5`: green on Ubuntu, Windows and macOS |
| Code changed by the review | none |

**Labels.** **[Observed]** means: I read it in the code, or I saw it in a run. **[Inferred]** means: a
conclusion from the code, and no run shows it. **[Recommended]** means: a proposal, and the owner decides.

**Method.** Nine tasks changed code or records after `a839bb7`, the commit of the last review: TASK-0111 (the
review), TASK-0112 (P0.44), TASK-0115 (P0.46), TASK-0116 (K0d), TASK-0117 (K2d), TASK-0114 (P0.45), TASK-0113 (R9),
TASK-0118 (P0.47) and TASK-0119 (P8e). Three review agents worked in parallel at `6e065d5`, each one in its own git
worktree: the engine, the desktop, and the gates with the HTTP surface. Each agent wrote probe tests that only its
worktree holds. I ran each failing probe again: the 6 of the engine, the 7 of the desktop, and P1 and P2 of the HTTP
surface. I injected the heading of T96 and its control. I wrote and ran the probe of E100. A line that says "an agent
ran it" means that I read the cited code and did not repeat the run.

## Summary

- **The fixes of the last review held for the forms that they named.** T88, E94 to E96, T89 to T95 and V46 to V50
  are fixed, and E11, E14 and E16 of the earlier reviews are fixed. **[Observed]**
- **A solve can give a circle a negative radius or a radius of zero, and the sketch shows as solved (E97, high).**
  Two circles with fixed centres 10 apart, a radius of 20 for one and an outer tangent give the radii −10 and 20. A
  tangent of a line through the fixed centre stores a circle of radius 0 in the Document. **[Observed]**
- **A file that v0.117 could write does not open since v0.118 (E100, medium).** The guard of E95 refuses a second
  move with the same command identifier, and ADR-0015 item 8 says that an open applies each entry again. This is a
  conflict between a decision and the code, so Q1 asks the owner. **[Observed]**
- **A repeated member of a JSON envelope removes the check of the version (E101, medium).** The surface keeps the
  last value of a member, so `"expectedDocumentVersion":999999,"expectedDocumentVersion":null` applies. **[Observed]**
- **The gizmo and the overlay have two medium defects (V51, V52).** A drag of an arrow past the vanishing point of its
  axis puts the body behind the eye, and the limit of V49 drops a sketch line whose far part is in the window.
  **[Observed]**
- **Six forms pass a gate (T96 to T101, medium).** A ledger heading with two spaces hides a product milestone from the
  review counter. The diagnostics gate misses a joined code through a type of one name in a referenced project, a
  reference in a `Directory.Build.props` file, and a code in an interpolated string. **[Observed]** for T96;
  **[Observed]** by an agent for the others.

## Measures

Each review repeats these measures at its commit, so that two reviews show how the code changes. A source
line is a line of a `.cs` or `.razor` file that git tracks.

| Measure | Value | Method |
|---|---|---|
| Projects in the solution | 14 | `.csproj` lines in `3DEngine.sln`. The last review gave 14. Git tracks 15 project files: the native package is not in the solution. |
| Production source lines | 19,877 | Each tracked `.cs` and `.razor` file outside `Engine.Tests`, `3DEngine.Tests` and `3DEngine.ImageTests`, 212 files. The last review gave 18,359 in 204 files. |
| Test source lines | 22,181 | `Engine.Tests` (16,740 in 93 files), `3DEngine.Tests` (2,308 in 17 files) and `3DEngine.ImageTests` (3,133 in 19 files). The last review gave 20,707. |
| Tests | 1,248 | `dotnet test 3DEngine.sln`: 910, 210 and 128 passed, 0 failed, 0 skipped. The last review gave 1,145. |
| Gate classes | 19 | Tracked files whose name ends in `GateTests.cs`. `eng/repo-gates` gives 283 runs of 131 checks. The last review gave 19. |
| Build warnings (clean build) | 0 | `dotnet build 3DEngine.sln --no-incremental`. |
| Open register entries | 14 | Section "Open" of `docs/register.md`; the limit is 15. This review adds R-0046, so the count becomes 15. The last review gave 14. |
| ADRs | 39 | `docs/adr/0001` to `0039`, with 0003 and 0011 in `docs/adr/archive/`. In force: 37 (17 accepted, 20 amended). None is proposed. |
| Findings: critical | 0 | Each finding that is open at this commit, from any review. The last review gave 0. |
| Findings: high | 6 | The same method: V2, V3, T1, T4, T31, E97. The last review gave 6. |
| Findings: medium | 29 | The same method: E5, E6, E12, E13, E18, E19, E21, E22, E25, E27, E28, E33, E100, E101, V4, V51, V52, T2, T5, T10, T17, T32, T37, T96, T97, T98, T99, T100, T101. The last review gave 29. |
| Findings: low | 30 | The same method: E17, E24, E26, E29 to E32, E34, E35, E37 to E39, E57, E84, E98, E99, V7, V15, V25, V29, V53 to V55, T7, T13, T23, T25, T102, C1, P1. The last review gave 32. |
| Lines in the rule files | 2,425 | The ten rule files of `docs/reviews/2026-09-25-rules-review.md`, section 1, that still exist, counted with the method of that section. The last review gave 2,388. |
| Sentences with "must" | 57 | `grep -o -w -i must` on the same ten files. The last review gave 57. |
| Sentences with "do not" | 55 | `grep -o -i -E '\bdo not\b'` on the same ten files. The last review gave 54. |

A finding that is "fixed for its forms" counts as fixed: a new identifier holds each form that a later probe
found. E13, T32 and E84 are fixed in part and count as open.

## Findings of the previous review

- **E1** — Fixed. TASK-0035, v0.43. No change.
- **E2** — Fixed. TASK-0034, v0.39. No change.
- **E3** — Fixed. TASK-0036, v0.44. No change.
- **E4** — Fixed. TASK-0034, v0.39. No change.
- **E5** — Open. No change: `Engine.Api.Http/Endpoints/JsonParameters.cs` has no commit since `a839bb7`. Register entry R-0027.
- **E6** — Open. No change: `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` has no commit since `a839bb7`. Register entry R-0027.
- **E7** — Fixed. TASK-0035, v0.43. No change.
- **E8** — Fixed. TASK-0044, v0.37. No change.
- **E9** — Fixed, and it holds. No change.
- **E10** — Fixed. TASK-0035, v0.43. No change.
- **E11** — Fixed. TASK-0115, v0.115.
- **E12** — Open. No ledger entry since v0.112 names it. Register entry R-0027.
- **E13** — Fixed in part. No change.
- **E14** — Fixed. TASK-0115, v0.115.
- **E15** — Fixed. TASK-0044, v0.37. No change.
- **E16** — Fixed. TASK-0115, v0.115.
- **E17** — Open. No change: `Engine.Core/CommandBus.cs` has no commit since `a839bb7`. Register entry R-0028.
- **E18** — Open. No change: `Engine.Core/CommandBus.cs` has no commit since `a839bb7`. Register entry R-0033.
- **E19** — Open, and smaller. No ledger entry since v0.112 names it. Register entry R-0033.
- **E20** — Fixed, and it holds. No change.
- **E21** — Open, and wider by decision. No ledger entry since v0.112 names it. Register entry R-0033.
- **E22** — Open. No change: `Engine.Core/CommandBus.cs` has no commit since `a839bb7`. Register entry R-0033.
- **E23** — Fixed. TASK-0051, v0.48. No change.
- **E24** — Open. No change: `Engine.Api.Http/WebSockets/WireMessage.cs`, `Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs` have no commit since `a839bb7`. Register entry R-0033.
- **E25** — Open. No change: `Engine.Core/CommandBus.cs` has no commit since `a839bb7`. Register entry R-0036.
- **E26** — Open. No change: `Engine.Contracts/Document.cs` has no commit since `a839bb7`. Register entry R-0036.
- **E27** — Open. No change: `Engine.Core/Replay.cs` has no commit since `a839bb7`. Register entry R-0036.
- **E28** — Open. No change: `Engine.Core/CommandBus.cs` has no commit since `a839bb7`. Register entry R-0036.
- **E29** — Open. No change: `Engine.Core/Replay.cs` has no commit since `a839bb7`. Register entry R-0036.
- **E30** — Open. No ledger entry since v0.112 names it. Register entry R-0036.
- **E31** — Open, and smaller. `Engine.Core/Sketch/SketchResults.cs` changed for E94 only (TASK-0114); the comments of the other parts did not change. Register entry R-0036.
- **E32** — Open. No ledger entry since v0.112 names it. Register entry R-0036.
- **E33** — Open. No change: `Engine.Api.Http/WebSockets/WireMessage.cs` has no commit since `a839bb7`. Register entry R-0036.
- **E34** — Open. `Engine.Geometry.Manifold/ManifoldGeometryBackend.cs` changed for E14 only (TASK-0115); the version that `/schema/backend` gives did not change. Register entry R-0036.
- **E35** — Open. No ledger entry since v0.112 names it. Register entry R-0036.
- **E36** — Fixed. TASK-0079, v0.85. No change.
- **E37** — Open. No change: `Engine.Cli` has no commit since `a839bb7`. Register entry R-0036.
- **E38** — Open. No ledger entry since v0.112 names it. Register entry R-0036.
- **E39** — Open. No change: `3DEngine/DesktopSurface.cs` has no commit since `a839bb7`. Register entry R-0036.
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
- **E57** — Open. No change: `Engine.Tests/Http/DesktopSurfaceTests.cs` has no commit since `a839bb7`. Register entry R-0039.
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
- **V2** — Open. The commits in `3DEngine.Vulkan/` since `a839bb7` add `OverlayRenderer.cs` and its two shaders only. Register entry R-0022.
- **V3** — Open. The commits in `3DEngine.Vulkan/` since `a839bb7` add `OverlayRenderer.cs` and its two shaders only. Register entry R-0022.
- **V4** — Open. No ledger entry since v0.112 names it. Register entry R-0027.
- **V5** — Fixed. TASK-0076, v0.74. No change.
- **V6** — Fixed. TASK-0076, v0.74. No change.
- **V7** — Open. No change: `3DEngine.Vulkan/Ui/UiRenderer.cs`, `3DEngine.Vulkan/Ui/UiTexture.cs`, `3DEngine.Vulkan/GpuBuffer.cs` have no commit since `a839bb7`. Register entry R-0030.
- **V8** — Fixed. TASK-0074, v0.75. No change.
- **V9** — Fixed. TASK-0074, v0.75. No change.
- **V10** — Fixed. TASK-0074, v0.75. No change.
- **V11** — Fixed in part. TASK-0074, v0.75. No change.
- **V12** — Fixed, and changed by decision. No change.
- **V13** — Fixed for its parts. No change.
- **V14** — Fixed. TASK-0074, v0.75. No change.
- **V15** — Open by decision, and smaller. `3DEngine/SessionBodies.cs` meshes each body one time for each frame (V48, TASK-0118). It still waits for the device for each consumed or removed body and for each new geometry key.
- **V16** — Fixed. TASK-0079, v0.85. No change.
- **V17** — Fixed. TASK-0079, v0.85. No change.
- **V18** — Fixed for its forms. TASK-0090, v0.93. No change.
- **V19** — Fixed, with no injection. TASK-0079, v0.85. No change.
- **V20** — Fixed. TASK-0079, v0.85. No change.
- **V21** — Fixed by reading, with no test. TASK-0079, v0.85. No change.
- **V22** — Fixed for its forms. TASK-0079, v0.85. No change.
- **V23** — Fixed. TASK-0079, v0.85. No change.
- **V24** — Fixed. TASK-0079, v0.85. No change.
- **V25** — Open. No change: `3DEngine.Vulkan/3DEngine.Vulkan.csproj` has no commit since `a839bb7`. Register entry R-0020.
- **T1** — Open in one part of three. No ledger entry since v0.112 names it. Register entry R-0023.
- **T2** — Open in one part of five. No ledger entry since v0.112 names it. Register entry R-0027.
- **T3** — Fixed. TASK-0047, v0.42. No change.
- **T4** — Open. No change: `Engine.Tests/Hosting/DispatchSurfaceGateTests.cs` has no commit since `a839bb7`. Register entry R-0031.
- **T5** — Open in one part of three. No ledger entry since v0.112 names it. Register entry R-0031.
- **T6** — Fixed. TASK-0043, v0.36. No change.
- **T7** — Open in four parts of five. No ledger entry since v0.112 names it. Register entry R-0031.
- **T8** — Fixed. TASK-0047, v0.42. No change.
- **T9** — Fixed. TASK-0047, v0.42. No change.
- **T10** — Open. No ledger entry since v0.112 names it. Register entry R-0031.
- **T11** — Fixed. TASK-0053, v0.51. No change.
- **T12** — Fixed. TASK-0059, v0.55. No change.
- **T13** — Open. No change: `.github/workflows/ci.yml` has no commit since `a839bb7`. Register entry R-0031.
- **T14** — Fixed. TASK-0053, v0.51. No change.
- **T15** — Fixed. TASK-0050, v0.47. No change.
- **T16** — Fixed. TASK-0066, v0.67. No change.
- **T17** — Open. No ledger entry since v0.112 names it. Register entry R-0031.
- **T18** — Fixed. TASK-0049, v0.46. No change.
- **T19** — Fixed. TASK-0049, v0.46. No change.
- **T20** — Fixed. TASK-0049, v0.46. No change.
- **T21** — Fixed. TASK-0049, v0.46. No change.
- **T22** — Fixed. TASK-0049, v0.46. No change.
- **T23** — Open. No ledger entry since v0.112 names it. Register entry R-0031.
- **T24** — Fixed. No change.
- **T25** — Open in part. No ledger entry since v0.112 names it. Register entry R-0031.
- **T26** — Fixed. TASK-0057, v0.54. No change.
- **T27** — Fixed. TASK-0058, v0.56. No change.
- **T28** — Fixed. No change.
- **T29** — Fixed. TASK-0057, v0.54. No change.
- **T30** — Fixed. TASK-0057, v0.54. No change.
- **T31** — Open. No change: `eng/repo-gates/RepoGates.csproj` has no commit since `a839bb7`. Register entry R-0031.
- **T32** — Fixed in part. No change.
- **T33** — Fixed. TASK-0057, v0.54. No change.
- **T34** — Fixed. TASK-0057, v0.54. No change.
- **T35** — Fixed. TASK-0057, v0.54. No change.
- **C1** — Open. No change: `Engine.Contracts/Geometry/IBRepOps.cs`, `Engine.Contracts/Geometry/IFeatureIdMap.cs` have no commit since `a839bb7`. Register entry R-0027.
- **P1** — Open, and smaller. No ledger entry since v0.112 names it. Register entry R-0027.
- **T36** — Fixed. TASK-0061, v0.61. No change.
- **T37** — Open in part. No ledger entry since v0.112 names it. Register entry R-0031.
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
- **V29** — Open. No ledger entry since v0.112 names it.
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
- **E94** — Fixed. TASK-0114, v0.118.
- **E95** — Fixed. TASK-0114, v0.118.
- **E96** — Fixed. TASK-0114, v0.118.
- **V46** — Fixed. TASK-0118, v0.120.
- **V47** — Fixed. TASK-0118, v0.120.
- **V48** — Fixed. TASK-0118, v0.120.
- **V49** — Fixed. TASK-0118, v0.120.
- **V50** — Fixed. TASK-0118, v0.120.
- **T88** — Fixed. TASK-0112, v0.114.
- **T89** — Fixed. TASK-0114, v0.118.
- **T90** — Fixed. TASK-0114, v0.118.
- **T91** — Fixed. TASK-0114, v0.118.
- **T92** — Fixed. TASK-0114, v0.118.
- **T93** — Fixed. TASK-0114, v0.118.
- **T94** — Fixed. TASK-0114, v0.118.
- **T95** — Fixed. TASK-0114, v0.118.

## 1 Current architecture

**[Observed]** `FeatureGraph.Of(log)` (`Engine.Core/Sketch/FeatureGraph.cs`) gives the command in effect that creates
each handle and the command that consumes it. After an edit of a sketch, `SketchRebuild` takes each consumed extrude
of the sketch as a seed, walks to its live ends, and evaluates the upstream of each end under
`GeometryKeys.Derive(edit, body)` (ADR-0039). A failed step gives the end the fault `W-SKETCH-REBUILD-FAILED`.

**[Observed]** The sketch has the circle and the arc (K2d). The radius of each circle is a variable of the solver
after the points, and an arc gives the row |s − c| − |e − c|. `tangent`, `radius`, `pointOn` and `equal` act on a
curve. An extrude refuses a profile with a curve (`Engine.Core/Sketch/SketchProfile.cs:15`). The desktop does not
draw a curve.

**[Observed]** The desktop has a translate gizmo (R9, ADR-0038). `TranslateGizmo` in `3DEngine.Core/Gizmo/` gives
the layout, the hit test and the offset of a drag; `3DEngine/GizmoActions.cs` moves the drawing during the drag and
sends one `Translate` at the release; `3DEngine.Vulkan/OverlayRenderer.cs` draws the handles with no depth test. The
numeric pad shows the length unit of the session (P8e).

## 2 Patterns in use

- **A new solver variable has no domain.** The radius is the first variable of the solver that has a sign, and the
  solver accepts each root of the equations. E97 is the result. **[Observed]**
- **A guard of the live session also acts on an open.** The guard of E95 runs in the handler, and the open applies
  each entry through the same handler, so a file that an earlier engine wrote can fail (E100). **[Observed]**
- **Each correction holds for its forms, and a probe finds the next form.** T96 follows T93, T97 and T98 follow T90,
  T99 follows T91, T100 follows T94 and T95, T101 follows T89, T102 follows T92, E101 follows E16, and V52 is a
  result of the fix of V49. **[Observed]** for T96 and E101; **[Observed]** by an agent for the others.
- **A drag maps a pixel to the scene with no check of the side of the eye.** The square handle of the gizmo has the
  check (`Picker.OnPlane`, `t > 0`), and the arrow has none (V51). **[Observed]** by reading and by an agent.

## 3 Review of the code

### The engine and the hosts

#### E97 · High · A solve can give a circle a negative radius or a radius of zero, and the sketch shows as solved · Confirmed by a run

`Engine.Core/Sketch/SketchSolver.cs:244` copies the radius variable into the record, and `Converge` (`:72-80`)
accepts each solution of the equations. Nothing keeps the radius positive. `AddSketchCircle` refuses a radius of 0
(`Engine.Core/Commands/AddSketchCircleCommandHandler.cs:75`), but a solve can make one.

- Two circles with fixed centres (1, 1) and (11, 1), `radius` 20 on the second, and an outer `tangent`: the radii
  are −10 and 20, and `Status` gives solved with 0 free degrees.
- A fixed line (0, 1) to (10, 1), a fixed centre (5, 1), a circle of radius 2, then `AddSketchConstraint tangent`
  through a session: Applied, and the Document holds a circle of radius 0.
- A `pointOn` of a point that is fixed at the fixed centre, and a `tangent` of a line with an end at the centre
  (`SketchConstraints.cs:75-76` accepts it), give a radius of 0.

**[Observed]** I ran the four probes of the engine agent again. **[Recommended]** `Converge` gives no solution when a
radius is not greater than 0, so the record keeps the positions of the command, as for each other solve that fails
(`SketchSolver.cs:30-32`). `Refusal` refuses a `tangent` of a line with an end at the centre of the curve, as
`pointOn` refuses a point of its own curve. A bit test holds each case on the three runners.

#### E98 · Low · `AddSketchArc` accepts an arc of radius zero, and the solve then moves the points of the person · Confirmed by a run

`Engine.Core/Commands/AddSketchArcCommandHandler.cs:80` compares the identifiers of the three points, not their
positions. A centre and a start at (2, 2) and an end at (5, 2) give Applied, and the solve moves the start and the
end to (3.5, 2). The same circle of radius 0 gives `E-SKETCH-CURVE-INVALID`. **[Observed]** I ran the probe.
**[Recommended]** Refuse an arc when |s − c| or |e − c| is 0 at the add.

#### E99 · Low · `Extrude` refuses a command that a client sends again only because the backend holds the key · Confirmed by a run

`ExtrudeCommandHandler` has no `SketchResults.InUse` check. After the cache forgets 1,024 results, a repeated
`Extrude` gives `E-GEOM-NATIVE-OP` from the backend, and a repeated `AddSketchCircle` gives `E-SKETCH-ID-IN-USE`.
The version does not change, so the Document is correct; the refusal depends on the state of the backend.
**[Observed]** by an agent. **[Recommended]** Decide it with E100: a new guard in a handler also acts on an open.

#### E100 · Medium · A file in which a client sent a move again does not open since v0.118 · Confirmed by a run

`SketchResults.InUse` (`Engine.Core/Sketch/SketchResults.cs:63-66`) refuses a command whose identifier the log
holds, and since TASK-0114 (E95) `MoveSketchPoint`, `SetSketchConstraintValue` and `DeleteSketchItem` call it. Up to
v0.117 a move that a client sent again after the cache forgot it applied a second time, so the file holds two entries
with one command identifier. ADR-0015 item 8 and the comment of `Engine.Core/Persistence/DocumentFile.cs:196-199`
say that an open applies such an entry again, "as the live session applied it (E75)". The open applies each entry
through the same handlers, so it now refuses the file: "E-IO-LOAD-FAILED: The entry of version 4
(MoveSketchPoint) gave Rejected, with E-SKETCH-ID-IN-USE". **[Observed]** I wrote the file with a session and added
the second entry as v0.117 wrote it, then ran `DocumentFile.Check`. **[Inferred]** E86 (TASK-0106) gave the four add
commands the same guard, so an add that a client sent again before v0.110 has the same effect. A decision and the
code disagree, so Q1 asks the owner.

#### E101 · Medium · A repeated member of a JSON envelope keeps the last value, and the check of the version goes · Confirmed by a run

`Engine.Api.Http/Json/ApiJson.cs:18-28` does not refuse a repeated property, and `CommandsEndpoint.cs` and
`QueriesEndpoint.cs` read their envelopes with these options. `"expectedDocumentVersion":999999,
"expectedDocumentVersion":null` gives HTTP 200 and Applied, and `"echo":"a","echo":"b"` gives the echo `b`. This is
the form of E16 by another spelling. **[Observed]** I ran P1 and P2 of the agent. **[Recommended]** Set
`AllowDuplicateProperties = false` in `ApiJson.Options`, and add a test for each envelope and for a parameter.

### The render side and the desktop

#### V51 · Medium · A drag of an arrow past the vanishing point of its axis puts the body behind the eye · Confirmed by a run

`3DEngine.Core/Gizmo/TranslateGizmo.cs:141-143` takes the nearest point between the axis and the ray, and `Nearest`
(`:230-240`) does not check that the point is in front of the eye. With yaw 60, pitch 0 and distance 10 in a window
of 800 × 600, a drag of the X arrow toward the left gives −335.9 at x = 110 and +356.9 at depth −299 at x = 90. The
body jumps to the other side, behind the camera, and the release sends that `Translate`. **[Observed]** I ran the
probe and read `Nearest`. **[Recommended]** Compute the parameter of the ray, and give no offset when it is not
greater than 0 or the point is nearer than the near plane, so the drag keeps its last offset (ADR-0038 item 8).

#### V52 · Medium · The limit of V49 removes a sketch line that passes behind the eye, also when its far part is in the window · Confirmed by a run

`3DEngine/SketchOverlay.cs:186-188` drops a segment with a pixel farther than `PixelLimit` (10⁷). `Segment` cuts a
line at `Near` = 10⁻³ in front of the eye (`:42`, `:156-166`), and the pixel of that cut is farther than the limit.
With the datum XZ, the front camera at distance 10 and a line from (20, −30) to (20, 20), the far end is at the
pixel (951.8, 360), and the overlay gives no line. Before V49 the line showed. **[Observed]** I ran the probe.
**[Recommended]** Clip the segment to the window with a margin before the limit test.

#### V53 · Low · A zoom, an orbit, an undo or a hide still acts during a drag of the gizmo · Confirmed by a run

`3DEngine.Core/Input/IntentMachine.cs:97-101` and `Key` (`:261-280`) give their intents while a drag of the gizmo
runs. An agent saw `GizmoStart, GizmoMove, Zoom, Orbit, Undo, Hide, GizmoMove, GizmoEnd`, and a zoom of 1.25 with no
motion of the pointer changed the offset from 0.4811 to 0.6014. ADR-0038 does not say what other input does during a
drag, so Q2 asks the owner. **[Observed]** I ran the probes.

#### V54 · Low · The release sends a `Translate` of a body that left the document during the drag · Confirmed by a run

`3DEngine/GizmoActions.cs:55-60` sends the move of `_body` with no check that the body is live. An agent
`Translate` or an undo during the drag gives `E-GEOM-BODY-NOT-FOUND` at the release. The log does not change.
**[Observed]** I ran the probes. **[Recommended]** At the end, send nothing when the body has no pivot.

#### V55 · Low · A body that is created and changed in one frame gives two meshes · Confirmed by a run

`3DEngine/SessionBodies.cs:142` and `Superseded` (`:158-166`) skip a change that a later change follows, but not a
creation. An `Extrude` and five `MoveSketchPoint` commands before one update give 2 new meshes, and 1 is enough.
**[Observed]** I ran the probe, on a GPU. **[Recommended]** Apply a creation with the state of the last change of
the frame, and skip those changes.

### Tests, gates and the pipeline

#### T96 · Medium · A ledger heading with a near form hides a product milestone from the review counter · Confirmed by a run

`Engine.Tests/Governance/CodebaseReviewGateTests.cs:298-299` reads a ledger heading with `^## v` only. A fifth
product milestone as `## v0.122 … (P8e, TASK-0119)` fails `A_Review_Is_Not_Overdue`, and the same line with two
spaces after `##` passes each test. An agent also passed a leading space, `## V0.122` and a tab. Markdown shows each
one as a heading. **[Observed]** I injected the control and the form with two spaces. **[Recommended]** Match each
line that starts with optional spaces, `#` and any case of `v`, and refuse each match that is not the exact form.

#### T97 · Medium · A type of one name in a referenced project hides a joined code from the diagnostics gate · Confirmed by a run

`Engine.Tests/Governance/ConstantStrings.cs:30-33` puts the trees of a project and of its referenced projects into
one compilation. Two internal types `Engine.Probe.Shared` in `Engine.Cli` and `Engine.Core` give CS0101 there, and
the joined code `E-SKETCH-DUPX` has no value; the build of `Engine.Cli` has no warning. **[Observed]** by an agent.
**[Recommended]** Compile each referenced project on its own and give it as a `CompilationReference`.

#### T98 · Medium · The reader of constants finds a project reference in the project file only · Confirmed by a run

`ConstantStrings.cs:59` reads `<project>/<project>.csproj`. A reference to `Engine.Core` in
`Engine.Cli/Directory.Build.props` builds, and a code joined from `DiagnosticCodes.SketchIdInUse` passes. The
dependency gate reads the same reference through `FilesThatMsBuildReads`. **[Observed]** by an agent.
**[Recommended]** Use `FilesThatMsBuildReads` in `ConstantStrings`.

#### T99 · Medium · The diagnostics gate misses a code in an interpolated string or built by a call · Confirmed by a run

`Engine.Tests/Diagnostics/DiagnosticsScanner.cs:61-66` reads constant expressions only. `$"E-SKETCH-{Tag}"`,
`"E" + ("-SKETCH-" + Tag)`, `string.Join("-", "I", "SKETCH", Tag)` and `"e-sketch-hidden".ToUpperInvariant()` pass.
**[Observed]** by an agent. **[Recommended]** Read each text part of an interpolated string, and each literal that
the pattern of a code matches in part, with no regard to case.

#### T100 · Medium · The checks of T94 and T95 miss near forms and the helper files of the gates · Confirmed by a run

`eng/repo-gates/DiscardedTasks.cs:80-86` misses `TaskContinuationOptions.LongRunning`, `(TaskCreationOptions)2` and
an await of `new SemaphoreSlim(0).WaitAsync()`, and `:27-29` does not read the helper files, so `new Thread` and
`new TaskCompletionSource()` in `DiagnosticsScanner.cs` pass. **[Observed]** by an agent. **[Recommended]** Read the
constant value of each argument of these two types, read the helper files, and refuse an await of a source that no
code completes.

#### T101 · Medium · A source with another extension compiles through a `Compile` item, and the gates read `.cs` only · Confirmed by a run

`Engine.Tests/Governance/UiLibraryGateTests.cs:240-244` and `:293`, and `DiagnosticsScanner.cs:89`, read `*.cs`.
`Engine.Cli/ProbeUi.txt` with `[DllImport("cimgui")]` and `<Compile Include="ProbeUi.txt" />` puts `igNewFrame`
into `engine.dll`, and the UI gate passes. **[Observed]** by an agent. **[Recommended]** Refuse a `Compile` item
whose extension is not `.cs`.

#### T102 · Low · The control byte gate permits a lone CR and the C1 controls other than U+0085 · Confirmed by a run

`Engine.Tests/Governance/DependencyDirectionGateTests.cs:189` and `:210`. U+009B, U+008D, U+200B, U+FEFF after the
start and U+2060 pass in `docs/glossary.md`, and a lone CR in a comment of `Engine.Core/DiagnosticCodes.cs` starts a
line that the compiler reads as code. **[Observed]** by an agent. **[Recommended]** Refuse a CR with no LF after it,
and U+0080 to U+009F.

### Correct

**[Observed]** by the agents, with the probes that passed:

- The chain of K0d: an undo and a redo of a rebuild through a `Subtract`, two times; a redo after a rebuild through a
  `Translate`; two consumed extrudes and one live extrude of one sketch; a `Subtract` of a rebuilt body; a faulted
  end that a `Translate` then consumes. Each key is `Derive(edit, body)`, and an undo swaps back exactly.
- The new solver code calls `Abs`, `Sqrt`, `Max` and `Min` only, and an arc with its start at its end solves to the
  same bits two times. The refusals of `equal`, `tangent` and `radius` on a wrong kind hold.
- 300 cycles of a box, a failed `Subtract`, a `Translate` and a bounding box, then two disposes, give no crash.
- The gizmo: a drag back to its start sends nothing; a loss of the focus cancels; a pivot at or behind the eye gives
  no layout; a rejected `Translate` puts the drawing back. V46, V47, V48 and P8e hold under their probes.
- The HTTP surface: a member with another case, a wrong content type, a malformed body, `null`, a version 1.5, and
  each number that is not finite give 4xx. A repeated `commandId` does not apply a second command. The loopback
  check permits 127.0.0.1 and `localhost` only.

**Not verified.** The `OverlayRenderer` on a GPU other than this computer; the desktop loop with a person; Linux,
macOS and arm64 for the new probes; a native package that sets `TargetName`; the display of a lone CR on GitHub.

## 4 The next planned task

The plan of 2026-10-30 is complete: K2c, this kind of review, P0.44, R9, P0.45, P0.46, K0d and K2d are merged, and
P8e came after them. The row of K2d says that the desktop waits for K2e, so K2e is the next product phase.
`docs/templates.md`, section 7, puts the high finding E97 into a task before K2e: P0.48 (TASK-0121). K2e is
TASK-0122. P0.49 (TASK-0123) holds the medium and low findings of the engine, the hosts and the gates after K2e, and
P0.50 (TASK-0124) holds those of the desktop. E100 and E99 wait for Q1, and V53 waits for Q2.

## 5 Research for that task

**[Observed]** I read the cited code.

- **The data is there.** `GetSketch` gives the circles and the arcs (`Engine.Core/Queries/GetSketchQuery.cs:19-29`),
  so the desktop needs no change of the engine.
- **The overlay draws points and lines only.** `3DEngine/SketchOverlay.cs` gives strokes, spots, labels and rings.
  A curve is a polyline of strokes. A polyline is presentation (ADR-0007), so the desktop computes it with a sine and
  a cosine outside the log; the determinism rules do not cover it.
- **The tool knows points and lines.** `3DEngine.Core/Sketching/SketchTool.cs` has the kinds Line and Select, and
  `ISketchEditor` has `AddPoint` and `AddLine`. A circle is two taps: the centre and a point on the circle. An arc is
  three taps: the centre, the start and the end.
- **The panel offers the five kinds of K3a.** `DesktopUi.ConstraintButtons` holds coincident, horizontal, vertical,
  distance and fixed. The kinds of K2c and K2d have no button.
- **An extrude refuses a curve** (`SketchProfile.cs:15`), so the Extrude button of a sketch with a curve gives the
  refusal of the engine.

## 6 Recommended approach

**[Recommended]** K2e in this order:

1. `SketchOverlay` draws each circle and each arc as a polyline of 64 segments for a full turn, with the cut and the
   clip of a line for each segment. V52 changes the same function, so P0.50 follows K2e.
2. `SketchTool` gains the kinds Circle and Arc, and `ISketchEditor` gains `AddCircle(centre, radius)` and
   `AddArc(centre, start, end)`. The radius of a circle is the distance in the plane, with `Math.Sqrt` only, so the
   command records the same number on each platform.
3. The Select tool picks a curve by its distance to the polyline.
4. The panel gains the buttons of the kinds of K2c and K2d that take the selection of the tool: tangent, radius,
   pointOn and equal. A radius opens the pad.
5. The tests: a tool script for each kind with no window, a picture of a circle and of an arc on a GPU, and a check
   by the owner with pictures (the rule of the visual checks). The estimate is 4 to 6 agent hours.

## Questions for the owner

**Q1. An entry whose command identifier the log already holds, in an open (E100, E99).** Up to v0.117 a client that
sent a move again after the cache forgot it gave such an entry, and ADR-0015 item 8 says that an open applies it.
- A: An open applies an entry whose identifier came before, as ADR-0015 item 8 says: the guard of a repeated
  identifier acts on a live command only, and the open gives the result that the earlier engine gave.
- B: ADR-0015 item 8 changes: an open refuses such a file, and the error names the line and the reason.
- C: An open skips such an entry with a warning. The document then differs from the session that wrote the file.

**Recommended: A.** It keeps the decision and each file that an earlier engine wrote. E99 then takes the same guard
with no risk to an open.

**Q2. Other input during a drag of the gizmo (V53).** ADR-0038 does not say what a wheel, a key of the camera, an
undo or a hide does while a drag runs.
- A: The intent machine drops the camera keys, the wheel, undo, redo and hide while a drag runs.
- B: Each of these inputs cancels the drag, and then acts.
- C: No change. The offset follows the new camera at the next motion.

**Recommended: A.** A CAD program keeps the view still during a drag, and a wheel that a person turns by accident
does not lose the drag.
