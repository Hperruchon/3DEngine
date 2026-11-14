---
id: 0128
title: A value typed in the command line goes to the pad of its value, through the evaluator
status: Accepted
topic: Clients, rendering
date: 2026-11-14
supersedes: []
superseded-by: []
amends: ['0098']
amended-by: []
affects:
  - 3DEngine.App/Ui/DesktopUi.Command.cs
  - 3DEngine.App/Ui/DesktopUi.Values.cs
  - 3DEngine.App/Ui/DesktopUi.Pad.cs
  - 3DEngine.App/Ui/DesktopUi.Sketch.cs
  - 3DEngine.ImageTests/TypedValueTests.cs
enforced-by: 3DEngine.ImageTests/TypedValueTests.cs
---

# ADR-0128 — A value typed in the command line goes to the pad of its value, through the evaluator

This document uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0098 Q1 recommended A: the command line runs the commands of the state, and a number typed in it finds no command. On
2026-11-08 the owner chose B: "a value too". ADR-0108 item 4 gave the field the text of the render, "Type a command or a
value", and said that the typed value is slice S8 (`docs/proposals/2026-11-10-gaps-to-the-render.md`, gap G10). The
roadmap row R18h says: "A value typed in the command line goes to the pad of its command, through the evaluator".

ADR-0032 item 1 makes the pad and its evaluator the only path of a number of the model: the evaluator reads an expression,
and a command records the result number only. A value that the window applies with no pad has no place to show an error of
the evaluator or a refusal of the engine.

The window shows the values that a command changes as chips: the distance and the lengths of the sketch of a selected
extrude (ADR-0082), and the values of the active sketch (ADR-0105). A tap on a chip opens the pad of its value.

On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each question. This record takes
the recommended form below.

## Decision

1. **The list (amends ADR-0098 item 3).** When the evaluator of the pad (`Expression.Evaluate`) reads the typed text as a
   number, the list gives, before the commands whose name holds the text, one entry for each chip that the last frame
   showed, and "Extrude" while a sketch is active. Each entry names its value and the result: "C3 = 75.000 mm",
   "Distance = 75.000 mm", "Extrude 75.000 mm". The name of a value is its label in its sketch ("C3", ADR-0034 item 8), else
   the word of its row ("Distance"). A text that the evaluator does not read, as "fra", "2*" or "40 mm", gives no value
   entry. The value has three decimals and the unit (ADR-0115 item 1).
2. **The pad applies it.** Enter, or a tap on an entry, opens the pad of that value, as a tap on its chip does, with the
   typed text in the field: "40+35", not 75. The chip is pending while the pad is open (ADR-0082 item 4). Enter in the pad
   applies the result through the same action as before. So the pad and its evaluator stay the only path of a number
   (ADR-0032 item 1 holds), and an error or a refusal shows in the pad.
3. **No contract change.** The entries read the chips of the last frame, which are presentation (ADR-0007). No command,
   query or event is new.

## Consequences

**Good:** a person who sees "60.000 mm" on an edge types "75" and two times Enter, with no tap on the small chip. An
expression works as in the pad.

**Bad:** a value needs two times Enter, one for the list and one for the pad. A value that no chip shows, as the size of
a box, has no entry. A command with a pad of two or three fields (Move, Next point, the primitives) has no entry.

## Alternatives considered

- **A typed value applies at once to the selected value.** Refused for now: the window has no selected value outside an open
  pad, and an error of the evaluator or a refusal of the engine has no place to show; ADR-0032 item 1 would need an
  amendment.
- **The first field of each pad of two or three fields.** Refused: "5" for Move does not say which axis it moves.

## History

- 2026-11-14 · TASK-0357 · The record, from slice S8 of the study of the gaps to the render, with the recommended option
  under the order of the owner of 2026-11-14.
- 2026-11-14 · TASK-0357 · Shipped at v0.341.
