---
id: 0071
title: The desktop draws each live body of its session, from the events and the query GetTessellation
status: Done
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

- [x] Pictures of the window while a script sends the commands of the first demonstration over HTTP:
      the bodies appear, and after the subtraction only the cut body remains. The owner accepted them on
      2026-10-12.
- [x] A run of the desktop gives no validation message.
- [x] Each new test fails before its correction, or an injection shows that it can fail.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The
      merge records the run.

## Outcome

Status: Done · v0.70 · the commit that carries this block.

The desktop draws each live body of the session that it owns. `EngineHost.Subscribe` gives it the
events of the session in its process, in the order of the commits. `SessionBodies` reads the live bodies
and subscribes in one read of the session, fetches the mesh of each new body with `GetTessellation`,
narrows it around the centre of its box, and places it with an `Entity.Origin` in double precision; a
consumed body leaves the scene. ADR-0024 records the two answers of the owner and amends ADR-0019 and
ADR-0023.

## Method

**Mechanical.** Three pictures while a script sent the first demonstration over HTTP to the running
desktop: the empty window, the two boxes after the translate, and the cut body after the subtraction;
no validation message. Six new tests: four of the subscription in `Engine.Tests`, and the origin and
the centre in `3DEngine.Tests`. An injected rethrow in the broadcaster failed the test of a failing
action, and an injected float origin failed the test of the origin far from the scene.

**Judgement.** The action of a subscription runs inside the commit, so `SessionBodies` only puts the
body in a queue, and the render loop calls the query outside the commit. A consumed body waits for the
GPU to be idle before its buffers go, because a frame in flight can read them; a consumption is rare, so
the wait costs little. A body with no triangle gets no mesh, because a buffer of size 0 is not valid.

**Weakest.** No test calls `SessionBodies`: no test project references the desktop (R-0039). The
pictures are its evidence. The rule that an action must not block is text and a comment.

## Notes for the implementer

- **The session.** An action of `Subscribe` runs inside the commit; it only puts the event in a queue.
  The render loop drains the queue and calls the query outside the commit.
- **The size.** About 6 to 8 evenings.

## Progress

- 2026-10-12: the task is open, with ADR-0024, after the answers of the owner on R5.
- 2026-10-12: the work. `EngineHost.Subscribe` and the actions of the broadcaster, `Entity.Origin` and
  the walk in double precision, `MeshVertex.Centre`, and `SessionBodies` in the desktop, which replaces
  the fixed mesh. Three pictures while a script sent the first demonstration over HTTP: the empty
  window, the two boxes, and the cut body; no validation message. An injected rethrow in the
  broadcaster and an injected float origin each failed their test.
- 2026-10-12: the owner accepted the pictures. Closed, after a full local run of 402 tests.
