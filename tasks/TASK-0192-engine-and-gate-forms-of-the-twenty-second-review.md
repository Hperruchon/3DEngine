---
id: 0192
title: The kept solids belong to one Document, the faces of the kernel keep their holes, and the next forms of five gates fail
status: Done
phase: P0.84
opened: 2026-11-05
depends-on: [0191]
governed-by: [0004, 0008, 0012, 0016, 0018, 0021, 0028, 0031, 0033, 0035, 0037, 0039, 0045, 0053, 0054, 0057, 0058, 0059, 0060]
writes:
  create:
    - tasks/TASK-0192-engine-and-gate-forms-of-the-twenty-second-review.md
    - Engine.Tests/Governance/BuiltAssemblyReferenceTests.cs
  modify:
    - Engine.Contracts/Schema/FieldSchema.cs
    - Engine.Core/Sketch/FaceGone.cs
    - Engine.Core/Features/KernelProjection.cs
    - Engine.Core/Commands/PlaceImageCommandHandler.cs
    - Engine.Core/Queries/GetFaceAtQueryHandler.cs
    - Engine.Kernel/Rational.cs
    - Engine.Kernel/Brep.cs
    - Engine.Tests/Kernel/PlanarSubtractTests.cs
    - Engine.Tests/Sketch/FaceGoneTests.cs
    - Engine.Tests/Http/SchemaEndpointGateTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - Engine.Tests/Diagnostics/DiagnosticsScanner.cs
    - Engine.Tests/Diagnostics/DiagnosticsRegistryGateTests.cs
    - Engine.Tests/Governance/CodebaseReviewGateTests.cs
    - docs/adr/0059-references-by-the-identity-of-a-face.md
    - docs/adr/0060-unit-kind-in-the-command-schema.md
    - docs/templates.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0192 — The kept solids belong to one Document, the faces of the kernel keep their holes, and the next forms of five gates fail

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-05 (b) found E135 and T139 (medium), and E136 to E141, T136 to T138 and T140 (low). By
`docs/templates.md`, section 7, they come after the next product phase, and after P0.83. The standing order of
2026-11-05 answers Q3 to Q5 of the review with A. The first commit of this task corrects the write set, the names of
the ADR files and the field `governed-by` for the files that its tests need.

## Goal

The kept solids belong to one Document, the conversion of a rational number rounds one time, each face of the kernel
gives its holes on each side, a face-gone fault stays while the end of its chain has a body fault, the schema of each
number and each integer gives its unit, and the next form of each gate fails.

## Scope (in)

1. E135: the kept solids in the session, or under the identity of the Document and the key.
2. E136: one rounding in the subnormal range (Q5 A).
3. E137 and E138: the loops at a vertex with two outgoing edges, and the owner of a hole in an island.
4. E139: the previous face-gone fault stays (Q4 A).
5. E140 and E141, T140: a test of the members of the converter, the units of query parameters, and the rule for
   `integer` (Q3 A), with the amendment of ADR-0060.
6. T136 to T139: the forms of the review, each with an injection before the change.

## Scope (out)

- V85 to V87 (TASK-0191).

## Acceptance criteria

- [x] Each finding has a test or an injection that fails before the change and passes after it.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p084-engine-forms) · v0.195

## Method

Mechanical: a test or an injection for each finding, before the change.
Judgement:  the place of the kept solids, and the rule of T136 that reads the built assemblies.
Weakest:    the rule of E137 is proved on one touching vertex only; a vertex where three loops meet has no test. The
            test of T136 reads the assemblies of the test run, so it sees the projects that Engine.Tests references.

## Progress

- 2026-11-05: opened by the codebase review of 2026-11-05 (b).
- 2026-11-05: the write set takes `Engine.Contracts/Schema/FieldSchema.cs` for the unit kind `direction` (E141, with
  the amendment of ADR-0060 in the same commit), `FaceGone.cs` for E139, and a test file for T136. GetBodyInfo has no
  number parameter: its `distance` is a member of the result, which R-0053 holds, so E141 is the six parameters of
  GetFaceAt. Q3 to Q5 of the review are A, answered by the standing order of 2026-11-05.
- 2026-11-05: E135 to E141 and T136 to T140 with 21 new tests; 13 injections each fail a test.
- 2026-11-05: closed at v0.195.
