---
id: 0165
title: A tap of the sketch snaps to the intersection of two lines or curves
status: Done
phase: R11c
opened: 2026-11-02
depends-on: [0164]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0029, 0034, 0041]
writes:
  create:
    - tasks/TASK-0165-intersection-snap.md
  modify:
    - 3DEngine.Core/Sketching/SketchTool.cs
    - 3DEngine/SketchOverlay.cs
    - 3DEngine.Tests/SketchToolTests.cs
    - 3DEngine.ImageTests/SketchActionsTests.cs
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

# TASK-0165 — A tap of the sketch snaps to the intersection of two lines or curves

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-11-02 the owner gave the order "start R11". The roadmap row R11 is "Escalated selection and a snap engine. The
rest after R11b." The owner answered two questions on the same day:

- The next slice R11c holds one snap: the intersection of two lines, of a line and a curve, or of two curves.
- The escalated selection of faces and edges moves to a new row after K4, because it needs a stable identity of each
  face and each edge (ADR-0041 item 5). The row R11 closes after R11c.

## Goal

A tap of the tools Line, Circle and Arc near the intersection of two edges of the active sketch takes that
intersection, and the mark of ADR-0041 item 6 shows it before the tap.

## Scope (in)

1. `SketchTool.Snap` takes the nearest intersection within 22 pixels before the midpoint of a line (ADR-0041 item 1).
2. The picture gives the pixel of a position of the plane, and the start and the end of each arc.
3. ADR-0041 items 1, 2 and 5 and its history; the roadmap rows R11, R11c and the new row of the escalated selection.

## Scope (out)

- A change of the engine. A snap records no constraint (ADR-0041 item 3).
- The escalated selection of faces and edges (the new row after K4).
- The snap of the translate gizmo (ADR-0038).

## Acceptance criteria

- [x] A tap near the intersection of two lines, of a line and a circle, and of two circles adds a point at the
      intersection, and an intersection comes before a midpoint.
- [x] An intersection of the circle of an arc outside the sweep of the arc gives no snap.
- [x] The mark under the mouse in the desktop is the pixel of the intersection (through `SketchActions.Mark`).
- [x] An injection of each part fails its test.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r11c-intersection-snap) · v0.167

## Method

Mechanical: the intersections in the plane, the test of the sweep of an arc, and the distance in pixels.
Judgement:  an intersection comes before a midpoint; two lines that share a point give no intersection, because the
            point is a point of the picture.
Weakest:    the drawing of the mark does not change, so no new picture of the GPU shows it, and no person moved the
            mouse over the window.

## Progress

- 2026-11-02: opened from the answers of the owner.
- 2026-11-02: the snap to an intersection, the pixel function and the ends of an arc in the picture; 6 new test
  cases, and 7 injections caught (no intersection, no skip of a shared point, no range of two segments, no range of a
  segment and a curve, no sweep, no pixel function, no ends of an arc).
- 2026-11-02: closed at v0.167; the row R11 closes, and the escalated selection is the row R12.
