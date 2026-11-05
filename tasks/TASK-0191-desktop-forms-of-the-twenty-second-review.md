---
id: 0191
title: A press of a hand keeps its place, the far plane holds the images, and a drag queries no size in each frame
status: Ready
phase: P0.83
opened: 2026-11-05
depends-on: [0190]
governed-by: [0007, 0017, 0019, 0022, 0023, 0025, 0029, 0031, 0032, 0043]
writes:
  create:
    - tasks/TASK-0191-desktop-forms-of-the-twenty-second-review.md
  modify:
    - 3DEngine.Vulkan/SdlInput.cs
    - 3DEngine/SceneRenderer.cs
    - 3DEngine/SessionBodies.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine.Tests/SdlInputTests.cs
    - 3DEngine.Tests/CameraTests.cs
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

A press takes the place of the cursor only when no motion came after the last button event, the far plane and the frame
hold the live images, and a drag of the gizmo reads the size of a body one time.

## Scope (in)

1. V85: the rule of Q2 A in `SdlInput`, with a test of a click and a motion in one poll.
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
