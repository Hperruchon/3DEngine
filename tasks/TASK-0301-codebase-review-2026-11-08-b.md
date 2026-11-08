---
id: 0301
title: The codebase review of 2026-11-08 (b) is a record
status: Done
phase: governance
opened: 2026-11-08
depends-on: [0299]
governed-by: []
writes:
  create:
    - tasks/TASK-0301-codebase-review-2026-11-08-b.md
    - docs/reviews/2026-11-08-b-codebase-review.md
    - tasks/TASK-0302-medium-forms-of-the-thirty-second-review.md
    - tasks/TASK-0303-low-forms-of-the-thirty-second-review.md
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

# TASK-0301 — The codebase review of 2026-11-08 (b) is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The review of 2026-11-08 examined v0.275. After it came 4 product milestones and 10 ledger entries, so the next product
milestone would make the review late. No task was ready, and the night changed the exact kernel and the phone.

## Goal

The repository holds the thirty-second codebase review, with the state of each earlier finding, and a task for each new
finding.

## Scope (in)

1. `docs/reviews/2026-11-08-b-codebase-review.md`, in the form of `docs/templates.md`, section 7, at the merge of v0.285.
2. No finding is high. TASK-0302 (P0.127) holds the medium findings, and TASK-0303 (P0.128) the low ones.
3. The roadmap rows and one ledger entry.

## Scope (out)

- A change of code. A fix of a finding.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with the new review.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch codebase-review-2026-11-08-b) · v0.286

## Method

Mechanical: the measures with the methods of the last review, and the line of each earlier finding from the review of
            2026-11-08.
Judgement:  two agents examined two areas at one tree and proved their main findings with probes that they deleted; the
            session read E196 and the leads of Codex.
Weakest:    V143, V144, V145 and T179 are confirmed by reading only. The lines of the findings up to T174 are carried.

## Progress

- 2026-11-08: the review, with 4 medium and 7 low new findings, and the two tasks of the fixes.
- 2026-11-08: run 42450584188 passed. Closed at v0.286.
