---
id: 0074
title: The subscription reports and isolates its failures, and the renderer survives a resize, an old device and a bad camera
status: Ready
phase: P0.29
opened: 2026-10-12
depends-on: [0072]
governed-by: [0004, 0005, 0006, 0007, 0009, 0010, 0011, 0017, 0019, 0020, 0021, 0022, 0023, 0024]
writes:
  create:
    - tasks/TASK-0074-host-and-render-gaps-of-the-eighth-review.md
  modify:
    - Engine.Api.Http/EngineHost.cs
    - Engine.Api.Http/WebSockets/EventBroadcaster.cs
    - Engine.Core/CommandBus.cs
    - Engine.Tests/Http/EngineHostSubscriptionTests.cs
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Tests/**
    - docs/diagnostics.md
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/Commands/**
    - Engine.Core/Queries/**
    - Engine.Cli/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - eng/**
    - docs/adr/**
---

# TASK-0074 — The subscription reports and isolates its failures, and the renderer survives a resize, an old device and a bad camera

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-13 found gaps in the subscription of R5 and in the render side of R3 to R5.
Each finding gives its evidence in `docs/reviews/2026-10-13-codebase-review.md`.

- E61 to E65 (low): the order of the live bodies in the queue, a silent removal of a failing action, a
  shared payload, the disposal of a subscription, and a failure in `SessionBodies.Apply`.
- V8, V9 (medium): no swapchain is made again after OUT_OF_DATE, and nothing checks the API version of the
  device or the depth format.
- V10 to V15 (low): a mirrored transform, the parameters of the camera, a body far from the origin, the
  interface between the shaders and the pipeline, a byte order mark, and costs in the render loop.

## Goal

A failure of a subscription is visible and touches no other subscriber; the desktop keeps drawing after a
resize or a minimise, refuses a device that it cannot use with a message, and checks its camera.

## Scope (in)

1. E61 to E65: the live bodies enter the queue inside the read; a removed action is reported with a
   registered warning code; each payload is read-only; each subscription is one entry; `Apply` isolates a
   failure of one body.
2. V8, V9: the swapchain, its framebuffers and the depth image are made again; the device must give the
   target version and the depth format, or the desktop stops with a message.
3. V10 to V15: a negative scale is refused; the camera checks its parameters; a test reads the push
   constant block of the SPIR-V; `compile.js` removes a byte order mark; the costs of V15 are measured
   first. V12 and V13 go into the task of R6 if R6 takes them.

## Acceptance criteria

- [ ] Each new test fails before its correction, or an injection shows that it can fail.
- [ ] A picture of the window after a minimise and a restore shows the scene. The owner accepts it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The diagnostic code.** E62 needs a new `W-` code, registered in `docs/diagnostics.md` in the same commit.
- **The size.** About 2 to 3 evenings.
