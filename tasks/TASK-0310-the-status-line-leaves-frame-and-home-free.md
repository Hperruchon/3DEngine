---
id: 0310
title: The status line leaves Frame and Home free on each display
status: Active
phase: P0.129
opened: 2026-11-08
depends-on: [0309]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0080, 0083, 0093]
writes:
  create:
    - tasks/TASK-0310-the-status-line-leaves-frame-and-home-free.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.Fixed.cs
    - 3DEngine.ImageTests/FixedControlsTests.cs
    - docs/adr/0093-the-layout-of-the-mix-has-no-panel-chrome.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
---

# TASK-0310 — The status line leaves Frame and Home free on each display

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-08 (c) found V146 (high): on a phone of 390 by 844 the status line of R16j covers Frame and
Home, and a tap on them does nothing. It comes before the next product phase.

## Goal

The status line never covers an anchor, and a tap on Frame and Home gives its action at 390 by 844.

## Scope (in)

1. V146: the status line shows on the row of the anchors only where it fits right of the anchors; else a shorter form, or
   none. ADR-0093 item 3 says so.
2. A test of a tap on Frame and Home at 390 by 844, and of the status line clear of the anchors at three sizes.

## Scope (out)

- V147, V149, V150 and V151 (TASK-0311).

## Acceptance criteria

- [ ] Each finding has a test, an injection or a reading that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: a test, an injection or a reading for each finding, before the change.
Judgement:  the line drops its address, then itself, where it does not fit (Q of the review: no row above the anchors,
            which the portrait ring holds).
Weakest:    to be written at the close.

## Progress
- 2026-11-08: the fix and its test. With the status code of v0.292 the test fails at 390 x 844 and at 844 x 390. The first
  form of the fix placed the window by a pivot at its right edge, which took the width of the last frame; on the second
  frame the line still covered Home at 844 x 390. The window now starts at the right edge less the width of its text.
