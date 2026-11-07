---
id: 0284
title: The ring of a sketch sees the windows that come after it, and a window at the left does not pull it under the panel
status: Active
phase: P0.118
opened: 2026-11-07
depends-on: [0283]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0086, 0087, 0088]
writes:
  create:
    - tasks/TASK-0284-the-ring-of-a-sketch-and-the-windows-after-it.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.ImageTests/ToolRingTests.cs
    - docs/adr/0086-the-tools-of-the-sketch-on-the-ring.md
    - docs/adr/0088-the-window-of-a-sketch-beside-its-ring.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0284 — The ring of a sketch sees the windows that come after it, and a window at the left does not pull it under the panel

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The review of Codex of v0.265 gave two medium leads in the fix of V134 (TASK-0280), and the session confirmed both in the
code:

- The Images window and the message come after the ring in the frame, so the check of the segments did not see them. With
  the Images window open over Line, the window of the sketch dropped its Line button.
- The centre of the ring moved left to the window of the sketch with no limit. A window that the person moved to the left,
  or collapsed there, put each segment under the panel, and a collapsed window shows no button.

A medium finding comes after the next product phase. That phase, K5c, waits for the owner (ADR-0089), and the order of the
owner takes the next ready work when the queue is empty, so this task comes now. The lead of Codex of v0.264 (TASK-0281
was Ready before the product phase) is a record of the past: TASK-0281 and its entry v0.266 say why it came first.

## Goal

Each tool of the ring of a sketch keeps a place that a tap reaches, with any window of the desktop open or moved.

## Scope (in)

1. The check of the segments reads the windows of the last frame too.
2. The centre moves left to the window of the sketch only when the band then stays to the right of the panel.

## Scope (out)

- The other leads of Codex, which go into the next codebase review.

## Acceptance criteria

- [ ] With the Images window open over Line, the window of the sketch keeps Line.
- [ ] A window of the sketch at x 500 does not pull the band under the panel.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the two tests, and an injection of each old form.
Judgement:  the windows of the last frame, because the order of the frame puts the Images window and the message after
            the ring; a window that moves in this frame counts from the next frame.
Weakest:    to be written at the close.

## Progress

- 2026-11-07: the two leads of Codex of v0.265, confirmed in the code. Two new tests; injections of a check with no windows
  of the last frame and of a centre with no limit each fail a test. A tap on the arrow of the Images window needs the
  button on the release sample of a mouse, or the next move drags the window.
