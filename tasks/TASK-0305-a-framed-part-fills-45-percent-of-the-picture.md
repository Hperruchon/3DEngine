---
id: 0305
title: A framed part fills 45 % of the half picture, as in the render of the mix
status: Done
phase: R16k
opened: 2026-11-08
depends-on: [0304]
governed-by: [0007, 0009, 0019, 0022, 0023, 0029, 0072, 0094]
writes:
  create:
    - tasks/TASK-0305-a-framed-part-fills-45-percent-of-the-picture.md
    - docs/adr/0094-a-framed-part-fills-45-percent-of-the-picture.md
  modify:
    - 3DEngine.Core/Models/Camera.cs
    - 3DEngine.App/SessionScene.cs
    - 3DEngine.Tests/CameraTests.cs
    - docs/adr/0025-observe-a-cut.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0305 — A framed part fills 45 % of the half picture, as in the render of the mix

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The second slice that the owner chose on 2026-11-08 is the framing. ADR-0025 item 6 frames a box at the edge of the
picture, so at 1440 x 900 the bracket of the render fills the height of the display, under the panel and the ring.

## Goal

A framed part is about a third of the width at 1440 x 900, in the middle of the display, as in the render.

## Scope (in)

1. ADR-0094: `FrameFill` on the camera, 0.45 for the camera of the session.
2. A test that a framed box with a fill of 0.45 is inside 45 % of the half picture and not much smaller.

## Scope (out)

- A shift of the middle of the projection. A camera with Z up.

## Acceptance criteria

- [x] The new test passes, and the picture at 1440 x 900 shows the part in the middle.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r16k-framing) · v0.290

## Method

Mechanical: the size of the part in the render at 1440 x 900.
Judgement:  45 % of the half picture, and no shift of the middle (ADR-0094 Q1 A).
Weakest:    the fill of 0.45 comes from one render at 1440 x 900; at 1280 x 720 the part is smaller than in the render
            of that size.

## Progress

- 2026-11-08: the fill and its test; each test of the camera and each image test passes.
- 2026-11-08: run 42479307642 passed. Closed at v0.290.
