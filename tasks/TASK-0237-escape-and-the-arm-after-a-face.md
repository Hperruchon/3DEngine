---
id: 0237
title: The key Escape goes back from the face, and the button "Sketch on face" ends the arm of Face
status: Active
phase: P0.94
opened: 2026-11-06
depends-on: [0236]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0026, 0029, 0032, 0034, 0038, 0041, 0072]
writes:
  create:
    - tasks/TASK-0237-escape-and-the-arm-after-a-face.md
  modify:
    - 3DEngine.Core/Input/IntentMachine.cs
    - 3DEngine.Core/Input/Intents.cs
    - 3DEngine.App/FaceArm.cs
    - 3DEngine.Tests/IntentParityTests.cs
    - 3DEngine.Tests/SdlInputTests.cs
    - 3DEngine.ImageTests/BodyActionsTests.cs
    - 3DEngine.ImageTests/FaceArmTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0237 — The key Escape goes back from the face, and the button "Sketch on face" ends the arm of Face

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The review of Codex of v0.221 (22143bd) gives three findings. The session read the code for each finding.

- Finding 1, medium, confirmed. ADR-0075 item 2 says that `CancelIntent`, the key Escape, goes back from the face to
  its body. `IntentMachine` gives `CancelIntent` only for a lost pointer. The key Escape cancels a press on a handle of
  the gizmo, and with no such press it gives nothing. The test of R12a gave `CancelIntent` directly, so it did not see
  this. The ledger entry of v0.221 says that the key Escape gives the body; that statement is false.
- Finding 2, medium, confirmed. `FaceArm.Panel` ends the arm of the button Face for each start of a sketch (finding
  V77), but not for the new button "Sketch on face". After Face, "Sketch on face" and Finish, the next tap on a body
  starts one more sketch.
- Finding 3, low: the outline of the face does not follow a drag of the gizmo. It goes into the next codebase review.

## Goal

The key Escape goes back from the face to its body, and each start of a sketch ends the arm of Face.

## Scope (in)

1. The key Escape with no press on a handle of the gizmo gives `CancelIntent`, in each mode.
2. `SketchOnSelectedFaceAction` ends the arm of Face, as the other starts of a sketch do.

## Scope (out)

- Finding 3: the next codebase review.

## Acceptance criteria

- [ ] A key sample of Escape through the intent layer clears the selected face.
- [ ] The button "Sketch on face" ends the arm.
- [ ] An injection of each defect fails a test.

## Outcome

Status: Active

## Method

Mechanical: one case of the key Escape, and one action in the list of the starts of a sketch.
Judgement:  Escape gives the existing `CancelIntent`, which ADR-0075 names, and no new kind of intent. A lost pointer
            during a press also gives `CancelIntent`, so it also clears the face.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: the two fixes; the injection of each defect fails a test.
