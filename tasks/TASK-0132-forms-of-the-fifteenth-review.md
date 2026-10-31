---
id: 0132
title: The engine, the surface and the gates close the forms of the fifteenth review
status: Done
phase: P0.52
opened: 2026-10-31
depends-on: [0131]
governed-by: [0004, 0012, 0016, 0019, 0021, 0028, 0030, 0031, 0033, 0035, 0037, 0039]
writes:
  create:
    - tasks/TASK-0132-forms-of-the-fifteenth-review.md
  modify:
    - Engine.Core/Commands/AddSketchArcCommandHandler.cs
    - Engine.Core/Commands/AddSketchCircleCommandHandler.cs
    - Engine.Core/Commands/AddSketchConstraintCommandHandler.cs
    - Engine.Core/Commands/AddSketchLineCommandHandler.cs
    - Engine.Core/Commands/AddSketchPointCommandHandler.cs
    - Engine.Core/Commands/CreateSketchCommandHandler.cs
    - Engine.Core/Commands/DeleteSketchItemCommandHandler.cs
    - Engine.Core/Commands/ExtrudeCommandHandler.cs
    - Engine.Core/Commands/MoveSketchPointCommandHandler.cs
    - Engine.Core/Commands/SetSketchConstraintValueCommandHandler.cs
    - Engine.Core/Sketch/SketchConstraints.cs
    - Engine.Core/Sketch/SketchResults.cs
    - Engine.Api.Http/Endpoints/DocumentEndpoint.cs
    - Engine.Tests/Sketch/SketchGapTests.cs
    - Engine.Tests/Http/DocumentEndpointTests.cs
    - Engine.Tests/Governance/ConstantStrings.cs
    - Engine.Tests/Governance/CodebaseReviewGateTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Diagnostics/DiagnosticsScanner.cs
    - Engine.Tests/Diagnostics/DiagnosticsRegistryGateTests.cs
    - eng/repo-gates/DiscardedTasks.cs
    - docs/diagnostics.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - Engine.Cli/**
    - Engine.Geometry.Manifold/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0132 — The engine, the surface and the gates close the forms of the fifteenth review

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-31 found E103 (medium), E104 and E105 (low), T104 to T107 (medium), and T108 and T109
(low). `docs/templates.md`, section 7, puts them after the next product phase, so this task comes after R11a. The
standing answer of the owner takes Q1 A for E103.

## Goal

The engine, the surface and the gates close each form of the review, and a test holds each one.

## Scope (in)

1. E103 (Q1 A): under `ApplyingEntry`, the refusals of a tangent through the centre and of an arc at its centre do not
   act. E104: an add whose item identifier the sketch holds gives a refusal with its own message. E105: the routes of
   open and save refuse a repeated member.
2. T104 and T105: the reader of constants has the implicit usings and grants only the items `InternalsVisibleTo`.
   T106: each heading line of the ledger is `## v` or a level-three heading. T107: an output with the item name
   `Compile`. T108: a strict decoder and the category Format. T109: the constant values of `string.Join`.

## Scope (out)

- The desktop findings (P0.53). The write set gains a file when a fix needs it, with the reason in the ledger.

## Acceptance criteria

- [x] Each form of the review fails a test or the gate program.
- [x] An injection that restores each old rule fails a test.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p052-fifteenth-forms) · v0.134

## Method

Mechanical: the flag of an entry in two refusals and in the guard of an add, one option of the route, and the forms of
            the gates.
Judgement:  an add keeps its guard at an entry, because its second entry gives two items with one identifier; T106
            refuses each heading that is not "## v", because the ledger has no other heading; T108 refuses each
            character of the category Format, and no tracked file holds one.
Weakest:    T109 still reads the syntax for a call whose separator is not a literal; the implicit usings of an SDK
            other than Microsoft.NET.Sdk are not in the list, because the references of the compilation do not hold
            their assemblies.

## Progress

- 2026-10-31: opened by the codebase review of 2026-10-31 (TASK-0129).
- 2026-10-31: E103 (Q1 A), E104, E105, and T104 to T109. The write set gains `eng/repo-gates/DiscardedTasks.cs`, which
  takes the list of implicit usings of `ConstantStrings`, and `docs/diagnostics.md`, whose row of
  `E-SKETCH-ID-IN-USE` said that an open never raises the code, and the nine handlers that call `SketchResults.InUse`
  with its new parameter.
- 2026-10-31: 32 new test cases; 13 injections caught; closed at v0.134.
