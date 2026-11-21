---
id: 0432
title: The window and an agent measure the volume, the distance and the angle of bodies of plane faces by one path of queries
status: Done
phase: K15a
opened: 2026-11-21
depends-on: [0430]
governed-by: [0004, 0013, 0016, 0018, 0019, 0028, 0033, 0037, 0060, 0083, 0089, 0112, 0114, 0153, 0155, 0158, 0166, 0167, 0168, 0169, 0179, 0180]
writes:
  create:
    - tasks/TASK-0432-the-measure-queries-of-plane-faces.md
    - docs/adr/0180-the-measure-queries-of-plane-faces.md
    - Engine.Core/Queries/GetMassPropertiesQuery.cs
    - Engine.Core/Queries/GetMassPropertiesQueryHandler.cs
    - Engine.Core/Queries/GetDistanceQuery.cs
    - Engine.Core/Queries/GetDistanceQueryHandler.cs
    - Engine.Core/Queries/GetAngleQuery.cs
    - Engine.Core/Queries/GetAngleQueryHandler.cs
    - Engine.Core/Queries/MeasureTarget.cs
    - Engine.Core/Queries/NearestPoints.cs
    - Engine.Tests/Queries/MeasureQueryTests.cs
    - Engine.Tests/Http/HttpMeasureScenarioTests.cs
  modify:
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - docs/diagnostics.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - 3DEngine.App/**
---

# TASK-0432 — The window and an agent measure the volume, the distance and the angle of bodies of plane faces by one path of queries

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K15 (ADR-0157 item 11) asks for measure queries in the engine: `GetMassProperties`, `GetDistance` and `GetAngle`. The
codebase review of 2026-11-21 (TASK-0430, sections 4 to 6) made K15 the next product phase and researched it. Its questions
Q1 to Q4 have the answer A by the standing order of the owner of 2026-11-20. K10a is done, and K10b waits for the sizes of
the lamp from the owner (R-0070), so K15 starts before K10b.

## Goal

Three queries give the exact volume and centre of a body, the minimum distance between two bodies, faces or edges with its
two points, and the cosine and the sine between two faces, for bodies of plane faces, by the one dispatch path of queries.

## Scope (in)

1. `GetMassProperties`, `GetDistance` and `GetAngle` in `Engine.Core/Queries`, registered in `HandlerCatalog`, with their
   schemas. HTTP and the command line reach them through the registry.
2. The exact steps on the solid of the owned kernel: the volume and the centre, the nearest points of two sets of convex
   polygons or segments, the inside of a solid, and the cosine and the sine of two normals.
3. The refusals: the codes of `GetFaces` for the body, and two new codes, `E-MEASURE-NAME-NOT-FOUND` and
   `E-MEASURE-CURVED`, registered in `docs/diagnostics.md` and in `/schema/diagnostics`.
4. The volume and the centre of the facets of a curved body, with the flag `facets` (Q2 A).
5. ADR-0180, the split of the row K15 into K15a and K15b, the ledger entry and the counts of the tests.

## Scope (out)

- A vertex as a side of `GetDistance`. `GetFaces` gives no name of a vertex (K15b).
- The distance and the angle of a curved face, and the volume of the curve (K15b).
- A change of `Engine.Contracts` or of `Engine.Kernel`. The results use `DatumPoint`, and the queries read public members
  of the kernel.
- A tool of the window that shows a measure. The window is out of the write set (`3DEngine.App/**`).

## Acceptance criteria

- [x] `GetMassProperties` gives the exact volume and centre of a box, a plate with a pocket, a turned box, an extrude with a
  chamfer and a panel of the bookcase, each against a value computed by hand.
- [x] `GetDistance` gives the distance and the two points of two boxes apart, touching, overlapping and one inside the
  other; of faces and edges of one box; and of the gaps of the bookcase.
- [x] `GetAngle` gives the cosine and the sine of parallel, normal and opposite faces, and of a chamfer at 45 degrees.
- [x] A curved body gives the volume of its facets with `facets` true; `GetDistance` and `GetAngle` refuse its curved face
  with `E-MEASURE-CURVED`.
- [x] A name that the body does not have, a part of a name, a body that is not live and a mesh body are refused with their
  codes.
- [x] The three queries answer through the route `/queries` of HTTP.
- [x] An injection in each part makes a named test fail.
- [x] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.
  (Local: each passes. Continuous integration runs after the push.)

## Decisions

The answers Q1 to Q4 are answered by the standing order of the owner of 2026-11-20 (Q1 to Q4 of the codebase review of
2026-11-21):

- **Q1 A.** K15 comes first, in a slice for bodies of plane faces: this task, K15a.
- **Q2 A.** The volume of a curved body in the first slice is the exact volume of its facets. The result says so with the
  field `facets`, true when a face of the body is curved; the centre is the centre of the facets too.
- **Q3 A.** A face or an edge is named by the body and the name that `GetFaces` gives: a face by its feature, element and
  item, an edge by the names of its two faces in either order.
- **Q4 A.** The results are doubles in millimetres, and a distance gives its two points.

## Outcome

Status: Done · commit (the merge of the branch k15a-measure-queries) · v0.415

## Method

Mechanical: the three query records and handlers in the form of `GetFaces`; the registration; the two codes in
`DiagnosticCodes`, `docs/diagnostics.md` and `/schema/diagnostics`; the rows of the roadmap; the counts of
`eng/test-floor.txt`.

Judgement:

- A face name is curved when its pieces lie on more than one plane, the rule of the normal of `GetFaces`. A plane face of a
  curved body, as the top of a cylinder, is measured; a body with a curved face is not, for `GetDistance`.
- The parameters are flat, as those of `CreateDatum`, because a parameter of a query holds no object (ADR-0016). A side of
  `GetDistance` with no face name is the body; with one name it is a face; with two names it is the edge between them.
- A body is a solid for `GetDistance`: a side inside a body is at the distance 0. The test of inside is the exact subtract
  of the kernel with a small cube about one vertex, so the kernel does not change.
- The angle is the angle between the outward normals, from 0 to 180 degrees, with a sine that is never negative.
- When several pairs give the minimum distance, the first pair in the order of the pieces is the result.

Weakest: the nearest points between two convex polygons take the minimum over the edges of each polygon against the other
polygon. A wrong case in the exact segment against segment step for parallel segments would give a distance that is too
large. `The_Nearest_Points_Of_Two_Pieces_Are_Exact` and the tests of parallel faces and edges of a box examine it; no test
examines a polygon that is not a rectangle or a triangle against a parallel polygon.

Injections (`inject0432.js` in the scratchpad), each restored byte for byte (sha256). Each one made the named tests fail:

- a parameter below 0 clamps to 1 (`NearestPoints.Clamp`): `The_Nearest_Points_Of_Two_Pieces_Are_Exact`. The first run of
  this injection passed each test; the two cases of parallel segments in that test catch it now;
- the parameter t of the far end clamps to 0 in place of 1: the same test, after the same addition;
- no crossing of a segment and a polygon: `The_Nearest_Points_Of_Two_Pieces_Are_Exact`;
- the test of the boxes skips the near pairs: the nearest points test, `Two_Boxes_Apart_Touching_Overlapping_And_One_Inside_The_Other`
  and `The_Bookcase_Gives_Its_Gaps_Its_Back_Panel_And_A_Right_Angle`;
- the foot of a point on the wrong side of the plane: seven tests, among them the HTTP test;
- the test of a point inside a polygon reversed: the nearest points test, the two boxes test and the curved body test;
- the test of inside a solid reversed: the two boxes test and the bookcase test;
- no test of inside a solid: the two boxes test;
- the centre divided by 12 times the volume: the plate test, the chamfer test and the bookcase test;
- no flag of facets, and no face is curved: `A_Curved_Body_Gives_The_Volume_Of_Its_Facets_And_Is_Refused_Where_A_Plane_Is_Needed`;
- a body with a curved face is measured by `GetDistance`: the curved body test;
- the cosine loses its sign: the box angle test and the chamfer test;
- the sine from m × m: four tests, among them the HTTP test;
- an edge between a face and itself, and an element alone as the body: `A_Name_That_The_Body_Does_Not_Have_Is_Refused`;
- no refusal of a mesh body: `A_Body_That_Is_Not_Live_Or_A_Mesh_Body_Is_Refused`;
- `GetDistance` not in `HandlerCatalog`: eight tests, among them the HTTP test;
- no row for `E-MEASURE-CURVED` in `docs/diagnostics.md`: `All_Diagnostic_Codes_In_Engine_Sources_Are_Registered`;
- no entry for `E-MEASURE-CURVED` in `/schema/diagnostics`: `Every_DiagnosticCodes_Constant_Appears_In_Schema_Diagnostics`.

The comments of `DiagnosticCodes.cs` and `SchemaDiagnosticsEndpoint.cs` name the phase and the task, not ADR-0180: an ADR
that affects them would need a change of the open TASK-0428, which is outside this write set.

## Progress

- 2026-11-21: opened after the codebase review of 2026-11-21, branch `k15a-measure-queries` from `f210f2c8` (TASK-0430,
  v0.414), which is not merged yet. The three queries, two codes, ADR-0180, twelve tests. Ledger entry v0.415.
- 2026-11-21: run 44853829471 of `878f7a87` passed on each job. Closed at v0.415.
