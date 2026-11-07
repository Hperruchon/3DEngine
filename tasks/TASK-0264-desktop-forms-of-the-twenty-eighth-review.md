---
id: 0264
title: The chips, the ring and the collapsed panel hold their next forms
status: Active
phase: P0.107
opened: 2026-11-07
depends-on: [0263]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083]
writes:
  create:
    - tasks/TASK-0264-desktop-forms-of-the-twenty-eighth-review.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Pad.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.Values.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
    - 3DEngine.ImageTests/EdgeValueTests.cs
    - 3DEngine.ImageTests/ToolRingTests.cs
    - 3DEngine.ImageTests/FixedControlsTests.cs
    - docs/adr/0081-the-tool-ring-of-the-redesign.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0264 — The chips, the ring and the collapsed panel hold their next forms

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-07 found V119 to V123 (low). By `docs/templates.md`, section 7, they come after the next
product phase, and after P0.106.

## Goal

A pad of another tool clears the pending chip, one tap gives one tool, the ring stays inside the display and its safe
area, and a collapsed panel stays above the anchors.

## Scope (in)

1. V119 to V123.

## Scope (out)

- E168 to E171 and T165 to T167 (TASK-0263).

## Acceptance criteria

- [ ] Each finding has a test or an injection that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  V119: a new pad ends the pending state, and the two pads of a value set it again. V120: the chip wins,
            because it is drawn over the ring; the ring reads the chips of the last frame, which the person sees.
            V121: the portrait segments narrow to fit a quarter of a turn, not a smaller ring. V122: the inset now,
            not a register entry, because it is one argument. V123: the left clamp is 16 pixels inside the inset, as
            the comment said, and the comment of the title moves to Title.
Weakest:    to be written at the close.

## Progress

- 2026-11-07: opened by the codebase review of 2026-11-07.
- 2026-11-07: V119 to V123, each with a test that failed before the change: the pending chip after the pad of Move,
  the chip on Hide that hid the body, the fourth portrait segment at 4.94 radians, the portrait centre with no inset,
  and the collapsed panel at (-24, 680) over Undo. ADR-0081 items 1 and 2 give the portrait forms.
