---
id: 0148
title: A line that closes a chain of the design shows its reference in parentheses, and each value keeps its row in each view
status: Accepted
topic: Clients, drawing
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0146']
amended-by: []
affects:
  - 3DEngine.App/EdgeValues.cs
  - 3DEngine.App/Ui/DesktopUi.Values.cs
  - 3DEngine.App/Ui/DesktopUi.Rows.cs
  - 3DEngine.ImageTests/ChainReferenceTests.cs
  - 3DEngine.ImageTests/FlatViewValuesTests.cs
enforced-by: 3DEngine.ImageTests/ChainReferenceTests.cs
---

# ADR-0148 — A line that closes a chain of the design shows its reference in parentheses, and each value keeps its row in each view

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row R22b (ADR-0146 item 2, ADR-0118 item 5): a chain of n parts gets n − 1 values of the design and one reference in
parentheses. The renders of 2026-11-10 give the case: the total 60 and the parts 15 and 20 are values of the design, and the
part 25 is a reference, "(25.000)", computed from the design values, which a person cannot edit (ISO 129-1).

ADR-0146 item 1 (R22a) removed a value whose length is not true from the list of `EdgeValues.Of`. The rows of the body read
the same list, so in a flat view the rows lost those values too. The render of the phone says that the list gives each value
of the design.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **Each value keeps its row (amends ADR-0146 item 1).** A value whose length the flat view does not show true stays in
   the list with the flag `InView` false: it keeps its row and has no chip.
2. **The reference of a chain.** In a flat view a line of the sketch of the selected extrude with no value of its own, across
   the view, closes a chain when, along its direction, it and the lines with a value tile exactly the interval of one other
   line with a value, the total: each part one after the other, with no gap and no overlap, within 1e-6 of its length. A line
   whose interval is the interval of the total is not a part. The reference is the total less the sum of the parts, from the
   values of the design, as "(20.000 mm)", on the edge of the line. It has a chip, no row and no pad. With perspective no
   reference shows.
3. **What stays.** A value between two points that are not the ends of one line has no chip yet, so a chain whose parts are
   such values, as the place of a pocket on the Bracket, gives no reference. A line whose length is fixed by a value that is
   not a length, as an angle, gives no reference.
4. **No contract change.** The values and the references are presentation (ADR-0007).

## Consequences

**Good:** a flat view of a stepped profile shows the length of each step: the values of the design, and the one step that
they fix by the total, in parentheses, so a drawing has no gap and no extra value.

**Bad:** a chain of the Bracket, whose parts are places of a pocket, waits for a value between two points.

## History

- 2026-11-14 · TASK-0383 · The record, from row R22b and the renders of 2026-11-10, under the order of the owner of
  2026-11-14.
- 2026-11-14 · TASK-0383 · Shipped at v0.368.
