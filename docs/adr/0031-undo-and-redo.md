---
id: 0031
title: Undo and redo as commands of the log
status: Amended
topic: Contracts, runtime
date: 2026-10-28
supersedes: []
superseded-by: []
amends: ['0006', '0021']
amended-by: ['0032']
affects:
  - Engine.Contracts/Handlers/ICommandHandler.cs
  - Engine.Core/CommandBus.cs
  - Engine.Core/Commands/**
  - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
  - 3DEngine/SessionBodies.cs
enforced-by: Engine.Tests/Commands/UndoRedoTests.cs (TASK-0093 creates it)
---

# ADR-0031 — Undo and redo as commands of the log

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase P8c gives undo and redo. Two decisions disagree:

- ADR-0006, section "Open challenges", plans an undo that cuts the end of the log and replays it.
- ADR-0015 makes the log the document file. Its consequences say that an undo appends a command, so that nothing
  rewrites the file. The decision sheet of 2026-10-27, KD4 B, says the same.

A cut of the file breaks the rule of ADR-0015 item 4: each applied command is in the file before its result. A cut
also loses each redo at a restart. An inverse command for each command type costs more: the inverse of a `Subtract`
must bring back its two operands, so its command must hold their geometry, and a tessellation must not go into the
Document (ADR-0007).

On 2026-10-28 the owner answered four questions:

- Q1 B: `Undo` and `Redo` are commands that the log holds. They name no geometry.
- Q2 A: one history for the document. An agent and a person undo the same history.
- Q3 A: a new command after an undo clears the redo list.
- Q4 A: a new event kind `body.removed`.

One fact makes the work small. The backend never changes and never frees a body. Each operation writes a new body
under a new handle, and the source body stays in the backend (ADR-0012 §4, `ITransformOps`, ADR-0021 item 3). An
undo therefore needs no call of the backend and no replay. It changes only the live body set of the Document.

## Decision

1. **Two commands.** `Undo` and `Redo`, schema version 1, have no parameters. The output `commandId` (guid) gives
   the command that the undo or the redo reverses or applies again. The log and the document file hold them as each
   other command (ADR-0015). Each adds one to the document version (ADR-0020), so that a client with an old version
   gets `E-CMD-VERSION-STALE`. An agent sends them through `POST /commands`, as a person does in the desktop
   (ADR-0002).
2. **A step.** Each applied command that is not `Undo` or `Redo` is one step. The Document has one history (Q2).
   `Undo` reverses the last step that is not undone. `Redo` applies again the last step that an `Undo` reversed. A
   new step clears the redo list, so those undone steps never come back (Q3).
3. **The record of a step.** At the commit of a step, the command bus records its effect: each body record that it
   created, each body record that it consumed, and each body record that it changed, before and after. The bus
   keeps the history in memory. The document file does not hold it. An open rebuilds it, because the open applies
   each entry through the bus again (ADR-0015 item 6).
4. **An undo.** The commit of an `Undo` removes each body that the step created, makes each body that the step
   consumed live again with its record before the step, and gives each body that the step changed its record
   before the step. A `Redo` gives the same changes and the same event kinds as the first commit of the step.
5. **The events.** The events of one commit come in this order: `command.applied`, each `body.created`, each
   `body.consumed`, each `body.removed`, then each `body.changed`. A body that an undo makes live again gives
   `body.created` with its full record. A subscriber that draws between two events therefore never sees fewer
   bodies than the result has.
6. **The new event kind `body.removed`.** Its payload is `bodyId`. The cause of the event is the `Undo` that
   removed the body. `/schema/events` lists the kind. `body.consumed` keeps its meaning: an operation used the
   body.
7. **The contract.** `CommandHandlerResult` gains `History`, of the new enumeration `HistoryMove` with the values
   `None`, `Undo` and `Redo`. The default is `None`. The handlers of `Undo` and `Redo` give `Undo` or `Redo` and
   no list of bodies. The bus computes the changes from its history, so no handler can give a wrong reversal. The
   bus throws when a result gives a move and a list of bodies. That is a defect of the handler.
8. **Nothing to reverse.** An `Undo` with no step to reverse gives the new code `E-CMD-NOTHING-TO-UNDO`. A `Redo`
   with no undone step gives the new code `E-CMD-NOTHING-TO-REDO`. A rejection does not go into the log and does
   not change the version.
9. **This record amends ADR-0021 items 1 and 5.** A consumed body becomes live again only by an `Undo` of the
   step that consumed it. A removed body becomes live again only by a `Redo` of the step that created it. No
   other command makes a body live again. Item 5 of ADR-0021 gets the order of item 5 above.
10. **This record amends ADR-0006.** The open challenge "undo as a cut of the log and a replay" closes. An undo
    appends a command.
11. **The desktop.** The panel gets the buttons Undo and Redo, for touch and for the mouse. A rejection shows in
    the status line of the panel. The projection of the desktop treats `body.removed` as `body.consumed`: the body
    leaves the scene and the selection. The keys Ctrl+Z, Ctrl+Y and Ctrl+Shift+Z are not part of this record: a
    key of the desktop is an intent, and each intent needs a touch gesture (ADR-0026 items 2 and 5). Register
    entry R-0041 holds them.

## Consequences

**Good:** The file stays append-only, so ADR-0015 item 4 holds and a crash during an undo loses no work. An undo
survives a restart: an open rebuilds the history from the file. An undo costs the size of one step, with no replay
and no call of the backend. No command type needs an inverse, so a new command type gets undo with no work. A
replay gives the same history on each platform, because the history depends only on the log.

**Bad:**
- The public shape of `Engine.Contracts` changes: one new enumeration, one new property of `CommandHandlerResult`,
  and one new event kind that each strict subscriber must accept.
- The backend keeps each body that an undo removes, and nothing frees it. The leak already exists for a consumed
  body. It grows by one body for each undone step that a new step clears.
- `SetBodyVisibility` is a step, so an undo reverses a hide. Some CAD programs do not undo a change of the view. The
  visibility is in the Document (ADR-0028), so it is a step here.
- One shared history means that a person can undo the step of an agent. Q2 accepts this.
- The history is in memory and has no limit. A step holds some body records only, so the cost is small.

**Next:** TASK-0093 implements this record. Register entry R-0041 holds the keys of item 11. A limit to the depth of the history and the free of an unreachable body
are not planned. A register entry records them when a measure shows a need.

## History

- 2026-10-28 · TASK-0093 · Proposed, and accepted by the owner on the same day.
- 2026-10-28 · TASK-0093 · The owner chose A: item 11 gives the buttons only, and R-0041 holds the keys.
