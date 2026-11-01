---
id: 0160
title: The export reads the document file only, the store takes parallel puts, and each edge of the export and the calibration gives a code
status: Ready
phase: P0.69
opened: 2026-11-01
depends-on: [0158]
governed-by: [0002, 0004, 0012, 0013, 0015, 0016, 0018, 0019, 0021, 0028, 0031, 0033, 0037, 0044, 0045, 0048]
writes:
  create:
    - tasks/TASK-0160-engine-forms-of-the-eighteenth-review.md
  modify:
    - Engine.Cli/Cli.cs
    - Engine.Core/Persistence/DocumentFile.cs
    - Engine.Core/Assets/AssetStore.cs
    - Engine.Core/Export/StlExport.cs
    - Engine.Core/Commands/CalibrateImageCommandHandler.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Api.Http/Endpoints/AssetsEndpoint.cs
    - Engine.Api.Http/Endpoints/ExportEndpoint.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Tests/Export/StlExportTests.cs
    - Engine.Tests/Assets/AssetStoreTests.cs
    - Engine.Tests/Images/ImageCommandTests.cs
    - docs/diagnostics.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - 3DEngine/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
    - eng/repo-gates/**
---

# TASK-0160 — The export reads the document file only, the store takes parallel puts, and each edge of the export and the calibration gives a code

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-11-01 found E119 (medium) and E120 to E124 (low). It comes after the next product phase, which Q1 of the review asks the owner for, by `docs/templates.md`, section 7.

## Goal

The export changes no document file, two puts of the same bytes each give the name, and each edge of the export and of the calibration gives a coded answer.

## Scope (in)

1. E119: `engine export stl` loads the document file with no journal and with read access only, and gives the warnings of the open.
2. E120: a put moves with no overwrite first, replaces only a file that does not hash to its name, and reads the name again before a failure.
3. E121: the export refuses a vertex or a normal that is not finite as a float, and a count over the limit of the file, each with a code and the envelope.
4. E122: the rows of `docs/diagnostics.md` name the export, or the export has its own code, and an empty path has its own message.
5. E123: a calibration divides `dx` and `dy` by the larger of their magnitudes before the root, or ADR-0045 item 6 and the message give the limit.
6. E124: the route gives 400 for a body that the server cannot read, or the comment is true.

## Scope (out)

- The findings of the gates (P0.70) and of the desktop (P0.71).

## Acceptance criteria

- [ ] Each finding has a test that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

