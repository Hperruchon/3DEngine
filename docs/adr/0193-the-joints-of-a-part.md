---
id: 0193
title: A joint connects two bodies of a part in a tree with no solver, and its commands place the child and each body under it
status: Amended
topic: Contracts, geometry, assembly
date: 2026-11-28
supersedes: []
superseded-by: []
amends: ['0157', '0191']
amended-by: ['0194']
affects:
  - Engine.Contracts/Joints/JointRecord.cs
  - Engine.Contracts/Document.cs
  - Engine.Contracts/Handlers/ICommandHandler.cs
  - Engine.Core/CommandBus.cs
  - Engine.Core/Features/FeatureList.cs
  - Engine.Core/Commands/AddJointCommand.cs
  - Engine.Core/Commands/AddJointCommandHandler.cs
  - Engine.Core/Commands/SetJointValueCommand.cs
  - Engine.Core/Commands/SetJointValueCommandHandler.cs
  - Engine.Core/Commands/RemoveJointCommand.cs
  - Engine.Core/Commands/RemoveJointCommandHandler.cs
  - Engine.Core/Commands/JointResults.cs
  - Engine.Core/Commands/JointTree.cs
  - Engine.Core/Commands/PlaceBodyCommandHandler.cs
  - Engine.Core/Queries/GetJointsQuery.cs
  - Engine.Core/Queries/GetJointsQueryHandler.cs
  - Engine.Core/Queries/GetConnectorsQueryHandler.cs
  - Engine.Core/Hosting/HandlerCatalog.cs
  - Engine.Core/DiagnosticCodes.cs
  - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
  - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
  - Engine.Tests/Commands/JointTests.cs
  - Engine.Tests/Persistence/DocumentFileTests.cs
  - Engine.Core/Determinism/CrossPlatformProbe.cs
  - Engine.Tests/ReplayDeterminism/CrossPlatformHashTests.cs
enforced-by: Engine.Tests/Commands/JointTests.cs
notes: Phase P11a, TASK-0479. The owner answered "Yes, all A" for Q1 to Q7 on 2026-11-28 (docs/proposals/2026-11-28-the-joints-of-p11.md).
---

# ADR-0193 — A joint connects two bodies of a part in a tree with no solver, and its commands place the child and each body under it

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0157 items 3 to 5 give the frame of the joints: a part has connectors, a joint connects two connectors, the joints make a
kinematic tree with one parent for each part, the engine computes each place from the tree with `+ - * /` only and has no
solver, a joint that closes a loop is refused, and the first kinds are fixed, revolute and slider. ADR-0191 (P11.0) gave each
body a placement and the absolute command `PlaceBody`. The joints are a new record of the Document, new commands and new
events, so they change the public shape of `Engine.Contracts`. The proposal `docs/proposals/2026-11-28-the-joints-of-p11.md`
gave the facts and the questions Q1 to Q7, and the owner answered "Yes, all A" on 2026-11-28.

## Decision

1. **The record.** The Document of a part holds joints (`Document.Joints`, the record `JointRecord` of
   `Engine.Contracts/Joints/JointRecord.cs`). A joint has the identifier of its command `AddJoint`, a kind (`fixed`, `pivot` or
   `slider`), a parent body and a child body, a connector on each, a value and optional limits. A record never changes: a new
   value gives a new record with the same identifier, as for a sheet (ADR-0153 item 1). An assembly holds no joint, because
   the commands of a joint apply in a part only (ADR-0071 item 2).
2. **A connector (Q1 A).** A connector names a face of its body as `GetConnectors` names it, with `featureId`, `element` and an
   optional `itemId`, or a datum in effect on the body, with `datumId`, and not both. A face is a plane or the side of a
   cylinder of the design; another curved face gives no frame. A datum is on the body when the chain of consumers of its
   feature ends at the body, as in `GetConnectors`, and a datum with a fault is refused. The record holds the frame of the
   connector in the frame of its body: the origin, a direction U at a right angle to the normal, V = normal × U, and the
   normal. `AddJoint` reads the frame one time, so the frame follows each later placement of the body:
   - a plane face gives the exact centre of its area and its outward normal from the shape of the body, with no placement, so
     the frame has the same bits at each placement of the body;
   - the side of a cylinder gives the centre of its base and its axis in the frame of the Document, and a datum gives its
     frame in the frame of the Document; the inverse of the placement of the body now takes each into the frame of the body,
     in doubles;
   - for a face, U is the axis of the Document with the smallest part of the normal (x first, then y, then z), less its part
     along the normal, divided by its length; for a datum, U is the U of the datum.
3. **The place of the child (Q2 A).** A joint gives the placement of its child:
   child = parent · frame(parent connector) · motion(value) · flip · frame(child connector)⁻¹. `flip` is a half turn about U,
   so the normal of the child connector is opposite to the normal of the parent connector, as two faces that meet. The motion
   is the identity for a fixed joint, a turn about the normal for a pivot, and a move along the normal for a slider. The
   quaternion of a frame comes from the matrix of U, V and the normal by the method of Shepperd with no root, and
   `Placements.Compose` and `Placements.Inverse` (ADR-0191 item 6) give the products. Each step uses `+ - * /` and `sqrt` only.
   The probe of the determinism hash (`CrossPlatformProbe`) places a root with a pivot and a slider under it, and then sets
   the pivot to a turn with a negative cosine, so the four platforms compare the bits of the joints.
4. **The value (Q3 A).** A pivot takes `cos` and `sin`, a direction of a length greater than zero that gives the turn; the log
   holds the two numbers and no angle (`CLAUDE.md`, determinism rule 2). With r the length of (cos, sin), the quaternion of the
   turn is (r + cos, 0, 0, sin), or (sin, 0, 0, r − cos) when cos < 0, so no sum cancels; each is the quaternion of the half
   angle times a number. A slider takes `distance`, a finite length in millimetres. A fixed joint takes no value, and its
   record has the value (1, 0, 0). A number of the wrong kind, a number that is not finite, or a pair that gives no turn is
   refused.
5. **The limits.** A pivot takes `lowerCos`, `lowerSin`, `upperCos` and `upperSin`, all four or none; the turns of the joint go
   counterclockwise about the normal of the parent connector from the lower to the upper limit. A slider takes `lowerDistance`
   and `upperDistance`, both or none, and the lower is not above the upper. A fixed joint has no limits. A value outside the
   limits is refused, and nothing moves. The check of a pivot compares the order of the two turns from the lower limit with a
   function of `+ - /` only that grows with the angle. Row K12 reads the limits.
6. **The commands and the tree (Q4 A).** `AddJoint` (the kind, the two bodies, the two connectors, the value, the limits),
   `SetJointValue` (the joint and the value) and `RemoveJoint` (the joint). A body has one parent joint at most, and a joint
   whose child is above its parent in the tree closes a loop; each joint of the Document counts, also a joint of a body that a
   later command consumed, because an Undo can make that body live again. `AddJoint` and `SetJointValue` place the child and
   each body under it in the same step; `RemoveJoint` moves nothing, and the child keeps its placement. A `PlaceBody` of the
   child of a joint is refused; a `PlaceBody` of another body places each body under it in the same step (amends ADR-0191
   item 3). The walk takes the children of a body in the order of the log of their joints, so the events of a step have one
   order. `Undo` and `Redo` move the joint and the bodies as one step. The commit emits `joint.created`, `joint.changed` and
   `joint.removed` after the sheet events, each with the whole record but `joint.removed`; the body events of the step come
   first, so a subscriber has the placement of each child. A joint whose parent or child is not live places nothing.
7. **The query.** `GetJoints` gives each joint of the Document with its whole record in the order of the log of its
   `AddJoint`, as `GetSheets` does for the sheets (ADR-0189 item 3). The snapshot of `/events` gives no joint, as it gives no
   sheet; a client reads `GetJoints` after a reset.
8. **The diagnostic codes.** `E-JOINT-ID-IN-USE` (the identifier of a command of the log), `E-JOINT-NOT-FOUND` (no such
   joint), `E-JOINT-INVALID` (a kind, a pair of bodies, a value or limits that are not valid), `E-JOINT-CONNECTOR-INVALID` (a
   connector that names no face and no datum, both, a face that the body does not have or that gives no frame, or a datum that
   is not on the body or has a fault), `E-JOINT-PARENT-IN-USE`, `E-JOINT-LOOP`, `E-JOINT-LIMIT` and `E-JOINT-PLACED` (a
   `PlaceBody` of the child of a joint). An unknown body gives `E-GEOM-BODY-NOT-FOUND`.
9. **The placements of a step (amends ADR-0191 item 5).** A joint places bodies that its command does not name, so the walks of
   the steps cannot read each placement from the `PlaceBody` commands of the log only. The command bus records, for each step,
   each body whose placement the step changes and its new placement, before the log holds the command. The feature list reads
   these placements as steps of placement of the bodies, as it reads a `PlaceBody`, and a log with no Document reads the
   `PlaceBody` commands only. So the solid of a body, a regeneration, a boolean and the chain of a datum take the placement that
   a joint gave. A replay, an open and a fork apply each command through a bus, so they make the same table.
10. **The order (Q5 A, Q6 A and Q7 A).** P11a is this ADR with the record, the three kinds, the values, the limits, the tree,
    the refusals and a test of each rule (`Engine.Tests/Commands/JointTests.cs`). P11b adds the loose state of a joint (Q5 A),
    the intended pair of each joint in `CheckInterference` (ADR-0157 item 5), and the command `PlaceByConnectors`, which places a
    child by two connectors one time and records no joint (Q6 A).

## Alternatives considered

- **Only a named datum as a connector, in the frame of the Document (Q1 B).** A joint then needs a new datum after each move.
- **The same normals (Q2 B).** Each client must turn one connector itself for the usual case of two faces that meet.
- **A quaternion as the value of a pivot (Q3 B).** A limit and a check of K12 are then a comparison of two quaternions.
- **A placement that each reader computes from the tree (Q4 B).** Each reader of ADR-0191 item 7 must then walk the tree, and a
  reader that forgets it is wrong for each child.
- **The connector frames of each step from the shape at that step.** A change of the shape of a parent then moves its child. It
  needs the solid of a body at each place of the log in the feature list, which is a fold of the log with no geometry
  (ADR-0054). A later task can add it with no change of the log.

## Consequences

**Good:** a part has joints in its Document, and one command moves a child with each body under it, with `Undo`, an open and a
replay bit for bit. The placements of the joints are placements of ADR-0191, so each reader of the geometry, the window and the
preview need no change.

**Bad:**
- `AddJoint` reads the frame of each connector one time. A later change of the shape of the parent or of the child, for
  example a new distance of an extrude, does not move the child; the next command of the joint or of its tree uses the old
  frame. Register entry R-0083 holds it.
- A `Rotate`, a `Translate` or a boolean consumes its body and makes a new body with no joint. A joint of a consumed body stays
  in the Document and places nothing; R-0083 holds it.
- A datum does not follow a `PlaceBody` of its body (ADR-0191 item 5), so a datum connector reads the datum where it is now, in
  the frame of the body now. The frame of a datum and of the side of a cylinder has the rounding of the inverse placement.
- The table of item 9 is state of the command bus that the log gives. A walk of a log with no Document does not see the
  placements of the joints; no code walks such a log today.
- A version of the app before P11a refuses a file that holds a command of a joint, because it does not know the command.

## History

- 2026-11-28 · TASK-0479 · The record, Accepted: the answers "Yes, all A" of the owner for Q1 to Q7 of the proposal. Amends
  ADR-0157 items 3 and 4 and ADR-0191 items 3 and 5.
- 2026-11-28 · TASK-0489 · Amended by ADR-0194: item 6, a `PlaceBody` of the child of a loose joint applies, and a loose joint
  places nothing; the state, the intended pairs and `PlaceByConnectors` of item 10.
