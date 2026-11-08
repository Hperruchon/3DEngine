---
id: 0294
title: The main function of a stale session of the phone does not return, and a native crash gives its stack
status: Ready
phase: P0.123
opened: 2026-11-08
depends-on: [0293]
governed-by: [0007, 0019, 0025, 0029, 0061, 0072, 0073, 0074, 0079, 0083]
writes:
  create:
    - tasks/TASK-0294-the-stack-of-a-native-crash-of-the-phone.md
    - eng/android/tombstone-lines.ere
  modify:
    - 3DEngine.Android/MainActivity.cs
    - docs/adr/0079-the-activity-may-start-again.md
    - eng/android/smoke-emulator.sh
    - eng/android/native-crash.ere
    - Engine.Tests/Governance/NativeCrashPatternTests.cs
    - 3DEngine.Android/PhoneHost.cs
    - 3DEngine.App/PhoneLifecycle.cs
    - 3DEngine.ImageTests/PhoneLifecycleTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0294 — The main function of a stale session of the phone does not return, and a native crash gives its stack

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Finding V141 of the codebase review of 2026-11-08 (high): the main run of v0.273 (42433201851) ended with a native crash
of the program of the phone on the emulator, after a lost surface in the start of the engine and a second destroy of the
activity. The job gave the line `Cmdline` of the tombstone only. The review infers the cause from the classes of SDL: when
a main function returns, SDL finishes the newest activity and clears its thread, and the stale session of the first
activity returned. A high finding comes before the next product phase.

## Goal

The main function of a stale session does not return, so SDL keeps the newest activity and its thread. A next native
crash in the job names its signal, its thread and the frames of its stack.

## Scope (in)

1. The main function of a session whose turn is stale waits, with a line, and does not return; ADR-0079 item 4 says why.
   A test holds the rule.
2. The smoke test gives the lines of the tombstone (the signal, the thread, the abort message and the first frames of
   the stack) as annotations, in the limit of 10 annotations of each level.

## Scope (out)

- A change of the order of the events of Android.

## Acceptance criteria

- [ ] Each finding has a test, an injection or a reading that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: a test, an injection or a reading for each finding, before the change.
Judgement:  to be written.
Weakest:    to be written at the close.

## Progress
