---
id: 0074
title: The subscription reports and isolates its failures, and the renderer survives a resize, an old device and a bad camera
status: Active
phase: P0.29
opened: 2026-10-12
depends-on: [0072]
governed-by: [0004, 0005, 0006, 0007, 0009, 0010, 0011, 0013, 0016, 0017, 0019, 0020, 0021, 0022, 0023, 0024]
writes:
  create:
    - tasks/TASK-0074-host-and-render-gaps-of-the-eighth-review.md
  modify:
    - Engine.Api.Http/EngineHost.cs
    - Engine.Api.Http/WebSockets/EventBroadcaster.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Core/DiagnosticCodes.cs
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

## Progress

- 2026-10-16: the task is active, first in the order of the owner of 2026-10-16. The write set now names
  `Engine.Core/DiagnosticCodes.cs` and `Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs`: the new
  `W-` code of E62 needs a constant and a row of `/schema/diagnostics` (`docs/diagnostics.md`, "Adding a
  code"), and the task file named neither. TASK-0076 already did V12 (the camera frames the bodies) and the
  synchronization part of V13. The two files bring ADR-0013 and ADR-0016 into `governed-by`.
- 2026-10-16: E61 to E65. `SessionBodies` puts the live bodies in the queue inside the read, before the
  subscription, gives no warning for a body that a command consumed before the frame, and writes a
  warning for a failure of one body and continues. The broadcaster keeps one entry for each subscription,
  removes it once, and reports a removed action with the new code `W-API-SUBSCRIPTION-REMOVED`. Each
  payload is read-only. The three tests of E62, E63 and E64 each failed with the old rule put back. E61
  and E65 have no automatic test: `SessionBodies` needs a GPU device, and `3DEngine.ImageTests` is outside
  this write set.
- 2026-10-16: V8 and V9. The presenter owns the surface and the render pass, skips a frame while the
  surface has no area, and makes the swapchain, its framebuffers and its depth image again when the extent
  changes or an acquire or a present gives OUT_OF_DATE or SUBOPTIMAL. A device must give Vulkan 1.3 and a
  depth attachment of `D32_SFLOAT`, and the message of a refusal names each device and its reason. On this
  computer a minimise and a restore kept the scene, but the driver kept the swapchain valid, so the new
  path did not run; an injected remake every 60 frames ran it 634 times with no validation message, and
  the scene stayed. An injected required version 1.9 refused both devices with their reasons.
