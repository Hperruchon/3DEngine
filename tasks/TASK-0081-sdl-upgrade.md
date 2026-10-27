---
id: 0081
title: The desktop uses a binding with SDL 3.2 or later
status: Ready
phase: P0.32
opened: 2026-10-17
depends-on: [0083]
governed-by: [0007, 0017, 0019, 0022, 0023, 0025]
writes:
  create:
    - tasks/TASK-0081-sdl-upgrade.md
  modify:
    - 3DEngine.Vulkan/**
    - 3DEngine/**
    - 3DEngine.Tests/**
    - 3DEngine.ImageTests/**
    - THIRD-PARTY-NOTICES.md
    - docs/INDEX.md
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - 3DEngine.Core/**
    - eng/**
---

# TASK-0081 — The desktop uses a binding with SDL 3.2 or later

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-17 found that the native `SDL3.dll` of `Alimer.Bindings.SDL` 3.9.2 is version
3.1.3, a preview of SDL 3, and that the package has no `linux-arm64` runtime (finding V25). The preview has no
event that cancels a finger and no pen hint. On 2026-10-17 the owner chose decision D7, option B: a task
before R7 moves to a binding with SDL 3.2 or later. Versions 3.9.3 to 3.9.8 of the binding exist on NuGet;
the review did not check which SDL each one ships.

## Goal

The desktop runs on a binding whose native SDL is 3.2 or later, with no change of behaviour.

## Scope (in)

1. Find the newest version of `Alimer.Bindings.SDL` whose native SDL is 3.2 or later, and record its version
   of SDL and its runtimes. If none exists, stop and report to the owner.
2. Move the one pin in `3DEngine.Vulkan/3DEngine.Vulkan.csproj` (ADR-0017 item 4), and adapt each call that
   the new binding changes.
3. Correct finding V25 in the register or report what stays: the runtimes of the new package, and whether a
   `linux-arm64` runtime exists.

## Acceptance criteria

- [ ] The file version of the native SDL of the new package is 3.2 or later.
- [ ] A run of the desktop with the first demonstration gives the same picture as before, with no `[WARN]`
      and no `[ERROR]` line. The owner accepts it.
- [ ] The image test passes on this computer and on the Ubuntu runner.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The order.** After TASK-0078 and before TASK-0082 (R7).
- **The size.** About 1 to 2 evenings.
