---
id: 0236
title: A second tap on the selected body selects the face under the point
status: Done
phase: R12a
opened: 2026-11-06
depends-on: [0234]
governed-by: [0004, 0007, 0016, 0018, 0019, 0025, 0027, 0028, 0029, 0030, 0032, 0033, 0034, 0037, 0058, 0060, 0072, 0073, 0075]
writes:
  create:
    - 3DEngine.App/FaceOverlay.cs
    - 3DEngine.ImageTests/FaceSelectionTests.cs
    - tasks/TASK-0236-a-second-tap-selects-the-face.md
    - docs/adr/0075-a-second-tap-selects-the-face.md
  modify:
    - Engine.Core/Queries/GetFacesQuery.cs
    - Engine.Core/Queries/GetFacesQueryHandler.cs
    - Engine.Tests/Features/GetFacesTests.cs
    - 3DEngine.App/BodyActions.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.ImageTests/BodyActionsTests.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.App/Ui/DesktopUi.Overlay.cs
    - 3DEngine.App/Ui/PanelAction.cs
    - docs/adr/0029-selection-and-hide-gestures.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
---

# TASK-0236 — A second tap on the selected body selects the face under the point

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The roadmap row R12 gives the escalated selection of faces and edges, after K4. K4b gives a name to each face, and
`GetFaceAt` gives the face under a hit point. No name of an edge exists. The fallback queue of the owner (2026-11-05)
gives R12 after the split of the user interface.

## Goal

A person selects a face of the selected body with a second tap, sees its outline in the scene, and reads it in the
panel, with a finger, a pen or a mouse.

## Scope (in)

1. ADR-0075 with the options of the gesture, of the mark and of the panel, answered by the standing order of 2026-11-05.
2. The outline of each face name in `GetFaces` (ADR-0075 item 1).
3. The gesture of the second tap, the key Escape, and the row of the body (ADR-0075 item 2).
4. The mark of the outline in the scene, and the face in the panel with its area, its normal and a button Sketch
   (ADR-0075 item 3).

## Scope (out)

- The edges (R12b), which need a name of an edge in track K.
- The layout of the redesign.

## Acceptance criteria

- [x] Each face of a box has an outline of four edges, a hole is in the outline, and no edge inside a curve is.
- [x] A test of the gesture with a finger and with a mouse gives the same face.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r12a-face-selection) · v0.221

## Method

Mechanical: the outline from the edges of the B-Rep, and a test of each form before the change.
Judgement:  the three answers of ADR-0075, which the standing order gave.
Weakest:    no person tapped a face on the desktop or on the phone, and no image of the GPU shows the outline; a
            curved face cannot be selected, because GetFaceAt names plane faces only.

## Progress

- 2026-11-06: ADR-0075 is Proposed, with the options. Item 1: GetFaces gives the outline of each face name; two tests,
  and an injection that counts the edges inside a name fails the test of the curve. The gesture and the panel come next.
- 2026-11-06: item 2, the gesture: a second tap on the selected body selects the face, Escape and the row give the
  body. Two tests, one with a finger and a mouse; an injection that keeps the old select fails both.
- 2026-11-06: items 3 and 4, the panel and the mark, with three tests; an injection that keeps a face of a body that is
  gone, and an injection that gives a curved face the button, each fail a test. ADR-0075 is Accepted.
- 2026-11-06: run 42173547034 passed on the three runners and on the emulator. Closed at v0.221.
