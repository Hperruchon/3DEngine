---
id: 0110
title: A proposal shows each value that it changes on the chip of that value
status: Amended
topic: Clients, surface
date: 2026-11-10
supersedes: []
superseded-by: []
amends: ['0092']
amended-by: ['0114']
affects:
  - 3DEngine.App/Ui/DesktopUi.Proposal.cs
  - 3DEngine.App/Ui/DesktopUi.Values.cs
  - 3DEngine.App/ProposedValues.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine/HostedSession.cs
  - 3DEngine.ImageTests/PendingChipTests.cs
enforced-by: 3DEngine.ImageTests/PendingChipTests.cs
---

# ADR-0110 — A proposal shows each value that it changes on the chip of that value

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0092 item 5 shows each command of a proposal as its name and the values of its parameters, and keeps "the pending chip of
a value of ADR-0082" for a later slice. R16r shipped it at v0.309. Its weakest point: a command line such as
"SetSketchConstraintValue 09e310bf 7611b05a 75" does not say which value changes, because an identifier shows its first 8
characters. The render "Mix 3" shows "C3 60 → 75" on the edge of the value. On 2026-11-09 the owner chose the pending chip
as the next slice, after the codebase review.

What exists:

- ADR-0082: the chips of the values of a selected extrude on its edges: its distance, and each `distance` and `radius`
  constraint of its sketch. A chip has a key: `EdgeValues.DistanceKey(body)` or `EdgeValues.ConstraintKey(constraint)`.
- ADR-0105: while a sketch is active, the chips of its lengths, with the label of the constraint: "C1  60 mm".
- ADR-0082 item 4: a chip whose pad is open has the colour of the dimensions and a dashed border.
- The queue of ADR-0092 keeps the parameters of each command of a proposal.

### Q1. Which commands

- **A.** `SetSketchConstraintValue` (a value of a constraint) and `SetExtrudeDistance` (the distance of an extrude): the
  values that have a chip.
- **B.** A, and each other command, with a text of its own.

**Recommended: A.**

### Q2. The look of a chip whose value a proposal changes

- **A.** The chip of the value shows the value now and the proposed value, "C3  60 → 75 mm", in the colour of the dimensions
  with a dashed border, as a pending chip of ADR-0082 item 4.
- **B.** A second chip beside the chip of the value, with the proposed value only.

A keeps one chip for one value, and the arrow says what changes. **Recommended: A.**

### Q3. The command lines of the arc

- **A.** A command of Q1 shows the name of the value and its change, "Plate C3  60 → 75 mm" or "Bracket distance  20 → 30
  mm", with no identifier; each other command keeps its line of ADR-0092 item 5.
- **B.** No change of the lines.

**Recommended: A.**

### Q4. A value with no chip on the screen

- **A.** No chip shows; the line of the arc names the value. The window does not select a body for a proposal.
- **B.** The window selects the body of the first value, so that its chips show.

B changes the selection of the person for an agent. **Recommended: A.**

## Decision

The owner chose the slice; the recommendations answer Q1 to Q4.

1. **The values (Q1 A).** `ProposedValues.Of` reads the commands of a proposal and gives one proposed value for each command
   `SetSketchConstraintValue` (its sketch, its constraint and `value`) and each command `SetExtrudeDistance` (its extrude and
   `distance`). A later command for the same value replaces the earlier one. A command with a parameter that does not bind
   gives no value.
2. **The chip (Q2 A).** While the proposal is the one that the window shows, each chip whose key is the key of a proposed
   value shows "label  now → proposed unit", for example "C3  60 → 75 mm", or "60 → 75 mm" for a chip with no label. It has
   the colour of the dimensions and the dashed border of ADR-0082 item 4. A tap on it does what a tap on the chip does.
   The fonts of the window have no glyph of the arrow, so the window draws it as a line with a head, in the width of "->".
3. **The lines (Q3 A).** A line of the arc for a command of item 1 is the name of the value and its change: the name of the
   sketch and the label of the constraint, or the name of the body and the word "distance", then "now → proposed unit". The
   host names each value from the sketches, the bodies and the chips of the frame (`ProposedValues.Named`), so the window
   reads no state of the session. The value now of a distance is the value of the chip of the distance of the selected
   extrude; with no value now, the line is "→ proposed unit".
4. **No selection (Q4 A).** A proposal does not change the selection, and a value with no chip on the screen has its line only.
5. **No contract change.** The values are presentation (ADR-0007); the queue and the routes of ADR-0092 do not change.

## Consequences

**Good:** the person sees which value an agent changes, where the value is, and the change, before an accept.

**Bad:** only two commands have a chip; a new value command needs its line here. The chip of a value of a body shows only
while that body is selected.

## History

- 2026-11-10 · TASK-0334 · The record, with the slice (the order of the owner of 2026-11-09).
- 2026-11-10 · TASK-0334 · Shipped at v0.314.
- 2026-11-10 · TASK-0339 · ADR-0114 item 2 amends item 3: the line shows an angle in degrees, and it names only a value that
  it can show in its unit (finding V164).
