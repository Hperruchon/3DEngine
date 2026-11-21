---
id: 0443
title: The query GetConnectors gives the named datums of a body, the centre and the normal of each plane face, and the axis of a whole cylinder
status: Active
phase: K17
opened: 2026-11-21
depends-on: [0436]
governed-by: [0004, 0016, 0018, 0028, 0033, 0037, 0060, 0083, 0089, 0112, 0153, 0155, 0158, 0166, 0167, 0168, 0169, 0179, 0183]
writes:
  create:
    - tasks/TASK-0443-the-connectors-of-a-body.md
    - docs/adr/0183-the-connectors-of-a-body.md
    - Engine.Core/Queries/GetConnectorsQuery.cs
    - Engine.Core/Queries/GetConnectorsQueryHandler.cs
    - Engine.Tests/Queries/GetConnectorsQueryTests.cs
    - Engine.Tests/Http/HttpConnectorsScenarioTests.cs
  modify:
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Tests/Guides/AgentGuideExamplesTests.cs
    - docs/guides/agent-loop.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - 3DEngine.App/**
    - Engine.Contracts/**
    - Engine.Kernel/**
---

# TASK-0443 — The query GetConnectors gives the named datums of a body, the centre and the normal of each plane face, and the axis of a whole cylinder

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K17 follows K10 (ADR-0157 item 11). K10a is done (TASK-0429); K10b, the lamp, waits for the sizes of the owner (R-0070),
and the order of the owner of the weekend lets the next rows start, as K15 started. ADR-0157 item 4 (answer A4 of the owner
of 2026-11-15) says that a connector is a named datum of a body, or an implicit frame that a query gives from its faces.
Card N8 of `docs/proposals/2026-11-15-assembly-and-agent-design.md` names the query `GetConnectors(bodyId)`. The measure
queries (K15a, K15b, ADR-0180) read the exact solid of the owned kernel and the curve of the design of a whole curved face,
and the pair check (K11, ADR-0182) uses the same path. No query gives the centre of a face or the axis of a cylinder.

## Goal

One query gives the named datums of a body and, for each face as `GetFaces` names it, the exact centre and the outward
normal of a plane face, or the axis and the radius of the whole side of a cylinder, with no change of the Document.

## Scope (in)

1. The query `GetConnectors` with the parameter `bodyId`, registered in `HandlerCatalog`.
2. The named datums whose feature has the body at the end of its chain of consumers, with the frame of the record.
3. The exact centre of the area of a plane face, also with a hole, and its outward unit normal.
4. The axis and the radius of the whole side of a `CreateCylinder` or of an extrude of a circle, with only `Translate` and
   `Rotate` after it; the kind `Curved` with no frame for each other curved face.
5. ADR-0183, one example in the guide for an agent, the places of four codes in `docs/diagnostics.md`, the ledger entry
   and the counts of the tests.

## Scope (out)

- The axis of a cylinder face after a `Subtract`, a `Union` or an `Intersect`, the axis of a cone, and the length of a side.
- A frame of a datum that follows a later step of its body (P11.0 decides it), and a command that names a datum.
- A change of `Engine.Contracts` or of `Engine.Kernel`, a new diagnostic code, and a tool of the window.

## Acceptance criteria

- [x] A box gives the six exact centres and outward normals of its faces, in the order of `GetFaces`, with no change of the
  version or of the bodies.
- [x] A box after a `Translate` and a `Rotate` of an integer quaternion gives each centre as the exact turn of the nine
  doubles, and each normal as the turned axis.
- [x] A cylinder, moved and turned or not, gives its axis and its radius, and its two caps as planes; an extrude of a circle
  gives its axis; a cone, a sphere, a torus and the side of an arc are `Curved` with no frame.
- [x] The named datums of the body are listed in the order of the log; a datum with no name and a datum of another body are
  not; after a `Translate` the datum stays a connector of the new body with the frame of its record.
- [x] A plane face with a square hole gives the centre of its area with the hole; the side of a drilled hole is `Curved`.
- [x] The bookcase of K10a gives the six frames of a module from its table.
- [x] A body that is not live, a mesh body and a body with a fault are refused with the codes of the measure queries.
- [x] An agent reaches the query through `/queries` of HTTP, and the guide for an agent has one example that a host answers.
- [x] An injection in each part makes a named test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Decisions

ADR-0183 records each decision; ADR-0157 item 4 gives the rule of the owner. This task takes these decisions:

- The name `GetConnectors` of card N8, with two lists: `datums` (a whole frame) and `faces` (a point and a direction).
- Only a datum with a name, because ADR-0157 item 4 says "a named datum".
- The frame of a datum as its record holds it: a datum does not follow a later `Translate` or `Rotate` (ADR-0051 item 2), and
  a frame that follows is a decision of P11.0.
- The centre of a plane face is the exact centre of its area, from a fan of triangles weighted by ((b - a) × (c - a)) · n, so
  no root is necessary; the point of `GetFaces` is not a centre.
- A unit direction has one `Math.Sqrt` for each component, of the nearest double to v_i² / (v · v), so it does not depend on
  the scale of the exact normal.
- The axis of a cylinder only for the cases that `MeasureCurve.Whole` gives (K15b); each other curved face is `Curved`.
- The result records live in `Engine.Core/Queries`, as the results of the measure queries; no contract changes.

## Outcome

Status: Active · commit (the merge of the branch k17-connectors) · v0.427

## Method

Mechanical: the query and its registration in the form of the measure queries; the datums by `KernelProjection.End`, as
`FaceGone` finds the body of a datum; the cylinder by `MeasureCurve.Whole`; the rows of the roadmap and of the index of the
ADRs; the places of the four codes in `docs/diagnostics.md`; the counts of `eng/test-floor.txt`.

Judgement: the centre of the area. Each piece of the kernel is convex and the pieces of one name lie on one plane, so a fan
of each piece covers the face once, and a hole is outside each piece. The weight ((b - a) × (c - a)) · n is the area times
2 |n| for each triangle, so the sum needs no root. The unit direction takes one root for each component, so that the exact
normal and any multiple of it give the same bits.

Weakest: the centre of a cap of a cylinder is that of the polygon of its facets, which the handler builds from doubles, so it
is within 1e-12 mm of the axis and not on it; the test holds 1e-12. A datum whose body moved after it gives a frame that is
not on its face; the test `Only_The_Named_Datums_Of_The_Body_Are_Its_Datum_Connectors` holds that fact, and the guide says
it.

Tests first: the nine new tests failed against a stub of the handler that gave no datum and no face, and the test of the
blocks of the guide failed before the guide named `GetConnectors`.

Injections (`inject0443.js` in `%TEMP%\k17`), each restored byte for byte (sha256). Each of the
seventeen injections made a named test fail:

- the centre from the first piece only, and a weight of 1 for each triangle: the hole, the cylinder and the extrude tests;
- the centre divided by 2: each test of `GetConnectorsQueryTests` that reads a centre, the HTTP test and the examples of
  the guide;
- no sign of a component, and a component that is not squared: the box, the turned box, the cylinder, the extrude, the
  hole and the bookcase tests (and the HTTP test and the examples of the guide for the second);
- the normal of a plane face to the inside: the same tests, the HTTP test and the examples of the guide;
- a cone as a cylinder: `An_Extrude_Of_A_Circle_Gives_Its_Axis_And_Each_Other_Curved_Face_Has_No_Frame`;
- the point of a cylinder at the other end, and the direction from the base point: the cylinder and the extrude tests (and
  the HTTP test for the first);
- each curved face as a plane: the cylinder, the extrude, the hole and the HTTP tests;
- a datum with no name, the datums of each body, and the reverse order of the log:
  `Only_The_Named_Datums_Of_The_Body_Are_Its_Datum_Connectors`;
- the faces in the reverse order: the box, the turned box, the cylinder, the extrude and the hole tests;
- the solid of the projection with no refusal of a mesh or a fault:
  `A_Body_That_Is_Not_Live_A_Mesh_Body_Or_A_Body_With_A_Fault_Is_Refused` and the HTTP test;
- no registration in `HandlerCatalog`: each of the nine tests and the examples of the guide;
- the centre of the body in the example of the guide:
  `Each_Example_Of_The_Guide_Is_Accepted_By_A_Host_And_Gives_Its_Shape`.

The first form of "each curved face as a plane" (`if (true)`) did not build, because the compiler refuses code that it
cannot reach; the second form made the tests fail.

## Progress

- 2026-11-21: opened after K11 (TASK-0436, v0.420), P17 (TASK-0439, v0.422) and P0.177 (TASK-0442, v0.426), branch
  `k17-connectors` from `e8112673`. ADR-0183, the query, nine tests, the example of the guide. Ledger entry v0.427.
