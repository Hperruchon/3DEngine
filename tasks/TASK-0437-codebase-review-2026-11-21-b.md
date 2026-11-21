---
id: 0437
title: The second codebase review of 2026-11-21 is a record
status: Active
phase: governance
opened: 2026-11-21
depends-on: [0436]
governed-by: []
writes:
  create:
    - tasks/TASK-0437-codebase-review-2026-11-21-b.md
    - docs/reviews/2026-11-21-b-codebase-review.md
    - tasks/TASK-0438-the-findings-of-the-review-of-2026-11-21-b.md
  modify:
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.App/**
---

# TASK-0437 — The second codebase review of 2026-11-21 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Four product milestones (K15a, K16, K15b and K11) follow v0.413, the version that the review of 2026-11-21 examined, so the
next product milestone needs a review first (`docs/templates.md`, section 7). The order of the owner of 2026-11-20 runs the
work until 2026-11-22 09:00, and takes the recommended option of each question.

## Goal

A review of the code at `1e4e345` (v0.420) is in `docs/reviews/`, and a task holds each new finding in the order of section 7.

## Scope (in)

1. `docs/reviews/2026-11-21-b-codebase-review.md`, in the form of `docs/templates.md`, section 7.
2. TASK-0438 (P0.174) for the three medium and four low findings, after the next product phase. No finding is critical or
   high, so no task comes before it.
3. The roadmap row of P0.174 and its place in the order.

## Scope (out)

- A correction of a finding: TASK-0438 holds them.
- The answers to Q1 to Q7: the first commit of the next task of each records them (section 7): P17 for Q1 to Q4, and P0.174
  for Q5 to Q7. Under the order of 2026-11-20 each answer is the recommended option A.

## Acceptance criteria

- [x] Each finding of the review has its place and its evidence, and the session read the headings, the measures, the summary
      and the questions.
- [x] The measures use the method of the last review.
- [ ] `CodebaseReviewGateTests`, `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on
      the branch and on `main` after the merge.

## Outcome

Status: Active · commit (the merge of the branch codebase-review-2026-11-21-b) · v0.421

## Method

Mechanical: the measures, from the script of the last review (`measures.js`).
Judgement:  the severity of each finding. One agent examined the change from `92d1c54` to `1e4e345` in a detached copy of
            the commit, and checked each correction of P0.172 in the code.
Weakest:    E233 gives estimates of time, not runs; E234 is inferred from the code of the check of a mesh.

## Progress

- 2026-11-21: opened after K11 (TASK-0436), branch codebase-review-2026-11-21-b from `1e4e345`. The session read the summary,
  the measures, the heading of each finding and the questions. Ledger entry v0.421.
