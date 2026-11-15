---
id: 0392
title: The low findings of the codebase review of 2026-11-15
status: Ready
phase: P0.159
opened: 2026-11-15
depends-on: [0391]
governed-by: []
writes:
  create:
    - tasks/TASK-0392-the-low-findings-of-the-review-of-2026-11-15.md
  modify:
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
---

# TASK-0392 — The low findings of the codebase review of 2026-11-15

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-15 (`docs/reviews/2026-11-15-codebase-review.md`) gives the low findings E207, E208, E209,
E210, V192 and V193. Section 7 of `docs/templates.md` puts them after the next product phase, P10.2.

## Goal

The fixed lengths do not depend on the scale, the ends of an edge come from all its segments, and the small faults of form go.

## Scope (in)

1. E207: the rows of an angle scaled by a length, or a register entry for the limit.
2. E208: the history of ADR-0142.
3. E209 and E210: TASK-0391 removed `ProductWith` and the pattern `is var`, so they are fixed there.
4. V192: one helper of the ends of an edge.
5. V193: the state of each modifier key.
6. A test for E207, V192 and V193, which fails before the fix.

## Scope (out)

- E206 (TASK-0391).

## Acceptance criteria

- [ ] A test proves the fixed lengths of two rectangles of 1e5 mm and 0.001 mm in one sketch.
- [ ] A test proves the ends of an edge in two parts.
- [ ] A test proves that the flag of the modifier stays while one of two keys is down.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: E208 to E210.
Judgement:  the scale of a row of an angle.
Weakest:    to be written at the close.

## Progress

- 2026-11-15: opened by the codebase review of 2026-11-15 (TASK-0390).
- 2026-11-15: TASK-0391 fixed E209 and E210 with its change of the same lines.
