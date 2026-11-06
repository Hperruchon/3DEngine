---
id: 0198
title: The window keeps no input between sessions, the hook has the cases of one axis, and the next forms of four gates fail
status: Ready
phase: P0.86
opened: 2026-11-06
depends-on: [0197]
governed-by: [0007, 0019, 0022, 0029, 0030, 0061]
writes:
  create:
    - tasks/TASK-0198-desktop-and-gate-forms-of-the-twenty-third-review.md
  modify:
    - 3DEngine/DesktopEngineHost.cs
    - 3DEngine.Tests/SdlInputTests.cs
    - Engine.Tests/Governance/BuiltAssemblyReferenceTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Diagnostics/DiagnosticsScanner.cs
    - Engine.Tests/Diagnostics/DiagnosticsRegistryGateTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0198 — The window keeps no input between sessions, the hook has the cases of one axis, and the next forms of four gates fail

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-06 found V88 to V90 and T141 to T144 (low). By `docs/templates.md`, section 7, they come
after the next product phase, and after P0.85. The standing order of 2026-11-05 answers Q4 of the review with A. V88
goes into R14e with the fact of the runtime identifiers of the Android project. The first commit of this task corrects
the write set and the field `governed-by`.

## Goal

The first frame of a later session gets no input of the time between sessions, the theory of the hook has the cases of
one axis, and the next form of each of four gates fails.

## Scope (in)

1. V89 and V90: two cases of the theory, and a flush of the input events at the start of each later session.
2. T141 and T142: the assembly references of the render kernel and of the Vulkan layer, and only the framework and the
   reached first-party assemblies for the path of the log.
3. T143: a diagnostic code as a type (Q4 A), or the forms that the scanner misses in a register entry.
4. T144: the pin rule reads `Update`, and the host names only the packages of a list.

## Scope (out)

- E142 to E151 (TASK-0197). The Android project of V88 (R14e).

## Acceptance criteria

- [ ] Each finding has a test or an injection that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  the form of a diagnostic code as a type.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: opened by the codebase review of 2026-11-06.
