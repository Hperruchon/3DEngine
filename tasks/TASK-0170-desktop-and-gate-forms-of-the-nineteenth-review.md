---
id: 0170
title: The mark of a calibration, the cost of the snap, and the next forms of three gates
status: Ready
phase: P0.73
opened: 2026-11-03
depends-on: [0168]
governed-by: [0007, 0009, 0019, 0022, 0023, 0025, 0029, 0034, 0041]
writes:
  create:
    - tasks/TASK-0170-desktop-and-gate-forms-of-the-nineteenth-review.md
  modify:
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine.Core/Sketching/SketchTool.cs
    - 3DEngine.Tests/SketchToolTests.cs
    - 3DEngine.ImageTests/ImageActionsTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Diagnostics/DiagnosticsScanner.cs
    - Engine.Tests/Diagnostics/DiagnosticsRegistryGateTests.cs
    - Engine.Tests/Governance/CodebaseReviewGateTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Api.Http/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
---

# TASK-0170 — The mark of a calibration, the cost of the snap, and the next forms of three gates

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-03 found V74 and V75 (low) in the desktop and T127 to T129 (low) in the gates. They come
after K0h, by `docs/templates.md`, section 7.

## Goal

The mark of the snap shows only where a tap goes, the snap of a large sketch fits in a frame, and each gate catches the
next form that the review found.

## Scope (in)

1. V74: no mark while a calibration waits for its taps.
2. V75: the snap to an intersection skips a pair whose box in pixels is far from the tap, with a limit of the time or of
   the allocation for 200 lines.
3. T127: the pin rules refuse an `Output` whose item name is `PackageReference`, `GlobalPackageReference` or computed.
4. T128: the scanner follows `.ToString()`, an array and `new string(...)`.
5. T129: the rule of a setext heading takes a capital V and a space before the digits.

## Scope (out)

- The findings of the engine (P0.72).

## Acceptance criteria

- [ ] Each finding has a test or an injection that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: each fix and its test or injection, with a control.
Judgement:  the limit of the test of V75.
Weakest:    the time of V75 depends on the runner; a limit of the allocation is more stable than a limit of the time.

## Progress

- 2026-11-03: opened by the codebase review of 2026-11-03.
