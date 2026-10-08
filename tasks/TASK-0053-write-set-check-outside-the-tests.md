---
id: 0053
title: The write-set check runs as a program that the pipeline builds from main
status: Active
phase: P0.17
opened: 2026-10-08
depends-on: [0052]
governed-by: []
writes:
  create:
    - tasks/TASK-0053-write-set-check-outside-the-tests.md
    - eng/write-set-check/WriteSetCheck.csproj
    - eng/write-set-check/Program.cs
    - eng/write-set-check/Judge.cs
    - eng/write-set-check/TaskFiles.cs
    - Engine.Tests/Governance/WriteSetJudgeTests.cs
  modify:
    - .github/workflows/ci.yml
    - Engine.Tests/Engine.Tests.csproj
    - Engine.Tests/Governance/WriteSetGateTests.cs
    - Engine.Tests/Governance/RepositoryFiles.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - CLAUDE.md
    - docs/templates.md
    - docs/INDEX.md
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - docs/adr/**
---

# TASK-0053 — The write-set check runs as a program that the pipeline builds from main

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Finding T14 of the codebase review of 2026-10-08 (high): the dynamic part of
`Engine.Tests/Governance/WriteSetGateTests.cs` returns when the variable `WRITE_SET_FILES` is empty, and
the pipeline runs it from the build of the commit that it judges. A review agent added a file under
`Engine.Tests/` with a module initializer that clears the variable, and the gate passed for a forbidden
path and for a gate file. Each task that permits a path under `Engine.Tests/` can do this.

On 2026-10-08 the owner chose option A of question Q1 of that review: the check runs as a program in
`eng/` that the pipeline builds from `main`, so that a change on a branch cannot change its judge.
Register entry R-0031 records the answer.

## Goal

A commit on a branch cannot change the code that judges its write set.

## Scope (in)

1. A program in `eng/write-set-check/` holds the dynamic rules of today: the governing task from the
   trailer, the task file at the commit, the exact name of a gate file, a task that is `Done` before
   and after the commit, the cut-off commit, and the rules for a merge commit.
2. The job "Write set of the governing task" checks out `main` in a separate folder, builds or runs the
   program from there, and gives it the commits of the push. A push to `main` itself uses the program
   of the commit before the push.
3. The dynamic test in `WriteSetGateTests` is removed or calls the program, so that one implementation
   stays. The static tests stay.
4. Each file in `eng/write-set-check/` is a gate file, named exactly.
5. `CLAUDE.md`, section "Method", and `docs/templates.md`, section 2, give the new local command.

## Scope (out)

- No new rule. The rules of the gate move, and they do not change.
- No change to `eng/write-set-cutoff.txt`.

## Acceptance criteria

- [ ] The injection of the review fails the gate: a file under `Engine.Tests/` with a module
      initializer that clears `WRITE_SET_FILES`, in a commit that also changes a forbidden path. The
      Outcome block records the run.
- [ ] A commit on a branch that changes the program is judged by the program of `main`. A run on a
      branch that is never merged shows it.
- [ ] Each rule of today fails on its own injection, as before the move.
- [ ] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The language.** The parsers of the task files are C# in `Engine.Tests/Governance/RepositoryFiles.cs`.
  A Node.js script is the pattern of `eng/report-failed-tests.js`, and the computer of the owner has
  Node.js and no Python. A small C# console project can link the parser file and avoid a second copy.
  Choose one, and give the reason in the Method block.
- **The order.** TASK-0038 depends on this task, because TASK-0038 permits `Engine.Tests/**`.

## Progress

- 2026-10-09: the judge `eng/write-set-check` holds the rules, and the pipeline builds it from `main`.
  The replay of the 103 commits after the cut-off passes, and each of eight rules fails on its own
  injection into `Judge.cs`.
