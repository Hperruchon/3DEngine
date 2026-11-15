---
id: 0393
title: The views of a sheet, and a read of a sheet with the lines of each view on the paper
status: Active
phase: P10.2
opened: 2026-11-15
depends-on: [0391]
governed-by: [0004, 0012, 0013, 0016, 0018, 0019, 0021, 0028, 0031, 0033, 0037, 0060, 0083, 0089, 0112, 0114, 0153, 0155]
writes:
  create:
    - tasks/TASK-0393-the-views-of-a-sheet.md
    - docs/adr/0155-the-views-of-a-sheet.md
    - Engine.Core/Commands/AddSheetViewCommand.cs
    - Engine.Core/Commands/AddSheetViewCommandHandler.cs
    - Engine.Core/Commands/MoveSheetViewCommand.cs
    - Engine.Core/Commands/MoveSheetViewCommandHandler.cs
    - Engine.Core/Commands/RemoveSheetViewCommand.cs
    - Engine.Core/Commands/RemoveSheetViewCommandHandler.cs
    - Engine.Core/Drawing/SheetLayout.cs
    - Engine.Core/Queries/GetSheetQuery.cs
    - Engine.Core/Queries/GetSheetQueryHandler.cs
    - Engine.Tests/Drawing/SheetViewTests.cs
  modify:
    - Engine.Core/Commands/SheetResults.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - docs/diagnostics.md
    - docs/register.md
    - docs/adr/0153-the-sheet-in-the-document.md
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

# TASK-0393 — The views of a sheet, and a read of a sheet with the lines of each view on the paper

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row P10.2 of the roadmap: the views of a sheet (ADR-0153 items 4 and 6). P10.1 (TASK-0389) gave the record of a sheet with an
empty list of views. The engine gives the exact lines of a view (ADR-0142), and no client reads them. The plan of P10.2 gave
seven questions; on 2026-11-15 the owner answered "all A for Q1-Q7" and ordered TASK-0391 first.

## Goal

A person or an agent adds, moves and removes a view of a sheet, and a read of the sheet gives the lines of each view in
millimetres on the paper.

## Scope (in)

1. AddSheetView, MoveSheetView and RemoveSheetView, with undo and redo, in the document file (ADR-0155 items 1 to 4).
2. The query GetSheet with the lines of each view on the paper (ADR-0155 item 5).
3. The codes `E-SHEET-VIEW-NOT-FOUND` and `E-SHEET-VIEW-INVALID` (ADR-0155 item 6).
4. Register entry R-0068 for the pictorial view (Q1 A), and R-0069 for the time of a read (Q7 A).

## Scope (out)

- The dimensions of a sheet (P10.3).
- A client that shows a sheet, and a cache of a read.

## Acceptance criteria

- [x] Each command, each refusal, undo, redo and the document file have a test, and each check has an injection that a test
      catches.
- [x] A read gives the lines of a box at two scales, with a frame of length 2, in two views, and a mesh body as skipped.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the commands, their checks and the encoding, as for the commands of P10.1.
Judgement:  the box of the centre with whole circles and ellipses, and the centre of a view with no line.
Weakest:    the time of a read of a curved body (R-0069); the JSON of a view in an event has no test of the HTTP surface.

## Progress

- 2026-11-15: opened under the order of the owner of 2026-11-15 ("yes, all A for Q1-Q7, start TASK-0391"; the run of TASK-0391,
  then P10.2), branch p10-2-sheet-views from v0.376. The first form of AddSheetView held the direction as a list, and the test of
  the document file compares commands by value, so the command has nine number properties. Fifteen injections each failed a
  test, and each file came back byte for byte. Three views of a sphere took 4.6 s and 5.1 s on the computer of the owner.
