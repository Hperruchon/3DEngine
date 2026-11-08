---
id: 0304
title: The layout of the mix has a top bar, a panel with no chrome and a status line at the lower right
status: Done
phase: R16j
opened: 2026-11-08
depends-on: [0303]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0086, 0087, 0088, 0089, 0091, 0093]
writes:
  create:
    - tasks/TASK-0304-the-layout-of-the-mix-has-no-panel-chrome.md
    - docs/adr/0093-the-layout-of-the-mix-has-no-panel-chrome.md
    - 3DEngine.ImageTests/MixImageTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.ImageTests/FixedControlsTests.cs
    - 3DEngine.ImageTests/FocusClickTests.cs
    - 3DEngine.ImageTests/PanelImageTests.cs
    - docs/adr/0080-the-fixed-controls-of-the-redesign.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0304 — The layout of the mix has a top bar, a panel with no chrome and a status line at the lower right

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-08 the owner found the desktop too far from the render "mix" of the designer brief, and chose five slices: no
panel chrome, framing, the orbit ring, the sketch plane and the command line. Each slice merges after green CI, with a
picture at 1440 x 900 beside the render. This task is the first slice.

## Goal

The title of the document is a top bar with no ground, the panel has no window chrome, and the status line is at the lower
right, as in the render.

## Scope (in)

1. ADR-0093: the top bar, the panel with no title bar, ground or border, and the status line in the mono font.
2. A picture test at 1440 x 900 of the scene of the render: a bracket of 60 x 40 x 10, selected, with its ring and values.
3. The tests of ADR-0080 for the new places.

## Scope (out)

- The framing, the ring, the sketch and the command line (the next slices). The rows move left in a later slice.

## Acceptance criteria

- [x] Each test of the fixed controls passes for the new places, and the picture shows the slice.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r16j-no-panel-chrome) · v0.289

## Method

Mechanical: the places of the top bar, the panel and the status line, from the render at 1440 x 900.
Judgement:  the rows stay at the right in this slice (ADR-0093 Q1 A).
Weakest:    the height of the panel comes from the frame before, so a new row shows one frame late; no run on the
            S23 showed the slice yet.

## Progress

- 2026-11-08: the slice. The first form of the ring read the lowest top of the fixed windows as the top of the anchors; the
  top bar made it 16, and the portrait ring went above the display. A test of the ring found it, and the ring now reads
  the row of the anchors.
- 2026-11-08: run 42477968134 passed. Closed at v0.289.
