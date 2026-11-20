---
id: 0425
title: The codebase review of 2026-11-20 is a record
status: Active
phase: governance
opened: 2026-11-20
depends-on: [0423]
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

Four product milestones (R31a, R31b, R30e and R20d) follow v0.396, the version that the second review of 2026-11-18 examined,
so the next product milestone, R30f, needs a review first (`docs/templates.md`, section 7). The order of the owner of
2026-11-20 runs the work until 2026-11-22 09:00, and takes the recommended option of each question.

## Goal

A review of the code at `b3b9b6e` (v0.407) is in `docs/reviews/`, and a task holds each new finding in the order of section 7.

## Scope (in)

1. `docs/reviews/2026-11-20-codebase-review.md`, in the form of `docs/templates.md`, section 7.
2. TASK-0426 (P0.171) for the four medium and eleven low findings, after the next product phase, R30f. No finding is critical
   or high, so no task comes before R30f.
3. The roadmap row of P0.171 and its place in the order.

## Scope (out)

- A correction of a finding: TASK-0426 holds them.
- The answers to Q1 to Q6: the first commit of the next task records them (section 7). Under the order of 2026-11-20 each
  answer is the recommended option A.

## Acceptance criteria

- [x] Each finding of the review has its place and its evidence, and the session read the headings and the summary.
- [x] The measures use the method of the last review, and the script gives the values of the last review at `8b2619a`.
- [ ] `CodebaseReviewGateTests`, `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on
      the branch and on `main` after the merge.

## Outcome

Status: Active · commit (the merge of the branch codebase-review-2026-11-20) · v0.408

## Method

Mechanical: the measures, from one script (`measures.js`) that gives the values of the last review at its commit.
Judgement:  the severity of each finding. One agent examined the change from `8b2619a` to `b3b9b6e` in a detached copy of
            the commit, with probes in its scratch folder.
Weakest:    V211 (the order of the windows of the file menu and the structure view) is inferred from the code, not run.

## Progress

- 2026-11-20: opened after R20d (TASK-0423), branch codebase-review-2026-11-20 from `b3b9b6e`. One agent wrote the review;
  the session read its summary, its measures and the heading of each finding. Ledger entry v0.408.
