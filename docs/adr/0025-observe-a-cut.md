---
id: 0025
title: The first demonstration, a device with no window, and an image test
status: Amended
topic: Rendering, boundary
date: 2026-10-14
supersedes: []
superseded-by: []
amends: ['0009', '0017', '0022']
amended-by: ['0029']
affects:
  - 3DEngine.ImageTests/**
  - scripts/first-demonstration.js
enforced-by: 3DEngine.ImageTests/FirstDemonstrationImageTests.cs (TASK-0076 creates it)
---

# ADR-0025 — The first demonstration, a device with no window, and an image test

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase R6 asks a person to observe a cut, and asks for an image test with no window. It completes
the first objective of `docs/CHARTER.md`. The codebase review of 2026-10-13, section 5, found four
conditions:

1. The demonstration of the probes moved the second box by (5, 0, 0). The cut body was a box of
   5 × 10 × 10, and a person cannot tell it from a box made directly.
2. The camera did not frame the bodies. The cut body covered about 2.7 % of the picture.
3. No Vulkan device can exist with no window: `GraphicsDevice` takes a `Window`, needs present support
   and builds the swapchain, and `RenderPipeline` uses the render pass of the swapchain.
4. No runner of the pipeline has a GPU. The package `mesa-vulkan-drivers` of Ubuntu gives lavapipe, a
   Vulkan driver on the CPU. The images of Windows and macOS give no usable Vulkan device.

On 2026-10-14 the owner answered question Q2 of that review with option A for each of six decisions
(D1 to D6).

## Decision

1. **The demonstration (D1).** The first demonstration sends four commands: `CreateBox` of 10 × 10 × 10
   (box A, centred at the origin), `CreateBox` of 10 × 10 × 10 (box B), `Translate` of box B by
   (5, 5, 5), and `Subtract` of box B from box A. The cut removes the corner cube from (0, 0, 0) to
   (5, 5, 5), so the body shows a notch. Its tessellation has 14 vertices and 24 triangles.
2. **A device with no window.** `GraphicsDevice` is the core of the device: the instance, the physical
   device, the device and the graphics queue. It takes a window or no window. `Presenter` holds the
   presentation to a window: the surface, the swapchain, the resources of each frame, the acquire and
   the present. `OffscreenTarget` holds a colour image and a depth image, its own render pass, and a copy
   of the colour image to memory that the host reads. `RenderPipeline` takes the render pass of its
   target. A device with no window needs no SDL call and no present support.
3. **The image test (D2).** The project `3DEngine.ImageTests` holds the image test. It may reference the
   projects that a host that draws references, and the host `3DEngine`, whose internals it reads; it
   also tests `SessionBodies`. The dependency gate holds this rule in a class of its own. `Engine.Tests`
   and `3DEngine.Tests` keep their rules (ADR-0022 item 3).
4. **The runners (D3).** The pipeline installs lavapipe and the validation layers on `ubuntu-latest`, and
   the image test must run there: with no Vulkan device it fails. On `windows-latest` and `macos-latest`
   it skips, and the register holds the reason. On the computer of a person it runs when a Vulkan device
   exists, and skips with a reason when none exists.
5. **The comparison (D4).** The test compares the picture by structure, with no reference picture:
   - the tessellation of the cut body has 14 vertices and 24 triangles;
   - each pixel outside the silhouette that the CPU projects from the tessellation has the colour of
     the clear, and each pixel inside it has another colour; the pixels within 2 pixels of an edge of
     the silhouette are not compared;
   - the centre of each of the three faces of the notch has the shade that the shader gives to the
     normal of that face, and the three shades differ.

   The test enables the validation layer with synchronization validation, and fails on each message.
   It writes the picture as a PNG file, and the pipeline keeps that file when the test fails.
6. **The frame of the camera (D5).** When the set of visible bodies goes from empty to not empty, the camera
   frames their box: the target goes to the centre of the box, and the distance puts the sphere around the
   box inside the smaller angle of the field of view. The yaw and the pitch do not change. A later change of
   a body does not move the camera (ADR-0029 item 8).
7. **The script (D6).** `scripts/first-demonstration.js` sends the four commands of item 1 over HTTP to
   the desktop, at `http://127.0.0.1:5180` or at the address of its argument.

This record amends ADR-0022 items 2 and 3, ADR-0017 items 1, 3 and 7, and ADR-0009 section 2.

## Consequences

**Good:** A person sees the notch, and a runner of the pipeline holds the same result with no GPU. The
structural comparison does not depend on the driver: lavapipe and a GPU give the same structure, and no
picture is stored in the repository. The device with no window also gives a later phase a picture for
a review.

**Bad:** The test does not run on Windows and macOS in the pipeline; a defect of those platforms in the
Vulkan layer stays out of view (R-0022). The structural comparison does not see a small defect of the
shading inside a face. The frame of the camera resets the zoom of the person at each change of a body.
The image test project references the host, so a change of the host can break it.

**Next:** TASK-0076 applies this decision.

## History

- 2026-10-14 · TASK-0076 · The record is written with the answers of the owner to question Q2 of the
  codebase review of 2026-10-13.
- 2026-10-28 · TASK-0086 · ADR-0029 amends item 6: the camera frames the visible bodies only when their set
  goes from empty to not empty (decision KD6 of the owner).
