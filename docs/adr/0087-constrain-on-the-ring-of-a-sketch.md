---
id: 0087
title: "Constrain" on the ring of a sketch opens the constraints that fit the selection
status: Accepted
topic: Clients, sketch
date: 2026-11-07
supersedes: []
superseded-by: []
amends: ['0086']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
  - 3DEngine.App/Ui/DesktopUi.Sketch.cs
  - 3DEngine.App/Ui/DesktopUi.cs
enforced-by: 3DEngine.ImageTests/ToolRingTests.cs
---

# ADR-0087 — "Constrain" on the ring of a sketch opens the constraints that fit the selection

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner ordered the layout of the redesign slice by slice, with no change of the contract (2026-11-06, evening). ADR-0086
put Line, Select, Circle, Arc and Finish on the ring while a sketch is active; its "Next" names "Constrain" with a second
ring. The picture of step 3 of the designer brief shows "Constrain" on the ring of a sketch.

Today the window of the sketch shows a button for each kind of constraint that fits the selection: for example Horizontal,
Vertical and Distance for one line. A kind with a value opens the pad of its value. Each kind has a rule of fit on the
kinds and the count of the selected items.

### Q1. When "Constrain" shows

- **A.** Only when at least one kind of constraint fits the selection, as the last segment before Finish.
- **B.** Always, with an empty second ring when no kind fits.

A shows no segment that does nothing. **Recommended: A.**

### Q2. Which kinds the second ring holds

- **A.** The kinds that fit the selection, from the same rule as the buttons of the window of the sketch, then "Back".
- **B.** Each kind, and a kind that does not fit is grey.

A keeps one rule for the two places. A selection of one line gives four segments (three kinds and "Back"), not the
seventeen of B. **Recommended: A.**

### Q3. What closes the second ring

- **A.** A tap on a kind (after its action or its pad), a tap on "Back", or a change of the selection.
- **B.** Only a tap on "Back".

A goes back to the tools after each constraint, as the window of the sketch does. **Recommended: A.**

## Decision

The standing order of 2026-11-05 gives A for each question.

1. **The segment (Q1 A).** While a sketch is active and at least one kind of constraint fits the selection, the ring holds
   "Constrain" between Arc and Finish (this amends ADR-0086 item 2). With no selection, or with no kind that fits, the ring
   holds the five tools of ADR-0086.
2. **One rule (Q2 A).** One method gives the kinds that fit the selection, each with what its button does: the action of
   the constraint, or the pad of its value. The window of the sketch and the ring both use it, so the two always agree.
3. **The second ring (Q2 A, Q3 A).** A tap on "Constrain" replaces the segments with the kinds that fit, in the order of
   the window of the sketch, then "Back". A tap on a kind does what its button does and closes the second ring. A tap on
   "Back" closes it. A change of the selection closes it.
4. **No contract change.** The ring gives the actions and the pads of the window of the sketch, which exist.
5. **A wide label.** On a landscape display, a label of the ring of a sketch that is wider than the band ends 6 pixels
   outside the inner edge of its segment, and starts at least 4 pixels to the right of the panel. A plate in the colour
   of its segment is under it. The picture of the first form showed "Horizontal" centred over the points of the sketch.

## Consequences

**Good:** a constraint is two taps away beside the sketch, on each device, as the brief draws.

**Bad:** the window of the sketch still holds the same buttons, so each kind shows two times. The ring shows its new
segments one frame after the tap, as each control of the immediate interface does. On a display of 1280 by 720, the room
between the panel and the window of the sketch is about 310 pixels, so a label such as "Horizontal" (about 140 pixels)
still covers the edge of a sketch of 150 pixels; an icon for each kind needs the fonts that wait for the owner. No test holds the second ring on a
portrait display; the largest fit today is four kinds (two lines), and the portrait ring of ADR-0086 item 3 grows with
the count of segments.

**Next:** the window of the sketch with its rows only.

## History

- 2026-11-07 · TASK-0276 · The record, Accepted under the standing order.
- 2026-11-07 · TASK-0276 · Shipped at v0.261.
