---
id: 0337
title: The codebase review of 2026-11-10 is a record
status: Done
phase: governance
opened: 2026-11-10
depends-on: [0335]
governed-by: []
writes:
  create:
    - tasks/TASK-0337-codebase-review-2026-11-10.md
    - docs/reviews/2026-11-10-codebase-review.md
    - tasks/TASK-0338-the-high-finding-of-the-review-of-2026-11-10.md
    - tasks/TASK-0339-medium-and-low-findings-of-the-review-of-2026-11-10.md
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

# TASK-0337 — The codebase review of 2026-11-10 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Four product milestones (R16t, R17a, R19 and K8) and eight ledger entries follow the review of v0.310. R18a (TASK-0332) and
R18b (TASK-0333) are complete on their branches, and the close of either one is the fifth product milestone, which the gate
of the review refuses. On 2026-11-10 the owner chose that this session writes the review now, before the two closes.

## Goal

The review of `main` at v0.318 is in `docs/reviews/`, and its findings have tasks in the order of section 7 of
`docs/templates.md`.

## Scope (in)

1. The review, with the state of each finding of the last review and the new findings E200, V164, E201, V165 and T181.
2. TASK-0338 (P0.140) for the high finding E200, before the next product phase.
3. TASK-0339 (P0.141) for the medium and low findings, after the next product phase.

## Scope (out)

- A fix of a finding.
- The closes of R18a and R18b.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with the new review.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch codebase-review-2026-11-10) · v0.319

## Method

Mechanical: the measures with the methods of the last review; the lines of the findings of the last review.
Judgement:  the severity of each new finding.
Weakest:    one agent examined the change; the session checked each main finding in the code.

## Progress

- 2026-11-10: opened at main 09f1827 (v0.318), branch codebase-review-2026-11-10, after the choice of the owner.
- 2026-11-10: the review is written. The agent found E200 after a Rotate; the session ran its probe again and found the
  same closed skin after a Translate by a decimal along the normal, so E200 is older than K8. TASK-0338 and TASK-0339 hold
  the findings.
