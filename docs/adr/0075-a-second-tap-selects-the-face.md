---
id: 0075
title: A second tap on the selected body selects the face under the point
status: Proposed
topic: Clients, selection
date: 2026-11-06
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - Engine.Core/Queries/GetFacesQuery.cs
  - Engine.Core/Queries/GetFacesQueryHandler.cs
enforced-by: Engine.Tests/Features/GetFacesTests.cs
---

# ADR-0075 — A second tap on the selected body selects the face under the point

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The roadmap row R12 gives "the escalated selection of faces and edges", after K4, because it needs a stable identity
of each face and each edge (ADR-0041 item 5; the owner, 2026-11-02). No other text on disk defines the escalated
selection.

What exists:

- ADR-0029 item 1: the desktop selects one body or none, by a ray against the triangles of each visible body.
- ADR-0053: the query `GetFaceAt` gives the reference of the face under a hit point and its normal; the button Face of
  the window of the sketch arms the next tap, and that tap makes a sketch on the face.
- ADR-0058 (K4b): the query `GetFaces` gives each face name of a body: the reference, the parts, the holes, the area, a
  point and the normal. The B-Rep of the owned kernel holds the loops of each face and the faces of each edge.
- The tessellation gives no face for a triangle, and no query gave the outline of a face before this record.
- No type and no query names an edge. An edge needs a name first, which is a decision of track K.
- The redesign of the owner, still work in progress, has a glow of the edges and values on the edges.

The owner ordered the fallback queue under the standing order of 2026-11-05, which answers each question with the
recommended option. This record stays Proposed until the change of its slice, R12a, is complete.

### Q1. The gesture

- **A.** A first tap selects the body (ADR-0029). A second tap on the selected body selects the face under the point. A
  tap on another face of the same body selects that face. A tap on another body selects that body, and a tap on no body
  clears the selection. The key Escape, or the row of the body in the panel, goes back to the body.
- **B.** A filter Body or Face in the panel: in the mode Face, each tap selects a face.
- **C.** A tap always selects the face, and the panel selects the body.

A keeps ADR-0029 for the first tap, needs no mode, and is the same for a finger, a pen and a mouse (objective 9). B adds
a mode that a person must see and change. C changes each test of the selection. **Recommended: A.**

### Q2. The mark of a selected face

- **A.** `GetFaces` gives the outline of each face name: each edge between a face of the name and a face of another name.
  The overlay draws the outline of the selected face in the colour of the selection.
- **B.** A new query gives the outline of one face.
- **C.** A new query gives the triangles of one face, and the pipeline of the highlight draws them.

A keeps the cache and the codes of `GetFaces` (a new query needs new codes for the same faults, because a code keeps its
meaning), works for a curved face, and agrees with a glow of the edges. B adds a query and its codes. C needs a map of
the triangles of the tessellation to the faces. **Recommended: A.** A query is a read: it does not enter the log, and a
replay does not depend on it.

### Q3. What a selected face gives

- **A.** The panel shows the name of the face, its area and its normal, and a button Sketch, which makes the sketch on the
  face as the armed tap of ADR-0053 item 6 does.
- **B.** The panel shows the face, with no button.

**Recommended: A.** The edges come in a later slice, R12b, after a name of an edge in track K.

## Decision

The standing order of 2026-11-05 gives A for each question. The items take effect when this record is Accepted.

1. **The outline (Q2 A).** Each entry of `GetFaces` has `outline`: one segment, a start and an end, for each edge of the
   B-Rep with exactly one face of the name, in the order of the edges of the B-Rep. An edge between two facets of a
   curve, or between two faces of one name, is inside the name. The schema version of the query stays 1: the field is
   new, and no field changes.
2. **The gesture (Q1 A).** To be written with the change of the desktop.
3. **The panel (Q3 A).** To be written with the change of the desktop.

## Consequences

**Good:** a client reads the mark of a face from the query that it reads for the names, with no new code.

**Bad:** each entry of `GetFaces` is larger, by one segment for each edge of its outline.

**Next:** the gesture and the panel of R12a; the edges in R12b, after a name of an edge.

## History

- 2026-11-06 · TASK-0236 · The record, Proposed, with item 1 and its tests.
