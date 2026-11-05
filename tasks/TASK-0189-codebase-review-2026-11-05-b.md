---
id: 0189
title: The codebase review of 2026-11-05 (b) is a record
status: Active
phase: governance
opened: 2026-11-05
depends-on: [0188]
governed-by: []
writes:
  create:
    - tasks/TASK-0189-codebase-review-2026-11-05-b.md
    - docs/reviews/2026-11-05-b-codebase-review.md
    - tasks/TASK-0190-an-older-datum-on-a-gone-face-opens.md
    - tasks/TASK-0191-desktop-forms-of-the-twenty-second-review.md
    - tasks/TASK-0192-engine-and-gate-forms-of-the-twenty-second-review.md
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

# TASK-0189 — The codebase review of 2026-11-05 (b) is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The review of 2026-11-05 examined v0.181. After it came 4 product milestones (K4a, K4b, K4c and P8h) and 9 ledger
entries, so the gate does not make a review due yet. On 2026-11-05 the owner gave the order "fix R-0043 first, then the
review". P0.81 (TASK-0188) fixed R-0043 at v0.190.

## Goal

The repository holds the twenty-second codebase review, with the state of each earlier finding, and a task for each new
finding.

## Scope (in)

1. `docs/reviews/2026-11-05-b-codebase-review.md`, in the form of `docs/templates.md`, section 7, at the merge of v0.190.
2. E134 (high) goes into TASK-0190 (P0.82), before the next product phase. TASK-0191 (P0.83) holds V85 (medium), V86
   (medium) and V87 (low). TASK-0192 (P0.84) holds E135 and T139 (medium) and E136 to E141, T136 to T138 and T140
   (low). Both come after the next product phase.
3. The roadmap rows and one ledger entry. The standing order of 2026-11-05 answers Q1 to Q5 with the recommended option.

## Scope (out)

- A change of code. A fix of a finding.

## Acceptance criteria

- [ ] `CodebaseReviewGateTests` passes with the new review.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-11-05: two review agents at `d9292fa`; 1 new high, 4 new medium and 11 new low findings; the review written.
