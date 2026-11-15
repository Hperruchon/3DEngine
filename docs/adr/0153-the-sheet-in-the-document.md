---
id: 0153
title: A drawing sheet is one record of the Document, with its views and its dimensions, and small commands change it
status: Amended
topic: Contracts, drawing
date: 2026-11-15
supersedes: []
superseded-by: []
amends: ['0118']
amended-by: ['0155']
affects:
  - Engine.Contracts/Drawing/SheetRecord.cs
  - Engine.Contracts/Document.cs
  - Engine.Contracts/Handlers/ICommandHandler.cs
  - Engine.Core/CommandBus.cs
  - Engine.Core/Commands/CreateSheetCommand.cs
  - Engine.Core/Commands/CreateSheetCommandHandler.cs
  - Engine.Core/Commands/RenameSheetCommand.cs
  - Engine.Core/Commands/RenameSheetCommandHandler.cs
  - Engine.Core/Commands/RemoveSheetCommand.cs
  - Engine.Core/Commands/RemoveSheetCommandHandler.cs
  - Engine.Core/Hosting/HandlerCatalog.cs
  - Engine.Tests/Drawing/SheetTests.cs
  - Engine.Tests/Persistence/DocumentFileTests.cs
  - Engine.Core/StepHistory.cs
  - Engine.Core/DiagnosticCodes.cs
  - Engine.Core/Commands/SheetResults.cs
  - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
  - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
enforced-by: Engine.Tests/Drawing/SheetTests.cs
---

# ADR-0153 — A drawing sheet is one record of the Document, with its views and its dimensions, and small commands change it

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0118 item 3 puts a drawing sheet, its views, the scale of each view and the place of each dimension in the Document, as
commands of the log, and says that P10 changes the public shape of `Engine.Contracts`, so its ADR and a question to the owner
come first. `docs/proposals/2026-11-14-the-sheet-in-the-document.md` gave eight questions (register entry R-0067). On
2026-11-14 the owner answered each one in the chat with option A.

The engine gives the exact lines of a view (ADR-0142 to ADR-0145, ADR-0147) and the values of the design on a flat view
(ADR-0146, ADR-0148, ADR-0149, ADR-0151); the window shows them. No record holds a sheet.

## Decision

1. **One record (Q1 A).** `Engine.Contracts.Drawing.SheetRecord` holds a sheet: its identifier, its name, its paper, its
   orientation, its projection, its views and its dimensions. A record never changes: an edit gives a new record with the
   same identifier, as a sketch and an image do. `Document.Sheets` holds each live sheet. The bus raises `sheet.created`,
   `sheet.changed` and `sheet.removed` with the whole record, after the events of the references; undo and redo restore the
   record before and after, as for an image. `CommandHandlerResult` gets `CreatedSheets`, `ChangedSheets` and
   `RemovedSheets`.
2. **The paper (Q2 A).** The paper is "A4", "A3", "A2", "A1" or "A0" (ISO 216), with `Landscape` true or false. The frame and
   the title block are presentation.
3. **The projection (ADR-0118 item 4).** `ThirdAngle` is false by default: ISO first angle.
4. **A view (Q3 A, Q4 A, Q8 A).** `SheetView` holds its identifier, a direction of nine doubles (the right, the up and the
   direction toward the eye, as `ViewFrame` of ADR-0142 item 2; determinism rule 2), its scale as two whole numbers greater
   than zero (`ScaleNumerator` : `ScaleDenominator`), and the place of its centre on the paper in millimetres. A view shows
   each live body of the part. The client gives the six standard views as named matrices.
5. **A dimension (Q5 A, Q6 A).** `SheetDimension` holds its identifier, its view, its source (the identifier of a constraint
   of a sketch, or of an extrude for its distance) with the kind of the source, and the place of its line on the paper in
   millimetres. The number comes from the design at each read. When the source is not in the Document, a read gives the
   dimension as "lost"; it keeps its record until a person removes it or gives it a new source.
6. **The commands (Q7 A).** CreateSheet, RenameSheet and RemoveSheet (P10.1); AddSheetView, MoveSheetView and RemoveSheetView
   (P10.2); PlaceSheetDimension and RemoveSheetDimension (P10.3). Each is one step of undo. A command on a sheet that is not
   live, a paper that is not one of the five, an empty name, a scale that is not two whole numbers greater than zero, or a
   direction that is not a valid frame fails with a diagnostic code of the namespace `E-SHEET-`.
7. **The slices.** P10.1 gives items 1 to 3, the record with its lists of views and dimensions, and the commands of item 6
   for a sheet; P10.2 and P10.3 add the commands of the views and of the dimensions, with no new shape of the contract.

## Consequences

**Good:** a replay gives the same sheets; a later slice adds commands and reads, not fields of the contract.

**Bad:** the contract grows by one collection, three event kinds and three lists of the handler result.

## History

- 2026-11-15 · TASK-0389 · The record, from the answers of the owner of 2026-11-14 to Q1 to Q8 (R-0067).
- 2026-11-15 · TASK-0389 · Shipped at v0.374.
- 2026-11-15 · TASK-0393 · ADR-0155 amends items 4 and 6: the exact frame, the place on the paper, the commands of a view,
  the read GetSheet and two codes of a view.
