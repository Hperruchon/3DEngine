---
id: 0374
title: The medium and low findings of the codebase review of 2026-11-14 (d)
status: Active
phase: P0.150
opened: 2026-11-14
depends-on: [0372, 0373]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0038, 0041, 0053, 0072, 0082, 0084, 0092, 0094, 0096, 0098, 0099, 0107, 0108, 0110, 0111, 0113, 0114, 0115, 0117, 0119, 0120, 0129, 0130, 0131, 0133, 0134, 0135, 0136, 0137, 0139, 0140, 0141]
writes:
  create:
    - tasks/TASK-0374-medium-and-low-findings-of-the-review-of-2026-11-14-d.md
    - docs/adr/0141-the-scale-and-the-depth-of-a-flat-view.md
  modify:
    - 3DEngine.Core/Models/Camera.cs
    - 3DEngine.Core/Picking/Picker.cs
    - 3DEngine.Core/Gizmo/TranslateGizmo.cs
    - 3DEngine.App/SketchOverlay.cs
    - 3DEngine.App/Ui/DesktopUi.Rows.cs
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.Tests/FlatViewTests.cs
    - 3DEngine.ImageTests/FlatViewWindowTests.cs
    - 3DEngine.ImageTests/ProposalPortraitTests.cs
    - docs/adr/0135-the-arc-of-a-proposal-on-a-portrait-display.md
    - docs/adr/0137-the-flat-views-of-the-camera.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0374 — The medium and low findings of the codebase review of 2026-11-14 (d)

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-14 (d) (`docs/reviews/2026-11-14-d-codebase-review.md`) gives the medium findings V188 and V189 and
the low finding V190. Section 7 of `docs/templates.md` puts them after the next product phase, R21c.

## Goal

A zoom in a flat view does not cut the part, the edge lines of a flat view do not pull through faces, and the rows know the
arc of a portrait display in its first frame.

## Scope (in)

1. V188: a scale of its own for a flat view, with the eye behind the box of the scene.
2. V189: a pull of the edge lines for a flat view.
3. V190: the place of the arc of a portrait display in the same frame as the rows.
4. A test for each, which fails before the fix.

## Scope (out)

- V185 to V187 (TASK-0373).

## Acceptance criteria

- [ ] A test proves that a zoom of ten times in a flat view keeps the eye out of the box of the scene.
- [ ] A test or a picture proves the pull of the edge lines in a flat view.
- [ ] A test proves that the rows are under the arc in the first frame of a proposal.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the scale of a flat view; the pull; the place of the arc.
Judgement:  a scale of its own in place of a move of the eye; the far plane in place of a new shader; ADR-0141.
Weakest:    the pull of the edge lines is computed, not seen on a GPU in a flat view.

## Progress

- 2026-11-14: opened by the codebase review of 2026-11-14 (d) (TASK-0371).
- 2026-11-14: started after R21c, branch p0150-review-findings, while the main run of v0.354 waits for the owner. V188 and V189
  change rules of ADR-0137, and V190 a rule of ADR-0135, so ADR-0141 records them. V188: a flat view has a scale of its own
  (ScaleDistance), which a zoom changes, and the pick, the overlay, the gizmo and the pan take it. V189: the far plane of a
  flat view is just behind the box, and Frame in a flat view is not held at half of it. V190: the floor of the rows is the
  middle of the display while a proposal waits on a portrait display, so the field of the bottom of the sheet went. Eight
  injections each failed a named test. Each file came back byte for byte.
