---
id: 0282
title: The asset verb, the second ring, three comments and the dialog filter hold their next forms
status: Ready
phase: P0.117
opened: 2026-11-07
depends-on: [0281]
governed-by: [0002, 0007, 0016, 0017, 0018, 0019, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0081, 0083, 0084, 0086, 0087, 0088]
writes:
  create:
    - tasks/TASK-0282-low-forms-of-the-thirtieth-review.md
  modify:
    - Engine.Cli/Cli.cs
    - Engine.Tests/Assets/AssetStoreTests.cs
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

- [ ] Each finding has a test, an injection or a reading that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: to be written.
Judgement:  to be written.
Weakest:    to be written at the close.

## Progress
