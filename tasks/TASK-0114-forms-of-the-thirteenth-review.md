---
id: 0114
title: The sketch keeps read-only lists and refuses each identifier that the log holds, and the gates read the forms of the thirteenth review
status: Done
phase: P0.45
opened: 2026-10-30
depends-on: [0112]
governed-by: [0004, 0007, 0012, 0016, 0019, 0021, 0025, 0027, 0028, 0029, 0030, 0031, 0032, 0033, 0034, 0035, 0037, 0039]
writes:
  create:
    - tasks/TASK-0114-forms-of-the-thirteenth-review.md
    - tasks/TASK-0118-desktop-forms-of-the-thirteenth-review.md
  modify:
    - Engine.Core/Sketch/SketchResults.cs
    - Engine.Core/Commands/SubtractCommand.cs
    - Engine.Core/Commands/MoveSketchPointCommandHandler.cs
    - Engine.Core/Commands/SetSketchConstraintValueCommandHandler.cs
    - Engine.Core/Commands/DeleteSketchItemCommandHandler.cs
    - eng/repo-gates/DiscardedTasks.cs
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

# TASK-0114 — The sketch keeps read-only lists and refuses each identifier that the log holds, and the gates read the forms of the thirteenth review

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
4. T89: extensions with no case. T90: one compilation for each project. T91: a string with the form of a code that
   is not a full code. T92: the separators and the marks of direction. T93: the exact form of a heading. T94 and
   T95: a check that leaves a task that is not complete fails, and a gate file names no `LongRunning` and no
   `new Thread`.
3. The desktop items V46 to V50, and R-0043 from TASK-0115, move to TASK-0118 (P0.47): R9 waits for the visual check
   of the owner on its branch and changes `3DEngine/NativeThreeDEngine.cs`, so this task changes no file of the
   desktop and merges first.

## Scope (out)

- E17 in the bus (R-0028). The write set gains a file when a fix needs it, with the reason in the ledger.

## Acceptance criteria

- [x] Each form of the review fails a test or the gate program.
- [x] An injection that restores each old rule fails a test.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p045-review-forms) · v0.118

## Method

Mechanical: the read-only lists, the guards of a used identifier, the comment of Subtract, the predicate of a
            source, the separators, the form of a heading, and the three refusals of the runner.
Judgement:  T90 binds each file in the compilation of its own project with the projects that it references, so the
            internal types of two projects never meet. T94 and T95 refuse a thread, a task of its own thread and a
            source of a task in a gate file, because no gate needs them and the runner cannot wait for their work.
Weakest:    a task that waits for another kind of signal that nobody gives, for example a semaphore, still passes the
            runner; R-0031 records it. The desktop items wait in TASK-0118.

## Progress

- 2026-10-30: opened by the codebase review of 2026-10-30 (TASK-0111).
- 2026-10-30: TASK-0115 gave this task R-0043 (item 5), because R-0043 needs the desktop after the merge of R9.
- 2026-10-30: E94 to E96 and T89 to T95, with their tests; 11 injections caught. The desktop items move to TASK-0118.
- 2026-10-30: closed at v0.118.
