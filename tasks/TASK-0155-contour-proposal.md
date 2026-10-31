---
id: 0155
title: A contour proposal of a placed image becomes an editable sketch
status: Active
phase: V3
opened: 2026-10-31
depends-on: [0153]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0027, 0029, 0030, 0032, 0034, 0041, 0046, 0047]
writes:
  create:
    - tasks/TASK-0155-contour-proposal.md
    - docs/adr/0047-contour-proposal.md
    - 3DEngine.Core/Vision/ContourProposal.cs
    - 3DEngine.Tests/ContourProposalTests.cs
    - 3DEngine.ImageTests/ContourAcceptTests.cs
    - 3DEngine.ImageTests/ContourImageTests.cs
  modify:
    - 3DEngine/ImageActions.cs
    - 3DEngine/ImagePlacement.cs
    - 3DEngine/SketchOverlay.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine/Ui/DesktopUi.cs
    - 3DEngine/Ui/PanelAction.cs
    - docs/adr/0042-vision-library.md
    - docs/adr/README.md
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Api.Http/**
    - Engine.Cli/**
    - 3DEngine.VisionTests/**
---

# TASK-0155 — A contour proposal of a placed image becomes an editable sketch

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

V2 (v0.152) places a calibrated image on a datum. On 2026-10-31 the owner gave the order "After V3: the cross-platform
determinism hash in CI, then an STL export of the live bodies, then the review when it is due", with the recommended
answer to each question. ADR-0042 item 3 gives the shape of V3: the client proposes, and the sketch commands record the
accepted points.

## Goal

The person proposes the contour of a placed image, sees it over the scene, and accepts it as a sketch on the datum of
the image, which the tools of the sketch then edit.

## Scope (in)

1. `ContourProposal`: the threshold of Otsu, the largest region, the trace of Moore and the simplification of Douglas and
   Peucker, in managed code (ADR-0047 items 1 and 2).
2. The button "Propose contour", the lines of the proposal over the scene, and Accept and Reject (items 3 and 4).
3. ADR-0047, which amends ADR-0042 item 3, and register entry R-0048 on the licences of the vision library.

## Scope (out)

- A hole, more than one region, an arc or a constraint from a contour, and one step of undo for one accept.
- The vision library in a distributed program (R-0048).

## Acceptance criteria

- [ ] The proposal has a test for each rule of ADR-0047 item 2, with exact pixels.
- [ ] A test with no GPU accepts a proposal and reads the sketch on the datum of the image.
- [ ] A picture on a GPU shows the accepted sketch on the edges of its image, with no message of the validation layer.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-31: ADR-0047, the proposal, the desktop and the tests. The Windows payload of the vision library holds about
  twelve libraries and an FFmpeg file, so V3 proposes with its own code (R-0048). 11 new test cases: 7 in
  3DEngine.Tests, and 3 with no GPU and 1 image test in 3DEngine.ImageTests. 7 injections; the first threshold
  injection passed, because each test image had two greys, until a test with a ring of a third grey.
