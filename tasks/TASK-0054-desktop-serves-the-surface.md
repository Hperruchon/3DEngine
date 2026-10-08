---
id: 0054
title: The desktop host owns a document session and serves the HTTP and WebSocket surface
status: Ready
phase: P0.19
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

- [ ] The desktop host starts on Windows, opens its window, and answers `GET /schema/commands` on the
      loopback address. The Outcome block records the address and the reply.
- [ ] `DependencyDirectionGateTests` fails on an injected reference from the surface library to
      `3DEngine`, and passes without it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The window.** The first criterion needs a run on the computer of the owner. A runner of the
  pipeline has no display.
- **The address guard.** The desktop must use the checks of TASK-0051 for its address, through the
  library, and not a copy of them.
