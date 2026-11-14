---
id: 0146
title: In a flat view a value shows only where its length is true, and row R22 splits into four slices
status: Accepted
topic: Clients, drawing
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0082']
amended-by: []
affects:
  - 3DEngine.App/EdgeValues.cs
  - 3DEngine.ImageTests/FlatViewValuesTests.cs
enforced-by: 3DEngine.ImageTests/FlatViewValuesTests.cs
---

# ADR-0146 — In a flat view a value shows only where its length is true, and row R22 splits into four slices

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row R22 of the roadmap gives the right dimensions on a flat view (ADR-0118 item 5): each value of the design one time, in a
view where its length is true; a chain with one reference in parentheses; a separate form for a size that no constraint
fixes and for a lost value; and the place of each dimension proposed by the agent. It estimates three to five sessions.

The window shows the values of the selected extrude on its edges (ADR-0082, ADR-0115). In a flat view it shows each value,
also a value whose edge is along the view: from the front, the width of a plate along y sits on an edge that is a point, so
its number is not the length that the view shows.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **The true length (amends ADR-0082 item 1).** In a flat view a value shows only where its length is true. The edge of a
   length or of the distance of an extrude is across the direction of the view: the part of its direction along the view is
   at most 0.001 of its length. The circle of a radius faces the view: the normal of the plane of its edge is along the view
   within the same limit, whatever the chord of its segments. With perspective each value shows, as before.
2. **The slices of R22 (answered by the standing order of 2026-11-14, the recommended option).** R22 splits by the rules of
   ADR-0118 item 5, each one a slice after the one before it:
   - R22a: the true length (item 1);
   - R22b: a chain of n parts gives n − 1 values and one reference in parentheses;
   - R22c: a size that no constraint fixes and a lost value show in a separate form, from a search of the free parts of a
     sketch in the solver;
   - R22d: the agent proposes the place of each dimension, and the person accepts or rejects it (ADR-0092).
3. **No contract change.** The values are presentation (ADR-0007).

## Consequences

**Good:** a flat view shows only the numbers that its lines measure, so a value that a person reads is the length that the
view shows.

**Bad:** a value whose edge is at a slant to each flat view shows in no flat view; it shows with perspective. A drawing of the
sheet (P10) needs its own rule for such a value.

## Alternatives considered

- **Each value in each view, grey where its length is not true.** Refused: a drawing shows a value one time, where it is
  true (ADR-0118 item 5).

## History

- 2026-11-14 · TASK-0381 · The record, from row R22 and ADR-0118 item 5, under the order of the owner of 2026-11-14.
