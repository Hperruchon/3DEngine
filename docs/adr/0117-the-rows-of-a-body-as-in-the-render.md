---
id: 0117
title: The rows of a body show its name larger, the sketch that made it, and the direction and the constraint of each value
status: Accepted
topic: Clients, rendering
date: 2026-11-10
supersedes: []
superseded-by: []
amends: ['0084', '0091', '0099']
amended-by: []
affects:
  - 3DEngine.App/EdgeValues.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Rows.cs
  - 3DEngine.ImageTests/BodyRowsRenderTests.cs
enforced-by: 3DEngine.ImageTests/BodyRowsRenderTests.cs
---

# ADR-0117 — The rows of a body show its name larger, the sketch that made it, and the direction and the constraint of each value

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The proposal `docs/proposals/2026-11-10-gaps-to-the-render.md` gives gap G4: in the render "Mix 1" the rows of the selected
bracket show "Bracket" larger and bold with "Extrude · correct" in green after it, a row "Made from" with "Plate on XY",
and the values "Distance", "Width C3" and "Depth C4", each as "60.000 mm" in the mono font. The window gave one size of
text, the state in grey on a second line, no row "Made from" (ADR-0099 Q2 A waited for it), and two rows named "Length".
ADR-0115 gave the values three decimals.

Slice S4 of the proposal closes G4. On 2026-11-10 the owner accepted each recommended answer of the proposal and ordered
R18d. The session gave the choices of this record. The owner can change them.

## Decision

1. **The name (amends ADR-0084 item 2 and ADR-0091 item 2).** The first row shows the name of the body in Instrument Sans
   at 34 pixels, 1.3 times the text, as the render. The atlas holds Instrument Sans a second time at that size, as its
   third font. The binding draws the weight 400 only (ADR-0091), so the name is drawn a second time 1 pixel to the right,
   and it looks bold. The state follows the name on the same line, 12 pixels after it: the kind and "correct" in green, or
   "failed" and the code in red, and "hidden". When the state has no room after the name, it goes on a line under the
   name, and the first row is higher by that line.
2. **The row "Made from" (ADR-0099 Q2, amends ADR-0099 item 3).** Under the first row, a row gives "Made from" in the
   quiet colour and the sketch that made the body in the accent at its right, as "Plate on XY". It comes from the history
   of the body (ADR-0104): the first step that opens a sketch. A press and a release on the row open that sketch, as its
   node of the history arc does. A body that no sketch made, as a box, has no such row. The row is at least 44 pixels
   high.
3. **The label of a value (amends ADR-0084 item 2).** A length of the sketch of an extrude takes its label from the
   direction of its edge in the Document: along the x axis "Width", along the y axis "Depth", along the z axis "Height"
   (the z axis is up, ADR-0096), and in another direction "Length". An edge is along an axis when that part of its
   direction is at least 0.999 of its length; the test squares both sides, so it needs no square root. The name of the
   constraint in its sketch follows the label in the quiet colour, as "Width C1". The distance of the extrude stays
   "Distance" and a radius stays "Radius". The label is in the colour of the text, and the value in the mono font.
4. **No contract change, no engine change.** The rows read the values, the history and the faces that the window has.

## Consequences

**Good:** the rows look as in the render. A person reads which direction each value measures and which constraint holds
it, and opens the sketch of a body with one tap.

**Bad:** the bold is a second draw, not a bold face: it is less clean than a bold font. The atlas is larger by one size of
one font. A long name with a long state takes two lines. The labels of a sketch that is not on an axis plane are
"Length".

## History

- 2026-11-10 · TASK-0342 · The record, from slice S4 of the proposal and the judgement of the session on items 1 to 3.
