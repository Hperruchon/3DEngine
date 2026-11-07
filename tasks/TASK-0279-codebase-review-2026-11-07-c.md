---
id: 0279
title: The codebase review of 2026-11-07 (c) is a record
status: Active
phase: governance
opened: 2026-11-07
depends-on: [0277]
governed-by: []
writes:
  create:
    - tasks/TASK-0279-codebase-review-2026-11-07-c.md
    - docs/reviews/2026-11-07-c-codebase-review.md
    - tasks/TASK-0280-the-reader-of-each-earlier-result-and-the-ring-of-a-sketch.md
    - tasks/TASK-0281-medium-forms-of-the-thirtieth-review.md
    - tasks/TASK-0282-low-forms-of-the-thirtieth-review.md
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

# TASK-0279 — The codebase review of 2026-11-07 (c) is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The review of 2026-11-07 (b) examined v0.252. After it came 4 product milestones (R16e, R16f, R16g and R16h) and 11 ledger
entries, and the next product milestone would be the fifth, so the review is due before it.

## Goal

The repository holds the thirtieth codebase review, with the state of each earlier finding, and a task for each new
finding.

## Scope (in)

1. `docs/reviews/2026-11-07-c-codebase-review.md`, in the form of `docs/templates.md`, section 7, at the merge of v0.263.
2. Three findings are high: TASK-0280 (P0.115) holds them, before the next product phase. TASK-0281 (P0.116) and
   TASK-0282 (P0.117) hold the medium and low findings, after it.
3. The roadmap rows and one ledger entry.

## Scope (out)

- A change of code. A fix of a finding.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with the new review.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the measures with the methods of the last review, and the line of each earlier finding from the review of
            2026-11-07 (b).
Judgement:  two agents examined two areas at one tree and proved their main findings with probes that they deleted; the
            session checked the high findings in the code. The standing order answers Q1 with A, which changes no code;
            Q2 changes the contract and waits for the owner.
Weakest:    E184 and V139 are confirmed by reading only, and no run on a phone examined V136. The lines of the findings up
            to T167 are carried, not examined.

## Progress

- 2026-11-07: the review, with 3 high, 6 medium and 4 low new findings, and the three tasks of the fixes.
