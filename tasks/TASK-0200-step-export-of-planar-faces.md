---
id: 0200
title: The live bodies leave the engine as one STEP file of their planar faces
status: Active
phase: K6a
opened: 2026-11-06
depends-on: [0184]
governed-by: [0002, 0004, 0013, 0016, 0018, 0019, 0030, 0048, 0070]
writes:
  create:
    - tasks/TASK-0200-step-export-of-planar-faces.md
    - docs/adr/0070-step-export-of-the-owned-kernel.md
    - Engine.Core/Export/StepExport.cs
    - Engine.Tests/Export/StepExportTests.cs
    - Engine.Tests/Export/StepReader.cs
  modify:
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Api.Http/Endpoints/ExportEndpoint.cs
    - Engine.Api.Http/EngineSurface.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Cli/Cli.cs
    - Engine.Cli/Usage.cs
    - docs/diagnostics.md
    - docs/adr/README.md
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
    - eng/test-floor.txt
  forbid:
    - Engine.Contracts/**
    - Engine.Kernel/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0200 — The live bodies leave the engine as one STEP file of their planar faces

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

Roadmap row K6 is "STEP export for the analytic subset" (6 to 12 evenings). It does not fit one session, so it comes in
slices; K6a is the first. On 2026-11-05 the owner ordered two lanes; this task is in the engine lane, with the standing
order of 2026-11-05. Since K4b the owned kernel gives the B-Rep of each live body (ADR-0058), and the kernel has planes
only until K5. ADR-0070 holds the decisions.

## Goal

`engine export step` and `GET /export/step` write each live body as the planar B-Rep of the owned kernel in one STEP
file of AP214, with the same bytes on each platform.

## Scope (in)

1. `StepExport` in `Engine.Core/Export`: the text of ADR-0070 items 1 to 3.
2. The route `GET /export/step` and the verb `engine export step`, with the read of `engine export stl`.
3. The code `E-EXPORT-BODY-FAILED`, registered.
4. Tests that read the file back and compare the counts of its faces, edges and vertices with `Brep.Of`, a hash of the
   bytes of a fixed body, and the same bytes in a culture with a decimal comma.

## Scope (out)

- The desktop button. The main session changes `3DEngine/**` for Android in the same days, so this lane does not.
- `BREP_WITH_VOIDS`, the name and the colour of a body, the surfaces of K5, and an import of STEP (anti-objective 14).
- A change of the kernel, of `Engine.Contracts`, of the log or of the document file.

## Acceptance criteria

- [x] A box, a plate with a hole, a body that a cut splits, a concave extrude and a document of the engine read back
      with the counts of their B-Reps.
- [x] The bytes of a fixed body have one SHA-256 in each culture; the pipeline runs the test on three runners.
- [x] A void, a fault and a coordinate out of the range of a double give `E-EXPORT-BODY-FAILED` and no file.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Notes for the implementer

The writer reads `KernelProjection.OfRecord`, so a body with a fault gives no solid. The kernel and the export use no
transcendental function; `DeterminismCallGateTests` scans `Engine.Core`.

## Outcome

Status: Done · commit (the merge of the branch k6a-step-export) · v0.TBD

## Method

Mechanical: the route and the verb, as for the STL export; the registration of the code.
Judgement:  AP214 and not AP242; one PRODUCT for the Document and one solid for each shell; a refusal of the whole
            file for one body that the export does not write, not a skip; no time in the header, so the bytes repeat.
Weakest:    no reader of STEP outside the repository read the files. `StepReader` checks the topology and the numbers,
            not each rule of AP214. An import into FreeCAD or another program is the next check.

## Progress

- 2026-11-06: opened by the engine lane of the order of 2026-11-05. AP214, not AP242, answered by the standing order of
  2026-11-05 (ADR-0070 item 1). The writer, the route, the verb, the code, ADR-0070 and the tests. Three injections
  each fail a test: the current culture in `Real`, no refusal of a void, and the same sense on each oriented edge.
