---
id: 0116
title: An edit of a sketch rebuilds each translate and subtract that descends from its extrude
status: Done
phase: K0d
opened: 2026-10-30
depends-on: [0111]
governed-by: [0004, 0033, 0035, 0037, 0039]
writes:
  create:
    - tasks/TASK-0116-chain-of-a-consumed-extrude.md
    - docs/adr/0039-chain-of-a-consumed-extrude.md
    - Engine.Core/Sketch/FeatureGraph.cs
    - Engine.Tests/Sketch/ChainRebuildTests.cs
  modify:
    - Engine.Core/Sketch/SketchRebuild.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Tests/Sketch/RebuildTests.cs
    - docs/diagnostics.md
    - docs/adr/0037-rebuild-of-an-extrude.md
    - docs/adr/0021-operand-consumption.md
    - docs/adr/0031-undo-and-redo.md
    - docs/adr/README.md
    - tasks/TASK-0114-forms-of-the-thirteenth-review.md
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/CommandBus.cs
    - Engine.Api.Http/**
    - Engine.Cli/**
    - 3DEngine/**
    - 3DEngine.Vulkan/**
---

# TASK-0116 — An edit of a sketch rebuilds each translate and subtract that descends from its extrude

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

K0b (ADR-0037) rebuilds a live extrude after an edit of its sketch, and a consumed extrude gives
`W-SKETCH-EXTRUDE-CONSUMED`. The owner decided on 2026-10-30 (KQ1 to KQ4 A) that the first slice of K0 is the chain
of a consumed extrude, as a projection of the log, with a reference by body handle and command identifier. The
codebase review of 2026-10-30, sections 5 and 6, gives the research, and its questions Q1 to Q3 ask three more
decisions. The owner answered A to Q1, Q2 and Q3 on 2026-10-30 (R-0045 closes): the full upstream from the live
sketches, the wider meaning of `W-SKETCH-REBUILD-FAILED`, and `W-SKETCH-EXTRUDE-CONSUMED` for an unknown consumer.

## Goal

After an edit of a sketch, each `Translate` and `Subtract` in effect that descends from an extrude of the sketch
runs again in log order under new keys, and the live end keeps its handle and gets its new key or a fault.

## Scope (in)

1. ADR-0039, from KQ1 to KQ4 A and the answers to Q1 to Q3 of the review.
2. `FeatureGraph.Of(log)`, the rebuild of the chain, and its tests with an injection for each.

## Scope (out)

- A change of the public shape of `Engine.Contracts`. If the design needs one, the task stops and asks the owner.
- The desktop: `SessionBodies` already meshes a body again when its key changes.

## Acceptance criteria

- [x] Each case of the review, section 6, item 6, has a test, and an injection fails each one.
- [x] A replay and an open give the same keys and solids on the three runners.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch k0d-chain-rebuild) · v0.116

## Method

Mechanical: the graph of the steps in effect, the walk from each seed to its end, and the records.
Judgement:  the evaluation of an end runs its upstream steps from the live sketches (Q1 A), so an operand of
            another sketch follows its own sketch; each handle has one consumer, so the upstream of an end is a tree
            and each step runs one time with no memo.
Weakest:    each edit runs each step upstream of each end on the backend again, and each old solid stays in the
            backend. A step that the graph does not know gives a fault, and none exists today.

## Progress

- 2026-10-30: opened by the codebase review of 2026-10-30 (TASK-0111). Deferred until the answers to Q1 to Q3.
- 2026-10-30: the owner answered A to Q1 to Q3. ADR-0039 (Accepted under KQ4 A), `FeatureGraph`, the chain in
  `SketchRebuild`, `ChainRebuildTests` and the flipped test of a consumed extrude in `RebuildTests`.
- 2026-10-30: closed at v0.116.
