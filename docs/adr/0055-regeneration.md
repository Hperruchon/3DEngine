---
id: 0055
title: The regeneration of the features after an edit
status: Accepted
topic: Feature graph, geometry, runtime
date: 2026-11-03
supersedes: []
superseded-by: []
amends: ['0037', '0039', '0054']
amended-by: []
affects:
  - Engine.Core/Features/Regeneration.cs
  - Engine.Core/Sketch/SketchRebuild.cs
  - Engine.Core/Sketch/DatumResolver.cs
  - docs/CHARTER.md
enforced-by: Engine.Tests/Features/RegenerationTests.cs (TASK-0174 creates it)
---

# ADR-0055 — The regeneration of the features after an edit

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Phase P8d gives the three layers of a document: the log, the ordered feature list (ADR-0054, P8d1) and the cache of
the regeneration (P8d2). Before P8d2, three paths rebuilt the features after an edit: `SketchRebuild.Run` for each live
extrude, its class `Chain` for each consumed extrude (ADR-0039), and `DatumResolver.Rebuild` for each datum and each
reference of a sketch (ADR-0051, ADR-0052). The paths gave correct results, but each had its own order, its own skip
rule and its own form of fault. A later edit of a box, a `Translate` or a `Subtract` had no place to go.

Charter anti-objective 4 states that a failed feature stays in the feature list, that the features after it continue
to evaluate, and that an error lives in the cache and never in the log. The faults are in the records of the Document
(`BodyRecord`, `DatumRecord`, `SketchReference`), and the log holds none.

The owner gave the order "go ahead with P8d2" on 2026-11-03 and answered four questions on the same day, each with the
recommended answer:

- **The scope.** One regeneration pass replaces the three paths. Each current test passes with no change.
- **The reuse.** A step whose inputs do not change keeps its solid, as before. No state outside the Document is added.
- **The place of a fault.** The faults stay in the records. The records of the Document are a projection of the log, so
  they are the cache of anti-objective 4, and one sentence of the charter says so. `Engine.Contracts` does not change.
- **The suppressed state.** No suppress now.

## Decision

1. **The regeneration.** `Regeneration` in `Engine.Core/Features/` evaluates the steps in effect of the feature list
   again after an edit, inside the handler of the edit and before the commit, so a replay gives the same records under
   the same keys (ADR-0037 item 4). It makes two walks of the steps in effect, in the order of the log:
   - **The planes.** Each datum and each reference of a sketch resolves again from the state after the edit
     (`DatumResolver.Rebuild`, ADR-0051 item 6, ADR-0052 item 8). This walk reads the parameters of the features only,
     never a solid.
   - **The solids.** Each live body whose chain holds an extrude that the edit targets is evaluated again.

   The planes come first because a reference later in the log can change a sketch that an earlier extrude reads, so the
   order of the log is not the order of the inputs. `SketchRebuild.Rebuild` (an edit of a sketch) and
   `SketchRebuild.RebuildDistance` (an edit of a distance) give the inputs of their edit to the regeneration and do
   nothing else.
2. **The ends (amends ADR-0037 item 5 and ADR-0039 item 2).** A live extrude that the edit targets is the end of its own
   chain, and a consumed extrude has the live body at the end of its chain of consumers. The regeneration takes each
   end one time, in the log order of the command that creates the end. The changed records and their `body.changed`
   events come in that order. Before, each live extrude came before each end of a chain.
3. **The reuse (Q of the reuse).** An end with no fault, whose chain holds no extrude with new parameters
   (`ExtrudeProfile.Same`), changes nothing and calls no backend. In the chain of an end that changes, a `Translate` or
   a `Subtract` whose operands did not change and a box keep their keys. The record of a consumed body is not in the
   Document, so each extrude upstream of an end that changes extrudes again (ADR-0039 item 3). No cache keeps a solid
   across edits; a later change can add one at a low cost.
4. **The faults (Q of the place of a fault).** The faults stay in `BodyRecord`, `DatumRecord` and `SketchReference`.
   Anti-objective 4 of the charter gets the sentence: "The records of the Document are a projection of the log, so a
   fault in a record lives in the cache." A failed end keeps its last solid, and each later feature continues to
   evaluate (ADR-0037 item 6, ADR-0039 item 5). The fault of a live extrude now has the form of the fault of a chain,
   "The body '…' keeps its last solid: …". Its code and its data do not change.
5. **No suppress (Q of the suppressed state).** No command suppresses a feature, and the four states of ADR-0054 item 3
   do not change.
6. **A later edit.** An edit of a box, a `Translate` or a `Subtract` adds a case to the test of a changed step and to
   the evaluation of a step in `Regeneration`, and names the features that it targets. No such edit exists now.
7. **Determinism.** The order of each walk comes from the feature list, never from a hash set. Each new solid has the
   key `GeometryKeys.Derive(edit, handle)`. Nothing enters the log, and no command, event kind or saved file changes.
8. **The codes.** No new code.
9. **Out of this record.** A cache of solids across edits, a suppressed feature, an edit of a box, a `Translate` or a
   `Subtract`, and a panel of the features in the desktop.

## Consequences

- One class holds the order, the skip rule and the form of a fault of each rebuild.
- An agent reads the same form of fault for a live extrude and for the end of a chain.
- The ends come in one order, so a client that reads the `body.changed` events of an edit reads them in the order of
  the log.
- The regeneration still extrudes each extrude upstream of a changed end again, because the Document holds no record of
  a consumed body.

## History

- 2026-11-03 · TASK-0174 · Accepted with the four answers of the owner of 2026-11-03.
