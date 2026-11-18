---
id: 0400
title: The medium and low findings of the codebase review of 2026-11-17
status: Done
phase: P0.162
opened: 2026-11-17
depends-on: [0399]
governed-by: [0004, 0033, 0035, 0037, 0039, 0048, 0142, 0143, 0144, 0145, 0147, 0149, 0154, 0155, 0156, 0159, 0160, 0164]
writes:
  create:
    - tasks/TASK-0400-the-findings-of-the-review-of-2026-11-17.md
    - docs/adr/0164-the-findings-of-the-review-of-2026-11-17.md
  modify:
    - Engine.Core/Drawing/ViewEdges.cs
    - Engine.Core/Drawing/SheetLayout.cs
    - Engine.Core/Sketch/SketchSolver.cs
    - Engine.Core/Export/DxfExport.cs
    - Engine.Core/Export/SvgExport.cs
    - Engine.Tests/Drawing/SheetViewTests.cs
    - Engine.Tests/Drawing/ViewCurvesTests.cs
    - Engine.Tests/Sketch/FixedLengthsTests.cs
    - Engine.Tests/Export/DxfExportTests.cs
    - Engine.Tests/Export/SvgExportTests.cs
    - docs/adr/0143-the-circles-of-a-view.md
    - docs/adr/0144-the-ellipses-and-the-outline-of-a-view.md
    - docs/adr/0154-the-fixed-lengths-from-one-elimination.md
    - docs/adr/0155-the-views-of-a-sheet.md
    - docs/adr/0156-the-rank-of-a-sketch-at-each-size.md
    - docs/adr/0159-the-svg-export-of-a-sheet.md
    - docs/adr/0160-the-dxf-export-of-a-sheet.md
    - docs/adr/README.md
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
---

# TASK-0400 — The medium and low findings of the codebase review of 2026-11-17

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-17 (`docs/reviews/2026-11-17-codebase-review.md`) gives the medium findings E211, E212, E213
and E214, and the low findings E215, E216 and E217. Section 7 of `docs/templates.md` puts them after the next product phase,
P10c (TASK-0401).

## Goal

Each of the seven findings has a correction or a recorded limit, and a test that fails on the code before the correction.

## Scope (in)

1. E211: a view with an exact frame of a length other than 1 shows the curves of the design, or the limit is recorded.
2. E212: a frame whose length gives no finite double greater than zero is refused, or the factor does not overflow.
3. E213: the fixed lengths of a stair of 600 lines take a time near the time of the status, with a test of the time.
4. E214: the freedom of a constraint that joins sizes in a ratio of about 1e6, or the limit in ADR-0156.
5. E215: the DXF writer gives one defined answer for a number that is not finite.
6. E216: each part of the chain of an ellipse is less than a half turn, also when the arc is almost closed.
7. E217: the SVG file stays XML for each name that the rules of a name accept.

## Scope (out)

- A new refusal of a sheet name: a log that is valid today must stay valid.
- The PDF writer (P10c), which starts with the stops of each quarter turn and the guard of each number.

## Acceptance criteria

- [x] Each finding has a test that failed on the code of v0.383, and each rule has an injection that a test catches.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Notes for the implementer

Read the section of each finding in the review: its place, its probe and its recommended correction. E211 and E212 touch
the same factor of `SheetLayout.Place`. E213 and E214 touch the same elimination; keep the bits of a replay of the solver.

## Outcome

Status: Done · commit (the merge of the branch p0162-review-findings) · v0.388

## Method

Mechanical: the guards of the numbers and the names of the files, and the stops of each quarter turn from the PDF writer.
Judgement:  E211 and E212 from one length of the frame that never squares a double; E213 with the solve of one right side
            unchanged; E214 recorded as a limit, not corrected.
Weakest:    the time test of E213 compares the analysis with the status on one computer; a runner with a very uneven load
            could still fail it.

## Progress

- 2026-11-18: opened under the order of the owner "Start P0.162", branch p0162-review-findings from `main` at v0.386. The
  write set adds ADR-0143 and ADR-0144, whose decision held the limit of a frame of length 1, ADR-0164, and the register for
  R-0068; it drops `AddSheetViewCommandHandler.cs`, because no refusal changed. Each new test failed on the code of v0.386.
  Twelve injections: ten failed a test at once. One (no `ScaleB`) passed, because √2 needs no shift; a frame of length 4√2
  now catches it. One (no guard in the DXF number) passed, because the SVG number already gives 0, so the DXF guard could
  not act, and the code lost it. Each file came back byte for byte. A heredoc lost backslashes two times; Node with
  `String.fromCharCode(92)` wrote those lines again.
- 2026-11-18: the full run found `ViewCurvesTests.A_Frame_Of_Another_Length_Keeps_The_Facets`, the test of the old decision of
  ADR-0143 item 4. It now asserts the new rule: a frame of length 2 gives one circle of the exact radius 20.
- 2026-11-18: run 44276399803 passed on the branch. The session of the desktop window merged first, as P0.164 (TASK-0404,
  v0.387), with its ADR-0163, so the ADR of this task is ADR-0164. The commit 21d1490 still names ADR-0163 in its message;
  the history keeps it, because a pushed commit does not change.
- 2026-11-18: the merge of `main` at v0.387 into this branch: the index of the ADRs holds the row of ADR-0163 of P0.164
  and the row of ADR-0164 of this task, in their order; the test floor takes 1672 for `Engine.Tests` and the image counts of `main`.
- 2026-11-18: closed at v0.388. The old path of ADR-0163 leaves the write set: the commit of the rename needed it, and the
  gate of the numbers of the ADRs reads it as a plan for a number that P0.164 has.
