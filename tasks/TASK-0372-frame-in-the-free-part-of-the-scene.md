---
id: 0372
title: Frame fits the bodies in the free part of the scene, and the centre of the picture moves there
status: Done
phase: R21c
opened: 2026-11-14
depends-on: [0370, 0373]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0038, 0041, 0053, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0089, 0091, 0092, 0093, 0094, 0096, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0110, 0111, 0113, 0114, 0116, 0117, 0119, 0120, 0123, 0124, 0125, 0127, 0129, 0130, 0131, 0137, 0138, 0139, 0140]
writes:
  create:
    - tasks/TASK-0372-frame-in-the-free-part-of-the-scene.md
    - docs/adr/0140-frame-in-the-free-part-of-the-scene.md
    - 3DEngine.Tests/FrameFreePartTests.cs
    - 3DEngine.ImageTests/FrameFreePartWindowTests.cs
  modify:
    - 3DEngine.Core/Models/Camera.cs
    - 3DEngine.Core/Picking/Picker.cs
    - 3DEngine.Core/Gizmo/TranslateGizmo.cs
    - 3DEngine.App/SketchOverlay.cs
    - 3DEngine.App/Ui/DesktopUi.Views.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - docs/adr/0023-camera-depth-and-draw-list.md
    - docs/adr/0137-the-flat-views-of-the-camera.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0372 — Frame fits the bodies in the free part of the scene, and the centre of the picture moves there

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row R21c (ADR-0137 item 5) and problem 6 of the UX test of 2026-11-10: Frame fits the bodies in the whole picture, so the
panel at the left covers a part of them. The high findings of the review of 2026-11-14 (d) came first (TASK-0373). On
2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question.

## Goal

After Frame the bodies are in the part of the scene that the person sees.

## Scope (in)

1. ADR-0140, which amends ADR-0023 item 1 and ADR-0137 item 3.
2. The shift of the centre of the picture in the camera, the projection, the pick, the overlay and the gizmo.
3. A frame in a part of the picture, and the free part from the windows of the last frame.
4. Tests with no GPU.

## Scope (out)

- The first frame after an open, which keeps the whole picture.

## Acceptance criteria

- [x] ADR-0140 exists. ADR-0023 and ADR-0137 show the amendment.
- [x] Tests prove that a frame in a part puts the target at its centre and the box and its sphere in it, with and without
      perspective, that the pick and the gizmo agree with the projection, and that a frame of the whole picture has no shift.
- [x] A test proves that the free part lies on no window of the last frame at four states.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r21c-frame-free-part) · v0.358

## Method

Mechanical: the shift in the four users of the projection; the fit in a part.
Judgement:  the bands of the free part; a shift in place of a target off the centre.
Weakest:    no person used it; the free part comes from fixed bands of the display.

## Progress

- 2026-11-14: opened under the order of the owner of 2026-11-14, after R21b, review (d) and P0.149, branch
  r21c-frame-free-part; it moved onto the head of P0.149 before its commit. TASK-0372 and ADR-0140 are free. The window with a
  short panel or short rows takes them as a window of the upper band, so the free part is under them across the width. A fit
  to the whole picture was caught by no test with the corners of the box only, so the test checks the sphere too. Eight
  injections each failed a named test. Each file came back byte for byte. The merges wait for the owner, who must run the
  red main run of v0.354 again.
- 2026-11-14: run 43566591043 passed on the work commit. Closed at v0.358.
