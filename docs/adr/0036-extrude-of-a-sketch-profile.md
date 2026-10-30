---
id: 0036
title: The extrude of a closed profile of a sketch
status: Amended
topic: Contracts, geometry, sketch
date: 2026-10-29
supersedes: []
superseded-by: []
amends: []
amended-by: ['0037', '0040']
affects:
  - Engine.Contracts/Geometry/**
  - Engine.Core/Commands/ExtrudeCommand.cs
  - Engine.Core/Commands/ExtrudeCommandHandler.cs
  - Engine.Core/Sketch/SketchProfile.cs
  - Engine.Geometry.Manifold/**
enforced-by: Engine.Tests/Sketch/ExtrudeTests.cs (TASK-0103 creates it)
---

# ADR-0036 — The extrude of a closed profile of a sketch

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase K0a extrudes a closed profile of line segments over Manifold. It is the second part of the milestone
"CAD on one part": a sketch with constraints, an extrude, and a solid that follows an edit of the sketch. The owner
chose on 2026-10-28 (Q7 of the second codebase review of that date) that the command records the identifier of its
sketch and a distance, not coordinates, and that the handler reads the current sketch. K0b then makes the solid
follow an edit; this record does not decide K0b.

Six facts constrain the design:

- **The native export (KD5).** `manifoldc.h` of Manifold 3.5.2 declares `manifold_simple_polygon`,
  `manifold_polygons` and `manifold_extrude(mem, polygons, height, slices, twist_degrees, scale_x, scale_y)`.
  On 2026-10-29 a search of the binaries of `Engine.Geometry.Manifold.Native` 3.5.2.1 found each name in
  `win-x64`, `linux-x64` and `osx-arm64`. The first commit of the code proves each export with a load on each runner.
- **The capabilities (ADR-0012 item 1).** A new operation is its own interface. The managed stub does not implement
  it, and a handler gets `E-GEOM-CAP-MISSING` from it. So `Engine.Contracts` changes, which needs this record.
- **The frame of a datum (ADR-0033 item 1).** Manifold extrudes a polygon of the plane XY along +Z. The three datums
  have axes of 0 and ±1 only, so the move to the frame of a datum multiplies by 0 and ±1 only, which is exact.
- **Determinism.** The handler reads the solved positions of the record (ADR-0035). A replay reads the same record at
  the same place of the log, so it gives the same polygon. No arc exists, so no sine is necessary.
- **The polygon of Manifold.** Manifold reads a polygon with a counter-clockwise order as a solid, and a clockwise one
  as a hole. A person can draw a profile in each order.
- **ADR-0006.** The engine rejects a command as one unit, with a code. An extrude of a profile that is not closed must
  fail before the backend.

### Questions for the owner

**Q1. The profile.**
- A: The lines of the sketch form one closed loop: each point that a line uses is the end of exactly two lines, the
  lines form one cycle, and no two lines that do not share a point cross. A point with no line does not count. The
  handler orders the loop counter-clockwise.
- B: Several loops, and a loop inside another one is a hole.
- C: The lines that the person selects form the loop.

**Recommended: A.** The milestone needs one profile, and the tools of K2b draw one chain. B needs a rule for nested
loops and more tests; it is additive later, because the command does not change. C needs a list of identifiers in a
command, and the binder takes scalars only (ADR-0016).

**Q2. The distance.**
- A: `distance` is a finite number that is not zero. A positive value extrudes along the normal n of the datum, and a
  negative value along −n.
- B: `distance` is greater than zero, and the solid always goes along n.

**Recommended: A.** A person on the plane XZ sees −Y as the normal, and a sign is the shortest way to go the other way.
The backend always gets a positive height and a frame, so it needs no case of its own.

**Q3. A sketch that is not solved.**
- A: The handler refuses the extrude with the new code `E-SKETCH-PROFILE-INVALID` and a message that says that the
  sketch is not solved.
- B: The handler extrudes the positions as they are.

**Recommended: A.** A solid of a sketch that conflicts is not the part that the person asked for, and K0b must not
rebuild from it. The person sees "Not solved" in the window already (ADR-0035 item 8).

**Q4. The desktop.**
- A: The window "Sketch" gets the button "Extrude". It opens the pad with one field, `distance`, with the text `10`.
  An apply sends `Extrude`, ends the sketch mode, and the new body becomes the selection.
- B: No desktop change. An agent extrudes through HTTP.

**Recommended: A.** The milestone is a part that a person makes. The pad of ADR-0032 already holds one field.

## Decision

The decision below applies the recommended answers: Q1 A, Q2 A, Q3 A and Q4 A.

1. **The command.** `Extrude` has the fields `sketchId` (a guid) and `distance` (a number), and the output `bodyId`.
   The body has the identifier of the command (ADR-0012 item 4) and the kind `Extrude`. The command records no
   coordinate. It lives in `Engine.Core/Commands/` with an encoder for the document file (ADR-0015 item 11).
2. **The profile (Q1).** `Engine.Core/Sketch/SketchProfile.cs` finds the loop with `+ - * /` and comparisons only:
   - Each point that a line uses must be the end of exactly two lines, and the lines must form one cycle.
   - No two lines that share no point may cross or touch.
   - The loop starts at the start of the first line of the record and follows the lines in the order of the record.
   - When the area of the loop is negative, the loop turns round, so it is counter-clockwise.
   - The area must not be zero.
3. **The distance (Q2).** A distance that is zero or not finite gives `E-GEOM-INVALID-PARAM`. The backend gets the
   height |distance|, and the frame of the datum moved by min(0, distance) along n.
4. **The refusals (Q3).** No live sketch with the identifier gives `E-SKETCH-NOT-FOUND`. A profile that breaks item 2,
   and a sketch that is not solved (ADR-0035 item 8), give the new code `E-SKETCH-PROFILE-INVALID`, with a message
   that names the reason. A backend without the capability gives `E-GEOM-CAP-MISSING`, and a native failure gives
   `E-GEOM-NATIVE-OP`.
5. **The contract.** `Engine.Contracts/Geometry/` gains:
   - the interface `IExtrudeOps` with one method, `Extrude(BodyHandle handle, ExtrudeParameters parameters)`;
   - the value type `ExtrudeParameters`, with the loop as a list of (x, y) in the plane, the height, the origin and
     the three axes u, v and n of the frame;
   - the flag `BackendCapabilities.Extrude`.
   The managed stub does not implement it (ADR-0012 item 7).
6. **The backend.** `ManifoldGeometryBackend` makes one simple polygon of the loop, extrudes it with no slice, no twist
   and the scale 1, and moves the solid with `manifold_transform` by the matrix of the columns u, v and n and the
   origin. Each entry of the matrix is 0 or ±1, and the origin is (0, 0, 0) moved along n, so the move is exact.
7. **The sketch.** The extrude does not consume the sketch and does not change it (ADR-0033 item 5). The sketch stays
   live and the desktop still draws it. K0b decides what an edit of the sketch does to the solid.
8. **Undo.** The extrude is one step: an undo removes the body (ADR-0031).
9. **The desktop (Q4).** The window "Sketch" gets "Extrude". It opens a pad of one field, `distance`, with the text
   `10`. An apply sends `Extrude`; a success ends the sketch mode and selects the new body, and a rejection stays in
   the pad with its code.
10. **No new event kind.** The extrude gives `body.created`, as each body command does.

## Consequences

**Good:** A person draws a profile, constrains it and extrudes it, and an agent does the same over HTTP. The log holds
the identifier of the sketch and a distance, so K0b needs no new command and no migration. The move to the frame is
exact on each platform.

**Bad:**
- A public shape of `Engine.Contracts` changes: one interface, one value type and one flag.
- One loop only: a profile with a hole waits for a later phase.
- The solid does not follow an edit of the sketch until K0b. An edit after the extrude leaves the solid as it was.
- An extrude of a sketch on a datum other than the three origin planes needs a frame of its own, which waits for a
  datum command.

**Next:** TASK-0103 applies this record. K0b makes the solid follow its sketch.

## History

- 2026-10-29 · TASK-0103 · Proposed, with the questions Q1 to Q4 for the owner.
- 2026-10-29 · TASK-0103 · The owner accepted the record on 2026-10-29 with Q1 to Q4 A, and no change.
- 2026-10-29 · TASK-0103 · The code applies items 1 to 8 and 10 in the engine and over Manifold. A load of each native
  function on each runner replaces the search of KD5.
- 2026-10-29 · TASK-0108 · ADR-0037 amends item 7: an edit of the sketch rebuilds each live extrude of it; the sketch is still not
  consumed.
- 2026-10-29 · TASK-0106 · Item 2 gains the reason of a line of length zero, whose two points are at one position
  (finding E92 of the codebase review of 2026-10-29); before, the check of a touch gave the reason. Item 3 has no
  smallest distance: each finite distance that is not zero applies, and the backend decides on a solid of almost no
  height or area.
