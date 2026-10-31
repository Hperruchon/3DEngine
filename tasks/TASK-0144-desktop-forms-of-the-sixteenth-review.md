---
id: 0144
title: An arc ends within a pixel of its start, and Escape cancels a press on a handle before the slop
status: Done
phase: P0.62
opened: 2026-10-31
depends-on: [0141]
governed-by: [0009, 0022, 0023, 0026, 0029, 0032, 0034, 0038, 0041]
writes:
  create:
    - tasks/TASK-0144-desktop-forms-of-the-sixteenth-review.md
  modify:
    - 3DEngine.Core/Sketching/SketchTool.cs
    - 3DEngine.Core/Input/IntentMachine.cs
    - 3DEngine.Tests/SketchToolTests.cs
    - 3DEngine.Tests/GizmoTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Api.Http/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
---

# TASK-0144 — An arc ends within a pixel of its start, and Escape cancels a press on a handle before the slop

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-31 (b) found V63 and V64 (low). It comes after R11b (TASK-0134), the next product phase, by `docs/templates.md`, section 7; it starts only when R11b is merged. R11b changes `IntentMachine.cs` and
`SketchTool.cs` too.

## Goal

The tool Arc ends the curve when the new end is within the hit distance of the start, and Escape before the slop makes
the rest of a press on a handle give nothing.

## Scope (in)

1. V63: `SketchTool` ends the curve when the new end is within `HitPixels` of the start, in pixels.
2. V64: `CancelGizmo` marks a press that has not started as cancelled, and gives `GizmoCancelIntent` only for a
   drag that started.

## Scope (out)

- A change of the engine.

## Acceptance criteria

- [x] A test of each finding fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p062-desktop-forms) · v0.146

## Method

Mechanical: a tool script for each finding, and each fix undone against its test.
Judgement:  the scale in pixels comes from the tap and the start, a local measure that a perspective view bends a
            little; an end within 22 pixels of the start ends the curve, as a tap on the start does.
Weakest:    no person tried the arc or the cancel with the real window; the tests drive the tool and the intent
            machine with scripts.

## Progress

- 2026-10-31: opened by the codebase review of 2026-10-31 (b).
- 2026-10-31: R11b merged; the task starts. The tool Arc ends the curve when the new end is within HitPixels of the
  start, with the scale of the tap and the start; the pad gives no pixel and keeps the limit of V61. Escape before the
  slop cancels the press on a handle with no intent. The first form of the rule in pixels ended each arc of the pad,
  because the scale was 0 with no pixel; the GPU test of the curves showed it, and a test of the pad holds it now. 3 new
  test cases; 3 injections caught.
- 2026-10-31: main merged into the branch (P0.60 and P0.61, v0.144 and v0.145); 3DEngine.Tests counts 226 + 3 = 229.
- 2026-10-31: closed at v0.146.
