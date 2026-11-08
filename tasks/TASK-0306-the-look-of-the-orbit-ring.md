---
id: 0306
title: The ring has the look of the orbit of the render, with a guide, icons and a caption
status: Done
phase: R16l
opened: 2026-11-08
depends-on: [0305]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0081, 0083, 0084, 0086, 0087, 0088, 0093, 0095]
writes:
  create:
    - tasks/TASK-0306-the-look-of-the-orbit-ring.md
    - docs/adr/0095-the-look-of-the-orbit-ring.md
    - 3DEngine.App/Ui/DesktopUi.Orbit.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.ImageTests/ToolRingTests.cs
    - docs/adr/0081-the-tool-ring-of-the-redesign.md
    - docs/adr/0086-the-tools-of-the-sketch-on-the-ring.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0306 — The ring has the look of the orbit of the render, with a guide, icons and a caption

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The third slice that the owner chose on 2026-11-08 is the orbit ring: the look of the ring of the render "mix".

## Goal

The ring at 1440 x 900 has the guide circle, the light segments with icons and short labels, and the caption of the render.

## Scope (in)

1. ADR-0095: the guide, the ground, the icons, the short labels and the caption.
2. A test of the icons over the labels and of the caption on a landscape and a portrait display.

## Scope (out)

- New tools on the ring (Edit sketch, Rename). The places of the segments.

## Acceptance criteria

- [x] The new test passes, and the picture at 1440 x 900 shows the ring of the render.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r16l-orbit-ring-c) · v0.291

## Method

Mechanical: the parts of the ring in the render at 1440 x 900.
Judgement:  icons drawn with the draw list (ADR-0095 Q1 A).
Weakest:    the icons are simple drawings of lines and arcs, and the test reads their places, not their look.

## Progress

- 2026-11-08: the slice and its test; with no icon the test fails in each of its three cases.
- 2026-11-08: run 42479731634 passed. Closed at v0.291.
