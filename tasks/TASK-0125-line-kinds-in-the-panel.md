---
id: 0125
title: The window of the sketch offers the constraints of K2c on lines
status: Active
phase: K2f
opened: 2026-10-31
depends-on: [0110, 0122, 0124]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034]
writes:
  create:
    - tasks/TASK-0125-line-kinds-in-the-panel.md
  modify:
    - 3DEngine/Ui/DesktopUi.cs
    - 3DEngine/Ui/SketchText.cs
    - 3DEngine.ImageTests/SketchWindowUiTests.cs
    - docs/adr/0034-sketch-tools.md
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

# TASK-0125 — The window of the sketch offers the constraints of K2c on lines

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

K2c (TASK-0110) gave the engine pointOn, midpoint, parallel, perpendicular, equal, symmetric and angle on points and
lines. K2e (TASK-0122) gave the window of the sketch the kinds on a curve, and its Scope (out) says that a later task
adds the kinds of K2c on lines. The owner said "ok continue" on 2026-10-31 after the plan of 2026-10-30 was complete.

## Goal

A person adds each kind of K2c that a selection of two items can name from the window of the sketch.

## Scope (in)

1. On line and Midpoint for a point and a line, when the point is not an end of the line.
2. Equal, Parallel, Perpendicular and Angle for two lines. Angle opens a pad of degrees with the current angle, and the
   client gives the cosine and the sine to the command (ADR-0033 item 3, determinism rule 2).
3. ADR-0034 item 4 names the buttons.

## Scope (out)

- Symmetric. It takes two points and a line, and the selection holds two items at most (ADR-0034 item 4).
- A change of the engine.

## Acceptance criteria

- [ ] A test of the window gives the buttons of each selection, and an injection that removes a fit fails it.
- [ ] Angle gives the cosine and the sine of the degrees, with exact numbers for a multiple of 90 degrees.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-31: the six buttons, the angle pad, and 2 new test cases; the test of the selection of a line and O gains
  On line and Midpoint.
