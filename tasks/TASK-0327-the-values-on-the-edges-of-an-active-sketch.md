---
id: 0327
title: The values of an active sketch show on its edges
status: Active
phase: R16s
opened: 2026-11-09
depends-on: [0326]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0089, 0091, 0092, 0093, 0097, 0098, 0099, 0102, 0104, 0105]
writes:
  create:
    - tasks/TASK-0327-the-values-on-the-edges-of-an-active-sketch.md
    - docs/adr/0105-the-values-on-the-edges-of-an-active-sketch.md
    - 3DEngine.App/SketchValues.cs
    - 3DEngine.ImageTests/SketchValueTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.Values.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.ImageTests/MixImageTests.cs
    - docs/adr/README.md
    - tasks/TASK-0314-a-path-line-from-the-document-to-the-selection.md
    - tasks/TASK-0315-a-focus-on-one-body.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0327 — The values of an active sketch show on its edges

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The last of the four slices that the owner chose on 2026-11-08 after v0.297: the values on the edges of a sketch, as the
render "Mix 3" draws them.

## Goal

While a sketch is active, each length constraint has a chip on the line or the curve that it measures, and a tap on the
chip opens the pad of its value.

## Scope (in)

1. ADR-0105: the values, the place, the chip and the tap.
2. `SketchValues`: the chips of an active sketch, with no GPU, and its tests.
3. The chips in the window, their tests, and the picture of the mix at 1440 by 900 with an active sketch.

## Scope (out)

- An angle chip, a text in the plane of the sketch, and the pending chip of a proposal of an agent (ADR-0092 item 5).

## Acceptance criteria

- [ ] The tests pass, and the picture at 1440 by 900 shows a chip on each length of the active sketch.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the chips follow ADR-0082; the labels follow the rows of ADR-0034 item 8.
Judgement:  the lengths only (Q1 A); a chip flat on the screen (Q2 A); the pad of the row (Q3 A).
Weakest:    to be written at the close.

## Progress
- 2026-11-09: ADR-0105, `SketchValues`, the chips of an active sketch and eleven tests. An agent made the code in this
  worktree. Four injections failed their tests, and each file came back byte for byte: the place 24 pixels out, the tap
  that opens the pad of the row, the connection of the host (`NativeThreeDEngine.SketchValuesOf`, also with no move under
  the body), and the chips as a part of the panels. The review added to ADR-0105 item 2 the centre of the sketch on the
  screen, which the record did not define. Local: the gates, a clean build with zero warnings, the tests of the solution
  and the phone project.
