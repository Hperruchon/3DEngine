---
id: 0230
title: The codebase review of 2026-11-06 (c) is a record
status: Done
phase: governance
opened: 2026-11-06
depends-on: [0229]
governed-by: []
writes:
  create:
    - tasks/TASK-0230-codebase-review-2026-11-06-c.md
    - docs/reviews/2026-11-06-c-codebase-review.md
    - tasks/TASK-0231-gate-and-engine-forms-of-the-twenty-fifth-review.md
    - tasks/TASK-0232-desktop-and-phone-forms-of-the-twenty-fifth-review.md
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

# TASK-0230 — The codebase review of 2026-11-06 (c) is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The review of 2026-11-06 (b) examined v0.207. After it came 4 product milestones (R14e2, R14e3, R14f1 and R14f2) and 7
ledger entries, and R14f3 would be the fifth, so the review is due before R14f3. The gate does not count these
milestones (T147 of this review), so the count is by hand.

## Goal

The repository holds the twenty-fifth codebase review, with the state of each earlier finding, and a task for each new
finding.

## Scope (in)

1. `docs/reviews/2026-11-06-c-codebase-review.md`, in the form of `docs/templates.md`, section 7, at `df11d6e`.
2. No new finding is critical or high. TASK-0231 (P0.91) holds E156, E157 and T147 to T151, and TASK-0232 (P0.92) holds
   V97 to V103, after the next product phase, R14f3.
3. The roadmap rows and one ledger entry. The standing order of 2026-11-05 answers Q1 with the recommended option.

## Scope (out)

- A change of code. A fix of a finding.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with the new review.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch codebase-review-2026-11-06-c) · v0.215

## Method

Mechanical: the measures with the methods of the last review, and the line of each earlier finding from the files that
            it cites and the commits since `41db916`.
Judgement:  the severity of each new finding; V97 is medium, as V91 of the same class, and as the lead of Codex.
Weakest:    the two review agents ran their probes on Windows x64 only; the crash of the console colour on Android came
            after the commit of the review, from the job of the emulator, and no agent found it.

## Progress

- 2026-11-06: two review agents at `df11d6e`; 3 new medium and 11 new low findings; the review written.
- 2026-11-06: the lead of the review of Codex of v0.212 (finding 1, the popup of the list of a cut) goes into TASK-0232.
- 2026-11-06: closed at v0.215.
