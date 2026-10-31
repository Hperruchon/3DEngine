---
id: 0143
title: The gates read disabled text, the routes of the test host, each sink, and the next forms of the fifteenth review
status: Active
phase: P0.61
opened: 2026-10-31
depends-on: [0141]
governed-by: []
writes:
  create:
    - tasks/TASK-0143-gate-forms-of-the-sixteenth-review.md
  modify:
    - Engine.Tests/Governance/ConstantStrings.cs
    - Engine.Tests/Diagnostics/DiagnosticsScanner.cs
    - Engine.Tests/Diagnostics/DiagnosticsRegistryGateTests.cs
    - eng/repo-gates/DiscardedTasks.cs
    - Engine.Tests/Http/SchemaEndpointGateTests.cs
    - Engine.Tests/EventSinkContractTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/CodebaseReviewGateTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Core/**
    - Engine.Api.Http/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0143 — The gates read disabled text, the routes of the test host, each sink, and the next forms of the fifteenth review

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-31 (b) found T110 (medium) and T111 to T116 (low). It comes after R11b (TASK-0134), the next product phase, by `docs/templates.md`, section 7; it starts only when R11b is merged.

## Goal

Code that a condition `#if` hides, a route under `/schema` in any form, a sink of any type, and the next forms of
T106 to T109 fail a gate.

## Scope (in)

1. T110: the reader of constants, the scanner of codes and the check of the tasks refuse disabled text, as
   `DeterminismCallGateTests` does.
2. T111: the gate of the routes reads the `EndpointDataSource` of the test host, and compares with no regard to case.
3. T112: the gate of the sinks keeps each type that is not abstract and not an interface, in each assembly of the
   solution that the test project references.
4. T113: the control byte gate refuses E0100 to E01EF and 180B to 180F.
5. T114: an ordered list marker and a setext heading in a quote fail the counter of the review.
6. T115: an `ItemName` that holds `$(` or `@(` is refused, and the value is trimmed.
7. T116: the scanner takes the constant value of the left operand of a code.

## Scope (out)

- The findings of the engine (P0.60) and of the desktop (P0.62).

## Acceptance criteria

- [ ] Each gate fails on an injection of its new form before the change, and the control still fails after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-10-31: opened by the codebase review of 2026-10-31 (b).
- 2026-10-31: R11b merged; the task starts. The seven gates and 13 new test cases. The gate of the sinks first missed a
  struct sink in Engine.Cli, because the project builds engine.dll; it reads the AssemblyName of each project now. 13
  injections caught: 7 forms in real files or undone fixes, and 6 undone fixes against their new test cases.
- 2026-10-31: main merged into the branch (P0.60, v0.144); Engine.Tests counts 1006 + 6 + 13 = 1025.
