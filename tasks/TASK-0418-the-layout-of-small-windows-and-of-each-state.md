---
id: 0418
title: The layout of small windows and of each state, down to 360 by 560
status: Done
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
in the near-square windows and 367 faults in six states that no test held (ADR-0172, context). The owner answered Q1 A and
Q2 A on 2026-11-18; Q3 A and Q4 A follow from the choice of the left panel (ADR-0172, context).

## Goal

Each control stays in the display and free for a tap in ten states, at each size of ADR-0113 and at each step of a resize
from 360 by 560.

## Scope (in)

1. The rows of a body beside the ring (ADR-0172 item 1).
2. The pad, the rows of a sketch and its ring, the history arc and the home screen (items 2, 3).
3. The images as a fixed part of the layout (item 4).
4. The minimum window of 360 by 560, the ten states of the test, and the test of the fields and keys of a pad (item 5).

## Scope (out)

- The compact layout for a window less than 560 high (a later proposal, Q2 A).
- The left panel as the ring of the part and a structure view, and the rows of 30 pixels (the choice of the owner of
  2026-11-18; a later task with its own record).

## Acceptance criteria

- [x] The two theories of `ResponsiveLayoutTests` pass in ten states, and an injection of each fix fails a test.
- [x] The desktop window stops at 360 by 560.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes on the branch.

## Outcome

Status: Done · commit (the merge of the branch responsive-small-windows) · v0.400

## Method

Mechanical: the rules of the panel for the rows of a sketch and for the images, the height of the home screen.
Judgement:  the pad in the place of the panel and of the rows; a compact pad and a low pad that hides the anchors; the
            images hidden during a sketch; the first tool of a sketch ring under the command line; no rule for a path
            line on a narrow landscape display, because no size of the window or of a phone needs it.
Weakest:    the test reads the width of each key and of its text at the least scale, not the drawn text, so the scale of
            the text of a key has no injection; the real window showed it at 360 by 560 and at 900 by 560.

## Progress

- 2026-11-18: opened in the side session of the review. The review page, the pictures and the questions Q1 to Q4 went to the
  owner; the owner answered Q1 A and Q2 A, and the choice of the left panel gave A for Q3 and Q4. The first fixes (the
  rows beside the ring, "Save as..." on its own line) came first; a probe of a grid of sizes then gave no fault in
  landscape from a height of 400 in the four old states. The six new states had 367 faults at 17 sizes; after the fixes
  of the pad, the sketch, the history arc, the home screen and the images, the probe gave none at 13 sizes, and the image
  tests passed (791).
- 2026-11-18: 17 injections, each restored from its saved text. 12 failed a test. Two did not compile and got a second form,
  which failed a test. Three passed, so their code had no case: the path line under the top bar on a narrow landscape
  display (a window of at least 560 is wider than 560 in landscape), the width of the images beside the ring, and the five
  lines of keys of a landscape pad. The first two left the code and ADR-0172; the third got its own test.
- 2026-11-19: a second round of 12 injections. Each failed a test except three, whose code had no case at a size of the
  test: the bottom of the images above a tool of the ring, the rows at most as wide as the display, and the fixed width of
  the window of "Cut with". They left the code. The branch was rebased on `origin/main` at v0.399 (`8301412`).
- 2026-11-19: the real window at 360 by 560 showed that a pad of three fields (New box) scrolled its keys out of view, and at
  900 by 560 cut "Delete" and "Cancel". The test of the pad now has a pad of three fields, portrait sizes and the width
  of the text of each key; it failed on that code. The compact pad, the low pad and the scale of the text of a key fixed
  it. A third round of 6 injections: 5 failed the test; a gap of 12 pixels between keys passed, so the gap of 8 pixels left
  the code. Pictures of the real window at 360 by 560, 900 by 560 and 1280 by 720 showed each key and its text, and the
  log had no message of the validation layer.
- 2026-11-19: run 44423569915 passed on the branch on each of its seven jobs (`8dc1083`). A clean build gave 0 warnings,
  the phone project built, and `dotnet test` passed 2902 tests. Closed at v0.400; the merge waits for the owner.
