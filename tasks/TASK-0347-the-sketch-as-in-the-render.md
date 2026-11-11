---
id: 0347
title: The sketch as in the render, with the rows of its constraints, its tools on the ring and its plate
status: Active
phase: R18f
opened: 2026-11-11
depends-on: [0346]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0041, 0053, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0088, 0089, 0091, 0092, 0093, 0095, 0096, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0110, 0111, 0113, 0114, 0117, 0119, 0120]
writes:
  create:
    - tasks/TASK-0347-the-sketch-as-in-the-render.md
    - docs/adr/0120-the-sketch-as-in-the-render.md
    - 3DEngine.ImageTests/SketchRenderTests.cs
  modify:
    - 3DEngine.App/HistorySteps.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/SketchOverlay.cs
    - 3DEngine.App/Ui/PanelAction.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Sketch.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.Command.cs
    - 3DEngine.App/Ui/DesktopUi.Orbit.cs
    - 3DEngine.App/Ui/DesktopUi.Overlay.cs
    - 3DEngine.ImageTests/SketchWindowUiTests.cs
    - 3DEngine.ImageTests/ToolRingTests.cs
    - 3DEngine.ImageTests/MixImageTests.cs
    - docs/adr/0085-the-sketch-under-its-body.md
    - docs/adr/0086-the-tools-of-the-sketch-on-the-ring.md
    - docs/adr/0097-the-rows-of-a-sketch-take-the-place-of-the-panel.md
    - docs/adr/0098-the-command-line.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0347 — The sketch as in the render, with the rows of its constraints, its tools on the ring and its plate

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The proposal `docs/proposals/2026-11-10-gaps-to-the-render.md` gives gaps G7 and G8: the render gives the rows of a sketch as
its constraints with their residual and a row "Makes", and the sketch as a glowing plate under the body with its label and a
caption. The window gives buttons, a row for each point, line and constraint, and no plate. The owner accepted each
recommended answer on 2026-11-10 and ordered R18f on 2026-11-11.

## Goal

The active sketch looks as in the render, and its rows and its ring give each of its actions.

## Scope (in)

1. ADR-0120, from slice S6 of the proposal.
2. The rows: the name and the state, one row for each constraint with its value or its residual, and the row "Makes".
3. "Next point" and "Extrude" on the ring of the sketch and "Extrude" in the command line, in the span of a body ring.
4. The plate of the active sketch with its label and, under its body, its caption.
5. Tests: the text of a row; the rows and their taps; the bodies made; the two tools of the ring and their pads; the command;
   the plate, its label and its caption; the picture of the bracket with its sketch at 1440 by 900.

## Scope (out)

- The preview of a proposal (S11) and the proposal in the window (S9).
- A label along the edge of the plate.

## Acceptance criteria

- [ ] ADR-0120 exists. ADR-0085, ADR-0086, ADR-0097 and ADR-0098 show the amendment.
- [ ] A test proves the row "Makes" and its tap; it fails when the tap gives no action.
- [ ] A test proves the two tools of the ring; it fails when Extrude is not on the ring.
- [ ] The picture of the bracket with its sketch at 1440 by 900 shows the rows, the tools and the plate.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the rows from the constraints; the bodies made from the features, as the history.
Judgement:  the place of the label of the plate; the ring of a sketch that grows; no row for a point.
Weakest:    to be written at the close.

## Progress

- 2026-11-11: opened and started on the order of the owner, at `main` c11ff56 (v0.329), branch r18f-sketch. TASK-0347 and
  ADR-0120 are free on `main` and on each branch. The first places of the label of the plate (along the lowest edge, then
  the highest) were under a value or over the body in the picture of the mix, so the label is under the plate with the
  caption. The first ring of a small sketch did not narrow, because 46 pixels at its inner radius need more than 0.42
  radians, so its inner radius grows.
