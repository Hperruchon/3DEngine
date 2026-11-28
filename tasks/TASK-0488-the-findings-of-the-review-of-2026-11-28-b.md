---
id: 0488
title: The medium and low findings of the second codebase review of 2026-11-28
status: Ready
phase: P0.201
opened: 2026-11-28
depends-on: [0487]
governed-by: []
writes:
  create:
    - tasks/TASK-0488-the-findings-of-the-review-of-2026-11-28-b.md
  modify:
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
---

# TASK-0488 — The medium and low findings of the second codebase review of 2026-11-28

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The second codebase review of 2026-11-28 (`docs/reviews/2026-11-28-b-codebase-review.md`) found two medium and five low findings.
By the order of the review they come after the next product phase, P11b.

## Goal

Each finding of the list below is corrected with a test, or recorded with its reason.

## Scope (in)

1. E251: key the table of the placements of a step by the place of the entry in the log, and keep the placements of each step
   beside the step in the feature list. The probe of the review as a test, with an open.
2. E252: the check of the charset in the form that the read takes; `utf-7` and a quoted charset in `RequestTextTests`.
3. E253: the order of two directions of a pivot with exact numbers.
4. E254: the answer of the owner to Q1 of the review.
5. E255: the exception of the handler and the two keys when the reversal of a group throws a cancellation.
6. T206: the tests of the joints compare the faces after an open, an `Undo` and a `Redo`, and test a boolean of a child, a
   cylinder connector and a datum with a fault.
7. T207: the hosts of the tests set no time limit of the subscribe frame.

The write set grows when the task starts.

## Scope (out)

- Each other open finding.

## Acceptance criteria

- [ ] Each finding of the scope has a test that fails with the old code, or a reason in the ledger.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Ready · no commit yet

## Method

Mechanical: the probes of the review.
Judgement:  the correction of each finding.
Weakest:    not started.

## Progress

- 2026-11-28: opened by TASK-0487, the second codebase review of 2026-11-28.
