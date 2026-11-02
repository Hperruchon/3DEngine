---
id: 0168
title: The codebase review of 2026-11-03 is a record
status: Active
phase: governance
opened: 2026-11-03
depends-on: [0166]
governed-by: []
writes:
  create:
    - tasks/TASK-0168-codebase-review-2026-11-03.md
    - docs/reviews/2026-11-03-codebase-review.md
    - tasks/TASK-0169-engine-forms-of-the-nineteenth-review.md
    - tasks/TASK-0170-desktop-and-gate-forms-of-the-nineteenth-review.md
  modify:
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - Engine.Tests/**
    - 3DEngine/**
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

# TASK-0168 — The codebase review of 2026-11-03 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The review of 2026-11-01 examined v0.159. After it came 4 product milestones (K0f, K1, R11c and K0g) and 9 ledger
entries, so the next product milestone, K0h, would make the review late. On 2026-11-03 the owner told the session:
"do the review, then close K0h".

## Goal

The repository holds the nineteenth codebase review, with the state of each earlier finding, and a task for each new
finding.

## Scope (in)

1. `docs/reviews/2026-11-03-codebase-review.md`, in the form of `docs/templates.md`, section 7, at the merge of
   v0.168.
2. No new finding is critical or high. TASK-0169 (P0.72) and TASK-0170 (P0.73) hold the eight low findings, after K0h.
3. The roadmap rows and one ledger entry. The questions Q1 to Q3 wait for the owner.

## Scope (out)

- A change of code. A fix of a finding. K0h, which closes after this task.

## Acceptance criteria

- [ ] `CodebaseReviewGateTests` passes with the new review.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the measures with the methods of the last review, and the line of each earlier finding from the files that
            it cites and the commits since `3fd8b9b`.
Judgement:  the severity of each new finding, and which findings of the last review are fixed for their forms.
Weakest:    the two review agents ran their probes on Windows x64 only; the claim of E125 about arm64 is inferred.

## Progress

- 2026-11-03: two review agents at `1c3fb7c`; 8 new findings, all low; the review written.
