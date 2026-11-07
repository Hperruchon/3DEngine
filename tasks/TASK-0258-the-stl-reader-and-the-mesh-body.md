---
id: 0258
title: The engine reads an STL file into a mesh body
status: Ready
phase: P9b
opened: 2026-11-07
depends-on: [0257]
governed-by: [0004, 0012, 0016, 0021, 0028, 0031, 0033, 0037, 0060, 0083]
writes:
  create:
    - tasks/TASK-0258-the-stl-reader-and-the-mesh-body.md
    - Engine.Core/Import/StlReader.cs
    - Engine.Core/Commands/ImportStlCommand.cs
    - Engine.Core/Commands/ImportStlCommandHandler.cs
    - Engine.Tests/Import/StlImportTests.cs
  modify:
    - Engine.Core/Hosting/HandlerCatalog.cs
    - docs/diagnostics.md
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

Status: Ready

## Method

Mechanical: to be written in the task.
Judgement:  to be written in the task.
Weakest:    to be written at the close.

## Progress

- 2026-11-07: opened by TASK-0257.
