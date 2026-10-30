---
id: 0118
title: The pads of the sketch follow their values, and the desktop meshes each body one time for each frame
status: Ready
phase: P0.47
opened: 2026-10-30
depends-on: [0114]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0031, 0032, 0034]
writes:
  create:
    - tasks/TASK-0118-desktop-forms-of-the-thirteenth-review.md
  modify:
    - 3DEngine/Ui/DesktopUi.cs
    - 3DEngine/Ui/UiInputRouter.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine/SessionBodies.cs
    - 3DEngine/SketchOverlay.cs
    - 3DEngine.ImageTests/SketchGapUiTests.cs
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Api.Http/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - docs/adr/**
---

# TASK-0118 — The pads of the sketch follow their values, and the desktop meshes each body one time for each frame

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-30 found V46 (medium) and V47 to V50 (low) in the desktop, and TASK-0115 gave this task
R-0043. TASK-0114 took the items of the engine and the gates, and these items wait until R9 (TASK-0113), which changes
`3DEngine/NativeThreeDEngine.cs` on its branch, merges after the visual check of the owner.

## Goal

A pad of the sketch closes when a value that it shows changes, a pad action of a stale scope is refused, the desktop
meshes each body one time for each frame, the overlay drops a pixel of a large magnitude, the fault text fits the
panel, and R-0043 has a test of the order of the samples of a focus click.

## Scope (in)

1. V46 and V47: the key of a pad holds the values that it shows, and the host refuses a pad action whose key is not
   the current key.
2. V48: one mesh for each body and frame. V49: a limit of the magnitude of a pixel. V50: the fault text fits the panel.
3. R-0043: a test of the order of the samples of a focus click into the desktop window, and a change or a recorded
   reason that closes the entry.

## Scope (out)

- A change of the engine.

## Acceptance criteria

- [ ] Each form of the review fails a test, and an injection that restores each old rule fails a test.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-30: opened by TASK-0114, with the desktop items of TASK-0114 and R-0043 of TASK-0115.
