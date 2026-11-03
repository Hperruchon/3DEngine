---
id: 0176
title: The walk of the planes repeats until no datum and no sketch changes, and an edit of a sketch takes the record of the walk
status: Active
phase: P0.75
opened: 2026-11-03
depends-on: [0175]
governed-by: [0004, 0033, 0035, 0037, 0039, 0051, 0052, 0054, 0055]
writes:
  create:
    - tasks/TASK-0176-the-planes-of-a-later-reference.md
  modify:
    - Engine.Core/Sketch/DatumResolver.cs
    - Engine.Core/Sketch/SketchResults.cs
    - Engine.Core/Features/Regeneration.cs
    - Engine.Tests/Sketch/SketchReferenceTests.cs
    - docs/adr/0055-regeneration.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0176 — The walk of the planes repeats until no datum and no sketch changes, and an edit of a sketch takes the record of the walk

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The second codebase review of 2026-11-03 found E128 and E129 (medium). By `docs/templates.md`, section 7, a medium
finding goes after the next product phase. On 2026-11-03 the owner gave the order "merge, fix E128 and E129, then R13",
so this task comes before R13.

## Goal

A datum and a reference of a sketch read the last record of each sketch after an edit, so no later edit moves them,
and an edit of a sketch never throws because the walk changes the same sketch.

## Scope (in)

1. E128: `DatumResolver.Rebuild` walks the planes again until no datum and no sketch changes. A warning comes from the
   last walk only.
2. E129: `SketchResults.Changed` takes the record of the walk for the edited sketch when the walk gives one.
3. A test of the layout of each finding before the change, and an ADR-0055 history line.

## Scope (out)

- V76, V77, E130 and T130 to T132 (TASK-0177, after R13).

## Acceptance criteria

- [ ] A test of the layout of E128 fails before the change and passes after it.
- [ ] A test of E129 fails before the change and passes after it.
- [ ] Each injection of the change fails a test.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the two tests before the change; injections.
Judgement:  the bound of the repeated walk.
Weakest:    to be written at the close.

## Progress

- 2026-11-03: opened by the second codebase review of 2026-11-03, with the order of the owner.
