---
id: 0066
title: The checks from main take no input of the build from the judged checkout, and the judge reads each change of a merge
status: Done
phase: P0.26
opened: 2026-10-12
depends-on: [0065]
governed-by: []
writes:
  create:
    - tasks/TASK-0066-gate-holes-of-the-seventh-review.md
  modify:
    - .github/workflows/ci.yml
    - eng/write-set-check/Judge.cs
    - eng/write-set-check/Program.cs
    - eng/repo-gates/Program.cs
    - eng/repo-gates/RepoGates.csproj
    - Engine.Tests/Engine.Tests.csproj
    - Engine.Tests/Governance/DeterminismCallGateTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/WriteSetJudgeTests.cs
    - Engine.Tests/Governance/WriteSetRangeTests.cs
    - docs/templates.md
    - docs/INDEX.md
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

# TASK-0066 — The checks from main take no input of the build from the judged checkout, and the judge reads each change of a merge

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-12 found gaps in the checks that run from a build of `main`. Each finding
gives its evidence in `docs/reviews/2026-10-12-codebase-review.md`.

- T41 (high): a `global.json` of the judged checkout chooses the SDK that builds the judge and the gates
  of `main`, because the job builds them in that checkout.
- T42 (high): a merge can undo a change of its first parent in a file that its second parent changed,
  and the judge does not see it. T43 (medium): a clean merge after `main` moved fails the judge.
- T44 (high): files that are not gate files switch the gates to the judged commit and control its
  packages.
- T45, T46 (medium): the determinism gate and the dependency gate miss forms that compile.
- T47 (low): the runner of the gates skips forms with no failure.

## Goal

A commit cannot replace a check of `main` or feed it an input of the build, and a merge is judged on
each file where it differs from the automatic merge of its parents.

## Scope (in)

1. Each build and each run of a program of `main` in the pipeline runs in a folder outside the judged
   checkout, and `global.json` is a gate file (T41).
2. One list of the inputs of a build (`global.json`, `nuget.config`, `nuget/**`, each `Directory.*` file
   by its real name) is read by `Judge.IsGateFile` and by the switch of the step "Run the repository
   gates" (T44).
3. The judge reads a merge on the files where it differs from the automatic merge of its parents
   (`git merge-tree --write-tree`) and on each file with a conflict (T42, T43).
4. The determinism gate reads the called methods of the compiled assemblies of the log path (T45). The
   dependency gate reads the evaluated references, or refuses each import that it cannot resolve
   (T46). The runner fails on each form that it cannot run, and counts a skip as a failure (T47).

## Scope (out)

- No change to engine code or to a host.
- The three gates that run engine code stay tests (R-0031).

## Acceptance criteria

- [x] Each form of the review fails its check: the planted `global.json`, a file
      `docs/Directory.Packages.txt`, the merge that takes the side version, the eight calls of T45, the
      wildcard import and the `.csproj.user` file of T46, and the probe forms of T47.
- [x] The clean merge of T43 passes the judge.
- [x] Each new test fails before its correction.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The
      merge records the run.

## Outcome

Status: Done · v0.67 · the commit that carries this block.

The checks of `main` take no input of the build from the judged checkout: each build of a program of
`main` runs in `$RUNNER_TEMP`, and `global.json`, each NuGet configuration and `nuget/` are gate files.
The judge of `main` answers the switch of the gates (`--gate-sources`) from `Judge.IsGateProgramSource`,
the list that the gate files share. A merge is judged on each file where it differs from the automatic
merge of its parents. The determinism gate binds each name of its list with Roslyn. The dependency
gate reads the `.user` file of a project and refuses an import that it cannot resolve. The runner of
the gates fails on each form that it cannot run and on a skip.

## Method

**Mechanical.** The tests of T42, T43 and T44 ran first on the judge of `main` (`4a4450d`), put in
place of the built judge: the 15 new cases failed, and they pass on the new judge with the 98 older
governance tests. T41: a build from a folder with a planted `global.json` stopped with its message, and
the same build from another folder ignored it and wrote the judge. T45: a file in `Engine.Core` with the
eight forms of the review and a generic `T.Sin` failed the gate from the program on each of its nine
lines; the review agent saw the old gate pass the eight. T46: a wildcard import in `Engine.Core` failed
the new check, and a `.csproj.user` file in `Engine.Contracts` failed two rules of the authority diagram.
T47: a probe file with a static check, a skip, a failing `ValueTask`, a row of `[MemberData]`, a nested
class, an internal class and an inherited check failed each form. Each probe was removed, and the run
after it passed. The full suite of 369 tests passes, and the program passes 71 runs of 66 checks.

**Judgement.** The automatic merge, and not a third set beside the two of TASK-0061: it is the one
reference against which each of the four merges of the reviews (T30, T36, T42, T43) is right. The
determinism gate compiles the sources with the assemblies of the framework that runs it, and not the
output of a build of the judged commit, which that commit controls. A name of the list that does not
bind fails, so a form that the compiler cannot resolve is not a hole. The switch needs one bootstrap:
the judge of `main` before this task does not know `--gate-sources`, so the step takes the gates of
the judged commit only for a base up to `29ed267`, and fails for any later base with no answer.

**Weakest.** The fix of T41 is in the workflow, which no test runs; the local run of the same commands
is the evidence. The bootstrap clause holds a fixed commit; a force push that moves `main` back before
this task would open it again (R-0038). The determinism gate binds with the framework that runs it, so a
newer framework with a new forbidden method needs a new name in the list.

## Notes for the implementer

- **The write set.** Each file of the judge, of the gate program, of `Engine.Tests/Governance/` and the
  workflow is a gate file, which the write set must name exactly. Name each new file in the first
  commit of the task, before the work.
- **The scripts.** The review agent kept the two merges as shell scripts; the review gives their steps.
- **The size.** About 2 to 3 evenings. T41 first: it is a few lines.

## Progress

- 2026-10-12: the task is open, first in the order of the owner of 2026-10-12 (question Q1 of the
  review, option A). This commit names each file of the work exactly: the program of the judge gets a
  mode for the switch of the gates (T44), and the determinism gate needs a C# parser in both projects
  that compile it (T45).
- 2026-10-12: T41 to T44. The builds of `main` run in the folder of the runner, each input of a build is
  a gate file, the judge gives the switch of the gates from that one list, and a merge is judged
  against the automatic merge of its parents. The 14 new test cases failed on the judge of `main`.
  The judge of `main` does not know the question of the switch, so the step takes the gates of the
  judged commit for a base up to `29ed267`, the tip of `main` before this task, and fails for each
  other base with no answer.
- 2026-10-12: T45 to T47. The determinism gate binds each name of its list with Roslyn, the dependency
  gate reads the `.user` file and refuses an import that it cannot resolve, and the runner fails on a
  form that it cannot run and on a skip. Each form of the review failed from the program.
- 2026-10-12: closed, after a full local run of 369 tests and 71 runs of the program.
