---
id: 0277
title: While the ring of a sketch shows, the window of the sketch does not repeat it
status: Active
phase: R16h
opened: 2026-11-07
depends-on: [0278]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0087, 0088]
writes:
  create:
    - tasks/TASK-0277-the-window-of-a-sketch-beside-its-ring.md
    - docs/adr/0088-the-window-of-a-sketch-beside-its-ring.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.Sketch.cs
    - 3DEngine.ImageTests/ToolRingTests.cs
    - docs/adr/0034-sketch-tools.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0277 — While the ring of a sketch shows, the window of the sketch does not repeat it

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner ordered the layout of the redesign slice by slice, with no change of the contract. ADR-0087 names the next
slice: the window of the sketch with its rows only. The next phase of K5, the cone, needs a change of the contract, so the
standing order records that question for the owner, and this slice comes first.

## Goal

While the ring of a sketch shows, the window of the sketch holds no tool, no Finish and no kind of a constraint.

## Scope (in)

1. ADR-0088 with its two questions, answered by the standing order.
2. The window of the sketch, in a frame with the ring and in a frame with no ring.

## Scope (out)

- Extrude, Delete and the other buttons of the window on the ring.

## Acceptance criteria

- [ ] With the ring, the window holds no Line, Select, Circle, Arc, Finish, Horizontal, Vertical or Distance, and keeps
      Extrude, Delete and the rows.
- [ ] With no ring, the window holds each of them, as before.
- [ ] The tests of the window of the sketch pass.
- [ ] A picture of the sketch with its ring and its window on the GPU is kept for the owner.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the test of the window with and with no ring; an injection of a window that repeats the ring.
Judgement:  the window keeps each button when the ring does not show (Q2 A), so a tool always has a place.
Weakest:    to be written at the close.

## Progress

- 2026-11-07: ADR-0088 and the window of the sketch beside its ring. An injection of a window that repeats the ring fails
  the new test. The picture on the GPU shows the window with its title, its state, Next point, Extrude and the rows.
