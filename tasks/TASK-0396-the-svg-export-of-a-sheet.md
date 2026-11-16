---
id: 0396
title: The export of a sheet as one SVG file
status: Done
phase: P10a
opened: 2026-11-16
depends-on: [0395]
governed-by: [0002, 0004, 0016, 0018, 0019, 0028, 0030, 0033, 0037, 0048, 0060, 0083, 0092, 0155, 0158, 0159]
writes:
  create:
    - tasks/TASK-0396-the-svg-export-of-a-sheet.md
    - docs/adr/0159-the-svg-export-of-a-sheet.md
    - Engine.Core/Export/SvgExport.cs
    - Engine.Tests/Export/SvgExportTests.cs
  modify:
    - Engine.Core/Queries/GetSheetQueryHandler.cs
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
    - 3DEngine.App/**
    - 3DEngine.Core/**
---

# TASK-0396 — The export of a sheet as one SVG file

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row P10a of the roadmap: the export of a sheet as SVG, from the exact numbers of the views and the values of the design
(ADR-0118 item 2). P10.2 gave the lines of each view on the paper, and P10.3 (TASK-0395) the value of each dimension. On
2026-11-16 the owner ordered "then start P10a, merge when green".

## Goal

A person or an agent gets one sheet as one SVG file, from the engine, the HTTP surface or the command line, with the lines of
each view, the frame, the title block and the value of each dimension.

## Scope (in)

1. `SvgExport` in `Engine.Core/Export`, from the read of GetSheet (ADR-0159 items 1 to 6).
2. The route `GET /export/svg` and the command `engine export svg` (ADR-0159 item 7).
3. The sentence of row R22d that takes the dimension line (answer D7 A of 2026-11-15).

## Scope (out)

- A dimension line and an arrow (R22d), the DXF export (P10b) and the PDF export (P10c).
- A button of the desktop.

## Acceptance criteria

- [x] Each part of the file, the route and the command line has a test, and each rule has an injection that a test catches.
- [x] A drawing of a real part shows in a browser with its views, its hidden lines and its values.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p10a-sheet-svg) · v0.381

## Method

Mechanical: the route and the command line, as for the STL export.
Judgement:  the arcs of an ellipse without an angle, the line widths and the title block.
Weakest:    a dimension has no line, so a reader must know which edge it measures.

## Progress

- 2026-11-16: opened under the order of the owner of 2026-11-16, branch p10a-sheet-svg after P10.3. A drawing of a plate
  with a through-hole showed the hole in the top view as one hidden circle: the lines of the view (K9) mark the circle of the
  top face hidden. The cause is outside this write set; a separate task holds it.
  Twenty-three injections each failed a test, and each file came back byte for byte; two first did not (one did not compile,
  and one met no arc whose end is off the axes), and a new case and a new form fixed them.
- 2026-11-16: run 43969315034 passed on the branch. Closed at v0.381.
