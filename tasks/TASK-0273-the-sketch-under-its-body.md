---
id: 0273
title: The active sketch of a live extrude moves out of its body, and the camera stays
status: Done
phase: R16e
opened: 2026-11-07
depends-on: [0270]
governed-by: [0007, 0019, 0025, 0029, 0034, 0072, 0075, 0076, 0077, 0080, 0081, 0082, 0083, 0085]
writes:
  create:
    - tasks/TASK-0273-the-sketch-under-its-body.md
    - docs/adr/0085-the-sketch-under-its-body.md
    - 3DEngine.App/SketchLift.cs
    - 3DEngine.ImageTests/SketchLiftTests.cs
    - 3DEngine.ImageTests/SketchLiftImageTests.cs
  modify:
    - 3DEngine.App/SketchActions.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - docs/adr/0034-sketch-tools.md
    - docs/adr/README.md
    - tasks/TASK-0271-desktop-forms-of-the-twenty-ninth-review.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0273 — The active sketch of a live extrude moves out of its body, and the camera stays

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner ordered the layout of the redesign slice by slice, with no change of the contract, and on 2026-11-07 put the
next redesign slices after the STL demonstration. After the rows without a panel (R16d) comes "the sketch under its body"
of the "mix" direction of the designer brief. The review of 2026-11-07 (b) put its two high tasks before this slice, and
both are done (v0.254, v0.255).

## Goal

"Edit" of the sketch of a live extrude keeps the camera and shows the sketch out of the body, along the normal of its
plane, with a line from each point to its place; each tap on the moved sketch gives the position that the person sees.

## Scope (in)

1. ADR-0085 with its three questions, answered by the standing order.
2. `SketchLift`, its use in the drawing and in `SketchActions.Current`, and the edit with no turn of the camera.

## Scope (out)

- The tools of the sketch on the ring, and the values of the sketch on its items.

## Acceptance criteria

- [x] The plane moves to the side away from the body, a quarter of the diagonal of the box past it.
- [x] The move changes the origin of its sketch only, and a line links each moved point to its place.
- [x] A tap on the moved sketch in an oblique view gives the position that the person sees.
- [x] A picture of the sketch out of its body on the GPU is kept for the owner.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r16e-sketch-under-body) · v0.256

## Method

Mechanical: four tests with no GPU and one picture; an injection of the picks on the plane of the record.
Judgement:  the move of the plane along its normal keeps each position in the plane, so one move serves the drawing and
            the picks, and no command changes; the side and the gap come from the box of each frame (Q3 A).
Weakest:    the edit with no turn of the camera runs in no test, because the host needs a window; the tests hold the
            move, the links and the picks. In the view of the desktop, with Y up, the sketch moves behind the body.

## Progress

- 2026-11-07: ADR-0085, `SketchLift`, the drawing, the picks and the edit with no turn of the camera. The first form of the
  test of the tap read the pixel from `Current`, so an injection that left the move out of `Current` still passed; the test
  now takes the pixel from the record and the move, and that injection fails it ((16, 14) for (12, 8)). The first picture
  used the camera of the session at 40 mm and showed the sketch on the face of the body; the picture now uses an oblique
  view at 150 mm. The view has the axis Y up, so the sketch of a plate on XY moves behind it.
- 2026-11-07: run 42365252084 passed. Closed at v0.256.
