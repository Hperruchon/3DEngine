---
id: 0174
title: One regeneration evaluates the features again after an edit, and each step whose inputs did not change keeps its solid
status: Active
phase: P8d2
opened: 2026-11-03
depends-on: [0173]
governed-by: [0004, 0033, 0035, 0037, 0039, 0049, 0051, 0052, 0054, 0055]
writes:
  create:
    - tasks/TASK-0174-regeneration.md
    - docs/adr/0055-regeneration.md
    - Engine.Core/Features/Regeneration.cs
    - Engine.Tests/Features/RegenerationTests.cs
  modify:
    - Engine.Core/Sketch/SketchRebuild.cs
    - Engine.Core/Sketch/DatumResolver.cs
    - docs/CHARTER.md
    - docs/adr/0037-rebuild-of-an-extrude.md
    - docs/adr/0039-chain-of-a-consumed-extrude.md
    - docs/adr/0054-ordered-feature-list.md
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0174 — One regeneration evaluates the features again after an edit, and each step whose inputs did not change keeps its solid

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Phase P8d gives the three layers of a document. P8d1 gave the ordered feature list (ADR-0054). P8d2 gives the
regeneration. The owner gave the order "go ahead with P8d2" on 2026-11-03 and answered four questions on the same day,
each with the recommended answer: one regeneration pass replaces the three rebuild paths, a step whose inputs did not
change keeps its solid, the faults stay in the records, and no suppress. ADR-0055 holds them.

## Goal

One class evaluates the features again after an edit, with one order, one skip rule and one form of fault, and the
results of each current test do not change.

## Scope (in)

1. `Regeneration`: the walk of the planes (`DatumResolver.Rebuild`), then the walk of the solids, with a live extrude as
   the end of its own chain and each end in the log order of the command that creates it.
2. `SketchRebuild.Rebuild` and `SketchRebuild.RebuildDistance` give their inputs to the regeneration.
3. The sentence of anti-objective 4 of the charter on the place of a fault.

## Scope (out)

- A cache of solids across edits, a suppressed feature, an edit of a box, a `Translate` or a `Subtract`.
- A panel of the features in the desktop.

## Acceptance criteria

- [ ] Each test of v0.175 passes with no change of the test.
- [ ] After each command of eight random sessions, each live body with no fault has the solid that a build from
      nothing gives, and each body with a fault has a chain that a build from nothing cannot give.
- [ ] An edit calls the backend only for the steps downstream of it, in the log order of the ends.
- [ ] Each injection of the regeneration fails a test.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: the suite of v0.175 with no change; a comparison with a build from nothing after each of 2,000 commands of
            eight sessions; a count of the calls of the backend; ten injections.
Judgement:  the order of the ends, and the form of the fault of a live extrude.
Weakest:    the build from nothing of the test uses the same backend and the same formulas as the engine, so it does not
            find a fault of `ExtrudeProfile` or of the backend.

## Progress

- 2026-11-03: opened with the four answers of the owner. The regeneration, the two edits, two tests and the records.
