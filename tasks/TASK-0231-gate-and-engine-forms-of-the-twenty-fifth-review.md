---
id: 0231
title: The review gate counts each product phase, and the export, the host and three checks hold their next forms
status: Ready
phase: P0.91
opened: 2026-11-06
depends-on: [0230]
governed-by: [0002, 0015, 0016, 0018, 0019, 0024]
writes:
  create:
    - tasks/TASK-0231-gate-and-engine-forms-of-the-twenty-fifth-review.md
  modify:
    - Engine.Tests/Governance/CodebaseReviewGateTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Cli/Cli.cs
    - Engine.Api.Http/EngineHost.cs
    - Engine.Tests/Assembly/ContainerModelTests.cs
    - eng/android/check-apk.sh
    - eng/android/smoke-emulator.sh
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0231 — The review gate counts each product phase, and the export, the host and three checks hold their next forms

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-06 (c) found T147 and T149 (medium) and E156, E157, T148, T150 and T151 (low). By
`docs/templates.md`, section 7, they come after the next product phase, R14f3.

## Goal

The review gate counts each product phase, the export and the host hold the next forms of E155 and V91, the pin rule
reads the file of an Update, and the two checks of the pipeline fail where they must.

## Scope (in)

1. T147: the expression of a product phase reads a letter and then a digit.
2. E156: the read of the kind inside the handler of the export.
3. E157: the unsaved folder with or without a final separator.
4. T148: an Update of a pinned package outside the pinning file.
5. T149: the smoke test fails on the line of an ended session.
6. T150: each library of the APK has the checksum of its own row.
7. T151: the correction of the counts in a ledger entry.

## Scope (out)

- V97 to V103 (TASK-0232).

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

- 2026-11-06: opened by the codebase review of 2026-11-06 (c).
