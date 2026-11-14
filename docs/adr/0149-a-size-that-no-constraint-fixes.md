---
id: 0149
title: The solver finds the lengths that the constraints fix, and a flat view shows a length that no constraint fixes in amber
status: Accepted
topic: Kernel, clients, drawing
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0146', '0148']
amended-by: []
affects:
  - Engine.Core/Sketch/SketchSolver.cs
  - Engine.Core/Queries/GetSketchQuery.cs
  - Engine.Core/Queries/GetSketchQueryHandler.cs
  - Engine.Tests/Sketch/FixedLengthsTests.cs
  - 3DEngine.App/EdgeValues.cs
  - 3DEngine.App/Ui/DesktopUi.Values.cs
  - 3DEngine.App/Ui/DesktopUi.Rows.cs
  - 3DEngine.ImageTests/FreeSizeTests.cs
  - 3DEngine.ImageTests/ChainReferenceTests.cs
  - 3DEngine.ImageTests/FlatViewValuesTests.cs
enforced-by: 3DEngine.ImageTests/FreeSizeTests.cs
---

# ADR-0149 — The solver finds the lengths that the constraints fix, and a flat view shows a length that no constraint fixes in amber

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row R22c (ADR-0146 item 2, ADR-0118 item 5): a size that no constraint fixes and a lost value show in a separate form, so
the drawing does not hide a gap in the design. The renders of 2026-11-10 show a size that is not fixed in amber, with a
dashed line and "not fixed". The solver gives the count of the free degrees (ADR-0035 item 8) and does not say which length
is free.

The reference of a chain (ADR-0148 item 2) takes the total less the parts. When the design does not fix the line of the
reference, that number is not a fact of the design: an L with no direction constraint can lean.

A lost value is a dimension of a sheet whose constraint left the design. No sheet exists before P10.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **The fixed lengths.** `SketchSolver.FixedLengths` gives the lines of a sketch whose length the constraints fix at the
   positions of the record. A length is fixed when its gradient lies in the space of the rows of the Jacobian of the
   constraints: the rank of the Jacobian with the gradient as one more row is the rank of the Jacobian, by the elimination
   and the limit of a pivot of the solver. A line whose two ends are fixed points is fixed. `GetSketch` gives them in the
   field `fixedLengths`, in the order of the lines. The code uses + − × ÷ and sqrt only.
2. **A size that no constraint fixes (amends ADR-0146 item 2).** In a flat view a line of the sketch of the selected extrude
   with no value of its own, across the view, whose length the design does not fix, shows the length of the line in the
   sketch, as "20.000 mm not fixed", in amber with a dashed border. It has a chip, no row and no pad; the person or the agent
   adds the constraint.
3. **A reference needs a fixed line (amends ADR-0148 item 2).** A line closes a chain only when the design fixes its length.
4. **The lost value waits for P10.** A lost value has a meaning on a sheet, so it comes with the sheet.
5. **No contract change.** `SketchInfo` is in `Engine.Core`, and the field is added at the end of the result of `GetSketch`.

## Consequences

**Good:** a flat view shows each gap of the design where it is, and a reference only where the design fixes it.

**Bad:** the test of the rank uses the limit of a pivot of the solver; a length near the limit can be called fixed or free.
A free size of a point between two lines that is not a line has no chip.

## History

- 2026-11-14 · TASK-0384 · The record, from row R22c and the renders of 2026-11-10, under the order of the owner of
  2026-11-14.
- 2026-11-14 · TASK-0384 · Shipped at v0.369.
