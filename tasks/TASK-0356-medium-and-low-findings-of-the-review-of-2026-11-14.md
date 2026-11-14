---
id: 0356
title: The medium and low findings of the codebase review of 2026-11-14
status: Ready
phase: P0.146
opened: 2026-11-14
depends-on: [0355]
governed-by: []
writes:
  create:
    - tasks/TASK-0356-medium-and-low-findings-of-the-review-of-2026-11-14.md
  modify:
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0356 — The medium and low findings of the codebase review of 2026-11-14

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-14 (`docs/reviews/2026-11-14-codebase-review.md`) gives two medium findings (V173 and V174)
and three low findings (V175, V176 and V177) in the window, the dimension plates and the edge lines. Section 7 of
`docs/templates.md` puts them after the next product phase, R18h.

## Goal

No plate goes under a window of the frame, each hidden body and "Show all" can be reached on a small landscape display, a
T-vertex of a turned body adds no line, a stuck plate stays at its start, and a step text goes to its own command.

## Scope (in)

1. V173: the covers of the plates hold the panels and the fixed rectangles of the last frame.
2. V174: "Show all" and each hidden body can be reached at 844 by 390 and 640 by 360.
3. V175: collinear edges with a relative tolerance, and the uncovered parts of each long edge.
4. V176: a plate that no move frees goes back to its start.
5. V177: an expected text matches the identifier of its command.
6. A test for each, which fails before the fix.

## Scope (out)

- A change of the order of the windows of the frame.

## Acceptance criteria

- [ ] A test proves that no plate is under the window of the live sketches in the scene of the probe.
- [ ] A test proves that "Show all" and the last hidden body are inside the display at 844 by 390.
- [ ] A test proves that a turned mesh with a T-vertex and two T-vertices side by side give no false line.
- [ ] A test proves that a stuck plate stays at its start.
- [ ] A test proves that a command of another client with the same name does not take the text of the window.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: the covers of the last frame; the start of a stuck plate; the match by identifier.
Judgement:  the form of the list of the hidden bodies; the tolerance of a collinear edge.
Weakest:    to be written at the close.

## Progress

- 2026-11-14: opened by the codebase review of 2026-11-14 (TASK-0355).
