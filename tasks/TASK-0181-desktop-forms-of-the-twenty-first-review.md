---
id: 0181
title: The far plane holds each visible body from the eye, and the dimension overlay keeps its labels apart and follows a drag
status: Done
phase: P0.78
opened: 2026-11-05
depends-on: [0180]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0027, 0029, 0030, 0031, 0032, 0034, 0043, 0056]
writes:
  create:
    - tasks/TASK-0181-desktop-forms-of-the-twenty-first-review.md
  modify:
    - 3DEngine.Core/Models/Camera.cs
    - 3DEngine/SceneRenderer.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine/DimensionOverlay.cs
    - 3DEngine/SessionBodies.cs
    - 3DEngine/Ui/DesktopUi.cs
    - 3DEngine.Tests/CameraTests.cs
    - 3DEngine.ImageTests/DimensionOverlayTests.cs
    - 3DEngine.ImageTests/DimensionImageTests.cs
    - 3DEngine.ImageTests/FaceArmTests.cs
    - docs/adr/0056-dimension-overlay.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0181 — The far plane holds each visible body from the eye, and the dimension overlay keeps its labels apart and follows a drag

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-05 found V78 (medium) in the far plane of P0.77, and V79 to V84 (low) in the dimension
overlay of R13 and in the arm of the button Face. By `docs/templates.md`, section 7, they come after the next product
phase. On 2026-11-05 the owner answered A to Q1 and Q2 of the review, and then, because the redesign of the desktop
is still work in progress in another session, gave the order "Ok" to the recommendation: P0.78 and P0.79 now, before
the next product phase, and then K4. Q2 A amends ADR-0056 items 2 and 7 to the code (V83).

## Goal

A hide never makes a visible body vanish, each label of a dimension shows apart and inside the window, the lines follow
a drag, the overlay reads the boxes once for each change, and a test drives the call of the arm.

## Scope (in)

1. V78: the far plane from the eye, with a test of the hide of the review.
2. V79, V80: the place of a label.
3. V81: a kept union. V82: the offset of a drag.
4. V83: ADR-0056 items 2 and 7, by Q2.
5. V84: the call of the arm in a class that a test drives.

## Scope (out)

- E131 to E133 and T133 to T135 (TASK-0182).

## Acceptance criteria

- [x] Each finding has a test or an injection that fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p078-desktop-forms) · v0.183

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  the margin of the far plane, and the rule that keeps two labels apart.
Weakest:    no one opened the desktop window, and no test drives the two lines of the host that read the arm.

## Progress

- 2026-11-05: opened by the codebase review of 2026-11-05.
- 2026-11-05: the seven findings, each with a test, ten injections; closed at v0.183.
