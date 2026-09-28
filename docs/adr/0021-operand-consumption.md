---
id: 0021
title: Operand consumption and the live body set
status: Accepted
topic: Contracts, geometry, observability
date: 2026-09-25
supersedes: []
superseded-by: []
amends: ['0012']
amended-by: []
affects:
  - Engine.Contracts/Document.cs
  - Engine.Contracts/Handlers/ICommandHandler.cs
  - Engine.Core/CommandBus.cs
  - Engine.Core/Commands/**
  - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
  - Engine.Api.Http/WebSockets/WireMessage.cs
enforced-by: Engine.Tests/Commands/OperandConsumptionTests.cs (TASK-0037 creates it)
---

# ADR-0021 — Operand consumption and the live body set

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0012 §3 gives the Document a body collection with one operation, `AddBody`. ADR-0012 §5 gives one
event kind for bodies, `body.created`. No code removes a body. The handler result has only the list
`CreatedBodies` (`Engine.Contracts/Handlers/ICommandHandler.cs:43-47`).

Each operation therefore adds a body and keeps its operands:

- `Translate` of body A gives a new body B, and A stays (`TranslateCommandHandler.cs:70,95`).
- `Subtract` of B from A gives a new body C, and A and B stay (`SubtractCommandHandler.cs:73-99`).

After the first demonstration of objective 6, the Document holds four bodies, and the person sees
one solid. Roadmap phase R6 plans "a view filter for the latest result" in the host. That filter
decides which bodies are the model, which is a decision about design truth in a client. Anti-objective
1 forbids a second source of truth, and anti-objective 2 forbids business logic in a client.

On 2026-09-25 the owner decided that an edit consumes its operands in the engine and not in the host.
`docs/reviews/2026-09-23-architecture-challenge.md`, section "Decisions of the owner, 2026-09-25",
item 8, gives the question and the answer.

The change adds a field to the handler result and a new event kind. `CLAUDE.md`, section "Stop and
ask", gives both to the owner. This record holds the decision.

## Decision

1. **A live body set.** `Document.Bodies` holds the live bodies only. A body is live from the command
   that creates it to the command that consumes it. A consumed body never becomes live again.
2. **A handler names what it consumes.** `CommandHandlerResult` gains `ConsumedBodies`, a list of
   `BodyHandle`. The list is empty by default. A handler must consume only a live body, and it gives
   each handle one time.
3. **The operations.** `Subtract` consumes the minuend and the subtrahend. `Translate` consumes its
   source body. `CreateBox` consumes nothing.
4. **A command on a consumed body fails.** A handler checks each operand against `Document.Bodies`
   before it calls the backend. An operand that is not live gives `E-GEOM-BODY-NOT-FOUND`, which
   exists. No new diagnostic code.
5. **The commit.** The bus removes each consumed body from `Document.Bodies` in the commit section,
   with the other changes of the commit. The events of one commit come in this order:
   `command.applied`, then each `body.created`, then each `body.consumed`. A subscriber that draws
   between two events therefore never sees fewer bodies than the result has.
6. **The new event kind `body.consumed`.** Its payload is `bodyId`. The cause of the event is the
   command that consumed the body. `/schema/events` lists the kind.
7. **The snapshot.** The reset snapshot of ADR-0010, extended by ADR-0012, lists the live bodies
   only.
8. **Replay.** The log does not change. A replay runs the same handlers, and they consume the same
   bodies, therefore the live set after a replay is the same.

## Consequences

**Good:** The engine decides which bodies are the model. Each host draws the live set and needs no
filter. A reopened document holds the live bodies only. The render state can key its objects by
`BodyHandle`, because a handle is either live or gone.

**Bad:** The contracts grow by one field and one event kind. The backend keeps the geometry of each
consumed body until the session ends, so a long session uses more memory. A later record can add a
release. A person who wants each intermediate solid visible as a history must wait for a history
mode, which no task plans.

**Next:** TASK-0037 implements the decision. The projection from events to render state (roadmap
phase R5) reads `body.created` and `body.consumed`. Register entry R-0024 holds the open question
of the reference direction between the render projection and `Engine.Contracts` (ADR-0009 §2).
