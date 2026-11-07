---
id: 0263
title: A failed copy, a test of the faces, the STL reader, the command line, two gates and the affects fields hold their next forms
status: Done
phase: P0.106
opened: 2026-11-07
depends-on: [0262]
governed-by: [0002, 0004, 0007, 0009, 0015, 0016, 0017, 0018, 0019, 0023, 0027, 0029, 0061, 0072, 0083]
writes:
  create:
    - tasks/TASK-0263-engine-and-gate-forms-of-the-twenty-eighth-review.md
    - Engine.Tests/Governance/AffectsCitationGateTests.cs
  modify:
    - Engine.Core/Persistence/DocumentJournal.cs
    - Engine.Core/Import/StlReader.cs
    - Engine.Cli/Cli.cs
    - Engine.Cli/Usage.cs
    - Engine.Tests/Features/GetFacesTests.cs
    - Engine.Tests/Import/StlImportTests.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - Engine.Tests/Export/StlExportTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - 3DEngine.App/3DEngine.App.csproj
    - 3DEngine.Core/3DEngine.Core.csproj
    - 3DEngine.Vulkan/3DEngine.Vulkan.csproj
    - eng/android/check-platforms.js
    - docs/adr/0015-command-log-persistence.md
    - docs/adr/0072-the-drawing-code-is-a-library.md
    - docs/adr/0082-the-values-on-the-edges-of-the-redesign.md
    - docs/adr/0083-the-mesh-body-and-the-stl-import.md
    - docs/diagnostics.md
    - docs/templates.md
    - tasks/TASK-0264-desktop-forms-of-the-twenty-eighth-review.md
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

- [x] Each finding has a test or an injection that fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p0106-review-forms) · v0.250

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  E170: a file of 84 bytes or more that starts with "solid" and is not UTF-8 text with no zero byte is a
            binary file of the wrong length; a shorter file keeps E-STL-SYNTAX. The name of the solid is the rest of
            its line; a file of one line keeps the old rule. E171: --folder for apply, query and export, as asset has
            it. T165: the step builds a probe in each project and requires the error CA1416, so a silence of any
            form that holds the whole project fails it; the gate refuses a pragma with no code and a category
            severity, which a probe in its own file does not see. T167: a code file that cites an ADR from 0082 on
            must match its affects field; the earlier ADRs have 379 such citations, many in passing.
Weakest:    the platform step finds a silence that holds a whole project; a file-local silence other than a pragma
            with no code (an attribute SuppressMessage over two lines) still escapes both rules. 3DEngine.Android
            itself does not set TreatWarningsAsErrors. The affects rule holds ADR-0082 and later only.

## Progress

- 2026-11-07: opened by the codebase review of 2026-11-07.
- 2026-11-07: E168 to E171, T165 to T167, each with a test or an injection that failed before the change: the new tests
  of the faces (an injected list of two axis edges), the reader, the failed copy and the command line; the three
  injections of the review and a project with no TreatWarningsAsErrors fail the dependency gate or the platform step;
  the affects gate found 27 files before the paths were added.
- 2026-11-07: run 42358673617 passed, with the probe builds of the platform step on Linux. Closed at v0.250.
