---
id: 0196
title: CheckTravel moves a slider from limit to limit by one exact sweep, the first vertex on a face or edge on an edge
status: Accepted
topic: Kernel, queries, assembly
date: 2026-11-28
supersedes: []
superseded-by: []
amends: ['0195']
amended-by: []
affects:
  - Engine.Core/Queries/CheckTravelQueryHandler.cs
  - Engine.Tests/Queries/CheckSlideTests.cs
enforced-by: Engine.Tests/Queries/CheckSlideTests.cs
notes: Phase K12b, TASK-0491. The standing order of the owner of 2026-11-28 answers the choices of this ADR with the recommended option; no change of Engine.Contracts.
---

# ADR-0196 — CheckTravel moves a slider from limit to limit by one exact sweep, the first vertex on a face or edge on an edge

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K12 asks for one exact sweep of a slider. ADR-0195 gave the travel of a pivot (K12a) and left the slider to K12b. A slider
moves the child and the bodies under it along the normal of the parent connector, with no turn, so the motion of each point is a
straight line, and the first contact is an exact value.

## Decision

1. **The query (amends ADR-0195 item 1).** `CheckTravel` also takes a slider with limits. The bodies, the exempt pairs and the
   refusals are those of ADR-0195 item 2; a slider with no limits gives `E-CHECK-TRAVEL-INVALID`.
2. **The sweep.** The bodies that move go along the normal n of the parent connector in the Document (the doubles of the placed
   frame, read as exact rationals), from the lower to the upper distance. Two polyhedra that are apart first touch at a vertex of
   the one on a face of the other, or at an edge of the one on an edge of the other: an edge on a face also touches at the edges
   of that face first, because the tests take the borders. So the first contact is the smallest distance of these events, each
   the root of one linear equation in rational numbers (a vertex and a plane, or Cramer's rule for two edges and n). A face or an
   edge parallel to n gives no event of its own; a vertex event at its border holds it. The value at the lower limit also finds
   an overlap, as in ADR-0195 item 3.
3. **The answer and the work.** `TravelInfo` gives the contact as the distance of the slider, `samples` 1, `radius` 0 and the gap
   0; the tolerance is not used, because the sweep is exact. One travel tests at most the work of `CheckInterference`
   (ADR-0182 item 8), counted as the vertices of the one body times the faces of the other, both ways, and the vertices of the
   one times the vertices of the other for the edges; above it the query gives `E-CHECK-WORK-LIMIT`.

## Alternatives considered

- **Samples, as for a pivot.** A straight motion has an exact first contact, so a sample would only lose it.
- **A swept solid and a boolean.** The Minkowski sum of a solid that is not convex and a segment needs a split into convex pieces;
  the events need none.

## Consequences

**Good:** the travel of a slider is exact, and it gives the distance of the first contact with no tolerance.

**Bad:**
- The work of a pair grows with the product of the counts of the features, so a body with many facets reaches the limit soon.
- The direction comes from the doubles of the placed frame, so the distance has the rounding of that direction.

## History

- 2026-11-28 · TASK-0491 · The record, Accepted under the standing order of the owner of 2026-11-28. Amends ADR-0195 item 1.
