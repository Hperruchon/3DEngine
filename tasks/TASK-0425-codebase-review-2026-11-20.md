---
id: 0425
title: The codebase review of 2026-11-20 is a record
status: Active
phase: governance
opened: 2026-11-20
depends-on: [0427]
governed-by: []
writes:
  create:
    - tasks/TASK-0425-codebase-review-2026-11-20.md
    - docs/reviews/2026-11-20-codebase-review.md
    - tasks/TASK-0426-the-findings-of-the-review-of-2026-11-20.md
  modify:
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.App/**
---

# TASK-0425 — The codebase review of 2026-11-20 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Four product milestones (R31a, R31b, R30e and R30f) follow v0.396, the version that the second review of 2026-11-18 examined,
so the next product milestone, R20d, needs a review first (`docs/templates.md`, section 7). The order of the owner of
2026-11-20 runs the work until 2026-11-22 09:00, and takes the recommended option of each question.

## Goal

A review of the code at `c45a825` (v0.407) is in `docs/reviews/`, and a task holds each new finding in the order of section 7.

## Scope (in)

1. `docs/reviews/2026-11-20-codebase-review.md`, in the form of `docs/templates.md`, section 7.
2. TASK-0426 (P0.171) for the six medium and ten low findings, after the next product phase, R20d. No finding is critical or
   high, so no task comes before R20d.
3. The roadmap row of P0.171 and its place in the order.

## Scope (out)

- A correction of a finding: TASK-0426 holds them. R20d (TASK-0423) gives the ground and the cover of the two texts of the
  history arc that section 4 of the review names.
- The answers to Q1 to Q8: the first commit of the next task of each records them (section 7): P0.171 for Q1, Q2, Q3 and Q8,
  and R30i for Q4 to Q7. Under the order of 2026-11-20 each answer is the recommended option A.

## Acceptance criteria

- [x] Each finding of the review has its place and its evidence, and the session read the headings, the summary and the
      questions.
- [x] The measures use the method of the last review, and the script gives the values of the last review at `8b2619a`.
- [ ] `CodebaseReviewGateTests`, `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on
      the branch and on `main` after the merge.

## Outcome

Status: Active · commit (the merge of the branch codebase-review-2026-11-20-b) · v0.408

## Method

Mechanical: the measures, from one script (`measures.js`) that gives the values of the last review at its commit.
Judgement:  the severity of each finding. One agent examined the change from `8b2619a` to `c45a825` in a detached copy of
            the commit, with probes in its scratch folder.
Weakest:    V211 and V221 (the order of windows and of the background list) are inferred from the code and the rules of
            Dear ImGui, not run.

## Progress

- 2026-11-20: the first form of the review examined `b3b9b6e`, the commit of R20d. A parallel session then merged R30f
  (TASK-0422, v0.406), which made the review due before R20d, so the agent wrote it again for `c45a825` (P0.170, v0.407),
  with R30f in its scope. The first branch, codebase-review-2026-11-20, is not merged.
- 2026-11-20: opened on the branch codebase-review-2026-11-20-b from the close of TASK-0427. The session read the summary, the
  measures, the heading of each finding and the questions. Ledger entry v0.408.
