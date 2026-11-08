---
id: 0308
title: The rows of an active sketch take the place of the panel, and its ring is on the right
status: Done
phase: R16n
opened: 2026-11-08
depends-on: [0310]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0086, 0087, 0088, 0089, 0091, 0093, 0095, 0097]
writes:
  create:
    - tasks/TASK-0308-the-rows-of-a-sketch-take-the-place-of-the-panel.md
    - docs/adr/0097-the-rows-of-a-sketch-take-the-place-of-the-panel.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Sketch.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.Pad.cs
    - 3DEngine.App/Ui/DesktopUi.Orbit.cs
    - 3DEngine.ImageTests/ToolRingTests.cs
    - 3DEngine.ImageTests/SketchWindowUiTests.cs
    - 3DEngine.ImageTests/SketchLiftImageTests.cs
    - docs/adr/0034-sketch-tools.md
    - docs/adr/0086-the-tools-of-the-sketch-on-the-ring.md
    - docs/adr/0087-constrain-on-the-ring-of-a-sketch.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0308 — The rows of an active sketch take the place of the panel, and its ring is on the right

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The fourth slice that the owner chose on 2026-11-08 is the sketch plane. With the z axis up (TASK-0307), the sketch of
ADR-0085 is under its body. The window of the sketch and the side of its ring are still far from the render "Mix 3".

## Goal

While a sketch is active, its rows are at the left with no chrome, and its ring is on the right with icons and a caption.

## Scope (in)

1. ADR-0097: the rows in the place of the panel, the heading, the pad, and the ring on the right.
2. Tests of the layout of the ring, of the pad beside the rows, and of the picture of the sketch under its body.

## Scope (out)

- The caption under the sketch (ADR-0097 Q2). The values on the edges of the sketch. The window of the live sketches.

## Acceptance criteria

- [x] The tests pass, and the picture of the sketch under its body shows the rows at the left and the ring on the right.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r16n-sketch-rows-b) · v0.295

## Method

Mechanical: the places of the rows and the ring in the render "Mix 3".
Judgement:  no caption under the sketch (ADR-0097 Q2 A), after the first form crossed the ring at 1280 x 720.
Weakest:    while a sketch is active the panel does not show, so its controls wait for Finish; no run on the S23 showed
            the slice yet.

## Progress

- 2026-11-08: the slice and its tests. With the segments on the left again, three tests fail. The first form of the
  label rule took Select, Circle and Finish as wide labels and drew no icon; a tool with an icon now keeps it.
- 2026-11-08: the codebase review of 2026-11-08 (c) found V146 (high), so TASK-0310 came first; this task depends on it.
  The change of this task fixes V148 of that review.
- 2026-11-08: run 42483436816 passed. Closed at v0.295.
