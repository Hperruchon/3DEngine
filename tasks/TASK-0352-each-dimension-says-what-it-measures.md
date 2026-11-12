---
id: 0352
title: Each dimension says what it measures
status: Active
phase: R20c
opened: 2026-11-12
depends-on: [0340, 0351]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0056, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083, 0084, 0086, 0087, 0089, 0091, 0092, 0093, 0097, 0098, 0099, 0100, 0101, 0102, 0104, 0105, 0107, 0108, 0113, 0116, 0117, 0119, 0120, 0123, 0124, 0125]
writes:
  create:
    - tasks/TASK-0352-each-dimension-says-what-it-measures.md
    - docs/adr/0125-each-dimension-says-what-it-measures.md
    - 3DEngine.ImageTests/DimensionLabelsTests.cs
  modify:
    - 3DEngine.App/DimensionOverlay.cs
    - 3DEngine.App/Ui/DesktopUi.Overlay.cs
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.ImageTests/DimensionOverlayTests.cs
    - 3DEngine.ImageTests/DimensionImageTests.cs
    - docs/adr/0056-dimension-overlay.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - 3DEngine.Core/**
---

# TASK-0352 — Each dimension says what it measures

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The UX test of 2026-11-10 (`docs/proposals/2026-11-10-ux-test-of-a-multi-part-project.md`, problems 3 and 4) found that
a dimension does not say what it measures, and that the labels cover each other, the status line, the ring and the
history arc. The owner took improvement C on 2026-11-10 (roadmap row R20c), after R18c (TASK-0340). The owner started
this task with "Go" on 2026-11-12, after the merge of R20b2 (TASK-0351).

## Goal

A person with the dimensions on says at once which value is of the selected body and which is of the whole, and no
control covers a value.

## Scope (in)

1. ADR-0125, which amends ADR-0056 items 4, 5 and 6.
2. The dimensions of the selected body in the colour of the selection with its name; the dimensions of the whole in a
   grey with the word "Overall".
3. Each label on a plate outside the outline of its box, with its dimension line through the plate.
4. No plate under the status line, the fixed controls, the panels, the rows, the ring or the history arc.
5. No thickness for a body that is not a board.
6. Tests for items 2 to 5.

## Scope (out)

- The three decimals of ADR-0115 on the plates of the overlay.
- A plate that the placement cannot free from a control stays under it.
- The dimensions on a portrait phone: no test on the phone.

## Acceptance criteria

- [ ] ADR-0125 exists. ADR-0056 shows the amendment.
- [ ] A test proves the word and the name of each plate; it fails when a plate has no word.
- [ ] A test proves the colour of the selection and the grey of the whole.
- [ ] A test proves that each plate is outside the outline of its box; it fails when a plate is on its edge.
- [ ] A test proves that no plate is under the status line, the ring or the history arc, in a scene where a plate starts
      under one of them.
- [ ] A test proves that a cube and a bar show no thickness, and a panel does.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the word, the colour and the plate of each label, and the covers of the placement.
Judgement:  the grey of the whole, the width of a name, the ratio of a board (a quarter of the middle extent), the order
            of the moves of a plate, and the covers (the icons and the labels of the ring, not its band).
Weakest:    to be written at the close.

## Progress

- 2026-11-12: opened by the order "Go" of the owner, after the merge of R20b2.
