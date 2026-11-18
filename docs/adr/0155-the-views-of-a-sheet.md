---
id: 0155
title: A view of a sheet has an exact frame, a scale and a place on the paper, and GetSheet gives its lines on the paper
status: Amended
topic: Drawing
date: 2026-11-15
supersedes: []
superseded-by: []
amends: ['0153']
amended-by: ['0164', '0165']
affects:
  - Engine.Core/Commands/AddSheetViewCommand.cs
  - Engine.Core/Commands/AddSheetViewCommandHandler.cs
  - Engine.Core/Commands/MoveSheetViewCommand.cs
  - Engine.Core/Commands/MoveSheetViewCommandHandler.cs
  - Engine.Core/Commands/RemoveSheetViewCommand.cs
  - Engine.Core/Commands/RemoveSheetViewCommandHandler.cs
  - Engine.Core/Commands/SheetResults.cs
  - Engine.Core/Drawing/SheetLayout.cs
  - Engine.Core/Queries/GetSheetQuery.cs
  - Engine.Core/Queries/GetSheetQueryHandler.cs
  - Engine.Core/Hosting/HandlerCatalog.cs
  - Engine.Core/DiagnosticCodes.cs
  - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
  - Engine.Tests/Drawing/SheetViewTests.cs
  - Engine.Tests/Persistence/DocumentFileTests.cs
enforced-by: Engine.Tests/Drawing/SheetViewTests.cs
---

# ADR-0155 — A view of a sheet has an exact frame, a scale and a place on the paper, and GetSheet gives its lines on the paper

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0153 item 4 puts a view of a sheet in the Document: a direction of nine doubles, a scale of two whole numbers and the place
of its centre on the paper. Item 6 names the commands of P10.2: AddSheetView, MoveSheetView and RemoveSheetView. The engine
gives the exact lines of a view (ADR-0142 to ADR-0145, ADR-0147), and no client reads them yet.

The plan of P10.2 gave seven questions. On 2026-11-15 the owner answered "all A for Q1-Q7" in the chat of the main session,
and gave the same answer in the side session that prepared the plans of P10.3 and K10.

## Decision

1. **AddSheetView (Q1 A, Q3 A, Q4 A).** The command takes the sheet, nine number parameters `rightX` to `eyeZ` with the unit
   Direction, `scaleNumerator` and `scaleDenominator` with the unit Count, and `x` and `y` with the unit Length. The
   identifier of the view is the identifier of the command. The checks come in this order: the sheet is live; the log does
   not hold the identifier; the nine numbers are finite and give a valid exact frame (ADR-0142 item 2) with no tolerance; each
   part of the scale is greater than zero; the place is on the paper. A view from a sine and a cosine, as an isometric view,
   is not an exact frame of one length, so it is refused; register entry R-0068 holds the pictorial view.
2. **MoveSheetView.** The command takes the sheet, the view and a new place on the paper. The new record has the view at the
   new place and the views in the same order.
3. **RemoveSheetView (Q6 A).** The command takes the sheet and the view. The new record has no such view and no dimension of
   that view, so one undo gives both back.
4. **The paper and the place (Q4 A).** The paper has the size of ISO 216 in millimetres: A4 210 by 297, A3 297 by 420, A2 420
   by 594, A1 594 by 841, A0 841 by 1189, across by up when the sheet is portrait, and up by across when it is landscape. A
   place is in millimetres from the lower left corner, and the centre of a view is on the paper: from 0 to the width and from
   0 to the height. No command changes the paper, so a valid place stays valid.
5. **GetSheet (Q2 A).** The query gives the sheet, the size of its paper, its dimensions, and for each view its record, its
   lines on the paper and each body that gives no lines with its reason (ADR-0142 item 6). A point (u, v) of the view goes to
   (X + f (u − cu), Y + f (v − cv)): f is the scale divided by the length of the right of the frame, a length that never squares a double (ADR-0164 item 1),
   and (cu, cv) is the
   centre of the box of the lines of the view, from their ends and the whole circle or ellipse of a line that has one. A view
   with no line has its centre at the origin of the part. A circle and an ellipse on the paper have the size of the view
   times f. The numbers become doubles before the place, and the code uses + − × ÷ and sqrt only.
   ADR-0165 items 1 and 3: a view with `HiddenLines` false gives no hidden line, and a view with `CentreLines` true also
   gives its centre lines, of the kind "centre", 3 mm of paper past their ends; the box of the view leaves them out.
6. **The codes (Q5 A).** `E-SHEET-VIEW-NOT-FOUND` is a view that is not on the sheet. `E-SHEET-VIEW-INVALID` is a direction, a
   scale or a place of a view that is not valid. `E-SHEET-NOT-FOUND` is raised for a sheet that is not live by the three
   commands and by GetSheet, and `E-SHEET-ID-IN-USE` by AddSheetView, because a view takes the identifier of its command.
7. **No cache (Q7 A).** Each read computes the lines of each view. A test measures the read of three views of a sphere; when
   it takes more than 1 s, a register entry holds the time. On the computer of the owner it took 4.6 s and 5.1 s, so register
   entry R-0069 holds it.
8. **No contract change.** The commands, the query and its result records are in `Engine.Core`. The record of a view is the
   record of ADR-0153 item 4.

## Consequences

**Good:** a sheet holds views that a replay gives back, and a client gets each line of a view in millimetres on the paper.

**Bad:** a pictorial view waits for a later slice. A read of a sheet with a curved body takes seconds, and the query holds the
session for that time. The centre of a view moves when the part grows on one side.

## History

- 2026-11-15 · TASK-0393 · The record, from the plan of P10.2 and the answers of the owner of 2026-11-15 to Q1 to Q7.
- 2026-11-15 · TASK-0393 · Shipped at v0.377.
- 2026-11-18 · TASK-0400 · Amended by ADR-0164 item 1: the length of the frame never squares a double, so a frame of a length near 1e-200 or 1e200 gives a read that is finite (finding E212).
- 2026-11-18 · TASK-0405 · Amended by ADR-0165 items 1 and 3: the flags of a view and its centre lines in GetSheet (R30g).
