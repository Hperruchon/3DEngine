---
id: 0081
title: The tools of the selected body are the segments of a ring that follows it
status: Amended
topic: Clients, rendering
date: 2026-11-06
supersedes: []
superseded-by: []
amends: []
amended-by: ['0084']
affects:
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/NativeThreeDEngine.cs
enforced-by: 3DEngine.ImageTests/ToolRingTests.cs
---

# ADR-0081 — The tools of the selected body are the segments of a ring that follows it

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner ordered the layout of the redesign slice by slice on 2026-11-06 (evening); the second is "the tool ring that
follows the selection, with the tools that exist today". The "mix" direction of the designer brief puts the tools of the
selected body on segments of a ring around it (Edit sketch, Move, Distance, Hide, Rename in its picture); on a tablet and
on the phone the ring is under the thumb in the lower right corner. Each control is at least 44 pixels (ADR-0027 item 6),
and a segment is at least 54 pixels across (`size-ring-band` 68, the brief).

The tools of the selected body today are buttons of the panel: Rename with its name field, Hide or Show, Move with the pad,
Distance for an extrude with the pad, the list "Cut with", and "Sketch on face" for a selected face with a plane.

### Q1. Which tools the ring holds

- **A.** The tools that need no text and no list: Move, Distance for an extrude, Hide or Show, and Sketch on face. Rename
  and "Cut with" stay in the panel until the rows without a panel.
- **B.** Each tool, with a field and a list on the ring.

A keeps each segment one tap, as the brief draws it. **Recommended: A.**

### Q2. The panel

- **A.** The buttons of the panel stay, so the ring adds a way and removes none; a later slice removes the panel.
- **B.** The ring takes the place of the buttons now.

A keeps each test of the panel and each step of the test sheet of the phone. **Recommended: A.**

## Decision

The standing order of 2026-11-05 gives A for each question.

1. **The place.** While a body is selected, no pad is open and no sketch is active, its tools are segments of a ring. On a
   landscape display the ring is around the centre of the box of the body on the screen, with an inner radius of a third
   of the height of the display, at least the half of the diagonal of the box on the screen and 24 pixels more, and from
   160 to 375 pixels; its segments are to the right of the body, and the ring moves inward so that they stay inside the
   display. On a portrait display, as the phone, the ring is in the lower right corner, 16 pixels inside the safe area,
   with an inner radius of 150 pixels, and its segments go from the left up to the top.
2. **The segments.** Each segment is 68 pixels deep and 0.42 radians wide, with a gap of 0.04 radians, and holds its label
   at its middle. On a portrait display the segments are narrower when they must be, so that each count fits between
   the left and the top: four segments are 0.36 radians, 54 pixels at the inner radius. The band of the ring is a part of the panels for a tap, so a tap on a segment does not reach the scene;
   the inside of the ring is the scene.
3. **The tools (Q1 A, Q2 A).** Move opens the pad of a move, Distance asks the host for the distance of the extrude, Hide or
   Show changes the visibility, and "Sketch on face" makes a sketch on the selected face, as the buttons of the panel do. A
   press and a release on one segment, with no window of the panels under the pointer, give its tool.

## Consequences

**Good:** the tools of the selected body are next to it on a large display and under the thumb on the phone.

**Bad:** a body that fills the display can lie under the ring, which keeps the largest inner radius; Rename and "Cut with"
are not on the ring yet.

**Next:** the values on the edges, the rows without a panel, and the sketch under its body.

## History

- 2026-11-06 · TASK-0253 · The record, Accepted under the standing order.
- 2026-11-06 · TASK-0253 · Shipped at v0.236.
- 2026-11-07 · TASK-0264 · Items 1 and 2: the portrait ring is 16 pixels inside the safe area, and its segments fit
  between the left and the top for each count (findings V121 and V122 of the codebase review of 2026-11-07); until
  TASK-0264 the fourth segment went out of the display, and the navigation bar could cover Move. Item 2: a release on
  a chip of ADR-0082 that lies on a segment gives the chip only (finding V120).
- 2026-11-07 · TASK-0267 · ADR-0084 amends item 2: a release on a row of the selected body gives the row only, because the rows are drawn
  over the ring.
- 2026-11-07 · TASK-0271 · Item 3: a mesh body has no Move on its ring, because no move takes it (ADR-0083 item 4,
  finding V130 of the codebase review of 2026-11-07 (b)). Item 2: a label stays 4 pixels inside the safe area (the review
  of Codex of v0.251).
