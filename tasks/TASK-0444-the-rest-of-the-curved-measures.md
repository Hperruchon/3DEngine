---
id: 0444
title: The measure queries give the volume of a plate with holes, and the distance and the angle of the side of a cone, of an arc and of two curved faces
status: Done
phase: K15c
opened: 2026-11-21
depends-on: [0434]
governed-by: [0004, 0016, 0018, 0028, 0033, 0037, 0060, 0180]
writes:
  create:
    - tasks/TASK-0444-the-rest-of-the-curved-measures.md
    - Engine.Core/Queries/CutMass.cs
    - Engine.Core/Queries/RevolvedDistance.cs
  modify:
    - Engine.Core/Queries/CurvedDistance.cs
    - Engine.Core/Queries/MeasureCurve.cs
    - Engine.Core/Queries/MeasureTarget.cs
    - Engine.Core/Queries/GetAngleQueryHandler.cs
    - Engine.Tests/Queries/MeasureQueryTests.cs
    - docs/adr/0180-the-measure-queries-of-plane-faces.md
    - docs/diagnostics.md
    - docs/guides/agent-loop.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - 3DEngine.App/**
    - Engine.Contracts/**
---

# TASK-0444 — The measure queries give the volume of a plate with holes, and the distance and the angle of the side of a cone, of an arc and of two curved faces

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K15c follows K15b (TASK-0434, ADR-0180 items 10 to 13) and P0.174 (TASK-0438). K15b measures a whole curved face of
one command with only Translate and Rotate steps after it, and refuses the other cases with `E-MEASURE-CURVED`: a plate with
a hole gives the volume of its facets, and the side of a cone, the side of an arc, two curved faces other than two spheres,
a nearest point past an end of a cylinder and the angle of two curved faces are refused. The answers Q2 A, Q3 A and Q4 A of
the codebase review of 2026-11-21 still hold. K17 (TASK-0443, ADR-0183) reads `MeasureCurve.Whole` for the axis of a
cylinder.

## Goal

The measure queries give the values of the curve for the cases of row K15c that have a closed form for faces across or
along an axis, and refuse each other case with `E-MEASURE-CURVED`.

## Scope (in)

1. `GetMassProperties`: the volume and the centre of the curve of a body with a Subtract of a cylinder, a cone, a sphere or
   an extrude of one circle from a body of plane faces, when each other face near the tool is across its axis, also with
   several holes that do not meet, and Translate and Rotate steps after them.
2. `GetDistance`: the side of a cone; a part past an end of the side of a cylinder; the side of an arc; a sphere and the
   side of a cylinder or a cone; the sides of two cylinders with parallel axes.
3. `GetAngle`: the side of an arc and a plane face; two sides of a cylinder, a cone or an arc, as the angle of their axes.
4. ADR-0180 items 14 to 19 with the precision of each value, the row K15d for the cases that stay refused, the guide for
   an agent, the row of `E-MEASURE-CURVED`, the ledger entry and the counts of the tests.

## Scope (out)

- A vertex as a side of `GetDistance`. No query gives a name of a vertex: `GetFaces` and `GetConnectors` give faces, edges
  and datums, and the segments of `GetFaces` give positions only. No public name is invented (K15d).
- A slanted piece near the side of a cone or past an end of a cylinder, the side of a hole, a hole at an angle, a notch, two
  holes that meet, a tube, two cylinders that are not parallel, a cylinder and a cone, two cones, an arc and a curved face
  (K15d).
- The axis of the side of an arc in `GetConnectors` (ADR-0183 keeps it `Curved`).
- A change of `Engine.Contracts` or of `Engine.Kernel`, a new diagnostic code, and a tool of the window.

## Acceptance criteria

- [x] A plate of 100 by 50 by 10 with a hole of radius 5 gives 50000 - 250 π mm³ with its centre; a blind hole and a hole
  of an extrude of a circle remove 125 π and 40 π more; a Translate and a Rotate after them move and turn the centre.
- [x] A cone through a plate removes its frustum (875 π / 12 mm³), and a sphere removes its cap (17 π / 3 mm³), each with
  its centre.
- [x] A notch and two holes that meet keep the volume of their facets with `facets` true.
- [x] The side of a cylinder gives its distance past its ends to faces, a body, an edge across the axis and an edge along
  the axis; a turned box near its rim is refused.
- [x] The side of a cone gives its distance to a face beside it, over its apex, under its base, a face that crosses it, a
  box around it, and two edges along the axis; a turned box near it is refused.
- [x] Two cylinders with parallel axes give their distance apart, with a gap along the axis, one inside the other, crossing
  and on one axis; two axes that are not parallel are refused.
- [x] A sphere gives its distance to the side of a cylinder (apart, past an end, inside, crossing, around) and of a cone.
- [x] The side of an arc gives its distance when the nearest point of the whole turn is on the arc, and is refused else;
  it gives its angle to three plane faces.
- [x] Two sides of a cylinder, a cone or an arc give the angle of their axes; a sphere is refused.
- [x] An injection in each part makes a named test fail.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.
  (Local: each passes. Continuous integration runs after the push.)

## Decisions

The answers Q2 A, Q3 A and Q4 A of the codebase review of 2026-11-21 (the standing order of the owner of 2026-11-20) still
hold. This task takes these decisions; the owner accepts the change of the section "Decision" of ADR-0180:

- One ADR: ADR-0180 gets items 14 to 19, as K15b got items 10 to 13. No new ADR, because the shape of the three queries and
  of their results does not change.
- The closed forms hold for faces across or along an axis only, the faces of the bookcase and of a plate. A slanted piece
  needs a quartic or a search with a bound, and the row K15d holds it, with the other refused cases.
- The volume of a Subtract needs each other face near the tool to be across its axis, a test on exact rationals, and the
  test of inside of item 4 for each part of the axis. So a plate with holes has a closed form, and a notch keeps its facets.
- The angle of two curved faces is the angle of their axes as lines, from 0 to 90 degrees: two turns of outward normals
  always share a direction, so the rule of item 12 gives 0 for each pair.
- The side of an arc is measured only when the nearest point of its whole turn is on the arc. `GetConnectors` keeps the
  side of an arc as `Curved`, with the parameter `arcs` of `MeasureCurve.Whole` false, so K17 does not change.
- No vertex: no query gives its name, and the row K15d keeps it.
- The test of a sphere through a plate uses a cap, because the exact facets of a sphere through the whole plate take about
  45 s in the kernel.

## Outcome

Status: Done · commit (the merge of the branch k15c-curved-rest) · v0.428

## Method

Mechanical: the rows of the roadmap and of `docs/diagnostics.md`, the limits of the guide for an agent, and the counts of
`eng/test-floor.txt`.

Judgement:

- A side of revolution has a profile segment in the half-plane (ρ, z), so the distance from a point is a 2D distance. On a
  plane across the axis it is a convex function of ρ, on a line along the axis a convex function of z, and on a plane along
  the axis its gradient is 0 only on one line, where the function is convex. So each piece across or along the axis has its
  minimum at a clamp that exact comparisons of squares decide.
- A tool of a Subtract is a solid of revolution with the square of its radius R² q(s), q a polynomial. When each other face
  in its slab is across the axis or keeps the distance R, the body holds each part of the axis between two cuts whole or not
  at all, and the integral of q is exact.
- Two cylinders with parallel axes are a circle times a span each, so the square of the distance is a sum of two squares.

Weakest: the decisions of a sphere against a side compare doubles, so a tie within the rounding gives 0 or a value within
the rounding of 0; no test examines such a tie. The volume of a Subtract has no proved bound beyond the count of its
roundings; the tests hold 1e-12 of the volume and 1e-9 mm of the centre.

Tests first: before the change, the eight new tests and the changed test of K15b (a face over the top of a cylinder, now
2 mm) failed on the code of `2a79f573` (a script restored the engine files of that commit, ran the tests, and restored the
new files byte for byte). The test of K15b with a plate with a hole is now
`A_Notch_Gives_The_Volume_Of_Its_Facets_And_Each_Case_With_No_Closed_Form_Is_Refused`, because a plate with a hole has a
closed form; it passes on both, because the old code refuses more cases.

Injections (`inject0444.js` in `%TEMP%\k15c`), each restored byte for byte (sha256). Each of the twenty-four injections
made a named test fail:

- the integral of a cone with s³ / 2, and the moment of a sphere with s⁴ / 2:
  `A_Cone_Or_A_Sphere_Through_A_Plate_Gives_The_Volume_Of_The_Curve`;
- no test of two holes that meet, and no test of a face near the tool:
  `A_Notch_Gives_The_Volume_Of_Its_Facets_And_Each_Case_With_No_Closed_Form_Is_Refused`;
- each part of the tool inside the body, and no cut at a face across the axis: that cone test and
  `A_Plate_With_Holes_Gives_The_Volume_Of_The_Curve`; a Rotate that does not turn the centre: the plate test;
- the farthest vertex of a level piece replaced by its nearest point:
  `The_Distance_Past_An_End_Of_The_Side_Of_A_Cylinder_Is_To_Its_Rim`;
- a level piece that crosses the side not at 0, the target over the apex at R0, a segment along a cone far out at its
  high end, and the height of a cone at the radius ρ with + ρ: `The_Distance_From_The_Side_Of_A_Cone`;
- a sphere around a side at M - s, and the crossing at the smaller root:
  `The_Distance_Between_A_Sphere_And_The_Side_Of_A_Cylinder_Or_A_Cone`;
- no gap between the spans, the inner cylinder on the wrong side, and two axes that are not parallel measured:
  `The_Distance_Between_The_Sides_Of_Two_Cylinders_With_Parallel_Axes`;
- no test of the arc for a distance, each direction on the arc, the normal of the arc the other way, no side of an arc for
  a distance, and the nearest normal of the whole turn for an arc: `The_Distance_And_The_Angle_Of_The_Side_Of_An_Arc` (and
  the notch test for the first three);
- the cosine and the sine of two axes swapped: `The_Angle_Between_Two_Curved_Faces_Is_The_Angle_Between_Their_Axes`;
- a refused piece below the best distance not refused:
  `The_Distance_From_The_Side_Of_A_Cylinder_To_Faces_Edges_And_Bodies`.

The first run missed three of them, because another piece gave the same answer: the vertical edges of the small box over
the cylinder reach its corner, the lower part of an edge cut at the base of the cone gives its rim, and the centre of the
first sphere that crosses the side lies on the side, so both roots are on it. The tests now measure the face of the small
box alone, an edge from z = 0 up, and a sphere that crosses the side near its top, and each of the three fails.

The first full run failed in `AffectsCitationGateTests`: a comment of `MeasureCurve.cs` cited ADR-0183, whose affects field
does not name that file. The comment now names `GetConnectors` without the ADR.

## Progress

- 2026-11-21: opened after K17 (TASK-0443, v0.427, not merged yet), branch `k15c-curved-rest` from `2a79f573`. ADR-0180
  items 14 to 19, the files `CutMass.cs` and `RevolvedDistance.cs`, eight new tests, two tests of K15b changed, the row
  K15d. Ledger entry v0.428.
- 2026-11-21: run 44932220648 of `53772dce` passed on each job. Closed at v0.428.
