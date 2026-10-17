---
id: 0078
title: No file outside the gate files can empty a test project, and the gates read MSBuild names as MSBuild does
status: Done
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

- [x] Each form of the review fails its check, and each injection of the review is in a test where a test
      can hold it. T59 and T60 have no test (see "Weakest").
- [x] Each new test fails before its correction.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The
      merge records the run.

## Outcome

Status: Done · v0.79 · the commit that carries this block.

No file outside the gate files can take a test out of the pipeline with a green run: a gate refuses a shadow
of the names of xUnit, and each test project must execute its exact count, with one result file. The gates
read MSBuild names with no regard to case and refuse a wildcard reference; the determinism gate binds each
name of the log path. The runner fails an async lambda that returns void, the workflow reads the repository
only and logs the versions of lavapipe, and a review must declare a ledger version that exists.

## Method

**Mechanical.** Each correction met the form of the review first. The shadow file failed the new gate from the
gate program, which passed it before. A real folder of results with one project removed and a forged result
passed the count of `main` (1,439 against 437) and failed the new count three times. The new cases of T55 to
T57 and T61 each failed with the old rule put back into the new code (8, 6 and 2 cases). The async lambda of
T59 passed the runner of `main` and failed the new runner. Run 39028617456 proved the exact counts on the
three runners, and run 39029140388 the workflow with a read-only token.

**Judgement.** An exact count and not a floor: a floor has slack, and the review used it; an exact count asks
each task that adds tests to write the new number, which the judge then shows. The determinism gate permits the
name of a type or a member through reflection, because `Engine.Core/QueryBus.cs` reads one and it calls no
code. The file of the attribute of the image test needs no rule of its own, because a skip on Ubuntu changes
the exact count.

**Weakest.** A test of one project can still overwrite the result file of another project that the runner
already wrote, if it finds its name; the count then reads a forged number for that project. T59 and T60 have no
test: the runner is a program that `Engine.Tests` does not reference, and the workflow runs only in the
pipeline. The three gates that run engine code stay in the test process (T31).

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
- 2026-10-17: T55 and T56. The dependency gate and the determinism gate read the names of MSBuild elements
  and attributes with no regard to case, the properties that import a file in each case, and the element
  of a runtime identifier in each case; a project reference with a wildcard is refused. Eight new cases
  hold the lower-case forms of the review and the wildcard; with names compared with case and no wildcard
  check, each of the eight failed.
- 2026-10-17: T57. The determinism gate binds each name of the log path: a member of the framework whose
  name ends in `Native`, a member of the intrinsics of one platform, reflection (except the name of a type
  or a member, which `Engine.Core/QueryBus.cs` reads), and a native import outside the Manifold binding are
  offences. The six forms of the review are test cases; with only the names of the list bound, each of the
  six failed.
- 2026-10-17: T59. The gate runner fails each async method or lambda of the build that returns void, so a
  check that calls one cannot pass before its end. The probe of the review (an async lambda that fails
  after 30 s) passed the runner of `main` and failed the new runner. No test holds it; the runner is a
  program that `Engine.Tests` does not reference, as for T52.
- 2026-10-17: T60. The workflow gives each job a token that reads the repository only, no checkout keeps
  the token, and the step of lavapipe writes the version of each package to the log. CI is the proof.
- 2026-10-17: T61. The review gate requires that the declared `ledger` version of each review has an entry in
  `docs/CURRENT-STATE.md`. Three cases hold it; with the old rule, the two versions with no entry passed.
- 2026-10-17: closed, after a clean build and a full local run of 495 tests (449 in `Engine.Tests`, 39 in
  `3DEngine.Tests`, 7 in `3DEngine.ImageTests`) and 123 runs of 81 checks of the gate program.
