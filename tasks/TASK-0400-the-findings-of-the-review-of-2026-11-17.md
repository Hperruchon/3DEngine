---
id: 0400
title: The medium and low findings of the codebase review of 2026-11-17
status: Ready
phase: P0.162
opened: 2026-11-17
depends-on: [0399]
governed-by: [0004, 0012, 0016, 0021, 0028, 0031, 0033, 0035, 0037, 0039, 0048, 0060, 0142, 0143, 0144, 0145, 0147, 0149, 0154, 0155, 0156, 0159, 0160]
writes:
  create:
    - tasks/TASK-0400-the-findings-of-the-review-of-2026-11-17.md
  modify:
    - Engine.Core/Drawing/ViewEdges.cs
    - Engine.Core/Drawing/SheetLayout.cs
    - Engine.Core/Commands/AddSheetViewCommandHandler.cs
    - Engine.Core/Sketch/SketchSolver.cs
    - Engine.Core/Export/DxfExport.cs
    - Engine.Core/Export/SvgExport.cs
    - Engine.Tests/Drawing/SheetViewTests.cs
    - Engine.Tests/Sketch/FixedLengthsTests.cs
    - Engine.Tests/Export/DxfExportTests.cs
    - Engine.Tests/Export/SvgExportTests.cs
    - docs/adr/0154-the-fixed-lengths-from-one-elimination.md
    - docs/adr/0155-the-views-of-a-sheet.md
    - docs/adr/0156-the-rank-of-a-sketch-at-each-size.md
    - docs/adr/0159-the-svg-export-of-a-sheet.md
    - docs/adr/0160-the-dxf-export-of-a-sheet.md
    - docs/adr/README.md
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

- [ ] Each finding has a test that failed on the code of v0.383, and each rule has an injection that a test catches.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Notes for the implementer

Read the section of each finding in the review: its place, its probe and its recommended correction. E211 and E212 touch
the same factor of `SheetLayout.Place`. E213 and E214 touch the same elimination; keep the bits of a replay of the solver.
