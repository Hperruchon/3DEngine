---
id: 0374
title: The medium and low findings of the codebase review of 2026-11-14 (d)
status: Ready
phase: P0.150
opened: 2026-11-14
depends-on: [0373]
governed-by: []
writes:
  create:
    - tasks/TASK-0374-medium-and-low-findings-of-the-review-of-2026-11-14-d.md
  modify:
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
---

# TASK-0374 — The medium and low findings of the codebase review of 2026-11-14 (d)

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-14 (d) (`docs/reviews/2026-11-14-d-codebase-review.md`) gives the medium findings V188 and V189 and
the low finding V190. Section 7 of `docs/templates.md` puts them after the next product phase, R21c.

## Goal

A zoom in a flat view does not cut the part, the edge lines of a flat view do not pull through faces, and the rows know the
arc of a portrait display in its first frame.

## Scope (in)

1. V188: a scale of its own for a flat view, with the eye behind the box of the scene.
2. V189: a pull of the edge lines for a flat view.
3. V190: the place of the arc of a portrait display in the same frame as the rows.
4. A test for each, which fails before the fix.

## Scope (out)

- V185 to V187 (TASK-0373).

## Acceptance criteria

- [ ] A test proves that a zoom of ten times in a flat view keeps the eye out of the box of the scene.
- [ ] A test or a picture proves the pull of the edge lines in a flat view.
- [ ] A test proves that the rows are under the arc in the first frame of a proposal.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: the scale of a flat view; the pull; the place of the arc.
Judgement:  where the eye of a flat view goes.
Weakest:    to be written at the close.

## Progress

- 2026-11-14: opened by the codebase review of 2026-11-14 (d) (TASK-0371).
