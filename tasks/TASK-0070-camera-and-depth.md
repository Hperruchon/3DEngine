---
id: 0070
title: The desktop draws a scene with depth through an orbit camera and a draw list
status: Active
phase: R4
opened: 2026-10-12
depends-on: [0069]
governed-by: [0007, 0009, 0017, 0019, 0022, 0023]
writes:
  create:
    - tasks/TASK-0070-camera-and-depth.md
    - docs/adr/0023-camera-depth-and-draw-list.md
  modify:
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - 3DEngine/**
    - 3DEngine.Tests/**
    - docs/adr/README.md
    - docs/INDEX.md
    - docs/glossary.md
    - docs/roadmap.md
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - Engine.Tests/**
    - eng/**
---

# TASK-0070 — The desktop draws a scene with depth through an orbit camera and a draw list

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase R4: "A camera with depth." R3 (TASK-0069) draws one mesh with a fixed view and no depth
buffer. On 2026-10-12 the owner answered four questions on R4; ADR-0023 records the answers: reversed
depth, an orbit camera with the mouse now, the object graph `Scene` and `Entity`, and a draw list with
direct draws.

## Goal

The desktop draws a scene of several entities with a depth buffer, through an orbit camera that the
mouse moves, and each draw goes through the draw list.

## Scope (in)

1. ADR-0023 and its index row; the roadmap line of R4 says what the owner decided.
2. A depth attachment of `D32_SFLOAT` in the render pass, cleared to 0, and a pipeline that keeps the
   greater depth.
3. The orbit camera of `3DEngine.Core`, with a reversed perspective projection, and the matrices of each
   model relative to the eye in double precision.
4. `IDrawList` in `3DEngine.Core`, the walk of the scene that fills it, and its executor in
   `3DEngine.Vulkan` with one direct indexed draw for each record.
5. The desktop: a scene with two overlapping copies of the cut body of the first demonstration; a left
   drag orbits and the wheel zooms.
6. Tests with no GPU: the projection, the orbit, the relative matrices, and the walk of the scene.

## Scope (out)

- No geometry from the engine (R5), no image test (R6), no input layer (R7).
- No indirect draw and no flat array (ADR-0023 items 3 and 4).

## Acceptance criteria

- [ ] A picture of the window shows the two bodies with the correct overlap, and a second picture after
      an orbit shows them from another side. The owner accepts the pictures.
- [ ] A run of the desktop gives no validation message.
- [ ] Each new test fails before its correction, or an injection shows that it can fail.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The pictures.** The owner told me to run the desktop and show a picture of the window
  (TASK-0069). An orbit for the second picture can come from a drag of the mouse that a script sends.
- **The size.** About 5 to 7 evenings.

## Progress

- 2026-10-12: the task is open, with ADR-0023, after the answers of the owner on R4.
