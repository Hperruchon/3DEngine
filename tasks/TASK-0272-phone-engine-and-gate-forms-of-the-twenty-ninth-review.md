---
id: 0272
title: The phone, the reader, the APK check, the gates and the roadmap hold their next forms
status: Ready
phase: P0.112
opened: 2026-11-07
depends-on: [0271]
governed-by: [0004, 0017, 0023, 0061, 0073, 0079, 0083]
writes:
  create:
    - tasks/TASK-0272-phone-engine-and-gate-forms-of-the-twenty-ninth-review.md
  modify:
    - 3DEngine.Android/PhoneHost.cs
    - 3DEngine.Android/3DEngine.Android.csproj
    - 3DEngine.Vulkan/Window.FileDialog.cs
    - Engine.Core/Import/StlReader.cs
    - Engine.Tests/Import/StlImportTests.cs
    - eng/android/check-apk.sh
    - eng/android/check-platforms.js
    - .github/workflows/ci.yml
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Governance/AffectsCitationGateTests.cs
    - docs/templates.md
    - docs/register.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0272 — The phone, the reader, the APK check, the gates and the roadmap hold their next forms

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-07 (b) found E175 and T168 (medium) and E176 to E179 and T169 to T173 (low). By
`docs/templates.md`, section 7, they come after the next product phase.

## Goal

A lost surface keeps a chosen STL, a stale session frees its host, the APK check fails when it cannot read, the reader
gives the codes of ADR-0083 item 3, the dialog of the phone starts free, the host of the phone treats CA1416 as an error,
the gates read the next forms, the two questions of TASK-0263 are in the register, and the roadmap lists its versions in
order.

## Scope (in)

1. E175 to E179.
2. T168 to T171 and T173. T172 is corrected in the entry of the review.

## Scope (out)

- The desktop findings (TASK-0271).

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

- 2026-11-07: opened by the codebase review of 2026-11-07 (b).
