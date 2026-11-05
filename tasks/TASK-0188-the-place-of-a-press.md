---
id: 0188
title: A press and a release of the mouse take the place of the cursor, not the last place of a motion
status: Active
phase: P0.81
opened: 2026-11-05
depends-on: []
governed-by: [0007, 0017, 0019, 0022, 0023, 0025, 0029, 0032]
writes:
  create:
    - tasks/TASK-0188-the-place-of-a-press.md
    - 3DEngine.ImageTests/FocusClickTests.cs
  modify:
    - 3DEngine.Vulkan/SdlInput.cs
    - 3DEngine.Vulkan/Window.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine.Tests/SdlInputTests.cs
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0188 — A press and a release of the mouse take the place of the cursor, not the last place of a motion

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Register entry R-0043, class risk, due 2026-11-27. In the visual check of K2b a synthetic click into the desktop
window dragged the panel once and turned the camera once, and clicks on a new pad hit other keys. A probe of
2026-10-30 (TASK-0118) stopped, because a person used the mouse. On 2026-11-05 the owner gave the order "fix R-0043
first, then the review".

The source of SDL 3.2.10, the version of `Alimer.Bindings.SDL` 3.9.8, gives the cause. `SDL_PrivateSendMouseButton`
in `src/events/SDL_mouse.c` fills `button.x` and `button.y` from `mouse->x` and `mouse->y`, the last place of a
motion. The Windows backend does not read the place of a button message. A probe of 2026-11-05 posted messages to a
hidden SDL window and moved no cursor: a press posted at (300, 200) came at (0, 299), the last place that SDL had.

## Goal

A press and a release of the mouse reach the place of the cursor on Windows, also when no motion came before them.

## Scope (in)

1. `SdlInput` takes an optional source of the cursor place. A press and a release take that place; a motion keeps its
   own.
2. `Window.Cursor` reads the place as SDL reads it at a focus gain on Windows: `GetCursorPos`, then
   `ScreenToClient`. It gives null on another video driver.
3. The desktop host gives `Window.Cursor` to `SdlInput`.
4. Tests of the order of the samples of such a click, through `SdlInput`, the panel and the intent machine.

## Scope (out)

- Linux and macOS. The cause is in the common file `SDL_mouse.c`, but no probe ran on X11, Wayland or Cocoa, and on
  Wayland a program cannot read the place of the cursor. A hand moves the mouse before a click, so a person sees the
  fault only after a jump of the cursor with no motion.

## Acceptance criteria

- [ ] With the place of the event, the order of the probe moves the panel and turns the camera.
- [ ] With the place of the cursor, the same order reaches the place of the click, and nothing moves.
- [ ] Each injection of the change fails a test.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-11-05: opened with the order of the owner; the source of SDL 3.2.10 and a probe that moved no cursor give the
  cause.
- 2026-11-05: the source of the cursor, its call in the host, 5 tests, and 3 injections that each fail a test.
