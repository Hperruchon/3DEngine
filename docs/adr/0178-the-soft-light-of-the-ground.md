---
id: 0178
title: The ground of the scene has the soft light of the render, lighter at the middle and darker at the corners
status: Accepted
topic: Clients, rendering
date: 2026-11-20
supersedes: []
superseded-by: []
amends: ['0108']
amended-by: []
affects:
  - 3DEngine.Vulkan/GroundPipeline.cs
  - 3DEngine.Vulkan/Shaders/ground.vert
  - 3DEngine.Vulkan/Shaders/ground.frag
  - 3DEngine.App/SceneRenderer.cs
  - 3DEngine.App/Ui/Theme.cs
  - 3DEngine.ImageTests/GroundImageTests.cs
  - 3DEngine.ImageTests/GroundPixels.cs
  - 3DEngine.ImageTests/ThemeTests.cs
  - 3DEngine.ImageTests/BodyFocusTests.cs
  - 3DEngine.ImageTests/FirstDemonstrationImageTests.cs
  - 3DEngine.ImageTests/ImageQuadImageTests.cs
  - 3DEngine.ImageTests/PlacedImageImageTests.cs
enforced-by: 3DEngine.ImageTests/GroundImageTests.cs
---

# ADR-0178 — The ground of the scene has the soft light of the render, lighter at the middle and darker at the corners

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0108 item 1 gave the window the almost black ground of the render "mix": one colour, `#070d14`, for the whole scene. A
sample of the pixels of the render Mix 1 at 1440 by 900 gave a ground from `#03060b` at the corners to `#0b1b2a` at the
middle. ADR-0108 kept the soft light at the middle for later: the user interface draws over the scene, so a light that it
draws covers the body. It named the pass of the edges (R18b) or a later slice for the light. R18b (ADR-0109) did not add it,
and no later row of the roadmap held it.

On 2026-11-20 the owner asked if the window has the gradient of the render, and then said "Yes, add it". Row R18p holds this
slice.

## Decision

1. **The colour of the ground (amends ADR-0108 item 1).** For each pixel, s is the square distance from the centre of the
   pixel to the middle of the target, divided by the square distance from the middle to a corner. So s is 0 at the middle and
   1 at each corner. The colour is the colour of a corner plus (1 − s) times the step from the colour of a corner to the colour
   of the middle. The colour of a corner is `#03060b`, as in the render. The colour of the middle is `#0a1a28`, one step under
   the sample `#0b1b2a`: the sample has a relative luminance of 0.0102, and the test of ADR-0108 item 1 for "almost black" asks
   for less than 0.01. The middle has a luminance of 0.0096. Each colour of a text keeps 4.5:1 and each colour of a mark keeps
   3:1 on the middle, which is the lightest point of the ground. The computation uses + − × ÷ only. `Theme.GroundAt` gives the
   same colour on the CPU for a test.
2. **The draw.** A pipeline of the ground draws one triangle that covers the target, as the first draw after the clear of the
   render pass. It has no vertex buffer, no depth test, no depth write and no blend. So each body, image, edge line and
   overlay draws over the light, and the depth of the clear stays. The window, the phone and the image tests use the one
   `SceneRenderer`, so each of them shows the light. The middle of the light is the middle of the target.
3. **The clear stays.** `Theme.Clear` (`#070d14`) stays the colour of the clear under the ground and of the plate under a text
   over the scene (ADR-0108 item 2). The ground of a window of the panel stays `#0b1219` at alpha 0.94.
4. **The tests read the ground at each pixel.** A test that asks if a pixel is the ground compares it with `Theme.GroundAt` at
   that pixel, within 1 or 2 for each channel, through `GroundPixels.IsGround`. Before this change each such test compared a
   pixel with the one colour of the clear. With the light, a test of the form "a pixel that is not the clear is drawn" passes
   for each pixel, so each such test changes in this slice.

## Consequences

**Good:** the scene has the soft light of the render behind the body, and the corners are darker than before, so the controls
at the corners read better. A test holds each pixel of an empty scene against the computation, at a landscape and a portrait
size.

**Bad:** a target of 8 bits gives about 30 steps of blue from a corner to the middle, so a step is on average about 25 pixels wide at 1280
by 720. A person can see the steps on some displays; a dither can remove them later, and a test of an exact pixel then
changes. A plate at the middle of the scene is a little darker than the ground around it. The middle of the light is the middle
of the target, not the middle of the free part of the scene beside the panel.

## Alternatives considered

- **A light that the user interface draws on the background list.** ImGui draws after the scene, so the light covers each
  body, as ADR-0108 item 1 says.
- **A ground that follows the window, an ellipse with the shape of the target.** On a portrait phone the light becomes a tall
  ellipse. A circle in pixels keeps the shape of the render at each size.
- **The sample of the render, `#0b1b2a`, at the middle.** It is not "almost black" by the test of ADR-0108 item 1, and the
  contrast of the colours on it is less.

## History

- 2026-11-20 · TASK-0423 · The colour of the ground, the pipeline of the ground, and the tests that read it (phase R18p).
