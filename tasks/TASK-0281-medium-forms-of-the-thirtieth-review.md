---
id: 0281
title: The size limit, the lost surface, the taps over a window, the portrait ring and the host warnings hold their next forms
status: Ready
phase: P0.116
opened: 2026-11-07
depends-on: [0280]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0061, 0072, 0073, 0075, 0076, 0077, 0079, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087]
writes:
  create:
    - tasks/TASK-0281-medium-forms-of-the-thirtieth-review.md
  modify:
    - 3DEngine.App/StlOpening.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.Android/PhoneHost.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.Rows.cs
    - 3DEngine.App/Ui/DesktopUi.Values.cs
    - 3DEngine.ImageTests/ToolRingTests.cs
    - eng/android/check-platforms.js
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0281 — The size limit, the lost surface, the taps over a window, the portrait ring and the host warnings hold their next forms

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-07 (c) found six medium findings. E182 waits for the owner (Q1, answered A under the
standing order in TASK-0280). This task holds the other five, after the next product phase.

## Goal

E183, E184, V135, V136 and T174 are fixed, each with a test or an injection.

## Scope (in)

1. E183: the limit of the client is the limit of the store, and a size that SDL does not know is read with that limit.
2. E184: a lost surface with a new part waiting waits for the foreground, as the background does.
3. V135: a release over a window is the tap of the window only, for the ring, the rows and the chips.
4. V136: the portrait ring of a sketch has no segment under an anchor.
5. T174: the step reads `NoWarn` and `WarningsNotAsErrors` of the host, and builds a probe in it.

## Scope (out)

- The low findings (TASK-0282).

## Acceptance criteria

- [ ] Each finding has a test or an injection that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: to be written.
Judgement:  to be written.
Weakest:    to be written at the close.

## Progress
