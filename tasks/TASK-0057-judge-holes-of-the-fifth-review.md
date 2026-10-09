---
id: 0057
title: The judge of the write set and the contract gate close the holes of the fifth review
status: Done
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
   and `Directory.SetCurrentDirectory` in a changed file under `Engine.Tests/` (T31). The owner chose
   this option, option A of question Q2 of the review, on 2026-10-10.
4. The job fails when the base holds no judge, or when the previous tip of `main` is not known (T28).
5. On a branch, the range starts at the merge base with `main` (T32). The judge finds a task by its
   identifier at the commit (T33).
6. The contract gate checks each commit (T26).
7. The documented check stops at the first failure (T34). A failed git command is a failure, the
   judge reads the paths of git with `-z`, and `Engine.Tests/Governance/WriteSetRangeTests.cs` tests
   the steps of `eng/write-set-check/Program.cs` on a scratch repository (T35).

## Scope (out)

- No new rule of the write set. The holes close; the rules stay.
- No branch protection. That is a setting of the owner. On 2026-10-10 the owner answered yes to
  question Q1 of the review: the owner protects `main` on GitHub against a force push.

## Acceptance criteria

- [x] Each attack of the review fails the judge: a program named `git` in the root, an orphan commit
      with a forbidden change, a merge of unrelated history, a merge that takes the tree of an old
      commit, and the redirect of T31. A run on a branch that is never merged shows it. The tests of
      `WriteSetRangeTests` run the real judge on a scratch repository in each run of the pipeline,
      on three operating systems, in place of one run on such a branch; see "Method".
- [x] The contract gate fails a commit that changes `Engine.Contracts/**` with no ADR when another
      commit of the same push changes an ADR.
- [x] The 103 commits after the cut-off, and each commit after them on `main`, still pass: 112
      commits, of which 23 merges with no change of their own.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The
      merge records the run.

## Notes for the implementer

- **The first run.** This task changes the judge, and the pipeline builds the judge from `main`, so
  the branch of this task is judged by the old judge. The proof on a never-merged branch follows the
  merge, as for TASK-0053.
- **A scratch repository.** A test can make one with `git init` in a temporary folder, so it needs no
  history of this repository; the gate jobs clone one commit only.

## Outcome

Status: Done · v0.54 · the commit that carries this block.

The judge runs git by a full path from a folder of `PATH` outside the judged checkout, with `-C`
(T29). A commit with no parent fails, a merge with more than two parents fails, and a merge is judged
on each file that differs from its first parent and that no commit of its second parent changed; a
merge of unrelated history has no merge base and fails (T30). A changed test file that holds one of
four calls fails (T31). The job builds the judge from `main` or fails, and on `main` it fails when the
tip before the push is not known (T28). A branch is judged from its merge base with `main` (T32). The
task files come from the commit (T33). The contract rule is a rule of the judge, for each commit, and
the job "Contract-touched-needs-ADR" is gone (T26). The documented check joins its two commands with
`&&` (T34). Git gives its paths with `-z`, a failed git command is a failure, and the tests cover the
steps of the program (T35).

## Method

**Mechanical.** The tests first. `WriteSetRangeTests` runs the real program on a scratch repository
with git. With a stand-in `FindGit` that searched the checkout before `PATH`, as the old start of git
did, 8 of its 10 tests failed: each attack passed the old judge. The sanity test passed, and the test
of a program named `git` returns at once on Windows, where a program that exits with 0 needs a build;
`The_Judge_Takes_Git_From_Outside_The_Judged_Checkout` covers Windows. After the change each test
passes, and the replay of the history passes.

**Judgement.** The contract rule moved into the judge and not into a loop in the workflow, because the
judge comes from `main` and a step of the workflow comes from the judged commit. A merge is judged
against its first parent minus the files that the side of its second parent changed: on `main` that
side is the branch, whose commits the same run judges. The texts of the four calls are in two parts in
the source of the judge and of its tests, so that the judge does not refuse its own tests.

The first version of the test of an old tree reverted only files that its task permits, and the new
judge passed it; that is correct under the rules. The test now reverts a file that the task forbids,
as the run of the review did. That version did not run on the old judge.

**Weakest.** The proof on a branch that is never merged, which TASK-0053 gave, is replaced by tests
that run the real judge in each run of the pipeline; the steps of the workflow itself have no such
test. The workflow still comes from the judged commit (T28, the part that a branch protection on
GitHub closes). A pull request judges only the commits after the merge base, and the push of the
branch judges each one. A judge of `main` from before this task answers the new arguments with exit
code 2, and the step then runs the old form with a notice, so that this branch and its merge can be
judged; after the merge the notice cannot appear. `docs/adr/0014-manifold-native-interop.md:94` still
names the job `contract-gate`, in a sentence about its own task; ADRs are outside this write set.

## Progress

- 2026-10-10: the owner answered the two questions of the review of 2026-10-09: yes to Q1 (a
  branch protection of `main` against a force push, which the owner sets on GitHub) and option A to
  Q2 (the judge refuses four calls under `Engine.Tests/`). The task stays `Ready`.
- 2026-10-10: closed. 312 tests pass, and the replay of the 112 commits after the cut-off passes.
