---
id: 0192
title: The snapshot of the events and CheckInterference give the live bodies in the order of the log
status: Accepted
topic: Contracts, events, assembly
date: 2026-11-28
supersedes: []
superseded-by: []
amends: ['0012', '0182', '0189']
amended-by: []
affects:
  - Engine.Core/LogOrder.cs
  - Engine.Api.Http/WebSockets/WireMessage.cs
  - Engine.Core/Queries/CheckInterferenceQueryHandler.cs
  - Engine.Core/Queries/GetBodiesQueryHandler.cs
  - Engine.Tests/Queries/BodyLogOrderTests.cs
enforced-by: Engine.Tests/Queries/BodyLogOrderTests.cs
notes: Phase K25, TASK-0478. Register entry R-0080; the owner answered "task fixes" on 2026-11-26 (TASK-0472).
---

# ADR-0192 — The snapshot of the events and CheckInterference give the live bodies in the order of the log

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

`Document.Bodies` gives the values of a .NET `Dictionary`. After a command removes a record, the bus adds the new bodies of the
command before it removes the consumed ones, so the place of a consumed record is free when the command ends, and the next new
record takes it. The snapshot of `/events` (ADR-0012 section 6) and `CheckInterference` with no `bodyIds` (ADR-0182 item 2) took
that order, which a person or an agent cannot predict from the log. A replay of one log with one runtime gives the same order;
a new version of .NET can change it. `GetSheets` and `GetBodies` (ADR-0189 item 3) give the order of the log, and ADR-0189 said
that the snapshot has another order. Register entry R-0080 held the risk, and on 2026-11-26 the owner answered "task fixes"
(row K25). The order of the codebase review of 2026-11-28 puts K25 before R34 and R35 (Q1 A, TASK-0476).

## Decision

1. **One order.** `Engine.Core/LogOrder.cs` gives the live bodies in the order of the log: the place in `Document.Log` of the
   command that made each one, as ADR-0189 item 3 says. A rename, a change of the visibility and a `PlaceBody` do not move a
   body; an `Undo` of a consume gives the record back at its place. A body whose handle is not the identifier of a command, which
   no command makes today, comes after the others in the order of its identifier.
2. **The snapshot (amends ADR-0012 section 6).** The member `bodies` of the snapshot of `/events` gives the live bodies in that
   order.
3. **The check (amends ADR-0182 item 2).** With no `bodyIds`, `CheckInterference` checks each live body in that order, so the
   pairs come in that order.
4. **The lists (amends ADR-0189 item 3).** `GetBodies` takes the same function, so the snapshot, the check and the list give one
   order. The sentence of ADR-0189 item 3 that the snapshot has another order no longer holds.
5. **The shape.** No field is new or removed, and no type changes. Only the order of a list changes. Nothing of the order goes
   into the log, so no replay changes, and the function calls no function of the determinism rules.

## Alternatives considered

- **An ordered collection in `Document`.** `Engine.Contracts/Document.cs` would change for an order that only three readers
  need, and each command that adds or removes a body would keep it.
- **The order of the identifier.** A client chooses the identifier of a command, so the order would not be the order of the
  work.
- **The owner accepts the order of the collection.** The owner answered "task fixes".

## Consequences

**Good:** a person and an agent read the bodies in the order of the work in each answer, and the order is the same on each
version of .NET.

**Bad:** each read walks the log once, so its cost grows with the length of the log; a snapshot comes at each subscription and a
check is a large read already. `Document.Sheets` keeps the order of its collection; no answer lists the sheets but `GetSheets`,
which gives the order of the log. The window takes the bodies of its first read in the order of the collection; the export to STL
orders them by identifier.

## History

- 2026-11-28 · TASK-0478 · The record, Accepted: the answer "task fixes" of the owner of 2026-11-26 and Q1 A of the review of
  2026-11-28 by the standing order of 2026-11-28. Amends ADR-0012 section 6, ADR-0182 item 2 and ADR-0189 item 3.
