---
id: 0143
title: A view shows the circle of the design for a curved side, and an end seen from the side as one line
status: Amended
topic: Kernel, drawing
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0142']
amended-by: ['0144', '0145']
affects:
  - Engine.Core/Drawing/ViewEdges.cs
  - Engine.Core/Features/KernelProjection.cs
  - Engine.Tests/Drawing/ViewCurvesTests.cs
enforced-by: Engine.Tests/Drawing/ViewCurvesTests.cs
---

# ADR-0143 — A view shows the circle of the design for a curved side, and an end seen from the side as one line

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0142 (K9a) gives the visible and the hidden lines of a view from the faces of the owned kernel. A curved side is a set
of facets with one tag, so from the top the end of a cylinder is 64 short lines, and from the front it is 32 short lines on
one line. Row K9 asks for the circle or the arc of a curved side and not its facets, and R22 needs the radius of the design
on the line. ADR-0142 item 7 gives this to K9b.

The kernel holds no curve: the radius and the centre are in the commands. The facets come from a turn in doubles (ADR-0040
item 2), so a vertex at a quarter turn is not exactly on an axis.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **The curves of the design (amends ADR-0142 item 7).** `KernelProjection.Curves` walks the commands of a live body as
   the walk of its solid does, and gives the curve of each curved side under the tag of its faces: for a cylinder or a cone
   the centre of its base, the axis to the other end or to the apex, and the radius; for a sphere the centre and the radius.
   CreateCylinder, CreateCone, CreateSphere, and each circle and each arc of the sketch of an extrude give a curve. A
   Translate moves it, a Rotate turns it with the exact turn of the solid, and a Subtract keeps the curves of the two
   bodies. A torus gives no curve.
2. **A circle along the view (amends ADR-0142 item 5).** When the frame has a length of 1 and the axis of a cylinder or a
   cone is exactly along the direction of the view, each piece of an end is an arc of the circle of the design, from its
   first end to its second end counter-clockwise on the view. Each piece of the outline of a sphere is an arc of its circle
   too. The arcs of one circle, one body, one visibility and one pair of tags that follow each other are one arc; a closed
   chain is the whole circle, with its two ends at one point. The centre and the radius are exact; the ends of an arc are
   the exact ends of the pieces of K9a, so they are on the facets, within the rounding of the turn.
3. **An end from the side.** When the axis is exactly across the direction of the view, the pieces of an end lie on one
   line; the pieces of one line, one visibility and one pair of tags that follow each other are one line.
4. **What stays.** An end whose axis is neither along nor across the view is an ellipse: it keeps its facets. A frame of
   another length keeps the facets, because the radius of the design is not the radius on that view. A turn from a
   quaternion in doubles is seldom exactly along an axis, so a turned cylinder keeps its facets too. The outline of a
   cylinder and a cone stays the outline of its facets.
5. **No contract change.** The curves and the circles are internal to `Engine.Core`, as the lines of ADR-0142 are.

## Consequences

**Good:** from the top a hole or a cylinder is one circle with its radius of the design, and from the front it is one
rectangle, so R22 can put the value of the design on the circle.

**Bad:** an ellipse and a turned body keep their facets. An end of an arc is on the facets, not on the circle, by up to the
sagitta of one facet.

## Alternatives considered

- **A circle fitted to the vertices of the facets.** Refused: the vertices are rounded, so the fit needs a tolerance, and
  the radius would not be the value of the design.
- **Curves in the owned kernel.** Refused: the kernel holds identity, and the charter keeps its surfaces exact by a closed
  list; a curve of a drawing is presentation (ADR-0142 item 1).

## History

- 2026-11-14 · TASK-0376 · The record, from row K9b and ADR-0142 item 7, under the order of the owner of 2026-11-14.
- 2026-11-14 · TASK-0376 · Shipped at v0.361.
- 2026-11-14 · TASK-0377 · ADR-0144 items 1 and 2 amend item 4: an end at a slant to the view is an ellipse, and the
  outline of a cylinder and a cone comes from its axis.
- 2026-11-14 · TASK-0379 · ADR-0145 items 1 and 2 amend items 2 to 4: a line has its place along the axis, and an axis within
  1e-9 of the view is along it.
