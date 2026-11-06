---
id: 0248
title: The title of the document, Undo, Redo and the status line are fixed controls of the redesign
status: Done
phase: R16b1
opened: 2026-11-06
depends-on: [0246]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0078, 0080]
writes:
  create:
    - tasks/TASK-0248-the-fixed-controls-of-the-redesign.md
    - docs/adr/0080-the-fixed-controls-of-the-redesign.md
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.ImageTests/FixedControlsTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - docs/adr/0030-document-file-in-the-desktop.md
    - docs/adr/0031-undo-and-redo.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0248 — The title of the document, Undo, Redo and the status line are fixed controls of the redesign

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner ordered the layout of the redesign slice by slice on 2026-11-06 (evening), with the fixed controls first: the
name, Undo and Redo. The "mix" direction of the designer brief shows them at the top left and the bottom left. The panel
holds them today, and at 1280 by 720 it has no free line.

## Goal

The title of the document is the title bar of the panel, Undo and Redo are round anchors at the bottom left, and the
address is a status line on their row; the panel holds two lines fewer.

## Scope (in)

1. ADR-0080, Accepted under the standing order of 2026-11-05; it amends ADR-0030 item 3 and ADR-0031 item 11.
2. The title bar, the anchors with their drawn arrows, the status line, and their rectangles.

## Scope (out)

- The command field of the brief, which needs the agent of the redesign and so a question to the owner.
- The tool ring, the values on the edges, the rows without a panel, and the sketch under its body: later slices.

## Acceptance criteria

- [x] Undo and Redo are anchors of 48 pixels at the bottom left, under no panel, and a tap on them does not reach the
      scene.
- [x] The title names the program, the file or "unsaved", and the unit.
- [x] Each image test passes, and the pictures are kept for the owner.
- [x] An injection that removes the fixed controls, and one that lets the panel cover them, each fail a test.

## Outcome

Status: Done · commit (the merge of the branch redesign-b1-fixed-b) · v0.233

## Method

Mechanical: three small windows with no decoration and no background.
Judgement:  the title in the title bar (Q1 A) and the status line on the row of the anchors (Q2 A), because the panel and
            the window of the sketch leave no other room at 1280 by 720.
Weakest:    no person tapped the anchors on a display or on the phone; on the portrait screen of the phone the panel
            and the anchors share the left edge, which no picture shows yet.

## Progress

- 2026-11-06: the fixed controls and two tests; the two injections each fail a test. The bitmap font has no middle dot,
  so the title joins its parts with a bar; the dim colour of a status line gave 2.9:1 on the clear, so it takes the
  colour of the text.
- 2026-11-06: run 42298401536 passed. Closed at v0.233.
