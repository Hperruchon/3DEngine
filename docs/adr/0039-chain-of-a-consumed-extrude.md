---
id: 0039
title: The rebuild of the chain of a consumed extrude
status: Accepted
topic: Geometry, sketch
date: 2026-10-30
supersedes: []
superseded-by: []
amends: ['0037']
amended-by: []
affects:
  - Engine.Core/Sketch/**
enforced-by: Engine.Tests/Sketch/ChainRebuildTests.cs (TASK-0116 creates it)
---

# ADR-0039 — The rebuild of the chain of a consumed extrude

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0037 rebuilds each live extrude of a sketch after an edit of the sketch. When a `Translate` or a `Subtract`
consumed the extrude, the rebuild stops with `W-SKETCH-EXTRUDE-CONSUMED` (ADR-0037 item 7), so the part that the
person sees does not follow the sketch. Roadmap phase K0 is a feature graph over Manifold. The owner chose its first
slice on 2026-10-30:

- **KQ1 A.** The first slice is the chain of a consumed extrude: after a rebuild of an extrude, each `Translate` and
  `Subtract` in effect that descends from it runs again in log order under new keys, and the live result keeps its
  handle. No new command.
- **KQ2 A.** The feature graph is a projection of the log, computed in `Engine.Core`, with no new field of the
  Document and no change of the document file.
- **KQ3 A.** A reference is the body handle and the command identifier in the log. It resolves at the rebuild. An
  unresolved or failed step gives the fault `W-SKETCH-REBUILD-FAILED` on the result body, and the edit applies
  (anti-objective 4, ADR-0037 item 6).
- **KQ4 A.** These answers are the acceptance of this record, because it holds exactly them and changes no public
  shape of `Engine.Contracts`.

The codebase review of 2026-10-30 (sections 5 and 6) gave the research, and the owner answered its questions on
2026-10-30:

- **Q1 A.** The rebuild evaluates the full upstream of each end from the live sketches, so an operand of a `Subtract`
  that comes from another sketch follows its own sketch too.
- **Q2 A.** `W-SKETCH-REBUILD-FAILED` gets the wider meaning "the rebuild of an extrude of the sketch, or of a body
  of its chain, failed". The owner chose A over the recommended B (a new code), so this change of the meaning of a
  code is a decision of the owner (`CLAUDE.md`, "Diagnostic codes").
- **Q3 A.** `W-SKETCH-EXTRUDE-CONSUMED` stays registered, and the engine gives it only for a consumer that the graph
  does not know. Today the graph knows each consumer.

## Decision

1. **The graph.** `FeatureGraph.Of(log)` in `Engine.Core/Sketch/` is a pure function of the steps in effect
   (`SketchRebuild.InEffect`). It gives the command that creates each handle and the command that consumes it. A
   `CreateBox`, an `Extrude`, a `Translate` and a `Subtract` create the body whose handle is their command
   identifier; a `Translate` consumes its body, and a `Subtract` consumes its two operands (ADR-0021 item 3). One
   handle has at most one consumer in effect (ADR-0021 item 1).
2. **The seeds and the ends.** A seed is an extrude in effect of the edited sketch whose body is not live. Its end is
   the live body that the walk from the seed through each consumer reaches. The rebuild takes each end one time, in
   the log order of the command that creates it.
3. **The evaluation (Q1 A).** For each end, the rebuild evaluates the steps upstream of it, in log order and one time
   each for one edit:
   - an extrude of the edited sketch extrudes the solved record of the edit under `GeometryKeys.Derive(edit, body)`;
   - an extrude of another live sketch extrudes the record of that sketch under the same form of key;
   - a `Translate` or a `Subtract` with a changed operand runs again on the backend under `Derive(edit, body)`;
   - a step with no changed operand, for example a box, keeps its key and calls no backend.
4. **An unchanged edit.** An end changes nothing when each extrude of the edited sketch upstream of it has the same
   parameters before and after the edit (`ExtrudeProfile.Same`) and the end has no fault.
5. **A failure (KQ3 A, Q2 A).** A step that cannot resolve (no creator in effect, a creator that the graph does not
   know, a sketch that is not live, a broken profile, an operation that the backend does not give, or an exception
   of the backend, for example an empty result of a `Subtract`) gives the end the fault `W-SKETCH-REBUILD-FAILED`
   with the step and the reason. The end keeps its key, the result gives the same code with `bodyId`, and the edit
   applies. A fault that does not change gives no new record.
6. **The commit.** `ChangedBodies` holds the live ends only, with the new key and no fault, or with the fault. The
   bodies between have a solid in the backend only. An undo stays a swap of records and calls no backend (ADR-0031).
7. **The consumed warning (Q3 A).** A seed whose chain meets a consumer that the graph does not know gives
   `W-SKETCH-EXTRUDE-CONSUMED`, as ADR-0037 item 7 gave it for each consumed extrude.
8. **Determinism.** The graph and the evaluation walk the log in one order, and each key is a name-based guid of
   SHA-1, so a replay and an open give the same keys and the same solids on each platform. Nothing iterates a hash
   set for an order.
9. **Saved files.** No command and no field changes. A file with an edit after a `Translate` of an extrude opens with
   the rebuilt end, as ADR-0035 item 10 opens a sketch with its solved positions.

This record amends ADR-0037 item 7: a consumed extrude now follows through its chain.

## Consequences

**Good:** The part follows its sketch after a move and a cut. The feature graph needs no new state, so undo, redo,
replay and the document file stay as they are.

**Bad:**
- An edit runs each step upstream of each end again, so a long chain costs one backend call for each step.
- Each old solid stays in the backend (ADR-0037 item 9), and a chain adds one solid for each step and edit.
- An operand of another sketch runs again at each edit of the first sketch (Q1 A).

**Next:** TASK-0116 applies this record. A later slice of K0 can give the graph to a query.

## History

- 2026-10-30 · TASK-0116 · Accepted under KQ4 A, from KQ1 to KQ4 A and Q1 to Q3 A of the owner of 2026-10-30.
