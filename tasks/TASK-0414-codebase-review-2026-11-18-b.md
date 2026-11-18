---
id: 0414
title: The second codebase review of 2026-11-18 is a record
status: Done
phase: governance
opened: 2026-11-18
depends-on: [0413]
governed-by: []
writes:
  create:
    - tasks/TASK-0414-codebase-review-2026-11-18-b.md
    - docs/reviews/2026-11-18-b-codebase-review.md
    - tasks/TASK-0415-the-high-finding-of-the-review-of-2026-11-18-b.md
    - tasks/TASK-0416-the-findings-of-the-review-of-2026-11-18-b.md
  modify:
    - tasks/TASK-0410-the-findings-of-the-review-of-2026-11-18.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.App/**
---

# TASK-0414 — The second codebase review of 2026-11-18 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Four product milestones (R30c, R30h, R30d and the Section action of R30h) follow v0.391, the version that the first review of
2026-11-18 examined, so the next product milestone needs a review first (`docs/templates.md`, section 7). On 2026-11-18 the
owner answered "Start the review" after the merge of v0.396.

## Goal

A review of the code at `8b2619a` (v0.396) is in `docs/reviews/`, and a task holds each new finding in the order of section 7.

## Scope (in)

1. `docs/reviews/2026-11-18-b-codebase-review.md`, in the form of `docs/templates.md`, section 7.
2. TASK-0415 (P0.166) for the high finding, before the next product phase, R30e, and TASK-0416 (P0.167) for the medium and
   low findings, after it.
3. The roadmap rows of P0.166 and P0.167 and their place in the order.
4. TASK-0410: a note that R30c (TASK-0408) fixed V195 and V199 and a part of V196.

## Scope (out)

- A correction of a finding. TASK-0410, TASK-0415 and TASK-0416 hold them; E223 waits for the answer Q1.

## Acceptance criteria

- [x] Each finding of the review has its place and its evidence, and the session read it at its line.
- [x] The measures use the method of the last review, and the script gives the values of the last review at `1bdd376`.
- [x] `CodebaseReviewGateTests`, `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on
      the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch review-2026-11-18-b) · v0.397

## Method

Mechanical: the measures, from one script that gives the values of the last review at its commit.
Judgement:  the severity of each finding; E223 as a question to the owner, because ADR-0169 item 3 does not say what a slanted
            edge of a cone gives.
Weakest:    E224, V208 and V209 come from a reading of the lines; no run on the phone shows V209.

## Progress

- 2026-11-18: opened under the answer of the owner "Start the review", branch review-2026-11-18-b from `main` at v0.396 (run
  44374038396 on `main` passed). Two agents examine the change from `1bdd376` to `8b2619a` in their own copies with probes.
- 2026-11-18: the session read each finding of the agents at its line, and removed one that did not hold (a command of the
  queue after a change of project: each session has its own engine and its own queue). Ledger entry v0.397.
- 2026-11-18: run 44381592014 passed on the branch on each job. Closed at v0.397; the merge and the answer Q1 wait for the owner.
