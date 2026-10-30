---
id: 0123
title: The engine, the surface and the gates close the forms of the fourteenth review
status: Active
phase: P0.49
opened: 2026-10-30
depends-on: [0122]
governed-by: [0004, 0006, 0012, 0015, 0016, 0019, 0020, 0021, 0028, 0031, 0033, 0035, 0036, 0037, 0039]
writes:
  create:
    - tasks/TASK-0123-forms-of-the-fourteenth-review.md
  modify:
    - Engine.Core/Commands/AddSketchArcCommandHandler.cs
    - Engine.Core/Commands/ExtrudeCommandHandler.cs
    - Engine.Core/Sketch/SketchResults.cs
    - Engine.Core/Persistence/DocumentFile.cs
    - Engine.Core/CommandBus.cs
    - Engine.Api.Http/Json/ApiJson.cs
    - Engine.Tests/Sketch/CircleArcTests.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - Engine.Tests/Sketch/SketchGapTests.cs
    - Engine.Tests/Http/CommandsEndpointTests.cs
    - Engine.Tests/Governance/CodebaseReviewGateTests.cs
    - Engine.Tests/Governance/ConstantStrings.cs
    - Engine.Tests/Governance/UiLibraryGateTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Diagnostics/DiagnosticsScanner.cs
    - Engine.Tests/Diagnostics/DiagnosticsRegistryGateTests.cs
    - eng/repo-gates/DiscardedTasks.cs
    - eng/test-floor.txt
    - docs/register.md
    - docs/diagnostics.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0123 — The engine, the surface and the gates close the forms of the fourteenth review

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-30 (b) found medium and low findings in the engine, the HTTP surface and the gates
(E98 to E101, T96 to T102). `docs/templates.md`, section 7, puts them into a task after the next product phase, so
this task comes after K2e. E99 and E100 wait for the answer to Q1 of the review (register entry R-0046).

## Goal

The engine, the surface and the gates close each form of the review, and a test holds each one.

## Scope (in)

1. E98: `AddSketchArc` refuses an arc of radius zero.
2. E99 and E100, as Q1 decides: an open of a file in which a client sent a command again, and the guard of
   `Extrude`.
3. E101: the surface refuses a repeated member of an envelope and of a parameter.
4. T96: the exact form of each ledger heading. T97 and T98: each referenced project as its own compilation, and the
   references that MSBuild reads. T99: a code in an interpolated string or built by a call. T100: the near forms of
   T94 and T95, and the helper files. T101: a `Compile` item that is not `.cs`. T102: a lone CR and the C1 controls.

## Scope (out)

- E17 in the bus (R-0028). The write set gains a file when a fix needs it, with the reason in the ledger.
- E99 and E100 when the owner did not answer Q1. They then stay open with R-0046.

## Acceptance criteria

- [ ] Each form of the review fails a test or the gate program.
- [ ] An injection that restores each old rule fails a test.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-30: opened by the codebase review of 2026-10-30 (b) (TASK-0120).
- 2026-10-31: E98, E101 and T96 to T102 with 30 new test cases and five new forms of the self check of the runner;
  17 injections caught. E99 and E100 wait for Q1 (R-0046).
- 2026-10-30: the owner answered Q1 A. The guard of a repeated identifier acts on a live command only: the bus sets
  `SketchResults.ApplyingEntry` for an open and a replay. `Extrude` takes the guard (E99). The write set gains
  `Engine.Core/CommandBus.cs`, `Engine.Tests/Sketch/SketchGapTests.cs` and `docs/diagnostics.md`. The line of
  2026-10-31 above has a wrong date: the work was on 2026-10-30.
