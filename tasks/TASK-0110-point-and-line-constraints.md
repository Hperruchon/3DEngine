---
id: 0110
title: The engine solves the seven constraints of points and lines that K2a named
status: Active
phase: K2c
opened: 2026-10-30
depends-on: [0107]
governed-by: [0004, 0012, 0016, 0021, 0028, 0031, 0033, 0035, 0037]
writes:
  create:
    - tasks/TASK-0110-point-and-line-constraints.md
    - Engine.Tests/Sketch/LineConstraintTests.cs
  modify:
    - Engine.Core/Sketch/SketchConstraints.cs
    - Engine.Core/Sketch/SketchSolver.cs
    - Engine.Core/Commands/AddSketchConstraintCommandHandler.cs
    - Engine.Core/Commands/SetSketchConstraintValueCommandHandler.cs
    - Engine.Tests/Sketch/SketchModelTests.cs
    - docs/adr/0033-sketch-model.md
    - docs/adr/0035-sketch-solver.md
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
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - eng/write-set-check/**
    - eng/repo-gates/**
---

# TASK-0110 — The engine solves the seven constraints of points and lines that K2a named

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0033 item 3 names fourteen constraints. K2a and K3a give the kinds 1 to 5. The owner approved the plan of
2026-10-30 (answers A): K2c gives the kinds of the list that act on points and lines only, and K2d gives the circle,
the arc and the kinds on them. The kind is a string of `SketchConstraint`, and `AddSketchConstraint` already has the
fields `third`, `value` and `secondValue`, so `Engine.Contracts` does not change. Under answer Q2 A, the equations of
the solver are a History line of ADR-0035, and the kinds of K2c a History line of ADR-0033.

## Goal

`AddSketchConstraint` accepts `pointOn` (a point and a line), `midpoint`, `parallel`, `perpendicular`, `equal` (two
lines), `symmetric` and `angle`, the solver satisfies each one with the same bits on each platform, and `GetSketch`
gives the residual of each one.

## Scope (in)

1. The check of the items and the values of the seven kinds, with `E-SKETCH-CONSTRAINT-INVALID`. A point of `pointOn`
   or `midpoint` is not an end of its line. Two lines are two lines. The two points of `symmetric` are two points.
2. `angle` takes (cos θ, sin θ). The handler refuses a pair whose cos² + sin² differs from 1 by more than 1e-9, and
   records the pair that it normalises with `sqrt` (ADR-0033 item 3). `SetSketchConstraintValue` does the same.
3. The equations of the solver, in length units, with their derivatives by formula, and the residual of each kind.
4. Tests: each kind solves, the derivatives agree with a central difference, the bits of a fixed solve, `angle`
   keeps its branch, and each refusal.

## Scope (out)

- The circle, the arc, `tangent`, `radius`, and `pointOn` and `equal` on a circle or an arc: K2d.
- The desktop: the panel of constraints keeps its five kinds. K2e or a later task adds the new kinds to it.

## Acceptance criteria

- [ ] Each of the seven kinds solves a rough sketch to a residual less than 1e-9.
- [ ] The analytic derivatives of each new equation agree with a central difference.
- [ ] A fixed solve with the new kinds gives constant bits, and the pipeline runs it on the three runners.
- [ ] An injection that breaks each new equation or each new refusal fails a test.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-30: opened by the plan of 2026-10-30 (answers A to Q1 to Q4 and KQ1 to KQ4).
- 2026-10-30: the seven kinds, their equations, their residuals and their refusals; 19 new test cases; 12
  injections caught. The gate of governed-by added 0004, 0012, 0016, 0021, 0028, 0031 and 0037.
