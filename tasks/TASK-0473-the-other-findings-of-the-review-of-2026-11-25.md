---
id: 0473
title: The other findings of the codebase review of 2026-11-25
status: Active
phase: P0.191
opened: 2026-11-27
depends-on: [0468]
governed-by: [0025, 0185, 0187, 0188]
writes:
  create:
    - tasks/TASK-0473-the-other-findings-of-the-review-of-2026-11-25.md
  modify:
    - Engine.Tests/Http/ProposalAcceptTests.cs
    - Engine.Tests/Http/ProposalPreviewTests.cs
    - 3DEngine.ImageTests/ProposalPreviewWindowTests.cs
    - Engine.Tests/Governance/PathPatternWorkTests.cs
    - Engine.Tests/Governance/TaskGovernanceGateTests.cs
  forbid:
    - Engine.Contracts/**
---

# TASK-0473 — The other findings of the codebase review of 2026-11-25

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-25 (`docs/reviews/2026-11-25-codebase-review.md`, TASK-0467) found two medium and eleven low
findings. P0.190 (TASK-0472) did V238, and P0.189 (TASK-0468) did T197, T201, T202 and E244 under the standing order of
2026-11-26. This task holds each other finding.

## Goal

Each finding of this task has a correction and a test, or a register entry with its reason.

## Scope (in)

1. Medium: E242 (after a failed accept, one `Redo` applies the part of the proposal that ran). The decided correction (Q2 A of the
   owner, 2026-11-26): the reversal of a failed group puts nothing on the redo list, and the log marks it, so that each walker
   of the log agrees. The mark waits for the answer of the owner to R-0081, because the smallest mark gives the field `GroupId`
   of `Engine.Contracts` a new meaning. An ADR comes in the same commit as the mark. Add the test of the probe of the review.
2. Low: E243, E245, V236, V237, T198, T199 and T200, each with the correction that the review recommends.
3. ADR-0188 items 5, 7 and 9 and "Bad" say what the code does after the corrections (E242, E245, V237).

## Scope (out)

- T197, T201, T202 and E244: TASK-0468 did them. V238: TASK-0472 did it.
- The forbid is `Engine.Contracts/**` until the owner answers R-0081.
- The write set holds only this file now. The task adds the files of each correction, and the ADRs that govern them, when it
  starts (`docs/templates.md`, section 2).

## Acceptance criteria

- [ ] Each finding has a test that fails before its correction, or a register entry.
- [ ] Each injection of a correction makes a named test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Notes for the implementer

- Read the section "3 Review of the code" of the review for the place, the probe and the correction of each finding.
- E242 can start only after the answer of R-0081. E243, E245, V236, V237, T198, T199 and T200 need no answer.

## Outcome

Status: Active · no commit on `main` yet

## Method

Mechanical: the corrections that the review names.
Judgement:  each choice between the two corrections that the review gives.
Weakest:    E242 waits for the answer of R-0081, and E243, E245, V236, V237 and T200 are not started.

## Progress

- 2026-11-27: opened by TASK-0468 with the findings that it did not do.
- 2026-11-27: under the standing order of 2026-11-26, branch `p0191-findings` from `ea2f4dac` (v0.456). T199: the gate of
  governed-by gives the texts that it can ask for, from the objects that it reads one time, and the test asks that each asked
  text is one of them; an injection that gives a distinct text for each path makes
  `The_Gate_Of_Governed_By_Builds_At_Most_One_Expression_For_Each_Pattern` fail, and the comment says "the three newest tasks,
  of any status". T198: the test of a stale group releases the fork only when the accept waits for a preview, at the version 2,
  with no sleep; an `ApplyGroup` that ignores the version makes
  `A_Commit_Between_The_Comparison_And_The_Apply_Makes_The_Accept_Compare_Again` fail; the limits of 30 s of the new tests are
  60 s (ADR-0173 item 4). Not merged: the other findings are open.
