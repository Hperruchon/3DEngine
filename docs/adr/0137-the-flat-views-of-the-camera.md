---
id: 0137
title: The camera has six flat views with no perspective, and the command line gives each one
status: Accepted
topic: Rendering, clients
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0023']
amended-by: []
affects:
  - 3DEngine.Core/Models/Camera.cs
  - 3DEngine.Core/Picking/Picker.cs
  - 3DEngine.Core/Gizmo/TranslateGizmo.cs
  - 3DEngine.App/SketchOverlay.cs
  - 3DEngine.App/Ui/PanelAction.cs
  - 3DEngine.App/Ui/DesktopUi.Command.cs
  - 3DEngine.App/NativeThreeDEngine.cs
  - 3DEngine.Tests/FlatViewTests.cs
  - 3DEngine.ImageTests/FlatViewWindowTests.cs
  - 3DEngine.ImageTests/MixImageTests.cs
enforced-by: 3DEngine.Tests/FlatViewTests.cs
---

# ADR-0137 — The camera has six flat views with no perspective, and the command line gives each one

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0118 item 2 starts the 2D drawing with row R21: the flat views in the window, a front, a top, a side and the other three
views with no perspective, one tap each, a return to the 3D view, and a Frame that fills the free part of the scene. The UX
test of 2026-11-10 (problem 6) found that no view of the front existed.

The camera had a projection with perspective only (ADR-0023 item 1), and a pitch from -89 to 89 degrees (finding V30), so a
view from the top did not exist. Four places computed the axes of the screen each in its own way: the view, the ray of a pick,
the overlay of the window and the gizmo. A flat view must change all four together, or a pick and an overlay do not agree
with the picture.

Row R21 holds three parts of different kinds: the camera, the controls of one tap, and the frame of the free part. The
controls are a choice of the look of the window that the owner wants to see.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Decision

1. **The flat views (amends ADR-0023 item 1).** The camera holds a flat view, or none. The six flat views look along the axes
   of the world: Front from the side of negative y with z up (the yaw of 0), Right from positive x, Back from positive y,
   Left from negative x, Top from above and Bottom from below. Top and Bottom have an exact pitch of 90 and -90 degrees; the
   up of the screen is then the direction away from the eye of the front view. A flat view keeps the target and the distance.
2. **The return.** "3D view" gives back the yaw and the pitch before the first flat view. An orbit leaves a flat view and
   turns from its direction. A pan and a zoom keep the flat view.
3. **One source of the axes, and no perspective.** The camera gives the right, the up and the forward direction of the screen
   (`Axes`), and the view, the ray of a pick, the overlay of the window, the gizmo and the pan take them from there. A flat
   view projects each point at the scale that the perspective gives at the target (`HalfHeight`), so the part keeps its size
   when the view changes, with the same reversed depth: 1 at the near plane and 0 at the far plane. A ray of a pick goes along
   the view from the plane of the eye.
4. **The commands (phase R21a).** The command line has "Front view", "Back view", "Left view", "Right view", "Top view",
   "Bottom view" and "3D view", with or without a selected body.
5. **The split of row R21.** R21a is items 1 to 4. R21b gives the controls of one tap and the keys of the views. R21c makes
   Frame fill the free part of the scene. Each opens its task at its start.
6. **No contract change.** The camera is render state (ADR-0007).

## Consequences

**Good:** the person sees the part from the front, the top or a side with no perspective, and each pick, chip and plate
agrees with the picture.

**Bad:** a value whose edge runs along the view, as the depth in the view from the front, still has its chip on one point;
R22 puts each value one time in a view where its length is true. No person used the views; the controls of one tap wait for
R21b.

## Alternatives considered

- **A pitch of 89.99 degrees for the top view.** Refused: the view is then not exact, and R22 measures true lengths in it.
- **A camera of its own for the flat views.** Refused: the target, the distance and the pan of the person stay with one
  camera, and four places would need a second source of axes.

## History

- 2026-11-14 · TASK-0369 · The record, from row R21 and ADR-0118 item 2, under the order of the owner of 2026-11-14.
