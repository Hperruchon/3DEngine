---
id: 0422
title: The ground of the scene has the soft light of the render, lighter at the middle and darker at the corners
status: Active
phase: R18p
opened: 2026-11-20
depends-on: [0332, 0333]
governed-by: [0007, 0017, 0019, 0022, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0043, 0072, 0078, 0101, 0108, 0109, 0122, 0129, 0166, 0177]
writes:
  create:
    - tasks/TASK-0422-the-soft-light-of-the-ground.md
    - docs/adr/0177-the-soft-light-of-the-ground.md
    - 3DEngine.Vulkan/GroundPipeline.cs
    - 3DEngine.Vulkan/Shaders/ground.vert
    - 3DEngine.Vulkan/Shaders/ground.frag
    - 3DEngine.Vulkan/Shaders/ground.vert.spv
    - 3DEngine.Vulkan/Shaders/ground.frag.spv
    - 3DEngine.ImageTests/GroundImageTests.cs
    - 3DEngine.ImageTests/GroundPixels.cs
  modify:
    - 3DEngine.Vulkan/Shaders/shaders.lock
    - 3DEngine.App/SceneRenderer.cs
    - 3DEngine.App/Ui/Theme.cs
    - 3DEngine.ImageTests/ThemeTests.cs
    - 3DEngine.ImageTests/BodyFocusTests.cs
    - 3DEngine.ImageTests/BodyRowsImageTests.cs
    - 3DEngine.ImageTests/EdgeValueImageTests.cs
    - 3DEngine.ImageTests/FirstDemonstrationImageTests.cs
    - 3DEngine.ImageTests/FixedStatusImageTests.cs
    - 3DEngine.ImageTests/HomeImageTests.cs
    - 3DEngine.ImageTests/ImageQuadImageTests.cs
    - 3DEngine.ImageTests/PanelImageTests.cs
    - 3DEngine.ImageTests/PlacedImageImageTests.cs
    - 3DEngine.ImageTests/RingImageTests.cs
    - 3DEngine.ImageTests/SketchLiftImageTests.cs
    - docs/adr/0108-the-look-of-the-render.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.Android/**
---

# TASK-0422 — The ground of the scene has the soft light of the render, lighter at the middle and darker at the corners

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0108 item 1 (R18a) gave the scene one almost black colour, `#070d14`, and kept the soft light at the middle of the render
"mix" for later. No later row held it. On 2026-11-20 the owner asked if the window has the gradient of the render, and then
said "Yes, add it". Row R18p holds this slice, and ADR-0177 records it.

## Goal

The ground of the scene goes from a dark colour at the corners to a lighter colour at the middle, as in the render, and each
body lies over it. A person sees it in the window and on the phone.

## Scope (in)

1. The colour of the ground at each pixel, on the GPU and on the CPU for a test (ADR-0177 item 1).
2. A pipeline of the ground, the first draw of the render pass of the scene (item 2).
3. Each image test that compared a pixel with the one colour of the clear compares it with the ground at that pixel (item 4).

## Scope (out)

- A dither of the steps of 8 bits, and a light at the middle of the free part of the scene.
- The soft shadow under the selected body (ADR-0109 item 6).
- A change of `Engine.Core/**`, of `Engine.Contracts/**` or of the phone project.

## Acceptance criteria

- [ ] With no body, each pixel of a picture at 1280 by 720 and at 360 by 780 is `Theme.GroundAt` at that pixel, within 1.
- [ ] A body at the middle lies over the light, and a corner stays the ground.
- [ ] Each point of the ground is almost black, and each colour keeps its contrast on the middle of the ground.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes on the branch.

## Outcome

Status: Active

## Method

- Read ADR-0108 (item 1 and its context), ADR-0109 and ADR-0022 (the lock of the shaders) before the code.
- Compiled the shaders with `node 3DEngine.Vulkan/Shaders/compile.js` (shaderc v2023.8, the compiler of the lock). The SPIR-V
  of each other shader did not change.
- Ran `GroundImageTests` and `ThemeTests` after each change, then six injections with `inject.js` in the scratchpad; each file
  came back byte for byte (sha256).

## Progress

- 2026-11-20: opened under the words of the owner "Yes, add it", branch ground-light from `main` at `d7936b4`. P0.167
  (TASK-0416) holds v0.405 on its branch, so this slice takes v0.406.
- 2026-11-20: the work: the colour, the pipeline, three tests in `GroundImageTests`, and eleven files of image tests that
  read the ground at each pixel. Six injections each made a named test fail: no draw of the ground, the ground after the bodies, a
  computation on the CPU without the height, a middle that is not almost black, the two colours swapped in the push block, and
  the old comparison with the one clear colour (nine tests fail).
