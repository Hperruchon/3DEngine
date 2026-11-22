---
id: 0174
title: The tools of the part on the ring, a file menu, and a structure view in the place of the panel
status: Amended
topic: Clients, layout
date: 2026-11-19
supersedes: []
superseded-by: []
amends: ['0056', '0076', '0081', '0093', '0100', '0102', '0172']
amended-by: ['0175', '0184']
affects:
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/Ui/DesktopUi.Fixed.cs
  - 3DEngine.App/Ui/DesktopUi.Orbit.cs
  - 3DEngine.App/Ui/DesktopUi.Panel.cs
  - 3DEngine.App/Ui/DesktopUi.Part.cs
  - 3DEngine.App/Ui/DesktopUi.Ring.cs
  - 3DEngine.App/Ui/DesktopUi.Views.cs
  - 3DEngine.ImageTests/PartRingTests.cs
enforced-by: 3DEngine.ImageTests/PartRingTests.cs
---

# ADR-0174 — The tools of the part on the ring, a file menu, and a structure view in the place of the panel

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-18 the owner said that the panel at the left takes too much space. A study measured the panel with two bodies
and no selection: it took 29 % of a display of 1280 by 720, and 64 % of a phone in portrait. Of its height of about 590
pixels, only "New" and "Sketch on" (about 110 pixels) were frequent tools. The rest was status that the top bar repeats,
four file actions that a person uses one time in a session, and controls for a rare case.

The study gave three options: A, a rail of tools; B, no panel, and the ring gives the tools of the part; C, the panel
folded to one line, with a ground when it is open. The owner chose "B plus C": the ring gives the tools of the part, and a
structure view, folded by default, takes the place of the panel. The owner then said that the structure view was too
spaced, and chose density B: rows of 30 pixels for a mouse or a pen, 44 pixels on a phone, and a setting "Compact" or
"For touch". In each option of the study, the file actions go to a menu on the file name, and the toggles of the
dimensions go to the menu "Views".

Phase R31 does the change in two slices. R31a (TASK-0419) gives the ring of the part, the file menu, the toggles in the
net of the views and the structure view with rows of 44 pixels. R31b gives the rows of 30 pixels and the setting; it
amends ADR-0027 item 6 for lists, so it has its own record.

## Decision

1. **The ring of the part (amends ADR-0081 item 1).** With no selected body, no active sketch, no open pad, no open
   images, no sheet page, no home screen, no lines of a flat view and no open net of the views, the ring holds the tools of the part: "New", "Sketch", "Bodies" when the
   part has a live body, and "Images" when the host gives images. On a landscape display its centre is the centre of the box
   of the visible live bodies on the screen, as for the box of a selected body; with no body, it is the middle of the scene
   less the fixed controls of the last frame. The structure view does not move it. Its caption is "TOOLS FOR THE PART". On the light paper of the lines of a flat view
   (ADR-0166 item 3) its labels do not read, so it does not show there. The open net of the views covered it at 640 by 360. On a portrait
   display it is the thumb ring of ADR-0081. The ring follows each rule of a body ring: the targets of ADR-0116 item 1,
   the fade of a gesture and the dim of a proposal.
2. **The second level.** "New" opens a second level with "Box", "Cylinder", "Cone", "Sphere", "Torus" and "Back".
   "Sketch" opens a second level with "XY", "XZ", "YZ", "Face" and "Back". A tool of the second level does what its
   control in the panel did before (it opens the pad of the kind, starts a sketch on the plane, or arms a tap on a face),
   and the ring goes back to the first level. "Back" goes back with no action. A change of the selection also goes back.
3. **The structure view (amends ADR-0093 item 2, ADR-0100 items 4 and 5, and ADR-0056 item 2).** The structure view takes
   the place of the panel on each display, landscape and portrait. Folded, it is one row: the first row of the document of
   ADR-0100 item 4 (the kind; then the unit, the count of the bodies and their state), with a mark at its right. A tap on
   that row, or on "Bodies" of the ring, opens it; a second tap folds it. It has the ground of a window. Open, it holds
   the row of each body of ADR-0100 item 4, then "Show all" while a body is hidden. The filter shows only with eight live
   bodies or more. A message of the document shows under the first row. The view is at most as wide as the rows of a body
   (360 pixels), and it keeps the rules of the place and the height of the panel (above the anchors, left of the command
   line, above a portrait ring). Its rows are 44 pixels high in R31a. While the net of the views is open, the structure view
   does not show: on a portrait display the net opens in its place, and before R31a the net covered the panel there.
4. **The file menu (amends ADR-0102 item 1 and ADR-0172 item 4).** With no selected body, the file name of the path line
   is the last segment, and a tap on it opens the file menu under it: "Open project...", "Save as...", "Export STL..."
   and "Import STL...". The menu has the ground of a window. A tap on an entry closes the menu and opens the dialog of the
   system, as the buttons of the panel did. A tap outside the menu closes it, and that tap gives no other action. With a selected body, a tap on the file name selects the document, as before (ADR-0100
   item 2). The images open from "Images" of the ring and from the command "Images"; the button "Images..." leaves the
   panel.
5. **The toggles in the net of the views (amends ADR-0056 item 2).** The net of the views holds "Dimensions" in the cell
   at the left of "Bottom" and "Hidden too" in the cell at its right, on the row of "Lines". Each has the colour of a tool
   in use while it is on, and "Hidden too" is disabled while "Dimensions" is off. A tap does not close the net. The text
   of a cell that is too wide gets smaller, to 0.6 of the font at least, with the narrow inner margin of a key of the pad
   (ADR-0172 item 2): on a phone of 360 pixels a cell is 79 pixels wide, and "Dimensions" needs 0.64 of the font there. The ring of
   a body and the command line keep "Dimensions".
6. **The list "New" leaves (amends ADR-0076 item 7).** The second level of "New" and the command line give each kind.
7. **The test.** `PartRingTests` holds items 1 to 5. The layout test of ADR-0172 item 5 gets two states: the open
   structure view and the open file menu. Its state "the button Images... in the panel" becomes the state of the ring of
   the part with images.
8. **No contract change.**

## Consequences

**Good:** with no selection, the part has the whole display less the folded row: about 2 % of 1280 by 720 and about 5 % of
a phone, instead of 29 % and 64 %. A person finds the tools of the part where the tools of a body are. The file actions
and the toggles of the dimensions are in the menus that the study proposed.

**Bad:** "New" and "Sketch on" need two taps instead of one or two. The file actions need two taps. A person who knew the
panel must learn the ring of the part. The structure view uses rows of 44 pixels until R31b. While the net of the views
is open, the structure view and the ring of the part do not show, and on the paper of a flat view the ring of the part does
not show.

**Next:** R31b, the rows of 30 pixels for a mouse or a pen, and the setting "Compact" or "For touch".

## History

- 2026-11-19 · TASK-0419 · The record, from the study of the left panel and the choices of the owner of 2026-11-18 (B plus
  C, density B).
- 2026-11-20 · TASK-0420 · ADR-0175 amends item 3 (the rows of 30 pixels in the density "Compact", and no gap between two
  body rows) and item 5 (the cells "Compact" and "For touch" on the row of "Top").
- 2026-11-21 · TASK-0447 · ADR-0184 item 1 amends item 1: the ring of the part has "Section" after "Bodies" while the part has
  a live body, and while a section lasts it holds the tools of the section.
