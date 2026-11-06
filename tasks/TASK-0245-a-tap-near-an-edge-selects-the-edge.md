---
id: 0245
title: An edge is named by its two faces, and a tap near an edge of the selected face selects it
status: Done
phase: R12b
opened: 2026-11-06
depends-on: [0236, 0242]
governed-by: [0004, 0007, 0016, 0018, 0019, 0025, 0027, 0028, 0029, 0030, 0032, 0033, 0034, 0037, 0058, 0060, 0072, 0073, 0075, 0076, 0077]
writes:
  create:
    - tasks/TASK-0245-a-tap-near-an-edge-selects-the-edge.md
    - docs/adr/0077-a-tap-near-an-edge-selects-the-edge.md
    - 3DEngine.ImageTests/EdgeSelectionTests.cs
  modify:
    - Engine.Core/Queries/GetFacesQuery.cs
    - Engine.Core/Queries/GetFacesQueryHandler.cs
    - Engine.Tests/Features/GetFacesTests.cs
    - 3DEngine.App/BodyActions.cs
    - 3DEngine.App/FaceOverlay.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.App/Ui/DesktopUi.Overlay.cs
    - 3DEngine.App/Ui/PanelAction.cs
    - docs/adr/0075-a-second-tap-selects-the-face.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0245 — An edge is named by its two faces, and a tap near an edge of the selected face selects it

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The roadmap row R12 gives the escalated selection of faces and edges. R12a (v0.221, ADR-0075) gives the face, and
leaves the edges to R12b, "after a name of an edge in track K". The owner put R12 first in the queue of 2026-11-06
(evening). ADR-0052 names the edges of a box and of an extrude for a reference of a sketch, but a cut gives edges that
have no such name. The B-Rep of the owned kernel gives each edge its two faces, and each face its name (ADR-0059).

## Goal

A person selects an edge of a body: a first tap gives the body, a second the face, and a tap near an edge of that face
gives the edge, with its name, its length and its mark.

## Scope (in)

1. ADR-0077, Accepted under the standing order of 2026-11-05; it amends ADR-0075 item 2.
2. `GetFaces` gives the edges: the pair of the names of the two faces, the parts, the length and the segments.
3. `BodyActions` keeps the selected edge; Escape goes back from the edge to the face; the host finds the edge of the
   selected face nearest the tap, at most 12 pixels away.
4. The overlay draws the selected edge, and the panel gives its line.

## Scope (out)

- An edge as a reference of a sketch: it needs the name in `Engine.Contracts`.
- The values on the edges of the redesign.

## Acceptance criteria

- [x] A box gives twelve edges, a cylinder two rims of 64 segments, and a cut the rims of its hole.
- [x] A tap near an edge of the selected face selects the edge; Escape gives the face, then the body.
- [x] The nearest edge on the screen is at most 12 pixels away.
- [x] An injection of each rule fails a test.

## Outcome

Status: Done · commit (the merge of the branch r12b-edge-selection) · v0.230

## Method

Mechanical: the edges from the B-Rep that `GetFaces` keeps; the gesture as the gesture of the face.
Judgement:  the name as a pair of face names (Q1 A), the edges in `GetFaces` (Q2 A), and a tap near an edge (Q3 A).
Weakest:    no person tapped an edge on the desktop or on the phone; the curved side of a cylinder cannot be selected,
            so its rims are selected from the top or the bottom face only.

## Progress

- 2026-11-06: ADR-0077, the edges of `GetFaces`, the gesture, the mark and the panel; six tests. An injection that keeps
  the edges inside a name, one that removes the step of Escape from the edge, and one that widens the reach of a tap
  each fail a test.
- 2026-11-06: run 42291699658 passed. Closed at v0.230.
