---
id: 0176
title: "Put on sheet" in the pad of a value of a flat view places that value on the view of the sheet with the same frame
status: Accepted
topic: Drawing, window
date: 2026-11-20
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - 3DEngine.App/SheetValuePlace.cs
  - 3DEngine.App/EdgeValues.cs
  - 3DEngine.App/SheetActions.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/Ui/DesktopUi.Pad.cs
  - 3DEngine.App/Ui/PanelAction.cs
  - 3DEngine.ImageTests/SheetValueTests.cs
enforced-by: 3DEngine.ImageTests/SheetValueTests.cs
---

# ADR-0176 — "Put on sheet" in the pad of a value of a flat view places that value on the view of the sheet with the same frame

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0162 item 7 records the answer D7 A of the owner of 2026-11-18: "Put on sheet" on a value chip of a flat view places that
value on the view of the sheet with the same frame. R22d keeps the dimension line and the proposal of the agent. Row R30e holds
the slice, and the owner ordered "then start R30e" on 2026-11-20.

The engine has the command: PlaceSheetDimension (ADR-0158 item 1) places the value of a constraint "distance" or "radius" of a
live sketch (kind "constraint") or the distance of an extrude in effect (kind "distance") on a view of a sheet, at a place on
the paper, and a sheet shows each value one time. The place is two numbers in millimetres that the client computes, so a
replay reads them from the log. GetSheet gives the centre (cu, cv) of the box of each view (ADR-0171 item 1): a point p of
the design goes to (X + f (p · right − cu), Y + f (p · up − cv)) on the paper, where f is the scale divided by the length of
the right (ADR-0155 item 5).

A tap on a chip of the selected body opens the pad of its value (ADR-0082 item 3), and that pad is the one control that a
chip has on the desktop and on the phone. ADR-0162 item 7 does not say where the words "Put on sheet" go, which sheet takes
the value, or where on the paper it goes. This record decides them.

## Decision

1. **The edge of a value.** A value of the selected body (the distance of the extrude, and each length and radius of its
   sketch) keeps the two ends of its measured edge in the design, as `EdgeValues.Value.Model`. A radius has one point, the
   middle of its curve, two times. A reference of a chain, a length that no constraint fixes, and a value of an active sketch
   have no edge: they have no "Put on sheet".
2. **The control.** The pad of a value of the selected body has a button "Put on sheet", 44 pixels high (ADR-0027 item 6),
   when three conditions hold: the window shows a flat view, the value has an edge, and a live sheet has a view with the frame
   of that flat view that is not a section. The button is a line at the full width under the fields of the pad, and the pad
   counts its height, so the keys keep their room (ADR-0172 item 2). A compact pad has no room for a line: there the button is
   one more column of the line of its fields. A tap sends the value to the host, and the pad stays open: the person can change
   the value after, and the sheet shows the new number at its next read.
3. **The sheet and the view.** The value goes on the first live sheet, in the order of the Document, that has a view with the
   exact frame of the flat view (`SheetPlacement.FrameOf`) and no section, on the first such view of that sheet. With no such
   sheet the text is "No sheet has the front view: add the view first.", and no command goes to the session.
4. **The place.** The value goes 10 mm of paper outside the middle of its measured edge on the paper: along the normal of the
   edge, on the side away from the centre of the view, (X, Y). For a radius, the direction goes from (X, Y) to its point. A
   direction of zero length is up. The code uses + − * / and sqrt only (`SheetValuePlace`). A place off the paper gives the
   refusal of the engine as the text.
5. **One command off the frame thread.** The source is the constraint of the value, or the extrude for the distance. A value
   that the sheet shows already gives "Sheet 1 shows this value already." and no command. Else the queue of the sheet
   (finding V196, TASK-0410) sends one PlaceSheetDimension, one step of undo, and the text is "Put 10.000 mm on the front view
   of Sheet 1.".

## Consequences

**Good:** a person puts a value on a sheet with two taps in the place where the value is, and the log holds the same command
that an agent sends. The place follows the edge, so the value is next to the line that it measures on the paper.

**Bad:** the value has no dimension line on the sheet until R22d. Two values of parallel edges near each other can go to places
that meet; the person cannot move a value on the sheet page yet. The values of an active sketch have no "Put on sheet".

## Alternatives considered

- **A key in the lines of keys of the pad.** The keys of the pad have a fixed grid in each state of ADR-0172, and a key of one
  pad only changes that grid.
- **A press and hold on a chip, with a menu.** It is a new gesture, and a person on the phone does not find it.
- **The place of the chip on the screen.** It depends on the zoom of the window, so the same value goes to a new place at each
  zoom.
- **A tap on the sheet page for the place.** It needs the page and a second mode; the sheet page can move a value later.

## History

- 2026-11-20 · TASK-0421 · The edge of a value, the line of the pad, the place and the command (phase R30e).
