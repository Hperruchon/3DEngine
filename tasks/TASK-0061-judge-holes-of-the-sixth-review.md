---
id: 0061
title: The judge of the write set judges each change of a merge, and its tests hold each rule
status: Active
phase: P0.23
opened: 2026-10-11
depends-on: [0060]
governed-by: []
writes:
  create:
    - tasks/TASK-0061-judge-holes-of-the-sixth-review.md
  modify:
    - eng/write-set-check/Program.cs
    - eng/write-set-check/Judge.cs
    - Engine.Tests/Governance/WriteSetJudgeTests.cs
    - Engine.Tests/Governance/WriteSetRangeTests.cs
    - .github/workflows/ci.yml
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

# TASK-0061 — The judge of the write set judges each change of a merge, and its tests hold each rule

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-11 found that the judge of TASK-0057 lost one rule of the old judge
(T36, high): a merge is judged on its own change minus the files that the side of its second parent
changed, so a merge of `main` into a branch can rewrite any file that `main` changed. The old judge read
a merge with `git diff-tree --cc`, and it failed that merge. The same gap lets a merge skip the four
texts and the contract rule. The review also found that the tests of the judge do not hold each rule
(T37), a dead fallback in the workflow (T39), and smaller gaps (T40). Each finding gives its evidence in
`docs/reviews/2026-10-11-codebase-review.md`.

## Goal

Each change that a merge makes itself is judged, and each rule of the judge fails a test when it is
removed.

## Scope (in)

1. A merge is judged on the union of two sets: each file that differs from each parent (`--cc`), and
   each file that differs from its first parent and that no commit of its second parent changed (T36).
2. Tests of a scratch repository for each form of T36, for a merge of unrelated history that names its
   task, and for `--staged` (T37).
3. The fallback for exit code 2 leaves the workflow (T39).
4. `FindGit` resolves links before it compares a folder with the checkout, and a failed `git show`
   that a rule needs is a failure (T40).
5. `docs/templates.md` describes the rule of a merge.

## Scope (out)

- No move of the repository gates out of the test assembly. Question Q1 of the review asks the owner;
  a separate task holds the answer.

## Acceptance criteria

- [x] The two runs of T36 in the review fail the judge, and each passed it before the correction.
- [x] With the refusal of a merge of unrelated history removed, a test fails.
- [x] The commits after the cut-off still pass: 128 commits, of which 30 merges with no change of
      their own.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The
      merge records the run.

## Outcome

Status: Done · v0.61 · the commit that carries this block.

A merge is judged on the union of two sets: each file that differs from each parent (`--cc`), and
each file that differs from its first parent and that no commit of its second parent changed. Each set
alone misses the case of the other one: the first is the edit inside a merge (T36), the second is a
merge that takes the tree of an old commit (T30). `FindGit` resolves each link and junction on the path
of a program before it compares the path with the checkout, and a failed read of a file that a rule
needs is a failure (T40). The fallback for exit code 2 left the workflow (T39). Four new tests of a
scratch repository and two new unit tests hold the rules (T37).

## Method

**Mechanical.** The tests first. The two forms of T36 failed on the judge of TASK-0057: a merge of
`main` into a branch that rewrote a forbidden file of `main`, and a merge that changed again a test
file and a contract file of its second parent. The new test of a merge of unrelated history, which
names its task and changes its task file, failed when the refusal of a merge with no merge base was
removed, and the older test passed with the same injection. The new test of a junction failed when
`FindGit` compared the path with no resolution of links. The replay of the history passes.

**Judgement.** The union and not `--cc` alone, because `--cc` alone lost the case of T30. A merge with
an edit of its own now needs a governing task, as a commit does: its message names the task, or the
merge changes one task file.

**Weakest.** On Windows the test of a link makes a junction, and on Linux and macOS a symbolic link;
this computer refuses a symbolic link, so only the pipeline runs the second form. No test runs the
steps of the workflow itself.

## Progress

- 2026-10-11: the task is open, in the order of the owner of 2026-10-11.
- 2026-10-11: closed. The judge reads each change of a merge, and each rule fails a test when it is
  removed.
- 2026-10-11: reopened. Run 38167055691 failed on macOS in the test of a link: RealPath did not resolve
  the target of a link again, and the folder of Path.GetTempPath of macOS is under /var, a link to
  /private/var. RealPath now resolves each target again from its root.
- 2026-10-11: closed again.
- 2026-10-11: reopened. Run 38167418801 failed in the marker gate on the three runners: the comment of
  RealPath held a marker word with no register identifier. I ran only the tests of the judge before
  that commit, and not the full suite.
