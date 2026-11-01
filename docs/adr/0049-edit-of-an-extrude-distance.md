---
id: 0049
title: The edit of the distance of an extrude
status: Accepted
topic: Geometry, feature graph, clients
date: 2026-11-01
supersedes: []
superseded-by: []
amends: ['0028', '0037', '0039']
amended-by: []
affects:
  - Engine.Core/Commands/SetExtrudeDistanceCommand.cs
  - Engine.Core/Commands/SetExtrudeDistanceCommandHandler.cs
  - Engine.Core/Sketch/SketchRebuild.cs
  - Engine.Core/Queries/GetBodyInfoQuery.cs
  - Engine.Core/Queries/GetBodyInfoQueryHandler.cs
enforced-by: Engine.Tests/Sketch/ExtrudeDistanceTests.cs (TASK-0163 creates it)
---

# ADR-0049 — The edit of the distance of an extrude

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Up to v0.161 an edit of a sketch is the only edit after the fact: the extrudes of the sketch and their chains rebuild
(ADR-0037, ADR-0039). The distance of an extrude is fixed at the `Extrude` command, so a person who wants another
height must undo the extrude and each later step. Objective 11 of `docs/CHARTER.md` is editable parametric CAD.

The codebase review of 2026-11-01 asked the owner two questions (`docs/reviews/2026-11-01-codebase-review.md`, section
"Questions for the owner"). On 2026-11-01 the owner answered A to each one:

- **Q1 A.** The next product phase is K0f: a command changes the distance of a live extrude, and the rebuild of the
  extrude and of each descendant runs as K0d runs for an edit of a sketch. The desktop gives the pad of the distance.
- **Q2 A.** One command, `SetExtrudeDistance`, records the identifier of the extrude and the new distance. The rebuild
  reads the last distance in effect for each extrude.

Four facts constrain the design:

- **The open applies each entry again.** ADR-0035 says that a command that the engine accepted at the save must not
  get a rejection at the open. So the rebuild of this command never rejects it, as ADR-0037 item 6 says for a sketch.
- **An undo calls no backend** (ADR-0031). The command gives the changed body records, and the step records them.
- **The codes of a sketch edit name a sketch command.** `W-SKETCH-REBUILD-FAILED` and `E-SKETCH-ID-IN-USE` say "a
  sketch command", and a code never changes its meaning. This command is not a sketch command.
- **The feature graph is a projection of the log** (ADR-0039 item 1), so the distance in effect is a projection too,
  with no new field of the Document.

## Decision

1. **The command (Q2 A).** `SetExtrudeDistance { extrudeId: guid, distance: number }`, schema version 1, gives the
   output `extrudeId`. The identifier is the command identifier of an `Extrude`, which is also the handle of its body
   (ADR-0012 §4). The command records no coordinate and no solid.
2. **The refusals.**
   - A live command with the identifier of a command that the log holds gives `E-FEATURE-ID-IN-USE`, with the rule of
     `SketchResults.InUse` for a command that creates no item (findings E95 and E100).
   - A distance that is not finite, or that is zero, gives `E-GEOM-INVALID-PARAM`, as `Extrude` does (ADR-0036 item 3).
   - An identifier that is not an `Extrude` in effect gives `E-FEATURE-NOT-FOUND`. An extrude that an undo removed is
     not in effect. An extrude that a later `Translate` or `Subtract` consumed is in effect, and the command takes it.
3. **The distance in effect (Q2 A).** The distance of an extrude is the distance of the last `SetExtrudeDistance` in
   effect for it, in the order of the log, or else the distance of the `Extrude`. Each rebuild reads it: the rebuild of
   an edit of a sketch (ADR-0037 item 5, ADR-0039 item 3) and the rebuild of this command. So a later edit of the
   sketch keeps the new distance.
4. **The rebuild (Q1 A).** The rebuild is the rebuild of ADR-0037 item 5 and ADR-0039 items 2 to 6, with one changed
   extrude. The sketch is its live record.
   - A live extrude extrudes the live sketch with the new distance under `GeometryKeys.Derive(edit, body)`.
   - A consumed extrude is a seed of its chain, and the live end runs again in log order.
   - When the parameters before and after are the same and the body has no fault, nothing changes and no event comes.
5. **A failure.** When the sketch is not solved, when its profile is not valid, when a step of the chain does not
   resolve, or when the backend fails, the body keeps its last solid and gets the fault `W-FEATURE-REBUILD-FAILED` with
   the reason. The command applies, and the result gives the same code with `bodyId`. A later edit whose rebuild
   succeeds clears the fault. A consumer that the feature graph does not know gives the same code, because no body
   follows.
6. **Undo and redo.** The step records the body records before and after, so an undo is a swap of records and calls no
   backend (ADR-0031). An undone `SetExtrudeDistance` is not in effect, so item 3 gives the distance before it.
7. **The query.** `GetBodyInfo` gains the field `distance`: the distance in effect of the extrude whose handle is the
   body, or null for a body that no `Extrude` in effect creates. A client reads the distance there, and does not read
   the log.
8. **The desktop (Q1 A).** A selected body of the kind "Extrude" gets the button "Set distance". The pad starts with the
   distance of `GetBodyInfo`, and its apply sends `SetExtrudeDistance`. The body keeps its handle, its name and the
   selection, and `body.changed` gives the new geometry key, so the desktop reads the new mesh (K0c). The end of a
   chain has no button in this phase, because the desktop does not know its extrude. An agent sends the command with
   the identifier of the extrude.
9. **Determinism.** The command records a number. The rebuild uses `ExtrudeProfile` (`+ - * / sqrt` and comparisons
   only), the keys are name-based guids of SHA-1, and each walk follows the order of the log. So a replay and an open
   give the same keys and the same solids on each platform.
10. **Saved files.** No field of an existing command changes. A file with this command opens in an engine of v0.162 or
    later. An earlier engine does not know the command, so its open refuses the file at that entry.
11. **The codes.** The new subsystem token `FEATURE` holds the codes of an edit of a feature: `E-FEATURE-ID-IN-USE`,
    `E-FEATURE-NOT-FOUND` and `W-FEATURE-REBUILD-FAILED`.
12. **Amendments.**
    - ADR-0028 item 2: `GetBodyInfo` gives the distance of item 7.
    - ADR-0037 item 5: the rebuild of an extrude of an edited sketch uses the distance in effect of item 3, not the
      distance of the `Extrude` command.
    - ADR-0039 item 3: an extrude upstream of an end uses the distance in effect of item 3.

## Consequences

**Good:** A person or an agent changes the height of a part after the fact, and each move and cut of it follows. Undo,
redo, replay and the document file stay as they are. The edit uses the code of the rebuild of K0d, so it adds no
second path to a solid.

**Bad:**
- The backend keeps one more solid for each edit, as ADR-0037 item 9 accepts.
- Each rebuild reads the log for the distance in effect, in time that grows with the log.
- The desktop cannot edit the extrude of a moved or cut body in this phase.
- A file with the command does not open in an earlier engine.

**Next:** TASK-0163 applies this record. A later slice of K0 can give the edit of the other numbers of a feature, for
example the offsets of a `Translate`, with the same form.

## History

- 2026-11-01 · TASK-0163 · Accepted under Q1 A and Q2 A of the owner of 2026-11-01. The query field, the codes and the
  button of the desktop follow from those answers and the four facts of the context.
