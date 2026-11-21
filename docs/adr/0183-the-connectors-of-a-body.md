---
id: 0183
title: The query GetConnectors gives the named datums of a body and the implicit frames of its faces
status: Accepted
topic: Kernel, queries, assembly
date: 2026-11-21
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - Engine.Core/Queries/GetConnectorsQuery.cs
  - Engine.Core/Queries/GetConnectorsQueryHandler.cs
  - Engine.Tests/Queries/GetConnectorsQueryTests.cs
  - Engine.Tests/Http/HttpConnectorsScenarioTests.cs
enforced-by: Engine.Tests/Queries/GetConnectorsQueryTests.cs
notes: K17. ADR-0157 item 4 gives the decision of the owner; this record gives the shape of the query, by the standing order of the owner of 2026-11-20.
---

# ADR-0183 — The query GetConnectors gives the named datums of a body and the implicit frames of its faces

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K17 of the roadmap asks for the connectors of a body. ADR-0157 item 4 (answer A4 of the owner of 2026-11-15) says what a
connector is: "a named datum of a body, or an implicit frame that a query gives from the faces of the body: the centre and the
normal of a plane face, and the axis of a cylinder face." Card N8 of `docs/proposals/2026-11-15-assembly-and-agent-design.md`
names the query `GetConnectors(bodyId)`. The joints of P11 connect two connectors, so an agent needs the connectors of a body
before it can name "the top of A" with no pick of a face.

Three facts of the code shape this record:

- `GetFaces` gives a point of each face, the mean of the vertices of its first convex piece, and a unit normal in doubles.
  That point is not the centre of the face, and it changes with the partition of the kernel. `GetFaces` gives no axis of a
  curved face.
- The measure queries (ADR-0180) read the exact solid of the owned kernel (`MeasureTarget.SolidOf`) and, since K15b, the
  curve of the design of a whole curved face (`MeasureCurve.Whole`, ADR-0180 item 11): the side of a `CreateCylinder` or of
  an extrude of a circle with only `Translate` and `Rotate` steps after it.
- A datum resolves its frame at its place in the log (ADR-0051 item 2): a `Translate` or a `Rotate` of its body before the
  datum moves the frame, and a later one does not. The design file says that "a datum of 3DEngine is already a frame that
  follows its body"; that is true only for the steps before the datum.

The constraint that removes the obvious alternative, a connector computed by each client from the doubles of `GetFaces`, is
the same as for the measures: two clients and two platforms must give the same frame, and the window and an agent must
name the same place.

## Decision

1. **One query.** `GetConnectors`, schema version 1, in `Engine.Core/Queries`, registered in `HandlerCatalog`. The route
   `/queries` of HTTP, the command `engine query` and `DocumentSession.Query` reach it through the registry, with no new
   route (ADR-0016). It reads the solid of the owned kernel through `MeasureTarget.SolidOf`, so it needs no capability of
   the backend, and it writes nothing to the log. No record of `Engine.Contracts` changes: the result uses `DatumReference`,
   `DatumPoint`, `DatumAxis`, `DatumFrame` and `DatumFault` of `Engine.Contracts.Sketch`, as `GetFaces` and `GetDatum` do.
   The records `ConnectorsInfo`, `DatumConnector` and `FaceConnector` and the enumeration `ConnectorKind` are in
   `Engine.Core/Queries`, beside the results of the measure queries. `Engine.Kernel` does not change.
2. **The parameter and the result.** The parameter is `bodyId`. The result is `datums`, the named datums of the body, and
   `faces`, one entry for each face name of the body. A face is named as `GetFaces` names it (ADR-0180 item 2): the feature,
   the element, and the item when `GetFaces` gives one. The faces are in the order of `GetFaces`, the order of the first
   B-Rep face of each name.
3. **The datums.** A datum of the body is a datum whose reference names a feature, and the chain of consumers of that feature
   ends at the body (`KernelProjection.End`, ADR-0059 item 3). The check of a face that a cut removes finds the body of a
   datum in the same way. Only a datum with a name is a connector, because ADR-0157 item 4 says "a named datum". No command
   gives a name to a datum after `CreateDatum`, so a client gives the name of a connector in `CreateDatum`. The datums are in
   the order of the log. Each entry gives `datumId`, `name`, and the `reference`, the `frame` and the `fault` of the record,
   as `GetDatum` gives them. The query does not compute a new frame: a datum keeps the frame of its place in the log
   (ADR-0051 item 2), so after a later `Translate` or `Rotate` of the body the frame is not on its face. A frame that follows
   each later step is a change of what a datum is, and P11.0 decides it with the model of a move (ADR-0157 item 2). A datum
   with a fault, as a datum on a face that a cut removed, is in the list with its fault.
4. **A plane face.** A face name whose pieces lie on one plane (ADR-0180 item 6) has the kind `Plane`. `point` is the centre
   of its area and `direction` its outward unit normal; `radius` is null. The centre is exact: each convex piece of the
   name is a fan of triangles (a, b, c) from its first vertex; with n the normal of the plane, the weight
   w = ((b - a) × (c - a)) · n of a triangle is its area times 2 |n|, with one sign for each triangle that turns the same way
   about n. The centre is Σ w (a + b + c) / (3 Σ w). The factor 2 |n| is the same for each triangle, so no root is
   necessary. The pieces of the kernel leave out each hole, so the centre is that of the area with its holes. The normal is
   the normal of the plane of the pieces, which points out of the solid, as `GetFaces` and `GetAngle` read it.
5. **The side of a cylinder.** A curved face name for which `MeasureCurve.Whole` gives the whole side of a cylinder has the
   kind `Cylinder`: the side of a `CreateCylinder` or of an extrude of a circle, with only `Translate` and `Rotate` steps
   after the command (ADR-0180 item 11). `point` is the centre of the base of the side, `direction` the unit direction of
   the axis from the base to the other end, and `radius` the radius of the command or of the circle. The base and the axis
   are those of the curve of the design (ADR-0143 item 1): exact rationals of the doubles of the commands, moved and turned
   exactly as the kernel moves the solid. Each other curved face has the kind `Curved`, and `point`, `direction` and
   `radius` are null: the side of a cone, the surface of a sphere or a torus, the side of an arc, and a curved face of a
   body with a `Subtract`, a `Union` or an `Intersect` after its command. So a kind other than `Curved` always has a frame.
6. **Determinism.** The centre of item 4 uses `+`, `-`, `*` and `/` on exact rationals, and each coordinate leaves as the
   nearest double. A unit direction of items 4 and 5 is the one place with a root: each component is the square root of the
   nearest double to the exact square v_i² / (v · v), with the sign of v_i. That is one `Math.Sqrt` for each component. The
   value does not depend on the scale of v, so two forms of one plane give the same bits, and a component of an exact unit
   vector, as 1 or 0.5, is exact. No transcendental function is called; the gate of the determinism rules scans these files
   as each file of `Engine.Core`.
7. **The refusals.** The refusals of the measure queries (ADR-0180 item 7): a body that is not live gives
   `E-GEOM-BODY-NOT-FOUND`, a mesh body of an STL file `E-MESH-UNSUPPORTED`, a body with a fault `E-GEOM-BODY-FAULTED`, and a
   body that the kernel cannot build `E-GEOM-FACES-FAILED`. No new code.

## Alternatives considered

- **One list of connectors of one type.** Refused: a datum has a whole frame (an origin and three axes), and a face has a
  point and a direction only. Two types keep each field of each entry meaningful.
- **The point of `GetFaces` as the centre.** Refused: it is the mean of the vertices of one convex piece, so it moves when the
  kernel cuts the face in another way, and it is not on a line of symmetry of the face.
- **A full frame for a plane face.** Refused: a face has no preferred direction in its plane. A joint of P11 decides how it
  turns a frame about the normal.
- **The axis of each cylinder face from `KernelProjection.Curves`, also after a `Subtract`.** Refused for K17: the measure
  queries take a curved face only when it is whole, and one rule for the measures and the connectors keeps one answer. The
  axis of the hole of a plate is the next case (see "Next").
- **A frame of a datum that follows each later step of its body.** Refused for K17 (item 3).
- **A datum with no name in the list.** Refused: ADR-0157 item 4 says "a named datum", and a name is the handle that an
  agent gives to a joint.

## Consequences

**Good:** an agent gets the places of a body by one query, with one answer on each platform: the centre and the outward
normal of each plane face, also of a face with a hole, and the axis of a whole cylinder. The bookcase of K10a gives the six
frames of each module from its table. No contract changes.

**Bad:** the side of a hole that a `Subtract` made, the first case of a screw, has the kind `Curved` and no axis. A datum
keeps the frame of its place, so after a later move of its body its frame is not on its face; the entry of the face of its
reference gives the place of now, and the guide for an agent says so. The point of a cylinder is the centre of its base, not
the middle of its side, and the query gives no length of the side. A curved face gives no kind of its surface. The query
builds the B-Rep of the body at each call and keeps no cache, as the measure queries do; the cost is that of `GetFaces`
with no cache, plus the walk of the curve for each curved name.

**Next:** P11.0 decides if a datum follows each later step of its body (the model of a move, ADR-0157 item 2, R-0070). The
axis of the side of a cylinder after a `Subtract`, a `Union` or an `Intersect`, the axis of a cone and the length of a side
come with K15c or with the joints of P11, which need them.

## History

- 2026-11-21 · TASK-0443 · The record: the query `GetConnectors`, the exact centre of the area of a plane face, the axis of a
  whole cylinder, and the frame of a named datum as its record holds it.
