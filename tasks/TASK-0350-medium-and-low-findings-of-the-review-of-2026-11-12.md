---
id: 0350
title: The medium and low findings of the codebase review of 2026-11-12
status: Ready
phase: P0.144
opened: 2026-11-12
depends-on: [0349]
governed-by: [0007, 0017, 0019, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0086, 0087, 0088, 0089, 0091, 0092, 0093, 0095, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0107, 0108, 0109, 0111, 0113, 0116, 0117, 0119, 0120, 0121, 0122]
writes:
  create:
    - tasks/TASK-0350-medium-and-low-findings-of-the-review-of-2026-11-12.md
  modify:
    - 3DEngine.App/HistorySteps.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.History.cs
    - 3DEngine.Vulkan/EdgeLines.cs
    - 3DEngine.ImageTests/BodyEdgesImageTests.cs
    - 3DEngine.ImageTests/RingTapTests.cs
    - 3DEngine.ImageTests/BodyRowsTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0350 — The medium and low findings of the codebase review of 2026-11-12

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-12 (`docs/reviews/2026-11-12-codebase-review.md`) gives one medium finding (V168) and three
low findings (V169, V170 and V171) in the window and the edge lines. Section 7 of `docs/templates.md` puts them after the next
product phase, R20b1. V172 goes to TASK-0348.

## Goal

The sketch that made a body shows after each count of steps, one tap gives one action, the ring and the arc fade as one, and
a T-vertex inside a closed flat face gives no false line.

## Scope (in)

1. V168: the row "Made from" and "Edit sketch" find the sketch of the body from the whole chain, along the first input.
2. V169: the ring skips a release on each row of the rows area.
3. V170: the state of a failed step and the plate of a label fade with the alpha of the style.
4. V171: a T-vertex inside a closed flat face adds no edge, or a probe shows that no backend and no STL file gives one.
5. A test for each, which fails before the fix.

## Scope (out)

- The limit of six steps of the history arc.
- A change of the limit of 30 degrees or of `MostEdges`.

## Acceptance criteria

- [ ] A test proves that a plate cut three times keeps "Made from" and "Edit sketch".
- [ ] A test proves that a tap on the row "Made from" under a segment of the ring gives one action.
- [ ] A test proves that the two colours fade during a gesture.
- [ ] A test proves V171 on a closed mesh, or the task records the probe that makes it moot.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: the reader of the sketch of a body; the guard of the ring; the two colours.
Judgement:  the fix of V171, and whether it is needed.
Weakest:    to be written at the close.

## Progress

- 2026-11-12: opened by the codebase review of 2026-11-12 (TASK-0349).
