---
id: 0121
title: A solve keeps each radius of a circle positive
status: Ready
phase: P0.48
opened: 2026-10-30
depends-on: [0120]
governed-by: [0004, 0033, 0035, 0037, 0039]
writes:
  create:
    - tasks/TASK-0121-positive-radius-of-a-solve.md
  modify:
    - Engine.Core/Sketch/SketchSolver.cs
    - Engine.Core/Sketch/SketchConstraints.cs
    - Engine.Tests/Sketch/CircleArcTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Api.Http/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0121 — A solve keeps each radius of a circle positive

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-30 (b) found E97 (high): a solve can give a circle a negative radius or a radius of
zero, and the sketch shows as solved. `docs/templates.md`, section 7, puts a high finding into a task before the next
product phase, so this task comes before K2e.

## Goal

No solve gives a circle a radius that is not greater than 0, and the sketch then shows as not solved.

## Scope (in)

1. `SketchSolver.Converge` gives no solution when a radius is not greater than 0, so the record keeps the positions
   of the command, as for each other solve that fails.
2. `SketchConstraints.Refusal` refuses a `tangent` of a line with an end at the centre of the curve.
3. A test for each case of E97, with the bits of the result on the three runners.

## Scope (out)

- E98 (an arc of radius zero at the add). P0.49 holds it.

## Acceptance criteria

- [ ] Each probe of E97 fails before the change and passes after it.
- [ ] An injection that removes the check of the radius fails a test.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-30: opened by the codebase review of 2026-10-30 (b) (TASK-0120).
