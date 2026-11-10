---
id: 0084
title: The data of the selected body is in rows with no panel at the upper right
status: Amended
topic: Clients, rendering
date: 2026-11-07
supersedes: []
superseded-by: []
amends: ['0034', '0081', '0082']
amended-by: ['0098', '0099', '0117']
affects:
  - 3DEngine.App/Ui/DesktopUi.Rows.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Values.cs
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
  - 3DEngine.App/EdgeValues.cs
enforced-by: 3DEngine.ImageTests/BodyRowsTests.cs
---

# ADR-0084 — The data of the selected body is in rows with no panel at the upper right

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner ordered the layout of the redesign slice by slice on 2026-11-06 (evening): the fixed controls (ADR-0080), the
tool ring (ADR-0081), the values on the edges (ADR-0082), then "the rows without a panel" and "the sketch under its body",
with no change of the contract. On 2026-11-07 the owner repeated the order: the next redesign slices after the STL
demonstration.

The "mix" direction of the designer brief puts the data of the selected part in rows with no ground and no border (the
idea "Spatial"): the name of the body with its kind and its state ("Extrude · correct"), then a row for each value, with
its label at the left and its exact value with the unit at the right, in a font with equal-width digits. At 1280 by 720
the brief shows four rows, at 1920 by 1080 seven. On a portrait tablet the rows go under the part. The brief has no panel;
the panel of today holds the file, the list "New", the sketches, the list of the bodies and the tools of the selection,
and later slices take each of them away.

The values of ADR-0082 are the values that a command changes: the distance of an extrude, and each length and radius of
its sketch. The principle "Show the state of the engine" of the brief asks for the state of each feature, and red always
comes with the word "failed" and the code.

### Q1. Where the rows go while the panel stays

- **A.** At the upper right of a landscape display, 16 pixels inside the safe area, in a column 360 pixels wide.
- **B.** At the upper left, in the place of the brief, with the panel folded to its title bar while a body is selected.
- **C.** In the panel, as rows with no border.

A shows the rows of the brief beside the panel of today, with no change of the panel; the tool ring is to the right of
the body, and the rows are drawn over it where the two meet. B hides the controls of the panel that have no other place
yet (Rename, the cut, Show all). C keeps the panel as it is and shows nothing new. **Recommended: A.**

### Q2. A portrait display

- **A.** No rows on a portrait display in this slice; the panel keeps the data there.
- **B.** The rows under the part, as the brief shows for a portrait tablet.

On the phone the thumb ring is at the lower right and Undo at the lower left; the place under the part changes with the
part and needs its own rules. **Recommended: A.**

### Q3. What the rows hold

- **A.** The name, the kind and the state ("correct", or "failed" and the code, and "hidden"), then one row for each value
  of ADR-0082, with "Distance", "Length" or "Radius". A tap on a value row opens its pad, as its chip does.
- **B.** Also the size of the box of the body (ADR-0056) and the sketch that makes it ("Made from").

A uses the data that the client has today. "Made from" needs the name of the sketch of the extrude, and the size is a
value that no command changes, so it does not take a tap (ADR-0082 Q1). **Recommended: A.**

## Decision

The standing order of 2026-11-05 gives A for each question.

1. **The place (Q1 A, Q2 A).** On a landscape display, while a body is selected, the home screen does not show and no
   sketch is active, the rows are a column 360 pixels wide at the upper right, 16 pixels inside the safe area. A portrait
   display shows no rows.
2. **The rows (Q3 A).** Each row is 44 pixels high (ADR-0027 item 6), with no ground and no border, and a line under it.
   The first row gives the name of the body on its first line, and its kind and its state on its second line:
   "correct", or "failed" and the code of its fault, and "hidden" for a hidden body; only "failed" and the code are red.
   Each line fits the column: a long name ends with "...", and a state that is too wide loses the kind, then "hidden",
   so that "failed" and the code stay whole. Then one row for each value that `EdgeValues.Of` gives, in its order: its
   label ("Distance", "Length" or "Radius") at the left, and the value with the unit at the right, as its chip gives it.
   The rows end 16 pixels above the anchors; a value with no room keeps its chip only.
3. **The tap.** A press and a release on one value row, with no window of the panels under the pointer and no pad open,
   open the pad of its value, as its chip does (ADR-0082 item 3); while that pad is open, the row and the chip are
   pending (ADR-0082 item 4). The rows are a part of the panels for a tap. The rows are drawn over the ring and the
   chips, so a release on a row gives the row only (this amends ADR-0081 item 2 and ADR-0082 item 3).
4. **The chips.** A chip that the placement of ADR-0082 item 2 puts on the rows goes under them, at the same x (this
   amends ADR-0082 item 2), and a chip that then covers an earlier chip goes 4 pixels under it.
6. **The window of the live sketches.** While the rows show, they take the place of the window that lists the live
   sketches with "Edit", which is at the upper right too (this amends ADR-0034 item 8). With no selected body, or on a
   portrait display, the window shows as before; the window of an active sketch shows as before.
5. **No contract change.** The rows are presentation (ADR-0007): they read the live bodies and the values of ADR-0082,
   and each apply is a command that exists.

## Consequences

**Good:** the name, the state and the values of the selected body show at one place with no panel, as the brief draws
them, and a tap on a value is the same job as a tap on its chip.

**Bad:** the panel and the rows show the name of the body two times until a later slice takes the tools of the selection
out of the panel. A value whose edge is behind the eye has no chip of ADR-0082, and so no row either. The bitmap font of
ADR-0027 item 6 is wide, so a name of more than about nine letters ends with "..." next to the state of an extrude.

**Next:** the sketch under its body; the rows on a portrait display; the tools of the selection out of the panel.

## History

- 2026-11-07 · TASK-0267 · The record, Accepted under the standing order.
- 2026-11-07 · TASK-0267 · Shipped at v0.252.
- 2026-11-07 · TASK-0271 · Items 2, 4 and 6: the state on a second line, with only "failed" and the code in red (finding
  V129 of the codebase review of 2026-11-07 (b)); the rows end above the anchors, and two chips under the rows stay apart
  (V128 and the review of Codex of v0.252); the rows take the place of the window of the live sketches, which covered
  them (the review of Codex of v0.252).
- 2026-11-08 · TASK-0312 · ADR-0098 item 4 amends item 1: the rows start 8 pixels under the command line.
- 2026-11-08 · TASK-0313 · ADR-0099 item 1 amends item 1: on a landscape display the rows are at the upper left in the
  place of the panel. Item 6 holds.
- 2026-11-10 · TASK-0342 · ADR-0117 items 1 and 3 amend item 2: the name is larger with its state after it, and a length
  takes its label from its direction and the name of its constraint.
