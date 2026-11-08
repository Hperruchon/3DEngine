---
id: 0094
title: A framed part fills 45 % of the half picture, as in the render of the mix
status: Accepted
topic: Clients, rendering
date: 2026-11-08
supersedes: []
superseded-by: []
amends: ['0025']
amended-by: []
affects:
  - 3DEngine.Core/Models/Camera.cs
  - 3DEngine.App/SessionScene.cs
enforced-by: 3DEngine.Tests/CameraTests.cs
---

# ADR-0094 — A framed part fills 45 % of the half picture, as in the render of the mix

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The second slice that the owner chose on 2026-11-08 is the framing: the part in the middle of the free area, as in the
render "mix". ADR-0025 item 6 frames a box so that the sphere around it touches the narrower edge of the picture; at 1440 x
900 the bracket of the render then fills the height of the display, and the panel, the rows and the ring cover it.

In the render at 1440 x 900, the part is about a third of the width, and the ring and the values sit around it. The middle
of the free area, between the panel at the left and the rows at the right, is about 50 pixels from the middle of the
display, so a frame about the middle of the display is close enough; a shift of the middle of the projection would change
each place that projects a point (the picker, the gizmo and the overlays).

### Q1. How much of the picture a framed part fills

- **A.** 45 % of the half picture for the camera of the session, and the whole of it (ADR-0025 item 6) for each other camera.
- **B.** The free area between the columns, from the layout of each frame.

A is one number on the camera; B needs the layout of the interface in the camera. **Recommended: A.**

## Decision

The owner chose the slice; the recommendation answers Q1.

1. **The fill (amends ADR-0025 item 6).** A camera has `FrameFill`, greater than 0 and at most 1, and 1 by default. A frame
   puts the target at the middle of the box and the eye at the distance where the tangent of the angle of the sphere is
   `FrameFill` times the tangent of the smaller half angle of the field of view. The yaw and the pitch do not change.
2. **The camera of the session** has `FrameFill` 0.45, so the button Frame, the first frame of the bodies and the frame after
   an import leave room for the ring and the values.
3. **No shift of the middle.** The part frames about the middle of the display.
4. **No contract change.** The camera is client state (ADR-0007).

## Consequences

**Good:** a framed part has the size of the render, and the ring, the chips and the rows do not cover it.

**Bad:** on a phone in portrait the part is small. The camera keeps Y up, so a plate on XY stands upright, where the render
lays it flat with Z up; that is a separate decision.

## History

- 2026-11-08 · TASK-0305 · The record, with the slice (the choice of the owner of 2026-11-08).
