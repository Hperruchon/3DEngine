---
id: 0356
title: The medium and low findings of the codebase review of 2026-11-14
status: Active
phase: P0.146
opened: 2026-11-14
depends-on: [0355]
governed-by: [0007, 0017, 0019, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0056, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0085, 0086, 0087, 0089, 0091, 0092, 0093, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0106, 0107, 0108, 0109, 0110, 0111, 0113, 0114, 0115, 0116, 0117, 0119, 0120, 0121, 0123, 0124, 0125, 0127, 0129]
writes:
  create:
    - tasks/TASK-0356-medium-and-low-findings-of-the-review-of-2026-11-14.md
    - docs/adr/0129-the-findings-of-the-review-of-2026-11-14.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.App/Ui/DesktopUi.Overlay.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.App/DimensionOverlay.cs
    - 3DEngine.App/StepTexts.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - 3DEngine.App/BodyActions.cs
    - 3DEngine.App/SketchActions.cs
    - 3DEngine/HostedSession.cs
    - 3DEngine.Vulkan/EdgeLines.cs
    - 3DEngine.ImageTests/HiddenBodiesTests.cs
    - 3DEngine.ImageTests/DimensionLabelsTests.cs
    - 3DEngine.ImageTests/FixedStatusTests.cs
    - 3DEngine.ImageTests/FixedStatusImageTests.cs
    - 3DEngine.ImageTests/BodyEdgesImageTests.cs
    - docs/adr/0109-the-edges-of-the-body.md
    - docs/adr/0124-the-selection-after-a-hide-and-the-hidden-bodies-in-the-status-line.md
    - docs/adr/0125-each-dimension-says-what-it-measures.md
    - docs/adr/0127-the-fixed-controls-and-the-status-as-in-the-render.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0356 — The medium and low findings of the codebase review of 2026-11-14

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-14 (`docs/reviews/2026-11-14-codebase-review.md`) gives two medium findings (V173 and V174)
and three low findings (V175, V176 and V177) in the window, the dimension plates and the edge lines. Section 7 of
`docs/templates.md` puts them after the next product phase, R18h.

## Goal

No plate goes under a window of the frame, each hidden body and "Show all" can be reached on a small landscape display, a
T-vertex of a turned body adds no line, a stuck plate stays at its start, and a step text goes to its own command.

## Scope (in)

1. V173: the covers of the plates hold the panels and the fixed rectangles of the last frame.
2. V174: "Show all" and each hidden body can be reached at 844 by 390 and 640 by 360.
3. V175: collinear edges with a relative tolerance, and the uncovered parts of each long edge.
4. V176: a plate that no move frees goes back to its start.
5. V177: an expected text matches the identifier of its command.
6. A test for each, which fails before the fix.

## Scope (out)

- A change of the order of the windows of the frame.

## Acceptance criteria

- [ ] A test proves that no plate is under the window of the live sketches in the scene of the probe.
- [ ] A test proves that "Show all" and the last hidden body are inside the display at 844 by 390.
- [ ] A test proves that a turned mesh with a T-vertex and two T-vertices side by side give no false line.
- [ ] A test proves that a stuck plate stays at its start.
- [ ] A test proves that a command of another client with the same name does not take the text of the window.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the covers of the last frame; the start of a stuck plate; the match by identifier.
Judgement:  the form of the list of the hidden bodies; the tolerance of a collinear edge; one-axis moves for a plate.
Weakest:    a plate that a move along one axis cannot free stays under its control, also when a free place exists in
            another direction (three of 96 views of the test of V173). No real mesh of the backend gave a T-vertex yet.

## Progress

- 2026-11-14: opened by the codebase review of 2026-11-14 (TASK-0355).
- 2026-11-14: started under the order of the owner of 2026-11-14, after R18h (v0.341). ADR-0129 records the decisions that
  change: the list of the hidden bodies when its rows do not fit (V174), the place of a stuck plate (V176), the text of a
  step by the identifier of its command (V177), and the chain of a T-vertex (V175). The fix of V173 takes the windows of the
  last frame as covers; at 1280 by 720 the plates under a window went from 118 to 3, each a plate that no move of
  ADR-0125 item 4 frees. At 1280 by 720 the room above the anchors holds ten rows, so ten hidden bodies and "Show all" did
  not fit before this task either. Seven injections each failed a named test; each file came back byte for byte.
