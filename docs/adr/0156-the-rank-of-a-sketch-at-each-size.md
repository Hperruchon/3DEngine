---
id: 0156
title: The rank of a sketch comes from the rows of its Jacobian at unit length, so the freedom and the fixed lengths do not depend on its size
status: Accepted
topic: Sketch
date: 2026-11-15
supersedes: []
superseded-by: []
amends: ['0035', '0154']
amended-by: []
affects:
  - Engine.Core/Sketch/SketchSolver.cs
  - Engine.Tests/Sketch/FixedLengthsTests.cs
enforced-by: Engine.Tests/Sketch/FixedLengthsTests.cs
---

# ADR-0156 — The rank of a sketch comes from the rows of its Jacobian at unit length, so the freedom and the fixed lengths do not depend on its size

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0035 item 8 gives `freedom` from the rank of the Jacobian J, and ADR-0154 item 1 finds a fixed length with the same
elimination. A pivot is zero when it is at most 1e-12 of the largest diagonal entry of J·Jᵀ. A row of a perpendicular or a
parallel constraint has no unit and goes as 1 / L for a line of the length L; a row of a length has no such factor. The
codebase review of 2026-11-15 (finding E207, low) found that a rectangle of 1e5 mm and one of 0.001 mm in one sketch gave a
wrong fixed length and a wrong freedom; each rectangle alone was correct.

On 2026-11-15 the owner ordered the run of TASK-0391, P10.2 and TASK-0392.

## Decision

1. **Rows at unit length (amends ADR-0035 item 8 and ADR-0154 item 1).** The status and the analysis of `GetSketch` divide
   each row of J with a value other than zero by its length before the elimination. A scale of a row keeps the space of the
   rows, so the rank and the test of a fixed length keep their meaning, and a part of the sketch with a size of 1e-6 or 1e5
   gives the same answer as at a size of 1. The code uses + − × ÷ and sqrt only.
2. **The solver keeps its rows.** The steps of the solve use the rows of the equations as before, so a replay gives the same
   positions and `solved` does not change.
3. **No contract change.** The fields of `GetSketch` keep their meaning.

## Consequences

**Good:** the freedom and the fixed lengths of a small part next to a large one are correct.

**Bad:** each read of a sketch does one more pass over J. A row that is not finite keeps its values, as before.

## History

- 2026-11-15 · TASK-0392 · The record, from finding E207 of the codebase review of 2026-11-15.
