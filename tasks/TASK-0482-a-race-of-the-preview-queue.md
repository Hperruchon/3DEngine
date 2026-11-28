---
id: 0482
title: A race of the queue of the previews, a call between the copy of the log and its hand-over marks the queue behind
status: Active
phase: P0.196
opened: 2026-11-28
depends-on: [0481]
governed-by: [0019, 0185, 0187, 0188]
writes:
  create:
    - tasks/TASK-0482-a-race-of-the-preview-queue.md
  modify:
    - Engine.Api.Http/Proposals.Preview.cs
    - Engine.Tests/Http/ProposalPreviewTests.cs
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
---

# TASK-0482 — A race of the queue of the previews, a call between the copy of the log and its hand-over marks the queue behind

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Run 46103697245, the close run of TASK-0480 (bf46ccb8), failed on Windows:
`ProposalPreviewWindowTests.The_Host_Gives_The_Window_The_Preview_With_The_Shape_Of_Each_New_Solid` read `Current` false after
the wait for the preview of a new version. The close commit changed only its task file, and the run of the same code passed.
The worker of the queue clears the mark "behind" when it takes the copy of the log, and gives the copy to each proposal that
waits a moment later. A call of `Preview` in that window found no preview, no fork and no copy at its version, so it marked the
queue behind again. Its waiter then got the preview of its version, and `Kept` gave `Current` false until the worker read the
log again. TASK-0461 closed the same window for a fork that runs; this window stayed.

## Goal

A copy of the log that the worker took and did not yet give to the proposals covers each proposal that waits.

## Scope (in)

1. The queue keeps the version of the copy in flight, and a call of `Preview` does not mark the queue behind for a version that
   the copy covers.
2. A seam of the tests after the copy, and a test that calls `Preview` in the window.

## Scope (out)

- Each other rule of the queue.

## Acceptance criteria

- [x] The test fails when the call compares its version with the preview, the fork and the pending copy only.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Active · commit (the merge of the branch p0196-preview-race) · v0.465

## Method

Mechanical: the window that the failed run shows, and the form of the correction of TASK-0461.
Judgement:  the copy in flight covers a proposal that waits at the call, because the worker gives the copy to each such proposal
            at the hand-over; a seam after the copy holds the worker in the window, as `PumpGate` holds the pump of a subscriber.
Weakest:    the failed run did not keep the state of the queue, so the window is the cause that the code allows, not a cause
            that a log shows.

## Progress

- 2026-11-28: opened under the standing order of 2026-11-28, after the red close run of TASK-0480. `Proposals` keeps
  `_copying`, the version of the copy in flight, and the call of `Preview` compares its version with it too. The seam
  `AfterCopy` holds the worker after the copy; `A_Caller_That_Waits_While_The_Copy_Of_Its_Version_Is_Not_Yet_Given_Does_Not_Mark_The_Queue_Behind`
  fails with the old comparison ("the queue is behind"). Ledger v0.465.
