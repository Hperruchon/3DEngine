---
id: 0242
title: The readers of a file, the pin rule, the smoke test and the ledger hold their next forms
status: Ready
phase: P0.97
opened: 2026-11-06
depends-on: [0241]
governed-by: [0004, 0006, 0015, 0019, 0020, 0021, 0024, 0028, 0031, 0033, 0035, 0037, 0039, 0045, 0051, 0052, 0055, 0059, 0074, 0076]
writes:
  create:
    - tasks/TASK-0242-engine-and-pipeline-forms-of-the-twenty-sixth-review.md
  modify:
    - Engine.Core/Persistence/DocumentFile.cs
    - Engine.Api.Http/EngineHost.cs
    - Engine.Core/Sketch/DatumResolver.cs
    - Engine.Core/Determinism/CrossPlatformProbe.cs
    - Engine.Core/CommandBus.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - eng/android/smoke-emulator.sh
    - .github/workflows/ci.yml
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0242 — The readers of a file, the pin rule, the smoke test and the ledger hold their next forms

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-06 (d) found E163, T152 and T153 (medium) and E158 to E161 and T155 to T159 (low). By
`docs/templates.md`, section 7, they come after the next product phase. T154 and two calls of T158 are fixed at v0.224
(TASK-0239).

## Goal

Each reader of a document file gives a code for a path that is not valid, the host compares the unsaved folder with no
case on Windows, the reasons of a datum name each creator, the probe of the phone holds a curve, the bus refuses a null
required field, the pin rule reads an escaped value, a failure of the phone keeps the desktop comparison, and the smoke
test fails on a native crash and on a hash with no line.

## Scope (in)

1. E158: DocumentFile.Check, ReadKind and ReadLengthUnit with a path that is not valid.
2. E159: the unsaved folder in another case on Windows.
3. E160: the reasons of DatumResolver name CreateCylinder.
4. E161: the probe holds a circle and a cylinder (a change of the hash on each platform, in one commit).
5. E163: the bus refuses a missing required field before the handler.
6. T152: the desktop hashes compare when the emulator job fails.
7. T153 and T157: the smoke test fails on a native signal and on a hash with no line.
8. T155 and T156: the pin rule and the rule of CA1416 read an escaped value and a Remove.
9. T158: the four other calls of adb have a limit.
10. T159: a correction of the counts in a ledger entry.

## Scope (out)

- V104 to V111 (TASK-0243).

## Acceptance criteria

- [ ] Each finding has a test or an injection that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Ready

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  to be written in the task.
Weakest:    to be written at the close.

## Progress

- 2026-11-06: opened by the codebase review of 2026-11-06 (d).
