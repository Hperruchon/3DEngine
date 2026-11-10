---
id: 0344
title: The codebase review of 2026-11-11 is a record
status: Done
phase: governance
opened: 2026-11-11
depends-on: [0340]
governed-by: []
writes:
  create:
    - tasks/TASK-0344-codebase-review-2026-11-11.md
    - docs/reviews/2026-11-11-codebase-review.md
    - tasks/TASK-0345-the-low-findings-of-the-review-of-2026-11-11.md
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

# TASK-0344 — The codebase review of 2026-11-11 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Four product milestones (R18a, R18b, R17b and R18c) and seven ledger entries follow the review of v0.318. R18d (TASK-0342) is
complete on its branch with a green run, and its close is the fifth product milestone, which
`CodebaseReviewGateTests.A_Review_Is_Not_Overdue` refuses. The owner ordered R18d after R18c on 2026-11-10, so this review
comes first, as the review of v0.318 came before R18a.

## Goal

The review of `main` at v0.325 is in `docs/reviews/`, and its findings have tasks in the order of section 7 of
`docs/templates.md`.

## Scope (in)

1. The review, with the state of each finding of the last review and the new findings V166 and V167.
2. TASK-0345 (P0.143) for the low findings, after the next product phase.

## Scope (out)

- A fix of a finding.
- The close of R18d.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with the new review.
- [x] Each finding of the last review has a line, and each new finding has a task.
- [x] `eng/repo-gates` and the write-set check pass.

## Outcome

Status: Done · commit (the merge of the branch codebase-review-2026-11-10-b) · v0.327

## Method

Mechanical: the measures by a script whose values at v0.318 equal the last review; the lines of the last review.
Judgement:  the severity and the state of V166 and V167.
Weakest:    V166 and V167 are proven on the function only; no real mesh gave either input yet.

## Progress

- 2026-11-11: written at `main` 60f02a5 (v0.325). One agent examined the change from `09f1827` to `60f02a5` with probes in a
  scratch folder; the session read each finding in the code and corrected three statements of the draft (the file of a test,
  the new register entries and the range of the ADRs). Closed at v0.326.
- 2026-11-11: the drawing record (TASK-0343) merged first at v0.326, so the merge of `main` renumbers this review to
  v0.327; the ledger and the roadmap keep both sides. This commit sets the task Active, and the next one Done again.
- 2026-11-11: Done again at v0.327.
