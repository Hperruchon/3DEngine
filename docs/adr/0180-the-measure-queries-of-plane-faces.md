---
id: 0180
title: The measure queries GetMassProperties, GetDistance and GetAngle read the exact solid of the owned kernel, first for plane faces
status: Accepted
topic: Kernel, queries
date: 2026-11-21
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - Engine.Core/Queries/GetMassPropertiesQuery.cs
  - Engine.Core/Queries/GetMassPropertiesQueryHandler.cs
  - Engine.Core/Queries/GetDistanceQuery.cs
  - Engine.Core/Queries/GetDistanceQueryHandler.cs
  - Engine.Core/Queries/GetAngleQuery.cs
  - Engine.Core/Queries/GetAngleQueryHandler.cs
  - Engine.Core/Queries/MeasureTarget.cs
  - Engine.Core/Queries/NearestPoints.cs
  - Engine.Tests/Queries/MeasureQueryTests.cs
enforced-by: Engine.Tests/Queries/MeasureQueryTests.cs
notes: K15a. The answers Q1 A to Q4 A of the codebase review of 2026-11-21, by the standing order of the owner of 2026-11-20.
---

# ADR-0180 — The measure queries GetMassProperties, GetDistance and GetAngle read the exact solid of the owned kernel, first for plane faces

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K15 of the roadmap (ADR-0157 item 11) asks for measure queries in the engine, so that the window and an agent measure
by one path: the volume and the centre of a body, the minimum distance between two bodies, faces, edges or vertices with its
two points, and the cosine and the sine between two faces. The checks of K11 and the preview of P15 need the same measures.

The owned kernel holds each body as exact convex polygons of rationals (ADR-0057), and `KernelProjection` gives the solid of a
live body from the steps in effect (ADR-0058). `KernelSolid.Volume` gives the exact volume. No query gives a measure today,
so a client computes it from the doubles of `GetFaces`, and two clients can give two answers.

The codebase review of 2026-11-21 (sections 4 to 6) researched the task and asked four questions. The standing order of the
owner of 2026-11-20 takes option A of each:

- **Q1 A.** K15 comes first, in a slice for bodies of plane faces (K15a). K10b waits for the sizes of the lamp (R-0070).
- **Q2 A.** A body with curved faces gives the exact volume of its facets in the first slice, with a flag that says so.
- **Q3 A.** A query names a face or an edge by the body and the name that `GetFaces` gives, as a datum does.
- **Q4 A.** The results are doubles in millimetres, and a distance gives its two points.

The constraint that removes the obvious alternative, a measure on the doubles of the mesh, is the determinism of the
engine: two platforms must give the same answer, and an agent must get the answer of the window. The exact solid gives one
answer, and `+`, `-`, `*`, `/` and `sqrt` give it on each platform.

## Decision

1. **One path.** Three queries of `Engine.Core/Queries`, registered in `HandlerCatalog`: `GetMassProperties`,
   `GetDistance` and `GetAngle`. The route `/queries` of HTTP, the command `engine query` and `DocumentSession.Query` reach
   them through the registry, with no new route (ADR-0016). Each query reads the solid of the owned kernel through
   `KernelProjection.OfRecord`, so it needs no capability of the backend. A query writes nothing to the log.
2. **The names (Q3 A).** A body is its id. A face is the reference that `GetFaces` gives: the feature, the element, and the
   item when `GetFaces` gives one. An edge is the pair of the names of the two faces that a B-Rep edge bounds, as an edge of
   `GetFaces` gives them, in either order (ADR-0077 items 1 and 6). The parameters are flat, as those of `CreateDatum`,
   because a parameter of a query holds no object (ADR-0016): `bodyIdA`, `featureIdA`, `elementA`, `itemIdA`,
   `secondFeatureIdA`, `secondElementA`, `secondItemIdA`, and the same with `B`. `GetFaces` gives no name of a vertex, so
   K15a measures no vertex.
3. **GetMassProperties(bodyId).** The result is `volume` in mm³, `centre` (x, y, z) in mm, and `facets`. The volume is
   `KernelSolid.Volume`. The centre is the sum of the centres of the tetrahedra of a fan of each polygon with the origin,
   each weighted by its signed volume. Both are exact rationals, and each leaves as the nearest double. `facets` is true
   when a face of the body is curved (item 6): the volume and the centre are then those of the plane facets of the solid,
   which lie inside the curve, and not those of the curve (Q2 A).
4. **GetDistance(A, B).** The result is `distance` in mm, `pointA` on side A and `pointB` on side B (Q4 A). Each side is a
   body, a face or an edge of a body of plane faces. The pieces of a side are the convex polygons of the body or of the face,
   or the segments of the edge. Between two pieces the nearest pair has a point on an edge of one of them, so the steps are
   a segment against a polygon and a segment against a segment, in exact rationals. A pair of pieces whose boxes are not
   nearer than the best pair is skipped. A body is a solid: when the pieces do not touch, a side inside the body of the
   other side is at the distance 0, and its first vertex is the common point. The test of inside is the exact subtract of
   the kernel: a cube about the point, small enough not to touch the surface, minus the solid gives no solid. When several
   pairs give the minimum, the result is the first pair in the order of the pieces, so a replay gives the same points.
5. **GetAngle(A, B).** The result is `cosine` and `sine` of the angle between the outward normals of two plane faces, from 0
   to 180 degrees. The two faces can be on two bodies. With the normals m and n, the square of the cosine is
   (m · n)² / (|m|² |n|²) and the square of the sine is |m × n|² / (|m|² |n|²), both exact; the sign of the cosine is the
   sign of m · n, and the sine is never negative. No angle is given (CLAUDE.md, determinism rule 2).
6. **Plane faces first (Q1 A).** A face name is curved when its pieces lie on more than one plane: the facets of a cylinder,
   a cone, a sphere, a torus or an arc of a profile, as `GetFaces` gives no normal for them. In K15a `GetDistance` refuses a
   body with a curved face, a curved face, and an edge of a curved face; `GetAngle` refuses a curved face. The code is
   `E-MEASURE-CURVED`. A plane face of a curved body, as the top of a cylinder, is measured. `GetMassProperties` refuses no
   curved body (item 3).
7. **The refusals.** A body that is not live gives `E-GEOM-BODY-NOT-FOUND`, a mesh body `E-MESH-UNSUPPORTED`, a body with a
   fault `E-GEOM-BODY-FAULTED`, and a body that the kernel cannot build `E-GEOM-FACES-FAILED`, as for `GetFaces`. A face or
   an edge that the body does not have, or a part of a name, gives `E-MEASURE-NAME-NOT-FOUND`.
8. **Determinism.** Each step uses `+`, `-`, `*`, `/` and comparisons on exact rationals. The last step is one `Math.Sqrt` of
   a double that is the nearest double to an exact square: the distance, the cosine and the sine. No transcendental function
   is called, and nothing enters the log.
9. **The kernel does not change.** The queries read the public members `KernelSolid.Polygons`, `KernelSolid.Volume`,
   `KernelSolid.Box`, `KernelSolid.Translate`, `PlanarBoolean.Subtract`, `Brep.Of`, `PlaneR` and `Vector3R`.

## Consequences

**Good:** The window, an agent, the checks of K11 and the preview of P15 can measure by one query, with one answer on each
platform. A gap of the bookcase of K10a is one query. No contract changes: the results use `DatumPoint` of
`Engine.Contracts.Sketch`, as `GetFaces` does.

**Bad:** The exact numbers have a cost. After 150 exact turns the first read of a solid took about 2 s (ledger entry v0.413),
and a measure of such a body pays it. The count of the pairs of pieces is the product of the counts of the two sides; the
test of the boxes removes most pairs, but a body of many polygons inside the box of the other side is slow. The test of
inside builds two trees of the kernel. A curved body gives no distance and no angle until K15b, and the volume of its facets
is below the volume of the curve. A vertex has no name, so no query measures from a vertex. Fourteen flat parameters are
long for a person to type; a client gives them from `GetFaces`.

**Next:** K15b gives the volume of the curve, the distance and the angle of curved faces from `SideCurve` (ADR-0143), and a
measure from a vertex when a name of a vertex exists. K11 builds its pair check on item 4.

## History

- 2026-11-21 · TASK-0432 · The three queries for bodies of plane faces, the volume of the facets of a curved body, and the
  codes `E-MEASURE-NAME-NOT-FOUND` and `E-MEASURE-CURVED`.
