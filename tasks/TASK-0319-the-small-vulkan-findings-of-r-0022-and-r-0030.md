---
id: 0319
title: The small Vulkan findings of R-0022 and R-0030 are fixed
status: Active
phase: P0.132
opened: 2026-11-09
depends-on: [0317]
governed-by: [0007, 0009, 0017, 0019, 0022, 0023, 0029, 0030, 0061, 0073, 0079, 0080, 0083]
writes:
  create:
    - tasks/TASK-0319-the-small-vulkan-findings-of-r-0022-and-r-0030.md
    - 3DEngine.Tests/SwapchainChoiceTests.cs
  modify:
    - 3DEngine.Vulkan/Window.cs
    - 3DEngine.Vulkan/Swapchain.cs
    - 3DEngine.Vulkan/Presenter.cs
    - 3DEngine.Vulkan/GraphicsDevice.cs
    - 3DEngine/DesktopEngineHost.cs
    - 3DEngine.Android/PhoneHost.cs
    - 3DEngine.Core/Abstractions/ISceneLoader.cs
    - 3DEngine.Core/Services/SampleSceneFactory.cs
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0319 — The small Vulkan findings of R-0022 and R-0030 are fixed

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-09 the owner chose a cleanup task before the codebase review that is due (the old findings, answer B), and
asked to fix the small findings in it. A read-only check of the code on 2026-11-09 found, for R-0022 (the codebase review
of 2026-09-23): V1 fixed, V2 fixed in part, V3 open; and for R-0030 (the codebase review of 2026-09-30, V7): five of six
parts open or fixed in part. This session read each line on disk before each change.

## Goal

Each small Vulkan finding of R-0022 and R-0030 is fixed with a test, and the register gives the state of each part.

## Scope (in)

1. V2: the swapchain takes the special extent only for 0xFFFFFFFF, and then the size of the window in pixels.
2. V3: the portability enumeration of the instance and the portability subset of the device, when the loader and the
   device give them.
3. V7 part 1: one queue family that draws and presents comes first; else the swapchain shares its images.
4. V7 part 2: the surface format needs the colour space of sRGB.
5. V7 part 3: the desktop disposes its window and ends SDL at the end of the run.
6. V7 part 4: the results of the reset of a fence and of a command pool are checked.
7. V7 part 5: the time step of the desktop and of the phone comes from the monotonic clock.
8. The Vulkan parts of V4 (R-0027): the log line of the instance gives the version of the loader, the fallback to the
   layers of LunarG and Google goes, and the two types of `3DEngine.Core` that nothing calls go (`ISceneLoader`,
   `SampleSceneFactory`).

## Scope (out)

- V7 part 6 (`IList` in `3DEngine.Core`): a judgement, which goes to the owner. A run on macOS.

## Acceptance criteria

- [ ] The tests of the choices pass, and a run of the desktop on Windows draws and ends with the exit code 0 and no
      message of the validation layer.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] Continuous integration passes on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the text of each finding and the specification of Vulkan.
Judgement:  the portability flag goes on each loader that gives the extension, also on Windows.
Weakest:    to be written at the close.

## Progress

- 2026-11-09: the fixes and four tests of the choices. A run of the desktop on Windows with the NVIDIA device drew the
  shelf (a scratch copy), listed the instance extension of portability, gave no message of the validation layer, and
  ended with the exit code 0. No window ran on macOS, so V3 is not proved there.
- 2026-11-09: the Vulkan parts of V4 (R-0027). Five injections each make a test of the choices fail: the old extent check,
  no colour space, the first graphics family only, exclusive images always, and no portability subset. A second run of the
  desktop logged "the loader gives 1.4.341", gave no message of the validation layer, and ended with the exit code 0.
