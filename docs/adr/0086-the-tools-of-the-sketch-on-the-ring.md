---
id: 0086
title: While a sketch is active, the ring holds the tools of the sketch
status: Accepted
topic: Clients, rendering
date: 2026-11-07
supersedes: []
superseded-by: []
amends: ['0081']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/NativeThreeDEngine.cs
enforced-by: 3DEngine.ImageTests/ToolRingTests.cs
---

# ADR-0086 — While a sketch is active, the ring holds the tools of the sketch

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner ordered the layout of the redesign slice by slice, with no change of the contract (2026-11-06, evening), and on
2026-11-07 put the next redesign slices after the STL demonstration. ADR-0085 moved the active sketch of a live extrude out
of its body; its "Next" names the tools of the sketch on the ring. Step 3 of the main flow of the designer brief says: "the
ring now holds sketch tools", and its picture shows Line, Circle, Arc, Constrain and Finish on the ring.

Today the ring hides while a sketch is active (ADR-0081 item 1), and the window of the sketch at the upper right holds the
buttons Line, Select, Circle, Arc and Finish (ADR-0034 item 8). The panel is at the upper left. On a display of 1280 by
720 the room between the panel and the window of the sketch is about 300 pixels wide.

### Q1. Which tools

- **A.** Line, Select, Circle, Arc and Finish: the tools of the window of the sketch, in its order.
- **B.** Also "Constrain", with a second ring of the kinds of a constraint, as the brief draws.

A gives each tool that exists today one tap. B needs a second ring and a choice of the items first. **Recommended: A.**

### Q2. Where the ring of a sketch goes on a landscape display

- **A.** On the left of the sketch, centred on the sketch on the screen, with an inner radius of 24 pixels more than the
  half of the diagonal of the sketch, at least 100, and to the right of the panel.
- **B.** On the right of the sketch, as the ring of a body.

The window of the sketch takes the right side, and a ring under it loses its taps; a first form of B moved the ring onto
the points of the sketch. **Recommended: A.**

### Q3. A portrait display

- **A.** The thumb ring of ADR-0081 in the lower right corner, with an inner radius that grows so that each of five
  segments is at least 44 pixels at it.
- **B.** No ring of a sketch on a portrait display.

A keeps the tools under the thumb on the phone. **Recommended: A.**

## Decision

The standing order of 2026-11-05 gives A for each question.

1. **The place (Q2 A).** While a sketch is active, the ring holds the tools of the sketch in the place of the tools of a
   body (this amends ADR-0081 item 1, which hid the ring). The host gives the centre of the points of the sketch as the
   person sees them (with the move of ADR-0085), and the half of their diagonal; a sketch with no point is at the middle
   of the window. On a landscape display the segments go down on the left of the centre, with the inner radius of Q2 A;
   the centre moves only as far as the band needs to stay 16 pixels to the right of the panel and inside the display.
2. **The tools (Q1 A).** Line, Select, Circle, Arc and Finish, in that order. A tap on a tool gives the tool, as its
   button of the window of the sketch does; a tap on Finish ends the sketch. The tool in use has the colour of an active
   control.
3. **A portrait display (Q3 A).** The ring is in the lower right corner, as in ADR-0081; its inner radius is at least
   150 pixels and grows so that each segment is at least 46 pixels at it (this amends ADR-0081 item 1).
4. **No contract change.** The ring gives the actions of the window of the sketch, which exist.

## Consequences

**Good:** the tools of a sketch are one tap away beside the sketch, on each device, as the brief draws.

**Bad:** the window of the sketch still holds the same tools, so each tool shows two times. "Constrain" has no place on
the ring yet. On a small portrait display the window of the sketch can cover the upper part of the ring.

**Next:** "Constrain" with a second ring of the kinds; the window of the sketch with its rows only.

## History

- 2026-11-07 · TASK-0275 · The record, Accepted under the standing order.
