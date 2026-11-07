---
id: 0263
title: A failed copy, a test of the faces, the STL reader, the command line, two gates and the affects fields hold their next forms
status: Ready
phase: P0.106
opened: 2026-11-07
depends-on: [0262]
governed-by: [0002, 0004, 0015, 0016, 0018, 0019, 0083]
writes:
  create:
    - tasks/TASK-0263-engine-and-gate-forms-of-the-twenty-eighth-review.md
  modify:
    - Engine.Core/Persistence/DocumentJournal.cs
    - Engine.Core/Import/StlReader.cs
    - Engine.Cli/Cli.cs
    - Engine.Tests/Features/GetFacesTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0263 — A failed copy, a test of the faces, the STL reader, the command line, two gates and the affects fields hold their next forms

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-07 found E168 to E171, T165 and T167 (low) and T166 (medium). By `docs/templates.md`,
section 7, they come after the next product phase.

## Goal

A failed copy reports a file that stays, the test of an edge of four faces checks each pair, the reader gives the code of
ADR-0083 item 3 for each form, the command line takes a folder of the store, each silence of CA1416 fails a gate, a
warning fails the build of each project of the phone, and the affects fields name their files.

## Scope (in)

1. E168 to E171.
2. T165 to T167.

## Scope (out)

- V119 to V123 (TASK-0264).

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

- 2026-11-07: opened by the codebase review of 2026-11-07.
