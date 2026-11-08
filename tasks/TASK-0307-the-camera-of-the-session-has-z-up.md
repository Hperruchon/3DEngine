---
id: 0307
title: The camera of the session has the z axis up, as the render of the mix draws a part
status: Active
phase: R16m
opened: 2026-11-08
depends-on: [0306]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0029, 0034, 0038, 0041, 0053, 0072, 0094, 0096]
writes:
  create:
    - tasks/TASK-0307-the-camera-of-the-session-has-z-up.md
    - docs/adr/0096-the-camera-of-the-session-has-z-up.md
  modify:
    - 3DEngine.Core/Models/Camera.cs
    - 3DEngine.Core/Sketching/SketchCamera.cs
    - 3DEngine.Core/Picking/Picker.cs
    - 3DEngine.Core/Gizmo/TranslateGizmo.cs
    - 3DEngine.App/SketchOverlay.cs
    - 3DEngine.App/SessionScene.cs
    - 3DEngine.Tests/CameraTests.cs
    - 3DEngine.Tests/SketchToolTests.cs
    - 3DEngine.Tests/PickingTests.cs
    - 3DEngine.Tests/GizmoTests.cs
    - 3DEngine.ImageTests/SketchOverlayTests.cs
    - docs/adr/0023-camera-depth-and-draw-list.md
    - docs/adr/0034-sketch-tools.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0307 — The camera of the session has the z axis up, as the render of the mix draws a part

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The fourth slice that the owner chose on 2026-11-08 is the sketch plane under the body. In the render, the part and its
sketch lie flat with the z axis up; the camera of the session has the y axis up. This task comes first.

## Goal

The camera of the session has the z axis up, and the bracket of the render lies flat at 1440 x 900.

## Scope (in)

1. ADR-0096: `Camera.ZUp`, the turn to a sketch, and the camera of the session at a yaw of 135° and a pitch of 30°.
2. The ray of a pick, the overlay of a sketch and the gizmo take the up axis of the camera.
3. Tests of the eye, the view, the pan, the turn to a sketch, the ray, the overlay and the gizmo with z up.

## Scope (out)

- The sketch under the body (the next slice). A control to change the up axis.

## Acceptance criteria

- [ ] The new tests pass, and the picture at 1440 x 900 shows the bracket flat.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the quarter turn about x between the frame of y up and the world.
Judgement:  the view at the start (ADR-0096 Q2 A), from the shades of the notch.
Weakest:    to be written at the close.

## Progress

- 2026-11-08: the slice and its tests. With `ToWorld` and `FromWorld` that ignore `ZUp`, six of the seven new cases fail.
  The test of the notch failed with a yaw of 30° (the notch is hidden) and with 150° (two faces have one shade).
- 2026-11-08: the ray of a pick, the overlay of a sketch and the gizmo had a fixed y up, so with z up a pick would not
  agree with the picture; no test found it. Each now takes `Camera.UpAxis`. With the fixed y up again, five new cases
  fail.
