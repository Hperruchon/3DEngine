---
id: 0243
title: A cancelled finger ends, the activity tells its starts, and the face and the texts hold their next forms
status: Active
phase: P0.98
opened: 2026-11-06
depends-on: [0242]
governed-by: [0007, 0009, 0017, 0019, 0022, 0023, 0025, 0026, 0029, 0031, 0032, 0034, 0038, 0041, 0043, 0061, 0072, 0073, 0074, 0075]
writes:
  create:
    - tasks/TASK-0243-desktop-and-phone-forms-of-the-twenty-sixth-review.md
  modify:
    - 3DEngine.Vulkan/SdlInput.cs
    - 3DEngine.Vulkan/Presenter.cs
    - 3DEngine.App/SceneRenderer.cs
    - 3DEngine.App/SessionBodies.cs
    - 3DEngine.App/PhoneLifecycle.cs
    - eng/android/smoke-emulator.sh
    - 3DEngine.Android/MainActivity.cs
    - 3DEngine.Android/PhoneHost.cs
    - 3DEngine.App/BodyActions.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/FaceOverlay.cs
    - 3DEngine.Core/Input/IntentMachine.cs
    - 3DEngine.Core/Input/Intents.cs
    - 3DEngine.Tests/SdlInputTests.cs
    - 3DEngine.Tests/IntentParityTests.cs
    - 3DEngine.ImageTests/BodyActionsTests.cs
    - docs/adr/0075-a-second-tap-selects-the-face.md
    - docs/android-test-sheet.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0243 — A cancelled finger ends, the activity tells its starts, and the face and the texts hold their next forms

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-06 (d) found V104 and V105 (medium) and V106 to V111 (low). By `docs/templates.md`,
section 7, they come after the next product phase, and after P0.97.

## Goal

A finger that Android cancels ends in the intent layer, the activity writes each start and each end, the line of the
first frame comes after a frame, the outline follows a drag, a lost pointer keeps the face, and the texts are true.

## Scope (in)

1. V104: SDL_EVENT_FINGER_CANCELED gives a touch sample with the phase Cancel.
2. V105: a line for each create, destroy and change of the configuration of the activity, and the missing changes of
   the configuration.
3. V106: the line of the first frame after a frame that drew.
4. V107: the outline of the face with the offset of a drag.
5. V108: a lost pointer does not clear the face.
6. V109: the query of the face does not wait for the serial section at each frame.
7. V110 and V111: the comments, ADR-0075 and TASK-0237.

## Scope (out)

- The findings of TASK-0242.

## Acceptance criteria

- [ ] Each finding has a test or an injection that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  P0.98 before P0.97, because the emulator job failed in 3 of the last 4 runs and V105 holds its probable
            cause; a flag ByKey on CancelIntent, not a new kind of intent, because each kind needs a touch script
            (ADR-0026 item 5); the cache of V109 by the geometry key that the client already holds.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: opened by the codebase review of 2026-11-06 (d).
- 2026-11-06: V104 to V111. A cancelled finger ends; the activity writes each create, change and destroy, it holds eight
  more configuration changes, and the smoke test waits for the launcher; the line of the first frame comes after a
  submitted frame; the outline follows a drag; a lost pointer keeps the face; the entry of the face waits for no
  command; the comments and ADR-0075 say what the code holds. An injection of V104, V107, V108 and V109 each fails a
  test; V105 and V106 have no test on this computer, and the probe of T149 gives the same exits for the smoke test.
