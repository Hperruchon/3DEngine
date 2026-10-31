---
id: 0041
title: The snap of a tap of the sketch
status: Accepted
topic: Clients, sketch
date: 2026-10-31
supersedes: []
superseded-by: []
amends: ['0026', '0034']
amended-by: []
affects:
  - 3DEngine.Core/Sketching/**
  - 3DEngine/SketchOverlay.cs
  - 3DEngine.Core/Input/**
enforced-by: 3DEngine.Tests/SketchToolTests.cs
---

# ADR-0041 — The snap of a tap of the sketch

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The roadmap row R11 is "Escalated selection and a snap engine". ADR-0034 item 3 rounds the position of a tap to the
step of a grid, and says that the step is presentation, not a snap engine. A tap within 22 pixels of a point uses that
point (ADR-0034 item 4). A person who wants a point at the middle of a line, or on a line or a circle, must add it and
then a constraint.

The codebase review of 2026-10-31, sections 5 and 6, proposed the first slice R11a and asked Q2. The owner told the
session on 2026-10-31 to use its recommendation for each question, so the recommended answer Q2 A is the answer of the
owner, and this record is Accepted.

## Decision

1. **The order.** A tap of the tools Line, Circle and Arc that has no point within 22 pixels takes the first of these
   that is within 22 pixels, and else the position of the grid:
   - the midpoint of a line;
   - the nearest point on a line, or on a circle or an arc, whichever is nearer.
2. **The position.** A midpoint is the middle of the two points in the plane. A point on a line is the point at the
   parameter of the nearest pixel of the segment, so it is on the line. A point on a curve is the centre plus the
   radius along the direction from the centre to the position of the tap, so it is on the curve.
3. **No constraint (Q2 A).** A snap gives a position to `AddSketchPoint`, as the grid does. It adds no `midpoint` and
   no `pointOn`, and a later move of the line does not move the point.
4. **Presentation.** The snap is client state (ADR-0007): the command records the numbers, and the log holds no snap.
   `SketchTool` computes it from the picture of the active sketch; the picture gives each curve its centre and its
   radius in the plane.
5. **Out of this slice.** The escalated selection of faces and edges needs their identity (K4), and waits for the
   rest of R11. Until R11b the mark of item 6 waited too.
6. **The mark (phase R11b, TASK-0134).** In the mode Sketch, the intent machine gives `SketchHoverIntent` for a
   motion of the mouse with no button, for the press of the left button, and for the first contact of a touch or a
   pen. This amends ADR-0026: a new intent, and each method gives it, so objective 9 holds: a finger shows the mark
   while it is down, before its tap. The host asks the tool for the point that a tap there would take (a point within
   22 pixels, else the snap of item 1), and the overlay draws a square of 12 pixels in cyan at its pixel. The tool
   Select has no mark. For the end of an arc the point is on the circle of its start, as the tap puts it, and there is
   no mark where the tap ends the curve. The mark goes when the finger lifts and when a pointer is lost.

## Consequences

- A person can put a point at the middle of a line, or on a line or a curve, with one tap.
- A tap near a line no longer gives the position of the grid; a person who wants the grid taps more than 22 pixels from
  each line.

## History

- 2026-10-31 · TASK-0131 · Accepted under the standing answer of the owner of 2026-10-31 (Q2 A of the codebase review
  of 2026-10-31).
- 2026-10-31 · TASK-0134 · Item 6: the mark of the snap and the intent `SketchHoverIntent` (phase R11b); the record
  amends ADR-0026.
- 2026-10-31 · TASK-0151 · Item 6: the mark at the end of an arc is on its circle, and it goes when the finger lifts
  (findings V66 and V67 of the codebase review of 2026-10-31 (c)).
