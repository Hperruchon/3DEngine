---
id: 0107
title: The gates read the target name, the root folders and each text file, and a host test waits for its file
status: Ready
phase: P0.43
opened: 2026-10-29
depends-on: [0106]
governed-by: []
writes:
  create:
    - tasks/TASK-0107-gate-forms-of-the-twelfth-review.md
  modify:
    - Engine.Tests/Governance/**
    - Engine.Tests/Diagnostics/**
    - Engine.Tests/Http/HostGuardTests.cs
    - eng/repo-gates/**
    - eng/write-set-check/Judge.cs
    - docs/templates.md
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - docs/adr/**
---

# TASK-0107 — The gates read the target name, the root folders and each text file, and a host test waits for its file

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-29 found medium and low forms of earlier gate findings, and a red run of `main`
(findings T80 to T87 of `docs/reviews/2026-10-29-codebase-review.md`). `docs/templates.md`, section 7, puts a
medium or low finding into a task after the next product phase, so this task comes after K0b.

## Goal

Each gate reads what the build binds in the forms of T80 to T86, and the test of T87 passes on each runner.

## Scope (in)

1. T80: the dependency gate refuses `TargetName`, or compares the name of each built assembly with its project.
2. T81: the diagnostics scanner and the check of the runtime identifier use the rule of the root of a project, and
   the scanner reads the value of a constant expression.
3. T82: the UI gate reads the constant value of each argument of a native import.
4. T83: the gate runner waits for each task that a check starts.
5. T84: the review counter counts a heading that names a product phase, and the row and the task file agree.
6. T85: an archived ADR holds a paragraph under each heading.
7. T86: the control byte gate reads each tracked text file and refuses the bidirectional controls.
8. T87: `HostGuardTests` waits until the file of the killed host opens.

## Scope (out)

- T79: TASK-0105.
- The three gates that run engine code stay in the test process (T31, register entry R-0031).

## Acceptance criteria

- [ ] Each form of the review fails the gate program, or fails a test for T87.
- [ ] An injection that restores each old rule fails a test.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-29: opened by the codebase review of 2026-10-29 (TASK-0104).
