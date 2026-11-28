---
id: 0487
title: The second codebase review of 2026-11-28 is a record
status: Active
phase: governance
opened: 2026-11-28
depends-on: [0479]
governed-by: []
writes:
  create:
    - tasks/TASK-0487-codebase-review-2026-11-28-b.md
    - docs/reviews/2026-11-28-b-codebase-review.md
    - tasks/TASK-0488-the-findings-of-the-review-of-2026-11-28-b.md
  modify:
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.App/**
---

# TASK-0487 — The second codebase review of 2026-11-28 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Twelve entries follow v0.458, the version that the review of 2026-11-28 examined, with two product milestones (K25 and P11a). So
the next entry of any kind needs a review first (`docs/templates.md`, section 7). The standing order of the owner of 2026-11-28
runs the queue of the roadmap until 09:00 on 2026-11-29, and the review is its next step.

## Goal

A review of the code at the merge of P11a (v0.470) is in `docs/reviews/`, and a task holds each new finding in the order of
section 7.

## Scope (in)

1. `docs/reviews/2026-11-28-b-codebase-review.md`, in the form of `docs/templates.md`, section 7.
2. TASK-0488 (P0.201) for the two medium and five low findings, after the next product phase.
3. The roadmap row of P0.201, its place in the order, and the line of this review in the list "Shipped".

## Scope (out)

- A correction of a finding: TASK-0488 holds them.
- The answer to Q1: the first commit of the next task records it (section 7).
- A register entry: no finding needs one, because TASK-0488 holds each finding. The register holds 11 open entries of 15.
- An ADR: the next free number stays ADR-0194.

## Acceptance criteria

- [x] Each finding of the review has its place and its evidence, and the session read each finding of the agents in the code.
- [x] The measures use the method of the last review.
- [ ] `CodebaseReviewGateTests`, `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on
      the branch and on `main` after the merge.

## Outcome

Status: Active · commit (the merge of the branch review-2026-11-28-b) · v0.471

## Method

Mechanical: the measures, from the script `measures.js` of the last review; the list of the previous findings, copied by a script
            with new lines for E6, E13, E17, E19, E21, E24, E57, V15, E160, E243 and V236, and a line for each finding of the
            last review.
Judgement:  the severity of each finding. Two agents examined the range `1a112239..5cfc55a5` and changed no file; the session
            read each finding at its lines. The probe of E251 ran in a console project of an agent, and the probe of E252 in a
            scratch program of an agent, both outside the repository.
Weakest:    E255 has no probe that ran, because it needs a journal or a handler that throws a cancellation. The probes of E251 and
            E252 ran outside the repository, so P0.201 must add each as a test.

## Progress

- 2026-11-28: opened under the standing order of 2026-11-28, after the merge of P11a (v0.470). The review, TASK-0488, the row
  P0.201, and ledger entry v0.471.
