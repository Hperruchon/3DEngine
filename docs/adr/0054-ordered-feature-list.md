---
id: 0054
title: The ordered feature list, a fold of the log
status: Accepted
topic: Feature graph, runtime
date: 2026-11-03
supersedes: []
superseded-by: []
amends: ['0039']
amended-by: []
affects:
  - Engine.Core/Features/**
  - Engine.Core/Queries/GetFeaturesQuery.cs
  - Engine.Core/Queries/GetFeaturesQueryHandler.cs
enforced-by: Engine.Tests/Features/FeatureListTests.cs (TASK-0173 creates it)
---

# ADR-0054 — The ordered feature list, a fold of the log

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Phase P8d of the roadmap gives the three layers of a document: the log, an ordered list of features, and a cache of
the regeneration. Since P8a the log exists. The second layer does not: each rebuild, each check of an identifier and
each query of a distance walks the whole log again (`SketchRebuild.InEffect`, `FeatureGraph.Of`, `Log.Any`). A probe of
TASK-0173 measured the cost: at 10,000 commands, a block of 250 boxes and moves of a sketch point took 497 ms, and the
time grows with the log. An agent also has no query that gives the features of a document in their order.

Charter rule 4 states that a feature in a rebuild is correct, failed, suppressed or has an unresolved reference, and
that an error lives in the cache. The owner gave the order "start P8d" on 2026-11-03 and answered four questions on the
same day, each with the recommended answer:

- **The first slice.** P8d1 gives the ordered feature list and the query `GetFeatures`, and the rebuilds read the list.
  The results of a rebuild do not change. P8d2 gives the cache and the general regeneration.
- **The features.** Each command in effect that creates an object is a feature. An edit changes its feature and is not
  a row.
- **The states.** Four: correct, failed, consumed and unresolved. No suppress now. The faults stay in the records of the
  Document, and their move to the cache waits for P8d2.
- **The form.** A fold of the log: the list applies each new entry one time, and a test compares it with a walk of the
  whole log.

## Decision

1. **The fold.** `FeatureList` in `Engine.Core/Features/` applies each entry of the log one time, in the order of the
   log, as `StepHistory` does: an Undo moves the last step in effect to the undone list and removes what the step gave;
   a Redo applies that step again; each other entry is a new step and clears the undone list. `FeatureList.Of(document)`
   advances the list of a Document to the end of its log, inside the serial boundary of the session. The list holds no
   state of the Document, and no code changes the Document through it.
2. **The features (Q of the features).** The rows are the steps in effect of the kinds `CreateBox`, `Extrude`,
   `Translate`, `Subtract`, `CreateDatum`, `CreateSketch` and `PlaceImage`, in the order of the log. The identifier of
   a feature is the identifier of its command, and also of the object that it creates. Each other command, for example a
   move of a sketch point, a new distance, a name or a calibration, changes a feature and is not a row.
3. **The states (Q of the states).** A body that a feature in effect consumes is `consumed`, with that feature. A live
   body with a fault is `failed`. A datum with a fault, and a sketch with a reference that has a fault, are
   `unresolved`. Each other feature is `correct`. A failed or an unresolved feature gives its fault (code and message).
   The faults stay in `BodyRecord`, `DatumRecord` and `SketchReference`, so `Engine.Contracts` does not change. No command
   suppresses a feature.
4. **The readers (amends ADR-0039 item 1).** The engine reads the steps in effect, the creator, the consumer and the
   place of a handle, the distance in effect of an extrude and the identifiers of the log from the list. `FeatureGraph`
   is a view of the list. A rebuild still visits the steps in effect in their order; P8d2 replaces that visit with the
   regeneration. `SketchRebuild.InEffect` stays as the walk of the whole log that the tests compare with the fold.
5. **The query `GetFeatures`.** It takes no parameter and gives `features`: for each row, `featureId`, `kind` (the name
   of the command), `inputs`, `consumedBy`, `state` and `fault`. `inputs` are the identifiers that the feature reads, in
   the order of its fields: the sketch of an extrude, the operands of a `Translate` and of a `Subtract`, the feature of a
   datum, the datum of a sketch and the feature of each of its references, and the datum of an image. An origin plane
   has its fixed identifier (ADR-0033 item 1). The query gives no error.
6. **Determinism.** The fold reads the log in one order, and each order that it gives comes from a list, never from a
   hash set. A replay and an open give the same list. Nothing enters the log, and no command, event or saved file
   changes.
7. **The codes.** No new code.
8. **Out of this record.** The cache of the regeneration, a regeneration of each feature after a changed feature, the
   place of a fault, a suppressed feature, and a panel of the features in the desktop.

## Consequences

- A command does not walk the whole log to find the steps in effect, a creator, a consumer, a distance or an
  identifier. At 10,000 commands the block of the probe takes 46 ms.
- An agent reads the features of a document in their order, with their inputs and their states.
- The list is state outside the Document. It is correct only when each entry of the log applies to it one time; the
  tests compare it with the walk after each command.

## History

- 2026-11-03 · TASK-0173 · Accepted with the four answers of the owner of 2026-11-03.
