---
id: 0392
title: The low findings of the codebase review of 2026-11-15
status: Active
phase: P0.159
opened: 2026-11-15
depends-on: [0391]
governed-by: [0004, 0007, 0017, 0019, 0022, 0023, 0025, 0029, 0032, 0033, 0035, 0037, 0039, 0072, 0082, 0084, 0098, 0115, 0117, 0127, 0138, 0146, 0148, 0149, 0150, 0151, 0154, 0156]
writes:
  create:
    - tasks/TASK-0392-the-low-findings-of-the-review-of-2026-11-15.md
    - docs/adr/0156-the-rank-of-a-sketch-at-each-size.md
  modify:
    - Engine.Core/Sketch/SketchSolver.cs
    - Engine.Tests/Sketch/FixedLengthsTests.cs
    - 3DEngine.App/EdgeValues.cs
    - 3DEngine.ImageTests/FlatViewValuesTests.cs
    - 3DEngine.Vulkan/SdlInput.cs
    - 3DEngine.Tests/SdlInputTests.cs
    - docs/adr/0035-sketch-solver.md
    - docs/adr/0142-the-edges-of-a-view-outside-the-kernel.md
    - docs/adr/0154-the-fixed-lengths-from-one-elimination.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
---

# TASK-0392 — The low findings of the codebase review of 2026-11-15

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-15 (`docs/reviews/2026-11-15-codebase-review.md`) gives the low findings E207, E208, E209,
E210, V192 and V193. Section 7 of `docs/templates.md` puts them after the next product phase, P10.2.

## Goal

The fixed lengths do not depend on the scale, the ends of an edge come from all its segments, and the small faults of form go.

## Scope (in)

1. E207: the rows of J at unit length in the status and the analysis (ADR-0156); the solver keeps its rows.
2. E208: the history of ADR-0142.
3. E209 and E210: TASK-0391 removed `ProductWith` and the pattern `is var`, so they are fixed there.
4. V192: one helper of the ends of an edge.
5. V193: the state of each modifier key.
6. A test for E207, V192 and V193, which fails before the fix.

## Scope (out)

- E206 (TASK-0391).

## Acceptance criteria

- [x] A test proves the fixed lengths of two rectangles of 1e5 mm and 0.001 mm in one sketch.
- [x] A test proves the ends of an edge in two parts.
- [x] A test proves that the flag of the modifier stays while one of two keys is down.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: E208 to E210, and the state of each modifier key (V193).
Judgement:  rows at unit length in the rank only, not in the steps of the solver (E207); the farthest points of an edge in
            more than one part, and the old ends for an edge in one part (V192).
Weakest:    no path of the window gives an edge in two parts now, so the test of V192 builds the edge by hand.

## Progress

- 2026-11-15: opened by the codebase review of 2026-11-15 (TASK-0390).
- 2026-11-15: TASK-0391 fixed E209 and E210 with its change of the same lines.
- 2026-11-15: started after P10.2 (TASK-0393), branch p0159-review-findings from its close. The new tests failed on the
  old code: the four sizes of the rectangles (a fixed side lost, or a freedom of 9 in place of 8), and the four pairs of
  modifier keys. Three injections each failed their test, and each file came back byte for byte; two first injections
  did not compile (code that the compiler cannot reach), so the session used forms that compile.
