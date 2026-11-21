---
id: 0449
title: The medium and low findings of the third codebase review of 2026-11-21
status: Active
phase: P0.180
opened: 2026-11-21
depends-on: [0448]
governed-by: [0004, 0007, 0016, 0018, 0019, 0025, 0027, 0028, 0029, 0030, 0031, 0032, 0033, 0034, 0037, 0057, 0060, 0072, 0109, 0113, 0114, 0121, 0131, 0155, 0163, 0172, 0173, 0180, 0181, 0182, 0184]
writes:
  create:
    - tasks/TASK-0449-the-findings-of-the-review-of-2026-11-21-c.md
  modify:
    - 3DEngine.App/SectionView.cs
    - 3DEngine.App/SessionBodies.cs
    - 3DEngine.App/Ui/DesktopUi.Section.cs
    - 3DEngine.ImageTests/ResponsiveLayoutTests.cs
    - 3DEngine.ImageTests/SectionViewTests.cs
    - Engine.Core/Drawing/ViewBreps.cs
    - Engine.Core/Queries/CheckInterferenceQueryHandler.cs
    - Engine.Core/Queries/GetAngleQueryHandler.cs
    - Engine.Core/Queries/MeasureCurve.cs
    - Engine.Core/Queries/RevolvedDistance.cs
    - Engine.Kernel/PlanarBoolean.cs
    - Engine.Tests/Guides/AgentGuideExamplesTests.cs
    - Engine.Tests/Http/HostGuardTests.cs
    - Engine.Tests/Pipeline/CrashReportTests.cs
    - Engine.Tests/Queries/CheckInterferenceQueryTests.cs
    - Engine.Tests/Queries/MeasureQueryTests.cs
    - eng/report-failed-tests.js
    - eng/test-floor.txt
    - docs/adr/0121-the-edges-of-a-large-mesh-are-found-on-the-thread-pool.md
    - docs/adr/0173-a-lost-runner-gets-one-automatic-re-run.md
    - docs/adr/0180-the-measure-queries-of-plane-faces.md
    - docs/adr/0182-the-pair-check-at-one-position.md
    - docs/adr/0184-the-section-view.md
    - docs/guides/agent-loop.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0449 — The medium and low findings of the third codebase review of 2026-11-21

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The third codebase review of 2026-11-21 (`docs/reviews/2026-11-21-c-codebase-review.md`, TASK-0448) found two medium and nine
low findings and no critical or high one. By `docs/templates.md`, section 7, they come after the next product phase. K15d
(TASK-0450, v0.433) is that phase, and this branch starts from it (`009e11f9`), because K15d changed `AboutAxis`, the method of
E235, and the header of `RevolvedDistance.cs`, a comment of E238.

Item 4, the addition of the session of 2026-11-21: `Engine.Tests/Http/HostGuardTests.cs` used process time limits of 30 s that
decide a test. ADR-0173 item 4 says that no time limit decides a test that passes; P0.173 (TASK-0435) listed these guards and
left them. On 2026-11-21 the K15d session saw one to three `HostGuardTests` fail at the limit of 30 s in each full local run,
different tests each time, and each test passed alone; the class alone took 2 min 22 s for 48 tests.

## Goal

Each finding has a correction and a test, or a register entry with its reason.

## Scope (in)

1. Medium: E235 (GetAngle gives a wrong angle for the side of an arc that bites into the profile), V229 (the section finds the
   edge lines of a cut mesh on the frame thread, with no limit of triangles, against ADR-0121).
2. Low: E236, E237, E238, V230, V231, V232, T189, T190, T191.
3. The answer Q6 of the review (E235), option A under the order of the owner of 2026-11-20: the angle takes the side of the
   material.
4. The time limits of `HostGuardTests` follow ADR-0173 item 4: a host process that makes no progress for 60 s fails the test,
   and a slow host that makes progress passes. Each test keeps what it protects, with an order in place of a time. The reason
   is in "Context".

## Scope (out)

- A query that lists the sheets, and a query that gives the live bodies with their names (T190). Each is a new shape of
  `Engine.Contracts`, which this task forbids; register entry R-0075 asks the owner.
- The layout of a ring of six segments at 640 by 360 pixels, which the new state of the layout test found (register entry
  R-0076).
- A cache of the solid of the kernel for each mesh of a body in the section view, the second part of the correction of V230.
  One cut at a time ends the pile of cuts; the cache is a gain of speed with no finding.
- A change of `Engine.Contracts`, a new diagnostic code, and a run of the desktop.

## Acceptance criteria

- [x] Each finding has a test that fails before its correction, or a register entry.
- [x] Each injection of a correction makes a named test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Decisions

- Q6 (E235) A: answered by the standing order of the owner of 2026-11-20 (Q6 of the third codebase review of 2026-11-21). The
  angle of the side of an arc takes the side of the material.
- The side of the material comes from a facet of the side: the sign of its outward normal against the part across the axis
  of its vertex. The test is exact, and it needs no walk of the profile. The flag `Inward` of the surface of the side of a
  hole (K15d) already means "the outward normals point to the axis", so a bite sets it too.
- E236: each void leaves the volume that two outer shells share (a subtract), so only material in both shells refuses the
  mesh. The work of each subtract counts in the bound of item 8.
- E237: the products of the pairs come first, from the count of the triangles of a mesh body; the products of the shells
  come after the solids, and the bound applies again.
- V229 and V230: the whole cut, with the narrowed positions, the edge lines and the hatch, runs in one task, and a body runs
  one task at a time. A new mesh of a body takes its last cut off the GPU at once, as before, because that cut is of the old
  mesh.
- V232: a plane of a datum keeps the identity of the datum (`SectionPlane.Datum`). A section whose datum is gone goes to XZ
  and keeps its flip.
- T190: an indented fence or a fence of tildes fails the read of the guide; the 15 sentences of the guide over 25 words are
  split.
- Item 4: progress is a new line of output or more processor time of the host; a refusal at the start fails at once when the
  host listens before it exits.

## Outcome

Status: Active · commit (the merge of the branch p0180-review-findings) · v0.434

## Method

Mechanical: the comments of E238, the sentences of the guide, the rows of the roadmap and the ledger, and the counts of
`eng/test-floor.txt`.

Judgement: see "Decisions". E238 has no test: a comment has no behaviour; the three comments now say what the code does.

Weakest: the progress of a host reads `Process.TotalProcessorTime`. An idle Kestrel host can still use a little processor
time for its timers, so a host that listens and never ends can count as progress; then the hang limit of the step "Test"
stops it. The injection "a host that makes no progress" measured this on Windows (see below).

Tests first: before each correction, its new test failed on the code of `009e11f9` (with only the hook `Searched` added to
`SectionView` for V229): `The_Distance_And_The_Angle_Of_The_Side_Of_An_Arc` (cos 0, sin 1 in place of cos 1, sin 0),
`A_Mesh_With_An_Island_In_A_Void_Is_Checked` (E-MESH-UNSUPPORTED), `The_Bound_Of_The_Work_Comes_Before_The_Solid_Of_A_Mesh_Body`
(E-MESH-UNSUPPORTED in place of E-CHECK-WORK-LIMIT), `With_Ten_Failed_Tests_The_Running_Test_Of_A_Stop_Has_The_First_Annotation`,
the two cases of `A_Fence_That_The_Test_Does_Not_Read_Fails_The_Read_Of_The_Guide`,
`The_Edge_Lines_And_The_Hatch_Of_A_Cut_Are_Found_In_The_Cut_And_Not_On_The_Thread_Of_The_Frame`,
`A_Body_Runs_One_Cut_At_A_Time_And_Keeps_Its_Last_Cut_Until_The_New_One`, `The_Section_Cuts_The_Visible_Bodies_Only` and
`A_Section_On_A_Datum_Follows_The_Datum_And_Goes_Back_To_XZ_When_It_Is_Gone`. T189 is a gap of the tests: its assertions of
item 5 passed on the old code, and its injections fail them. The state `SixSegments` of the layout test found R-0076.

Injections (`inject0449.js` in `%TEMP%\p0180`), each restored byte for byte (sha256), each run with `dotnet test` and its
filter as arguments, with no shell. Each of the seventeen injections made its named test fail:

- the search of the edge lines and the hatch done before the task of the cut, on the thread of the frame:
  `The_Edge_Lines_And_The_Hatch_Of_A_Cut_Are_Found_In_The_Cut_And_Not_On_The_Thread_Of_The_Frame`;
- a new plane that starts a cut while one runs, a stale cut that takes the last cut off the GPU, and a new mesh that keeps the
  cut of the old mesh shown: `A_Body_Runs_One_Cut_At_A_Time_And_Keeps_Its_Last_Cut_Until_The_New_One`;
- hidden bodies in the list of the section: `The_Section_Cuts_The_Visible_Bodies_Only`;
- a section that keeps the copy of its datum, and a section whose datum is gone that keeps its plane:
  `A_Section_On_A_Datum_Follows_The_Datum_And_Goes_Back_To_XZ_When_It_Is_Gone`;
- the state `SixSegments` with no "Images" (five segments): `Each_Control_Stays_In_The_Display_And_Free_For_A_Tap`, 14 sizes;
- the sector of a bite not turned, and the side of the material with the wrong sign:
  `The_Distance_And_The_Angle_Of_The_Side_Of_An_Arc`;
- no void that leaves the shared volume, and no void kept: `A_Mesh_With_An_Island_In_A_Void_Is_Checked`;
- no work of the pairs before the solid: `The_Bound_Of_The_Work_Comes_Before_The_Solid_Of_A_Mesh_Body`;
- the running test after the failed tests: `With_Ten_Failed_Tests_The_Running_Test_Of_A_Stop_Has_The_First_Annotation`;
- a pattern that misses an indented fence and a fence of tildes: both cases of
  `A_Fence_That_The_Test_Does_Not_Read_Fails_The_Read_Of_The_Guide`;
- a host that listens in the test of a start that fails: `A_Host_Whose_Start_Fails_Stops_With_Exit_Code_5`, at once (5 s);
- no read of the line of the address, so the host only waits: `On_The_Real_Host_A_WebSocket_Upgrade_With_A_Foreign_Origin_Is_Refused`,
  with "no progress for 60 s" after 3 min 18 s. The idle host used processor time for its timers for about two minutes
  before it stopped (see "Weakest").

The runs of `HostGuardTests` alone (48 tests), each green: 200 s, 178 s, 134 s, 143 s and 159 s. The first two ran beside builds
of the session. In the full local run (`dotnet test 3DEngine.sln`, 3190 tests) each `HostGuardTests` passed, and
`Engine.Tests` took 1 min 20 s.

## Progress

- 2026-11-21: branch `p0180-review-findings` from `009e11f9` (K15d, TASK-0450, v0.433, not merged yet). Eleven findings and
  item 4; register entries R-0075 and R-0076; ledger entry v0.434.
