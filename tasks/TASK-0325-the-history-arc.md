---
id: 0325
title: The history arc shows the steps that made the selected body
status: Active
phase: R16q
opened: 2026-11-09
depends-on: [0324]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0089, 0091, 0093, 0097, 0098, 0099, 0102, 0104]
writes:
  create:
    - tasks/TASK-0325-the-history-arc.md
    - docs/adr/0104-the-history-arc.md
    - 3DEngine.App/Ui/DesktopUi.History.cs
    - 3DEngine.App/HistorySteps.cs
    - 3DEngine.ImageTests/HistoryArcTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.ImageTests/MixImageTests.cs
    - docs/adr/README.md
    - tasks/TASK-0314-a-path-line-from-the-document-to-the-selection.md
    - tasks/TASK-0315-a-focus-on-one-body.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0325 — The history arc shows the steps that made the selected body

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The second of the four slices that the owner chose on 2026-11-08 after v0.297: the history arc of the render "mix".

## Goal

On a landscape display a selected body shows an arc at the lower left of its ring, with one node for each step that made
it, and a tap on a node of a sketch opens its edit.

## Scope (in)

1. ADR-0104: the steps, the tap and the place.
2. `HistorySteps`: the steps of a body from `GetFeatures`, with no GPU, and its tests.
3. The arc in the window, its tests, and the picture of the mix at 1440 by 900 with the arc.

## Scope (out)

- A rollback (an engine need). A portrait display.

## Acceptance criteria

- [ ] The tests pass, and the picture at 1440 by 900 shows the arc at the lower left of the ring with its nodes and caption.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the place of the arc in the render "Mix 1", and the query `GetFeatures`.
Judgement:  the steps of the selected body only (Q1 A); the tap of ADR-0104 item 2 (Q2 A); a landscape display (Q3 A).
Weakest:    to be written at the close.

## Progress

- 2026-11-09: ADR-0104, `HistorySteps`, the arc and eleven tests. An agent made them in a worktree. The review changed
  three things. The nodes were inside the inner guide circle, where the chips of the values covered them; they are on the
  band of the ring now, from 135° down to the bottom, with the caption at the left of the lower end. The frame read the
  version of the Document through `Session.Read`, which takes the lock of the session in each frame; a subscription now
  counts the committed events, and the frame reads that count. The ADR says both. An injection of the old radius failed
  three tests of the arc (the band and the two taps). The task files of TASK-0314 and TASK-0315 take ADR-0104 in
  `governed-by`, because its affects meet their write sets. Local: the gates, a clean build with zero warnings, the tests
  of the solution and the phone project.
