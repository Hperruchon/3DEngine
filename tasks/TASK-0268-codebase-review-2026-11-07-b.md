---
id: 0268
title: The codebase review of 2026-11-07 (b) is a record
status: Done
phase: governance
opened: 2026-11-07
depends-on: [0267]
governed-by: []
writes:
  create:
    - tasks/TASK-0268-codebase-review-2026-11-07-b.md
    - docs/reviews/2026-11-07-b-codebase-review.md
    - tasks/TASK-0269-the-phone-wait-the-stl-name-and-the-test-sheet.md
    - tasks/TASK-0270-the-import-and-the-home-screen-of-the-desktop.md
    - tasks/TASK-0271-desktop-forms-of-the-twenty-ninth-review.md
    - tasks/TASK-0272-phone-engine-and-gate-forms-of-the-twenty-ninth-review.md
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

# TASK-0268 — The codebase review of 2026-11-07 (b) is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The review of 2026-11-07 examined v0.243. After it came 4 product milestones (P9c, P9d, P9e and R16d) and 9 ledger
entries, and the next product milestone would be the fifth, so the review is due before it.

## Goal

The repository holds the twenty-ninth codebase review, with the state of each earlier finding, and a task for each new
finding.

## Scope (in)

1. `docs/reviews/2026-11-07-b-codebase-review.md`, in the form of `docs/templates.md`, section 7, at `d233679`.
2. Five findings are high: TASK-0269 (P0.109) and TASK-0270 (P0.110) hold them, before the next product phase. TASK-0271
   (P0.111) and TASK-0272 (P0.112) hold the medium and low findings, after it.
3. The roadmap rows and one ledger entry, which corrects the count of the entry v0.244 (T172).

## Scope (out)

- A change of code. A fix of a finding.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with the new review.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch codebase-review-2026-11-07-b) · v0.253

## Method

Mechanical: the measures with the methods of the last review, and the line of each earlier finding from the review of
            2026-11-07.
Judgement:  three agents examined three areas at one tree, with the ten leads of Codex of v0.244 to v0.249; the session
            checked the high findings in the code. The standing order answers the two questions (E173 A, E174 A).
Weakest:    E172 is confirmed by reading only: the order of the events of SDL at a new create is inferred, because the
            source of SDL is not in the repository. The lines of the findings up to T164 are carried, not examined.

## Progress

- 2026-11-07: the review, from the sections of three agents at the tree of v0.252, and the four tasks of its findings.
- 2026-11-07: run 42362739671 passed. Closed at v0.253.
