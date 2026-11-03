---
id: 0176
title: The walk of the planes repeats until no datum and no sketch changes, and an edit of a sketch takes the record of the walk
status: Done
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

- [x] A test of the layout of E128 fails before the change and passes after it.
- [x] A test of E129 fails before the change and passes after it.
- [x] Each injection of the walk fails a test. The two injections of the guard of E129 fail none, because no path
      reaches the guard after the fix of E128; the ledger of v0.178 records it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p075-planes-of-a-later-reference) · v0.178

## Method

Mechanical: three tests, two of them failing before the change; six injections.
Judgement:  the bound of the repeated walk, and the guard of E129 that no test reaches.
Weakest:    the guard of E129 has no test, and the bound of the loop has no test.

## Progress

- 2026-11-03: opened by the second codebase review of 2026-11-03, with the order of the owner.
- 2026-11-04: the repeated walk and the record of the walk; three tests; closed at v0.178.
