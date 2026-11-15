---
id: 0147
title: An end within 1e-9 of across the view is one line on its diameter
status: Accepted
topic: Kernel, drawing
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0143', '0145']
amended-by: []
affects:
  - Engine.Core/Drawing/ViewEdges.cs
  - Engine.Tests/Drawing/ViewPlaceTests.cs
  - Engine.Tests/Drawing/ViewCurvesTests.cs
enforced-by: Engine.Tests/Drawing/ViewPlaceTests.cs
---

# ADR-0147 — An end within 1e-9 of across the view is one line on its diameter

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-14 (e) gave the low finding E205 and the medium finding T182. E205: a cylinder that turns of
30 and 60 degrees about x laid on its side is across the top view only within 5.9e-15, so each end was a flat ellipse,
and the two halves of the end lay on one line, one of them hidden. T182: the test of the circles of a Document counted its
hidden lines and did not read them. ADR-0145 item 2 gives a tolerance for an axis along the view; an axis across the view
had none.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **An end across the view within the rounding (amends ADR-0143 item 3 and ADR-0145 item 2).** An end is across the view
   when the cosine of the angle between the axis and the view is at most 1e-9. When it is not exactly 0, each piece of the
   end goes onto the diameter of its circle on the view, from C − Major to C + Major at its place h along the axis, by the
   place of each of its ends along Major. Both halves of a flat end lie on that diameter: the union of the visible pieces is
   the visible line, and each part of a hidden piece outside that union, within 1e-9 of the radius, is a hidden line. An
   end exactly across the view keeps the rule of ADR-0143 item 3.
2. **The test reads its lines.** The test of the circles of a Document asserts the place and the ends of its hidden lines.

## Consequences

**Good:** a cylinder that turns laid on its side shows each end as one line, as a cylinder on its side by an exact turn does.

**Bad:** one more tolerance, with the limit of ADR-0145 item 2.

## History

- 2026-11-14 · TASK-0380 · The record, from the findings E205 and T182 of the codebase review of 2026-11-14 (e), under the
  order of the owner of 2026-11-14.
- 2026-11-14 · TASK-0380 · Shipped at v0.366.
