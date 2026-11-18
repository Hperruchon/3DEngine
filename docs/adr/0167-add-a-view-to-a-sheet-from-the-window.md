---
id: 0167
title: A person adds the flat view or the standard views to a sheet from the window, at the place of the projection and the scale that fits, and ScaleSheetView changes the scale
status: Accepted
topic: Drawing, window
date: 2026-11-18
supersedes: []
superseded-by: []
amends: ['0138', '0155']
amended-by: []
affects:
  - Engine.Core/Commands/ScaleSheetViewCommand.cs
  - Engine.Core/Commands/ScaleSheetViewCommandHandler.cs
  - Engine.Core/Hosting/HandlerCatalog.cs
  - 3DEngine.App/SheetPlacement.cs
  - 3DEngine.App/SheetActions.cs
  - 3DEngine.App/SessionSheets.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/Ui/DesktopUi.Sheet.cs
  - 3DEngine.App/Ui/DesktopUi.Views.cs
  - 3DEngine.App/Ui/DesktopUi.Command.cs
  - 3DEngine.App/Ui/DesktopUi.Panel.cs
  - 3DEngine.App/Ui/DesktopUi.Pad.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/PanelAction.cs
  - Engine.Tests/Drawing/SheetScaleTests.cs
  - 3DEngine.ImageTests/SheetAddTests.cs
enforced-by: 3DEngine.ImageTests/SheetAddTests.cs
---

# ADR-0167 — A person adds the flat view or the standard views to a sheet from the window, at the place of the projection and the scale that fits, and ScaleSheetView changes the scale

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0162 items 4 to 6 (the answers D4 A, D5 A and B, and D6 A of the owner of 2026-11-18) decide that "Add to sheet" in the
net of the views adds the current flat view to a sheet, that "Standard views" adds the front, the top and the side view, that
the first view opens a pad for the paper and the scale, that the app computes the place of a new view from the projection,
and that a new command ScaleSheetView changes the scale of one view or of each view in one step of undo. Row R30b holds this
slice. The tap on the paper that places a view (D5 B) needs the sheet page, so it comes in R30c. Until this slice only the
command line with nine numbers, the HTTP surface or an agent made a view of a sheet.

## Decision

1. **ScaleSheetView (amends ADR-0155 item 6).** The command takes the sheet, an optional view, and the two parts of the
   scale with the unit Count, as AddSheetView. With a view, that view takes the scale; with no view, each view of the sheet
   takes it. The places of the views and the dimensions do not change, and the command is one step of undo. The checks
   come in this order: the sheet is live (`E-SHEET-NOT-FOUND`); the view is on the sheet (`E-SHEET-VIEW-NOT-FOUND`); each
   part of the scale is greater than zero (`E-SHEET-VIEW-INVALID`). With no view named, a sheet with no view keeps its
   record; the codes keep their meanings. The event is `sheet.changed`, so no new event kind and no contract change come with it.
2. **"Add to sheet" and "Standard views" (amends ADR-0138 item 1).** Under the cells of the net of the views, a fourth row
   holds "Add to sheet" and "Standard views", each two cells wide. "Add to sheet" adds the flat view of the camera, and it is
   dim in the 3D view. "Standard views" adds the front view, the top view and the view from the right. A tap closes the net.
   The command line has "Add view to sheet" in a flat view, "Standard views", and "Scale the sheet views" when the first live
   sheet has a view. The views go to the first live sheet, at the scale of its first view (D6 A); a view whose frame the
   sheet holds is not added again. The window follows the live sheets from the events `sheet.created`, `sheet.changed` and
   `sheet.removed`, which carry the whole record, and sends AddSheetView, one command for each view, as an agent does. The
   panel shows the result, for example "Added the top and the right view to Sheet 1 at 2:1. Sheet 1 has the front view
   already."; the message of the document now wraps inside the panel.
3. **The place (D5 A).** The size of a view on the paper is the box of the live bodies along the right and the up of its
   frame, times its scale. The area of the views is the frame of the files, 10 mm inside the paper, less the band of the
   title block at its bottom (ADR-0159 item 4). A new view goes next to the first view of the sheet with which it shares an
   axis, as the projection of the sheet puts it, with a gap of 10 mm: in first angle a view from the right of the reference
   goes at its left and a view from above goes under it; third angle is the other way. When that place leaves the area or
   meets a view, the view goes to the free place on a grid of 5 mm that is nearest to it. With no free place, the view goes
   to the centre of the area, and the message says so. On a new sheet the views of the pad and the standard views are one
   group, and the box of the group goes to the centre of the area, so a first front view leaves room for its top and side
   view. The code uses + - * / only. The numbers go into the command, so a replay reads them from the log.
4. **The scale that fits (D6 A).** The first scale is the largest of the series of ISO 5455 (50:1 to 1:1000) at which the
   front, the top and the side view fit the area of the paper, as one group. Each next view takes the scale of the first view
   of the sheet.
5. **The pad of a new sheet (D4 A, D6 A).** With no live sheet, "Add to sheet" and "Standard views" open a pad: the paper A4
   to A0, landscape or portrait, the scales 10:1, 5:1, 2:1, 1:1, 1:2, 1:5 and 1:10 with the scale in effect, and a free
   ratio "n:d" of two whole numbers. It starts on A3 landscape (section 4.1 of the design file) at the scale that fits, and a
   new paper takes its scale that fits until the person chooses a scale. "Add" sends CreateSheet with the name "Sheet n" and
   the paper, then AddSheetView for each view, so one sheet with its views is several steps of undo. "Scale the sheet views"
   opens the same pad with the scales only, and "Apply" sends one ScaleSheetView with no view. Enter gives Add or Apply and
   Escape gives Cancel while the field of the ratio does not take the keys, and the keys of the views rest while the pad is
   open. One pad shows at a time.
6. **No contract change.** The command and its handler are in `Engine.Core`; the controls, the place and the scale are in the
   window (ADR-0007).

## Consequences

**Good:** three taps make the front, the top and the side view of the plate of 2026-11-17 at 2:1 on A3 at the places of
first angle (criterion C2 of the design file), and the PDF of that sheet is the sheet of the design file. A new scale is one
step of undo, and the dimensions stay.

**Bad:** a new sheet with its views is one step of undo for each command. The size of a view comes from the box of the
mesh, so a curved body can give a view a little larger or smaller than its lines, inside the gap of 10 mm. A new scale keeps
the places, so larger views can meet; the sheet page of R30c moves them. The views go to the first live sheet only.

## Alternatives considered

- **The place in the engine, from the lines of GetSheet.** It is exact, but each add reads the lines of each view (R-0069);
  the box of the mesh is enough for a place that the person moves after.
- **One command that makes a sheet with its views.** One step of undo, but a new command and a new record shape; the log of
  several commands is what an agent sends too.
- **ScaleSheetView with a flag "all" in place of an optional view.** Two fields for one meaning; an absent view is clear.

## History

- 2026-11-18 · TASK-0407 · The command, the controls, the place, the scale and the pad (phase R30b).
- 2026-11-18 · TASK-0407 · Shipped at v0.391.
