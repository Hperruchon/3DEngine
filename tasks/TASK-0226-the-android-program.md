---
id: 0226
title: The Android program builds an APK that draws a document of its own session
status: Ready
phase: R14e2
opened: 2026-11-06
depends-on: [0223]
governed-by: []
writes:
  create:
    - tasks/TASK-0226-the-android-program.md
  modify:
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0226 — The Android program builds an APK that draws a document of its own session

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Phase R14e of ADR-0061 item 9 is the Android host. R14e1 (TASK-0222, ADR-0072) made the library `3DEngine.App`, and a
probe APK that references it builds for `android-arm64`. R14d published the packages of Manifold 3.5.2.2 and of cimgui
1.91.6.1 for Android. The codebase review of 2026-11-06 (b) gives V88 to this task: no program ships a file of 32-bit
x86 (`CLAUDE.md`, determinism rule 5). The first commit of this task gives the write set and the field `governed-by`.

## Goal

A `net10.0-android` program for `android-arm64` and `android-x64` builds an APK that composes a session of its own and
draws its document with `3DEngine.App`.

## Scope (in)

1. The project, an activity that subclasses `SDLActivity`, a session of its own with an `ISessionHost`, and the APK.
2. The packages of R14d in `nuget/`, with the checksums of the notices.
3. The gates: the class of the program, the rule of ADR-0072 for it, the list of its packages, and V88.

## Scope (out)

- The picker, the loss of the surface, the pre-rotation, the pause and the test sheet (later slices of R14e).

## Acceptance criteria

- [ ] The APK builds on this computer with zero warnings, and holds the native libraries of SDL, cimgui and Manifold for both ABIs and no file of x86.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: the probe APK of the review 24.
Judgement:  to be written in the task.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: opened by the codebase review of 2026-11-06 (b).
