---
id: 0073
title: The test step cannot be emptied by a file that is not a gate file, and the gates see what MSBuild evaluates
status: Ready
phase: P0.28
opened: 2026-10-12
depends-on: [0072]
governed-by: []
writes:
  create:
    - tasks/TASK-0073-gate-holes-of-the-eighth-review.md
  modify:
    - .github/workflows/ci.yml
    - eng/write-set-check/Judge.cs
    - eng/repo-gates/Program.cs
    - Engine.Tests/Governance/DeterminismCallGateTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/CodebaseReviewGateTests.cs
    - Engine.Tests/Governance/WriteSetJudgeTests.cs
    - Engine.Tests/Governance/WriteSetRangeTests.cs
    - docs/templates.md
    - docs/register.md
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

# TASK-0073 — The test step cannot be emptied by a file that is not a gate file, and the gates see what MSBuild evaluates

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-13 found gaps in the checks of the pipeline. Each finding gives its
evidence in `docs/reviews/2026-10-13-codebase-review.md`.

- T48 (high): four kinds of file that are not gate files take `Engine.Tests` out of the step "Test", and
  the judge passes them.
- T49, T50 (medium): the determinism gate and the dependency gate miss forms that MSBuild evaluates.
- T51 (medium): a rename in one parent carries a change into a gate file through a clean merge.
- T52, T53 (low): the runner passes an `async void` check, and the review gate permits one review for each
  date.

## Goal

No file outside the gate files changes which tests the pipeline runs, and the gates read what MSBuild and
git evaluate.

## Scope (in)

1. The solution file, `Directory.Solution.*`, `*.sln.targets` and each `.user` file of a project are gate
   files, and the step "Test" fails below a minimum count of tests that `main` gives (T48).
2. The judge runs `git merge-tree` with no detection of renames, after a check of the version of git on
   the runner (T51).
3. The determinism gate parses with the symbols of each configuration and reads the evaluated `Compile`
   items, and refuses the creators of `System.Numerics` that call a transcendental function and reflection
   on `Math` (T49). The dependency gate reads the evaluated references, or refuses each form that it cannot
   resolve (T50).
4. The runner refuses an `async void` check (T52). The review gate permits a suffix after the date and
   compares the ledger versions (T53).

## Acceptance criteria

- [ ] Each form of the review fails its check, and each injection of the review is in a test.
- [ ] Each new test fails before its correction.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The write set.** Each file of the judge, of the gate program, of `Engine.Tests/Governance/` and the
  workflow is a gate file, which the write set must name exactly. Name each new file in the first commit.
- **The size.** About 2 to 3 evenings. T48 first.
