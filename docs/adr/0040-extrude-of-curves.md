---
id: 0040
title: The extrude of a profile with arcs and circles
status: Accepted
topic: Geometry, sketch
date: 2026-10-31
supersedes: []
superseded-by: []
amends: ['0036']
amended-by: []
affects:
  - Engine.Core/Sketch/SketchProfile.cs
  - Engine.Core/Commands/ExtrudeCommandHandler.cs
enforced-by: Engine.Tests/Sketch/CurveProfileTests.cs (TASK-0126 creates it)
---

# ADR-0040 — The extrude of a profile with arcs and circles

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0036 extrudes one closed loop of the lines of a sketch. K2d (TASK-0117) gave the sketch the circle and the arc,
and an extrude refuses a sketch with a curve (`Engine.Core/Sketch/SketchProfile.cs:15`). The reason is determinism:
the backend takes a polygon, and the polygon of an arc needs the points of the arc between its start and its end.
The usual way computes them with a sine and a cosine, and determinism rule 1 forbids a transcendental function in
code that writes to the log. The polygon decides the solid of a command of the log, and a rebuild (ADR-0037,
ADR-0039) computes it again on each platform.

The owner chose this item on 2026-10-31 ("1 3 4"), and asked for the ADR before the code. The owner then said: "work
in autonomy till tomorrow morning use your recomendation when there's question". So the recommended answers Q1 A,
Q2 A and Q3 A below are the answers of the owner, and this record is Accepted.

## Decision

1. **The profile (Q1).** A profile is one of these:
   - one closed loop of lines and arcs: each point that a line or an arc uses as an end is the end of exactly two
     edges, and the edges form one cycle. The centre of an arc is not a point of the loop;
   - one circle, with no other line and no other arc.
   The other rules of ADR-0036 item 2 hold for the polygon of item 2: no two segments that share no vertex cross or
   touch, the area is not zero, and the polygon turns counter-clockwise.
2. **The polygon of an arc (Q2).** The engine computes each vertex with `+ - * / sqrt` and comparisons only:
   - With c the centre, s the start and e the end, r = |s − c|, a = (s − c) / r and b = (e − c) / |e − c|, the sweep
     θ from a to b counter-clockwise (ADR-0033 item 2) has cos θ = dot(a, b) and sin θ = cross(a, b). An end in the
     direction of the start gives θ = 2π.
   - The step is θ / 2ᵏ, for the smallest k that gives a step whose sine is not negative and whose cosine is at least
     0.9951, the cosine of about 5.67 degrees. The number is a little below cos(2π / 64), so the count of a full turn,
     64, does not depend on the last bit of its six halvings. The half of an angle φ in (0, 2π] has
     sin(φ/2) = √((1 − cos φ) / 2), which is never negative, and cos(φ/2) = √((1 + cos φ) / 2) with the sign of
     sin φ; for φ = 2π the half is (−1, 0).
   - Vertex i + 1 is vertex i turned about c by the step: (x cos − y sin, x sin + y cos). The last vertex is e
     itself, so an arc ends at its point.
   - A circle starts at c + (r, 0) and has the step 2π / 64 after six halvings of 2π.
3. **The bits.** The polygon depends on the record only, and each operation gives one result on each platform, so
   a replay and a rebuild give the same solid on Windows, Linux and macOS, on x64 and arm64. A bit test of the
   polygon of an arc and of a circle runs on the three runners.
4. **The contract.** `IExtrudeOps` and `ExtrudeParameters` do not change (ADR-0036 item 5): the loop is a list of
   (x, y). No public shape of `Engine.Contracts` changes. The polygon is not in the Document (ADR-0007): the log holds
   the command, and the engine computes the polygon at each apply and each rebuild.
5. **The refusals (Q3).** A profile that breaks item 1 gives `E-SKETCH-PROFILE-INVALID`, with a message that names
   the reason, as for lines.
6. **The desktop.** No change: the button Extrude of the window of the sketch sends the command, and the overlay
   draws the curves with 64 segments for a full turn (ADR-0034 item 4), so the drawing and the solid agree.

## Questions for the owner

**Q1. What a profile can hold.**
- A: one closed loop of lines and arcs, or one circle alone (item 1).
- B: A, and also loops inside the outer loop as holes, for example a circle inside a rectangle.

**Recommended: A.** A hole needs a polygon with several loops in `ExtrudeParameters`, which is a change of the public
shape of `Engine.Contracts`, and a later task can add it.

**Q2. The number of segments.**
- A: 64 for a full turn, the number of the overlay; an arc has the part of them that its sweep needs, rounded up to a
  power of two. The chord is at most 0.12 % of the radius from the arc.
- B: a number from a tolerance of the chord in millimetres, so a large circle gets more segments.

**Recommended: A.** It needs no new setting, the drawing and the solid agree, and B can come later with a field of
the command.

**Q3. The diagnostic code.** `CLAUDE.md` says: "Do not change the meaning of a code."
- A: `E-SKETCH-PROFILE-INVALID` keeps its code, and its row says "lines and arcs, or one circle" for the loop.
- B: a new code `E-SKETCH-CURVE-PROFILE-INVALID` for a rule of an arc.

**Recommended: A.** The meaning stays "the profile of the sketch breaks the rules of an extrude"; the rules grow. The
owner chose the same form for `W-SKETCH-REBUILD-FAILED` on 2026-10-30.

## Consequences

- A sketch with a circle or an arc extrudes, and a rebuild follows each edit of a curve.
- A curved face of the solid is flat facets, as a tessellation of the overlay is. An exact curve needs a B-Rep (K4,
  K5).

## History

- 2026-10-31 · TASK-0126 · Proposed, with the questions Q1 to Q3 for the owner, and Accepted on the same day with Q1 A,
  Q2 A and Q3 A under the standing answer of the owner. Item 2 compares the cosine of the step with 0.9951, not with
  cos(2π / 64), as a margin: with cos(2π / 64) a full turn also gives 64 segments on Windows x64 (an injection of
  TASK-0126), but the count then depends on the last bit of the halvings.
