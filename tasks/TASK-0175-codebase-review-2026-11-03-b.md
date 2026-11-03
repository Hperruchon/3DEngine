---
id: 0175
title: The second codebase review of 2026-11-03 is a record
status: Done
phase: governance
opened: 2026-11-03
depends-on: [0174]
governed-by: []
writes:
  create:
    - tasks/TASK-0175-codebase-review-2026-11-03-b.md
    - docs/reviews/2026-11-03-b-codebase-review.md
    - tasks/TASK-0176-the-planes-of-a-later-reference.md
    - tasks/TASK-0177-desktop-and-gate-forms-of-the-twentieth-review.md
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

# TASK-0175 — The second codebase review of 2026-11-03 is a record

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The review of 2026-11-03 examined v0.168. After it came 4 product milestones (K0h, K0i, P8d1 and P8d2) and 8 ledger
entries, so the next product milestone would make the review late. On 2026-11-03 the owner told the session: "merge
when green, then start the codebase review", and later: "merge, fix E128 and E129, then R13".

## Goal

The repository holds the twentieth codebase review, with the state of each earlier finding, and a task for each new
finding.

## Scope (in)

1. `docs/reviews/2026-11-03-b-codebase-review.md`, in the form of `docs/templates.md`, section 7, at the merge of
   v0.176.
2. No new finding is critical or high. TASK-0176 (P0.75) holds E128 and E129 (medium) before R13, by the order of the
   owner. TASK-0177 (P0.76) holds V76 and V77 (medium) and E130 and T130 to T132 (low), after R13.
3. The roadmap rows and one ledger entry. The review asks no question.

## Scope (out)

- A change of code. A fix of a finding. The row and the ADR of R13, which its first task adds.

## Acceptance criteria

- [x] `CodebaseReviewGateTests` passes with the new review.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch codebase-review-2026-11-03-b) · v0.177

## Method

Mechanical: the measures with the methods of the last review, each checked at `1c3fb7c` against the values of that
            review, and the line of each earlier finding from the files that it cites and the commits since `1c3fb7c`.
Judgement:  the severity of each new finding, and which findings of the last review are fixed for their forms.
Weakest:    the two review agents ran their probes on Windows x64 only, and no one opened the desktop window for V76 and
            V77.

## Progress

- 2026-11-03: two review agents at `db28439`; 4 new medium and 4 new low findings; the review written.
- 2026-11-03: closed at v0.177.
