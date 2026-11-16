---
id: 0395
title: The dimensions of a sheet, and a read of a sheet with the value of each dimension from the design
status: Active
phase: P10.3
opened: 2026-11-16
depends-on: [0393]
governed-by: [0004, 0012, 0013, 0016, 0018, 0019, 0021, 0028, 0031, 0033, 0037, 0060, 0083, 0089, 0112, 0114, 0153, 0155, 0158]
writes:
  create:
    - tasks/TASK-0395-the-dimensions-of-a-sheet.md
    - docs/adr/0158-the-dimensions-of-a-sheet.md
    - docs/proposals/2026-11-15-plan-p10-3-the-dimensions-of-a-sheet.md
    - Engine.Core/Commands/PlaceSheetDimensionCommand.cs
    - Engine.Core/Commands/PlaceSheetDimensionCommandHandler.cs
    - Engine.Core/Commands/RemoveSheetDimensionCommand.cs
    - Engine.Core/Commands/RemoveSheetDimensionCommandHandler.cs
    - Engine.Core/Drawing/SheetSources.cs
    - Engine.Tests/Drawing/SheetDimensionTests.cs
    - Engine.Tests/Http/HttpSheetScenarioTests.cs
  modify:
    - Engine.Core/Commands/SheetResults.cs
    - Engine.Core/Queries/GetSheetQuery.cs
    - Engine.Core/Queries/GetSheetQueryHandler.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - docs/diagnostics.md
    - docs/proposals/2026-11-15-answer-sheet.md
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

# TASK-0395 — The dimensions of a sheet, and a read of a sheet with the value of each dimension from the design

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row P10.3 of the roadmap: the dimensions of a sheet (ADR-0153 items 5 and 6). P10.2 (TASK-0393) gave the views of a sheet and
the read GetSheet, with the dimensions as records only. A side session wrote the plan of P10.3 with seven questions; on
2026-11-15 the owner answered them with option A (D1 to D7 of the answer sheet). On 2026-11-16 the owner ordered "yes, start
P10.3, merge when green".

## Goal

A person or an agent places, places again and removes a dimension of a value of the design in a view of a sheet, and a read
of the sheet gives the value of each dimension from the design at the time of the read, or "lost".

## Scope (in)

1. PlaceSheetDimension, with the place again of a dimension, and RemoveSheetDimension, with undo and redo, in the document
   file (ADR-0158 items 1 to 3).
2. The value, the label and the state "lost" of each dimension in GetSheet (ADR-0158 item 4).
3. The codes `E-SHEET-DIMENSION-NOT-FOUND`, `E-SHEET-DIMENSION-INVALID` and `E-SHEET-SOURCE-NOT-FOUND` (ADR-0158 item 5).
4. A test of the JSON names of a view and of a dimension over HTTP and the WebSocket.
5. The plan of P10.3 in `docs/proposals/`, with a note of what changed before the copy.

## Scope (out)

- The geometry of the dimension line on the paper and the flag "true length" (P10a or R22d).
- A client that shows a sheet.

## Acceptance criteria

- [ ] Each command, each refusal, the place again, undo, redo and the document file have a test, and each check has an
      injection that a test catches.
- [ ] A read gives the value of a constraint and of an extrude after a change of the design with no sheet command, and a
      dimension whose source is not in the design as lost.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the commands, their checks and the encoding, as for the commands of P10.2.
Judgement:  the order of the checks, the place again at the same index, and a lost extrude that no command can give now.
Weakest:    a dimension of a distance becomes lost only in a Document that the test builds by hand.

## Progress

- 2026-11-16: opened under the order of the owner of 2026-11-16, branch p103-sheet-dimensions from v0.379. Undo is linear,
  so test 3 of the plan (an undo of the Extrude) cannot keep the dimension; the test of a lost extrude gives the read a
  Document with a source that is not in it. The JSON name of the identifier of a view and of a dimension in the event
  `sheet.changed` is `id`.
  Twenty injections each failed a test, and each file came back byte for byte.
