---
id: 0428
title: A sheet holds the fields of the title block of ISO 7200, SetSheetTitle gives them, and the files and the page draw the block, the symbol of the projection and the centring marks
status: Active
phase: R30i
opened: 2026-11-21
depends-on: [0426]
governed-by: [0004, 0006, 0007, 0008, 0012, 0013, 0015, 0016, 0018, 0019, 0020, 0021, 0025, 0027, 0028, 0029, 0030, 0031, 0032, 0033, 0034, 0037, 0045, 0048, 0060, 0072, 0083, 0089, 0112, 0114, 0153, 0155, 0158, 0159, 0160, 0161, 0164, 0165, 0166, 0167, 0168, 0169, 0170, 0171, 0177, 0179]
writes:
  create:
    - tasks/TASK-0428-the-title-block-of-a-sheet.md
    - docs/adr/0179-the-title-block-of-a-sheet.md
    - Engine.Core/Commands/SetSheetTitleCommand.cs
    - Engine.Core/Commands/SetSheetTitleCommandHandler.cs
    - Engine.Core/Drawing/TitleBlock.cs
    - Engine.Tests/Drawing/SheetTitleTests.cs
    - 3DEngine.ImageTests/SheetTitlePageTests.cs
  modify:
    - Engine.Contracts/Drawing/SheetRecord.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/Queries/GetSheetQuery.cs
    - Engine.Core/Queries/GetSheetQueryHandler.cs
    - Engine.Core/Export/SvgExport.cs
    - Engine.Core/Export/DxfExport.cs
    - Engine.Core/Export/PdfExport.cs
    - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - 3DEngine.App/SheetPlacement.cs
    - 3DEngine.App/Ui/DesktopUi.SheetPage.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - Engine.Tests/Export/SvgExportTests.cs
    - Engine.Tests/Export/DxfExportTests.cs
    - Engine.Tests/Export/PdfExportTests.cs
    - Engine.Tests/Http/HttpSheetScenarioTests.cs
    - 3DEngine.ImageTests/SheetAddTests.cs
    - 3DEngine.ImageTests/SheetPageTests.cs
    - docs/adr/0153-the-sheet-in-the-document.md
    - docs/adr/0155-the-views-of-a-sheet.md
    - docs/adr/0159-the-svg-export-of-a-sheet.md
    - docs/adr/0160-the-dxf-export-of-a-sheet.md
    - docs/adr/0161-the-pdf-export-of-a-sheet.md
    - docs/adr/0162-the-workflow-of-the-2d-views.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Kernel/**
---

# TASK-0428 — A sheet holds the fields of the title block of ISO 7200, SetSheetTitle gives them, and the files and the page draw the block, the symbol of the projection and the centring marks

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row R30i of the roadmap, from ADR-0162 items 15 and 17 (the answer D18 A of the owner of 2026-11-18). The codebase review of
2026-11-20 (v0.408), sections 5 and 6, examined R30i and asked Q4 to Q7. The change of `Engine.Contracts` has its ADR,
ADR-0179, in the same commit (`CLAUDE.md`, "Stop and ask"). The branch `r30i-title-block` starts from `1045aae2` (P0.171,
TASK-0426, v0.410), which is not merged yet.

## Goal

A sheet holds the eleven fields of its title block, one command sets them, and GetSheet, the SVG, the DXF and the PDF file and
the sheet page give the frame, the centring marks, the block with its fields and the symbol of the projection from one list.

## Scope (in)

1. The record `SheetTitleBlock` and the field `TitleBlock` of `SheetRecord` (ADR-0179 item 1).
2. The command SetSheetTitle, its refusals and the code `E-SHEET-TITLE-INVALID` (item 2).
3. The field `titleBlock` in `sheet.created`, `sheet.changed`, `/schema/events` and GetSheet (item 3).
4. `TitleBlock.Layout`: the block, the centring marks and the symbol (items 4 to 6), and the band of the area of new views.
5. The three files and the sheet page draw the list (item 7).

## Scope (out)

- A form of the fields on the sheet page (Q7 A): a slice of its own.
- A check of the form of a date, a field of the count of sheets or of the language, a revision table, a text on two lines.

## Decisions

Answered by the standing order of the owner of 2026-11-20 (Q4 to Q7 of the codebase review of 2026-11-20): the recommended
option A of each question.

- Q4 A: the block holds the nine fields of ADR-0162 item 15, plus the approval person and the document type, so that each field
  that ISO 7200 makes mandatory has a place.
- Q5 A: one command, SetSheetTitle, gives each field; an absent field is empty; one step of undo.
- Q6 A: the block is 180 mm wide at most and as high as its fields need (44 mm); an earlier view stays where its log put it,
  and no view moves.
- Q7 A: no form on the sheet page in R30i: the command, GetSheet, the three export files and the drawing of the block on the
  page; a form comes in a slice of its own.

The answers do not decide some points of the shape. The session took the smallest option of each, and ADR-0179, section
"Alternatives considered", lists them for the owner: one nested record, the rules of a name for each field, an empty block
after a command with each field empty, the name of the sheet in an empty title, a date as free text, a new code, the size of
44 mm and the cells, a smaller text in place of a cut or a second line, the centring marks, one DXF layer, the proportions of
the symbol, and a band across the paper for the area of the views.

## Acceptance criteria

- [x] Each rule has a test, and each rule has an injection that a named test catches, with the file restored byte for byte.
- [x] A clean build (`--no-incremental`) gives zero warnings, and the phone project builds with zero warnings.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass locally.
- [ ] Continuous integration passes on the branch and on `main` after the merge.

## Outcome

Status: Active · commit (the merge of the branch r30i-title-block) · v0.411

## Method

Mechanical: the command as SetSheetViewLines, the field of the events and of the schema, the sample of `DocumentFileTests`,
the code in the registry, the counts of `eng/test-floor.txt`.

Judgement: one list of lines and texts for the three files and the page, so they cannot disagree; the cells of the block and a
smaller size for a long text; the band of the area at the height of the block, which moves each new view of the old tests
12 mm up (`SheetAddTests`, `SheetPageTests`); a centre line of the symbol in DXF as a line with its own line type, so the
layer CENTER keeps the centre lines of the views only.

Weakest: the widths of Helvetica estimate the width of a text in the SVG file and on the page, which use other fonts, and the
proportions of the symbol and the centring marks come from memory of the standards. `SheetTitleTests` and
`SheetTitlePageTests` hold what is tested.

## Progress

- 2026-11-21: opened under the standing order of the owner of 2026-11-20, branch `r30i-title-block` from `1045aae2` (P0.171,
  v0.410). The answers Q4 to Q7 are in the section "Decisions".
- 2026-11-21: 26 injections, each restored byte for byte (sha256), each made a named test fail:
  - the block not kept, a field of the block before kept for an absent field, no check of a field, any sheet taken as live,
    a field not encoded in the file, no `titleBlock` in the events, no entry in `/schema/events`, GetSheet with the empty
    block, an undo that gives the record after, and a new sheet with an empty block in place of none: `SheetTitleTests`
    (the encode also `DocumentFileTests.Each_Handler_Of_The_Catalog_Reads_Back_Its_Own_Command`, the read also
    `HttpSheetScenarioTests`);
  - a block of 120 mm, a title with no name of the sheet, no smaller size of a long text, the symbol of the other projection,
    and a lower mark into the block: `SheetTitleTests`;
  - the cell lines thick, and no lines or no texts of the block in the SVG file: `SvgExportTests`; the same in the DXF file
    and no line type of a centre line: `DxfExportTests`; the same in the PDF file: `PdfExportTests`;
  - no lines or no texts of the block on the page: `SheetTitlePageTests`; the band of the area at 20 mm: `SheetAddTests`.
  The first run of the injection of the cell lines stayed green, because the test of the SVG file took the width from
  `SvgExport.Stroke`; the test now counts the thin and the thick paths of the file.
