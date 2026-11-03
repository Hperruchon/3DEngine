---
id: 0051
title: The references to the elements of a feature, and a datum on a face
status: Amended
topic: Contracts, feature graph, sketch
date: 2026-11-02
supersedes: []
superseded-by: []
amends: ['0012', '0031', '0033', '0036', '0039']
amended-by: ['0052', '0053']
affects:
  - Engine.Contracts/Sketch/DatumRecord.cs
  - Engine.Core/Sketch/DatumResolver.cs
  - Engine.Core/Commands/CreateDatumCommand.cs
  - Engine.Core/Commands/CreateDatumCommandHandler.cs
  - Engine.Core/Queries/GetDatumQuery.cs
enforced-by: Engine.Tests/Sketch/DatumTests.cs (TASK-0166 creates it)
---

# ADR-0051 — The references to the elements of a feature, and a datum on a face

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The roadmap row K0 is "A feature graph over Manifold. No B-Rep. Deferred-query references." K0a to K0f gave the
graph of the bodies and the rebuild of a chain (ADR-0039, ADR-0049). No feature can name a face, an edge or a vertex
of an earlier feature, so each sketch lies on one of the three origin planes (ADR-0033 item 1). Rule 3 of the order
of the roadmap says that K4 needs the reference model of K0.

Manifold gives no identity for a face (`docs/CHARTER.md`, "The kernel boundary"), and K4 gives it later. Before K4 a
reference is a deferred query: the log records what the reference names, and the engine resolves it again at each
apply and each rebuild. A sketch needs a plane only, so a query can give a plane with no topology.

The owner gave the order "start K0" on 2026-11-02 and answered eight questions on the same day:

- **Targets.** A reference names a face, an edge or a vertex. This is two slices: K0g gives the faces and a datum on
  a face, and K0h gives the edges and the vertices. The elements of an extrude and of a box both have names.
- **The record.** The command records a query, and the engine solves it again at each apply and each rebuild.
- **A failure.** A reference that does not resolve gives a fault, and the datum keeps its last plane.
- **The desktop.** The engine, HTTP and the command line come first (K0g, K0h). The desktop follows (K0i).
- **The place.** A reference resolves at its own place in the log.
- **The datum.** A datum on a face is its own entity, with a command, a record, events and a query.
- **A subtract.** A reference resolves the carrier of its element, also when a subtract removed the face.

## Decision

1. **The reference.** A reference has three fields: `featureId`, the identifier of a `CreateBox` or an `Extrude` in
   effect; `element`, a name from the table below; and `itemId`, the identifier of a sketch item, for the names that
   need one. The log never holds the index of a face, so an edit that keeps the item keeps the reference.

   | Feature | Faces (K0g) | Edges (K0h) | Vertices (K0h) |
   |---|---|---|---|
   | `Extrude` | `start`, `end`; `side` with a line | `startEdge`, `endEdge` with a line; `sideEdge` with a point | `startVertex`, `endVertex` with a point |
   | `CreateBox` | `+x`, `-x`, `+y`, `-y`, `+z`, `-z` | two signed axes in the order x, y, z, for example `+x-z` | three signed axes, for example `+x-y+z` |

   `start` is the cap on the plane of the sketch, and `end` is the cap at the distance. The side of a line is the face
   that the line sweeps. The names of K0h are reserved: until K0h, `CreateDatum` refuses them. Since ADR-0052 an
   edge or a vertex is a reference of a sketch, and `CreateDatum` still takes the faces only.
2. **The place (Q of the place).** A reference resolves on the steps in effect, at the place of the command that holds
   it in the log:
   - The feature gives its carrier from its current inputs: for an extrude, the sketch record in the Document, the
     distance in effect (ADR-0049 item 3), and the frame of the datum of the sketch; for a box, its three sizes.
   - Each `Translate` in effect on the chain of consumers of the body of the feature, before the place of the
     reference, moves the carrier by its offset. A `Subtract` passes the carrier with no change (item 8).
   - A step after the place of the reference does not move it. A reference names only a feature before it, so the
     references form no cycle.
3. **The frame of a face.** A face gives a right-handed frame: an origin o and the axes u, v and n = u × v, with n
   out of the solid.
   - The extrude of a sketch on the frame (O, U, V, N) by the distance d, with s the sign of d: `end` has the origin
     O + d·N and the axes (U, V, N) for s > 0, or (U, −V, −N) for s < 0. `start` has the origin O and the axes
     (U, −V, −N) for s > 0, or (U, V, N) for s < 0.
   - The side of a line: with a and b the ends of the line in the order of the counter-clockwise loop of the profile
     (ADR-0036 item 2) and t = b − a in the plane, the origin is the world point of a, u = (tₓU + t_yV) / |t|, v = N,
     and n = u × v, the outward side. A line that the profile does not use, an arc and a circle have no planar side.
   - A box of the sizes (x, y, z) has its centre at the origin of the Document. Its face ±x has the origin
     (±x/2, 0, 0), and the six faces have the axes (u, v, n): `+x` (Y, Z, X), `-x` (Z, Y, −X), `+y` (Z, X, Y),
     `-y` (X, Z, −Y), `+z` (X, Y, Z), `-z` (Y, X, −Z).
4. **The datum (Q of the datum).** `CreateDatum` has the fields `featureId`, `element`, `itemId` (optional) and `name`
   (optional), and the output `datumId`, the identifier of the command. The Document holds one `DatumRecord` for each
   datum in effect in `Document.Datums`, with the identifier, the name, the reference, the frame, and a fault or
   null. A record never changes: a change gives a new record with the same identifier. The frame is a projection of
   the log, not an input. The rules of a name are those of a sketch (ADR-0028 item 3). An Undo of `CreateDatum`
   removes the datum, and a Redo gives it back (ADR-0031).
5. **A sketch on a datum.** `CreateSketch` takes the identifier of a datum of `Document.Datums` as it takes an origin
   plane. An extrude reads the frame of the datum of its sketch: the origin of the frame, then the shift of ADR-0036
   item 3 along its normal. This amends ADR-0033 item 1, which had three datums only, and ADR-0036 items 3 and 6, which
   had the origin (0, 0, 0) and axes of 0 and ±1 only.
6. **The rebuild (Q of a failure).** This amends ADR-0039. After an edit, the engine resolves each datum in effect
   again, in the order of the log, from the state after the edit. A datum whose frame changes changes each extrude in
   effect of each sketch on it, and the rebuild of ADR-0039 takes each one as it takes an extrude of the edited
   sketch. A datum that does not resolve keeps its last frame, gets the fault `W-DATUM-REFERENCE-FAILED` with the
   reason, and the edit applies. A datum that resolves again loses its fault. The reasons are: the feature is not in
   effect, the item is not in the record, the item is not a line of the profile, the profile of the extrude does
   not resolve (ADR-0036 item 2, ADR-0040 item 1), and the frame has a value that is not finite or an axis of the
   length zero. The warning comes when the fault is new or changes; a fault that stays gives no new warning, and the
   record shows it. An extrude that follows a moved datum and fails gets the code of the edit, and the data of its
   warning gives `datumId`.
7. **The refusals.** `CreateDatum` refuses with `E-DATUM-REFERENCE-INVALID` a feature that is not a `CreateBox` or an
   `Extrude` in effect, an unknown name, a name of K0h, an absent or extra `itemId`, and a reference that does not
   resolve now, with the reason of item 6. A name that breaks the rules gives `E-DATUM-NAME-INVALID`, and an identifier
   that the log holds gives `E-DATUM-ID-IN-USE`. `GetDatum` with no datum of the identifier gives `E-DATUM-NOT-FOUND`.
8. **A subtract (Q of a subtract).** A reference resolves the carrier of its element: a plane for a face. A subtract
   can remove the face or cut it into parts, and before K4 the engine cannot see it. So the reference still resolves,
   and the datum stays where the face was. K4 can then detect a face that is gone.
9. **The contract.** `Engine.Contracts` gains:
   - `Sketch/DatumRecord.cs`: `DatumRecord`, `DatumReference`, `DatumFrame`, `DatumPoint` and `DatumFault`;
   - `Document.Datums`, and the lists `CreatedDatums` and `ChangedDatums` of `CommandHandlerResult`;
   - three event kinds: `datum.created` with `datumId` and `name`, `datum.changed` with `datumId`, and `datum.removed`
     with `datumId`. They come after the events of the images in one commit (ADR-0033 item 9, ADR-0045 item 8);
   - the snapshot of `subscription.reset` gains `datums`, with `datumId` and `name` for each datum (this amends
     ADR-0012 §6, as ADR-0045 did for the images);
   - a step of the history records the datums that it creates and changes, and an Undo reverses them (this amends
     ADR-0031 items 3 to 5);
   - the query `GetDatum { datumId }`, which gives the record.
10. **Determinism.** A frame uses `+ - * / sqrt` and comparisons only, so the engine computes it with the same bits on
    each platform. A cap of an extrude on an origin plane and a face of a box have axes of 0 and ±1. The side of a line
    has general axes, so `manifold_transform` multiplies general numbers in native code. The log of the determinism
    hash of P0.67 gains an extrude of a sketch on the side of a line, so the pipeline compares its bits on the three
    runners. If the bits differ, the task stops and asks the owner.
11. **Out of this record.** An image on a datum of the Document (`PlaceImage` keeps the origin planes, ADR-0045), a
    command that deletes or moves a datum, a datum that a person types as numbers, and the desktop (K0i). Until K0i the
    desktop does not draw a sketch on a datum of the Document; it draws its solids.

## Consequences

- A person or an agent can sketch on a face of an extrude or a box, extrude it, and the new solid follows an edit of
  the base.
- A face that a subtract removed still gives a datum, until K4.
- A public shape of `Engine.Contracts` changes: a record, a collection of the Document, two lists of a result, three
  event kinds, a member of the snapshot and a query.

## History

- 2026-11-02 · TASK-0166 · Accepted with the eight answers of the owner of 2026-11-02.
- 2026-11-03 · TASK-0167 · ADR-0052 amends items 1 and 6: an edge or a vertex is a reference of a sketch, and the
  rebuild resolves each reference of a sketch with the datums, in the order of the log.
- 2026-11-03 · TASK-0169 · Item 6, from findings E125 to E127 of the codebase review of 2026-11-03 and the answers
  Q2 A and Q3 A of the owner: a frame that is not finite does not resolve, and the side of a line scales by the
  larger of its two components when its square underflows or overflows; a fault that stays gives no new warning; and
  the warning of an extrude on a moved datum gives `datumId`. Until TASK-0169 each edit gave the warning again.
- 2026-11-03 · TASK-0171 · ADR-0053 amends item 11: the desktop starts a sketch on a face and draws a sketch on a
  datum of the Document (K0i).
