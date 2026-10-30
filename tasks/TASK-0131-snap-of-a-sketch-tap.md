---
id: 0131
title: A tap of the sketch snaps to a midpoint, a line, a curve or the grid
status: Ready
phase: R11a
opened: 2026-10-31
depends-on: [0130]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0027, 0029, 0030, 0032, 0034]
writes:
  create:
    - tasks/TASK-0131-snap-of-a-sketch-tap.md
    - docs/adr/0041-snap-of-a-sketch-tap.md
  modify:
    - 3DEngine.Core/Sketching/SketchTool.cs
    - 3DEngine/SketchActions.cs
    - 3DEngine/SketchOverlay.cs
    - 3DEngine/Ui/DesktopUi.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine.Tests/SketchToolTests.cs
    - 3DEngine.ImageTests/SketchActionsTests.cs
    - 3DEngine.ImageTests/SketchOverlayTests.cs
    - docs/adr/README.md
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

# TASK-0131 — A tap of the sketch snaps to a midpoint, a line, a curve or the grid

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The roadmap row R11 is "Escalated selection and a snap engine", with no slice. The codebase review of 2026-10-31,
sections 5 and 6, proposes its first slice, and the standing answer of the owner of 2026-10-31 takes Q2 A.

## Goal

A tap with no point within 22 pixels takes the midpoint of a line, then the nearest point on a line or a curve, then
the grid, and the overlay marks the snap before the tap.

## Scope (in)

1. ADR-0041: the order of the snaps, the tolerance of 22 pixels, and no constraint from a snap.
2. `SketchTool` finds the snap from the picture; the overlay draws its mark.

## Scope (out)

- Escalated selection of faces and edges (it needs a B-Rep, K4).
- A constraint from a snap (Q2 B).

## Acceptance criteria

- [ ] A tool script gives each snap in the order of ADR-0041.
- [ ] A picture on a GPU shows the mark.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-31: opened by the codebase review of 2026-10-31 (TASK-0129).
