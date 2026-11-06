---
id: 0224
title: A void on the plane of a distant face is written, a replay and an export know an assembly, and two gates read the transitive form
status: Done
phase: P0.89
opened: 2026-11-06
depends-on: [0226]
governed-by: [0002, 0004, 0013, 0015, 0016, 0018, 0019, 0020, 0048, 0070]
writes:
  create:
    - tasks/TASK-0224-engine-and-gate-forms-of-the-twenty-fourth-review.md
  modify:
    - Engine.Core/Export/StepExport.cs
    - Engine.Core/Replay.cs
    - Engine.Core/Persistence/EntryCodec.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
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

- [x] Each finding has a test or an injection that fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p089-review-forms) · v0.210

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  a new code for an export of an assembly, E-EXPORT-KIND-REFUSED, because each code of the export means a
            body or a write; a header of the version 2 for an assembly only, so a part file keeps its bytes.
Weakest:    no engine before TASK-0202 ran against a header of the version 2; its code at `ab55b07` refuses each
            version but 1. No reader outside the repository read the voids of E152.

## Progress

- 2026-11-06: opened by the codebase review of 2026-11-06 (b).
- 2026-11-06: the write set takes `Engine.Core/DiagnosticCodes.cs` and `SchemaDiagnosticsEndpoint.cs` for the new code,
  and leaves `DocumentFile.cs`, which needs no change. E152: `Crossings` skips a face whose plane holds the point and
  that does not hold it. E153: `Replay.ReplayLog` takes the kind. E154 (Q1 A): the header of an assembly has the version
  2, and an assembly of the version 1 still opens. E155: `engine export` refuses an assembly with `E-EXPORT-KIND-REFUSED`.
  T145: the rules of ADR-0072 read the framework references and the SDK of each project that the application and the
  program of the phone reach. T146: a fact holds the rows of the list of the packages equal to the projects of the
  desktop. Seven injections each fail a test or a gate: the plane test as before, the replay as a part, the header of
  the version 1, the export as a part, the web SDK on the application, a framework reference in `Engine.Core`, and a host
  with no row.
- 2026-11-06: closed at v0.210.
