---
id: 0418
title: The layout of small windows and of each state, down to 360 by 560
status: Active
phase: P0.169
opened: 2026-11-18
depends-on: [0404]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0086, 0087, 0088, 0089, 0091, 0092, 0093, 0095, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0111, 0113, 0114, 0116, 0117, 0119, 0120, 0122, 0123, 0124, 0125, 0127, 0128, 0129, 0130, 0131, 0132, 0135, 0137, 0138, 0139, 0140, 0141, 0148, 0149, 0150, 0152, 0163, 0166, 0167, 0168, 0170, 0172]
writes:
  create:
    - tasks/TASK-0418-the-layout-of-small-windows-and-of-each-state.md
    - docs/adr/0172-the-layout-of-small-windows-and-of-each-state.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Command.cs
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.App/Ui/DesktopUi.History.cs
    - 3DEngine.App/Ui/DesktopUi.Home.cs
    - 3DEngine.App/Ui/DesktopUi.Image.cs
    - 3DEngine.App/Ui/DesktopUi.Pad.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.Rows.cs
    - 3DEngine.App/Ui/DesktopUi.Sketch.cs
    - 3DEngine.ImageTests/NumericPadUiTests.cs
    - 3DEngine.ImageTests/PanelImageTests.cs
    - 3DEngine.ImageTests/ProjectPathTests.cs
    - 3DEngine.ImageTests/ResponsiveLayoutTests.cs
    - 3DEngine.ImageTests/RingTapTests.cs
    - 3DEngine.ImageTests/ToolRingTests.cs
    - docs/adr/0097-the-rows-of-a-sketch-take-the-place-of-the-panel.md
    - docs/adr/0099-the-rows-of-a-selected-body-take-the-place-of-the-panel.md
    - docs/adr/0104-the-history-arc.md
    - docs/adr/0113-the-layout-at-each-phone-size-and-small-window.md
    - docs/adr/0163-the-desktop-window-resizes-and-maximizes.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.Core/**
    - 3DEngine.Android/**
---

# TASK-0418 — The layout of small windows and of each state, down to 360 by 560

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-18 the owner asked a side session for a review of the responsive layout at each size, with pictures, a fix of
the small windows that are almost square (ADR-0163 fact 1), and a responsive window of the images. The review found faults
in the near-square windows and 367 faults in six states that no test held (ADR-0172, context). The owner answered the four
questions of the review with A on 2026-11-18.

## Goal

Each control stays in the display and free for a tap in ten states, at each size of ADR-0113 and at each step of a resize
from 360 by 560.

## Scope (in)

1. The rows of a body beside the ring (ADR-0172 item 1).
2. The pad, the rows of a sketch and its ring, the history arc and the home screen (items 2, 3).
3. The images as a fixed part of the layout (item 4).
4. The minimum window of 360 by 560, the ten states of the test, and the test of the keys of a wide pad (item 5).

## Scope (out)

- The compact layout for a window less than 560 high (a later proposal, Q2 A).
- The left panel as the ring of the part and a structure view, and the rows of 30 pixels (the choice of the owner of
  2026-11-18; a later task with its own record).

## Acceptance criteria

- [x] The two theories of `ResponsiveLayoutTests` pass in ten states, and an injection of each fix fails a test.
- [x] The desktop window stops at 360 by 560.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes on the branch.

## Outcome

Status: Active

## Method

Mechanical: the rules of the panel for the rows of a sketch and for the images, the height of the home screen.
Judgement:  the pad in the place of the panel and of the rows, with keys on lines of ten on a low landscape display; the
            images hidden during a sketch; the first tool of a sketch ring under the command line.
Weakest:    the pictures of the review are mockups for the options; the real window was not run again after the fixes.

## Progress

- 2026-11-18: opened in the side session of the review. The review page, the pictures and the questions Q1 to Q4 went to the
  owner; the owner answered A to each. The fixes of items 1 and 2 came first; a probe of a grid of sizes then gave no
  fault in landscape from a height of 400 in the four old states. The six new states had 367 faults at 17 sizes; after
  items 3 to 5, the probe gave none at 13 sizes, and the image tests passed (791).
