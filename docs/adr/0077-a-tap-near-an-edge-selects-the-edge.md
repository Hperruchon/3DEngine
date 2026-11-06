---
id: 0077
title: An edge is named by its two faces, and a tap near an edge of the selected face selects it
status: Accepted
topic: Clients, selection, kernel
date: 2026-11-06
supersedes: []
superseded-by: []
amends: ['0075']
amended-by: []
affects:
  - Engine.Core/Queries/GetFacesQuery.cs
  - Engine.Core/Queries/GetFacesQueryHandler.cs
  - 3DEngine.App/BodyActions.cs
  - 3DEngine.App/FaceOverlay.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/Ui/DesktopUi.cs
  - 3DEngine.App/Ui/DesktopUi.Panel.cs
  - 3DEngine.App/Ui/DesktopUi.Overlay.cs
  - 3DEngine.App/Ui/PanelAction.cs
enforced-by: 3DEngine.ImageTests/EdgeSelectionTests.cs
---

# ADR-0077 — An edge is named by its two faces, and a tap near an edge of the selected face selects it

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The roadmap row R12 gives "the escalated selection of faces and edges". R12a (ADR-0075) gives the face: a first tap
selects the body, and a second tap on the selected body selects the face under the point. ADR-0075 left the edges to
R12b, "after a name of an edge in track K".

What exists:

- ADR-0058: the owned kernel gives a B-Rep of each body. Each `BrepEdge` has its two vertices and the faces that it
  bounds, and each face has its name, a `FaceTag` of a feature, an element and an item (ADR-0059).
- ADR-0052 names the edges of a box (`+x-z`) and of an extrude (`startEdge`, `endEdge`, `sideEdge` with an item) for a
  reference of a sketch. Those names come from the feature, so a cut that splits an edge, or a wall of a hole, has none.
- ADR-0075 item 1: `GetFaces` gives the outline of each face name, the segments of each edge with exactly one face of
  that name.
- The redesign of the owner gives values on the edges, to change a dimension; a later slice needs a selected edge.

The owner ordered R12 in the queue of 2026-11-06 (evening) under the standing order of 2026-11-05, which answers each
question with the recommended option. This record is Accepted under that order; the owner checks it on 2026-11-08.

### Q1. The name of an edge

- **A.** The pair of the names of its two faces, in a fixed order. Each B-Rep edge between two faces of different names
  belongs to that pair, so the 64 segments of the rim of a cylinder are one edge, and a cut gives the wall of a hole its
  edges with no new rule.
- **B.** The names of ADR-0052, from the feature.
- **C.** A name of its own in the kernel, kept through each operation.

A comes from the names of ADR-0059, which a rebuild keeps, so a selected edge stays through a rebuild. B has no name for
an edge that a cut makes. C needs the kernel to carry a history of each edge, which no operation keeps today.
**Recommended: A.**

### Q2. Where the edges come from

- **A.** `GetFaces` gives the edges too, from the B-Rep that it keeps for the geometry key: each pair, the count of its
  parts, its length, and its segments.
- **B.** A new query `GetEdges`.

A keeps one cache and the codes of `GetFaces`; ADR-0075 Q2 gave the same reason. **Recommended: A.**

### Q3. The gesture

- **A.** A tap on the selected face, at most 12 pixels from an edge of that face on the screen, selects that edge, the
  nearest one. A tap further from each edge selects the face under the point, as before. The key Escape goes back from
  the edge to the face, and then to the body.
- **B.** A long press selects the edge.

A keeps the order body, face, edge with one kind of tap, for a finger, a pen and a mouse (objective 9). A long press hides
a body (ADR-0029 item 4). **Recommended: A.**

## Decision

The standing order of 2026-11-05 gives A for each question.

1. **The name (Q1 A).** An edge is the pair of the names of its two faces, the lower one first in the order of the
   identifier of the feature, then the element, then the item, as text with no culture. Each B-Rep edge with two faces of
   different names belongs to the pair of those names. A B-Rep edge between two faces of one name is inside the name and
   is no edge.
2. **The query (Q2 A).** `GetFaces` gives `edges`: one entry for each pair, in the order of the first B-Rep edge of the
   pair, with `first` and `second`, the count of its parts (the chains of its segments that share a vertex), its length,
   and its segments. The length is the sum of the lengths of the segments, each from its exact square, a double before
   its root, as the area of ADR-0058. The schema version of the query stays 1: the field is new, and no field changes.
3. **The gesture (Q3 A).** `BodyActions` keeps the selected edge by its pair, or none, only while a face is selected. A
   select intent, with a face selected, first asks the host for the edge of that face nearest the point of the tap on
   the screen, at most 12 pixels away, before the pick, so that a tap on each side of an edge on the silhouette selects
   it (finding V117, TASK-0252); with an edge, it selects that edge and keeps the body and the face. Else the tap
   selects the face under the point, with no edge. `CancelIntent` with `ByKey` goes back from the edge to the face, and
   then to the body. Each change of the face clears the edge. The edge is a state of the client (ADR-0007).
4. **The panel and the mark.** The overlay draws the segments of the selected edge over the outline of the face, wider.
   The controls of the selected body give one more line: "Edge", the two elements of the pair, and the length.
5. **The amendment.** This record amends ADR-0075 item 2: a tap on the selected face near one of its edges selects the
   edge.
6. **An edge of one face or of more than two faces.** Where two solids of a body touch along a line, a B-Rep edge has
   more than two faces. Such an edge belongs to each pair of different names of its faces. A B-Rep edge of one face is no
   edge. The outline of a face name (ADR-0075 item 2) holds each B-Rep edge between a face of the name and a face of
   another name, so each segment of an outline is a segment of an edge of that name (finding E165, TASK-0251).

## Consequences

**Good:** each edge of each body has a name, also after a cut, and the client reads its mark from the query that it reads
for the faces.

**Bad:** a pair of faces that meet in two places is one edge with two parts; a tap selects both parts. Each entry of
`GetFaces` is larger by the edges of the body. At an edge of more than two faces, some pairs bound no common solid,
for example two faces in one plane on the two sides of the line (item 6).

**Next:** the values on the edges of the redesign; an edge as a reference of a sketch, which needs the name in
`Engine.Contracts` and so a question to the owner.

## History

- 2026-11-06 · TASK-0245 · The record, Accepted under the standing order, with the query, the gesture and the panel.
- 2026-11-06 · TASK-0245 · Shipped at v0.230.
- 2026-11-06 · TASK-0251 · Item 6: an edge of one face or of more than two faces, and the outline that agrees with the
  edges (finding E165 of the codebase review of 2026-11-06 (e), the recommended option under the standing order). The
  affects field names the four files of the panel and of the mark that TASK-0245 changed (finding T164).
- 2026-11-06 · TASK-0252 · Item 3: the host asks for the edge before the pick (finding V117 of the codebase review of
  2026-11-06 (e)).
