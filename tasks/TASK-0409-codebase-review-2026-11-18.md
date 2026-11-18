---
id: 0409
title: The codebase review of 2026-11-18 is a record
status: Active
phase: governance
opened: 2026-11-18
depends-on: [0407]
governed-by: []
writes:
  create:
    - tasks/TASK-0409-codebase-review-2026-11-18.md
    - docs/reviews/2026-11-18-codebase-review.md
    - tasks/TASK-0410-the-findings-of-the-review-of-2026-11-18.md
  modify:
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.App/**
---

# TASK-0409 — The codebase review of 2026-11-18 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Four product milestones (P10c, R30a, R30g and R30b) follow v0.383, the version that the review of 2026-11-17 examined, so the
next product milestone, R30c, needs a review first (`docs/templates.md`, section 7). On 2026-11-18 the owner ordered "review
after R30b", then "start the review".

## Goal

A review of the code at `1bdd376` (v0.391) is in `docs/reviews/`, and a task holds each new finding in the order of section 7.

## Scope (in)

1. `docs/reviews/2026-11-18-codebase-review.md`, in the form of `docs/templates.md`, section 7.
2. TASK-0410 (P0.165) for the medium and low findings, after R30c.
3. The roadmap row of P0.165 and its place in the order.

## Scope (out)

- A correction of a finding. TASK-0410 holds them; E220 and V200 wait for the answers Q1 and Q2.

## Acceptance criteria

- [x] Each finding of the review has its place and its evidence, and the session read it at its line.
- [x] The measures use the method of the last review, and the script gives the values of the last review at `059ec7d`.
- [ ] `CodebaseReviewGateTests`, `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on
      the branch and on `main` after the merge.

## Outcome

## Method

Mechanical: the measures, from one script that gives the values of the last review at its commit.
Judgement:  the severity of each finding; E220 and V200 as questions to the owner, because a decision and the code disagree.
Weakest:    V196 to V198 and V201 come from a reading of the lines; no agent timed V196 with a heavy body.

## Progress

- 2026-11-18: opened under the order of the owner "start the review", branch review-2026-11-18 from `main` at v0.391 (run
  44307338251 on `main` passed). Two agents examined the change from `059ec7d` to `1bdd376` in their own copies with probes,
  and removed them. The session read each finding at its line.
