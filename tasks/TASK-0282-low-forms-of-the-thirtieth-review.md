---
id: 0282
title: The asset verb, the second ring, three comments and the dialog filter hold their next forms
status: Done
phase: P0.117
opened: 2026-11-07
depends-on: [0281]
governed-by: [0002, 0007, 0016, 0017, 0018, 0019, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0081, 0083, 0084, 0086, 0087, 0088]
writes:
  create:
    - tasks/TASK-0282-low-forms-of-the-thirtieth-review.md
    - 3DEngine.ImageTests/FileDialogStateTests.cs
  modify:
    - Engine.Cli/Cli.cs
    - Engine.Tests/Export/StlExportTests.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.Sketch.cs
    - 3DEngine.ImageTests/ToolRingTests.cs
    - 3DEngine.Vulkan/Window.FileDialog.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0282 — The asset verb, the second ring, three comments and the dialog filter hold their next forms

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-07 (c) found four low findings: E185, V137, V138 and V139. They come after TASK-0281.

## Goal

E185, V137, V138 and V139 are fixed, each with a test, an injection or a reading.

## Scope (in)

1. E185: the verb `asset` checks its folder as the other verbs do.
2. V137: a test of the second ring when the selection changes.
3. V138: the three comments say what the code does.
4. V139: a late callback of an earlier dialog does not touch the filter of the next dialog.

## Scope (out)

- The other findings.

## Acceptance criteria

- [x] Each finding has a test, an injection or a reading that fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0117-low-forms) · v0.267

## Method

Mechanical: E185 failed on the code before the change; injections of a ring that the selection does not close and of
            a late callback that gives its result each fail a test.
Judgement:  V139 moves the state of the dialog into FileDialogState, so a test reaches it with no dialog of SDL; the
            test of the asset verb is beside the tests of the blank folder of the other verbs.
Weakest:    no run on a computer with a file dialog showed a late callback; the test drives FileDialogState with no
            dialog of SDL.

## Progress
- 2026-11-07: E185, V137, V138 and V139. The test of E185 failed before the change with ArgumentException. Injections of a
  ring that the selection does not close and of a late callback that gives its result each fail a test.
- 2026-11-07: run 42379027742 passed. Closed at v0.267.
