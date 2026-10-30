---
id: 0107
title: The gates read the target name, the root folders and each text file, and a host test waits for its file
status: Done
phase: P0.43
opened: 2026-10-29
depends-on: [0106]
governed-by: []
writes:
  create:
    - tasks/TASK-0107-gate-forms-of-the-twelfth-review.md
    - eng/repo-gates/PendingTasks.cs
    - Engine.Tests/Governance/ConstantStrings.cs
  modify:
    - Engine.Tests/Governance/AdrGateTests.cs
    - Engine.Tests/Governance/CodebaseReviewGateTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/DeterminismCallGateTests.cs
    - Engine.Tests/Governance/UiLibraryGateTests.cs
    - Engine.Tests/Diagnostics/DiagnosticsRegistryGateTests.cs
    - Engine.Tests/Diagnostics/DiagnosticsScanner.cs
    - Engine.Tests/Http/HostGuardTests.cs
    - eng/repo-gates/DiscardedTasks.cs
    - eng/repo-gates/Program.cs
    - eng/repo-gates/RepoGates.csproj
    - eng/write-set-check/Judge.cs
    - docs/templates.md
    - eng/test-floor.txt
    - docs/register.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Cli/**
    - Engine.Api.Http/**
    - Engine.Api.Http.Server/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - docs/adr/**
---

# TASK-0107 — The gates read the target name, the root folders and each text file, and a host test waits for its file

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-29 found medium and low forms of earlier gate findings, and a red run of `main`
(findings T80 to T87 of `docs/reviews/2026-10-29-codebase-review.md`). `docs/templates.md`, section 7, puts a
medium or low finding into a task after the next product phase, so this task comes after K0b.

## Goal

Each gate reads what the build binds in the forms of T80 to T86, and the test of T87 passes on each runner.

## Scope (in)

1. T80: the dependency gate refuses `TargetName`, or compares the name of each built assembly with its project.
2. T81: the diagnostics scanner and the check of the runtime identifier use the rule of the root of a project, and
   the scanner reads the value of a constant expression.
3. T82: the UI gate reads the constant value of each argument of a native import.
4. T83: the gate runner waits for each task that a check starts.
5. T84: the review counter counts a heading that names a product phase, and the row and the task file agree.
6. T85: an archived ADR holds a paragraph under each heading.
7. T86: the control byte gate reads each tracked text file and refuses the bidirectional controls.
8. T87: `HostGuardTests` waits until the file of the killed host opens.

## Scope (out)

- T79: TASK-0105.
- The three gates that run engine code stay in the test process (T31, register entry R-0031).

## Acceptance criteria

- [x] Each form of the review fails the gate program, or fails a test for T87.
- [x] An injection that restores each old rule fails a test.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p043-gate-forms) · v0.111

## Method

Mechanical: the refusal of TargetName, the rule of the project root in two more gates, the paragraph under each
            heading of an archived ADR, the phase of a ledger heading, the scan of each tracked text file, and the
            read of the host file that tries for 10 s.
Judgement:  T81 and T82 use one compilation of the files with the assemblies of the runtime, and take the constant
            value of each expression from the compiler, so the gates add no form of syntax. T83 makes the runner wait
            until the thread pool and the timers of a check are idle, and collect each task that nobody holds, so the
            runtime reports a failed task that nobody read. No form of syntax is added to DiscardedTasks.
Weakest:    T83 does not see a failed task that a static field holds, or a task that the check leaves on a thread
            that is not of the pool. The constant scans cost about 5 s in each run. A Directory.Build.rsp file can
            set TargetName for each project, and no gate reads it (R-0031).

## Progress

- 2026-10-29: opened by the codebase review of 2026-10-29 (TASK-0104).
- 2026-10-30: the gates read the target name, the root folders, the constant values and each tracked text file, the
  runner waits for each task of a check, and the host test waits for its file (T80 to T87). 14 injections caught.
- 2026-10-30: closed at v0.111.
