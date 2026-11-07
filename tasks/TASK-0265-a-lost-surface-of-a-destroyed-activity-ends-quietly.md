---
id: 0265
title: A lost surface of a destroyed activity ends the session quietly, and the activity names its change
status: Done
phase: P0.108
opened: 2026-11-07
depends-on: [0262]
governed-by: [0061, 0073, 0079]
writes:
  create:
    - tasks/TASK-0265-a-lost-surface-of-a-destroyed-activity-ends-quietly.md
  modify:
    - 3DEngine.Android/PhoneHost.cs
    - 3DEngine.Android/MainActivity.cs
    - docs/adr/0079-the-activity-may-start-again.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0265 — A lost surface of a destroyed activity ends the session quietly, and the activity names its change

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-07 added V124 (high) from run 42323457861 of `main`: Android destroyed the activity a
second time after the first frame of the new session, and that session ended with `ErrorSurfaceLostKHR`. A failed run of
`main` comes before the next product phase, the close of P9c.

## Goal

A lost surface after a destroy of the activity does not end the program with a failure, and the next run names the
change of the configuration that destroys the activity.

## Scope (in)

1. V124: a lost surface ends the engine as the background does, or ends the session quietly when its turn is stale.
2. The activity writes `IsChangingConfigurations` and `ChangingConfigurations` at its destroy.

## Scope (out)

- The other findings of the review (TASK-0262, TASK-0263, TASK-0264).

## Acceptance criteria

- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0108-lost-surface-b) · v0.246

## Method

Mechanical: the phone project builds; the next runs of the emulator show the line of the destroy and the end of a session
            with a lost surface.
Judgement:  a lost surface ends the engine as the background does, so a session whose turn stays the last one waits for
            the foreground and makes a new engine, and a session whose turn is stale ends quietly; the exception is
            matched by the name of its result, because Vortice gives no result field.
Weakest:    no run since the change had a new create of the activity, so neither the end of a lost surface nor the line
            of the change of the configuration has run on a device or the emulator yet.

## Progress

- 2026-11-07: opened by the codebase review of 2026-11-07.
- 2026-11-07: the catch of a lost surface, the wait that ends a stale session, and the line of the destroy with the change
  of the configuration; the phone project builds.
- 2026-11-07: run 42325543893 passed, with one create of the activity. Closed at v0.246.
