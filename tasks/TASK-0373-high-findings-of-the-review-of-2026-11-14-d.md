---
id: 0373
title: The high findings of the codebase review of 2026-11-14 (d)
status: Active
phase: P0.149
opened: 2026-11-14
depends-on: [0371]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0041, 0072, 0081, 0083, 0084, 0086, 0087, 0088, 0092, 0093, 0094, 0095, 0096, 0097, 0101, 0107, 0108, 0110, 0111, 0113, 0114, 0115, 0116, 0119, 0120, 0122, 0129, 0130, 0131, 0132, 0133, 0134, 0135, 0136, 0137, 0139]
writes:
  create:
    - tasks/TASK-0373-high-findings-of-the-review-of-2026-11-14-d.md
    - docs/adr/0139-a-sketch-leaves-a-flat-view-and-no-tool-hides-under-the-arc.md
  modify:
    - 3DEngine.Core/Models/Camera.cs
    - 3DEngine.Core/Sketching/SketchCamera.cs
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.Tests/FlatViewTests.cs
    - 3DEngine.ImageTests/ProposalPlaceTests.cs
    - 3DEngine.ImageTests/ProposalArcTests.cs
    - docs/adr/0136-the-arc-ends-before-the-tools-and-the-caption-wraps.md
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

# TASK-0373 — The high findings of the codebase review of 2026-11-14 (d)

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-14 (d) (`docs/reviews/2026-11-14-d-codebase-review.md`) gives two high findings, V185 and V186,
and the medium finding V187, whose test V186 needs. Section 7 of `docs/templates.md` puts them before the next product phase,
R21c.

## Goal

A sketch leaves a flat view, no end of the arc of a proposal lies on a tool of the ring at a small landscape size, and the
guard of V160 has a test again.

## Scope (in)

1. V185: a sketch that starts in a flat view leaves it.
2. V186: Reject and its word as Accept, and the ring of the arc moved when no end can be free; the small landscape sizes.
3. V187: a scene where an end lies on a tool, and the control of the test of V160.
4. A test for each, which fails before the fix.

## Scope (out)

- V188 to V190 (TASK-0374).

## Acceptance criteria

- [ ] A test proves that a sketch from the top view and from the front view leaves the flat view.
- [ ] A test proves at the small landscape sizes that no end of the arc lies on a tool target.
- [ ] The test of V160 has a scene with an end on a tool and a control that fails without it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the exit of a flat view in Face; the test of both ends.
Judgement:  a tool under an end hides, in place of a move of the ring; ADR-0139.
Weakest:    on a small landscape display a tool can be away while a proposal waits; no person used it.

## Progress

- 2026-11-14: opened by the codebase review of 2026-11-14 (d) (TASK-0371).
- 2026-11-14: started after the review of 2026-11-14 (d), branch p0149-review-findings, while the main run of v0.354 waits
  for the owner. V185 and V186 change rules of ADR-0137 and ADR-0136, so ADR-0139 records them. V185: the view of a sketch
  leaves a flat view first (Camera.LeaveFlat). V186: Reject searches its angle up from 235 degrees, and a tool under an end
  of the arc of the last frame hides while the proposal waits. With the search, 20 tools hide at 915 by 412 over sixteen
  layouts, and 28 without it. V187: the test of V160 runs at 640 by 360, where no angle frees an end, with the control
  that a point is on a tool and that a tool hides; the rows lie on the word Reject there (R-0066), so the test skips the
  points on a window. Five injections each failed a named test. Each file came back byte for byte.
