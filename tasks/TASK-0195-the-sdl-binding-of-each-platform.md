---
id: 0195
title: ppy.SDL3-CS replaces Alimer.Bindings.SDL on the desktop, with no change of behaviour
status: Done
phase: R14c
opened: 2026-11-06
depends-on: [0194]
governed-by: [0007, 0017, 0019, 0022, 0023, 0025, 0029, 0030, 0032, 0061]
writes:
  create:
    - tasks/TASK-0195-the-sdl-binding-of-each-platform.md
  modify:
    - 3DEngine.Vulkan/3DEngine.Vulkan.csproj
    - 3DEngine.Vulkan/Window.cs
    - 3DEngine.Vulkan/SdlInput.cs
    - 3DEngine.Vulkan/GraphicsDevice.cs
    - 3DEngine/DesktopEngineHost.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine.Tests/SdlInputTests.cs
    - 3DEngine.ImageTests/FocusClickTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - THIRD-PARTY-NOTICES.md
    - docs/adr/0017-first-party-vulkan-layer.md
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0195 — ppy.SDL3-CS replaces Alimer.Bindings.SDL on the desktop, with no change of behaviour

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Phase c of the Android order of 2026-11-05 (ADR-0061 item 5). `Alimer.Bindings.SDL` has no Android library, and
`ppy.SDL3-CS` ships one with the Java bridge. The check of 2026-11-05 found 69 distinct compile errors in `Window.cs` and
`SdlInput.cs` alone: names of enumerations in the form of C, a window as a pointer, and arguments as pointers.

## Goal

The desktop runs on `ppy.SDL3-CS` 2026.1002.1 with the behaviour of today, and each test passes.

## Scope (in)

1. The pin in `3DEngine.Vulkan/3DEngine.Vulkan.csproj` and the port of the seven files that call SDL.
2. The pin rule of `DependencyDirectionGateTests` names the new binding, and a fact refuses the old one.
3. A probe of the hook of R-0043 on the new SDL, and a run of the desktop.

## Scope (out)

- The Android host (R14e). The files of 32-bit x86 that the package brings into the build output: the desktop has no
  distributed package yet, and ADR-0027 already says that the package of the desktop leaves such files out.

## Acceptance criteria

- [x] Each test passes with no change of an expectation.
- [x] Each injection of the gate fails a test.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r14c-sdl-binding) · v0.197

## Method

Mechanical: a script maps the names of the enumerations, and the compiler gives each other change.
Judgement:  the pin 2026.1002.1, whose SDL is a development snapshot (R-0054).
Weakest:    no test reaches the window and its swapchain; one run of the desktop on Windows proved them, and the
            pipeline runs no window on Linux or macOS.

## Progress

- 2026-11-06: opened with the Android order; the pin, the port of seven files, the pin rule, and two injections that
  each fail a test. A probe of the hook of R-0043 on SDL 3.5.0 corrects a stale press, and a run of the desktop opened
  a second file with a new Vulkan instance and no error.
- 2026-11-06: closed at v0.197.
