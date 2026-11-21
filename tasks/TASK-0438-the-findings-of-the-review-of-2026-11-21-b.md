---
id: 0438
title: The medium and low findings of the second codebase review of 2026-11-21
status: Done
phase: P0.174
opened: 2026-11-21
depends-on: [0437]
governed-by: [0004, 0013, 0016, 0018, 0019, 0028, 0033, 0037, 0057, 0060, 0083, 0089, 0114, 0149, 0153, 0154, 0155, 0156, 0158, 0164, 0166, 0179, 0180, 0181, 0182]
writes:
  create:
    - tasks/TASK-0438-the-findings-of-the-review-of-2026-11-21-b.md
  modify:
    - Engine.Kernel/Bsp.cs
    - Engine.Kernel/PlanarBoolean.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/Queries/CheckInterferenceQueryHandler.cs
    - Engine.Core/Queries/NearestPoints.cs
    - Engine.Core/Queries/MeasureCurve.cs
    - Engine.Core/Queries/CurvedDistance.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Tests/Queries/CheckInterferenceQueryTests.cs
    - Engine.Tests/Queries/MeasureQueryTests.cs
    - Engine.Tests/Kernel/PlanarUnionIntersectTests.cs
    - Engine.Tests/Sketch/FixedLengthsTests.cs
    - docs/adr/0154-the-fixed-lengths-from-one-elimination.md
    - docs/adr/0180-the-measure-queries-of-plane-faces.md
    - docs/adr/0181-the-union-and-the-intersection-of-two-bodies.md
    - docs/adr/0182-the-pair-check-at-one-position.md
    - docs/diagnostics.md
    - docs/guides/agent-loop.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
---

# TASK-0438 — The medium and low findings of the second codebase review of 2026-11-21

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The second codebase review of 2026-11-21 (`docs/reviews/2026-11-21-b-codebase-review.md`, TASK-0437) found three medium and
four low findings and no critical or high one. By `docs/templates.md`, section 7, they come after the next product phase.

## Goal

Each finding has a correction and a test, or a register entry with its reason.

## Scope (in)

1. Medium: E233 (CheckInterference can hold the serial lock for minutes, and a cancel acts only between two pairs), E234 (a
   closed STL mesh whose shells cross each other passes the check of a mesh, and the class can be wrong), T187 (the work
   counters compare the analysis with the status only).
2. Low: E230, E231, E232, T188.
3. The answers Q5, Q6 and Q7 of the review, each option A under the order of the owner of 2026-11-20.
4. The review asks for a register entry for the crash of the test host of `Engine.Tests` on Linux (run 44869058349).

## Scope (out)

- The write set holds the files of each correction and the ADRs that govern them, from the start of the task
  (`docs/templates.md`, section 2).
- A join of two faces on one plane after a union (E231): a known limit in ADR-0181 item 6, because it changes the names of
  ADR-0058. The register holds 15 of 15 entries after R-0074, so it gets no entry.
- A sort of the pieces by the gap of their boxes (a part of the correction of E233 in the review): it changes the first
  pair of points when several pairs give the minimum.

## Acceptance criteria

- [x] Each finding has a test that fails before its correction, or a register entry.
- [x] Each injection of a correction makes a named test fail.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Decisions

The answers Q5 to Q7 are answered by the standing order of the owner of 2026-11-20 (Q5 to Q7 of the second codebase review
of 2026-11-21). Each answer is the recommended option A:

- Q5 A (E233): `CheckInterference` gets a bound of work with the new refusal code `E-CHECK-WORK-LIMIT`, and the cancellation
  token is read in the inner loops, not only between two pairs.
- Q6 A (E230): a union of two bodies that touch on an edge keeps its body; ADR-0181 records the case, and what the STEP
  export and the pair check do with it.
- Q7 A (T187): the work counters of the fixed lengths get an absolute bound on the count of the status, 1.5 times the count
  of today, beside the ratio.

This task takes these decisions:

- E233: the work is the sum over each pair of bodies of the product of the counts of their pieces, plus the work of the test
  of the shells of E234. The limit is 100,000 (ADR-0182 item 8, from the measures: a sphere and a plate, 23,808 products,
  3.2 s; two spheres apart, 15,745,024 products, 101 s). The token goes to `NearestPoints.Nearest` (each piece of the first
  side) and to a new form `PlanarBoolean.Intersect(a, b, token)` of the kernel, whose tree reads it at each node of a walk
  that splits. The form with two parameters stays, because `IntersectCommandHandler` passes it as a method group.
- E234: the smaller correct option is a test of the shells, not a mark: a mark "not exact" needs the same test to find the
  mesh, and a value of `basis` that ADR-0157 item 7 does not give. A mesh with two outer shells that share a volume is
  refused with `E-MESH-UNSUPPORTED`. A void and a shell that crosses itself are not tested (ADR-0182 "Bad").
- E231: a known limit in ADR-0181 item 6, with the test that exists (`UnionIntersectCommandTests`, "A+z" and "B+z").
- E232: the comments and ADR-0180 items 8 and 13 say [0, 2π], at most about ten units of the last place, and the three steps
  of `CurvedDistance` that compare doubles (the review named two; the choice of the nearest piece is the third).
- The guide `docs/guides/agent-loop.md` said that a check can take minutes, that a cancel acts only between two pairs, and
  that a mesh with crossing shells can give a wrong class. These statements became false, so this task corrects them and adds
  the row of `E-CHECK-WORK-LIMIT`.
- Scope item 4: register entry R-0074 (class risk). No finding of this task closes an entry with evidence. The register holds
  15 of its 15 open entries.

## Outcome

Status: Done · commit (the merge of the branch p0174-review-findings) · v0.423

## Method

Mechanical: the registration of `E-CHECK-WORK-LIMIT` in `DiagnosticCodes`, `docs/diagnostics.md` and `/schema/diagnostics`,
the History lines of ADR-0154, ADR-0180, ADR-0181 and ADR-0182, the corrected comments of E232, the roadmap, the ledger and
the count of `eng/test-floor.txt`.

Judgement: the limit of 100,000 products, from two measures on the computer of the owner; the test of the outer shells in
place of a mark "not exact" (E234); a second form of `PlanarBoolean.Intersect` in place of an optional parameter, because a
method group of the command handler needs the form with two parameters; the shells by the points of the triangles and not by
the indices of the backend, because the backend gives one point as several vertices (the first form of the test of E234
stayed green with the indices).

Weakest: the passes of the token from the handler into `Nearest` and `Intersect` have no test: a token that cancels inside a
pair needs a time, and no time decides a test (ADR-0173 item 4). The test of the cancel calls `Nearest` and `Intersect` with
a cancelled token, and the query with a cancelled token stops before its first body. The limit of 100,000 holds a check
under about 13 s on the computer of the owner only by the measure of one intersection; an intersection of other shapes can
cost more for each product. A void shell and a shell that crosses itself are not tested (ADR-0182 "Bad").

Before the correction: on `31021dba` a probe test (removed) gave for the mesh of the cubes [0, 10]³ and [5, 15]³ and a box of
2 at (2, 2, 2) the class "apart" with a gap of 1, and the check of two spheres apart ran 101 s with no refusal. The tests of
T188, E230, E232 and T187 pass on that code: T188 and E230 add tests of a behaviour that was right, E232 corrects text, and the
bound of T187 holds the count of today; injections show what each one catches.

Injections (`inject.js` in the scratch folder of the session, `p0174`), each restored byte for byte (sha256), each made the
named test fail:

- I01 the limit 100,000 to 10,000; I16 the limit times 1000 (the test then ran two spheres for 101 s):
  `A_Check_Whose_Work_Is_Above_The_Limit_Is_Refused_Before_Its_First_Pair`;
- I02 no work of the shells; I03 a sum of the counts in place of their product:
  `The_Test_Of_The_Shells_Of_A_Mesh_Counts_In_The_Work_Of_The_Check`;
- I04 no test of the shells; I05 the voids tested as outer shells; I06 the shells by the indices of the backend:
  `A_Mesh_Whose_Shells_Share_A_Volume_Is_Refused_And_Shells_Apart_Or_A_Void_Are_Checked` (I06 also the test of the work);
- I07 no token in the outer loop of `Nearest`; I08 no token in the walks of `Intersect`:
  `A_Cancel_Stops_The_Gap_And_The_Intersection_Inside_One_Pair`;
- I09 boxes that share a volume give an overlap when the intersection is null:
  `A_Box_In_A_Pocket_Touches_Or_Is_Apart_While_The_Boxes_Of_The_Bodies_Share_A_Volume` (each test of the class before
  TASK-0438 stayed green, as the review found);
- I10 the pair check refuses a body whose B-Rep is not valid:
  `A_Union_Of_Two_Boxes_That_Touch_On_An_Edge_Is_Checked_And_Not_Written_To_Step`;
- I11 the sum of J·Jᵀ over each column (the dense branch of `Product`): both tests of the stairs, on the bound of the status;
- I12 the halvings of the arctangent stop at 1e-1; I13 the angle of a direction under the axis x one unit lower:
  `The_Arctangent_Is_At_Most_Ten_Units_Of_The_Last_Place_From_The_Angle_And_The_Range_Ends_At_Two_Pi`;
- I14 the row of the code in `docs/diagnostics.md`: `All_Diagnostic_Codes_In_Engine_Sources_Are_Registered`;
- I15 the row of the code in `/schema/diagnostics`: `Every_DiagnosticCodes_Constant_Appears_In_Schema_Diagnostics`.

## Progress

- 2026-11-21: opened after P17 (TASK-0439, v0.422), branch `p0174-review-findings` from `31021dba`, which is not merged yet.
  The bound of work and the token of the pair check, the test of the shells of a mesh, the bound of the status, the records of
  E230 and E231, the text of E232, register entry R-0074, eight new tests and sixteen injections. Ledger entry v0.423.
- 2026-11-21: run 44915298330 of `6cea608e` passed on each job. Closed at v0.423.
