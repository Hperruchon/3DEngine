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

- [x] The injection of the review fails the gate: a file under `Engine.Tests/` with a module
      initializer that clears `WRITE_SET_FILES`, in a commit that also changes a forbidden path. The
      Outcome block records the run. A local run of the pipeline steps; the pipeline run follows the
      merge, because `main` holds no judge before it.
- [x] A commit on a branch that changes the program is judged by the program of `main`. A run on a
      branch that is never merged shows it. A local run, as for the criterion above.
- [x] Each rule of today fails on its own injection, as before the move.
- [x] `dotnet build 3DEngine.sln --no-incremental` gives zero warnings. `dotnet test` passes.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The
      merge records the run.

## Notes for the implementer

- **The language.** The parsers of the task files are C# in `Engine.Tests/Governance/RepositoryFiles.cs`.
  A Node.js script is the pattern of `eng/report-failed-tests.js`, and the computer of the owner has
  Node.js and no Python. A small C# console project can link the parser file and avoid a second copy.
  Choose one, and give the reason in the Method block.
- **The order.** TASK-0038 depends on this task, because TASK-0038 permits `Engine.Tests/**`.

## Outcome

Status: Done · v0.51 · the commit that carries this block.

The judge of the write set is the program `eng/write-set-check`. The job "Write set of the governing
task" builds it from `main`, or from the tip before the push for a push to `main`, and runs it on each
commit of the push. A commit on a branch cannot change its judge. `Engine.Tests` keeps the static tests
of the write set and gains `WriteSetJudgeTests` for the rules.

The local run of the pipeline steps on 2026-10-09: two commits on a branch that was never pushed, on top
of the first commit of this task, which played the part of `main`. Commit 1 was the injection of the
review under TASK-0038, which permits `Engine.Tests/**`: a test file with a module initializer that
clears `WRITE_SET_FILES` and `WRITE_SET_TASK`, and a change to `Engine.Core/Commands/NoOpCommand.cs`,
which TASK-0038 forbids. Commit 2 changed `Judge.cs` to pass each change, under TASK-0053, and changed
`Engine.Contracts/Document.cs`, which TASK-0053 forbids. The judge built from the commit of `main`
failed both commits on the forbidden path. The judge built from the injected commit passed both.

## Method

**Mechanical.** The rules moved from `WriteSetGateTests` into `eng/write-set-check/Judge.cs` with the
same text; an xUnit assertion became a problem string. The replay of the 103 commits after the cut-off
through the new judge passes each one, as the old gate did. Each of eight rules failed on its own
injection into `Judge.cs`, and the file was restored after each run: a gate file of the judge, a
pattern that permits a gate file, a forbid, a task that is Done before and after, the start of the
rules of TASK-0047, a named task that the commit does not change, two task files with no name, and a
prose line as a trailer.

**Judgement.** A C# program and not a Node.js script. The reader of the task files moved into the
program, and the gates of `Engine.Tests` call it through a project reference, so one reader serves the
judge and the gates; a script would be a second reader in a second language. The program references no
project, and `DependencyDirectionGateTests` holds that, so the pipeline builds it alone from `main`. The
steps of the range, the cut-off and the merge rule moved from the workflow into the program too,
because a step in the workflow comes from the commit under judgment.

**Weakest.** The workflow itself comes from the commit under judgment, as GitHub runs each workflow
from the pushed commit. A task that names `.github/workflows/ci.yml` exactly can still change the job;
the diff shows it, and the exact name is the only permit. A required workflow or a branch protection
on GitHub closes that, and both are settings of the owner. The pipeline builds the judge with the SDK
of the commit under judgment; a commit that changes the pin in `global.json` can stop the build of the
judge, which fails the job and does not pass it. Until this task is on `main`, the job runs the judge
of the commit, and a notice says so.

## Progress

- 2026-10-09: the judge `eng/write-set-check` holds the rules, and the pipeline builds it from `main`.
  The replay of the 103 commits after the cut-off passes, and each of eight rules fails on its own
  injection into `Judge.cs`.
- 2026-10-09: closed. The local run of the pipeline steps failed both injected commits with the judge
  of `main` and passed both with the judge of the injected commit.
- 2026-10-09: reopened to record the pipeline run on a branch that is never merged.
