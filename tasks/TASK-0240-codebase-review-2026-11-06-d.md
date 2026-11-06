---
id: 0240
title: The codebase review of 2026-11-06 (d) is a record
status: Done
phase: governance
opened: 2026-11-06
depends-on: [0238]
governed-by: []
writes:
  create:
    - tasks/TASK-0240-codebase-review-2026-11-06-d.md
    - docs/reviews/2026-11-06-d-codebase-review.md
    - tasks/TASK-0241-a-save-to-a-new-name-keeps-its-handle.md
    - tasks/TASK-0242-engine-and-pipeline-forms-of-the-twenty-sixth-review.md
    - tasks/TASK-0243-desktop-and-phone-forms-of-the-twenty-sixth-review.md
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

# TASK-0240 — The codebase review of 2026-11-06 (d) is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The review of 2026-11-06 (c) examined v0.214. After it came 4 product milestones (R14f3, R15, R12a and K5a) and 9 ledger
entries, and the next product milestone would be the fifth, so the review is due before it.

## Goal

The repository holds the twenty-sixth codebase review, with the state of each earlier finding, and a task for each new
finding.

## Scope (in)

1. `docs/reviews/2026-11-06-d-codebase-review.md`, in the form of `docs/templates.md`, section 7, at `3379675`.
2. E162 is high: TASK-0241 (P0.96) holds it, before the next product phase. TASK-0242 (P0.97) holds E158 to E161, E163,
   T152, T153 and T155 to T159, and TASK-0243 (P0.98) holds V104 to V111, after the next product phase.
3. The roadmap rows and one ledger entry. The standing order of 2026-11-05 answers Q1 with the recommended option.

## Scope (out)

- A change of code. A fix of a finding.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with the new review.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch codebase-review-2026-11-06-d) · v0.225

## Method

Mechanical: the measures with the methods of the last review, and the line of each earlier finding from the files that
            it cites and the changes since `df11d6e`.
Judgement:  the severity of each new finding; E162 stays high, as the audit of Codex gave it, because its result is a
            document that does not open.
Weakest:    E162 and E163 are confirmed by reading only, and no probe ran the race of E162; V105 is an inference from
            the strings of SDL, and no log of a job shows the new start of the activity.

## Progress

- 2026-11-06: two review agents at `3379675`; the leads of Codex of v0.214 to v0.222 and two audits; 1 new high, 6 new
  medium and 15 new low findings; the review written.
- 2026-11-06: the message of the desktop agent named eight findings, and its report held seven; the agent said that
  it had written the title of V110 with no examination. It then examined V110 and wrote it, and I read its cited lines.
- 2026-11-06: closed at v0.225.
