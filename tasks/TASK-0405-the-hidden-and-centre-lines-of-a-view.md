---
id: 0405
title: Each view of a sheet shows or hides its hidden lines and its centre lines, and a whole circle reads as a diameter
status: Done
phase: R30g
opened: 2026-11-18
depends-on: [0403]
governed-by: [0004, 0008, 0012, 0016, 0018, 0021, 0028, 0031, 0033, 0037, 0048, 0060, 0083, 0089, 0112, 0142, 0143, 0144, 0145, 0147, 0153, 0155, 0158, 0159, 0160, 0161, 0164, 0165]
writes:
  create:
    - tasks/TASK-0405-the-hidden-and-centre-lines-of-a-view.md
    - docs/adr/0165-the-hidden-and-centre-lines-of-a-view.md
    - Engine.Core/Commands/SetSheetViewLinesCommand.cs
    - Engine.Core/Commands/SetSheetViewLinesCommandHandler.cs
  modify:
    - Engine.Contracts/Drawing/SheetRecord.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/Drawing/ViewEdges.cs
    - Engine.Core/Drawing/SheetLayout.cs
    - Engine.Core/Drawing/SheetSources.cs
    - Engine.Core/Queries/GetSheetQuery.cs
    - Engine.Core/Queries/GetSheetQueryHandler.cs
    - Engine.Core/Export/SvgExport.cs
    - Engine.Core/Export/DxfExport.cs
    - Engine.Core/Export/PdfExport.cs
    - Engine.Tests/Drawing/SheetViewTests.cs
    - Engine.Tests/Drawing/SheetDimensionTests.cs
    - Engine.Tests/Drawing/ViewEdgesTests.cs
    - Engine.Tests/Drawing/ViewLinesTests.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - Engine.Tests/Export/SvgExportTests.cs
    - Engine.Tests/Export/DxfExportTests.cs
    - Engine.Tests/Export/PdfExportTests.cs
    - docs/adr/0153-the-sheet-in-the-document.md
    - docs/adr/0155-the-views-of-a-sheet.md
    - docs/adr/0158-the-dimensions-of-a-sheet.md
    - docs/adr/0159-the-svg-export-of-a-sheet.md
    - docs/adr/0160-the-dxf-export-of-a-sheet.md
    - docs/adr/0161-the-pdf-export-of-a-sheet.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Kernel/**
    - 3DEngine.App/**
    - 3DEngine.Core/**
---

# TASK-0405 — Each view of a sheet shows or hides its hidden lines and its centre lines, and a whole circle reads as a diameter

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row R30g of the roadmap, from ADR-0162 items 11, 12 and 14 (the answers D14 A, D15 A and D17 A of the owner of 2026-11-18).
It runs beside R30a, which a separate session does in the window. On 2026-11-18 the owner ordered "Start them" for R30g and
R30a, then "start R30g". The change of `Engine.Contracts` has its ADR, ADR-0165, in the same commit (`CLAUDE.md`, "Stop and
ask"; ADR-0162 item 17 says that this slice gives the exact shape).

## Goal

A drawing shows the centre lines of each hole and each round side, each view can hide its hidden lines or its centre lines,
and a hole prints ⌀ and its diameter.

## Scope (in)

1. The two flags of `SheetView` and the command SetSheetViewLines (ADR-0165 items 1 and 2).
2. The centre lines in GetSheet and in the SVG, the DXF and the PDF file (ADR-0165 item 3).
3. The diameter of a whole circle in the read and in the three files (ADR-0165 item 4).

## Scope (out)

- The section view (R30h), a control of the window for the flags (R30c), and each change of the window (R30a).

## Acceptance criteria

- [x] Each rule has a test, each new test fails on the code before the change, and each rule has an injection that a test
      catches.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r30g-centre-lines) · v0.390

## Method

Mechanical: the command as MoveSheetView, the layers and the dashes of the three files.
Judgement:  the centre lines from the curves of the design and from the extent of the outline, so the axis of a hole ends
            where the hole ends; the box of a view without the centre lines, so a flag never moves a view; Ø in the PDF
            file, because WinAnsiEncoding has no ⌀.
Weakest:    a centre line of an arc, of a sphere from the side and between the holes of a pattern does not exist.

## Progress

- 2026-11-18: opened under the order of the owner "start R30g", branch r30g-centre-lines from `main` at v0.388. The session
  told the R30a session to take TASK-0406 and ADR-0166. Each test that counted the lines of a view and the two tests of a
  radius on a circle now take the centre lines and the diameter into account.
- 2026-11-18: twelve injections each failed a test at once, and each file came back byte for byte. Run 44285305081 passed
  on the branch. Closed at v0.389.
- 2026-11-18: R30a merged first as v0.389 (TASK-0406, ADR-0166). The merge of `main` into this branch keeps both sets of
  lines in the list files, and this task closes at v0.390.
- 2026-11-18: with R30a merged, `ViewLinesTests.The_Lines_Are_The_Lines_Of_The_Sheet_Moved_By_One_Shift` compared the lines
  of a sheet view, now with the axis of a hole, with the lines of a flat view, which has no centre line. The test now leaves
  the centre lines of the sheet out; a flat view shows no centre line yet.
- 2026-11-18: the merge of `main` set the task to Active, because it changed a test of R30a; this commit closes it again
  at v0.390.
