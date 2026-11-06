---
id: 0053
title: The desktop starts a sketch on a face, and draws a sketch on a datum of the Document
status: Amended
topic: Desktop, sketch, feature graph
date: 2026-11-03
supersedes: []
superseded-by: []
amends: ['0029', '0034', '0051']
amended-by: ['0059']
affects:
  - Engine.Core/Queries/GetFaceAtQuery.cs
  - Engine.Core/Queries/GetFaceAtQueryHandler.cs
  - Engine.Core/Sketch/FaceFinder.cs
  - 3DEngine.App/SketchOverlay.cs
enforced-by: Engine.Tests/Sketch/FaceFinderTests.cs (TASK-0171 creates it)
---

# ADR-0053 — The desktop starts a sketch on a face, and draws a sketch on a datum of the Document

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

K0g (ADR-0051) gave the datum on a face of an extrude or a box, and K0h (ADR-0052) gave the references of a sketch to
an edge or a vertex. ADR-0051 item 11 kept the desktop out: the desktop draws only a sketch on an origin plane, and a
person cannot start a sketch on a face. K0i closes that gap, so a person gets the features that an agent has since
K0g (objective 9), and K0 ends, which K4 needs.

The pick of the desktop (ADR-0029 item 1) gives the body and the distance of the hit, not the face. The owner gave the
order "go ahead" on 2026-11-03 and answered four questions on the same day, each with the recommended answer:

- **The face.** An engine query names the face under a tap, so the engine stays the authority for design truth, and
  an agent can use the same query.
- **The tap.** A tap on a face reuses a datum with the same reference and the same frame, or creates one, and then
  creates the sketch.
- **A chain.** The query walks back through each `Translate`, and through a `Subtract` to its minuend. A face that it
  cannot name gives a message and no sketch.
- **The references.** The desktop draws the lines and the points of a reference in a dim colour. A tool that adds a
  reference by a tap waits for a later phase.

## Decision

1. **The query `GetFaceAt`.** It takes `bodyId`, the point `x`, `y`, `z` of the hit, and the normal `nx`, `ny`, `nz` of
   the hit triangle, toward the eye. It gives `reference` (`featureId`, `element`, `itemId`), as `CreateDatum` takes it,
   and `datumId`: the first datum of `Document.Datums` in the order of the log with an equal reference, no fault and
   the frame of the face now, or null.
2. **The walk (Q of a chain).** From the body, the query takes the command that creates it: a `Translate` gives its
   body, and a `Subtract` gives its minuend, until it reaches an `Extrude` or a `CreateBox`. Each other command gives
   `E-DATUM-FACE-NOT-FOUND`. A face of the subtrahend, for example the wall of a hole, is not a face of the minuend, so
   it gives the same code. Since K4c the face comes from the kernel solid of the body, and a wall of a hole is a face
   (ADR-0059 item 5).
3. **The match.** The query resolves each face of the feature with `DatumResolver.Resolve` at the end of the log, so the
   frame includes each `Translate` of the chain (ADR-0051 item 2). The order is `start`, `end`, the side of each line
   of the record of the sketch, and for a box `+x`, `-x`, `+y`, `-y`, `+z`, `-z`. The first face matches when:
   - the dot product of its normal and the normal of the hit is at least 0.999;
   - the distance of the point from its plane is at most t = 1e-6 × (1 + the largest magnitude of a coordinate of the
     point and of the origin of the face);
   - the point is on the face within t: inside the loop of the profile for a cap, on the rectangle of the line and the
     height for a side, and on the rectangle of the two other half sizes for a face of a box.
   A curved side is not a face that a datum names (ADR-0051 item 1), so a tap on it gives `E-DATUM-FACE-NOT-FOUND`.
4. **Determinism.** The query is a read: it does not enter the log. The commands that follow record the reference and
   the identifiers only, and no number of the pick, so a replay does not depend on the ray.
5. **The pick (amends ADR-0029 item 1).** A hit gives the point and the normal of the hit triangle too. The desktop
   turns the normal toward the eye, so the order of the vertices of the tessellation does not matter.
6. **The tap on a face (Q of the tap, amends ADR-0034 item 6).** The window of the sketch gains the button `Face` beside
   `XY`, `XZ` and `YZ`. It arms the next tap of the mode Scene. A tap on a body calls `GetFaceAt`. With a `datumId`,
   `CreateSketch` takes it; with null, `CreateDatum` with the reference comes first, then `CreateSketch` on the new
   datum. The new sketch becomes the active sketch, and the camera looks along the normal of the face. A tap that hits
   no body, or a refusal, ends the arm and shows the message. A new datum and its sketch are two commands, so two
   undo steps remove them.
7. **The plane of a sketch (amends ADR-0051 item 11).** `GetSketch` gains `frame`: the frame of the datum of the sketch
   now, an origin plane or the last frame of a datum of the Document. The overlay, the tool, the mark of the snap and
   the camera of the desktop use it: a position (x, y) of the sketch is the point O + x U + y V. The projection of the
   desktop reads a sketch again when the frame of its datum changes (`datum.changed`).
8. **The references (Q of the references).** The overlay draws each line and each point of a reference in a dim colour.
   The engine refuses a move of a point of a reference (ADR-0052 item 5), and the window shows the refusal.
9. **The codes.** `E-DATUM-FACE-NOT-FOUND` is the only new code.
10. **Out of this record.** A tool that adds a reference by a tap on an edge or a vertex, an image on a datum of the
    Document (ADR-0045 keeps the origin planes), a sketch on a curved face (K5), and a datum that a person types as
    numbers.

## Consequences

- A person can start a sketch on a face of an extrude or a box, also after a `Translate` or a `Subtract`, and the desktop
  draws it where the face is.
- An agent can ask the engine which face is under a point, with the same query.
- The result of `GetSketch` gains one field, and the pick gains the point and the normal of a hit.

## History

- 2026-11-03 · TASK-0171 · Accepted with the four answers of the owner of 2026-11-03.
- 2026-11-05 · TASK-0185 · ADR-0059 item 5 amends items 2 and 3: the face comes from the kernel solid of the body.
