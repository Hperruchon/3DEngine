---
id: 0189
title: The queries GetSheets and GetBodies give the live sheets and the live bodies of the Document in the order of the log
status: Amended
topic: Surface, agent
date: 2026-11-26
supersedes: []
superseded-by: []
amends: []
amended-by: ['0192']
affects:
  - Engine.Core/Queries/GetSheetsQuery.cs
  - Engine.Core/Queries/GetSheetsQueryHandler.cs
  - Engine.Core/Queries/GetBodiesQuery.cs
  - Engine.Core/Queries/GetBodiesQueryHandler.cs
  - Engine.Tests/Queries/DocumentListQueryTests.cs
enforced-by: Engine.Tests/Queries/DocumentListQueryTests.cs
notes: P21. The owner approved the two queries with the answer Q5 A of 2026-11-25 (TASK-0464); this record gives their shape.
---

# ADR-0189 — The queries GetSheets and GetBodies give the live sheets and the live bodies of the Document in the order of the log

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row P17 (TASK-0439) wrote the guide for an agent, `docs/guides/agent-loop.md`, and found two gaps (register entry R-0075):

- No query and no field of the snapshot gives the sheets of the Document. An agent learns the identifier of a sheet only from
  the event `sheet.created` while it listens. For an earlier sheet it must ask the person.
- No query gives the live bodies with their names. The snapshot of `/events` gives them, but it needs a WebSocket.
  `GetFeatures` gives the identifiers with no name, and `GetBodyInfo` gives one name for each request.

On 2026-11-25 the owner answered Q5 A (TASK-0464): add two queries, the sheets of the Document and the live bodies with their
names, with an ADR and tested examples of the guide. That answer is the approval of the owner for the change of the public
surface. On 2026-11-26 the owner ordered "Start P21". The owner did not decide the fields and the order of each result; this
record decides them.

Three facts of the code shape this record:

- The results of the queries are records of `Engine.Core/Queries`, not of `Engine.Contracts`: the measure queries
  (ADR-0180), `GetSheet` (ADR-0155 item 5) and `GetConnectors` (ADR-0183 item 1) put them there. The row P21 and R-0075 call
  each query "a new shape of `Engine.Contracts`". The new shape is a shape of the public surface, `/queries` and
  `/schema/queries`; no file of `Engine.Contracts` changes.
- `Document.Bodies` and `Document.Sheets` are the values of a .NET `Dictionary`. When a command removes a record, the next
  record that the Document adds takes the free place in the collection. In the test of this record, a box A, a box B, a
  cylinder C, a `Translate` of B and an extrude give the collection A, extrude, C, moved B. The snapshot of the events
  (ADR-0012 section 6) and `CheckInterference` with no `bodyIds` (ADR-0182 item 2) use that order. It is the same on each
  replay of one log, but a person or an agent cannot predict it from the log, and a change of the .NET runtime can change it.
- The identifier of a body is the identifier of the command that made it (ADR-0012 section 4), and `CreateSheet` gives a sheet
  the identifier of its command. The log holds each command once.

## Decision

1. **Two queries.** `GetSheets` and `GetBodies`, schema version 1, in `Engine.Core/Queries`, registered in `HandlerCatalog`.
   The route `/queries` of HTTP, the command `engine query` and `DocumentSession.Query` reach them through the registry, with
   no new route (ADR-0016). Each takes no parameter. Each reads the records of the Document only: it needs no capability of
   the backend, gives no error, writes nothing to the log and changes no version. No diagnostic code is new. No file of
   `Engine.Contracts` changes: the records `SheetsInfo`, `SheetEntry` and `BodiesInfo` are in `Engine.Core/Queries`, and
   `GetBodies` takes the record `BodyInfo` of `GetBodyInfo` (ADR-0028 item 2).
2. **A sheet.** The result of `GetSheets` is `sheets`. Each item gives `sheetId`, `name`, `paper`, `landscape`, `thirdAngle`
   and `viewIds`, the identifier of each view in the order of the record. These fields come from the record of the sheet with
   no read of the design. `sheetId` is the parameter of each command of a sheet and of `GetSheet`; `viewIds` gives the
   parameter `viewId` of the commands of a view. A removed sheet is not live, so it is not in the list. `GetSheet` gives the
   size of the paper, the lines of each view, the dimensions and the title block of one sheet.
3. **The order.** Each list is in the order of the log: the place in `Document.Log` of the command that made each live sheet
   or body. A rename, a change of a view and a change of the visibility do not move an item. An undo of a remove or of a
   consume gives the record back at its place. This order is not the order of the snapshot of the events (see "Context").
4. **A live body.** A live body is a body of `Document.Bodies`. A body that a command consumed is not live (ADR-0021 item 1),
   and a body whose command an undo reversed is not live. A hidden body and a body with a fault are live, as in the snapshot.
   No command deletes a body.
5. **A body.** The result of `GetBodies` is `bodies`. Each item is the answer of `GetBodyInfo` for that body: `bodyId`,
   `kind`, `name`, `visible`, and `distance`, the distance in effect of an extrude or null (ADR-0049 item 7). The item gives
   less than the snapshot: no geometry key, because it is a key of the backend (ADR-0037 item 1) that no query takes, and no
   fault, because `GetFeatures` gives the state `failed` and the fault of each feature (ADR-0054 item 3). So an agent reads
   one shape for one body and for each body.
6. **Determinism.** The two queries copy fields of the records and compare identifiers. They write nothing to the log, so no
   replay changes. They call no function of the determinism rules.

## Alternatives considered

- **The order of the Document, as the snapshot gives it.** Refused: it is the order of a .NET collection after its removals
  (see "Context"). The log order is a property of the Document, and an agent can predict it.
- **A field `sheets` in the snapshot of `/events`.** Refused: it changes an existing shape (ADR-0012 section 6), and an agent
  then needs a WebSocket for the sheets.
- **The fields of the snapshot in `GetBodies`.** Refused for the geometry key and the fault (item 5).
- **The lines of each view in `GetSheets`.** Refused: a read of a sheet computes the lines of each view with no cache
  (ADR-0155 item 7), so a list of each sheet with its lines costs the sum of those reads. `GetSheet` gives them for one sheet.
- **The size of the paper in `GetSheets`.** Refused: the paper and the orientation give it (ISO 216), and `GetSheet` gives it.
- **A parameter that filters the list, for example the visible bodies only.** Refused: no requirement asks for it, and a
  later version of the query can add an optional parameter.

## Consequences

**Good:** an agent gets each sheet and each body with its name from one request on `/queries`, with no WebSocket and no event.
The guide for an agent shows both queries, and its test sends them to a host. R-0075 closes.

**Bad:** the surface has two orders for one set of bodies: the log order of `GetBodies`, and the order of the collection of
the snapshot and of `CheckInterference` with no `bodyIds`. The guide says that the two can differ. Each query walks the whole
log, so its cost grows with the log, as the cost of `GetFeatures` does.

**Next:** the order of the snapshot and of `CheckInterference` with no `bodyIds` stays the order of the collection. A later
change of those shapes can take the log order of item 3; it needs its own ADR, because it changes an existing shape.

## History

- 2026-11-26 · TASK-0471 · The record: the queries `GetSheets` and `GetBodies`, their fields, and the order of the log.
- 2026-11-28 · TASK-0478 · ADR-0192 amends item 3: the snapshot of the events and `CheckInterference` take the same order of the log as the two lists.
