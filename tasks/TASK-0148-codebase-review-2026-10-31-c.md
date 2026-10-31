---
id: 0148
title: The codebase review of 2026-10-31 (c) is a record
status: Done
phase: governance
opened: 2026-10-31
depends-on: [0146]
governed-by: []
writes:
  create:
    - tasks/TASK-0148-codebase-review-2026-10-31-c.md
    - docs/reviews/2026-10-31-c-codebase-review.md
    - tasks/TASK-0149-engine-forms-of-the-seventeenth-review.md
    - tasks/TASK-0150-gate-forms-of-the-seventeenth-review.md
    - tasks/TASK-0151-desktop-forms-of-the-seventeenth-review.md
  modify:
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - docs/register.md
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

# TASK-0148 — The codebase review of 2026-10-31 (c) is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The review of 2026-10-31 (b) examined v0.141. After it came 4 product milestones (R11b, V0, V1b and V1a) and 8 ledger
entries, so the next product milestone would make the review late. The owner told the session on 2026-10-31: "do the
review, then start V2", and told it to work alone until 2026-11-01 10:00 with its recommendation for each question.

## Goal

The repository holds the seventeenth codebase review, with the state of each earlier finding, and a task for each new
finding.

## Scope (in)

1. `docs/reviews/2026-10-31-c-codebase-review.md`, in the form of `docs/templates.md`, section 7, at the merge of
   v0.149.
2. TASK-0149 (P0.63), TASK-0150 (P0.64) and TASK-0151 (P0.65) hold the medium and low findings, after V2.
3. The roadmap rows and one ledger entry. The standing answer of the owner takes Q1 A, Q2 A and Q3 A; the task of V2
   records them in its ADR.

## Scope (out)

- A change of code. A fix of a finding.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with the new review.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch codebase-review-2026-10-31-c) · v0.150

## Method

Mechanical: the measures and the lines of the earlier findings come from scripts that read git, the ledger and the
            last review; the 23 probes of the agents ran again on this computer.
Judgement:  the severity of each new finding; E113 is medium because the command line throws with no exit code, and
            T117 is medium because it lets the vision library into the engine with no failure.
Weakest:    each probe ran on Windows only; E113 on Linux and the mark of V67 in a live window are not verified.

## Progress

- 2026-10-31: two review agents probed `b410a59`; I ran their 23 probes again and injected T117 with its control.
- 2026-10-31: closed at v0.150.
- 2026-10-31: run 41214590078 failed on Windows in `EventsEndpointLagTests`, which depends on the speed of the runner;
  register entry R-0047 records it.
