---
id: 0363
title: A label of the corner ring is drawn smaller until it lies on no other label and fits inside the display
status: Active
phase: R18l
opened: 2026-11-14
depends-on: [0360, 0361]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0081, 0083, 0084, 0086, 0087, 0088, 0093, 0095, 0097, 0101, 0108, 0111, 0113, 0116, 0119, 0120, 0122, 0131, 0132]
writes:
  create:
    - tasks/TASK-0363-the-labels-of-the-corner-ring.md
    - docs/adr/0132-the-labels-of-the-corner-ring.md
    - 3DEngine.ImageTests/CornerRingLabelsTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - docs/adr/0081-the-tool-ring-of-the-redesign.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0363 — A label of the corner ring is drawn smaller until it lies on no other label and fits inside the display

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Gap H4 of the comparison at the other sizes of the brief (`docs/proposals/2026-11-14-the-render-at-other-sizes.md`) is roadmap
row R18l: on the corner ring of a portrait display, "Focus" and "Dimensions" lie on each other. On 2026-11-14 the owner
ordered work alone until Sunday, with the recommended option for each question. The codebase review of 2026-11-14 (b) came
first, as the gate asks.

## Goal

Each label of the corner ring is in its segment, on no other label and inside the display, at each portrait size.

## Scope (in)

1. ADR-0132, which amends ADR-0081.
2. The size of each label of the corner ring from its neighbours and the right edge.
3. A test at five portrait sizes.

## Scope (out)

- The ring of a landscape display.

## Acceptance criteria

- [ ] ADR-0132 exists. ADR-0081 shows the amendment.
- [ ] A test proves at five portrait sizes that no label of the corner ring lies on another and each is in the display.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the size of a label from its room.
Judgement:  the floor of half of the size, the gap of 4 pixels, and the neighbours at the same height.
Weakest:    "Dimensions" is in a text of half the size on a phone; no person read it on the S23.

## Progress

- 2026-11-14: opened under the order of the owner of 2026-11-14, after the review of 2026-11-14 (b), branch r18l-ring-labels.
  TASK-0363 and ADR-0132 are free. A first rule, the room across the band at the middle of the segment, left the labels on
  each other, because near the top the segments are side by side and the clamp to the edge moved "Dimensions"; a floor of
  0.6 of the size left "Dimensions" 70 pixels wide for a room of 57. The rule of the neighbours and the edge, with a floor of
  half of the size, holds at each size.
