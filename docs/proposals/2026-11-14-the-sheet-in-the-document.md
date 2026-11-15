# The sheet in the Document: the questions before P10

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

- Date: 2026-11-14.
- Asked by: roadmap row P10 and ADR-0118 item 3. P10 changes the public shape of `Engine.Contracts`, so its ADR and a
  question to the owner come before its code (`CLAUDE.md`, "Stop and ask").
- Status: open. The owner answers Q1 to Q8; the answers go into the first commit of P10, with its ADR. Under the order of
  2026-11-14 a change of the contract is recorded and not done, so no answer is taken in the place of the owner here.

## 1. Why the work stops here

After R22c (v0.369) each product row waits for P10:

- R22d (the agent proposes the place of each dimension) needs a place on a sheet, which is a command (ADR-0118 item 3).
- P10a to P10c (the export of a sheet as SVG, DXF and PDF) need the sheet.
- The assembly work (K10 and later) comes after P10c (ADR-0126, the owner, 2026-11-12).

The engine already has what a sheet reads: the visible and hidden lines of a view, exact, with the circles, the arcs and the
ellipses of the design (`Engine.Core/Drawing/ViewEdges.cs`, K9a to K9c), and the values of the design with their references
and free sizes (R22a to R22c).

## 2. What is decided already

- The sheet, its views, the scale of each view and the place of each dimension are in the Document, as commands of the log
  (Q2 A of 2026-11-10, ADR-0118 item 3).
- The lines of a view are computed and not stored; the direction of a view is a matrix and not an angle (determinism rules 2
  and 6).
- ISO first angle by default, third angle as a choice of a sheet (Q3 A of 2026-11-10, ADR-0118 item 4).
- The exports come in the order SVG, DXF, PDF (Q4 of 2026-11-10).

## 3. The shape that the questions shape

A new collection `Sheets` in `Document`, each a record that never changes (an edit gives a new record with the same
identifier, as `SketchRecord` and `ImageRecord` do), with the events `sheet.created`, `sheet.changed` and `sheet.removed` and
the whole record as their payload, as for a sketch, an image or a datum.

## 4. The questions, each with the recommended option first

| Q | The question | A (recommended) | B |
|---|---|---|---|
| Q1 | The record of a sheet | One record holds the sheet, its views and its dimensions, and each command gives a new record. | A record for each view and for each dimension, with their own events. |
| Q2 | The paper | ISO A4 to A0, landscape or portrait, by name; the frame and the title block are presentation. | Any width and height in millimetres. |
| Q3 | The direction of a view | A matrix of nine doubles in the command (determinism rule 2); the client gives the six standard views as named matrices. | The six standard views only, by name. |
| Q4 | The scale of a view | A ratio of two whole numbers, as 1 : 2 or 2 : 1, so a length on the sheet is exact. | A double. |
| Q5 | A dimension on a sheet | A reference to its source (a constraint of a sketch, or the distance of an extrude) and the place of its line on the sheet; the number comes from the design at each read. | A copy of the number in the record. |
| Q6 | A lost dimension | It keeps its record, and a read says "lost" when its source is gone (ADR-0118 item 5); a person removes it or gives it a new source. | A removal of the source removes the dimension. |
| Q7 | The commands | Small commands: CreateSheet, RenameSheet, AddSheetView, MoveSheetView, RemoveSheetView, PlaceSheetDimension, RemoveSheetDimension, RemoveSheet; undo and redo by the log. | One command SetSheet with the whole record. |
| Q8 | The source of a view | A view shows the live bodies of the part, as the lines of K9 do; a later choice can name bodies. | A view names its bodies from its start. |

## 5. What follows the answers

1. P10: the ADR of the contract, the record, the commands, the events, the query of a sheet, and the reader of a file.
2. R22d: the agent proposes the place of each dimension on the sheet (ADR-0092).
3. P10a to P10c: the exports, from the exact lines of K9 and the values of the design.
