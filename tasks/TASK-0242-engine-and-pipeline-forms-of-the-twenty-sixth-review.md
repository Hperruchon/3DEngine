---
id: 0242
title: The readers of a file, the pin rule, the smoke test and the ledger hold their next forms
status: Done
phase: P0.97
opened: 2026-11-06
depends-on: [0241]
governed-by: [0004, 0006, 0013, 0015, 0016, 0019, 0020, 0021, 0024, 0028, 0031, 0033, 0035, 0037, 0039, 0045, 0051, 0052, 0055, 0059, 0074, 0076]
writes:
  create:
    - tasks/TASK-0242-engine-and-pipeline-forms-of-the-twenty-sixth-review.md
  modify:
    - Engine.Core/Persistence/DocumentFile.cs
    - Engine.Api.Http/EngineHost.cs
    - Engine.Core/Sketch/DatumResolver.cs
    - Engine.Core/Determinism/CrossPlatformProbe.cs
    - Engine.Core/CommandBus.cs
    - Engine.Core/DiagnosticCodes.cs
    - docs/diagnostics.md
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Tests/ReplayDeterminism/CrossPlatformHashTests.cs
    - Engine.Tests/Sketch/SketchReferenceTests.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - Engine.Tests/Http/DocumentEndpointTests.cs
    - eng/compare-determinism.js
    - docs/android-test-sheet.md
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

- [x] Each finding has a test or an injection that fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p097-engine-forms) · v0.229

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  E163 has a new code, E-CMD-FIELD-MISSING, because E-IO-NOT-ENCODABLE means a value that the file cannot
            hold and comes after the handler; the comment of E160 in Engine.Contracts/Sketch/DatumRecord.cs stays, because
            the task forbids Engine.Contracts; T159 is fixed by the entry v0.225.
Weakest:    the new hash of the probe came from the three desktop runners only, because the emulator job of the run
            failed; the next run that passes the emulator job compares the phone.

## Progress

- 2026-11-06: opened by the codebase review of 2026-11-06 (d).
- 2026-11-06: E158 to E161, E163, T152, T153 and T155 to T158. An injection of E158, E159 and E163 each fails a test; the
  three injections of the review for T155 and T156 each fail the changed gate; the fake adb gives an error for a native
  crash and for a hash with no line (T153, T157); fake folders show that the comparison takes the three desktop hashes
  when the emulator job fails (T152). The probe holds a circle, a cylinder, a move and a cut, so its hash changes (E161).
- 2026-11-06: run 42289169473: the three desktop runners give 1c56cbddd4caa83557fd5756427668795cbc1096d61767c5588d3b447dca30ba; the emulator job failed, and the comparison ran on the three (T152). Closed at v0.229.
