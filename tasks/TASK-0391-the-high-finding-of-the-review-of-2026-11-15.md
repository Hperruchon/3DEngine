---
id: 0391
title: The high finding of the codebase review of 2026-11-15
status: Ready
phase: P0.158
opened: 2026-11-15
depends-on: [0390]
governed-by: []
writes:
  create:
    - tasks/TASK-0391-the-high-finding-of-the-review-of-2026-11-15.md
  modify:
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
---

# TASK-0391 — The high finding of the codebase review of 2026-11-15

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-15 (`docs/reviews/2026-11-15-codebase-review.md`) gives the high finding E206: each read of a
sketch finds its fixed lengths with one full elimination for each line, and the window reads the active sketch in each frame.
Section 7 of `docs/templates.md` puts it before the next product phase, P10.2.

## Goal

A read of a sketch of 100 lines takes less time than one frame, and the fixed lengths stay the same.

## Scope (in)

1. E206: the fixed lengths from one reduction of the Jacobian, and only for a read that asks for them.
2. A test of the time of a sketch of 100 lines, which fails before the fix.

## Scope (out)

- E207 to E210, V192 and V193 (TASK-0392).

## Acceptance criteria

- [ ] Each test of the fixed lengths passes with the new computation.
- [ ] A test proves that a read of a sketch of 100 lines takes less time than its limit.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: the reduction against the pivot rows.
Judgement:  the limit of the time in the test.
Weakest:    to be written at the close.

## Progress

- 2026-11-15: opened by the codebase review of 2026-11-15 (TASK-0390).
