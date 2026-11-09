---
id: 0105
title: The values of an active sketch show on its edges
status: Accepted
topic: Clients, rendering
date: 2026-11-09
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - 3DEngine.App/SketchValues.cs
  - 3DEngine.App/Ui/DesktopUi.Values.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.ImageTests/SketchValueTests.cs
enforced-by: 3DEngine.ImageTests/SketchValueTests.cs
---

# ADR-0105 — The values of an active sketch show on its edges

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-08, after v0.297, the owner chose four slices of the render "mix" in this order: the rows of a selected body at
the left (R16p, v0.298), the history arc (R16q, v0.308), the agent arc (R16r, v0.309) and the values on the edges of a
sketch. Each slice merges after green CI, with a picture. In the render "Mix 3" the active sketch shows its values on its
edges: "C3 60 → 75" on one line, "C4 40" on another, and the name and the state of the sketch on a third.

What exists:

- ADR-0082: while an extrude is selected and no sketch is active, each `distance` and `radius` constraint of its sketch is a
  chip on an edge of the body. A tap on a chip opens the pad of the value. The chip has at least 44 pixels on each side, the
  font of a value (ADR-0091) and a dashed border while its pad is open.
- ADR-0034 items 8 and 9: the rows of the active sketch name each constraint "C1", "C2" in the order of the record, and a
  tap on the row of a constraint with a value opens its pad (`SetConstraintValueAction`).
- ADR-0097: while a sketch is active, its rows are at the left and its ring is at the right.

So a value of an active sketch is in its rows only, far from the line that it measures.

### Q1. Which values

- **A.** Each `distance` constraint of one line, or of two points, and each `radius` constraint of a circle or an arc of
  the active sketch: the lengths, as ADR-0082.
- **B.** A, and each `angle` constraint, at the point where its two lines meet.

B adds a place that A does not need, and the pad of an angle already is in the rows. **Recommended: A.**

### Q2. The look

- **A.** A chip flat on the screen, as ADR-0082: "C3  60 mm" in the font of a value, at least 44 pixels on each side.
- **B.** The text in the plane of the sketch, along its line, as the render draws it.

A is a target for a finger and reads at each angle of the view; B follows the render, but a text in the plane is small
and hard to tap when the view is low. **Recommended: A.**

### Q3. What a tap does

- **A.** A tap on a chip opens the pad of its constraint, as a tap on its row does.
- **B.** Nothing; the chip shows the value only.

**Recommended: A.**

## Decision

The owner chose the slice and its merge after green CI; the recommendations answer Q1 to Q3.

1. **The values (Q1 A).** While a sketch is active, `SketchValues.Of` gives a value for each `distance` constraint of one
   line, or of the two ends of a line or of two points, and for each `radius` constraint of a circle or an arc of that
   sketch. A value whose place is behind the eye has no chip. A constraint of a reference item has no chip.
2. **The place.** The chip of a distance is at the middle of its line, or of the segment between its two points; the chip of
   a radius is on its curve, at the point of the curve farthest from the centre of the sketch on the screen. Each chip goes
   24 pixels farther from the centre of the sketch on the screen, inside the display, and the placement of ADR-0056 moves
   the chips apart, as ADR-0082 item 2 does.
   The centre of the sketch on the screen is the middle of the box of the pixels of its points, with no origin, so a
   rectangle far from the origin moves its chips straight out from its own middle.
3. **The chip (Q2 A).** The text is the label of the constraint, two spaces, the value and the unit: "C3  60 mm", or
   "C5  R 12 mm" for a radius. It has the font of a value and at least 44 pixels on each side, and its rectangle is a part
   of the panels for a tap.
4. **The tap (Q3 A).** A press and a release on one chip open the pad of its constraint, the pad that its row opens, and
   the apply gives `SetSketchConstraintValue` of the active sketch. While that pad is open, the chip has the colour of the
   dimensions and a dashed border, as ADR-0082 item 4.
5. **No contract change.** The values are presentation (ADR-0007): the client reads `GetSketch`, and the apply is a command
   that exists.

## Consequences

**Good:** the person sees each length of the sketch on the line that it measures, and changes it there, as in the render.

**Bad:** an angle has no chip; the chips are flat on the screen and not in the plane of the sketch, as the render draws
them.

## History

- 2026-11-09 · TASK-0327 · The record, with the slice (the order of the owner of 2026-11-08).
