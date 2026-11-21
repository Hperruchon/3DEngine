---
id: 0164
title: A view of a frame of any length gives the curves of the design, the files of a sheet guard each number and each name, and the analysis of a sketch keeps its time
status: Amended
topic: Drawing, sketch
date: 2026-11-18
supersedes: []
superseded-by: []
amends: ['0143', '0144', '0154', '0155', '0156', '0159', '0160']
amended-by: ['0173']
affects:
  - Engine.Core/Drawing/ViewEdges.cs
  - Engine.Core/Drawing/SheetLayout.cs
  - Engine.Core/Sketch/SketchSolver.cs
  - Engine.Core/Export/DxfExport.cs
  - Engine.Core/Export/SvgExport.cs
  - Engine.Tests/Drawing/SheetViewTests.cs
  - Engine.Tests/Sketch/FixedLengthsTests.cs
  - Engine.Tests/Export/DxfExportTests.cs
  - Engine.Tests/Export/SvgExportTests.cs
enforced-by: Engine.Tests/Drawing/SheetViewTests.cs
---

# ADR-0164 — A view of a frame of any length gives the curves of the design, the files of a sheet guard each number and each name, and the analysis of a sketch keeps its time

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-17 (`docs/reviews/2026-11-17-codebase-review.md`) gives the medium findings E211 to E214
and the low findings E215 to E217, and TASK-0400 (P0.162) holds them after P10c. On 2026-11-18 the owner ordered "Start
P0.162". ADR-0162 item 16 makes the frame of whole numbers the pictorial view, and that view needs E211.

In doubles, the only exact unit vectors are the axes: a unit vector whose coordinates have a power of two as denominator
needs a² + b² + c² = 4^k, which has only the trivial answers. So each exact view at a slant has a frame of a length other
than 1, and until this record such a view kept the facets of each curved side (E211). A frame of a length near 1e-200 or
1e200 squared its length into a double, which became 0 or +∞ (E212).

## Decision

1. **A frame of any length (E211, E212; amends ADR-0143 items 2 and 4, ADR-0144 item 1 and ADR-0155 item 5).** The length L
   of the right and the up of a frame is exact when Right · Right is the square of a rational number, as for each frame of
   whole numbers; else it is the double of sqrt(Right · Right). The square never becomes a double: its numerator and its
   denominator move by an even power of two to a quotient near 1, and the root moves back by `ScaleB`. The direction
   toward the eye in doubles comes from the exact direction divided by its largest coordinate. A circle of the design is a
   circle of radius L times its radius on the view: exact when L is exact, else from the double of L. GetSheet divides by
   the same L. The code uses + − × ÷, sqrt and `ScaleB` only.
2. **A number that is not finite (E215; amends ADR-0159 item 6 and ADR-0160 item 7).** The SVG file writes 0 for a number
   that is not finite, as the PDF file does (ADR-0161 item 7). The DXF file takes its numbers from the same function, so no
   letter of "NaN" or "Infinity" reaches its search of an exponent.
3. **The stops of an ellipse in the DXF file (E216; amends ADR-0160 item 4).** The chain of an arc of an ellipse has a stop
   at each quarter turn from its start, up to the quarter that holds its end, as in the PDF file (ADR-0161 item 4). An end
   in the direction of the start, but not equal to it, is a whole turn less the rounding.
4. **A name in the SVG file (E217; amends ADR-0159 item 6).** A character that XML 1.0 does not permit is U+FFFD: a
   character of control other than the tab, the line feed and the return, U+FFFE, U+FFFF and a surrogate with no pair. The
   rules of a name do not change, so each log that is valid today stays valid.
5. **The time of the analysis (E213; amends ADR-0154 item 4).** With several right sides, the back-substitution of the
   analysis takes the entries of each pivot row that are not zero, found one time. With one right side, the solve of the
   solver and of a replay, it takes each entry as before, so it keeps its bits. A test compares the fixed lengths of a stair
   of 600 lines with 1,000 constraints with its status on one computer: at most six times the status and 100 ms. ADR-0173
   item 4 (TASK-0435): the test compares the products of two numbers, not two times; the analysis computes at most six
   times the products of the status.
6. **The limit of joined sizes (E214; amends ADR-0156 item 1).** The scale of the rows gives the right answer for parts of
   a sketch that no constraint joins. When one constraint joins two sizes in a ratio above about 1e5, for example a
   parallel between a line of 1e-6 mm and a line of 1 mm, the freedom of the status can be wrong by one. The fixed lengths
   were right in the probe of the review. A scale of the columns would also change the test of a fixed length, so it waits
   for a need.
7. **R-0068 closes.** The test of item 1 adds a view of the frame of whole numbers right (2, 2, −1), up (−1, 2, 2) and toward
   the eye (2, −1, 2), and it gives the ellipses of a cylinder with their exact half axes. That is the exit of the entry
   (ADR-0162 item 16).

## Consequences

**Good:** each exact view, at a slant or not, shows the circles and the ellipses of the design, a pictorial view exists, and
an agent cannot make a view whose read fails. The files of a sheet have one defined answer for each number and each name.
A sketch of 600 lines keeps its analysis near the time of its status.

**Bad:** a circle of a frame whose length is not rational has a radius from a double, not an exact one. The freedom of a
sketch whose constraint joins sizes in a ratio above about 1e5 can still be wrong by one, as item 6 states.

## History

- 2026-11-18 · TASK-0400 · The record, for the findings E211 to E217 of the codebase review of 2026-11-17.
- 2026-11-18 · TASK-0400 · The number is 0164, because P0.164 took 0163 first; shipped at v0.388.
- 2026-11-21 · TASK-0435 · Amended by ADR-0173 item 4: the test of item 5 compares the products of two numbers of the
  analysis and of the status, not two times (run 44866049139).
