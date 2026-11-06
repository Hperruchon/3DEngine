---
id: 0252
title: The cache of a face, the fixed controls, the list New, the edges and the safe area hold their next forms
status: Ready
phase: P0.102
opened: 2026-11-06
depends-on: [0251]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081]
writes:
  create:
    - tasks/TASK-0252-desktop-and-phone-forms-of-the-twenty-seventh-review.md
  modify:
    - 3DEngine.App/BodyActions.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.ImageTests/FixedControlsTests.cs
    - 3DEngine.ImageTests/EdgeSelectionTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0252 — The cache of a face, the fixed controls, the list New, the edges and the safe area hold their next forms

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-06 (e) found V113 to V118 (low). By `docs/templates.md`, section 7, they come after the
next product phase, and after P0.101.

## Goal

A fault clears the kept face, the panel does not cover the anchors, the title keeps the file name, a tap outside the
open list closes it, a tap near an edge outside the silhouette selects the edge, and the fixed controls stay inside the
safe area of the phone.

## Scope (in)

1. V113: the kept entry of a body with a fault.
2. V114: the panel and the anchors.
3. V115: the title bar and a long file name.
4. V116: the open list New and a tap on the scene.
5. V117: a tap near an edge outside the silhouette.
6. V118: the safe area of the phone.

## Scope (out)

- The findings of TASK-0250 and TASK-0251.

## Acceptance criteria

- [ ] Each finding has a test or an injection that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  to be written in the task.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: opened by the codebase review of 2026-11-06 (e).
