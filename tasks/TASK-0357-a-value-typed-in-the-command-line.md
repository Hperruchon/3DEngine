---
id: 0357
title: A value typed in the command line goes to the pad of its value, through the evaluator
status: Done
phase: R18h
opened: 2026-11-14
depends-on: [0354, 0355]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0082, 0084, 0087, 0088, 0091, 0097, 0098, 0099, 0101, 0102, 0105, 0108, 0110, 0113, 0115, 0119, 0120, 0127, 0128]
writes:
  create:
    - tasks/TASK-0357-a-value-typed-in-the-command-line.md
    - docs/adr/0128-a-value-typed-in-the-command-line-goes-to-its-pad.md
    - 3DEngine.ImageTests/TypedValueTests.cs
  modify:
    - 3DEngine.App/Ui/DesktopUi.Command.cs
    - 3DEngine.App/Ui/DesktopUi.Values.cs
    - 3DEngine.App/Ui/DesktopUi.Pad.cs
    - 3DEngine.App/Ui/DesktopUi.Sketch.cs
    - docs/adr/0098-the-command-line.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0357 — A value typed in the command line goes to the pad of its value, through the evaluator

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Slice S8 of the study of the gaps to the render (`docs/proposals/2026-11-10-gaps-to-the-render.md`, gap G10) is roadmap row
R18h. On 2026-11-08 the owner chose ADR-0098 Q1 B, a value in the command line. The field says "Type a command or a value"
since R18a, and a typed number finds no command. On 2026-11-14 the owner ordered work alone until Sunday, with the
recommended option for each question.

## Goal

A person types a value or an expression in the command line, and Enter opens the pad of the value with that text, so the
pad applies it.

## Scope (in)

1. ADR-0128, which amends ADR-0098 item 3.
2. The entries of the values of the frame and of "Extrude", before the commands, when the evaluator reads the text.
3. The pad of the value with the typed text.
4. Tests for items 2 and 3.

## Scope (out)

- A value that applies with no pad.
- The pads of two or three fields.

## Acceptance criteria

- [x] ADR-0128 exists. ADR-0098 shows the amendment.
- [x] A test proves that an expression lists each value with its result, and that Enter opens the pad of the first one with
      the typed text, whose apply gives the result.
- [x] A test proves that the arrows choose a value of a constraint.
- [x] A test proves that an active sketch lists its values with their labels and "Extrude".
- [x] A test proves that a text that is not a number lists no value.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [x] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch r18h-typed-value) · v0.341

## Method

Mechanical: the entries, the start text of the pads.
Judgement:  the pad as the path of a typed value, and the chips of the last frame as the values.
Weakest:    a value needs two times Enter, and a value with no chip has no entry. No person used the change in the window.

## Progress

- 2026-11-14: opened under the order of the owner of 2026-11-14, after the codebase review of 2026-11-14 (TASK-0355), at
  `main` 78d0096 (v0.340), branch r18h-typed-value. TASK-0357 and ADR-0128 are free. Seven injections each failed a named
  test; each file came back byte for byte.
- 2026-11-14: run 43536120912 passed on the work commit. A picture of the bracket at 1280 by 720 with "40+35" typed shows the
  list "Distance = 75.000 mm", "C1 = 75.000 mm" and "C2 = 75.000 mm". Closed at v0.341.
