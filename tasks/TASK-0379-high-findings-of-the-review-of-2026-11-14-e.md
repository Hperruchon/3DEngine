---
id: 0379
title: The high findings of the codebase review of 2026-11-14 (e)
status: Ready
phase: P0.151
opened: 2026-11-14
depends-on: [0378]
governed-by: []
writes:
  create:
    - tasks/TASK-0379-high-findings-of-the-review-of-2026-11-14-e.md
  modify:
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
---

# TASK-0379 — The high findings of the codebase review of 2026-11-14 (e)

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-14 (e) (`docs/reviews/2026-11-14-e-codebase-review.md`) gives the high findings E202, E203 and
V191. Section 7 of `docs/templates.md` puts them before the next product phase, R22.

## Goal

The outline of a cut side ends at its body, a turned cylinder gives a view, and Frame leaves the window of the live sketches.

## Scope (in)

1. E202: the place of each end of an outline by its exact place along the axis.
2. E203: the end of a piece by its exact place along the axis, and no line of no length or with a value that is not finite.
3. V191: the free part leaves a tall window at the right edge.
4. A test for each, which fails before the fix.

## Scope (out)

- E204, T182 and E205 (TASK-0380).

## Acceptance criteria

- [ ] A test proves the outline of a hole through a plate from face to face.
- [ ] A test proves the view of a cylinder that a Rotate of 180 degrees turned, at the origin and moved.
- [ ] A test proves that the free part leaves the window of the live sketches.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: the cut of the free part.
Judgement:  the exact place along the axis.
Weakest:    to be written at the close.

## Progress

- 2026-11-14: opened by the codebase review of 2026-11-14 (e) (TASK-0378).
