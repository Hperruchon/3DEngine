---
id: 0177
title: A sketch on a face looks from the side of the eye, the arm of a face ends with a new sketch, and the next forms of four rules fail
status: Active
phase: P0.76
opened: 2026-11-03
depends-on: [0179]
governed-by: [0004, 0007, 0009, 0019, 0022, 0023, 0025, 0029, 0033, 0034, 0035, 0037, 0039, 0041, 0051, 0052, 0055]
writes:
  create:
    - tasks/TASK-0177-desktop-and-gate-forms-of-the-twentieth-review.md
    - 3DEngine/FaceArm.cs
    - 3DEngine.ImageTests/FaceArmTests.cs
  modify:
    - 3DEngine.Core/Sketching/SketchCamera.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine.Tests/SketchToolTests.cs
    - Engine.Core/Sketch/DatumResolver.cs
    - Engine.Tests/Sketch/DatumTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Diagnostics/DiagnosticsScanner.cs
    - Engine.Tests/Diagnostics/DiagnosticsRegistryGateTests.cs
    - Engine.Tests/Governance/CodebaseReviewGateTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0177 — A sketch on a face looks from the side of the eye, the arm of a face ends with a new sketch, and the next forms of four rules fail

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The second codebase review of 2026-11-03 found V76 and V77 (medium) in the desktop of K0i, E130 (low) in the datums,
and T130 to T132 (low) in three gates. By `docs/templates.md`, section 7, they come after the next product phase, R13.
The first commit of this task corrects the write set and the field `governed-by` for the files that its tests need.

## Goal

The camera of a sketch on a face stays on the side of the eye, a tap to select never starts a sketch after a new
sketch, a datum on a short side has axes of the length 1, and the next form of each gate fails.

## Scope (in)

1. V76: the side of the face from `(eye − frame.Origin) · n`, with a test of the camera of the review.
2. V77: the arm of the button Face ends at each start of the mode Sketch and at each change of mode, with a test.
3. E130: the scale of a side whose sum of squares is subnormal.
4. T130 to T132: the forms of the review, each with an injection before the change.

## Scope (out)

- E128 and E129 (TASK-0176).

## Acceptance criteria

- [ ] Each finding has a test or an injection that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  the list of the item names that the rule of T130 refuses.
Weakest:    to be written at the close.

## Progress

- 2026-11-03: opened by the second codebase review of 2026-11-03.
