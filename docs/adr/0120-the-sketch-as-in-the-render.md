---
id: 0120
title: The rows of a sketch are its constraints and the body it makes, its ring holds Next point and Extrude, and its plate has a label
status: Accepted
topic: Clients, sketch
date: 2026-11-11
supersedes: []
superseded-by: []
amends: ['0085', '0086', '0097', '0098']
amended-by: []
affects:
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
  - 3DEngine.ImageTests/SketchRenderTests.cs
enforced-by: 3DEngine.ImageTests/SketchRenderTests.cs
---

# ADR-0120 — The rows of a sketch are its constraints and the body it makes, its ring holds Next point and Extrude, and its plate has a label

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The proposal `docs/proposals/2026-11-10-gaps-to-the-render.md` gives gaps G7 and G8. In the render "Mix 3" the rows of the
active sketch give "Plate on XY" with "solved · 2 free" in green, one row for each constraint with its value or its residual
("C1 horizontal L1  r 0"), and a row "Makes  Bracket". The ring of the sketch holds its tools. The sketch is a glowing plate
under the body, with its label "PLATE ON XY · SOLVED · 2 FREE" and the caption "SKETCH · UNDER THE BODY IT MAKES". The window
gave the buttons "Next point" and "Extrude" at the top of the rows, then one row for each point, each line and each
constraint in a list that scrolled, and drew the lines of the sketch with no plate.

Slice S6 of the proposal closes G7 and G8. The owner accepted each recommended answer of the proposal on 2026-11-10 and
ordered R18f on 2026-11-11. The session gave the choices of this record. The owner can change them.

## Decision

1. **The rows (amends ADR-0097 items 1 and 2).** The first row gives the name of the sketch in the font of a title (ADR-0117
   item 1) and its state: "solved · N free" in green, or "not solved" in red. One row for each constraint follows: its label,
   its kind and the labels of its items at the left ("C3 distance P1 P2"), and at the right in the mono font its value with
   three decimals and the unit (ADR-0115 item 1), an angle in degrees, or "r" and the residual of a constraint with no value.
   A tap on a row selects the constraint, as before. A point, a line and a curve have no row; the tool Select selects them on
   the sketch. The buttons of the selection stay above the rows.
2. **The body that the sketch makes.** A row "Makes" gives each live body that the sketch makes, in the accent: each Extrude
   that reads the sketch, and, when a later feature consumed that body, the feature that consumed it, to a live body. The host
   reads the features off the frame thread, as the history (ADR-0104). A tap on the row ends the sketch and selects the body.
3. **Next point and Extrude (amends ADR-0086 items 1 and 2, and ADR-0098 item 3).** The ring of the sketch holds "Next point"
   after the tools while the tool Line is active, and "Extrude" before Finish; each opens the pad that its button opened. The
   command line holds "Extrude" while a sketch is active. While the ring holds the tools, the window does not show "Next
   point" and "Extrude"; End and Close stay in the window.
4. **The span of the ring of a sketch.** The ring of a sketch takes the span of ADR-0119 item 2, so its tools end before
   Accept of a proposal. A sketch needs each of its tools, so no tool leaves the ring: its inner radius grows until a narrowed
   segment is at least 46 pixels at it, as a portrait ring does, up to the half of the display.
5. **The plate (amends ADR-0085 item 2).** The overlay gives the plate of the active sketch: the box of its points in its
   plane, with no origin, and a margin of 12 percent of its larger side, at least 2 units. The window fills it with the
   accent at a low alpha and draws its edge with a glow, under the lines of the sketch. Over the lines, under the plate and
   ending at its middle, the name and the state of the sketch in capitals in the accent, on the ground of the scene; and,
   while the sketch is under the body that it makes, the caption "SKETCH · UNDER THE BODY IT MAKES" under them. The render puts
   the name along the far edge of the plate; there the values of ADR-0115 and the body cover it, so it is under the plate. The
   tools of the ring are at the right, so the text ends at the middle of the plate.
6. **No contract change, no engine change.** The rows, the plate and the bodies made are presentation (ADR-0007); the client
   reads `GetSketch` and `GetFeatures`, and each action exists.

## Consequences

**Good:** the sketch looks as in the render: its rows tell its constraints and the body it makes, and its tools are on its ring.

**Bad:** a point or a line has no row, so the person selects it on the sketch only. The ring of a small sketch is larger than
the sketch. The label of the plate is not along its edge, as the render. The row "Makes" waits for the read of the features,
so it shows one frame or more after the sketch opens.

## History

- 2026-11-11 · TASK-0347 · The record, from slice S6 of the proposal and the judgement of the session.
