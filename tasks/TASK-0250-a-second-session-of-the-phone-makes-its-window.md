---
id: 0250
title: A second session of the phone in its process makes its window
status: Done
phase: P0.100
opened: 2026-11-06
depends-on: [0249]
governed-by: [0017, 0023, 0025, 0061, 0073, 0079]
writes:
  create:
    - tasks/TASK-0250-a-second-session-of-the-phone-makes-its-window.md
  modify:
    - 3DEngine.Vulkan/Window.cs
    - 3DEngine.Android/PhoneHost.cs
    - 3DEngine.ImageTests/WindowLifecycleTests.cs
    - docs/adr/0079-the-activity-may-start-again.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0250 — A second session of the phone in its process makes its window

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-06 (e) found V112 (high): with the hint of P0.99 (ADR-0079), a new start of the activity
during a session runs `PhoneHost.Run` again in its process. `Window` has no `Dispose`, so the first SDL window stays, and
SDL on Android permits one window: the second run cannot make its window. A high finding comes before the next product
phase.

## Goal

A session of the phone destroys its window at its end, so a later session in the same process makes its own.

## Scope (in)

1. `Window` gets `Dispose`, which calls `SDL_DestroyWindow` one time.
2. `PhoneHost.Run` disposes its window at the end of the session.
3. A test makes a window, disposes it, and makes a second one.

## Scope (out)

- The other findings of the review (TASK-0251, TASK-0252).

## Acceptance criteria

- [x] Each finding has a test or an injection that fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0100-window-dispose) · v0.235

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  the test uses the video driver "offscreen" of SDL and asks SDL for the window by its identifier, because the
            limit of one window is a rule of SDL on Android only.
Weakest:    the test runs on the video driver "offscreen" of SDL, not on Android, where the limit of one window is; no
            device had a new start of the activity during a session.

## Progress

- 2026-11-06: opened by the codebase review of 2026-11-06 (e).
- 2026-11-06: `Window.Dispose` and the `using` of the phone; one test. An injection of a `Dispose` with no destroy fails it.
- 2026-11-06: run 42302467567 passed, with the test of the window on the Linux runner. Closed at v0.235.
