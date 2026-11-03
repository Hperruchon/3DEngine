---
id: 0173
title: The ordered feature list is a fold of the log that the engine reads, and GetFeatures gives each feature with its inputs and its state
status: Done
phase: P8d1
opened: 2026-11-03
depends-on: [0172]
governed-by: [0004, 0012, 0016, 0018, 0021, 0028, 0031, 0033, 0035, 0037, 0039, 0045, 0049, 0051, 0052, 0053, 0054]
writes:
  create:
    - tasks/TASK-0173-ordered-feature-list.md
    - docs/adr/0054-ordered-feature-list.md
    - Engine.Core/Features/FeatureList.cs
    - Engine.Core/Queries/GetFeaturesQuery.cs
    - Engine.Core/Queries/GetFeaturesQueryHandler.cs
    - Engine.Tests/Features/FeatureListTests.cs
  modify:
    - Engine.Core/Sketch/FeatureGraph.cs
    - Engine.Core/Sketch/SketchRebuild.cs
    - Engine.Core/Sketch/DatumResolver.cs
    - Engine.Core/Sketch/FaceFinder.cs
    - Engine.Core/Sketch/SketchResults.cs
    - Engine.Core/Commands/AddSketchReferenceCommandHandler.cs
    - Engine.Core/Commands/CreateDatumCommandHandler.cs
    - Engine.Core/Commands/SetExtrudeDistanceCommandHandler.cs
    - Engine.Core/Commands/ImageResults.cs
    - Engine.Core/Queries/GetBodyInfoQueryHandler.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - docs/adr/0039-chain-of-a-consumed-extrude.md
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

# TASK-0173 — The ordered feature list is a fold of the log that the engine reads, and GetFeatures gives each feature with its inputs and its state

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Phase P8d gives the three layers of a document: the log, an ordered list of features, and a cache of the
regeneration. The owner gave the order "fix R-0050, then start P8d" on 2026-11-03 and answered four questions on the
same day, each with the recommended answer: the feature list first (P8d1), the creators as rows, four states with no
suppress, and a fold of the log. ADR-0054 holds them.

## Goal

The engine reads the steps in effect from one list that it applies each entry of the log to one time, and an agent
reads the features of a document in their order.

## Scope (in)

1. `FeatureList`: the fold of the log, with the steps in effect, the identifiers of the log, the creator, the consumer
   and the place of a handle, and the distance in effect of an extrude.
2. The engine readers of the log read the list; `FeatureGraph` is a view of it.
3. The query `GetFeatures` with the inputs, the consumer, the state and the fault of each feature.

## Scope (out)

- The cache of the regeneration and the place of a fault (P8d2).
- A suppressed feature, and a panel of the features in the desktop.

## Acceptance criteria

- [x] After each command of a log with undos and redos, the fold gives what a walk of the whole log gives.
- [x] `GetFeatures` gives each state, and each injection of the fold and of the query fails a test.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p8d1-feature-list) · v0.175

## Method

Mechanical: a comparison of the fold with the walk after each of 400 commands of a session and 600 entries of a
            written log; ten injections; a probe of the time of 10,000 commands before and after.
Judgement:  the inputs of each kind of feature, and the mapping of a fault to a state.
Weakest:    the list is state outside the Document; a code path that changes the log of a Document without an append
            would make it wrong, and no such path exists now.

## Progress

- 2026-11-03: opened with the four answers of the owner. The fold, the readers, the query and three tests.
- 2026-11-03: ten injections each fail a test; closed at v0.175.
