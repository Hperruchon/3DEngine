---
id: 0093
title: Undo and redo as commands of the log
status: Done
phase: P8c
opened: 2026-10-28
depends-on: [0089, 0091, 0092]
governed-by: [0004, 0006, 0007, 0008, 0012, 0013, 0015, 0016, 0019, 0020, 0021, 0025, 0027, 0028, 0029, 0030, 0031]
writes:
  create:
    - tasks/TASK-0093-undo-and-redo.md
    - docs/adr/0031-undo-and-redo.md
    - Engine.Core/Commands/UndoCommand.cs
    - Engine.Core/Commands/UndoCommandHandler.cs
    - Engine.Core/Commands/RedoCommand.cs
    - Engine.Core/Commands/RedoCommandHandler.cs
    - Engine.Core/StepHistory.cs
    - Engine.Tests/Commands/UndoRedoTests.cs
  modify:
    - Engine.Contracts/Handlers/ICommandHandler.cs
    - Engine.Contracts/Document.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - 3DEngine/SessionBodies.cs
    - 3DEngine/BodyActions.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine/Ui/**
    - 3DEngine/Documentation/Running.md
    - Engine.Tests/Http/**
    - Engine.Tests/Persistence/**
    - 3DEngine.ImageTests/**
    - docs/adr/0006-command-execution-model.md
    - docs/adr/0021-operand-consumption.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/glossary.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - docs/register.md
    - eng/test-floor.txt
  forbid:
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - Engine.Core/Persistence/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - eng/write-set-check/**
    - eng/repo-gates/**
---

# TASK-0093 — Undo and redo as commands of the log

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase P8c, after P8a (TASK-0091) and P8f (TASK-0092), in the path to "CAD on one part". ADR-0006 and
ADR-0015 disagree on the form of an undo. On 2026-10-28 the owner answered Q1 B, Q2 A, Q3 A and Q4 A. ADR-0031
records the decision. This phase changes the public shape of `Engine.Contracts`, so the owner must accept ADR-0031
before the code.

## Goal

A person and an agent undo and redo each step of the document, and an undo survives a save, a stop and an open.

## Scope (in)

1. ADR-0031 is accepted. ADR-0006 and ADR-0021 get the amendment and a History line.
2. `HistoryMove` and `CommandHandlerResult.History` (ADR-0031 item 7).
3. The history of the steps in the command bus, and the commit of an undo and a redo (items 2 to 5).
4. The commands `Undo` and `Redo` in `HandlerCatalog`, with their encoders (item 1).
5. The event kind `body.removed` in the bus and in `/schema/events` (item 6).
6. The codes `E-CMD-NOTHING-TO-UNDO` and `E-CMD-NOTHING-TO-REDO`, registered (item 8).
7. The desktop: the buttons and the projection of `body.removed` (item 11). The owner chose A on 2026-10-28: the
   keys go to register entry R-0041.
8. Tests: each command type undone and redone, the redo list cleared by a new step, an undo after an open of the
   file, a replay on two runs, the events and their order, the two rejections, and the desktop.

## Scope (out)

- A limit to the depth of the history.
- The free of a body that no step can make live again.
- A query that gives the count of steps to undo or redo.
- A separate history for each client (Q2 B).
- The keys Ctrl+Z, Ctrl+Y and Ctrl+Shift+Z (R-0041).

## Acceptance criteria

- [x] Each new test fails before its correction, or an injection shows that it can fail.
- [x] For each command type, an undo gives the live body set before the step, and a redo gives the set after it.
- [x] A file with `Undo` and `Redo` entries opens, and a further undo gives the expected live set.
- [x] The two rejections give their codes and leave the version unchanged.
- [x] The visual check: the owner sees a subtract, an undo and a redo in the desktop.
- [x] `dotnet test` passes, the build has 0 warnings, and continuous integration is green on the three runners.

## Notes for the implementer

- The bus owns the history, as it owns the set of created handles (TASK-0058). No handler reads it.
- `RefuseWrongBodyLists` stays for a step. An undo and a redo do not pass through it, because the bus makes their
  lists.
- A reversal must not call the backend. Each body that it makes live again is still in the backend.

## Progress

- 2026-10-28: opened after TASK-0089 (v0.94). The first commit holds ADR-0031, accepted by the owner, the
  amendments of ADR-0006 and ADR-0021, and the contract: `HistoryMove` and `CommandHandlerResult.History`.
- 2026-10-28: the code (ADR-0031 items 1 to 8 and the buttons of item 11). The bus records each step and makes
  the lists of an undo and a redo; `Undo` and `Redo` are in `HandlerCatalog`; `body.removed` is in the bus, in
  `/schema/events` and in the projection of the desktop; the two codes are registered. 13 tests in
  `Engine.Tests`, 3 in `3DEngine.ImageTests`. 13 injections: 12 were caught at once; a schema with no
  `body.removed` passed until the payload test applied undos, and it now fails there. The keys of item 11 wait.
- 2026-10-28: the visual check passed, and the owner accepted it: a cut, an undo that gives back the plate and the
  cutter, and a redo whose picture is the picture of the cut, byte for byte; no message of the validation layer.
  The owner chose A for the keys (R-0041). The panel is 640 pixels high at most, so that Show all fits.

## Outcome

Status: Done · commit (the merge of the branch undo-and-redo) · v0.95

## Method

Mechanical: the two commands and their encoders, the catalog, the codes, the schema row, the projection case and
            the two buttons.
Judgement:  the bus, and no handler, makes the lists of a move, because the bus already owns the set of created
            handles and a handler cannot give a wrong reversal. An undo calls no backend, because no backend
            changes or frees a body; the Manifold backend frees a body only at its own disposal.
Weakest:    the history depends on the claim that no backend frees a body. A backend that frees a consumed body
            breaks an undo of that consumption, and only the native test of the file and the image test show it
            (`An_Undo_And_A_Redo_Continue_After_An_Open_Of_The_File`, `The_Scene_Follows_An_Undo_And_A_Redo_Of_The_Cut`).
