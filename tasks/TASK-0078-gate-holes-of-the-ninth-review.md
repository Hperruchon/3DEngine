---
id: 0078
title: No file outside the gate files can empty a test project, and the gates read MSBuild names as MSBuild does
status: Active
phase: P0.30
opened: 2026-10-17
depends-on: [0077]
governed-by: []
writes:
  create:
    - tasks/TASK-0078-gate-holes-of-the-ninth-review.md
    - Engine.Tests/Governance/TestAttributeGateTests.cs
  modify:
    - .github/workflows/ci.yml
    - eng/count-tests.js
    - eng/test-floor.txt
    - eng/write-set-check/Judge.cs
    - eng/repo-gates/Program.cs
    - Engine.Tests/Governance/DeterminismCallGateTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/CodebaseReviewGateTests.cs
    - Engine.Tests/Governance/WriteSetJudgeTests.cs
    - docs/templates.md
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

# TASK-0078 — No file outside the gate files can empty a test project, and the gates read MSBuild names as MSBuild does

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-17 found gaps in the checks of the pipeline. Each finding gives its
evidence in `docs/reviews/2026-10-17-codebase-review.md`.

- T54 (high): a file with its own test attributes in the namespace `Engine.Tests` leaves no test in that
  assembly, and a forged result file beats the floor of executed tests.
- T55 (high): the gates compare MSBuild names with case, and MSBuild does not.
- T56, T57, T58 (medium): a wildcard in a project reference, six forms that the determinism gate misses,
  and a floor that is old and one total.
- T59, T60, T61 (low): an async lambda in a void check, the token and the packages of the workflow, and a
  review gate that trusts the declared ledger version.

## Goal

No file outside the gate files can take a test out of the pipeline, and the gates read MSBuild and the
compiler as they evaluate.

## Scope (in)

1. T54: the gate files name the attributes of xUnit in a form that a type of the test assembly cannot
   shadow, a gate refuses a type with the name of an attribute of xUnit outside `Xunit`, and the count of
   tests reads only the result files of the runner, with a floor for each test project.
2. T55, T56: each MSBuild name is compared with no regard to case, and a wildcard in a reference is
   refused.
3. T57: the determinism gate refuses the intrinsics of a platform except a permitted list, each name that
   ends in `Native`, `DeclaredMethods`, and a native import in the log path outside the Manifold binding.
4. T58: the floors equal the counts of `main`, and `3DEngine.ImageTests/VulkanImageFactAttribute.cs` is a
   gate file, or the image test cannot skip on Ubuntu by an edit of a file that is not a gate file.
5. T59 to T61: the runner refuses an async lambda in a check, the workflow reads the repository with the
   least permission and records the version of Mesa, and the review gate requires a ledger version that
   exists.

## Acceptance criteria

- [ ] Each form of the review fails its check, and each injection of the review is in a test where a test
      can hold it.
- [ ] Each new test fails before its correction.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The write set.** Each file of the judge, of the gate program, of `Engine.Tests/Governance/` and the
  workflow is a gate file, which the write set must name exactly. Name each new file in the first commit;
  remove a named new file that the work does not need before the close.
- **The order.** Question Q1 of the review asks whether this task comes before R7.
- **The size.** About 2 to 3 evenings.

## Progress

- 2026-10-17: the task is active, first in the order of the owner of 2026-10-17 (question Q1 of the review,
  option A). Item 4 of the scope takes its second form: an exact count for each test project makes a skip
  of the image test on Ubuntu fail the run, so the file of the attribute needs no new rule of its own.
- 2026-10-17: T54, first line. `TestAttributeGateTests` refuses a type, a nested type, a delegate or an alias
  with a name that a test finds through `Xunit`, and a namespace named `Xunit`, in each test project. The
  shadow file of the review failed it from the gate program, which passed that file before; the eight forms
  are test cases.
- 2026-10-17: T54, second line, and T58. `eng/count-tests.js` checks an exact count for each test project,
  with an optional value for one platform, and requires one result file for each project and no other. A
  real result folder with one project removed and a forged result of 1,000 tests passed the script of `main`
  (1,439 against 437) and failed the new script for each of the three reasons. Nine test cases hold the
  rules. A skip of the image test on Ubuntu now lowers its exact count and fails the run, so its attribute
  file needs no rule of its own.
