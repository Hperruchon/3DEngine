---
id: 0102
title: The engine solves the five constraints of a sketch, and the desktop shows if a sketch is solved
status: Done
phase: K3a
opened: 2026-10-29
depends-on: [0099, 0101]
governed-by: [0004, 0007, 0012, 0016, 0018, 0019, 0021, 0025, 0027, 0028, 0029, 0030, 0031, 0032, 0033, 0034, 0035]
writes:
  create:
    - tasks/TASK-0102-sketch-solver.md
    - docs/adr/0035-sketch-solver.md
  modify:
    - Engine.Core/Sketch/**
    - Engine.Core/Commands/MoveSketchPointCommandHandler.cs
    - Engine.Core/Queries/GetSketchQuery.cs
    - Engine.Core/Queries/GetSketchQueryHandler.cs
    - Engine.Tests/Sketch/**
    - Engine.Tests/Http/HttpSketchScenarioTests.cs
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
- A change of the fields or of the schema version of a command. Only `MoveSketchPoint` changes: it names its
  point as held (ADR-0035 item 7).

## Acceptance criteria

- [x] The owner accepts ADR-0035 before the code.
- [x] A rectangle of four rough points with two `horizontal`, two `vertical` and two `distance` constraints and one
      `fixed` gives exact positions, and `GetSketch` says solved with 0 free degrees.
- [x] A set that conflicts gives an applied command, the positions of the command, and `solved` false.
- [x] A move of a point holds the point when a solution exists.
- [x] A test of the bits of fixed solves passes on the three runners of the pipeline.
- [x] An undo after a solve gives back the record before, with no solve.
- [x] A file of v0.101 with an unsolved constraint opens, and its sketch is solved.
- [x] Each new test fails when its injection breaks the code that it covers.
- [x] The visual check: a rough rectangle in the desktop becomes exact as its constraints come, and the window shows
      the line of the status.
- [x] `dotnet test` passes, and the build gives zero warnings.

## Notes for the implementer

- The determinism gate reads `Engine.Core`. Do not call `Math.Pow`, `Math.FusedMultiplyAdd` or a vector type.
- Keep one fixed order: the points and the constraints in the order of the record, the pivot search from the first
  row, and a tie of pivots to the first row.

## Progress

- 2026-10-29: the owner chose the order K3a, then P0.39. TASK-0098 now depends on this task. ADR-0035 is proposed
  with the questions Q1 to Q4.
- 2026-10-29: the owner accepted ADR-0035 with Q1 to Q4 A. ADR-0033 is amended.
- 2026-10-29: `SketchSolver` solves each changed record in `SketchResults.Changed`; `MoveSketchPoint` holds its point;
  `GetSketch` gives `solved` and `freedom`, and the window "Sketch" shows them. Four tests of K2a and K2b that
  expected an unsolved residual now expect a solved record or a conflict. A file that the server of v0.101 wrote
  opens and solves. 14 injections: 11 caught at once, and 3 after two more tests (a move with a free start, and
  redundant rows with irrational entries).
- 2026-10-29: CI run 40836376649 passed the step Test on the three runners, the bit test with them (macOS on arm64),
  and failed only at the count of the tests. The test floor now gives 703 in `Engine.Tests` and 96, or 101 on Linux,
  in `3DEngine.ImageTests`. The visual check ran the desktop on 127.0.0.1.
- 2026-10-29: the owner accepted the pictures of the visual check: a rough rectangle with 6 free degrees, then 2
  after two horizontal and two vertical constraints, then 0 after the lengths 10 and 6; a length 7 of L3 gave
  "Not solved" with the positions kept, and Ctrl+Z gave "Solved, 0 free". The constraints of the check came through
  the HTTP surface of the desktop, and the rectangle through clicks.

## Outcome

Status: Done · commit (the merge of the branch k3a-sketch-solver) · v0.103

## Method

Mechanical: the equations of the five kinds and their derivatives, the elimination, the two fields of GetSketch,
            and the status line of the window.
Judgement:  the solve runs in `SketchResults.Changed`, the one place that each sketch command uses, so no handler
            can forget it; only `MoveSketchPoint` changes, to name its held point. A converged first step
            gives the record back as it is, so a solved record keeps its bits. The test of an old file reads a
            file that the real server of v0.101 wrote, and not a file that this engine writes.
Weakest:    the limit of a pivot (1e-12 of the largest diagonal entry of J·Jᵀ) squares the condition of J, so
            a sketch with very different sizes can count a redundant row as independent, or the reverse;
            `A_Redundant_Row_With_Irrational_Entries_Is_Dropped` tests one case only. The bit test proves
            one sketch on three runners, not each sketch.
