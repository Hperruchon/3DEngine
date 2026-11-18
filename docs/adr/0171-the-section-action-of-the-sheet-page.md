---
id: 0171
title: On the sheet page a person cuts a view with "Section", a look and two taps, and the page draws the section as the files do
status: Accepted
topic: Drawing, window
date: 2026-11-18
supersedes: []
superseded-by: []
amends: ['0155', '0168', '0169']
amended-by: []
affects:
  - Engine.Core/Drawing/SheetLayout.cs
  - Engine.Core/Queries/GetSheetQuery.cs
  - Engine.Core/Queries/GetSheetQueryHandler.cs
  - 3DEngine.App/SheetPage.cs
  - 3DEngine.App/SheetActions.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/Ui/DesktopUi.SheetPage.cs
  - 3DEngine.App/Ui/PanelAction.cs
  - 3DEngine.ImageTests/SheetSectionPageTests.cs
enforced-by: 3DEngine.ImageTests/SheetSectionPageTests.cs
---

# ADR-0171 — On the sheet page a person cuts a view with "Section", a look and two taps, and the page draws the section as the files do

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0162 item 13 (D16 A) says: on the sheet page a person taps a view, then "Section", and moves the cut line, which snaps to
the centre of a circle. ADR-0169 gives the section of a sheet in the engine, GetSheet and the three files, and its item 8 puts
the action of the page in a later record. ADR-0168 gives the sheet page; ADR-0170 adds its button "Export". On 2026-11-18 the
owner ordered "then start the Section action on the sheet page".

The page knows a view only from GetSheet, on the paper. AddSheetSection takes a point of the design. GetSheet does not give
the centre of the box of a view in the coordinates of the view, so the page cannot turn a place on the paper back into a
point of the design.

## Decision

1. **The centre of a view (amends ADR-0155 item 5).** GetSheet gives each view `CentreU` and `CentreV`, the centre (cu, cv)
   of the box of its lines in the coordinates of the view, 0 and 0 for a view with no line. A place (x, y) on the paper is
   u = cu + (x − X) / f and v = cv + (y − Y) / f on the view, with f the scale divided by the length L of the right, and the
   point (u Right + v Up) / L² of the design is on that place. The point has no part along the direction of the view, which
   a cut does not need.
2. **"Section" (D16 A; amends ADR-0168 item 2).** A selected view gets the action "Section". It starts the section of that
   view: the row of the page shows the looks "Left", "Right", "Up" and "Down", the direction of the arrows (ADR-0169 item 2),
   with "Up" first, and "Cancel section". Escape, "Cancel section" and the close of the page end it; Escape ends a section before it ends the selection.
3. **The cut (D16 A).** While the section has no cut, a line of the colour of the accent goes across the box of the view
   through the pointer: horizontal for "Up" and "Down", vertical for "Left" and "Right". Its place snaps to the centre of
   each whole circle of the view that is within 2 mm of paper. A tap on the view, or within 3 mm of its box, sets the cut at
   the snapped place; the line stays and the row asks for the place of the section. A tap on another place does nothing.
4. **The place (D16 A).** With a cut, a tap on the paper outside each view gives one AddSheetSection with the view, the look,
   the point of the cut (item 1) and the place of the tap, and ends the section. The command takes the first free letter.
   The commands run off the frame thread, in their order (ADR-0168 item 6). A drag does nothing while a section is open.
5. **The drawing of the page (amends ADR-0169 item 6 for the page).** The page draws the lines of a section as the three
   files do: a hatch line thin and continuous, a cut line as a centre line, a thick end thick, an arrow thin with a filled
   head, and each text of a view at its place, centred, with its size on the paper.

## Consequences

**Good:** a person makes a section with four taps, and the page shows it as the files print it. The point of the design
comes from the read, so the page holds no copy of the kernel.

**Bad:** the snap uses whole circles only; an arc of a circle or an ellipse gives no snap. The place of a section is a tap;
no place of the projection is proposed.

**Neutral:** the command and the read stay those of ADR-0169; the page sends no new command.

## Rejected alternatives

- **A drag of the cut line, then a release.** A tap is the same on a phone and on a desktop, and the line follows the pointer
  on the desktop without a press.
- **The place of the projection for each section.** The paper of a section is often full; the person knows where it fits.

## History

- 2026-11-18 · TASK-0413 · The record, for the action of the page of row R30h (ADR-0169 item 8).
