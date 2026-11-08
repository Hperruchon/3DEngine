---
id: 0096
title: The camera of the session has the z axis up, as the render of the mix draws a part
status: Accepted
topic: Clients, rendering
date: 2026-11-08
supersedes: []
superseded-by: []
amends: ['0023', '0034']
amended-by: []
affects:
  - 3DEngine.Core/Models/Camera.cs
  - 3DEngine.Core/Sketching/SketchCamera.cs
  - 3DEngine.Core/Picking/Picker.cs
  - 3DEngine.Core/Gizmo/TranslateGizmo.cs
  - 3DEngine.App/SketchOverlay.cs
  - 3DEngine.App/SessionScene.cs
enforced-by: 3DEngine.Tests/CameraTests.cs
---

# ADR-0096 — The camera of the session has the z axis up, as the render of the mix draws a part

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-08 the owner chose five slices of the redesign, to bring the desktop near to the render "mix" of the designer
brief. The fourth slice is the sketch plane: the sketch floats under the body that it makes. In the render, the bracket
and its sketch "Plate on XY" lie flat, so the z axis is up. The camera of ADR-0023 item 2 has the y axis up
(ADR-0034, "The camera"), so a part on the plane of x and y stands upright, and a sketch "under" the body is behind it.
The slices of the redesign need no contract change, and the camera is state of the client (ADR-0007).

### Q1. How the camera gets z up

- **A.** A property `ZUp` on the camera. The yaw, the pitch, the orbit and the pan keep their frame of y up, and the eye,
  the pan and the view turn that frame by a quarter turn about x. The camera of the session sets it.
- **B.** New formulas for each direction, with z up only.

A keeps each test and each caller of the camera with y up, and changes three methods. **Recommended: A.**

### Q2. The view at the start

- **A.** A yaw of 135° and a pitch of 30°: the eye on the positive side of x, y and z.
- **B.** A yaw of 30° and a pitch of 20°, as today: with z up, the eye is on the negative side of y.

With B, the notch of the first demonstration (ADR-0025 item 1, at the corner of positive x, y and z) is hidden. With a
yaw of 150° and a pitch of 20°, two faces of the notch have the same shade. A shows the three faces with three shades,
and it looks down on a part as the render does. **Recommended: A.**

## Decision

The owner chose the slices and the render as the target; the recommendations answer Q1 and Q2.

1. **Z up (Q1 A, amends ADR-0023 item 2 and ADR-0034, "The camera").** `Camera.ZUp` is false by default. When it is
   true, a yaw of 0 puts the eye on the negative y side of the target, the yaw turns about the z axis, the pitch is above
   the plane of x and y, a pan moves the target in the plane of the view, and the view draws z up on the screen.
   `ToWorld` and `FromWorld` turn a direction between the frame of y up and the world. `UpAxis` is z or y.
2. **One up axis for each frame of the screen.** The ray of a pick (`Picker.RayThrough`), the overlay of a sketch and the
   gizmo take the right of the screen from the forward direction and `UpAxis`, as `Camera.View` does, so a pick, a line
   of a sketch and a handle agree with the picture.
3. **The turn to a sketch.** `SketchCamera.Face` turns the normal into the frame of y up first, so a camera with z up looks
   down on a sketch on the plane of x and y, with the pitch limit of the camera (ADR-0034 item 6).
4. **The camera of the session (Q2 A).** It has z up, a yaw of 135° and a pitch of 30°. The desktop and the phone use it.
5. **No change of the log.** The camera stays out of the Document (ADR-0007). The trigonometry of the camera is
   presentation and never reaches the log.

## Consequences

**Good:** a part lies on the plane of x and y as in the render, and the sketch plane of the next slice can float under it.

**Bad:** each picture of the session changes, and a person who knows the old view sees the part from a new side.

## History

- 2026-11-08 · TASK-0307 · The record, with the slice.
