---
id: 0028
title: The name and the visibility of a body
status: Accepted
topic: Contracts, geometry
date: 2026-10-28
supersedes: []
superseded-by: []
amends: ['0012', '0021']
amended-by: []
affects:
  - Engine.Contracts/BodyRecord.cs
  - Engine.Contracts/Handlers/ICommandHandler.cs
  - Engine.Core/CommandBus.cs
  - Engine.Core/Commands/**
  - Engine.Core/Queries/**
enforced-by: Engine.Tests/Commands/BodyNameAndVisibilityTests.cs (TASK-0085 creates it)
---

# ADR-0028 — The name and the visibility of a body

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Phase R8a gives each body a name and a visibility in the engine. Phase R8b shows them in the desktop. The
owner decided the defaults R8-Q1 to R8-Q8 on 2026-10-27 (decision sheet of that date, part 2). A body has no
state today other than its handle and its kind (ADR-0012 §3), and a commit gives three kinds of event:
`command.applied`, `body.created` and `body.consumed` (ADR-0021 item 5).

## Decision

1. **The record.** `BodyRecord` gains `Name` (a string, or null when the body has no name) and `Visible`
   (true by default). The engine records both; the client draws what they say.
2. **Two commands and one query.** `RenameBody { bodyId, name }` sets the name; an empty name clears it.
   `SetBodyVisibility { bodyId, visible }` sets the visibility. `GetBodyInfo { bodyId }` gives the
   identifier, the kind, the name and the visibility. A command that sets the value that the body has is
   applied, and gives its event.
3. **The rules of a name.** A name has 1 to 128 UTF-16 units. A control character is refused. The engine
   does not trim, normalize or compare with the rules of a culture. Two bodies can have one name. A refused
   name gives the new code `E-VAL-BODY-NAME-INVALID`. An unknown or consumed body gives
   `E-GEOM-BODY-NOT-FOUND`.
4. **A hidden body is a body.** Each query and each command works on a hidden body as on a visible one.
   Visibility is not a filter of the engine.
5. **Inheritance.** The result of `Translate` takes the name and the visibility of its source. The result of
   `Subtract` takes those of its minuend. The handler sets them in the record that it creates.
6. **The result of a handler.** `CommandHandlerResult` gains `ChangedBodies`, a list of `BodyRecord`, empty
   by default. A handler gives each live body that it changes one time. The bus replaces the record in
   `Document.Bodies` in the commit section.
7. **The new event kind `body.changed`.** Its payload is `bodyId`, `name` and `visible`. The events of one
   commit come in this order: `command.applied`, each `body.created`, each `body.consumed`, each
   `body.changed`. `body.created` and each body of the `subscription.reset` snapshot also give `name` and
   `visible`. `/schema/events` lists the kind and its fields.
8. **Replay.** The log does not change form: a rename is a command in the log, so a replay gives the same
   names and visibilities.
9. **The exception to the rule of a new command.** `CLAUDE.md` says that a new command changes no file
   outside `Engine.Core/Commands/` and `Engine.Core/Hosting/HandlerCatalog.cs` (ADR-0016). A change of a
   body is a new kind of output of a commit, so this phase also changes `BodyRecord`,
   `CommandHandlerResult`, the bus, the snapshot and the schema of the events, one time. A later command
   that changes a body uses `ChangedBodies` and changes no central file. The rule of
   `DispatchSurfaceGateTests` does not change: no host names a command, and a test injects a host that
   names `RenameBody` and watches the gate fail.

This record amends ADR-0012 §3 and §6 (the record of a body and the snapshot) and ADR-0021 item 5 (the order
of the events).

## Consequences

**Good:** The engine holds the name and the visibility, so an agent and the desktop see the same state, and
a log comparison of touch and mouse (anti-objective 7) has commands to compare. A later change of a body
reuses `ChangedBodies`.

**Bad:** A public shape of `Engine.Contracts` changes: two fields of `BodyRecord`, one list of
`CommandHandlerResult`, one event kind, and two fields of `body.created` and of the snapshot. A client that
reads the payloads strictly must accept the new fields. The rule of a new command has one recorded exception.

**Next:** TASK-0085 (R8a) applies this decision. R8b shows the name and the visibility in the desktop.

## History

- 2026-10-28 · TASK-0085 · The record is written with the defaults R8-Q1 to R8-Q8 of the owner of 2026-10-27,
  and the owner accepted the contract on 2026-10-28.
- 2026-10-28 · TASK-0085 · The code applies the record. A field of an event payload with `Required: false` can
  be null, as the name of a body with no name (item 7).
