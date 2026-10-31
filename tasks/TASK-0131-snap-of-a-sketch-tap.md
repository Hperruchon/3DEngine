---
id: 0131
title: A tap of the sketch snaps to a midpoint, a line, a curve or the grid
status: Active
phase: R11a
opened: 2026-10-31
depends-on: [0130]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0041]
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
    - docs/adr/0034-sketch-tools.md
    - tasks/TASK-0133-desktop-forms-of-the-fifteenth-review.md
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
the grid.

## Scope (in)

1. ADR-0041: the order of the snaps, the tolerance of 22 pixels, and no constraint from a snap.
2. `SketchTool` finds the snap from the picture; the picture gives each curve its centre and its radius.

## Scope (out)

- Escalated selection of faces and edges (it needs a B-Rep, K4).
- A constraint from a snap (Q2 B).
- The mark of the snap under the pointer before a tap. It needs a hover intent of the intent machine (ADR-0026), which
  the review did not see; ADR-0041 item 5 leaves it to the rest of R11.

## Acceptance criteria

- [ ] A tool script gives each snap in the order of ADR-0041.
- [ ] A test of the picture gives the centre and the radius of each curve.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-31: opened by the codebase review of 2026-10-31 (TASK-0129).
- 2026-10-31: ADR-0041 Accepted under the standing answer (Q2 A). The review planned a mark of the snap before the tap;
  the intent machine has no hover intent, so the mark moves to Scope (out), and the acceptance criterion of a picture
  of the mark becomes a test of the picture. The snap and 5 new test cases.
