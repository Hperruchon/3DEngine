---
id: 0093
title: Undo and redo as commands of the log
status: Active
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
7. The desktop: the buttons, the keys and the projection of `body.removed` (item 11).
8. Tests: each command type undone and redone, the redo list cleared by a new step, an undo after an open of the
   file, a replay on two runs, the events and their order, the two rejections, and the desktop.

## Scope (out)

- A limit to the depth of the history.
- The free of a body that no step can make live again.
- A query that gives the count of steps to undo or redo.
- A separate history for each client (Q2 B).

## Acceptance criteria

- [ ] Each new test fails before its correction, or an injection shows that it can fail.
- [ ] For each command type, an undo gives the live body set before the step, and a redo gives the set after it.
- [ ] A file with `Undo` and `Redo` entries opens, and a further undo gives the expected live set.
- [ ] The two rejections give their codes and leave the version unchanged.
- [ ] The visual check: the owner sees a subtract, an undo and a redo in the desktop.
- [ ] `dotnet test` passes, the build has 0 warnings, and continuous integration is green on the three runners.

## Notes for the implementer

- The bus owns the history, as it owns the set of created handles (TASK-0058). No handler reads it.
- `RefuseWrongBodyLists` stays for a step. An undo and a redo do not pass through it, because the bus makes their
  lists.
- A reversal must not call the backend. Each body that it makes live again is still in the backend.

## Progress

- 2026-10-28: opened after TASK-0089 (v0.94). The first commit holds ADR-0031, accepted by the owner, the
  amendments of ADR-0006 and ADR-0021, and the contract: `HistoryMove` and `CommandHandlerResult.History`.
