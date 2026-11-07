---
id: 0264
title: The chips, the ring and the collapsed panel hold their next forms
status: Ready
phase: P0.107
opened: 2026-11-07
depends-on: [0263]
governed-by: [0007, 0019, 0027, 0029, 0030, 0032, 0034, 0072, 0073, 0075, 0076, 0077, 0078, 0080, 0081, 0082, 0083]
writes:
  create:
    - tasks/TASK-0264-desktop-forms-of-the-twenty-eighth-review.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.cs
    - 3DEngine.App/Ui/DesktopUi.Pad.cs
    - 3DEngine.App/Ui/DesktopUi.Ring.cs
    - 3DEngine.App/Ui/DesktopUi.Values.cs
    - 3DEngine.App/Ui/DesktopUi.Panel.cs
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

Status: Ready

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  to be written in the task.
Weakest:    to be written at the close.

## Progress

- 2026-11-07: opened by the codebase review of 2026-11-07.
