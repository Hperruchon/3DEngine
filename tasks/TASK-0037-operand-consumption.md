---
id: 0037
title: An operation consumes its operands, and the Document holds the live bodies only
status: Done
phase: P0.16
opened: 2026-09-25
depends-on: [0035]
governed-by: [0004, 0005, 0006, 0008, 0010, 0011, 0012, 0013, 0016, 0019, 0020, 0021]
writes:
  create:
    - tasks/TASK-0037-operand-consumption.md
    - Engine.Tests/Commands/OperandConsumptionTests.cs
  modify:
    - Engine.Contracts/Document.cs
    - Engine.Contracts/Handlers/ICommandHandler.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/Commands/SubtractCommandHandler.cs
    - Engine.Core/Commands/TranslateCommandHandler.cs
    - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
    - Engine.Api.Http/WebSockets/WireMessage.cs
    - Engine.Tests/**
    - docs/adr/0021-operand-consumption.md
    - docs/glossary.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - docs/register.md
  forbid:
    - Engine.Contracts/Geometry/**
    - Engine.Contracts/Schema/**
    - Engine.Core/Queries/**
    - Engine.Core/Hosting/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Core/**
    - BlazorApp/**
    - nuget/**
---

# TASK-0037 — An operation consumes its operands, and the Document holds the live bodies only

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0021 gives the decision and each reason. Today each operation adds a body and keeps its
operands, so after the first demonstration the Document holds four bodies and the person sees one
solid. Roadmap phase R6 then needed a view filter in the host, which is a decision about design
truth in a client.

## Goal

After the first demonstration, `Document.Bodies` holds one body: the cut solid.

## Scope (in)

1. `CommandHandlerResult` gains `ConsumedBodies` (ADR-0021, item 2). A handler that gives no list
   consumes nothing.
2. `Subtract` consumes the minuend and the subtrahend. `Translate` consumes its source body
   (ADR-0021, item 3).
3. Each handler checks each operand against `Document.Bodies` before it calls the backend
   (ADR-0021, item 4).
4. The commit removes each consumed body and emits `body.consumed` after each `body.created`
   (ADR-0021, items 5 and 6).
5. `/schema/events` lists `body.consumed`.
6. Roadmap phase R6 loses "a view filter for the latest result".
7. The field `enforced-by` of ADR-0021 loses the text "(TASK-0037 creates it)".
8. A glossary term: live body.

## Scope (out)

- No release of the geometry of a consumed body in the backend. ADR-0021, section "Consequences",
  gives the reason.
- No undo and no history mode.
- No change to `CreateBox`, which consumes nothing.

## Acceptance criteria

- [x] After `CreateBox` A, `CreateBox` B, `Translate` B and `Subtract`, `Document.Bodies` holds one
      body, and its handle is the `CommandId` of the `Subtract`.
- [x] The events of the `Subtract` come in the order `command.applied`, `body.created`,
      `body.consumed`, `body.consumed`.
- [x] A command that names a consumed body is rejected with `E-GEOM-BODY-NOT-FOUND`, and the backend
      receives no call.
- [x] A replay of the log gives the same live set.
- [x] The reset snapshot lists the live bodies only.
- [ ] `/schema/events` lists `body.consumed` with its payload field `bodyId`. The kind is listed. The
      payload field is not: no entry of the endpoint has payload fields, and ADR-0021 item 6 asks for
      the kind only. Register entry R-0037 asks the owner.
- [x] A subtract of a body from itself is rejected, or it consumes the body one time. The test
      records which, and the Outcome block gives the reason.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The
      owner permitted the merge after a green run on the three runners, and the ledger of the merge
      records the run.

## Notes for the implementer

- **The contract gate.** This task changes `Engine.Contracts`. Scope item 7 gives the ADR change that
  the pipeline job "Contract-touched-needs-ADR" wants.
- **The dispatch gate.** No command is added, so `DispatchSurfaceGateTests` does not change.
- **Existing tests** assert the old body counts. Correct each one to ADR-0021. Do not delete one.

## Outcome

Status: Done · v0.49 · the commit that carries this block.

A subtract of a body from itself is rejected with `E-GEOM-INVALID-PARAM`, before the backend, and the
body stays live. The reason: ADR-0021 item 2 permits each handle one time in the consumed list, and a
body minus itself has no use. A rejection tells the client; a quiet consumption of one body would
hide the error. The code exists, so no new code is registered.

Scope item 6 was already done before this task: phase R6 names the live body set and no filter. The
roadmap gets the line for P0.16 and the new sum of the six phases that remain.

## Method

**Mechanical.** The tests first. The contract field came first and alone, so the first run showed the
behaviour and not a compile error: 9 of 9 new tests failed. The Document kept each operand, and no
`body.consumed` event existed. Then the commit of the bus and the two handlers. Two earlier native
tests asserted the old state, and each one now asserts ADR-0021: the replay round trip expects one
live body, and the smoke test queries the moved box before the subtract that consumes it. No test was
deleted. A new schema test failed with the line of `body.consumed` removed and passed with it.

**Judgement.** `ConsumedBodies` is an `init` property with an empty default and not a new positional
parameter, so each existing construction of the result still compiles. The bus refuses a wrong
consumed list with an exception and not a rejection, because a wrong list is a defect in a handler and
not an error of the client; it throws before the commit, so the Document does not change. The engine
tests use a recording backend, so they run with no native library and can count each backend call.
The write set gained `docs/register.md` in the commit that closes the task, because `CLAUDE.md` puts
each open question in the register, and the write-set gate refused the entry R-0037 without it.

**Weakest.** `Document.Bodies` is the value collection of a dictionary. After a removal, a new body can
take the freed slot, so the collection is no longer in creation order. The order is the same for the
same commands, so a replay gives the same order, but a client must not read the order as history.
The bus throws for a wrong consumed list after the handler called the backend, so the backend can
hold an orphan body, as in finding E21.

## Progress

- 2026-10-08: the task is open, after TASK-0051 in the order of the owner of 2026-10-08.
- 2026-10-08: closed. 283 tests pass, and the clean build gives zero warnings. R-0037 holds the
  question about the payload fields of `/schema/events`.
