---
id: 0130
title: A radius at the noise level does not solve, and the check of the tasks binds the real gate files
status: Ready
phase: P0.51
opened: 2026-10-31
depends-on: [0129]
governed-by: [0004, 0033, 0035, 0037, 0039]
writes:
  create:
    - tasks/TASK-0130-noise-radius-and-gate-usings.md
  modify:
    - Engine.Core/Sketch/SketchSolver.cs
    - Engine.Tests/Sketch/CircleArcTests.cs
    - eng/repo-gates/DiscardedTasks.cs
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

# TASK-0130 — A radius at the noise level does not solve, and the check of the tasks binds the real gate files

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-31 found E102 (high): a radius of 5.6e-17 passes the check of E97, and the sketch shows
as solved. It found T103 (high): the check of the tasks of a gate has no implicit global using, so it binds no task
of a real gate file. `docs/templates.md`, section 7, puts a high finding before the next product phase.

## Goal

A solve gives no circle or arc of a radius at the noise level, and the check of the tasks refuses each form of T74,
T94, T95 and T100 with the spelling of a real gate file.

## Scope (in)

1. E102: each radius, |s − c| and |e − c| must be greater than `SketchSolver.Tolerance` in `Converge` and `Status`.
2. T103: the compilation of `DiscardedTasks` has the implicit usings of the test project; a run whose compilation of a
   gate file has an error fails; the self check uses the preamble of a real file.

## Scope (out)

- The medium and low findings (P0.52, P0.53).

## Acceptance criteria

- [ ] Each probe of E102 fails before the change and passes after it.
- [ ] A thread, a task in a local, a semaphore and LongRunning with no full name in a gate file fail the gate program.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-31: opened by the codebase review of 2026-10-31 (TASK-0129).
