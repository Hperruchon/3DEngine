---
id: 0254
title: A value that a command changes sits on the edge of the selected body that it measures
status: Active
phase: R16c
opened: 2026-11-07
depends-on: [0252, 0253, 0255]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082]
writes:
  create:
    - tasks/TASK-0254-the-values-on-the-edges-of-the-redesign.md
    - docs/adr/0082-the-values-on-the-edges-of-the-redesign.md
    - 3DEngine.App/EdgeValues.cs
    - 3DEngine.App/Ui/DesktopUi.Values.cs
    - 3DEngine.ImageTests/EdgeValueTests.cs
    - 3DEngine.ImageTests/EdgeValueImageTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Pad.cs
    - 3DEngine.App/Ui/PanelAction.cs
    - 3DEngine.App/SketchActions.cs
    - 3DEngine.App/BodyActions.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0254 — A value that a command changes sits on the edge of the selected body that it measures

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The owner ordered the layout of the redesign slice by slice on 2026-11-06 (evening); after the fixed controls (R16b1) and
the tool ring (R16b2) come "the values on the edges for 'change a dimension'". The "mix" direction of the designer brief
puts the values of the selected part on its edges, and the job "change a dimension" ends in one command.

## Goal

The values of a selected extrude that a command changes sit on the edges that they measure, and a tap on a value opens
its pad, whose apply gives one command.

## Scope (in)

1. ADR-0082, Accepted under the standing order of 2026-11-05.
2. The values of a selected extrude from `GetFaces` and its sketch: the distance, and each length and radius.
3. The chips, their taps, their pads and their pending state.
4. The values from the host, with no query that waits for the serial section in each frame.

## Scope (out)

- A value of a box or of a cylinder, which no command changes today.
- The sketch under its body, and a value of each kind of constraint on its item.

## Acceptance criteria

- [ ] An extrude gives its distance and the lengths of its sketch, each once; a box gives none.
- [ ] Each chip is at least 44 pixels; a tap and a click on a chip open its pad and do not reach the scene.
- [ ] The apply of the pad of a length gives the new value of that constraint; the chip is pending while its pad is open.
- [ ] A picture of the values on the GPU is kept for the owner.

## Outcome

Status: Active

## Method

Mechanical: the values from the edge entries of GetFaces and the items of the side faces of the extrude.
Judgement:  the values that a command changes (Q1 A), the pad on a tap (Q2 A), and the pending state (Q3 A).
Weakest:    to be written at the close.

## Progress

- 2026-11-07: `EdgeValues`, the chips of `DesktopUi`, the action of a value of a sketch with no active sketch; six tests
  with no GPU and one image test. The first test showed a pending state that started only at the tap of a chip; the pad
  of the distance now gives it from the chip, the ring and the panel.
