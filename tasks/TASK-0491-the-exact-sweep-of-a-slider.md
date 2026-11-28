---
id: 0491
title: CheckTravel moves a slider from limit to limit by one exact sweep
status: Active
phase: K12b
opened: 2026-11-28
depends-on: [0490]
governed-by: [0004, 0016, 0018, 0028, 0033, 0037, 0060, 0195, 0196]
writes:
  create:
    - tasks/TASK-0491-the-exact-sweep-of-a-slider.md
    - docs/adr/0196-the-exact-sweep-of-a-slider.md
    - Engine.Tests/Queries/CheckSlideTests.cs
  modify:
    - Engine.Core/Queries/CheckTravelQueryHandler.cs
    - docs/adr/0195-the-travel-of-a-pivot.md
    - docs/adr/README.md
    - docs/diagnostics.md
    - docs/guides/agent-loop.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
---

# TASK-0491 — CheckTravel moves a slider from limit to limit by one exact sweep

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Row K12b: one exact sweep of a slider. ADR-0195 (K12a) gave the travel of a pivot and left the slider to this row.

## Goal

`CheckTravel` gives the exact distance of the first contact of a slider, or says that its travel is free.

## Scope (in)

1. The sweep of a slider in `CheckTravel` and ADR-0196. No change of `Engine.Contracts`, so the standing order of 2026-11-28
   answers its choices with the recommended option.

## Scope (out)

- The envelope (K13) and the tools of the window (R23, R27).

## Acceptance criteria

- [x] A test of each item of ADR-0196, and two injections that make a test fail.
- [ ] A clean build, `dotnet test`, the write-set check and `eng/repo-gates` pass, and continuous integration passes.

## Outcome

Status: Active · commit (the merge of the branch k12b-slider-sweep) · v0.475

## Method

Mechanical: each contact of the tests is a plane at a known x: 9 for a face at x = 35, and 14 - 2√2 for a bar turned by 45 degrees.
Judgement:  the events of a vertex on a face and of an edge on an edge, with the borders, in place of a swept solid.
Weakest:    the direction of the slider comes from the doubles of the placed frame.

## Progress

- 2026-11-28: opened under the standing order of 2026-11-28, after K12a (v0.474). ADR-0196, the sweep, and 5 tests in
  `Engine.Tests/Queries/CheckSlideTests.cs`. Two injections (no events of two edges; no events of a vertex of the fixed body) each
  made a test fail. Ledger v0.475.
