---
id: 0038
title: The desktop host owns a document session and serves the HTTP and WebSocket surface
status: Ready
phase: P0.17
opened: 2026-09-25
depends-on: [0034, 0036]
governed-by: [0005, 0007, 0010, 0011, 0013, 0016, 0017, 0018, 0019, 0020, 0021]
writes:
  create:
    - tasks/TASK-0038-hybrid-topology.md
    - Engine.Api.Http.Server/**
    - Engine.Tests/Http/DesktopSurfaceTests.cs
  modify:
    - Engine.Api.Http/**
    - 3DEngine/**
    - 3DEngine.sln
    - Engine.Tests/**
    - .github/workflows/ci.yml
    - CLAUDE.md
    - docs/INDEX.md
    - docs/adr/0019-hybrid-deployment-topology.md
    - docs/glossary.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/Commands/**
    - Engine.Core/Queries/**
    - Engine.Geometry.Manifold/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Core/**
    - BlazorApp/**
    - nuget/**
---

# TASK-0038 — The desktop host owns a document session and serves the HTTP and WebSocket surface

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0019 gives the decision and each reason. The owner decided on 2026-09-25 that an agent must work
live on the document that the person has open. Today the desktop host has no path to the engine
(`3DEngine/3DEngine.csproj` references no `Engine.*` project), and only `engine-api-http` has the
HTTP and WebSocket surface.

## Goal

An agent sends a command over HTTP to the desktop host, and the command changes the document that
the desktop host holds.

## Scope (in)

1. **The library.** `Engine.Api.Http` becomes a class library that mounts the surface on a document
   session. It keeps its namespaces, so each existing test keeps its meaning.
2. **The program.** `Engine.Api.Http.Server` is the program `engine-api-http`. It creates a session
   and mounts the library. The assembly name `engine-api-http` does not change.
3. **The desktop.** `3DEngine` creates a session in its own process and mounts the surface on the
   loopback address. The port is a setting, and the desktop writes the address to its output at
   start.
4. **The rules.** `CLAUDE.md`, sections "Deployment topology" and "Dependency rules", and
   `DependencyDirectionGateTests` agree with ADR-0019: the desktop host may reference the surface
   library, and the library is not a client.
5. The field `enforced-by` of ADR-0019 loses the text "(TASK-0038 creates it)".

## Scope (out)

- No drawing of engine geometry. Phase R5 does that.
- No authentication and no address other than the loopback address (ADR-0019, item 5).
- No persistence.

## Acceptance criteria

- [ ] A test starts the surface on a session in the same process, sends `CreateBox` over HTTP, and
      reads the body from the session with a typed query.
- [ ] A WebSocket subscriber on the desktop surface receives `body.created` for a command that the
      same process sent with a typed call.
- [ ] `engine-api-http` starts and passes each existing HTTP test with no change to the meaning of a
      test.
- [ ] The desktop host starts on Windows, opens its window, and answers `GET /schema/commands` on the
      loopback address. The Outcome block records the address and the reply.
- [ ] `DependencyDirectionGateTests` fails on an injected reference from the surface library to
      `3DEngine`, and passes without it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The size of the task.** If the work does not fit one session (objective 12), split it into two
  tasks: first the library and the program, then the desktop.
- **The pipeline.** Each step in `.github/workflows/ci.yml` that names the project path of
  `engine-api-http` must follow the move.
- **ADR-0015.** It affects `Engine.Api.Http/**`, and it has the status `Proposed`. It is not in
  force, therefore it is not in `governed-by`.
