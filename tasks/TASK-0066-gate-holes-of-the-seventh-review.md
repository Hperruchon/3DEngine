---
id: 0066
title: The checks from main take no input of the build from the judged checkout, and the judge reads each change of a merge
status: Active
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

- [ ] Each form of the review fails its check: the planted `global.json`, a file
      `docs/Directory.Packages.txt`, the merge that takes the side version, the eight calls of T45, the
      wildcard import and the `.csproj.user` file of T46, and the probe forms of T47.
- [ ] The clean merge of T43 passes the judge.
- [ ] Each new test fails before its correction.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

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
