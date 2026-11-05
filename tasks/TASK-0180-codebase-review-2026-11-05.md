---
id: 0180
title: The codebase review of 2026-11-05 is a record
status: Active
phase: governance
opened: 2026-11-05
depends-on: [0177]
governed-by: []
writes:
  create:
    - tasks/TASK-0180-codebase-review-2026-11-05.md
    - docs/reviews/2026-11-05-codebase-review.md
    - tasks/TASK-0181-desktop-forms-of-the-twenty-first-review.md
    - tasks/TASK-0182-engine-and-gate-forms-of-the-twenty-first-review.md
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

# TASK-0180 — The codebase review of 2026-11-05 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The review of 2026-11-03 (b) examined v0.176. After it came 1 product milestone (R13) and 5 ledger entries, so the gate
does not make a review due. On 2026-11-05 the owner gave the order "start the codebase review", and later "merge the
review when green".

## Goal

The repository holds the twenty-first codebase review, with the state of each earlier finding, and a task for each new
finding.

## Scope (in)

1. `docs/reviews/2026-11-05-codebase-review.md`, in the form of `docs/templates.md`, section 7, at the merge of v0.181.
2. No new finding is critical or high. TASK-0181 (P0.78) holds V78 (medium) and V79 to V84 (low), and TASK-0182 (P0.79)
   holds E131 to E133 and T133 to T135 (low), after the next product phase.
3. The roadmap rows and one ledger entry. Q1 and Q2 wait for the owner.

## Scope (out)

- A change of code. A fix of a finding. The amendment of ADR-0056 of Q2, which P0.78 makes.

## Acceptance criteria

- [ ] `CodebaseReviewGateTests` passes with the new review.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the measures with the methods of the last review, and the line of each earlier finding from the files that
            it cites and the commits since `db28439`.
Judgement:  the severity of each new finding, and which findings of the last review are fixed for their forms.
Weakest:    the two review agents ran their probes on Windows x64 only, and no one opened the desktop window.

## Progress

- 2026-11-05: two review agents at `3cd76af`; 1 new medium and 12 new low findings; the review written.
