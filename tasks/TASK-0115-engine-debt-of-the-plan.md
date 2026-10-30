---
id: 0115
title: The native wrapper frees each buffer with its allocator, and the binder and the endpoint read numbers and versions as the schema says
status: Ready
phase: P0.46
opened: 2026-10-30
depends-on: [0114]
governed-by: [0004, 0014, 0016, 0018, 0019, 0036]
writes:
  create:
    - tasks/TASK-0115-engine-debt-of-the-plan.md
  modify:
    - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
    - Engine.Core/Hosting/ParameterBinder.cs
    - Engine.Api.Http/Endpoints/CommandsEndpoint.cs
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - 3DEngine.Vulkan/**
    - docs/adr/**
---

# TASK-0115 — The native wrapper frees each buffer with its allocator, and the binder and the endpoint read numbers and versions as the schema says

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The plan of 2026-10-30 (answer Q4 A) gives this task the open findings E11, E14 and E16 of the reviews, and the
test of the first click of R-0043. E14 runs at each extrude and at each rebuild since K0a and K0b.

## Goal

The Manifold wrapper frees each buffer with the function of its allocator, and E11 and E16 are closed, and the
first click into the desktop window has a test.

## Scope (in)

1. E14: each buffer from `Marshal.AllocHGlobal` is freed with `Marshal.FreeHGlobal`, and each object of Manifold
   with its own delete function.
2. E11 and E16, as the reviews give them. The task reads their text in `docs/reviews/` first.
3. R-0043: a test of the first click into the desktop window, and the exit of the register entry.

## Scope (out)

- A change of the native package.

## Acceptance criteria

- [ ] A test or an injection proves each fix.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-30: opened by the codebase review of 2026-10-30 (TASK-0111), from the plan of 2026-10-30.
