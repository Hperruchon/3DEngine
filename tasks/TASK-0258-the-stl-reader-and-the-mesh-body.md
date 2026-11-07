---
id: 0258
title: The engine reads an STL file into a mesh body
status: Active
phase: P9b
opened: 2026-11-07
depends-on: [0257]
governed-by: [0002, 0004, 0007, 0012, 0013, 0014, 0015, 0016, 0018, 0019, 0021, 0024, 0028, 0029, 0031, 0033, 0035, 0036, 0037, 0039, 0044, 0048, 0051, 0052, 0053, 0054, 0055, 0058, 0059, 0060, 0061, 0070, 0074, 0075, 0076, 0077, 0083]
writes:
  create:
    - tasks/TASK-0258-the-stl-reader-and-the-mesh-body.md
    - Engine.Core/Import/StlReader.cs
    - Engine.Core/Assets/IAssetSource.cs
    - Engine.Core/Commands/ImportStlCommand.cs
    - Engine.Core/Commands/ImportStlCommandHandler.cs
    - Engine.Tests/Import/StlImportTests.cs
    - testdata/stl/bracket.stl
    - testdata/stl/bracket-damaged.stl
  modify:
    - Engine.Core/Assets/AssetStore.cs
    - Engine.Core/Commands/AddSketchReferenceCommandHandler.cs
    - Engine.Core/Commands/CreateDatumCommandHandler.cs
    - Engine.Core/Commands/SubtractCommandHandler.cs
    - Engine.Core/Commands/TranslateCommandHandler.cs
    - Engine.Core/Determinism/CrossPlatformProbe.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/Export/StepExport.cs
    - Engine.Core/Features/FeatureList.cs
    - Engine.Core/Features/KernelProjection.cs
    - Engine.Core/Features/Regeneration.cs
    - Engine.Core/Geometry/InProcessMeshBackend.cs
    - Engine.Core/Hosting/EngineHosting.cs
    - Engine.Core/Hosting/HandlerCatalog.cs
    - Engine.Core/Queries/GetFaceAtQueryHandler.cs
    - Engine.Core/Queries/GetFacesQueryHandler.cs
    - Engine.Core/Sketch/DatumResolver.cs
    - Engine.Geometry.Manifold/ManifoldGeometryBackend.cs
    - Engine.Api.Http/EngineHost.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Api.Http.Server/Program.cs
    - Engine.Api.Http.Server/ServerEngine.cs
    - Engine.Cli/Cli.cs
    - 3DEngine/DesktopSession.cs
    - Engine.Tests/Persistence/DocumentFileTests.cs
    - Engine.Tests/ReplayDeterminism/CrossPlatformHashTests.cs
    - Engine.Tests/Governance/DependencyDirectionGateTests.cs
    - docs/adr/0083-the-mesh-body-and-the-stl-import.md
    - docs/android-test-sheet.md
    - docs/diagnostics.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
---

# TASK-0258 — The engine reads an STL file into a mesh body

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

ADR-0083 (TASK-0257) records the mesh body and the command `ImportStl`, with the decisions D1 to D6 of the owner of
2026-11-07. This task builds the engine part of the demonstration "Open the same STL on desktop and Android".

## Goal

`ImportStl` reads binary and text STL from the store of the assets into a mesh body of each backend, with a code for
each fault and for each refused operation, and a test opens `bracket.stl` and `bracket-damaged.stl` on Windows, Linux,
macOS and the emulator with the same result.

## Scope (in)

1. `StlReader`, binary and text, with no arithmetic on the floats, and the codes of ADR-0083 item 3.
2. `ImportStl`, its handler, the store of the assets in the composition of the engine, and the mesh body in both
   backends.
3. The refusals of ADR-0083 item 4, and the limit of item 6 with a measure.
4. `bracket.stl` (an L bracket with two holes of 6 mm, made with the app and its STL export) and `bracket-damaged.stl`
   (the same file cut in the middle), and the test on the four platforms.

## Scope (out)

- The home screen, the file dialog and the picker (P9c, P9d).

## Acceptance criteria

- [ ] Each fault of the reader and each refused operation has a test.
- [ ] The two files give the same triangle count and the same code on Windows, Linux, macOS and the emulator.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Active

## Method

Mechanical: a test for each fault of the reader and each refused operation; the probe of ADR-0074 gives the export and
            the import of the bracket, so the pipeline compares them on four platforms.
Judgement:  the store reaches the handler through the composition of the engine (EngineHosting, HandlerCatalog, the
            hosts); the capability flag of a mesh backend is not added, because it changes the contract and TryGet answers
            without it; the files are the export of the probe, so a test holds them equal on each platform.
Weakest:    to be written at the close.

## Progress

- 2026-11-07: opened by TASK-0257.
- 2026-11-07: the reader, the import, the mesh body in both backends, the refusals, the store in the hosts, the probe, the
  two files and eight tests. The first test found that each Document is in millimetres (ADR-0015 item 7), so the unit
  check of ADR-0083 item 5 could never refuse; TASK-0257 corrected the record, and no code E-STL-UNIT exists.
