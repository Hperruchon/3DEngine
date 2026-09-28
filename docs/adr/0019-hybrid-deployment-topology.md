---
id: 0019
title: The hybrid deployment topology
status: Accepted
topic: Clients, deployment
date: 2026-09-25
supersedes: []
superseded-by: []
amends: ['0011']
amended-by: []
affects:
  - Engine.Api.Http/**
  - 3DEngine/**
enforced-by: Engine.Tests/Http/DesktopSurfaceTests.cs (TASK-0038 creates it)
---

# ADR-0019 — The hybrid deployment topology

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0011 §1 makes `engine-api-http` the approved deployment, and it says: "There is no privileged
in-process path that bypasses the API surface." ADR-0011 §4 sends the desktop host `3DEngine` to that
server through HTTP.

The product is a desktop CAD application for one person. For that product, ADR-0011 has three costs:

- The person must start two processes and keep both alive.
- Each tessellation goes through JSON on a local connection.
- Each selection query and each hover query waits for an HTTP round trip. Objective 13 (very large
  scenes) makes each cost larger.

A topology with no HTTP removes the costs, but then an agent can only work on saved files. On
2026-09-25 the owner decided that an agent must work live on the document that the person has open.
The answer was option C, the hybrid. `docs/reviews/2026-09-23-architecture-challenge.md`, section
"Decisions of the owner, 2026-09-25", item 1, gives the question and the answer.

Two facts make the hybrid possible with no new rule for authority:

1. The surface of the engine is the set of commands, queries and events. HTTP and the WebSocket are
   one transport for that surface. The desktop host and an agent can use two transports and the same
   command bus.
2. Anti-objective 8 forbids a privileged client lane: a slow subscriber can never stop the authority.
   A typed call in the same process is a different transport. It is not a different authority.

## Decision

1. **The desktop owns the session.** The desktop host creates the document session of the open
   document in its own process. It sends commands and queries to the same command bus and the same
   query bus that each other client uses, and it gets typed results. TASK-0034 builds the session.
2. **No privileged lane.** Each change from the desktop is a command. It goes through the same
   validation, the same log and the same events as a command from an agent. The desktop gets no
   command, no query and no event that the surface does not give to each other client.
3. **One surface library.** The HTTP and WebSocket surface of `Engine.Api.Http` becomes a library
   that a host mounts on a session. The desktop host mounts it, so that an agent works live on the
   document that the person sees. The library references only `Engine.Core` and `Engine.Contracts`,
   as the project does today.
4. **The server stays.** `engine-api-http` becomes a small program that creates a session and mounts
   the same library. It serves headless work: continuous integration, scripts, and a shared server
   later. Its lifecycle stays independent of each client.
5. **The loopback address only.** The desktop listens on the loopback address only. The limit of
   ADR-0011 stays: a record for authentication must exist before the surface listens on a network.
6. **The session ends with the desktop.** When the person closes the desktop host, its surface
   stops. An agent that must continue uses `engine-api-http`.
7. **What stays from ADR-0011.** §2 (embedded mode), §3 (engine code knows nothing about topology)
   and §5 (the command line stays embedded) do not change. This record replaces the last sentence of
   §1 and all of §4.

## Consequences

**Good:** The person starts one program. The desktop gets meshes and query results with no JSON and
no round trip. An agent sees the document that the person sees, and each of its commands appears in
the view of the person through the same events. One library holds the surface, so one set of tests
covers the desktop and the server.

**Bad:** `Engine.Api.Http` must split into a library and a program. The desktop process holds a
network listener, and a defect in the surface can stop the program of the person. A crash of the
desktop ends the live session of each agent. Several people cannot share one live document in this
mode. They need `engine-api-http`, and a record for authentication must come first.

**Next:** TASK-0038 splits the surface and mounts it in the desktop host. `CLAUDE.md`, section
"Deployment topology", describes this record as the target until TASK-0038 is done. The dependency
rules in `CLAUDE.md` and `DependencyDirectionGateTests` change in TASK-0038: the desktop host may
reference the surface library, and the library is not a client. Register entry R-0021 holds the open
question of how to show the application to the person.
