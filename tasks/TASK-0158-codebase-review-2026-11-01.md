---
id: 0158
title: The codebase review of 2026-11-01 is a record
status: Done
phase: governance
opened: 2026-11-01
depends-on: [0157]
governed-by: []
writes:
  create:
    - tasks/TASK-0158-codebase-review-2026-11-01.md
    - docs/reviews/2026-11-01-codebase-review.md
    - tasks/TASK-0159-png-reader-refuses-each-zlib-error.md
    - tasks/TASK-0160-engine-forms-of-the-eighteenth-review.md
    - tasks/TASK-0161-gate-forms-of-the-eighteenth-review.md
    - tasks/TASK-0162-desktop-forms-of-the-eighteenth-review.md
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

# TASK-0158 — The codebase review of 2026-11-01 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The review of 2026-10-31 (c) examined v0.149. After it came 4 product milestones (V2a, V2b, V3 and P8g) and 10 ledger
entries, so the next product milestone would make the review late. On 2026-11-01 the owner told the session: "do the
review".

## Goal

The repository holds the eighteenth codebase review, with the state of each earlier finding, and a task for each new
finding.

## Scope (in)

1. `docs/reviews/2026-11-01-codebase-review.md`, in the form of `docs/templates.md`, section 7, at the merge of
   v0.159.
2. TASK-0159 (P0.68) holds the high finding V68, before the next product phase. TASK-0160 (P0.69), TASK-0161 (P0.70)
   and TASK-0162 (P0.71) hold the medium and low findings, after it.
3. The roadmap rows and one ledger entry. The questions Q1 to Q3 wait for the owner.

## Scope (out)

- A change of code. A fix of a finding.
- A task for the next product phase: it waits for Q1.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with the new review.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch codebase-review-2026-11-01) · v0.160

## Method

Mechanical: the measures and the lines of the earlier findings come from scripts that read git, the ledger and the
            last review; the probes of the agents ran again on this computer, and T123 and T124 were injected with controls.
Judgement:  the severity of each new finding; V68 is high because a file from an agent or a saved document stops the
            desktop at each start; E119, V69 and V70 are medium because a person meets them in normal use.
Weakest:    each probe ran on Windows only; E120 on Linux and macOS, and V68 and V70 in a live window, are not verified.

## Progress

- 2026-11-01: two review agents probed `3fd8b9b`; I ran their probes again and injected T123 and T124 with their
  controls.
- 2026-11-01: closed at v0.160.
