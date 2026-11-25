---
id: 0467
title: The codebase review of 2026-11-25 is a record
status: Active
phase: governance
opened: 2026-11-25
depends-on: [0466]
governed-by: []
writes:
  create:
    - tasks/TASK-0467-codebase-review-2026-11-25.md
    - docs/reviews/2026-11-25-codebase-review.md
    - tasks/TASK-0468-the-findings-of-the-review-of-2026-11-25.md
  modify:
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.App/**
---

# TASK-0467 — The codebase review of 2026-11-25 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Four product milestones (R25, P16, R32a and K10b) and six entries follow v0.444, the review of 2026-11-23, which examined v0.443.
So the next product milestone needs a review first (`docs/templates.md`, section 7). The owner ordered "Start the review" in the
chat on 2026-11-25.

## Goal

A review of the code at `1975089e` (v0.450) is in `docs/reviews/`, and a task holds each new finding in the order of section 7.

## Scope (in)

1. `docs/reviews/2026-11-25-codebase-review.md`, in the form of `docs/templates.md`, section 7.
2. TASK-0468 (P0.189) for the two medium and eleven low findings, after the next product phase. No finding is critical or high,
   so no task comes before it.
3. The roadmap row of P0.189, its place in the order, and the line of this review in the list "Shipped".

## Scope (out)

- A correction of a finding: TASK-0468 holds them.
- The answers to Q1 to Q4: the first commit of the next task of each records them (section 7).
- A register entry: no finding needs one, because TASK-0468 holds each finding. T197 gives the cause of R-0078, and TASK-0468
  does its exit. The register holds 13 open entries of 15.
- An ADR: the next free number stays ADR-0189.

## Acceptance criteria

- [x] Each finding of the review has its place and its evidence, and the session read each finding of the agents in the code.
- [x] The measures use the method of the last review.
- [ ] `CodebaseReviewGateTests`, `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on
      the branch and on `main` after the merge.

## Outcome

Status: Active · commit (the merge of the branch codebase-review-2026-11-25) · v0.451

## Method

Mechanical: the measures, from a new script `measures.js` that gives each value of the last review at `a708a91a`; the list of
            the previous findings, copied by a script with new lines for E19, E21, E24 and V225, and a line for each finding of
            the last review and for V235.
Judgement:  the severity of each finding. Two agents examined the change from `70fb0264` to `1975089e` in the worktree of the
            review and changed no file; the session read each finding at its lines.
Weakest:    the cause of R-0078 (T197) comes from the code; the test passed 25 times of 25 alone on one computer, so no run
            shows the race. The path of E242 in the window is read, not run.

## Progress

- 2026-11-25: opened after K10b (TASK-0466), branch codebase-review-2026-11-25 from `1975089e`. Ledger entry v0.451.
- 2026-11-25: run 45706338430 of `26d32264` failed on Windows in
  `EventsHandshakeTests.No_Heartbeat_Comes_Before_The_Subscription_Message` (a guard of 10 s that TASK-0435 kept). The review adds it as T201.
- 2026-11-25: run 45707816188 of `d9f34c4f` failed on Ubuntu in the step "Keep the determinism hash" ("Failed to
  FinalizeArtifact ... ECONNRESET"); no test failed and no re-run started. The review adds it as T202.
