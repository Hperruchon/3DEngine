---
id: 0150
title: The pin rules read each imported file, the sink gate reads the desktop, and the next forms of the sixteenth review fail
status: Active
phase: P0.64
opened: 2026-10-31
depends-on: [0148]
governed-by: [0025]
writes:
  create:
    - tasks/TASK-0150-gate-forms-of-the-seventeenth-review.md
    - 3DEngine.ImageTests/DesktopSinkContractTests.cs
  modify:
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/EventSinkContractTests.cs
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
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0150 — The pin rules read each imported file, the sink gate reads the desktop, and the next forms of the sixteenth review fail

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-31 (c) found T117 (medium) and T118 to T122 (low). It comes after V2, the next product phase, by `docs/templates.md`, section 7; it starts only when V2 is merged.

## Goal

A pin through an imported file, a sink in any host, and the next forms of T113 to T116 fail a gate.

## Scope (in)

1. T117: the pin rules of the vision library and of the Vulkan layer read each file that MSBuild reads.
2. T118: the gate of the sinks fails when a project of the solution that is not a test has no assembly in its folder, or it reads `3DEngine` in another way.
3. T119: the check of T115 decodes each `%XX` of an item name.
4. T120: the scanner treats each constant `char` E, W or I as a part of a severity.
5. T121: the control byte gate refuses the Unicode set `Default_Ignorable_Code_Point`, with U+034F and U+2800 as exceptions.
6. T122: a line of `=` or `-` only after a line that holds a version fails the counter of the review, at any indent.

## Scope (out)

- The findings of the engine (P0.63) and of the desktop (P0.65).

## Acceptance criteria

- [ ] Each gate fails on an injection of its new form before the change, and the control still fails after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress
- 2026-10-31: T117 to T122. Each new form was a test case that the old gate passed; T117 and T118 were also
  injected in real files: an imported .props file with OpenCvSharp4 in Engine.Core and with Vortice.Vulkan in
  3DEngine.Core, a throwing sink in 3DEngine, and a list with no entry for 3DEngine. The set of T121 first named
  U+FEFF, and the gate then refused the byte order mark of a file; the rule of the category Format keeps its exception.
  12 new test cases in Engine.Tests and 1 in 3DEngine.ImageTests.
