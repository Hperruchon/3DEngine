---
id: 0330
title: Accept of the agent arc takes a tap in the default window
status: Ready
phase: P0.138
opened: 2026-11-09
depends-on: [0329]
governed-by: [0007, 0019, 0025, 0027, 0029, 0030, 0032, 0034, 0072, 0092]
writes:
  create:
    - tasks/TASK-0330-the-high-finding-of-the-review-of-2026-11-09-b.md
  modify:
    - 3DEngine.App/Ui/DesktopUi.Proposal.cs
    - 3DEngine.ImageTests/ProposalArcTests.cs
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0330 — Accept of the agent arc takes a tap in the default window

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

The write set is a first estimate; the implementer corrects it at the start of the work.

## Context

The codebase review of 2026-11-09 (b) (`docs/reviews/2026-11-09-b-codebase-review.md`) found V159: in the default window of
1280 by 720, with a live sketch and no selection, the window of the live sketches covers Accept, and a tap on it does
nothing. A high finding goes into a task before the next product phase.

## Goal

At 1280 by 720, 1440 by 900 and 1920 by 1080, with a live sketch and no selection, a tap on Accept and a tap on Reject each
give their action.

## Scope (in)

1. V159: Accept and Reject are out of each panel of the frame, or the window of the live sketches does not show while a
   proposal waits.
2. A test at the three display sizes, which fails on an injection of the old place.

## Scope (out)

- V160 (a tap on Accept that also uses a ring tool), which TASK-0331 holds; the fix must not move Accept onto a tool of a ring.

## Acceptance criteria

- [ ] The test fails on an injection of the old place and passes after the fix.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings, and the phone project builds.
- [ ] Continuous integration passes on the branch and on `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: the probe of the review gives the places at the three sizes.
Judgement:  to be written.
Weakest:    to be written at the close.

## Progress
