---
id: 0349
title: The codebase review of 2026-11-12 is a record
status: Done
phase: governance
opened: 2026-11-12
depends-on: [0345]
governed-by: []
writes:
  create:
    - tasks/TASK-0349-codebase-review-2026-11-12.md
    - docs/reviews/2026-11-12-codebase-review.md
    - tasks/TASK-0350-medium-and-low-findings-of-the-review-of-2026-11-12.md
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

# TASK-0349 — The codebase review of 2026-11-12 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Four product milestones (R18d, R18e, R18f and R20a) and five ledger entries follow the review of v0.327. R20b1 (TASK-0348) is
complete on its branch, and its close is the fifth product milestone, which `CodebaseReviewGateTests.A_Review_Is_Not_Overdue`
refuses. The owner asked for "Dimensions" on the ring on 2026-11-11, so this review comes first, as the review of v0.325 came
before R18d.

## Goal

The review of `main` at v0.332 is in `docs/reviews/`, and its findings have tasks in the order of section 7 of
`docs/templates.md`.

## Scope (in)

1. The review, with the state of each finding of the last review and the new findings V168 to V172.
2. TASK-0350 (P0.144) for V168 to V171, after the next product phase.
3. V172 goes to TASK-0348, which amends ADR-0119 in the same change.

## Scope (out)

- A fix of a finding.
- The close of R20b1.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with the new review.
- [x] Each finding of the last review has a line, and each new finding has a task.
- [x] `eng/repo-gates` and the write-set check pass.

## Outcome

Status: Done · commit (the merge of the branch codebase-review-2026-11-12) · v0.333

## Method

Mechanical: the measures by a script whose values at v0.325 equal the last review; the lines of the last review.
Judgement:  the severity and the state of V168 to V172.
Weakest:    V171 is proven on the function only; no real mesh gave its input yet. The second form of V168 is traced, not run.

## Progress

- 2026-11-12: written at `main` 65a26d7 (v0.332). One agent examined the change from `60f02a5` to `65a26d7` with probes in a
  copy of the commit; the session read each finding in the code and ran the four probes again. Closed at v0.333.
