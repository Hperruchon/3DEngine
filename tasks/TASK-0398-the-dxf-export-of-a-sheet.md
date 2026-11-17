---
id: 0398
title: The export of a sheet as one DXF file
status: Done
phase: P10b
opened: 2026-11-17
depends-on: [0396]
governed-by: [0002, 0004, 0016, 0018, 0019, 0030, 0048, 0083, 0092, 0159, 0160]
writes:
  create:
    - tasks/TASK-0398-the-dxf-export-of-a-sheet.md
    - docs/adr/0160-the-dxf-export-of-a-sheet.md
    - Engine.Core/Export/DxfExport.cs
    - Engine.Tests/Export/DxfExportTests.cs
  modify:
    - Engine.Core/Export/SvgExport.cs
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
    - 3DEngine.App/**
    - 3DEngine.Core/**
---

# TASK-0398 — The export of a sheet as one DXF file

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row P10b of the roadmap: the export of a sheet as DXF, for the exchange with a CAD program and a laser cutter (ADR-0118 item
2, second of three formats). P10a (TASK-0396, ADR-0159) gave the SVG file from the read GetSheet. On 2026-11-17 the owner
ordered "start P10b".

## Goal

A person or an agent gets one sheet as one DXF file, from the engine, the HTTP surface or the command line, with the lines of
each view, the frame, the title block and the value of each dimension.

## Scope (in)

1. `DxfExport` in `Engine.Core/Export`, from the read of GetSheet (ADR-0160 items 1 to 7).
2. The route `GET /export/dxf` and the command `engine export dxf` (ADR-0160 item 8).
3. The scale text of `SvgExport` becomes public, so the two files give the same title block.

## Scope (out)

- DXF of release 2000 or later, an ELLIPSE or SPLINE entity, a dimension line and an arrow (R22d), and the PDF export (P10c).
- A button of the desktop.
- A change of the read of a sheet (`Engine.Core/Drawing/**`, `Engine.Core/Queries/**`).

## Acceptance criteria

- [x] Each part of the file, the route and the command line has a test, and each rule has an injection that a test catches.
- [x] Each arc of an ellipse is within 0.001 mm of the ellipse, at 17 points of each arc of the chain.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p10b-sheet-dxf) · v0.383

## Method

Mechanical: the route and the command line, as for the SVG export; the tables of release 12.
Judgement:  release 12 with no handles; an arc as a bulge with no angle; an ellipse as arcs within a tolerance.
Weakest:    no CAD program opened the file in this task; the test reads it with its own reader of the pairs.

## Progress

- 2026-11-17: opened under the order of the owner of 2026-11-17 ("start P10b"), branch p10b-sheet-dxf after the merge of
  P0.161 (run 44162245091 on `main` passed). Fifteen injections each failed a test (twelve in `DxfExport.cs`, three in the
  route and the command line), and each file came back byte for byte. Four first did not: a lost start of an ellipse arc
  passed, because each start of the test was on an axis of the frame (the test now starts off the axes); two did not
  compile (code that cannot be reached); one did not apply, because a heredoc lost a backslash.
- 2026-11-17: run 44165659996 passed on the branch. The owner ordered "Close and merge". Closed at v0.383.
