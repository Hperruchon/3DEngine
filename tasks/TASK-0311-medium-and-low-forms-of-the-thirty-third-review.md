---
id: 0311
title: The dimension lines take the up axis of the camera, and the top bar and three comments hold their next forms
status: Active
phase: P0.130
opened: 2026-11-08
depends-on: [0308]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0056, 0072, 0073, 0075, 0076, 0077, 0080, 0083, 0089, 0093, 0094, 0096]
writes:
  create:
    - tasks/TASK-0311-medium-and-low-forms-of-the-thirty-third-review.md
  modify:
    - 3DEngine.Core/Measuring/Dimensions.cs
    - 3DEngine.App/DimensionOverlay.cs
    - 3DEngine.Tests/DimensionsTests.cs
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.Core/Models/Camera.cs
    - 3DEngine.ImageTests/FixedControlsTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0311 — The dimension lines take the up axis of the camera, and the top bar and three comments hold their next forms

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-08 (c) found V147 (medium) and V149, V150 and V151 (low). They come after R16n (TASK-0308),
which fixes V148.

## Goal

V147, V149, V150 and V151 are fixed, each with a test or a reading.

## Scope (in)

1. V147: `Dimensions.Of` takes the up axis of the camera, so the lines follow the eye with z up.
2. V149: the top bar cuts the name of the file to the room left of the rows.
3. V150 and V151: the comments of the panel and of the yaw and the pitch say what the code does.

## Scope (out)

- The command line (the next slice of the owner).

## Acceptance criteria

- [ ] Each finding has a test, an injection or a reading that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: a test, an injection or a reading for each finding, before the change.
Judgement:  the dimension lines change the roles of y and z with a flag of the camera, not a turn of the box; the room
            of the name keeps at least 48 pixels.
Weakest:    to be written at the close.

## Progress
- 2026-11-08: the fixes and their tests. With the lines of y up again, the new test of the dimensions fails; with the
  name cut to the width of the rows only, the new test of the top bar fails at 844 x 390. V150 and V151 are comments.
