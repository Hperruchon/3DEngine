---
id: 0038
title: The HTTP and WebSocket surface is a library, and engine-api-http is its program
status: Done
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
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Hosting/DispatchSurfaceGateTests.cs
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

- [x] A test starts the surface on a session in the same process, sends `CreateBox` over HTTP, and
      reads the body from the session with a typed query.
- [x] A WebSocket subscriber on that surface receives `body.created` for a command that the same
      process sent with a typed call.
- [x] `engine-api-http` starts and passes each existing HTTP test with no change to the meaning of a
      test, and the process tests of the address guard still give exit code 1.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The
      merge records the run.

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

## Outcome

Status: Done · v0.57 · the commit that carries this block.

`Engine.Api.Http` is a class library with one public entry point, `EngineSurface`: a host calls
`ConfigurationRefusal`, `AddEngineSurface` with the session that it owns, `MapEngineSurface` and
`BoundAddressRefusal`. `EngineHost` is public: a host composes it with the backend of its choice, and
it owns its broadcaster. The library references `Engine.Core` and `Engine.Contracts` only.
`Engine.Api.Http.Server` is the program `engine-api-http`: it chooses the backend in
`ServerEngine`, mounts the library, and keeps exit codes 1 and 3. Each existing HTTP test passes with
no change to its meaning; two tests changed the name of a call: `HostGuardTests` calls
`EngineSurface` in place of `Program`, and `BackendSelectionTests` calls `ServerEngine.Create`.

## Method

**Mechanical.** `DesktopSurfaceTests` mounts the surface on a session that the test owns, on a real
Kestrel at 127.0.0.1, and passes. The tests use the new entry point, so the old code could not compile
them; two injections show that they can fail. With the plain sink in place of the broadcasting sink,
the test of the subscriber failed. With a second session behind the surface, both tests failed.

The two gates were proved by injection too. The dispatch gate passed a command name in the program,
because it did not read the new project; with the project in its list, it failed the name. The
original dependency gate passed a reference from the library to `Engine.Geometry.Manifold`, because
the library was a client and a client may name the native backend; with the new class "surface
library" and its rule, the same reference failed with the text of ADR-0019 item 3.

**Judgement.** The choice of the backend moved into the program and not into an option of the
library, because ADR-0014 section 4 lets only a composition root name the native backend. `EngineHost`
keeps its name and its namespace, so the tests that read it from the service container did not
change, the gate file `SchemaEndpointGateTests.cs` among them. A headless client may reference the
surface library; the permit for the desktop host stays with TASK-0054. The write set gained the two
gate files in the commit that closes the task, because the new project must be in a class of the
dependency gate and in the list of the dispatch gate.

**Weakest.** The section "Authority diagram" of `CLAUDE.md` still lists `Engine.Api.Http` with the
clients; TASK-0054 changes that section. ADR-0019 item 3 said "as the project does today", which was
false, and this task corrected the item. No test runs the program as a process on its success path
other than the tests of the address guard and the real-host WebSocket test.

## Progress

- 2026-10-11: the task is open, in the order of the owner of 2026-10-11.
- 2026-10-11: closed. The library, the program, the tests of the topology and the two gates are in.
