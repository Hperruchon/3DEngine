---
id: 0389
title: A drawing sheet is one record of the Document, and CreateSheet, RenameSheet and RemoveSheet change it
status: Done
phase: P10.1
opened: 2026-11-15
depends-on: [0386]
governed-by: [0004, 0006, 0008, 0012, 0013, 0015, 0016, 0019, 0020, 0021, 0028, 0031, 0033, 0037, 0045, 0060, 0083, 0089, 0112, 0114, 0153]
writes:
  create:
    - tasks/TASK-0389-the-sheet-in-the-document.md
    - docs/adr/0153-the-sheet-in-the-document.md
    - Engine.Contracts/Drawing/SheetRecord.cs
    - Engine.Core/Commands/CreateSheetCommand.cs
    - Engine.Core/Commands/CreateSheetCommandHandler.cs
    - Engine.Core/Commands/RenameSheetCommand.cs
    - Engine.Core/Commands/RenameSheetCommandHandler.cs
    - Engine.Core/Commands/RemoveSheetCommand.cs
    - Engine.Core/Commands/RemoveSheetCommandHandler.cs
    - Engine.Core/Commands/SheetResults.cs
    - Engine.Tests/Drawing/SheetTests.cs
  modify:
    - Engine.Contracts/Document.cs
    - Engine.Contracts/Handlers/ICommandHandler.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/StepHistory.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - eng/test-floor.txt
    - docs/adr/0118-the-2d-drawing-of-a-part.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - 3DEngine.App/**
    - 3DEngine.Core/**
    - Engine.Kernel/**
---

# TASK-0389 — A drawing sheet is one record of the Document, and CreateSheet, RenameSheet and RemoveSheet change it

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0118 item 3 puts a drawing sheet in the Document. P10 changes the public shape of `Engine.Contracts`, so register entry
R-0067 held its eight questions for the owner. On 2026-11-14 the owner took option A for each of Q1 to Q8.

## Goal

The Document holds sheets. A command creates, renames and removes a sheet, each with its event, one step of undo, and a replay
that gives the same sheet.

## Scope (in)

1. ADR-0153, which records the answers and amends ADR-0118.
2. `SheetRecord`, `SheetView` and `SheetDimension` in `Engine.Contracts`, and the sheets of the Document.
3. The events `sheet.created`, `sheet.changed` and `sheet.removed`, and the lists of the sheets in the result of a handler and
   in a step of undo.
4. The commands `CreateSheet`, `RenameSheet` and `RemoveSheet`, and the codes `E-SHEET-NOT-FOUND`, `E-SHEET-INVALID` and
   `E-SHEET-ID-IN-USE`.
5. R-0067 closed by its exit.

## Scope (out)

- The commands of the views (P10.2) and of the dimensions (P10.3).
- A read of a sheet with the lines of its views (P10.2).
- A sheet in the window.

## Acceptance criteria

- [x] ADR-0153 exists, and ADR-0118 shows the amendment.
- [x] A created sheet has the identifier of its command, its name, its paper, its orientation and its projection, and the
      event `sheet.created` carries the record.
- [x] The default paper is A4 landscape with ISO first angle; a wrong paper, an empty name and a sheet that is not live each
      give their code.
- [x] A rename and a remove give their events, and an undo and a redo of each give the record before and the record after.
- [x] An open of the document file gives the same sheet.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p10-1-sheet-contract) · v0.374

## Method

Mechanical: the record, the commands and the schema of the events, in the form of the images and the references.
Judgement:  one record for each sheet with its lists, and an edit that gives a new record.
Weakest:    the lists of the views and the dimensions have no command until P10.2 and P10.3.

## Progress

- 2026-11-15: opened after the answers of the owner of 2026-11-14 to Q1 to Q8, branch p10-1-sheet-contract. ADR-0153 and
  TASK-0389 are free. A first test of E-SHEET-ID-IN-USE got the result of the cache of the session for the same identifier,
  so the test fills the cache with 1024 refused Redo commands first, as the test of an assembly reference does. Five
  injections each failed a named test: no event sheet.created, no check of the paper, an undo that gives the record after,
  no check of an identifier in use, and no sample of RemoveSheet in the test of the file. Each file came back byte for byte.
  The guard of the lists of the sheets in the bus has no test, as the guard of the images has none.
- 2026-11-15: run 43718200348 passed on the work commit. Closed at v0.374.
