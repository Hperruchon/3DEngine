---
id: 0362
title: The medium and low findings of the codebase review of 2026-11-14 (b)
status: Ready
phase: P0.147
opened: 2026-11-14
depends-on: [0361]
governed-by: []
writes:
  create:
    - tasks/TASK-0362-medium-and-low-findings-of-the-review-of-2026-11-14-b.md
  modify:
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0362 — The medium and low findings of the codebase review of 2026-11-14 (b)

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-14 (b) (`docs/reviews/2026-11-14-b-codebase-review.md`) gives one medium finding (V178) and two
low findings (V179 and V180). Section 7 of `docs/templates.md` puts them after the next product phase, R18l.

## Goal

"Undo" of the line after an accept undoes the step of the proposal or does not show, no plate lies under the arc of a
proposal, and the mix test checks Reject and Accept at each size.

## Scope (in)

1. V178: the button "Undo" of the line shows only while the newest step is the last step of the proposal.
2. V179: the covers of the plates hold the targets and the plate of the lines of the arc of the last frame.
3. V180: the checks of Reject and Accept at each size of the mix test.
4. A test for V178 and V179, which fails before the fix.

## Scope (out)

- An undo of the whole proposal in one tap.

## Acceptance criteria

- [ ] A test proves that a step after an accept takes the button "Undo" away.
- [ ] A test proves that no plate is under Accept or the lines of the arc of a proposal.
- [ ] The mix test checks Reject and Accept at each size.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: the count of the log at the accept; the covers; the two checks.
Judgement:  when the button goes.
Weakest:    to be written at the close.

## Progress

- 2026-11-14: opened by the codebase review of 2026-11-14 (b) (TASK-0361).
