---
id: 0023
title: The camera, the depth and the draw list of the renderer
status: Amended
topic: Rendering
date: 2026-10-12
supersedes: []
superseded-by: []
amends: []
amended-by: ['0024']
affects:
  - 3DEngine.Core/**
  - 3DEngine.Vulkan/**
enforced-by: 3DEngine.Tests/CameraTests.cs (TASK-0070 creates it)
---

# ADR-0023 — The camera, the depth and the draw list of the renderer

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase R4 asked for "a camera with depth, flat scene arrays, and each draw through an indirect
interface". R3 (ADR-0022) draws one mesh with a fixed view, culled back faces and no depth buffer. Four
choices of R4 change the render kernel `3DEngine.Core` (ADR-0009) and the Vulkan layer (ADR-0017), and a
later phase builds on each one:

1. The depth buffer: its format, its range and its comparison. A CAD scene holds small parts and large
   distances, and the precision of a depth buffer is not even over its range.
2. The camera: whether the person moves it in R4, or only in R7, which brings the input layer.
3. The scene: flat arrays that the renderer reads, or the object graph `Scene` and `Entity` that exists.
4. The draws: GPU indirect draws from a buffer of commands, or a code layer with direct draws.

On 2026-10-12 the owner answered: reversed depth (1A), an orbit camera with the mouse now (2B), the
object graph (3B), and a code layer (4B), with the note that indirect draws could also fit later.

## Decision

1. **Depth.** The render pass of the swapchain has a depth attachment of `D32_SFLOAT`, cleared to 0. The
   projection maps the near plane to depth 1 and the far plane to depth 0, and the pipeline keeps a
   fragment whose depth is greater (reversed depth). A float holds the most precision near 0, which
   reversed depth gives to the far range.
2. **Camera.** The camera of `3DEngine.Core` is an orbit camera: a target in double precision, a
   distance, a yaw and a pitch, which are render state (ADR-0009 §3). A left drag of the mouse orbits it
   and the wheel zooms it. The renderer computes each model matrix relative to the eye in double
   precision, and only then narrows it to float. Each entity has an origin in double precision
   (ADR-0024). R7 replaces the reading of the mouse with the intent layer.
3. **Scene.** The renderer walks the object graph `Scene` and `Entity` at each frame. No flat array
   exists for it.
4. **Draws.** The host fills a draw list (`IDrawList` of `3DEngine.Core`) with one record for each
   entity that has a mesh: the mesh and its matrix relative to the eye. The Vulkan layer executes the
   list with one direct indexed draw for each record. A later phase can put an executor with indirect
   draws behind the same list, and no caller changes.

## Consequences

**Good:** The depth buffer keeps its precision at a distance. The person can look at a body from each
side in R4. The scene code that exists stays, and the draw list keeps the choice of indirect draws open.
The relative matrices keep the detail of a body far from the origin of the engine, as R5 asks.

**Bad:** A walk of the graph and one draw for each entity cost more for a large scene than flat arrays
and indirect draws; a later phase measures it when a scene is large. The reading of the mouse in the
host is replaced in R7. The roadmap line of R4 no longer names flat arrays or indirect draws.

**Next:** TASK-0070 applies this decision.

## History

- 2026-10-12 · TASK-0070 · The record is written with the answers of the owner on R4.
- 2026-10-12 · TASK-0071 · ADR-0024 amends item 2: each entity has an origin in double precision.
