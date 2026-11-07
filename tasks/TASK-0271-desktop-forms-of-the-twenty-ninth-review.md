---
id: 0271
title: The home screen, the rows, the chips, the dialog and three comments hold their next forms
status: Ready
phase: P0.111
opened: 2026-11-07
depends-on: [0270]
governed-by: [0007, 0017, 0019, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084]
writes:
  create:
    - tasks/TASK-0271-desktop-forms-of-the-twenty-ninth-review.md
  modify:
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Home.cs
    - 3DEngine.App/Ui/DesktopUi.Rows.cs
    - 3DEngine.App/Ui/DesktopUi.Values.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.Vulkan/Window.FileDialog.cs
    - 3DEngine/DesktopDocument.cs
    - 3DEngine.ImageTests/BodyRowsTests.cs
    - 3DEngine.ImageTests/HomeImageTests.cs
    - 3DEngine.ImageTests/StlOpenTests.cs
    - docs/adr/0084-the-rows-of-the-selected-body.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0271 — The home screen, the rows, the chips, the dialog and three comments hold their next forms

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-07 (b) found V127 to V129 (medium) and V130 to V133 (low) in the desktop. By
`docs/templates.md`, section 7, they come after the next product phase.

## Goal

A failed open on the home screen shows its message, the chips under the rows stay apart, the state of a body leaves its
name in the rows, the home screen holds the scene, a failed dialog says so, and the comments are true.

## Scope (in)

1. V127 to V133.

## Scope (out)

- E175 to E179 and T168 to T173 (TASK-0272).

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

- 2026-11-07: opened by the codebase review of 2026-11-07 (b).
