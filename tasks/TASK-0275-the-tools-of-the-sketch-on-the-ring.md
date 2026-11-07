---
id: 0275
title: While a sketch is active, the ring holds the tools of the sketch
status: Done
phase: R16f
opened: 2026-11-07
depends-on: [0272]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086]
writes:
  create:
    - tasks/TASK-0275-the-tools-of-the-sketch-on-the-ring.md
    - docs/adr/0086-the-tools-of-the-sketch-on-the-ring.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.ImageTests/ToolRingTests.cs
    - 3DEngine.ImageTests/SketchLiftImageTests.cs
    - docs/adr/0081-the-tool-ring-of-the-redesign.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0275 — While a sketch is active, the ring holds the tools of the sketch

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner ordered the layout of the redesign slice by slice, with no change of the contract, and on 2026-11-07 put the
next redesign slices after the STL demonstration. ADR-0085 names the next slice: the tools of the sketch on the ring.

## Goal

While a sketch is active, the ring holds Line, Select, Circle, Arc and Finish beside the sketch, and a tap gives the tool.

## Scope (in)

1. ADR-0086 with its three questions, answered by the standing order.
2. The ring of a sketch, its place on each display, and its tap.

## Scope (out)

- "Constrain" with a second ring; the window of the sketch with its rows only.

## Acceptance criteria

- [x] While a sketch is active, the ring holds the five tools, and a tap by touch and by mouse gives the tool or Finish.
- [x] On a landscape display the ring is on the left of the sketch, outside it and to the right of the panel.
- [x] Each of five portrait segments is at least 44 pixels at the inner radius.
- [x] A picture of the sketch with its ring on the GPU is kept for the owner.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r16f-sketch-ring) · v0.260

## Method

Mechanical: the tests of the ring of a sketch, of its layout and of five portrait segments; the picture; injections of the
            old hidden ring and of a ring under the window of the sketch.
Judgement:  the left of the sketch on a landscape display (Q2 A), because the window of the sketch takes the right side;
            the first form put the ring onto the points of the sketch, which the picture showed.
Weakest:    the centre of the ring comes from the host, which needs a window, so no test holds it; each tool shows two
            times, on the ring and in the window of the sketch.

## Progress

- 2026-11-07: ADR-0086, the ring of a sketch and its layout. The first form kept the ring left of the window of the sketch
  by a narrower display, and the picture showed the ring on the points P2 and P3; the ring is now on the left of the
  sketch. Injections of the hidden ring and of a ring with no room for the window each fail the test of the tap.
- 2026-11-07: run 42368827245 passed. Closed at v0.260.
