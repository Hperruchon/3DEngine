---
id: 0071
title: The desktop draws each live body of its session, from the events and the query GetTessellation
status: Active
phase: R5
opened: 2026-10-12
depends-on: [0070]
governed-by: [0005, 0007, 0009, 0010, 0011, 0017, 0019, 0020, 0022, 0023, 0024]
writes:
  create:
    - tasks/TASK-0071-geometry-from-the-engine.md
    - docs/adr/0024-in-process-events-and-body-origin.md
  modify:
    - Engine.Api.Http/EngineHost.cs
    - Engine.Api.Http/WebSockets/EventBroadcaster.cs
    - Engine.Tests/Http/EngineHostSubscriptionTests.cs
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - 3DEngine/**
    - 3DEngine.Tests/**
    - docs/adr/0019-hybrid-deployment-topology.md
    - docs/adr/0023-camera-depth-and-draw-list.md
    - docs/adr/README.md
    - tasks/TASK-0067-host-gaps-of-the-seventh-review.md
    - docs/INDEX.md
    - docs/glossary.md
    - docs/roadmap.md
    - docs/register.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - eng/**
---

# TASK-0071 — The desktop draws each live body of its session, from the events and the query GetTessellation

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase R5: "Geometry from the engine. Subscribe to events, fetch a mesh, draw it. Subtract a
double-precision origin before the narrowing to float. Choose frontFace together with the viewport
flip." R3 chose the front face with the flip of the viewport (ADR-0022, `PipelineStateTests`). R4 draws a
fixed mesh. On 2026-10-12 the owner answered two questions on R5; ADR-0024 records the answers.

## Goal

The desktop draws each live body of the session that it owns: a body appears when a command creates it,
and it goes when a command consumes it.

## Scope (in)

1. ADR-0024, and the text of ADR-0019 and ADR-0023 that it amends.
2. `EngineHost.Subscribe` and the call of each action in the broadcaster, with tests in `Engine.Tests`.
3. `Entity.Origin` and the walk of the scene in double precision, with tests in `3DEngine.Tests`.
4. The desktop: it reads the live bodies and subscribes in one read of the session, fetches the mesh of
   each new body with `GetTessellation`, narrows it around the centre of its box, and removes the mesh
   of each consumed body. The fixed mesh of the first demonstration goes.

## Scope (out)

- No image test (R6). No input layer (R7).

## Acceptance criteria

- [ ] Pictures of the window while a script sends the commands of the first demonstration over HTTP:
      the bodies appear, and after the subtraction only the cut body remains. The owner accepts them.
- [ ] A run of the desktop gives no validation message.
- [ ] Each new test fails before its correction, or an injection shows that it can fail.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The session.** An action of `Subscribe` runs inside the commit; it only puts the event in a queue.
  The render loop drains the queue and calls the query outside the commit.
- **The size.** About 6 to 8 evenings.

## Progress

- 2026-10-12: the task is open, with ADR-0024, after the answers of the owner on R5.
