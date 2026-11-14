---
id: 0361
title: The codebase review of 2026-11-14 (b) is a record
status: Done
phase: governance
opened: 2026-11-14
depends-on: [0360]
governed-by: []
writes:
  create:
    - tasks/TASK-0361-codebase-review-2026-11-14-b.md
    - docs/reviews/2026-11-14-b-codebase-review.md
    - tasks/TASK-0362-medium-and-low-findings-of-the-review-of-2026-11-14-b.md
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

# TASK-0361 — The codebase review of 2026-11-14 (b) is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Four product milestones (R18h, R18i, R18j and R18k) and five ledger entries follow the review of v0.340, so the next product
milestone needs a review first. On 2026-11-14 the owner ordered work alone until Sunday.

## Goal

The review of `main` at v0.345 is in `docs/reviews/`, and its findings have tasks in the order of section 7 of
`docs/templates.md`.

## Scope (in)

1. The review, with the state of each finding of the last review and the new findings V178 to V180.
2. TASK-0362 (P0.147) for V178 to V180, after the next product phase.

## Scope (out)

- A fix of a finding.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with the new review.
- [x] Each finding of the last review has a line, and each new finding has a task.
- [x] `eng/repo-gates` and the write-set check pass.

## Outcome

Status: Done · commit (the merge of the branch codebase-review-2026-11-14-b) · v0.346

## Method

Mechanical: the measures by a script whose values at v0.339 equal the last review; the lines of the last review.
Judgement:  the severity of V178 to V180, and the rejection of the second form of V177.
Weakest:    V178 and V179 are traced by reading, not run.

## Progress

- 2026-11-14: written at `main` 94f5ea9 (v0.345). One agent examined the change from `af78473` to `125acb8`, the work commit of
  R18k, with probes in a copy of the commit; the session ran the probe of the cut row again and read each other finding.
  Its first finding, the cut row on a phone, went to TASK-0360 before the merge of R18k. Closed at v0.346.
