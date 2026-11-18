---
id: 0404
title: The desktop window resizes and maximizes, and the layout follows a live resize
status: Active
phase: P0.164
opened: 2026-11-18
depends-on: [0403]
governed-by: [0007, 0017, 0019, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0061, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0089, 0091, 0092, 0093, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0110, 0111, 0113, 0114, 0116, 0117, 0119, 0120, 0123, 0124, 0125, 0127, 0129, 0130, 0131, 0137, 0138, 0140, 0163]
writes:
  create:
    - tasks/TASK-0404-the-desktop-window-resizes.md
    - docs/adr/0163-the-desktop-window-resizes-and-maximizes.md
  modify:
    - 3DEngine/DesktopEngineHost.cs
    - 3DEngine.Vulkan/Window.cs
    - 3DEngine.Vulkan/Presenter.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Image.cs
    - 3DEngine.ImageTests/ResponsiveLayoutTests.cs
    - docs/adr/0113-the-layout-at-each-phone-size-and-small-window.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - Engine.Core/**
    - 3DEngine.Core/**
    - 3DEngine.Android/**
---

# TASK-0404 — The desktop window resizes and maximizes, and the layout follows a live resize

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-18 the owner asked why the desktop window has a fixed size, when the responsive work (R19, ADR-0113) was done to
support each size. A design session of the 2D views (TASK-0402) found the cause: the host made the window with no flags, and
`Window` adds `SDL_WINDOW_RESIZABLE` only for `WindowFlags.Resizable`. The design session started this task in a separate
session.

## Goal

A person resizes, maximizes, minimizes and restores the desktop window, and the layout follows each step of a drag.

## Scope (in)

1. The flag `WindowFlags.Resizable` and a minimum size (ADR-0163 items 1 and 2).
2. A frame at each step of a drag on Windows, and the swapchain before the frame reads its extent (ADR-0163 item 3).
3. The test of the layout at each step of a resize, and the window of the images inside the display (items 4 and 5).

## Scope (out)

- The faults of the layout in a small window that is almost square (ADR-0163 fact 1). The minimum size keeps them out.
- A new place for the window of the images on a narrow display, and the phone host.

## Acceptance criteria

- [x] The window has a resize border and a maximize button, and SDL keeps it at 360 by 600 or more.
- [x] A drag of the corner on Windows draws each step with the layout of its size, with no warning in the log.
- [x] A maximize, a minimize and a restore give a correct frame.
- [x] The layout test passes at each step of 8 pixels on three lines of a resize, and an injection fails it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes on the branch.

## Progress

- 2026-11-18: opened from the question of the owner of 2026-11-18. A grid of sizes from 360 by 360 found faults of the
  layout in small windows that are almost square (width less than 750 and height less than 570), and a fault of the test
  at each width: a row that the bottom of the panel cuts in two. The minimum size is 360 by 600 (ADR-0163 item 2). A run
  on Windows dragged the corner in 40 steps: before the event watch, the window showed its last frame of 1280 by 720, cut,
  until the release; after it, the swapchain was made again 40 times and each picture had the layout of its size. The
  window of the images kept its first place at x 492 and the right edge cut it at 680 by 600; it now moves inside.
  Injections: the old rule of a scrolled row fails the sweep (1920 by 608), a minimum height of 540 fails it, and no move
  of the window of the images fails its test (772 for 664). Each file came back byte for byte.
- 2026-11-18: the work first had TASK-0403, ADR-0162, P0.163 and v0.386 on the branch `claude/kind-joliot-0a6460`, where
  run 44202716779 passed. Before its merge, the answers of 2026-11-18 on the 2D views took those numbers on `main`
  (`94ecd61`). This task takes TASK-0404, ADR-0163, P0.164 and v0.387 on the branch `p0164-resizable-window`, from
  `origin/main`, with the same code. The old branch is not merged.
