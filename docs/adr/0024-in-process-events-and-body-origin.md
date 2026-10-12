---
id: 0024
title: The events of a session in its host, and the origin of a body in double precision
status: Accepted
topic: Rendering, clients
date: 2026-10-12
supersedes: []
superseded-by: []
amends: ['0019', '0023']
amended-by: []
affects:
  - Engine.Api.Http/EngineHost.cs
  - Engine.Api.Http/WebSockets/EventBroadcaster.cs
  - 3DEngine.Core/Models/Entity.cs
  - 3DEngine.Core/Rendering/**
enforced-by: Engine.Tests/Http/EngineHostSubscriptionTests.cs (TASK-0071 creates it)
---

# ADR-0024 — The events of a session in its host, and the origin of a body in double precision

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase R5 asks the desktop to subscribe to the events of the engine, fetch a mesh, and draw it.
ADR-0009 says that a host that draws projects the events of `Engine.Core` into the state of
`3DEngine.Core`. Two conditions make two decisions:

1. No public API gives a host the events of the session that it owns, in its own process. Each event
   goes to the subscribers of `/events` through an internal broadcaster of `Engine.Api.Http`.
2. `Transform.Position` of `3DEngine.Core` is a float. The engine gives positions in double precision
   (ADR-0018 item 3), and ADR-0023 item 2 narrows each matrix relative to the eye; a body far from the
   origin of the scene loses its detail before that subtraction.

On 2026-10-12 the owner chose option A for each question: a public subscription on `EngineHost`, and a
double origin on `Entity`.

## Decision

1. **A subscription in the process.** `EngineHost.Subscribe(Action<EventRecord>)` gives each event of
   the session to the action, after its commit and in the order of the commits, the same events that
   `/events` gives (ADR-0019 item 2). The action runs inside the commit, so it must not call the session
   and must not block; a host puts the event in a queue and reads the queue in its own loop. An action
   that throws is removed, and the commit continues. A host that reads the live bodies and subscribes in
   one read of the session misses no event and gets no event two times.
2. **A body origin in double precision.** `Entity` has an `Origin` of three doubles in the space of its
   parent, before `Transform.Position`. The walk of the scene adds each origin and each position in
   double precision, and only the position relative to the eye is narrowed to float. A host narrows each
   mesh around the centre of its own box, and that centre is the origin of its entity.

## Consequences

**Good:** The desktop draws the bodies of its session with no network and no parse of JSON, in the order
of the commits. A body keeps its detail far from the origin of the scene.

**Bad:** `EngineHost` has one more public member, which each host of the surface carries. An action that
blocks would hold each commit; the rule is in text and in the comment of the method, and only a review
sees a break of it. The parent turn of a child is a float matrix, so a turned child origin keeps the
precision of that matrix.

**Next:** TASK-0071 applies this decision.

## History

- 2026-10-12 · TASK-0071 · The record is written with the answers of the owner on R5.
