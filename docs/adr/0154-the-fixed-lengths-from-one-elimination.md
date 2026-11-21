---
id: 0154
title: The status and the fixed lengths of a sketch come from one elimination, and the product J·Jᵀ takes the columns with a value
status: Amended
topic: Kernel
date: 2026-11-15
supersedes: []
superseded-by: []
amends: ['0149']
amended-by: ['0156', '0164', '0173']
affects:
  - Engine.Core/Sketch/SketchSolver.cs
  - Engine.Core/Queries/GetSketchQueryHandler.cs
  - Engine.Tests/Sketch/FixedLengthsTests.cs
enforced-by: Engine.Tests/Sketch/FixedLengthsTests.cs
---

# ADR-0154 — The status and the fixed lengths of a sketch come from one elimination, and the product J·Jᵀ takes the columns with a value

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0149 item 1 finds a fixed length by the rank of the Jacobian J with the gradient g of the length as one more row. The code
of R22c built J·Jᵀ with that row and eliminated it in full for each line. `GetSketch` runs it, and the window reads the active
sketch at each tap and in each frame while the pointer is over the window. The codebase review of 2026-11-15 measured 23 ms
for a sketch of 25 lines and 5.5 s for 100 lines (finding E206, high).

The product J·Jᵀ summed each column, and a row of J has a value in a few columns only. The status of a stair of 100 lines took
28 ms before this record.

On 2026-11-15 the owner ordered TASK-0391 for E206.

## Decision

1. **One elimination (amends ADR-0149 item 1).** The test of a fixed length stays: g lies in the space of the rows of J, at the
   limit of a pivot of the solver. The code eliminates J·Jᵀ once, with J·g of each line as one more column of the right side,
   and gets λ with J·Jᵀ·λ = J·g. The residual r = g − Jᵀ·λ is the part of g outside the space of the rows of J, and r·r is
   the last pivot that the elimination with g as one more row gives. The length is fixed when r·r is at most PivotRatio times
   the larger of the largest diagonal entry of J·Jᵀ and g·g, which is the limit of that pivot.
2. **One analysis for `GetSketch`.** The query takes the status of ADR-0035 item 8 and the fixed lengths from the same
   elimination.
3. **The product takes the columns with a value.** Each entry of J·Jᵀ sums, in the order of the columns, the columns where
   both rows have a value. When each entry of J is finite, this gives the same bits as the sum over each column: a term with a
   zero factor is +0 or −0, and the sum is never −0. A Jacobian with an entry that is not finite takes the sum over each
   column. The solver uses the same product, so a replay gives the same bits as before this record.
4. **A test of the time.** The status and the fixed lengths of a stair of 100 lines take less than 1 s. On the computer of the
   owner they take about 9 ms. ADR-0164 item 5: the fixed lengths of a stair of 600 lines take at most six times its status
   and 100 ms, with a back-substitution on the entries that are not zero. ADR-0173 item 4 (TASK-0435): no time decides the
   test. For each stair the analysis computes at most six times the products of two numbers of the status. TASK-0438 (finding
   T187, Q7 A of the second codebase review of 2026-11-21): a slower status keeps that ratio, so the status of each stair
   also computes at most 1.5 times its count of 2026-11-21, 31,289 products for 100 lines and 1,103,894 for 600 lines.
5. **No contract change.** The result of `GetSketch` keeps its fields and their meaning.

## Consequences

**Good:** a read of a sketch of 100 lines takes less than one frame at 60 Hz, and the field `fixedLengths` keeps its meaning.

**Bad:** the residual and the old pivot are equal in exact arithmetic only; a length near the limit can change from fixed to
free, as ADR-0149 says of the old test. The test of the time can fail on a very slow computer.

## History

- 2026-11-15 · TASK-0391 · The record, from finding E206 of the codebase review of 2026-11-15, under the order of the owner of
  2026-11-15.
- 2026-11-15 · TASK-0391 · Shipped at v0.376.
- 2026-11-15 · TASK-0392 · ADR-0156 item 1 amends item 1: the elimination takes the rows of J at unit length.
- 2026-11-18 · TASK-0400 · Amended by ADR-0164 item 5: the back-substitution of several right sides takes the entries that are not zero (finding E213).
- 2026-11-21 · TASK-0435 · Amended by ADR-0173 item 4: the test of item 4 compares the products of two numbers of the analysis
  and of the status, not two times (run 44866049139).
- 2026-11-21 · TASK-0438 · Item 4: a bound on the count of the products of the status of each stair, beside the ratio
  (finding T187, Q7 A).
