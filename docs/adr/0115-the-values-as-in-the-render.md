---
id: 0115
title: A value shows three decimals on a pill on its dimension line, outside the outline of the body
status: Accepted
topic: Clients, rendering
date: 2026-11-10
supersedes: []
superseded-by: []
amends: ['0082', '0105', '0110']
amended-by: []
affects:
  - 3DEngine.App/EdgeValues.cs
  - 3DEngine.App/SketchValues.cs
  - 3DEngine.App/Ui/DesktopUi.Values.cs
  - 3DEngine.App/Ui/DesktopUi.Proposal.cs
  - 3DEngine.ImageTests/DimensionLineTests.cs
enforced-by: 3DEngine.ImageTests/DimensionLineTests.cs
---

# ADR-0115 — A value shows three decimals on a pill on its dimension line, outside the outline of the body

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The proposal `docs/proposals/2026-11-10-gaps-to-the-render.md` compares the window with the renders of the designer brief.
Its gap G3 is the values on the edges. In the render "Mix 1", each value has a dimension line parallel to the edge that it
measures, with an extension line from each end of the edge. The value is on an outlined, round pill in the mono font, as
"60.000 mm", and each pill is outside the body. In the window before this record, a value was a filled box of 44 by 44
pixels or more, as "60 mm", 24 pixels out from the middle of its edge, and two of the boxes were over the body.

Slice S3 of the proposal closes G3. On 2026-11-10 the owner accepted each recommended answer of the proposal. Q6 A is the
form of a value: three decimals and the unit, and more decimals when the value has them, because principle 3 of the brief
says that a value is exact, with equal-width digits.

The session gave the other choices of this record. The owner can change them.

## Decision

1. **The form (Q6 A, amends ADR-0082 item 2 and ADR-0105 item 3).** A value on an edge shows three decimals and its unit,
   and more decimals when it has them: "60.000 mm", "60.125 mm". The form has at most 15 significant digits, so a value of
   0.1 + 0.2 shows "0.300", and it never shows "-0.000". A value that a proposal changes shows the proposed value in the same
   form on its chip, as "60.000 → 75.000 mm" (amends ADR-0110 item 2). The lines of a proposal on its arc or its sheet keep
   their form until slice S9 (R18i) changes the proposal, because the longer form does not fit the sheet of a phone at 390
   pixels.
2. **The dimension line (amends ADR-0082 item 2 and ADR-0105 item 2).** The outline of a body is the ends of the segments of
   its edges on the screen; the outline of a sketch is its points and its curves on the screen. For a value of a distance or
   of a length, the dimension line is parallel to the edge on the screen, on the side away from the centre of the outline.
   It is past each point of the outline by a gap of 16 pixels and the half of the pill along the normal of the line. So the
   whole pill is outside the outline. An extension line goes from 4 pixels off each end of the edge to 6 pixels past the
   dimension line. A radius has no edge with two ends: a leader goes from the point of its curve straight out from the
   centre, past the outline in the same way.
3. **The pill.** The pill is the text in the mono font (ADR-0091) with 10 pixels of padding at each side and 5 pixels above
   and under. It is round at its two ends, with the ground of the scene and an outline of 1.5 pixels, as the chips of
   ADR-0108 item 2. The pill is centred on the dimension line. Its target for a tap stays at least 44 pixels on each side
   (ADR-0027 item 6) and holds the pill, so the drawn pill is smaller than its target. A pending value keeps the dashed
   border and the colour of the dimensions (ADR-0082 item 4), and the lines take that colour too.
4. **The placement.** The placement of ADR-0056 still moves two pills apart, and a pill under the rows still goes under them
   (ADR-0084 item 4). The dimension line goes through the middle of the pill, so it follows a pill that the placement
   moves along the normal; a pill that the placement moves along the line is no longer at the middle of the line.
5. **No contract change, no engine change.** The lines are presentation (ADR-0007). The window computes them from the edges
   of `GetFaces` and from the sketch, as before.

## Consequences

**Good:** the values look as in the render. A value is exact, and it never covers the body. The extension lines show which
edge a value measures.

**Bad:** a pill is farther from its edge than before, so on a small display a pill can go to the edge of the display, where
the placement of ADR-0056 holds it inside. The extension lines of two values can cross. The rows of a body show the text of
its values, so they show three decimals too, before slice S4 changes the rows. The lines of a proposal show the short form
until R18i.

## History

- 2026-11-10 · TASK-0340 · The record, with Q6 A of 2026-11-10 and the judgement of the session on items 2 to 4.
