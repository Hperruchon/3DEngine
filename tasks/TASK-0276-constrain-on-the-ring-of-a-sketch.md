---
id: 0276
title: "Constrain" on the ring of a sketch opens the constraints that fit the selection
status: Done
phase: R16g
opened: 2026-11-07
depends-on: [0275]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0086, 0087]
writes:
  create:
    - tasks/TASK-0276-constrain-on-the-ring-of-a-sketch.md
    - docs/adr/0087-constrain-on-the-ring-of-a-sketch.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.Sketch.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.ImageTests/ToolRingTests.cs
    - docs/adr/0086-the-tools-of-the-sketch-on-the-ring.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0276 — "Constrain" on the ring of a sketch opens the constraints that fit the selection

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner ordered the layout of the redesign slice by slice, with no change of the contract. ADR-0086 names the next
slice: "Constrain" on the ring of a sketch, with a second ring of the kinds of a constraint.

## Goal

While a sketch is active and a kind of constraint fits the selection, the ring holds "Constrain", which opens a second
ring of the kinds that fit, and a tap on a kind does what its button of the window of the sketch does.

## Scope (in)

1. ADR-0087 with its three questions, answered by the standing order.
2. One method for the kinds that fit the selection, used by the window of the sketch and by the ring.
3. The segment "Constrain", the second ring with "Back", and what closes it.

## Scope (out)

- The window of the sketch with its rows only.

## Acceptance criteria

- [x] With no selection the ring holds no "Constrain"; with a line selected it holds "Constrain" before Finish.
- [x] A tap on "Constrain" gives Horizontal, Vertical, Distance and Back for a line; Horizontal adds the constraint, and
      Distance opens the pad of its value.
- [x] The tests of the window of the sketch and of its constraints pass with the one method.
- [x] A picture of the second ring on the GPU is kept for the owner.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r16g-constrain-ring) · v0.261

## Method

Mechanical: the test of the second ring by touch; injections of a ring with no "Constrain" and of a "Constrain" that does
            not open; the tests of the window of the sketch.
Judgement:  the kinds that fit, from the rule of the window of the sketch (Q2 A), so the two places agree.
Weakest:    on a display of 1280 by 720 a wide label still covers the edge of the sketch, and no test holds the
            second ring on a portrait display.

## Progress

- 2026-11-07: ADR-0087; the fit table moves from the buttons of the window into one method; "Constrain" and the second
  ring. The first form of the test read the ring one frame early, because the release counts in the frame after it and
  the ring shows its new labels in the next frame; the test draws those two frames. Injections of a ring with no
  "Constrain" and of a "Constrain" that does not open each fail the test.
- 2026-11-07: the picture of the second ring on the GPU showed "Horizontal" and "Vertical" centred over the points of
  the sketch, and "Vertical" on the edge of the panel. A wide label now ends outside the inner edge, right of the panel,
  on a plate (ADR-0087 item 5). An injection of a centred label fails the test. The picture constrain-ring.png is kept.
- 2026-11-07: run 42371079980 passed. Closed at v0.261.
