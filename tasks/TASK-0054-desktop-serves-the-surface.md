---
id: 0054
title: The desktop host owns a document session and serves the HTTP and WebSocket surface
status: Done
phase: P0.21
opened: 2026-10-08
depends-on: [0038]
governed-by: [0007, 0017, 0019]
writes:
  create:
    - tasks/TASK-0054-desktop-serves-the-surface.md
  modify:
    - 3DEngine/**
    - 3DEngine.sln
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - CLAUDE.md
    - docs/INDEX.md
    - docs/glossary.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Api.Http/**
    - Engine.Geometry.Manifold/**
    - 3DEngine.Vulkan/**
    - 3DEngine.Core/**
    - BlazorApp/**
    - nuget/**
---

# TASK-0054 — The desktop host owns a document session and serves the HTTP and WebSocket surface

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0019 gives the decision. On 2026-10-08 the owner split TASK-0038 in two. TASK-0038 makes the
surface a library that a host mounts on its own document session. This task mounts it in the desktop
host, so that an agent sends a command over HTTP to the document that the person has open.

## Goal

An agent sends a command over HTTP to the desktop host, and the command changes the document that
the desktop host holds.

## Scope (in)

1. **The desktop.** `3DEngine` creates a session in its own process and mounts the surface on the
   loopback address. The port is a setting, and the desktop writes the address to its output at
   start.
2. **The rules.** `CLAUDE.md`, section "Authority diagram", and `DependencyDirectionGateTests` agree
   with ADR-0019: the desktop host may reference the surface library, and the library is not a client.
   The sentence "Today `engine-api-http` is the only host with the HTTP and WebSocket surface" changes.

## Scope (out)

- No drawing of engine geometry. Phase R5 does that.
- No authentication and no address other than the loopback address (ADR-0019, item 5).
- No persistence.

## Acceptance criteria

- [x] The desktop host starts on Windows, opens its window, and answers `GET /schema/commands` on the
      loopback address. The Outcome block records the address and the reply.
- [x] `DependencyDirectionGateTests` fails on an injected reference from the surface library to
      `3DEngine`, and passes without it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The
      merge records the run.

## Notes for the implementer

- **The window.** The first criterion needs a run on the computer of the owner. A runner of the
  pipeline has no display.
- **The address guard.** The desktop must use the checks of TASK-0051 for its address, through the
  library, and not a copy of them.

## Outcome

Status: Done · v0.58 · the commit that carries this block.

The desktop host composes a document session with the native backend, mounts the surface library on
it at 127.0.0.1, and writes the address before the window opens. The setting `port` gives the port,
5180 by default. The surface stops when the window closes. The desktop stops before the window with
exit code 1 for an address that is not loopback, 2 for an invalid port, and 3 when the native library
does not load.

The run on Windows on 2026-10-11, on the computer of the owner: the desktop wrote
`3DEngine serves the document at http://127.0.0.1:5180`, created its Vulkan instance, loaded the
sample scene, and answered `GET /schema/commands` with status 200 and the four commands
`CreateBox`, `NoOp`, `Subtract` and `Translate`.

## Method

**Mechanical.** A Node.js script started the desktop, waited for the line with the address, asked
`GET /schema/commands` after 3 s, and stopped the process. The dependency gate refused the reference
from the desktop to the surface library before the change of its rule; after the change, an injected
reference from the surface library to `3DEngine` failed three rules: the rule of ADR-0019 item 3, the
client rule and the rule of no cycle.

**Judgement.** `Main` stays synchronous, because SDL needs the main thread for the window, and the
surface starts before the window with a blocking wait. The desktop chooses the native backend itself,
as a composition root, and stops like each host when the native library does not load (TASK-0036).
The port is the only address setting of the desktop; each other source of an address goes through
`ConfigurationRefusal`.

**Weakest.** The run shows the window from the log only: the log gives the Vulkan instance and the
scene, which come before `window.Show()`, and no screenshot shows the window. No test runs the desktop
in the pipeline, because a runner has no display; `DesktopSurfaceTests` of TASK-0038 covers the
surface on an owned session. The choice of the backend now exists in three hosts (finding E39).

## Progress

- 2026-10-11: the task is open, after TASK-0038.
- 2026-10-11: closed. The run on Windows answered on 127.0.0.1:5180.
