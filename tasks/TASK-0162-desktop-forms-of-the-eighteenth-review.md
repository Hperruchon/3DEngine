---
id: 0162
title: A proposal gives a simple polygon, the taps of a calibration reach it, and the image actions follow their refusals
status: Ready
phase: P0.71
opened: 2026-11-01
depends-on: [0158]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0029, 0046, 0047]
writes:
  create:
    - tasks/TASK-0162-desktop-forms-of-the-eighteenth-review.md
  modify:
    - 3DEngine.Core/Vision/ContourProposal.cs
    - 3DEngine/ImageActions.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine.Tests/ContourProposalTests.cs
    - 3DEngine.ImageTests/ImageActionsTests.cs
    - 3DEngine.ImageTests/ContourAcceptTests.cs
    - docs/adr/0047-contour-proposal.md
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

1. V69: the proposal of Q3: with A, an opening with a cross of 3 × 3 before the trace and a refusal of a polygon that is still not simple; ADR-0047 item 2 says it.
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

