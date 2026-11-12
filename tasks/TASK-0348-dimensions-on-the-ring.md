---
id: 0348
title: Dimensions on the ring of a body
status: Done
phase: R20b1
opened: 2026-11-11
depends-on: [0341]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0081, 0083, 0084, 0086, 0087, 0088, 0092, 0093, 0095, 0097, 0101, 0107, 0108, 0111, 0113, 0114, 0116, 0119, 0120, 0122]
writes:
  create:
    - tasks/TASK-0348-dimensions-on-the-ring.md
    - docs/adr/0122-dimensions-is-a-segment-of-the-ring.md
    - 3DEngine.ImageTests/RingDimensionsTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.Orbit.cs
    - 3DEngine.ImageTests/RingToolsTests.cs
    - 3DEngine.ImageTests/ProposalArcTests.cs
    - 3DEngine.ImageTests/RingImageTests.cs
    - docs/adr/0119-the-ring-and-the-history-arc-as-in-the-render.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - tasks/TASK-0350-medium-and-low-findings-of-the-review-of-2026-11-12.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0348 — Dimensions on the ring of a body

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The UX test of 2026-11-10 (`docs/proposals/2026-11-10-ux-test-of-a-multi-part-project.md`, problem 2) found that the
dimensions are hard to find: the ring of a body has no "Dimensions", so a person opens the panel. Roadmap row R20b holds the
fix with the hidden bodies. On 2026-11-11 the owner asked: "i want the dimention option on the wheel". This task splits R20b:
R20b1 holds the dimensions, and R20b2 holds the hidden bodies.

## Goal

A person turns the dimensions on and off on the ring of a body, and the ring shows if they are on.

## Scope (in)

1. ADR-0122, which amends ADR-0119 items 1 and 2.
2. "Dimensions" on the ring of a body, with an icon, the state of a tool in use while the dimensions are on, and its place
   in the order of the tools that leave a small ring.
3. Tests: the tap turns the dimensions on and off; the ring shows a state from the panel; the labels of a small ring.

## Scope (out)

- The hidden bodies, "N hidden · Show" and "Show hidden" (R20b2).
- The labels of the dimensions (R20c).

## Acceptance criteria

- [x] ADR-0122 exists. ADR-0119 shows the amendment.
- [x] A test proves the tap of "Dimensions"; it fails when the tap does not change the dimensions.
- [x] A test proves the state of a tool in use; it fails when the segment does not show it.
- [x] A test proves the order of the tools that leave a small ring; it fails when "Dimensions" does not leave.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r20b-ring-dimensions) · v0.334

## Method

Mechanical: the segment, its icon, its state and its tap.
Judgement:  the place of "Dimensions" in the order of the tools that leave a small ring.
Weakest:    no person used the segment in the window or on the phone. The tests drive Dear ImGui with no device, and the
            picture of the ring test shows the label "Dimensions" a little wider than its segment at 1280 by 720.

## Progress

- 2026-11-11: opened by the request of the owner, after the merge of P0.143.
- 2026-11-11: the segment, its icon, its state and its tests. The close of R20b1 is the fifth product milestone after the
  review of v0.327, so the codebase review of 2026-11-12 (TASK-0349, v0.333) came first and merged into this branch.
- 2026-11-12: corrected V172 of that review in ADR-0119 (1.80 radians, not 1.84). Run 43116794483 passed on the branch. Closed
  at v0.334. TASK-0350 changes DesktopUi.Ring.cs, so its governed-by now names ADR-0122.
