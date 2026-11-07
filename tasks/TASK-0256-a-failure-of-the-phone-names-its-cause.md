---
id: 0256
title: A failure of the window of the phone and a new create of its activity name their cause
status: Done
phase: P0.104
opened: 2026-11-07
depends-on: [0255]
governed-by: [0017, 0023, 0025, 0074]
writes:
  create:
    - tasks/TASK-0256-a-failure-of-the-phone-names-its-cause.md
  modify:
    - 3DEngine.Vulkan/Window.cs
    - 3DEngine.ImageTests/BodyActionsTests.cs
    - eng/android/smoke-emulator.sh
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0256 — A failure of the window of the phone and a new create of its activity name their cause

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Run 42313242154 of the branch of P0.102 failed in two jobs. On the emulator, Android destroyed the activity about one
second after its create and created it again, as in runs 42308415060 and 42310351172. The session of the destroyed
activity made no window (TASK-0255 held), but the new session failed at "SDL: failed to create vulkan surface", with no
text of SDL. No line of the job said why Android created the activity again. On the Windows runner the test
`The_Entry_Of_The_Face_Does_Not_Wait_For_A_Long_Command` failed at its condition "The read did not start" in 5 seconds.

## Goal

The next failure names its cause: the text of SDL, and the lines of Android about a new create of the activity.

## Scope (in)

1. Each failure of SDL in `Window` gives the text of `SDL_GetError`.
2. When the activity is destroyed, the smoke test gives the lines of Android about a relaunch and a change of the
   configuration, and the last lines of the program, as warnings.
3. The condition of the test of V109 waits 30 seconds.

## Scope (out)

- The correction of the new create of the activity and of the surface, which needs the cause first.

## Acceptance criteria

- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0104-crash-causes) · v0.239

## Method

Mechanical: a probe of the new grep on the line form of the task manager of Android.
Judgement:  diagnostics before a correction, because three runs show the new create and none shows its cause.
Weakest:    the new lines show only in a run where Android creates the activity again; run 42315180361 had no such
            create, so no run shows them yet.

## Progress

- 2026-11-07: opened by the failed run 42313242154.
- 2026-11-07: the text of SDL, the lines of a relaunch and the wait of 30 seconds. Run 42315180361 passed. Closed at v0.239.
