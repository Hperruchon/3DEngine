---
id: 0099
title: The rows of a selected body take the place of the panel at the upper left
status: Amended
topic: Clients, rendering
date: 2026-11-08
supersedes: []
superseded-by: []
amends: ['0084', '0093', '0098']
amended-by: ['0100', '0117', '0131']
affects:
  - 3DEngine.App/Ui/DesktopUi.Rows.cs
  - 3DEngine.App/Ui/DesktopUi.Command.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Fixed.cs
  - 3DEngine.ImageTests/BodyRowsTests.cs
  - 3DEngine.ImageTests/FixedControlsTests.cs
  - 3DEngine.ImageTests/NumericPadUiTests.cs
  - 3DEngine.ImageTests/DesktopUiTests.cs
  - 3DEngine.ImageTests/FaceSelectionTests.cs
  - 3DEngine.ImageTests/CutUiTests.cs
  - 3DEngine.ImageTests/DimensionOverlayTests.cs
  - 3DEngine.ImageTests/DesktopFormsTests.cs
  - 3DEngine.ImageTests/DimensionImageTests.cs
  - 3DEngine.ImageTests/EdgeValueTests.cs
enforced-by: 3DEngine.ImageTests/BodyRowsTests.cs
---

# ADR-0099 — The rows of a selected body take the place of the panel at the upper left

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-08, after v0.297, the owner chose the next slices, in this order: the rows of a selected body at the left, the
history arc, the agent arc and the values on the edges of a sketch. Each slice merges after green CI, with a picture.

In the render "Mix 1" of the designer brief, the upper left holds the rows of the selected part and no panel: the name with
the kind and the state, a row "Made from" with the sketch of the part, then a row for each value. ADR-0093 Q1 A kept the
rows at the upper right until the panel holds no actions. ADR-0098 item 4 put them under the command line.

While a body is selected, the panel holds: the file (its name, Save as, Open, Export STL and the message of the document),
the list "New", "Sketch on" with XY, XZ, YZ and Face, the toggles of the dimensions, the list of the bodies with its filter,
and the controls of the selected body: the line of a fault, the size, the line of the selected face with its sketch, the
line of the selected edge, the name field with Rename, Hide, Move, Set distance, the list of a cut and Show all. The ring
holds Move, Distance, Hide or Show and the sketch on the selected face (ADR-0081). The command line holds the list "New",
the sketch on a plane, Move, Set distance, Hide or Show, Show all, Undo, Redo and Frame (ADR-0098).

### Q1. Where the controls of the panel go while a body is selected

- **A.** The rows hold each control that acts on the selected body or gives its state. The command line holds the other
  controls that a person uses with a selection. The file, the list of the bodies and its filter show again when the
  selection is empty, after a tap on the scene where no body is.
- **B.** A smaller panel under the rows.

B fills the left column as before this slice, and the render has no panel. **Recommended: A.**

### Q2. The row "Made from"

- **A.** Not in this slice. It needs the sketch of a feature from a query, and a tap that opens that sketch.
- **B.** In this slice.

**Recommended: A.**

## Decision

The owner chose the slice and its merge after green CI; the recommendations answer Q1 and Q2.

1. **The place (amends ADR-0084 item 1, ADR-0093 item 4 and ADR-0098 item 4).** On a landscape display, while a body is
   selected and no sketch is active, the rows are at the upper left, 16 pixels inside the safe area and 8 pixels under the
   top bar, 360 pixels wide. The panel does not show. ADR-0084 item 6 holds: the window of the live sketches does not show
   either.
2. **The name (Q1 A).** A tap on the first row opens a name field in its place, with the name of the body selected. Enter
   renames the body with one command. Escape, or a tap that takes the keys from the field, closes it with no command. A
   pointer over the first row of a failed body gives the reason of the fault, as the line of the fault in the panel does.
3. **The lines (Q1 A).** Under the values, each where it has room: the line of the selected face, the line of the selected
   edge, the size while the dimensions show (ADR-0056 item 6) and the message of the document. They take no tap.
4. **The cut (Q1 A).** While another body is visible, a row "Cut with" under the values holds the list of a cut of
   ADR-0073 item 1. The open list belongs to the panels, as in the panel (V116).
5. **The command line (Q1 A, amends ADR-0098 item 3).** The command line also holds Rename, which opens the name field, a
   cut with each other visible body, "Sketch Face" (the armed tap of ADR-0053 item 6), Dimensions and, while the dimensions
   show, Hidden too.
6. **The top bar (amends ADR-0093 item 1).** A long name of the file ends 12 pixels left of the command line, which is now
   the only control at the upper right.
7. **A portrait display** keeps the panel with each control, as before. ADR-0131 item 1 amends this item: a portrait
   display shows the rows under the part, and the panel has no controls of a selected body.
8. **No contract change.** Each control gives the same action as its button in the panel.

## Consequences

**Good:** the left column is as in the render, and each control of the panel has a place while a body is selected.

**Bad:** while a body is selected, a person who wants to save, to open or to choose a body in the list taps the scene where
no body is first. A finger has no hover, so on a touch display the reason of a fault shows only in the panel, on a portrait
display. The row "Made from" waits.

## History

- 2026-11-08 · TASK-0313 · The record, with the slice.
- 2026-11-08 · TASK-0313 · Shipped at v0.298.
- 2026-11-09 · TASK-0314 · ADR-0100 item 4 amends item 1 and Q1 A: with no selected body on a landscape display, the
  first part of the panel is the rows of the document, and they hold the list of the bodies and its filter.
- 2026-11-10 · TASK-0342 · ADR-0117 item 2 answers Q2 and amends item 3: the row "Made from" with the sketch of the body,
  whose tap opens that sketch.
- 2026-11-14 · TASK-0360 · ADR-0131 amends items 1 and 7: the rows show on a portrait display too, under the part.
