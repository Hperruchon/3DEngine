---
id: 0312
title: The command line at the upper right runs the commands of the state with Control and K
status: Active
phase: R16o
opened: 2026-11-08
depends-on: [0311]
governed-by: [0007, 0009, 0017, 0019, 0022, 0023, 0025, 0026, 0027, 0029, 0030, 0032, 0034, 0038, 0041, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0086, 0087, 0089, 0091, 0093, 0097, 0098]
writes:
  create:
    - tasks/TASK-0312-the-command-line.md
    - docs/adr/0098-the-command-line.md
    - 3DEngine.App/Ui/DesktopUi.Command.cs
    - 3DEngine.ImageTests/CommandLineTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Rows.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.Core/Input/InputSamples.cs
    - 3DEngine.Vulkan/SdlInput.cs
    - 3DEngine.Tests/SdlInputTests.cs
    - 3DEngine.ImageTests/BodyRowsTests.cs
    - docs/adr/0032-numeric-pad-and-evaluator.md
    - docs/adr/0084-the-rows-of-the-selected-body.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0312 — The command line at the upper right runs the commands of the state with Control and K

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The fifth slice that the owner chose on 2026-11-08 is the command line (Ctrl K) of the render "mix".

## Goal

Control and K give a field at the upper right the keys; a text lists the commands of the state, and Enter runs one.

## Scope (in)

1. ADR-0098: the key K, the field, the list, the rows under the field, and the release of a key while a pad is open.
2. Tests of the key, of the field, of the list, of a pad from the list, and of Escape.

## Scope (out)

- A value typed in the field (ADR-0098 Q1). Command and K on macOS (Q3). The phone (Q2).

## Acceptance criteria

- [ ] The tests pass, and the picture at 1440 x 900 shows the field at the upper right and the rows under it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the place of the field in the render, and the commands of the buttons and the segments that exist.
Judgement:  commands only, no value (ADR-0098 Q1 A); a landscape display only (Q2 A).
Weakest:    to be written at the close.

## Progress

- 2026-11-08: the slice and its tests. A test of a second command after a pad found that the pad took the release of Enter,
  so the library held Enter as down; the release now reaches the library. With no release, one test fails; with no
  Control and K, four tests fail.
