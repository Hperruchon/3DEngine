---
id: 0224
title: A void on the plane of a distant face is written, a replay and an export know an assembly, and two gates read the transitive form
status: Ready
phase: P0.89
opened: 2026-11-06
depends-on: [0226]
governed-by: [0002, 0004, 0015, 0016, 0018, 0019, 0020, 0048, 0070]
writes:
  create:
    - tasks/TASK-0224-engine-and-gate-forms-of-the-twenty-fourth-review.md
  modify:
    - Engine.Core/Export/StepExport.cs
    - Engine.Core/Replay.cs
    - Engine.Core/Persistence/EntryCodec.cs
    - Engine.Core/Persistence/DocumentFile.cs
    - Engine.Cli/Cli.cs
    - Engine.Tests/Export/StepExportTests.cs
    - Engine.Tests/Assembly/ContainerModelTests.cs
    - Engine.Tests/Export/StlExportTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - docs/adr/0070-step-export-of-the-owned-kernel.md
    - docs/adr/0071-container-model.md
    - docs/diagnostics.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0224 — A void on the plane of a distant face is written, a replay and an export know an assembly, and two gates read the transitive form

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-06 (b) found E152 and T145 (medium) and E153 to E155 and T146 (low). By `docs/templates.md`, section 7,
they come after the next product phase, R14e2. The standing order of 2026-11-05 answers Q1 of the review with A. The
first commit of this task corrects the write set and the field `governed-by`.

## Goal

A void whose first vertex lies on the plane of a distant face is written, a replay and an export of the command line
know an assembly, an older engine refuses an assembly, and two gates read the transitive form of their rule.

## Scope (in)

1. E152: a point on the plane of a face outside that face does not stop the ray test.
2. E153: `Replay.ReplayLog` takes the kind of the Document.
3. E154 (Q1 A): the header of an assembly has `formatVersion` 2.
4. E155: `engine export` reads the kind of the header and gives a clear refusal for an assembly.
5. T145: the rule of ADR-0072 reads the SDK and the framework references of each project that the application reaches.
6. T146: each project of the desktop has a row in the list of the packages.

## Scope (out)

- V91 to V96 (TASK-0225).

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

- 2026-11-06: opened by the codebase review of 2026-11-06 (b).
