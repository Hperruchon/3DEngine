---
id: 0157
title: The live bodies leave the engine as one binary STL file, from the surface, the command line and the desktop
status: Done
phase: P8g
opened: 2026-10-31
depends-on: [0156]
governed-by: [0002, 0004, 0007, 0015, 0016, 0018, 0019, 0027, 0029, 0030, 0032, 0034, 0048]
writes:
  create:
    - tasks/TASK-0157-stl-export.md
    - docs/adr/0048-stl-export.md
    - Engine.Core/Export/StlExport.cs
    - Engine.Api.Http/Endpoints/ExportEndpoint.cs
    - Engine.Tests/Export/StlExportTests.cs
  modify:
    - Engine.Core/DocumentSession.cs
    - Engine.Api.Http/EngineSurface.cs
    - Engine.Cli/Cli.cs
    - Engine.Cli/Usage.cs
    - 3DEngine/NativeThreeDEngine.cs
    - 3DEngine/Ui/DesktopUi.cs
    - 3DEngine/Ui/PanelAction.cs
    - docs/adr/README.md
    - eng/test-floor.txt
    - docs/roadmap.md
    - docs/CURRENT-STATE.md
  forbid:
    - Engine.Contracts/**
    - 3DEngine.Core/**
    - 3DEngine.Vulkan/**
---

# TASK-0157 — The live bodies leave the engine as one binary STL file, from the surface, the command line and the desktop

This task uses Simplified Technical English (ASD-STE100). See `CLAUDE.md`, section "Language".

## Context

On 2026-10-31 the owner gave the order: after V3 and the determinism hash, "an STL export of the live bodies", with the
recommended answer to each question. P0.67 (v0.158) showed that the native triangles are the same bits on the three
platforms.

## Goal

A person, an agent and a script each get the live bodies of a document as one STL file.

## Scope (in)

1. `StlExport` and `DocumentSession.ReadGeometry` (ADR-0048 items 1 to 3).
2. `GET /export/stl`, `engine export stl` and the button "Export STL" (items 4 to 6).

## Scope (out)

- ASCII STL, colours, the visible bodies only, and STEP (K6).

## Acceptance criteria

- [x] A box gives twelve triangles with unit normals out of it, and each vertex is a position of the tessellation.
- [x] The route and the command line give the file and each refusal.
- [x] A clean build (`--no-incremental`) gives zero errors and zero warnings.
- [x] Continuous integration passes on the three runners, on the branch and on `main` after the merge.

## Outcome

Status: Done · commit (the merge of the branch p8g-stl-export) · v0.159

## Method

Mechanical: the file of a box read back triangle by triangle, two boxes, an empty document, the managed backend, the
            route, the verb, and one injection for each rule (7).
Judgement:  binary STL in millimetres; each live body, visible or hidden; a new read of the session in place of a
            query, because a file is not the result of a query.
Weakest:    no other program read the file; the test reads it with its own reader, and the button of the desktop has no
            test of its own.

## Progress

- 2026-10-31: ADR-0048, the writer, the read of the session, the route, the verb and the button. The test of the command
  line found that its export held the document file after it; the verb closes its session now. 4 new test cases.
- 2026-10-31: closed at v0.159.
