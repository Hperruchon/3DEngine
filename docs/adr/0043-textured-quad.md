---
id: 0043
title: An image on a flat rectangle of the scene
status: Accepted
topic: Rendering, vision
date: 2026-10-31
supersedes: []
superseded-by: []
amends: []
amended-by: []
affects:
  - 3DEngine.Core/Rendering/ImageQuad.cs
  - 3DEngine.Vulkan/ImageRenderer.cs
  - 3DEngine.Vulkan/Shaders/image.*
  - 3DEngine/SceneRenderer.cs
enforced-by: 3DEngine.Tests/ImageQuadTests.cs
---

# ADR-0043 — An image on a flat rectangle of the scene

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The roadmap row V1 is "A content-addressed asset store. A textured quad." V2 places an image on a datum, and V3 traces
a contour of it, so the viewport must draw an image on a rectangle in the space of the scene, with the depth of the
bodies. The renderer has one sampled texture today, the font atlas of the user interface (ADR-0027 item 3), and no
pipeline of the scene samples a texture.

The owner told the session on 2026-10-31 to start V1, and to use its recommendation for each question, so this record
is Accepted. V1 has two slices: V1a, this record, on the render side only; V1b, the store of the assets in the engine.

## Decision

1. **The quad.** `ImageQuad` in `3DEngine.Core/Rendering` is an image on a parallelogram of the scene: the identifier of
   the image, the corner at the bottom left of the image, and the two edges, to the right and up, in units of the
   scene. It is render state, not design truth: V2 makes it from the Document.
2. **The matrix.** The quad gives the model-view matrix that takes the unit square to it, relative to the eye of the
   camera: the corner moves by minus the eye in double precision before it narrows to float, as `SceneWalker` does for
   an entity (ADR-0023 item 2).
3. **The pipeline.** `ImageRenderer` in `3DEngine.Vulkan` draws each quad in subpass 0 of the render pass, after the
   bodies. Its shaders `image.vert` and `image.frag` read a corner of the unit square and one combined image sampler.
   It tests the depth with `Greater` and writes it, as the mesh pipeline does, so a body in front hides the image and
   the image hides a body behind it. It culls no face, because a person can look at an image from behind. The image is
   opaque: the fragment takes the colour of the texture with an alpha of 1. A sampler with linear filters clamps each
   coordinate to the edge.
4. **The images.** The renderer uploads each image one time, under its identifier, through the texture of ADR-0027
   (`UiTexture`, R8G8B8A8 in memory of the device). An identifier names one content (V1b gives the SHA-256 of the
   bytes), so the renderer never replaces an image: a second upload under the same identifier does nothing. It holds
   at most 16 images. A quad whose image the renderer does not hold is not drawn.
5. **No engine change.** No `Engine.*` project changes in V1a, and nothing enters the Document or the log.

## Consequences

- A transparent image, a mip map and the release of an image that no quad uses wait for a phase that needs them.
- The name `UiTexture` now also covers an image of the scene; a rename is a later change of the Vulkan layer.

## History

- 2026-10-31 · TASK-0146 · The record, Accepted under the standing answer of the owner of 2026-10-31.
