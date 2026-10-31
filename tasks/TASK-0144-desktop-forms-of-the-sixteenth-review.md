---
id: 0144
title: An arc ends within a pixel of its start, and Escape cancels a press on a handle before the slop
status: Ready
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

- [ ] A test of each finding fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-31: opened by the codebase review of 2026-10-31 (b).
