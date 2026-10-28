---
id: 0101
title: The desktop draws a sketch of lines and adds its constraints, by touch and by mouse
status: Active
phase: K2b
opened: 2026-10-28
depends-on: [0100]
governed-by: [0007, 0009, 0017, 0019, 0022, 0023, 0025, 0026, 0027, 0029, 0030, 0031, 0032, 0034]
writes:
  create:
    - tasks/TASK-0101-sketch-tools.md
    - docs/adr/0034-sketch-tools.md
  modify:
    - 3DEngine.Core/Input/**
    - 3DEngine.Core/Picking/**
    - 3DEngine.Core/Numeric/**
    - 3DEngine.Core/Sketching/**
    - 3DEngine/**
    - 3DEngine.Tests/**
    - 3DEngine.ImageTests/**
    - docs/adr/0034-sketch-tools.md
    - docs/adr/0026-intent-layer.md
    - docs/adr/0029-selection-and-hide-gestures.md
    - docs/adr/0032-numeric-pad-and-evaluator.md
    - docs/adr/0033-sketch-model.md
    - docs/adr/README.md
    - tasks/TASK-0099-document-file-and-pad-gaps.md
    - docs/glossary.md
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Api.Http/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine.Vulkan/**
    - eng/write-set-check/**
    - eng/repo-gates/**
---

# TASK-0101 — The desktop draws a sketch of lines and adds its constraints, by touch and by mouse

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap phase K2b is the second phase of the sketch on the path to the milestone "CAD on one part". K2a (TASK-0100,
v0.100) gave the engine the sketch model: the seven sketch commands, the query `GetSketch`, and a drawing of each
live sketch in the desktop, read only. The owner chose the split of K2 into K2a and K2b on 2026-10-28 (Q8 of
`docs/reviews/2026-10-28-b-codebase-review.md`). K2b gives the desktop the tools: a sketch mode in the intent layer, a
ray to the plane of the sketch, a pad for a line, and a panel of constraints. ADR-0034 gives the decisions, and the
owner accepts it before the code.

## Goal

A person makes a closed profile of lines on an origin plane and adds the five constraints of K2a, by touch or by
mouse, and the log holds the same commands for both.

## Scope (in)

1. ADR-0034: the sketch mode of the intent layer, the ray to the plane of a sketch, the tools of the sketch mode,
   the pad for a point of a line, and the window of the sketch with its constraints.
2. The touch and mouse scripts of each new intent in the parity test, and a test that compares the logs of a touch
   script and a mouse script that draw one closed profile.
3. The amendments of ADR-0026, ADR-0029 and ADR-0032 that ADR-0034 gives.

## Scope (out)

- A change of the engine: the commands and the query of K2a are sufficient.
- The solver: K3a. A constraint that the positions do not satisfy shows its residual, and no point moves.
- The circle, the arc and the kinds 6 to 14: phase K2.
- A drag of a sketch point, and a snap to a grid of the scene or to a body: R11.
- A group of undo steps for one gesture (ADR-0033 item 10).

## Acceptance criteria

- [x] The owner accepts ADR-0034 before the code.
- [x] A touch script and a mouse script give the same intents for each new intent kind, and a check fails when a
      kind has no touch script.
- [x] A touch script and a mouse script that draw one closed profile give the same commands in the log.
- [x] The ray to a plane gives the position of a pixel in the frame of each of the three datums, and no position for
      a ray parallel to the plane.
- [x] Each of the five kinds of constraint can be added from the window of the sketch, and its residual shows.
- [x] Each new test fails under an injection that breaks its rule.
- [ ] The visual check: the owner accepts a picture of a profile drawn in the desktop with its constraints.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on `ubuntu-latest`, `windows-latest` and `macos-latest`.

## Notes for the implementer

- **The order.** The ADR first. Then the ray to the plane and the intent of the sketch mode, then the tool state
  machine, then the host and the window of the sketch, then the pad, then the visual check.
- **The governance.** `governed-by` must equal the ADRs in force whose `affects` intersect the write set; the gate
  `TaskGovernance` gives the list. ADR-0034 joins the list when the owner accepts it.
- **The overlap.** TASK-0099 (P0.40) changes the pad of the panel too. Do not run the two tasks at the same time.
- **The size.** About 4 to 6 evenings.

## Progress

- 2026-10-28: opened with ADR-0034 as Proposed. The work waits for the answers of the owner.
- 2026-10-28: the owner accepted ADR-0034 with Q1 to Q6 A. The record amends ADR-0026, ADR-0029 and ADR-0032, and
  their text changes in this commit. ADR-0034 also governs TASK-0099, which writes the pad, so its `governed-by`
  names 0034.
- 2026-10-28: the mode Sketch and its intent, the ray to a plane, the step of a tap, the turn of the camera, the
  tools Line and Select, a pad of one to three fields, `SketchActions`, the window of the sketch and its drawing.
  57 new test cases; 17 injections each failed a test. The panel image test counts 60 % dark pixels, not 70 %: the
  line of a new sketch adds three buttons.
