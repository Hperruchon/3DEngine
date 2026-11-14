---
id: 0142
title: The visible and the hidden edges of a view are computed outside the owned kernel, from its faces and its edges
status: Accepted
topic: Kernel, drawing
date: 2026-11-14
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - Engine.Core/Drawing/ViewEdges.cs
  - Engine.Tests/Drawing/ViewEdgesTests.cs
enforced-by: Engine.Tests/Drawing/ViewEdgesTests.cs
---

# ADR-0142 — The visible and the hidden edges of a view are computed outside the owned kernel, from its faces and its edges

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K9 of the roadmap gives the edges of a view: the visible edges and the hidden edges of the live bodies seen along a
direction, from the exact faces of the owned kernel (ADR-0118 item 2). R22 puts the values of the design on these lines, and
P10 puts them on a sheet. ADR-0118 item 6 says that a view of the edges is not one of the six operations of the closed
feature boundary of the charter, so K9 needs an ADR that adds it, or that computes it outside the kernel from the faces that
the kernel gives.

The owned kernel holds exact planar polygons with rational coordinates (ADR-0057). A curved side is a set of facets with one
tag, and `Brep.Of` joins the polygons of one plane and one tag into a face with its loops and its edges (ADR-0057 item 6).
The purpose of the kernel is identity, not computation (charter, "The kernel boundary").

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question. This record takes
the recommended option of ADR-0118 item 6 under that order; the owner can change it.

## Decision

1. **Outside the kernel (ADR-0118 item 6, answered by the standing order of 2026-11-14).** `Engine.Core` computes the lines
   of a view from the faces and the edges of `Brep`. The six operations of the charter stay, and the charter does not
   change. The lines are presentation, as a tessellation is: no command, no event and no Document holds them (ADR-0007,
   ADR-0118 item 3).
2. **The view.** A view is three exact directions: the right, the up, and the direction toward the eye. The right and the up
   are at a right angle with one length, and right × up points toward the eye, so a view has one scale and no shear. The six
   standard views have the axes of the flat views of the window (ADR-0137 item 1): Front from negative y with z up, Back from
   positive y, Right from positive x, Left from negative x, Top from above with y up, and Bottom from below.
3. **The lines.** An edge between faces of two tags is a line. An edge between two faces of one tag is a line, an outline,
   when one face looks toward the eye and the other does not. An edge between two facets of one tag that look the same way
   is no line. An edge along the direction of the view is a point, and it is no line.
4. **The visibility, exact.** Each line is cut at each place where another edge crosses it or lies on it. A piece is hidden
   when the middle of it is inside a face that looks toward the eye or on its border, and that face is strictly nearer to
   the eye there. The border counts, because a point behind a seam between two facets of a curved side is on the border of
   each facet; an edge of the face itself has the same depth, so it is not covered. Only a face that looks toward the eye
   covers a point: a ray from a point to the eye leaves each closed solid that it crosses through such a face. Each test
   uses + − × ÷ and a comparison on rational numbers. A filter of rounded values goes first; rounding to the nearest keeps
   the order, so the filter rejects only what the exact test rejects.
5. **One line, one time.** Pieces with the same ends are one line: a visible piece goes before a hidden one, and the nearest
   to the eye goes first. A hidden line under a line is not drawn. The pieces of one edge that follow each other with one
   visibility are one line again. Each line keeps its body and the tags of the two faces of its edge, so R22 can find the
   values of the design that it shows.
6. **The live bodies.** The lines of a Document come from the solid of each live body in the order of its handle
   (`KernelProjection.OfRecord`, as the STEP export reads it, ADR-0070). A mesh body of an STL file (ADR-0083 item 4) and a
   body with a fault give no lines; the result names each one with its reason.
7. **The slices.** K9a gives items 1 to 6, with the outline of the facets of a curved side. K9b gives the circle or the arc
   of a curved side in place of its facets, and the outline of a cylinder, a cone and a sphere from its axis and its radius.
   No client reads the lines before R22.
8. **No contract change.** The lines are internal to `Engine.Core`. R22 or P10 exposes them, with its own ADR when it
   changes the public shape of `Engine.Contracts`.

## Consequences

**Good:** the boundary of the charter stays closed. A view is exact on each platform, and the coincident edges of a flat view
(the back edges of a box under its front edges) give one line with no tolerance.

**Bad:** in K9a a curved side gives the outline of its facets and an end of 64 short lines, not a circle. The time grows with
the square of the count of edges: on the computer of the owner a cylinder takes about 0.15 s, and a sphere or a torus of the
primitives about 2 s. Bodies that cross each other with no boolean give no line where they cross.

## Alternatives considered

- **A seventh operation of the kernel.** Refused: the kernel owns identity, and a view is presentation that reads the faces.
  A seventh operation changes the charter for no gain in identity.
- **Lines from the tessellation, with a tolerance.** Refused: the coincident edges of a flat view need an exact test, and a
  drawing must show the exact numbers of the design (ADR-0118 item 5).
- **The depth buffer of the window.** Refused: the window is presentation of the render side, and the lines are for the
  sheet of the design side (ADR-0009).

## History

- 2026-11-14 · TASK-0375 · The record, from row K9 and ADR-0118 item 6, under the order of the owner of 2026-11-14.
- 2026-11-14 · TASK-0375 · Shipped at v0.360.
