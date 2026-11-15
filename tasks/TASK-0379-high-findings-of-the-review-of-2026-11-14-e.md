---
id: 0379
title: The high findings of the codebase review of 2026-11-14 (e)
status: Done
phase: P0.151
opened: 2026-11-14
depends-on: [0378]
governed-by: [0004, 0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0138, 0140, 0142, 0143, 0144, 0145]
writes:
  create:
    - tasks/TASK-0379-high-findings-of-the-review-of-2026-11-14-e.md
    - docs/adr/0145-the-place-of-a-line-along-the-axis.md
    - Engine.Tests/Drawing/ViewPlaceTests.cs
  modify:
    - Engine.Core/Drawing/ViewEdges.cs
    - 3DEngine.App/Ui/DesktopUi.Views.cs
    - 3DEngine.ImageTests/FrameFreePartWindowTests.cs
    - docs/adr/0140-frame-in-the-free-part-of-the-scene.md
    - docs/adr/0143-the-circles-of-a-view.md
    - docs/adr/0144-the-ellipses-and-the-outline-of-a-view.md
    - docs/adr/README.md
    - tasks/TASK-0380-medium-and-low-findings-of-the-review-of-2026-11-14-e.md
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
V191, and the medium finding E204, whose fix the fix of E202 and E203 needs. Section 7 of `docs/templates.md` puts them before the next product phase, R22.

## Goal

The outline of a cut side ends at its body, a turned cylinder gives a view, and Frame leaves the window of the live sketches.

## Scope (in)

1. E202: the place of each end of an outline by its exact place along the axis.
2. E203: the end of a piece by its exact place along the axis, and no line of no length or with a value that is not finite.
3. V191: the free part leaves a tall window at the right edge.
4. E204: the circle at the place of an edge along the axis, with the radius of the side there.
5. A test for each, which fails before the fix.

## Scope (out)

- T182 and E205 (TASK-0380).

## Acceptance criteria

- [x] A test proves the outline of a hole through a plate from face to face.
- [x] A test proves the view of a cylinder that a Rotate of 180 degrees turned, at the origin and moved.
- [x] A test proves that the free part leaves the window of the live sketches.
- [x] A test proves the circle of radius 5 of a cone cut at its middle, and the outline of the cut cone to its cut.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0151-review-findings) · v0.364

## Method

Mechanical: the cut of the free part.
Judgement:  the exact place along the axis.
Weakest:    a tolerance of 1e-9 for an axis along the view.

## Progress

- 2026-11-14: opened by the codebase review of 2026-11-14 (e) (TASK-0378).
- 2026-11-14: started on the review commit, branch p0151-review-findings. ADR-0145 records the place along the axis, the
  tolerance of an axis along the view and the window at the right. A first change with "the ends only" failed the test of the
  circles of a Document: each rim of a hole is a cut of its cylinder, so E204 came into this task. A guard of values that are
  not finite went, because no input reaches it. Seven injections each failed a named test; each file came back byte for byte.
- 2026-11-14: run 43574459809 passed on the work commit. Closed at v0.364.
