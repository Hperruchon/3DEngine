---
id: 0033
title: The sketch model in the engine
status: Accepted
topic: Contracts, sketch
date: 2026-10-28
supersedes: []
superseded-by: []
amends: ['0012', '0021', '0031']
amended-by: []
affects:
  - Engine.Contracts/Document.cs
  - Engine.Contracts/Handlers/ICommandHandler.cs
  - Engine.Contracts/Sketch/**
  - Engine.Core/CommandBus.cs
  - Engine.Core/StepHistory.cs
  - Engine.Core/Sketch/**
  - Engine.Core/Commands/**
  - Engine.Core/Queries/**
  - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
  - Engine.Api.Http/WebSockets/WireMessage.cs
enforced-by: Engine.Tests/Sketch/SketchModelTests.cs (TASK-0100 creates it)
---

# ADR-0033 — The sketch model in the engine

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Phase K2a gives the engine a 2D sketch: the first part of the milestone "CAD on one part" (a sketch with
constraints, an extrude, and a solid that follows an edit of the sketch). The owner answered the questions Q1 to
Q9 of `docs/reviews/2026-10-28-b-codebase-review.md` on 2026-10-28, A to each. Section 5 of that review gives the
research.

Four conditions make this a decision and not a preference:

- **The Document holds bodies only.** The events, the snapshot, the step of an undo (`Engine.Core/StepHistory.cs`)
  and the result of a handler know body records only. A sketch as a body breaks anti-objective 9 (no universal body
  type) and the consumption of ADR-0021. So the public shape of `Engine.Contracts` changes, which needs this record.
- **A parameter is a scalar.** The binder, the codec of the document file and the command line pass scalars only
  (ADR-0016). A command with a whole sketch needs objects and arrays; a command for each item does not (Q2).
- **Determinism.** A command must not store an angle, and code of the log path must not call a transcendental
  function (`CLAUDE.md`, rules 1 and 2).
- **K0b must not rewrite the log.** K0b makes the solid of an extrude follow an edit of its sketch. If a sketch item
  changes its identifier at an edit, or an extrude records coordinates, each saved file needs a migration then.
  The identity rules must be right before the first saved sketch (`CLAUDE.md`, "Method").

## Decision

1. **The datums.** A sketch lies on a datum. Three datums exist, the origin planes, each with a fixed identifier and
   a right-handed frame of an origin, an axis u, an axis v and a normal n = u × v:

   | Datum | Identifier | u | v | n |
   |---|---|---|---|---|
   | XY | `00000000-0000-0000-0000-000000000001` | +X | +Y | +Z |
   | XZ | `00000000-0000-0000-0000-000000000002` | +X | +Z | −Y |
   | YZ | `00000000-0000-0000-0000-000000000003` | +Y | +Z | +X |

   The origin of each one is (0, 0, 0). A sketch records the identifier of its datum, so a later command that makes
   a datum plane is additive (Q4). `Engine.Contracts/Sketch/Datums.cs` holds the three.
2. **The five entities.** The point, the line segment, the frame of a sketch, the circle and the arc (Q1). K2a gives
   the point, the line and the frame; phase K2 gives the circle and the arc after K2b. The ellipse of
   `docs/CHARTER.md` waits for a later phase.
   - A **point** has an identifier and a position (x, y) in the plane, in the length unit of the Document.
   - A **line** has an identifier and references two different points: a start and an end (Q3).
   - The **frame** of a sketch is its origin point and its two axes. The origin is a point with the identifier of
     the sketch and the position (0, 0); no command moves or deletes it. "Horizontal" means parallel to u, and
     "vertical" means parallel to v.
   - A **circle** references a centre point and has a radius. An **arc** references a centre, a start and an end
     point, counter-clockwise from the start; its two radii are equal by an implicit equation. No entity stores an
     angle.
3. **The fourteen constraints.** Each one has a kind, an ordered list of items, and a list of values. Its residual
   uses `+ - * / sqrt` only, so the engine computes it on each platform with the same bits. With d = b − a for a
   line from a to b, cross(p, q) = pₓq_y − p_yqₓ and dot(p, q) = pₓqₓ + p_yq_y:

   | # | Kind | Items | Values | Residual is zero when |
   |---|---|---|---|---|
   | 1 | `coincident` | point, point | — | the two positions are equal |
   | 2 | `horizontal` | line | — | d_y = 0 |
   | 3 | `vertical` | line | — | dₓ = 0 |
   | 4 | `distance` | point, point; point, line; or line | length ≥ 0 | the distance of the two points, of the point to the line, or the length of the line equals the length |
   | 5 | `fixed` | point | x, y | the position equals (x, y) |
   | 6 | `pointOn` | point, line or circle or arc | — | cross(d, p − a) = 0, or the distance to the centre equals the radius |
   | 7 | `midpoint` | point, line | — | p = (a + b) / 2 |
   | 8 | `parallel` | line, line | — | cross(d₁, d₂) = 0 |
   | 9 | `perpendicular` | line, line | — | dot(d₁, d₂) = 0 |
   | 10 | `equal` | line, line; or circle or arc, circle or arc | — | the two lengths, or the two radii, are equal |
   | 11 | `tangent` | line, circle or arc; or circle or arc, circle or arc | 1 or −1 for two circles | the distance of the centre to the line equals the radius; or the distance of the centres equals r₁ + r₂ (1) or the difference (−1) |
   | 12 | `symmetric` | point, point, line | — | the midpoint of the two points is on the line, and dot(q − p, d) = 0 |
   | 13 | `radius` | circle or arc | radius > 0 | the radius equals the value; a diameter is a display of the client |
   | 14 | `angle` | line, line | cos θ, sin θ | cross(d₁, d₂)·cos θ − dot(d₁, d₂)·sin θ = 0, with dot(d₁, d₂)·cos θ + cross(d₁, d₂)·sin θ > 0 |

   K2a gives the kinds 1 to 5 (the set of K3a). Phase K2 gives the kinds 6 to 14. For `angle` the client computes the
   cosine and the sine of the angle that a person types, and the command records the two numbers (Q5, ADR-0032 item
   2). The handler refuses a pair whose cos² + sin² differs from 1 by more than 1e-9, and records the pair that it
   normalises with `sqrt`.
4. **The record of a sketch.** `SketchRecord` holds the identifier, the identifier of the datum, a name (null until
   a rename, with the rules of ADR-0028 item 3), the points, the lines, the circles, the arcs and the constraints.
   The Document holds one record for each live sketch in `Document.Sketches`. A record never changes: an edit gives
   a new record with the same identifier. The positions of the points are a projection of the record and not an
   input: in K2a each position comes from the command that adds or moves the point, and from K3a the solver writes
   them (Q6). The log holds the inputs only.
5. **Identity.** The identifier of a sketch, of a point, of a line and of a constraint is the identifier of the
   command that adds it (ADR-0012 §4). No edit changes an identifier, and each command names an item by its
   identifier, never by its place in a list. A sketch is not consumed as a body is (ADR-0021): an edit replaces the
   record and keeps the identifier, so a later feature that references the sketch stays valid.
6. **The commands of K2a.** Each one has scalar fields only, lives in `Engine.Core/Commands/`, and has an encoder for
   the document file (ADR-0015 item 11). Each one is a step of the history.

   | Command | Fields | Effect |
   |---|---|---|
   | `CreateSketch` | `datumId`, `name` (optional) | a new sketch with its frame; output `sketchId` |
   | `AddSketchPoint` | `sketchId`, `x`, `y` | a new point; output `pointId` |
   | `AddSketchLine` | `sketchId`, `startPointId`, `endPointId` | a new line; output `lineId` |
   | `AddSketchConstraint` | `sketchId`, `kind`, `first`, `second` (optional), `third` (optional), `value` (optional), `secondValue` (optional) | a new constraint; output `constraintId` |
   | `SetSketchConstraintValue` | `sketchId`, `constraintId`, `value`, `secondValue` (optional) | new values for a constraint |
   | `MoveSketchPoint` | `sketchId`, `pointId`, `x`, `y` | a new position for a point |
   | `DeleteSketchItem` | `sketchId`, `itemId` | removes a point with each line that references it, a line, or a constraint, and each constraint that references a removed item |

   A refusal gives a new code of the namespace `SKETCH`: `E-SKETCH-NOT-FOUND` (no live sketch with the identifier),
   `E-SKETCH-DATUM-NOT-FOUND`, `E-SKETCH-ITEM-NOT-FOUND`, `E-SKETCH-LINE-INVALID` (the two points are one point),
   and `E-SKETCH-CONSTRAINT-INVALID` (an unknown kind, items of the wrong kind or count, a value that is absent,
   negative or not a unit pair, or a kind that this phase does not give). The frame and the origin point are not
   deletable.
7. **The query.** `GetSketch { sketchId }` gives the record, and the residual of each constraint. The residual is
   for the person and for K3a; the engine applies a command with a constraint that the positions do not satisfy.
8. **The result of a handler.** `CommandHandlerResult` gains `CreatedSketches` and `ChangedSketches`, two lists of
   `SketchRecord`, empty by default. A handler changes only a live sketch, gives each one time, and keeps its
   identifier and its datum; the bus refuses a list that breaks the rule. The bus adds or replaces each record in
   the commit section.
9. **The events.** Three new kinds: `sketch.created` with `sketchId`, `datumId` and `name`; `sketch.changed` with
   `sketchId`; `sketch.removed` with `sketchId`. A client reads the content with `GetSketch`. The events of one
   commit come in this order: `command.applied`, each `body.created`, each `body.consumed`, each `body.removed`, each
   `body.changed`, each `sketch.created`, each `sketch.removed`, each `sketch.changed`. The snapshot of
   `subscription.reset` gains `sketches`, with `sketchId`, `datumId` and `name` for each live sketch.
   `/schema/events` lists the kinds and their fields.
10. **Undo and redo.** A step records each sketch that it created, and the record before and after of each sketch
    that it changed. An undo removes each created sketch (`sketch.removed`) and gives back each record before
    (`sketch.changed`); a redo does the reverse (`sketch.created`, `sketch.changed`). So a sketch command gets undo
    with no inverse, as a body command does (ADR-0031). One undo reverses one command: a rectangle of four points,
    four lines and four constraints is twelve steps. A group of steps waits for a measured need.
11. **Determinism.** The sketch code lives in `Engine.Core/Sketch/` and `Engine.Core/Commands/`, which the
    determinism gate reads. It uses `+ - * / sqrt` only.
12. **The exception to the rule of a new command.** A sketch is a new kind of output of a commit, so K2a also
    changes `Document`, `CommandHandlerResult`, the bus, the history, the snapshot and the schema of the events, one
    time, as ADR-0028 item 9 did for a change of a body. A later sketch command uses the two lists and changes no
    central file. The rule of `DispatchSurfaceGateTests` does not change, and a test injects a host that names
    `CreateSketch` and watches the gate fail.
13. **The desktop.** The desktop draws each live sketch, read only, as lines of the UI layer over the scene: each
    point and each line, projected with the camera from the frame of its datum (Q9). The UI code stays in
    `3DEngine/Ui/` (ADR-0027). K2b gives the tools.

This record amends ADR-0012 §6 (the snapshot), ADR-0021 item 5 (the order of the events of a commit) and ADR-0031
items 3 to 5 (a step records sketch records, and an undo gives sketch events).

## Consequences

**Good:** A sketch has stable identifiers from its first saved file, so the extrude of K0a can record the identifier
of its sketch (Q7), and K0b needs no new command and no migration. The commands fit the binder, the file and the
command line of today. An undo of a sketch command needs no inverse. The fourteen constraints have records now, so
phase K2 is additive.

**Bad:** A public shape of `Engine.Contracts` changes: a collection of the Document, the records of a sketch, two
lists of `CommandHandlerResult`, three event kinds, and a field of the snapshot. A client that reads the payloads
strictly must accept them. The rule of a new command has a second recorded exception. A tool that adds many items
gives many steps of undo. Each edit copies the record of its sketch, so the cost of an edit grows with the size of
the sketch.

**Next:** TASK-0100 (K2a) applies items 1 to 13 for the point, the line, the frame and the kinds 1 to 5. K2b gives
the tools of the desktop. K3a writes the solved positions. K0a records `sketchId` and a distance. Phase K2 gives
the circle, the arc and the kinds 6 to 14.

## History

- 2026-10-28 · TASK-0100 · Proposed, with the answers Q1 to Q9 of the owner of 2026-10-28 to the second codebase
  review of that date.
- 2026-10-28 · TASK-0100 · The owner accepted the record on 2026-10-28 with no change.
