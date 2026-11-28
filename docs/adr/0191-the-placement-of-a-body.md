---
id: 0191
title: A body has a placement, a turn and an offset, and the command PlaceBody sets it, so a move changes no shape
status: Amended
topic: Contracts, geometry, assembly
date: 2026-11-28
supersedes: []
superseded-by: []
amends: ['0037', '0112', '0126', '0157']
amended-by: ['0193']
affects:
  - Engine.Contracts/BodyPlacement.cs
  - Engine.Contracts/BodyRecord.cs
  - Engine.Core/Placements.cs
  - Engine.Core/Commands/PlaceBodyCommand.cs
  - Engine.Core/Commands/PlaceBodyCommandHandler.cs
  - Engine.Core/Hosting/HandlerCatalog.cs
  - Engine.Core/Features/FeatureList.cs
  - Engine.Core/Sketch/FeatureGraph.cs
  - Engine.Core/Features/KernelProjection.cs
  - Engine.Core/Sketch/DatumResolver.cs
  - Engine.Core/Features/Regeneration.cs
  - Engine.Core/Commands/TranslateCommandHandler.cs
  - Engine.Core/Commands/RotateCommandHandler.cs
  - Engine.Core/Commands/SubtractCommandHandler.cs
  - Engine.Core/Commands/BooleanStep.cs
  - Engine.Core/Queries/GetTessellationQueryHandler.cs
  - Engine.Core/Queries/GetBoundingBoxQueryHandler.cs
  - Engine.Core/Queries/GetFacesQueryHandler.cs
  - Engine.Core/Queries/CheckInterferenceQueryHandler.cs
  - Engine.Core/Queries/CutMass.cs
  - Engine.Core/Queries/GetBodyInfoQuery.cs
  - Engine.Core/Queries/GetBodyInfoQueryHandler.cs
  - Engine.Core/Queries/GetBodiesQueryHandler.cs
  - Engine.Core/Export/StlExport.cs
  - Engine.Core/Preview/PreviewDifferences.cs
  - Engine.Core/CommandBus.cs
  - Engine.Core/Determinism/CrossPlatformProbe.cs
  - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
  - Engine.Api.Http/WebSockets/WireMessage.cs
  - 3DEngine.App/SessionBodies.cs
  - 3DEngine.App/SectionView.cs
  - 3DEngine.App/ProposalPreview.cs
  - Engine.Tests/Commands/PlaceBodyTests.cs
enforced-by: Engine.Tests/Commands/PlaceBodyTests.cs
notes: Phase P11.0, TASK-0474. The owner answered Q1 A on 2026-11-25 and Q1 to Q7 "All A" on 2026-11-27 (docs/proposals/2026-11-27-the-model-of-placements-of-p11-0.md).
---

# ADR-0191 — A body has a placement, a turn and an offset, and the command PlaceBody sets it, so a move changes no shape

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Until P11.0 a move made a new body. `Rotate` (ADR-0112) and `Translate` copy the solid of a body, move the copy, and consume
the source, so the part has a new identity after each move. The owned kernel reads the nine numbers of each turn exactly, so
the exact numbers of a coordinate grow by one product for each turn. The measure of K10a (R-0070,
`docs/proposals/2026-11-21-the-bookcase-case-of-k10.md`) gave a numerator of 2576 digits after 150 turns of one module, and a
first read of its exact solid of about 2 s.

ADR-0157 item 2 left the model of a move to the ADR of P11.0, after that measure, and ADR-0126 item 3 said that a joint moves a
body with `Rotate`. On 2026-11-25 the owner answered Q1 A: a model of placements. The proposal
`docs/proposals/2026-11-27-the-model-of-placements-of-p11-0.md` gave the facts and the questions Q1 to Q7, and the owner
answered "All A" on 2026-11-27.

## Decision

1. **A placement (Q2 A).** A body has a placement: a turn and an offset. A point p of the shape of the body is at R·p + t in
   the Document, where R is the turn of the quaternion (qw, qx, qy, qz) that `Rotation.FromQuaternion` gives (ADR-0112 items
   2 and 3) and t is the offset (dx, dy, dz). The solid under the geometry key of the body is its shape in its own frame. The
   record `BodyRecord` holds the seven numbers in the new optional field `Placement` (the type `BodyPlacement`), and null is
   the identity, so each body of before P11.0 has the same geometry.
2. **The stored form.** The log holds the seven numbers of each `PlaceBody` as doubles in their shortest form, and no angle
   (`CLAUDE.md`, determinism rule 2). A replay gives each placement bit for bit on each platform. The placement is state that
   the commands make, so a document file needs no new section, and the format version stays 1.
3. **The command `PlaceBody` (Q1 A).** `PlaceBody` has the parameters `bodyId`, `qw`, `qx`, `qy`, `qz`, `dx`, `dy` and `dz`. It
   is absolute: it holds the new placement, not a change of it. The body keeps its handle, its kind, its key and its shape;
   the step records the record before and after, so `Undo` and `Redo` move the placement as for a rename (ADR-0028). An
   unknown body gives `E-GEOM-BODY-NOT-FOUND`; a quaternion that gives no turn gives `E-GEOM-INVALID-PARAM`, or
   `E-GEOM-QUATERNION-TOO-SMALL` for the case of ADR-0114 item 1; an offset that is not finite gives `E-GEOM-INVALID-PARAM`.
   The handler needs no capability of the backend. A placement of the seven numbers of the identity stores null, so a body
   that goes back has the record of a body that never moved. No new diagnostic code.
4. **A mesh body (Q4 A; amends ADR-0112 item 1).** A mesh body of an STL file also takes a placement, because a placement
   changes no point of its mesh. `Rotate` and `Translate` still refuse a mesh body.
5. **The exact numbers stay small.** The owned kernel keeps the shape of each body in its own frame. Each read of a body in
   the Document applies its placement one time: `KernelProjection.Of`, `OfRecord`, `Curves` and `Placed` give the solid, the
   curves of the design and the exact map with the placement. A walk of the steps applies the placement of each body on the
   chain at the step that consumes it: before a `Translate` or a `Rotate` the placement of its operand, and at a boolean the
   placement of its second operand in the frame of the first (item 6). `DatumResolver` gives these placements as steps of the
   chain of a datum on a face, in doubles and in exact numbers. A datum takes the placement of its body at its own place in the
   log, as a datum before a `Translate` does not move (ADR-0051 item 2). The cache of the solids and the cache of `GetFaces`
   hold the placement in their key, because a `PlaceBody` keeps the geometry key. `CutMass` moves its cut from the placement
   that the step gives to the placement of the body.
6. **A move and a boolean of a placed body (Q3 A and Q5 A).** `Rotate` and `Translate` keep their meaning: each moves the shape
   of its operand in its place and makes a new body at the identity, so an old log replays with the same result. A
   `Subtract`, a `Union` and an `Intersect` give their result in the frame of the first body, with the placement of the first
   body. The second body goes into that frame with its placement relative to the first, which `Placements.Relative` computes
   in doubles with `+ - * /` only. The backend takes the moved operand under a key that comes from the key of the result and
   the operand (ADR-0037 item 3). The regeneration applies the same placements, which the feature list gives for each step.
7. **The readers.** Each reader of the geometry of a body applies the placement:
   - `GetTessellation`, `GetBoundingBox`, the export to STL and the check of a mesh body take the triangles of the backend
     with the placement in doubles; a tessellation is presentation (ADR-0007). The box of a turned body comes from its
     triangles, or from the eight corners of the box of its shape on a backend with no triangles.
   - `GetFaces`, `GetFaceAt`, `GetMassProperties`, `GetDistance`, `GetAngle`, `CheckInterference`, `GetConnectors`,
     `GetViewLines`, the views and the sections of a sheet, the export to STEP and a datum on a face read the owned kernel
     (item 5).
   - The plan of a preview counts a new placement as a new solid, and the window counts it as a new mesh, also in its section
     view.
   `Engine.Tests/Commands/PlaceBodyTests.cs` holds one test of each reader: it builds one scene with a `Rotate` and a
   `Translate`, and again with one `PlaceBody` of the same seven numbers, and the two answers must agree.
8. **The surface.** `body.created`, `body.changed` and the snapshot of `/events` give the member `placement`, an object of the
   seven numbers, or null at the identity, as `fault` is null (amends ADR-0037 item 8). `GetBodyInfo` and `GetBodies` give
   the placement too, so a client reads where a body is before it sends an absolute placement. `GetTessellation` gives the
   triangles in the Document, as before, so the window draws them with no change of its pipeline. A client that keeps the
   triangles of a body reads them again when `geometry` or `placement` differs from what it holds (amends the rule of
   ADR-0037 item 8, which names `geometry` only). The JSON of a placement holds the seven numbers only.
9. **The order (Q6 A and Q7 A).** P11.0 is this ADR with the field, the command and the test of each reader, in one commit.
   The joints, P11a and after, follow, and a joint gives a `PlaceBody` of the child (amends ADR-0126 item 3 and ADR-0157
   item 2). The measure of K10a runs again with `PlaceBody`:
   `One_Hundred_And_Fifty_Placements_Of_A_Module_About_Its_Hinge_Keep_Its_Box_And_Small_Numbers`. The probe of the
   determinism hash (`CrossPlatformProbe`) places two boxes and cuts the one by the other, and reads each body with its
   placement, so the four platforms compare the bits of the placements.

## Alternatives considered

- **A schema version 2 of `Rotate` and `Translate` (Q1 B).** Two meanings for one name, and a client must know the version.
- **A relative `MoveBody` (Q1 C).** A repeated command gives another result, and the errors of many moves add up.
- **Twelve doubles (Q2 B).** A check that nine numbers are a turn, and more numbers in the log for the same information.
- **A boolean in the frame of the Document (Q3 B).** Each boolean turns both shapes, so the numbers grow at each boolean.
- **A mesh body at the identity (Q4 B).** A part of an STL file then cannot take part in an assembly.
- **The triangles in the frame of the body and a model matrix in the window.** A drag of P11 can need it. Today each reader of
  the triangles (the window, the preview, the section view, the export to STL) reads the frame of the Document, so one change
  in `GetTessellation` keeps each of them right. A later task can add the matrix with no change of the log.

## Consequences

**Good:** a move keeps the identity and the exact shape of a body. After 150 turns of the module of K10a with `PlaceBody`, the
longest numerator of a coordinate has 19 digits (2576 with `Rotate`), the first read of the exact solid takes less than 1 ms
(about 2 s), and the box is within 4.6e-13 mm of the exact turn (1.6e-12 mm). An old log gives the same bodies and the same
solids, bit for bit.

**Bad:**
- Each reader of the geometry must apply the placement. A reader that forgets it is right for each body at the identity and
  wrong for a placed body. The tests of item 7 hold the readers of today; a new reader needs its own test. No gate finds a
  reader with no test.
- The result of a boolean of two placed bodies is not bit for bit the result of the same bodies moved with `Rotate` and
  `Translate`: the relative placement of the second body is a product in doubles. The difference is of the order of the
  rounding of a double.
- A datum on a face keeps the placement of its body at its place in the log. A later `PlaceBody` of the body does not move the
  datum, its sketch, or an extrude from that sketch.
- The backend keeps the moved operand of each move and each boolean of a placed body, as it keeps each consumed body
  (ADR-0021), until the session ends.
- A version of the app before P11.0 refuses a file that holds `PlaceBody`, because it does not know the command.
- Each event of a body has the new member `placement`, so its bytes change also for a body at the identity. The log does not
  change.

## History

- 2026-11-28 · TASK-0474 · The record, Accepted: the answers of the owner of 2026-11-25 (Q1 A) and of 2026-11-27 ("All A"
  for Q1 to Q7 of the proposal). Amends ADR-0037 item 8, ADR-0112 item 1, ADR-0126 item 3 and ADR-0157 item 2.
- 2026-11-28 · TASK-0479 · Amended by ADR-0193: item 3, a `PlaceBody` of the child of a joint is refused and a `PlaceBody` of
  another body places each body under it; item 5, the walks of the steps also read the placements that a step of a joint gives.
- 2026-11-28 · TASK-0477 · Findings E247 and E248 of the codebase review of 2026-11-28: item 8 gives the rule of a client that
  keeps the triangles (read again when `geometry` or `placement` differs), and says that the JSON of a placement holds the
  seven numbers only; `BodyPlacement.IsIdentity` is not written. Finding E249: `Relative` and `Compose` scale each quaternion
  by a power of two first (the answer Q2 A), so each product of two accepted placements gives a turn.
