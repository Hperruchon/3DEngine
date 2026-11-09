---
id: 0324
title: The medium and low findings of the review of 2026-11-09 are fixed
status: Done
phase: P0.136
opened: 2026-11-09
depends-on: [0323]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0086, 0087, 0088, 0089, 0091, 0093, 0097, 0098, 0099, 0102]
writes:
  create:
    - tasks/TASK-0324-medium-and-low-findings-of-the-review-of-2026-11-09.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.Sketch.cs
    - 3DEngine.App/Ui/DesktopUi.Command.cs
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/ProjectFiles.cs
    - 3DEngine.App/Ui/DesktopUi.Image.cs
    - 3DEngine/DesktopSurface.cs
    - 3DEngine.ImageTests/BodyRowsTests.cs
    - 3DEngine.ImageTests/CommandLineTests.cs
    - 3DEngine.ImageTests/ProjectPathTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0324 — The medium and low findings of the review of 2026-11-09 are fixed

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

The write set is a first estimate; the implementer corrects it at the start of the work.

## Context

The codebase review of 2026-11-09 (`docs/reviews/2026-11-09-codebase-review.md`) found no critical and no high finding.
A medium or low finding goes into a task after the next product phase.

## Goal

V152, V153, V154, V155, V156, V157 and E197 are fixed, each with a test; V158 follows the answer of the owner to Q5.

## Scope (in)

1. V152: the window of the live sketches does not cover the command line.
2. V153: each command of a cut names its body as its row does.
3. V154: the folder of the top bar ends left of the window of the images.
4. V155: only the save that the question opened takes the waiting change.
5. V156: the top bar takes the width of the command line only where it shows.
6. V157: no request of the focus waits while the command line does not show.
7. E197: a save of the surface goes into the recent projects, or ADR-0102 item 6 says that it does not.

## Scope (out)

- V158 until the owner answers Q5 of the review.

## Acceptance criteria

- [x] Each finding fails on an injection before its fix and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0136-review-findings) · v0.307

## Method

Mechanical: a test, an injection or a reading for each finding, before the change.
Judgement:  V152 sets the place of the window of the live sketches in each frame, so the person no longer drags it;
            V154 moves the window of the images under the top bar, because a cut of the folder at its left edge hid the
            folder at 1440 pixels; E197 is a step of the desktop host that remembers a file that a request gave the session.
Weakest:    the new places of the two windows have no picture yet; V158 waits for the owner.

## Progress
- 2026-11-09: the fixes and eight tests. An agent made them in a worktree; each test failed on an injection of the old
  behaviour, and each file came back byte for byte. This session reviewed the diff and corrected the write set, which did not
  list the window of the images, the surface of the desktop and the three files of tests.
- 2026-11-09: run 42558072228 passed. Closed at v0.307.
