---
id: 0026
title: Correct the forbid rule and run the write-set gate on each commit
status: Done
phase: P0.12
opened: 2026-09-22
depends-on: [0025]
governed-by: []
writes:
  create:
    - eng/write-set-cutoff.txt
    - tasks/TASK-0026-per-commit-write-set-gate.md
  modify:
    - .github/workflows/ci.yml
    - Engine.Tests/Governance/WriteSetGateTests.cs
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - BlazorApp/**
---

# TASK-0026 — Correct the forbid rule and run the write-set gate on each commit

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Two jobs in `.github/workflows/ci.yml` reported `skipped` on each run: the contract gate and the
write-set gate. Each one had the condition `github.event_name == 'pull_request'`, and v0.23 made a
push the normal trigger. The write-set gate of v0.22 therefore never ran in the pipeline. It ran on a
local computer only, by hand, one change at a time.

A measurement of what a pull request would report found a defect in the gate itself.

TASK-0022 wrote this rule: a forbid beats a permit, and it beats a permit from another task. The task
called the strict reading the safe one. That rule is wrong. A forbid binds the task that declares it.
`Engine.Cli/**` in the forbid list of the persistence task says that the persistence work must stay
out of the command line. It does not say that nobody may touch the command line.

The rule only looked correct because each change that tested it carried one task.

The measurement:

- The whole-branch difference against the main branch reported 19 files, and each report was false.
  `Engine.Core/**` in the forbid list of TASK-0014, which pinned the SDK, blocked each file that
  TASK-0016 created.
- One commit failed for the same reason. TASK-0018 modified `Engine.Cli/Cli.cs` and declared it, and
  the forbid list of the deferred TASK-0019 blocked that declaration.

## Goal

The gate runs in the pipeline, on each push, and each report that it gives is true.

## Scope (in)

1. A permit beats a forbid. A forbid blocks a file only when no task in the change permits it.
2. The gate reads one commit at a time, and not a range.
3. The job runs on a push and on a pull request.
4. Each commit that exists today is grandfathered, with the cut-off recorded in a file.

## Scope (out)

- Do not remove the forbid list. It still blocks a file that no task permits, and it gives the better
  message when it does.
- Do not change the contract gate. It compares two trees, it needs a base reference, and a pull
  request is the correct condition for it.
- Do not change a task file to make a commit pass. The cut-off is the mechanism for history.

## Acceptance criteria

- [x] A permit from one task beats a forbid from another task.
- [x] A forbid still fails a file that no task permits, and the message names the task.
- [x] The gate reads one commit at a time.
- [x] The job runs on a push.
- [x] Each commit that exists today is exempt, and the exemption names one commit.
- [x] Each commit after the cut-off passes.

## Outcome

Shipped as v0.27.

**The forbid rule is corrected.** A permit beats a forbid. A forbid blocks a file only when no task
in the change permits it, and it then gives the better message, because it names the boundary that an
author wrote. The measured result on this branch: 12 of 13 commits pass, from 11 of 13 before.

This makes the forbid list a statement of intent that produces a clearer failure, and not an extra
power to block. That is the honest description. Under the new rule a file that every task forbids and
no task permits fails either way, once as a forbid and once as a file in no list. The value of the
list is the message and the record of what an author decided to stay away from, and
`No_Task_Forbids_A_Path_That_It_Also_Writes` keeps the two lists coherent.

**The gate reads one commit at a time.** A task governs the commit that carries it. A difference
across many tasks cannot say which task made which change, therefore it can only compare against the
union of each write set, and the union is not the rule. The job builds the tests one time, then runs
the gate for each commit in the push.

**The job runs on a push.** It needs no base reference now, because it reads one commit. The contract
gate keeps its condition, because it compares two trees and a pull request is the correct condition
for that.

**History is grandfathered by one named commit.** `eng/write-set-cutoff.txt` holds
`0609f13070d6917ee80f3aa14ecb553972b5efcf`, which was the tip when this task ran. The gate skips each
commit behind it. One commit needs this: `ecb1f9a`, "gitignore: ignore graphify-out", which opened the
branch and touches no task file. The exemption names one commit, it covers the history behind that
commit, and it covers nothing after it.

**Each case was verified by injection.** A file that one task forbids and another permits passes. A
file that a task forbids and no task permits fails and names the task. A file in no list fails. A
change with no task file fails.

## Method

**Mechanical.** The order of the two checks. The loop over the commits. The cut-off file.

**Judgement.** The measurement came first and the decision came second. A simulation of the pull
request took one command and it turned a guess into a count: 19 false reports on the branch, and the
exact commit that failed. Without it the choice between "run it on a pull request" and "run it on a
push" would have hidden a defect that made both options wrong.

**Weakest.** The gate cannot attribute a file to a task inside one commit. It compares against the
union of each task in that commit, therefore a commit that carries two tasks can let one task write a
file that belongs to the other. The correct fix is one task in one commit, which this repository
already does, and not more code in the gate.

A second weak point: the cut-off is a value in a file, and a person can move it. Register rule 6
already forbids that action in words. Nothing enforces it, and the file says so.

The lesson repeats the one from v0.24. TASK-0022 wrote a rule, argued for it in prose, and shipped it
with tests that each carried one task. The rule was wrong for every change with two. A test that only
ever sees the easy case gives the same false confidence as a statement that nobody checked.
