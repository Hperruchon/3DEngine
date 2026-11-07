---
id: 0088
title: While the ring of a sketch shows, the window of the sketch does not repeat it
status: Accepted
topic: Clients, sketch
date: 2026-11-07
supersedes: []
superseded-by: []
amends: ['0034']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.Sketch.cs
enforced-by: 3DEngine.ImageTests/ToolRingTests.cs
---

# ADR-0088 — While the ring of a sketch shows, the window of the sketch does not repeat it

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner ordered the layout of the redesign slice by slice, with no change of the contract (2026-11-06, evening).
ADR-0086 put Line, Select, Circle, Arc and Finish on the ring of a sketch, and ADR-0087 put the kinds of a constraint that
fit the selection on a second ring. The "Next" of ADR-0087 names this slice: the window of the sketch with its rows only.

Today the window of the sketch (ADR-0034 item 8) still holds the same tools, Finish and the kinds, so each one shows two
times. With one line selected, the buttons take four lines at the top of the window, and the rows of the points, the
lines and the constraints start under them. The host gives no centre of the ring in some states, for example while a
pad is open, and then the ring does not show.

### Q1. Which buttons leave the window

- **A.** The buttons that the ring holds: Line, Select, Circle, Arc, Finish and the kinds of a constraint. Next point,
  End, Close, Extrude, Move point, Value and Delete stay.
- **B.** Each button: Extrude, Delete and the others go onto the ring too.

A removes each button that shows two times and needs no new segment. B needs new segments and a choice of their order.
**Recommended: A.**

### Q2. When they leave

- **A.** In each frame where the ring of the sketch shows, on each display; in a frame with no ring, the window holds
  them as before.
- **B.** On a landscape display only.

A never leaves a tool with no place to tap: with no ring, the window has it. **Recommended: A.**

## Decision

The standing order of 2026-11-05 gives A for each question.

1. **The window (Q1 A, Q2 A).** In a frame where the ring of the active sketch shows, the window of the sketch holds the
   name and the state of the sketch, Next point, End and Close for the tool Line, Extrude, the buttons of the selection
   that the ring does not hold (Move point, Value and Delete), the message of a rejected command, and the rows. It does
   not hold Line, Select, Circle, Arc, Finish or a kind of a constraint. This amends ADR-0034 item 8.
2. **No ring.** In a frame with no ring of the sketch, the window holds each button of ADR-0034 item 8, as before.
3. **The order of the frame.** The ring comes before the window of the sketch in the frame, so the window reads the ring
   of the same frame.
4. **No contract change.**

## Consequences

**Good:** each tool shows one time. With one line selected, the buttons take two lines (Extrude, then Delete) in the
place of four, so more rows show on a small display.

**Bad:** the buttons of the window change when the ring shows or hides, for example when a pad opens and closes.

**Next:** the redesign slices that need a change of the contract wait for the owner: the agent proposal arc, the author of
a command and the rollback.

## History

- 2026-11-07 · TASK-0277 · The record, Accepted under the standing order.
