---
id: 0313
title: The rows of a selected body take the place of the panel at the upper left
status: Active
phase: R16p
opened: 2026-11-08
depends-on: [0312]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0086, 0087, 0089, 0091, 0093, 0097, 0098, 0099]
writes:
  create:
    - tasks/TASK-0313-the-rows-of-a-selected-body-take-the-place-of-the-panel.md
    - docs/adr/0099-the-rows-of-a-selected-body-take-the-place-of-the-panel.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.Rows.cs
    - 3DEngine.App/Ui/DesktopUi.Command.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.ImageTests/BodyRowsTests.cs
    - 3DEngine.ImageTests/FixedControlsTests.cs
    - 3DEngine.ImageTests/NumericPadUiTests.cs
    - 3DEngine.ImageTests/DesktopUiTests.cs
    - 3DEngine.ImageTests/FaceSelectionTests.cs
    - 3DEngine.ImageTests/CutUiTests.cs
    - 3DEngine.ImageTests/DimensionOverlayTests.cs
    - 3DEngine.ImageTests/DesktopFormsTests.cs
    - 3DEngine.ImageTests/DimensionImageTests.cs
    - 3DEngine.ImageTests/EdgeValueTests.cs
    - docs/adr/0084-the-rows-of-the-selected-body.md
    - docs/adr/0093-the-layout-of-the-mix-has-no-panel-chrome.md
    - docs/adr/0098-the-command-line.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0313 — The rows of a selected body take the place of the panel at the upper left

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-08, after v0.297, the owner chose four slices in order. This is the first: the rows of a selected body at the
left, as in the render "Mix 1".

## Goal

On a landscape display, while a body is selected, its rows are at the upper left and the panel does not show; each control
of the panel keeps a place in the rows or in the command line.

## Scope (in)

1. ADR-0099: the place, the name field, the lines, the list of a cut, the commands and the top bar.
2. Tests of each item; the tests of the controls of the panel with a selection use a portrait display.

## Scope (out)

- The row "Made from" (ADR-0099 Q2). The history arc and the agent arc (the next slices).

## Acceptance criteria

- [ ] The tests pass, and the picture at 1440 x 900 shows the rows at the upper left and no panel.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the place of the rows in the render, and the actions of the buttons of the panel.
Judgement:  the controls of the panel go to the rows and the command line (Q1 A); no row "Made from" (Q2 A).
Weakest:    to be written at the close.

## Progress

- 2026-11-08: the slice and its tests. Thirteen injections each make a test fail. A flag that selects the name on focus
  was not needed: the request of the focus selects it, so the flag was removed.
