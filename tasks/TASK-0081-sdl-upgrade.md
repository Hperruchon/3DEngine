---
id: 0081
title: The desktop uses a binding with SDL 3.2 or later
status: Done
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

- [x] The file version of the native SDL of the new package is 3.2 or later (3.2.10).
- [x] A run of the desktop with the first demonstration gives the same picture as before, with no `[WARN]`
      and no `[ERROR]` line. The owner accepts it (2026-10-27).
- [x] The image test passes on this computer and on the Ubuntu runner (run 40572683824).
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`. The
      merge records the run.

## Outcome

Status: Done · v0.82 · the commit that carries this block.

The desktop uses `Alimer.Bindings.SDL` 3.9.8, whose native SDL is version 3.2.10, a stable release; the
binding 3.9.2 gave the 3.1.3 preview. No call changed. Finding V25 of the codebase review of 2026-10-17 is
corrected for the version of SDL; the package still has no `linux-arm64` runtime, so that part stays open.

## Method

**Mechanical.** The registration of the package on NuGet lists 3.9.8 (2025-04-04) as its newest version. Its
`SDL3.dll` for `win-x64` has the file version 3.2.10, which the output folder of the desktop also holds. The
solution built with no change and no warning, the 506 tests passed, run 40572683824 passed on the three runners,
and the owner accepted the picture of the window after the demonstration, a minimise and a restore.

**Judgement.** The newest version, because it is the only line of the binding with a stable SDL that has had
three months of use, and the move changed no call. The metadata of the package names no version of SDL, so the
file version of the native library is the evidence.

**Weakest.** No runner opens a window, so only this computer shows the new SDL at work: the image test uses no
SDL. Linux and macOS load a new `libSDL3` that no run of a window checked (R-0022).

## Notes for the implementer

- **The order.** After TASK-0078 and before TASK-0082 (R7).
- **The size.** About 1 to 2 evenings.

## Progress

- 2026-10-27: the task is active, second in the order of the owner of 2026-10-27. The newest version of
  `Alimer.Bindings.SDL` is 3.9.8 (2025-04-04); its native `SDL3.dll` is version 3.2.10, and its runtimes are
  `linux-x64`, `osx`, `win-arm64` and `win-x64`, still with no `linux-arm64`. The one pin in
  `3DEngine.Vulkan/3DEngine.Vulkan.csproj` moved to 3.9.8; the solution built with no change of a call and no
  warning, and the 506 tests passed. The desktop loaded SDL 3.2.10 and drew the first demonstration, before and
  after a minimise, with no `[WARN]` and no `[ERROR]` line.
- 2026-10-27: the owner accepted the picture. Closed, after a clean build and a full local run of 506 tests.
