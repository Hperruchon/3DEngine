---
id: 0450
title: The measure queries give the side of a hole, holes on one axis, a tube, two sides on one axis, two cylinders that are not parallel and an arc with a curved face
status: Done
phase: K15d
opened: 2026-11-21
depends-on: [0444]
governed-by: [0004, 0016, 0018, 0028, 0033, 0037, 0060, 0180]
writes:
  create:
    - tasks/TASK-0450-the-curved-cases-of-k15d.md
  modify:
    - Engine.Core/Queries/CutMass.cs
    - Engine.Core/Queries/RevolvedDistance.cs
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

# TASK-0450 — The measure queries give the side of a hole, holes on one axis, a tube, two sides on one axis, two cylinders that are not parallel and an arc with a curved face

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K15d follows K15c (TASK-0444, ADR-0180 items 14 to 19). K15c gives the closed forms for faces across or along an axis
and refuses each other case with `E-MEASURE-CURVED`; ADR-0180 item 18 lists them. The third codebase review of 2026-11-21
(TASK-0448) makes P15 the next product phase, with K15d alongside, because K15d needs no decision of the owner. The
answers Q2 A, Q3 A and Q4 A of the codebase review of 2026-11-21 still hold. The findings E235 (the angle of an arc that
bites into the profile) and E238 (a false comment of `RevolvedDistance`) belong to P0.180 (TASK-0449).

## Goal

The measure queries give a closed form for the refused cases of item 18 that a person who measures a part meets first, and
ADR-0180 states each case that stays refused with its reason.

## Scope (in)

The cases of item 18, in the order of their value for a person who measures a part:

1. The side of a hole: the distance from it and its angle, for a hole of a cylinder, a cone or an extrude of one circle.
   A part has holes more than any other curved face, and the distance from a hole to an edge or to another hole is the
   first measure of a drawing.
2. Holes that meet on one axis, as a counterbore or a countersink, and a tube: the volume of the curve. These are the next
   holes of a part, and a tube is the first body with a curved face that a person makes from a cylinder.
3. Two sides on one axis: a cylinder and a cone, two cones, a pin in a countersink.
4. Two cylinders whose axes are not parallel, as two pipes that cross at a distance.
5. The side of an arc with a curved face: a rounded corner against a pin or a ball.
6. The rest, which stays refused with its reason: a slanted piece near a cone or past an end of a cylinder, a cone and
   another side on another axis, two cylinders whose common perpendicular is past an end or whose sides cross, a torus, a
   body with a curved face as a side, an edge of a curved face, a notch, a hole at an angle, two holes that meet on two
   axes, a sphere for an angle, and a vertex.

The file changes: ADR-0180 items 20 to 24 and the changed items, the row K15d, the guide for an agent, the row of
`E-MEASURE-CURVED`, the ledger entry and the counts of the tests.

## Scope (out)

- A vertex as a side of `GetDistance`. No query gives a name of a vertex: `GetFaces` gives faces and edges, with the
  segments of an edge as positions, and `GetConnectors` gives datums, faces and axes. K15b, K15c and K15d invent no public
  name.
- An iterative solver. The cases of item 23 need the roots of a polynomial of degree four; no bound of the work of a search
  is proved here.
- The axis of the side of a hole in `GetConnectors` (ADR-0183 keeps it `Curved`).
- E235 and E238 (P0.180, TASK-0449). This task changes `GetAngleQueryHandler.AboutAxis`, the method of E235, for the side of
  a hole only, and adds a sentence to the header of `RevolvedDistance.cs`, the comment of E238, without a change of its
  false sentence.
- A change of `Engine.Contracts` or of `Engine.Kernel`, a new diagnostic code, and a tool of the window.

## Acceptance criteria

- [x] The side of a hole of radius 5 in a plate of 100 by 50 by 10 is 15 from the face +x, 10 from the face +y, 10 from a
  box over the plate, 40 from a second hole, at 90 degrees to the face +z and at 0 degrees to the face +x. The hole of a
  clevis in two parts is 6 from the face of its slot, and its upper part 2 from a box over the clevis.
- [x] The side of a countersink of 90 degrees is at 45 degrees to the face +z (its tool alone at 135 degrees), 3 from a box
  over the plate and 1 from a pin on its axis.
- [x] A counterbore removes 154 π, a countersink 280 π / 3, a clevis 40 π, each with its centre; a tube keeps 1280 π and a
  cone with a bore 72 π at z = -4.125; the outer side of the tube is 10 from a face, and its inner side 4 from a sphere.
- [x] A cone and a cylinder on one axis are √5 apart, and two cones 2.2; a cone and a cylinder on two parallel axes are
  refused.
- [x] Two cylinders whose axes cross at a right angle are 4 apart; a foot past an end and two sides that cross are refused.
- [x] The side of an arc is 4 from a sphere and from a cylinder beyond it, and refused for a sphere on the other side.
- [x] An injection in each part makes a named test fail.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Decisions

The answers Q2 A, Q3 A and Q4 A of the codebase review of 2026-11-21 (the standing order of the owner of 2026-11-20) still
hold. This task takes these decisions; the owner accepts the change of the section "Decision" of ADR-0180:

- One ADR: ADR-0180 gets items 20 to 24, as K15c got items 14 to 19. No new ADR: the shape of the three queries and of
  their results does not change, and the kernel does not change.
- Tools on one line make one group, so a counterbore and a countersink have a closed form; the radius of a group is a
  linear function on each part, so each integral is exact. A tool joins a group only before a Translate or a Rotate, because
  the group keeps the solid before its first tool for the test of inside.
- A first body of one curved command with tools on its axis (a tube) needs no test of inside: its radius is known on each
  part. A tool on another axis keeps the facets, because the facets of the first body lie inside its curve.
- The parts of the volume also give the faces: the side of a hole is the side of a cylinder or of a frustum on each part,
  measured by the code of K15b and K15c. The outward normals of a hole point to the axis.
- The lowest distance over a part of a set is not below the lowest over the whole set, and is equal when the part holds the
  pair. So the side of an arc with a curved face takes the pair of the whole turn when its point is on the arc.
- No iterative solver: the rest needs the roots of a polynomial of degree four (item 23).

## Outcome

Status: Done · commit (the merge of the branch k15d-curved-last) · v0.433

## Method

Mechanical: the rows of the roadmap and of `docs/diagnostics.md`, the limits of the guide for an agent, and the counts of
`eng/test-floor.txt`.

Judgement:

- A hole in a body of plane faces, with each other face across its axis or far from it, has the side of its tool on each
  part of the axis that the body holds. So the side of a hole is a list of sides of revolution, and the closed forms of
  items 11, 15 and 16 measure each one. A frustum is a profile from (R0, 0) to (R1, H) with R1 above 0; the arguments of
  item 15 hold for it, because the curve where the side meets a plane along the axis ends at the plane of the base, where
  the caller cuts the piece.
- Two surfaces of revolution on one axis are nearest in one half-plane, because the angle between two points enters the
  square of their distance as - 2 ρ σ cos Δθ only. The heights as (x - P) · A keep each step on rationals.
- For two cylinders that are not parallel the lower bound D - r - s is reached only by the common perpendicular, so its feet
  on the spans and D ≥ r + s decide the case exactly. Each other case needs the distance from a circle.

Weakest: the test of inside for a part of a hole reads a point of the curve in doubles, so a distance below about 1e-12 of
the radius can give a wrong answer to that test, as for a whole side (ADR-0180 "Bad"). The volume of a group has no proved
bound beyond the count of its roundings; the tests hold 1e-12 of the volume and 1e-9 mm of the centre. No test examines
two tools on one axis with the same radius on a part (the refusal of a tie).

Tests first: before the change, the six new tests failed on the engine files of `d7077cf8`, and the 26 other tests of
`MeasureQueryTests` passed, with the two changed tests of K15c. The first run after the change found an error of the test:
two holes at x = 30 and x = -20 are 40 apart, not 30.

Injections (`inject0450.js` in `%TEMP%\k15d`), each restored byte for byte (sha256), each run with `dotnet test` and its
filter as arguments, with no shell. Each of the sixteen injections made a named test fail:

- the integral of β² s² as s³ / 2, the moment of 2 α β s² without its factor 2, no cut where two radii are equal, no tool
  that joins a group, the smallest radius of a group in place of the largest, a bore wider than the first body that removes
  its own radius, and no part of the outer side of a tube: `Holes_On_One_Axis_And_A_Tube_Give_The_Volume_Of_The_Curve`;
- the side of a hole with outward normals away from the axis, the slope of a frustum from its larger radius only, and a
  part of a cone hole as a whole cone to its apex: `The_Side_Of_A_Countersink_Gives_Its_Distance_And_Its_Angle`;
- no side of a hole, and the first part of a hole only: `The_Side_Of_A_Hole_Gives_Its_Distance_And_Its_Angle`;
- no test of the arc for two curved faces: `The_Side_Of_An_Arc_And_A_Curved_Face`;
- the heights of two profiles not divided by A · A: `Two_Sides_On_One_Axis_Are_As_Far_Apart_As_Their_Profiles`;
- a foot of the common perpendicular past an end, and two cylinders that cross, measured:
  `The_Sides_Of_Two_Cylinders_Whose_Axes_Are_Not_Parallel`.

The first form of four injections replaced a condition by `false`, and the build refused the unreachable code; the second
form keeps a condition that is never true, and each fails its test. The test of the clevis has a box over the clevis, 2 from
the upper part of the hole, because both parts are 6 from the slot, so the first part alone gives the same answer there.

## Progress

- 2026-11-21: opened after the third codebase review of 2026-11-21 (TASK-0448, v0.432, not merged yet), branch
  `k15d-curved-last` from `d7077cf8`. ADR-0180 items 20 to 24, six new tests, two tests of K15c changed. Ledger entry v0.433.
- 2026-11-22: run 44952444341 of `009e11f9` passed on each job. Closed at v0.433.
