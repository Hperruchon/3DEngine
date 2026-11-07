---
id: 0286
title: A surface lost while the phone engine starts ends the engine as the background does
status: Done
phase: P0.120
opened: 2026-11-07
depends-on: [0285]
governed-by: [0007, 0019, 0025, 0029, 0061, 0072, 0073, 0079, 0083]
writes:
  create:
    - tasks/TASK-0286-a-lost-surface-in-the-start-of-the-phone-engine.md
  modify:
    - 3DEngine.Android/PhoneHost.cs
    - 3DEngine.App/PhoneLifecycle.cs
    - 3DEngine.ImageTests/PhoneLifecycleTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0286 — A surface lost while the phone engine starts ends the engine as the background does

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The audit of Codex of the phone host at `d5209a2` gave one high lead, and the session confirmed it in the code.
`PhoneHost.cs` called `engine.Initialize()` with no guard; only `engine.Render()` had the guard of a lost surface (V124,
TASK-0265). When the activity goes into the background again while the new engine makes its device, the query of the
surface gives `ErrorSurfaceLostKHR`, the exception reaches the outer catch, the session ends, and the document of the phone,
which has no file, is lost.

The review of 2026-11-07 (b) named this gap in its line of V124, and the counts of that review and of the review of
2026-11-07 (c) took V124 as fixed, so no finding held it. A confirmed high lead comes before the next product phase.

## Goal

A surface lost in the start of the phone engine ends the engine as the background does, and the document stays.

## Scope (in)

1. `PhoneLifecycle.IsSurfaceLost`, by the result of Vulkan, for the start and for each frame.
2. The guard of the start of the engine in `PhoneHost.cs`, with the same next step as a frame (E184).
3. A test of the helper, and a test that reads the source of the host for the two guards.

## Scope (out)

- A file of the document on the phone.

## Acceptance criteria

- [x] The test of the source fails with the host before the change.
- [x] The phone project builds with zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0120-lost-surface-start) · v0.271

## Method

Mechanical: the test of the source on the old host; an injection of the wrong result in the helper.
Judgement:  the result of the exception, not its message: the message names the result ("[-1000000000]
            ErrorSurfaceLostKHR - ..."), so the two tests were the same, and the result is typed.
Weakest:    the phone host runs on no computer here, so the test reads its source for the guards; no run on a device
            lost the surface in the start of an engine.

## Progress

- 2026-11-07: the guard of the start, the helper and two tests. The test of the source failed with the old host; an
  injection of the wrong result in the helper fails its test. The phone project builds with zero warnings.
- 2026-11-07: run 42396688289 passed. Closed at v0.271.
