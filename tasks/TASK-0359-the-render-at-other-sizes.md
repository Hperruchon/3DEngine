---
id: 0359
title: The window and the render at the other sizes of the brief, and the slices that close the gaps
status: Active
phase: R18j
opened: 2026-11-14
depends-on: [0358]
governed-by: [0025]
writes:
  create:
    - tasks/TASK-0359-the-render-at-other-sizes.md
    - docs/proposals/2026-11-14-the-render-at-other-sizes.md
  modify:
    - 3DEngine.ImageTests/MixImageTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Kernel/**
    - 3DEngine.App/**
---

# TASK-0359 — The window and the render at the other sizes of the brief, and the slices that close the gaps

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Slice S10 of the study of the gaps to the render (`docs/proposals/2026-11-10-gaps-to-the-render.md`) is roadmap row R18j: the
comparison of the window with the render at 1280 by 720, 1024 by 768, 768 by 1024 and the phone. The study of 2026-11-10
compared 1440 by 900 only. On 2026-11-14 the owner ordered work alone until Sunday, with the recommended option for each
question.

## Goal

The gaps between the window and the render at the other sizes have names, and the roadmap has a slice for each gap that a
change of the window closes.

## Scope (in)

1. A picture of the step of the proposal at each of the four sizes, from the mix test.
2. The study `docs/proposals/2026-11-14-the-render-at-other-sizes.md`: the gaps H1 to H7, the slices and the questions.
3. The rows R18k to R18n and the note of their order.

## Scope (out)

- Each change of the window: each slice opens its task.
- 1920 by 1080 and the other workflows of the brief.

## Acceptance criteria

- [ ] The four pictures draw on the GPU, with no message of the validation layer.
- [ ] The study names each gap with the render and the window, and each slice with its gaps.
- [ ] The roadmap holds R18k to R18n after R18j.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] `dotnet test`, the write-set check and `eng/repo-gates` pass; continuous integration passes on the branch and on
      `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the size of the mix test, the four pictures, the rows.
Judgement:  the gaps that the pictures show, the slices, and the recommended answers.
Weakest:    the pictures come from the image tests, not from a run of the program on a tablet or on the S23.

## Progress

- 2026-11-14: opened under the order of the owner of 2026-11-14, after R18i, at the commit of R18i (bb85833), branch
  r18j-sizes. TASK-0359 is free. A portrait display has no history arc and shows a proposal as a sheet, so the checks of the
  arc in the mix test hold on a landscape display only.
