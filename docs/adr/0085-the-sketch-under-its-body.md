---
id: 0085
title: The active sketch of a live extrude moves out of its body, and the camera stays
status: Amended
topic: Clients, rendering
date: 2026-11-07
supersedes: []
superseded-by: []
amends: ['0034']
amended-by: ['0120']
affects:
  - 3DEngine.App/SketchLift.cs
  - 3DEngine.App/SketchActions.cs
  - 3DEngine.App/NativeThreeDEngine.cs
enforced-by: 3DEngine.ImageTests/SketchLiftTests.cs
---

# ADR-0085 — The active sketch of a live extrude moves out of its body, and the camera stays

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner ordered the layout of the redesign slice by slice, with no change of the contract (2026-11-06, evening), and on
2026-11-07 put the next redesign slices after the STL demonstration: after the rows without a panel (ADR-0084) comes "the
sketch under its body". The "mix" direction of the designer brief (the idea "Diegetic") draws step 3 of its main flow so:
"The sketch moves into view under the body, with its values written on it. The camera does not move, and the ring now
holds sketch tools." Its picture shows the sketch of a plate below the plate, with a dashed line from each corner of the
sketch to the corner of the body that it makes.

Today "Edit" of a sketch turns the camera to look along the normal of its plane (ADR-0034 item 6, ADR-0053 item 6). The
sketch of an extrude lies on the base of its body, so the face of the body covers it, and the turn of the camera loses the
view of the part. The engine gives each feature with its inputs (`GetFeatures`, ADR-0054), and the identifier of an
extrude is the identifier of its body.

A position (x, y) of a sketch is the point O + x U + y V of its plane (ADR-0053 item 7). A move of the plane along its
normal keeps the position of each point in the plane: a pick on the moved plane gives the same (x, y) as a pick of the
same point on the plane of the record.

### Q1. Which sketch moves

- **A.** The active sketch, when a live extrude in effect is made from it: the feature of the kind `Extrude` that reads
  the sketch, not consumed, with a state "correct", and whose body the renderer holds.
- **B.** Each sketch of a body, also when it is not active.

A changes the view of the sketch that the person edits only; the other sketches stay where the document puts them.
**Recommended: A.**

### Q2. The camera

- **A.** The camera does not move for a sketch that moves. Another sketch turns the camera as before.
- **B.** The camera turns as before, and the sketch also moves.

A is step 3 of the brief, and an oblique view shows the move; a view along the normal hides a move along the normal.
**Recommended: A.**

### Q3. Where the sketch goes

- **A.** Along the normal of its plane, out of the box of the body, to the side away from the body, a quarter of the
  diagonal of the box past it.
- **B.** A fixed distance below the body on the screen.

A is a property of the model, the same in each view, and a pick needs a plane in the space of the Document. The brief
draws the axis Z up, so its sketch is under the body; the view of the desktop draws the axis Y up, so the sketch of a
plate on XY moves behind it there. **Recommended: A.**

## Decision

The standing order of 2026-11-05 gives A for each question.

1. **The sketch that moves (Q1 A, Q2 A).** "Edit" of a sketch that makes a live extrude keeps the camera; this amends
   ADR-0034 item 6 for that sketch. Each other sketch turns the camera as before. The body is the feature of the kind
   `Extrude` in effect that reads the sketch, not consumed, with the state "correct", and whose box the renderer holds.
2. **The moved plane.** `SketchLift` gives the sketch with its plane moved. The drawing of the sketches and
   `SketchActions.Current` both use it, so each tap, each mark of a snap and each selection uses the plane that the person
   sees, and each command gets the position in the plane, as on the plane of the record. "Finish" and each other "Edit"
   end the move.
3. **The place (Q3 A).** The plane moves along its normal: past the corner of the box of the body that is farthest on the
   side away from the body, by a quarter of the diagonal of the box. The client takes the box of each frame, so the
   place follows a change of the body.
4. **The links.** A quiet line goes from each moved point to its place on the plane of the record, so that the person
   sees which body the sketch makes.
5. **No contract change.** The move is presentation (ADR-0007): no command, no event and no log holds it.

## Consequences

**Good:** the person edits the sketch of a part beside the part, with the view of the part, as the brief draws it.

**Bad:** in the view of the desktop, with the axis Y up, the sketch of a plate on XY moves behind the plate, not under it.
The ring still holds the tools of the body, and the values of the sketch are not yet on its items. A sketch of a body that
another feature consumed opens as before.

**Next:** the tools of the sketch on the ring, and the values of the sketch on its items.

## History

- 2026-11-07 · TASK-0273 · The record, Accepted under the standing order.
- 2026-11-07 · TASK-0273 · Shipped at v0.256.
- 2026-11-11 · TASK-0347 · ADR-0120 item 5 amends item 2: the active sketch is a plate with its label, and, under its body,
  the caption "SKETCH · UNDER THE BODY IT MAKES".
