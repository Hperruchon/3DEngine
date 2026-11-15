---
id: 0391
title: The status and the fixed lengths of a sketch come from one elimination
status: Done
phase: P0.158
opened: 2026-11-15
depends-on: [0390]
governed-by: [0004, 0016, 0018, 0028, 0033, 0035, 0037, 0039, 0060, 0149, 0154]
writes:
  create:
    - tasks/TASK-0391-the-high-finding-of-the-review-of-2026-11-15.md
    - docs/adr/0154-the-fixed-lengths-from-one-elimination.md
  modify:
    - Engine.Core/Sketch/SketchSolver.cs
    - Engine.Core/Queries/GetSketchQueryHandler.cs
    - Engine.Tests/Sketch/FixedLengthsTests.cs
    - docs/adr/0149-a-size-that-no-constraint-fixes.md
    - docs/adr/README.md
    - tasks/TASK-0392-the-low-findings-of-the-review-of-2026-11-15.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
---

# TASK-0391 — The status and the fixed lengths of a sketch come from one elimination

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-15 (`docs/reviews/2026-11-15-codebase-review.md`) gives the high finding E206: each read of a
sketch finds its fixed lengths with one full elimination for each line, and the window reads the active sketch in each frame.
Section 7 of `docs/templates.md` puts it before the next product phase, P10.2.

## Goal

A read of a sketch of 100 lines takes less time than one frame, and the fixed lengths stay the same.

## Scope (in)

1. E206: the status and the fixed lengths from one elimination of J·Jᵀ, and a product J·Jᵀ that takes the columns with a
   value (ADR-0154). The fixed lengths stay in each read: with one elimination, a read of 100 lines takes about 9 ms, so a
   read that does not ask for them is not necessary, and the query keeps its parameters.
2. A test of the time of a sketch of 100 lines, which fails before the fix.
3. The comment of `Product` and the result of `GetSketch` (E209 and E210): the change removes `ProductWith` and the
   pattern `is var`.

## Scope (out)

- E207, E208, V192 and V193 (TASK-0392).

## Acceptance criteria

- [x] Each test of the fixed lengths passes with the new computation.
- [x] A test proves that a read of a sketch of 100 lines takes less time than its limit.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0158-review-findings) · v0.376

## Method

Mechanical: the elimination with columns, the residual, and the product of the columns with a value.
Judgement:  the limit of the time in the test (1 s for about 9 ms here), and no parameter of the query.
Weakest:    the residual and the old pivot are equal in exact arithmetic only; the four tests of the fixed lengths and the
            tests of the solver are the proof, and no test compares the two methods near the limit.

## Progress

- 2026-11-15: opened by the codebase review of 2026-11-15 (TASK-0390).
- 2026-11-15: started on the order of the owner ("start TASK-0391"), branch p0158-review-findings. The new test of the
  stair of 100 lines failed on the old code at 5.4 s with the correct fixed lines and freedom. The first form with one
  elimination took 85 ms: the status alone took 28 ms, from the full product J·Jᵀ. With the product of the columns with a
  value: status 0.9 ms, analysis 7.7 ms. An injection of 1e-15 in the product failed `SketchSolverTests`,
  `LineConstraintTests` and `CircleArcTests`; the file came back byte for byte.
- 2026-11-15: run 43753428670 passed on the branch. Closed at v0.376.
