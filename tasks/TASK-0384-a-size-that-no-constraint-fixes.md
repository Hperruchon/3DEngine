---
id: 0384
title: The solver finds the lengths that the constraints fix, and a flat view shows a length that no constraint fixes in amber
status: Done
phase: R22c
opened: 2026-11-14
depends-on: [0383]
governed-by: [0004, 0007, 0016, 0018, 0019, 0025, 0027, 0028, 0029, 0030, 0032, 0033, 0034, 0035, 0037, 0039, 0060, 0072, 0082, 0084, 0091, 0098, 0099, 0105, 0108, 0110, 0115, 0117, 0119, 0128, 0130, 0131, 0135, 0141, 0146, 0148, 0149]
writes:
  create:
    - tasks/TASK-0384-a-size-that-no-constraint-fixes.md
    - docs/adr/0149-a-size-that-no-constraint-fixes.md
    - Engine.Tests/Sketch/FixedLengthsTests.cs
    - 3DEngine.ImageTests/FreeSizeTests.cs
  modify:
    - Engine.Core/Sketch/SketchSolver.cs
    - Engine.Core/Queries/GetSketchQuery.cs
    - Engine.Core/Queries/GetSketchQueryHandler.cs
    - 3DEngine.App/EdgeValues.cs
    - 3DEngine.App/Ui/DesktopUi.Values.cs
    - 3DEngine.App/Ui/DesktopUi.Rows.cs
    - 3DEngine.ImageTests/ChainReferenceTests.cs
    - 3DEngine.ImageTests/FlatViewValuesTests.cs
    - docs/adr/0146-a-value-shows-where-its-length-is-true.md
    - docs/adr/0148-the-reference-of-a-chain.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
---

# TASK-0384 — The solver finds the lengths that the constraints fix, and a flat view shows a length that no constraint fixes in amber

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row R22c (ADR-0146 item 2). The solver gives the count of the free degrees and not the free lengths. On 2026-11-14 the
owner ordered work alone until Sunday, with the recommended option for each question.

## Goal

A flat view shows each length of the sketch that the design does not fix, and a reference only where the design fixes it.

## Scope (in)

1. ADR-0149, which amends ADR-0146 item 2 and ADR-0148 item 2.
2. `SketchSolver.FixedLengths`, and the field `fixedLengths` of `GetSketch`.
3. The chip "not fixed" in amber with no row and no pad; a reference only on a fixed line.
4. Tests.

## Scope (out)

- A lost value (P10, the sheet).
- R22d.

## Acceptance criteria

- [x] ADR-0149 exists, and ADR-0146 and ADR-0148 show the amendment.
- [x] Tests prove the fixed lengths of a rectangle with and without its directions and of a line between fixed points, the
      free sizes of an L from the top and from the front, no free size where the design fixes each length, and a free size
      with a chip, no row and no pad.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r22c-free-sizes) · v0.369

## Method

Mechanical: the gradient of a length; the chip and its colour.
Judgement:  the rank test with the limit of the solver; a reference only on a fixed line.
Weakest:    a length near the limit of a pivot; no person used it.

## Progress

- 2026-11-14: opened under the order of the owner of 2026-11-14, after R22b, branch r22c-free-sizes. TASK-0384 and ADR-0149 are
  free. The L of the tests of R22b had no direction constraint, so its free lines were not fixed by the design; the tests now
  add the directions. The plate of the tests of R22a has free sides too, so that test counts the values of the design only.
  A `sed -i` dropped the CRLF of a file; node restored it. Seven injections each failed a named test; each file came back
  byte for byte.
- 2026-11-14: run 43579344685 passed on the work commit. Row R22d now comes after P10: the place of a dimension is a command
  on a sheet (ADR-0118 item 3), and the sheet comes with P10. Closed at v0.369.
