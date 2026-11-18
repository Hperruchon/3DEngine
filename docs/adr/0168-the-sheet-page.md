---
id: 0168
title: A sheet page replaces the scene with the paper of a sheet, where a tap on a view gives its actions, a drag keeps the alignment with one MoveSheetViews, and a tap on the paper places a view
status: Accepted
topic: Drawing, window
date: 2026-11-18
supersedes: []
superseded-by: []
amends: ['0138', '0155', '0167']
amended-by: []
affects:
  - Engine.Core/Commands/MoveSheetViewsCommand.cs
  - Engine.Core/Commands/MoveSheetViewsCommandHandler.cs
  - Engine.Core/Hosting/HandlerCatalog.cs
  - 3DEngine.App/SheetPage.cs
  - 3DEngine.App/SheetActions.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/Ui/DesktopUi.SheetPage.cs
  - 3DEngine.App/Ui/DesktopUi.Sheet.cs
  - 3DEngine.App/Ui/DesktopUi.Home.cs
  - 3DEngine.App/Ui/DesktopUi.Fixed.cs
  - 3DEngine.App/Ui/DesktopUi.Views.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/PanelAction.cs
  - Engine.Tests/Drawing/SheetMoveTests.cs
  - 3DEngine.ImageTests/SheetPageTests.cs
enforced-by: 3DEngine.ImageTests/SheetPageTests.cs
---

# ADR-0168 — A sheet page replaces the scene with the paper of a sheet, where a tap on a view gives its actions, a drag keeps the alignment with one MoveSheetViews, and a tap on the paper places a view

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0162 items 1, 5 and 10 (the answers D1 A, D5 A and B, and D13 A of the owner of 2026-11-18) decide that a sheet shows on a
page that replaces the scene, on the desktop and on the phone, that a tap on the paper is a second path that places a view,
and that a drag keeps the alignment of a projected view with one new command MoveSheetViews and one step of undo. Row R30c
holds this slice. The codebase review of 2026-11-18 (TASK-0409) asks R30c to send its commands off the frame thread (V196)
and to let the page and its pads hold their keys (V195, V199). R30g gave each view the switches of its hidden lines and its
centre lines (ADR-0165), and asks for their control on the page if it fits.

## Decision

1. **The page (D1 A, amends ADR-0138 item 2).** While a live sheet exists, a button "Sheet" stands beside "Views", on the row
   of the anchors of a landscape display and at the upper right of a portrait display. A tap opens the page and a second tap
   closes it; "3D" on the page closes it too. The page lies in the free part of the scene of the last frame (ADR-0140 item 3)
   and replaces the scene: the ground of the scene covers the bodies, and the window of the live sketches, the window of the
   images, the selection, its ring, its rows and its values do not show. On a portrait display the page takes the place of
   the panel too. The paper fits the page with its aspect under room for three rows of the header, so it keeps its place
   while the header changes. The page draws the frame and the title block as the files do (ADR-0159 item 4), each visible
   line of 2 pixels, each hidden line of 1 pixel in dashes, each centre line of 1 pixel in long dashes, and the value of each
   dimension at its place (ADR-0158 item 4), with "Ø" for a diameter. The header holds "3D", "Keep aligned", the name of the
   sheet or, with several live sheets, a button for each sheet, and "…" while a read or a command runs. The window reads
   GetSheet of the sheet off the frame thread, one read at a time, for a new sheet or a new version of the Document, and
   keeps the last read of the same sheet until the new one comes.
2. **A tap on a view.** A tap on a view, or within 3 mm of the box of its lines, selects it with an outline in the accent; a
   second tap ends the selection. The header then gives "Go to" (the flat view of its frame, framed, and the page closes),
   "Scale" (the pad of the scale of ADR-0167 item 5 for that view, which sends ScaleSheetView with the view), "Remove"
   (RemoveSheetView; Undo gives the view back), and the switches "Hidden lines" and "Centre lines" (SetSheetViewLines,
   ADR-0165).
3. **The drag and MoveSheetViews (D13 A, amends ADR-0155 item 6).** The first view of the sheet is the base. With "Keep
   aligned" on and no Shift, a drag of the base moves it free and takes each view on a line of projection through it, a view
   on the vertical line of the base slides up and down only, and a view on its horizontal line slides left and right only.
   Each other drag, and each drag with Shift or "Keep aligned" off, moves the view free and alone. The new command
   MoveSheetViews takes the sheet, the view, its new place and a flag `aligned`: with the flag, each other view whose frame has
   the same right and whose centre has the same X, or the same up and the same Y, moves by the same distance. No command
   has a list parameter (the unit gate, the file and the HTTP surface read plain fields), so the command finds the aligned
   views from the record; D13 A gave each moved view and its place, and the result is the same. The checks come in the order
   of the record with the codes of a view: the sheet is live, the view is on it, and each new place is on the paper. One drag
   is one command and one step of undo. The places compare with no tolerance; the window keeps them equal.
4. **The commands off the frame thread (finding V196).** Each command of the page and of the pad of the scale goes to a queue
   that sends it on the thread pool, one at a time in their order, so a frame never waits for the serial lock of the session.
   The panel shows the text of each command when it ends.
5. **A tap on the free paper (D5 B).** A tap on the paper with no view under it marks the place and gives the six views and
   "Cancel" in the header. A view goes to that place at the scale of the first view of the sheet, or at the scale that fits for
   a sheet with no view (ADR-0167 item 4); a view whose frame the sheet holds is not added again.
6. **The keys (findings V195 and V199, amends ADR-0167 item 5).** The page and the pad of a sheet hold the keys of the frame in
   which they show, also the Escape that closes them, so no key reaches the intents of the scene. On the page Escape ends the
   mark of a tap, then the selection, then the page, and the keys of the views, F and L rest. The home screen closes the page
   and the pad of a sheet, and the keys of the pad rest while it shows.
7. **The free place (the answer of the owner of 2026-11-18, amends ADR-0167 item 3).** D5 A of the design file says that a
   view that does not fit goes to the largest free part of the paper. The owner chose the free place nearest to the place of
   the projection, as R30b does ("Nearest place"); the design file has a note.
8. **No contract change.** MoveSheetViews and its handler are in `Engine.Core`; the page is presentation (ADR-0007).

## Consequences

**Good:** the window shows the sheet that the files print, and a person moves, scales, removes and places views, and sets their
lines, with taps and drags on the desktop and on the phone, each as one command of the log. No command of the page stops a
frame.

**Bad:** the alignment rests on exact places; a view that a command of an agent puts off the line of the base slides free
until it is on the line again. A new scale keeps the places, so views can meet. The page shows the first live sheet first and
offers each other sheet by name only. The texts of the title block need a paper of about 300 pixels or more to read.

## Alternatives considered

- **A list parameter in MoveSheetViews (D13 A as written).** It needs a list type in the schema of a command, the unit gate,
  the file and the HTTP surface. The flag gives the same result for the drags of the page.
- **A parent field in the record of a view (D13 B).** The owner chose the alignment in the client.
- **The commands on the frame thread, as R30b sends AddSheetView.** Finding V196: a read of the lines holds the session.

## History

- 2026-11-18 · TASK-0408 · The page, MoveSheetViews, the drag, the tap on the paper and the keys (phase R30c).
