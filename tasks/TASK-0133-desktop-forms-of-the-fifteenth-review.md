---
id: 0133
title: The pad of an angle takes degrees, and the tools and the window follow the review
status: Done
phase: P0.53
opened: 2026-10-31
depends-on: [0131]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0026, 0027, 0029, 0030, 0032, 0034, 0038, 0041]
writes:
  create:
    - tasks/TASK-0133-desktop-forms-of-the-fifteenth-review.md
  modify:
    - 3DEngine/Ui/DesktopUi.cs
    - 3DEngine.Core/Sketching/SketchTool.cs
    - 3DEngine.Core/Input/IntentMachine.cs
    - 3DEngine.Tests/SketchToolTests.cs
    - 3DEngine.Tests/GizmoTests.cs
    - 3DEngine.ImageTests/SketchWindowUiTests.cs
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

# TASK-0133 — The pad of an angle takes degrees, and the tools and the window follow the review

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-31 found V56 (medium) and V57 to V62 (low) in the desktop. `docs/templates.md`,
section 7, puts them after the next product phase, and R11a changes `SketchTool.cs`, so this task comes after it.

## Goal

The Value pad of an angle takes degrees, the tools read the current positions, a press on a handle keeps the view
still, and the window offers only the kinds that the engine accepts.

## Scope (in)

1. V56: the Value pad of an angle in degrees; a tangent of two curves has no Value button. V60: `CosineAndSine` from
   the turn, and a value that is not finite is refused.
2. V57: the centre and the start from the picture at each tap. V61: a new end in the direction of the start ends the
   curve. V62: the nearest of the lines and the curves.
3. V58: `Dragging` from a press on a handle. V59: the fit rules take the item checks of `SketchConstraints.Refusal`.

## Scope (out)

- A change of the engine.

## Acceptance criteria

- [x] Each form of the review fails a test.
- [x] An injection that restores each old rule fails a test.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p053-desktop-fifteenth) · v0.135

## Method

Mechanical: the pad of an angle, the item checks of the window, the turn of CosineAndSine, the current positions of a
            curve, the end of an arc on the ray of its start, the nearer of a line and a curve, and the press on a
            handle.
Judgement:  a press on a handle drops the other input from the press on, as a drag does (Q2 A of the fourteenth
            review), because the anchor of the drag is the press.
Weakest:    no person tried the new pad with the real window.

## Progress

- 2026-10-31: opened by the codebase review of 2026-10-31 (TASK-0129).
- 2026-10-31: V56 to V62 with 6 new test cases; 9 injections caught.
- 2026-10-31: closed at v0.135.
