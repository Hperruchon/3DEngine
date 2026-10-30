---
id: 0122
title: The desktop draws and adds circles and arcs
status: Done
phase: K2e
opened: 2026-10-30
depends-on: [0117, 0121]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0027, 0029, 0030, 0032, 0034]
writes:
  create:
    - tasks/TASK-0122-desktop-circle-and-arc.md
    - 3DEngine.ImageTests/CurveImageTests.cs
  modify:
    - 3DEngine/SketchOverlay.cs
    - 3DEngine/SketchActions.cs
    - 3DEngine/SessionSketches.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine/Ui/DesktopUi.cs
    - 3DEngine/Ui/PanelAction.cs
    - 3DEngine/Ui/SketchText.cs
    - 3DEngine.Core/Sketching/SketchTool.cs
    - 3DEngine.Tests/SketchToolTests.cs
    - 3DEngine.ImageTests/SketchOverlayTests.cs
    - 3DEngine.ImageTests/SketchActionsTests.cs
    - 3DEngine.ImageTests/SketchWindowUiTests.cs
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

The desktop draws each circle and each arc of a sketch, adds them with two tools, and offers the constraints of K2d
on a curve in the panel.

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
- The kinds of K2c on lines in the panel (pointOn and midpoint on a line, parallel, perpendicular, equal of two
  lines, symmetric and angle). They change the buttons of the selections of K2b, so a later task adds them.

## Acceptance criteria

- [x] A tool script with no window adds a circle and an arc, and a test picks each one.
- [x] A picture on a GPU shows a circle and an arc.
- [x] The owner accepts the pictures of the visual check.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch k2e-circle-arc) · v0.124

## Method

Mechanical: the polyline of a curve, the two tools, the pick of a curve, the labels and rows, and five buttons.
Judgement:  a new end of an arc goes on the circle of the start, so the arc is solved at its add; the radius of a
            circle is the distance with sqrt only, so the command records the same number on each platform. The
            visual check used a picture of the renderer with no window, because a person used the computer.
Weakest:    no person drew a curve with the real window and a mouse or a finger; the tool scripts run with no
            window, and the picture comes from the same renderer and panel code.

## Progress

- 2026-10-30: opened by the codebase review of 2026-10-30 (b) (TASK-0120).
- 2026-10-30: the tools Circle and Arc, the polyline of a curve, the pick of a curve and the kinds on a curve in the
  window. The Goal named the kinds of K2c too; Scope (in) item 4 names the kinds on a curve only, and the Goal and the
  row of K2e now agree with it.
- 2026-10-30: 10 new test cases (5 in `3DEngine.Tests`, 5 in `3DEngine.ImageTests`); 8 injections caught. The row of
  Next point stays the second row of the window, so the pad stays inside a display of 1024 x 768.
- 2026-10-30: `CurveImageTests` draws a circle, an arc and a tangent line on a GPU with no window and gives the
  picture of the visual check; an injection that draws no curve fails it. A person used the computer, so the check
  drove no mouse and opened no window.
- 2026-10-30: the owner accepted the picture ("yes"); closed at v0.124.
