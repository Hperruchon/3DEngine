---
id: 0371
title: The codebase review of 2026-11-14 (d) is a record
status: Done
phase: governance
opened: 2026-11-14
depends-on: [0370]
governed-by: []
writes:
  create:
    - tasks/TASK-0371-codebase-review-2026-11-14-d.md
    - docs/reviews/2026-11-14-d-codebase-review.md
    - tasks/TASK-0373-high-findings-of-the-review-of-2026-11-14-d.md
    - tasks/TASK-0374-medium-and-low-findings-of-the-review-of-2026-11-14-d.md
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

# TASK-0371 — The codebase review of 2026-11-14 (d) is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Four product milestones (R18o, P0.148, R21a and R21b) and four ledger entries follow the review of v0.351, so the next product
milestone needs a review first. On 2026-11-14 the owner ordered work alone until Sunday.

## Goal

The review of v0.355 is in `docs/reviews/`, and its findings have tasks in the order of section 7 of `docs/templates.md`.

## Scope (in)

1. The review, with the state of each finding of the last review and the new findings V185 to V190.
2. TASK-0373 (P0.149) for V185 to V187 before the next product phase, and TASK-0374 (P0.150) for V188 to V190 after it.

## Scope (out)

- A fix of a finding.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with the new review.
- [x] Each finding of the last review has a line, and each new finding has a task.
- [x] `eng/repo-gates` and the write-set check pass.

## Outcome

Status: Done · commit (the merge of the branch codebase-review-2026-11-14-d) · v0.356

## Method

Mechanical: the measures by the script of the last review; the lines of the last review.
Judgement:  the severity of V185 to V190.
Weakest:    V189 is calculated, not seen on a GPU; V190 is traced by reading.

## Progress

- 2026-11-14: written at `89e8666`, the head of R21b at v0.355, while the main run of v0.354 waits for the owner. One agent
  examined the change from `7be5370` to `ce4217b` with probes in a copy of the commit; the session read each finding at its line.
  Its two findings in R21b went to TASK-0370 before the merge. Closed at v0.356.
