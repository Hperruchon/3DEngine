---
id: 0057
title: The judge of the write set and the contract gate close the holes of the fifth review
status: Ready
phase: P0.18
opened: 2026-10-09
depends-on: [0056]
governed-by: []
writes:
  create:
    - tasks/TASK-0057-judge-holes-of-the-fifth-review.md
    - Engine.Tests/Governance/WriteSetRangeTests.cs
  modify:
    - eng/write-set-check/Program.cs
    - eng/write-set-check/Judge.cs
    - Engine.Tests/Governance/WriteSetJudgeTests.cs
    - .github/workflows/ci.yml
    - CLAUDE.md
    - docs/templates.md
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

# TASK-0057 — The judge of the write set and the contract gate close the holes of the fifth review

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-09 found holes in the judge of TASK-0053 and in the pipeline. Each
finding gives its evidence in `docs/reviews/2026-10-09-codebase-review.md`.

- T29 (high): a program named `git` in the judged checkout replaces git for the judge.
- T30 (high): a root commit, a merge of unrelated history, and a merge that takes the tree of an old
  commit each pass any change, because the judge reads a merge with `git diff-tree --cc`.
- T31 (high): a test file can point the other gates of `Engine.Tests` at a clean copy of the
  repository, the attack class of T14.
- T26, T28, T32, T33 (medium): the contract gate checks a range; the judge can come from the judged
  commit; a green later push hides a red earlier push; a renamed task file fails its earlier commits.
- T34, T35 (low): the documented local check hides a failure; other defects of the judge.

## Goal

A commit with a change outside its write set fails in the pipeline in each form that the review found.

## Scope (in)

1. The judge runs git by a full path that is outside the judged checkout (T29).
2. A commit with no parent after the cut-off fails. A merge is judged against its first parent; a file
   that a judged commit of the same run changed passes (T30).
3. The judge refuses a module initializer, `AppContext.SetData`, `Environment.SetEnvironmentVariable`
   and `Directory.SetCurrentDirectory` in a changed file under `Engine.Tests/` (T31), unless the owner
   answers question Q2 of the review with option B.
4. The job fails when the base holds no judge, or when the previous tip of `main` is not known (T28).
5. On a branch, the range starts at the merge base with `main` (T32). The judge finds a task by its
   identifier at the commit (T33).
6. The contract gate checks each commit (T26).
7. The documented check stops at the first failure (T34). A failed git command is a failure, the
   judge reads the paths of git with `-z`, and `Engine.Tests/Governance/WriteSetRangeTests.cs` tests
   the steps of `eng/write-set-check/Program.cs` on a scratch repository (T35).

## Scope (out)

- No new rule of the write set. The holes close; the rules stay.
- No branch protection. That is a setting of the owner (question Q1 of the review).

## Acceptance criteria

- [ ] Each attack of the review fails the judge: a program named `git` in the root, an orphan commit
      with a forbidden change, a merge of unrelated history, a merge that takes the tree of an old
      commit, and the redirect of T31. A run on a branch that is never merged shows it.
- [ ] The contract gate fails a commit that changes `Engine.Contracts/**` with no ADR when another
      commit of the same push changes an ADR.
- [ ] The 103 commits after the cut-off, and each commit after them on `main`, still pass.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The first run.** This task changes the judge, and the pipeline builds the judge from `main`, so
  the branch of this task is judged by the old judge. The proof on a never-merged branch follows the
  merge, as for TASK-0053.
- **A scratch repository.** A test can make one with `git init` in a temporary folder, so it needs no
  history of this repository; the gate jobs clone one commit only.
