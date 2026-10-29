---
id: 0037
title: The rebuild of an extrude after an edit of its sketch
status: Accepted
topic: Contracts, geometry, sketch
date: 2026-10-29
supersedes: []
superseded-by: []
amends: ['0012', '0021', '0028', '0031', '0036']
amended-by: []
affects:
  - Engine.Contracts/BodyRecord.cs
  - Engine.Core/Sketch/**
  - Engine.Core/Commands/**
  - Engine.Core/Queries/**
  - Engine.Core/CommandBus.cs
  - Engine.Api.Http/Endpoints/SchemaEventsEndpoint.cs
  - Engine.Api.Http/WebSockets/WireMessage.cs
enforced-by: Engine.Tests/Sketch/RebuildTests.cs (TASK-0108 creates it)
---

# ADR-0037 — The rebuild of an extrude after an edit of its sketch

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase K0b makes the solid of an extrude follow an edit of its sketch. It completes the engine part of the
milestone "CAD on one part". The owner answered Q1 to Q6 of the codebase review of 2026-10-29 on 2026-10-29, A to
each (`docs/reviews/2026-10-29-codebase-review.md`, section "Questions for the owner"):

- Q1 A: a body keeps its handle, and a rebuild gives it a new geometry key.
- Q2 A: an edit that breaks the profile applies; the body keeps its last solid and gets a fault.
- Q3 A: an extrude that a later command consumed does not rebuild, and the edit gives a warning.
- Q4 A: the engine finds the extrudes of a sketch in the log.
- Q5 A: `body.changed` gains the geometry key and the fault.
- Q6 A: K0b is the engine and HTTP; K0c is the desktop.

Six facts constrain the design (section 5 of the review):

- **A handle lives one time.** ADR-0021 item 1, and `Engine.Core/CommandBus.cs:332-336` refuses a handle that the
  bus created before. So a rebuild cannot consume a body and create it again under the same handle.
- **An undo calls no backend.** ADR-0031: "The backend never changes and never frees a body." A step records body
  records and no geometry. So a solid that changes in place breaks an undo: the record goes back, and the solid does
  not.
- **No link from a body to its sketch.** A body record holds a handle, a kind, a name and a visibility. The link is
  the `ExtrudeCommand` in the log, whose command identifier is the handle (ADR-0012 §4).
- **The open applies each entry again.** An open refuses an entry that it does not apply
  (`Engine.Core/Persistence/DocumentFile.cs:210-214`), and ADR-0035 says that a command that the engine accepted at
  the save must not get a rejection at the open. So a rebuild never rejects an edit.
- **Anti-objective 4.** A rebuild is a projection, and "an error lives in the cache and never in the log"
  (`docs/CHARTER.md:206-210`).
- **Determinism.** The rebuild runs inside the handler of the edit, with the same solve, the same profile and the
  same keys at each replay. A key comes from integer operations only.

## Decision

1. **The body record.** `BodyRecord` gains two optional fields: `Geometry`, a guid or null, the key of the solid of
   the body in the backend, and `Fault`, a `BodyFault(Code, Message)` or null. A null `Geometry` means the handle.
   The property `GeometryKey` gives `Geometry ?? Handle.Id`. A record of v0.107 is a valid record with both fields
   null, so no saved file changes.
2. **Each read of a solid uses the key.** `Translate`, `Subtract`, `GetTessellation` and `GetBoundingBox` call the
   backend with `GeometryKey` of the record, not with the body identifier.
3. **The key of an extrude.** `Extrude` stores its solid under `GeometryKeys.Derive(commandId, bodyId)`, a
   name-based guid (RFC 9562, version 5) of the two identifiers, and never under the body identifier. So a call site
   that item 2 misses fails with `E-GEOM-NATIVE-OP`, and does not read an old solid in silence. The function uses
   SHA-1, which is integer operations only.
4. **The steps in effect.** `SketchRebuild.InEffect(log)` walks the log as `StepHistory` does: an `Undo` moves the
   last step in effect to the undone list, a `Redo` moves it back, and each other entry is a new step that clears the
   undone list. It depends only on the log, so a replay gives the same set.
5. **The rebuild (Q1 A, Q4 A).** After the solve, each sketch command that gives a changed sketch rebuilds each
   extrude of that sketch: each `ExtrudeCommand` of the log in effect whose `SketchId` is the sketch and whose body
   is live, in the order of the log. For each one:
   - The handler computes the parameters with the code of `Extrude` (`ExtrudeProfile.Parameters`), from the new
     record and the distance of the command.
   - When the parameters equal the parameters of the record before the edit and the body has no fault, nothing
     changes, and no event comes.
   - Else it calls `IExtrudeOps.Extrude` under `GeometryKeys.Derive(editCommandId, bodyId)`, and the body record gets
     the new `Geometry` and no fault.
   - The command gives each such record in `ChangedBodies`, so the step records the record before and after, and an
     undo gives back the old key with no call of the backend.
6. **A profile that breaks (Q2 A).** When the new record is not solved, when its lines are not one closed loop, or
   when the backend fails, the body keeps its `Geometry` and gets the fault `W-SKETCH-REBUILD-FAILED` with the reason.
   The edit applies, and the result gives the same code as a diagnostic. A later edit that makes the profile valid
   rebuilds the body and clears the fault.
7. **A consumed extrude (Q3 A).** An extrude in effect whose body is not live was consumed by a later command in
   effect. It does not rebuild. The edit applies, and the result gives `W-SKETCH-EXTRUDE-CONSUMED` with the body
   identifier. An extrude that an undo removed is not in effect, so it gives no warning. The chain of a consumed body
   waits for phase K0.
8. **The events (Q5 A).** `body.created` and `body.changed` gain `geometry` (a guid) and `fault` (an object with
   `code` and `message`, or null). The snapshot of a subscription gives both fields for each body. A client reads the
   mesh again when `geometry` differs from the key that it holds.
9. **The backend does not change.** `IExtrudeOps` keeps its shape. Each old solid stays in the backend, as ADR-0031
   accepts for each step.
10. **Amendments.**
    - ADR-0012 §3: the Document holds the geometry key of each body. §4: a command that makes a solid for a body
      that it does not create derives the key from its own identifier and the body identifier.
    - ADR-0021: a live handle can point at a new solid after a rebuild; a client keys a mesh by the geometry key.
    - ADR-0028 item 7: the payloads of item 8 of this record.
    - ADR-0031: the backend still never changes and never frees a solid; a body record can point at a newer solid.
    - ADR-0036 item 7: the edit of a sketch rebuilds its extrudes; the sketch is still not consumed.

## Consequences

**Good:** A person or an agent edits a sketch, and each live extrude of it follows, with the same identifier, the
same name and the same selection. Undo and redo stay a swap of records. A file of v0.105 opens; an edit after an
extrude now gives the new solid at the open. A missed call site fails loudly. A broken profile is a fact of the
body, and the edit is never refused.

**Bad:**
- `Engine.Contracts` changes: two fields of `BodyRecord` and one record, `BodyFault`.
- The backend keeps one more solid for each rebuild. ADR-0031 already accepts this growth.
- Each sketch command reads the log to find its extrudes, in time that grows with the log.
- A file of v0.105 with an edit after an extrude gives another solid at the open than it gave live. That run had no
  rebuild, so the new solid is the one that the sketch describes (ADR-0035 is the precedent).
- A consumed extrude does not follow until K0.
- The desktop draws the first mesh until K0c reads `geometry`.

**Next:** TASK-0108 applies this record. K0c gives the desktop part.

## History

- 2026-10-29 · TASK-0108 · Proposed, from the answers of the owner to Q1 to Q6 of the codebase review of 2026-10-29.
- 2026-10-29 · TASK-0108 · The owner accepted the record on 2026-10-29, with no change.
