---
id: 0366
title: The codebase review of 2026-11-14 (c) is a record
status: Done
phase: governance
opened: 2026-11-14
depends-on: [0365]
governed-by: []
writes:
  create:
    - tasks/TASK-0366-codebase-review-2026-11-14-c.md
    - docs/reviews/2026-11-14-c-codebase-review.md
    - tasks/TASK-0367-medium-and-low-findings-of-the-review-of-2026-11-14-c.md
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

# TASK-0366 — The codebase review of 2026-11-14 (c) is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Four product milestones (R18l, P0.147, R18m and R18n) and four ledger entries follow the review of v0.346, so the next
product milestone needs a review first. On 2026-11-14 the owner ordered work alone until Sunday.

## Goal

The review of `main` at v0.350 is in `docs/reviews/`, and its findings have tasks in the order of section 7 of
`docs/templates.md`.

## Scope (in)

1. The review, with the state of each finding of the last review and the new findings V181 to V184.
2. TASK-0367 (P0.148) for V181 to V184, after the next product phase.

## Scope (out)

- A fix of a finding.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with the new review.
- [x] Each finding of the last review has a line, and each new finding has a task.
- [x] `eng/repo-gates` and the write-set check pass.

## Outcome

Status: Done · commit (the merge of the branch codebase-review-2026-11-14-c) · v0.351

## Method

Mechanical: the measures by the script of the last review, whose values at 94f5ea9 equal the last review; the lines of the
            last review.
Judgement:  the severity of V181 to V184.
Weakest:    V182 and V183 are traced by reading, not run.

## Progress

- 2026-11-14: written at `main` a9cf69e (v0.350). One agent examined the change from `d68637b` to `c41cc40`, the work commit of
  R18n, with probes in a copy of the commit; the session read each finding in the code at its line. Closed at v0.351.
