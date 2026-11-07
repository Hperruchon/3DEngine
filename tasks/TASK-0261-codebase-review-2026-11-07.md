---
id: 0261
title: The codebase review of 2026-11-07 is a record
status: Done
phase: governance
opened: 2026-11-07
depends-on: [0258]
governed-by: []
writes:
  create:
    - tasks/TASK-0261-codebase-review-2026-11-07.md
    - docs/reviews/2026-11-07-codebase-review.md
    - tasks/TASK-0262-the-check-of-an-open-reads-the-store.md
    - tasks/TASK-0263-engine-and-gate-forms-of-the-twenty-eighth-review.md
    - tasks/TASK-0264-desktop-forms-of-the-twenty-eighth-review.md
    - tasks/TASK-0265-a-lost-surface-of-a-destroyed-activity-ends-quietly.md
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

# TASK-0261 — The codebase review of 2026-11-07 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The review of 2026-11-06 (e) examined v0.233. After it came 4 product milestones (R16b2, R16c, P9a and P9b) and 10 ledger
entries, and the next product milestone, P9c, would be the fifth, so the review is due before it.

## Goal

The repository holds the twenty-eighth codebase review, with the state of each earlier finding, and a task for each new
finding.

## Scope (in)

1. `docs/reviews/2026-11-07-codebase-review.md`, in the form of `docs/templates.md`, section 7, at `86517c8`.
2. E167 and V124 are high: TASK-0262 (P0.105) and TASK-0265 (P0.108) hold them, before the close of P9c. TASK-0263 (P0.106) holds E168 to E171 and T165 to
   T167, and TASK-0264 (P0.107) holds V119 to V123, after P9c.
3. The roadmap rows and one ledger entry.

## Scope (out)

- A change of code. A fix of a finding.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with the new review.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch codebase-review-2026-11-07) · v0.244

## Method

Mechanical: the measures with the methods of the last review, and the line of each earlier finding from the files that
            it cites and the changes since `4667e4b`.
Judgement:  three agents examined three areas at one tree; the session checked each finding and set two grades: E167
            high (the agent gave medium) and T166 medium (the agent gave low).
Weakest:    the lines of the findings up to T159 are carried from the last review with no new examination; V124 comes
            from the annotations of one run, not from a device.

## Progress

- 2026-11-07: the review, from the sections of three agents at the tree of v0.243, and the three tasks of its findings.
- 2026-11-07: run 42324736514 passed. Closed at v0.244.
