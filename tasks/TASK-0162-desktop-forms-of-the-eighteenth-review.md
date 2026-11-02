---
id: 0162
title: A proposal gives a simple polygon, the taps of a calibration reach it, and the image actions follow their refusals
status: Active
phase: P0.71
opened: 2026-11-01
depends-on: [0158]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0029, 0046, 0047]
writes:
  create:
    - tasks/TASK-0162-desktop-forms-of-the-eighteenth-review.md
    - 3DEngine.ImageTests/StlNameTests.cs
  modify:
    - 3DEngine.Core/Vision/ContourProposal.cs
    - 3DEngine/ImageActions.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine.Tests/ContourProposalTests.cs
    - 3DEngine.ImageTests/ImageActionsTests.cs
    - 3DEngine.ImageTests/ContourAcceptTests.cs
    - docs/adr/0047-contour-proposal.md
    - docs/register.md
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

# TASK-0162 — A proposal gives a simple polygon, the taps of a calibration reach it, and the image actions follow their refusals

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-01 found V69 and V70 (medium) and V71 to V73 (low). It comes after the next product phase, which Q1 of the review asks the owner for, by `docs/templates.md`, section 7. V69 follows the answer of the owner to Q3.

## Goal

An accepted proposal extrudes or is refused with a reason, a calibration takes its two taps in each mode, and the actions of an image follow a refusal of the engine.

## Scope (in)

1. V69: the proposal of Q3: with A, an opening with a cross of 3 × 3 before the trace and a refusal of a polygon that is still not simple; ADR-0047 item 2 says it. The session measured the cross and used a square of 3 × 3 (see Progress and R-0049).
2. V70: a calibration takes the next two point intents of each kind before the sketch tool.
3. V71: a refusal of the calibration clears its two points.
4. V72: a final `.stl` of the name is removed before `Resolve`.
5. V73: a test with a session that refuses one command of an accept, and an accept that stops and says which items exist.

## Scope (out)

- A change of the engine.
- A hole of a region, or more than one region (ADR-0047 item 5).

## Acceptance criteria

- [ ] Each finding has a test that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-11-02: V69: the class of the region gets an opening with a square of 3 × 3 before the largest region and the
  trace, and a polygon that is not simple gives no proposal and its reason. Q3 named a cross; the cross gave the
  rectangle of the tests a skew of one pixel, (41.5, 30.5), (158.5, 30.5), (159.5, 68.5), (41.5, 69.5), because it
  removes the corner pixel of each rectangle. The square keeps the seven old tests unchanged, so ADR-0047 item 2 says
  a square and R-0049 asks the owner to confirm it. After the opening, 111,413 random unions of rectangles and discs,
  with noise, gave no polygon that is not simple; a slot of 2 pixels along a circle of radius 20 gives one, so it is
  the test of the refusal. V70: `ImageActions.Tap` takes the intent, and a select and a sketch point are both taps of
  a calibration. V71: a refused calibration clears its two points and asks for two taps again. V72: `StlPath`
  removes a final `.stl` before `Resolve`. V73: an accept that stops gives the points and the lines that exist and
  the number of steps of undo; the test gives the accept a live record with a scale of 4e306 mm for each pixel, so the
  engine refuses the second point. The write set adds `3DEngine.ImageTests/StlNameTests.cs`, because the name of
  the export is not an action of the images, and `docs/register.md` for R-0049. 14 new test cases; an injection of
  the old behaviour of each finding fails its tests.

