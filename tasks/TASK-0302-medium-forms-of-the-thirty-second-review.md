---
id: 0302
title: The identifier of a reference endpoint, the volume of a very small solid, the markers of a conflict and the width of the main panel hold their next forms
status: Active
phase: P0.127
opened: 2026-11-08
depends-on: [0301]
governed-by: [0004, 0007, 0012, 0014, 0016, 0018, 0019, 0021, 0025, 0027, 0028, 0029, 0030, 0031, 0032, 0033, 0034, 0035, 0036, 0037, 0039, 0052, 0060, 0072, 0073, 0075, 0076, 0077, 0080, 0083, 0089]
writes:
  create:
    - tasks/TASK-0302-medium-forms-of-the-thirty-second-review.md
  modify:
    - Engine.Core/Sketch/SketchResults.cs
    - Engine.Core/Commands/AddSketchPointCommandHandler.cs
    - Engine.Core/Commands/AddSketchLineCommandHandler.cs
    - Engine.Core/Commands/AddSketchCircleCommandHandler.cs
    - Engine.Core/Commands/AddSketchArcCommandHandler.cs
    - Engine.Core/Commands/AddSketchConstraintCommandHandler.cs
    - Engine.Core/Commands/AddSketchReferenceCommandHandler.cs
    - Engine.Tests/Sketch/SketchReferenceTests.cs
    - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
    - Engine.Tests/Commands/CreateConeCommandTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.ImageTests/FixedControlsTests.cs
    - docs/adr/0089-the-cone-the-sphere-and-the-torus-are-closed-meshes.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0302 — The identifier of a reference endpoint, the volume of a very small solid, the markers of a conflict and the width of the main panel hold their next forms

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-08 (b) found four medium findings: E193, E194, T177 and V142.

## Goal

E193, E194, T177 and V142 are fixed, each with a test or an injection.

## Scope (in)

1. E193: a command that makes a sketch item refuses an identifier that an item of the sketch already holds.
2. E194: the backend refuses a volume below zero only, so a very small solid of an earlier document opens again.
3. T177: a gate refuses the markers of a conflict of git in a tracked text file.
4. V142: the main panel is at most as wide as the display less its margins.

## Scope (out)

- The low findings (TASK-0303).

## Acceptance criteria

- [ ] Each finding has a test, an injection or a reading that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: a test, an injection or a reading for each finding, before the change.
Judgement:  each of the six commands that make an item of a sketch checks the items of the sketch, not only the point, so
            no form of E193 stays; the reference checks the identifiers of its two ends too.
Weakest:    to be written at the close.

## Progress
- 2026-11-08: the four fixes, each with a test. The injections (a check that never refuses, the old check of the volume,
  markers in the register, and the old width of the panel) fail the new tests.
