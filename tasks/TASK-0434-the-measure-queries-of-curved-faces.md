---
id: 0434
title: The measure queries give the volume of the curve and the distance and the angle of whole curved faces from the curve of the design
status: Done
phase: K15b
opened: 2026-11-21
depends-on: [0432]
governed-by: [0004, 0016, 0018, 0028, 0033, 0037, 0054, 0058, 0059, 0060, 0076, 0083, 0089, 0112, 0143, 0180, 0181]
writes:
  create:
    - tasks/TASK-0434-the-measure-queries-of-curved-faces.md
    - Engine.Core/Queries/MeasureCurve.cs
    - Engine.Core/Queries/CurvedDistance.cs
  modify:
    - Engine.Core/Queries/GetMassPropertiesQuery.cs
    - Engine.Core/Queries/GetMassPropertiesQueryHandler.cs
    - Engine.Core/Queries/GetDistanceQueryHandler.cs
    - Engine.Core/Queries/GetAngleQuery.cs
    - Engine.Core/Queries/GetAngleQueryHandler.cs
    - Engine.Core/Queries/MeasureTarget.cs
    - Engine.Core/Features/KernelProjection.cs
    - Engine.Tests/Queries/MeasureQueryTests.cs
    - docs/adr/0180-the-measure-queries-of-plane-faces.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - 3DEngine.App/**
---

# TASK-0434 — The measure queries give the volume of the curve and the distance and the angle of whole curved faces from the curve of the design

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K15b follows K15a (TASK-0432, ADR-0180). K15a gives the volume of the facets of a curved body with the flag `facets`,
and refuses each curved face of `GetDistance` and `GetAngle` with `E-MEASURE-CURVED`. The kernel holds the facets and not
the curve, but `KernelProjection.Curves` gives the curve of the design of a curved side (ADR-0143 item 1). The answers Q2 A,
Q3 A and Q4 A of the codebase review of 2026-11-21 still hold: the names of `GetFaces`, and doubles in millimetres with the
two points of a distance.

## Goal

The three measure queries give the values of the curve of the design for a body of one curved command, moved or turned, and
refuse each other curved case with `E-MEASURE-CURVED`.

## Scope (in)

1. `GetMassProperties`: the volume and the centre of the curve of a cylinder, a cone, a sphere, a torus, and an extrude of a
   sketch with arcs or a circle, each with only Translate and Rotate steps after it, with `facets` false.
2. `GetDistance`: the distance from the whole side of a cylinder (a CreateCylinder or an extrude of a circle) and from the
   whole surface of a sphere to a body, a face or an edge of item 4 of ADR-0180, and between two spheres.
3. `GetAngle`: the angle between the whole side of a cylinder or a cone and a plane face.
4. The angle of an arc from `+`, `-`, `*`, `/` and the square root only.
5. ADR-0180 items 10 to 13, the split of K15b into K15b and K15c, the ledger entry and the counts of the tests.

## Scope (out)

- A vertex as a side of `GetDistance`. No query gives a name of a vertex: `GetFaces` gives faces and edges, and its
  segments give positions only. No public name is invented (K15c).
- The volume of the curve of a body with a Subtract, the distance from the side of a cone or of an arc, between two curved
  faces other than two spheres, and past an end of the side of a cylinder, and the angle of two curved faces (K15c).
- A change of `Engine.Contracts` or of `Engine.Kernel`. The results keep the shape of K15a.
- A tool of the window that shows a measure.

## Acceptance criteria

- [x] A cylinder, a cone, a sphere, a torus, a turned and moved cylinder and a moved cone give π r² h, π r² h / 3,
  4 π r³ / 3 and 2 π² R r² with their centres, within 1e-12 of the volume and 1e-9 mm, with `facets` false.
- [x] An extrude of a square with a half disc as a bulge and as a bite, and an extrude of a circle, give the volume and the
  centre of the curve.
- [x] The surface of a sphere gives its distance to a face, a body, a box inside it, a face that crosses it, from inside a
  large box, and to three spheres.
- [x] The side of a cylinder gives its distance to a face, a body, a face past its top, an edge, a box inside it, a face that
  crosses it, from inside a large box, and from the side of an extrude of a circle; a face over its top is refused.
- [x] The side of a cylinder and of a cone give their angles to plane faces, among them a cone and its base: -3/5 and 4/5.
- [x] A plate with a hole keeps the volume of its facets with the flag, and each case with no closed form is refused with
  `E-MEASURE-CURVED`.
- [x] An injection in each part makes a named test fail.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.
  (Local: each passes. Continuous integration runs after the push.)

## Decisions

The answers Q2 A, Q3 A and Q4 A of the codebase review of 2026-11-21 (the standing order of the owner of 2026-11-20) still
hold. This task takes these decisions; the owner accepts the change of the section "Decision" of ADR-0180:

- A closed form only for a body of one command with only Translate and Rotate steps after it. A Subtract can leave a part of
  a curve, so a plate with a hole keeps the volume of its facets with the flag (K15c).
- The values of the curve are doubles from the doubles of the commands, not exact rationals. Each step that decides a case
  is exact.
- The angle of a curved face is the angle to the nearest outward normal of its whole turn. It is the angle of K15a between
  outward normals where the side has that normal, and it gives the angle of a cone and its base.
- The angle of an arc comes from halvings of its tangent and a series, with `+`, `-`, `*`, `/` and the square root only,
  because the determinism gate scans each file of `Engine.Core` for `Math.Atan2`.
- No name of a vertex: the row K15c keeps the vertex.

## Outcome

Status: Done · commit (the merge of the branch k15b-curved-measure) · v0.418

## Method

Mechanical: the walk of the Translate and Rotate steps in `KernelProjection.Placed`, in the form of the walk of the solid;
the reads of the design of an extrude in `KernelProjection.ExtrudeDesign`; the rows of the roadmap and of
`docs/diagnostics.md`; the counts of `eng/test-floor.txt`.

Judgement:

- The area of a profile with arcs is the exact polygon of the first point of each edge, with the signed segment of each arc.
  The moment of a segment about the centre of its circle is c³ / 12 along its bisector, with no angle.
- For a point in the slab of the side of a cylinder, the nearest point of the side is along the radius. So the part of a
  piece in the slab, cut exactly, has a closed form, and a part past an end has the lower bound (its distance to the axis
  segment) - r. A case whose bound is below the best distance is refused.
- A curved face that keeps a distance above 0 from each piece of a body is all inside the solid or all outside it, so the
  test of inside of K15a on one point decides.

Weakest: the test of inside for the side of a cylinder reads a vertex of its facets, which is off the curve by the rounding
of the turn. A distance below about 1e-12 of the radius could give a wrong answer to that test; no test examines it. The
precision of the values of the curve is the precision that the tests hold (1e-12 of a volume, 1e-9 mm of a centre); no bound
is proved.

Tests first: before the change, the six new tests failed on the code of v0.416 (five with `E-MEASURE-CURVED` or the volume of
the facets, and the test of the angle of an arc against a stub that gave 0). The changed test of K15a passes on both, because
K15a refuses more cases.

Injections (`inject0434.js` in `%TEMP%\k15b`), each restored byte for byte (sha256). Each one made the named tests fail:

- the volume of a cone divided by 2, the centre of a cone at half its height, and a Rotate that does not turn the offset:
  `A_Cylinder_A_Cone_A_Sphere_And_A_Torus_Give_The_Volume_Of_The_Curve`;
- the mass handler ignores the design: that test and `An_Extrude_Of_Arcs_Or_Of_A_Circle_Gives_The_Volume_Of_The_Curve`;
- an arc walked back adds its segment, the moment of a chord along the chord, and the centre of an extrude at its far end:
  `An_Extrude_Of_Arcs_Or_Of_A_Circle_Gives_The_Volume_Of_The_Curve`;
- the series of the arctangent with 1/4, and the reduction of a tangent above 1 that adds:
  `The_Angle_Of_A_Direction_Uses_Plus_Minus_Times_Divide_And_Root_Only`;
- the side of an arc is whole, and a sphere is measured by `GetAngle`:
  `A_Body_With_A_Hole_Gives_The_Volume_Of_Its_Facets_And_Each_Case_With_No_Closed_Form_Is_Refused`;
- a sphere outside adds its radius, a piece inside a sphere crosses it, the inner sphere on the wrong side, and the crossing
  takes the smaller root: `The_Distance_From_A_Sphere_To_Faces_Bodies_And_Spheres`. The first run of the last one passed
  each test, because both roots of the first crossing face lie on that face; the face from y = 3 to y = 13 catches it now;
- no cut at the top of the slab, and no refusal past an end: `The_Distance_From_The_Side_Of_A_Cylinder_To_Faces_Edges_And_Bodies`.
  The case of the turned box was added before the run for the second one: the face over the top has no part in the slab,
  so it gives no best distance and is refused with or without the bound;
- no test of inside for a curved face: the cylinder test and the sphere test;
- the cosine of a cone loses its sign, and the cosine and the sine of a cylinder swap:
  `The_Angle_Between_The_Side_Of_A_Cylinder_Or_A_Cone_And_A_Plane_Face`.

## Progress

- 2026-11-21: opened after K15a and P0.172, branch `k15b-curved-measure` from `6d78b908` (TASK-0431, v0.416). ADR-0180 items
  10 to 13, the files `MeasureCurve.cs` and `CurvedDistance.cs`, six new tests, and one test of K15a changed: the cylinder of
  `A_Curved_Body_Gives_The_Volume_Of_Its_Facets_And_Is_Refused_Where_A_Plane_Is_Needed` now gives the volume of its curve, so
  the test is `A_Body_With_A_Hole_Gives_The_Volume_Of_Its_Facets_And_Each_Case_With_No_Closed_Form_Is_Refused`, with a plate
  with a hole. Ledger entry v0.418.
- 2026-11-21: merged `main` with K16 (TASK-0433, v0.417), which the owner ordered first ("merge k16"): the ledger
  entry v0.417 comes before v0.418, the test floor of `Engine.Tests` is 1771, and ADR-0181 governs this task too.
- 2026-11-21: run 44893590618 of `e3c7c136` passed on each job. Closed at v0.418.
