---
id: 0342
title: The rows of a body as in the render, with the name larger, the row "Made from" and the labels of the values
status: Active
phase: R18d
opened: 2026-11-10
depends-on: [0340]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0086, 0087, 0089, 0091, 0092, 0093, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0107, 0108, 0113, 0115, 0117]
writes:
  create:
    - tasks/TASK-0342-the-rows-of-a-body-as-in-the-render.md
    - docs/adr/0117-the-rows-of-a-body-as-in-the-render.md
    - 3DEngine.ImageTests/BodyRowsRenderTests.cs
  modify:
    - 3DEngine.App/EdgeValues.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Rows.cs
    - 3DEngine.ImageTests/BodyRowsTests.cs
    - 3DEngine.ImageTests/BodyRowsImageTests.cs
    - 3DEngine.ImageTests/FontTests.cs
    - docs/adr/0084-the-rows-of-the-selected-body.md
    - docs/adr/0091-the-fonts-of-the-redesign.md
    - docs/adr/0099-the-rows-of-a-selected-body-take-the-place-of-the-panel.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0342 — The rows of a body as in the render, with the name larger, the row "Made from" and the labels of the values

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The proposal `docs/proposals/2026-11-10-gaps-to-the-render.md` gives gap G4: the render shows the name of the body larger
and bold with its state in green after it, a row "Made from: Plate on XY", and the labels "Width C3" and "Depth C4". The
window gives one size of text, the state in grey, no row "Made from" and two rows "Length". The owner accepted each
recommended answer on 2026-11-10 and ordered R18d after R18c.

## Goal

The rows of a selected body look as in the render and tell what each value measures.

## Scope (in)

1. ADR-0117, from slice S4 of the proposal.
2. The name larger and bold, with its state after it, or under it when it has no room.
3. The row "Made from" with the sketch of the body from its history, whose tap opens that sketch.
4. The label of a length from the direction of its edge, the name of its constraint, and the value in the mono font.
5. Tests: the labels of each direction; the values of a real plate; the first row on one line or two; the row "Made from"
   and its tap; the picture of the bracket at 1440 by 900.

## Scope (out)

- "Edit sketch" and "Rename" on the ring (S5, R18e).
- The rows of a sketch (S6, R18f).
- A bold face of Instrument Sans, which needs a new font file.

## Acceptance criteria

- [ ] ADR-0117 exists. ADR-0084, ADR-0091 and ADR-0099 show the amendment.
- [ ] A test proves each label of a direction; it fails when a width gives "Length".
- [ ] A test proves the tap on "Made from"; it fails when the row gives no action.
- [ ] The picture of the bracket at 1440 by 900 shows the name, "Made from", "Width C1" and "Depth C2".
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the row "Made from" from the steps of the history; the labels from the edges that the window has.
Judgement:  the bold by a second draw; the state on one line or two; the limit of 0.999 for an axis.
Weakest:    to be written at the close.

## Progress

- 2026-11-10: opened and started on the order of the owner, on the branch r18d-rows from the branch of R18c (r18c-values-b,
  10041ee), so that it holds the values of ADR-0115. TASK-0342 and ADR-0117 are free on `main` and on each branch.
