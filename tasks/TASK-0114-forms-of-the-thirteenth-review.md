---
id: 0114
title: The sketch keeps read-only lists, the pads follow their values, and the gates read the forms of the thirteenth review
status: Ready
phase: P0.45
opened: 2026-10-30
depends-on: [0112]
governed-by: [0004, 0007, 0012, 0016, 0019, 0021, 0025, 0027, 0028, 0029, 0030, 0031, 0032, 0033, 0034, 0035, 0037]
writes:
  create:
    - tasks/TASK-0114-forms-of-the-thirteenth-review.md
  modify:
    - Engine.Core/Sketch/SketchResults.cs
    - Engine.Core/Commands/SubtractCommand.cs
    - 3DEngine/Ui/DesktopUi.cs
    - 3DEngine/SessionBodies.cs
    - 3DEngine/SketchOverlay.cs
    - Engine.Tests/Governance/UiLibraryGateTests.cs
    - Engine.Tests/Governance/ConstantStrings.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/CodebaseReviewGateTests.cs
    - Engine.Tests/Diagnostics/DiagnosticsScanner.cs
    - Engine.Tests/Diagnostics/DiagnosticsRegistryGateTests.cs
    - eng/repo-gates/PendingTasks.cs
    - eng/repo-gates/Program.cs
    - Engine.Tests/Sketch/SketchGapTests.cs
    - 3DEngine.ImageTests/SketchGapUiTests.cs
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Api.Http/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine.Vulkan/**
    - docs/adr/**
---

# TASK-0114 — The sketch keeps read-only lists, the pads follow their values, and the gates read the forms of the thirteenth review

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-30 found medium and low forms of earlier findings (E94 to E96, V46 to V50, T89 to
T95 of `docs/reviews/2026-10-30-codebase-review.md`). `docs/templates.md`, section 7, puts a medium or low finding
into a task after the next product phase, so this task comes after R9.

## Goal

The engine, the desktop and the gates close each form of the review, and a test holds each one.

## Scope (in)

1. E94: `SketchResults.Changed` makes the points, the lines and the constraints of a record read-only.
2. E95: each sketch command refuses an identifier that the log holds. E96: the comment of `SubtractCommand`.
3. V46 and V47: the key of a pad holds the values that it shows, and the host refuses a pad action whose key is
   not the current key. V48: one mesh for each body and frame. V49: a limit of the magnitude of a pixel. V50: the
   fault text fits the panel.
4. T89: extensions with no case. T90: one compilation for each project. T91: a string with the form of a code that
   is not a full code. T92: the separators and the marks of direction. T93: the exact form of a heading. T94 and
   T95: a check that leaves a task that is not complete fails, and a gate file names no `LongRunning` and no
   `new Thread`.

## Scope (out)

- E17 in the bus (R-0028). The write set gains a file when a fix needs it, with the reason in the ledger.

## Acceptance criteria

- [ ] Each form of the review fails a test or the gate program.
- [ ] An injection that restores each old rule fails a test.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-30: opened by the codebase review of 2026-10-30 (TASK-0111).
