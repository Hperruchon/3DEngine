---
id: 0127
title: The selection holds three items, and the window of the sketch offers Symmetric
status: Active
phase: K2g
opened: 2026-10-31
depends-on: [0125]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0027, 0029, 0030, 0032, 0034]
writes:
  create:
    - tasks/TASK-0127-symmetric-in-the-panel.md
  modify:
    - 3DEngine.Core/Sketching/SketchTool.cs
    - 3DEngine/Ui/DesktopUi.cs
    - 3DEngine.Tests/SketchToolTests.cs
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

# TASK-0127 — The selection holds three items, and the window of the sketch offers Symmetric

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

K2f (TASK-0125) gave the window of the sketch the kinds of K2c on lines except Symmetric, which takes two points and
a line, while the selection holds two items at most (ADR-0034 item 4). On 2026-10-31 the owner chose this item
("1 3 4", item 3).

## Goal

A person selects two points and a line, and adds Symmetric from the window of the sketch.

## Scope (in)

1. `SketchTool.MaximumSelection` is 3; a fourth item replaces the first.
2. Symmetric fits two points that are not ends of the line, and the line.
3. ADR-0034 item 4 holds both.

## Scope (out)

- A change of the engine.

## Acceptance criteria

- [ ] A test of the tool holds three items and replaces the first with a fourth.
- [ ] A test of the window gives Symmetric for two points and a line, and not for an end of the line.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-31: the limit of three items, the button Symmetric, the test of the tool changed, and 1 new test case.
