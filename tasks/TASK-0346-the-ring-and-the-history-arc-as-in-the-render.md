---
id: 0346
title: The ring with Edit sketch and Rename, and the history arc, as in the render
status: Active
phase: R18e
opened: 2026-11-11
depends-on: [0342]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0086, 0087, 0088, 0089, 0091, 0092, 0093, 0095, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0107, 0108, 0111, 0113, 0114, 0117, 0119]
writes:
  create:
    - tasks/TASK-0346-the-ring-and-the-history-arc-as-in-the-render.md
    - docs/adr/0119-the-ring-and-the-history-arc-as-in-the-render.md
    - 3DEngine.ImageTests/RingToolsTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.Orbit.cs
    - 3DEngine.App/Ui/DesktopUi.Command.cs
    - 3DEngine.App/Ui/DesktopUi.History.cs
    - 3DEngine.App/Ui/DesktopUi.Rows.cs
    - 3DEngine.ImageTests/RingImageTests.cs
    - 3DEngine.ImageTests/ProposalArcTests.cs
    - docs/adr/0081-the-tool-ring-of-the-redesign.md
    - docs/adr/0098-the-command-line.md
    - docs/adr/0104-the-history-arc.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0346 — The ring with Edit sketch and Rename, and the history arc, as in the render

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The proposal `docs/proposals/2026-11-10-gaps-to-the-render.md` gives gaps G5 and G6: the render has "Edit sketch" and "Rename"
on the ring of the bracket, and a bright history arc with its nodes on it. The window has neither tool, and a faint arc with a
round button for each node. The owner accepted each recommended answer on 2026-11-10 and ordered R18e after R18d.

## Goal

The ring and the history arc of a selected body look as in the render, and no tool of the ring goes under the arc of a
proposal.

## Scope (in)

1. ADR-0119, from slice S5 of the proposal.
2. "Edit sketch" and "Rename" on the ring, with their icons, and "Edit sketch" in the command line.
3. The span of a landscape ring at most four segments, with narrower segments, and the two tools out of a small ring.
4. The history arc as a bright line with a glow, and each node a dot on it.
5. Tests: the tools and their taps; no Edit sketch for a box and no Rename on a portrait ring; the span at three sizes;
   the command; the target of a node; the picture of the bracket at 1440 by 900.

## Scope (out)

- The rows of a sketch (S6, R18f).
- An icon for "Focus".

## Acceptance criteria

- [ ] ADR-0119 exists. ADR-0081, ADR-0098 and ADR-0104 show the amendment.
- [ ] A test proves the tap of each new tool; it fails when the tap gives no action.
- [ ] A test proves the span of a landscape ring; it fails when six segments keep 0.42 radians.
- [ ] The picture of the bracket at 1440 by 900 shows "Edit sketch", "Rename" and the history arc.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the two tools from the actions that exist; the look of the arc.
Judgement:  the span of four segments; the tools that leave a small ring; no Rename on a portrait ring.
Weakest:    to be written at the close.

## Progress

- 2026-11-11: opened and started on the order of the owner, on the branch r18e-ring-history from the branch of R18d
  (r18d-rows, 53d1d9e). TASK-0346 and ADR-0119 are free on `main` and on each branch. A first form let a landscape ring span
  2.6 radians; the layout test at 640 by 360 and the test of the dimmed tools showed that the top segment then reaches the
  top bar and the arc of a proposal, so the span is four segments.
- 2026-11-11: merge of the closed branch of R18d (4deec1d, v0.328): the index of the ADRs keeps the rows of ADR-0118 and
  ADR-0119; TASK-0342 is Done, so it needs no ADR-0119.
