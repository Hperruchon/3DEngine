---
id: 0104
title: The history arc shows the steps that made the selected body
status: Accepted
topic: Clients, rendering
date: 2026-11-09
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.History.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/HistorySteps.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.ImageTests/HistoryArcTests.cs
enforced-by: 3DEngine.ImageTests/HistoryArcTests.cs
---

# ADR-0104 — The history arc shows the steps that made the selected body

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-08, after v0.297, the owner chose four slices of the render "mix" in this order: the rows of a selected body at
the left (R16p, v0.298), the history arc, the agent arc and the values on the edges of a sketch; each slice merges after green
CI, with a picture. In the render "Mix 1", an arc at the lower left of the ring of the selected part holds one node for each
step that made the part, the newest at the top ("Bracket", then "Plate on XY"), with the caption "HISTORY". In "Mix 4" a new
step appears on the arc after a change.

The engine gives the steps already: `GetFeatures` (ADR-0054) lists the features in effect in the order of the log, with the
inputs of each one (the sketch of an extrude, the operands of a move and of a cut, the datum of a sketch). The identifier of a
feature is the identifier of its command and of the body that it makes. A change of a value, a name or a point changes a
feature and is not a row (ADR-0054 item 2). The design brief names a rollback in the history as an engine need that does not
exist.

### Q1. Which steps the arc shows

- **A.** The steps of the selected body: its feature and each feature that it reads, back to its sketches and datums, in the
  order of the log, the newest at the top of the arc; at most six, the newest first.
- **B.** Each feature of the Document.

A is the render and stays short; B gives a long arc with steps of other bodies. **Recommended: A.**

### Q2. What a tap on a node does

- **A.** A node of a sketch opens the edit of that sketch, as "Edit sketch" does (ADR-0085); a node of a live body that is
  not consumed selects that body; another node does nothing.
- **B.** Nothing in this slice.
- **C.** A rollback to that step, which needs the engine (the design brief, "Rollback in the history").

C changes the engine and waits for the owner. **Recommended: A.**

### Q3. Where the arc shows

- **A.** On a landscape display, while a body is selected and no sketch is active: an arc with the centre of the ring of the
  body, at the lower left, on the band of the ring between its guide circles, with the caption "HISTORY" in spaced capitals.
  Each node is a circle of at least 44 pixels for a tap, with its text beside it.
- **B.** On a portrait display too.

The brief puts no history arc on a portrait tablet. **Recommended: A.**

## Decision

The owner chose the slice and its merge after green CI; the recommendations answer Q1 to Q3.

1. **The steps (Q1 A).** The host reads `GetFeatures` again only when the session committed events or the selection changed,
   and counts the events through a subscription, so the frame takes no lock of the session. It gives the
   window the steps of the selected body: its feature and the features that it reads, by the inputs, in the order of the
   log, at most six with the newest. A step has the identifier of its feature, its kind and its text: the name of a body, or
   its kind; the name of a sketch, or "Sketch", with "on" and the plane (XY, XZ, YZ, or "a face"); and the kind of another
   step. A step that is failed or unresolved says so, as the rows do.
2. **The tap (Q2 A).** A press and a release on one node of a sketch give the edit of that sketch; on one node of a live body
   that is not consumed, the selection of that body. The nodes are a part of the panels for a tap.
3. **The place (Q3 A).** The arc has the centre of the ring of the selected body and the radius of the middle of its band,
   from the lower left (135°) down to the bottom, where the tools of a body are not; the newest node is at the upper end. The
   caption "HISTORY" is at the left of the lower end. The arc does not show while a pad is open.
4. **No contract change.** Nothing enters the log; the arc is a view of the query.

## Consequences

**Good:** the person sees how the selected part was made, and opens its sketch from it, as in the render.

**Bad:** a change of a value adds no node, because the engine records no step for it; a rollback waits for the engine.

## History

- 2026-11-09 · TASK-0325 · The record, with the slice (the order of the owner of 2026-11-08).
- 2026-11-09 · TASK-0325 · Shipped at v0.308.
