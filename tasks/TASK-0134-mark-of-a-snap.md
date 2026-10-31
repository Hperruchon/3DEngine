---
id: 0134
title: The overlay marks the point that a tap under the mouse or a finger would take
status: Active
phase: R11b
opened: 2026-10-31
depends-on: [0131, 0133]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0026, 0027, 0029, 0030, 0032, 0034, 0038, 0041]
writes:
  create:
    - tasks/TASK-0134-mark-of-a-snap.md
  modify:
    - 3DEngine.Core/Input/Intents.cs
    - 3DEngine.Core/Input/IntentMachine.cs
    - 3DEngine.Core/Sketching/SketchTool.cs
    - 3DEngine/SketchActions.cs
    - 3DEngine/SketchOverlay.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine/Ui/DesktopUi.cs
    - 3DEngine.Tests/IntentParityTests.cs
    - 3DEngine.Tests/GizmoTests.cs
    - 3DEngine.ImageTests/SketchActionsTests.cs
    - 3DEngine.ImageTests/SketchOverlayTests.cs
    - 3DEngine.ImageTests/CurveImageTests.cs
    - docs/adr/0041-snap-of-a-sketch-tap.md
    - docs/adr/0026-intent-layer.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Api.Http/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
---

# TASK-0134 — The overlay marks the point that a tap under the mouse or a finger would take

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

R11a (TASK-0131) gave a tap of the sketch a snap, and ADR-0041 item 5 left the mark of the snap to the rest of R11,
because the intent machine had no hover intent. The owner told the session on 2026-10-31 to use its recommendation for
each question until the morning.

## Goal

In the mode Sketch, the overlay shows a square at the point that a tap would take, under the mouse and under a finger
before its tap.

## Scope (in)

1. `SketchHoverIntent` for a motion of the mouse with no button, the press of the left button, and the first contact
   of a touch or a pen, in the mode Sketch (ADR-0041 item 6, which amends ADR-0026).
2. `SketchTool.Target`, `SketchActions.Mark` and `SketchOverlay.Pixel`; the panel draws the square.

## Scope (out)

- A change of the engine. The escalated selection of the rest of R11.

## Acceptance criteria

- [ ] A touch script and a mouse script give the same `SketchHoverIntent` (objective 9).
- [ ] The mark is the pixel of the point that a tap would take, and the tool Select has none.
- [ ] A picture on a GPU shows the mark, and the owner accepts it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-31: a hover for the mouse alone failed the parity test of objective 9, so a press of the left button and the
  first contact of a finger give the hover too; the tests of the gestures of the mode Sketch skip the hover.
- 2026-10-31: the hover, the target, the mark and the square; 3 new test cases, and the GPU test of the curves also draws
  the mark; 6 injections caught. The visual check waits for the owner.
