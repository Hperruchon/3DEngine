---
id: 0149
title: The store of the assets takes only a true name and gives a code for each error, and an arc follows the solver
status: Ready
phase: P0.63
opened: 2026-10-31
depends-on: [0148]
governed-by: [0002, 0004, 0012, 0013, 0016, 0018, 0019, 0021, 0028, 0031, 0033, 0037, 0044]
writes:
  create:
    - tasks/TASK-0149-engine-forms-of-the-seventeenth-review.md
  modify:
    - Engine.Core/Assets/AssetStore.cs
    - Engine.Core/DiagnosticCodes.cs
    - Engine.Core/Commands/AddSketchArcCommandHandler.cs
    - Engine.Api.Http/Endpoints/AssetsEndpoint.cs
    - Engine.Api.Http/Endpoints/SchemaDiagnosticsEndpoint.cs
    - Engine.Cli/Cli.cs
    - Engine.Tests/Assets/AssetStoreTests.cs
    - Engine.Tests/Commands/EngineDebtTests.cs
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

# TASK-0149 — The store of the assets takes only a true name and gives a code for each error, and an arc follows the solver

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

The codebase review of 2026-10-31 (c) found E113 (medium) and E114 to E118 (low). It comes after V2, the next product phase, by `docs/templates.md`, section 7; it starts only when V2 is merged.

## Goal

Each name, each error of the file system and each size of a body gives a coded answer from the store, the route and the command line, and a live arc follows the solver.

## Scope (in)

1. E113: the pattern of a name ends with `\z`, so a final line feed is not a name.
2. E114: `Read` and `Put` give a coded error for `IOException` and `UnauthorizedAccessException`, and the command line gives an exit code for each one.
3. E115: a put whose name holds a file that does not hash to the name writes the file again and replaces it.
4. E116: a put deletes its scratch file when the move did not occur, also after a cancellation.
5. E117: a live add of an arc refuses a start or an end whose distance from the centre is not greater than `SketchSolver.Tolerance`; under `ApplyingEntry` the rule of 0 stays.
6. E118: the route refuses a `Content-Length` over `MaximumBytes` before the read, and the 413 of the server gives `E-ASSET-TOO-LARGE`.

## Scope (out)

- The findings of the gates (P0.64) and of the desktop (P0.65).
- A command that names an asset (V2).

## Acceptance criteria

- [ ] Each finding has a test that fails before the change and passes after it.
- [ ] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [ ] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Progress

