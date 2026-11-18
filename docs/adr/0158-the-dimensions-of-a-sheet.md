---
id: 0158
title: A dimension of a sheet shows a length or a radius of a sketch or the distance of an extrude, and a read gives its value from the design or "lost"
status: Amended
topic: Drawing
date: 2026-11-16
supersedes: []
superseded-by: []
amends: ['0153']
amended-by: ['0165']
affects:
  - Engine.Core/Commands/PlaceSheetDimensionCommand.cs
  - Engine.Core/Commands/PlaceSheetDimensionCommandHandler.cs
  - Engine.Core/Commands/RemoveSheetDimensionCommand.cs
  - Engine.Core/Commands/RemoveSheetDimensionCommandHandler.cs
  - Engine.Core/Commands/SheetResults.cs
  - Engine.Core/Drawing/SheetSources.cs
  - Engine.Core/Queries/GetSheetQuery.cs
  - Engine.Core/Queries/GetSheetQueryHandler.cs
  - Engine.Core/Hosting/HandlerCatalog.cs
  - Engine.Core/DiagnosticCodes.cs
  - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
  - Engine.Tests/Drawing/SheetDimensionTests.cs
  - Engine.Tests/Http/HttpSheetScenarioTests.cs
  - Engine.Tests/Persistence/DocumentFileTests.cs
enforced-by: Engine.Tests/Drawing/SheetDimensionTests.cs
---

# ADR-0158 — A dimension of a sheet shows a length or a radius of a sketch or the distance of an extrude, and a read gives its value from the design or "lost"

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0153 item 5 puts a dimension of a sheet in the Document: its view, its source (a constraint of a sketch, or an extrude for
its distance) with the kind of the source, and the place of its line on the paper. The number comes from the design at each
read, and a dimension whose source is not in the Document is "lost". Item 6 names the commands of P10.3:
PlaceSheetDimension and RemoveSheetDimension. P10.2 (ADR-0155) gave the views and the read GetSheet.

A side session wrote the plan of P10.3 with seven questions
(`docs/proposals/2026-11-15-plan-p10-3-the-dimensions-of-a-sheet.md`). On 2026-11-15 the owner answered them with option A,
as D1 to D7 of the answer sheet (`docs/proposals/2026-11-15-answer-sheet.md`). ADR-0157 records the other answers of that
sheet.

## Decision

1. **PlaceSheetDimension (Q1 A, Q3 A, Q4 A).** The command takes the sheet, the view, the source, the kind of the source
   (`"constraint"` or `"distance"`), `x` and `y` with the unit Length, and an optional `dimensionId`. A new dimension takes
   the identifier of its command. The checks come in this order, and the first failure gives its code:
   - the sheet is live;
   - with no `dimensionId`, the log does not hold the identifier; with a `dimensionId`, the sheet holds that dimension;
   - the sheet holds the view;
   - the kind of the source is `"constraint"` or `"distance"`;
   - the source is in the design now: a constraint of kind `distance` or `radius` in a live sketch, or an Extrude in effect;
   - the place is finite and on the paper, as the centre of a view (ADR-0155 item 4);
   - no other dimension of the sheet has the same source. Another sheet can show it.
   The command does not test that the view shows the length true: the design changes after the command, so such a test
   becomes false later.
2. **A place again (Q2 A).** With a `dimensionId`, the command gives that dimension a new view, source and place, with the
   same identifier at the same index, in one step of undo. It moves the line of a dimension, and it gives a lost dimension a
   new source.
3. **RemoveSheetDimension.** The command takes the sheet and the dimension. The new record has the other dimensions in the
   same order. The command does not read the source, so a lost dimension can be removed. RemoveSheetView removes the
   dimensions of its view (ADR-0155 item 3), and a change of a source changes no sheet record.
4. **The read (Q6 A, Q7 A).** GetSheet gives each dimension in the order of the record: its record, `lost`, `label` and
   `value`. A constraint `distance` gives the label `"length"`, a constraint `radius` gives `"radius"`, and an extrude gives
   `"distance"`. The value is the first value of the constraint, or the distance in effect of the extrude, at the time of the
   read. A dimension whose source is not in the design is lost: `lost` is true, and the label and the value are null. "Lost"
   is never stored, so an undo that gives the source back gives the value back. A lost dimension gives no diagnostic code.
   The read copies numbers, and doubles a radius into a diameter (ADR-0165 item 4). The read gives no geometry of the dimension line: the export of P10a or
   R22d adds the ends of the measured edge on the paper.
   ADR-0165 item 4: a constraint `radius` on a whole circle of its sketch gives the label "diameter" and twice its value;
   on an arc it stays "radius".
5. **The codes (Q5 A).** `E-SHEET-DIMENSION-NOT-FOUND` is a dimension that is not on the sheet. `E-SHEET-DIMENSION-INVALID`
   is a kind of a source that is not one of the two, a constraint of another kind, a place that is not finite or not on the
   paper, or a second dimension of a source. `E-SHEET-SOURCE-NOT-FOUND` is a source that is not in the design.
   `E-SHEET-NOT-FOUND`, `E-SHEET-ID-IN-USE` and `E-SHEET-VIEW-NOT-FOUND` get new raise sites with the same meaning.
6. **No contract change.** The commands, the list of the kinds of a source, and the result record `SheetDimensionInfo` are in
   `Engine.Core`. The record of a dimension is the record of ADR-0153 item 5.

## Consequences

**Good:** a sheet holds dimensions that a replay gives back, and each read gives the value of the design now.

**Bad:** undo is linear, so an undo of a source first removes each later dimension. No command removes an extrude, so only a
removed constraint makes a dimension lost now; a later command that removes a feature makes a distance lost with no change
here. The kind `"distance"` of a source and the kind `distance` of a constraint have the same word; the label of the read
keeps them apart.

## History

- 2026-11-16 · TASK-0395 · The record, from the plan of P10.3 and the answers of the owner of 2026-11-15 to D1 to D7.
- 2026-11-16 · TASK-0395 · Shipped at v0.380.
- 2026-11-18 · TASK-0405 · Amended by ADR-0165 item 4: a radius on a whole circle reads as a diameter (R30g).
