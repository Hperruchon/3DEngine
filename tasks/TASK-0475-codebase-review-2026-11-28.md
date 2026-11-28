---
id: 0475
title: The codebase review of 2026-11-28 is a record
status: Active
phase: governance
opened: 2026-11-28
depends-on: [0474]
governed-by: []
writes:
  create:
    - tasks/TASK-0475-codebase-review-2026-11-28.md
    - docs/reviews/2026-11-28-codebase-review.md
    - tasks/TASK-0476-the-high-finding-of-the-review-of-2026-11-28.md
    - tasks/TASK-0477-the-findings-of-the-review-of-2026-11-28.md
  modify:
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.App/**
---

# TASK-0475 — The codebase review of 2026-11-28 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Four product milestones (R32b, R33, P21 and P11.0) and seven entries follow v0.451, the review of 2026-11-25, which examined
v0.450. So the next product milestone needs a review first (`docs/templates.md`, section 7). The standing order of the owner of
2026-11-28 ("same as every previous week") runs the queue of the roadmap until 09:00 on 2026-11-29, and the review is its next
step.

## Goal

A review of the code at the merge of P11.0 (v0.458) is in `docs/reviews/`, and a task holds each new finding in the order of
section 7.

## Scope (in)

1. `docs/reviews/2026-11-28-codebase-review.md`, in the form of `docs/templates.md`, section 7.
2. TASK-0476 (P0.192) for the high finding E246, before the next product phase.
3. TASK-0477 (P0.193) for the two medium and seven low findings, after the next product phase.
4. The roadmap rows of P0.192 and P0.193, their place in the order, and the line of this review in the list "Shipped".

## Scope (out)

- A correction of a finding: TASK-0476 and TASK-0477 hold them.
- The answers to Q1 and Q2: the first commit of TASK-0476 records them (section 7).
- A register entry: no finding needs one, because the two tasks hold each finding. The register holds 13 open entries of 15.
- An ADR: the next free number stays ADR-0192.

## Acceptance criteria

- [x] Each finding of the review has its place and its evidence, and the session read each finding of the agents in the code.
- [x] The measures use the method of the last review.
- [ ] `CodebaseReviewGateTests`, `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on
      the branch and on `main` after the merge.

## Outcome

Status: Active · commit (the merge of the branch codebase-review-2026-11-28) · v0.459

## Method

Mechanical: the measures, from the script `measures.js` of the last review; the list of the previous findings, copied by a script
            with new lines for E6, E19, E21, E24, E57 and V15, and a line for each finding of the last review.
Judgement:  the severity of each finding. Two agents examined the change from `7efe89ce` to `c885ea90` and changed no file; the
            session read each finding at its lines, and four probes ran as tests in a scratch worktree.
Weakest:    E250 has no probe that ran, because it needs a journal that fails at one write and a handler that throws. The effect
            of V239 on the screen is read, not seen: computer use on the window is denied.

## Progress

- 2026-11-28: opened under the standing order of 2026-11-28, after the merge of P11.0 (v0.458). The review, TASK-0476 and
  TASK-0477, the rows P0.192 and P0.193, and ledger entry v0.459.
