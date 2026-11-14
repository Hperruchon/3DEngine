---
id: 0145
title: A line of a curved side keeps its place along the axis, an axis within 1e-9 of the view is along it, and Frame leaves a window at the right
status: Accepted
topic: Kernel, drawing, clients
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0140', '0143', '0144']
amended-by: []
affects:
  - Engine.Core/Drawing/ViewEdges.cs
  - Engine.Tests/Drawing/ViewPlaceTests.cs
  - 3DEngine.App/Ui/DesktopUi.Views.cs
  - 3DEngine.ImageTests/FrameFreePartWindowTests.cs
enforced-by: Engine.Tests/Drawing/ViewPlaceTests.cs
---

# ADR-0145 — A line of a curved side keeps its place along the axis, an axis within 1e-9 of the view is along it, and Frame leaves a window at the right

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-14 (e) gave three high findings and one medium finding:

- E202: the outline of a cylinder or a cone that a Subtract cut runs to the ends of the uncut side.
- E203: a cylinder that a Rotate of 180 degrees turned makes the view throw when it moves, and gives arcs that do not join.
- E204: an edge of a cut side goes onto the circle of an end.
- V191: Frame puts the bodies under the window of the live sketches at the right.

E202 to E204 have one cause: the circles and the outlines of ADR-0143 and ADR-0144 use the lines on the view only. The fix
of E202 and E203 gives each line its place along the axis. With the rule "the ends only", each rim of a hole drops its circle,
because a rim is a cut of the cylinder at its middle. So the fix of E204 is in the same change.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **The place along the axis (amends ADR-0143 items 2 and 3 and ADR-0144 items 1 and 2).** Each line of a view keeps the
   exact ends in the world of its piece of K9a. The place of a point along the axis of a side is h = (p − Base) · Axis /
   (Axis · Axis), exact. A line whose two ends have one place h is on the circle of the side at Base + h Axis, with the
   radius R for a cylinder and R (1 − h) for a cone; it is an arc of that circle, of its ellipse at a slant, or one line
   across the view. Each other line, and a line at the apex, keeps its facets. A piece of the outline goes onto the line of
   the outline of the design at the place of each of its ends, so the outline of a cut side ends where the side ends.
2. **An axis along the view (amends ADR-0143 item 4 and ADR-0144 item 4).** An axis is along the view when the sine of the
   angle between them is at most 1e-9: exactly, or within the rounding of a turn from a quaternion in doubles. Its lines are
   circles with the centre of the projection of the base, and the side has no outline. A whole visible circle is drawn one
   time, and each other arc of that circle lies under it and is not drawn (ADR-0142 item 5).
3. **Frame and a window at the right (amends ADR-0140 item 3).** A tall window at the right edge of a landscape display, as
   the window of the live sketches, cuts the free part as the panel at the left does, 16 pixels from it.
4. **No contract change.** The lines and the free part are internal.

## Consequences

**Good:** a hole shows its walls from face to face and its rims as circles of its radius; a cut cone shows the circle of
its cut; a cylinder turned by a quaternion gives one circle; Frame leaves the window of the live sketches.

**Bad:** a tolerance of 1e-9 is one more rule; an axis at an angle of less than 1e-9 by design is drawn as along the view.

## Alternatives considered

- **The ends only, and the facets for each cut.** Refused: each rim of a hole lost its circle.
- **The end of a piece by its nearer ellipse (ADR-0144 item 1).** Refused: a rounding of 1e-16 decided the end (E203).

## History

- 2026-11-14 · TASK-0379 · The record, from the findings E202 to E204 and V191 of the codebase review of 2026-11-14 (e),
  under the order of the owner of 2026-11-14.
