---
id: 0355
title: The codebase review of 2026-11-14 is a record
status: Done
phase: governance
opened: 2026-11-14
depends-on: [0354]
governed-by: []
writes:
  create:
    - tasks/TASK-0355-codebase-review-2026-11-14.md
    - docs/reviews/2026-11-14-codebase-review.md
    - tasks/TASK-0356-medium-and-low-findings-of-the-review-of-2026-11-14.md
  modify:
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - Engine.Tests/**
    - 3DEngine/**
    - 3DEngine.App/**
    - 3DEngine.Android/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Tests/**
    - 3DEngine.ImageTests/**
    - 3DEngine.VisionTests/**
    - eng/**
    - CLAUDE.md
    - docs/CHARTER.md
    - docs/templates.md
    - docs/adr/**
---

# TASK-0355 — The codebase review of 2026-11-14 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Four product milestones (R20b1, R20b2, R20c and R18g) and six ledger entries follow the review of v0.333, so the next product
milestone needs a review first (`CodebaseReviewGateTests.A_Review_Is_Not_Overdue`). On 2026-11-14 the owner ordered work
alone until Sunday, with the rules of the order of 2026-11-05.

## Goal

The review of `main` at v0.339 is in `docs/reviews/`, and its findings have tasks in the order of section 7 of
`docs/templates.md`.

## Scope (in)

1. The review, with the state of each finding of the last review and the new findings V173 to V177.
2. TASK-0356 (P0.146) for V173 to V177, after the next product phase.

## Scope (out)

- A fix of a finding.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with the new review.
- [x] Each finding of the last review has a line, and each new finding has a task.
- [x] `eng/repo-gates` and the write-set check pass.

## Outcome

Status: Done · commit (the merge of the branch codebase-review-2026-11-14) · v0.340

## Method

Mechanical: the measures by a script whose values at v0.332 equal the last review; the lines of the last review.
Judgement:  the severity and the state of V173 to V177, and two corrections of the report of the agent.
Weakest:    V175 is proven on the function only; no real mesh gave its input yet. V177 is traced, not run.

## Progress

- 2026-11-14: written at `main` af78473 (v0.339). One agent examined the change from `65a26d7` to `af78473` with probes in a
  copy of the commit; the session ran each probe again and read each finding in the code. Two corrections of the report: the
  stuck plate of V176 is in a consequence of ADR-0125, so the finding is the place on the other plates at the top edge; and
  an accept holds each value edit, so the form of V177 "an edit clears the texts of an accept" does not occur. Closed at
  v0.340.
