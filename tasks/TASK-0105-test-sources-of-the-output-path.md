---
id: 0105
title: The test framework gate reads the output path and each MSBuild file of a test project
status: Ready
phase: P0.41
opened: 2026-10-29
depends-on: [0104]
governed-by: []
writes:
  create:
    - tasks/TASK-0105-test-sources-of-the-output-path.md
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

# TASK-0105 — The test framework gate reads the output path and each MSBuild file of a test project

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-29 found a new form of T68 and T69, high (finding T79 of
`docs/reviews/2026-10-29-codebase-review.md`):

- `TestAttributeGateTests` leaves out the folder `<project>/bin/` by name, and MSBuild leaves out
  `$(BaseOutputPath)`. A test project that moves its output with `BaseOutputPath` compiles its root `bin/` folder,
  and a test framework file there passes the gate.
- MSBuild imports `<project>.csproj.user`, and the gate reads only `<project>.csproj`. A `<Using Alias>` item in the
  `.user` file hides each name of the framework.

In both forms each failed test of `3DEngine.Tests` passed with the exact count of `eng/test-floor.txt`.
`docs/templates.md`, section 7, puts a high finding into a task before the next product phase, so this task comes
before K0b.

## Goal

No file outside the gate files can change the result of a test in a test project, in the forms of T79.

## Scope (in)

1. The gate refuses `BaseOutputPath`, `OutputPath`, `BaseIntermediateOutputPath` and `DefaultItemExcludes` in each
   MSBuild file of a test project, or reads the folders that MSBuild leaves out from the evaluation.
2. The gate reads each MSBuild file that a test project imports, the `.csproj.user` file too, with the rule of its
   project file. The judge names those files as gate files.
3. The two forms of the review are test cases of the gate.
4. `docs/templates.md`, section 5, names the new gate files.

## Scope (out)

- The other gates that skip `bin/` at each depth, and `TargetName` (T80, T81): TASK-0107.
- The three gates that run engine code stay in the test process (T31, register entry R-0031).

## Acceptance criteria

- [ ] The file of each form of T79 fails the gate program.
- [ ] An injection that restores each old rule fails a test.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`, on the branch and on
      `main` after the merge.

## Notes for the implementer

- The forms need `git add -f`, because `.gitignore` holds `bin/` and `*.user`. A test of the gate writes the files
  in a scratch copy, as the tests of T68 and T69 do.
- The review recommends the evaluation of MSBuild over more names of properties: each new name is a new form.

## Progress

- 2026-10-29: opened by the codebase review of 2026-10-29 (TASK-0104).
