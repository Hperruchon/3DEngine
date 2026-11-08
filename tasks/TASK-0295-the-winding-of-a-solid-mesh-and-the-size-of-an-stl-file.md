---
id: 0295
title: A solid mesh with inward triangles is refused, and a file too large for the store has its own code
status: Ready
phase: P0.124
opened: 2026-11-08
depends-on: [0294]
governed-by: [0007, 0014, 0018, 0019, 0025, 0029, 0036, 0072, 0075, 0076, 0077, 0080, 0081, 0082, 0083, 0085, 0086, 0089]
writes:
  create:
    - tasks/TASK-0295-the-winding-of-a-solid-mesh-and-the-size-of-an-stl-file.md
  modify:
    - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
    - Engine.Tests/Commands/CreateConeCommandTests.cs
    - 3DEngine.App/StlOpening.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.ImageTests/StlOpenTests.cs
    - docs/diagnostics.md
    - docs/adr/0083-the-mesh-body-and-the-stl-import.md
    - docs/adr/0089-the-cone-the-sphere-and-the-torus-are-closed-meshes.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0295 — A solid mesh with inward triangles is refused, and a file too large for the store has its own code

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-08 found two medium findings. E186: `CreateSolid` takes a mesh with inward triangles, and
the body of Manifold then has a negative volume. E190: since TASK-0281 the client gives `E-STL-TOO-LARGE` for a file of
more than 16 MiB, while `docs/diagnostics.md` gives the code the meaning "more triangles than the limit". They come after
the next product phase (R16i, TASK-0292).

## Goal

E186 and E190 are fixed, each with a test.

## Scope (in)

1. E186: `CreateSolid` refuses a mesh whose volume is negative, and a test compares the volume of Manifold with the
   volume of the kernel for the cone.
2. E190: a new code, registered in `docs/diagnostics.md`, for a file too large for the store and for a size that SDL
   does not know; `E-STL-TOO-LARGE` keeps its meaning.

## Scope (out)

- The low findings (TASK-0296).

## Acceptance criteria

- [ ] Each finding has a test, an injection or a reading that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: a test, an injection or a reading for each finding, before the change.
Judgement:  to be written.
Weakest:    to be written at the close.

## Progress
