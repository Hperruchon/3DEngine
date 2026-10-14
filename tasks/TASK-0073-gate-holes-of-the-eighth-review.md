---
id: 0073
title: The test step cannot be emptied by a file that is not a gate file, and the gates see what MSBuild evaluates
status: Done
phase: P0.28
opened: 2026-10-12
depends-on: [0072]
governed-by: []
writes:
  create:
    - tasks/TASK-0073-gate-holes-of-the-eighth-review.md
    - eng/count-tests.js
    - eng/test-floor.txt
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

- [x] Each form of the review fails its check, and each injection of the review is in a test. T52 is the
      exception: its probe failed from the program, and no test holds it (see "Weakest").
- [x] Each new test fails before its correction.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The
      merge records the run.

## Outcome

Status: Done · v0.73 · the commit that carries this block.

No file outside the gate files can empty the step "Test": the solution file, `Directory.Solution.*`,
`*.sln.targets` and each `.user` file are gate files, and the step fails when fewer tests ran than
`eng/test-floor.txt` (437). The judge runs `git merge-tree` with `-X no-renames`. The determinism gate
refuses hidden code, a `Compile` or `Analyzer` item, the creators of `System.Numerics` that call a
transcendental function, and reflection, and it reads a folder `obj/` below the root of a project. The
dependency gate refuses each reference that it cannot read as text. The runner refuses an `async void`
check, and the review gate permits a second review on one day.

## Method

**Mechanical.** Each correction met its injection first. T48: the nine new cases of the gate files failed
on the judge of `main`, and a run that a `Directory.Solution.targets` emptied counted 0 tests and failed.
T51: the range test of a rename failed on the judge of `main`. T49 and T50: the five forms and the three
forms of the review, written into the checkout, failed the new gates; the five passed the gate of `main`,
and `dotnet msbuild -getItem:ProjectReference` listed each of the three references. The same forms are now
21 test cases; with the logic of `main` put back, 20 of them failed, and the 21st is a guard that `main`
also keeps. T52: a probe failed with its reason, and the runner of `main` crashed on it. T53: a second
review of 2026-10-13 with a suffix was read, and the gate of `main` ignored it. The full suite of 437
tests passes, and the program passes 95 runs of 75 checks.

**Judgement.** Scope item 3 asked for a parse with the symbols of each configuration and for the
evaluated `Compile` items. The gate refuses each hidden part and each `Compile` item in place: the log
path uses neither today, a refusal needs no build of the judged commit, and a parse with a list of
symbols misses a symbol that a later SDK defines. The dependency gate refuses each form that it cannot
read as text, the second option of the review, for the same reason. The floor is the count of this
task; a later task that adds tests may raise it.

**Weakest.** T52 has no test: the runner is a program that `Engine.Tests` does not reference, and a probe
needs a second build of it. A package can carry a source generator through a `PackageReference`; the log
path references one package, the native payload of this repository, so no generator exists today. NuGet
writes the two files `.nuget.g.*` in `obj/` at each restore, so the gate does not read them; a committed
copy is replaced by the restore of the pipeline, which is a property of NuGet and not of a check here.

## Notes for the implementer

- **The write set.** Each file of the judge, of the gate program, of `Engine.Tests/Governance/` and the
  workflow is a gate file, which the write set must name exactly. Name each new file in the first commit.
- **The size.** About 2 to 3 evenings. T48 first.

## Progress

- 2026-10-14: the task is open, first in the order of the owner of 2026-10-14 (question Q1 of the review,
  option A). This commit names the two new files of T48 exactly: a script that counts the tests of a run,
  and the floor that it compares with.
- 2026-10-14: T48. The solution file, `Directory.Solution.*`, `*.sln.targets` and each `.user` file are
  build inputs, so gate files, and the names of the Directory files are compared with no regard to case.
  `eng/count-tests.js` fails the step when fewer tests ran than `eng/test-floor.txt` (413). The nine new
  cases failed on the judge of `main`, and a run that a `Directory.Solution.targets` emptied counted 0
  tests and failed.
- 2026-10-14: T51. The judge runs `git merge-tree` with `-X no-renames` (git 2.55 here). The new range
  test of a rename on the branch and an edit of the old path on `main` failed on the judge of `main`.
- 2026-10-14: T52 and T53. The runner refuses an `async void` check; a probe failed with that reason,
  and the runner of `main` crashed on it. The review gate reads a name with a suffix after the date and
  orders the reviews by their ledger versions; a second review of 2026-10-13 with a suffix passed, and the
  same review with no `commit` failed, so the gate reads it. The gate of `main` ignored that file.
- 2026-10-14: T49. The determinism gate refuses code that a preprocessor condition hides, reads a folder
  `obj/` below the root of a project, refuses a `Compile` or `Analyzer` item in each file that MSBuild
  reads for the log path, and forbids the creators of `System.Numerics` that call a transcendental
  function, `Complex.Phase` and reflection. The five forms of the review failed the new gate and passed
  the gate of `main`.
- 2026-10-14: T50. The dependency gate refuses an `Include` with `$(`, `@(` or `%(`, a `Reference` item,
  each property that imports a file or moves the Directory files or `obj/`, an SDK other than the two that
  the repository uses, an `Sdk` element, and each file `obj/*.props` or `obj/*.targets` that NuGet did not
  write. `dotnet msbuild -getItem:ProjectReference` listed the three references of the review, and the new
  check refused each one while the 18 other checks passed them.
- 2026-10-14: the injections of T49, T50 and T53 are tests now. Each gate gives its rule as a function of
  a text, a folder or a list of reviews, and 21 new cases read the forms of the review. With the logic of
  `main` put back into those functions, 20 cases failed; the 21st is a review with an earlier date, which
  `main` also refuses. T52 has no test: the runner is a program that `Engine.Tests` does not reference, and
  a probe needs a second build of it. Its probe is recorded above.
- 2026-10-14: closed, after a full local run of 437 tests and 95 runs of 75 checks of the program. The
  floor is 437, and the step "Report the runner" shows the version of git for the judge.
