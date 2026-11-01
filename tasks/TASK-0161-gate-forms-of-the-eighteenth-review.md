---
id: 0161
title: The comparison refuses an empty hash, the pin rules read a global reference, and the next forms of the seventeenth review fail
status: Active
phase: P0.70
opened: 2026-11-01
depends-on: [0158]
governed-by: []
writes:
  create:
    - tasks/TASK-0161-gate-forms-of-the-eighteenth-review.md
  modify:
    - eng/compare-determinism.js
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/CodebaseReviewGateTests.cs
    - Engine.Tests/Diagnostics/DiagnosticsScanner.cs
    - Engine.Tests/Diagnostics/DiagnosticsRegistryGateTests.cs
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

# TASK-0161 — The comparison refuses an empty hash, the pin rules read a global reference, and the next forms of the seventeenth review fail

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-01 found T123 to T126 (low). It comes after the next product phase, which Q1 of the review asks the owner for, by `docs/templates.md`, section 7.

## Goal

An empty hash, a global reference of a pinned package, and the next forms of T120 and T122 each fail a gate, and a comparison with a constant character is not a part of a code.

## Scope (in)

1. T123: `eng/compare-determinism.js` refuses a first line that is not 64 hexadecimal digits, and names the runner.
2. T124: the pin rules of the vision library and of the Vulkan layer read `GlobalPackageReference`, or refuse it.
3. T125: the counter of the review reads a paragraph back to its first line, and refuses a letter that is not ASCII before the digits of a version.
4. T126: the scanner counts a constant character only as an operand of a concatenation, an interpolation or a call that joins text.

## Scope (out)

- The findings of the engine (P0.69) and of the desktop (P0.71).

## Acceptance criteria

- [ ] Each gate fails on an injection of its new form before the change, and the control still fails after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

- 2026-11-01: T123: the comparison refuses a first line that is not 64 lower-case hexadecimal digits, and names the
  runner; three empty files gave exit 0 before and exit 1 after, and the controls did not change. T124: the reader of
  the pin rules takes `GlobalPackageReference` too; an injected `Engine.Core/Directory.Packages.props` passed the old
  gate and fails the new one, and the control `PackageReference` fails it. T125: the rule of a setext heading reads
  the paragraph back to its first line, and a letter that is not ASCII counts as the v. T126: a constant char of a
  severity is a part where it joins text only, and `char.ToUpperInvariant('e')` and `"EWI"[0]` count there. 6 new
  test cases; each new test failed on the old gate.
