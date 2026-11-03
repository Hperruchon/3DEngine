---
id: 0179
title: The far plane of the camera of the desktop follows the box of the visible bodies
status: Done
phase: P0.77
opened: 2026-11-04
depends-on: [0178]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0029, 0043]
writes:
  create:
    - tasks/TASK-0179-far-plane-of-the-desktop.md
  modify:
    - 3DEngine.Core/Models/Camera.cs
    - 3DEngine/SceneRenderer.cs
    - 3DEngine.Tests/CameraTests.cs
    - 3DEngine.ImageTests/DimensionImageTests.cs
    - docs/adr/0023-camera-depth-and-draw-list.md
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0179 — The far plane of the camera of the desktop follows the box of the visible bodies

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

TASK-0178 (R13) found register entry R-0051: the camera of the desktop keeps the default far plane of 1000 units, and
`Camera.Frame` and `Zoom` hold the distance at half the far plane, so a model in millimetres larger than about 1000 mm
cannot show whole. The shelf of the owner is 1900 mm high. On 2026-11-04 the owner gave the order "merge when green,
then fix R-0051", so this task comes after R13 and before P0.76. It takes the first exit of the entry: the far plane
follows the size of the live bodies.

## Goal

The desktop frames a model of 1900 mm whole, in a wide and in a narrow window, with no setting.

## Scope (in)

1. `Camera.FarPlaneFor`: the far plane that holds a box.
2. `SceneRenderer.Update` sets it at each change of the bodies, before the first frame.
3. Tests that frame a shelf of 1900 mm whole, and the image test of R13 with no far plane of its own.

## Scope (out)

- A setting of the planes in the panel. The near plane, which stays 0.1.

## Acceptance criteria

- [x] The default camera cannot frame the shelf, and the far plane for its box frames each corner in the picture and in
      front of the far plane, for the aspects 16:9, 1 and 0.3 and far from the origin.
- [x] The image test of the dimensions passes with no far plane of its own.
- [x] Each injection of the change fails a test.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p077-far-plane) · v0.180

## Method

Mechanical: the projection of each corner of a framed box; five injections.
Judgement:  the factor of sixteen, from the distance of a frame at the aspect 0.3.
Weakest:    no one opened the desktop window; the image test renders the same code offscreen.

## Progress

- 2026-11-04: opened with the order of the owner; the rule, its call, the tests and five injections.
- 2026-11-04: closed at v0.180.
