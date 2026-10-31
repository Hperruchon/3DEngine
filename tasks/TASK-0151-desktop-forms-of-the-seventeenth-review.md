---
id: 0151
title: A cancelled press gives nothing, and the mark of the snap is where the point goes and only while a finger is down
status: Done
phase: P0.65
opened: 2026-10-31
depends-on: [0148]
governed-by: [0007, 0009, 0019, 0022, 0023, 0026, 0029, 0032, 0034, 0038, 0041]
writes:
  create:
    - tasks/TASK-0151-desktop-forms-of-the-seventeenth-review.md
    - 3DEngine.Core/Input/HoverMark.cs
    - 3DEngine.Tests/HoverMarkTests.cs
  modify:
    - 3DEngine.Core/Input/IntentMachine.cs
    - 3DEngine.Core/Input/Intents.cs
    - 3DEngine.Core/Sketching/SketchTool.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine/SketchActions.cs
    - 3DEngine.Tests/GizmoTests.cs
    - 3DEngine.Tests/SketchToolTests.cs
    - 3DEngine.Tests/IntentParityTests.cs
    - docs/adr/0041-snap-of-a-sketch-tap.md
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

# TASK-0151 — A cancelled press gives nothing, and the mark of the snap is where the point goes and only while a finger is down

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-31 (c) found V65 to V67 (low). It comes after V2, the next product phase, by `docs/templates.md`, section 7; it starts only when V2 is merged.

## Goal

A press that Escape cancelled gives no intent, the mark at the end of an arc is on the circle, and the mark goes when a finger lifts.

## Scope (in)

1. V65: a press that Escape cancelled gives no click and no tap, for the mouse and for a touch.
2. V66: `SketchTool.Target` projects onto the circle for the end of an arc, as `PlaceCurve` does; ADR-0041 item 6 says it.
3. V67: the comment of `SketchHoverIntent` is true, and the host clears the mark on a lift, a cancel and a change of mode.

## Scope (out)

- A change of the engine.

## Acceptance criteria

- [x] Each finding has a test that fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p065-desktop-forms) · v0.155

## Method

Mechanical: each probe of the review as a test case, with a control, and each fix undone against its test (4
            injections).
Judgement:  the target of an arc end is null where the tap ends the curve; the last pointer sample of a frame decides
            the mark; the release of the mouse keeps it.
Weakest:    the host clears the mark on a change of mode, and no test drives the host; the rule of the samples is in
            HoverMark, which the tests reach.

## Progress
- 2026-10-31: V65 to V67. The release of a cancelled press gives no point; `SketchTool.Target` is a method of the
  tool and shares the end of an arc with the tap; `HoverMark` clears the mark after a lift of a finger or a pen and
  after a cancel, and the host clears it on a change of mode. 11 new test cases in 3DEngine.Tests.
- 2026-10-31: closed at v0.155.
