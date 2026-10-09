---
id: 0038
title: The HTTP and WebSocket surface is a library, and engine-api-http is its program
status: Ready
phase: P0.20
opened: 2026-09-25
depends-on: [0034, 0036, 0051, 0053, 0058]
governed-by: [0005, 0010, 0011, 0013, 0016, 0018, 0019, 0020, 0021]
writes:
  create:
    - tasks/TASK-0038-hybrid-topology.md
    - Engine.Api.Http.Server/**
    - Engine.Tests/Http/DesktopSurfaceTests.cs
  modify:
    - Engine.Api.Http/**
    - 3DEngine.sln
    - Engine.Tests/**
    - Engine.Tests/Engine.Tests.csproj
    - .github/workflows/ci.yml
    - docs/INDEX.md
    - docs/adr/0019-hybrid-deployment-topology.md
    - docs/glossary.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/Commands/**
    - Engine.Core/Queries/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Core/**
    - BlazorApp/**
    - nuget/**
---

# TASK-0038 — The HTTP and WebSocket surface is a library, and engine-api-http is its program

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0019 gives the decision and each reason. The owner decided on 2026-09-25 that an agent must work
live on the document that the person has open. Today the desktop host has no path to the engine
(`3DEngine/3DEngine.csproj` references no `Engine.*` project), and only `engine-api-http` has the
HTTP and WebSocket surface.

On 2026-10-08 the owner split the first version of this task in two, as its notes permitted. This
task makes the surface a library that any host can mount on a document session. TASK-0054 then
mounts it in the desktop host.

## Goal

A host mounts the HTTP and WebSocket surface on a document session that it owns, and `engine-api-http`
is one such host.

## Scope (in)

1. **The library.** `Engine.Api.Http` becomes a class library that mounts the surface on a document
   session. It keeps its namespaces, so each existing test keeps its meaning.
2. **The program.** `Engine.Api.Http.Server` is the program `engine-api-http`. It creates a session
   and mounts the library. The assembly name `engine-api-http` does not change. The address checks of
   TASK-0051 move into the library, so that each host uses the same checks (TASK-0054). The program
   keeps the exit codes of TASK-0036 and TASK-0051.
3. **The proof of the hybrid topology.** `Engine.Tests/Http/DesktopSurfaceTests.cs` mounts the surface
   on a session that the test owns, as the desktop host will do.
4. The field `enforced-by` of ADR-0019 loses the text "(TASK-0038 creates it)".

## Scope (out)

- No change to the desktop host. TASK-0054 does that.
- No drawing of engine geometry. Phase R5 does that.
- No authentication and no address other than the loopback address (ADR-0019, item 5).
- No persistence.

## Acceptance criteria

- [ ] A test starts the surface on a session in the same process, sends `CreateBox` over HTTP, and
      reads the body from the session with a typed query.
- [ ] A WebSocket subscriber on that surface receives `body.created` for a command that the same
      process sent with a typed call.
- [ ] `engine-api-http` starts and passes each existing HTTP test with no change to the meaning of a
      test, and the process tests of the address guard still give exit code 1.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The pipeline.** Each step in `.github/workflows/ci.yml` that names the project path of
  `engine-api-http` must follow the move.
- **The tests.** `WebApplicationFactory<Program>` must name the `Program` of the new project, and the
  project file of the tests must reference it. The write set names that file exactly, because it is a
  gate file (finding T15 of the review of 2026-10-08).
- **ADR-0015.** It affects `Engine.Api.Http/**`, and it has the status `Proposed`. It is not in
  force, therefore it is not in `governed-by`.
- **The first version of this task** named the sections "Deployment topology" and "Dependency rules"
  of `CLAUDE.md`. No such sections exist; the section "Authority diagram" holds the topology, and
  `DependencyDirectionGateTests` holds each dependency rule. TASK-0054 changes both.
