---
id: 0109
title: The edges of a body are lines, and the selected body is transparent with dashed hidden edges
status: Accepted
topic: Rendering, clients
date: 2026-11-10
supersedes: []
superseded-by: []
amends: ['0029']
amended-by: []
affects:
  - 3DEngine.App/SceneRenderer.cs
  - 3DEngine.App/SessionBodies.cs
  - 3DEngine.Vulkan/EdgeLines.cs
  - 3DEngine.Vulkan/EdgePipeline.cs
  - 3DEngine.Vulkan/GpuMesh.cs
  - 3DEngine.Vulkan/RenderPipeline.cs
  - 3DEngine.Vulkan/Shaders/edge.vert
  - 3DEngine.Vulkan/Shaders/edge.frag
  - 3DEngine.Vulkan/Shaders/highlight.frag
  - 3DEngine.ImageTests/BodyEdgesImageTests.cs
enforced-by: 3DEngine.ImageTests/BodyEdgesImageTests.cs
---

# ADR-0109 — The edges of a body are lines, and the selected body is transparent with dashed hidden edges

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-10 the owner accepted the proposal `docs/proposals/2026-11-10-gaps-to-the-render.md` (each recommended
answer). Gap G2 and slice S2: in the renders Mix 1 to Mix 4 the edges of a body are bright lines, the selected body is
transparent with an orange outline, its hidden edges are dashed, and a soft shadow is on the ground under it. At v0.318 the
window draws a solid shaded body with no edge line, and the highlight of ADR-0029 item 2 tints the selected body over its
shaded draw.

The renderer has the triangles of each body (the query `GetTessellation`, ADR-0024). The edges of `GetFaces` (ADR-0077)
are in the client for the values, but the renderer does not read them. The phone uses the same renderer, and a Vulkan
device can have no wide lines.

## Decision

1. **The source of the edges.** The edges are the feature edges of the triangles of the body: an edge of a triangle with
   one triangle only, with more than two, or with two whose normals differ by more than 30 degrees. Two vertices at the
   same position are one vertex. So the twelve edges of a box show, and the seams between the facets of a cylinder do not.
   The renderer finds them one time, when the mesh of the body goes to the GPU. The lines are presentation, as the
   tessellation is (ADR-0007, determinism rule 6): the Document and the log hold none. The limit is a constant cosine, so
   no transcendental function runs.
2. **The lines.** Each live, visible body draws its edges as lines of a fixed width in pixels: 1.5 pixels, near white,
   with a soft border of one pixel. Each edge is two triangles, and the shader `edge.vert` gives them their width on the
   screen, so the pass needs no wide lines. A line comes nearer to the eye by 0.2 percent of its depth, so it wins the depth
   test against the faces of its body. A line writes no depth.
3. **The selected body (amends ADR-0029 item 2).** The selected body has no opaque draw. Its depth goes first with no
   colour, then the highlight tints its visible faces as a transparent orange fill (alpha 0.25) over what is behind it. Its
   hidden edges, behind the stored depth, are dashed lines of 2 pixels in the colour of the selection (6 pixels on, 6
   pixels off). Its visible edges are solid lines of 2 pixels in the same colour. The other bodies, their edges and the
   images draw before the selected body, so a body behind it shows through it.
4. **The order of a frame.** Each body but the selected one, the images, the edges of those bodies, then the depth, the
   fill, the hidden edges and the visible edges of the selected body, then the overlay.
5. **The cost.** An edge takes 192 bytes on the GPU. A body with more than 100 000 feature edges draws no lines, so an
   imported scan with many creases does not take more than about 18 MiB. On the computer of the owner (2026-11-10), a box
   and a cylinder at 1440 by 900 with the box selected gave a median frame of 22.2 ms before and 22.2 ms after, with the
   copy to the CPU; the difference is below the noise. The search of the edges of a mesh of one million triangles takes
   about 0.5 s, one time, at the upload.
6. **The shadow.** The soft shadow under the selected body waits. The scene has no ground plane to receive it, and the
   ground of the render comes with R18a (ADR-0108). A later slice can add it after R18a.

## Consequences

**Good:** the body shows its edges as in the render, also on a phone, with no feature of the device that the S23 Ultra can
lack. A test reads the pixels on each edge of a box, so a pass that draws nothing fails the build.

**Bad:** the edges are the creases of the triangles, not the edges of the faces of the kernel: a fillet with a soft
crease shows no edge, and a coarse curve can show a seam. A silhouette of a curved body (the outline of a sphere) is not
an edge, so it has no line. The selected body loses its shade, so its form shows only through its edges and its fill. The
frame time on the phone is not measured; register entry R-0061 holds the question.

## History

- 2026-11-10 · TASK-0333 · The record, from slice S2 of the proposal of 2026-11-10 and the answers of the owner.
