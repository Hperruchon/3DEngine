---
id: 0380
title: The medium and low findings of the codebase review of 2026-11-14 (e)
status: Ready
phase: P0.152
opened: 2026-11-14
depends-on: [0379]
governed-by: []
writes:
  create:
    - tasks/TASK-0380-medium-and-low-findings-of-the-review-of-2026-11-14-e.md
  modify:
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
---

# TASK-0380 — The medium and low findings of the codebase review of 2026-11-14 (e)

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-14 (e) (`docs/reviews/2026-11-14-e-codebase-review.md`) gives the medium findings E204 and T182
and the low finding E205. Section 7 of `docs/templates.md` puts them after the next product phase, R22.

## Goal

An edge of a cut side shows its own circle, the test of the circles reads its hidden lines, and a flat end draws one line.

## Scope (in)

1. E204: the circle or the ellipse at the place of the edge along the axis.
2. T182: the ends of the hidden lines in the test.
3. E205: an ellipse with a minor half axis near zero as an end across the view.
4. A test for each, which fails before the fix.

## Scope (out)

- E202, E203 and V191 (TASK-0379).

## Acceptance criteria

- [ ] A test proves the circle of radius 5 of a cone cut at its middle.
- [ ] The test of the circles of a Document asserts the ends of its hidden lines.
- [ ] A test proves one line for an end of a cylinder that two turns laid on its side.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: the assertions of the test.
Judgement:  the limit of a flat ellipse.
Weakest:    to be written at the close.

## Progress

- 2026-11-14: opened by the codebase review of 2026-11-14 (e) (TASK-0378).
