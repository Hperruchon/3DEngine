---
id: 0477
title: The medium and low findings of the codebase review of 2026-11-28
status: Ready
phase: P0.193
opened: 2026-11-28
depends-on: [0476]
governed-by: []
writes:
  create:
    - tasks/TASK-0477-the-findings-of-the-review-of-2026-11-28.md
  modify: []
  forbid:
    - Engine.Contracts/**
---

# TASK-0477 — The medium and low findings of the codebase review of 2026-11-28

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-28 (`docs/reviews/2026-11-28-codebase-review.md`, TASK-0475) found two medium and seven low
findings. By `docs/templates.md`, section 7, they come after the next product phase.

## Goal

Each finding of this task has a correction and a test, or a register entry with its reason.

## Scope (in)

1. Medium: V239 (the window keeps the faces and the edges of the selected body by its key only) and E247 (ADR-0037 item 8 tells
   a client to read a mesh again only when the key changes).
2. Low: E248, E249, E250, V240, T203, T204 and T205, each with the correction that the review recommends. E249 takes the
   correction of the answer to Q2 of the review.
3. The write set grows when the task starts (`docs/templates.md`, section 2).

## Scope (out)

- E246: TASK-0476.
- E248 changes the JSON of `BodyPlacement` in `Engine.Contracts`, so the forbid holds `Engine.Contracts/**` until the task
  starts and adds the ADR or the History line that the change needs.

## Acceptance criteria

- [ ] Each finding has a test that fails before its correction, or a register entry.
- [ ] Each injection of a correction makes a named test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Ready · no commit on `main` yet

## Method

Mechanical: the corrections that the review names.
Judgement:  none yet.
Weakest:    none yet.

## Progress

- 2026-11-28: opened by TASK-0475.
