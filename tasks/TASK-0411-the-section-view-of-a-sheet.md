---
id: 0411
title: A section of a sheet gives its cut, its hatch, its cut line and its label in GetSheet and in the three files
status: Active
phase: R30h
opened: 2026-11-18
depends-on: [0405, 0408]
governed-by: [0004, 0008, 0012, 0016, 0018, 0021, 0028, 0031, 0033, 0037, 0048, 0060, 0083, 0089, 0112, 0142, 0143, 0144, 0145, 0147, 0153, 0155, 0158, 0159, 0160, 0161, 0164, 0165, 0166, 0167, 0168, 0169]
writes:
  create:
    - tasks/TASK-0411-the-section-view-of-a-sheet.md
    - docs/adr/0169-the-section-view-of-a-sheet.md
    - Engine.Core/Commands/AddSheetSectionCommand.cs
    - Engine.Core/Commands/AddSheetSectionCommandHandler.cs
    - Engine.Core/Drawing/SectionCut.cs
    - Engine.Tests/Drawing/SheetSectionTests.cs
  modify:
    - Engine.Contracts/Drawing/SheetRecord.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/Drawing/ViewEdges.cs
    - Engine.Core/Drawing/SheetLayout.cs
    - Engine.Core/Queries/GetSheetQuery.cs
    - Engine.Core/Queries/GetSheetQueryHandler.cs
    - Engine.Core/Export/SvgExport.cs
    - Engine.Core/Export/DxfExport.cs
    - Engine.Core/Export/PdfExport.cs
    - Engine.Tests/Drawing/SheetViewTests.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - Engine.Tests/Export/SvgExportTests.cs
    - Engine.Tests/Export/DxfExportTests.cs
    - Engine.Tests/Export/PdfExportTests.cs
    - docs/adr/0153-the-sheet-in-the-document.md
    - docs/adr/0155-the-views-of-a-sheet.md
    - docs/adr/0159-the-svg-export-of-a-sheet.md
    - docs/adr/0160-the-dxf-export-of-a-sheet.md
    - docs/adr/0161-the-pdf-export-of-a-sheet.md
    - docs/adr/0165-the-hidden-and-centre-lines-of-a-view.md
    - docs/adr/README.md
    - tasks/TASK-0410-the-findings-of-the-review-of-2026-11-18.md
    - docs/diagnostics.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Kernel/**
    - 3DEngine.App/**
    - 3DEngine.Core/**
---

# TASK-0411 — A section of a sheet gives its cut, its hatch, its cut line and its label in GetSheet and in the three files

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row R30h of the roadmap, from ADR-0162 items 11, 13 and 17 (the answers D14 A and D16 A of the owner of 2026-11-18). On
2026-11-18 the owner ordered "start R30h", then "and r30d". R30d (TASK-0412, ADR-0170) runs in the session of R30a and R30c,
and it changes the sheet page. So this task leaves the window alone, and the action "Section" of the sheet page comes after
R30d (ADR-0169 item 8). The change of `Engine.Contracts` has its ADR, ADR-0169, in the same commit (`CLAUDE.md`, "Stop and
ask").

## Goal

A sheet can hold a section of a view, and GetSheet, the SVG file, the DXF file and the PDF file give its cut faces with their
hatch, its cut line with its arrows and its letters on the parent, and its label "A-A".

## Scope (in)

1. The record `SheetSection` and the field `Section` of `SheetView` (ADR-0169 item 1).
2. The command AddSheetSection and its refusals (ADR-0169 item 2).
3. The cut of each solid by the plane of a section (ADR-0169 item 3).
4. The hatch, the cut line, the arrows, the letters and the label in GetSheet (ADR-0169 items 4, 5 and 7).
5. The new kinds of line and the texts in the three files (ADR-0169 item 6).

## Scope (out)

- The action "Section" of the sheet page and each other change of the window. It comes after R30d.
- A cut line with steps, a half section and a detail view.

## Acceptance criteria

- [ ] Each rule has a test, each new test fails on the code before the change, and each rule has an injection that a test
      catches.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the command as AddSheetView, the layers, the widths and the order of the three files.
Judgement:  the cut as a subtraction of a box of the owned kernel; the frame of a section from the look on the parent; the
            hatch on the polygon of the kernel and not on the arc.
Weakest:    the cost of a read with a section on a heavy body is not measured.

## Progress

- 2026-11-18: opened under the order of the owner "start R30h", branch r30h-section from `main` at v0.393 (run 44325262804
  on `main` passed).
- 2026-11-18: a picture of the plate with two sections showed that section A-A had no axis of the hole, because its cut goes
  through the corners of the facets and only an outline gave an axis; an edge of a cut face along the axis now gives it
  (ADR-0169 item 3, amends ADR-0165 item 3). Twenty-four injections each failed a named test after a test of a cut across
  the hole came in, and each file came back byte for byte.
