---
id: 0175
title: Compact rows of the structure view for a mouse or a pen, and the setting "Compact" or "For touch"
status: Accepted
topic: Clients, layout
date: 2026-11-20
supersedes: []
superseded-by: []
amends: ['0027', '0091', '0174']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.Density.cs
  - 3DEngine.App/Ui/DesktopUi.Panel.cs
  - 3DEngine.App/Ui/DesktopUi.Views.cs
  - 3DEngine.ImageTests/RowDensityTests.cs
enforced-by: 3DEngine.ImageTests/RowDensityTests.cs
---

# ADR-0175 — Compact rows of the structure view for a mouse or a pen, and the setting "Compact" or "For touch"

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0027 item 6 makes each control at least 44 pixels high, so that a finger can hit it. Each row of a list is a control,
so each row of the structure view of ADR-0174 item 3 is 44 pixels high. On 2026-11-18 the owner said that the open
structure view was too spaced, and chose density B of the study of that date: rows of 30 pixels for a mouse or a pen, rows
of 44 pixels on a phone, and a setting "Compact" or "For touch" in the menu of the views. The choice depends on the device
and on the menu, not on each input, so the rows do not move under a finger. The ring, the anchors and the pad keep 44
pixels. ADR-0174 did R31a with rows of 44 pixels and gave the rows of 30 pixels to this record (R31b).

The drawings of the study used a text of 13 pixels in a row of 30 pixels and a column of 280 pixels. The text of the
application is 26 pixels (ADR-0027 item 6, ADR-0091 item 2), about 1.5 times the text of those drawings. A line of 26
pixels in a row of 30 pixels leaves 2 pixels above and below, and the row line touches the letters.

## Decision

1. **The two densities (amends ADR-0027 item 6 for the rows of the structure view).** In the density "Compact" each row of
   the structure view is 30 pixels high, and its text is Instrument Sans at 20 pixels, the text of a list. In the density
   "For touch" each row is 44 pixels high with the text of 26 pixels, as in R31a. A phone starts "For touch"; each other
   device starts "Compact". The density does not change with the last input. Each other control keeps 44 pixels: the
   ring, the anchors, the pad, the path line, the file menu, the filter, "Show all", the rows of a selected body and the
   cells of the net of the views.
2. **The setting (amends ADR-0174 item 5).** The net of the views holds "Compact" in the cell at the left of "Top" and "For
   touch" in the cell at its right, on the row of "Top". The cell of the density in use has the colour of a tool in use. A
   tap sets that density and does not close the net. The text of a cell that is too wide gets smaller, as for
   "Dimensions" (ADR-0174 item 5). The setting lasts for the session; the desktop saves no setting (ADR-0027 item 6 keeps
   no file of a layout).
3. **The compact rows (amends ADR-0174 item 3).** In the density "Compact" the first row of the structure view is one line:
   the kind, then the unit, the count of the bodies and their state, with "failed" in red. A body row gives its name at
   the left and its state at the right, as in R31a, in the text of a list. In each density two body rows have no gap
   between them, as in the drawings of the study; in R31a the gap of two items (10 pixels) was between two rows, so a row
   took 54 pixels. "Show all" keeps that gap above it. In the density "For touch" the first row is as high as its two
   lines and a margin of 4 pixels above and under them (60 pixels); in R31a it was 44 pixels, and the line under it crossed
   its second line. The view ends one margin of the window under its last row; in R31a the gap of two items was under it
   too. The structure view keeps its width, at most 360 pixels, the width
   of the rows of a selected body. The text of a compact row is 1.5 times the text of the drawings of the study, so a
   column of 280 pixels has less room for the name of a body and the code of a fault.
4. **The atlas (amends ADR-0091 item 2).** The atlas holds Instrument Sans a fourth time, at 20 pixels, for the text of a
   list.
5. **The test.** `RowDensityTests` holds items 1 to 4. The layout test of ADR-0172 item 5 runs its state of the open
   structure view in each density.
6. **No contract change.**

## Consequences

**Good:** on a desktop, each body row of the open structure view takes 30 pixels instead of 54 in R31a: 192 pixels less
for eight bodies. On a phone each body row takes 44 pixels instead of 54. A person with a finger on a tablet sets "For touch" one time, and the rows do not move with each
input.

**Bad:** the text of a compact row is smaller than each other text of the application. A tablet with no pen starts
"Compact" until the person sets "For touch". The setting does not last after the session. The first touch on a compact
list does not ask "Change to rows for touch?", as the study proposed.

**Next:** a saved setting, and the question on the first touch, when the owner asks for them. The tabs of an assembly in
the structure view (ADR-0174) use the same densities.

## History

- 2026-11-20 · TASK-0420 · The record, from the choice of the owner of 2026-11-18 (density B, 30 pixels) and the order "start
  R31b" of 2026-11-20.
