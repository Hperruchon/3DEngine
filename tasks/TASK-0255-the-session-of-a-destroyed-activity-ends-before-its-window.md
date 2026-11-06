---
id: 0255
title: The session of a destroyed activity ends before its window
status: Done
phase: P0.103
opened: 2026-11-07
depends-on: [0251]
governed-by: [0007, 0019, 0025, 0029, 0061, 0072, 0073, 0079]
writes:
  create:
    - tasks/TASK-0255-the-session-of-a-destroyed-activity-ends-before-its-window.md
    - 3DEngine.App/SessionTurns.cs
    - 3DEngine.ImageTests/SessionTurnsTests.cs
  modify:
    - 3DEngine.Android/PhoneHost.cs
    - 3DEngine.Android/MainActivity.cs
    - docs/adr/0079-the-activity-may-start-again.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0255 — The session of a destroyed activity ends before its window

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Run 42308415060, the run of `main` at `5b84a10` (the merge of v0.236), failed in the job "Start the APK on the
emulator". The activity was created, its session started the probe of ADR-0074, and Android destroyed the activity 0.7
seconds later and created it again. With the hint of ADR-0079 a second session started in the same process. The two
probes ended at the same time, and the second session made its window first; the first session then could not make its
window ("SDL: failed to create window"), and its line "the session of the phone ended" failed the job. The hashes agreed.
SDL on Android permits one window. Run 42310351172 of the branch of P0.102 gave the other order: the session of the
destroyed activity made its window, and the new session could not make one. A failed run of `main` comes before the next
product phase, so this task comes before P0.102.

## Goal

A session whose activity was destroyed ends before its window, and a session waits until the window of an earlier
session of the process is gone.

## Scope (in)

1. Each create of the activity takes a turn; a session whose turn is not the last one ends before its window, with one
   line.
2. A session holds the window from its make to its dispose; a later session waits for it, for 10 seconds at most.
3. Tests of the turns and of the wait, with the order of the run.

## Scope (out)

- The values on the edges of the redesign (TASK-0254), which come after this task.

## Acceptance criteria

- [x] Each change has a test or an injection that fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0103-stale-session-b) · v0.238

## Method

Mechanical: a test or an injection for each change, before the change.
Judgement:  the turns and the hold live in `3DEngine.App`, as `PhoneLifecycle` does, so that a test holds them with no
            device; `PhoneHost` and `MainActivity` only call them.
Weakest:    no run can force a new create of the activity during the probe, so the order of the two failed runs is
            held by a test of the turns with no device, and the next runs of the emulator show the rest.

## Progress

- 2026-11-07: opened by the failed run 42308415060 of `main`.
- 2026-11-07: `SessionTurns`, the check before and after the hold of the window, and the turn of each create; three tests.
  An injection of a turn that is always the last one and of a hold that does not wait fails the three tests.
- 2026-11-07: run 42311258959 passed. Closed at v0.238.
