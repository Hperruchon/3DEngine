---
id: 0460
title: The codebase review of 2026-11-23 is a record
status: Active
phase: governance
opened: 2026-11-23
depends-on: [0459]
governed-by: []
writes:
  create:
    - tasks/TASK-0460-codebase-review-2026-11-23.md
    - docs/reviews/2026-11-23-codebase-review.md
    - tasks/TASK-0461-the-findings-of-the-review-of-2026-11-23.md
  modify:
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.App/**
---

# TASK-0460 — The codebase review of 2026-11-23 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Four product milestones (K15d, P15a, R18p and P15b) and twelve entries follow v0.431, the version that the third review of
2026-11-21 examined, so the next product milestone or the next entry needs a review first (`docs/templates.md`, section 7). The
owner ordered "Start the review" in the chat on 2026-11-23.

## Goal

A review of the code at `a708a91a` (v0.443) is in `docs/reviews/`, and a task holds each new finding in the order of section 7.

## Scope (in)

1. `docs/reviews/2026-11-23-codebase-review.md`, in the form of `docs/templates.md`, section 7.
2. TASK-0461 (P0.187) for the one medium and nine low findings, after the next product phase. No finding is critical or high, so
   no task comes before it.
3. The roadmap row of P0.187, its place in the order, and the line of this review in the list "Shipped".

## Scope (out)

- A correction of a finding: TASK-0461 holds them.
- The answers to Q1 to Q6: the first commit of the next task of each records them (section 7).
- A register entry: no finding needs one, because TASK-0461 holds each finding. Q6 asks the owner if the sum of the checks of a
  preview gets a bound or a register entry. The register holds 12 open entries of 15.

## Acceptance criteria

- [x] Each finding of the review has its place and its evidence, and the session read each finding of the agents in the code.
- [x] The measures use the method of the last review.
- [ ] `CodebaseReviewGateTests`, `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on
      the branch and on `main` after the merge.

## Outcome

Status: Active · commit (the merge of the branch codebase-review-2026-11-23) · v0.444

## Method

Mechanical: the measures, from the script of the last review (`measures.js`), which gives the values of the last review at
            `2aed6d7`; the list of the previous findings, copied by a script with new lines for T4, E21 and V15.
Judgement:  the severity of each finding. Two agents examined the change from `2aed6d7` to `a708a91a` in the worktree of the
            review and changed no file; the session read each finding at its lines.
Weakest:    the times of E239 and the status 500 of E240 come from the code and the ledger, not from a run.

## Progress

- 2026-11-23: opened after P15b (TASK-0459), branch codebase-review-2026-11-23 from `a708a91a`. Ledger entry v0.444.
