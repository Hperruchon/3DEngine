---
id: 0165
title: Each view of a sheet has a flag for its hidden lines and one for its centre lines, the read gives the centre lines, and a whole circle reads as a diameter
status: Accepted
topic: Contracts, drawing
date: 2026-11-18
supersedes: []
superseded-by: []
amends: ['0153', '0155', '0158', '0159', '0160', '0161']
amended-by: []
affects:
  - Engine.Contracts/Drawing/SheetRecord.cs
  - Engine.Core/Commands/SetSheetViewLinesCommand.cs
  - Engine.Core/Commands/SetSheetViewLinesCommandHandler.cs
  - Engine.Core/Drawing/ViewEdges.cs
  - Engine.Core/Drawing/SheetLayout.cs
  - Engine.Core/Drawing/SheetSources.cs
  - Engine.Core/Queries/GetSheetQuery.cs
  - Engine.Core/Queries/GetSheetQueryHandler.cs
  - Engine.Core/Export/SvgExport.cs
  - Engine.Core/Export/DxfExport.cs
  - Engine.Core/Export/PdfExport.cs
  - Engine.Tests/Drawing/SheetViewTests.cs
enforced-by: Engine.Tests/Drawing/SheetViewTests.cs
---

# ADR-0165 — Each view of a sheet has a flag for its hidden lines and one for its centre lines, the read gives the centre lines, and a whole circle reads as a diameter

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0162 records the answers of the owner of 2026-11-18 on the 2D views. Items 11, 12 and 14 of it (D14 A, D15 A and D17 A)
ask for a flag of the hidden lines of each view, on by default and off for a section; for centre lines and centre marks in
GetSheet and in the three files, with a flag of each view; and for ⌀ and the diameter of a whole circle. Row R30g of the
roadmap holds them. ADR-0162 item 17 says that this slice writes the exact shape of its change of `Engine.Contracts`. On
2026-11-18 the owner ordered "start R30g".

A view of a sheet goes into the events `sheet.created` and `sheet.changed` as its whole record, and a Document rebuilds its
sheets from the commands of its log. So a new field with a default keeps each log valid: a log written before this record
gives the same views with the default.

## Decision

1. **The contract (D14 A, D15 A; amends ADR-0153 item 4).** `SheetView` in `Engine.Contracts/Drawing/SheetRecord.cs` gets
   two fields at its end: `bool HiddenLines = true` and `bool CentreLines = true`. Each is true by default, so AddSheetView
   and each log before this record give a view that shows its hidden lines and its centre lines. The events carry the two
   fields as `hiddenLines` and `centreLines` in each view of a sheet. No field is required, no name changes, and no event
   kind is new.
2. **The command (D14 A).** SetSheetViewLines takes the sheet, the view and the two flags, all required, and gives a new
   record of the sheet with the two flags of the view and the views in the same order: one step of undo. It refuses a sheet
   that is not live (`E-SHEET-NOT-FOUND`) and a view that the sheet does not have (`E-SHEET-VIEW-NOT-FOUND`), as
   MoveSheetView does. It lives in `Engine.Core/Commands` (ADR-0016).
3. **The centre lines (D15 A; amends ADR-0155 item 5).** GetSheet gives each view its two flags. A view with `HiddenLines`
   false gives no hidden line. A view with `CentreLines` true also gives its centre lines, of the kind "centre": a cross on
   each whole circle, its diameter along the right and along the up of the view; and the axis of each cylinder and each cone
   that has lines of its outline on the view, from the lowest to the highest place of those lines along the axis, so the axis
   of a hole ends where the hole ends. The centre lines come from each line of the view, the hidden lines too, so the axis of
   a hole stays when its walls do not show. On the paper each centre line goes 3 mm past its ends (ISO 128), and the box and
   the place of a view leave the centre lines out, so a flag never moves a view. The three files draw a centre line thin,
   0.25 mm, with a long dash of 6 mm, a gap of 0.75 mm, a dot of 0.125 mm and a gap of 0.75 mm (ISO 128, type 04), between
   the hidden lines and the visible lines; the DXF file puts them on a layer CENTER of the colour 8 with a line type CENTER
   (amends ADR-0159 item 3, ADR-0160 item 3 and ADR-0161 item 3).
4. **The diameter (D17 A; amends ADR-0158 item 4).** A dimension whose source is a constraint `radius` on a whole circle of
   its sketch reads with the label "diameter" and twice the radius; on an arc it stays "radius" with the radius. The SVG file
   prints ⌀ and the value, the DXF file "%%c" and the value (the code of AutoCAD for the sign), and the PDF file Ø and the
   value, because WinAnsiEncoding has no ⌀; the width of Ø in Helvetica is 778 thousandths (amends ADR-0159 item 5, ADR-0160
   item 6 and ADR-0161 item 6).
5. **Not in R30g.** A section and its flag off by default (R30h), a centre mark on an arc, a centre line of a sphere seen from
   the side other than its cross, and a control of the window for the flags (R30c).

## Consequences

**Good:** a drawing shows the axes and the centres that a workshop reads, a person or an agent turns the hidden lines off
where they add nothing, and a hole prints ⌀16 and not R8. Each log stays valid.

**Bad:** each view now carries two more fields in its events. The cross of a circle and the axis of a cylinder are the only
centre lines; a pattern of holes gets one cross for each hole and no line between them.

## History

- 2026-11-18 · TASK-0405 · The record, for row R30g and ADR-0162 items 11, 12 and 14.
- 2026-11-18 · TASK-0405 · Shipped at v0.389.
