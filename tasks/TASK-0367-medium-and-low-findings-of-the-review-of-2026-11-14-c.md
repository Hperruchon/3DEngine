---
id: 0367
title: The medium and low findings of the codebase review of 2026-11-14 (c)
status: Ready
phase: P0.148
opened: 2026-11-14
depends-on: [0366]
governed-by: []
writes:
  create:
    - tasks/TASK-0367-medium-and-low-findings-of-the-review-of-2026-11-14-c.md
  modify:
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0367 — The medium and low findings of the codebase review of 2026-11-14 (c)

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-14 (c) (`docs/reviews/2026-11-14-c-codebase-review.md`) gives one medium finding (V181) and
three low findings (V182 to V184). Section 7 of `docs/templates.md` puts them after the next product phase, R18o.

## Goal

No word of the summary of a proposal is lost, "Undo" of the line holds the count of the last commit of the accept, a moved
block of lines stays inside the inner circle, and "Edit sketch" takes a tap while a proposal waits.

## Scope (in)

1. V181: the whole caption under the arc in lines that fit the chord, with no cut.
2. V182: the count of the log at the last commit of the accept.
3. V183: the left limit at the top and the bottom of the plate.
4. V184: no end of the arc on the target of a tool of the ring, and the comment of `DimRing`.
5. A test for each, which fails before the fix.

## Scope (out)

- The arc on a portrait display (R18o).

## Acceptance criteria

- [ ] A test proves with a summary longer than the chord that each word of it shows.
- [ ] A test proves that a command of another client after the accept takes "Undo" away.
- [ ] A test proves that the plate of a moved block is inside the inner circle.
- [ ] A test proves that no end of the arc lies on the target of a tool of the ring.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: the lines of the caption; the count in the outcome; the corners of the plate.
Judgement:  where the arc ends or where the first tool goes.
Weakest:    to be written at the close.

## Progress

- 2026-11-14: opened by the codebase review of 2026-11-14 (c) (TASK-0366).
