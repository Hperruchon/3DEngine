---
id: 0271
title: The home screen, the rows, the chips, the dialog and three comments hold their next forms
status: Done
phase: P0.111
opened: 2026-11-07
depends-on: [0274]
governed-by: [0007, 0017, 0019, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085]
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
    - 3DEngine.ImageTests/ToolRingTests.cs
    - docs/adr/0034-sketch-tools.md
    - docs/adr/0081-the-tool-ring-of-the-redesign.md
    - docs/adr/0083-the-mesh-body-and-the-stl-import.md
    - docs/adr/README.md
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
2. Three leads of Codex, checked on disk: the window of the live sketches covers the rows and a long list of values
   goes past the display (v0.252), and the fourth label of a portrait ring goes past the right edge (v0.251).

## Scope (out)

- E175 to E179 and T168 to T173 (TASK-0272).

## Acceptance criteria

- [x] Each finding has a test or an injection that fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0111-desktop-forms) · v0.258

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  V129: two lines in the first row, with a fallback that keeps "failed" and the code whole. The window of
            the live sketches gives way to the rows while a body is selected (the standing order: the rows are the
            slice of the brief; "Edit" of a sketch stays with no selection). V127 and V131 change the engine, which
            needs a window, so no test holds them.
Weakest:    V127 and V131 change the engine, which needs a window, so no test holds them; while a body is selected,
            "Edit" of a sketch needs a tap on the empty scene first, because the rows take the place of its window.

## Progress

- 2026-11-07: opened by the codebase review of 2026-11-07 (b).
- 2026-11-07: V127 to V133 and three leads of Codex. Seven new tests with no GPU and a stronger picture test of the home
  screen. Injections: no separation of the chips, no end of the rows, the window over the rows, Move on a mesh body, no
  clamp of a label and the samples of the home screen each fail their test.
- 2026-11-07: run 42366757388 passed. Closed at v0.258.
