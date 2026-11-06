---
id: 0251
title: A failed delete, an edge of three faces, the ledger, two gates and the smoke test hold their next forms
status: Done
phase: P0.101
opened: 2026-11-06
depends-on: [0250]
governed-by: [0004, 0007, 0008, 0013, 0015, 0016, 0018, 0019, 0028, 0029, 0033, 0037, 0051, 0058, 0060, 0072, 0074, 0075, 0076, 0077, 0081]
writes:
  create:
    - tasks/TASK-0251-engine-and-pipeline-forms-of-the-twenty-seventh-review.md
    - eng/android/check-platforms.js
    - eng/android/native-crash.ere
    - Engine.Tests/Governance/NativeCrashPatternTests.cs
  modify:
    - Engine.Core/Persistence/DocumentJournal.cs
    - Engine.Core/DocumentSession.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Core/Queries/GetFacesQueryHandler.cs
    - Engine.Contracts/Sketch/DatumRecord.cs
    - 3DEngine.App/NativeThreeDEngine.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - Engine.Tests/Features/GetFacesTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - eng/android/smoke-emulator.sh
    - .github/workflows/ci.yml
    - docs/adr/0015-command-log-persistence.md
    - docs/adr/0076-the-cylinder.md
    - docs/adr/0077-a-tap-near-an-edge-selects-the-edge.md
    - docs/diagnostics.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
---

# TASK-0251 — A failed delete, an edge of three faces, the ledger, two gates and the smoke test hold their next forms

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-06 (e) found E164 to E166 and T160 to T164 (low). By `docs/templates.md`, section 7, they
come after the next product phase.

## Goal

A save reports an old file that it cannot delete, the edges and the outlines of a face agree, the ledger counts each
failed run, the gates read the forms that MSBuild evaluates, and the smoke test matches the lines that Android writes.

## Scope (in)

1. E164: a delete of the old file that fails is reported.
2. E165: an edge of more than two faces.
3. E166: a register entry or a task for the comment of `DatumRecord.cs`.
4. T160: a correction of the counts in a ledger entry.
5. T161 and T162: a wildcard in an Update, a condition and a NoWarn of CA1416.
6. T163: the patterns of a native crash.
7. T164: the affects fields of ADR-0076 and ADR-0077.

## Scope (out)

- V112 (TASK-0250) and V113 to V118 (TASK-0252).

## Acceptance criteria

- [x] Each finding has a test or an injection that fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Decisions

The standing order of 2026-11-05 gives the recommended option for each question.

- **E164.** A delete of the old file that fails gives the warning `W-IO-OLD-FILE-KEPT` to the caller, and the save
  succeeds. `SaveAs` takes a list of warnings in a second form, so that each caller of the first form does not change.
  The desktop gives the warning on the status line. The response of the HTTP surface does not change, because a new
  field there is a change of the wire shape: that is a question for the owner on Sunday.
- **E165.** The first option of the review: an edge of more than two faces belongs to each pair of different names of its
  faces, and an edge of one face is no edge (ADR-0077 item 6).
- **E166.** The comment is a false statement about the repository, so this task corrects it (`CLAUDE.md`, "Stop and
  ask"). It is the one line of `Engine.Contracts` in the write set, and no public shape changes.
- **T161 and T162.** The rules of the text refuse a wildcard, a Condition and CA1416 in the forms of the review. The
  pipeline step `eng/android/check-platforms.js` asks MSBuild for the evaluated forms of each project that the phone
  reaches; the comparison of each evaluated version of a pin waits for a later review.

## Outcome

Status: Done · commit (the merge of the branch p0101-engine-forms) · v0.237

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  the four decisions above; the forms of the lines of a tombstone come from the format of debuggerd, not from a
            crash on a device.
Weakest:    no device wrote a tombstone for the test of the patterns; the version of a pin is not compared with the
            version that MSBuild evaluates; an edge of more than two faces gives pairs that bound no common solid.

## Progress

- 2026-11-06: opened by the codebase review of 2026-11-06 (e).
- 2026-11-06: run 42306849941 passed, with the new step of the platforms in the job of the APK. Closed at v0.237.
