---
id: 0267
title: The data of the selected body is in rows with no panel
status: Active
phase: R16d
opened: 2026-11-07
depends-on: [0264]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084]
writes:
  create:
    - tasks/TASK-0267-the-rows-of-the-selected-body.md
    - docs/adr/0084-the-rows-of-the-selected-body.md
    - 3DEngine.App/Ui/DesktopUi.Rows.cs
    - 3DEngine.ImageTests/BodyRowsTests.cs
    - 3DEngine.ImageTests/BodyRowsImageTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Values.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/EdgeValues.cs
    - docs/adr/0081-the-tool-ring-of-the-redesign.md
    - docs/adr/0082-the-values-on-the-edges-of-the-redesign.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0267 — The data of the selected body is in rows with no panel

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner ordered the layout of the redesign slice by slice, with no change of the contract (2026-11-06, evening), and
repeated it on 2026-11-07: after the STL demonstration, the next redesign slices. After the fixed controls, the tool ring
and the values on the edges comes "the rows without a panel" of the "mix" direction of the designer brief.

## Goal

While a body is selected on a landscape display, its name, its state and its values are rows with no panel at the upper
right, and a tap on a value opens its pad.

## Scope (in)

1. ADR-0084 with its three questions, answered by the standing order.
2. The rows, their tap, and the chips that move out of them.

## Scope (out)

- The rows on a portrait display, the tools of the selection out of the panel, and the sketch under its body.

## Acceptance criteria

- [ ] The rows are at the upper right, 16 pixels inside the safe area, each 44 pixels high, and a part of the panels.
- [ ] A tap on a value row opens its pad, by touch and by mouse, and a row over the ring gives the row only.
- [ ] The state gives "correct", or "failed" and the code, and "hidden".
- [ ] A chip on the rows goes under them.
- [ ] A picture of the rows on the GPU is kept for the owner.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the tests of the place, the tap, the state and the chips, with no GPU; a picture on the GPU; injections of a
            ring with no guard of the rows and of a chip that stays on the rows.
Judgement:  the upper right while the panel stays (Q1 A); no rows on a portrait display yet (Q2 A); the data that the
            client has today (Q3 A). The column is 360 pixels, because the bitmap font cut "Bracket" at 320.
Weakest:    to be written at the close.

## Progress

- 2026-11-07: ADR-0084, the rows, their tap and the chips; seven tests with no GPU and one picture. The first picture
  showed the state past the right edge and a chip on the rows; the state moved to the right of the first row, the chip
  goes under the rows, and the column is 360 pixels. Injections of a ring with no guard of the rows and of a chip that
  stays on the rows each fail a test.
