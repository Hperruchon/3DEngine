---
id: 0130
title: A radius at the noise level does not solve, and the check of the tasks binds the real gate files
status: Done
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

- [x] Each probe of E102 fails before the change and passes after it.
- [x] A thread, a task in a local, a semaphore and LongRunning with no full name in a gate file fail the gate program.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p051-noise-radius-usings) · v0.132

## Method

Mechanical: the tolerance in the check of a radius, the global usings, the failure of a file that does not compile,
            and four forms of the self check.
Judgement:  the threshold of a radius is the tolerance of the equations, the length below which the solver cannot tell
            a value from 0; a gate file with an error of the compiler fails the run, so a missing using cannot hide a
            form again.
Weakest:    the list of global usings is the list of the SDK and of Xunit in the code; a new item Using of a project
            needs a line there, and a gate file that needs it then fails the run with its error, which shows it.

## Progress

- 2026-10-31: opened by the codebase review of 2026-10-31 (TASK-0129).
- 2026-10-31: E102 with the tolerance of the solver; T103 with the global usings, a failure for a gate file that does not
  compile, and four forms of the self check with no full name; 2 new test cases; 4 injections caught.
- 2026-10-31: closed at v0.132.
