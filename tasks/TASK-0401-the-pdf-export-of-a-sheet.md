---
id: 0401
title: The export of a sheet as one PDF file
status: Done
phase: P10c
opened: 2026-11-17
depends-on: [0399]
governed-by: [0002, 0004, 0016, 0018, 0019, 0030, 0048, 0083, 0092, 0159, 0160, 0161]
writes:
  create:
    - tasks/TASK-0401-the-pdf-export-of-a-sheet.md
    - docs/adr/0161-the-pdf-export-of-a-sheet.md
    - Engine.Core/Export/PdfExport.cs
    - Engine.Tests/Export/PdfExportTests.cs
  modify:
    - Engine.Api.Http/Endpoints/ExportEndpoint.cs
    - Engine.Api.Http/EngineSurface.cs
    - Engine.Cli/Cli.cs
    - Engine.Cli/Usage.cs
    - docs/diagnostics.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/Drawing/**
    - Engine.Core/Queries/**
    - Engine.Core/Export/SvgExport.cs
    - Engine.Core/Export/DxfExport.cs
    - 3DEngine.App/**
    - 3DEngine.Core/**
---

# TASK-0401 — The export of a sheet as one PDF file

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row P10c of the roadmap: the export of a sheet as PDF, for the documentation (ADR-0118 item 2, third of three formats). P10a
(ADR-0159) gave the SVG file and P10b (ADR-0160) the DXF file from the read GetSheet. The codebase review of 2026-11-17
(TASK-0399) is the review before this product milestone. On 2026-11-17 the owner ordered "start the review, then P10c" and
"merge P10c when green".

## Goal

A person or an agent gets one sheet as one PDF file, from the engine, the HTTP surface or the command line, with the lines of
each view, the frame, the title block and the value of each dimension.

## Scope (in)

1. `PdfExport` in `Engine.Core/Export`, from the read of GetSheet (ADR-0161 items 1 to 7).
2. The route `GET /export/pdf` and the command `engine export pdf` (ADR-0161 item 8).

## Scope (out)

- The findings E211 to E217 of the review of 2026-11-17 (TASK-0400), and each change of the SVG and the DXF writer.
- An embedded font, a page of more than one sheet, a dimension line and an arrow (R22d), and a button of the desktop.

## Acceptance criteria

- [x] Each part of the file, the route and the command line has a test, and each rule has an injection that a test catches.
- [x] Each Bézier curve of an arc is within 0.001 mm of the circle or the ellipse, at 17 points of each curve.
- [x] A reader of PDF opens a sample of the engine with its views, its hidden lines and its title block.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p10c-sheet-pdf) · v0.385

## Method

Mechanical: the route and the command line, as for the SVG and the DXF export; the table of the objects of PDF.
Judgement:  Bézier curves with a stop at each quarter turn and a bound of their error; the widths of Helvetica for a
            centred value; no date in the file.
Weakest:    the bound 0.1 · a · t⁶ of the error of a curve comes from the known error of a quarter turn; the test measures
            17 points of each curve, not each point.

## Progress

- 2026-11-17: opened under the order of the owner of 2026-11-17, branch p10c-sheet-pdf after the merge of the review
  (TASK-0399). The Read tool of the session opened a sample PDF of a plate with a hole and a turned cylinder: the frame,
  the title block with its middle dots, the hidden lines and the ellipses were correct. Fourteen injections in
  `PdfExport.cs`: twelve each failed a test at once. One (the minor axis not turned) passed, because the test did not check
  the direction of an arc; the test now checks the parameter of each point. One (the last stop of a quarter) changed only
  the rounding, so the code lost that expression. One first did not apply, because the Write tool turned the escapes
  `\u00a0` and `\u00ff` into their characters; the file now has the escapes. Each file came back byte for byte.
- 2026-11-17: run 44180590636 passed on the branch. Closed at v0.385, and merged under the order "merge P10c when green".
