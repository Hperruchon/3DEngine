---
id: 0182
title: A datum on a diagonal subnormal side has unit axes, the walk gives its sketches in the order of the log, and the next forms of three gates fail
status: Done
phase: P0.79
opened: 2026-11-05
depends-on: [0181]
governed-by: [0004, 0033, 0035, 0037, 0039, 0051, 0052, 0055]
writes:
  create:
    - tasks/TASK-0182-engine-and-gate-forms-of-the-twenty-first-review.md
  modify:
    - Engine.Core/Sketch/DatumResolver.cs
    - Engine.Core/Sketch/SketchResults.cs
    - Engine.Tests/Sketch/DatumTests.cs
    - Engine.Tests/Sketch/SketchReferenceTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Diagnostics/DiagnosticsScanner.cs
    - Engine.Tests/Diagnostics/DiagnosticsRegistryGateTests.cs
    - Engine.Tests/Governance/CodebaseReviewGateTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0182 — A datum on a diagonal subnormal side has unit axes, the walk gives its sketches in the order of the log, and the next forms of three gates fail

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-05 found E131 to E133 (low) in the datums and in the walk of the planes, and T133 to
T135 (low) in three gates. By `docs/templates.md`, section 7, they come after the next product phase. On 2026-11-05 the
owner gave the order "Ok" to P0.78 and P0.79 before the next product phase, K4.

## Goal

A datum on each short side has unit axes, the walk gives its sketches in the order of the log, the guard of E129 has a
test or a register entry, and the next form of each gate fails.

## Scope (in)

1. E131: the scaled branch divides the scaled components, with a diagonal case.
2. E132: a test of `SketchResults.Changed` with a stale reference that the test builds, or a register entry.
3. E133: the order of the changed sketches, or the comment.
4. T133 to T135: the forms of the review, each with an injection before the change.

## Scope (out)

- V78 to V84 (TASK-0181).

## Acceptance criteria

- [x] Each finding has a test or an injection that fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p079-engine-gate-forms) · v0.184

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  the list of the item elements that the rule of T133 permits.
Weakest:    the guard of E129 takes the record of a solve with no hold, and no command reaches the guard.

## Progress

- 2026-11-05: opened by the codebase review of 2026-11-05.
- 2026-11-05: the six findings, each form before and after the change, four injections; closed at v0.184.
