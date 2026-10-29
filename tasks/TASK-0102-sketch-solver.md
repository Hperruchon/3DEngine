---
id: 0102
title: The engine solves the five constraints of a sketch, and the desktop shows if a sketch is solved
status: Active
phase: K3a
opened: 2026-10-29
depends-on: [0099, 0101]
governed-by: [0004, 0007, 0016, 0018, 0019, 0025, 0027, 0028, 0029, 0030, 0032, 0033, 0034]
writes:
  create:
    - tasks/TASK-0102-sketch-solver.md
    - docs/adr/0035-sketch-solver.md
  modify:
    - Engine.Core/Sketch/**
    - Engine.Core/Queries/GetSketchQuery.cs
    - Engine.Core/Queries/GetSketchQueryHandler.cs
    - Engine.Tests/Sketch/**
    - 3DEngine/Ui/**
    - 3DEngine.ImageTests/**
    - docs/adr/0035-sketch-solver.md
    - docs/adr/0033-sketch-model.md
    - docs/adr/README.md
    - tasks/TASK-0098-gate-forms-of-the-eleventh-review.md
    - docs/glossary.md
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/Commands/**
    - Engine.Core/CommandBus.cs
    - Engine.Api.Http/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine.Vulkan/**
    - eng/write-set-check/**
    - eng/repo-gates/**
---

# TASK-0102 — The engine solves the five constraints of a sketch, and the desktop shows if a sketch is solved

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase K3a is the third phase of the sketch on the path to the milestone "CAD on one part". K2a (TASK-0100,
v0.100) gave the sketch model and the residual of each constraint. K2b (TASK-0101, v0.101) gave the tools of the
desktop. P0.40 (TASK-0099, v0.102) came first by the order of the owner of 2026-10-28. On 2026-10-29 the owner set
the next order: K3a, then P0.39 (TASK-0098). ADR-0035 gives the decisions, and the owner accepts it before the code.

## Goal

Each sketch command gives a record whose points satisfy its constraints when a solution exists, with the same bits
on each platform.

## Scope (in)

1. ADR-0035: the method, the equations, the case with no solution, the held point of a move, and the two new fields
   of `GetSketch`.
2. `Engine.Core/Sketch/SketchSolver.cs`, and the call in `SketchResults.Changed`.
3. The fields `solved` and `freedom` of `GetSketch`, and one line in the window "Sketch" of the desktop.
4. A test of the bits of fixed solves, which the pipeline runs on the three operating systems.
5. The amendment of ADR-0033 items 4 and 7.

## Scope (out)

- The kinds 6 to 14 and their equations: phase K3, after the milestone.
- A name for the constraint that causes a conflict.
- A drag of a point with a live solve: R11.
- A sparse solver, and a limit of the size of a sketch.
- A change of a command or of its schema version. `Engine.Core/Commands/**` stays as it is.

## Acceptance criteria

- [ ] The owner accepts ADR-0035 before the code.
- [ ] A rectangle of four rough points with two `horizontal`, two `vertical` and two `distance` constraints and one
      `fixed` gives exact positions, and `GetSketch` says solved with 0 free degrees.
- [ ] A set that conflicts gives an applied command, the positions of the command, and `solved` false.
- [ ] A move of a point holds the point when a solution exists.
- [ ] A test of the bits of fixed solves passes on the three runners of the pipeline.
- [ ] An undo after a solve gives back the record before, with no solve.
- [ ] A file of v0.101 with an unsolved constraint opens, and its sketch is solved.
- [ ] Each new test fails when its injection breaks the code that it covers.
- [ ] The visual check: a rough rectangle in the desktop becomes exact as its constraints come, and the window shows
      the line of the status.
- [ ] `dotnet test` passes, and the build gives zero warnings.

## Notes for the implementer

- The determinism gate reads `Engine.Core`. Do not call `Math.Pow`, `Math.FusedMultiplyAdd` or a vector type.
- Keep one fixed order: the points and the constraints in the order of the record, the pivot search from the first
  row, and a tie of pivots to the first row.

## Progress

- 2026-10-29: the owner chose the order K3a, then P0.39. TASK-0098 now depends on this task. ADR-0035 is proposed
  with the questions Q1 to Q4.
