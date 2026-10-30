---
id: 0122
title: The desktop draws and adds circles and arcs
status: Ready
phase: K2e
opened: 2026-10-30
depends-on: [0117, 0121]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0027, 0029, 0030, 0032, 0034]
writes:
  create:
    - tasks/TASK-0122-desktop-circle-and-arc.md
  modify:
    - 3DEngine/SketchOverlay.cs
    - 3DEngine/SketchActions.cs
    - 3DEngine/SessionSketches.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine/Ui/DesktopUi.cs
    - 3DEngine/Ui/PanelAction.cs
    - 3DEngine.Core/Sketching/SketchTool.cs
    - 3DEngine.Tests/SketchToolTests.cs
    - 3DEngine.ImageTests/SketchOverlayTests.cs
    - 3DEngine.ImageTests/SketchActionsTests.cs
    - docs/adr/0034-sketch-tools.md
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

# TASK-0122 — The desktop draws and adds circles and arcs

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

K2d (TASK-0117) gave the engine the circle and the arc, and `tangent`, `radius`, `pointOn` and `equal` on a curve.
The row of K2d says that the desktop waits for K2e. The codebase review of 2026-10-30 (b), sections 5 and 6, gives the
research and the recommended approach.

## Goal

The desktop draws each circle and each arc of a sketch, adds them with two tools, and offers the constraints of K2c
and K2d in the panel.

## Scope (in)

1. `SketchOverlay` draws a curve as a polyline. A polyline is presentation (ADR-0007).
2. `SketchTool` gains the kinds Circle and Arc, and `ISketchEditor` gains `AddCircle` and `AddArc`. The radius of a
   new circle is the distance in the plane, with `Math.Sqrt` only.
3. The Select tool picks a curve.
4. The panel offers tangent, radius, pointOn and equal, and the radius opens the pad.
5. ADR-0034 gains the two tools.

## Scope (out)

- A change of the engine. An extrude of a profile with a curve (TASK-0117, Scope (out)).
- V51 to V55. P0.50 holds them, after this task.

## Acceptance criteria

- [ ] A tool script with no window adds a circle and an arc, and a test picks each one.
- [ ] A picture on a GPU shows a circle and an arc.
- [ ] The owner accepts the pictures of the visual check.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-30: opened by the codebase review of 2026-10-30 (b) (TASK-0120).
