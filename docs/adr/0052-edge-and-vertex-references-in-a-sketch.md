---
id: 0052
title: The edges and the vertices of a feature as references in a sketch
status: Accepted
topic: Contracts, feature graph, sketch
date: 2026-11-03
supersedes: []
superseded-by: []
amends: ['0033', '0035', '0036', '0051']
amended-by: []
affects:
  - Engine.Core/Commands/AddSketchReferenceCommand.cs
  - Engine.Core/Commands/AddSketchReferenceCommandHandler.cs
  - Engine.Core/Sketch/DatumResolver.cs
enforced-by: Engine.Tests/Sketch/SketchReferenceTests.cs (TASK-0167 creates it)
---

# ADR-0052 — The edges and the vertices of a feature as references in a sketch

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0051 gave the reference to a face and a datum on it (K0g), and it reserved the names of the edges and the vertices
of an extrude and of a box for K0h. A face gives a plane for a sketch. An edge or a vertex gives a line or a point that
a sketch can constrain to, for example a hole that stays at a distance from an edge of the part.

The owner gave the order "start K0h" on 2026-11-02 and answered four questions on the same day, each with the
recommended answer:

- **The projection.** A vertex adds a point, and a straight edge adds two points and a line, at the orthogonal
  projection onto the plane of the sketch. An edge that projects to one point is refused. A curved edge waits for K5.
- **The solver.** A reference point has no variable, as the origin of the frame. A person can constrain to it, but
  cannot move it or delete it alone. The record of the sketch gains a list of references.
- **A cycle.** The engine refuses a reference whose feature depends on the sketch.
- **A failure.** The edit applies, the points keep their last positions, and the reference gets a fault.

## Decision

1. **The elements (ADR-0051 item 1).** A reference of a sketch names an edge or a vertex:
   - of an `Extrude`: `startEdge` and `endEdge` with a line of the profile, the line at the start cap or at the end
     cap; `sideEdge` with a point, the edge from the point at the start cap to the point at the end cap;
     `startVertex` and `endVertex` with a point. A point is an end of a line or an arc of the profile. An arc for
     `startEdge` or `endEdge` is a curved edge, and waits for K5;
   - of a `CreateBox`: an edge by two signed axes in the order x, y, z, for example `+x-z`, which runs along the third
     axis from −h to +h; a vertex by three signed axes, for example `+x-y+z`.
   A reference resolves at its place in the log, as ADR-0051 item 2 says, and a `Subtract` passes its carrier (item 8).
2. **The command.** `AddSketchReference` has the fields `sketchId`, `featureId`, `element` and `itemId` (optional), and
   the output `referenceId`, the identifier of the command. A vertex adds one point with the identifier of the
   command. An edge adds a line with the identifier of the command and its two points, whose identifiers are
   `GeometryKeys.Derive(command, a)` for the first end and `GeometryKeys.Derive(command, b)` for the second end, with
   a = `00000000-0000-0000-0000-000000000001` and b = `00000000-0000-0000-0000-000000000002`. So a replay gives the
   same identifiers. The command adds no constraint.
3. **The projection.** With the frame (O, U, V) of the datum of the sketch, a point P of the space goes to
   (dot(P − O, U), dot(P − O, V)). The code uses `+ - *` only. An edge whose two ends go to one position is refused at
   the add, and gives a fault at a rebuild.
4. **The record.** `SketchRecord` gains `References`, empty by default: each `SketchReference` has its identifier, the
   target (`DatumReference`), the points that it drives, its line or null, and a fault or null. The points and the line
   are in the lists of the record as each other point and line, so each constraint of ADR-0033 item 3 can name them.
   This amends ADR-0033 items 4 and 6.
5. **The solver (amends ADR-0035 items 3 and 4).** A point of a reference has no variable: the solver reads its
   position from the record, as for the origin. `MoveSketchPoint` refuses it with `E-SKETCH-ITEM-NOT-FOUND`, as for
   the origin. `DeleteSketchItem` of the identifier of the reference removes the reference, its points and its line,
   with each item and constraint that ADR-0033 item 6 removes with them; a delete of one point of an edge alone is
   refused with `E-SKETCH-ITEM-NOT-FOUND`.
6. **The profile (amends ADR-0036 item 2).** A line of a reference is not an edge of the profile: the profile ignores
   it, as a construction line. A line of a person that ends at a point of a reference is an edge of the profile.
7. **A cycle (Q of a cycle).** `AddSketchReference` refuses with `E-SKETCH-REFERENCE-CYCLE` a feature that depends on
   the sketch: an extrude of the sketch, or an extrude whose sketch lies on a datum or holds a reference whose feature
   depends on the sketch, at each depth. The other refusals give `E-SKETCH-REFERENCE-INVALID` with the reason: the form
   of item 1, a reference that does not resolve now, a curved edge, and an edge that projects to one point.
   `CreateDatum` keeps the faces only (ADR-0051 item 7), and its message names `AddSketchReference` for an edge or a
   vertex.
8. **The rebuild (amends ADR-0051 item 6).** After an edit, the engine takes each `CreateDatum` and each
   `AddSketchReference` in effect in the order of the log, from the state after the edit. A reference whose points
   move changes its sketch, the solver solves the sketch again, and each extrude of the sketch is a target of the
   rebuild of ADR-0039. The result gives each sketch that changes. A reference that does not resolve keeps its points,
   gets the fault `W-SKETCH-REFERENCE-FAILED`, and the edit applies; a reference that resolves again loses its fault.
   A projected point that is not finite does not resolve. The warning comes when the fault is new or changes; a fault
   that stays gives no new warning, and the record shows it.
9. **The contract.** `Engine.Contracts/Sketch/SketchRecord.cs` gains `SketchReference` and `SketchRecord.References`.
   No event kind changes: a change of a reference is a `sketch.changed`. `GetSketch` gives the references.
10. **Determinism.** The carrier, the moves and the projection use `+ - * / sqrt` only. The log of the determinism hash
    of P0.67 gains a reference to an edge of the extrude on the side of a slanted line, and an edit of its base.
11. **Out of this record.** A curved edge (K5), a reference in the desktop (K0i), and a reference that a person
    converts into a line of the profile.

## Consequences

- A person or an agent can put a point at a distance from an edge of the part, and the point follows an edit of the
  part.
- A sketch can show lines that are not part of its profile. Until K0i the desktop draws them as the other lines.
- A public shape of `Engine.Contracts` changes: a record and a member of the record of a sketch.

## History

- 2026-11-03 · TASK-0167 · Accepted with the four answers of the owner of 2026-11-02.
- 2026-11-03 · TASK-0169 · Item 8, from findings E125 and E127 of the codebase review of 2026-11-03 and the answer
  Q2 A of the owner: a projected point that is not finite does not resolve, and a fault that stays gives no new
  warning.
