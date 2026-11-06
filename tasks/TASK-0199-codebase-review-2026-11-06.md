---
id: 0199
title: The codebase review of 2026-11-06 is a record
status: Done
phase: governance
opened: 2026-11-06
depends-on: [0200]
governed-by: []
writes:
  create:
    - tasks/TASK-0199-codebase-review-2026-11-06.md
    - docs/reviews/2026-11-06-codebase-review.md
    - tasks/TASK-0197-engine-and-export-forms-of-the-twenty-third-review.md
    - tasks/TASK-0198-desktop-and-gate-forms-of-the-twenty-third-review.md
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

# TASK-0199 — The codebase review of 2026-11-06 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The review of 2026-11-05 (b) examined v0.190. After it came 4 product milestones (R14a, R14b, R14c and K6a) and 8 ledger
entries, and K6b would be the fifth, so the gate makes the review due before K6b (order 2 of 2026-11-05, item 5).

## Goal

The repository holds the twenty-third codebase review, with the state of each earlier finding, and a task for each new
finding.

## Scope (in)

1. `docs/reviews/2026-11-06-codebase-review.md`, in the form of `docs/templates.md`, section 7, at the merge of v0.198.
2. No new finding is critical or high. TASK-0197 (P0.85) holds E142 to E151, and TASK-0198 (P0.86) holds V88 to V90 and
   T141 to T144, after the next product phase.
3. The roadmap rows and one ledger entry. The standing order of 2026-11-05 answers Q1 to Q4 with the recommended option.

## Scope (out)

- A change of code. A fix of a finding.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with the new review.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch codebase-review-2026-11-06) · v0.199

## Method

Mechanical: the measures with the methods of the last review, and the line of each earlier finding from the files that
            it cites and the commits since `d9292fa`.
Judgement:  the severity of each new finding; E142 stays medium, because a person meets it only on a face of two regions
            with a hole that touches its side at its first vertex.
Weakest:    the two review agents ran their probes on Windows x64 only, and no reader of STEP outside the repository read
            a file.

## Progress

- 2026-11-06: two review agents at `2c77991`, the tree of the merge of v0.198; 4 new medium and 13 new low findings; the
  review written.
- 2026-11-06: closed at v0.199.
