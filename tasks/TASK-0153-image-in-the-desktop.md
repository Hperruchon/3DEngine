---
id: 0153
title: The desktop places a PNG file on a datum, draws it, and calibrates it with two taps
status: Done
phase: V2b
opened: 2026-10-31
depends-on: [0152]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0046]
writes:
  create:
    - tasks/TASK-0153-image-in-the-desktop.md
    - docs/adr/0046-image-in-the-desktop.md
    - 3DEngine.Core/Imaging/PngReader.cs
    - 3DEngine/SessionImages.cs
    - 3DEngine/ImagePlacement.cs
    - 3DEngine/ImageActions.cs
    - 3DEngine.Tests/PngReaderTests.cs
    - 3DEngine.ImageTests/ImageActionsTests.cs
    - 3DEngine.ImageTests/PlacedImageImageTests.cs
  modify:
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine/DesktopEngineHost.cs
    - 3DEngine/DesktopSurface.cs
    - 3DEngine/Ui/DesktopUi.cs
    - 3DEngine/Ui/PanelAction.cs
    - docs/adr/README.md
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

# TASK-0153 — The desktop places a PNG file on a datum, draws it, and calibrates it with two taps

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

V2a (TASK-0152, v0.151) put the record of an image on a datum in the Document (ADR-0045). The standing answer Q3 A of
the codebase review of 2026-10-31 (c) says that the client decodes the image. The owner told the session on 2026-10-31
to work alone until 2026-11-01 10:00 and not to wait for the visual check: the session judges the picture, merges
after a green pipeline, and sends the picture.

## Goal

A person places a PNG file on an origin plane from the window of the images, sees it on its datum with the depth of the
bodies, and calibrates it with two taps on it and a real distance.

## Scope (in)

1. `PngReader`: 8 bits for each channel, the five colour types, the five filters, no interlace, at most 4096 pixels on
   a side (ADR-0046 item 1).
2. `SessionImages` and `ImagePlacement`: the live images, one upload for each asset, and the quad of each image
   (item 2).
3. The window "Images", `ImageActions` and the host: the place of a file, and the calibration by two taps (items 3
   and 4).

## Scope (out)

- A mark of the taps, a move, a rotation and a removal of an image, and a format other than PNG.

## Acceptance criteria

- [x] The reader has a test for each colour type, each filter and each refusal.
- [x] A test with no GPU places a file, calibrates the image with two taps, and follows the projection through undo.
- [x] A picture on a GPU shows a placed and calibrated PNG file on its datum, with no message of the validation layer.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch v2b-image-in-the-desktop) · v0.152

## Method

Mechanical: each colour type, filter and refusal of the reader, the place, the taps and the projection with no GPU, the
            picture on a GPU, the live window over HTTP, and one injection for each rule.
Judgement:  an own reader and no package; at most 4096 pixels on a side; the window closed at the start and to the right
            of the panel.
Weakest:    no test drives the host: the taps of a calibration in the live window, and the buttons of the window of the
            images, ran only through ImageActions and the panel code. The owner's visual check comes after the merge.

## Progress

- 2026-10-31: ADR-0046, the reader, the projection, the window of the images and the host. 24 new test cases: 20 in
  `3DEngine.Tests` and 4 in `3DEngine.ImageTests`, and 1 image test. 10 injections; the first mutant of the Paeth
  predictor changed no result, and the second one passed until a test with a tie of up and the corner. The live window
  drew a PNG file of an independent encoder; under the panel the window of the images hid Show all, so it is to the
  right of the panel and closed at the start.
- 2026-10-31: closed at v0.152.
