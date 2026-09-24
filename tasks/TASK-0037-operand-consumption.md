---
id: 0037
title: An operation consumes its operands, and the Document holds the live bodies only
status: Ready
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

- [ ] After `CreateBox` A, `CreateBox` B, `Translate` B and `Subtract`, `Document.Bodies` holds one
      body, and its handle is the `CommandId` of the `Subtract`.
- [ ] The events of the `Subtract` come in the order `command.applied`, `body.created`,
      `body.consumed`, `body.consumed`.
- [ ] A command that names a consumed body is rejected with `E-GEOM-BODY-NOT-FOUND`, and the backend
      receives no call.
- [ ] A replay of the log gives the same live set.
- [ ] The reset snapshot lists the live bodies only.
- [ ] `/schema/events` lists `body.consumed` with its payload field `bodyId`.
- [ ] A subtract of a body from itself is rejected, or it consumes the body one time. The test
      records which, and the Outcome block gives the reason.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The contract gate.** This task changes `Engine.Contracts`. Scope item 7 gives the ADR change that
  the pipeline job "Contract-touched-needs-ADR" wants.
- **The dispatch gate.** No command is added, so `DispatchSurfaceGateTests` does not change.
- **Existing tests** assert the old body counts. Correct each one to ADR-0021. Do not delete one.
