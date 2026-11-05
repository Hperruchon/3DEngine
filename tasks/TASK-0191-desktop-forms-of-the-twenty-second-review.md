---
id: 0191
title: A press of a hand keeps its place, the far plane holds the images, and a drag queries no size in each frame
status: Active
phase: P0.83
opened: 2026-11-05
depends-on: [0190]
governed-by: [0007, 0017, 0019, 0022, 0023, 0025, 0029, 0031, 0032, 0043]
writes:
  create:
    - tasks/TASK-0191-desktop-forms-of-the-twenty-second-review.md
    - 3DEngine.ImageTests/TwentySecondReviewDesktopTests.cs
  modify:
    - 3DEngine.Vulkan/Window.cs
    - 3DEngine.Vulkan/SdlInput.cs
    - 3DEngine/SceneRenderer.cs
    - 3DEngine/SessionBodies.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine.Tests/SdlInputTests.cs
    - 3DEngine.ImageTests/FocusClickTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0191 — A press of a hand keeps its place, the far plane holds the images, and a drag queries no size in each frame

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-05 (b) found V85 and V86 (medium) and V87 (low) in the desktop. By
`docs/templates.md`, section 7, they come after the next product phase. The standing order of 2026-11-05 answers Q2 of
the review with A. The first commit of this task corrects the write set and the field `governed-by` for the files that
its tests need.

## Goal

A press and a release reach the place of their button message, also when the hand moves on, the far plane and the frame
hold the live images, and a drag of the gizmo reads the size of a body one time.

## Scope (in)

1. V85: the hook of the Windows messages in `Window` (Q2 B, see Progress), `SdlInput` with the place of the event
   again, a test of a click and a motion in one poll, and a probe of the hook on SDL 3.2.10.
2. V86: the quads of the live images in the box of the far plane and of the frame, with a test of an image alone.
3. V87: no query of `GetBodyInfo` for each frame of a preview.

## Scope (out)

- E135 to E141 and T136 to T140 (TASK-0192).

## Acceptance criteria

- [ ] Each finding has a test or an injection that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  the key of the size of a body during a preview.
Weakest:    to be written at the close.

## Progress

- 2026-11-05: opened by the codebase review of 2026-11-05 (b).
- 2026-11-05: Q2 of the review. The standing order gives A: the cursor only when no motion came after the last
  button event. A does not hold the case of R-0043: a hover gives a motion before the jump of the cursor, so A keeps
  the old place (the order of `FocusClickTests`). So the task takes B, the hook of the Windows messages. The owner
  checks this answer on 2026-11-08.
- 2026-11-05: a probe of the real `Window` on SDL 3.2.10 posted a motion at (50, 60) and a press at the place of the
  cursor in one pump: with the hook the press came at the place of the cursor, with one log line; without the hook it
  came at (50, 60). A press after a motion at its place gave no correction. The probe moved no cursor.
- 2026-11-05: V85 to V87 with 14 test cases; 5 injections each fail a test.
