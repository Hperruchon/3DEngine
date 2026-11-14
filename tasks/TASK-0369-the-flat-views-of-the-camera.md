---
id: 0369
title: The camera has six flat views with no perspective, and the command line gives each one
status: Active
phase: R21a
opened: 2026-11-14
depends-on: [0367, 0368]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0038, 0041, 0053, 0072, 0073, 0075, 0076, 0077, 0080, 0081, 0082, 0083, 0085, 0086, 0089, 0092, 0094, 0096, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0106, 0108, 0110, 0111, 0113, 0114, 0116, 0119, 0120, 0124, 0127, 0128, 0129, 0130, 0135, 0137]
writes:
  create:
    - tasks/TASK-0369-the-flat-views-of-the-camera.md
    - docs/adr/0137-the-flat-views-of-the-camera.md
    - 3DEngine.Tests/FlatViewTests.cs
    - 3DEngine.ImageTests/FlatViewWindowTests.cs
  modify:
    - 3DEngine.Core/Models/Camera.cs
    - 3DEngine.Core/Picking/Picker.cs
    - 3DEngine.Core/Gizmo/TranslateGizmo.cs
    - 3DEngine.App/SketchOverlay.cs
    - 3DEngine.App/Ui/PanelAction.cs
    - 3DEngine.App/Ui/DesktopUi.Command.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.ImageTests/MixImageTests.cs
    - docs/adr/0023-camera-depth-and-draw-list.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0369 — The camera has six flat views with no perspective, and the command line gives each one

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row R21 (ADR-0118 item 2) starts the 2D drawing with the flat views in the window. The camera had a projection with
perspective only and no view from the top, and four places computed the axes of the screen. On 2026-11-14 the owner ordered
work alone until Sunday, with the recommended option for each question.

## Goal

The person sees the part from the front, the back, a side, the top or the bottom with no perspective, and each pick, chip and
plate agrees with the picture.

## Scope (in)

1. ADR-0137, which amends ADR-0023 item 1, and the split of row R21 into R21a, R21b and R21c.
2. The six flat views of the camera, the return to the 3D view, one source of the axes and the projection with no perspective.
3. The pick, the overlay of the window, the gizmo and the pan in a flat view.
4. The commands of the views in the command line.
5. Tests with no GPU, and pictures of the bracket from the front and from the top.

## Scope (out)

- The controls of one tap and the keys (R21b).
- Frame in the free part of the scene (R21c).
- The values in a view where their length is true (R22).

## Acceptance criteria

- [ ] ADR-0137 exists. ADR-0023 shows the amendment.
- [ ] Tests prove the axes of each flat view, the projection with no perspective at the scale of the target, the reversed
      depth, the return, the parallel rays of a pick, the pan from the top and the length of the gizmo.
- [ ] A test proves the commands of the views and the overlay with no perspective.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the axes from the yaw and the pitch; the projection with no perspective; the four users of the axes.
Judgement:  the exact pitch of the top view; the split of R21; the names of the commands.
Weakest:    no person used the views; a value along the view still has its chip on one point until R22.

## Progress

- 2026-11-14: opened under the order of the owner of 2026-11-14, after P0.148, branch r21a-flat-views. TASK-0369 and
  ADR-0137 are free. The pitch stopped at 89 degrees and the view took the up of the world, so a view from the top had no
  view matrix; the camera now gives its axes, and the view, the pick, the overlay, the gizmo and the pan take them. The
  pictures of the mix test show the bracket as a rectangle of 60 by 10 from the front and of 60 by 40 from the top; the chip
  of the depth, an edge along the view, stays on one point. Nine injections each failed a named test: the projection with
  perspective, a pitch of 89 degrees, no return, an orbit that stays flat, a pan with the pitch, a pick, a gizmo and an
  overlay with perspective, and no commands. Each file came back byte for byte.
