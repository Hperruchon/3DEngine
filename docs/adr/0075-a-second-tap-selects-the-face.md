---
id: 0075
title: A second tap on the selected body selects the face under the point
status: Accepted
topic: Clients, selection
date: 2026-11-06
supersedes: []
superseded-by: []
amends: ['0029']
amended-by: []
affects:
  - Engine.Core/Queries/GetFacesQuery.cs
  - Engine.Core/Queries/GetFacesQueryHandler.cs
  - 3DEngine.App/BodyActions.cs
  - 3DEngine.App/FaceOverlay.cs
  - 3DEngine.Core/Input/IntentMachine.cs
  - 3DEngine.App/FaceArm.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.App/Ui/DesktopUi.Panel.cs
  - 3DEngine.App/Ui/DesktopUi.Overlay.cs
  - 3DEngine.App/Ui/PanelAction.cs
enforced-by: 3DEngine.ImageTests/FaceSelectionTests.cs
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
recommended option. This record is Accepted under that order; the owner checks it on 2026-11-08.

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

The standing order of 2026-11-05 gives A for each question.

1. **The outline (Q2 A).** Each entry of `GetFaces` has `outline`: one segment, a start and an end, for each edge of the
   B-Rep with exactly one face of the name, in the order of the edges of the B-Rep. An edge between two facets of a
   curve, or between two faces of one name, is inside the name. The schema version of the query stays 1: the field is
   new, and no field changes.
2. **The gesture (Q1 A).** `BodyActions` keeps the selected face of the selected body by its name, or none. A select
   intent on the selected body asks `GetFaceAt` with the point and the normal of the hit, and selects that face; a
   point that `GetFaceAt` does not name, as a curved side, gives the body with no face. A select intent on another body
   or on no body selects that body or none, with no face. `CancelIntent`, the key Escape, goes back from the face to its
   body; with no face it does nothing. The row of a body in the panel gives the body with no face. Each change of the
   selected body clears the face. The pick of the desktop gives `PickHit`, with its point and its normal (ADR-0053
   item 5). The face is a state of the client (ADR-0007): no command and no event names it.
3. **The panel and the mark (Q3 A).** `BodyActions.SelectedFace` gives the entry of `GetFaces` with the name of the
   selected face; a name that the body no longer has, or a body whose faces are not known, clears the face. The
   overlay draws the outline in the colour and the width of a selected item, over the scene and under the panel. The
   controls of the selected body give one line: "Face", the element of the name and the area, and, for a face with a
   normal, the button "Sketch on face", which makes the sketch from the point and the normal of the entry, as the armed
   tap of ADR-0053 item 6 does. A curved face has no button.
4. **The amendment.** This record amends ADR-0029 item 1: the desktop selects one body, one face of the selected
   body, or none.

## Consequences

**Good:** a client reads the mark of a face from the query that it reads for the names, with no new code.

**Bad:** each entry of `GetFaces` is larger, by one segment for each edge of its outline.

**Next:** the gesture and the panel of R12a; the edges in R12b, after a name of an edge.

## History

- 2026-11-06 · TASK-0236 · The record, Proposed, with item 1 and its tests.
- 2026-11-06 · TASK-0236 · Item 2, the gesture, with its tests.
- 2026-11-06 · TASK-0236 · Items 3 and 4, the panel and the mark; the record is Accepted under the standing order.
- 2026-11-06 · TASK-0237 · Item 2: the key Escape gives `CancelIntent` through the intent layer; the button "Sketch on
  face" ends the arm of Face.
- 2026-11-06 · TASK-0243 · Item 2: only the key Escape (`CancelIntent.ByKey`) goes back from the face, and a lost pointer
  keeps it; item 3: the outline follows a drag of the gizmo, and the entry comes again only for a new geometry key. The
  affects field names the six files of items 2 and 3 (finding V111).
