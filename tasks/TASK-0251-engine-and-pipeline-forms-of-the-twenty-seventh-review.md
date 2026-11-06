---
id: 0251
title: A failed delete, an edge of three faces, the ledger, two gates and the smoke test hold their next forms
status: Ready
phase: P0.101
opened: 2026-11-06
depends-on: [0250]
governed-by: [0004, 0015, 0016, 0018, 0028, 0033, 0037, 0058, 0060, 0074, 0075, 0077]
writes:
  create:
    - tasks/TASK-0251-engine-and-pipeline-forms-of-the-twenty-seventh-review.md
  modify:
    - Engine.Core/Persistence/DocumentJournal.cs
    - Engine.Core/Queries/GetFacesQueryHandler.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - eng/android/smoke-emulator.sh
    - docs/adr/0076-the-cylinder.md
    - docs/adr/0077-a-tap-near-an-edge-selects-the-edge.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
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

- 2026-11-06: opened by the codebase review of 2026-11-06 (e).
