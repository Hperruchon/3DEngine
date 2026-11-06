---
id: 0178
title: The desktop shows the dimensions of the overall box and of a selected body, with a toggle in the panel
status: Done
phase: R13
opened: 2026-11-04
depends-on: [0176]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0027, 0029, 0030, 0031, 0032, 0034, 0041, 0043, 0053, 0056]
writes:
  create:
    - tasks/TASK-0178-dimension-overlay.md
    - docs/adr/0056-dimension-overlay.md
    - 3DEngine.Core/Measuring/Dimensions.cs
    - 3DEngine.App/DimensionOverlay.cs
    - 3DEngine.Tests/DimensionsTests.cs
    - 3DEngine.ImageTests/DimensionOverlayTests.cs
    - 3DEngine.ImageTests/DimensionImageTests.cs
  modify:
    - 3DEngine/SketchOverlay.cs
    - 3DEngine/SessionBodies.cs
    - 3DEngine/SceneRenderer.cs
    - 3DEngine/Ui/DesktopUi.cs
    - 3DEngine/NativeThreeDEngine.cs
    - docs/adr/README.md
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
---

# TASK-0178 — The desktop shows the dimensions of the overall box and of a selected body, with a toggle in the panel

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner used the desktop and an agent to make an L-shaped shelf for an entry hall, and needs to read its dimensions
on the model. On 2026-11-03 the owner added the phase R13 to the plan, after P0.75, and on 2026-11-04 answered four
questions: the thickness is the distance of a live extrude, else the smallest extent of the box; the overall size is
of the visible bodies or, as an option, of each live body; the dimensions are lines with labels in the scene; and the
toggle is a state of the client, off at each start. ADR-0056 holds them.

## Goal

A person turns on "Dimensions" in the panel by a tap or a click, and reads the overall size and the size and the
thickness of a selected board on the model.

## Scope (in)

1. `Dimensions` in `3DEngine.Core/Measuring/`: the three dimensions of a box from the side of the eye, the format, the
   thickness and the union of the boxes.
2. `DimensionOverlay` in the desktop: the lines and the labels in pixels, and the text of the size.
3. The toggles "Dimensions" and "Hidden too" in the panel, the drawing, and the line of the size of the selection.
4. A unit test, a test of the panel with no GPU, and an image test.

## Scope (out)

- A dimension between two points, an angle, a dimension of a sketch item, a saved dimension, technical drawing.
- The far plane of the camera (register entry R-0051).

## Acceptance criteria

- [x] The toggles are off at each start, take a tap and a click, and are inside the panel with a long list of bodies.
- [x] The overall box gives three dimension lines with labels in the unit of the document, and the selected body three
      more in the colour of the selection, with its size and its thickness in the panel.
- [x] An image test of a shelf shows the lines, and the owner gets its picture.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r13-dimension-overlay) · v0.179

## Method

Mechanical: unit tests of the geometry, tests of the panel by tap and by click, an image test with counts of pixels.
Judgement:  the place of each line on the box, the place of the toggles in the panel, and the colour.
Weakest:    no one opened the desktop window; the image test renders the same code offscreen.

## Progress

- 2026-11-04: opened with the four answers of the owner. The geometry, the overlay, the toggles and the tests.
- 2026-11-04: ten injections each fail a test; the picture of the image test goes to the owner; closed at v0.179.
- 2026-11-06: TASK-0222 moved `3DEngine/DimensionOverlay.cs` into `3DEngine.App/` (ADR-0072), so the
  create list names the new path. This task made the file at the old path.
