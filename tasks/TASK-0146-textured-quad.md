---
id: 0146
title: The scene draws an image on a quad with the depth of the bodies
status: Active
phase: V1a
opened: 2026-10-31
depends-on: [0145]
governed-by: [0007, 0009, 0017, 0019, 0022, 0023, 0024, 0025, 0029, 0043]
writes:
  create:
    - tasks/TASK-0146-textured-quad.md
    - docs/adr/0043-textured-quad.md
    - 3DEngine.Core/Rendering/ImageQuad.cs
    - 3DEngine.Vulkan/ImageRenderer.cs
    - 3DEngine.Vulkan/Shaders/image.vert
    - 3DEngine.Vulkan/Shaders/image.vert.spv
    - 3DEngine.Vulkan/Shaders/image.frag
    - 3DEngine.Vulkan/Shaders/image.frag.spv
    - 3DEngine.Tests/ImageQuadTests.cs
    - 3DEngine.ImageTests/ImageQuadImageTests.cs
  modify:
    - 3DEngine.Vulkan/Shaders/shaders.lock
    - 3DEngine/SceneRenderer.cs
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

# TASK-0146 — The scene draws an image on a quad with the depth of the bodies

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

V0 merged at v0.147, and the owner told the session on 2026-10-31 to start V1: "A content-addressed asset store. A
textured quad." V1 has two slices: V1a, the quad on the render side (ADR-0043), and V1b, the store of the assets in the
engine. V1a comes first, because its picture needs the owner while V1b goes on.

## Goal

The scene draws an image on a parallelogram of the scene, and a body in front of it hides it as a body hides a body.

## Scope (in)

1. `ImageQuad` and its model-view matrix relative to the eye (ADR-0043 items 1 and 2).
2. `ImageRenderer`, the shaders `image.vert` and `image.frag`, and the draw of the quads in `SceneRenderer` after the
   bodies (items 3 and 4).

## Scope (out)

- The store of the assets, a decoder of image files, and a quad in the desktop window (V1b and V2).
- A transparent image, a mip map and the release of an image.

## Acceptance criteria

- [ ] A test with no GPU gives the matrix of each corner, far from the origin too, and the state of the pipeline.
- [ ] A picture on a GPU shows the image behind a box and a small image in front of it, with no message of the
      validation layer, and the owner accepts it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-31: the record, the quad, the renderer and the shaders (compiled with shaderc v2023.8; the other SPIR-V
  files did not change). 9 new test cases: 8 in `3DEngine.Tests` and 1 image test. The first image test missed an
  image with no depth test, because its point behind the box fell in the blend of red and blue; it compares two
  points of the face of the box now. 5 injections caught.
- 2026-10-31: merged main (V1b at v0.148); the conflicts were the rows of the ADR index, the roadmap and the test floor.
