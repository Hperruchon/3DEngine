---
id: 0123
title: The sketch that made a body is found along the first input, and the history keeps it after the six newest steps
status: Accepted
topic: Clients, rendering
date: 2026-11-12
supersedes: []
superseded-by: []
amends: ['0104', '0117', '0119']
amended-by: []
affects:
  - 3DEngine.App/HistorySteps.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.History.cs
  - 3DEngine.ImageTests/HistoryArcTests.cs
enforced-by: 3DEngine.ImageTests/HistoryArcTests.cs
---

# ADR-0123 — The sketch that made a body is found along the first input, and the history keeps it after the six newest steps

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0104 item 1 gives the window the steps of the selected body: its feature and each feature that it reads, by the inputs,
at most six with the newest. ADR-0117 item 2 (the row "Made from") and ADR-0119 item 1 ("Edit sketch" on the ring) take the
first step of those six that opens a sketch.

The codebase review of 2026-11-12 gives finding V168 (medium). A plate from a sketch and an extrude, cut three times by
boxes, has a chain of eight steps, so the six newest do not hold the sketch, and the row "Made from", the segment
"Edit sketch" and the command go. A plate with three holes is a normal model. A second form: when the tool of a cut is an
extrude of a sketch that is newer than the sketch of the plate, the first sketch of the six is the sketch of the tool.

## Decision

1. **The sketch that made a body (amends ADR-0104 item 1, ADR-0117 item 2 and ADR-0119 item 1).** The sketch that made a
   body is found along the first input of each step from the body: an extrude reads its sketch, a cut reads the body that it
   cuts, and a move or a turn reads the body that it moves. A body whose walk ends at a step with no input, as a box, has
   no sketch that made it. The history marks the step of that sketch. When it is not one of the six newest steps, the
   history gives it after them. The arc draws the six newest steps only; the row "Made from", "Edit sketch" on the ring and
   the command "Edit sketch" take the marked step.
2. **No contract change, no engine change.** The walk reads the inputs that `GetFeatures` gives (ADR-0054).

## Consequences

**Good:** a plate keeps "Made from" and "Edit sketch" after each count of cuts, and a cut opens the sketch of the body that
it cuts, not the sketch of its tool.

**Bad:** the list of the history can hold seven steps, so a reader of the list must take the first six for the arc. A body
that a cut made from a box and an extrude, in that order, has no sketch that made it, although the tool has one; its row
"Made from" does not show.

## Alternatives considered

- **More steps on the arc.** Refused: the arc has room for six nodes (ADR-0104 item 3), and a long chain would still lose
  the sketch.
- **A second query for the sketch.** Refused: the history has each feature of the chain already, so the walk costs no read.

## History

- 2026-11-12 · TASK-0350 · The record, from finding V168 of the codebase review of 2026-11-12.
