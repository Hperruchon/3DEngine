---
id: 0097
title: The test framework gate reads each source and each alias that MSBuild compiles into a test project
status: Ready
phase: P0.38
opened: 2026-10-28
depends-on: [0096]
governed-by: []
writes:
  create:
    - tasks/TASK-0097-test-sources-that-msbuild-compiles.md
  modify:
    - Engine.Tests/Governance/TestAttributeGateTests.cs
    - Engine.Tests/Governance/WriteSetJudgeTests.cs
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

# TASK-0097 — The test framework gate reads each source and each alias that MSBuild compiles into a test project

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The second codebase review of 2026-10-28 found two new forms of T62, each high (findings T68 and T69 of
`docs/reviews/2026-10-28-b-codebase-review.md`):

- T68: `TestAttributeGateTests` skips each path with a segment `bin` or `obj`, and MSBuild skips only the `bin/`
  and `obj/` folders at the root of a project. A test framework file at `Engine.Tests/Http/bin/` passes the gate,
  the build compiles it, and each failed test passes with the exact count.
- T69: the project files of `3DEngine.Tests` and `3DEngine.ImageTests` are not gate files, and the gate reads the
  syntax of `.cs` files only. A `<Using Alias>` item in such a project file hides each name of the framework.

`docs/templates.md`, section 7, puts a high finding into a task before the next product phase, so this task comes
before K2.

## Goal

No file outside the gate files can change the result of a test in a test project, in the forms of T68 and T69.

## Scope (in)

1. The gate reads each `.cs` file that MSBuild compiles: it skips `bin/` and `obj/` at the root of a project only,
   with the rule of `DeterminismCallGateTests.IsCompiled`.
2. The project file of each test project is a gate file for the judge (`eng/write-set-check/Judge.cs`), and the
   gate refuses a `Using` item and a `Compile` item that brings a name of the framework or a file from outside the
   folder of the project.
3. The two forms of the review are test cases of the gate.
4. `docs/templates.md`, section 2, names the new gate files.

## Scope (out)

- The other users of the same skip of `bin/` (`UiLibraryGateTests`, `DispatchSurfaceGateTests:38`,
  `DependencyDirectionGateTests`): TASK-0098 holds them with T70 to T72.
- The three gates that run engine code stay in the test process (T31, register entry R-0031).

## Acceptance criteria

- [ ] The file of T68 and the project file of T69 each fail the gate program.
- [ ] An injection that restores each old rule fails a test.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The judge.** A change of `eng/write-set-check/` runs the judge of `main`, so the commit that makes the new gate
  files must name each of them exactly (TASK-0053).
- **The size.** About 1 evening.

## Progress

- 2026-10-28: opened by the second codebase review of 2026-10-28 (TASK-0095).
