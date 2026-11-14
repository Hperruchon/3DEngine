---
id: 0373
title: The high findings of the codebase review of 2026-11-14 (d)
status: Ready
phase: P0.149
opened: 2026-11-14
depends-on: [0371]
governed-by: []
writes:
  create:
    - tasks/TASK-0373-high-findings-of-the-review-of-2026-11-14-d.md
  modify:
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0373 — The high findings of the codebase review of 2026-11-14 (d)

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-14 (d) (`docs/reviews/2026-11-14-d-codebase-review.md`) gives two high findings, V185 and V186,
and the medium finding V187, whose test V186 needs. Section 7 of `docs/templates.md` puts them before the next product phase,
R21c.

## Goal

A sketch leaves a flat view, no end of the arc of a proposal lies on a tool of the ring at a small landscape size, and the
guard of V160 has a test again.

## Scope (in)

1. V185: a sketch that starts in a flat view leaves it.
2. V186: Reject and its word as Accept, and the ring of the arc moved when no end can be free; the small landscape sizes.
3. V187: a scene where an end lies on a tool, and the control of the test of V160.
4. A test for each, which fails before the fix.

## Scope (out)

- V188 to V190 (TASK-0374).

## Acceptance criteria

- [ ] A test proves that a sketch from the top view and from the front view leaves the flat view.
- [ ] A test proves at the small landscape sizes that no end of the arc lies on a tool target.
- [ ] The test of V160 has a scene with an end on a tool and a control that fails without it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: the exit of a flat view in Face; the test of both ends.
Judgement:  how the ring moves when no angle frees an end.
Weakest:    to be written at the close.

## Progress

- 2026-11-14: opened by the codebase review of 2026-11-14 (d) (TASK-0371).
