---
id: 0144
title: An end at a slant to the view is an ellipse, and the outline of a cylinder and a cone comes from its axis
status: Accepted
topic: Kernel, drawing
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0143']
amended-by: []
affects:
  - Engine.Core/Drawing/ViewEdges.cs
  - Engine.Tests/Drawing/ViewEllipsesTests.cs
enforced-by: Engine.Tests/Drawing/ViewEllipsesTests.cs
---

# ADR-0144 — An end at a slant to the view is an ellipse, and the outline of a cylinder and a cone comes from its axis

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0143 (K9b) gives the circle of the design for an end whose axis is exactly along the view, and one line for an end whose
axis is exactly across it. Item 4 of ADR-0143 keeps the facets for each other end, for a body that a quaternion in doubles
turned, and for the outline of a cylinder and a cone. Row K9c of the roadmap gives them. The charter lists the ellipse as one
of the three curves.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **The ellipse of an end (amends ADR-0143 item 4).** With the frame of length 1, an end of a cylinder or a cone whose axis
   is neither exactly along nor exactly across the view is an ellipse. With a the unit axis and e the unit direction toward
   the eye, P = a × e over its length and Q = a × P lie on the plane of the end, so on the view the half axes are Major = R P
   and Minor = R Q, at a right angle, with R the radius of the design. The centre is the exact projection of the centre of
   the end. A cylinder has two ends; a piece goes to the end whose ellipse is nearer to its first point. The arcs of one
   ellipse follow the rules of the arcs of a circle (ADR-0143 item 2).
2. **The outline from the axis (amends ADR-0143 item 4).** When the axis is not along the view, the outline of a cylinder is
   the two lines from C0 + s Major to C1 + s Major, for s = 1 and −1. The outline of a cone is the two lines from its apex to
   the points where a tangent from the apex meets the ellipse of its base: with the apex q in the coordinates of the
   ellipse and ρ² = |q|² > 1, the points are (q ± √(ρ² − 1) q⊥) / ρ². Each piece of the outline of the facets goes to the
   nearer line: each end goes to its place along it, and the first end and the last end of the line go to its two ends.
   When the apex is inside the ellipse, the cone keeps the outline of its facets.
3. **The arithmetic.** The half axes and the outline use + − × ÷ and the square root on doubles. These operations give one
   result on each platform (CLAUDE.md, determinism rules), and the lines are presentation, so they do not enter the log.
   The visibility stays exact (ADR-0142 item 4).
4. **What stays.** The outline of a sphere is its circle (ADR-0143 item 2). A torus keeps its facets. An end whose axis is
   along the view only within the rounding of a turn is an ellipse with two half axes of almost one length.

## Consequences

**Good:** each end of a turned cylinder or cone is one ellipse with the radius of the design, and its outline is at the
radius of the design from its axis, so R22 can use them.

**Bad:** an interior cut of an outline goes to its place along the line by a projection, so it is on the line of the design
but at the place of the facets. An end at a slant is an ellipse also when the turn is a small error of rounding.

## Alternatives considered

- **An angle of the tangent from atan2 and acos.** Refused: a transcendental function differs between platforms, and the
  square root gives the same points.

## History

- 2026-11-14 · TASK-0377 · The record, from row K9c and ADR-0143 item 4, under the order of the owner of 2026-11-14.
