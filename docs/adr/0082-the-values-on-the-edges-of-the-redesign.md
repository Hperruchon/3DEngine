---
id: 0082
title: A value that a command changes sits on the edge of the selected body that it measures
status: Accepted
topic: Clients, rendering
date: 2026-11-07
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - 3DEngine.App/EdgeValues.cs
  - 3DEngine.App/Ui/DesktopUi.Values.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/PanelAction.cs
  - 3DEngine.App/SketchActions.cs
  - 3DEngine.App/BodyActions.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/Ui/DesktopUi.Pad.cs
enforced-by: 3DEngine.ImageTests/EdgeValueTests.cs
---

# ADR-0082 — A value that a command changes sits on the edge of the selected body that it measures

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner ordered the layout of the redesign slice by slice on 2026-11-06 (evening); after the fixed controls and the
tool ring come "the values on the edges for 'change a dimension'". The "mix" direction of the designer brief puts the
values of the selected part on its edges (the idea "Spatial"), and the job "change a dimension" ends in one command that
rebuilds the extrude. A value is in one of four states: selected, pending (a dashed amber edge, not yet in the log),
applied and refused. Each target is at least 44 pixels (ADR-0027 item 6).

Two commands change a dimension of a body today, with no change of the contract: `SetExtrudeDistance` for the distance of
an extrude, and `SetSketchConstraintValue` for the value of a constraint of its sketch. `GetFaces` gives each edge of the
body by the pair of its face names, with its segments (ADR-0077). The side face of an extrude has the item of the sketch
that makes it (ADR-0051), so the edge between the face `end` and the side face of a line is the image of that line. The
overlay of ADR-0056 gives the size of the box of the selected body, with no command.

### Q1. Which values

- **A.** The values that a command changes: the distance of a selected extrude, and each `distance` constraint of one
  line (or of the two ends of one line) and each `radius` constraint of its sketch, on the edge that it measures.
- **B.** Also the sizes of a box and of a cylinder, which no command changes today, as values that do not take a tap.

A keeps one look for one meaning: a value on an edge changes when a person taps it (the brief, principle 5: one action,
one command). The overlay of ADR-0056 keeps the size of each body. **Recommended: A.**

### Q2. What a tap does

- **A.** It opens the pad of that value, as the segment Distance of the ring does; the apply gives one command, with no
  active sketch.
- **B.** It opens the sketch of the extrude.

A is the job of the brief in one tap and one apply; the sketch under its body is a later slice. **Recommended: A.**

### Q3. The pending state

- **A.** While the pad of a value is open, its chip has the warm colour of attention and a dashed border.
- **B.** No state on the chip; the pad shows the value.

A is the state "pending" of the brief, with a shape that does not need colour. **Recommended: A.**

## Decision

The standing order of 2026-11-05 gives A for each question.

1. **The values (Q1 A).** While an extrude is selected, no pad of another tool is open and no sketch is active,
   `EdgeValues.Of` gives its values from the edges of `GetFaces` and the sketch of the extrude:
   - the distance, on an edge between two side faces of the body: of those edges, the one whose middle on the screen is
     farthest from the middle of the edges of the body on the screen;
   - each `distance` constraint of one line, or of the start and the end of one line, on the edge between the face `end`
     and the side face of that line;
   - each `radius` constraint of a circle or an arc, with "R " before it, on the edge between the face `end` and the side
     face of that item.
   A value whose edge the body does not have, or whose edge is behind the eye, has no chip. A box and a cylinder have no
   chip.
2. **The chip.** The text is the value and the length unit, as the engine holds it (`Dimensions.Format`). The chip is at
   the middle of the middle segment of its edge, 24 pixels farther from the middle of the body on the screen, inside the
   display; the placement of ADR-0056 (`DimensionOverlay.Place`) moves the chips apart. Each chip is at least 44 pixels
   high and wide, and its rectangle is a part of the panels for a tap.
3. **The tap (Q2 A).** A press and a release on one chip, with no window of the panels under the pointer, open the pad
   of its value. The apply of the distance gives `SetExtrudeDistance`, as the ring does; the apply of a constraint gives
   `SetSketchConstraintValue` of the sketch of the extrude, with no active sketch (`SketchActions.SetValueOf`).
4. **The state (Q3 A).** While the pad of a chip is open, the chip has the colour of the dimensions and a dashed border.
   The pad of the distance gives that state from the chip, the ring or the panel, because each of them changes the same
   value.
   An applied value is the next value of the chip. A refusal is the message of the pad, as for each pad.
5. **No contract change.** The values are presentation (ADR-0007): the client reads `GetFaces` and `GetSketch`, and each
   apply is a command that exists.

## Consequences

**Good:** the job "change a dimension" of the brief takes one tap on the body and one apply, on each device, with no
panel.

**Bad:** a chip whose edge is behind the body still shows, because the overlay has no test of what hides an edge. A box
and a cylinder show no chip until a command changes their sizes.

**Next:** the rows without a panel, and the sketch under its body, where a sketch value of each kind sits on its item.

## History

- 2026-11-07 · TASK-0254 · The record, Accepted under the standing order.
- 2026-11-07 · TASK-0254 · Shipped at v0.241.
- 2026-11-07 · TASK-0263 · The affects field names `3DEngine.App/Ui/DesktopUi.Pad.cs`, which TASK-0254 changed for the
  pending state of item 4 (finding T167 of the codebase review of 2026-11-07).
