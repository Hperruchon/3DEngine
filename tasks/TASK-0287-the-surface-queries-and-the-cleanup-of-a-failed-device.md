---
id: 0287
title: Each query of a surface checks its result, and a device that fails to start frees what it made
status: Done
phase: P0.121
opened: 2026-11-08
depends-on: [0286]
governed-by: [0017, 0023, 0025]
writes:
  create:
    - tasks/TASK-0287-the-surface-queries-and-the-cleanup-of-a-failed-device.md
  modify:
    - 3DEngine.Vulkan/GraphicsDevice.cs
    - 3DEngine.Vulkan/Presenter.cs
    - 3DEngine.ImageTests/PhoneLifecycleTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0287 — Each query of a surface checks its result, and a device that fails to start frees what it made

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The review of Codex of v0.271 gave two leads in the fix of TASK-0286, and the session confirmed both in the code:

- High: `GraphicsDevice` ignored the result of `vkGetPhysicalDeviceSurfaceSupportKHR`. With a lost surface the device
  counted as one that cannot present, the start ended with `InvalidOperationException`, the guard of TASK-0286 did not take
  it, and the document of the phone was lost again.
- Medium: when the constructor of `GraphicsDevice` threw after its instance, only the path with no device destroyed the
  surface and disposed the device. A failed start left an instance, a messenger, a surface and perhaps a device. The
  constructor of `Presenter` left its render pass when the swapchain threw.

A confirmed high lead comes before the next product phase.

## Goal

A lost surface in the start of the phone engine always reaches the guard of TASK-0286, and a failed start frees what it
made.

## Scope (in)

1. The query of the support of a queue checks its result.
2. The constructor of `GraphicsDevice` destroys the surface that no presenter took and disposes itself when a step after
   its instance throws; the constructor of `Presenter` frees its render pass and its swapchain when a later step throws.
3. A test that reads the code of Vulkan: each query of a surface checks its result.

## Scope (out)

- A test that makes a surface lost: no computer here can.

## Acceptance criteria

- [x] The test of the queries fails with the code before the change.
- [x] The tests of the GPU that make a device pass.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0121-surface-queries) · v0.272

## Method

Mechanical: the test of the queries on the code before the change; the tests of the GPU.
Judgement:  the cleanup by reading only, because no test can make a step after the instance throw.
Weakest:    the cleanup of a failed start has its proof by reading only, and the per-frame objects of a Presenter whose
            last step throws are not freed.

## Progress

- 2026-11-08: the check of the support query, and the cleanup of a failed start in `GraphicsDevice` and `Presenter`. The
  test of the queries failed with the old `GraphicsDevice` at the line of the support query.
- 2026-11-08: run 42427784771 passed. Closed at v0.272.
