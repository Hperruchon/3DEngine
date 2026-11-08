---
id: 0296
title: Five comments and messages, the cleanup of a failed device, the second ring and two gates hold their next forms
status: Active
phase: P0.125
opened: 2026-11-08
depends-on: [0295]
governed-by: [0004, 0007, 0017, 0019, 0023, 0025, 0027, 0029, 0030, 0032, 0033, 0034, 0035, 0037, 0039, 0051, 0052, 0054, 0055, 0057, 0058, 0059, 0072, 0076, 0083, 0087, 0088, 0089]
writes:
  create:
    - tasks/TASK-0296-low-forms-of-the-thirty-first-review.md
  modify:
    - Engine.Kernel/KernelSolid.cs
    - Engine.Core/Sketch/DatumResolver.cs
    - Engine.Core/Features/KernelProjection.cs
    - Engine.Core/Import/StlReader.cs
    - Engine.Tests/Sketch/SketchReferenceTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - 3DEngine.Vulkan/Presenter.cs
    - 3DEngine.Vulkan/GraphicsDevice.cs
    - 3DEngine.App/Ui/DesktopUi.Sketch.cs
    - 3DEngine.ImageTests/ToolRingTests.cs
    - docs/adr/0088-the-window-of-a-sketch-beside-its-ring.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0296 — Five comments and messages, the cleanup of a failed device, the second ring and two gates hold their next forms

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-08 found eight low findings: E187, E188, E189, E191, E192, V140, T175 and T176. They come
after TASK-0295.

## Goal

Each of the eight findings is fixed, each with a test, an injection or a reading.

## Scope (in)

1. E187, E188 and E189: the comment of `KernelSolid.Closed`, the messages of `DatumResolver` that name the cone, the
   header of `KernelProjection`, and the name of the rule of the reader as in ADR-0083 item 3.
2. E191 and E192: `Presenter` frees the objects of each frame that it made before a throw, and the catch of
   `GraphicsDevice` keeps the first exception when `Dispose` throws.
3. V140: while the second ring is open, each tool of the sketch has a place, or ADR-0088 item 1 says why not.
4. T175: the gate of the host refuses the switches of the analyzers that rule T170 refuses, and checks the line
   `WarningsAsErrors`.
5. T176: the test of the places of the tools checks that each place is inside the display, and the window of the
   sketch is at most as wide as the display.

## Scope (out)

- The other findings.

## Acceptance criteria

- [ ] Each finding has a test, an injection or a reading that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: a test, an injection or a reading for each finding, before the change.
Judgement:  E191, E192, E187, E188 and E189 are proved by reading; a failure of Vulkan in a constructor does not come on
            demand on a computer. E192 lets the wait of a lost device fail and destroys the objects, as Vulkan permits.
Weakest:    to be written at the close.

## Progress

- 2026-11-08: the eight findings. Injections: V140 (the tools on the ring while the second ring is open) fails two
  tests; T176 (the window 480 wide) fails the new test at 390 x 844; T175 (a mode of the analysis in the host, and no
  WarningsAsErrors) fails the gate with each form.
